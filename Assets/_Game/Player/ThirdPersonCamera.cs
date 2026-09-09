using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Player
{
    public enum CameraMode { Normal, Vehicle, Swim, Underwater, Photo, Cinematic }

    /// <summary>
    /// Premium third-person camera: smooth follow, spherecast collision, shoulder switching,
    /// zoom, mode presets (vehicle/swim/photo/cinematic) and configurable FOV/sensitivity.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Follow")]
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 4.5f;
        public float minDistance = 1.4f;
        public float maxDistance = 9f;
        public float followSmoothing = 0.06f;
        public float shoulderOffset = 0.7f;

        [Header("Look")]
        public float yawSensitivity = 120f;
        public float pitchSensitivity = 90f;
        public float minPitch = -50f;
        public float maxPitch = 65f;

        [Header("Fov")]
        public float baseFov = 58f;

        public CameraMode Mode { get; set; } = CameraMode.Normal;
        public int ShoulderSide { get; private set; } = 1;

        Camera _camera;
        IInputSource _input;
        float _yaw = 180f;
        float _pitch = 12f;
        float _currentDistance;
        Vector3 _currentPivot;
        Vector3 _pivotVelocity;
        float _modeFov;
        bool _freeLook;
        Vector3 _freePosition;
        Vector3 _freeRotation;

        public Camera Camera => _camera;

        public void Bind(IInputSource input)
        {
            _input = input;
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<Camera>();
            _camera.fieldOfView = baseFov;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 3000f;
            _camera.depth = 10f;
            _modeFov = baseFov;
            _currentDistance = distance;
            tag = "MainCamera";
        }

        public void SetTarget(Transform t, bool snap)
        {
            target = t;
            if (t != null)
            {
                var pivot = t.position + pivotOffset;
                if (snap)
                {
                    _currentPivot = pivot;
                    _pivotVelocity = Vector3.zero;
                    transform.position = pivot - transform.forward * _currentDistance;
                }
            }
        }

        void Update()
        {
            if (_camera == null) return;
            var dt = Time.deltaTime;

            switch (Mode)
            {
                case CameraMode.Photo: UpdatePhoto(dt); break;
                case CameraMode.Cinematic: UpdateCinematic(dt); break;
                default: UpdateFollow(dt); break;
            }

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _modeFov, 6f * dt);
        }

        void UpdateFollow(float dt)
        {
            if (_input != null)
            {
                var look = _input.Look;
                float sens = GameConfig.Settings.lookSensitivity;
                _yaw += look.x * yawSensitivity * 0.02f * sens;
                _pitch += (GameConfig.Settings.invertLookY ? look.y : -look.y) * pitchSensitivity * 0.02f * sens;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            if (target == null) return;

            var modeDistance = Mode switch
            {
                CameraMode.Vehicle => distance * 1.35f,
                CameraMode.Swim => distance * 1.1f,
                _ => distance
            };
            _modeFov = Mode switch
            {
                CameraMode.Vehicle => baseFov + 12f,
                CameraMode.Underwater => baseFov - 6f,
                CameraMode.Cinematic => baseFov - 10f,
                _ => baseFov
            };

            var pivot = target.position + pivotOffset;
            // shoulder offset perpendicular to view
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var side = rot * Vector3.right * (shoulderOffset * ShoulderSide);
            var desiredPivot = pivot + side;
            if (_currentPivot == Vector3.zero) _currentPivot = desiredPivot;
            _currentPivot = Vector3.SmoothDamp(_currentPivot, desiredPivot, ref _pivotVelocity, followSmoothing);

            var desiredDir = rot * Vector3.back;
            var desiredPos = _currentPivot + desiredDir * modeDistance;

            // collision: spherecast from pivot toward desired camera
            var castDir = desiredPos - _currentPivot;
            float castLen = castDir.magnitude;
            if (castLen > 0.01f &&
                Physics.SphereCast(_currentPivot, 0.25f, castDir.normalized, out var hit, castLen, GameLayers.DefaultMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = _currentPivot + castDir.normalized * Mathf.Max(minDistance, hit.distance - 0.3f);
            }

            transform.position = desiredPos;
            transform.rotation = rot;
            _currentDistance = modeDistance;
        }

        void UpdatePhoto(float dt)
        {
            if (_input == null) return;
            var look = _input.Look;
            _freeRotation += new Vector3(-look.y * 0.12f, look.x * 0.12f, 0f);
            _freeRotation.x = Mathf.Clamp(_freeRotation.x, -70f, 70f);
            var move = _input.Move;
            var rot = Quaternion.Euler(_freeRotation);
            var moveDir = rot * new Vector3(move.x, 0f, move.y) * (move.magnitude > 0.01f ? 6f * dt : 0f);
            _freePosition += moveDir;
            transform.SetPositionAndRotation(_freePosition, rot);
        }

        /// <summary>Enter photo mode: detach from target, remember return state.</summary>
        public void EnterPhotoMode(Vector3 origin)
        {
            _freePosition = origin;
            _freeRotation = transform.eulerAngles;
            Mode = CameraMode.Photo;
            _modeFov = 40f;
        }

        public void ExitPhotoMode()
        {
            Mode = CameraMode.Normal;
            _modeFov = baseFov;
        }

        /// <summary>Photo mode vertical fly.</summary>
        public void PhotoFly(float direction, float dt)
        {
            if (Mode != CameraMode.Photo) return;
            _freePosition += Vector3.up * direction * 4f * dt;
        }

        public void PhotoZoom(float deltaFov)
        {
            if (Mode != CameraMode.Photo) return;
            _modeFov = Mathf.Clamp(_modeFov + deltaFov, 18f, 70f);
        }

        public void SwitchShoulder() => ShoulderSide = -ShoulderSide;

        /// <summary>Cinematic orbit used during couple interactions.</summary>
        public void BeginCinematic(Transform subject, float height, float radius)
        {
            Mode = CameraMode.Cinematic;
            _cinematicSubject = subject;
            _cinematicHeight = height;
            _cinematicRadius = radius;
            _cinematicAngle = 0f;
        }

        public void EndCinematic()
        {
            Mode = CameraMode.Normal;
            _cinematicSubject = null;
        }

        Transform _cinematicSubject;
        float _cinematicHeight = 2f, _cinematicRadius = 4.5f, _cinematicAngle;

        void UpdateCinematic(float dt)
        {
            if (_cinematicSubject == null) { Mode = CameraMode.Normal; return; }
            _cinematicAngle += dt * 0.35f;
            var center = _cinematicSubject.position + Vector3.up * _cinematicHeight;
            var pos = center + new Vector3(Mathf.Cos(_cinematicAngle) * _cinematicRadius, 1.2f, Mathf.Sin(_cinematicAngle) * _cinematicRadius);
            transform.position = Vector3.Slerp(transform.position, pos, 5f * dt);
            transform.LookAt(center);
        }

        public bool IsCinematic => Mode == CameraMode.Cinematic;
    }
}
