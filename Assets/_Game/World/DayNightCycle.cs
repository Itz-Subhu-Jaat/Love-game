using LoveGame.Core;
using UnityEngine;

namespace LoveGame.World
{
    /// <summary>Queue point between the streamer (region environment) and the renderer service.</summary>
    public static class EnvironmentBlender
    {
        static EnvironmentRequest? _pending;
        public static void Request(in EnvironmentRequest request) => _pending = request;
        public static bool Poll(out EnvironmentRequest request)
        {
            if (_pending.HasValue) { request = _pending.Value; _pending = null; return true; }
            request = default;
            return false;
        }
    }

    /// <summary>
    /// Full day/night cycle: configurable real-time day length, rotating sun/moon light,
    /// gradient sky + stars + procedural ambient. Region profiles and weather modulate it.
    /// </summary>
    public sealed class DayNightCycle : IGameService
    {
        public string ServiceName => "DayNightCycle";

        Light _sun;
        Transform _sunTransform;
        float _hour;
        float _hourPerSecond;
        EnvironmentRequest _current;
        EnvironmentRequest _target;
        float _blend;
        float _weatherSunMul = 1f;
        float _weatherFogMul = 1f;
        Color _weatherFogTint = Color.white;
        Material _skyboxInstance;
        int _lastPublishedHour = -1;

        /// <summary>Real minutes for a full 24h cycle.</summary>
        public float DayLengthMinutes { get; set; } = 24f;
        public float Hour
        {
            get => _hour;
            set { _hour = Mathf.Repeat(value, 24f); }
        }
        /// <summary>0 at night, 1 at noon - used by shaders and gameplay gating.</summary>
        public float DayFactor => Mathf.Clamp01(Mathf.InverseLerp(5.5f, 8f, _hour) * Mathf.InverseLerp(20.5f, 18f, _hour));
        public float SunAltitudeDeg => 90f * Mathf.Sin((_hour - 6f) / 12f * Mathf.PI);

        public void Initialize()
        {
            _hour = SaveSystem.Current.world.worldTimeHour;
            _hourPerSecond = 24f / (DayLengthMinutes * 60f);
            _current = DefaultEnvironment();
            _target = _current;

            var sunGo = new GameObject("~Sun");
            _sunTransform = sunGo.transform;
            UnityEngine.Object.DontDestroyOnLoad(sunGo);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowDistance = 180f;

            _skyboxInstance = new Material(MaterialLibrary.Skybox);
            RenderSettings.skybox = _skyboxInstance;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            GameEvents.Subscribe<LowMemoryWarningEvent>(OnLowMemory);
            ApplyHour(0f);
        }

        static EnvironmentRequest DefaultEnvironment() => new EnvironmentRequest
        {
            SkyTop = new Color(0.31f, 0.76f, 0.97f),
            SkyHorizon = new Color(0.74f, 0.91f, 0.96f),
            SkyBottom = new Color(0.91f, 0.85f, 0.66f),
            SunColor = new Color(1f, 0.95f, 0.84f),
            SunIntensity = 1.1f,
            Ambient = new Color(0.62f, 0.72f, 0.78f),
            FogColor = new Color(0.81f, 0.91f, 0.94f),
            FogDensity = 0.0016f,
            Ambience = "wind"
        };

        public void Tick(float delta)
        {
            ApplyHour(_hourPerSecond * delta);

            if (EnvironmentBlender.Poll(out var req)) _target = req;

            _blend = Mathf.MoveTowards(_blend, 1f, delta / 12f); // 12s region blend
            _current.SkyTop = Color.Lerp(_current.SkyTop, _target.SkyTop, delta * 0.4f);
            _current.SkyHorizon = Color.Lerp(_current.SkyHorizon, _target.SkyHorizon, delta * 0.4f);
            _current.SkyBottom = Color.Lerp(_current.SkyBottom, _target.SkyBottom, delta * 0.4f);
            _current.SunColor = Color.Lerp(_current.SunColor, _target.SunColor, delta * 0.4f);
            _current.Ambient = Color.Lerp(_current.Ambient, _target.Ambient, delta * 0.4f);
            _current.FogColor = Color.Lerp(_current.FogColor, _target.FogColor, delta * 0.4f);
            _current.SunIntensity = Mathf.Lerp(_current.SunIntensity, _target.SunIntensity, delta * 0.4f);
            _current.FogDensity = Mathf.Lerp(_current.FogDensity, _target.FogDensity, delta * 0.4f);

            RenderSkyAndLight();
        }

