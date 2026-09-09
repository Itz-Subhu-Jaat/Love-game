using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Player
{
    /// <summary>
    /// Modular character visuals: procedural placeholder rig (capsule torso, sphere head,
    /// articulated arm/leg pivots) with slots for final assets. Clothing/hair/etc. attach
    /// points exist so imported models replace visuals without touching gameplay code.
    /// Locomotion animation is procedural until real clips arrive - the pose API stays stable.
    /// </summary>
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [Header("Identity")]
        public string characterName = "Player";
        public bool isPartner;

        [Header("Colors")]
        public Color outfitColor = new Color(0.29f, 0.5f, 0.9f);
        public Color accentColor = new Color(1f, 0.42f, 0.5f);
        public Color skinColor = new Color(0.96f, 0.8f, 0.68f);

        // slots (future final assets attach here)
        public Transform BodySlot { get; private set; }
        public Transform HeadSlot { get; private set; }
        public Transform HairSlot { get; private set; }
        public Transform LeftHandSlot { get; private set; }
        public Transform RightHandSlot { get; private set; }
        public Transform AccessorySlot { get; private set; }

        // procedural rig
        Transform _root, _torso, _head, _armL, _armR, _legL, _legR;
        float _animPhase;
        readonly Dictionary<string, Pose> _poses = new Dictionary<string, Pose>();
        string _currentPose;
        float _poseBlend;
        float _poseHold;
        ThirdPersonController _controller;

        /// <summary>Replaceable model prefab hook - assign a final character model later.</summary>
        public GameObject customModelPrefab;

        void Awake()
        {
            _controller = GetComponent<ThirdPersonController>();
            BuildRig();
            RegisterDefaultPoses();
            if (customModelPrefab != null) AttachModel(customModelPrefab);
        }

        void BuildRig()
        {
            _root = new GameObject("rig").transform;
            _root.SetParent(transform, false);

            // Palette setup
            Color skinCol = isPartner ? new Color(1.0f, 0.89f, 0.82f) : new Color(0.98f, 0.85f, 0.75f);
            Color hairCol = isPartner ? new Color(0.96f, 0.64f, 0.70f) : new Color(0.18f, 0.16f, 0.20f);
            Color outfitMainCol = isPartner ? new Color(1.0f, 0.44f, 0.62f) : new Color(0.16f, 0.28f, 0.50f);
            Color outfitAccentCol = isPartner ? new Color(0.98f, 0.98f, 1.0f) : new Color(0.95f, 0.95f, 0.98f);
            Color pantsCol = isPartner ? new Color(0.18f, 0.16f, 0.22f) : new Color(0.13f, 0.16f, 0.24f);
            Color shoesCol = isPartner ? new Color(0.92f, 0.35f, 0.50f) : new Color(0.85f, 0.22f, 0.26f);
            Color soleCol = new Color(0.96f, 0.96f, 0.96f);
            Color eyesCol = isPartner ? new Color(0.72f, 0.35f, 0.88f) : new Color(0.18f, 0.65f, 0.96f);
            Color goldCol = new Color(0.96f, 0.78f, 0.24f);
            Color blushCol = new Color(1.0f, 0.52f, 0.64f);

            string pfx = isPartner ? "ptn_" : "ply_";
            var matSkin = MaterialLibrary.Tinted(MaterialLibrary.Lit, skinCol, pfx + "skin");
            var matHair = MaterialLibrary.Tinted(MaterialLibrary.Lit, hairCol, pfx + "hair");
            var matOutfit = MaterialLibrary.Tinted(MaterialLibrary.Lit, outfitMainCol, pfx + "outfit");
            var matAccent = MaterialLibrary.Tinted(MaterialLibrary.Lit, outfitAccentCol, pfx + "accent");
            var matPants = MaterialLibrary.Tinted(MaterialLibrary.Lit, pantsCol, pfx + "pants");
            var matShoes = MaterialLibrary.Tinted(MaterialLibrary.Lit, shoesCol, pfx + "shoes");
            var matSole = MaterialLibrary.Tinted(MaterialLibrary.Lit, soleCol, "chr_sole");
            var matEyeWhite = MaterialLibrary.Tinted(MaterialLibrary.Lit, Color.white, "chr_eyewhite");
            var matEyeIris = MaterialLibrary.Tinted(MaterialLibrary.Lit, eyesCol, pfx + "eyeIris");
            var matEyePupil = MaterialLibrary.Tinted(MaterialLibrary.Lit, new Color(0.06f, 0.06f, 0.10f), "chr_pupil");
            var matBlush = MaterialLibrary.Tinted(MaterialLibrary.Lit, blushCol, "chr_blush");
            var matGold = MaterialLibrary.Tinted(MaterialLibrary.Lit, goldCol, "chr_gold");

            // --- Torso & Clothing ---
            _torso = Pivot(_root, "torso", new Vector3(0f, 0.82f, 0f));
            Prim(_torso, "pelvis", PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.14f, 0.23f), matPants, new Vector3(0f, 0f, 90f));
            Prim(_torso, "waist", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.08f, 0.24f), matOutfit);
            Prim(_torso, "chest", PrimitiveType.Capsule, new Vector3(0f, 0.36f, 0f), new Vector3(0.40f, 0.22f, 0.25f), matOutfit);
            Prim(_torso, "neck", PrimitiveType.Cylinder, new Vector3(0f, 0.58f, 0f), new Vector3(0.12f, 0.12f, 0.12f), matSkin);

            if (isPartner)
            {
                // Female outfit: cute flared skirt, frills, chest ribbon bow, and golden brooch
                Prim(_torso, "skirtUpper", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(0.44f, 0.16f, 0.36f), matOutfit);
                Prim(_torso, "skirtFrill", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), new Vector3(0.48f, 0.06f, 0.40f), matAccent);
                Prim(_torso, "blouseCollar", PrimitiveType.Cube, new Vector3(0f, 0.51f, 0.02f), new Vector3(0.24f, 0.06f, 0.20f), matAccent);
                Prim(_torso, "bowL", PrimitiveType.Cube, new Vector3(-0.05f, 0.42f, 0.14f), new Vector3(0.08f, 0.06f, 0.03f), matOutfit, new Vector3(0f, 0f, 25f));
                Prim(_torso, "bowR", PrimitiveType.Cube, new Vector3(0.05f, 0.42f, 0.14f), new Vector3(0.08f, 0.06f, 0.03f), matOutfit, new Vector3(0f, 0f, -25f));
                Prim(_torso, "brooch", PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0.155f), new Vector3(0.045f, 0.045f, 0.03f), matGold);
            }
            else
            {
                // Male outfit: stylish casual jacket, inner graphic tee, upturned collar, and golden belt buckle
                Prim(_torso, "innerShirt", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.128f), new Vector3(0.16f, 0.30f, 0.02f), matAccent);
                Prim(_torso, "collar", PrimitiveType.Cube, new Vector3(0f, 0.52f, -0.01f), new Vector3(0.26f, 0.08f, 0.22f), matOutfit);
                Prim(_torso, "buckle", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0.125f), new Vector3(0.08f, 0.06f, 0.02f), matGold);
            }

            // --- Head & Stylized Anime Face ---
            _head = Pivot(_root, "head", new Vector3(0f, 1.48f, 0f));
            Prim(_head, "face", PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.38f, 0.35f), matSkin);

            // Anime Eyes (Expressive, sparkling with highlights)
            Prim(_head, "eyeWhiteL", PrimitiveType.Cube, new Vector3(-0.082f, 0.18f, 0.160f), new Vector3(0.065f, 0.075f, 0.015f), matEyeWhite, new Vector3(0f, -8f, 0f));
            Prim(_head, "eyeWhiteR", PrimitiveType.Cube, new Vector3(0.082f, 0.18f, 0.160f), new Vector3(0.065f, 0.075f, 0.015f), matEyeWhite, new Vector3(0f, 8f, 0f));
            Prim(_head, "eyeIrisL", PrimitiveType.Cube, new Vector3(-0.082f, 0.176f, 0.168f), new Vector3(0.048f, 0.055f, 0.012f), matEyeIris, new Vector3(0f, -8f, 0f));
            Prim(_head, "eyeIrisR", PrimitiveType.Cube, new Vector3(0.082f, 0.176f, 0.168f), new Vector3(0.048f, 0.055f, 0.012f), matEyeIris, new Vector3(0f, 8f, 0f));
            Prim(_head, "pupilL", PrimitiveType.Cube, new Vector3(-0.082f, 0.176f, 0.173f), new Vector3(0.025f, 0.032f, 0.008f), matEyePupil);
            Prim(_head, "pupilR", PrimitiveType.Cube, new Vector3(0.082f, 0.176f, 0.173f), new Vector3(0.025f, 0.032f, 0.008f), matEyePupil);
            Prim(_head, "sparkleL1", PrimitiveType.Sphere, new Vector3(-0.072f, 0.193f, 0.176f), new Vector3(0.018f, 0.018f, 0.01f), matEyeWhite);
            Prim(_head, "sparkleR1", PrimitiveType.Sphere, new Vector3(0.092f, 0.193f, 0.176f), new Vector3(0.018f, 0.018f, 0.01f), matEyeWhite);
            Prim(_head, "sparkleL2", PrimitiveType.Sphere, new Vector3(-0.088f, 0.163f, 0.175f), new Vector3(0.011f, 0.011f, 0.008f), matEyeWhite);
            Prim(_head, "sparkleR2", PrimitiveType.Sphere, new Vector3(0.076f, 0.163f, 0.175f), new Vector3(0.011f, 0.011f, 0.008f), matEyeWhite);
            Prim(_head, "browL", PrimitiveType.Cube, new Vector3(-0.082f, 0.235f, 0.155f), new Vector3(0.065f, 0.015f, 0.015f), matHair, new Vector3(0f, -8f, -6f));
            Prim(_head, "browR", PrimitiveType.Cube, new Vector3(0.082f, 0.235f, 0.155f), new Vector3(0.065f, 0.015f, 0.015f), matHair, new Vector3(0f, 8f, 6f));

            // Cheeks blush & smile
            Prim(_head, "blushL", PrimitiveType.Sphere, new Vector3(-0.125f, 0.128f, 0.148f), new Vector3(0.055f, 0.025f, 0.02f), matBlush);
            Prim(_head, "blushR", PrimitiveType.Sphere, new Vector3(0.125f, 0.128f, 0.148f), new Vector3(0.055f, 0.025f, 0.02f), matBlush);
            Prim(_head, "smile", PrimitiveType.Cube, new Vector3(0f, 0.108f, 0.168f), new Vector3(0.045f, 0.012f, 0.012f), matBlush);

            // Hair Volume & Styling
            var hairCap = Prim(_head, "hairCap", PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.02f), new Vector3(0.39f, 0.35f, 0.39f), matHair);
            Prim(_head, "hairBack", PrimitiveType.Sphere, new Vector3(0f, 0.14f, -0.10f), new Vector3(0.37f, 0.32f, 0.30f), matHair);
            // Fringe bangs
            Prim(_head, "bangMid", PrimitiveType.Capsule, new Vector3(0f, 0.27f, 0.145f), new Vector3(0.08f, 0.10f, 0.06f), matHair, new Vector3(25f, 0f, 0f));
            Prim(_head, "bangL", PrimitiveType.Capsule, new Vector3(-0.09f, 0.25f, 0.135f), new Vector3(0.07f, 0.11f, 0.06f), matHair, new Vector3(22f, -15f, 15f));
            Prim(_head, "bangR", PrimitiveType.Capsule, new Vector3(0.09f, 0.25f, 0.135f), new Vector3(0.07f, 0.11f, 0.06f), matHair, new Vector3(22f, 15f, -15f));
            // Side locks
            Prim(_head, "sideLockL", PrimitiveType.Capsule, new Vector3(-0.18f, 0.13f, 0.06f), new Vector3(0.06f, 0.18f, 0.06f), matHair, new Vector3(5f, 0f, -8f));
            Prim(_head, "sideLockR", PrimitiveType.Capsule, new Vector3(0.18f, 0.13f, 0.06f), new Vector3(0.06f, 0.18f, 0.06f), matHair, new Vector3(5f, 0f, 8f));

            if (isPartner)
            {
                // Partner Twin-tails with cute hair ribbon ties
                Prim(_head, "ribbonL", PrimitiveType.Sphere, new Vector3(-0.21f, 0.26f, -0.09f), new Vector3(0.07f, 0.07f, 0.07f), matOutfit);
                Prim(_head, "tailL", PrimitiveType.Capsule, new Vector3(-0.26f, 0.09f, -0.12f), new Vector3(0.11f, 0.26f, 0.11f), matHair, new Vector3(15f, -15f, 25f));
                Prim(_head, "ribbonR", PrimitiveType.Sphere, new Vector3(0.21f, 0.26f, -0.09f), new Vector3(0.07f, 0.07f, 0.07f), matOutfit);
                Prim(_head, "tailR", PrimitiveType.Capsule, new Vector3(0.26f, 0.09f, -0.12f), new Vector3(0.11f, 0.26f, 0.11f), matHair, new Vector3(15f, 15f, -25f));
                Prim(_head, "hairClip", PrimitiveType.Sphere, new Vector3(-0.14f, 0.32f, 0.11f), new Vector3(0.06f, 0.06f, 0.03f), matGold);
            }
            else
            {
                // Player stylish anime hair tufts & swept locks
                Prim(_head, "tuft", PrimitiveType.Capsule, new Vector3(0.02f, 0.39f, 0.02f), new Vector3(0.06f, 0.11f, 0.06f), matHair, new Vector3(-15f, 10f, 18f));
                Prim(_head, "sweptBack", PrimitiveType.Capsule, new Vector3(0f, 0.26f, -0.16f), new Vector3(0.10f, 0.15f, 0.08f), matHair, new Vector3(-38f, 0f, 0f));
            }

            // --- Arms & Hands ---
            _armL = Pivot(_root, "armL", new Vector3(-0.28f, 1.34f, 0f));
            BuildArm(_armL, true, matOutfit, isPartner ? matSkin : matOutfit, matAccent, matSkin);

            _armR = Pivot(_root, "armR", new Vector3(0.28f, 1.34f, 0f));
            BuildArm(_armR, false, matOutfit, isPartner ? matSkin : matOutfit, matAccent, matSkin);

            // --- Legs & Stylized Sneakers ---
            _legL = Pivot(_root, "legL", new Vector3(-0.13f, 0.82f, 0f));
            BuildLeg(_legL, matPants, matAccent, matShoes, matSole);

            _legR = Pivot(_root, "legR", new Vector3(0.13f, 0.82f, 0f));
            BuildLeg(_legR, matPants, matAccent, matShoes, matSole);

            BodySlot = _torso;
            HeadSlot = _head;
            HairSlot = hairCap;
            LeftHandSlot = _armL;
            RightHandSlot = _armR;
            AccessorySlot = _root;
        }

        static void BuildArm(Transform parent, bool isLeft, Material matShoulder, Material matArm, Material matCuff, Material matSkin)
        {
            Prim(parent, "shoulder", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), matShoulder);
            Prim(parent, "upperArm", PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.12f, 0.15f, 0.12f), matShoulder);
            Prim(parent, "elbow", PrimitiveType.Sphere, new Vector3(0f, -0.29f, 0f), new Vector3(0.11f, 0.11f, 0.11f), matShoulder);
            Prim(parent, "forearm", PrimitiveType.Capsule, new Vector3(0f, -0.42f, 0f), new Vector3(0.10f, 0.14f, 0.10f), matArm);
            Prim(parent, "cuff", PrimitiveType.Cylinder, new Vector3(0f, -0.49f, 0f), new Vector3(0.115f, 0.04f, 0.115f), matCuff);
            Prim(parent, "hand", PrimitiveType.Sphere, new Vector3(0f, -0.56f, 0.01f), new Vector3(0.09f, 0.11f, 0.08f), matSkin);
            Prim(parent, "thumb", PrimitiveType.Capsule, new Vector3(isLeft ? 0.04f : -0.04f, -0.54f, 0.025f), new Vector3(0.035f, 0.045f, 0.035f), matSkin, new Vector3(0f, 0f, isLeft ? -25f : 25f));
        }

        static void BuildLeg(Transform parent, Material matPants, Material matSock, Material matShoe, Material matSole)
        {
            Prim(parent, "thigh", PrimitiveType.Capsule, new Vector3(0f, -0.19f, 0f), new Vector3(0.15f, 0.20f, 0.15f), matPants);
            Prim(parent, "knee", PrimitiveType.Sphere, new Vector3(0f, -0.38f, 0f), new Vector3(0.14f, 0.14f, 0.14f), matPants);
            Prim(parent, "calf", PrimitiveType.Capsule, new Vector3(0f, -0.56f, 0f), new Vector3(0.13f, 0.18f, 0.13f), matPants);
            Prim(parent, "sock", PrimitiveType.Cylinder, new Vector3(0f, -0.71f, 0.01f), new Vector3(0.13f, 0.06f, 0.13f), matSock);
            // Sneakers with stylish white rubber soles and toe caps
            Prim(parent, "shoe", PrimitiveType.Cube, new Vector3(0f, -0.75f, 0.04f), new Vector3(0.15f, 0.10f, 0.24f), matShoe);
            Prim(parent, "sole", PrimitiveType.Cube, new Vector3(0f, -0.785f, 0.04f), new Vector3(0.16f, 0.035f, 0.25f), matSole);
            Prim(parent, "toeCap", PrimitiveType.Sphere, new Vector3(0f, -0.75f, 0.12f), new Vector3(0.14f, 0.08f, 0.09f), matSock);
        }

        static Transform Prim(Transform parent, string name, PrimitiveType prim, Vector3 pos, Vector3 scale, Material mat, Vector3? rot = null)
        {
            var go = GameObject.CreatePrimitive(prim);
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = scale;
            if (rot.HasValue) t.localEulerAngles = rot.Value;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return t;
        }

        static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        /// <summary>Swap in a final character model: disables the placeholder rig, keeps slots.</summary>
        public void AttachModel(GameObject prefab)
        {
            var instance = Instantiate(prefab, _root);
            _root.Find("torso")?.gameObject.SetActive(false);
            _root.Find("head")?.gameObject.SetActive(false);
            _root.Find("armL")?.gameObject.SetActive(false);
            _root.Find("armR")?.gameObject.SetActive(false);
            _root.Find("legL")?.gameObject.SetActive(false);
            _root.Find("legR")?.gameObject.SetActive(false);
            Log.Info("Character", $"{characterName}: custom model attached");
        }

        // ------------------------------------------------------------- poses

        public sealed class Pose
        {
            public string Id;
            public Vector3 ArmL = new Vector3(0f, 0f, 4f);
            public Vector3 ArmR = new Vector3(0f, 0f, -4f);
            public Vector3 TorsoTilt;
            public float RootYOffset;
            public Vector3 LegL = new Vector3(4f, 0f, 0f);
            public Vector3 LegR = new Vector3(-4f, 0f, 0f);
            public float Duration = 2.5f;
            public bool Loops;
        }

        void RegisterDefaultPoses()
        {
            AddPose(new Pose { Id = "wave", ArmR = new Vector3(0f, 165f, 0f), Duration = 2.2f, Loops = true });
            AddPose(new Pose { Id = "dance", ArmL = new Vector3(40f, 0f, 30f), ArmR = new Vector3(-40f, 0f, -30f), TorsoTilt = new Vector3(0f, 0f, 8f), LegL = new Vector3(0f, 25f, 0f), LegR = new Vector3(0f, -25f, 0f), Duration = 6f, Loops = true });
            AddPose(new Pose { Id = "sit", RootYOffset = -0.62f, LegL = new Vector3(-85f, 0f, 0f), LegR = new Vector3(-85f, 0f, 0f), ArmL = new Vector3(-25f, 0f, 8f), ArmR = new Vector3(25f, 0f, -8f), Duration = 9999f });
            AddPose(new Pose { Id = "hug", ArmL = new Vector3(-50f, 25f, 55f), ArmR = new Vector3(50f, -25f, -55f), TorsoTilt = new Vector3(6f, 0f, 0f), Duration = 3.5f });
            AddPose(new Pose { Id = "kiss", TorsoTilt = new Vector3(14f, 0f, 0f), RootYOffset = 0.04f, ArmL = new Vector3(-45f, 15f, 40f), ArmR = new Vector3(45f, -15f, -40f), Duration = 2.4f });
            AddPose(new Pose { Id = "highFive", ArmL = new Vector3(0f, 150f, 0f), ArmR = new Vector3(0f, 150f, 0f), Duration = 1.6f });
            AddPose(new Pose { Id = "holdHands", ArmR = new Vector3(18f, 8f, 42f), ArmL = new Vector3(-12f, 5f, 10f), Duration = 9999f });
            AddPose(new Pose { Id = "point", ArmR = new Vector3(-88f, 0f, 0f), Duration = 1.8f });
            AddPose(new Pose { Id = "photoPose", ArmL = new Vector3(-15f, 8f, 32f), ArmR = new Vector3(15f, -8f, -32f), TorsoTilt = new Vector3(0f, 0f, -5f), Duration = 9999f });
            AddPose(new Pose { Id = "celebrate", ArmL = new Vector3(0f, 175f, 20f), ArmR = new Vector3(0f, 175f, -20f), RootYOffset = 0.15f, Duration = 2.6f, Loops = true });
            AddPose(new Pose { Id = "sleep", RootYOffset = -0.62f, TorsoTilt = new Vector3(0f, 0f, 90f), LegL = new Vector3(-85f, 0f, 0f), LegR = new Vector3(-85f, 0f, 0f), Duration = 9999f });
        }

        public void AddPose(Pose pose) => _poses[pose.Id] = pose;
        public bool HasPose(string id) => _poses.ContainsKey(id);

        public void PlayPose(string id, float duration = 0f)
        {
            if (!_poses.TryGetValue(id, out var pose)) { Log.Warn("Character", $"pose '{id}' missing on {characterName}"); return; }
            _currentPose = id;
            _poseHold = duration > 0f ? duration : pose.Duration;
            _poseBlend = 0f;
        }

        public void ClearPose() => _currentPose = null;

        public bool InPose => _currentPose != null;

        void Update()
        {
            float dt = Time.deltaTime;
            Pose pose = default;
            bool inPose = _currentPose != null && _poses.TryGetValue(_currentPose, out pose);

            if (inPose)
            {
                _poseHold -= dt;
                if (_poseHold <= 0f && !pose.Loops) { _currentPose = null; inPose = false; }
            }

            if (inPose)
            {
                _poseBlend = Mathf.MoveTowards(_poseBlend, 1f, dt * 5f);
                var t = Time.time * (pose.Loops ? 3f : 1.5f);
                float b = _poseBlend;
                _armL.localRotation = Quaternion.Slerp(_armL.localRotation, Quaternion.Euler(pose.ArmL + (pose.Loops ? new Vector3(0f, Mathf.Sin(t) * 12f, 0f) : Vector3.zero)), b);
                _armR.localRotation = Quaternion.Slerp(_armR.localRotation, Quaternion.Euler(pose.ArmR + (pose.Loops ? new Vector3(0f, Mathf.Sin(t + 1f) * 12f, 0f) : Vector3.zero)), b);
                _legL.localRotation = Quaternion.Slerp(_legL.localRotation, Quaternion.Euler(pose.LegL), b);
                _legR.localRotation = Quaternion.Slerp(_legR.localRotation, Quaternion.Euler(pose.LegR), b);
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, Quaternion.Euler(pose.TorsoTilt), b);
                _root.localPosition = Vector3.Slerp(_root.localPosition, new Vector3(0f, pose.RootYOffset, 0f), b);
                return;
            }

            // ---- procedural locomotion
            _poseBlend = 0f;
            _root.localPosition = Vector3.zero;
            _torso.localRotation = Quaternion.identity;

            float speed = _controller != null ? new Vector3(_controller.Velocity.x, 0f, _controller.Velocity.z).magnitude : 0f;
            bool swimming = _controller != null && _controller.IsSwimming;
            _animPhase += dt * Mathf.Clamp(speed * 1.6f, 0f, 12f);

            if (swimming)
            {
                float swing = Mathf.Sin(_animPhase * 0.7f);
                _armL.localRotation = Quaternion.Euler(-70f + swing * 30f, 0f, 25f);
                _armR.localRotation = Quaternion.Euler(-70f - swing * 30f, 0f, -25f);
                _legL.localRotation = Quaternion.Euler(swing * 18f, 0f, 0f);
                _legR.localRotation = Quaternion.Euler(-swing * 18f, 0f, 0f);
                return;
            }

            if (speed > 0.2f)
            {
                float amp = Mathf.Clamp(speed / jogAmp, 0.25f, 1.6f);
                float swing = Mathf.Sin(_animPhase) * 34f * amp;
                float counter = Mathf.Cos(_animPhase) * 24f * amp;
                _legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
                _legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
                _armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, 4f);
                _armR.localRotation = Quaternion.Euler(swing * 0.8f, 0f, -4f);
                _torso.localRotation = Quaternion.Euler(amp * 4f, counter * 0.12f, 0f);
                if (_head != null) _head.localRotation = Quaternion.Euler(-amp * 2f, 0f, 0f);
            }
            else
            {
                float idle = Mathf.Sin(Time.time * 1.4f) * 2.4f;
                _legL.localRotation = Quaternion.identity;
                _legR.localRotation = Quaternion.identity;
                _armL.localRotation = Quaternion.Euler(2f + idle, 0f, 5f);
                _armR.localRotation = Quaternion.Euler(2f - idle, 0f, -5f);
                _torso.localRotation = Quaternion.Euler(0f, 0f, idle * 0.2f);
                if (_head != null) _head.localRotation = Quaternion.Euler(0f, 0f, -idle * 0.15f);
            }
        }

        const float jogAmp = 4.2f;

        /// <summary>Facing helper used by couple interactions to align two characters.</summary>
        public void FaceTowards(Vector3 point)
        {
            var dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        }
    }
}
