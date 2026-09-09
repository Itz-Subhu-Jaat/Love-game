using System;
using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.World;
using UnityEngine;

namespace LoveGame.Inventory
{
    /// <summary>
    /// Collectibles (shells, crystals, postcards, artifacts...) with region/total completion
    /// percentages and journal data. Spawning happens through world POIs; collection flows here.
    /// </summary>
    public sealed class CollectibleService : IGameService
    {
        public string ServiceName => "Collectibles";

        InventoryService _inventory;
        readonly Dictionary<string, CollectibleEntry> _collected = new Dictionary<string, CollectibleEntry>();

        public event System.Action Changed;

        public void Initialize()
        {
            _inventory = Services.Get<InventoryService>();
            foreach (var entry in SaveSystem.Current.progress.collectibles)
                _collected[entry.itemId] = entry;
        }

        public void Tick(float delta) { }

        public bool Collect(string itemId, string regionId)
        {
            if (_collected.ContainsKey(itemId)) return false;
            var entry = new CollectibleEntry { itemId = itemId, regionId = regionId };
            _collected[itemId] = entry;
            SaveSystem.Current.progress.collectibles.Add(entry);
            _inventory?.Add(itemId, 1);
            GameEvents.Publish(new CollectibleCollectedEvent { ItemId = itemId, RegionId = regionId });
            GameEvents.Publish(new NotificationEvent { Title = "Collectible found", Body = ItemName(itemId), Duration = 3f });
            SaveSystem.Save();
            Changed?.Invoke();
            return true;
        }

        public string ItemName(string itemId) => _inventory != null && _inventory.TryGetDefinition(itemId, out var def) ? def.name : itemId;

        public bool IsCollected(string itemId) => _collected.ContainsKey(itemId);

        public int TotalCollected => _collected.Count;

        public int TotalInRegion(string regionId)
        {
            var count = 0;
            if (_inventory == null) return 0;
            foreach (var kv in _inventory.Catalog)
                if (kv.Value.category == ItemCategory.Collectible && kv.Value.regionId == regionId) count++;
            return count;
        }

        public int CollectedInRegion(string regionId)
        {
            var count = 0;
            foreach (var entry in _collected.Values)
                if (entry.regionId == regionId) count++;
            return count;
        }

        public float RegionCompletion(string regionId)
        {
            var total = TotalInRegion(regionId);
            return total == 0 ? 0f : CollectedInRegion(regionId) / (float)total;
        }

        public List<CollectibleEntry> Journal() => new List<CollectibleEntry>(_collected.Values);

        public void Shutdown() { }
    }

    /// <summary>
    /// Shared memories journal: couple moments, photos, gifts, achievements - the emotional
    /// progression backbone. Optional by design; nothing gates gameplay behind it.
    /// </summary>
    public sealed class MemoryService : IGameService
    {
        public string ServiceName => "Memories";

        readonly List<MemoryEntry> _memories = new List<MemoryEntry>();
        int _counter;

        public event System.Action Changed;

        public void Initialize()
        {
            _memories.AddRange(SaveSystem.Current.progress.memories);
            _counter = _memories.Count;
        }

        public void Tick(float delta) { }

        public MemoryEntry Record(string sourceId, string title, string kind, string body, string regionId = null)
        {
            var entry = new MemoryEntry
            {
                id = $"mem_{_counter++:0000}_{sourceId}",
                title = title,
                body = body,
                kind = kind,
                regionId = regionId ?? "azure_haven",
                worldHour = Services.TryGet<World.DayNightCycle>(out var dayNight) ? dayNight.Hour : 12f,
            };
            _memories.Add(entry);
            SaveSystem.Current.progress.memories.Add(entry);
            SaveSystem.Save();
            GameEvents.Publish(new MemoryRecordedEvent { MemoryId = entry.id });
            GameEvents.Publish(new NotificationEvent { Title = "Memory saved", Body = title, Duration = 3f });
            Changed?.Invoke();
            return entry;
        }

        public IReadOnlyList<MemoryEntry> All => _memories;
        public int Count => _memories.Count;

        public void RecordPhoto(string filePath, string regionId)
        {
            Record($"photo_{_counter}", "Photo together", "photo", "A photo of a shared moment.", regionId);
        }

        public void Shutdown() { }
    }

    /// <summary>Giving gifts: inventory consumption + partner reaction + memory + progression.</summary>
    public sealed class GiftService : IGameService
    {
        public string ServiceName => "Gifts";

        InventoryService _inventory;
        MemoryService _memories;

        public void Initialize()
        {
            _inventory = Services.Get<InventoryService>();
            _memories = Services.Get<MemoryService>();
        }

        public void Tick(float delta) { }

        public bool CanGive(string itemId) => _inventory != null && _inventory.Has(itemId) &&
            _inventory.TryGetDefinition(itemId, out var def) && def.category == ItemCategory.Gift;

        public bool Give(string itemId, string regionId = null)
        {
            if (!CanGive(itemId)) return false;
            if (!_inventory.TryGetDefinition(itemId, out var def)) return false;
            if (!_inventory.Remove(itemId, 1)) return false;

            if (!SaveSystem.Current.progress.giftsGiven.Contains(itemId))
                SaveSystem.Current.progress.giftsGiven.Add(itemId);
            _memories?.Record($"gift_{itemId}", $"Gifted: {def.name}", "gift", def.description ?? "A gift from the heart.", regionId);
            GameEvents.Publish(new GiftGivenEvent { ItemId = itemId });
            GameEvents.Publish(new NotificationEvent { Title = "Gift given", Body = def.name, Duration = 3f });
            SaveSystem.Save();
            return true;
        }

        public IReadOnlyList<string> Given => SaveSystem.Current.progress.giftsGiven;

        public void Shutdown() { }
    }
}
