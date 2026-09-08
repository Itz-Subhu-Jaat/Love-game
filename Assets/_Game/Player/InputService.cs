using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Player
{
    /// <summary>
    /// Input abstraction: gameplay reads this service, never UnityEngine.Input directly.
    /// Touch joysticks (UI), keyboard/mouse (editor), and future gamepads plug in as sources.
    /// </summary>
    public interface IInputSource
    {
        string Name { get; }
        /// <summary>Normalized movement, screen space (x right, y forward).</summary>
        Vector2 Move { get; }
        /// <summary>Look delta in degrees this frame (x yaw, y pitch).</summary>
        Vector2 Look { get; }
        bool JumpPressed { get; }
        bool SprintHeld { get; }
        bool InteractPressed { get; }
        bool ActionPressed { get; }
        bool DiveHeld { get; }
    }

    /// <summary>State written by the HUD virtual joysticks - the touch source.</summary>
    public sealed class TouchInputSource : IInputSource
    {
        public string Name => "Touch";
        public Vector2 Move { get; set; }
        Vector2 _lookDelta;
        bool _jump, _interact, _action;
        public float LookSensitivity { get; set; } = 1f;

        public Vector2 Look => _lookDelta * LookSensitivity;
        public bool JumpPressed { get { var v = _jump; _jump = false; return v; } }
        public bool SprintHeld { get; set; }
        public bool InteractPressed { get { var v = _interact; _interact = false; return v; } }
        public bool ActionPressed { get { var v = _action; _action = false; return v; } }
        public bool DiveHeld { get; set; }

        public void AddLook(Vector2 delta) => _lookDelta += delta;
        public void PressJump() => _jump = true;
        public void PressInteract() => _interact = true;
        public void PressAction() => _action = true;
        public void EndFrame() => _lookDelta = Vector2.zero;
    }

    /// <summary>Keyboard + mouse for editor/desktop testing.</summary>
    public sealed class KeyboardInputSource : IInputSource
    {
        public string Name => "Keyboard";
        public Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        public Vector2 Look => new Vector2(Input.GetAxis("Mouse X") * 2.2f, Input.GetAxis("Mouse Y") * 2.2f);
        public bool JumpPressed => Input.GetKeyDown(KeyCode.Space);
        public bool SprintHeld => Input.GetKey(KeyCode.LeftShift);
        public bool InteractPressed => Input.GetKeyDown(KeyCode.E);
        public bool ActionPressed => Input.GetKeyDown(KeyCode.F);
        public bool DiveHeld => Input.GetKey(KeyCode.LeftControl);
    }

    /// <summary>Service facade: picks the active source, applies user sensitivity settings.</summary>
    public sealed class InputService : IGameService
    {
        public string ServiceName => "Input";

        public TouchInputSource Touch { get; } = new TouchInputSource();
        public KeyboardInputSource Keyboard { get; } = new KeyboardInputSource();
        IInputSource _active;

        public IInputSource Active => _active ?? Keyboard;

        public void Initialize()
        {
            bool hasTouch = Application.isMobilePlatform || Input.touchCount > 0;
            _active = hasTouch ? (IInputSource)Touch : Keyboard;
            Touch.LookSensitivity = GameConfig.Settings.lookSensitivity;
            Log.Info("Input", $"source: {Active.Name}");
        }

        public void Tick(float delta)
        {
            // hot-switch when a touch appears on a desktop build
            if (_active == Keyboard && Input.touchCount > 0) _active = Touch;
            else if (_active == Touch && Input.touchCount == 0 && !Application.isMobilePlatform && Input.anyKey) _active = Keyboard;
            Touch.LookSensitivity = GameConfig.Settings.lookSensitivity;
            Touch.EndFrame();
        }

        public void Shutdown() { }
    }

    /// <summary>Everything movement code needs from the owner character.</summary>
    public interface ICharacterBody
    {
        Transform Transform { get; }
        bool AllowControl { get; }
        void OnLanded(Vector3 velocity);
        void OnStartedSwimming();
        void OnStoppedSwimming();
        float HeightScale { get; }
    }
}
