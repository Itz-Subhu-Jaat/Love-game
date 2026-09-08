using UnityEngine;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// A love arrow launched upward. Mends the first broken heart it touches,
    /// then recycles itself into the pool.
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        private float _speed;
        private float _topY;

        /// <summary>Launch configuration. Assumes the object was just rented.</summary>
        public void Launch(float speed)
        {
            _speed = speed;
            if (Camera.main != null)
            {
                _topY = Camera.main.orthographicSize + 2f;
            }
            else
            {
                _topY = 10f;
            }
        }

        private void Update()
        {
            var mgr = GameManager.Instance;
            if (mgr == null || mgr.State != GameManager.GameState.Playing)
            {
                return;   // frozen while paused or on the end screen
            }

            transform.position += Vector3.up * (_speed * Time.deltaTime);

            var spawner = HeartSpawner.Current;
            if (spawner != null)
            {
                var hearts = spawner.ActiveHearts;
                for (int i = 0; i < hearts.Count; i++)
                {
                    var heart = hearts[i];
                    if (heart == null || heart.IsResolved) continue;
                    if (Vector2.Distance(transform.position, heart.transform.position) < 1.05f)
                    {
                        heart.Mend();
                        GameObjectPool.Return("arrow", gameObject);
                        return;
                    }
                }
            }

            if (transform.position.y > _topY)
            {
                GameObjectPool.Return("arrow", gameObject);
            }
        }
    }
}
