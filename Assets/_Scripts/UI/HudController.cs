using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;
using LoveGame.Gameplay;

namespace LoveGame.UI
{
    /// <summary>
    /// In-game HUD: lives row, wave label, love meter, score, combo, wave
    /// banner, hurt flash, slow-mo tint and the pause button. Subscribes to
    /// <see cref="GameEvents"/> and never touches gameplay code directly.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        private Text _scoreLabel;
        private Text _comboLabel;
        private Text _waveLabel;
        private Text _bannerLabel;
        private Image[] _lifeIcons;
        private Image _meterFill;
        private Image _hurtFlash;
        private Image _slowmoTint;

        private Coroutine _bannerRoutine;
        private Coroutine _flashRoutine;
        private Coroutine _comboPopRoutine;
        private float _lastCombo;

        /// <summary>Builds the whole HUD under the given canvas. Call once.</summary>
        public void Build(Canvas canvas)
        {
            var uiHeart = Resources.Load<Sprite>("Sprites/ui_heart");
            var root = canvas.transform;

            // ---- lives row (top-left)
            _lifeIcons = new Image[GameConstants.MaxLives];
            for (int i = 0; i < GameConstants.MaxLives; i++)
            {
                var icon = UiFactory.CreateImage(root, "Life" + i,
                    GameConstants.Pink,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f + i * 52f, -72f), new Vector2(68f + i * 52f, -28f),
                    uiHeart);
                _lifeIcons[i] = icon;
            }

