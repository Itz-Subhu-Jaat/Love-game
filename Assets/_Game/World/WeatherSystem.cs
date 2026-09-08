using System;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    public enum WeatherType { Clear, Cloudy, Rain, Storm, Fog }

    /// <summary>
    /// Region-aware weather: weighted state selection per region, smooth transitions,
    /// rain/storm particles that follow the player, fog and sun modulation hooks.
    /// Data-driven via each region's weatherWeights; behavior extensions subscribe to events.
    /// </summary>
    public sealed class WeatherSystem : IGameService
    {
        public string ServiceName => "Weather";

        public WeatherType Current { get; private set; } = WeatherType.Clear;
        public WeatherType Target { get; private set; } = WeatherType.Clear;
        public float Transition { get; private set; } = 1f;

        DayNightCycle _dayNight;
        RegionCatalogService _catalog;
        ParticleSystem _rain;
        ParticleSystem _snow;
        float _changeCooldown = 60f;
        Vector3 _playerPos;

        static readonly Dictionary<WeatherType, (float sun, float fog, Color tint)> Profiles =
            new Dictionary<WeatherType, (float, float, Color)>
            {
                { WeatherType.Clear,  (1.00f, 1.00f, new Color(1f, 1f, 1f)) },
                { WeatherType.Cloudy, (0.72f, 1.35f, new Color(0.85f, 0.87f, 0.92f)) },
                { WeatherType.Rain,   (0.45f, 2.20f, new Color(0.55f, 0.62f, 0.72f)) },
                { WeatherType.Storm,  (0.30f, 3.00f, new Color(0.42f, 0.45f, 0.55f)) },
                { WeatherType.Fog,    (0.80f, 4.50f, new Color(0.78f, 0.82f, 0.88f)) },
            };

        public event Action<WeatherType> WeatherChanged;

        public void Initialize()
        {
            _dayNight = Services.Get<DayNightCycle>();
            _catalog = Services.Get<RegionCatalogService>();
            Current = ParseWeather(SaveSystem.Current.world.weather, WeatherType.Clear);
            Target = Current;
            BuildParticles(ref _rain, "Rain", new Color(0.6f, 0.75f, 1f, 0.55f), 4200, 26f);
            BuildParticles(ref _snow, "Snow", new Color(1f, 1f, 1f, 0.85f), 2400, 6f);
            ApplyProfileInstant();
        }

        static WeatherType ParseWeather(string value, WeatherType fallback) =>
            Enum.TryParse(value, true, out WeatherType w) ? w : fallback;

        public void Tick(float delta)
        {
            _changeCooldown -= delta;
            if (_changeCooldown <= 0f && _catalog != null)
            {
                _changeCooldown = UnityEngine.Random.Range(90f, 200f);
                var region = _catalog.ActiveRegion;
                if (region != null) RollWeather(region);
            }

            if (Transition < 1f)
            {
                Transition = Mathf.MoveTowards(Transition, 1f, delta / 15f);
                ApplyProfileInstant();
                if (Transition >= 1f && Current != Target) OnWeatherCommitted(Target);
            }

            var rainRate = RainRate(Current, Target, Transition);
            if (_rain != null)
            {
                var em = _rain.emission;
                em.rateOverTimeMultiplier = rainRate;
                _rain.transform.position = _playerPos + Vector3.up * 18f;
            }
            if (_snow != null)
            {
                var region = _catalog?.ActiveRegion;
                bool snowRegion = region != null && (region.Biome == BiomeKind.Snow || region.Biome == BiomeKind.Mountain);
                var em = _snow.emission;
                em.rateOverTimeMultiplier = snowRegion ? rainRate : 0f;
                _snow.transform.position = _playerPos + Vector3.up * 14f;
            }

            if (Current == WeatherType.Storm || Target == WeatherType.Storm)
            {
                if (UnityEngine.Random.value < delta * 0.08f)
                    GameEvents.Publish(new ThunderRequestedEvent());
            }
        }

        public Vector3 PlayerPosition { set => _playerPos = value; }

        void RollWeather(RegionData region)
        {
            var w = region.WeatherWeights;
            float total = Mathf.Max(1f, w[0] + w[1] + w[2] + w[3] + w[4]);
            float roll = UnityEngine.Random.value * total;
            var pick = WeatherType.Clear;
            for (int i = 0; i < 5; i++)
            {
                if (roll < w[i]) { pick = (WeatherType)i; break; }
                roll -= w[i];
            }
            if (pick == Target) return;
            Target = pick;
            Transition = 0f;
            Log.Info("Weather", $"region '{region.Id}' weather -> {pick}");
        }

        float RainRate(WeatherType a, WeatherType b, float t)
        {
            float RateOf(WeatherType w) => w == WeatherType.Rain ? 0.55f : w == WeatherType.Storm ? 1f : 0f;
            return Mathf.Lerp(RateOf(a), RateOf(b), t);
        }

        void ApplyProfileInstant()
        {
            var a = Profiles[Current];
            var b = Profiles[Target];
            float sun = Mathf.Lerp(a.sun, b.sun, Transition);
            float fog = Mathf.Lerp(a.fog, b.fog, Transition);
            var tint = Color.Lerp(a.tint, b.tint, Transition);
            _dayNight?.SetWeatherMultiplier(sun, fog, tint);
        }

        void OnWeatherCommitted(WeatherType w)
        {
            Current = w;
            SaveSystem.Current.world.weather = w.ToString();
            GameEvents.Publish(new WeatherChangedEvent { Weather = w.ToString() });
            WeatherChanged?.Invoke(w);
        }

        /// <summary>Debug/testing + fast travel hook: force a weather state immediately.</summary>
        public void Force(WeatherType type)
        {
            Target = type;
            Transition = 1f;
            OnWeatherCommitted(type);
            ApplyProfileInstant();
        }

        void BuildParticles(ref ParticleSystem system, string name, Color color, float rate, float fallSpeed)
        {
            var go = new GameObject($"~{name}");
            UnityEngine.Object.DontDestroyOnLoad(go);
            system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = 2.2f;
            main.startSpeed = fallSpeed;
            main.startSize = 0.12f;
            main.maxParticles = Mathf.RoundToInt(rate * 3);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTimeMultiplier = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 1f, 60f);
            var vel = system.velocityOverLifetime;
            vel.y = -fallSpeed;
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(color);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialLibrary.Particle;
            renderer.material.color = color;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            if (name == "Snow") { main.startSize = 0.2f; renderer.renderMode = ParticleSystemRenderMode.Billboard; }
        }

        public void Shutdown()
        {
            if (_rain != null) UnityEngine.Object.Destroy(_rain.gameObject);
            if (_snow != null) UnityEngine.Object.Destroy(_snow.gameObject);
        }
    }
}
