using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LoveGame.Core
{
    /// <summary>Categorized logging. Verbose is stripped from release builds.</summary>
    public static class Log
    {
        const bool VerboseEnabled = true;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Verbose(string category, string message)
        {
            if (VerboseEnabled) UnityEngine.Debug.Log($"[{category}] {message}");
        }

        public static void Info(string category, string message) => UnityEngine.Debug.Log($"[{category}] {message}");
        public static void Warn(string category, string message) => UnityEngine.Debug.LogWarning($"[{category}][WARN] {message}");
        public static void Error(string category, string message) => UnityEngine.Debug.LogError($"[{category}][ERROR] {message}");

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string category, string message)
        {
            if (!condition) UnityEngine.Debug.LogError($"[{category}][ASSERT] {message}");
        }
    }

    /// <summary>Frame-rate and memory monitor used by the debug overlay and performance validation.</summary>
    public sealed class PerformanceMonitor
    {
        public float CurrentFps { get; private set; }
        public float AverageFps { get; private set; }
        public long TotalAllocatedMb { get; private set; }
        public long ReservedMb { get; private set; }

        int _frames;
        float _accum;
        readonly float[] _history = new float[120];
        int _historyIndex;

        public void Tick(float unscaledDelta)
        {
            if (unscaledDelta <= 0f) return;
            _frames++;
            _accum += unscaledDelta;
            CurrentFps = 1f / unscaledDelta;
            if (_accum >= 0.5f)
            {
                AverageFps = _frames / _accum;
                _history[_historyIndex] = AverageFps;
                _historyIndex = (_historyIndex + 1) % _history.Length;
                _frames = 0;
                _accum = 0f;
            }
            TotalAllocatedMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() >> 20;
            ReservedMb = UnityEngine.Profiling.Profiler.GetReservedMemoryLong() >> 20;
        }

        public float MinFps()
        {
            float min = float.MaxValue;
            for (int i = 0; i < _history.Length; i++)
            {
                if (_history[i] > 0f && _history[i] < min) min = _history[i];
            }
            return min == float.MaxValue ? 0f : min;
        }
    }
}
