using System;
using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;

namespace LoveGame.Home
{
    public enum FurnitureCategory { Bed, Sofa, Table, Chair, Lamp, Plant, Painting, Shelf, Electronics, Rug, Decoration, Outdoor }

    [Serializable]
    public class FurnitureDef
    {
        public string id;
        public string name;
        public string category;
        public string color = "#FFFFFF";
        public float footprint = 1f;
    }

    /// <summary>
    /// Home + furniture: enter/exit the beach house, place/rotate/move/delete furniture
    /// with grid snapping, persistence per home id. Architecture supports many homes
    /// (each Home POI in any region can host an interior).
    /// </summary>
    public sealed class FurnitureService : IGameService
    {
        public string ServiceName => "Furniture";

        public readonly Dictionary<string, FurnitureDef> Catalog = new Dictionary<string, FurnitureDef>();
        readonly Dictionary<string, GameObject> _placed = new Dictionary<string, GameObject>();
        Transform _homeRoot;
        ThirdPersonController _player;
        int _counter;
        public string ActiveHomeId { get; private set; } = "beach_house";

        public event System.Action Changed;

        public void Bind(ThirdPersonController player) => _player = player;

        public void Initialize()
        {
            var text = Resources.Load<TextAsset>("Data/furniture");
            if (text != null)
            {
                var file = JsonUtility.FromJson<FurnitureCatalogFile>(text.text);
                if (file?.furniture != null)
                    foreach (var f in file.furniture)
                        if (!string.IsNullOrEmpty(f.id)) Catalog[f.id] = f;
            }
            Log.Info("Home", $"{Catalog.Count} furniture definitions");
        }

        public void Tick(float delta) { }

        public void EnterHome(string homeId, Vector3 interiorOrigin)
        {
            ActiveHomeId = homeId;
            if (_homeRoot == null)
            {
                _homeRoot = new GameObject("~Home").transform;
                UnityEngine.Object.DontDestroyOnLoad(_homeRoot.gameObject);
            }
            bool hasSavedFurniture = false;
            foreach (var p in SaveSystem.Current.home.furniture)
                if (p.homeId == homeId) { hasSavedFurniture = true; break; }
            // defaults are only furnished on the very first visit
            HomeBuilder.BuildInterior(_homeRoot, homeId, interiorOrigin, placeDefaults: !hasSavedFurniture, this);
            foreach (var p in SaveSystem.Current.home.furniture)
            {
                if (p.homeId != homeId) continue;
                Place(p.furnitureId, new Vector3(p.x, p.y, p.z), p.rotationY, persist: false);
            }
            GameEvents.Publish(new NotificationEvent { Title = "Home", Body = "Welcome home.", Duration = 2.5f });
        }

        public void ExitHome()
        {
            if (_homeRoot == null) return;
            foreach (var go in _placed.Values) if (go != null) UnityEngine.Object.Destroy(go);
            _placed.Clear();
            for (int i = _homeRoot.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_homeRoot.GetChild(i).gameObject);
        }

