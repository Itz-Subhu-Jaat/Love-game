using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;

namespace LoveGame.Vehicles
{
    /// <summary>
    /// Spawns/registers vehicles from region data, restores them from save, and handles
    /// enter/exit through the interaction system. Placeholder visuals from PropLibrary-style
    /// factories are replaceable by prefabs later (visuals only).
    /// </summary>
    public sealed class VehicleService : IGameService
    {
        public string ServiceName => "Vehicles";

        World.WorldStreamer _streamer;
        readonly List<VehicleBase> _vehicles = new List<VehicleBase>();
        Transform _root;
        ThirdPersonController _player;
        ThirdPersonCamera _camera;
        InputService _input;
        VehicleBase _occupied;
        int _counter;

        public IReadOnlyList<VehicleBase> All => _vehicles;
        public VehicleBase Occupied => _occupied;

        public void Bind(ThirdPersonController player, ThirdPersonCamera camera, InputService input)
        {
            _player = player;
            _camera = camera;
            _input = input;
        }

        public void Initialize()
        {
            _streamer = Services.Get<World.WorldStreamer>();
            _root = new GameObject("~Vehicles").transform;
            UnityEngine.Object.DontDestroyOnLoad(_root.gameObject);
            if (_streamer != null)
            {
                _streamer.RegionBuilt += OnRegionBuilt;
                _streamer.RegionUnloaded += OnRegionUnloaded;
            }
        }

        void OnRegionBuilt(World.RegionInstance inst)
        {
            foreach (var spawn in inst.Data.VehicleSpawns)
            {
                var world = inst.Data.WorldCenter + new Vector3(spawn.x, 0f, spawn.z);
                float ground = _streamer.SampleHeight(world.x, world.z);
                bool isWater = ground < inst.Data.WaterLevel - 0.5f;
                var kind = ParseKind(spawn.kind);
                if (kind != VehicleKind.Boat && isWater) continue;
                if (kind == VehicleKind.Boat) world.y = inst.Data.WaterLevel - 0.3f;
                else world.y = ground + 0.6f;
                Spawn(kind, world, inst.Data.Id);
            }
            // restore player-owned vehicles from save for this region
            foreach (var saved in SaveSystem.Current.worldObjects.vehicles)
            {
                if (saved.regionId != inst.Data.Id) continue;
                if (_vehicles.Exists(v => v.VehicleId == saved.vehicleId)) continue;
                var restored = Spawn(ParseKind(saved.kind), new Vector3(saved.x, saved.y, saved.z), saved.regionId);
                if (restored != null)
                {
                    restored.VehicleId = saved.vehicleId;
                    restored.transform.rotation = Quaternion.Euler(0f, saved.rotationY, 0f);
                }
            }
        }

        void OnRegionUnloaded(string regionId)
        {
            for (int i = _vehicles.Count - 1; i >= 0; i--)
            {
                var v = _vehicles[i];
                // vehicles persist in save; only despawn if not owned and region gone
                if (v == null) { _vehicles.RemoveAt(i); continue; }
                if (!IsOwned(v.VehicleId) && RegionOf(v) == regionId)
                {
                    UnityEngine.Object.Destroy(v.gameObject);
                    _vehicles.RemoveAt(i);
                }
            }
        }

        static bool IsOwned(string vehicleId) =>
            SaveSystem.Current.worldObjects.vehicles.Exists(e => e.vehicleId == vehicleId);

        string RegionOf(VehicleBase v) =>
            _streamer != null ? _streamer.RegionIdAt(v.transform.position) : null;

        static VehicleKind ParseKind(string kind) =>
            System.Enum.TryParse(kind, true, out VehicleKind k) ? k : VehicleKind.Car;

        public VehicleBase Spawn(VehicleKind kind, Vector3 position, string regionId)
        {
            var go = BuildVisual(kind);
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.layer = GameLayers.Vehicle;
            var vehicle = go.AddComponent<VehicleBase>();
            vehicle.VehicleId = $"veh_{kind.ToString().ToLower()}_{_counter++:000}";
            vehicle.Kind = kind;
            vehicle.Bind(_input != null ? _input.Active : null, _streamer);
            vehicle.DriverSeat = go.transform.Find("seats/driver");
            vehicle.PassengerSeat = go.transform.Find("seats/passenger");

            var inter = go.AddComponent<Interactable>();
            inter.interactableId = vehicle.VehicleId;
            inter.prompt = kind == VehicleKind.Boat ? "Board boat" : $"Enter {kind.ToString().ToLower()}";
            inter.range = 4.5f;
            inter.context = $"Vehicle:{kind}";
            inter.InteractEvent += interactor => TryEnter(vehicle);
            _vehicles.Add(vehicle);
            Log.Verbose("Vehicles", $"spawned {vehicle.VehicleId} in {regionId}");
            return vehicle;
        }

