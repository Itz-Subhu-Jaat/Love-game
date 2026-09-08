using UnityEngine;
using LoveGame.UI;

namespace LoveGame.Core
{
    /// <summary>
    /// Shared base for the two scene bootstraps. Guarantees an orthographic
    /// camera, a scaled HUD canvas and an unscaled pixel-perfect popup canvas
    /// exist before anything else runs.
    /// </summary>
    public abstract class BootstrapBase : MonoBehaviour
    {
        /// <summary>Reference resolution for the HUD canvas (1280 x 720).</summary>
        protected const float ReferenceHeight = 720f;
        protected const float ReferenceWidth = 1280f;

        protected Camera Cam;
        protected Canvas ScaledCanvas;
        protected Canvas PopupCanvas;

        protected virtual void Awake()
        {
            Time.timeScale = 1f;

            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                Cam = go.GetComponent<Camera>();
            }

            Cam.transform.position = new Vector3(0f, 0f, -10f);
            Cam.orthographic = true;
            Cam.orthographicSize = 6.4f;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 60f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = GameConstants.DeepPurple;

            ScaledCanvas = UiFactory.CreateOverlayCanvas("HUDCanvas", 10, true);
            PopupCanvas = UiFactory.CreateOverlayCanvas("PopupCanvas", 20, false);
        }

        /// <summary>Creates a sprite object from Resources/Sprites.</summary>
        protected GameObject MakeSprite(string spritePath, string goName, int sortingOrder)
        {
            var go = new GameObject(goName, typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>(spritePath);
            sr.sortingOrder = sortingOrder;
            return go;
        }

        /// <summary>
        /// Stretches a background sprite so it always covers the camera view,
        /// whatever the window aspect ratio is.
        /// </summary>
        protected GameObject CoverBackground(string spritePath)
        {
            var go = MakeSprite(spritePath, "Background", -10);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr.sprite == null) return go;

            var bounds = sr.sprite.bounds;
            float halfH = Cam.orthographicSize;
            float halfW = halfH * Cam.aspect;
            float scale = Mathf.Max(halfW / bounds.extents.x, halfH / bounds.extents.y) * 1.05f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.position = new Vector3(0f, 0f, 20f);
            return go;
        }
    }
}
