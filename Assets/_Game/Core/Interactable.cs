using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>Anything the player can target: doors, chairs, NPCs, vehicles, POI beacons, gifts.</summary>
    public interface IInteractor
    {
        Transform Transform { get; }
        bool CanInteract { get; }
    }

    /// <summary>
    /// Generic interactable component. Systems add behavior by overriding Interact or by
    /// subscribing to InteractEvent - never by creating bespoke per-object scripts.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("Identity")]
        public string interactableId;
        public string prompt = "Interact";
        public float range = 3.5f;
        public bool requireFacing = true;
        public bool singleUse = false;
        public bool consumed;

        /// <summary>Optional context payload (POI kind, item id, ...) consumed by handlers.</summary>
        public string context;

        public event System.Action<IInteractor> InteractEvent;

        public virtual bool CanBeInteracted => enabled && !consumed;

        public void Interact(IInteractor interactor)
        {
            if (!CanBeInteracted || interactor == null || !interactor.CanInteract) return;
            if (singleUse) consumed = true;
            try { OnInteract(interactor); }
            catch (System.Exception e) { Log.Error("Interact", $"{name}: {e.Message}"); }
            InteractEvent?.Invoke(interactor);
        }

        protected virtual void OnInteract(IInteractor interactor) { }
    }
}
