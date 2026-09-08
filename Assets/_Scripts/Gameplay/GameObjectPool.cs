using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoveGame.Gameplay
{
    /// <summary>
    /// Minimal named object pool. Rent() returns an inactive object (created by
    /// the factory on first use) — configure it, then SetActive(true).
    /// The pool is cleared on every scene unload because pooled objects are
    /// scene-bound (see GameBootstrap.OnDestroy).
    /// </summary>
    public static class GameObjectPool
    {
        private static readonly Dictionary<string, Stack<GameObject>> Pools =
            new Dictionary<string, Stack<GameObject>>();

        /// <summary>
        /// Returns an inactive pooled object ready for configuration,
        /// or a freshly created one via the factory.
        /// </summary>
        public static GameObject Rent(string key, Func<GameObject> create)
        {
            Stack<GameObject> stack;
            if (Pools.TryGetValue(key, out stack))
            {
                while (stack.Count > 0)
                {
                    var pooled = stack.Pop();
                    if (pooled == null) continue;   // destroyed by scene unload
                    pooled.transform.parent = null;
                    return pooled;                   // still inactive
                }
            }

            var go = create();
            go.SetActive(false);
            Pools[key] = stack ?? new Stack<GameObject>();
            return go;
        }

        /// <summary>Deactivates an object and pushes it back into its pool.</summary>
        public static void Return(string key, GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            Stack<GameObject> stack;
            if (!Pools.TryGetValue(key, out stack))
            {
                stack = new Stack<GameObject>();
                Pools[key] = stack;
            }
            stack.Push(go);
        }

        /// <summary>Drops all pool references (scene objects die with the scene).</summary>
        public static void ResetAll()
        {
            Pools.Clear();
        }
    }
}
