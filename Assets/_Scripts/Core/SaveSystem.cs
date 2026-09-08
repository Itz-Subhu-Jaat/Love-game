using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>
    /// Persists the best score and the mute preference.
    /// Uses PlayerPrefs, which works on desktop builds and WebGL browsers alike.
    /// </summary>
    public static class SaveSystem
    {
        private const string KeyBest = "LoveQuest.BestScore";
        private const string KeyMuted = "LoveQuest.Muted";

        private static int _best;
        private static bool _muted;
        private static bool _loaded;

        public static int BestScore
        {
            get { EnsureLoaded(); return _best; }
        }

        public static bool Muted
        {
            get { EnsureLoaded(); return _muted; }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _best = PlayerPrefs.GetInt(KeyBest, 0);
            _muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;
            _loaded = true;
        }

        /// <summary>Stores the score if it beats the record. Returns true on new record.</summary>
        public static bool SubmitScore(int score)
        {
            EnsureLoaded();
            if (score <= _best) return false;
            _best = score;
            PlayerPrefs.SetInt(KeyBest, _best);
            PlayerPrefs.Save();
            return true;
        }

        public static void SetMuted(bool muted)
        {
            EnsureLoaded();
            _muted = muted;
            PlayerPrefs.SetInt(KeyMuted, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
