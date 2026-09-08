using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Interaction
{
    /// <summary>
    /// Emote system: quick gestures any character can play. Data-driven catalog; new emotes
    /// are pose registrations, not code changes.
    /// </summary>
    public sealed class EmoteService : IGameService
    {
        public string ServiceName => "Emotes";

        Player.PlayerCharacter _player;
        Player.PartnerCompanion _partner;

        static readonly string[] DefaultEmotes = { "wave", "dance", "point", "celebrate", "photoPose", "sit" };

        public void Bind(Player.PlayerCharacter player, Player.PartnerCompanion partner)
        {
            _player = player;
            _partner = partner;
        }

        public void Initialize() { }

        public void Tick(float delta) { }

        public System.Collections.Generic.IReadOnlyList<string> Available => DefaultEmotes;

        /// <summary>Play an emote on the player; partner mirrors 40% of the time for charm.</summary>
        public void Play(string emote)
        {
            if (_player == null || !_player.HasPose(emote))
            {
                Log.Warn("Emotes", $"emote '{emote}' not available");
                return;
            }
            _player.PlayPose(emote);
            GameEvents.Publish(new NotificationEvent { Title = "Emote", Body = emote, Duration = 1.2f });
            if (_partner != null && Random.value < 0.4f)
            {
                var partnerVisual = _partner.GetComponent<Player.PlayerCharacter>();
                if (partnerVisual != null && partnerVisual.HasPose(emote)) partnerVisual.PlayPose(emote);
            }
        }

        public void Shutdown() { }
    }
}
