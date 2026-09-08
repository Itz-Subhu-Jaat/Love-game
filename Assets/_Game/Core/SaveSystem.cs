using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LoveGame.Core
{
    // ------------------------------------------------------- save data model

    [Serializable]
    public class PlayerSaveData
    {
        public string currentRegion = "azure_haven";
        public float positionX, positionY, positionZ;
        public float rotationY;
        public int coins;
    }

    [Serializable]
    public class WorldSaveData
    {
        public float worldTimeHour = 10f;                       // 0..24
        public string weather = "Clear";
        public List<string> discoveredRegions = new List<string>();
        public List<string> discoveredFastTravel = new List<string>();
        public List<string> completedPois = new List<string>();
    }

    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count = 1;
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<InventoryEntry> entries = new List<InventoryEntry>();
    }

    [Serializable]
    public class CollectibleEntry
    {
        public string itemId;
        public string regionId;
    }

    [Serializable]
    public class MemoryEntry
    {
        public string id;
        public string title;
        public string body;
        public string regionId;
        public string kind;
        public float worldHour;
    }

    [Serializable]
    public class ProgressSaveData
    {
        public List<CollectibleEntry> collectibles = new List<CollectibleEntry>();
        public List<MemoryEntry> memories = new List<MemoryEntry>();
        public List<string> completedActivities = new List<string>();
        public List<string> giftsGiven = new List<string>();
        public float relationshipXp = 0f;
        public int relationshipLevel = 1;
    }

    [Serializable]
    public class FurniturePlacementSave
    {
        public string furnitureId;
        public float x, y, z, rotationY;
        public string homeId = "beach_house";
    }

    [Serializable]
    public class HomeSaveData
    {
        public List<FurniturePlacementSave> furniture = new List<FurniturePlacementSave>();
    }

    [Serializable]
    public class VehicleSaveEntry
    {
        public string vehicleId;
        public string kind;
        public string regionId;
        public float x, y, z, rotationY;
    }

    [Serializable]
    public class WorldObjectsSaveData
    {
        public List<VehicleSaveEntry> vehicles = new List<VehicleSaveEntry>();
    }

    [Serializable]
    public class ContentSaveData
    {
        public List<string> downloadedPacks = new List<string>();
        public List<string> packVersions = new List<string>();
    }

    [Serializable]
    public class SaveData
    {
        public int schemaVersion = SaveSystem.CurrentSchemaVersion;
        public string savedAtUtc = "";
        public long playtimeSeconds = 0;
        public PlayerSaveData player = new PlayerSaveData();
        public WorldSaveData world = new WorldSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public ProgressSaveData progress = new ProgressSaveData();
        public HomeSaveData home = new HomeSaveData();
        public WorldObjectsSaveData worldObjects = new WorldObjectsSaveData();
        public ContentSaveData content = new ContentSaveData();
    }

    // ------------------------------------------------------------- providers

    /// <summary>Storage abstraction so a cloud provider can be added later without touching gameplay.</summary>
    public interface ISaveProvider
    {
        string Name { get; }
        bool Exists(string slot);
        void Write(string slot, string json);
        string Read(string slot);
        void Delete(string slot);
    }

    /// <summary>Atomic JSON file storage with a rotating backup. One invalid field never destroys the whole save.</summary>
    public sealed class LocalSaveProvider : ISaveProvider
    {
        public string Name => "Local";

        static string SlotPath(string slot) => Path.Combine(Application.persistentDataPath, $"Saves/{slot}.json");

        public bool Exists(string slot) => File.Exists(SlotPath(slot));

        public void Write(string slot, string json)
        {
            var path = SlotPath(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // atomic: write temp, rotate old to .bak, move temp in
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                var bak = path + ".bak";
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(path, bak);
            }
            File.Move(tmp, path);
        }

        public string Read(string slot)
        {
            var path = SlotPath(slot);
            if (File.Exists(path)) return TryRead(path);
            var bak = path + ".bak";
            if (File.Exists(bak)) return TryRead(bak);
            return null;
        }

        static string TryRead(string p)
        {
            try { return File.ReadAllText(p); }
            catch (Exception e) { Log.Error("Save", $"unreadable save file {p}: {e.Message}"); return null; }
        }

        public void Delete(string slot)
        {
            var path = SlotPath(slot);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }

    /// <summary>Versioned save facade: serialize, migrate, autosave. Gameplay only talks to this class.</summary>
    public static class SaveSystem
    {
        public const int CurrentSchemaVersion = 1;
        public const string MainSlot = "main";

        static ISaveProvider _provider = new LocalSaveProvider();
        public static ISaveProvider Provider
        {
            get => _provider;
            set => _provider = value ?? new LocalSaveProvider();
        }

        /// <summary>Current in-memory save state. Tests and tools may reset it directly.</summary>
        public static SaveData Current { get; set; } = new SaveData();
        public static bool HasSave => _provider.Exists(MainSlot);
        static double _playtimeAccumulator;
        static double _autosaveTimer;

        public static bool Load(string slot = MainSlot)
        {
            try
            {
                var json = _provider.Read(slot);
                if (string.IsNullOrEmpty(json)) { Current = new SaveData(); return false; }
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) { Log.Warn("Save", "parsed null, starting fresh"); Current = new SaveData(); return false; }
                Current = Migrate(data);
                _playtimeAccumulator = Current.playtimeSeconds;
                Log.Info("Save", $"loaded slot '{slot}' schema v{Current.schemaVersion}");
                GameEvents.Publish(new SaveLoadedEvent { Success = true });
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Save", $"load failed: {e.Message}");
                Current = new SaveData();
                GameEvents.Publish(new SaveLoadedEvent { Success = false });
                return false;
            }
        }

        /// <summary>Forward migrations. Each step upgrades exactly one schema version.</summary>
        static SaveData Migrate(SaveData data)
        {
            if (data.schemaVersion < 1)
            {
                // schema 0 -> 1 (historical placeholder, keeps the pattern real)
                data.schemaVersion = 1;
                Log.Info("Save", "migrated schema 0 -> 1");
            }
            if (data.player == null) data.player = new PlayerSaveData();
            if (data.world == null) data.world = new WorldSaveData();
            if (data.inventory == null) data.inventory = new InventorySaveData();
            if (data.progress == null) data.progress = new ProgressSaveData();
            if (data.home == null) data.home = new HomeSaveData();
            if (data.worldObjects == null) data.worldObjects = new WorldObjectsSaveData();
            if (data.content == null) data.content = new ContentSaveData();
            return data;
        }

        public static void Save(string slot = MainSlot)
        {
            try
            {
                Current.schemaVersion = CurrentSchemaVersion;
                Current.savedAtUtc = DateTime.UtcNow.ToString("o");
                Current.playtimeSeconds = (long)_playtimeAccumulator;
                _provider.Write(slot, JsonUtility.ToJson(Current, false));
                GameEvents.Publish(new SaveWrittenEvent { Slot = slot });
            }
            catch (Exception e) { Log.Error("Save", $"save failed: {e.Message}"); }
        }

        public static void TickPlaytime(float delta)
        {
            _playtimeAccumulator += delta;
            _autosaveTimer += delta;
            if (_autosaveTimer >= 120f)
            {
                _autosaveTimer = 0f;
                Save();
            }
        }

        public static void Delete(string slot = MainSlot)
        {
            _provider.Delete(slot);
            Current = new SaveData();
        }
    }
}
