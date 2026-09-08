using UnityEngine;
using LoveGame.Core;

namespace LoveGame.Audio
{
    /// <summary>
    /// Persistent audio rig: one looping music source (procedural ambient pad)
    /// plus a one-shot SFX source. Survives scene changes via DontDestroyOnLoad
    /// and guards itself against duplicates.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        private static MusicPlayer _instance;

        private AudioSource _music;
        private AudioSource _sfx;

        private const float MusicVolume = 0.3f;

        /// <summary>Creates (once) or returns the persistent music core.</summary>
        public static MusicPlayer Ensure()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("MusicCore");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MusicPlayer>();
            _instance.Setup();
            return _instance;
        }

        private void Setup()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = ProceduralAudio.Get("music");
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = SaveSystem.Muted ? 0f : MusicVolume;
            if (_music.clip != null) _music.Play();

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = 1f;
        }

        /// <summary>Plays a named one-shot sound effect (respects mute).</summary>
        public static void PlaySfx(string name, float volume = 0.75f)
        {
            if (_instance == null || _instance._sfx == null) return;
            if (SaveSystem.Muted) return;

            var clip = ProceduralAudio.Get(name);
            if (clip == null) return;
            _instance._sfx.PlayOneShot(clip, volume);
        }

        /// <summary>Toggles mute for both music and effects, and persists it.</summary>
        public static void SetMuted(bool muted)
        {
            SaveSystem.SetMuted(muted);
            if (_instance != null && _instance._music != null)
            {
                _instance._music.volume = muted ? 0f : MusicVolume;
            }
        }
    }
}
