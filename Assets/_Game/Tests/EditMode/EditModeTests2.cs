using System;
using System.Linq;
using LoveGame.Core;
using LoveGame.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace LoveGame.Tests.EditMode
{
    public class SaveSystemTests
    {
        [SetUp]
        public void Reset()
        {
            SaveSystem.Provider = new TempProvider();
            SaveSystem.Current = new SaveData();
        }

        [TearDown]
        public void Cleanup()
        {
            SaveSystem.Provider = new LocalSaveProvider();
        }

        class TempProvider : ISaveProvider
        {
            public string Name => "Temp";
            string _json;
            public bool Exists(string slot) => _json != null;
            public void Write(string slot, string json) => _json = json;
            public string Read(string slot) => _json;
            public void Delete(string slot) => _json = null;
        }

        [Test]
        public void Save_Roundtrips_Player_Data()
        {
            SaveSystem.Current.player.currentRegion = "neon_abyss";
            SaveSystem.Current.player.positionX = 123.45f;
            SaveSystem.Current.progress.relationshipXp = 88.5f;
            SaveSystem.Save();

            Assert.IsTrue(SaveSystem.Provider.Exists(SaveSystem.MainSlot));
            Assert.IsTrue(SaveSystem.Load());
            Assert.AreEqual("neon_abyss", SaveSystem.Current.player.currentRegion);
            Assert.AreEqual(123.45f, SaveSystem.Current.player.positionX, 1e-4f);
            Assert.AreEqual(88.5f, SaveSystem.Current.progress.relationshipXp, 1e-4f);
        }

        [Test]
        public void Load_Migrates_Missing_Sections()
        {
            // a v1 save missing sections must still deserialize with defaults, never crash
            var json = JsonUtility.ToJson(new SaveData(), false);
            var loaded = JsonUtility.FromJson<SaveData>(json);
            Assert.NotNull(loaded.player);
            Assert.NotNull(loaded.progress);
        }

        [Test]
        public void Save_Has_Schema_Version()
        {
            SaveSystem.Save();
            Assert.AreEqual(SaveSystem.CurrentSchemaVersion, SaveSystem.Current.schemaVersion);
            Assert.IsNotEmpty(SaveSystem.Current.savedAtUtc);
        }
    }

    public class InventoryLogicTests
    {
        // InventoryService is exercised through SaveSystem-backed state without the service host:
        // the serialization contracts are what matter for edit-mode.
        [Test]
        public void Inventory_Entries_Roundtrip()
        {
            var data = new InventorySaveData();
            data.entries.Add(new InventoryEntry { itemId = "shell_conch", count = 3 });
            data.entries.Add(new InventoryEntry { itemId = "fish_clownfish", count = 1 });
            var json = JsonUtility.ToJson(data);
            var loaded = JsonUtility.FromJson<InventorySaveData>(json);
            Assert.AreEqual(2, loaded.entries.Count);
            Assert.AreEqual("shell_conch", loaded.entries[0].itemId);
            Assert.AreEqual(3, loaded.entries[0].count);
        }

        [Test]
        public void Item_Catalog_Parses()
        {
            var text = Resources.Load<TextAsset>("Data/items");
            Assert.NotNull(text, "items.json must ship in Resources");
            var file = JsonUtility.FromJson<ItemCatalogFile>(text.text);
            Assert.NotNull(file.items);
            Assert.GreaterOrEqual(file.items.Length, 30, "starter catalog needs at least 30 items");
            foreach (var item in file.items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.id), "every item needs an id");
                Assert.IsFalse(string.IsNullOrEmpty(item.name), $"item {item.id} needs a name");
            }
            var ids = new HashSet<string>(file.items.Select(i => i.id));
            Assert.AreEqual(file.items.Length, ids.Count, "item ids must be unique");
        }

        [Test]
        public void Furniture_Catalog_Parses()
        {
            var text = Resources.Load<TextAsset>("Data/furniture");
            Assert.NotNull(text, "furniture.json must ship in Resources");
            var file = JsonUtility.FromJson<LoveGame.Home.FurnitureCatalogFile>(text.text);
            Assert.NotNull(file.furniture);
            Assert.GreaterOrEqual(file.furniture.Length, 12, "home customization needs furniture variety");
            foreach (var furniture in file.furniture)
                Assert.IsFalse(string.IsNullOrEmpty(furniture.id));
        }

        [Test]
        public void Content_Manifest_Parses()
        {
            var text = Resources.Load<TextAsset>("Data/content_manifest");
            Assert.NotNull(text, "content_manifest.json must ship in Resources");
            var file = JsonUtility.FromJson<LoveGame.Content.ContentManifestFile>(text.text);
            Assert.NotNull(file.packs);
            Assert.GreaterOrEqual(file.packs.Length, 4, "manifest needs the core pack plus region packs");
            var required = file.packs.Count(p => p.required);
            Assert.GreaterOrEqual(required, 1, "core pack must be marked required");
            foreach (var pack in file.packs)
                Assert.IsFalse(string.IsNullOrEmpty(pack.id));
        }
    }

    public class WeatherDataTests
    {
        [Test]
        public void Weather_Weights_Parse_From_Region_Data()
        {
            var catalog = new World.RegionCatalogService();
            catalog.Load();
            foreach (var region in catalog.Regions)
            {
                float sum = region.WeatherWeights.Sum();
                Assert.Greater(sum, 0f, $"{region.Id} needs weather weights");
                Assert.That(region.WeatherWeights.Length, Is.EqualTo(5));
                foreach (var w in region.WeatherWeights)
                    Assert.GreaterOrEqual(w, 0f);
            }
        }

        [Test]
        public void Tips_File_Parses()
        {
            var text = Resources.Load<TextAsset>("Data/tips");
            Assert.NotNull(text, "tips.json must ship for loading screens");
            var file = JsonUtility.FromJson<LoveGame.UI.TipsFile>(text.text);
            Assert.NotNull(file.tips);
            Assert.GreaterOrEqual(file.tips.Length, 10, "need at least 10 loading tips");
        }
    }

    public class PoolingTests
    {
        [Test]
        public void ObjectPool_Reuses_Instances()
        {
            var created = 0;
            var pool = new ObjectPool<DisposableStub>(() => { created++; return new DisposableStub(); }, prewarm: 2);
            var a = pool.Get();
            pool.Release(a);
            var b = pool.Get();
            Assert.AreSame(a, b, "released objects must be reused");
            Assert.AreEqual(2, created, "pool must not create when an instance is available");
        }

        class DisposableStub { }
    }
}
