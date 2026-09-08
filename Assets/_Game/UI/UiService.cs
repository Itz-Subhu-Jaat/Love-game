using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoveGame.UI
{
    /// <summary>
    /// UI foundation: persistent root canvas (1080p reference), screen stack with fades,
    /// toast notifications, and a factory for all touch widgets. Screens are plain classes
    /// built entirely from code so the project ships without scene UI serialization.
    /// </summary>
    public sealed class UiService : IGameService
    {
        public string ServiceName => "UI";

        public Canvas RootCanvas { get; private set; }
        readonly Stack<Screen> _stack = new Stack<Screen>();
        Transform _screenLayer;
        Transform _toastLayer;
        readonly List<Toast> _toasts = new List<Toast>();
        Image _fader;
        Font _font;

        public Font DefaultFont => _font ?? (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public void Initialize()
        {
            var go = new GameObject("~UI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            RootCanvas = go.AddComponent<Canvas>();
            RootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RootCanvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            _screenLayer = NewLayer("screens", 10);
            _toastLayer = NewLayer("toasts", 20);
            var faderGo = NewLayer("fader", 30).gameObject;
            _fader = faderGo.AddComponent<Image>();
            _fader.color = new Color(0.03f, 0.02f, 0.06f, 0f);
            _fader.raycastTarget = true;
            _fader.gameObject.SetActive(false);

            GameEvents.Subscribe<NotificationEvent>(OnNotification);
            GameLogRuntimeBridge.EnsureEventSystem();
        }

        Transform NewLayer(string name, int sibling)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(RootCanvas.transform, false);
            var rt = layer.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return layer.transform;
        }

        public void Tick(float delta)
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (_toasts[i].Tick(delta)) _toasts.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------- stack

        public void Push(Screen screen)
        {
            screen.Service = this;
            screen.Build(_screenLayer);
            _stack.Push(screen);
            screen.OnShown();
            StartCoroutineSafe(screen.EnterRoutine());
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            var screen = _stack.Pop();
            screen.OnHidden();
            UnityEngine.Object.Destroy(screen.Root.gameObject);
        }

        public void PopAll()
        {
            while (_stack.Count > 0) Pop();
        }

        public Screen Top => _stack.Count > 0 ? _stack.Peek() : null;
        public bool IsOverlayOpen => _stack.Count > 0;

        public void StartCoroutineSafe(IEnumerator routine) => Services.Host.Run(routine);

        public void SetFader(float alpha)
        {
            _fader.gameObject.SetActive(alpha > 0.01f);
            var c = _fader.color;
            c.a = Mathf.Clamp01(alpha);
            _fader.color = c;
        }

        void OnNotification(NotificationEvent evt)
        {
            if (string.IsNullOrEmpty(evt.Title)) return;
            var toast = Toast.Build(_toastLayer, this, evt.Title, evt.Body, evt.Duration <= 0 ? 3f : evt.Duration);
            _toasts.Add(toast);
        }

        public void Shutdown() => PopAll();
    }

    /// <summary>Base class for all screens - lifecycle hooks + root rect.</summary>
    public abstract class Screen
    {
        public UiService Service { get; set; }
        public RectTransform Root { get; protected set; }
        public virtual bool ShowOnHudStack => true;

        public void Build(Transform parent)
        {
            var go = new GameObject(GetType().Name);
            Root = go.AddComponent<RectTransform>();
            Root.SetParent(parent, false);
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.one;
            Root.offsetMin = Vector2.zero;
            Root.offsetMax = Vector2.one;
            OnBuild(Root);
        }

        protected abstract void OnBuild(RectTransform root);
        public virtual void OnShown() { }
        public virtual void OnHidden() { }
        public virtual void OnTick(float delta) { }

        public IEnumerator EnterRoutine()
        {
            var group = Root.GetComponent<CanvasGroup>() ?? Root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
    }

    /// <summary>Auto-expiring toast card.</summary>
    public sealed class Toast
    {
        RectTransform _root;
        float _remaining;
        UiService _service;

        public static Toast Build(Transform parent, UiService service, string title, string body, float duration)
        {
            var root = UiFactory.Panel(parent, "toast", new Color(0.08f, 0.07f, 0.12f, 0.92f));
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -220f);
            root.sizeDelta = new Vector2(680f, 130f);
            UiFactory.Label(root, title, 34, service.DefaultFont, new Color(1f, 0.8f, 0.55f), new Vector2(0f, 26f));
            UiFactory.Label(root, body, 26, service.DefaultFont, Color.white, new Vector2(0f, -18f));
            return new Toast { _root = root, _remaining = duration, _service = service };
        }

        public bool Tick(float delta)
        {
            _remaining -= delta;
            if (_remaining <= 0f)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
                return true;
            }
            return false;
        }
    }

    /// <summary>Procedural widget factory - 9-slice sprites, buttons, sliders, labels, icons.</summary>
    public static class UiFactory
    {
        static Sprite _panel, _button, _buttonRound, _joyRing, _joyThumb, _heart, _track, _fill, _pin;

        public static Sprite PanelSprite => _panel ?? (_panel = Load("ui_panel"));
        public static Sprite ButtonSprite => _button ?? (_button = Load("ui_button"));
        public static Sprite ButtonRoundSprite => _buttonRound ?? (_buttonRound = Load("ui_button_round"));
        public static Sprite JoyRingSprite => _joyRing ?? (_joyRing = Load("ui_joystick_ring"));
        public static Sprite JoyThumbSprite => _joyThumb ?? (_joyThumb = Load("ui_joystick_thumb"));
        public static Sprite HeartSprite => _heart ?? (_heart = Load("ui_heart"));
        public static Sprite TrackSprite => _track ?? (_track = Load("ui_slider_track"));
        public static Sprite FillSprite => _fill ?? (_fill = Load("ui_slider_fill"));
        public static Sprite PinSprite => _pin ?? (_pin = Load("map_pin"));

        static Sprite Load(string name)
        {
            var sprite = Resources.Load<Sprite>($"Sprites/{name}");
            if (sprite == null) Log.Warn("UI", $"sprite 'Sprites/{name}' missing");
            return sprite;
        }

        /// <summary>Full-screen dimmer behind overlay screens.</summary>
        public static RectTransform Dim(RectTransform parent)
        {
            var go = new GameObject("dim");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.5f);
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Color tint)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = PanelSprite;
            img.type = Image.Type.Sliced;
            img.color = tint;
            return rt;
        }

        public static RectTransform Label(RectTransform parent, string text, int size, Font font, Color color, Vector2 offset)
        {
            var go = new GameObject("label");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            var label = go.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return rt;
        }

        public static Button TextButton(RectTransform parent, string text, int fontSize, Font font, Vector2 anchoredPos, Vector2 size, System.Action onClick, Color? tint = null)
        {
            var go = new GameObject($"btn_{text}");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;
            img.color = tint ?? new Color(0.95f, 0.35f, 0.45f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var label = Label(rt, text, fontSize, font, Color.white, Vector2.zero);
            label.sizeDelta = size;
            button.onClick.AddListener(() =>
            {
                Services.TryGet<Audio.AudioService>(out var audio);
                audio?.QueueSfx(Audio.ProceduralAudioLibrary.UiClick());
                onClick?.Invoke();
            });
            return button;
        }

        public static Button RoundIconButton(RectTransform parent, string text, Font font, Vector2 anchoredPos, float size, System.Action onClick, Color? tint = null)
        {
            var go = new GameObject($"round_{text}");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.one * size;
            var img = go.AddComponent<Image>();
            img.sprite = ButtonRoundSprite;
            img.color = tint ?? new Color(0.1f, 0.12f, 0.2f, 0.8f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var label = Label(rt, text, Mathf.RoundToInt(size * 0.24f), font, Color.white, Vector2.zero);
            label.sizeDelta = rt.sizeDelta;
            button.onClick.AddListener(() =>
            {
                Services.TryGet<Audio.AudioService>(out var audio);
                audio?.QueueSfx(Audio.ProceduralAudioLibrary.UiClick());
                onClick?.Invoke();
            });
            return button;
        }

        public static Slider Slider(RectTransform parent, string label, float min, float max, float value, Font font, Vector2 pos, float width, System.Action<float> onChange)
        {
            var row = new GameObject($"row_{label}");
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(width, 70f);

            var labelRt = Label(rowRt, label, 26, font, new Color(0.9f, 0.92f, 0.95f), new Vector2(0f, 18f));
            labelRt.sizeDelta = new Vector2(width, 34f);

            var sliderGo = new GameObject("slider");
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.SetParent(rowRt, false);
            sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRt.anchoredPosition = new Vector2(0f, -12f);
            sliderRt.sizeDelta = new Vector2(width - 60f, 26f);

            var bg = sliderGo.AddComponent<Image>();
            bg.sprite = TrackSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.15f, 0.16f, 0.22f, 0.9f);

            var fillGo = new GameObject("fill");
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.SetParent(sliderRt, false);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0.5f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = FillSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.95f, 0.4f, 0.5f, 0.95f);

            var handleGo = new GameObject("handle");
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.SetParent(sliderRt, false);
            handleRt.sizeDelta = new Vector2(30f, 44f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = ButtonRoundSprite;
            handleImg.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        /// <summary>Scrollable vertical list used by inventory/journal/downloads.</summary>
        public static RectTransform ScrollList(RectTransform parent, string name, out RectTransform content, float height)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1500f, height);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.06f, 0.1f, 0.6f);
            img.raycastTarget = true;

            var viewport = new GameObject("viewport");
            var viewportRt = viewport.AddComponent<RectTransform>();
            viewportRt.SetParent(rt, false);
            viewportRt.anchorMin = new Vector2(0f, 0f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.offsetMin = new Vector2(20f, 20f);
            viewportRt.offsetMax = new Vector2(-20f, -20f);
            viewport.AddComponent<RectMask2D>();

            var contentGo = new GameObject("content");
            content = contentGo.AddComponent<RectTransform>();
            content.SetParent(viewportRt, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, height);

            var scroll = go.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            return rt;
        }
    }

    /// <summary>Virtual joystick: drag drives a normalized vector + optional look delta.</summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform ring;
        public RectTransform thumb;
        public bool isLookJoystick;
        public float maxRadius = 150f;
        public Vector2 Value { get; private set; }
        TouchInputSource _touch;
        Vector2 _pointerStart;
        Vector2 _thumbStart;

        public void Bind(TouchInputSource touch) => _touch = touch;

        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ring, eventData.position, eventData.pressEventCamera, out _pointerStart);
            _thumbStart = thumb.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ring, eventData.position, eventData.pressEventCamera, out var current);
            var delta = current - _pointerStart;
            delta *= GameConfig.Settings.joystickSize;
            if (delta.magnitude > maxRadius) delta = delta.normalized * maxRadius;
            thumb.anchoredPosition = _thumbStart + delta;
            Value = delta / maxRadius;
            if (isLookJoystick && _touch != null) _touch.AddLook(new Vector2(Value.x, -Value.y) * 8f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            thumb.anchoredPosition = _thumbStart;
            _thumbStart = Vector2.zero;
        }
    }
}
