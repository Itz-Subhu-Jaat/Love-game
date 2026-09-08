using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoveGame.Core
{
    /// <summary>
    /// Persistent entry point (scene 00_Bootstrap). Initializes core services, shows a minimal splash,
    /// then routes to the main menu (or straight into the world in editor play-tests).
    /// Survives all scene loads as the single DontDestroyOnLoad root.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string BootstrapScene = "00_Bootstrap";
        public const string MainMenuScene = "01_MainMenu";
        public const string WorldScene = "02_World";

        public static GameBootstrap Instance { get; private set; }
        public static bool CoreReady { get; private set; }

        IEnumerator Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                yield break;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = 0.02f;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Random.InitState(System.Environment.TickCount);

            GameConfig.Load();
            GameLogRuntimeBridge.EnsureEventSystem();
            Services.InitializeAll();
            CoreReady = true;
            Log.Info("Bootstrap", $"Love Game bootstrap complete ({Application.platform}, quality={GameConfig.Quality})");

            // Splash: give one frame for a clean first render, then route.
            yield return null;

            string target = SceneManager.GetActiveScene().name == BootstrapScene ? MainMenuScene : SceneManager.GetActiveScene().name;
            var op = SceneManager.LoadSceneAsync(target);
            while (op != null && !op.isDone) yield return null;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && CoreReady) SaveSystem.Save();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && CoreReady) SaveSystem.Save();
        }
    }

    /// <summary>Small runtime helpers that do not warrant their own file.</summary>
    public static class GameLogRuntimeBridge
    {
        static bool _ensured;

        /// <summary>The uGUI input module is required by every screen; create once, survive scene loads.</summary>
        public static void EnsureEventSystem()
        {
            if (_ensured && UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("~EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(es);
            }
            _ensured = true;
        }
    }
}
