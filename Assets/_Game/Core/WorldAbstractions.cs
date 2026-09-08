using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>
    /// World queries the player/vehicles need (height, water, region) without depending on the World assembly.
    /// Implemented by the world streaming service; keeps gameplay decoupled from world internals.
    /// </summary>
    public interface IWorldQuery
    {
        /// <summary>Terrain height at world position. Returns 0 when no region is loaded there.</summary>
        float SampleHeight(float worldX, float worldZ);
        /// <summary>True when the position is inside a water volume (ocean, lake, river).</summary>
        bool IsWaterAt(Vector3 worldPos);
        /// <summary>Surface height of the water volume at that position (float.NaN if none).</summary>
        float WaterSurfaceAt(float worldX, float worldZ);
        /// <summary>Region identifier containing this world position, or null.</summary>
        string RegionIdAt(Vector3 worldPos);
    }
}
