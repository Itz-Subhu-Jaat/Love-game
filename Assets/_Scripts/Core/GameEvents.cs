using System;

namespace LoveGame.Core
{
    /// <summary>
    /// Tiny static event hub. UI and gameplay never talk to each other directly,
    /// everything flows through these events so components stay decoupled.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Raised with (score, combo) whenever the score changes.</summary>
        public static event Action<int, int> ScoreChanged;

        /// <summary>Raised with the new lives count.</summary>
        public static event Action<int> LivesChanged;

        /// <summary>Raised when a new wave starts.</summary>
        public static event Action<int> WaveStarted;

        /// <summary>Raised with overall love progress (0..1).</summary>
        public static event Action<float> LoveMeterChanged;

        /// <summary>Raised when the player runs out of lives.</summary>
        public static event Action GameLost;

        /// <summary>Raised when the final wave is cleared.</summary>
        public static event Action GameWon;

        /// <summary>Raised with the power-up name (e.g. "slowmo").</summary>
        public static event Action<string> PowerUpPicked;

        /// <summary>Raised with the new pause state.</summary>
        public static event Action<bool> PausedChanged;

        // -------------------------------------------------------------- raisers

        public static void RaiseScoreChanged(int score, int combo)
        {
            if (ScoreChanged != null) ScoreChanged(score, combo);
        }

        public static void RaiseLivesChanged(int lives)
        {
            if (LivesChanged != null) LivesChanged(lives);
        }

        public static void RaiseWaveStarted(int wave)
        {
            if (WaveStarted != null) WaveStarted(wave);
        }

        public static void RaiseLoveMeterChanged(float progress)
        {
            if (LoveMeterChanged != null) LoveMeterChanged(progress);
        }

        public static void RaiseGameLost()
        {
            if (GameLost != null) GameLost();
        }

        public static void RaiseGameWon()
        {
            if (GameWon != null) GameWon();
        }

        public static void RaisePowerUpPicked(string name)
        {
            if (PowerUpPicked != null) PowerUpPicked(name);
        }

        public static void RaisePausedChanged(bool paused)
        {
            if (PausedChanged != null) PausedChanged(paused);
        }

        /// <summary>
        /// Called when a scene unloads so stale delegates from destroyed
        /// objects can never fire into a fresh scene.
        /// </summary>
        public static void ClearAll()
        {
            ScoreChanged = null;
            LivesChanged = null;
            WaveStarted = null;
            LoveMeterChanged = null;
            GameLost = null;
            GameWon = null;
            PowerUpPicked = null;
            PausedChanged = null;
        }
    }
}
