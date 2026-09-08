#if UNITY_EDITOR
using System.IO;
using LoveGame.Core;
using LoveGame.World;
using UnityEditor;
using UnityEngine;

namespace LoveGame.EditorTools
{
    /// <summary>
    /// Generates production ScriptableObject region assets from the JSON blockout catalog.
    /// This is the "blockout -> authored content" bridge: after running this, designers
    /// edit RegionDefinition assets in the inspector and regions load from Resources/Regions.
    /// Also exports standalone region scenes for hand-tuning.
    /// </summary>
    public static class RegionAssetGenerator
    {
        const string OutputFolder = "Assets/Resources/Regions";

        [MenuItem("Love Game/Regions/Generate Region Assets from JSON")]
        public static void GenerateRegionAssets()
        {
            Directory.CreateDirectory(OutputFolder);
            var text = Resources.Load<TextAsset>("Data/regions");
            if (text == null)
            {
                Debug.LogError("[RegionGen] Resources/Data/regions.json not found");
                return;
            }
            var catalog = new RegionCatalogService();
            catalog.Load();
            int created = 0;
            foreach (var region in catalog.Regions)
            {
                var path = $"{OutputFolder}/{region.Id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<RegionDefinition>(path);
                if (existing != null)
                {
                    Debug.Log($"[RegionGen] exists, skipped: {path}");
                    continue;
                }
                var def = ScriptableObject.CreateInstance<RegionDefinition>();
                def.id = region.Id;
                def.displayName = region.DisplayName;
                def.description = region.Description;
                def.biome = region.Biome;
                def.seed = region.Seed;
                def.worldX = Mathf.RoundToInt(region.WorldCenter.x / (region.Size + 50f));
                def.worldZ = Mathf.RoundToInt(region.WorldCenter.z / (region.Size + 50f));
                def.size = region.Size;
                def.waterLevel = region.WaterLevel;
                def.amplitude = region.Amplitude;
                def.frequency = region.Frequency;
                def.mountainHeight = region.MountainHeight;
                def.mountainRadius = region.MountainRadius;
                def.flattenCenter = region.FlattenCenter;
                def.terrainLow = "#" + ColorUtility.ToHtmlStringRGB(region.TerrainLow);
                def.terrainMid = "#" + ColorUtility.ToHtmlStringRGB(region.TerrainMid);
                def.terrainHigh = "#" + ColorUtility.ToHtmlStringRGB(region.TerrainHigh);
                def.roadColor = "#" + ColorUtility.ToHtmlStringRGB(region.RoadColor);
                def.waterColor = "#" + ColorUtility.ToHtmlStringRGB(region.WaterColor);
                def.skyTop = "#" + ColorUtility.ToHtmlStringRGB(region.SkyTop);
                def.skyHorizon = "#" + ColorUtility.ToHtmlStringRGB(region.SkyHorizon);
                def.skyBottom = "#" + ColorUtility.ToHtmlStringRGB(region.SkyBottom);
                def.sunColor = "#" + ColorUtility.ToHtmlStringRGB(region.SunColor);
                def.sunIntensity = region.SunIntensity;
                def.ambientColor = "#" + ColorUtility.ToHtmlStringRGB(region.AmbientColor);
                def.fogColor = "#" + ColorUtility.ToHtmlStringRGB(region.FogColor);
                def.fogDensity = region.FogDensity;
                def.weatherWeights = string.Join(",", region.WeatherWeights);
                def.ambience = region.Ambience;
                def.propDensity = region.PropDensity;
                def.spawnPoint = region.SpawnPoint;
                def.pois.AddRange(region.Pois);
                def.fastTravel.AddRange(region.FastTravel);
                def.vehicleSpawns.AddRange(region.VehicleSpawns);
                AssetDatabase.CreateAsset(def, path);
                created++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[RegionGen] created {created} region assets in {OutputFolder} (restart play to load them)");
        }
    }
}
#endif
