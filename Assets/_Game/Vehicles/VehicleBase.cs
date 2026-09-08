using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;

namespace LoveGame.Vehicles
{
    public enum VehicleKind { Car, Motorcycle, Boat, Hover }

    [System.Serializable]
    public class VehicleConfig
    {
        public float motorTorque = 4200f;
        public float brakeTorque = 3000f;
        public float maxSpeed = 26f;
        public float steerAngle = 32f;
        public float buoyancy = 14f;
        public float waterDrag = 2.2f;
    }

    /// <summary>
    /// Vehicle framework: driver + passenger seats, Rigidbody physics for wheels/boats,
    /// enter/exit flow with the player, camera mode switching, engine audio hook.
    /// New vehicles = a config + optional custom visuals; no framework changes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleBase : MonoBehaviour
    {
        public string VehicleId;
        public VehicleKind Kind;
        public VehicleConfig Config = new VehicleConfig();
        public Transform DriverSeat;
        public Transform PassengerSeat;
        public Transform[] Wheels;

        public bool IsOccupied { get; private set; }
        public ThirdPersonController Driver { get; private set; }

        Rigidbody _body;
        IInputSource _input;
        IWorldQuery _world;
        float _steer, _throttle;
        AudioSource _engine;

        public System.Action<VehicleBase> OccupancyChanged;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 1200f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.centerOfMass = new Vector3(0f, -0.3f, 0f);
            _engine = gameObject.AddComponent<AudioSource>();
            _engine.spatialBlend = 1f;
            _engine.loop = true;
            _engine.volume = 0f;
            _engine.clip = LoveGame.Audio.ProceduralAudioLibrary.EngineLoop();
        }

        public void Bind(IInputSource input, IWorldQuery world)
        {
            _input = input;
            _world = world;
        }

        public void Enter(ThirdPersonController driver, bool asDriver = true)
        {
            if (IsOccupied && asDriver && Driver != null) return;
            Driver = driver;
            IsOccupied = true;
            driver.AllowControl = false;
            driver.transform.SetParent(asDriver ? DriverSeat : PassengerSeat, false);
            driver.transform.localPosition = Vector3.zero;
            driver.transform.localRotation = Quaternion.identity;
            OccupancyChanged?.Invoke(this);
            GameEvents.Publish(new VehicleEnteredEvent { VehicleKind = Kind.ToString() });
            Log.Info("Vehicles", $"entered {VehicleId} ({Kind})");
        }

        public void Exit(ThirdPersonController controller, ThirdPersonCamera camera)
        {
            controller.transform.SetParent(null, true);
            // place to the side of the vehicle, on ground
            var side = transform.right * -2.2f;
            var pos = transform.position + side;
            if (_world != null)
            {
                var ground = _world.SampleHeight(pos.x, pos.z);
                pos.y = Mathf.Max(pos.y, ground + 0.1f);
            }
            controller.Teleport(pos);
            controller.AllowControl = true;
            Driver = null;
            IsOccupied = false;
            _throttle = 0f;
            _body.linearVelocity = Vector3.zero;
            OccupancyChanged?.Invoke(this);
            GameEvents.Publish(new VehicleExitedEvent { VehicleKind = Kind.ToString() });
        }

        void FixedUpdate()
        {
            if (!IsOccupied || _input == null || Driver == null) return;
            var move = _input.Move;
            _throttle = move.y;
            _steer = move.x;
            bool brake = _input.SprintHeld; // brake button

            var speed = _body.linearVelocity.magnitude;

            if (Kind == VehicleKind.Boat)
            {
                var surface = _world != null ? _world.WaterSurfaceAt(transform.position.x, transform.position.z) : float.NaN;
                if (!float.IsNaN(surface))
                {
                    // buoyancy toward the surface
                    float depth = surface - transform.position.y;
                    var force = Vector3.up * Mathf.Clamp(depth, -1f, 1f) * Config.buoyancy;
                    _body.AddForce(force, ForceMode.Acceleration);
                    _body.AddForce(-_body.linearVelocity * Config.waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }
                if (Mathf.Abs(_throttle) > 0.05f && speed < Config.maxSpeed)
                {
                    var forward = transform.forward * (_throttle * Config.motorTorque * 0.004f);
                    _body.AddForce(forward, ForceMode.Acceleration);
                }
                _body.AddTorque(Vector3.up * (_steer * Config.steerAngle * 0.6f * Mathf.Sign(Mathf.Max(0.1f, _throttle)) * Time.fixedDeltaTime * 10f), ForceMode.VelocityChange);
            }
            else
            {
                // simple arcade car model: forward force + steering by speed
                if (Mathf.Abs(_throttle) > 0.05f && speed < Config.maxSpeed)
                {
                    var forward = transform.forward * (_throttle * Config.motorTorque * 0.0028f);
                    _body.AddForce(forward, ForceMode.Acceleration);
                }
                if (brake)
                {
                    _body.AddForce(-transform.forward * Config.brakeTorque * 0.004f, ForceMode.Acceleration);
                    _body.angularVelocity *= 0.9f;
                }
                if (speed > 0.5f)
                {
                    float steerAmount = _steer * Config.steerAngle * Mathf.Clamp01(speed / 8f) * (Vector3.Dot(_body.linearVelocity, transform.forward) >= 0f ? 1f : -1f);
                    _body.MoveRotation(_body.rotation * Quaternion.Euler(0f, steerAmount * Time.fixedDeltaTime, 0f));
                }
                // stick to ground lightly
                _body.AddForce(Vector3.down * 6f, ForceMode.Acceleration);
            }

            // keep upright
            var up = transform.up;
            if (Vector3.Dot(up, Vector3.up) < 0.6f)
            {
                var correction = Quaternion.FromToRotation(up, Vector3.up);
                _body.MoveRotation(Quaternion.Slerp(_body.rotation, correction * _body.rotation, 2f * Time.fixedDeltaTime));
            }

            if (_engine != null)
            {
                _engine.volume = Mathf.Lerp(_engine.volume, IsOccupied ? 0.35f * (0.3f + speed / Config.maxSpeed) : 0f, 2f * Time.fixedDeltaTime);
                _engine.pitch = 0.7f + (speed / Config.maxSpeed) * 1.3f;
            }
        }
    }
}
