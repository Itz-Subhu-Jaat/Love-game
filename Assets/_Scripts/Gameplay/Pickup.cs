using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Falling pickup: a rose heals one life (or scores when lives are full),
    /// an ice crystal triggers slow-motion.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Rose, Crystal }

        private SpriteRenderer _sr;
        private HeartSpawner _spawner;
        private Canvas _popup;
        private Kind _itemKind;
        private float _fallSpeed;

        /// <summary>Configures the pickup for the current wave.</summary>
        public void Init(Kind kind, int wave, HeartSpawner spawner, Canvas popup)
        {
            _itemKind = kind;
            _spawner = spawner;
            _popup = popup;
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = Resources.Load<Sprite>(
                kind == Kind.Rose ? "Sprites/rose" : "Sprites/crystal");
            _sr.sortingOrder = 10;

            _fallSpeed = WaveFormulas.FallSpeedForWave(wave) * 0.55f;
            transform.localScale = Vector3.one;
            transform.position = new Vector3(transform.position.x, spawner.SpawnY, 0f);
        }

        private void Update()
        {
            var mgr = GameManager.Instance;
            if (mgr == null || mgr.State != GameManager.GameState.Playing) return;

            float dt = Time.deltaTime;
            transform.position += Vector3.down * (_fallSpeed * dt);
            transform.Rotate(0f, 0f, 120f * dt, Space.Self);

            var player = _spawner.Player;
            if (player != null &&
                Vector2.Distance(transform.position, player.transform.position) < 1.05f)
            {
                Collect();
                return;
            }

            if (transform.position.y < _spawner.BottomY)
            {
                _spawner.NotifyResolved(null);
                Destroy(gameObject);
            }
        }

        private void Collect()
        {
            var mgr = GameManager.Instance;

            if (_itemKind == Kind.Rose)
            {
                if (mgr.AddLife())
                {
                    FloatyText.Show(_popup, "+1 LIFE", GameConstants.SoftPink,
                                    transform.position, Mathf.Max(18, Screen.height / 40));
                }
                else
                {
                    int bonus = ScoreFormulas.PointsForRose(
                        mgr.Lives, GameConstants.MaxLives, GameConstants.RoseBonusPoints);
                    mgr.AddPoints(bonus);
                    FloatyText.Show(_popup, "+" + bonus, GameConstants.Gold,
                                    transform.position, Mathf.Max(20, Screen.height / 38));
                }
                EffectBurst.Sparkles(transform.position, new Color(1f, 0.6f, 0.7f), 10, 2.8f);
                MusicPlayer.PlaySfx("heal");
            }
            else
            {
                mgr.BeginSlowmo();
                FloatyText.Show(_popup, "SLOW-MO!", GameConstants.IceBlue,
                                transform.position, Mathf.Max(20, Screen.height / 36));
                EffectBurst.Sparkles(transform.position, new Color(0.6f, 0.85f, 1f), 12, 3.4f);
                MusicPlayer.PlaySfx("power");
            }

            _spawner.NotifyResolved(null);
            Destroy(gameObject);
        }
    }
}
