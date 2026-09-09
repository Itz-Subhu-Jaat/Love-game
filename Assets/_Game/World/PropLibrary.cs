using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>
    /// Procedural placeholder props built from primitives with shared cached materials.
    /// Every prop is replaceable later by real assets - regions only place anchors,
    /// the visuals come from here until final art arrives.
    /// </summary>
    public static class PropLibrary
    {
        static Mesh _cube, _sphere, _cylinder, _capsule, _cone;

        static Mesh Cube => _cube ?? (_cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        static Mesh Sphere => _sphere ?? (_sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx"));
        static Mesh Cylinder => _cylinder ?? (_cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"));
        static Mesh Capsule => _capsule ?? (_capsule = Resources.GetBuiltinResource<Mesh>("Capsule.fbx"));

        // Cone is not a built-in primitive - synthesize once.
        static Mesh ConeMesh()
        {
            if (_cone != null) return _cone;
            var mesh = new Mesh { name = "LG_Cone" };
            const int seg = 10;
            var verts = new Vector3[seg * 2 + 2];
            var tris = new int[seg * 12];
            verts[0] = new Vector3(0, 0.5f, 0);
            verts[1] = new Vector3(0, -0.5f, 0);
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                verts[2 + i] = new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f);
                verts[2 + seg + i] = new Vector3(Mathf.Cos(a) * 0.5f, 0.499f, Mathf.Sin(a) * 0.5f);
            }
            int t = 0;
            for (int i = 0; i < seg; i++)
            {
                var n = (i + 1) % seg;
                tris[t++] = 0; tris[t++] = 2 + i; tris[t++] = 2 + n;
                tris[t++] = 1; tris[t++] = 2 + seg + n; tris[t++] = 2 + seg + i;
                tris[t++] = 2 + i; tris[t++] = 2 + seg + i; tris[t++] = 2 + seg + n;
                tris[t++] = 2 + i; tris[t++] = 2 + seg + n; tris[t++] = 2 + n;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _cone = mesh;
            return _cone;
        }

        static GameObject Part(Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 scale, Vector3 euler, string name = "p")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        static Material Lit(Color c) => MaterialLibrary.Tinted(MaterialLibrary.Lit, c, ColorUtility.ToHtmlStringRGBA(c));

        // ------------------------------------------------------------- props

        public enum PropKind
        {
            Palm, Pine, Broadleaf, SnowPine, Rock, Crystal, Tower, NeonTower, House, Cabin,
            StreetLamp, RuinColumn, RuinArch, PicnicTable, Campfire, Bench, Umbrella, Towel,
            DockPlank, Boat, Fence, Flower, Cactus, Windmill, Lighthouse, CloudPlatform,
            Coral, Lantern, Sign, Tent, Arch, Crate, Pylon, Shell
        }

        /// <summary>Builds one prop at the origin; caller positions/rotates the returned object.</summary>
        public static GameObject Build(PropKind kind, Rng rng, BiomeKind biome)
        {
            var root = new GameObject(kind.ToString());
            var t = root.transform;
            switch (kind)
            {
                case PropKind.Palm:
                {
                    var trunkMat = Lit(new Color(0.48f, 0.35f, 0.22f));
                    var frondMat = Lit(new Color(0.18f, 0.65f, 0.28f));
                    var frondTipMat = Lit(new Color(0.28f, 0.75f, 0.32f));
                    var coconutMat = Lit(new Color(0.38f, 0.24f, 0.12f));

                    float h = rng.Range(4.2f, 7.0f);
                    float leanAngle = rng.Range(6f, 14f);
                    float leanYaw = rng.Range(0f, 360f);
                    Quaternion leanRot = Quaternion.Euler(0f, leanYaw, leanAngle);

                    // Organic curved segmented trunk with natural taper
                    int segs = 6;
                    Vector3 curPos = Vector3.zero;
                    for (int i = 0; i < segs; i++)
                    {
                        float frac = (float)i / segs;
                        float segH = h / segs;
                        float width = Mathf.Lerp(0.38f, 0.20f, frac);
                        Vector3 offset = leanRot * new Vector3(0f, segH, Mathf.Sin(frac * Mathf.PI * 0.5f) * 0.12f);
                        Part(t, Cylinder, trunkMat, curPos + offset * 0.5f, new Vector3(width, segH * 0.55f, width), new Vector3(leanAngle * frac, leanYaw, 0f), "trunkSeg");
                        curPos += offset;
                    }

                    // Cluster of ripe coconuts at crown
                    for (int c = 0; c < 4; c++)
                    {
                        float ang = c * 90f + rng.Range(-15f, 15f);
                        Vector3 cPos = curPos + new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 0.25f, -0.15f, Mathf.Sin(ang * Mathf.Deg2Rad) * 0.25f);
                        Part(t, Sphere, coconutMat, cPos, new Vector3(0.26f, 0.30f, 0.26f), Vector3.zero, "coconut");
                    }

                    // Cascading arching palm fronds
                    int fronds = 8;
                    for (int f = 0; f < fronds; f++)
                    {
                        float fAngle = f * (360f / fronds) + rng.Range(-10f, 10f);
                        float archPitch = rng.Range(18f, 30f);
                        Quaternion fRot = Quaternion.Euler(archPitch, fAngle, 0f);

                        Vector3 midPt = curPos + fRot * new Vector3(0f, 0.25f, 1.2f);
                        Vector3 tipPt = curPos + fRot * new Vector3(0f, -0.15f, 2.3f);

                        Part(t, Cube, frondMat, midPt, new Vector3(0.65f, 0.05f, 1.6f), new Vector3(archPitch, fAngle, 0f), "frondBase");
                        Part(t, Cube, frondTipMat, tipPt, new Vector3(0.48f, 0.04f, 1.3f), new Vector3(archPitch + 22f, fAngle, 0f), "frondTip");
                    }

                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, h * 0.45f, 0); cap.height = h; cap.radius = 0.45f;
                    break;
                }
                case PropKind.Pine:
                {
                    var trunk = Lit(new Color(0.4f, 0.28f, 0.18f));
                    bool snow = biome == BiomeKind.Snow;
                    var foliage = Lit(snow ? new Color(0.82f, 0.87f, 0.92f) : new Color(0.13f, 0.42f, 0.2f));
                    float h = rng.Range(4f, 7.5f);
                    Part(t, Cylinder, trunk, new Vector3(0, h * 0.25f, 0), new Vector3(0.25f, h * 0.5f, 0.25f), Vector3.zero, "trunk");
                    for (int i = 0; i < 3; i++)
                        Part(t, ConeMesh(), foliage, new Vector3(0, h * (0.45f + i * 0.2f), 0), new Vector3(1.8f - i * 0.35f, h * 0.42f, 1.8f - i * 0.35f), Vector3.zero, "cone");
                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, h * 0.5f, 0); cap.height = h; cap.radius = 0.4f;
                    break;
                }
                case PropKind.Broadleaf:
                {
                    var trunkMat = Lit(new Color(0.42f, 0.30f, 0.18f));
                    var folMid = Lit(biome == BiomeKind.Meadows || biome == BiomeKind.FantasyIslands
                        ? new Color(0.35f, 0.76f, 0.35f) : new Color(0.22f, 0.58f, 0.26f));
                    var folShadow = Lit(new Color(0.14f, 0.42f, 0.18f));
                    var folSun = Lit(new Color(0.48f, 0.84f, 0.38f));

                    float h = rng.Range(4.5f, 7.5f);
                    // Tapered trunk + root spurs
                    Part(t, Cylinder, trunkMat, new Vector3(0, h * 0.35f, 0), new Vector3(0.45f, h * 0.7f, 0.45f), Vector3.zero, "trunk");
                    Part(t, Cylinder, trunkMat, new Vector3(0.3f, 0.25f, 0f), new Vector3(0.25f, 0.6f, 0.25f), new Vector3(0, 0, -25f), "root1");
                    Part(t, Cylinder, trunkMat, new Vector3(-0.25f, 0.25f, 0.2f), new Vector3(0.22f, 0.6f, 0.22f), new Vector3(20f, 0, 20f), "root2");

                    // Multi-clustered fluffy stylized canopy (Genshin / Ghibli style)
                    Part(t, Sphere, folShadow, new Vector3(0, h * 0.82f, 0), new Vector3(3.8f, 2.8f, 3.8f), Vector3.zero, "canopyBase");
                    Part(t, Sphere, folMid, new Vector3(0.6f, h + 0.3f, 0.4f), new Vector3(3.2f, 2.6f, 3.2f), Vector3.zero, "canopyMid1");
                    Part(t, Sphere, folMid, new Vector3(-0.7f, h + 0.1f, -0.5f), new Vector3(2.8f, 2.4f, 2.8f), Vector3.zero, "canopyMid2");
                    Part(t, Sphere, folSun, new Vector3(0.1f, h + 1.1f, 0.1f), new Vector3(2.6f, 2.2f, 2.6f), Vector3.zero, "canopyTop");

                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, h * 0.5f, 0); cap.height = h; cap.radius = 0.55f;
                    break;
                }
                case PropKind.SnowPine: goto case PropKind.Pine;

                case PropKind.Rock:
                {
                    var rock = Lit(biome == BiomeKind.Volcano ? new Color(0.16f, 0.13f, 0.12f) :
                        biome == BiomeKind.Underwater ? new Color(0.3f, 0.42f, 0.5f) : new Color(0.52f, 0.52f, 0.5f));
                    float s = rng.Range(0.8f, 2.6f);
                    Part(t, Cube, rock, Vector3.zero, new Vector3(s, s * rng.Range(0.55f, 0.9f), s * rng.Range(0.8f, 1.3f)), new Vector3(rng.Range(-15, 15), rng.Range(0, 180), rng.Range(-10, 10)), "rock");
                    if (s > 1.4f)
                    {
                        var box = root.AddComponent<BoxCollider>();
                        box.size = new Vector3(s, s * 0.7f, s);
                    }
                    break;
                }
                case PropKind.Crystal:
                {
                    var color = biome switch
                    {
                        BiomeKind.Cave => new Color(0.45f, 0.75f, 1f),
                        BiomeKind.Underwater => new Color(1f, 0.55f, 0.8f),
                        BiomeKind.Volcano => new Color(1f, 0.4f, 0.15f),
                        BiomeKind.FantasyIslands => new Color(0.7f, 0.5f, 1f),
                        _ => new Color(0.5f, 0.9f, 1f)
                    };
                    for (int i = 0; i < rng.Range(2, 5); i++)
                    {
                        float h = rng.Range(1.5f, 4.5f);
                        Part(t, Cube, MaterialLibrary.Emissive(color, rng.Range(1.2f, 2.2f)), new Vector3(rng.Range(-0.8f, 0.8f), h * 0.4f, rng.Range(-0.8f, 0.8f)),
                            new Vector3(0.45f, h, 0.45f), new Vector3(rng.Range(-14, 14), rng.Range(0, 90), rng.Range(-14, 14)), "crystal");
                    }
                    break;
                }
                case PropKind.Tower:
                {
                    var glass = Lit(new Color(0.35f, 0.5f, 0.62f));
                    var frame = Lit(new Color(0.25f, 0.28f, 0.32f));
                    float h = rng.Range(18f, 64f);
                    float w = rng.Range(5f, 11f);
                    Part(t, Cube, glass, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, w), Vector3.zero, "body");
                    Part(t, Cube, frame, new Vector3(0, h * 0.25f, 0), new Vector3(w + 0.3f, h * 0.5f, w + 0.3f), Vector3.zero, "podium");
                    Part(t, Cube, frame, new Vector3(0, h + 1.5f, 0), new Vector3(w * 0.4f, 3f, w * 0.4f), Vector3.zero, "roof");
                    Part(t, Cylinder, frame, new Vector3(0, h + 3.5f, 0), new Vector3(0.2f, 3f, 0.2f), Vector3.zero, "antenna");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(w + 0.4f, h, w + 0.4f); box.center = new Vector3(0, h * 0.5f, 0);
                    break;
                }
                case PropKind.NeonTower:
                {
                    var glass = Lit(new Color(0.1f, 0.12f, 0.18f));
                    float h = rng.Range(30f, 90f);
                    float w = rng.Range(6f, 12f);
                    var neon = MaterialLibrary.Emissive(biome == BiomeKind.CyberCity
                        ? new Color(rng.Range(0.6f, 1f), 0.15f, 0.85f)
                        : new Color(0.2f, 0.9f, 1f), 2.2f);
                    Part(t, Cube, glass, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, w), Vector3.zero, "body");
                    for (int i = 0; i < 6; i++)
                        Part(t, Cube, neon, new Vector3(0, rng.Range(4f, h - 2f), 0), new Vector3(w + 0.25f, rng.Range(0.4f, 1.6f), 0.3f), new Vector3(0, rng.Range(0, 90), 0), "neon");
                    Part(t, Cube, neon, new Vector3(0, h + 0.6f, 0), new Vector3(w * 0.7f, 0.5f, w * 0.7f), Vector3.zero, "crown");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(w + 0.4f, h, w + 0.4f); box.center = new Vector3(0, h * 0.5f, 0);
                    break;
                }
                case PropKind.House:
                case PropKind.Cabin:
                {
                    bool cabin = kind == PropKind.Cabin;
                    var wallMat = Lit(cabin ? new Color(0.52f, 0.38f, 0.26f) : new Color(0.96f, 0.94f, 0.90f));
                    var trimMat = Lit(new Color(0.38f, 0.26f, 0.18f));
                    var roofMat = Lit(cabin ? new Color(0.32f, 0.22f, 0.15f) : (biome == BiomeKind.Snow ? new Color(0.90f, 0.92f, 0.96f) : new Color(0.82f, 0.34f, 0.26f)));
                    var deckMat = Lit(new Color(0.58f, 0.44f, 0.30f));
                    var glassLit = MaterialLibrary.Emissive(new Color(1f, 0.88f, 0.60f), 1.8f);

                    float w = rng.Range(6.5f, 9.0f), d = rng.Range(5.5f, 7.5f), h = 3.6f;
                    // Porch / Stilt Deck base
                    Part(t, Cube, deckMat, new Vector3(0, 0.25f, 0), new Vector3(w + 1.2f, 0.5f, d + 1.8f), Vector3.zero, "deck");
                    // Main Villa Walls
                    Part(t, Cube, wallMat, new Vector3(0, 0.5f + h * 0.5f, -0.4f), new Vector3(w, h, d), Vector3.zero, "walls");
                    // Overhanging Roof with Eaves
                    Part(t, Cube, roofMat, new Vector3(0, 0.5f + h + 0.9f, -0.4f), new Vector3(w + 1.4f, 1.8f, d + 1.4f), new Vector3(0, 0, 32f), "roof");
                    // Front Porch Columns
                    Part(t, Cylinder, trimMat, new Vector3(-w * 0.45f, 0.5f + h * 0.5f, d * 0.5f + 0.2f), new Vector3(0.18f, h, 0.18f), Vector3.zero, "postL");
                    Part(t, Cylinder, trimMat, new Vector3(w * 0.45f, 0.5f + h * 0.5f, d * 0.5f + 0.2f), new Vector3(0.18f, h, 0.18f), Vector3.zero, "postR");
                    // Warm Glowing Framed Windows
                    Part(t, Cube, glassLit, new Vector3(-w * 0.26f, 0.5f + h * 0.55f, d * 0.5f - 0.38f), new Vector3(1.2f, 1.4f, 0.15f), Vector3.zero, "windowL");
                    Part(t, Cube, glassLit, new Vector3(w * 0.26f, 0.5f + h * 0.55f, d * 0.5f - 0.38f), new Vector3(1.2f, 1.4f, 0.15f), Vector3.zero, "windowR");
                    // Entrance Door
                    Part(t, Cube, trimMat, new Vector3(0, 0.5f + h * 0.45f, d * 0.5f - 0.38f), new Vector3(1.1f, 2.2f, 0.12f), Vector3.zero, "door");
                    // Entrance Lantern
                    Part(t, Sphere, MaterialLibrary.Emissive(new Color(1f, 0.82f, 0.45f), 2.5f), new Vector3(0.7f, 0.5f + h * 0.65f, d * 0.5f - 0.32f), new Vector3(0.28f, 0.36f, 0.28f), Vector3.zero, "lantern");

                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(w + 1.2f, h + 2.5f, d + 1.8f); box.center = new Vector3(0, (h + 2.5f) * 0.5f, 0);
                    break;
                }
                case PropKind.StreetLamp:
                {
                    var pole = Lit(new Color(0.2f, 0.22f, 0.25f));
                    Part(t, Cylinder, pole, new Vector3(0, 2.5f, 0), new Vector3(0.12f, 5f, 0.12f), Vector3.zero, "pole");
                    Part(t, Sphere, MaterialLibrary.Emissive(new Color(1f, 0.85f, 0.5f), 2.5f), new Vector3(0, 5.1f, 0), new Vector3(0.7f, 0.4f, 0.7f), Vector3.zero, "bulb");
                    break;
                }
                case PropKind.RuinColumn:
                {
                    var stone = Lit(new Color(0.72f, 0.7f, 0.62f));
                    float h = rng.Range(3f, 6f);
                    Part(t, Cylinder, stone, new Vector3(0, h * 0.5f, 0), new Vector3(0.8f, h, 0.8f), new Vector3(rng.Range(-4, 4), 0, rng.Range(-4, 4)), "column");
                    Part(t, Cube, stone, new Vector3(0, h, 0), new Vector3(1.2f, 0.3f, 1.2f), new Vector3(0, rng.Range(0, 30), 0), "capital");
                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, h * 0.5f, 0); cap.height = h; cap.radius = 0.45f;
                    break;
                }
                case PropKind.RuinArch:
                {
                    var stone = Lit(new Color(0.68f, 0.66f, 0.58f));
                    Part(t, Cube, stone, new Vector3(-1.8f, 2.5f, 0), new Vector3(1f, 5f, 1.2f), Vector3.zero, "left");
                    Part(t, Cube, stone, new Vector3(1.8f, 2.5f, 0), new Vector3(1f, 5f, 1.2f), Vector3.zero, "right");
                    Part(t, Cube, stone, new Vector3(0, 5.2f, 0), new Vector3(4.8f, 1f, 1.2f), Vector3.zero, "top");
                    break;
                }
                case PropKind.PicnicTable:
                {
                    var wood = Lit(new Color(0.62f, 0.45f, 0.3f));
                    Part(t, Cube, wood, new Vector3(0, 0.75f, 0), new Vector3(1.8f, 0.08f, 0.9f), Vector3.zero, "top");
                    Part(t, Cube, wood, new Vector3(0, 0.45f, -0.45f), new Vector3(1.8f, 0.06f, 0.3f), Vector3.zero, "seatA");
                    Part(t, Cube, wood, new Vector3(0, 0.45f, 0.45f), new Vector3(1.8f, 0.06f, 0.3f), Vector3.zero, "seatB");
                    Part(t, Cube, wood, new Vector3(-0.7f, 0.37f, 0), new Vector3(0.1f, 0.75f, 0.8f), Vector3.zero, "leg1");
                    Part(t, Cube, wood, new Vector3(0.7f, 0.37f, 0), new Vector3(0.1f, 0.75f, 0.8f), Vector3.zero, "leg2");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.9f, 0.8f, 1f); box.center = new Vector3(0, 0.4f, 0);
                    break;
                }
                case PropKind.Campfire:
                {
                    var stone = Lit(new Color(0.45f, 0.42f, 0.4f));
                    var log = Lit(new Color(0.4f, 0.28f, 0.16f));
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i / 7f * Mathf.PI * 2f;
                        Part(t, Cube, stone, new Vector3(Mathf.Cos(a) * 1f, 0.1f, Mathf.Sin(a) * 1f), new Vector3(0.35f, 0.22f, 0.35f), new Vector3(0, a * 57.3f, 0), "stone");
                    }
                    for (int i = 0; i < 4; i++)
                        Part(t, Cylinder, log, new Vector3(0, 0.25f, 0), new Vector3(0.18f, 1.3f, 0.18f), new Vector3(90, i * 90f, 12), "log");
                    Part(t, ConeMesh(), MaterialLibrary.Emissive(new Color(1f, 0.55f, 0.1f), 2.6f), new Vector3(0, 0.85f, 0), new Vector3(0.8f, 1.6f, 0.8f), Vector3.zero, "flame");
                    var light = root.AddComponent<Light>();
                    light.type = LightType.Point; light.color = new Color(1f, 0.6f, 0.3f); light.intensity = 2.2f; light.range = 14f;
                    break;
                }
                case PropKind.Bench:
                {
                    var wood = Lit(new Color(0.55f, 0.4f, 0.27f));
                    Part(t, Cube, wood, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.07f, 0.5f), Vector3.zero, "seat");
                    Part(t, Cube, wood, new Vector3(0, 0.75f, -0.22f), new Vector3(1.6f, 0.5f, 0.07f), new Vector3(-12, 0, 0), "back");
                    Part(t, Cube, wood, new Vector3(-0.65f, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.45f), Vector3.zero, "leg1");
                    Part(t, Cube, wood, new Vector3(0.65f, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.45f), Vector3.zero, "leg2");
                    break;
                }
                case PropKind.Umbrella:
                {
                    var cloth = Lit(biome == BiomeKind.CyberCity ? new Color(0.9f, 0.2f, 0.5f) : new Color(1f, 0.45f, 0.35f));
                    Part(t, Cylinder, Lit(new Color(0.85f, 0.83f, 0.8f)), new Vector3(0, 1.2f, 0), new Vector3(0.06f, 2.4f, 0.06f), Vector3.zero, "pole");
                    Part(t, ConeMesh(), cloth, new Vector3(0, 2.5f, 0), new Vector3(2.6f, 0.8f, 2.6f), Vector3.zero, "canopy");
                    break;
                }
                case PropKind.Towel:
                {
                    var cloth = Lit(biome == BiomeKind.LuxuryIsland ? new Color(1f, 0.9f, 0.75f) : new Color(rng.Range(0.4f, 1f), rng.Range(0.4f, 1f), rng.Range(0.4f, 1f)));
                    Part(t, Cube, cloth, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.04f, 2f), new Vector3(0, rng.Range(0, 360), 0), "cloth");
                    break;
                }
                case PropKind.DockPlank:
                {
                    var wood = Lit(new Color(0.5f, 0.38f, 0.26f));
                    Part(t, Cube, wood, new Vector3(0, 0.5f, 0), new Vector3(2f, 0.15f, 6f), Vector3.zero, "deck");
                    Part(t, Cylinder, wood, new Vector3(-0.8f, 0, 0), new Vector3(0.2f, 1f, 0.2f), Vector3.zero, "pile1");
                    Part(t, Cylinder, wood, new Vector3(0.8f, 0, 0), new Vector3(0.2f, 1f, 0.2f), Vector3.zero, "pile2");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(2f, 0.3f, 6f); box.center = new Vector3(0, 0.55f, 0);
                    break;
                }
                case PropKind.Boat:
                {
                    var hull = Lit(new Color(0.9f, 0.85f, 0.8f));
                    Part(t, Cube, hull, new Vector3(0, 0.3f, 0), new Vector3(2f, 0.5f, 5f), Vector3.zero, "hull");
                    Part(t, Cube, hull, new Vector3(0, 0.7f, -0.5f), new Vector3(1.4f, 0.4f, 2.2f), Vector3.zero, "cabin");
                    Part(t, Cylinder, Lit(new Color(0.3f, 0.3f, 0.32f)), new Vector3(0, 0.8f, 1.5f), new Vector3(0.12f, 1.6f, 0.12f), new Vector3(60, 0, 0), "mast");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(2.1f, 0.8f, 5.1f); box.center = new Vector3(0, 0.4f, 0);
                    break;
                }
                case PropKind.Fence:
                {
                    var wood = Lit(new Color(0.6f, 0.47f, 0.34f));
                    Part(t, Cube, wood, new Vector3(0, 0.55f, 0), new Vector3(3f, 0.08f, 0.08f), Vector3.zero, "rail1");
                    Part(t, Cube, wood, new Vector3(0, 0.3f, 0), new Vector3(3f, 0.08f, 0.08f), Vector3.zero, "rail2");
                    Part(t, Cube, wood, new Vector3(-1.4f, 0.5f, 0), new Vector3(0.1f, 1f, 0.1f), Vector3.zero, "post1");
                    Part(t, Cube, wood, new Vector3(1.4f, 0.5f, 0), new Vector3(0.1f, 1f, 0.1f), Vector3.zero, "post2");
                    break;
                }
                case PropKind.Flower:
                {
                    var petal = Lit(new Color(rng.Range(0.7f, 1f), rng.Range(0.3f, 0.8f), rng.Range(0.6f, 1f)));
                    Part(t, Cube, Lit(new Color(0.25f, 0.6f, 0.3f)), new Vector3(0, 0.2f, 0), new Vector3(0.06f, 0.4f, 0.06f), Vector3.zero, "stem");
                    Part(t, Sphere, petal, new Vector3(0, 0.45f, 0), new Vector3(0.25f, 0.18f, 0.25f), Vector3.zero, "petal");
                    break;
                }
                case PropKind.Cactus:
                {
                    var c = Lit(new Color(0.2f, 0.55f, 0.3f));
                    float h = rng.Range(1.5f, 3.5f);
                    Part(t, Capsule, c, new Vector3(0, h * 0.5f, 0), new Vector3(0.6f, h, 0.6f), Vector3.zero, "body");
                    Part(t, Capsule, c, new Vector3(0.5f, h * 0.6f, 0), new Vector3(0.35f, 1.2f, 0.35f), new Vector3(0, 0, -40), "arm");
                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, h * 0.5f, 0); cap.height = h; cap.radius = 0.35f;
                    break;
                }
                case PropKind.Windmill:
                {
                    var body = Lit(new Color(0.85f, 0.8f, 0.72f));
                    Part(t, Cylinder, body, new Vector3(0, 4f, 0), new Vector3(2.6f, 8f, 2.6f), Vector3.zero, "tower");
                    Part(t, ConeMesh(), Lit(new Color(0.6f, 0.3f, 0.25f)), new Vector3(0, 8.6f, 0), new Vector3(3.4f, 2f, 3.4f), Vector3.zero, "roof");
                    var blades = new GameObject("blades").transform;
                    blades.SetParent(t, false);
                    blades.localPosition = new Vector3(0, 7.6f, 1.45f);
                    var bladeMat = Lit(new Color(0.95f, 0.93f, 0.9f));
                    for (int i = 0; i < 4; i++)
                        Part(blades, Cube, bladeMat, new Vector3(0, 2.6f, 0), new Vector3(0.5f, 5.2f, 0.08f), new Vector3(0, 0, i * 90f), "blade");
                    var spin = root.AddComponent<PropSpinner>();
                    spin.target = blades; spin.speed = 20f;
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(2.8f, 8f, 2.8f); box.center = new Vector3(0, 4f, 0);
                    break;
                }
                case PropKind.Lighthouse:
                {
                    var white = Lit(new Color(0.95f, 0.94f, 0.9f));
                    var red = Lit(new Color(0.85f, 0.25f, 0.2f));
                    for (int i = 0; i < 5; i++)
                        Part(t, Cylinder, i % 2 == 0 ? white : red, new Vector3(0, 1.5f + i * 2.4f, 0), new Vector3(3.2f - i * 0.4f, 2.4f, 3.2f - i * 0.4f), Vector3.zero, "band");
                    Part(t, Cylinder, MaterialLibrary.Emissive(new Color(1f, 0.95f, 0.6f), 3f), new Vector3(0, 13.6f, 0), new Vector3(1.4f, 1.2f, 1.4f), Vector3.zero, "lamp");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(3.4f, 12f, 3.4f); box.center = new Vector3(0, 6f, 0);
                    break;
                }
                case PropKind.CloudPlatform:
                {
                    var cloud = Lit(new Color(0.97f, 0.97f, 1f));
                    Part(t, Sphere, cloud, Vector3.zero, new Vector3(7f, 1.6f, 7f), Vector3.zero, "puff");
                    Part(t, Sphere, cloud, new Vector3(2.5f, 0.4f, 1f), new Vector3(4f, 1.2f, 4f), Vector3.zero, "puff2");
                    Part(t, Sphere, cloud, new Vector3(-2.5f, 0.3f, -1f), new Vector3(3.5f, 1f, 3.5f), Vector3.zero, "puff3");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(6.5f, 0.8f, 6.5f); box.center = new Vector3(0, 0.3f, 0);
                    break;
                }
                case PropKind.Coral:
                {
                    var c = Lit(new Color(rng.Range(0.9f, 1f), rng.Range(0.3f, 0.6f), rng.Range(0.4f, 0.7f)));
                    for (int i = 0; i < rng.Range(3, 6); i++)
                        Part(t, Capsule, c, new Vector3(rng.Range(-0.6f, 0.6f), rng.Range(0.3f, 1.2f), rng.Range(-0.6f, 0.6f)), new Vector3(0.3f, rng.Range(0.6f, 1.8f), 0.3f), new Vector3(rng.Range(-25, 25), 0, rng.Range(-25, 25)), "branch");
                    break;
                }
                case PropKind.Lantern:
                {
                    Part(t, Cylinder, Lit(new Color(0.25f, 0.25f, 0.28f)), new Vector3(0, 0.8f, 0), new Vector3(0.08f, 1.6f, 0.08f), Vector3.zero, "post");
                    Part(t, Sphere, MaterialLibrary.Emissive(new Color(1f, 0.8f, 0.45f), 2.2f), new Vector3(0, 1.7f, 0), new Vector3(0.5f, 0.5f, 0.5f), Vector3.zero, "globe");
                    break;
                }
                case PropKind.Sign:
                {
                    var wood = Lit(new Color(0.55f, 0.42f, 0.28f));
                    Part(t, Cube, wood, new Vector3(0, 0.9f, 0), new Vector3(0.1f, 1.8f, 0.1f), Vector3.zero, "post");
                    Part(t, Cube, wood, new Vector3(0, 1.7f, 0), new Vector3(1.4f, 0.7f, 0.08f), Vector3.zero, "board");
                    break;
                }
                case PropKind.Tent:
                {
                    var cloth = Lit(new Color(0.35f, 0.55f, 0.4f));
                    Part(t, Cube, cloth, new Vector3(0, 1.1f, 0), new Vector3(3f, 2.6f, 3f), new Vector3(0, 45, 0), "prism");
                    Part(t, Cube, Lit(new Color(0.5f, 0.4f, 0.3f)), new Vector3(0, 0.03f, 0), new Vector3(3.4f, 0.06f, 3.4f), Vector3.zero, "groundsheet");
                    break;
                }
                case PropKind.Arch:
                {
                    var mat = biome == BiomeKind.Tropical ? Lit(new Color(1f, 0.5f, 0.6f)) : Lit(new Color(0.9f, 0.9f, 0.95f));
                    Part(t, Cylinder, mat, new Vector3(-1.4f, 1.5f, 0), new Vector3(0.14f, 3f, 0.14f), Vector3.zero, "left");
                    Part(t, Cylinder, mat, new Vector3(1.4f, 1.5f, 0), new Vector3(0.14f, 3f, 0.14f), Vector3.zero, "right");
                    Part(t, Cube, mat, new Vector3(0, 3.05f, 0), new Vector3(3f, 0.14f, 0.14f), new Vector3(0, 0, 8), "top");
                    Part(t, Sphere, MaterialLibrary.Emissive(new Color(1f, 0.4f, 0.55f), 1.8f), new Vector3(0, 3.35f, 0), new Vector3(0.5f, 0.5f, 0.5f), Vector3.zero, "heart");
                    break;
                }
                case PropKind.Crate:
                {
                    var wood = Lit(new Color(0.62f, 0.48f, 0.3f));
                    Part(t, Cube, wood, new Vector3(0, 0.5f, 0), new Vector3(1f, 1f, 1f), new Vector3(0, rng.Range(0, 45), 0), "box");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.05f, 1.05f, 1.05f); box.center = new Vector3(0, 0.5f, 0);
                    break;
                }
                case PropKind.Pylon:
                {
                    var metal = Lit(new Color(0.5f, 0.55f, 0.6f));
                    Part(t, Cylinder, metal, new Vector3(0, 3f, 0), new Vector3(0.5f, 6f, 0.5f), Vector3.zero, "column");
                    Part(t, Sphere, MaterialLibrary.Emissive(new Color(0.3f, 0.9f, 1f), 2.4f), new Vector3(0, 6.4f, 0), new Vector3(1f, 1f, 1f), Vector3.zero, "orb");
                    var box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.7f, 6.5f, 0.7f); box.center = new Vector3(0, 3.2f, 0);
                    break;
                }
                case PropKind.Shell:
                {
                    Part(t, Sphere, Lit(new Color(0.95f, 0.9f, 0.85f)), new Vector3(0, 0.06f, 0), new Vector3(0.3f, 0.12f, 0.3f), Vector3.zero, "shell");
                    break;
                }
            }
            return root;
        }

        /// <summary>Biome-driven prop palette. Region data decides density; this decides what grows where.</summary>
        public static System.Collections.Generic.IReadOnlyList<PropKind> Palette(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Tropical:
                case BiomeKind.Coastal:
                case BiomeKind.LuxuryIsland:
                    return new[] { PropKind.Palm, PropKind.Rock, PropKind.Umbrella, PropKind.Towel, PropKind.Shell, PropKind.Bench };
                case BiomeKind.FantasyIslands:
                    return new[] { PropKind.Broadleaf, PropKind.Crystal, PropKind.CloudPlatform, PropKind.Rock, PropKind.Arch };
                case BiomeKind.CyberCity:
                    return new[] { PropKind.NeonTower, PropKind.StreetLamp, PropKind.Crate, PropKind.Sign };
                case BiomeKind.ModernCity:
                    return new[] { PropKind.Tower, PropKind.House, PropKind.StreetLamp, PropKind.Bench, PropKind.Sign };
                case BiomeKind.Resort:
                    return new[] { PropKind.House, PropKind.Palm, PropKind.Umbrella, PropKind.Towel, PropKind.Lantern, PropKind.DockPlank };
                case BiomeKind.Mountain:
                    return new[] { PropKind.Pine, PropKind.Rock, PropKind.Broadleaf, PropKind.Bench };
                case BiomeKind.NightForest:
                    return new[] { PropKind.Pine, PropKind.Broadleaf, PropKind.Rock, PropKind.Lantern, PropKind.Bench };
                case BiomeKind.Cave:
                    return new[] { PropKind.Crystal, PropKind.Rock, PropKind.RuinColumn };
                case BiomeKind.Ocean:
                case BiomeKind.Underwater:
                    return new[] { PropKind.Coral, PropKind.Rock, PropKind.Crystal, PropKind.Shell };
                case BiomeKind.Snow:
                    return new[] { PropKind.SnowPine, PropKind.Rock, PropKind.Cabin, PropKind.Lantern, PropKind.Bench };
                case BiomeKind.Desert:
                    return new[] { PropKind.Cactus, PropKind.Rock, PropKind.RuinColumn, PropKind.Sign };
                case BiomeKind.Countryside:
                    return new[] { PropKind.Broadleaf, PropKind.Fence, PropKind.Windmill, PropKind.House, PropKind.Flower, PropKind.Bench };
                case BiomeKind.Meadows:
                    return new[] { PropKind.Flower, PropKind.Broadleaf, PropKind.Bench, PropKind.Arch, PropKind.Lantern };
                case BiomeKind.Ruins:
                    return new[] { PropKind.RuinColumn, PropKind.RuinArch, PropKind.Rock, PropKind.Broadleaf };
                case BiomeKind.Highway:
                    return new[] { PropKind.StreetLamp, PropKind.Sign, PropKind.Rock };
                case BiomeKind.Lake:
                    return new[] { PropKind.Broadleaf, PropKind.DockPlank, PropKind.Boat, PropKind.Bench, PropKind.Lantern };
                case BiomeKind.Volcano:
                    return new[] { PropKind.Rock, PropKind.Crystal, PropKind.RuinColumn };
                case BiomeKind.Sky:
                case BiomeKind.Cloud:
                    return new[] { PropKind.CloudPlatform, PropKind.Crystal, PropKind.Arch, PropKind.Lantern };
                case BiomeKind.Harbor:
                    return new[] { PropKind.Crate, PropKind.DockPlank, PropKind.Boat, PropKind.StreetLamp, PropKind.Sign };
                case BiomeKind.Tech:
                    return new[] { PropKind.Tower, PropKind.Pylon, PropKind.Crate, PropKind.Sign };
                default:
                    return new[] { PropKind.Rock, PropKind.Broadleaf };
            }
        }
    }

    /// <summary>Slow decorative rotation for windmills and similar moving props.</summary>
    public sealed class PropSpinner : MonoBehaviour
    {
        public Transform target;
        public float speed = 30f;
        void Update() { if (target != null) target.Rotate(Vector3.forward, speed * Time.deltaTime, Space.Self); }
    }
}
