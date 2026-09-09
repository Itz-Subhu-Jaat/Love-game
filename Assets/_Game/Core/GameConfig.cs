using System;
using System.IO;
using UnityEngine;

namespace LoveGame.Core
{
    public enum QualityLevel { Low = 0, Medium = 1, High = 2, Ultra = 3 }

    /// <summary>User-adjustable settings persisted to persistentDataPath/Settings.json (separate from game save).</summary>
    [Serializable]
    public class GameSettings
    {
        public int qualityLevel = 1;                // QualityLevel
        public int fpsTarget = 60;                  // 30 or 60
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float ambienceVolume = 0.9f;
        public float sfxVolume = 1f;
        public float lookSensitivity = 1.0f;
        public float joystickSize = 1.0f;           // 0.7 .. 1.4
        public float uiOpacity = 0.9f;              // 0.4 .. 1.0
        public bool invertLookY = false;
        public bool wifiOnlyDownloads = true;
        public bool showFps = false;
        public string playerName = "Player";
        public string partnerName = "Partner";
    }

    /// <summary>Static accessor + persistence for settings. Never throws; falls back to defaults.</summary>
    public static class GameConfig
    {
        public static GameSettings Settings { get; private set; } = new GameSettings();

        static string PathFor => Path.Combine(Application.persistentDataPath, "Settings.json");

        public static void Load()
        {
            try
            {
                if (!File.Exists(PathFor)) { Save(); return; }
                var loaded = JsonUtility.FromJson<GameSettings>(File.ReadAllText(PathFor));
                if (loaded != null) Settings = loaded;
            }
            catch (Exception e) { Log.Warn("Config", $"settings load failed, using defaults: {e.Message}"); }
        }

        public static void Save()
        {
            try
            {
                var tmp = PathFor + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Settings, true));
                if (File.Exists(PathFor)) File.Delete(PathFor);
                File.Move(tmp, PathFor);
            }
            catch (Exception e) { Log.Warn("Config", $"settings save failed: {e.Message}"); }
        }

        public static QualityLevel Quality
        {
            get => (QualityLevel)Mathf.Clamp(Settings.qualityLevel, 0, 3);
            set { Settings.qualityLevel = (int)value; Save(); GameEvents.Publish(new QualityChangedEvent { Level = (int)value }); }
        }

        public static int FpsTarget
        {
            get => Settings.fpsTarget >= 55 ? 60 : 30;
            set { Settings.fpsTarget = value; Save(); }
        }
    }
}
