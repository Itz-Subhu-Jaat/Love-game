using UnityEngine.SceneManagement;

namespace LoveGame.Core
{
    /// <summary>
    /// Scene name registry + safe loaders. The names must match the scene files
    /// under Assets/_Scenes exactly (they are also listed in EditorBuildSettings).
    /// </summary>
    public static class SceneFlow
    {
        public const string MainMenu = "00_MainMenu";
        public const string Game = "01_Game";

        public static void LoadMainMenu()
        {
            SceneManager.LoadScene(MainMenu);
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(Game);
        }

        public static void ReloadGame()
        {
            SceneManager.LoadScene(Game);
        }
    }
}
