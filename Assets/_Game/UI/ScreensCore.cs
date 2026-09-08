using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>Main menu: continue/new journey, settings, downloads, quit.</summary>
    public sealed class MainMenuScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            var bg = new GameObject("bg").AddComponent<Image>();
            var bgRt = bg.rectTransform;
            bgRt.SetParent(root, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var gradient = bg.gameObject.AddComponent<UiGradient>();
            gradient.top = new Color(0.16f, 0.05f, 0.24f);
            gradient.bottom = new Color(0.05f, 0.02f, 0.09f);

            var title = UiFactory.Label(root, "LOVE GAME", 110, font, new Color(1f, 0.55f, 0.68f), new Vector2(0f, 280f));
            title.sizeDelta = new Vector2(1200f, 160f);
            var subtitle = UiFactory.Label(root, "a private open world for two", 34, font, new Color(0.9f, 0.85f, 0.95f, 0.8f), new Vector2(0f, 180f));
            subtitle.sizeDelta = new Vector2(900f, 60f);

            bool hasSave = SaveSystem.HasSave;
            UiFactory.TextButton(root, hasSave ? "CONTINUE" : "BEGIN JOURNEY", 40, font, new Vector2(0f, 40f), new Vector2(560f, 110f), () => StartGame(), new Color(0.95f, 0.35f, 0.5f, 0.95f));
            if (hasSave)
                UiFactory.TextButton(root, "NEW JOURNEY", 34, font, new Vector2(0f, -100f), new Vector2(560f, 100f), () =>
                {
                    SaveSystem.Delete();
                    StartGame();
                }, new Color(0.5f, 0.4f, 0.6f, 0.9f));
            UiFactory.TextButton(root, "DOWNLOADS", 34, font, new Vector2(0f, -220f), new Vector2(560f, 100f), () => Service.Push(new DownloadScreen()), new Color(0.35f, 0.5f, 0.8f, 0.9f));
            UiFactory.TextButton(root, "SETTINGS", 34, font, new Vector2(0f, -340f), new Vector2(560f, 100f), () => Service.Push(new SettingsScreen()), new Color(0.35f, 0.5f, 0.8f, 0.9f));

            var footer = UiFactory.Label(root, "v0.3.0  -  placeholder art build", 22, font, new Color(1f, 1f, 1f, 0.35f), new Vector2(0f, -490f));
            footer.sizeDelta = new Vector2(800f, 40f);
        }

        void StartGame()
        {
            if (SaveSystem.HasSave) SaveSystem.Load();
            Services.Host.Run(LoadWorldRoutine());
        }

        System.Collections.IEnumerator LoadWorldRoutine()
        {
            Service.SetFader(1f);
            yield return new WaitForSecondsRealtime(0.3f);
            var op = SceneManager.LoadSceneAsync(GameBootstrap.WorldScene);
            while (op != null && !op.isDone) yield return null;
            Service.PopAll();
            Service.SetFader(0f);
        }
    }

    /// <summary>Vertical gradient background helper (no texture needed).</summary>
    public sealed class UiGradient : MonoBehaviour
    {
        public Color top = Color.black;
        public Color bottom = Color.white;

        void Start()
        {
            var img = GetComponent<Image>();
            if (img == null) return;
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { bottom, bottom, top, top });
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            img.sprite = sprite;
            img.color = Color.white;
        }
    }

    /// <summary>Pause menu: resume, save, map, settings, exit to menu.</summary>
    public sealed class PauseScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            var dim = new GameObject("dim").AddComponent<Image>();
            var dimRt = dim.rectTransform;
            dimRt.SetParent(root, false);
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.color = new Color(0f, 0f, 0f, 0.55f);

            var panel = UiFactory.Panel(root, "panel", new Color(0.08f, 0.07f, 0.12f, 0.95f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(640f, 860f);

            UiFactory.Label(panel, "PAUSED", 56, font, new Color(1f, 0.6f, 0.7f), new Vector2(0f, 340f)).sizeDelta = new Vector2(500f, 80f);
            UiFactory.TextButton(panel, "RESUME", 36, font, new Vector2(0f, 220f), new Vector2(480f, 96f), () => Service.Pop());
            UiFactory.TextButton(panel, "WORLD MAP", 36, font, new Vector2(0f, 105f), new Vector2(480f, 96f), () => Service.Push(new WorldMapScreen()));
            UiFactory.TextButton(panel, "INVENTORY", 36, font, new Vector2(0f, -10f), new Vector2(480f, 96f), () => Service.Push(new InventoryScreen()));
            UiFactory.TextButton(panel, "MEMORIES", 36, font, new Vector2(0f, -125f), new Vector2(480f, 96f), () => Service.Push(new JournalScreen()));
            UiFactory.TextButton(panel, "SETTINGS", 36, font, new Vector2(0f, -240f), new Vector2(480f, 96f), () => Service.Push(new SettingsScreen()));
            UiFactory.TextButton(panel, "SAVE & EXIT", 36, font, new Vector2(0f, -355f), new Vector2(480f, 96f), () =>
            {
                SaveSystem.Save();
                Services.Host.Run(ExitRoutine());
            }, new Color(0.6f, 0.3f, 0.4f, 0.9f));
        }

        System.Collections.IEnumerator ExitRoutine()
        {
            Time.timeScale = 0f;
            Service.SetFader(1f);
            yield return new WaitForSecondsRealtime(0.4f);
            var op = SceneManager.LoadSceneAsync(GameBootstrap.MainMenuScene);
            while (op != null && !op.isDone) yield return null;
            Time.timeScale = 1f;
            Service.PopAll();
            Service.SetFader(0f);
        }

        public override void OnShown() => Time.timeScale = 0f;
        public override void OnHidden() => Time.timeScale = 1f;
    }

    /// <summary>Loading screen with tips (shown during world scene loads).</summary>
    public sealed class LoadingScreen : Screen
    {
        protected override void OnBuild(RectTransform root)
        {
            var font = Service.DefaultFont;
            var bg = new GameObject("bg").AddComponent<Image>();
            var bgRt = bg.rectTransform;
            bgRt.SetParent(root, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bg.color = new Color(0.04f, 0.03f, 0.07f);

            var title = UiFactory.Label(root, "Loading your world...", 48, font, new Color(1f, 0.7f, 0.8f), new Vector2(0f, 80f));
            title.sizeDelta = new Vector2(1000f, 80f);
            var tip = UiFactory.Label(root, RandomTip(), 28, font, new Color(0.85f, 0.85f, 0.95f), new Vector2(0f, -60f));
            tip.sizeDelta = new Vector2(1300f, 120f);
        }

        static string RandomTip()
        {
            var text = Resources.Load<TextAsset>("Data/tips");
            if (text == null) return "Tip: hold RUN to sprint.";
            try
            {
                var file = JsonUtility.FromJson<TipsFile>(text.text);
                if (file?.tips != null && file.tips.Length > 0)
                    return "Tip: " + file.tips[Random.Range(0, file.tips.Length)];
            }
            catch { /* tips are cosmetic; ignore parse failures */ }
            return "Tip: hold RUN to sprint.";
        }
    }

    [System.Serializable]
    public class TipsFile { public string[] tips; }
}
