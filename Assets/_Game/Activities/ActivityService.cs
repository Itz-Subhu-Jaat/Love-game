using System.Collections;
using System.Collections.Generic;
using LoveGame.Core;
using LoveGame.Inventory;
using UnityEngine;

namespace LoveGame.Activities
{
    public enum ActivityState { Inactive, Running, Completed, Failed, Cancelled }

    /// <summary>
    /// Activity framework base: lifecycle (start/complete/fail/cancel), objectives,
    /// rewards, completion tracking in save, event hooks for UI. Concrete activities
    /// are data + coroutine - the framework owns all state transitions.
    /// </summary>
    public abstract class ActivityBase
    {
        public abstract string Id { get; }
        public virtual string DisplayName => Id;
        public ActivityState State { get; protected set; } = ActivityState.Inactive;
        public float ObjectiveProgress { get; protected set; }
        public string ObjectiveText { get; protected set; } = "";
        protected ActivityService Service;

        public void SetService(ActivityService service) => Service = service;

        public virtual bool CanStart() => State == ActivityState.Inactive;

        public bool Start()
        {
            if (!CanStart()) return false;
            State = ActivityState.Running;
            ObjectiveProgress = 0f;
            OnStarted();
            GameEvents.Publish(new ActivityStartedEvent { ActivityId = Id });
            Service?.Host.Run(Run());
            return true;
        }

        protected abstract IEnumerator Run();

        protected void Complete()
        {
            if (State != ActivityState.Running) return;
            State = ActivityState.Completed;
            OnCompleted();
            if (!SaveSystem.Current.progress.completedActivities.Contains(Id))
                SaveSystem.Current.progress.completedActivities.Add(Id);
            SaveSystem.Save();
            GameEvents.Publish(new ActivityCompletedEvent { ActivityId = Id });
            GameEvents.Publish(new NotificationEvent { Title = "Activity complete", Body = DisplayName, Duration = 3.5f });
        }

        protected void Fail(string reason)
        {
            if (State != ActivityState.Running) return;
            State = ActivityState.Failed;
            GameEvents.Publish(new NotificationEvent { Title = "Activity failed", Body = reason, Duration = 3f });
        }

        public void Cancel()
        {
            if (State != ActivityState.Running) return;
            State = ActivityState.Cancelled;
            OnCancelled();
        }

        protected virtual void OnStarted() { }
        protected virtual void OnCompleted() { }
        protected virtual void OnCancelled() { }

        protected static void Grant(string itemId, int count)
        {
            Services.TryGet<InventoryService>(out var inventory);
            inventory?.Add(itemId, count);
        }
    }

    /// <summary>
    /// Registry + lifecycle owner for all activities. Routes POI interactions to activities,
    /// exposes the running activity (only one at a time - mobile UX clarity).
    /// </summary>
    public sealed class ActivityService : IGameService
    {
        public string ServiceName => "Activities";

        public ServiceHost Host => Services.Host;
        readonly Dictionary<string, ActivityBase> _activities = new Dictionary<string, ActivityBase>();
        public ActivityBase Running { get; private set; }

        public event System.Action<ActivityBase> ActivityChanged;

        FishingActivity _fishing;
        RacingActivity _racing;
        TreasureHuntActivity _treasure;
        StargazingActivity _stargazing;
        PicnicActivity _picnic;
        CampingActivity _camping;
        PuzzleActivity _puzzle;
        TargetMiniGameActivity _target;

        public void Initialize()
        {
            _fishing = new FishingActivity(this, Services.Get<World.WorldStreamer>());
            _racing = new RacingActivity();
            _treasure = new TreasureHuntActivity();
            _stargazing = new StargazingActivity(this, Services.Get<World.DayNightCycle>());
            _picnic = new PicnicActivity();
            _camping = new CampingActivity();
            _puzzle = new PuzzleActivity();
            _target = new TargetMiniGameActivity();
            foreach (var a in new ActivityBase[] { _fishing, _racing, _treasure, _stargazing, _picnic, _camping, _puzzle, _target })
            {
                a.SetService(this);
                _activities[a.Id] = a;
            }
            GameEvents.Subscribe<PoiInteractedEvent>(OnPoiInteracted);
        }

        void OnPoiInteracted(PoiInteractedEvent evt)
        {
            switch (evt.Kind)
            {
                case nameof(World.PoiKind.Fishing): TryStart("fishing"); break;
                case nameof(World.PoiKind.Race): TryStart("racing"); break;
                case nameof(World.PoiKind.Treasure): TryStart("treasure_hunt"); break;
                case nameof(World.PoiKind.Stargaze): TryStart("stargazing"); break;
                case nameof(World.PoiKind.Picnic): TryStart("picnic"); break;
                case nameof(World.PoiKind.Camp): TryStart("camping"); break;
                case nameof(World.PoiKind.Cave): TryStart("puzzle"); break;
                case nameof(World.PoiKind.Activity): TryStart("target_minigame"); break;
            }
        }

        public void Tick(float delta)
        {
            if (Running != null && Running.State != ActivityState.Running)
            {
                Running = null;
                ActivityChanged?.Invoke(null);
            }
        }

        public bool TryStart(string activityId)
        {
            if (Running != null) return false;
            if (!_activities.TryGetValue(activityId, out var activity) || !activity.Start()) return false;
            Running = activity;
            ActivityChanged?.Invoke(activity);
            return true;
        }

        public void CancelRunning()
        {
            Running?.Cancel();
            Running = null;
            ActivityChanged?.Invoke(null);
        }

        public bool IsCompleted(string activityId) =>
            SaveSystem.Current.progress.completedActivities.Contains(activityId);

        public IReadOnlyDictionary<string, ActivityBase> All => _activities;

        public FishingActivity Fishing => _fishing;
        public RacingActivity Racing => _racing;

        public void Shutdown()
        {
            GameEvents.Unsubscribe<PoiInteractedEvent>(OnPoiInteracted);
        }
    }
}
