using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Content;
using LoveGame.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>Settings: graphics quality/FPS, audio sliders, controls sensitivity, downloads policy.</summary>
    public sealed class SettingsScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1500f, 980f);

            UiFactory.Label(panel, "SETTINGS", 52, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 420f)).sizeDelta = new Vector2(600f, 80f);
            UiFactory.TextButton(panel, "BACK", 32, font, new Vector2(560f, 420f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            var s = GameConfig.Settings;
            int y = 300;

            UiFactory.Label(panel, "GRAPHICS", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, y)).sizeDelta = new Vector2(400f, 50f);
            y -= 80;
            foreach (var level in new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" })
            {
                var qualityLevel = level;
                UiFactory.TextButton(panel, level, 28, font, new Vector2(-450f + System.Array.IndexOf(new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, level) * 300f, y), new Vector2(280f, 80f), () =>
                {
                    GameConfig.Quality = (QualityLevel)System.Array.IndexOf(new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, qualityLevel);
                    ApplyQuality();
                }, s.qualityLevel == System.Array.IndexOf(new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, level)
                    ? new Color(0.95f, 0.35f, 0.5f, 0.95f) : new Color(0.35f, 0.4f, 0.55f, 0.9f));
            }
            y -= 110;
            UiFactory.TextButton(panel, s.fpsTarget >= 55 ? "FPS TARGET: 60" : "FPS TARGET: 30", 28, font, new Vector2(0f, y), new Vector2(440f, 80f), () =>
            {
                GameConfig.FpsTarget = s.fpsTarget >= 55 ? 30 : 60;
                Application.targetFrameRate = GameConfig.FpsTarget;
                Service.Pop();
                Service.Push(new SettingsScreen());
            });
            y -= 110;

            UiFactory.Label(panel, "AUDIO", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, y)).sizeDelta = new Vector2(400f, 50f);
            y -= 90;
            AddSlider(panel, "Master volume", 0f, 1f, s.masterVolume, font, y, v => { s.masterVolume = v; ApplyAudio(); }); y -= 100;
            AddSlider(panel, "Music", 0f, 1f, s.musicVolume, font, y, v => { s.musicVolume = v; ApplyAudio(); }); y -= 100;
            AddSlider(panel, "Ambience", 0f, 1f, s.ambienceVolume, font, y, v => { s.ambienceVolume = v; ApplyAudio(); }); y -= 100;
            AddSlider(panel, "Effects", 0f, 1f, s.sfxVolume, font, y, v => { s.sfxVolume = v; ApplyAudio(); }); y -= 120;

            UiFactory.Label(panel, "CONTROLS", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, y)).sizeDelta = new Vector2(400f, 50f);
            y -= 90;
            AddSlider(panel, "Look sensitivity", 0.3f, 3f, s.lookSensitivity, font, y, v => { s.lookSensitivity = v; GameConfig.Save(); }); y -= 100;
            AddSlider(panel, "Joystick size", 0.7f, 1.4f, s.joystickSize, font, y, v => { s.joystickSize = v; GameConfig.Save(); }); y -= 100;
            UiFactory.TextButton(panel, s.invertLookY ? "INVERT Y: ON" : "INVERT Y: OFF", 28, font, new Vector2(0f, y), new Vector2(440f, 80f), () =>
            {
                s.invertLookY = !s.invertLookY;
                GameConfig.Save();
                Service.Pop();
                Service.Push(new SettingsScreen());
            });
            y -= 120;

            UiFactory.TextButton(panel, s.wifiOnlyDownloads ? "DOWNLOADS: WI-FI ONLY" : "DOWNLOADS: ANY NETWORK", 28, font, new Vector2(0f, y), new Vector2(640f, 80f), () =>
            {
                s.wifiOnlyDownloads = !s.wifiOnlyDownloads;
                GameConfig.Save();
                Service.Pop();
                Service.Push(new SettingsScreen());
            });
        }

        void AddSlider(RectTransform panel, string label, float min, float max, float value, Font font, float y, System.Action<float> onChange)
        {
            var slider = UiFactory.Slider(panel, label, min, max, value, font, new Vector2(0f, y), 900f, onChange);
            slider.transform.SetParent(panel, true);
        }
        void ApplyQuality()
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(GameConfig.Settings.qualityLevel, 0, 3), true);
            GameEvents.Publish(new QualityChangedEvent { Level = GameConfig.Settings.qualityLevel });
        }

        void ApplyAudio()
        {
            GameConfig.Save();
            Services.TryGet<Audio.AudioService>(out var audio);
            audio?.ApplyVolumes();
        }

    }

    /// <summary>World map: region grid, discovered state, fast travel.</summary>
    public sealed class WorldMapScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.06f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1800f, 1000f);

            UiFactory.Label(panel, "WORLD MAP", 52, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 440f)).sizeDelta = new Vector2(700f, 80f);
            UiFactory.TextButton(panel, "BACK", 32, font, new Vector2(700f, 440f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            var catalog = Services.Get<World.RegionCatalogService>();
            var travel = Services.Get<World.FastTravelService>();
            if (catalog == null) return;

            // world grid layout derived from region world placement
            var discovered = SaveSystem.Current.world.discoveredRegions;
            const int cols = 6;
            float cellW = 270f, cellH = 250f;
            float startX = -(cols - 1) * cellW * 0.5f;
            float startY = 320f;
            int index = 0;
            foreach (var region in catalog.Regions)
            {
                int col = index % cols;
                int row = index / cols;
                var cell = UiFactory.Panel(panel, $"region_{region.Id}", new Color(0.13f, 0.15f, 0.22f, 0.95f));
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
                cell.anchoredPosition = new Vector2(startX + col * cellW, startY - row * cellH);
                cell.sizeDelta = new Vector2(cellW - 18f, cellH - 18f);

                bool known = discovered.Contains(region.Id);
                UiFactory.Label(cell, known ? region.DisplayName : "???", 26, font, known ? Color.white : new Color(0.5f, 0.5f, 0.6f), new Vector2(0f, 80f)).sizeDelta = new Vector2(cellW - 40f, 40f);
                UiFactory.Label(cell, known ? region.Description : "Undiscovered region", 19, font, new Color(0.7f, 0.72f, 0.8f), new Vector2(0f, 20f)).sizeDelta = new Vector2(cellW - 40f, 80f);

                if (travel != null)
                {
                    var fastTravelPoints = region.FastTravel;
                    bool anyDiscovered = false;
                    foreach (var ft in fastTravelPoints)
                        if (travel.IsDiscovered(region.Id, ft.id)) { anyDiscovered = true; break; }
                    if (anyDiscovered)
                    {
                        UiFactory.TextButton(cell, "TRAVEL", 24, font, new Vector2(0f, -82f), new Vector2(180f, 64f), () =>
                        {
                            foreach (var ft in fastTravelPoints)
                            {
                                if (!travel.IsDiscovered(region.Id, ft.id)) continue;
                                if (travel.Travel(region.Id, ft.id))
                                {
                                    Service.Pop();
                                    Service.Pop();
                                }
                                break;
                            }
                        });
                    }
                    else
                    {
                        UiFactory.Label(cell, "no travel point", 19, font, new Color(0.45f, 0.45f, 0.55f), new Vector2(0f, -82f)).sizeDelta = new Vector2(cellW - 40f, 40f);
                    }
                }
                index++;
            }
        }

        protected static RectTransform Dim(RectTransform root) => ScreensJournal.Dim(root);
    }

    /// <summary>Download manager: pack list, sizes, progress bars, retry/cancel/delete, cache stats.</summary>
    public sealed class DownloadScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1600f, 960f);

            UiFactory.Label(panel, "CONTENT DOWNLOADS", 48, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 410f)).sizeDelta = new Vector2(900f, 80f);
            UiFactory.TextButton(panel, "BACK", 32, font, new Vector2(640f, 410f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            var content = Services.Get<ContentService>();
            if (content == null) return;
            var cacheMb = content.CacheSizeBytes() / (1024f * 1024f);
            UiFactory.Label(panel, $"Cache: {cacheMb:F1} MB   (remote CDN: {(string.IsNullOrEmpty(content.RemoteContentBaseUrl) ? "not configured - all content local" : content.RemoteContentBaseUrl)})", 24, font, new Color(0.7f, 0.75f, 0.85f), new Vector2(0f, 340f)).sizeDelta = new Vector2(1400f, 50f);

            RectTransform list;
            var scroll = UiFactory.ScrollList(panel, "packs", out list, 700f);
            scroll.anchoredPosition = new Vector2(0f, -40f);

            int y = 0;
            foreach (var pack in content.Packs)
            {
                var state = content.GetState(pack.id);
                var row = UiFactory.Panel(list, pack.id, new Color(0.13f, 0.13f, 0.2f, 0.95f));
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -y - 60f);
                row.sizeDelta = new Vector2(1420f, 110f);
                UiFactory.Label(row, pack.name, 28, font, Color.white, new Vector2(-560f, 26f)).sizeDelta = new Vector2(700f, 50f);
                UiFactory.Label(row, $"{(pack.sizeBytes / (1024f * 1024f)).ToString("F1")} MB  -  {StateText(state)}", 22, font, new Color(0.7f, 0.72f, 0.8f), new Vector2(-30f, -18f)).sizeDelta = new Vector2(900f, 40f);

                switch (state)
                {
                    case PackState.Downloaded:
                        UiFactory.TextButton(row, "REMOVE", 24, font, new Vector2(580f, 0f), new Vector2(220f, 70f), () => { content.DeletePack(pack.id); Rebuild(); }, new Color(0.6f, 0.3f, 0.35f, 0.9f));
                        break;
                    case PackState.NotDownloaded:
                    case PackState.UpdateRequired:
                    case PackState.Error:
                        UiFactory.TextButton(row, "DOWNLOAD", 24, font, new Vector2(580f, 0f), new Vector2(240f, 70f), () => { content.StartDownload(pack.id); Rebuild(); });
                        break;
                    case PackState.Downloading:
                        UiFactory.TextButton(row, "CANCEL", 24, font, new Vector2(580f, 0f), new Vector2(220f, 70f), () => { content.CancelDownload(pack.id); Rebuild(); }, new Color(0.5f, 0.5f, 0.6f, 0.9f));
                        break;
                }
                y += 125;
            }
            list.sizeDelta = new Vector2(0f, y + 40f);

            GameEvents.Subscribe<ContentDownloadFinishedEvent>(OnDownloadFinished);
        }

        static string StateText(PackState state) => state switch
        {
            PackState.Downloaded => "installed",
            PackState.Downloading => "downloading...",
            PackState.UpdateRequired => "update available",
            PackState.Error => "download failed",
            _ => "not installed"
        };

        void OnDownloadFinished(ContentDownloadFinishedEvent evt)
        {
            if (evt.Success) Rebuild();
        }

        void Rebuild()
        {
            Service.Pop();
            Service.Push(new DownloadScreen());
        }

        public override void OnHidden()
        {
            GameEvents.Unsubscribe<ContentDownloadFinishedEvent>(OnDownloadFinished);
        }
    }
}
