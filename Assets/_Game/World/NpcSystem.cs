using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>NPC simulation tiers - full quality only near the player.</summary>
    public enum SimulationTier { Near, Medium, Far, OutOfRange }

    /// <summary>
    /// Scalable NPC population: spawns per active region from NpcZone POIs, wanders on
    /// sidewalks/paths, talks through the interaction system. Distance tiers keep cities cheap.
    /// </summary>
    public sealed class NpcAgent : MonoBehaviour
    {
        public string NpcId;
        public string DisplayName;
        public RegionData Region;
        public SimulationTier Tier { get; private set; } = SimulationTier.Near;

        Vector3 _anchor;
        Vector3 _target;
        float _retargetTimer;
        float _tickTimer;
        CharacterController _controller;
        static readonly string[] Chatter =
        {
            "Lovely weather for a walk, isn't it?",
            "Have you seen the sunset from the cliffs?",
            "Welcome! Feel free to look around.",
            "They say a hidden treasure lies beyond the ridge...",
            "The stars are extra bright tonight.",
            "My favorite cafe is just around the corner.",
            "A couple was dancing here just yesterday.",
            "Careful near the water at storm time!",
        };

        public static NpcAgent Build(Transform parent, RegionData region, PoiDefinition zone, int index)
        {
            var go = new GameObject($"npc_{zone.id}_{index}");
            go.transform.SetParent(parent, false);
            go.layer = GameLayers.Npc;

            // placeholder silhouette: capsule body + sphere head + accent
            var outfit = new Color[] { new Color(0.85f, 0.32f, 0.36f), new Color(0.28f, 0.52f, 0.85f), new Color(0.95f, 0.75f, 0.3f), new Color(0.4f, 0.75f, 0.45f) }[index % 4];
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var bodyCol = body.GetComponent<Collider>();
            UnityEngine.Object.Destroy(bodyCol);
            body.name = "body";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Tinted(MaterialLibrary.Lit, outfit, $"npc{index % 4}");

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());
            head.name = "head";
            head.transform.SetParent(go.transform, false);
            head.transform.localScale = Vector3.one * 0.3f;
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.96f, 0.8f, 0.68f), "skin");

            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.95f, 0f);

            var agent = go.AddComponent<NpcAgent>();
            agent._controller = controller;
            agent.Region = region;
            agent.NpcId = $"{zone.id}_{index}";
            agent.DisplayName = Names[index % Names.Length];
            agent._anchor = go.transform.position;
            agent._target = agent._anchor;

            var inter = go.AddComponent<Interactable>();
            inter.interactableId = $"npc_{agent.NpcId}";
            inter.prompt = $"Talk to {agent.DisplayName}";
            inter.range = 3f;
            inter.context = "Npc";
            inter.InteractEvent += interactor => agent.SayLine();
            return agent;
        }

        static readonly string[] Names = { "Mia", "Kai", "Elena", "Ravi", "Sora", "Nadia", "Theo", "Luna", "Ajay", "Iris" };

        void SayLine()
        {
            var line = Chatter[Random.Range(0, Chatter.Length)];
            GameEvents.Publish(new NotificationEvent { Title = DisplayName, Body = line, Duration = 3.5f });
        }

        public void SetAnchor(Vector3 worldPos)
        {
            _anchor = worldPos;
            transform.position = worldPos;
            _target = worldPos;
        }

        public void SetTier(SimulationTier tier)
        {
            if (tier == Tier) return;
            Tier = tier;
            // Out of range: fully deactivate (pooled by manager)
            if (tier == SimulationTier.OutOfRange)
            {
                gameObject.SetActive(false);
            }
            else if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Simulate(float delta, Vector3 playerPos)
        {
            if (Tier == SimulationTier.OutOfRange) return;
            _tickTimer -= delta;
            if (_tickTimer > 0f) return;
            // tier-controlled tick rate: near every frame, far every 2s
            _tickTimer = Tier switch
            {
                SimulationTier.Near => 0f,
                SimulationTier.Medium => 0.25f,
                _ => 2f,
            };
            float step = _tickTimer <= 0f ? delta : _tickTimer + delta;

            _retargetTimer -= step;
            if (_retargetTimer <= 0f || (transform.position - _target).sqrMagnitude < 1f)
            {
                _retargetTimer = Random.Range(4f, 12f);
                var streamer = Services.Get<WorldStreamer>();
                if (streamer != null)
                {
                    var offset = Random.insideUnitCircle * 14f;
                    var dest = _anchor + new Vector3(offset.x, 0f, offset.y);
                    dest.y = streamer.SampleHeight(dest.x, dest.z) + 1f;
                    if (dest.y > Region.WaterLevel + 0.4f) _target = dest;
                }
            }

            var toTarget = _target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.25f && Tier == SimulationTier.Near)
            {
                var move = toTarget.normalized * (1.4f * step);
                if (move.sqrMagnitude > toTarget.sqrMagnitude) move = toTarget;
                _controller.SimpleMove(move.normalized * 1.4f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 4f * step);
            }
        }
    }

    /// <summary>Spawns and tiers NPCs around active regions; recycles pooled bodies on unload.</summary>
    public sealed class NpcManager : IGameService
    {
        public string ServiceName => "NpcManager";

        readonly List<NpcAgent> _agents = new List<NpcAgent>();
        Transform _root;
        WorldStreamer _streamer;
        RegionCatalogService _catalog;
        int _npcBudget;

        public int ActiveCount => _agents.Count;

        public void Initialize()
        {
            _streamer = Services.Get<WorldStreamer>();
            _catalog = Services.Get<RegionCatalogService>();
            _root = new GameObject("~NPCs").transform;
            UnityEngine.Object.DontDestroyOnLoad(_root.gameObject);
            _npcBudget = GameConfig.Quality switch
            {
                Core.QualityLevel.Low => 10,
                Core.QualityLevel.Medium => 18,
                Core.QualityLevel.High => 26,
                _ => 34
            };
            _streamer.RegionBuilt += OnRegionBuilt;
            _streamer.RegionUnloaded += OnRegionUnloaded;
        }

        void OnRegionBuilt(RegionInstance inst)
        {
            if (inst.Data.Biome == BiomeKind.Underwater || inst.Data.Biome == BiomeKind.Sky) return;
            var zones = new List<PoiDefinition>();
            foreach (var poi in inst.Data.Pois)
                if (poi.kind == PoiKind.NpcZone || poi.kind == PoiKind.Shop || poi.kind == PoiKind.Restaurant || poi.kind == PoiKind.Plaza)
                    zones.Add(poi);
            if (zones.Count == 0) return;

            var rng = new Rng(inst.Data.Seed + 77);
            int count = Mathf.Clamp(zones.Count * 2, 2, Mathf.Max(2, _npcBudget / 3));
            for (int i = 0; i < count; i++)
            {
                var zone = rng.Pick(zones);
                var local = new Vector3(zone.x + rng.Range(-zone.radius, zone.radius), 0f, zone.z + rng.Range(-zone.radius, zone.radius));
                var world = inst.Data.WorldCenter + local;
                float ground = _streamer.SampleHeight(world.x, world.z);
                if (ground < inst.Data.WaterLevel + 0.3f) continue;
                world.y = ground + 1.1f;
                var agent = NpcAgent.Build(_root, inst.Data, zone, i);
                agent.SetAnchor(world);
                _agents.Add(agent);
                if (_agents.Count >= _npcBudget) break;
            }
        }

        void OnRegionUnloaded(string regionId)
        {
            for (int i = _agents.Count - 1; i >= 0; i--)
            {
                if (_agents[i].Region != null && _agents[i].Region.Id == regionId)
                {
                    UnityEngine.Object.Destroy(_agents[i].gameObject);
                    _agents.RemoveAt(i);
                }
            }
        }

        public void Tick(float delta)
        {
            var player = _streamer.PlayerPosition;
            for (int i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                float dist = (agent.transform.position - player).magnitude;
                var tier = dist switch
                {
                    < 30f => SimulationTier.Near,
                    < 70f => SimulationTier.Medium,
                    < 140f => SimulationTier.Far,
                    _ => SimulationTier.OutOfRange
                };
                agent.SetTier(tier);
                agent.Simulate(delta, player);
            }
        }

        public void Shutdown()
        {
            if (_streamer != null)
            {
                _streamer.RegionBuilt -= OnRegionBuilt;
                _streamer.RegionUnloaded -= OnRegionUnloaded;
            }
            foreach (var a in _agents) if (a != null) UnityEngine.Object.Destroy(a.gameObject);
            _agents.Clear();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        }
    }
}
