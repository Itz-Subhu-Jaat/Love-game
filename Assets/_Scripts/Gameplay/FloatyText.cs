using UnityEngine;
using UnityEngine.UI;
using LoveGame.UI;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Floating "+25" style score popup. Lives on the unscaled pixel-perfect
    /// popup canvas so world -> screen mapping is 1:1 with real pixels.
    /// </summary>
    public class FloatyText : MonoBehaviour
    {
        private Text _text;
        private Camera _cam;
        private Vector3 _world;
        private float _life;
        private float _duration = 1.1f;

        /// <summary>Spawns a rising, fading text popup anchored to a world position.</summary>
        public static void Show(Canvas popupCanvas, string message, Color color,
                                Vector3 worldPos, int fontSize)
        {
            if (popupCanvas == null) return;

            var go = new GameObject("FloatyText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(popupCanvas.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var t = go.GetComponent<Text>();
            t.font = UiFactory.SharedFont;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.text = message;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            var ft = go.AddComponent<FloatyText>();
            ft._text = t;
            ft._cam = Camera.main;
            ft._world = worldPos;
            ft._life = ft._duration;
        }

        private void Update()
        {
            if (_cam == null)
            {
                Destroy(gameObject);
                return;
            }

            _life -= Time.unscaledDeltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float rise = (1f - _life / _duration) * 1.2f;
            Vector3 screen = _cam.WorldToScreenPoint(_world + Vector3.up * rise);
            ((RectTransform)transform).position = screen;

            var c = _text.color;
            c.a = Mathf.Clamp01(_life / (_duration * 0.55f));
            _text.color = c;
        }
    }
}
