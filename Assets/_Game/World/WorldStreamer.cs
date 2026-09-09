using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>A live, built region: root object, cached heightfield, estimated memory footprint.</summary>
    public sealed class RegionInstance
    {
        public RegionData Data;
        public GameObject Root;
        public float[,] HeightField;     // sampled terrain grid for cheap queries
        public float CellSize;
        public float EstimatedMb;
        public bool IsPrimary;
        public Transform PropRoot;
        public Transform PoiRoot;
        public GameObject Water;
    }

    /// <summary>
    /// Distance/priority-based world streaming. Only regions near the player are instantiated;
    /// neighbors preload asynchronously, far regions unload with hysteresis. Memory-aware:
    /// reacts to low-memory warnings by dropping the farthest secondary region.
    /// Implements IWorldQuery for the rest of the game (height/water/region lookups).
    /// </summary>
    public sealed class WorldStreamer : IGameService, IWorldQuery
    {
        public string ServiceName => "WorldStreamer";

        const float LoadRadiusFactor = 0.95f;     // of region size
        const float UnloadRadiusFactor = 1.35f;   // hysteresis so borders do not thrash
        const int TerrainGrid = 97;               // 97x97 samples per region heightfield

        readonly Dictionary<string, RegionInstance> _active = new Dictionary<string, RegionInstance>();
        readonly List<string> _loadQueue = new List<string>();
        Coroutine _buildRoutine;
        string _buildingRegionId;
        Transform _worldRoot;
        RegionCatalogService _catalog;
        RegionContentBuilder _builder;
        Vector3 _lastPlayerPos;
        float _queryTimer;
        public Vector3 PlayerPosition { get; set; }
        public Vector3 PlayerForward { get; set; } = Vector3.forward;

        public int ActiveRegionCount => _active.Count;
        public int QueuedCount => _loadQueue.Count;
        public float EstimatedMemoryMb { get; private set; }
        public string PrimaryRegionId { get; private set; }
        public IReadOnlyDictionary<string, RegionInstance> ActiveRegions => _active;
        public bool IsStreaming => _buildingRegionId != null || _loadQueue.Count > 0;

        public event System.Action<RegionInstance> RegionBuilt;
        public event System.Action<string> RegionUnloaded;

        public void Initialize()
        {
            _catalog = Services.Get<RegionCatalogService>();
            _builder = new RegionContentBuilder();
            _worldRoot = new GameObject("~WorldRoot").transform;
            UnityEngine.Object.DontDestroyOnLoad(_worldRoot.gameObject);
            GameEvents.Subscribe<LowMemoryWarningEvent>(OnLowMemory);
            Log.Info("Streamer", $"ready, {_catalog?.Count ?? 0} regions in catalog");
        }

        public void Tick(float delta)
        {
            var moved = (PlayerPosition - _lastPlayerPos).sqrMagnitude > 4f;
            _queryTimer += delta;
            if (_queryTimer < 0.25f && !moved) return;
            _queryTimer = 0f;
            _lastPlayerPos = PlayerPosition;
            UpdateDesiredRegions();
            PumpQueue();
        }

        void UpdateDesiredRegions()
        {
            if (_catalog == null) return;
            var loadRadius = 700f * LoadRadiusFactor; // max region size basis
            string primary = null;
            float primaryDist = float.MaxValue;

            foreach (var region in _catalog.Regions)
            {
                var d = PlayerPosition - region.WorldCenter; d.y = 0;
                var dist = d.magnitude;
                if (dist < primaryDist) { primaryDist = dist; primary = region.Id; }

                var wantLoad = dist < loadRadius && !_active.ContainsKey(region.Id);
                var wantUnload = dist > 700f * UnloadRadiusFactor && _active.ContainsKey(region.Id);

                if (wantLoad && !_loadQueue.Contains(region.Id))
                {
                    _loadQueue.Add(region.Id);
                    Log.Verbose("Streamer", $"queue load {region.Id}");
                }
                if (wantUnload)
                {
                    _loadQueue.Remove(region.Id);
                    // never unload the region being built or the primary
                    if (region.Id != _buildingRegionId && region.Id != PrimaryRegionId)
                        Unload(region.Id);
                }
            }

            // nearest-first priority
            _loadQueue.Sort((a, b) =>
            {
                var ra = _catalog.GetById(a); var rb = _catalog.GetById(b);
                if (ra == null || rb == null) return 0;
                return (PlayerPosition - ra.WorldCenter).sqrMagnitude.CompareTo((PlayerPosition - rb.WorldCenter).sqrMagnitude);
            });

            if (primary != null && primary != PrimaryRegionId)
            {
                PrimaryRegionId = primary;
                foreach (var inst in _active.Values) inst.IsPrimary = inst.Data.Id == primary;
                _catalog.SetActiveRegion(primary);
                ApplyEnvironment(_catalog.GetById(primary));
            }
        }

        void PumpQueue()
        {
            if (_buildingRegionId != null || _loadQueue.Count == 0) return;
            var regionId = _loadQueue[0];
            var data = _catalog.GetById(regionId);
            if (data == null || _active.ContainsKey(regionId)) { _loadQueue.RemoveAt(0); return; }

            _buildRoutine = Services.Host.Run(BuildRegionRoutine(data));
        }

        IEnumerator BuildRegionRoutine(RegionData data)
        {
            _buildingRegionId = data.Id;
            var inst = new RegionInstance { Data = data, EstimatedMb = EstimateRegionMb(data) };
            var rootName = $"Region_{data.Id}";
            var existing = _worldRoot.Find(rootName);
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);
            inst.Root = new GameObject(rootName);
            inst.Root.transform.SetParent(_worldRoot, false);
            inst.Root.transform.position = data.WorldCenter;

            yield return _builder.Build(inst, data, GameConfig.Quality);

            _active[data.Id] = inst;
            EstimatedMemoryMb += inst.EstimatedMb;
            _loadQueue.Remove(data.Id);
            _buildingRegionId = null;
            RegionBuilt?.Invoke(inst);
            Log.Info("Streamer", $"built '{data.DisplayName}' ({inst.PoiRoot?.childCount ?? 0} POIs, ~{inst.EstimatedMb:F0} MB est)");
        }

        static float EstimateRegionMb(RegionData d) => 6f + d.Pois.Count * 0.05f + d.PropDensity * 4f;

        public void Unload(string regionId)
        {
            if (!_active.TryGetValue(regionId, out var inst)) return;
            EstimatedMemoryMb -= inst.EstimatedMb;
            _active.Remove(regionId);
            if (inst.Root != null) UnityEngine.Object.Destroy(inst.Root);
            RegionUnloaded?.Invoke(regionId);
            Log.Verbose("Streamer", $"unloaded {regionId}");
        }

        public void UnloadAll()
        {
            if (_buildRoutine != null) { Services.Host.Stop(_buildRoutine); _buildRoutine = null; }
            _buildingRegionId = null;
            _loadQueue.Clear();
            foreach (var id in new List<string>(_active.Keys)) Unload(id);
        }

        void OnLowMemory(LowMemoryWarningEvent evt)
        {
            string farthest = null; float far = -1f;
            foreach (var inst in _active.Values)
            {
                if (inst.IsPrimary) continue;
                var d = (PlayerPosition - inst.Data.WorldCenter).magnitude;
                if (d > far) { far = d; farthest = inst.Data.Id; }
            }
            if (farthest != null)
            {
                Log.Warn("Streamer", $"low memory: dropping farthest region {farthest}");
                Unload(farthest);
            }
        }

        public void Shutdown()
        {
            GameEvents.Unsubscribe<LowMemoryWarningEvent>(OnLowMemory);
            UnloadAll();
            if (_worldRoot != null) UnityEngine.Object.Destroy(_worldRoot.gameObject);
        }

        /// <summary>Force-instant teleport: unload everything, ensure the target region, reposition on next tick.</summary>
        public void TeleportTo(string regionId, Vector3 localPos)
        {
            var data = _catalog.GetById(regionId);
            if (data == null) { Log.Error("Streamer", $"teleport target '{regionId}' unknown"); return; }
            UnloadAll();
            PlayerPosition = data.WorldCenter + new Vector3(localPos.x, 0f, localPos.z);
            _lastPlayerPos = PlayerPosition;
            PrimaryRegionId = null;
            _loadQueue.Insert(0, regionId);
            _queryTimer = 999f;
            Tick(0f);
            PumpQueue();
        }

        /// <summary>
        /// Teleport to an absolute world position (saved games store world coords - passing
        /// them through TeleportTo would double-offset by the region center).
        /// </summary>
        public void TeleportToWorld(string regionId, Vector3 worldPos)
        {
            var data = _catalog.GetById(regionId);
            if (data == null) { Log.Error("Streamer", $"teleport target '{regionId}' unknown"); return; }
            UnloadAll();
            PlayerPosition = new Vector3(worldPos.x, 0f, worldPos.z);
            _lastPlayerPos = PlayerPosition;
            PrimaryRegionId = null;
            _loadQueue.Insert(0, regionId);
            _queryTimer = 999f;
            Tick(0f);
            PumpQueue();
        }

        // ------------------------------------------------------ IWorldQuery

        public float SampleHeight(float worldX, float worldZ)
        {
            foreach (var inst in _active.Values)
            {
                var local = new Vector2(worldX - inst.Data.WorldCenter.x, worldZ - inst.Data.WorldCenter.z);
                if (Mathf.Abs(local.x) > inst.Data.Size * 0.5f || Mathf.Abs(local.y) > inst.Data.Size * 0.5f) continue;
                if (inst.HeightField == null) continue;
                var half = inst.Data.Size * 0.5f;
                var gx = Mathf.Clamp((local.x + half) / inst.CellSize, 0, TerrainGrid - 1);
                var gy = Mathf.Clamp((local.y + half) / inst.CellSize, 0, TerrainGrid - 1);
                var x0 = (int)gx; var z0 = (int)gy;
                var x1 = Mathf.Min(x0 + 1, TerrainGrid - 1); var z1 = Mathf.Min(z0 + 1, TerrainGrid - 1);
                var fx = gx - x0; var fz = gy - z0;
                var h = Mathf.Lerp(
                    Mathf.Lerp(inst.HeightField[x0, z0], inst.HeightField[x1, z0], fx),
                    Mathf.Lerp(inst.HeightField[x0, z1], inst.HeightField[x1, z1], fx), fz);
                return h;
            }
            return 0f;
        }

        public bool IsWaterAt(Vector3 worldPos)
        {
            var region = RegionFor(worldPos);
            if (region == null) return false;
            return SampleHeight(worldPos.x, worldPos.z) < region.WaterLevel - 0.05f;
        }

        public float WaterSurfaceAt(float worldX, float worldZ)
        {
            var region = RegionFor(new Vector3(worldX, 0f, worldZ));
            return region != null && SampleHeight(worldX, worldZ) < region.WaterLevel - 0.05f ? region.WaterLevel : float.NaN;
        }

        public string RegionIdAt(Vector3 worldPos)
        {
            var r = RegionFor(worldPos);
            return r?.Id;
        }

        RegionData RegionFor(Vector3 worldPos)
        {
            foreach (var inst in _active.Values)
            {
                var d = worldPos - inst.Data.WorldCenter; d.y = 0;
                if (Mathf.Abs(d.x) <= inst.Data.Size * 0.5f && Mathf.Abs(d.z) <= inst.Data.Size * 0.5f) return inst.Data;
            }
            return _catalog?.NearestTo(worldPos);
        }

        void ApplyEnvironment(RegionData region)
        {
            if (region == null) return;
            // Sky + fog applied live so transitions feel like travel, not teleport cuts.
            var render = new EnvironmentRequest
            {
                SkyTop = region.SkyTop, SkyHorizon = region.SkyHorizon, SkyBottom = region.SkyBottom,
                SunColor = region.SunColor, SunIntensity = region.SunIntensity,
                Ambient = region.AmbientColor, FogColor = region.FogColor, FogDensity = region.FogDensity,
                Ambience = region.Ambience
            };
            EnvironmentBlender.Request(render);
        }
    }

    /// <summary>Environment hand-off struct consumed by the day/night + weather renderer.</summary>
    public struct EnvironmentRequest
    {
        public Color SkyTop, SkyHorizon, SkyBottom, SunColor, Ambient, FogColor;
        public float SunIntensity, FogDensity;
        public string Ambience;
    }
}
