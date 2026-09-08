using UnityEngine;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Generic "move, spin, fade, die" behaviour shared by sparkles, mended
    /// hearts flying away, menu ambience hearts and pickup effects.
    /// Works pooled (set <see cref="PoolKey"/>) or plain (destroyed at end).
    /// </summary>
    public class AutoMotion : MonoBehaviour
    {
        public Vector2 Velocity = Vector2.zero;
        public float SpinDegPerSec;
        public float Life = 1f;
        /// <summary>Fraction of life spent fading out (0.5 = last half fades).</summary>
        public float FadeFraction = 0.5f;
        /// <summary>Pool key — when set, the object is recycled instead of destroyed.</summary>
        public string PoolKey;

        private SpriteRenderer _sr;
        private TextMesh _legacyText;
        private float _maxLife;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _legacyText = GetComponent<TextMesh>();
        }

        private void OnEnable()
        {
            _maxLife = Life;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(Velocity * dt);
            if (SpinDegPerSec != 0f)
            {
                transform.Rotate(0f, 0f, SpinDegPerSec * dt, Space.Self);
            }

            Life -= dt;
            float fadeWindow = Mathf.Max(0.001f, _maxLife * FadeFraction);
            float alpha = Life <= 0f ? 0f : Mathf.Clamp01(Life / fadeWindow);

            if (_sr != null)
            {
                var c = _sr.color;
                c.a = alpha;
                _sr.color = c;
            }
            if (_legacyText != null)
            {
                var c = _legacyText.color;
                c.a = alpha;
                _legacyText.color = c;
            }

            if (Life <= 0f)
            {
                if (!string.IsNullOrEmpty(PoolKey))
                {
                    GameObjectPool.Return(PoolKey, gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
