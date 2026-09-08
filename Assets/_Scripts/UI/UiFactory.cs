using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace LoveGame.UI
{
    /// <summary>
    /// Code-only uGUI construction kit. Every panel, label and button of the
    /// game is built through this factory so the repo needs zero serialized
    /// UI prefabs — nothing can break in scene files.
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        /// <summary>The shared built-in UI font (LegacyRuntime.ttf on Unity 6).</summary>
        public static Font SharedFont
        {
            get
            {
                if (_font == null) _font = ResolveFont();
                return _font;
            }
        }

        private static Font ResolveFont()
        {
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
            try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { }
            return Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        // ------------------------------------------------------------- canvas

        /// <summary>
        /// Creates a Screen Space Overlay canvas. Set <paramref name="scaled"/>
        /// for HUD/panels (1280x720 reference), leave false for the pixel
        /// perfect popup canvas used by floating score texts.
        /// </summary>
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder, bool scaled)
        {
            var go = new GameObject(name, typeof(Canvas));
            var cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = sortingOrder;

            if (scaled)
            {
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return cv;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null);
        }

        // ------------------------------------------------------------- layout

        public static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                      Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Stretch(RectTransform rt)
        {
            SetAnchors(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------- widgets

        public static Text CreateText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, offsetMin, offsetMax);

            var t = go.GetComponent<Text>();
            t.font = SharedFont;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = alignment;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Image CreateImage(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, offsetMin, offsetMax);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sprite != null) img.preserveAspect = true;
            return img;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            int fontSize, Color background, Color textColor, UnityAction onClick,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, offsetMin, offsetMax);

            var img = go.GetComponent<Image>();
            img.color = background;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;
            if (onClick != null) btn.onClick.AddListener(onClick);

            CreateText(go.transform, name + "_label", label, fontSize, textColor,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            return btn;
        }
    }
}
