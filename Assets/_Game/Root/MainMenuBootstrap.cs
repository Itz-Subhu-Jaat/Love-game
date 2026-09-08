using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Game
{
    /// <summary>
    /// Scene 01_MainMenu boot: ensures core is initialized (direct editor play support),
    /// then shows the main menu screen.
    /// </summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        void Start()
        {
            // direct editor play of the menu scene still gets full core init
            if (!Services.AnyInitialized)
            {
                var bootGo = new GameObject("~Boot");
                bootGo.AddComponent<GameBootstrap>();
            }
            // wait a frame for bootstrap init, then show the menu
            StartCoroutine(ShowMenuWhenReady());
        }

        System.Collections.IEnumerator ShowMenuWhenReady()
        {
            while (!Services.AnyInitialized) yield return null;
            var ui = Services.Get<UI.UiService>();
            while (ui == null)
            {
                ui = Services.Get<UI.UiService>();
                yield return null;
            }
            ui.PopAll();
            ui.Push(new UI.MainMenuScreen());
            var camera = Camera.main;
            if (camera != null) camera.backgroundColor = new Color(0.09f, 0.04f, 0.14f);
            camera?.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
