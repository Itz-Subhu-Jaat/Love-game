using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Player
{
    public enum LocomotionState { Idle, Walking, Running, Sprinting, Jumping, Falling, Swimming, Underwater, Sitting, InVehicle }

    /// <summary>
    /// Third-person character controller over CharacterController: walk/jog/sprint/jump/fall,
    /// slopes and steps via skin width, surface swimming with buoyancy, underwater 3D movement.
    /// Never reads input directly - consumes IInputSource through InputService.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour, ICharacterBody, IInteractor
    {
        public Transform Transform => transform;
        public bool AllowControl { get; set; } = true;
        public float HeightScale => 1f;

        [Header("Speeds (m/s)")]
        public float walkSpeed = 1.8f;
        public float jogSpeed = 4.2f;
        public float sprintSpeed = 6.8f;
        public float swimSpeed = 2.6f;
        public float underwaterSpeed = 3.2f;
        public float rotationSpeed = 10f;
        public float jumpHeight = 1.4f;
        public float gravity = 18f;

        public LocomotionState State { get; private set; } = LocomotionState.Idle;
        public Vector3 Velocity { get; private set; }
        public bool IsSwimming { get; private set; }
        public bool IsUnderwater { get; private set; }
        public bool IsGrounded { get; private set; }
        public Vector2 PlanarMove { get; private set; }

        CharacterController _cc;
        IInputSource _input;
        IWorldQuery _world;
        float _verticalVelocity;
        float _fallStartY;
        float _swimBobPhase;
        Vector3 _lastPosition;

        public bool CanInteract => AllowControl && !IsUnderwater;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        public void Bind(IInputSource input, IWorldQuery world)
        {
            _input = input;
            _world = world;
        }

        void Update()
        {
            if (_input == null || _world == null) return;
            float dt = Time.deltaTime;
            var pos = transform.position;

            // ---- water state
            var surface = _world.WaterSurfaceAt(pos.x, pos.z);
            bool inWaterVolume = !float.IsNaN(surface);
            float bodyY = pos.y + _cc.height * 0.5f;
            IsUnderwater = inWaterVolume && bodyY < surface - 0.4f;
            bool wantSwim = inWaterVolume && pos.y < surface - 0.6f;

            if (wantSwim != IsSwimming)
            {
                IsSwimming = wantSwim;
                if (IsSwimming) OnStartedSwimming();
                else OnStoppedSwimming();
            }

            if (!AllowControl)
            {
                Velocity = Vector3.zero;
                PlanarMove = Vector2.zero;
                State = LocomotionState.Idle;
                return;
            }

            var move = _input.Move;
            PlanarMove = move;

            if (IsSwimming)
            {
                SimulateSwimming(dt, move, surface);
            }
            else
            {
                SimulateGrounded(dt, move);
            }

            _lastPosition = transform.position;
        }

        void SimulateGrounded(float dt, Vector2 move)
        {
            var inputDir = new Vector3(move.x, 0f, move.y);
            var camFlat = Camera.main != null ? Camera.main.transform : transform;
            var forward = camFlat.forward; forward.y = 0f; forward.Normalize();
            var right = camFlat.right; right.y = 0f; right.Normalize();
            var worldDir = forward * inputDir.z + right * inputDir.x;

            bool moving = worldDir.sqrMagnitude > 0.04f;
            float speed = _input.SprintHeld ? sprintSpeed : jogSpeed;
            if (move.magnitude < 0.45f) speed = walkSpeed;

            if (moving)
            {
                var targetRot = Quaternion.LookRotation(worldDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);
            }

            IsGrounded = _cc.isGrounded;
            if (IsGrounded)
            {
                if (_verticalVelocity < -1f) OnLanded(Velocity);
                _verticalVelocity = -2f;
                if (_input.JumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                    State = LocomotionState.Jumping;
                }
            }
            else
            {
                _verticalVelocity -= gravity * dt;
                if (_verticalVelocity < 0f && State != LocomotionState.Falling)
                {
                    _fallStartY = transform.position.y;
                    State = LocomotionState.Falling;
                }
            }

            var horizontal = moving ? worldDir * speed : Vector3.zero;
            Velocity = new Vector3(horizontal.x, _verticalVelocity, horizontal.z);
            _cc.Move(Velocity * dt);

            if (IsGrounded && moving)
                State = speed >= sprintSpeed ? LocomotionState.Sprinting : speed <= walkSpeed ? LocomotionState.Walking : LocomotionState.Running;
            else if (IsGrounded) State = LocomotionState.Idle;
        }

        void SimulateSwimming(float dt, Vector2 move, float surface)
        {
            State = IsUnderwater ? LocomotionState.Underwater : LocomotionState.Swimming;

            var inputDir = new Vector3(move.x, 0f, move.y);
            var camFlat = Camera.main != null ? Camera.main.transform : transform;
            var forward = camFlat.forward; forward.y = 0f; forward.Normalize();
            var right = camFlat.right; right.y = 0f; right.Normalize();
            var worldDir = forward * inputDir.z + right * inputDir.x;

            if (worldDir.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(worldDir), rotationSpeed * 0.7f * dt);

            _swimBobPhase += dt * 2f;
            float targetY;
            if (IsUnderwater)
            {
                // free 3D movement while submerged; slight buoyancy up, dive input down
                float vertical = 0f;
                if (_input.DiveHeld) vertical -= 1f;
                else if (_input.JumpPressed) vertical += 1f;
                var v = new Vector3(worldDir.x * underwaterSpeed, vertical * underwaterSpeed * 0.7f + 0.25f, worldDir.z * underwaterSpeed);
                _cc.Move(v * dt);
                Velocity = v;
                return;
            }

            // surface swim: buoyancy keeps head above water, gentle bob
            float bob = Mathf.Sin(_swimBobPhase) * 0.08f;
            targetY = surface - 0.55f + bob;
            float currentY = transform.position.y;
            float newY = Mathf.Lerp(currentY, targetY, 6f * dt);
            var vel = new Vector3(worldDir.x * swimSpeed, (newY - currentY) / Mathf.Max(0.001f, dt), worldDir.z * swimSpeed);
            _cc.Move(vel * dt);
            Velocity = vel;
        }

        /// <summary>External repositioning (fast travel, vehicle exit) that respects colliders.</summary>
        public void Teleport(Vector3 worldPos)
        {
            _cc.enabled = false;
            transform.position = worldPos;
            _verticalVelocity = 0f;
            Velocity = Vector3.zero;
            _cc.enabled = true;
        }

        public void SnapToGround()
        {
            if (_world == null) return;
            var h = _world.SampleHeight(transform.position.x, transform.position.z);
            Teleport(new Vector3(transform.position.x, h + _cc.height * 0.5f + 0.1f, transform.position.z));
        }

        public event System.Action<Vector3> Landed;
        public event System.Action StartedSwimming;
        public event System.Action StoppedSwimming;

        void OnLanded(Vector3 velocity) => Landed?.Invoke(velocity);
        void OnStartedSwimming() => StartedSwimming?.Invoke();
        void OnStoppedSwimming() => StoppedSwimming?.Invoke();
    }
}
