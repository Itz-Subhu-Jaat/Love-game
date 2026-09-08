using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>Base contract for every long-running game system registered with <see cref="Services"/>.</summary>
    public interface IGameService
    {
        string ServiceName { get; }
        /// <summary>Called once, in registration order, before the first frame.</summary>
        void Initialize();
        /// <summary>Called every frame after initialization (cheap work only).</summary>
        void Tick(float delta);
        /// <summary>Called on application quit and scene teardown.</summary>
        void Shutdown();
    }

    /// <summary>Lightweight service locator. Replacement-ready for constructor injection later.</summary>
    public static class Services
    {
        static readonly List<IGameService> Ordered = new List<IGameService>();
        static readonly Dictionary<Type, IGameService> ByType = new Dictionary<Type, IGameService>();
        static readonly HashSet<IGameService> InitializedServices = new HashSet<IGameService>();
        static ServiceHost _host;

        /// <summary>True once at least one Initialize pass completed.</summary>
        public static bool AnyInitialized { get; private set; }

        public static ServiceHost Host
        {
            get
            {
                if (_host == null)
                {
                    var go = new GameObject("~ServiceHost");
                    _host = go.AddComponent<ServiceHost>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
                return _host;
            }
        }

        public static void Register<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (ByType.ContainsKey(type))
            {
                Log.Warn("Services", $"Duplicate registration ignored: {type.Name}");
                return;
            }
            if (service == null) throw new ArgumentNullException(nameof(service));
            ByType[type] = service;
            Ordered.Add(service);
            Log.Verbose("Services", $"registered {type.Name}");
        }

        public static T Get<T>() where T : class, IGameService
        {
            return ByType.TryGetValue(typeof(T), out var s) ? s as T : null;
        }

        public static bool TryGet<T>(out T service) where T : class, IGameService
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>Initializes every not-yet-initialized service in registration order.
        /// Safe to call repeatedly - late registrations initialize on the next pass.</summary>
        public static void InitializeAll()
        {
            int initialized = 0;
            foreach (var s in Ordered)
            {
                if (InitializedServices.Contains(s)) continue;
                try
                {
                    s.Initialize();
                    InitializedServices.Add(s);
                    initialized++;
                }
                catch (Exception e) { Log.Error("Services", $"initialize failed for {s.ServiceName}: {e.Message}\n{e.StackTrace}"); }
            }
            AnyInitialized = true;
            if (initialized > 0) Log.Info("Services", $"{initialized} new services initialized ({Ordered.Count} total)");
        }

        public static void TickAll(float delta)
        {
            for (int i = 0; i < Ordered.Count; i++)
            {
                var s = Ordered[i];
                if (!InitializedServices.Contains(s)) continue;
                try { s.Tick(delta); }
                catch (Exception e) { Log.Error("Services", $"tick failed for {s.ServiceName}: {e.Message}"); }
            }
        }

        public static void ShutdownAll()
        {
            for (int i = Ordered.Count - 1; i >= 0; i--)
            {
                try { Ordered[i].Shutdown(); }
                catch (Exception e) { Log.Error("Services", $"shutdown failed for {Ordered[i].ServiceName}: {e.Message}"); }
            }
            Ordered.Clear();
            ByType.Clear();
            InitializedServices.Clear();
            AnyInitialized = false;
        }
    }

    /// <summary>MonoBehaviour host that drives all services + provides a coroutine runner that survives scene loads.</summary>
    public sealed class ServiceHost : MonoBehaviour
    {
        PerformanceMonitor _perf;
        public PerformanceMonitor Performance => _perf ?? (_perf = new PerformanceMonitor());

        void Awake()
        {
            Application.lowMemory += OnLowMemory;
        }

        void Update()
        {
            Performance.Tick(Time.unscaledDeltaTime);
            Services.TickAll(Time.deltaTime);
        }

        void OnLowMemory()
        {
            GameEvents.Publish(new LowMemoryWarningEvent());
        }

        void OnApplicationQuit()
        {
            Services.ShutdownAll();
        }

        void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
        public void Stop(IEnumerator routine) { if (routine != null) StopCoroutine(routine); }
        public void Stop(Coroutine routine) { if (routine != null) StopCoroutine(routine); }
    }
}