        static GameObject BuildVisual(VehicleKind kind)
        {
            var root = new GameObject($"vehicle_{kind}");
            var seats = new GameObject("seats").transform;
            seats.SetParent(root.transform, false);
            var driver = new GameObject("driver").transform;
            driver.SetParent(seats, false);
            driver.localPosition = new Vector3(-0.4f, 0.4f, 0f);
            var passenger = new GameObject("passenger").transform;
            passenger.SetParent(seats, false);
            passenger.localPosition = new Vector3(0.4f, 0.4f, 0f);

            var paint = MaterialLibrary.Tinted(MaterialLibrary.Lit,
                kind == VehicleKind.Boat ? new Color(0.95f, 0.92f, 0.88f) :
                kind == VehicleKind.Motorcycle ? new Color(0.9f, 0.25f, 0.2f) :
                kind == VehicleKind.Hover ? new Color(0.3f, 0.85f, 0.9f) : new Color(0.25f, 0.45f, 0.85f), "veh" + kind);
            var dark = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.12f, 0.13f, 0.15f), "vehdark");
            var glass = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.5f, 0.7f, 0.85f), "vehglass");

            switch (kind)
            {
                case VehicleKind.Car:
                    Part(root.transform, "chassis", new Vector3(0f, 0.55f, 0f), new Vector3(1.9f, 0.6f, 4.2f), paint);
                    Part(root.transform, "cabin", new Vector3(0f, 1.15f, -0.4f), new Vector3(1.7f, 0.7f, 2.2f), glass);
                    AddWheel(root.transform, new Vector3(-0.95f, 0.35f, 1.4f));
                    AddWheel(root.transform, new Vector3(0.95f, 0.35f, 1.4f));
                    AddWheel(root.transform, new Vector3(-0.95f, 0.35f, -1.4f));
                    AddWheel(root.transform, new Vector3(0.95f, 0.35f, -1.4f));
                    break;
                case VehicleKind.Motorcycle:
                    Part(root.transform, "frame", new Vector3(0f, 0.7f, 0f), new Vector3(0.5f, 0.5f, 2f), paint);
                    Part(root.transform, "tank", new Vector3(0f, 1.0f, 0.2f), new Vector3(0.45f, 0.35f, 0.8f), dark);
                    AddWheel(root.transform, new Vector3(0f, 0.35f, 0.9f));
                    AddWheel(root.transform, new Vector3(0f, 0.35f, -0.9f));
                    break;
                case VehicleKind.Boat:
                    Part(root.transform, "hull", new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.6f, 5.2f), paint);
                    Part(root.transform, "deck", new Vector3(0f, 0.65f, -0.6f), new Vector3(1.8f, 0.3f, 2.4f), dark);
                    Part(root.transform, "windshield", new Vector3(0f, 1.05f, 0.6f), new Vector3(1.6f, 0.5f, 0.12f), glass);
                    break;
                case VehicleKind.Hover:
                    Part(root.transform, "pod", new Vector3(0f, 0.9f, 0f), new Vector3(1.8f, 0.5f, 3f), paint);
                    Part(root.transform, "canopy", new Vector3(0f, 1.35f, -0.2f), new Vector3(1.4f, 0.45f, 1.6f), glass);
                    Part(root.transform, "thrusterL", new Vector3(-1.2f, 0.5f, -0.8f), new Vector3(0.6f, 0.25f, 0.6f), dark);
                    Part(root.transform, "thrusterR", new Vector3(1.2f, 0.5f, -0.8f), new Vector3(0.6f, 0.25f, 0.6f), dark);
                    Part(root.transform, "glow", new Vector3(0f, 0.25f, 0f), new Vector3(2.4f, 0.12f, 3.4f), MaterialLibrary.Emissive(new Color(0.3f, 0.9f, 1f), 1.8f));
                    break;
            }
            var box = root.AddComponent<BoxCollider>();
            box.size = kind switch
            {
                VehicleKind.Car => new Vector3(2f, 1.6f, 4.3f),
                VehicleKind.Motorcycle => new Vector3(0.7f, 1.2f, 2.1f),
                VehicleKind.Boat => new Vector3(2.3f, 1.2f, 5.3f),
                _ => new Vector3(2.4f, 1.6f, 3.2f)
            };
            box.center = new Vector3(0f, box.size.y * 0.4f, 0f);
            return root;
        }

        static void Part(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void AddWheel(Transform parent, Vector3 pos)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(wheel.GetComponent<Collider>());
            wheel.name = "wheel";
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = pos;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.7f, 0.18f, 0.7f);
            wheel.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.1f, 0.1f, 0.11f), "wheel");
        }

        void TryEnter(VehicleBase vehicle)
        {
            if (_occupied != null) return;
            if (_player == null) return;
            _occupied = vehicle;
            vehicle.Enter(_player);
            if (_camera != null) _camera.Mode = CameraMode.Vehicle;
            _camera?.SetTarget(vehicle.transform, false);
            Log.Info("Vehicles", "vehicle entered");
        }

        public void ExitOccupied()
        {
            if (_occupied == null || _player == null) return;
            _occupied.Exit(_player, _camera);
            if (_camera != null)
            {
                _camera.Mode = CameraMode.Normal;
                _camera.SetTarget(_player.transform, false);
            }
            _occupied = null;
        }

        public void Tick(float delta)
        {
            if (_occupied != null && _input != null && _input.Active.ActionPressed)
                ExitOccupied();
        }

        public void Persist()
        {
            var list = SaveSystem.Current.worldObjects.vehicles;
            list.Clear();
            foreach (var v in _vehicles)
            {
                if (v == null || IsOwned(v.VehicleId)) continue;
                list.Add(new VehicleSaveEntry
                {
                    vehicleId = v.VehicleId,
                    kind = v.Kind.ToString(),
                    regionId = _streamer != null ? _streamer.RegionIdAt(v.transform.position) : "unknown",
                    x = v.transform.position.x, y = v.transform.position.y, z = v.transform.position.z,
                    rotationY = v.transform.eulerAngles.y,
                });
            }
        }

        public void Shutdown()
        {
            if (_streamer != null)
            {
                _streamer.RegionBuilt -= OnRegionBuilt;
                _streamer.RegionUnloaded -= OnRegionUnloaded;
            }
            Persist();
        }
    }
}
