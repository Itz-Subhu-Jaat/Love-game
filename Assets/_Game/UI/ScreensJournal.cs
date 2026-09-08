using LoveGame.Core;
using LoveGame.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>Inventory screen: categorized item grid with counts and descriptions.</summary>
    public sealed class InventoryScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            var dim = Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1600f, 900f);

            UiFactory.Label(panel, "INVENTORY", 52, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 380f)).sizeDelta = new Vector2(600f, 80f);
            UiFactory.TextButton(panel, "BACK", 32, font, new Vector2(0f, 380f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f))
                .transform.SetParent(panel, true);

            UiFactory.TextButton(panel, "GIFTS", 30, font, new Vector2(-500f, 300f), new Vector2(260f, 70f), () => { Service.Pop(); GiveFirstGift(); });
            RectTransform content;
            var list = UiFactory.ScrollList(panel, "items", out content, 660f);
            list.anchoredPosition = new Vector2(0f, -30f);

            var inventory = Services.Get<InventoryService>();
            if (inventory == null) return;
            var items = inventory.All();
            int y = 0;
            if (items.Count == 0)
            {
                UiFactory.Label(content, "Empty - explore to find items!", 30, font, new Color(0.8f, 0.8f, 0.9f), new Vector2(0f, -50f)).sizeDelta = new Vector2(1200f, 60f);
                return;
            }
            var grouped = new System.Collections.Generic.List<(ItemCategory cat, string title)>();
            foreach (var cat in new[] { ItemCategory.Gift, ItemCategory.Fish, ItemCategory.Collectible, ItemCategory.Food, ItemCategory.Tool, ItemCategory.Furniture, ItemCategory.Quest, ItemCategory.Photo, ItemCategory.Cosmetic })
                grouped.Add((cat, cat.ToString()));
            foreach (var group in grouped)
            {
                var entries = inventory.ByCategory(group.cat);
                if (entries.Count == 0) continue;
                var header = UiFactory.Label(content, group.title.ToUpper(), 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, -y - 30f));
                header.sizeDelta = new Vector2(1400f, 50f);
                y += 70;
                foreach (var (def, count) in entries)
                {
                    var row = UiFactory.Panel(content, def.id, new Color(0.14f, 0.13f, 0.2f, 0.9f));
                    row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                    row.anchoredPosition = new Vector2(0f, -y - 40f);
                    row.sizeDelta = new Vector2(1420f, 80f);
                    UiFactory.Label(row, def.name, 28, font, Color.white, new Vector2(-620f, 0f)).sizeDelta = new Vector2(600f, 60f);
                    UiFactory.Label(row, def.description ?? "", 20, font, new Color(0.75f, 0.75f, 0.85f), new Vector2(80f, 0f)).sizeDelta = new Vector2(700f, 50f);
                    UiFactory.Label(row, $"x{count}", 28, font, new Color(1f, 0.85f, 0.4f), new Vector2(650f, 0f)).sizeDelta = new Vector2(100f, 50f);
                    y += 95;
                }
                y += 30;
            }
            content.sizeDelta = new Vector2(0f, y + 60f);
        }

        void GiveFirstGift()
        {
            var gifts = Services.Get<InventoryService>();
            var giftService = Services.Get<GiftService>();
            if (gifts == null || gifts == null) return;
            foreach (var (def, count) in gifts.ByCategory(ItemCategory.Gift))
            {
                giftService.Give(def.id);
                Service.Pop();
                return;
            }
            GameEvents.Publish(new NotificationEvent { Title = "No gifts", Body = "Find or buy a gift first.", Duration = 3f });
        }

    }

    /// <summary>Journal: collectibles completion + shared memories timeline.</summary>
    public sealed class JournalScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1600f, 940f);

            UiFactory.Label(panel, "MEMORIES & COLLECTION", 52, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 400f)).sizeDelta = new Vector2(900f, 80f);
            UiFactory.TextButton(panel, "BACK", 32, font, new Vector2(620f, 400f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            RectTransform content;
            var list = UiFactory.ScrollList(panel, "journal", out content, 720f);
            list.anchoredPosition = new Vector2(0f, -40f);

            int y = 0;
            // memories
            var memories = Services.Get<MemoryService>();
            if (memories != null)
            {
                var header = UiFactory.Label(content, "SHARED MEMORIES", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, -y - 30f));
                header.sizeDelta = new Vector2(1400f, 50f);
                y += 70;
                foreach (var memory in memories.All)
                {
                    var row = UiFactory.Panel(content, memory.id, new Color(0.14f, 0.13f, 0.2f, 0.9f));
                    row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                    row.anchoredPosition = new Vector2(0f, -y - 40f);
                    row.sizeDelta = new Vector2(1420f, 90f);
                    UiFactory.Label(row, memory.title, 28, font, new Color(1f, 0.85f, 0.9f), new Vector2(-600f, 14f)).sizeDelta = new Vector2(700f, 50f);
                    UiFactory.Label(row, memory.body, 22, font, new Color(0.75f, 0.75f, 0.85f), new Vector2(0f, -18f)).sizeDelta = new Vector2(1200f, 40f);
                    y += 105;
                }
                if (memories.Count == 0)
                {
                    UiFactory.Label(content, "No memories yet - share moments together!", 26, font, new Color(0.8f, 0.8f, 0.9f), new Vector2(0f, -y - 40f)).sizeDelta = new Vector2(1300f, 50f);
                    y += 80;
                }
            }
            // collectibles
            var collectibles = Services.Get<CollectibleService>();
            if (collectibles != null)
            {
                y += 40;
                var header2 = UiFactory.Label(content, "COLLECTIBLES", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, -y - 30f));
                header2.sizeDelta = new Vector2(1400f, 50f);
                y += 70;
                var catalog = Services.Get<World.RegionCatalogService>();
                if (catalog != null)
                {
                    foreach (var region in catalog.Regions)
                    {
                        var total = collectibles.TotalInRegion(region.Id);
                        if (total == 0) continue;
                        float completion = collectibles.RegionCompletion(region.Id);
                        var row = UiFactory.Panel(content, $"col_{region.Id}", new Color(0.14f, 0.13f, 0.2f, 0.9f));
                        row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                        row.anchoredPosition = new Vector2(0f, -y - 35f);
                        row.sizeDelta = new Vector2(1420f, 70f);
                        UiFactory.Label(row, region.DisplayName, 26, font, Color.white, new Vector2(-560f, 0f)).sizeDelta = new Vector2(700f, 50f);
                        UiFactory.Label(row, $"{Mathf.RoundToInt(completion * 100)}%", 26, font, new Color(1f, 0.85f, 0.4f), new Vector2(580f, 0f)).sizeDelta = new Vector2(120f, 50f);
                        y += 85;
                    }
                }
                UiFactory.Label(content, $"Total collected: {collectibles.TotalCollected}", 28, font, new Color(0.9f, 0.9f, 1f), new Vector2(0f, -y - 40f)).sizeDelta = new Vector2(1300f, 50f);
                y += 80;
            }
            // relationship
            var progress = SaveSystem.Current.progress;
            UiFactory.Label(content, $"Bond level {progress.relationshipLevel}  ({progress.relationshipXp:F0} xp)", 30, font, new Color(1f, 0.6f, 0.75f), new Vector2(0f, -y - 60f)).sizeDelta = new Vector2(1300f, 60f);
            content.sizeDelta = new Vector2(0f, y + 140f);
        }
    }
}
