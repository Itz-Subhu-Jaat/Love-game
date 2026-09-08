using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LoveGame.Audio;
using LoveGame.Core;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Wave director. Spawns broken hearts (plus the occasional rose or ice
    /// crystal), tracks resolution, drives the wave loop and the overall
    /// love-meter progress. Arrows reach it through <see cref="Current"/>.
    /// </summary>
    public class HeartSpawner : MonoBehaviour
    {
        public static HeartSpawner Current { get; private set; }

        /// <summary>Hearts currently on screen — arrows scan this list.</summary>
        public readonly List<BrokenHeart> ActiveHearts = new List<BrokenHeart>();

        public PlayerController Player { get; private set; }
        public float PlayerLineY { get; private set; }
        public float BottomY { get; private set; }
        public float SpawnY { get; private set; }

        private Canvas _popup;
        private float _xMin, _xMax;
        private int _wave;
        private int _toSpawn;
        private int _toResolve;
        private int _resolvedThisWave;
        private int _waveTotal;
        private float _spawnTimer;
        private bool _transitioning;
        private bool _running;
        private bool _crystalUsedThisWave;

        /// <summary>Wires the spawner to the world. Called from GameBootstrap.Start.</summary>
        public void Setup(float halfWidth, float halfHeight, PlayerController player, Canvas popup)
        {
            Current = this;
            Player = player;
            _popup = popup;

            PlayerLineY = -halfHeight + 1.9f;
            BottomY = -halfHeight - 1.2f;
            SpawnY = halfHeight + 1.2f;
            _xMin = -halfWidth + 0.7f;
            _xMax = halfWidth - 0.7f;

            GameEvents.WaveStarted += OnWaveStarted;
        }

        private void OnDestroy()
        {
            GameEvents.WaveStarted -= OnWaveStarted;
            if (Current == this) Current = null;
        }

        private void OnWaveStarted(int wave)
        {
            _wave = wave;
            _waveTotal = WaveFormulas.HeartsForWave(wave);
            _toSpawn = _waveTotal;
            _toResolve = _waveTotal;
            _resolvedThisWave = 0;
            _spawnTimer = 0.6f;
            _transitioning = false;
            _crystalUsedThisWave = false;
            _running = true;

            RaiseLoveMeter();
        }

        private void Update()
        {
            var mgr = GameManager.Instance;
            if (mgr == null || !_running) return;
            if (mgr.State != GameManager.GameState.Playing) return;

            if (_toSpawn > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnOne();
                    _toSpawn--;
                    _spawnTimer = WaveFormulas.SpawnIntervalForWave(_wave) *
                                  Random.Range(0.75f, 1.25f);
                }
            }
            else if (_toResolve <= 0 && !_transitioning)
            {
                _transitioning = true;
                StartCoroutine(WaveTransition());
            }
        }

        private IEnumerator WaveTransition()
        {
            MusicPlayer.PlaySfx("wave", 0.5f);
            yield return new WaitForSeconds(1.4f);

            var mgr = GameManager.Instance;
            if (mgr == null) yield break;
            if (mgr.State != GameManager.GameState.Playing) yield break;

            if (WaveFormulas.IsFinalWave(_wave, GameConstants.TotalWaves))
            {
                _running = false;
                mgr.CompleteGame();
            }
            else
            {
                mgr.StartNextWave();   // -> OnWaveStarted -> next wave
            }
        }

        private void SpawnOne()
        {
            float x = Random.Range(_xMin, _xMax);
            float roll = Random.value;

            if (roll < GameConstants.RoseChance)
            {
                SpawnPickup(Pickup.Kind.Rose, x);
            }
            else if (!_crystalUsedThisWave &&
                     roll < GameConstants.RoseChance + GameConstants.CrystalChance)
            {
                _crystalUsedThisWave = true;   // at most one crystal per wave
                SpawnPickup(Pickup.Kind.Crystal, x);
            }
            else
            {
                SpawnHeart(x);
            }
        }

        private void SpawnHeart(float x)
        {
            var go = new GameObject("BrokenHeart", typeof(SpriteRenderer));
            var heart = go.AddComponent<BrokenHeart>();
            heart.Init(_wave, x, this, _popup);
            ActiveHearts.Add(heart);
        }

        private void SpawnPickup(Pickup.Kind kind, float x)
        {
            var go = new GameObject("Pickup", typeof(SpriteRenderer));
            var pickup = go.AddComponent<Pickup>();
            pickup.transform.position = new Vector3(x, SpawnY, 0f);
            pickup.Init(kind, _wave, this, _popup);
        }

        /// <summary>
        /// Called by hearts and pickups when they leave play (mended, missed,
        /// collected or despawned). Drives wave completion + the love meter.
        /// </summary>
        public void NotifyResolved(BrokenHeart heart)
        {
            if (heart != null)
            {
                ActiveHearts.Remove(heart);
            }

            _toResolve--;
            _resolvedThisWave++;
            RaiseLoveMeter();
        }

        private void RaiseLoveMeter()
        {
            float inWave = _waveTotal > 0 ? _resolvedThisWave / (float)_waveTotal : 0f;
            float progress = Mathf.Clamp01(
                ((_wave - 1) + inWave) / GameConstants.TotalWaves);
            GameEvents.RaiseLoveMeterChanged(progress);
        }
    }
}
