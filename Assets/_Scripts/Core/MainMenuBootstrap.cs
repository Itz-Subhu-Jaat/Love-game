using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Gameplay;
using LoveGame.UI;

namespace LoveGame.Core
{
    /// <summary>
    /// Builds the whole main menu: animated background, drifting hearts,
    /// logo, Play / How-to-play buttons, best score and sound toggle.
    /// </summary>
    public class MainMenuBootstrap : BootstrapBase
    {
        private RectTransform _logo;
        private RectTransform _playButton;
        private GameObject _howToPanel;
        private Text _soundLabel;
        private float _t;

        protected override void Awake()
        {
            base.Awake();
            MusicPlayer.Ensure();
            Build();
        }

        private void OnDestroy()
        {
            GameEvents.ClearAll();
        }

        private void Build()
        {
            CoverBackground("Sprites/background_menu");

            // logo
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(ScaledCanvas.transform, false);
            _logo = (RectTransform)logoGo.transform;
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 1f);
            _logo.pivot = new Vector2(0.5f, 0.5f);
            _logo.sizeDelta = new Vector2(680f, 190f);
            _logo.anchoredPosition = new Vector2(0f, -160f);

            var logoImage = logoGo.GetComponent<Image>();
            logoImage.sprite = Resources.Load<Sprite>("Sprites/logo_title");
            logoImage.raycastTarget = false;
            logoImage.preserveAspect = true;

            // tagline
            UiFactory.CreateText(ScaledCanvas.transform, "Tagline",
                GameConstants.Tagline, 26, GameConstants.SoftPink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-380f, -262f), new Vector2(380f, -292f), FontStyle.Bold);

            // play button
            var play = UiFactory.CreateButton(ScaledCanvas.transform, "PlayButton",
                "PLAY", 40, GameConstants.Pink, Color.white, OnPlay,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, -52f), new Vector2(150f, 32f));
            _playButton = (RectTransform)play.transform;

            // how to play
            UiFactory.CreateButton(ScaledCanvas.transform, "HowToButton",
                "HOW TO PLAY", 26, new Color(0.36f, 0.16f, 0.4f, 0.92f), GameConstants.Ink,
                OnHowTo,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, -126f), new Vector2(150f, -62f));

            // best score
            UiFactory.CreateText(ScaledCanvas.transform, "BestScore",
                "BEST SCORE: " + SaveSystem.BestScore, 24, GameConstants.Gold,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-200f, 96f), new Vector2(200f, 132f), FontStyle.Bold);

            // footer
            UiFactory.CreateText(ScaledCanvas.transform, "Footer",
                "Made with love  -  Love-game  -  Unity 6 + GameCI", 18,
                GameConstants.DimInk, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-300f, 40f), new Vector2(300f, 68f));

            // sound toggle (top-right)
            var soundBtn = UiFactory.CreateButton(ScaledCanvas.transform, "SoundButton",
                SoundButtonText(), 20, new Color(0.3f, 0.13f, 0.35f, 0.9f), GameConstants.DimInk,
                OnToggleSound,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-190f, -60f), new Vector2(-20f, -18f));
            _soundLabel = soundBtn.GetComponentInChildren<Text>();

            StartCoroutine(HeartAmbience());
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_logo != null)
            {
                _logo.anchoredPosition = new Vector2(0f, -160f + Mathf.Sin(_t * 1.2f) * 10f);
            }
            if (_playButton != null)
            {
                float s = 1f + Mathf.Sin(_t * 2.4f) * 0.02f;
                _playButton.localScale = new Vector3(s, s, 1f);
            }
        }

        // ------------------------------------------------------------------ actions

        private void OnPlay()
        {
            MusicPlayer.PlaySfx("click");
            SceneFlow.LoadGame();
        }

        private void OnHowTo()
        {
            MusicPlayer.PlaySfx("click");
            if (_howToPanel != null)
            {
                _howToPanel.SetActive(true);
                return;
            }
            _howToPanel = BuildHowToPanel();
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

        // ------------------------------------------------------------------ how-to

        private GameObject BuildHowToPanel()
        {
            var root = new GameObject("HowToOverlay", typeof(RectTransform));
            root.transform.SetParent(ScaledCanvas.transform, false);
            UiFactory.Stretch((RectTransform)root.transform);

            UiFactory.CreateImage(root.transform, "Veil",
                new Color(0.05f, 0.02f, 0.1f, 0.8f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.CreateImage(root.transform, "Panel",
                GameConstants.PanelDark,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-330f, -220f), new Vector2(330f, 220f));

            UiFactory.CreateText(panel.transform, "Title", "HOW TO PLAY", 38,
                GameConstants.SoftPink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-300f, -60f), new Vector2(300f, -12f), FontStyle.Bold);

            string body =
                "You are Cupid. Broken hearts are falling -\n" +
                "shoot LOVE ARROWS to mend them before they reach you.\n\n" +
                "MOVE:  A / D  or  arrow keys  (or drag with mouse / touch)\n" +
                "FIRE:  SPACE  (or tap)\n\n" +
                "Mended hearts fill the LOVE METER.\n" +
                "Consecutive mends build a COMBO up to x4.\n" +
                "Roses heal +1 life, ice crystals slow time.\n\n" +
                "Survive " + GameConstants.TotalWaves + " waves to win the night.";

            UiFactory.CreateText(panel.transform, "Body", body, 20,
                GameConstants.Ink, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 0.5f),
                new Vector2(24f, -300f), new Vector2(-24f, -24f));

            UiFactory.CreateButton(panel.transform, "OkButton", "GOT IT", 26,
                GameConstants.Pink, Color.white, OnCloseHowTo,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-120f, -58f), new Vector2(120f, -12f));

            return root;
        }

        private void OnCloseHowTo()
        {
            MusicPlayer.PlaySfx("click");
            if (_howToPanel != null) _howToPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ ambience

        private IEnumerator HeartAmbience()
        {
            var wait = new WaitForSeconds(0.9f);
            while (true)
            {
                var go = MakeSprite("Sprites/heart_mended", "AmbienceHeart", -5);
                if (go.GetComponent<SpriteRenderer>().sprite != null)
                {
                    float s = Random.Range(0.22f, 0.55f);
                    go.transform.localScale = new Vector3(s, s, 1f);
                    float halfW = Cam.orthographicSize * Cam.aspect;
                    go.transform.position = new Vector3(
                        Random.Range(-halfW, halfW), -Cam.orthographicSize - 1f, 0f);

                    var sr = go.GetComponent<SpriteRenderer>();
                    var c = sr.color;
                    c.a = 0.45f;
                    sr.color = c;

                    var motion = go.AddComponent<AutoMotion>();
                    motion.Velocity = new Vector2(Random.Range(-0.2f, 0.2f),
                                                  Random.Range(0.6f, 1.2f));
                    motion.SpinDegPerSec = Random.Range(-30f, 30f);
                    motion.Life = Random.Range(10f, 16f);
                    motion.FadeFraction = 0.9f;
                }
                else
                {
                    Destroy(go);
                }
                yield return wait;
            }
        }
    }
}
