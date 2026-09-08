using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>
    /// Deterministic region blockout builder: heightfield terrain with biome vertex colors,
    /// painted/leveled roads, water plane, biome props in cull-cells, POI beacons with
    /// interactables. All from RegionData - same seed always yields the same region.
    /// Runs as a coroutine so large regions never block a frame for long.
    /// </summary>
    public sealed class RegionContentBuilder
    {
        const int Grid = 97;                    // 96x96 cells per region side
        const float CellSizeFactor = 1f / 96f;
        const float RoadHalfWidth = 4.5f;

        public IEnumerator Build(RegionInstance inst, RegionData d, QualityLevel quality)
        {
            var rng = new Rng(d.Seed);
            var noise = new Noise(d.Seed);
            var half = d.Size * 0.5f;
            inst.CellSize = d.Size * CellSizeFactor;

            // ---- stage 1: road distance field (also flattens terrain along roads)
            var roads = BuildRoadNetwork(d, rng);
            yield return null;

            // ---- stage 2: terrain mesh + collider + heightfield cache
            inst.HeightField = new float[Grid, Grid];
            var vertices = new Vector3[Grid * Grid];
            var colors = new Color[Grid * Grid];
            var triangles = new int[(Grid - 1) * (Grid - 1) * 6];
            int tri = 0;

            for (int z = 0; z < Grid; z++)
            {
                for (int x = 0; x < Grid; x++)
                {
                    var local = new Vector2(-half + x * inst.CellSize, -half + z * inst.CellSize);
                    float h = SampleHeight(d, noise, local, half);
                    // road leveling
                    float roadDist = DistanceToRoads(roads, local);
                    if (roadDist < RoadHalfWidth * 2f)
                    {
                        float roadBlend = Mathf.Clamp01((roadDist - RoadHalfWidth) / RoadHalfWidth);
                        float target = SampleHeight(d, noise, new Vector2(Mathf.Round(local.x / 20f) * 20f, Mathf.Round(local.y / 20f) * 20f), half);
                        h = Mathf.Lerp(Mathf.Min(h, target + 0.2f), h, roadBlend);
                    }
                    inst.HeightField[x, z] = h;
                    vertices[z * Grid + x] = new Vector3(local.x, h, local.y);
                }
                if (z % 16 == 0) yield return null;
            }

            for (int z = 0; z < Grid - 1; z++)
            {
                for (int x = 0; x < Grid - 1; x++)
                {
                    int i0 = z * Grid + x, i1 = i0 + 1, i2 = i0 + Grid, i3 = i2 + 1;
                    triangles[tri++] = i0; triangles[tri++] = i2; triangles[tri++] = i1;
                    triangles[tri++] = i1; triangles[tri++] = i2; triangles[tri++] = i3;
                }
            }

            PaintVertexColors(d, vertices, colors, inst, roads, half);

            var terrainGo = new GameObject("Terrain");
            terrainGo.transform.SetParent(inst.Root.transform, false);
            var mesh = new Mesh { name = $"terrain_{d.Id}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var mf = terrainGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = terrainGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Terrain;
            var mc = terrainGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            inst.EstimatedMb += 1.2f;
            yield return null;

            // ---- stage 3: water plane
            if (HasWater(d))
            {
                inst.Water = new GameObject("Water");
                inst.Water.transform.SetParent(inst.Root.transform, false);
                inst.Water.transform.localPosition = new Vector3(0f, d.WaterLevel, 0f);
                var wmf = inst.Water.AddComponent<MeshFilter>();
                wmf.sharedMesh = BuildWaterMesh(d.Size);
                var wmr = inst.Water.AddComponent<MeshRenderer>();
                wmr.sharedMaterial = MaterialLibrary.WaterColored(d.WaterColor);
                wmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                inst.EstimatedMb += 0.2f;
            }
            yield return null;

            // ---- stage 4: props (cells enable distance culling)
            inst.PropRoot = new GameObject("Props").transform;
            inst.PropRoot.SetParent(inst.Root.transform, false);
            var palette = PropLibrary.Palette(d.Biome);
            int baseCount = quality switch { QualityLevel.Low => 60, QualityLevel.Medium => 110, _ => 160 };
            int propCount = Mathf.RoundToInt(baseCount * d.PropDensity);
            var cells = new Dictionary<Vector2Int, Transform>();

            for (int i = 0; i < propCount; i++)
            {
                var local = rng.InsideUnitCircle() * (half * 0.92f);
                float h = SampleHeightCached(inst, local);
                if (h < d.WaterLevel - 0.3f && d.Biome != BiomeKind.Underwater && d.Biome != BiomeKind.Ocean) continue; // do not plant underwater
                var kind = rng.Pick(palette);
                if (DistanceToRoads(roads, local) < RoadHalfWidth) continue;
                var prop = PropLibrary.Build(kind, rng, d.Biome);
                var cell = new Vector2Int(Mathf.FloorToInt(local.x / 60f), Mathf.FloorToInt(local.y / 60f));
                if (!cells.TryGetValue(cell, out var cellRoot))
                {
                    cellRoot = new GameObject($"cell_{cell.x}_{cell.y}").transform;
                    cellRoot.SetParent(inst.PropRoot, false);
                    cells[cell] = cellRoot;
                }
                prop.transform.SetParent(cellRoot, false);
                prop.transform.localPosition = new Vector3(local.x, h, local.y);
                prop.transform.localRotation = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f);
                prop.isStatic = true;
                if (i % 12 == 0) yield return null;
            }
            Log.Verbose("World", $"region {d.Id}: {propCount} props in {cells.Count} cells");
            yield return null;

            // ---- stage 5: POI beacons
            inst.PoiRoot = new GameObject("POIs").transform;
            inst.PoiRoot.SetParent(inst.Root.transform, false);
            foreach (var poi in d.Pois)
            {
                BuildPoiBeacon(inst, d, poi, rng);
                yield return null;
            }

            // ---- stage 6: batch static geometry for draw-call efficiency
            if (inst.PropRoot.childCount > 0)
            {
                StaticBatchingUtility.Combine(inst.PropRoot.gameObject);
                StaticBatchingUtility.Combine(inst.PoiRoot.gameObject);
            }
        }

        // ------------------------------------------------------- terrain math

        float SampleHeight(RegionData d, Noise noise, Vector2 local, float half)
        {
            float nx = local.x * 0.004f * d.Frequency;
            float nz = local.y * 0.004f * d.Frequency;
            float h = d.Amplitude * (noise.Fbm(nx, nz, 4) * 0.5f + 0.5f);

            // mountain ring
            float dist = local.magnitude;
            if (dist > 0.01f)
            {
                float mountainMask = Mathf.SmoothStep(d.MountainRadius * 0.55f, d.MountainRadius, dist);
                h += d.MountainHeight * mountainMask * (noise.Fbm(nx * 2.3f + 7.7f, nz * 2.3f - 3.1f, 3) * 0.5f + 0.5f);
            }

            // biome profile
            switch (d.Biome)
            {
                case BiomeKind.Tropical:
                case BiomeKind.Coastal:
                case BiomeKind.LuxuryIsland:
                case BiomeKind.Ocean:
                case BiomeKind.Resort:
                {
                    // island: fall below water toward the border -> natural beaches & open sea
                    float edge = Mathf.SmoothStep(half * 0.62f, half * 0.98f, dist);
                    h = Mathf.Lerp(h, d.WaterLevel - 9f, edge);
                    break;
                }
                case BiomeKind.Cave:
                {
                    float rim = Mathf.SmoothStep(half * 0.45f, half * 0.9f, dist);
                    h += 26f * rim;   // enclosed valley with high rim
                    h -= 4f * (1f - rim);
                    break;
                }
                case BiomeKind.ModernCity:
                case BiomeKind.CyberCity:
                case BiomeKind.Tech:
                case BiomeKind.Highway:
                case BiomeKind.Harbor:
                {
                    h = Mathf.Lerp(h, 2f, 0.85f);   // mostly flat, gentle undulation
                    if (d.Biome == BiomeKind.Harbor)
                    {
                        float edge = Mathf.SmoothStep(half * 0.7f, half * 0.98f, dist);
                        h = Mathf.Lerp(h, d.WaterLevel - 8f, edge);
                    }
                    break;
                }
                case BiomeKind.Underwater:
                    h = d.WaterLevel - Mathf.Max(3f, h * 0.6f) - 2f;
                    break;
                case BiomeKind.Desert:
                    h = 3f + h * 0.4f + 2.5f * Mathf.Sin(local.x * 0.02f) * Mathf.Cos(local.y * 0.015f);
                    break;
                case BiomeKind.Snow:
                case BiomeKind.Mountain:
                    h += d.MountainHeight * 0.45f * Mathf.SmoothStep(half * 0.3f, half * 0.85f, dist);
                    break;
                case BiomeKind.Sky:
                case BiomeKind.Cloud:
                case BiomeKind.FantasyIslands:
                {
                    // platforms: high flat plateaus separated by deep gaps
                    float plateau = noise.Fbm(nx * 0.7f, nz * 0.7f, 2) * 0.5f + 0.5f;
                    h = 24f * Mathf.SmoothStep(0.35f, 0.55f, plateau) - 10f * Mathf.SmoothStep(0.45f, 0.3f, plateau);
                    break;
                }
            }

            // flatten around the spawn/town center
            if (dist < d.FlattenCenter + 30f)
            {
                float flat = Mathf.SmoothStep(d.FlattenCenter, d.FlattenCenter + 30f, dist);
                h = Mathf.Lerp(Mathf.Max(h, d.WaterLevel + 1.5f), h, flat);
            }
            return h;
        }

        float SampleHeightCached(RegionInstance inst, Vector2 local)
        {
            var half = inst.Data.Size * 0.5f;
            var gx = Mathf.Clamp(Mathf.RoundToInt((local.x + half) / inst.CellSize), 0, Grid - 1);
            var gz = Mathf.Clamp(Mathf.RoundToInt((local.y + half) / inst.CellSize), 0, Grid - 1);
            return inst.HeightField[gx, gz];
        }

        void PaintVertexColors(RegionData d, Vector3[] vertices, Color[] colors, RegionInstance inst, List<RoadSegment> roads, float half)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                float h = v.y;
                Color c;
                if (h < d.WaterLevel - 0.6f)
                {
                    // sea/riverbed
                    c = Color.Lerp(d.TerrainMid * 0.55f + new Color(0.05f, 0.12f, 0.16f, 0f), d.TerrainLow, Mathf.Clamp01((h - (d.WaterLevel - 8f)) / 8f));
                }
                else if (h < d.WaterLevel + 1.6f)
                {
                    c = d.TerrainLow; // beach ring
                }
                else
                {
                    float slope = TerrainSlope(inst, i);
                    float alt = Mathf.Clamp01((h - d.Amplitude) / Mathf.Max(1f, d.MountainHeight));
                    c = Color.Lerp(d.TerrainMid, d.TerrainHigh, Mathf.Clamp01(slope * 1.6f + alt * 0.8f));
                    if (d.Biome == BiomeKind.Snow && h > d.WaterLevel + 9f) c = Color.Lerp(c, new Color(0.92f, 0.95f, 0.98f), 0.85f);
                    if (d.Biome == BiomeKind.Underwater) c = Color.Lerp(d.TerrainLow, new Color(0.35f, 0.6f, 0.7f), 0.5f);
                }
                // road paint
                float roadDist = DistanceToRoads(roads, new Vector2(v.x, v.z));
                if (roadDist < RoadHalfWidth) c = Color.Lerp(c, d.RoadColor, 0.9f);
                else if (roadDist < RoadHalfWidth * 1.8f) c = Color.Lerp(c, d.RoadColor, 0.35f);
                colors[i] = c;
            }
        }

        float TerrainSlope(RegionInstance inst, int vertexIndex)
        {
            int x = vertexIndex % Grid;
            int z = vertexIndex / Grid;
            float hL = inst.HeightField[Mathf.Max(0, x - 1), z];
            float hR = inst.HeightField[Mathf.Min(Grid - 1, x + 1), z];
            float hD = inst.HeightField[x, Mathf.Max(0, z - 1)];
            float hU = inst.HeightField[x, Mathf.Min(Grid - 1, z + 1)];
            float dh = Mathf.Max(Mathf.Abs(hR - hL), Mathf.Abs(hU - hD));
            return dh / (inst.CellSize * 2f);
        }

        static bool HasWater(RegionData d) => d.WaterLevel > -40f; // very low water level = dry biome

        static Mesh BuildWaterMesh(float size)
        {
            const int seg = 24;
            var verts = new Vector3[(seg + 1) * (seg + 1)];
            var tris = new int[seg * seg * 6];
            var uv = new Vector2[(seg + 1) * (seg + 1)];
            int t = 0;
            for (int z = 0; z <= seg; z++)
                for (int x = 0; x <= seg; x++)
                {
                    int i = z * (seg + 1) + x;
                    verts[i] = new Vector3(-0.5f + x / (float)seg, 0f, -0.5f + z / (float)seg);
                    uv[i] = new Vector2(x / (float)seg * 6f, z / (float)seg * 6f);
                }
            for (int z = 0; z < seg; z++)
                for (int x = 0; x < seg; x++)
                {
                    int i0 = z * (seg + 1) + x, i1 = i0 + 1, i2 = i0 + seg + 1, i3 = i2 + 1;
                    tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                    tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
                }
            var m = new Mesh { name = "LG_Water" };
            m.vertices = verts;
            m.uv = uv;
            m.triangles = tris;
            m.RecalculateNormals();
            return m;
        }

        // ------------------------------------------------------ road network

        readonly struct RoadSegment
        {
            public readonly Vector2 A, B;
            public RoadSegment(Vector2 a, Vector2 b) { A = a; B = b; }
        }

        List<RoadSegment> BuildRoadNetwork(RegionData d, Rng rng)
        {
            var segments = new List<RoadSegment>();
            var hub = new Vector2(d.SpawnPoint.x, d.SpawnPoint.y);
            var majorKinds = new HashSet<PoiKind> { PoiKind.Shop, PoiKind.Restaurant, PoiKind.Marina, PoiKind.Plaza, PoiKind.Home, PoiKind.Metro, PoiKind.Tower, PoiKind.Dock };
            var anchors = new List<Vector2> { hub };
            foreach (var poi in d.Pois)
            {
                var p = new Vector2(poi.x, poi.z);
                if (majorKinds.Contains(poi.kind)) segments.Add(new RoadSegment(hub, p));
                anchors.Add(p);
            }
            // one scenic ring road
            float ringR = d.Size * 0.32f;
            int ringPoints = 10;
            for (int i = 0; i < ringPoints; i++)
            {
                float a0 = i / (float)ringPoints * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)ringPoints * Mathf.PI * 2f;
                segments.Add(new RoadSegment(
                    hub + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * ringR,
                    hub + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * ringR));
            }
            // connect each POI to its nearest anchor (loops, not just stars)
            foreach (var poi in d.Pois)
            {
                var p = new Vector2(poi.x, poi.z);
                Vector2 best = hub; float bestD = float.MaxValue;
                foreach (var a in anchors)
                {
                    if ((a - p).sqrMagnitude < 1f) continue;
                    var dd = (a - p).sqrMagnitude;
                    if (dd < bestD) { bestD = dd; best = a; }
                }
                if (bestD < d.Size * 0.4f) segments.Add(new RoadSegment(p, best));
            }
            return segments;
        }

        float DistanceToRoads(List<RoadSegment> segments, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                var ab = s.B - s.A;
                float t = Mathf.Clamp01(Vector2.Dot(p - s.A, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                var closest = s.A + ab * t;
                float d = (p - closest).sqrMagnitude;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }

        // ------------------------------------------------------- POI beacons

        void BuildPoiBeacon(RegionInstance inst, RegionData d, PoiDefinition poi, Rng rng)
        {
            var go = new GameObject($"poi_{poi.id}");
            go.transform.SetParent(inst.PoiRoot, false);
            var local = new Vector3(poi.x, 0f, poi.z);
            float ground = SampleHeightCached(inst, new Vector2(poi.x, poi.z));
            local.y = Mathf.Max(ground, d.WaterLevel - 0.4f);
            go.transform.localPosition = local;

            switch (poi.kind)
            {
                case PoiKind.Picnic:
                {
                    var table = PropLibrary.Build(PropLibrary.PropKind.PicnicTable, rng, d.Biome);
                    Attach(go.transform, table);
                    var blanket = PropLibrary.Build(PropLibrary.PropKind.Towel, rng, d.Biome);
                    Attach(go.transform, blanket, new Vector3(2.5f, 0.02f, 1f));
                    AddInteractable(go, poi, "Start picnic", 4f, d);
                    break;
                }
                case PoiKind.Camp:
                {
                    var fire = PropLibrary.Build(PropLibrary.PropKind.Campfire, rng, d.Biome);
                    Attach(go.transform, fire);
                    var tent = PropLibrary.Build(PropLibrary.PropKind.Tent, rng, d.Biome);
                    Attach(go.transform, tent, new Vector3(4f, 0f, -1.5f), 40f);
                    AddInteractable(go, poi, "Make camp", 4f, d);
                    break;
                }
                case PoiKind.Fishing:
                {
                    var dock = PropLibrary.Build(PropLibrary.PropKind.DockPlank, rng, d.Biome);
                    Attach(go.transform, dock, Vector3.zero, 90f);
                    AddInteractable(go, poi, "Go fishing", 4f, d);
                    break;
                }
                case PoiKind.Stargaze:
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var lantern = PropLibrary.Build(PropLibrary.PropKind.Lantern, rng, d.Biome);
                        float a = i / 6f * Mathf.PI * 2f;
                        Attach(go.transform, lantern, new Vector3(Mathf.Cos(a) * 4f, 0f, Mathf.Sin(a) * 4f));
                    }
                    var towel = PropLibrary.Build(PropLibrary.PropKind.Towel, rng, d.Biome);
                    Attach(go.transform, towel);
                    AddInteractable(go, poi, "Stargaze together", 4f, d);
                    break;
                }
                case PoiKind.PhotoSpot:
                {
                    var arch = PropLibrary.Build(PropLibrary.PropKind.Arch, rng, d.Biome);
                    Attach(go.transform, arch, new Vector3(0f, 0f, -1.5f));
                    var bench = PropLibrary.Build(PropLibrary.PropKind.Bench, rng, d.Biome);
                    Attach(go.transform, bench, new Vector3(0f, 0f, 2.5f), 180f);
                    AddInteractable(go, poi, "Photo spot", 4f, d);
                    break;
                }
                case PoiKind.Beach:
                {
                    var umbrella = PropLibrary.Build(PropLibrary.PropKind.Umbrella, rng, d.Biome);
                    Attach(go.transform, umbrella);
                    var towel = PropLibrary.Build(PropLibrary.PropKind.Towel, rng, d.Biome);
                    Attach(go.transform, towel, new Vector3(1.6f, 0.02f, 0.8f), rng.Range(0f, 360f));
                    break;
                }
                case PoiKind.Viewpoint:
                {
                    var bench = PropLibrary.Build(PropLibrary.PropKind.Bench, rng, d.Biome);
                    Attach(go.transform, bench);
                    var lantern = PropLibrary.Build(PropLibrary.PropKind.Lantern, rng, d.Biome);
                    Attach(go.transform, lantern, new Vector3(3f, 0f, 0f));
                    AddInteractable(go, poi, "Watch the view together", 4f, d);
                    break;
                }
                case PoiKind.Shop:
                {
                    var house = PropLibrary.Build(PropLibrary.PropKind.House, rng, d.Biome);
                    Attach(go.transform, house);
                    var sign = PropLibrary.Build(PropLibrary.PropKind.Sign, rng, d.Biome);
                    Attach(go.transform, sign, new Vector3(3.5f, 0f, 2.5f), 30f);
                    AddInteractable(go, poi, $"Visit {poi.name}", 6f, d);
                    break;
                }
                case PoiKind.Restaurant:
                {
                    var house = PropLibrary.Build(PropLibrary.PropKind.House, rng, d.Biome);
                    Attach(go.transform, house);
                    for (int i = 0; i < 3; i++)
                    {
                        var table = PropLibrary.Build(PropLibrary.PropKind.PicnicTable, rng, d.Biome);
                        Attach(go.transform, table, new Vector3(5f + i * 2.5f, 0f, 3f), 90f);
                    }
                    AddInteractable(go, poi, $"Eat at {poi.name}", 6f, d);
                    break;
                }
                case PoiKind.Home:
                {
                    var house = PropLibrary.Build(PropLibrary.PropKind.House, rng, d.Biome);
                    Attach(go.transform, house);
                    AddInteractable(go, poi, "Enter home", 5f, d);
                    break;
                }
                case PoiKind.Lighthouse:
                {
                    var lh = PropLibrary.Build(PropLibrary.PropKind.Lighthouse, rng, d.Biome);
                    Attach(go.transform, lh);
                    break;
                }
                case PoiKind.Marina:
                case PoiKind.Dock:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var plank = PropLibrary.Build(PropLibrary.PropKind.DockPlank, rng, d.Biome);
                        Attach(go.transform, plank, new Vector3(i * 3f, 0f, 2f), 90f);
                    }
                    var boat = PropLibrary.Build(PropLibrary.PropKind.Boat, rng, d.Biome);
                    Attach(go.transform, boat, new Vector3(6f, d.WaterLevel * 0.5f, 6f), rng.Range(-30f, 30f));
                    AddInteractable(go, poi, "Boat dock", 5f, d);
                    break;
                }
                case PoiKind.Race:
                {
                    var arch = PropLibrary.Build(PropLibrary.PropKind.Arch, rng, d.Biome);
                    Attach(go.transform, arch);
                    AddInteractable(go, poi, "Start race", 5f, d);
                    break;
                }
                case PoiKind.Treasure:
                {
                    var crate = PropLibrary.Build(PropLibrary.PropKind.Crate, rng, d.Biome);
                    Attach(go.transform, crate, new Vector3(0f, 0.1f, 0f));
                    AddInteractable(go, poi, "Search for treasure", 3f, d);
                    break;
                }
                case PoiKind.Landmark:
                case PoiKind.Tower:
                {
                    if (d.Biome == BiomeKind.Mountain || d.Biome == BiomeKind.Snow)
                    {
                        var tower = PropLibrary.Build(PropLibrary.PropKind.Pylon, rng, d.Biome);
                        Attach(go.transform, tower);
                    }
                    else
                    {
                        var arch = PropLibrary.Build(PropLibrary.PropKind.RuinArch, rng, d.Biome);
                        Attach(go.transform, arch);
                    }
                    break;
                }
                case PoiKind.Cave:
                {
                    var arch = PropLibrary.Build(PropLibrary.PropKind.RuinArch, rng, d.Biome);
                    Attach(go.transform, arch);
                    for (int i = 0; i < 5; i++)
                    {
                        var rock = PropLibrary.Build(PropLibrary.PropKind.Rock, rng, d.Biome);
                        Attach(go.transform, rock, new Vector3(rng.Range(-5f, 5f), 0f, rng.Range(-4f, 4f)));
                    }
                    AddInteractable(go, poi, $"Enter {poi.name}", 4f, d);
                    break;
                }
                case PoiKind.Spring:
                {
                    var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    DestroyCollider(pool);
                    pool.name = "spring";
                    pool.transform.SetParent(go.transform, false);
                    pool.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    pool.transform.localScale = new Vector3(6f, 0.3f, 6f);
                    pool.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.WaterColored(new Color(0.5f, 0.9f, 0.85f, 0.9f));
                    var rocks = PropLibrary.Build(PropLibrary.PropKind.Rock, rng, d.Biome);
                    Attach(go.transform, rocks, new Vector3(3.6f, 0f, 3.6f));
                    AddInteractable(go, poi, "Relax at hot spring", 4f, d);
                    break;
                }
                case PoiKind.WildlifeZone:
                {
                    var sign = PropLibrary.Build(PropLibrary.PropKind.Sign, rng, d.Biome);
                    Attach(go.transform, sign);
                    go.name = $"wildlife_{poi.id}";
                    break;
                }
                case PoiKind.NpcZone:
                {
                    go.name = $"npczone_{poi.id}";
                    break;
                }
                case PoiKind.Metro:
                {
                    var arch = PropLibrary.Build(PropLibrary.PropKind.RuinArch, rng, d.Biome);
                    Attach(go.transform, arch);
                    var lamp = PropLibrary.Build(PropLibrary.PropKind.StreetLamp, rng, d.Biome);
                    Attach(go.transform, lamp, new Vector3(3f, 0f, 0f));
                    AddInteractable(go, poi, "Metro entrance", 4f, d);
                    break;
                }
                case PoiKind.Boardwalk:
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var plank = PropLibrary.Build(PropLibrary.PropKind.DockPlank, rng, d.Biome);
                        Attach(go.transform, plank, new Vector3(i * 2.2f - 4.4f, 0.4f, 0f), 0f);
                    }
                    var lamp = PropLibrary.Build(PropLibrary.PropKind.Lantern, rng, d.Biome);
                    Attach(go.transform, lamp, new Vector3(0f, 0.4f, 2f));
                    break;
                }
                case PoiKind.Ruins:
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var col = PropLibrary.Build(PropLibrary.PropKind.RuinColumn, rng, d.Biome);
                        float a = i / 4f * Mathf.PI * 2f;
                        Attach(go.transform, col, new Vector3(Mathf.Cos(a) * 5f, 0f, Mathf.Sin(a) * 5f));
                    }
                    AddInteractable(go, poi, "Explore ruins", 5f, d);
                    break;
                }
                case PoiKind.Plaza:
                {
                    var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    DestroyCollider(floor);
                    floor.name = "plaza";
                    floor.transform.SetParent(go.transform, false);
                    floor.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                    floor.transform.localScale = new Vector3(16f, 0.12f, 16f);
                    floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Tinted(MaterialLibrary.Lit, d.RoadColor, "plaza");
                    for (int i = 0; i < 4; i++)
                    {
                        var lamp = PropLibrary.Build(PropLibrary.PropKind.StreetLamp, rng, d.Biome);
                        float a = i / 4f * Mathf.PI * 2f + 0.78f;
                        Attach(go.transform, lamp, new Vector3(Mathf.Cos(a) * 7f, 0f, Mathf.Sin(a) * 7f));
                    }
                    AddInteractable(go, poi, $"{poi.name}", 6f, d);
                    break;
                }
                default:
                {
                    var sign = PropLibrary.Build(PropLibrary.PropKind.Sign, rng, d.Biome);
                    Attach(go.transform, sign);
                    break;
                }
            }
        }

        static void Attach(Transform parent, GameObject prop, Vector3 offset = default, float yaw = 0f)
        {
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = offset;
            prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            prop.isStatic = true;
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
        }

        static void AddInteractable(GameObject go, PoiDefinition poi, string prompt, float range, RegionData region)
        {
            var inter = go.AddComponent<Interactable>();
            inter.interactableId = $"poi_{poi.id}";
            inter.prompt = prompt;
            inter.range = range;
            inter.context = poi.kind.ToString();
            inter.InteractEvent += interactor =>
                GameEvents.Publish(new PoiInteractedEvent { PoiId = poi.id, Kind = poi.kind.ToString(), RegionId = region.Id });
            GameEvents.Publish(new PoiBeaconReadyEvent { PoiId = poi.id, Kind = poi.kind, RegionId = region.Id });
        }
    }

    /// <summary>Published when a POI beacon with an interaction becomes available in the world.</summary>
    public struct PoiBeaconReadyEvent
    {
        public string PoiId;
        public PoiKind Kind;
        public string RegionId;
    }
}
