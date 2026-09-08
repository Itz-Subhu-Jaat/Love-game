using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>
    /// In-world HUD: dual virtual joysticks (writes into TouchInputSource), action buttons,
    /// interaction prompt, clock + weather chip, radar minimap, couple quick-menu, fade overlay.
    /// Built once per world session by WorldSystems.
    /// </summary>
    public sealed class HudScreen
    {
        public RectTransform Root { get; private set; }
        UiService _ui;
        TouchInputSource _touch;
        VirtualJoystick _moveJoy, _lookJoy;
        Text _prompt, _clock, _activityLabel, _regionLabel;
        RectTransform _minimap;
        readonly System.Collections.Generic.List<MinimapMarker> _markers = new System.Collections.Generic.List<MinimapMarker>();
        Image _fade;
        float _markerTimer;

        public bool Visible
        {
            get => Root.gameObject.activeSelf;
            set => Root.gameObject.SetActive(value);
        }

        public void Build(UiService ui, TouchInputSource touch)
        {
            _ui = ui;
            _touch = touch;
            Root = new GameObject("HUD").AddComponent<RectTransform>();
            Root.SetParent(ui.RootCanvas.transform, false);
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.one;
            Root.offsetMin = Vector2.zero;
            Root.offsetMax = Vector2.zero;

            BuildJoysticks();
            BuildButtons();
            BuildStatus();
            BuildMinimap();
            BuildFade();

            GameEvents.Subscribe<InteractionPromptEvent>(OnPrompt);
            GameEvents.Subscribe<ActivityStartedEvent>(OnActivityStarted);
            GameEvents.Subscribe<ActivityCompletedEvent>(OnActivityCompleted);
        }

        void BuildJoysticks()
        {
            float scale = GameConfig.Settings.joystickSize;
            // left: movement
            _moveJoy = BuildJoystick(new Vector2(260f, 240f), scale, false, "move");
            // right: camera look
            _lookJoy = BuildJoystick(new Vector2(-260f, 240f), scale, true, "look");
        }

        VirtualJoystick BuildJoystick(Vector2 pos, float scale, bool isLook, string name)
        {
            var ringGo = new GameObject($"joy_{name}");
            var ring = ringGo.AddComponent<RectTransform>();
            ring.SetParent(Root, false);
            ring.anchorMin = new Vector2(isLook ? 1f : 0f, 0f);
            ring.anchorMax = new Vector2(isLook ? 1f : 0f, 0f);
            ring.pivot = new Vector2(isLook ? 1f : 0f, 0f);
            ring.anchoredPosition = pos;
            ring.sizeDelta = new Vector2(360f, 360f) * scale;
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite = UiFactory.JoyRingSprite;
            ringImg.color = new Color(1f, 1f, 1f, 0.16f * GameConfig.Settings.uiOpacity);

            var thumbGo = new GameObject("thumb");
            var thumb = thumbGo.AddComponent<RectTransform>();
            thumb.SetParent(ring, false);
            thumb.sizeDelta = new Vector2(150f, 150f) * scale;
            var thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.sprite = UiFactory.JoyThumbSprite;
            thumbImg.color = new Color(1f, 1f, 1f, 0.45f * GameConfig.Settings.uiOpacity);

            var joystick = ringGo.AddComponent<VirtualJoystick>();
            joystick.ring = ring;
            joystick.thumb = thumb;
            joystick.isLookJoystick = isLook;
            joystick.maxRadius = 120f * scale;
            joystick.Bind(_touch);
            return joystick;
        }

        void BuildButtons()
        {
            var font = _ui.DefaultFont;
            // jump (right side above look stick)
            UiFactory.RoundIconButton(Root, "JUMP", font, new Vector2(-160f, 620f), 170f, () => _touch.PressJump(),
                new Color(0.95f, 0.5f, 0.3f, 0.75f * GameConfig.Settings.uiOpacity));
            // sprint (hold)
            var sprint = UiFactory.RoundIconButton(Root, "RUN", font, new Vector2(-360f, 480f), 140f, () => { },
                new Color(0.35f, 0.6f, 0.95f, 0.7f * GameConfig.Settings.uiOpacity));
            var hold = sprint.gameObject.AddComponent<HoldButton>();
            hold.onPressed = () => _touch.SprintHeld = true;
            hold.onReleased = () => _touch.SprintHeld = false;

            // interact (contextual, appears via prompt events)
            UiFactory.RoundIconButton(Root, "USE", font, new Vector2(-160f, 460f), 150f, () => _touch.PressInteract(),
                new Color(0.4f, 0.95f, 0.55f, 0.75f * GameConfig.Settings.uiOpacity));
            // action (context-dependent: hook fish / pop target / exit vehicle)
            UiFactory.RoundIconButton(Root, "ACT", font, new Vector2(-350f, 660f), 130f, () => _touch.PressAction(),
                new Color(0.85f, 0.75f, 0.3f, 0.7f * GameConfig.Settings.uiOpacity));

            // top bar: map, pause, emote
            UiFactory.RoundIconButton(Root, "MAP", font, new Vector2(-760f, -60f), 110f, () => WorldSystemsBridge.OpenMap?.Invoke(),
                new Color(0.25f, 0.3f, 0.45f, 0.75f));
            UiFactory.RoundIconButton(Root, "II", font, new Vector2(-620f, -60f), 110f, () => WorldSystemsBridge.Pause?.Invoke(),
                new Color(0.25f, 0.3f, 0.45f, 0.75f));
            UiFactory.RoundIconButton(Root, "<3", font, new Vector2(-480f, -60f), 110f, () => WorldSystemsBridge.OpenCoupleMenu?.Invoke(),
                new Color(0.95f, 0.35f, 0.5f, 0.75f));
        }

        void BuildStatus()
        {
            var font = _ui.DefaultFont;
            // region + clock chip (top-left)
            var chip = UiFactory.Panel(Root, "chip", new Color(0.05f, 0.06f, 0.1f, 0.55f * GameConfig.Settings.uiOpacity));
            chip.anchorMin = new Vector2(0f, 1f);
            chip.anchorMax = new Vector2(0f, 1f);
            chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(40f, -40f);
            chip.sizeDelta = new Vector2(460f, 120f);
            _regionLabel = UiFactory.Label(chip, "Azure Haven", 30, font, new Color(0.95f, 0.85f, 0.6f), new Vector2(0f, 26f));
            _regionLabel.sizeDelta = new Vector2(420f, 36f);
            _clock = UiFactory.Label(chip, "10:00  Clear", 26, font, Color.white, new Vector2(0f, -14f));
            _clock.sizeDelta = new Vector2(420f, 32f);

            // interaction prompt (bottom-center)
            _prompt = UiFactory.Label(Root, "", 32, font, Color.white, new Vector2(0f, -420f));
            _prompt.sizeDelta = new Vector2(800f, 50f);
            var promptBg = _prompt.gameObject.AddComponent<Outline>();
            promptBg.effectColor = new Color(0f, 0f, 0f, 0.8f);

            // activity objective (top-center)
            _activityLabel = UiFactory.Label(Root, "", 28, font, new Color(1f, 0.9f, 0.7f), new Vector2(0f, -110f));
            _activityLabel.sizeDelta = new Vector2(1100f, 44f);
        }

        void BuildMinimap()
        {
            _minimap = new GameObject("minimap").AddComponent<RectTransform>();
            _minimap.SetParent(Root, false);
            _minimap.anchorMin = new Vector2(1f, 1f);
            _minimap.anchorMax = new Vector2(1f, 1f);
            _minimap.pivot = new Vector2(1f, 1f);
            _minimap.anchoredPosition = new Vector2(-40f, -40f);
            _minimap.sizeDelta = new Vector2(300f, 300f);
            var img = _minimap.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.JoyRingSprite;
            img.color = new Color(0.08f, 0.1f, 0.16f, 0.65f * GameConfig.Settings.uiOpacity);
        }

        void BuildFade()
        {
            var fadeGo = new GameObject("hudFade");
            _fade = fadeGo.AddComponent<Image>();
            var fadeRt = _fade.rectTransform;
            fadeRt.SetParent(Root, false);
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = Vector2.zero;
            fadeRt.offsetMax = Vector2.zero;
            fadeRt.SetAsLastSibling();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = false;
        }

        public void SetFade(float alpha)
        {
            var c = _fade.color;
            c.a = Mathf.Clamp01(alpha);
            _fade.color = c;
            _fade.raycastTarget = alpha > 0.5f;
        }

        public void Tick(float delta, World.WorldStreamer streamer, World.DayNightCycle dayNight, World.WeatherSystem weather)
        {
            if (!Visible) return;
            _touch.Move = _moveJoy.Value;

            if (_clock != null && dayNight != null)
            {
                int hour = Mathf.FloorToInt(dayNight.Hour);
                int minute = Mathf.FloorToInt((dayNight.Hour - hour) * 60f);
                var phase = hour is >= 5 and < 12 ? "Morning" : hour is >= 12 and < 17 ? "Afternoon" : hour is >= 17 and < 21 ? "Evening" : "Night";
                _clock.text = $"{hour:00}:{minute:00}  {phase}  {weather?.Current.ToString() ?? ""}";
            }
            if (_regionLabel != null)
            {
                var region = Services.Get<World.RegionCatalogService>()?.ActiveRegion;
                if (region != null) _regionLabel.text = region.DisplayName;
            }
            var activity = Services.Get<Activities.ActivityService>()?.Running;
            if (_activityLabel != null && activity != null && activity.State == Activities.ActivityState.Running)
                _activityLabel.text = $"{activity.DisplayName}: {activity.ObjectiveText}";

            UpdateMarkers(delta, streamer);
        }

        void OnPrompt(InteractionPromptEvent evt) => _prompt.text = evt.Visible ? $"[USE]  {evt.Prompt}" : "";

        void OnActivityStarted(ActivityStartedEvent evt) { }
        void OnActivityCompleted(ActivityCompletedEvent evt)
        {
            if (_activityLabel != null) _activityLabel.text = "";
        }

        // ---------------------------------------------------------- minimap

        void UpdateMarkers(float delta, World.WorldStreamer streamer)
        {
            _markerTimer -= delta;
            if (_markerTimer > 0f || streamer == null) return;
            _markerTimer = 1f;
            foreach (var marker in _markers) if (marker.Dot != null) UnityEngine.Object.Destroy(marker.Dot.gameObject);
            _markers.Clear();

            var player = streamer.PlayerPosition;
            var map = Services.Get<World.FastTravelService>();
            var catalog = Services.Get<World.RegionCatalogService>();
            if (map == null || catalog == null) return;

            foreach (var region in catalog.Regions)
            {
                var center = region.WorldCenter;
                var flat = center - player; flat.y = 0f;
                if (flat.magnitude > 800f) continue;
                AddMarker(flat, new Color(0.55f, 0.75f, 1f), 0.5f, true);

                foreach (var ft in region.FastTravel)
                {
                    if (!map.IsDiscovered(region.Id, ft.id)) continue;
                    var wp = region.WorldCenter + new Vector3(ft.x, 0f, ft.z);
                    var offset = wp - player; offset.y = 0f;
                    if (offset.magnitude < 900f) AddMarker(offset, new Color(1f, 0.85f, 0.4f), 0.42f, false);
                }
            }
            // partner marker (pink)
            if (WorldSystemsBridge.PartnerPosition != null)
            {
                var offset = WorldSystemsBridge.PartnerPosition() - player;
                offset.y = 0f;
                if (offset.magnitude < 900f) AddMarker(offset, new Color(1f, 0.45f, 0.65f), 0.55f, false);
            }
        }

        void AddMarker(Vector3 offset, Color color, float sizeScale, bool isRegion)
        {
            const float range = 800f;
            var go = new GameObject("marker");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_minimap, false);
            var normalized = new Vector2(offset.x, offset.z) / range;
            if (normalized.magnitude > 1f) normalized = normalized.normalized * 0.96f;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = normalized * 140f;
            rt.sizeDelta = new Vector2(26f, 26f) * sizeScale * 2f;
            var img = go.AddComponent<Image>();
            img.sprite = UiFactory.PinSprite;
            img.color = color;
            _markers.Add(new MinimapMarker { Dot = rt });
        }

        struct MinimapMarker
        {
            public RectTransform Dot;
        }
    }

    /// <summary>Button that reports press-and-hold state (sprint, dive).</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action onPressed;
        public System.Action onReleased;
        public void OnPointerDown(PointerEventData eventData) => onPressed?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => onReleased?.Invoke();
    }

    /// <summary>
    /// Decouples the HUD from the world wiring: WorldSystems assigns these callbacks.
    /// Keeps the UI assembly free of a hard dependency on the game assembly.
    /// </summary>
    public static class WorldSystemsBridge
    {
        public static System.Action OpenMap;
        public static System.Action Pause;
        public static System.Action OpenCoupleMenu;
        public static System.Func<Vector3> PartnerPosition;
        public static System.Action<ThirdPersonCamera> EnterPhotoMode;
        public static System.Action CapturePhoto;
        public static System.Action ExitPhotoMode;
        public static System.Action<float> ZoomPhoto;
        public static System.Action CyclePose;
    }
}