        public GameObject Place(string furnitureId, Vector3 position, float rotationY = 0f, bool persist = true)
        {
            if (!Catalog.TryGetValue(furnitureId, out var def))
            {
                Log.Warn("Home", $"unknown furniture '{furnitureId}'");
                return null;
            }
            var go = FurnitureVisual.Build(def);
            go.transform.SetParent(_homeRoot, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            var key = $"{ActiveHomeId}:{furnitureId}:{_counter++}";
            _placed[key] = go;
            if (persist) PersistPlacements();
            Changed?.Invoke();
            return go;
        }

        /// <summary>Grid-snap placement used by the furniture UI drag mode.</summary>
        public GameObject PlaceSnapped(string furnitureId, Vector3 position, float rotationY = 0f)
        {
            var snapped = new Vector3(Mathf.Round(position.x / 0.5f) * 0.5f, position.y, Mathf.Round(position.z / 0.5f) * 0.5f);
            return Place(furnitureId, snapped, Mathf.Round(rotationY / 15f) * 15f);
        }

        public void RemoveNearest(Vector3 position, float maxDistance = 2.5f)
        {
            string best = null;
            float bestD = maxDistance;
            foreach (var kv in _placed)
            {
                if (kv.Value == null) continue;
                var d = (kv.Value.transform.position - position).magnitude;
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            if (best != null)
            {
                UnityEngine.Object.Destroy(_placed[best]);
                _placed.Remove(best);
                PersistPlacements();
                Changed?.Invoke();
            }
        }

        void PersistPlacements()
        {
            var list = SaveSystem.Current.home.furniture;
            list.Clear();
            foreach (var kv in _placed)
            {
                if (kv.Value == null) continue;
                list.Add(new FurniturePlacementSave
                {
                    furnitureId = kv.Value.name.Split('_')[1],
                    homeId = ActiveHomeId,
                    x = kv.Value.transform.position.x, y = kv.Value.transform.position.y, z = kv.Value.transform.position.z,
                    rotationY = kv.Value.transform.eulerAngles.y,
                });
            }
            SaveSystem.Save();
        }

        public void Shutdown() => ExitHome();
    }

    [Serializable]
    public class FurnitureCatalogFile { public FurnitureDef[] furniture; }

    /// <summary>Placeholder furniture visuals from primitives; replace with real assets later.</summary>
    public static class FurnitureVisual
    {
        public static GameObject Build(FurnitureDef def)
        {
            var root = new GameObject($"furn_{def.id}");
            var color = CoreMath.HexToColor(def.color);
            var wood = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.62f, 0.45f, 0.3f), "wood");
            var fabric = MaterialLibrary.Tinted(MaterialLibrary.Lit, color, def.id);
            var metal = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.55f, 0.57f, 0.6f), "metal");

