using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;
using UnityEngine.EventSystems;
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
            UiFactory.ApplySafeArea(Root);

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
            // Movement virtual joystick (bottom-left)
            _moveJoy = BuildJoystick(new Vector2(190f, 190f), scale, false, "move");

            // Look swipe zone covering right screen region (smooth Free Fire / shooter touch-aim)
            var lookZoneGo = new GameObject("look_swipe_zone");
            var lookRt = lookZoneGo.AddComponent<RectTransform>();
            lookRt.SetParent(Root, false);
            lookRt.anchorMin = new Vector2(0.35f, 0f);
            lookRt.anchorMax = new Vector2(1f, 0.9f);
            lookRt.offsetMin = Vector2.zero;
            lookRt.offsetMax = Vector2.zero;
            lookRt.SetAsFirstSibling();
            var lookImg = lookZoneGo.AddComponent<Image>();
            lookImg.color = Color.clear;
            var lookSwipe = lookZoneGo.AddComponent<LookSwipeZone>();
            lookSwipe.Bind(_touch);
        }

        VirtualJoystick BuildJoystick(Vector2 pos, float scale, bool isLook, string name)
        {
            var ringGo = new GameObject($"joy_{name}");
            var ring = ringGo.AddComponent<RectTransform>();
            ring.SetParent(Root, false);
            ring.anchorMin = new Vector2(0f, 0f);
            ring.anchorMax = new Vector2(0f, 0f);
            ring.pivot = new Vector2(0.5f, 0.5f);
            ring.anchoredPosition = pos;
            ring.sizeDelta = new Vector2(240f, 240f) * scale;
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite = UiFactory.JoyRingSprite;
            ringImg.color = new Color(1f, 1f, 1f, 0.22f * GameConfig.Settings.uiOpacity);

            var thumbGo = new GameObject("thumb");
            var thumb = thumbGo.AddComponent<RectTransform>();
            thumb.SetParent(ring, false);
            thumb.sizeDelta = new Vector2(100f, 100f) * scale;
            var thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.sprite = UiFactory.JoyThumbSprite;
            thumbImg.color = new Color(1f, 1f, 1f, 0.55f * GameConfig.Settings.uiOpacity);

            var joystick = ringGo.AddComponent<VirtualJoystick>();
            joystick.ring = ring;
            joystick.thumb = thumb;
            joystick.isLookJoystick = isLook;
            joystick.maxRadius = 85f * scale;
            joystick.Bind(_touch);
            return joystick;
        }

        void BuildButtons()
        {
            var font = _ui.DefaultFont;
            float op = GameConfig.Settings.uiOpacity;
            var brAnchor = new Vector2(1f, 0f);
            var centerPivot = new Vector2(0.5f, 0.5f);
            var trAnchor = new Vector2(1f, 1f);
            var trPivot = new Vector2(1f, 1f);

            // --- Bottom-Right Action Cluster (Free Fire style ergonomic thumb arc) ---
            // Jump button (prominent circular button, bottom-right)
            UiFactory.RoundIconButton(Root, "JUMP", font, new Vector2(-130f, 130f), 145f, () => _touch.PressJump(),
                new Color(0.98f, 0.55f, 0.22f, 0.88f * op), brAnchor, centerPivot);

            // Sprint hold button (left of Jump)
            var sprint = UiFactory.RoundIconButton(Root, "RUN", font, new Vector2(-265f, 110f), 115f, () => { },
                new Color(0.25f, 0.65f, 0.95f, 0.85f * op), brAnchor, centerPivot);
            var hold = sprint.gameObject.AddComponent<HoldButton>();
            hold.onPressed = () => _touch.SprintHeld = true;
            hold.onReleased = () => _touch.SprintHeld = false;

            // Interact button (above Jump)
            UiFactory.RoundIconButton(Root, "USE", font, new Vector2(-120f, 285f), 125f, () => _touch.PressInteract(),
                new Color(0.22f, 0.88f, 0.52f, 0.88f * op), brAnchor, centerPivot);

            // Context Action button (hook fish / vehicle / activity)
            UiFactory.RoundIconButton(Root, "ACT", font, new Vector2(-245f, 230f), 115f, () => _touch.PressAction(),
                new Color(0.96f, 0.78f, 0.22f, 0.85f * op), brAnchor, centerPivot);

            // Couple Menu button (<3)
            UiFactory.RoundIconButton(Root, "<3", font, new Vector2(-120f, 425f), 105f, () => WorldSystemsBridge.OpenCoupleMenu?.Invoke(),
                new Color(1.0f, 0.32f, 0.58f, 0.90f * op), brAnchor, centerPivot);

            // --- Top-Right Utility Bar (Left of Minimap) ---
            // World Map button
            UiFactory.RoundIconButton(Root, "MAP", font, new Vector2(-265f, -40f), 76f, () => WorldSystemsBridge.OpenMap?.Invoke(),
                new Color(0.22f, 0.28f, 0.44f, 0.88f), trAnchor, trPivot);

            // Pause Menu button
            UiFactory.RoundIconButton(Root, "II", font, new Vector2(-355f, -40f), 76f, () => WorldSystemsBridge.Pause?.Invoke(),
                new Color(0.22f, 0.28f, 0.44f, 0.88f), trAnchor, trPivot);
        }

        void BuildStatus()
        {
            var font = _ui.DefaultFont;
            float op = GameConfig.Settings.uiOpacity;

            // Sleek region & time/weather status chip (top-left)
            var chip = UiFactory.Panel(Root, "statusChip", new Color(0.06f, 0.08f, 0.14f, 0.75f * op));
            chip.anchorMin = new Vector2(0f, 1f);
            chip.anchorMax = new Vector2(0f, 1f);
            chip.pivot = new Vector2(0f, 1f);
            chip.anchoredPosition = new Vector2(25f, -25f);
            chip.sizeDelta = new Vector2(360f, 84f);

            _regionLabel = UiFactory.Label(chip, "Azure Haven", 22, font, new Color(1f, 0.88f, 0.65f), Vector2.zero).GetComponent<Text>();
            _regionLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _regionLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            _regionLabel.rectTransform.pivot = new Vector2(0f, 1f);
            _regionLabel.rectTransform.anchoredPosition = new Vector2(18f, -12f);
            _regionLabel.rectTransform.sizeDelta = new Vector2(330f, 32f);
            _regionLabel.alignment = TextAnchor.MiddleLeft;

            _clock = UiFactory.Label(chip, "10:00  Morning  Clear", 17, font, new Color(0.85f, 0.92f, 1.0f), Vector2.zero).GetComponent<Text>();
            _clock.rectTransform.anchorMin = new Vector2(0f, 0f);
            _clock.rectTransform.anchorMax = new Vector2(0f, 0f);
            _clock.rectTransform.pivot = new Vector2(0f, 0f);
            _clock.rectTransform.anchoredPosition = new Vector2(18f, 10f);
            _clock.rectTransform.sizeDelta = new Vector2(330f, 26f);
            _clock.alignment = TextAnchor.MiddleLeft;

            // Interaction prompt (bottom-center)
            var promptBox = UiFactory.Panel(Root, "promptBox", new Color(0.06f, 0.08f, 0.14f, 0.85f));
            promptBox.anchorMin = new Vector2(0.5f, 0f);
            promptBox.anchorMax = new Vector2(0.5f, 0f);
            promptBox.pivot = new Vector2(0.5f, 0f);
            promptBox.anchoredPosition = new Vector2(0f, 140f);
            promptBox.sizeDelta = new Vector2(560f, 52f);

            _prompt = UiFactory.Label(promptBox, "", 24, font, new Color(1f, 0.92f, 0.55f), Vector2.zero).GetComponent<Text>();
            _prompt.rectTransform.sizeDelta = new Vector2(540f, 44f);
            var promptOutline = _prompt.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            promptBox.gameObject.SetActive(false);

            // Activity objective banner (top-center)
            _activityLabel = UiFactory.Label(Root, "", 22, font, new Color(1f, 0.92f, 0.72f), Vector2.zero).GetComponent<Text>();
            _activityLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _activityLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _activityLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _activityLabel.rectTransform.anchoredPosition = new Vector2(0f, -25f);
            _activityLabel.rectTransform.sizeDelta = new Vector2(750f, 40f);
            var actOutline = _activityLabel.gameObject.AddComponent<Outline>();
            actOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        }

        void BuildMinimap()
        {
            _minimap = new GameObject("minimap").AddComponent<RectTransform>();
            _minimap.SetParent(Root, false);
            _minimap.anchorMin = new Vector2(1f, 1f);
            _minimap.anchorMax = new Vector2(1f, 1f);
            _minimap.pivot = new Vector2(1f, 1f);
            _minimap.anchoredPosition = new Vector2(-25f, -25f);
            _minimap.sizeDelta = new Vector2(220f, 220f);
            var img = _minimap.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.JoyRingSprite;
            img.color = new Color(0.08f, 0.1f, 0.16f, 0.75f * GameConfig.Settings.uiOpacity);
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

        void OnPrompt(InteractionPromptEvent evt)
        {
            if (_prompt == null) return;
            bool show = evt.Visible && !string.IsNullOrEmpty(evt.Prompt);
            _prompt.text = show ? $"[USE]  {evt.Prompt}" : "";
            if (_prompt.transform.parent != null)
                _prompt.transform.parent.gameObject.SetActive(show);
        }

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
            rt.anchoredPosition = normalized * 95f;
            rt.sizeDelta = new Vector2(20f, 20f) * sizeScale * 2f;
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

    /// <summary>Touch swipe surface across right half of screen for modern mobile camera aim.</summary>
    public sealed class LookSwipeZone : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        TouchInputSource _touch;
        Vector2 _lastPos;

        public void Bind(TouchInputSource touch) => _touch = touch;

        public void OnPointerDown(PointerEventData eventData) => _lastPos = eventData.position;

        public void OnDrag(PointerEventData eventData)
        {
            var delta = eventData.position - _lastPos;
            _lastPos = eventData.position;
            if (_touch != null)
            {
                float sens = GameConfig.Settings.lookSensitivity * 0.16f;
                bool inv = GameConfig.Settings.invertLookY;
                _touch.AddLook(new Vector2(delta.x * sens, (inv ? delta.y : -delta.y) * sens));
            }
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
