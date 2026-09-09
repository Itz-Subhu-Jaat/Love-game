using LoveGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>Development-only debug menu. Stripped from release builds by guard.</summary>
    public sealed class DebugScreen : Screen
    {
        Text _stats;
        float _timer;

        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.05f, 0.06f, 0.1f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1100f, 980f);

            UiFactory.Label(panel, "DEBUG (dev builds only)", 44, font, new Color(0.6f, 0.9f, 1f), new Vector2(0f, 430f)).sizeDelta = new Vector2(900f, 70f);
            UiFactory.TextButton(panel, "CLOSE", 30, font, new Vector2(380f, 430f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            _stats = UiFactory.Label(panel, "...", 24, font, new Color(0.8f, 0.85f, 0.95f), new Vector2(0f, 320f)).GetComponent<Text>();
            _stats.rectTransform.sizeDelta = new Vector2(1000f, 220f);

            int y = 130;
            // time control
            var dayNight = Services.Get<World.DayNightCycle>();
            if (dayNight != null)
            {
                foreach (var hour in new[] { 7f, 12f, 18f, 21f, 0f })
                {
                    var target = hour;
                    UiFactory.TextButton(panel, $"{target:00}:00", 24, font, new Vector2(-330f + (int)(target / 6f) * 0f, y), new Vector2(180f, 64f), () => dayNight.Hour = target);
                }
                y -= 80;
            }
            // weather control
            var weather = Services.Get<World.WeatherSystem>();
            if (weather != null)
            {
                foreach (var w in new[] { World.WeatherType.Clear, World.WeatherType.Cloudy, World.WeatherType.Rain, World.WeatherType.Storm, World.WeatherType.Fog })
                {
                    var wt = w;
                    UiFactory.TextButton(panel, w.ToString().ToUpper(), 22, font, new Vector2(-330f + (int)w * 165f, y), new Vector2(160f, 60f), () => weather.Force(wt));
                }
                y -= 90;
            }
            // teleport regions
            var catalog = Services.Get<World.RegionCatalogService>();
            if (catalog != null)
            {
                UiFactory.Label(panel, "TELEPORT (first 8 regions)", 26, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, y)).sizeDelta = new Vector2(800f, 40f);
                y -= 70;
                int col = 0;
                for (int i = 0; i < catalog.Count && i < 8; i++)
                {
                    var region = catalog.Regions[i];
                    var r = region;
                    UiFactory.TextButton(panel, region.DisplayName, 20, font, new Vector2(-360f + (col % 2) * 360f, y - (col / 2) * 70f), new Vector2(350f, 62f), () =>
                    {
                        var streamer = Services.Get<World.WorldStreamer>();
                        var pos = r.WorldCenter + new Vector3(r.SpawnPoint.x, 0f, r.SpawnPoint.y);
                        var player = Object.FindFirstObjectByType<Player.ThirdPersonController>();
                        player?.Teleport(new Vector3(pos.x, 150f, pos.z));
                        streamer?.TeleportTo(r.Id, new Vector3(r.SpawnPoint.x, 0f, r.SpawnPoint.y));
                        if (player != null && streamer != null)
                            Services.Host.Run(SnapRoutine(streamer, player));
                        Service.Pop();
                    });
                    col++;
                }
                y -= 300;
            }
            // vehicle + save
            UiFactory.TextButton(panel, "SPAWN CAR", 24, font, new Vector2(-360f, y), new Vector2(350f, 70f), () =>
            {
                var vehicles = Services.Get<Vehicles.VehicleService>();
                var streamer = Services.Get<World.WorldStreamer>();
                if (vehicles != null && streamer != null)
                    vehicles.Spawn(Vehicles.VehicleKind.Car, streamer.PlayerPosition + streamer.PlayerForward * 6f, streamer.RegionIdAt(streamer.PlayerPosition));
            });
            UiFactory.TextButton(panel, "SPAWN BOAT", 24, font, new Vector2(0f, y), new Vector2(350f, 70f), () =>
            {
                var vehicles = Services.Get<Vehicles.VehicleService>();
                var streamer = Services.Get<World.WorldStreamer>();
                if (vehicles != null && streamer != null)
                    vehicles.Spawn(Vehicles.VehicleKind.Boat, streamer.PlayerPosition + streamer.PlayerForward * 8f, streamer.RegionIdAt(streamer.PlayerPosition));
            });
            UiFactory.TextButton(panel, "SAVE NOW", 24, font, new Vector2(360f, y), new Vector2(350f, 70f), () => SaveSystem.Save());
        }

        public override void OnTick(float delta)
        {
            _timer -= delta;
            if (_timer > 0f || _stats == null) return;
            _timer = 0.5f;
            var perf = Services.Host.Performance;
            var streamer = Services.Get<World.WorldStreamer>();
            _stats.text = $"FPS {perf.CurrentFps:F0} (avg {perf.AverageFps:F0})\n" +
                          $"mem {perf.TotalAllocatedMb} MB / reserved {perf.ReservedMb} MB\n" +
                          $"regions active {(streamer != null ? streamer.ActiveRegionCount : 0)}, queued {(streamer != null ? streamer.QueuedCount : 0)}\n" +
                          $"pos {(streamer != null ? streamer.PlayerPosition.ToString("F1") : "-")}";
        }
    }

    /// <summary>Photo mode HUD: zoom, poses, capture, exit.</summary>
    public sealed class PhotoModeScreen : Screen
    {
        public override bool ShowOnHudStack => false;

        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            var bar = UiFactory.Panel(root, "bar", new Color(0.05f, 0.06f, 0.1f, 0.85f));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 40f);
            bar.sizeDelta = new Vector2(1400f, 160f);

            UiFactory.Label(bar, "PHOTO MODE", 40, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 42f)).sizeDelta = new Vector2(500f, 60f);
            UiFactory.TextButton(bar, "CAPTURE", 30, font, new Vector2(-320f, -20f), new Vector2(280f, 80f), () => WorldSystemsBridge.CapturePhoto?.Invoke());
            UiFactory.TextButton(bar, "ZOOM -", 28, font, new Vector2(40f, -20f), new Vector2(190f, 80f), () => WorldSystemsBridge.ZoomPhoto?.Invoke(-6f));
            UiFactory.TextButton(bar, "ZOOM +", 28, font, new Vector2(250f, -20f), new Vector2(190f, 80f), () => WorldSystemsBridge.ZoomPhoto?.Invoke(6f));
            UiFactory.TextButton(bar, "POSE", 28, font, new Vector2(470f, -20f), new Vector2(190f, 80f), () => WorldSystemsBridge.CyclePose?.Invoke());
            UiFactory.TextButton(bar, "EXIT", 28, font, new Vector2(680f, -20f), new Vector2(170f, 80f), () => WorldSystemsBridge.ExitPhotoMode?.Invoke(), new Color(0.5f, 0.35f, 0.45f, 0.9f));
        }
    }

    /// <summary>Couple quick-menu: interactions + emotes, one tap away from the HUD heart button.</summary>
    public sealed class CoupleMenuScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            UiFactory.Dim(root);
            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1500f, 900f);

            UiFactory.Label(panel, "TOGETHER", 52, font, new Color(1f, 0.55f, 0.68f), new Vector2(0f, 390f)).sizeDelta = new Vector2(700f, 80f);
            UiFactory.TextButton(panel, "CLOSE", 30, font, new Vector2(600f, 390f), new Vector2(180f, 70f), () => Service.Pop(), new Color(0.4f, 0.45f, 0.6f, 0.9f));

            var couple = Services.Get<Interaction.CoupleInteractionManager>();
            int y = 290;
            if (couple != null)
            {
                foreach (var def in couple.Definitions.Values)
                {
                    var d = def;
                    UiFactory.TextButton(panel, d.DisplayName.ToUpper(), 30, font, new Vector2(0f, y), new Vector2(560f, 86f), () =>
                    {
                        if (couple.Start(d.Id)) Service.Pop();
                        else GameEvents.Publish(new NotificationEvent { Title = "Not now", Body = "Too far apart or still recovering.", Duration = 2.5f });
                    });
                    y -= 100;
                    if (y < -420f) break;
                }
            }
            var emotes = Services.Get<Interaction.EmoteService>();
            if (emotes != null && y > -420f)
            {
                y -= 20;
                UiFactory.Label(panel, "EMOTES", 34, font, new Color(1f, 0.75f, 0.5f), new Vector2(0f, y)).sizeDelta = new Vector2(400f, 50f);
                y -= 80;
                int col = 0;
                foreach (var emote in emotes.Available)
                {
                    var e = emote;
                    UiFactory.TextButton(panel, e.ToUpper(), 24, font, new Vector2(-360f + col * 240f, y), new Vector2(230f, 70f), () =>
                    {
                        emotes.Play(e);
                        Service.Pop();
                    }, new Color(0.35f, 0.5f, 0.8f, 0.9f));
                    col++;
                    if (col >= 6) { col = 0; y -= 85; }
                }
            }
            // photo together
            UiFactory.TextButton(panel, "PHOTO TOGETHER", 30, font, new Vector2(0f, -420f), new Vector2(560f, 86f), () =>
            {
                Service.Pop();
                WorldSystemsBridge.EnterPhotoMode?.Invoke(null);
            }, new Color(0.55f, 0.4f, 0.75f, 0.95f));
        }

        static System.Collections.IEnumerator SnapRoutine(World.WorldStreamer streamer, Player.ThirdPersonController player)
        {
            while (streamer.IsStreaming) yield return null;
            yield return new WaitForFixedUpdate();
            player.SnapToGround();
        }
    }
}