            // ---- wave label + love meter (top-center)
            _waveLabel = UiFactory.CreateText(root, "WaveLabel", "WAVE 1", 30,
                GameConstants.Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-90f, -58f), new Vector2(90f, -16f), FontStyle.Bold);

            UiFactory.CreateImage(root, "MeterBg", new Color(0.08f, 0.04f, 0.14f, 0.8f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-210f, -84f), new Vector2(210f, -66f));
            _meterFill = UiFactory.CreateImage(root, "MeterFill", GameConstants.Pink,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-206f, -82f), new Vector2(206f, -68f));
            UiFactory.CreateText(root, "MeterLabel", "LOVE", 16, GameConstants.SoftPink,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-250f, -86f), new Vector2(-214f, -62f), FontStyle.Bold);

            // ---- pause button (top-right)
            UiFactory.CreateButton(root, "PauseButton", "II", 26,
                new Color(0.16f, 0.09f, 0.28f, 0.9f), GameConstants.Ink,
                OnPauseClicked,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-64f, -64f), new Vector2(-16f, -16f));

            // ---- score + combo (right, below pause)
            _scoreLabel = UiFactory.CreateText(root, "ScoreLabel", "SCORE 0", 30,
                GameConstants.Gold, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-230f, -110f), new Vector2(-16f, -70f), FontStyle.Bold);
            _comboLabel = UiFactory.CreateText(root, "ComboLabel", "", 24,
                GameConstants.SoftPink, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-230f, -142f), new Vector2(-16f, -110f), FontStyle.Bold);

            // ---- wave banner (center)
            _bannerLabel = UiFactory.CreateText(root, "WaveBanner", "", 54,
                GameConstants.Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-320f, 190f), new Vector2(320f, 266f), FontStyle.Bold);
            _bannerLabel.color = new Color(1f, 1f, 1f, 0f);

            // ---- hurt flash (fullscreen)
            _hurtFlash = UiFactory.CreateImage(root, "HurtFlash",
                new Color(1f, 0.3f, 0.4f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ---- slow-mo tint (fullscreen)
            _slowmoTint = UiFactory.CreateImage(root, "SlowmoTint",
                new Color(0.55f, 0.8f, 1f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ---- events
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.LoveMeterChanged += OnLoveMeterChanged;
            GameEvents.PowerUpPicked += OnPowerUpPicked;
        }

        private void OnDestroy()
        {
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.LoveMeterChanged -= OnLoveMeterChanged;
            GameEvents.PowerUpPicked -= OnPowerUpPicked;
        }

        private void OnPauseClicked()
        {
            var mgr = GameManager.Instance;
            if (mgr != null && mgr.State == GameManager.GameState.Playing)
            {
                MusicPlayer.PlaySfx("click");
                mgr.SetPaused(true);
            }
        }

        // ------------------------------------------------------------- handlers

        private void OnScoreChanged(int score, int combo)
        {
            if (_scoreLabel != null) _scoreLabel.text = "SCORE " + score;
            if (_comboLabel != null) _comboLabel.text = ScoreFormulas.ComboLabel(combo);

            if (combo > _lastCombo && combo >= 2 && _comboLabel != null)
            {
                if (_comboPopRoutine != null) StopCoroutine(_comboPopRoutine);
                _comboPopRoutine = StartCoroutine(PopScale(_comboLabel.transform, 1.35f, 0.18f));
            }
            _lastCombo = combo;
        }

        private void OnLivesChanged(int lives)
        {
            for (int i = 0; i < _lifeIcons.Length; i++)
            {
                _lifeIcons[i].color = i < lives ? GameConstants.Pink
                                                : new Color(0.35f, 0.35f, 0.45f, 0.65f);
            }

            if (lives < GameConstants.StartingLives && _hurtFlash != null)
            {
                if (_flashRoutine != null) StopCoroutine(_flashRoutine);
                _flashRoutine = StartCoroutine(Flash(_hurtFlash, 0.32f, 0.45f));
            }
        }

        private void OnWaveStarted(int wave)
        {
            if (_waveLabel != null) _waveLabel.text = "WAVE " + wave;

            string banner = WaveFormulas.IsFinalWave(wave, GameConstants.TotalWaves)
                ? "FINAL WAVE!"
                : "WAVE " + wave;
            if (_bannerLabel != null)
            {
                if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
                _bannerRoutine = StartCoroutine(ShowBanner(_bannerLabel, banner, 1.4f));
            }
        }

        private void OnLoveMeterChanged(float progress)
        {
            if (_meterFill != null)
            {
                float p = Mathf.Clamp01(progress);
                var rt = (RectTransform)_meterFill.transform;
                rt.offsetMin = new Vector2(-206f, -82f);
                rt.offsetMax = new Vector2(-206f + 412f * p, -68f);
            }
        }

        private void OnPowerUpPicked(string name)
        {
            if (name == "slowmo" && _slowmoTint != null)
            {
                StartCoroutine(SlowmoTint(_slowmoTint));
            }
        }

        // ------------------------------------------------------------- tweens

        private IEnumerator PopScale(Transform target, float peak, float duration)
        {
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;
                float s = 1f + (peak - 1f) * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                target.localScale = baseScale * s;
                yield return null;
            }
            target.localScale = baseScale;
        }

        private IEnumerator ShowBanner(Text banner, string message, float duration)
        {
            banner.text = message;
            float t = 0f;
            var c = banner.color;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                c.a = Mathf.Clamp01(Mathf.Sin(k * Mathf.PI) * 1.6f);
                banner.color = c;
                yield return null;
            }
            c.a = 0f;
            banner.color = c;
        }

        private IEnumerator Flash(Image img, float peakAlpha, float duration)
        {
            float t = 0f;
            var c = img.color;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                c.a = peakAlpha * (1f - k);
                img.color = c;
                yield return null;
            }
            c.a = 0f;
            img.color = c;
        }

        private IEnumerator SlowmoTint(Image img)
        {
            float t = 0f;
            var c = img.color;
            while (t < GameConstants.SlowmoSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / GameConstants.SlowmoSeconds);
                c.a = 0.16f * Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 2f);
                img.color = c;
                yield return null;
            }
            c.a = 0f;
            img.color = c;
        }
    }
}
