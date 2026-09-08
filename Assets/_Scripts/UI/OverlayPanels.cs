using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;

namespace LoveGame.UI
{
    /// <summary>
    /// Pause overlay: veil + panel with Resume / Restart / Main Menu / Sound.
    /// Shown and hidden via <see cref="GameEvents.PausedChanged"/>.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private GameObject _root;
        private Text _soundLabel;

        /// <summary>Builds the hidden pause overlay. Call once.</summary>
        public void Build(Canvas canvas)
        {
            var root = new GameObject("PauseOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)root.transform;
            UiFactory.Stretch(rt);

            var veil = UiFactory.CreateImage(root.transform, "Veil",
                new Color(0.05f, 0.02f, 0.1f, 0.72f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.CreateImage(root.transform, "Panel",
                GameConstants.PanelDark,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, -190f), new Vector2(240f, 190f));

            UiFactory.CreateText(panel.transform, "Title", "PAUSED", 44,
                GameConstants.Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-200f, -64f), new Vector2(200f, -12f), FontStyle.Bold);

            UiFactory.CreateButton(panel.transform, "ResumeButton", "RESUME", 28,
                GameConstants.Pink, Color.white, OnResume,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-160f, 30f), new Vector2(160f, 90f));

            UiFactory.CreateButton(panel.transform, "RestartButton", "RESTART", 24,
                new Color(0.36f, 0.16f, 0.4f), GameConstants.Ink, OnRestart,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-160f, -46f), new Vector2(160f, 4f));

            UiFactory.CreateButton(panel.transform, "MenuButton", "MAIN MENU", 24,
                new Color(0.36f, 0.16f, 0.4f), GameConstants.Ink, OnMainMenu,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-160f, -122f), new Vector2(160f, -72f));

            _soundLabel = UiFactory.CreateButton(panel.transform, "SoundButton",
                SoundButtonText(), 20,
                new Color(0.3f, 0.13f, 0.35f), GameConstants.DimInk, OnToggleSound,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-130f, -56f), new Vector2(130f, -14f))
                .GetComponentInChildren<Text>();

            _root = root;
            _root.SetActive(false);

            GameEvents.PausedChanged += OnPausedChanged;
        }

        private void OnDestroy()
        {
            GameEvents.PausedChanged -= OnPausedChanged;
        }

        private void OnPausedChanged(bool paused)
        {
            if (_root != null) _root.SetActive(paused);
        }

        private void OnResume()
        {
            MusicPlayer.PlaySfx("click");
            GameManager.Instance.SetPaused(false);
        }

        private void OnRestart()
        {
            MusicPlayer.PlaySfx("click");
            Time.timeScale = 1f;
            SceneFlow.ReloadGame();
        }

        private void OnMainMenu()
        {
            MusicPlayer.PlaySfx("click");
            Time.timeScale = 1f;
            SceneFlow.LoadMainMenu();
        }

        private void OnToggleSound()
        {
            MusicPlayer.SetMuted(!SaveSystem.Muted);
            if (_soundLabel != null) _soundLabel.text = SoundButtonText();
            MusicPlayer.PlaySfx("click");
        }

        private static string SoundButtonText()
        {
            return "SOUND: " + (SaveSystem.Muted ? "OFF" : "ON");
        }
    }

    /// <summary>
    /// End-of-run overlay shown on win and on game over, with score, best
    /// score badge, stats and the play again / menu buttons.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        private GameObject _root;
        private Text _title;
        private Text _scoreLine;
        private Text _bestLine;
        private Text _statsLine;

        /// <summary>Builds the hidden end overlay. Call once.</summary>
        public void Build(Canvas canvas)
        {
            var root = new GameObject("EndOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)root.transform;
            UiFactory.Stretch(rt);

            UiFactory.CreateImage(root.transform, "Veil",
                new Color(0.05f, 0.02f, 0.1f, 0.82f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.CreateImage(root.transform, "Panel",
                GameConstants.PanelDark,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-290f, -220f), new Vector2(290f, 220f));

            _title = UiFactory.CreateText(panel.transform, "Title", "", 48,
                GameConstants.Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-260f, -76f), new Vector2(260f, -14f), FontStyle.Bold);

            _scoreLine = UiFactory.CreateText(panel.transform, "ScoreLine", "", 34,
                GameConstants.Gold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, 96f), new Vector2(240f, 140f), FontStyle.Bold);

            _bestLine = UiFactory.CreateText(panel.transform, "BestLine", "", 24,
                GameConstants.SoftPink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, 60f), new Vector2(240f, 96f), FontStyle.Bold);

            _statsLine = UiFactory.CreateText(panel.transform, "StatsLine", "", 20,
                GameConstants.DimInk, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, 18f), new Vector2(240f, 58f));

            UiFactory.CreateButton(panel.transform, "AgainButton", "PLAY AGAIN", 28,
                GameConstants.Pink, Color.white, OnPlayAgain,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-170f, -78f), new Vector2(170f, -18f));

            UiFactory.CreateButton(panel.transform, "MenuButton", "MAIN MENU", 24,
                new Color(0.36f, 0.16f, 0.4f), GameConstants.Ink, OnMainMenu,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-170f, -152f), new Vector2(170f, -98f));

            _root = root;
            _root.SetActive(false);

            GameEvents.GameLost += OnGameLost;
            GameEvents.GameWon += OnGameWon;
        }

        private void OnDestroy()
        {
            GameEvents.GameLost -= OnGameLost;
            GameEvents.GameWon -= OnGameWon;
        }

        private void OnGameLost()
        {
            Show(false);
        }

        private void OnGameWon()
        {
            Show(true);
        }

        private void Show(bool won)
        {
            var mgr = GameManager.Instance;
            if (mgr == null || _root == null) return;

            _title.text = won ? "LOVE PREVAILS!" : "HEARTS FALTERED";
            _title.color = won ? GameConstants.SoftPink : GameConstants.Danger;

            _scoreLine.text = "SCORE  " + mgr.Score;
            bool newBest = SaveSystem.BestScore > 0 && mgr.Score >= SaveSystem.BestScore && mgr.Score > 0;
            _bestLine.text = newBest
                ? "NEW BEST!  " + SaveSystem.BestScore
                : "BEST  " + SaveSystem.BestScore;
            _statsLine.text = "Wave " + mgr.Wave + " of " + GameConstants.TotalWaves +
                              "   |   " + mgr.HeartsMended + " hearts mended";

            _root.SetActive(true);
            MusicPlayer.PlaySfx(won ? "win" : "over");
        }

        private void OnPlayAgain()
        {
            MusicPlayer.PlaySfx("click");
            SceneFlow.ReloadGame();
        }

        private void OnMainMenu()
        {
            MusicPlayer.PlaySfx("click");
            SceneFlow.LoadMainMenu();
        }
    }
}
