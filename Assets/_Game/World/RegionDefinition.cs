using System;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    public enum BiomeKind
    {
        Tropical, Coastal, FantasyIslands, CyberCity, Resort, Mountain, NightForest, Cave,
        ModernCity, Ocean, Snow, Desert, Countryside, LuxuryIsland, Underwater, Tech,
        Meadows, Ruins, Highway, Lake, Volcano, Sky, Harbor, Cloud
    }

    public enum PoiKind
    {
        Home, Shop, Restaurant, Viewpoint, Picnic, Fishing, PhotoSpot, Beach, Cave,
        Landmark, Camp, Stargaze, Treasure, Marina, Dock, Lighthouse, Boardwalk,
        Ruins, Plaza, Tower, Metro, Spring, Race, Activity, NpcZone, WildlifeZone
    }

    [Serializable]
    public class PoiDefinition
    {
        public string id;
        public string name;
        public PoiKind kind;
        public float x, z;          // region-local
        public float radius = 8f;
    }

    [Serializable]
    public class FastTravelPoint
    {
        public string id;
        public string name;
        public float x, z;
    }

    [Serializable]
    public class VehicleSpawnDef
    {
        public string kind = "Car";   // Car, Motorcycle, Boat, Hover
        public float x, z;
    }

    /// <summary>
    /// Data-driven region definition. Authored as ScriptableObject in production (Editor tools generate
    /// them from the JSON catalog); blockouts ship as JSON in Resources and hydrate this same model.
    /// Nothing in gameplay reads region-specific behavior - everything flows through this data.
    /// </summary>
    [CreateAssetMenu(menuName = "Love Game/Region Definition", fileName = "Region_New")]
    public sealed class RegionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "region";
        public string displayName = "New Region";
        public string description = "";
        public BiomeKind biome = BiomeKind.Tropical;
        public int seed = 1;

        [Header("World placement (world grid)")]
        public int worldX = 0;
        public int worldZ = 0;
        public float size = 700f;

        [Header("Terrain")]
        public float waterLevel = 2f;
        public float amplitude = 12f;
        public float frequency = 1f;
        public float mountainHeight = 34f;
        public float mountainRadius = 200f;
        public float flattenCenter = 50f;

        [Header("Palette")]
        public string terrainLow = "#E8D8A8";
        public string terrainMid = "#7AC74F";
        public string terrainHigh = "#8A8A8A";
        public string roadColor = "#C9B89A";
        public string waterColor = "#2EC4B6";

        [Header("Sky & light")]
        public string skyTop = "#4FC3F7";
        public string skyHorizon = "#BDE8F5";
        public string skyBottom = "#E8D8A8";
        public string sunColor = "#FFF3D6";
        public float sunIntensity = 1.1f;
        public string ambientColor = "#9FB8C8";
        public string fogColor = "#CFE8F0";
        public float fogDensity = 0.0016f;

        [Header("Weather & audio")]
        public string weatherWeights = "60,25,10,2,3";   // clear, cloudy, rain, storm, fog
        public string ambience = "ocean";                // ocean, wind, forest, city, cave, desert, underwater
        public float propDensity = 1f;

        [Header("Content")]
        public List<PoiDefinition> pois = new List<PoiDefinition>();
        public List<FastTravelPoint> fastTravel = new List<FastTravelPoint>();
        public List<VehicleSpawnDef> vehicleSpawns = new List<VehicleSpawnDef>();
        public Vector2 spawnPoint = Vector2.zero;

        public Vector3 WorldCenter => new Vector3(worldX * (size + 50f), 0f, worldZ * (size + 50f));
    }

    /// <summary>Immutable runtime view over a RegionDefinition plus its world-space placement.</summary>
    public sealed class RegionData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public BiomeKind Biome;
        public int Seed;
        public Vector3 WorldCenter;
        public float Size;
        public float WaterLevel;
        public float Amplitude;
        public float Frequency;
        public float MountainHeight;
        public float MountainRadius;
        public float FlattenCenter;
        public Color TerrainLow, TerrainMid, TerrainHigh, RoadColor, WaterColor;
        public Color SkyTop, SkyHorizon, SkyBottom, SunColor, AmbientColor, FogColor;
        public float SunIntensity, FogDensity, PropDensity;
        public readonly float[] WeatherWeights = new float[5];
        public string Ambience;
        public readonly List<PoiDefinition> Pois = new List<PoiDefinition>();
        public readonly List<FastTravelPoint> FastTravel = new List<FastTravelPoint>();
        public readonly List<VehicleSpawnDef> VehicleSpawns = new List<VehicleSpawnDef>();
        public Vector2 SpawnPoint;

        public string ContentPackId => $"region_{Id}";

        public static RegionData From(RegionDefinition def)
        {
            var d = new RegionData
            {
                Id = def.id,
                DisplayName = def.displayName,
                Description = def.description,
                Biome = def.biome,
                Seed = def.seed,
                WorldCenter = def.WorldCenter,
                Size = def.size,
                WaterLevel = def.waterLevel,
                Amplitude = def.amplitude,
                Frequency = def.frequency,
                MountainHeight = def.mountainHeight,
                MountainRadius = def.mountainRadius,
                FlattenCenter = def.flattenCenter,
                TerrainLow = CoreMath.HexToColor(def.terrainLow),
                TerrainMid = CoreMath.HexToColor(def.terrainMid),
                TerrainHigh = CoreMath.HexToColor(def.terrainHigh),
                RoadColor = CoreMath.HexToColor(def.roadColor),
                WaterColor = CoreMath.HexToColor(def.waterColor),
                SkyTop = CoreMath.HexToColor(def.skyTop),
                SkyHorizon = CoreMath.HexToColor(def.skyHorizon),
                SkyBottom = CoreMath.HexToColor(def.skyBottom),
                SunColor = CoreMath.HexToColor(def.sunColor),
                AmbientColor = CoreMath.HexToColor(def.ambientColor),
                FogColor = CoreMath.HexToColor(def.fogColor),
                SunIntensity = def.sunIntensity,
                FogDensity = def.fogDensity,
                PropDensity = def.propDensity,
                Ambience = def.ambience,
                SpawnPoint = def.spawnPoint,
            };
            var w = def.weatherWeights.Split(',');
            for (int i = 0; i < 5; i++) d.WeatherWeights[i] = i < w.Length && float.TryParse(w[i].Trim(), out var v) ? v : 0f;
            d.Pois.AddRange(def.pois);
            d.FastTravel.AddRange(def.fastTravel);
            d.VehicleSpawns.AddRange(def.vehicleSpawns);
            return d;
        }
    }
}
