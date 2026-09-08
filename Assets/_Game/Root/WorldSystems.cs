using System.Collections;
using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;

namespace LoveGame.Game
{
    /// <summary>
    /// Scene 02_World boot: wires the entire runtime - world streaming, day/night, weather,
    /// player + partner + camera, interaction + couple systems, vehicles, activities, home,
    /// audio, content, HUD. Restores save state and enters the world.
    /// Scene-referenced component (02_World.unity) - file name must match the class name.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class WorldSystems : MonoBehaviour
    {
        ThirdPersonController _player;
        ThirdPersonCamera _cameraRig;
        PlayerCharacter _playerVisual;
        PartnerCompanion _partner;
        UI.HudScreen _hud;
        World.WorldStreamer _streamer;
        World.DayNightCycle _dayNight;
        World.WeatherSystem _weather;
        Vehicles.VehicleService _vehicles;
        bool _photoMode;
        int _poseIndex;
        static readonly string[] PhotoPoses = { "photoPose", "celebrate", "wave", "dance" };

        IEnumerator Start()
        {
            // 1) core services (supports direct editor play of the world scene)
            if (!Services.AnyInitialized)
            {
                var bootGo = new GameObject("~Boot");
                bootGo.AddComponent<GameBootstrap>();
            }
            while (!Services.AnyInitialized) yield return null;
            if (SaveSystem.HasSave) SaveSystem.Load();

            // 2) register every world + gameplay service (initialization happens in one pass below)
            var catalog = Register(new World.RegionCatalogService());
            _streamer = Register(new World.WorldStreamer());
            _dayNight = Register(new World.DayNightCycle());
            _weather = Register(new World.WeatherSystem());
            Register(new World.WaterSystem());
            Register(new World.NpcManager());
            Register(new World.WildlifeManager());
            Register(new World.FastTravelService());
            var input = Register(new InputService());
            Register(new Inventory.InventoryService());
            Register(new Inventory.CollectibleService());
            Register(new Inventory.MemoryService());
            Register(new Inventory.GiftService());
            var interaction = Register(new Interaction.InteractionService());
            var couple = Register(new Interaction.CoupleInteractionManager());
            Register(new Interaction.EmoteService());
            _vehicles = Register(new Vehicles.VehicleService());
            Register(new Activities.ActivityService());
            Register(new Home.FurnitureService());
            Register(new Audio.AudioService());
            Register(new Content.ContentService());
            Register(new UI.UiService());

            // 3) one initialization pass (per-service idempotent)
            Services.InitializeAll();
            yield return null;

            // 4) player + camera + partner (bound to the now-active input source)
            BuildPlayerAndCamera(input);

            // 5) wire cross-references
            interaction.Bind(_player, input);
            couple.Bind(_playerVisual, _partner, _cameraRig, _player);
            Services.Get<Interaction.EmoteService>()?.Bind(_playerVisual, _partner);
            _vehicles.Bind(_player, _cameraRig, input);
            Services.Get<Home.FurnitureService>()?.Bind(_player);

            // 6) HUD + photo mode
            BuildHud(input);

            // 7) restore player position + enter world
            var save = SaveSystem.Current;
            var region = catalog.GetById(save.player.currentRegion) ?? catalog.Regions[0];
            var spawn = new Vector3(save.player.positionX, 0f, save.player.positionZ);
            if (spawn.sqrMagnitude < 0.5f) spawn = new Vector3(region.SpawnPoint.x, 0f, region.SpawnPoint.y);
            _streamer.TeleportTo(region.Id, spawn);
            yield return null;
            while (_streamer.IsStreaming) yield return null;
            _player.SnapToGround();
            _dayNight.Hour = save.world.worldTimeHour;
            if (System.Enum.TryParse(save.world.weather, out World.WeatherType savedWeather)) _weather.Force(savedWeather);
            _partner.Teleport(_player.transform.position - _player.transform.forward * 2f + _player.transform.right * 1.2f);
            _partnerController.SnapToGround();

            // 8) world POI routing: home entry
            GameEvents.Subscribe<PoiInteractedEvent>(OnPoiInteracted);
            GameEvents.Subscribe<FastTravelStartedEvent>(OnFastTravelStarted);

            Log.Info("World", $"world ready: region '{region.DisplayName}', quality {GameConfig.Quality}");
            GameEvents.Publish(new PlayerSpawnedEvent { Player = _player.transform });
        }

        static T Register<T>(T service) where T : class, IGameService
        {
            if (Services.TryGet<T>(out var existing)) return existing;
            Services.Register(service);
            return service;
        }

        void BuildPlayerAndCamera(InputService input)
        {
            var playerGo = new GameObject("Player");
            playerGo.layer = GameLayers.Player;
            var controller = playerGo.AddComponent<ThirdPersonController>();
            var visual = playerGo.AddComponent<PlayerCharacter>();
            visual.characterName = GameConfig.Settings.playerName;
            controller.Bind(input.Active, _streamer);
            _player = controller;
            _playerVisual = visual;

            var partnerGo = new GameObject("Partner");
            partnerGo.layer = GameLayers.Player;
            var partnerController = partnerGo.AddComponent<ThirdPersonController>();
            var partnerChar = partnerGo.AddComponent<PlayerCharacter>();
            partnerChar.isPartner = true;
            partnerChar.characterName = GameConfig.Settings.partnerName;
            partnerChar.outfitColor = new Color(0.95f, 0.42f, 0.55f);
            partnerChar.accentColor = new Color(0.6f, 0.3f, 0.85f);
            var companion = partnerGo.AddComponent<PartnerCompanion>();
            companion.FollowTarget = playerGo.transform;
            _partner = companion;
            _partnerController = partnerController;

            var cameraGo = new GameObject("MainCamera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 3000f;
            cameraGo.AddComponent<AudioListener>();
            var rig = cameraGo.AddComponent<ThirdPersonCamera>();
            rig.Bind(input.Active);
            rig.SetTarget(playerGo.transform, true);
            _cameraRig = rig;
        }

        ThirdPersonController _partnerController;

        void BuildHud(InputService input)
        {
            _hud = new UI.HudScreen();
            _hud.Build(Services.Get<UI.UiService>(), input.Touch);
            UI.WorldSystemsBridge.PartnerPosition = () => _partner != null ? _partner.transform.position : Vector3.zero;
            UI.WorldSystemsBridge.OpenMap = () => Services.Get<UI.UiService>()?.Push(new UI.WorldMapScreen());
            UI.WorldSystemsBridge.Pause = () => Services.Get<UI.UiService>()?.Push(new UI.PauseScreen());
            UI.WorldSystemsBridge.OpenCoupleMenu = () => Services.Get<UI.UiService>()?.Push(new UI.CoupleMenuScreen());
            UI.WorldSystemsBridge.EnterPhotoMode = _ => EnterPhotoMode();
            UI.WorldSystemsBridge.CapturePhoto = CapturePhoto;
            UI.WorldSystemsBridge.ExitPhotoMode = ExitPhotoMode;
            UI.WorldSystemsBridge.ZoomPhoto = d => _cameraRig?.PhotoZoom(d);
            UI.WorldSystemsBridge.CyclePose = CyclePose;
        }

        void OnPoiInteracted(PoiInteractedEvent evt)
        {
            if (evt.Kind == World.PoiKind.Home.ToString())
            {
                var furniture = Services.Get<Home.FurnitureService>();
                if (furniture != null)
                {
                    var forward = _player.transform.forward;
                    var ground = _streamer.SampleHeight(_player.transform.position.x + forward.x * 24f, _player.transform.position.z + forward.z * 24f);
                    var origin = new Vector3(_player.transform.position.x + forward.x * 24f, ground + 0.1f, _player.transform.position.z + forward.z * 24f);
                    furniture.EnterHome("beach_house", origin);
                    _player.Teleport(origin + new Vector3(0f, 0.2f, 3.5f));
                    StartCoroutine(PlacePartnerAfterTravel());
                }
            }
            else if (evt.Kind == World.PoiKind.Viewpoint.ToString())
            {
                Services.Get<Interaction.CoupleInteractionManager>()?.Start("watchscenery");
            }
            else if (evt.Kind == World.PoiKind.Stargaze.ToString())
            {
                Services.Get<Interaction.CoupleInteractionManager>()?.Start("stargaze");
            }
            else if (evt.Kind == World.PoiKind.Picnic.ToString())
            {
                Services.Get<Interaction.CoupleInteractionManager>()?.Start("picnic");
            }
        }

        void OnFastTravelStarted(FastTravelStartedEvent evt)
        {
            // reposition the partner next to the player after the streamer teleports
            StartCoroutine(PlacePartnerAfterTravel());
        }

        IEnumerator PlacePartnerAfterTravel()
        {
            while (_streamer != null && _streamer.IsStreaming) yield return null;
            yield return null;
            if (_partner != null && _player != null)
                _partner.Teleport(_player.transform.position - _player.transform.forward * 1.8f + _player.transform.right * 1.1f);
            _partnerController?.SnapToGround();
        }

        // ------------------------------------------------------- photo mode

        void EnterPhotoMode()
        {
            if (_photoMode || _cameraRig == null || _player == null) return;
            _photoMode = true;
            _player.AllowControl = false;
            _playerVisual?.PlayPose(PhotoPoses[_poseIndex], 99999f);
            _partner?.GetComponent<PlayerCharacter>()?.PlayPose("photoPose", 99999f);
            var origin = _player.transform.position + _player.transform.up * 1.8f - _player.transform.forward * 4f;
            _cameraRig.EnterPhotoMode(origin);
            if (_hud != null) _hud.Visible = false;
            Services.Get<UI.UiService>()?.Push(new UI.PhotoModeScreen());
        }

        void CapturePhoto()
        {
            StartCoroutine(CaptureRoutine());
        }

        IEnumerator CaptureRoutine()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                var bytes = texture.EncodeToPNG();
                Object.Destroy(texture);
                var dir = System.IO.Path.Combine(Application.persistentDataPath, "Photos");
                System.IO.Directory.CreateDirectory(dir);
                var file = System.IO.Path.Combine(dir, $"photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
                System.IO.File.WriteAllBytes(file, bytes);
                GameEvents.Publish(new PhotoCapturedEvent { FilePath = file });
                GameEvents.Publish(new NotificationEvent { Title = "Photo saved", Body = "Added to your memories.", Duration = 3f });
                var memories = Services.Get<Inventory.MemoryService>();
                memories?.RecordPhoto(file, Services.Get<World.RegionCatalogService>()?.ActiveRegion?.Id);
            }
            catch (System.Exception e)
            {
                Log.Error("Photo", $"capture failed: {e.Message}");
                GameEvents.Publish(new NotificationEvent { Title = "Photo failed", Body = "Could not save the photo.", Duration = 3f });
            }
        }

        void CyclePose()
        {
            _poseIndex = (_poseIndex + 1) % PhotoPoses.Length;
            _playerVisual?.PlayPose(PhotoPoses[_poseIndex], 99999f);
        }

        void ExitPhotoMode()
        {
            if (!_photoMode) return;
            _photoMode = false;
            _cameraRig?.ExitPhotoMode();
            _cameraRig?.SetTarget(_player.transform, false);
            _playerVisual?.ClearPose();
            _partner?.GetComponent<PlayerCharacter>()?.ClearPose();
            _player.AllowControl = true;
            if (_hud != null) _hud.Visible = true;
            var ui = Services.Get<UI.UiService>();
            if (ui != null && ui.Top is UI.PhotoModeScreen) ui.Pop();
        }

        // ------------------------------------------------------------ update

        void Update()
        {
            if (_player == null || _streamer == null) return;

            _streamer.PlayerPosition = _player.transform.position;
            _streamer.PlayerForward = _player.transform.forward;
            Services.Get<World.WeatherSystem>().PlayerPosition = _player.transform.position;
            SaveSystem.Current.player.currentRegion = _streamer.RegionIdAt(_player.transform.position) ?? SaveSystem.Current.player.currentRegion;
            SaveSystem.TickPlaytime(Time.deltaTime);

            if (_hud != null)
                _hud.Tick(Time.deltaTime, _streamer, _dayNight, _weather);

            // pause key + debug menu (development builds only)
            if (Input.GetKeyDown(KeyCode.Escape) && Services.Get<UI.UiService>() is { } uiService && !uiService.IsOverlayOpen)
            {
                uiService.Push(new UI.PauseScreen());
            }
            if (Input.GetKeyDown(KeyCode.F3) && Debug.isDebugBuild)
            {
                var ui = Services.Get<UI.UiService>();
                if (ui != null && (ui.Top == null || !(ui.Top is UI.DebugScreen))) ui.Push(new UI.DebugScreen());
            }

            // underwater camera + swim mode
            var water = Services.Get<World.WaterSystem>();
            if (water != null && _cameraRig != null && !_photoMode)
            {
                var mode = _vehicles != null && _vehicles.Occupied != null ? CameraMode.Vehicle :
                    water.IsUnderwater ? CameraMode.Underwater :
                    _player.IsSwimming ? CameraMode.Swim : CameraMode.Normal;
                if (_cameraRig.Mode != mode) _cameraRig.Mode = mode;
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) PersistWorld();
        }

        void OnDestroy()
        {
            PersistWorld();
        }

        void PersistWorld()
        {
            if (_player == null) return;
            var save = SaveSystem.Current;
            save.player.positionX = _player.transform.position.x;
            save.player.positionY = _player.transform.position.y;
            save.player.positionZ = _player.transform.position.z;
            save.player.rotationY = _player.transform.eulerAngles.y;
            _vehicles?.Persist();
            SaveSystem.Save();
        }
    }
}
