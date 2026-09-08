using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// A lonely broken heart drifting down. If it reaches the player it hurts;
    /// if an arrow touches it, it gets mended and floats away happily.
    /// </summary>
    public class BrokenHeart : MonoBehaviour
    {
        private static Sprite _brokenSprite;
        private static Sprite _mendedSprite;

        private SpriteRenderer _sr;
        private HeartSpawner _spawner;
        private Canvas _popup;

        private float _fallSpeed;
        private float _baseX;
        private float _phase;
        private float _wobbleAmp;
        private float _wobbleFreq;
        private float _t;

        public bool IsResolved { get; private set; }

        /// <summary>Configures the heart for the current wave.</summary>
        public void Init(int wave, float x, HeartSpawner spawner, Canvas popup)
        {
            if (_brokenSprite == null) _brokenSprite = Resources.Load<Sprite>("Sprites/heart_broken");
            if (_mendedSprite == null) _mendedSprite = Resources.Load<Sprite>("Sprites/heart_mended");

            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = _brokenSprite;
            _spawner = spawner;
            _popup = popup;

            _fallSpeed = WaveFormulas.FallSpeedForWave(wave) * Random.Range(0.85f, 1.15f);
            _baseX = x;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _wobbleAmp = Random.Range(0.25f, 0.6f);
            _wobbleFreq = Random.Range(1.2f, 2.2f);
            _t = 0f;

            float scale = WaveFormulas.HeartScaleForWave(wave);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(x, spawner.SpawnY, 0f);
        }

        private void Update()
        {
            if (IsResolved) return;

            var mgr = GameManager.Instance;
            if (mgr == null || mgr.State != GameManager.GameState.Playing) return;

            float dt = Time.deltaTime;
            _t += dt;

            float y = transform.position.y - _fallSpeed * dt;
            float x = _baseX + Mathf.Sin(_t * _wobbleFreq + _phase) * _wobbleAmp;
            transform.position = new Vector3(x, y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(_t * _wobbleFreq + _phase) * 8f);

            // reached the player line?
            if (y < _spawner.PlayerLineY + 1.1f)
            {
                var player = _spawner.Player;
                if (player != null &&
                    Vector2.Distance(transform.position, player.transform.position) < 1.15f)
                {
                    player.TakeHit();
                }
            }

            if (y < _spawner.BottomY)
            {
                Miss();
            }
        }

        /// <summary>The heart fell past the screen — combo breaker.</summary>
        public void Miss()
        {
            if (IsResolved) return;
            IsResolved = true;

            GameManager.Instance.RegisterMiss();
            _spawner.NotifyResolved(this);
            Destroy(gameObject);
        }

        /// <summary>An arrow hit the heart: swap sprite, celebrate, float away.</summary>
        public void Mend()
        {
            if (IsResolved) return;
            IsResolved = true;

            int points = GameManager.Instance.RegisterMend();

            _sr.sprite = _mendedSprite;
            EffectBurst.Sparkles(transform.position, new Color(1f, 0.55f, 0.75f), 9, 3.2f);
            EffectBurst.HeartPop(transform.position + Vector3.up * 0.2f);
            FloatyText.Show(_popup, "+" + points, GameConstants.Gold,
                            transform.position, Mathf.Max(20, Screen.height / 38));
            MusicPlayer.PlaySfx("mend");

            var motion = gameObject.AddComponent<AutoMotion>();
            motion.Velocity = new Vector2(Random.Range(-0.4f, 0.4f), 3.2f);
            motion.SpinDegPerSec = 40f;
            motion.Life = 1.6f;
            motion.FadeFraction = 0.9f;

            _spawner.NotifyResolved(this);
            Destroy(this);   // remove the falling behaviour, keep the sprite
        }
    }
}
