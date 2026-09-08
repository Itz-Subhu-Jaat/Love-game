using UnityEngine;
using LoveGame.Audio;
using LoveGame.Gameplay;
using LoveGame.UI;

namespace LoveGame.Core
{
    /// <summary>
    /// Builds the playable scene: background, player, spawner, HUD, pause
    /// menu, end screen — then kicks off a fresh run. This component is the
    /// only MonoBehaviour placed in the scene file itself.
    /// </summary>
    public class GameBootstrap : BootstrapBase
    {
        private HudController _hud;
        private PauseMenu _pause;
        private EndScreen _end;
        private PlayerController _player;
        private HeartSpawner _spawner;

        protected override void Awake()
        {
            base.Awake();
            MusicPlayer.Ensure();

            CoverBackground("Sprites/background_game");

            gameObject.AddComponent<GameManager>();
            _hud = gameObject.AddComponent<HudController>();
            _pause = gameObject.AddComponent<PauseMenu>();
            _end = gameObject.AddComponent<EndScreen>();
            _spawner = gameObject.AddComponent<HeartSpawner>();

            _hud.Build(ScaledCanvas);
            _pause.Build(ScaledCanvas);
            _end.Build(ScaledCanvas);
        }

        private void Start()
        {
            float halfH = Cam.orthographicSize;
            float halfW = halfH * Cam.aspect;

            // player
            var playerGo = MakeSprite("Sprites/player_cupid", "Player", 12);
            playerGo.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            playerGo.transform.position = new Vector3(0f, -halfH + 1.9f, 0f);
            _player = playerGo.AddComponent<PlayerController>();
            _player.Setup(PopupCanvas);
            _player.SetBounds(-halfW + 1.1f, halfW - 1.1f);

            // spawner
            _spawner.Setup(halfW, halfH, _player, PopupCanvas);

            // go!
            GameManager.Instance.BeginGame();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var mgr = GameManager.Instance;
                if (mgr == null) return;
                if (mgr.State == GameManager.GameState.Playing)
                {
                    MusicPlayer.PlaySfx("click");
                    mgr.SetPaused(true);
                }
                else if (mgr.State == GameManager.GameState.Paused)
                {
                    MusicPlayer.PlaySfx("click");
                    mgr.SetPaused(false);
                }
            }
        }

        private void OnDestroy()
        {
            GameEvents.ClearAll();
            GameObjectPool.ResetAll();
        }
    }
}
