using System;
using System.Collections.Generic;
using System.Text;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    // JSON DTOs (mirror of RegionDefinition serialized fields)
    [Serializable] class RegionDto
    {
        public string id, name, description, biome, terrainLow, terrainMid, terrainHigh, roadColor, waterColor;
        public string skyTop, skyHorizon, skyBottom, sunColor, ambientColor, fogColor, weatherWeights, ambience;
        public int seed, worldX, worldZ;
        public float size, waterLevel, amplitude, frequency, mountainHeight, mountainRadius, flattenCenter;
        public float sunIntensity, fogDensity, propDensity;
        public float spawnX, spawnZ;
        public PoiDto[] pois;
        public FastTravelDto[] fastTravel;
        public VehicleSpawnDto[] vehicleSpawns;
    }
    [Serializable] class PoiDto { public string id, name, kind; public float x, z, radius; }
    [Serializable] class FastTravelDto { public string id, name; public float x, z; }
    [Serializable] class VehicleSpawnDto { public string kind; public float x, z; }
    [Serializable] class RegionCatalogFile { public int schemaVersion; public RegionDto[] regions; }

    /// <summary>
    /// Loads the world: blockout regions from Resources/Data/regions.json, production ScriptableObject
    /// regions from Resources/Regions (editor-generated). Both merge into the same runtime model.
    /// This is the single source of truth for "what regions exist" - nothing is hardcoded downstream.
    /// </summary>
    public sealed class RegionCatalogService : IGameService
    {
        public string ServiceName => "RegionCatalog";

        public readonly List<RegionData> Regions = new List<RegionData>();
        Dictionary<string, RegionData> _byId;
        string _activeRegionId;

        public int Count => Regions.Count;
        public RegionData ActiveRegion => GetById(_activeRegionId);
        public event Action CatalogLoaded;

        public void Initialize() { Load(); }
        public void Tick(float delta) { }
        public void Shutdown() { }

        public void Load()
        {
            Regions.Clear();
            _byId = new Dictionary<string, RegionData>();

            // 1) production ScriptableObjects (when the editor has generated them)
            var sos = Resources.LoadAll<RegionDefinition>("Regions");
            foreach (var so in sos) Add(RegionData.From(so));

            // 2) JSON blockout catalog
            var text = Resources.Load<TextAsset>("Data/regions");
            if (text != null)
            {
                try
                {
                    var file = JsonUtility.FromJson<RegionCatalogFile>(text.text);
                    if (file?.regions != null)
                        foreach (var dto in file.regions)
                        {
                            var data = FromDto(dto);
                            if (data != null) Add(data);
                        }
                }
                catch (Exception e) { Log.Error("RegionCatalog", $"regions.json parse failed: {e.Message}"); }
            }

            if (Regions.Count == 0) Log.Error("RegionCatalog", "no regions loaded - world will be empty");
            Log.Info("RegionCatalog", $"{Regions.Count} regions loaded");
            CatalogLoaded?.Invoke();
        }

        void Add(RegionData data)
        {
            if (_byId.ContainsKey(data.Id))
            {
                Log.Warn("RegionCatalog", $"duplicate region id '{data.Id}' skipped");
                return;
            }
            _byId[data.Id] = data;
            Regions.Add(data);
        }

        static RegionData FromDto(RegionDto dto)
        {
            if (string.IsNullOrEmpty(dto?.id)) return null;
            var def = ScriptableObject.CreateInstance<RegionDefinition>();
            def.id = dto.id;
            def.displayName = dto.name;
            def.description = dto.description ?? "";
            def.biome = ParseEnum(dto.biome, BiomeKind.Tropical);
            def.seed = dto.seed;
            def.worldX = dto.worldX;
            def.worldZ = dto.worldZ;
            def.size = dto.size > 0f ? dto.size : 700f;
            def.waterLevel = dto.waterLevel;
            def.amplitude = dto.amplitude;
            def.frequency = dto.frequency;
            def.mountainHeight = dto.mountainHeight;
            def.mountainRadius = dto.mountainRadius;
            def.flattenCenter = dto.flattenCenter;
            def.terrainLow = Safe(dto.terrainLow, "#E8D8A8");
            def.terrainMid = Safe(dto.terrainMid, "#7AC74F");
            def.terrainHigh = Safe(dto.terrainHigh, "#8A8A8A");
            def.roadColor = Safe(dto.roadColor, "#C9B89A");
            def.waterColor = Safe(dto.waterColor, "#2EC4B6");
            def.skyTop = Safe(dto.skyTop, "#4FC3F7");
            def.skyHorizon = Safe(dto.skyHorizon, "#BDE8F5");
            def.skyBottom = Safe(dto.skyBottom, "#E8D8A8");
            def.sunColor = Safe(dto.sunColor, "#FFF3D6");
            def.sunIntensity = dto.sunIntensity;
            def.ambientColor = Safe(dto.ambientColor, "#9FB8C8");
            def.fogColor = Safe(dto.fogColor, "#CFE8F0");
            def.fogDensity = dto.fogDensity;
            def.weatherWeights = string.IsNullOrEmpty(dto.weatherWeights) ? "60,25,10,2,3" : dto.weatherWeights;
            def.ambience = string.IsNullOrEmpty(dto.ambience) ? "wind" : dto.ambience;
            def.propDensity = dto.propDensity;
            def.spawnPoint = new Vector2(dto.spawnX, dto.spawnZ);
            if (dto.pois != null)
                foreach (var p in dto.pois)
                    def.pois.Add(new PoiDefinition { id = p.id, name = p.name, kind = ParseEnum(p.kind, PoiKind.Landmark), x = p.x, z = p.z, radius = p.radius > 0 ? p.radius : 8f });
            if (dto.fastTravel != null)
                foreach (var f in dto.fastTravel)
                    def.fastTravel.Add(new FastTravelPoint { id = f.id, name = f.name, x = f.x, z = f.z });
            if (dto.vehicleSpawns != null)
                foreach (var v in dto.vehicleSpawns)
                    def.vehicleSpawns.Add(new VehicleSpawnDef { kind = v.kind, x = v.x, z = v.z });
            var data = RegionData.From(def);
            UnityEngine.Object.Destroy(def);
            return data;
        }

        static string Safe(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : (value.StartsWith("#") ? value : "#" + value);
        static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }

        public RegionData GetById(string id) => !string.IsNullOrEmpty(id) && _byId != null && _byId.TryGetValue(id, out var r) ? r : null;
        public RegionData NearestTo(Vector3 worldPos)
        {
            RegionData best = null; float bestSq = float.MaxValue;
            foreach (var r in Regions)
            {
                var d = worldPos - r.WorldCenter; d.y = 0;
                float sq = d.sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = r; }
            }
            return best;
        }
        public RegionData RegionAtGrid(int worldX, int worldZ)
        {
            foreach (var r in Regions) if (r.WorldCenter.x / (r.Size + 50f) == worldX && r.WorldCenter.z / (r.Size + 50f) == worldZ) return r;
            return null;
        }

        /// <summary>Runtime validation - also asserted by EditMode tests and CI.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var seenGrid = new HashSet<long>();
            foreach (var r in Regions)
            {
                if (string.IsNullOrEmpty(r.Id)) errors.Add("region with empty id");
                if (r.Size < 100f) errors.Add($"{r.Id}: size too small");
                if (r.Pois.Count == 0) errors.Add($"{r.Id}: no POIs (empty region)");
                long cell = ((long)(r.WorldCenter.x / 10f) << 32) | (uint)(r.WorldCenter.z / 10f);
                if (!seenGrid.Add(cell)) errors.Add($"{r.Id}: overlapping world placement");
                for (int i = 0; i < 5; i++)
                    if (r.WeatherWeights[i] < 0) errors.Add($"{r.Id}: negative weather weight");
            }
            return errors;
        }

        public void SetActiveRegion(string regionId)
        {
            if (_activeRegionId == regionId) return;
            _activeRegionId = regionId;
            var r = GetById(regionId);
            if (r == null) return;
            if (!SaveSystem.Current.world.discoveredRegions.Contains(regionId))
            {
                SaveSystem.Current.world.discoveredRegions.Add(regionId);
                GameEvents.Publish(new RegionDiscoveredEvent { RegionId = r.Id, RegionName = r.DisplayName });
            }
            GameEvents.Publish(new RegionChangedEvent { RegionId = r.Id, RegionName = r.DisplayName });
        }
    }
}
