using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Interaction
{
    /// <summary>
    /// Scans for the best nearby Interactable (POI beacons, NPCs, vehicles, furniture),
    /// publishes prompts for the HUD, and triggers on the interact input.
    /// </summary>
    public sealed class InteractionService : IGameService
    {
        public string ServiceName => "Interaction";

        readonly List<Interactable> _cache = new List<Interactable>();
        Interactable _focused;
        float _scanTimer;
        IInteractor _interactor;
        Player.InputService _input;
        public Interactable Focused => _focused;

        public void Bind(IInteractor interactor, Player.InputService input)
        {
            _interactor = interactor;
            _input = input;
        }

        public void Initialize() { }

        public void Tick(float delta)
        {
            _scanTimer -= delta;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 0.25f;
                Scan();
            }

            if (_input != null && _input.Active.InteractPressed && _focused != null && _interactor != null)
            {
                _focused.Interact(_interactor);
            }
        }

        void Scan()
        {
            if (_interactor == null) return;
            var pos = _interactor.Transform.position;

            _cache.Clear();
            var all = UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var inter in all)
            {
                if (inter == null || !inter.CanBeInteracted || !inter.gameObject.activeInHierarchy) continue;
                var d = (inter.transform.position - pos).magnitude;
                if (d > inter.range) continue;
                // slight preference for interactables roughly in front
                float facingBonus = 0f;
                if (inter.requireFacing && _interactor.Transform != null)
                {
                    var dir = (inter.transform.position - pos).normalized;
                    if (Vector3.Dot(_interactor.Transform.forward, dir) < 0.1f) facingBonus = 2f;
                }
                float score = d + facingBonus;
                if (score < bestScore) { bestScore = score; best = inter; }
            }

            if (best != _focused)
            {
                _focused = best;
                GameEvents.Publish(new InteractionPromptEvent
                {
                    Prompt = best != null ? best.prompt : string.Empty,
                    Visible = best != null
                });
            }
        }

        public void Shutdown() => _cache.Clear();
    }
}
