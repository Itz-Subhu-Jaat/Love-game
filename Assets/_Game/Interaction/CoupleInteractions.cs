using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Player;
using UnityEngine;

namespace LoveGame.Interaction
{
    /// <summary>
    /// Two-character romantic interaction framework. Definitions describe proximity,
    /// alignment offsets, poses and duration; the manager runs sequences:
    /// approach -> align -> synchronized pose -> cinematic camera -> cleanup.
    /// Interruptible, cooldown-protected, save-integrated (memories + relationship xp).
    /// </summary>
    public sealed class CoupleInteractionDefinition
    {
        public string Id;
        public string DisplayName;
        public float Proximity = 2.2f;          // engage distance between characters
        public float Duration = 3f;
        public string PlayerPose = "hug";
        public string PartnerPose = "hug";
        public bool FaceEachOther = true;
        public bool CameraCinematic = true;
        public float Cooldown = 8f;
        public string MemoryTitle;
        public float RelationshipXp = 6f;
    }

    public sealed class CoupleInteractionManager : IGameService
    {
        public string ServiceName => "CoupleInteractions";

        readonly Dictionary<string, CoupleInteractionDefinition> _defs = new Dictionary<string, CoupleInteractionDefinition>();
        readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        PlayerCharacter _player;
        PartnerCompanion _partner;
        ThirdPersonCamera _camera;
        ThirdPersonController _playerController;
        Coroutine _active;
        string _activeId;

        public bool IsRunning => _active != null;
        public string ActiveId => _activeId;

        public event System.Action<CoupleInteractionDefinition> SequenceStarted;
        public event System.Action<CoupleInteractionDefinition, bool> SequenceEnded;

        public void Bind(PlayerCharacter player, PartnerCompanion partner, ThirdPersonCamera camera, ThirdPersonController playerController)
        {
            _player = player;
            _partner = partner;
            _camera = camera;
            _playerController = playerController;
        }

        public void Initialize()
        {
            Register(new CoupleInteractionDefinition { Id = "hug", DisplayName = "Hug", Duration = 3.2f, PlayerPose = "hug", PartnerPose = "hug", MemoryTitle = "A warm hug", RelationshipXp = 6f });
            Register(new CoupleInteractionDefinition { Id = "kiss", DisplayName = "Kiss", Duration = 2.4f, PlayerPose = "kiss", PartnerPose = "kiss", MemoryTitle = "A sweet kiss", RelationshipXp = 10f });
            Register(new CoupleInteractionDefinition { Id = "highfive", DisplayName = "High five", Duration = 1.6f, PlayerPose = "highFive", PartnerPose = "highFive", Cooldown = 5f, MemoryTitle = "High five!", RelationshipXp = 3f });
            Register(new CoupleInteractionDefinition { Id = "holdhands", DisplayName = "Hold hands", Duration = 12f, PlayerPose = "holdHands", PartnerPose = "holdHands", Cooldown = 4f, MemoryTitle = "Walking hand in hand", RelationshipXp = 8f });
            Register(new CoupleInteractionDefinition { Id = "dance", DisplayName = "Dance together", Duration = 12f, PlayerPose = "dance", PartnerPose = "dance", Cooldown = 10f, MemoryTitle = "We danced", RelationshipXp = 12f });
            Register(new CoupleInteractionDefinition { Id = "sittogether", DisplayName = "Sit together", Duration = 20f, PlayerPose = "sit", PartnerPose = "sit", CameraCinematic = false, MemoryTitle = "Sitting together", RelationshipXp = 7f });
            Register(new CoupleInteractionDefinition { Id = "wave", DisplayName = "Wave together", Duration = 2.2f, PlayerPose = "wave", PartnerPose = "wave", CameraCinematic = false, Cooldown = 4f, RelationshipXp = 2f });
            Register(new CoupleInteractionDefinition { Id = "celebrate", DisplayName = "Celebrate", Duration = 2.6f, PlayerPose = "celebrate", PartnerPose = "celebrate", Cooldown = 6f, MemoryTitle = "Celebration!", RelationshipXp = 5f });
            Register(new CoupleInteractionDefinition { Id = "watchscenery", DisplayName = "Watch the view together", Duration = 18f, PlayerPose = "photoPose", PartnerPose = "photoPose", CameraCinematic = false, MemoryTitle = "A beautiful view together", RelationshipXp = 9f });
            Register(new CoupleInteractionDefinition { Id = "picnic", DisplayName = "Picnic", Duration = 25f, PlayerPose = "sit", PartnerPose = "sit", CameraCinematic = false, MemoryTitle = "Picnic date", RelationshipXp = 11f });
            Register(new CoupleInteractionDefinition { Id = "stargaze", DisplayName = "Stargazing", Duration = 30f, PlayerPose = "sit", PartnerPose = "sit", CameraCinematic = false, MemoryTitle = "Stargazing together", RelationshipXp = 13f });
        }

        public void Register(CoupleInteractionDefinition def) => _defs[def.Id] = def;
        public IReadOnlyDictionary<string, CoupleInteractionDefinition> Definitions => _defs;

