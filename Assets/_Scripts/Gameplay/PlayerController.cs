using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// The player's winged cupid heart: moves along the bottom line with
    /// keyboard (A/D, arrows), mouse drag or touch, and fires love arrows
    /// with Space / click / tap. Handles invulnerability flicker after a hit.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private Transform _self;
        private SpriteRenderer _sr;
        private Canvas _popup;
        private float _xMin, _xMax;
        private float _y;
        private float _fireTimer;
        private float _invuln;
        private float _bobT;

        /// <summary>Wires references. Call once right after adding the component.</summary>
        public void Setup(Canvas popupCanvas)
        {
            _self = transform;
            _sr = GetComponent<SpriteRenderer>();
            _popup = popupCanvas;
            _y = _self.position.y;
        }

        public void SetBounds(float xMin, float xMax)
        {
            _xMin = xMin;
            _xMax = xMax;
        }

        private void Update()
        {
            var mgr = GameManager.Instance;
            if (mgr == null || _sr == null) return;

            float dt = Time.deltaTime;
            _bobT += dt;
            _invuln = Mathf.Max(0f, _invuln - dt);

            if (mgr.State == GameManager.GameState.Playing)
            {
                float x = _self.position.x;

                // keyboard movement
                float h = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(h) > 0.01f)
                {
                    x += h * GameConstants.PlayerSpeed * dt;
                }
                else
                {
                    // mouse / touch drag steering
                    bool pointerHeld = false;
                    float pointerX = 0f;
                    if (Input.touchCount > 0)
                    {
                        Touch touch = Input.GetTouch(0);
                        pointerHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                        pointerX = touch.position.x;
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        pointerHeld = true;
                        pointerX = Input.mousePosition.x;
                    }
                    if (pointerHeld)
                    {
                        Vector3 world = Camera.main.ScreenToWorldPoint(
                            new Vector3(pointerX, 0f, -Camera.main.transform.position.z));
                        x = Mathf.Lerp(x, world.x, 0.25f);
                    }
                }

                x = Mathf.Clamp(x, _xMin, _xMax);
                _self.position = new Vector3(x, _y + Mathf.Sin(_bobT * 2.2f) * 0.12f, 0f);

                // firing: Space, or pointer held in the lower 65% of the screen
                _fireTimer -= dt;
                bool firing = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow);
                if (!firing)
                {
                    bool mouseFire = Input.GetMouseButton(0) &&
                                     Input.mousePosition.y < Screen.height * 0.65f;
                    bool touchFire = Input.touchCount > 0 &&
                                     Input.GetTouch(0).position.y < Screen.height * 0.65f;
                    firing = mouseFire || touchFire;
                }
                if (firing && _fireTimer <= 0f)
                {
                    Fire();
                    _fireTimer = GameConstants.FireCooldown;
                }
            }

            // invulnerability flicker
            if (_invuln > 0f)
            {
                var c = _sr.color;
                c.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(_invuln * 18f));
                _sr.color = c;
            }
            else if (_sr.color.a < 1f)
            {
                var c = _sr.color;
                c.a = 1f;
                _sr.color = c;
            }
        }

        private void Fire()
        {
            var go = GameObjectPool.Rent("arrow", CreateArrow);
            go.transform.position = _self.position + Vector3.up * 0.7f;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var arrow = go.GetComponent<ArrowProjectile>();
            arrow.Launch(GameConstants.ArrowSpeed);

            go.SetActive(true);
            MusicPlayer.PlaySfx("arrow", 0.5f);
        }

        private static GameObject CreateArrow()
        {
            var go = new GameObject("Arrow", typeof(SpriteRenderer), typeof(ArrowProjectile));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/arrow");
            sr.sortingOrder = 15;
            return go;
        }

        /// <summary>Called by a broken heart that reached the player line.</summary>
        public void TakeHit()
        {
            if (_invuln > 0f) return;
            _invuln = GameConstants.InvulnerabilitySeconds;

            EffectBurst.Sparkles(_self.position, new Color(0.55f, 0.55f, 0.65f), 8, 2.2f);
            FloatyText.Show(_popup, "-1 LIFE", GameConstants.Danger,
                            _self.position + Vector3.up * 1.1f, Mathf.Max(18, Screen.height / 40));

            MusicPlayer.PlaySfx("hurt");
            GameManager.Instance.LoseLife();
        }
    }
}
