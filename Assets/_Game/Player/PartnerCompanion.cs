using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Player
{
    /// <summary>
    /// The couple's partner: follows the player, idles alongside, positions for couple
    /// interactions, teleports when left behind. State hooks are multiplayer-ready - a future
    /// network session drives the same interface instead of the local AI.
    /// </summary>
    public sealed class PartnerCompanion : MonoBehaviour
    {
        public Transform FollowTarget { get; set; }
        public float followDistance = 2.6f;
        public float teleportDistance = 45f;
        public float walkSpeed = 4.6f;
        public float runSpeed = 7.2f;

        public bool Busy { get; set; }   // during couple interactions / activities

        CharacterController _cc;
        PlayerCharacter _visual;
        Vector3 _idleAnchor;
        float _idleTimer;

        void Awake()
        {
            _cc = gameObject.GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.32f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _visual = GetComponent<PlayerCharacter>();
        }

        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
        }

        void Update()
        {
            if (FollowTarget == null) return;
            float dt = Time.deltaTime;

            var toTarget = FollowTarget.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > teleportDistance)
            {
                Teleport(FollowTarget.position - FollowTarget.forward * 1.6f);
                return;
            }

            if (Busy)
            {
                // interaction manager drives the pose; just stand
                if (_visual != null && !_visual.InPose) _visual.PlayPose("photoPose", 9999f);
                return;
            }

            if (dist > followDistance)
            {
                float speed = dist > 8f ? runSpeed : walkSpeed;
                var dir = toTarget.normalized;
                // stride to the left of the player so they walk side by side
                var side = FollowTarget.right * 0.8f;
                var step = (dir + side * 0.25f).normalized;
                _cc.SimpleMove(step * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 6f * dt);
            }
            else
            {
                _idleTimer += dt;
                if (_idleTimer > 6f)
                {
                    _idleTimer = 0f;
                    if (_visual != null && Random.value < 0.4f) _visual.PlayPose("wave", 1.6f);
                }
                // face same direction as player while strolling
                if (toTarget.sqrMagnitude > 0.5f && dist < followDistance)
                    transform.rotation = Quaternion.Slerp(transform.rotation, FollowTarget.rotation, 2f * dt);
            }
        }

        /// <summary>Move to an interaction anchor point (couple interactions).</summary>
        public bool MoveToAnchor(Vector3 anchor, float tolerance = 0.4f)
        {
            var to = anchor - transform.position;
            to.y = 0f;
            if (to.magnitude <= tolerance)
            {
                _cc.SimpleMove(Vector3.zero);
                return true;
            }
            _cc.SimpleMove(to.normalized * runSpeed);
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
            return false;
        }
    }
}
