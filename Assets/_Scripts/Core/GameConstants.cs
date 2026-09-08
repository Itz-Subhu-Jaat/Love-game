using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>
    /// Every gameplay tunable of Love Quest lives here.
    /// Balance the whole game from this single file.
    /// </summary>
    public static class GameConstants
    {
        // ------------------------------------------------------------------ identity
        public const string GameTitle = "LOVE QUEST";
        public const string Tagline = "Mend the broken hearts. Spread the love.";

        // ------------------------------------------------------------------ campaign
        public const int TotalWaves = 10;
        public const int StartingLives = 3;
        public const int MaxLives = 5;

        // ------------------------------------------------------------------ player
        public const float PlayerSpeed = 9.0f;
        public const float FireCooldown = 0.26f;
        public const float ArrowSpeed = 16.0f;
        public const float InvulnerabilitySeconds = 1.6f;

        // ------------------------------------------------------------------ scoring
        public const int BaseMendPoints = 25;
        public const int RoseBonusPoints = 100;
        public const int SlowmoSeconds = 4;

        // ------------------------------------------------------------------ pickups
        public const float RoseChance = 0.12f;
        public const float CrystalChance = 0.05f;

        // ------------------------------------------------------------------ palette
        public static readonly Color Pink = FromBytes(0xFF, 0x5C, 0x8A);
        public static readonly Color SoftPink = FromBytes(0xFF, 0x9E, 0xC3);
        public static readonly Color Gold = FromBytes(0xFF, 0xD1, 0x68);
        public static readonly Color DeepPurple = FromBytes(0x1A, 0x0F, 0x2E);
        public static readonly Color PanelDark = FromBytes(0x24, 0x14, 0x3C, 0xE6);
        public static readonly Color Ink = FromBytes(0xFF, 0xF4, 0xFA);
        public static readonly Color DimInk = FromBytes(0xC9, 0xB2, 0xCE);
        public static readonly Color Danger = FromBytes(0xFF, 0x5A, 0x5A);
        public static readonly Color IceBlue = FromBytes(0x8F, 0xE0, 0xFF);

        private static Color FromBytes(byte r, byte g, byte b, byte a = 0xFF)
        {
            return new Color32(r, g, b, a);
        }
    }
}