        void ApplyHour(float deltaHours)
        {
            _hour = Mathf.Repeat(_hour + deltaHours, 24f);
            SaveSystem.Current.world.worldTimeHour = _hour;
            int whole = Mathf.FloorToInt(_hour);
            if (whole != _lastPublishedHour)
            {
                _lastPublishedHour = whole;
                GameEvents.Publish(new TimeOfDayChangedEvent { Hour = _hour });
            }
        }

        void RenderSkyAndLight()
        {
            float day = DayFactor;
            float dusk = 1f - day; // night amount

            // sun rotation: sunrise east (-90) .. noon overhead .. sunset west
            float sunAngle = (_hour / 24f) * 360f - 90f;
            _sunTransform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

            float sunStrength = Mathf.Max(0.04f, Mathf.Sin((_hour - 6f) / 12f * Mathf.PI)) * _current.SunIntensity * _weatherSunMul;
            bool night = sunStrength < 0.08f;
            _sun.enabled = true;
            _sun.color = night
                ? new Color(0.55f, 0.65f, 0.95f) * 0.5f       // moonlight
                : Color.Lerp(new Color(1f, 0.55f, 0.35f), _current.SunColor, Mathf.Clamp01(sunStrength * 2f));
            _sun.intensity = night ? 0.22f : sunStrength;
            if (night) _sunTransform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f); // moon opposite

            // sky
            var dayTop = _current.SkyTop;
            var dayHor = _current.SkyHorizon;
            var dayBot = _current.SkyBottom;
            var nightTop = new Color(0.02f, 0.03f, 0.1f);
            var nightHor = new Color(0.05f, 0.07f, 0.19f);
            var nightBot = new Color(0.08f, 0.09f, 0.2f);
            var duskTint = new Color(1f, 0.55f, 0.35f);
            float duskAmount = Mathf.Clamp01(1f - Mathf.Abs(_hour - 18.5f) / 2.2f) + Mathf.Clamp01(1f - Mathf.Abs(_hour - 5.5f) / 2.2f);

            _skyboxInstance.SetColor("_TopColor", Color.Lerp(nightTop, dayTop, day));
            _skyboxInstance.SetColor("_HorizonColor", Color.Lerp(nightHor, dayHor, day));
            _skyboxInstance.SetColor("_BottomColor", Color.Lerp(nightBot, dayBot, day));
            _skyboxInstance.SetColor("_HorizonColor", Color.Lerp((Color)_skyboxInstance.GetColor("_HorizonColor"), duskTint, duskAmount * 0.45f));
            _skyboxInstance.SetFloat("_NightFactor", 1f - day);
            _skyboxInstance.SetVector("_SunDirection", -_sunTransform.forward);

            // ambient
            var ambient = Color.Lerp(_current.Ambient * 0.18f + new Color(0.02f, 0.03f, 0.08f, 0f), _current.Ambient, day);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = ambient * 0.85f;
            RenderSettings.ambientGroundColor = ambient * 0.6f;

            // fog (weather-tinted)
            var fog = Color.Lerp(_current.FogColor * 0.25f, _current.FogColor, day);
            fog = Color.Lerp(fog, _weatherFogTint, Mathf.Clamp01(_weatherFogMul - 1f));
            RenderSettings.fogColor = fog;
            var fogDensity = _current.FogDensity * Mathf.Lerp(1.5f, 1f, day) * _weatherFogMul;
            RenderSettings.fogDensity = fogDensity;
            MaterialLibrary.SetGlobalFog(fog, fogDensity, 60f);
            MaterialLibrary.SetSun(-_sunTransform.forward, day);
        }

        /// <summary>Weather modulation hooks (called by WeatherSystem).</summary>
        public void SetWeatherMultiplier(float sunMul, float fogMul, Color fogTint)
        {
            _weatherSunMul = sunMul;
            _weatherFogMul = fogMul;
            _weatherFogTint = fogTint;
        }

        public bool IsNight => _hour < 5.5f || _hour > 20f;

        void OnLowMemory(LowMemoryWarningEvent evt) { /* skybox is tiny; nothing to shed */ }

        public void Shutdown()
        {
            GameEvents.Unsubscribe<LowMemoryWarningEvent>(OnLowMemory);
            if (_sunTransform != null) UnityEngine.Object.Destroy(_sunTransform.gameObject);
            if (_skyboxInstance != null) UnityEngine.Object.Destroy(_skyboxInstance);
        }
    }
}
