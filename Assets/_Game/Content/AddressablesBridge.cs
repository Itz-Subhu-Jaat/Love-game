#if LOVEGAME_ADDRESSABLES
using System.Collections.Generic;
using LoveGame.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LoveGame.Content
{
    /// <summary>
    /// Bridge from the pack/manifest model to Unity Addressables. Active only when the
    /// Addressables package is installed (versionDefines). Region packs map 1:1 to
    /// Addressables groups/labels; this bridge keeps gameplay independent of the
    /// Addressables API so the local blockout pipeline stays functional without it.
    /// </summary>
    public sealed class AddressablesBridge
    {
        readonly Dictionary<string, AsyncOperationHandle> _active = new Dictionary<string, AsyncOperationHandle>();

        public static bool IsAvailable => true;

        public bool Initialize(System.Action<bool> onComplete)
        {
            var op = Addressables.InitializeAsync();
            op.Completed += handle => onComplete?.Invoke(handle.Status == AsyncOperationStatus.Succeeded);
            return true;
        }

        public AsyncOperationHandle<GameObject> InstantiateRegionAnchor(string regionId, Vector3 position)
        {
            var handle = Addressables.InstantiateAsync($"region_{regionId}", position, Quaternion.identity);
            _active[regionId] = handle;
            return handle;
        }

        public void ReleaseRegion(string regionId)
        {
            if (_active.TryGetValue(regionId, out var handle))
            {
                Addressables.Release(handle);
                _active.Remove(regionId);
            }
        }

        public float GetProgress(string regionId) =>
            _active.TryGetValue(regionId, out var handle) && handle.IsValid() ? handle.PercentComplete : 0f;
    }
}
#else
namespace LoveGame.Content
{
    /// <summary>Stub when Addressables is not installed - keeps call sites compiling identically.</summary>
    public sealed class AddressablesBridge
    {
        public static bool IsAvailable => false;
    }
}
#endif
