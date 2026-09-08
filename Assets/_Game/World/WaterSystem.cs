using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>
    /// Water volumes + swim state. Delegates surface/depth queries to the terrain heightfield,
    /// tracks underwater state for camera/audio, and exposes buoyancy anchors for boats.
    /// </summary>
    public sealed class WaterSystem : IGameService
    {
        public string ServiceName => "Water";

        WorldStreamer _streamer;
        public bool IsUnderwater { get; private set; }
        public event System.Action<bool> UnderwaterChanged;

        public void Initialize()
        {
            _streamer = Services.Get<WorldStreamer>();
        }

        public void Tick(float delta)
        {
            if (_streamer == null) return;
            var head = _streamer.PlayerPosition + Vector3.up * 1.5f;
            var surface = _streamer.WaterSurfaceAt(head.x, head.z);
            var underwater = !float.IsNaN(surface) && head.y < surface - 0.15f;
            if (underwater != IsUnderwater)
            {
                IsUnderwater = underwater;
                RenderSettings.fogDensity = underwater ? 0.05f : RenderSettings.fogDensity;
                UnderwaterChanged?.Invoke(underwater);
            }
        }

        /// <summary>Height of the water surface under a swimmer; NaN when dry land.</summary>
        public float SurfaceAt(Vector3 pos) => _streamer?.WaterSurfaceAt(pos.x, pos.z) ?? float.NaN;
        public bool IsWaterAt(Vector3 pos) => _streamer != null && _streamer.IsWaterAt(pos);
        /// <summary>Depth of water at a position (0 when dry).</summary>
        public float DepthAt(Vector3 pos)
        {
            var surface = SurfaceAt(pos);
            if (float.IsNaN(surface)) return 0f;
            return Mathf.Max(0f, surface - _streamer.SampleHeight(pos.x, pos.z));
        }

        public void Shutdown() { }
    }
}