            switch (def.category)
            {
                case "Bed":
                    Box(root, new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.4f, 2.1f), fabric);
                    Box(root, new Vector3(0f, 0.45f, -0.85f), new Vector3(1.4f, 0.2f, 0.4f), MaterialLibrary.Tinted(MaterialLibrary.Lit, Color.white, "pillow"));
                    break;
                case "Sofa":
                    Box(root, new Vector3(0f, 0.3f, 0f), new Vector3(1.9f, 0.5f, 0.85f), fabric);
                    Box(root, new Vector3(0f, 0.65f, -0.4f), new Vector3(1.9f, 0.6f, 0.22f), fabric);
                    break;
                case "Table":
                    Box(root, new Vector3(0f, 0.72f, 0f), new Vector3(1.3f, 0.08f, 0.85f), wood);
                    Box(root, new Vector3(-0.55f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), wood);
                    Box(root, new Vector3(0.55f, 0.36f, 0f), new Vector3(0.08f, 0.72f, 0.7f), wood);
                    break;
                case "Chair":
                    Box(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.07f, 0.5f), wood);
                    Box(root, new Vector3(0f, 0.72f, -0.22f), new Vector3(0.5f, 0.55f, 0.07f), wood);
                    break;
                case "Lamp":
                    Cylinder(root, new Vector3(0f, 0.8f, 0f), new Vector3(0.06f, 1.6f, 0.06f), metal);
                    SphereGlow(root, new Vector3(0f, 1.7f, 0f), 0.28f, new Color(1f, 0.85f, 0.55f));
                    break;
                case "Plant":
                    Cylinder(root, new Vector3(0f, 0.2f, 0f), new Vector3(0.22f, 0.4f, 0.22f), MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.7f, 0.55f, 0.4f), "pot"));
                    Sphere(root, new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.9f, 0.7f), MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.2f, 0.6f, 0.3f), "plant"));
                    break;
                case "Painting":
                    Box(root, new Vector3(0f, 1.6f, 0f), new Vector3(1.1f, 0.8f, 0.06f), fabric);
                    break;
                case "Shelf":
                    Box(root, new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 1.8f, 0.35f), wood);
                    break;
                case "Electronics":
                    Box(root, new Vector3(0f, 0.7f, 0f), new Vector3(1.2f, 0.75f, 0.1f), MaterialLibrary.Emissive(new Color(0.3f, 0.5f, 0.9f), 1.1f));
                    Box(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.3f), metal);
                    break;
                case "Rug":
                    Box(root, new Vector3(0f, 0.02f, 0f), new Vector3(2.2f, 0.04f, 1.6f), fabric);
                    break;
                case "Decoration":
                    Sphere(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.35f, 0.35f, 0.35f), fabric);
                    break;
                case "Outdoor":
                    Box(root, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.06f, 0.8f), wood);
                    Box(root, new Vector3(0f, 0.75f, -0.35f), new Vector3(0.8f, 0.65f, 0.1f), wood);
                    break;
                default:
                    Box(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), fabric);
                    break;
            }
            return root;
        }

        static void Box(GameObject root, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Cylinder(GameObject root, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Sphere(GameObject root, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void SphereGlow(GameObject root, Vector3 pos, float scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Emissive(color, 1.8f);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1.4f;
            light.range = 8f;
        }
    }

    /// <summary>Builds the beach-house interior blockout (rooms, floor, walls, windows).</summary>
    public static class HomeBuilder
    {
        public static void BuildInterior(Transform parent, string homeId, Vector3 origin, bool placeDefaults, FurnitureService furnitureService)
        {
            var root = new GameObject($"interior_{homeId}");
            root.transform.SetParent(parent, false);
            root.transform.position = origin;

            var floor = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.82f, 0.72f, 0.58f), "floor");
            var wall = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.95f, 0.93f, 0.88f), "wall");
            var windowMat = MaterialLibrary.Emissive(new Color(0.8f, 0.92f, 1f), 1.2f);

            // floor + ceiling
            Slab(root, new Vector3(0f, 0f, 0f), new Vector3(12f, 0.2f, 9f), floor);
            Slab(root, new Vector3(0f, 3.2f, 0f), new Vector3(12f, 0.2f, 9f), wall);
            // walls with window gaps (two segments per wall)
            Slab(root, new Vector3(-6f, 1.7f, -4.5f), new Vector3(0.2f, 3.2f, 9f), wall);
            Slab(root, new Vector3(6f, 1.7f, -4.5f), new Vector3(0.2f, 3.2f, 9f), wall);
            Slab(root, new Vector3(-2.2f, 1.7f, -4.5f), new Vector3(7.6f, 3.2f, 0.2f), wall);
            Slab(root, new Vector3(3.8f, 2.7f, -4.5f), new Vector3(4.4f, 1.0f, 0.2f), wall);
            Slab(root, new Vector3(3.8f, 0.5f, -4.5f), new Vector3(4.4f, 1.0f, 0.2f), wall);
            Slab(root, new Vector3(0f, 1.7f, 4.5f), new Vector3(12f, 3.2f, 0.2f), wall);
            // window glow panel in the gap
            Slab(root, new Vector3(3.8f, 1.7f, -4.55f), new Vector3(4.2f, 1.4f, 0.05f), windowMat);
            // bedroom divider
            Slab(root, new Vector3(-2.5f, 1.7f, 0f), new Vector3(0.2f, 3.2f, 5f), wall);

            // default furniture set (first visit only)
            if (placeDefaults && furnitureService != null)
            {
                furnitureService.PlaceSnapped("bed_queen", origin + new Vector3(-4f, 0.1f, -2.5f), 0f);
                furnitureService.PlaceSnapped("sofa_corner", origin + new Vector3(3.5f, 0.1f, 2f), 180f);
                furnitureService.PlaceSnapped("table_coffee", origin + new Vector3(3.5f, 0.1f, 0.2f), 0f);
                furnitureService.PlaceSnapped("lamp_floor", origin + new Vector3(5f, 0.1f, 3.5f), 0f);
                furnitureService.PlaceSnapped("plant_monstera", origin + new Vector3(-5.2f, 0.1f, 3.8f), 0f);
                furnitureService.PlaceSnapped("rug_round", origin + new Vector3(3.5f, 0.05f, 1f), 0f);
            }
        }

        static void Slab(GameObject root, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "slab";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
