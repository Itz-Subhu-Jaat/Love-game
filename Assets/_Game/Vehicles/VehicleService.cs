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
                kind == VehicleKind.Boat ? new Color(0.96f, 0.94f, 0.90f) :
                kind == VehicleKind.Motorcycle ? new Color(0.92f, 0.22f, 0.24f) :
                kind == VehicleKind.Hover ? new Color(0.25f, 0.82f, 0.95f) : new Color(0.18f, 0.42f, 0.85f), "veh" + kind);
            var dark = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.12f, 0.13f, 0.16f), "vehdark");
            var interior = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.82f, 0.46f, 0.26f), "vehleather");
            var glass = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.45f, 0.65f, 0.82f, 0.65f), "vehglass");
            var headlight = MaterialLibrary.Emissive(new Color(1f, 1f, 0.92f), 3.0f);
            var taillight = MaterialLibrary.Emissive(new Color(1f, 0.15f, 0.15f), 2.5f);
            var chrome = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.85f, 0.88f, 0.92f), "vehchrome");
            var woodDeck = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.68f, 0.48f, 0.32f), "vehwood");

            switch (kind)
            {
                case VehicleKind.Car:
                    // Sleek Island Convertible Sports Car
                    Part(root.transform, "chassis", new Vector3(0f, 0.35f, 0f), new Vector3(1.95f, 0.30f, 4.3f), dark);
                    Part(root.transform, "body", new Vector3(0f, 0.62f, -0.1f), new Vector3(1.90f, 0.40f, 4.2f), paint);
                    Part(root.transform, "hood", new Vector3(0f, 0.70f, 1.1f), new Vector3(1.75f, 0.20f, 1.8f), paint, new Vector3(4f, 0f, 0f));
                    Part(root.transform, "grille", new Vector3(0f, 0.52f, 2.02f), new Vector3(1.3f, 0.20f, 0.08f), dark);
                    // Dual LED Headlights
                    Part(root.transform, "headlightL", new Vector3(-0.68f, 0.66f, 1.98f), new Vector3(0.34f, 0.14f, 0.10f), headlight);
                    Part(root.transform, "headlightR", new Vector3(0.68f, 0.66f, 1.98f), new Vector3(0.34f, 0.14f, 0.10f), headlight);
                    // Dual Red Taillights
                    Part(root.transform, "taillightL", new Vector3(-0.70f, 0.68f, -2.12f), new Vector3(0.36f, 0.12f, 0.08f), taillight);
                    Part(root.transform, "taillightR", new Vector3(0.70f, 0.68f, -2.12f), new Vector3(0.36f, 0.12f, 0.08f), taillight);
                    // Dual Chrome Exhaust Tips
                    Part(root.transform, "exhaustL", new Vector3(-0.52f, 0.32f, -2.16f), new Vector3(0.14f, 0.14f, 0.22f), chrome);
                    Part(root.transform, "exhaustR", new Vector3(0.52f, 0.32f, -2.16f), new Vector3(0.14f, 0.14f, 0.22f), chrome);
                    // Tinted Windshield
                    Part(root.transform, "windshield", new Vector3(0f, 1.02f, 0.42f), new Vector3(1.68f, 0.52f, 0.08f), glass, new Vector3(32f, 0f, 0f));
                    // Side Mirrors
                    Part(root.transform, "mirrorL", new Vector3(-1.02f, 0.88f, 0.38f), new Vector3(0.18f, 0.10f, 0.12f), dark);
                    Part(root.transform, "mirrorR", new Vector3(1.02f, 0.88f, 0.38f), new Vector3(0.18f, 0.10f, 0.12f), dark);
                    // Saddle Brown Leather Bucket Seats
                    Part(root.transform, "seatDriver", new Vector3(-0.42f, 0.68f, -0.25f), new Vector3(0.55f, 0.50f, 0.55f), interior);
                    Part(root.transform, "headrestDriver", new Vector3(-0.42f, 1.08f, -0.48f), new Vector3(0.32f, 0.22f, 0.14f), interior);
                    Part(root.transform, "seatPassenger", new Vector3(0.42f, 0.68f, -0.25f), new Vector3(0.55f, 0.50f, 0.55f), interior);
                    Part(root.transform, "headrestPassenger", new Vector3(0.42f, 1.08f, -0.48f), new Vector3(0.32f, 0.22f, 0.14f), interior);
                    // Steering Wheel
                    Part(root.transform, "steering", new Vector3(-0.42f, 0.88f, 0.20f), new Vector3(0.30f, 0.30f, 0.05f), dark, new Vector3(-25f, 0f, 0f));
                    // 4 Sports Alloy Wheels
                    AddWheel(root.transform, new Vector3(-0.96f, 0.35f, 1.35f), chrome);
                    AddWheel(root.transform, new Vector3(0.96f, 0.35f, 1.35f), chrome);
                    AddWheel(root.transform, new Vector3(-0.96f, 0.35f, -1.35f), chrome);
                    AddWheel(root.transform, new Vector3(0.96f, 0.35f, -1.35f), chrome);
                    break;

                case VehicleKind.Motorcycle:
                    // Anime Sportbike
                    Part(root.transform, "frame", new Vector3(0f, 0.65f, 0f), new Vector3(0.42f, 0.45f, 1.9f), paint);
                    Part(root.transform, "tank", new Vector3(0f, 0.95f, 0.25f), new Vector3(0.48f, 0.36f, 0.75f), paint);
                    Part(root.transform, "seatTandem", new Vector3(0f, 0.88f, -0.40f), new Vector3(0.38f, 0.22f, 0.75f), dark);
                    Part(root.transform, "fairing", new Vector3(0f, 0.95f, 0.85f), new Vector3(0.44f, 0.45f, 0.45f), paint, new Vector3(-20f, 0f, 0f));
                    Part(root.transform, "headlight", new Vector3(0f, 0.90f, 1.08f), new Vector3(0.28f, 0.16f, 0.10f), headlight);
                    Part(root.transform, "windscreen", new Vector3(0f, 1.18f, 0.75f), new Vector3(0.36f, 0.28f, 0.06f), glass, new Vector3(40f, 0f, 0f));
                    Part(root.transform, "handlebars", new Vector3(0f, 1.08f, 0.55f), new Vector3(0.78f, 0.08f, 0.08f), chrome);
                    Part(root.transform, "exhaust", new Vector3(0.26f, 0.42f, -0.45f), new Vector3(0.12f, 0.12f, 0.95f), chrome, new Vector3(10f, 0f, 0f));
                    Part(root.transform, "taillight", new Vector3(0f, 0.95f, -0.92f), new Vector3(0.20f, 0.10f, 0.08f), taillight);
                    AddWheel(root.transform, new Vector3(0f, 0.38f, 0.95f), chrome);
                    AddWheel(root.transform, new Vector3(0f, 0.38f, -0.95f), chrome);
                    break;

                case VehicleKind.Boat:
                    // Streamlined Speedboat with Teak Trim
                    Part(root.transform, "hullBase", new Vector3(0f, 0.25f, 0f), new Vector3(2.3f, 0.55f, 5.4f), paint);
                    Part(root.transform, "bowTaper", new Vector3(0f, 0.40f, 2.2f), new Vector3(1.8f, 0.45f, 1.6f), paint, new Vector3(12f, 0f, 0f));
                    Part(root.transform, "teakTrim", new Vector3(0f, 0.56f, 0f), new Vector3(2.35f, 0.08f, 5.45f), woodDeck);
                    Part(root.transform, "cockpit", new Vector3(0f, 0.52f, -0.3f), new Vector3(1.75f, 0.30f, 2.6f), interior);
                    Part(root.transform, "windshield", new Vector3(0f, 0.98f, 0.8f), new Vector3(1.70f, 0.48f, 0.10f), glass, new Vector3(32f, 0f, 0f));
                    Part(root.transform, "outboardL", new Vector3(-0.6f, 0.35f, -2.85f), new Vector3(0.32f, 0.70f, 0.45f), dark);
                    Part(root.transform, "outboardR", new Vector3(0.6f, 0.35f, -2.85f), new Vector3(0.32f, 0.70f, 0.45f), dark);
                    break;

                case VehicleKind.Hover:
                    // Sleek Sci-fi Hover Speeder
                    Part(root.transform, "pod", new Vector3(0f, 0.85f, 0f), new Vector3(1.9f, 0.48f, 3.2f), paint);
                    Part(root.transform, "canopy", new Vector3(0f, 1.25f, -0.2f), new Vector3(1.4f, 0.42f, 1.7f), glass);
                    Part(root.transform, "headlight", new Vector3(0f, 0.85f, 1.58f), new Vector3(0.6f, 0.15f, 0.10f), headlight);
                    Part(root.transform, "thrusterL", new Vector3(-1.25f, 0.55f, -0.8f), new Vector3(0.55f, 0.30f, 0.85f), dark);
                    Part(root.transform, "thrusterR", new Vector3(1.25f, 0.55f, -0.8f), new Vector3(0.55f, 0.30f, 0.85f), dark);
                    Part(root.transform, "glowRing", new Vector3(0f, 0.28f, 0f), new Vector3(2.5f, 0.10f, 3.6f), MaterialLibrary.Emissive(new Color(0.2f, 0.9f, 1f), 2.2f));
                    break;
            }
            var box = root.AddComponent<BoxCollider>();
            box.size = kind switch
            {
                VehicleKind.Car => new Vector3(2.1f, 1.5f, 4.4f),
                VehicleKind.Motorcycle => new Vector3(0.8f, 1.3f, 2.2f),
                VehicleKind.Boat => new Vector3(2.4f, 1.2f, 5.5f),
                _ => new Vector3(2.5f, 1.5f, 3.4f)
            };
            box.center = new Vector3(0f, box.size.y * 0.45f, 0f);
            return root;
        }

        static void Part(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = scale;
            if (rot.HasValue) t.localEulerAngles = rot.Value;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void AddWheel(Transform parent, Vector3 pos, Material rimMat = null)
        {
            var wheelGo = new GameObject("wheel");
            var t = wheelGo.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;

            // Black rubber tire
            var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(tire.GetComponent<Collider>());
            tire.name = "tire";
            tire.transform.SetParent(t, false);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = new Vector3(0.72f, 0.22f, 0.72f);
            tire.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.12f, 0.12f, 0.14f), "wheeltire");

            // Metallic alloy rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(rim.GetComponent<Collider>());
            rim.name = "rim";
            rim.transform.SetParent(t, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(0.48f, 0.23f, 0.48f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = rimMat ?? MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.85f, 0.88f, 0.92f), "wheelrim");
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
