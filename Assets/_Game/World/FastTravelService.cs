using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>
    /// Fast travel: discovery by proximity, region+point registry, validated travel with fade.
    /// Travel happens through the world streamer so state stays consistent.
    /// </summary>
    public sealed class FastTravelService : IGameService
    {
        public string ServiceName => "FastTravel";

        WorldStreamer _streamer;
        RegionCatalogService _catalog;
        Player.ThirdPersonController _player;
        readonly List<string> _discovered = new List<string>();
        float _checkTimer;
        bool _travelling;

        public event System.Action<float> TravelFade; // UI listens: 0..1 out, then 1..0 in

        public void Bind(Player.ThirdPersonController player) => _player = player;

        public void Initialize()
        {
            _streamer = Services.Get<WorldStreamer>();
            _catalog = Services.Get<RegionCatalogService>();
            _discovered.AddRange(SaveSystem.Current.world.discoveredFastTravel);
        }

        public void Tick(float delta)
        {
            if (_travelling) return;
            _checkTimer -= delta;
            if (_checkTimer > 0f) return;
            _checkTimer = 1.5f;

            // discovery by proximity
            var pos = _streamer.PlayerPosition;
            foreach (var region in _catalog.Regions)
            {
                foreach (var ft in region.FastTravel)
                {
                    var world = region.WorldCenter + new Vector3(ft.x, 0f, ft.z);
                    var d = world - pos; d.y = 0;
                    if (d.magnitude < 12f)
                    {
                        var id = PointId(region.Id, ft.id);
                        if (!_discovered.Contains(id))
                        {
                            _discovered.Add(id);
                            SaveSystem.Current.world.discoveredFastTravel.Add(id);
                            GameEvents.Publish(new NotificationEvent { Title = "Fast travel unlocked", Body = $"{ft.name} ({region.DisplayName})", Duration = 4f });
                            SaveSystem.Save();
                        }
                    }
                }
            }
        }

        public static string PointId(string regionId, string pointId) => $"{regionId}::{pointId}";

        public bool IsDiscovered(string regionId, string pointId) => _discovered.Contains(PointId(regionId, pointId));

        public IReadOnlyList<string> Discovered => _discovered;

        public bool Travel(string regionId, string pointId)
        {
            if (_travelling) return false;
            var region = _catalog.GetById(regionId);
            if (region == null) return false;
            FastTravelPoint point = null;
            foreach (var ft in region.FastTravel) if (ft.id == pointId) { point = ft; break; }
            if (point == null) return false;
            if (!IsDiscovered(regionId, pointId))
            {
                Log.Warn("FastTravel", $"'{pointId}' not discovered yet");
                return false;
            }
            Services.Host.Run(TravelRoutine(region, point));
            return true;
        }

        IEnumerator TravelRoutine(RegionData region, FastTravelPoint point)
        {
            _travelling = true;
            GameEvents.Publish(new FastTravelStartedEvent { RegionId = region.Id });
            SaveSystem.Save();

            // fade out
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                TravelFade?.Invoke(Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            var destination = new Vector3(point.x, 0f, point.z);
            _streamer.TeleportTo(region.Id, destination);
            // the streamer only tracks a logical position - the actual player body has
            // to come along, or WorldSystems.Update() drags the streamer right back
            _player?.Teleport(region.WorldCenter + destination);
            // let the streamer build the region
            while (_streamer.IsStreaming) yield return null;
            yield return new WaitForFixedUpdate();
            _player?.SnapToGround();
            yield return new WaitForSeconds(0.2f);

            // fade in
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.8f)
            {
                TravelFade?.Invoke(Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            TravelFade?.Invoke(0f);
            GameEvents.Publish(new FastTravelFinishedEvent { RegionId = region.Id });
            GameEvents.Publish(new NotificationEvent { Title = region.DisplayName, Body = "Safe travels.", Duration = 2.5f });
            _travelling = false;
        }

        public void Shutdown() { }
    }
}