        public void Tick(float delta)
        {
            foreach (var key in new List<string>(_cooldowns.Keys))
            {
                _cooldowns[key] -= delta;
                if (_cooldowns[key] <= 0f) _cooldowns.Remove(key);
            }
        }

        public bool CanRun(string id)
        {
            if (IsRunning) return false;
            if (_defs.TryGetValue(id, out var def) && _cooldowns.ContainsKey(id)) return false;
            return true;
        }

        /// <summary>Start an interaction. Characters walk into position automatically.</summary>
        public bool Start(string id)
        {
            if (!CanRun(id)) return false;
            if (!_defs.TryGetValue(id, out var def)) { Log.Warn("Couple", $"unknown interaction '{id}'"); return false; }
            if (_player == null || _partner == null) { Log.Warn("Couple", "characters not bound"); return false; }

            _activeId = id;
            _active = Services.Host.Run(Sequence(def));
            return true;
        }

        IEnumerator Sequence(CoupleInteractionDefinition def)
        {
            _cooldowns[def.Id] = def.Cooldown + def.Duration;
            GameEvents.Publish(new CoupleInteractionStartedEvent { InteractionId = def.Id });
            SequenceStarted?.Invoke(def);
            GameEvents.Publish(new NotificationEvent { Title = def.DisplayName, Body = "...", Duration = 1.6f });

            bool wasControl = _playerController != null && _playerController.AllowControl;
            if (_playerController != null) _playerController.AllowControl = false;
            _partner.Busy = true;

            // 1) approach: partner walks next to player
            float approachTimeout = Time.time + 6f;
            while (Time.time < approachTimeout)
            {
                var anchor = _player.transform.position + _player.transform.right * 0.55f - _player.transform.forward * 0.2f;
                if (_partner.MoveToAnchor(anchor)) break;
                yield return null;
            }

            // 2) align: face each other
            if (def.FaceEachOther)
            {
                float t = 0f;
                while (t < 0.6f)
                {
                    _player.FaceTowards(_partner.transform.position);
                    _partner.transform.rotation = Quaternion.Slerp(_partner.transform.rotation,
                        Quaternion.LookRotation(_player.transform.position - _partner.transform.position), 10f * Time.deltaTime);
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            // 3) synchronized poses
            _player.PlayPose(def.PlayerPose, def.Duration);
            _partner.GetComponent<PlayerCharacter>()?.PlayPose(def.PartnerPose, def.Duration);

            // 4) cinematic camera
            if (def.CameraCinematic && _camera != null)
            {
                _camera.BeginCinematic(_player.transform, 1.3f, 3.4f);
            }

            // 5) hold for duration (interrupt on movement input? control disabled, so hold)
            float timer = def.Duration;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            // 6) cleanup
            _player.ClearPose();
            _partner.GetComponent<PlayerCharacter>()?.ClearPose();
            _partner.Busy = false;
            if (_camera != null && _camera.IsCinematic) _camera.EndCinematic();
            if (_playerController != null) _playerController.AllowControl = wasControl;

            // 7) memory + progression
            if (!string.IsNullOrEmpty(def.MemoryTitle))
            {
                var memory = Services.Get<LoveGame.Inventory.MemoryService>();
                memory?.Record(def.Id, def.MemoryTitle, "couple", "A moment shared together.");
                var progress = SaveSystem.Current.progress;
                progress.relationshipXp += def.RelationshipXp;
                int newLevel = Mathf.FloorToInt(Mathf.Sqrt(progress.relationshipXp / 25f)) + 1;
                if (newLevel > progress.relationshipLevel)
                {
                    progress.relationshipLevel = newLevel;
                    GameEvents.Publish(new NotificationEvent { Title = "Bond deepened", Body = $"Your bond reached level {newLevel}.", Duration = 4f });
                }
                SaveSystem.Save();
            }

            GameEvents.Publish(new CoupleInteractionEndedEvent { InteractionId = def.Id, Completed = true });
            SequenceEnded?.Invoke(def, true);
            _active = null;
            _activeId = null;
        }

        /// <summary>Cancel the running interaction immediately (activity starts, vehicle enters, fast travel).</summary>
        public void Cancel()
        {
            if (!IsRunning) return;
            Services.Host.Stop(_active);
            _active = null;
            if (_camera != null && _camera.IsCinematic) _camera.EndCinematic();
            _player?.ClearPose();
            _partner?.GetComponent<PlayerCharacter>()?.ClearPose();
            if (_partner != null) _partner.Busy = false;
            if (_playerController != null) _playerController.AllowControl = true;
            var def = _activeId != null && _defs.TryGetValue(_activeId, out var d) ? d : null;
            GameEvents.Publish(new CoupleInteractionEndedEvent { InteractionId = _activeId, Completed = false });
            if (def != null) SequenceEnded?.Invoke(def, false);
            _active = null;
            _activeId = null;
        }

        public void Shutdown() => Cancel();
    }
}
