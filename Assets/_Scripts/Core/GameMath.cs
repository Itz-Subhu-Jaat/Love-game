namespace LoveGame.Core
{
    /// <summary>
    /// Difficulty curve of the heart waves. Pure static math, no Unity state,
    /// so the rules can be reasoned about (and unit tested) in isolation.
    /// </summary>
    public static class WaveFormulas
    {
        /// <summary>Hearts (and pickups) that wave spawns. Wave is 1-based.</summary>
        public static int HeartsForWave(int wave)
        {
            return 6 + wave * 2;
        }

        /// <summary>Base fall speed of a heart during the given wave.</summary>
        public static float FallSpeedForWave(int wave)
        {
            float speed = 1.7f + 0.22f * (wave - 1);
            return speed > 4.6f ? 4.6f : speed;
        }

        /// <summary>Seconds between spawns inside one wave.</summary>
        public static float SpawnIntervalForWave(int wave)
        {
            float interval = 1.05f - 0.06f * (wave - 1);
            return interval < 0.32f ? 0.32f : interval;
        }

        /// <summary>Sprite scale of hearts (they get smaller = harder to hit).</summary>
        public static float HeartScaleForWave(int wave)
        {
            float scale = 1.0f - 0.02f * (wave - 1);
            return scale < 0.8f ? 0.8f : scale;
        }

        /// <summary>True when the given wave is the last one of the run.</summary>
        public static bool IsFinalWave(int wave, int totalWaves)
        {
            return wave >= totalWaves;
        }
    }

    /// <summary>
    /// Score rules. Pure static math like <see cref="WaveFormulas"/>.
    /// </summary>
    public static class ScoreFormulas
    {
        /// <summary>
        /// Combo multiplier: every 4 consecutive mends raise it by +1, capped at x4.
        /// combo = 1 -> x1, 5 -> x2, 9 -> x3, 13+ -> x4.
        /// </summary>
        public static int MultiplierForCombo(int combo)
        {
            int m = 1 + (combo - 1) / 4;
            return m > 4 ? 4 : m;
        }

        /// <summary>Points awarded for mending a heart at the given combo streak.</summary>
        public static int PointsForMend(int basePoints, int combo)
        {
            return basePoints * MultiplierForCombo(combo);
        }

        /// <summary>
        /// Points for a rose: full bonus when lives are already full,
        /// half bonus when the rose actually heals.
        /// </summary>
        public static int PointsForRose(int lives, int maxLives, int bonusPoints)
        {
            return lives >= maxLives ? bonusPoints : bonusPoints / 2;
        }

        /// <summary>HUD label for the current combo streak ("COMBO x2" or empty).</summary>
        public static string ComboLabel(int combo)
        {
            return combo >= 2 ? "COMBO x" + MultiplierForCombo(combo) : "";
        }
    }
}
