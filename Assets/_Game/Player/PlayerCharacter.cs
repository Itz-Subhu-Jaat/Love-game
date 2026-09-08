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

            var outfit = MaterialLibrary.Tinted(MaterialLibrary.Lit, outfitColor, isPartner ? "partnerOutfit" : "playerOutfit");
            var accent = MaterialLibrary.Tinted(MaterialLibrary.Lit, accentColor, isPartner ? "partnerAccent" : "playerAccent");
            var skin = MaterialLibrary.Tinted(MaterialLibrary.Lit, skinColor, isPartner ? "partnerSkin" : "playerSkin");

            _torso = Part(_root, "torso", new Vector3(0f, 1.15f, 0f), new Vector3(0.44f, 0.62f, 0.26f), outfit);
            _head = Part(_root, "head", new Vector3(0f, 1.72f, 0f), new Vector3(0.34f, 0.34f, 0.34f), skin);
            Part(_head, "hairCap", new Vector3(0f, 0.08f, -0.02f), new Vector3(0.36f, 0.16f, 0.36f), accent);

            _armL = Pivot(_root, "armL", new Vector3(-0.33f, 1.48f, 0f));
            Part(_armL, "limb", new Vector3(0f, -0.26f, 0f), new Vector3(0.13f, 0.55f, 0.13f), outfit);
            _armR = Pivot(_root, "armR", new Vector3(0.33f, 1.48f, 0f));
            Part(_armR, "limb", new Vector3(0f, -0.26f, 0f), new Vector3(0.13f, 0.55f, 0.13f), outfit);

            _legL = Pivot(_root, "legL", new Vector3(-0.14f, 0.62f, 0f));
            Part(_legL, "limb", new Vector3(0f, -0.3f, 0f), new Vector3(0.16f, 0.62f, 0.16f), accent);
            _legR = Pivot(_root, "legR", new Vector3(0.14f, 0.62f, 0f));
            Part(_legR, "limb", new Vector3(0f, -0.3f, 0f), new Vector3(0.16f, 0.62f, 0.16f), accent);

            BodySlot = _torso;
            HeadSlot = _head;
            HairSlot = _head.Find("hairCap");
            LeftHandSlot = _armL;
            RightHandSlot = _armR;
            AccessorySlot = _root;
        }

        static Transform Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
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
            bool inPose = _currentPose != null && _poses.TryGetValue(_currentPose, out var pose);

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
            }
            else
            {
                float idle = Mathf.Sin(Time.time * 1.4f) * 2.4f;
                _legL.localRotation = Quaternion.identity;
                _legR.localRotation = Quaternion.identity;
                _armL.localRotation = Quaternion.Euler(2f + idle, 0f, 5f);
                _armR.localRotation = Quaternion.Euler(2f - idle, 0f, -5f);
                _torso.localRotation = Quaternion.Euler(0f, 0f, idle * 0.2f);
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
