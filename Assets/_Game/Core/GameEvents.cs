using System;
using System.Collections.Generic;

namespace LoveGame.Core
{
    /// <summary>Strongly-typed global event hub. Decouples systems without reflection costs.</summary>
    public static class GameEvents
    {
        static readonly Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Subscribers.TryGetValue(typeof(T), out var d);
            Subscribers[typeof(T)] = Delegate.Combine(d, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!Subscribers.TryGetValue(typeof(T), out var d)) return;
            var combined = Delegate.Remove(d, handler);
            if (combined == null) Subscribers.Remove(typeof(T));
            else Subscribers[typeof(T)] = combined;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (!Subscribers.TryGetValue(typeof(T), out var d) || d == null) return;
            if (d is Action<T> typed)
            {
                try { typed(evt); }
                catch (Exception e) { Log.Error("Events", $"handler failed for {typeof(T).Name}: {e.Message}"); }
            }
        }

        public static void Clear() => Subscribers.Clear();
    }

    // ------------------------------------------------------------ events ---

    public struct SceneLoadRequestedEvent { public string SceneName; }
    public struct RegionChangedEvent { public string RegionId; public string RegionName; }
    public struct RegionDiscoveredEvent { public string RegionId; public string RegionName; }
    public struct TimeOfDayChangedEvent { public float Hour; }
    public struct WeatherChangedEvent { public string Weather; }
    public struct PlayerSpawnedEvent { public Transform Player; }
    public struct InteractionPromptEvent { public string Prompt; public bool Visible; }
    public struct NotificationEvent { public string Title; public string Body; public float Duration; }
    public struct ItemAddedEvent { public string ItemId; public int Count; }
    public struct ItemRemovedEvent { public string ItemId; public int Count; }
    public struct CollectibleCollectedEvent { public string ItemId; public string RegionId; }
    public struct GiftGivenEvent { public string ItemId; }
    public struct MemoryRecordedEvent { public string MemoryId; }
    public struct CoupleInteractionStartedEvent { public string InteractionId; }
    public struct CoupleInteractionEndedEvent { public string InteractionId; public bool Completed; }
    public struct ActivityStartedEvent { public string ActivityId; }
    public struct ActivityCompletedEvent { public string ActivityId; }
    public struct VehicleEnteredEvent { public string VehicleKind; }
    public struct VehicleExitedEvent { public string VehicleKind; }
    public struct ContentDownloadProgressEvent { public string PackId; public float Progress; }
    public struct ContentDownloadFinishedEvent { public string PackId; public bool Success; }
    public struct SaveLoadedEvent { public bool Success; }
    public struct SaveWrittenEvent { public string Slot; }
    public struct LowMemoryWarningEvent { }
    public struct ThunderRequestedEvent { }
    public struct PoiInteractedEvent { public string PoiId; public string Kind; public string RegionId; }
    public struct QualityChangedEvent { public int Level; }
    public struct PhotoCapturedEvent { public string FilePath; }
    public struct FastTravelStartedEvent { public string RegionId; }
    public struct FastTravelFinishedEvent { public string RegionId; }
}
