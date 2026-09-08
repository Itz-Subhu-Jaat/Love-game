using System;
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Audio
{
    /// <summary>
    /// Synthesized clips - 100% original (no third-party music, no licensing risk).
    /// Loops and one-shots are generated once and cached. Replace with licensed assets later.
    /// </summary>
    public static class ProceduralAudioLibrary
    {
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        const int SampleRate = 22050;

        public static AudioClip OceanLoop() => Get("ocean", 4f, t =>
        {
            float slow = Mathf.Sin(t * 0.35f) * 0.4f + Mathf.Sin(t * 0.13f) * 0.3f;
            float waves = Mathf.PerlinNoise(t * 0.8f, 0f) * 0.6f;
            return (slow + waves - 0.45f) * 0.35f;
        });

        public static AudioClip WindLoop() => Get("wind", 5f, t =>
        {
            float gust = Mathf.PerlinNoise(t * 0.25f, 5.3f);
            float hiss = Mathf.PerlinNoise(t * 1.7f, 9.1f) * 0.4f;
            return (gust * 0.6f + hiss - 0.35f) * 0.3f;
        });

        public static AudioClip ForestLoop() => Get("forest", 6f, t =>
        {
            float baseAmbient = Mathf.PerlinNoise(t * 0.4f, 1.2f) * 0.2f - 0.1f;
            float chirpPhase = (t * 0.5f) % 1f;
            float chirp = chirpPhase < 0.06f ? Mathf.Sin(chirpPhase / 0.06f * Mathf.PI * 14f) * 0.12f : 0f;
            return baseAmbient + chirp;
        });

        public static AudioClip CityLoop() => Get("city", 5f, t =>
        {
            float rumble = Mathf.PerlinNoise(t * 0.3f, 3.7f) * 0.25f - 0.12f;
            float pulse = Mathf.Sin(t * 2.2f) * 0.05f;
            return rumble + pulse;
        });

        public static AudioClip CaveLoop() => Get("cave", 7f, t =>
        {
            float deep = Mathf.Sin(t * 0.12f) * 0.15f;
            float dripPhase = (t * 0.7f) % 1f;
            float drip = dripPhase < 0.02f ? 0.25f * Mathf.Sin(dripPhase / 0.02f * Mathf.PI) : 0f;
            return deep + drip;
        });

        public static AudioClip DesertLoop() => Get("desert", 5f, t => (Mathf.PerlinNoise(t * 0.2f, 8.8f) - 0.5f) * 0.2f);

        public static AudioClip UnderwaterLoop() => Get("underwater", 6f, t =>
            Mathf.Sin(t * 0.18f) * 0.18f + (Mathf.PerlinNoise(t * 0.6f, 2.2f) - 0.5f) * 0.1f);

        public static AudioClip RainLoop() => Get("rain", 4f, t =>
            (UnityEngine.Random.value * 0.25f + Mathf.PerlinNoise(t * 3f, 0.5f) * 0.2f - 0.22f) * 0.8f);

        public static AudioClip EngineLoop() => Get("engine", 2f, t =>
            (Mathf.Sin(t * 28f) * 0.3f + Mathf.Sin(t * 57f) * 0.15f + (UnityEngine.Random.value - 0.5f) * 0.1f) * 0.4f);

        public static AudioClip MusicPad() => Get("musicpad", 8f, t =>
        {
            // gentle romantic chord progression: Am - F - C - G, one chord per 2 seconds
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f },
                new[] { 174.61f, 220.00f, 261.63f },
                new[] { 130.81f, 164.81f, 196.00f, 261.63f },
                new[] { 196.00f, 246.94f, 293.66f },
            };
            int chordIdx = Mathf.FloorToInt((t * 0.5f) % 4f);
            var chord = chords[chordIdx];
            float sample = 0f;
            for (int i = 0; i < chord.Length; i++)
                sample += Mathf.Sin(t * Mathf.PI * 2f * chord[i]) / chord.Length;
            float chordT = (t * 0.5f) % 1f;
            float env = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(chordT * 6f)) * (1f - Mathf.Clamp01((chordT - 0.85f) / 0.15f));
            return sample * env * 0.5f;
        });

        public static AudioClip UiClick() => OneShot("uiclick", 0.08f, t => Mathf.Sin(t / 0.08f * Mathf.PI * 2f * 880f) * (1f - t / 0.08f) * 0.4f);
        public static AudioClip UiBack() => OneShot("uiback", 0.12f, t => Mathf.Sin(t / 0.12f * Mathf.PI * 2f * 440f) * (1f - t / 0.12f) * 0.3f);
        public static AudioClip CollectChime() => OneShot("chime", 0.5f, t =>
        {
            float env = Mathf.Exp(-t * 5f);
            return (Mathf.Sin(t * Mathf.PI * 2f * 1318.5f) * 0.5f + Mathf.Sin(t * Mathf.PI * 2f * 1760f) * 0.3f + Mathf.Sin(t * Mathf.PI * 2f * 2637f) * 0.2f) * env * 0.35f;
        });
        public static AudioClip HeartWarm() => OneShot("heart", 0.9f, t =>
        {
            float env = Mathf.Exp(-t * 3f);
            return (Mathf.Sin(t * Mathf.PI * 2f * 523.25f) * 0.4f + Mathf.Sin(t * Mathf.PI * 2f * 659.25f) * 0.4f + Mathf.Sin(t * Mathf.PI * 2f * 783.99f) * 0.2f) * env * 0.4f;
        });
        public static AudioClip Splash() => OneShot("splash", 0.4f, t => (UnityEngine.Random.value - 0.5f) * Mathf.Exp(-t * 8f) * 0.6f);
        public static AudioClip Thunder() => OneShot("thunder", 2.2f, t =>
        {
            float rumble = Mathf.PerlinNoise(t * 3f, 0.7f) - 0.4f;
            float crack = t < 0.15f ? (UnityEngine.Random.value - 0.5f) * 0.9f : 0f;
            return (rumble * 0.7f + crack) * Mathf.Exp(-t * 1.2f) * 0.9f;
        });
        public static AudioClip Footstep() => OneShot("step", 0.09f, t => (UnityEngine.Random.value - 0.5f) * (1f - t / 0.09f) * 0.22f);
        public static AudioClip FishSplash() => OneShot("fish", 0.6f, t => (Mathf.Sin(t * 40f) * 0.2f + (UnityEngine.Random.value - 0.5f) * 0.3f) * Mathf.Exp(-t * 5f) * 0.5f);

        static AudioClip Get(string key, float seconds, Func<float, float> synth)
        {
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            var length = Mathf.NextPowerOfTwo(Mathf.CeilToInt(seconds * SampleRate));
            clip = AudioClip.Create("lg_" + key, length, 1, SampleRate, false);
            var data = new float[length];
            for (int i = 0; i < length; i++) data[i] = Mathf.Clamp(synth(i / (float)SampleRate), -1f, 1f);
            clip.SetData(data, 0);
            Cache[key] = clip;
            return clip;
        }

        static AudioClip OneShot(string key, float seconds, Func<float, float> synth) => Get(key, seconds, synth);
    }

    /// <summary>
    /// Audio buses: music crossfade playlist, region/weather-driven ambience, pooled SFX.
    /// Reacts to game events so gameplay code never touches AudioSources directly.
    /// </summary>
    public sealed class AudioService : IGameService
    {
        public string ServiceName => "Audio";

        AudioSource _music, _ambience, _sfxA, _sfxB;
        string _ambienceId;
        float _crossfade = 1f;
        readonly Queue<AudioClip> _sfxQueue = new Queue<AudioClip>();

        void OnCollectible(CollectibleCollectedEvent e) => QueueSfx(ProceduralAudioLibrary.CollectChime());
        void OnCoupleStart(CoupleInteractionStartedEvent e) => QueueSfx(ProceduralAudioLibrary.HeartWarm());
        void OnGift(GiftGivenEvent e) => QueueSfx(ProceduralAudioLibrary.HeartWarm());
        void OnDownloadDone(ContentDownloadFinishedEvent e) { if (e.Success) QueueSfx(ProceduralAudioLibrary.CollectChime()); }
        void OnThunder(ThunderRequestedEvent e) => QueueSfx(ProceduralAudioLibrary.Thunder());
        void OnItemAdded(ItemAddedEvent e) => QueueSfx(ProceduralAudioLibrary.UiClick());

        public void Initialize()
        {
            var go = new GameObject("~Audio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _music = go.AddComponent<AudioSource>();
            _ambience = go.AddComponent<AudioSource>();
            _sfxA = go.AddComponent<AudioSource>();
            _sfxB = go.AddComponent<AudioSource>();
            _music.loop = true; _music.playOnAwake = false;
            _ambience.loop = true; _ambience.playOnAwake = false;
            _sfxA.loop = false; _sfxA.playOnAwake = false;
            _sfxB.loop = false; _sfxB.playOnAwake = false;

            ApplyVolumes();
            PlayMusic(ProceduralAudioLibrary.MusicPad());
            GameEvents.Subscribe<RegionChangedEvent>(OnRegionChanged);
            GameEvents.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            GameEvents.Subscribe<CollectibleCollectedEvent>(OnCollectible);
            GameEvents.Subscribe<CoupleInteractionStartedEvent>(OnCoupleStart);
            GameEvents.Subscribe<GiftGivenEvent>(OnGift);
            GameEvents.Subscribe<ContentDownloadFinishedEvent>(OnDownloadDone);
            GameEvents.Subscribe<ThunderRequestedEvent>(OnThunder);
            GameEvents.Subscribe<ItemAddedEvent>(OnItemAdded);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || _music == null) return;
            _music.clip = clip;
            _music.loop = true;
            _crossfade = 0f;
            _music.Play();
        }

        void OnRegionChanged(RegionChangedEvent evt)
        {
            AudioClip clip;
            switch (evt.RegionId)
            {
                case string id when id.Contains("ocean") || id.Contains("coral") || id.Contains("azure") || id.Contains("sunset") || id.Contains("harbor") || id.Contains("island") || id.Contains("starlight"):
                    clip = ProceduralAudioLibrary.OceanLoop(); break;
                case string id when id.Contains("city") || id.Contains("nova") || id.Contains("technova") || id.Contains("lumen") || id.Contains("neon"):
                    clip = ProceduralAudioLibrary.CityLoop(); break;
                case string id when id.Contains("cave") || id.Contains("crystal") || id.Contains("sunken"):
                    clip = ProceduralAudioLibrary.CaveLoop(); break;
                case string id when id.Contains("desert") || id.Contains("golden"):
                    clip = ProceduralAudioLibrary.DesertLoop(); break;
                case string id when id.Contains("forest") || id.Contains("moonlight") || id.Contains("wildheart") || id.Contains("verdant") || id.Contains("willow") || id.Contains("meadow"):
                    clip = ProceduralAudioLibrary.ForestLoop(); break;
                default:
                    clip = ProceduralAudioLibrary.WindLoop(); break;
            }
            SetAmbience(clip);
        }

        void OnWeatherChanged(WeatherChangedEvent evt)
        {
            if (evt.Weather == "Rain" || evt.Weather == "Storm")
                SetAmbience(ProceduralAudioLibrary.RainLoop());
        }

        public void SetAmbience(AudioClip clip)
        {
            if (clip == null || _ambience == null || clip.name == _ambienceId) return;
            _ambienceId = clip.name;
            _ambience.clip = clip;
            _ambience.loop = true;
            _ambience.Play();
        }

        public void QueueSfx(AudioClip clip)
        {
            if (clip != null) _sfxQueue.Enqueue(clip);
        }

        public void PlayThunder() => QueueSfx(ProceduralAudioLibrary.Thunder());

        public void ApplyVolumes()
        {
            var s = GameConfig.Settings;
            if (_music != null) _music.volume = Mathf.Lerp(_music.volume, s.musicVolume * s.masterVolume, 0.5f);
            if (_ambience != null) _ambience.volume = s.ambienceVolume * s.masterVolume * 0.8f;
            if (_sfxA != null) _sfxA.volume = s.sfxVolume * s.masterVolume;
            if (_sfxB != null) _sfxB.volume = s.sfxVolume * s.masterVolume;
        }

        public void Tick(float delta)
        {
            if (_crossfade < 1f)
            {
                _crossfade = Mathf.MoveTowards(_crossfade, 1f, delta / 3f);
                ApplyVolumes();
            }

            if (_sfxQueue.Count > 0)
            {
                var clip = _sfxQueue.Dequeue();
                var player = _sfxA.isPlaying && !_sfxB.isPlaying ? _sfxB : _sfxA;
                if (player.isPlaying) player.Stop();
                player.clip = clip;
                player.Play();
            }
        }

        public void Shutdown()
        {
            GameEvents.Unsubscribe<RegionChangedEvent>(OnRegionChanged);
            GameEvents.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
            GameEvents.Unsubscribe<CollectibleCollectedEvent>(OnCollectible);
            GameEvents.Unsubscribe<CoupleInteractionStartedEvent>(OnCoupleStart);
            GameEvents.Unsubscribe<GiftGivenEvent>(OnGift);
            GameEvents.Unsubscribe<ContentDownloadFinishedEvent>(OnDownloadDone);
            GameEvents.Unsubscribe<ThunderRequestedEvent>(OnThunder);
            GameEvents.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        }
    }
}
