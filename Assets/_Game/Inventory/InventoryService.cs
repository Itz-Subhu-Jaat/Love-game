using System;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Inventory
{
    public enum ItemCategory { Gift, Fish, Collectible, Food, Tool, Furniture, Quest, Photo, Cosmetic }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string name;
        public string description;
        public ItemCategory category;
        public bool stackable = true;
        public int maxStack = 99;
        public int value = 1;
        public string color = "#FFFFFF";
        public string regionId;
    }

    /// <summary>Generic inventory service: stackable/unique items, categories, events, save.</summary>
    public sealed class InventoryService : IGameService
    {
        public string ServiceName => "Inventory";

        public readonly Dictionary<string, ItemDefinition> Catalog = new Dictionary<string, ItemDefinition>();
        readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        public event System.Action Changed;

        public int DistinctItemCount => _counts.Count;

        public void Initialize()
        {
            var text = Resources.Load<TextAsset>("Data/items");
            if (text != null)
            {
                var file = JsonUtility.FromJson<ItemCatalogFile>(text.text);
                if (file?.items != null)
                    foreach (var item in file.items)
                        if (!string.IsNullOrEmpty(item.id)) Catalog[item.id] = item;
            }
            foreach (var entry in SaveSystem.Current.inventory.entries)
                _counts[entry.itemId] = Mathf.Max(0, entry.count);
            Log.Info("Inventory", $"{Catalog.Count} item definitions, {_counts.Count} owned");
        }

        public void Tick(float delta) { }

        public bool TryGetDefinition(string itemId, out ItemDefinition def) => Catalog.TryGetValue(itemId, out def);

        public bool Add(string itemId, int count = 1)
        {
            if (count <= 0) return false;
            if (!Catalog.TryGetValue(itemId, out var def))
            {
                Log.Warn("Inventory", $"unknown item '{itemId}'");
                return false;
            }
            int current = CountOf(itemId);
            int max = def.stackable ? def.maxStack : 1;
            int room = max - current;
            if (room <= 0) return false;
            int added = Mathf.Min(room, count);
            _counts[itemId] = current + added;
            Persist();
            GameEvents.Publish(new ItemAddedEvent { ItemId = itemId, Count = added });
            Changed?.Invoke();
            return added == count;
        }

        public bool Remove(string itemId, int count = 1)
        {
            int current = CountOf(itemId);
            if (current < count) return false;
            if (current - count <= 0) _counts.Remove(itemId);
            else _counts[itemId] = current - count;
            Persist();
            GameEvents.Publish(new ItemRemovedEvent { ItemId = itemId, Count = count });
            Changed?.Invoke();
            return true;
        }

        public int CountOf(string itemId) => _counts.TryGetValue(itemId, out var c) ? c : 0;
        public bool Has(string itemId, int count = 1) => CountOf(itemId) >= count;

        public List<(ItemDefinition def, int count)> ByCategory(ItemCategory category)
        {
            var result = new List<(ItemDefinition, int)>();
            foreach (var kv in _counts)
            {
                if (Catalog.TryGetValue(kv.Key, out var def) && def.category == category)
                    result.Add((def, kv.Value));
            }
            return result;
        }

        public List<(ItemDefinition def, int count)> All()
        {
            var result = new List<(ItemDefinition, int)>();
            foreach (var kv in _counts)
                if (Catalog.TryGetValue(kv.Key, out var def)) result.Add((def, kv.Value));
            return result;
        }

        void Persist()
        {
            var entries = SaveSystem.Current.inventory.entries;
            entries.Clear();
            foreach (var kv in _counts)
                entries.Add(new InventoryEntry { itemId = kv.Key, count = kv.Value });
        }

        public void Shutdown() { }
    }

    [Serializable]
    public class ItemCatalogFile
    {
        public ItemDefinition[] items;
    }
}
