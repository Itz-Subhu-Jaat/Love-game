using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>
    /// Ambient wildlife: bird flocks, butterflies near meadows, fish schools under water.
    /// All visuals are cheap pooled placeholders; behavior upgrades with proximity.
    /// </summary>
    public sealed class WildlifeManager : IGameService
    {
        public string ServiceName => "Wildlife";

        sealed class Flock
        {
            public GameObject Root;
            public Vector3 Anchor;
            public float Radius;
            public float Speed;
            public float Phase;
            public string RegionId;
            public bool Aquatic;
            public bool Active;
        }

        readonly List<Flock> _flocks = new List<Flock>();
        WorldStreamer _streamer;
        RegionCatalogService _catalog;
        Transform _root;
        int _budget;

        public int FlockCount => _flocks.Count;

        public void Initialize()
        {
            _streamer = Services.Get<WorldStreamer>();
            _catalog = Services.Get<RegionCatalogService>();
            _root = new GameObject("~Wildlife").transform;
            UnityEngine.Object.DontDestroyOnLoad(_root.gameObject);
            _budget = GameConfig.Quality switch { Core.QualityLevel.Low => 6, Core.QualityLevel.Medium => 10, _ => 16 };
            _streamer.RegionBuilt += OnRegionBuilt;
            _streamer.RegionUnloaded += OnRegionUnloaded;
        }

        void OnRegionBuilt(RegionInstance inst)
        {
            var d = inst.Data;
            int flocks = Mathf.Min(_budget - _flocks.Count, d.Biome switch
            {
                BiomeKind.Ocean => 2,
                BiomeKind.Underwater => 3,
                BiomeKind.Cave => 1,
                BiomeKind.CyberCity or BiomeKind.ModernCity or BiomeKind.Tech or BiomeKind.Highway => 1,
                _ => 3,
            });
            for (int i = 0; i < flocks; i++)
            {
                bool aquatic = d.Biome == BiomeKind.Ocean || d.Biome == BiomeKind.Underwater ||
                               (i == 0 && d.WaterLevel > -10f && d.Biome != BiomeKind.Desert && d.Biome != BiomeKind.Cave);
                var rng = new Rng(d.Seed + 913 + i);
                var anchorLocal = rng.InsideUnitCircle() * (d.Size * 0.3f);
                var anchor = d.WorldCenter + new Vector3(anchorLocal.x, 0f, anchorLocal.y);
                if (aquatic)
                {
                    anchor.y = d.WaterLevel - 1.5f;
                }
                else
                {
                    anchor.y = _streamer.SampleHeight(anchor.x, anchor.z) + rng.Range(6f, 16f);
                }
                var flock = new Flock
                {
                    Root = BuildFlockVisual(d, aquatic, rng),
                    Anchor = anchor,
                    Radius = rng.Range(8f, 20f),
                    Speed = aquatic ? rng.Range(1.5f, 2.5f) : rng.Range(3f, 6f),
                    Phase = rng.NextFloat() * Mathf.PI * 2f,
                    RegionId = d.Id,
                    Aquatic = aquatic,
                    Active = true,
                };
                flock.Root.transform.SetParent(_root, false);
                flock.Root.transform.position = anchor;
                _flocks.Add(flock);
            }
        }

        static GameObject BuildFlockVisual(RegionData d, bool aquatic, Rng rng)
        {
            var root = new GameObject("flock");
            int members = aquatic ? 6 : 5;
            Color color = aquatic
                ? new Color(0.55f, 0.75f, 0.9f)
                : d.Biome == BiomeKind.Meadows || d.Biome == BiomeKind.Tropical ? new Color(1f, 0.7f, 0.3f) : new Color(0.9f, 0.9f, 0.95f);
            var mat = MaterialLibrary.Tinted(MaterialLibrary.Lit, color, aquatic ? "fish" : "bird");
            for (int i = 0; i < members; i++)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(body.GetComponent<Collider>());
                body.name = $"m{i}";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(rng.Range(-2.5f, 2.5f), rng.Range(-1f, 1f), rng.Range(-2.5f, 2.5f));
                body.transform.localScale = aquatic ? new Vector3(0.12f, 0.18f, 0.5f) : new Vector3(0.35f, 0.06f, 0.14f);
                var mr = body.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }
            return root;
        }

        void OnRegionUnloaded(string regionId)
        {
            for (int i = _flocks.Count - 1; i >= 0; i--)
            {
                if (_flocks[i].RegionId != regionId) continue;
                if (_flocks[i].Root != null) UnityEngine.Object.Destroy(_flocks[i].Root);
                _flocks.RemoveAt(i);
            }
        }

        public void Tick(float delta)
        {
            var player = _streamer.PlayerPosition;
            for (int i = 0; i < _flocks.Count; i++)
            {
                var f = _flocks[i];
                var toFlock = f.Anchor - player;
                float dist = toFlock.magnitude;
                if (dist > 170f)
                {
                    if (f.Active) { f.Root.SetActive(false); f.Active = false; }
                    continue;
                }
                if (!f.Active) { f.Root.SetActive(true); f.Active = true; }

                f.Phase += delta * f.Speed * 0.15f;
                var center = f.Anchor + new Vector3(Mathf.Cos(f.Phase) * f.Radius, Mathf.Sin(f.Phase * 0.7f) * 1.5f, Mathf.Sin(f.Phase) * f.Radius);
                f.Root.transform.position = center;
                var dir = new Vector3(-Mathf.Sin(f.Phase), 0f, Mathf.Cos(f.Phase));
                if (dir.sqrMagnitude > 0.001f) f.Root.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        public void Shutdown()
        {
            if (_streamer != null)
            {
                _streamer.RegionBuilt -= OnRegionBuilt;
                _streamer.RegionUnloaded -= OnRegionUnloaded;
            }
            foreach (var f in _flocks) if (f.Root != null) UnityEngine.Object.Destroy(f.Root);
            _flocks.Clear();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        }
    }
}
