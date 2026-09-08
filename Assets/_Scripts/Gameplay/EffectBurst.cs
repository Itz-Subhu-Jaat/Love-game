using UnityEngine;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Particle-style bursts built from pooled sparkle sprites —
    /// no ParticleSystem needed, keeps the project dependency-light.
    /// </summary>
    public static class EffectBurst
    {
        /// <summary>Radiating sparkle explosion (used for mends, pickups, hits).</summary>
        public static void Sparkles(Vector3 position, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var go = GameObjectPool.Rent("sparkle", CreateSparkle);
                go.transform.position = position;
                go.transform.rotation = Quaternion.identity;

                float scale = Random.Range(0.28f, 0.55f);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var sr = go.GetComponent<SpriteRenderer>();
                var c = color;
                c.a = 1f;
                sr.color = c;

                var motion = go.GetComponent<AutoMotion>();
                var dir = Random.insideUnitCircle.normalized * (speed * Random.Range(0.5f, 1f));
                motion.Velocity = dir;
                motion.SpinDegPerSec = Random.Range(-260f, 260f);
                motion.Life = Random.Range(0.5f, 0.9f);
                motion.FadeFraction = 0.6f;

                go.SetActive(true);
            }
        }

        /// <summary>Little heart trail that floats up (used when a heart is mended).</summary>
        public static void HeartPop(Vector3 position)
        {
            var go = GameObjectPool.Rent("miniheart", CreateMiniHeart);
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var motion = go.GetComponent<AutoMotion>();
            motion.Velocity = new Vector2(Random.Range(-0.4f, 0.4f), 3.2f);
            motion.SpinDegPerSec = 40f;
            motion.Life = 1.6f;
            motion.FadeFraction = 0.85f;

            go.SetActive(true);
        }

        // ------------------------------------------------------------------ factories

        private static GameObject CreateSparkle()
        {
            var go = new GameObject("Sparkle", typeof(SpriteRenderer), typeof(AutoMotion));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/sparkle");
            sr.sortingOrder = 30;
            var motion = go.GetComponent<AutoMotion>();
            motion.PoolKey = "sparkle";
            return go;
        }

        private static GameObject CreateMiniHeart()
        {
            var go = new GameObject("MiniHeart", typeof(SpriteRenderer), typeof(AutoMotion));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/heart_mended");
            sr.sortingOrder = 8;
            var motion = go.GetComponent<AutoMotion>();
            motion.PoolKey = "miniheart";
            return go;
        }
    }
}
