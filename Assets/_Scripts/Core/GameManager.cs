using System.Collections;
using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>
    /// Central run-state machine: score, lives, waves, combo, slow-motion and
    /// the win/lose transitions. The spawner and the UI talk to it through
    /// <see cref="GameEvents"/> and direct calls only.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Playing, Paused, Over, Won }

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Wave { get; private set; }
        public int Combo { get; private set; }
        public int HeartsMended { get; private set; }
        public int LastMendPoints { get; private set; }
        public int Best { get; private set; }

        private Coroutine _slowmoRoutine;

        private void Awake()
        {
            Instance = this;
            Best = SaveSystem.BestScore;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Resets a fresh run and raises the first wave.</summary>
        public void BeginGame()
        {
            Score = 0;
            Combo = 0;
            HeartsMended = 0;
            LastMendPoints = 0;
            Lives = GameConstants.StartingLives;
            Wave = 1;
            State = GameState.Playing;
            Time.timeScale = 1f;

            GameEvents.RaiseScoreChanged(Score, Combo);
            GameEvents.RaiseLivesChanged(Lives);
            GameEvents.RaiseWaveStarted(Wave);
        }

        /// <summary>Called when an arrow mends a broken heart. Returns points earned.</summary>
        public int RegisterMend()
        {
            Combo++;
            HeartsMended++;
            LastMendPoints = ScoreFormulas.PointsForMend(GameConstants.BaseMendPoints, Combo);
            Score += LastMendPoints;
            GameEvents.RaiseScoreChanged(Score, Combo);
            return LastMendPoints;
        }

        /// <summary>Called when a heart is missed (falls past the screen) — breaks the combo.</summary>
        public void RegisterMiss()
        {
            if (Combo == 0) return;
            Combo = 0;
            GameEvents.RaiseScoreChanged(Score, Combo);
        }

        public void LoseLife()
        {
            if (State != GameState.Playing) return;
            Combo = 0;
            Lives--;
            GameEvents.RaiseScoreChanged(Score, Combo);
            GameEvents.RaiseLivesChanged(Lives);
            if (Lives > 0) return;

            State = GameState.Over;
            Time.timeScale = 1f;
            SaveSystem.SubmitScore(Score);
            GameEvents.RaiseGameLost();
        }

        /// <summary>Heals one life if possible. Returns true when a life was actually added.</summary>
        public bool AddLife()
        {
            if (Lives >= GameConstants.MaxLives) return false;
            Lives++;
            GameEvents.RaiseLivesChanged(Lives);
            return true;
        }

        public void AddPoints(int points)
        {
            Score += points;
            GameEvents.RaiseScoreChanged(Score, Combo);
        }

        public void StartNextWave()
        {
            Wave++;
            GameEvents.RaiseWaveStarted(Wave);
        }

        /// <summary>Called by the spawner when the final wave is cleared.</summary>
        public void CompleteGame()
        {
            State = GameState.Won;
            SaveSystem.SubmitScore(Score);
            GameEvents.RaiseGameWon();
        }

        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            if (!paused && State != GameState.Paused) return;

            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            GameEvents.RaisePausedChanged(paused);
        }

        /// <summary>Slow-motion power-up: time dips for a few real-world seconds.</summary>
        public void BeginSlowmo()
        {
            if (_slowmoRoutine != null) StopCoroutine(_slowmoRoutine);
            _slowmoRoutine = StartCoroutine(SlowmoRoutine());
        }

        private IEnumerator SlowmoRoutine()
        {
            GameEvents.RaisePowerUpPicked("slowmo");
            Time.timeScale = 0.45f;
            float elapsed = 0f;
            while (elapsed < GameConstants.SlowmoSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            _slowmoRoutine = null;
        }
    }
}
