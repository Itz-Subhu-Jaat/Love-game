using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoveGame.Core
{
    /// <summary>Deterministic xorshift PRNG - identical seeds produce identical worlds on every device.</summary>
    public sealed class Rng
    {
        ulong _state;
        public Rng(int seed) { _state = ((ulong)seed ^ 0x9E3779B97F4A7C15UL) + 0x2545F4914F6CDD1DUL; if (_state == 0) _state = 0x853C49E6748FEA9BUL; NextULong(); }

        public int NextInt() => (int)(NextULong() & 0x7FFFFFFF);
        public int Range(int minInclusive, int maxExclusive) => minInclusive + (int)(NextULong() % (ulong)Mathf.Max(1, maxExclusive - minInclusive));
        public float NextFloat() => (float)(NextULong() % 1000000UL) / 1000000f;
        public float Range(float min, float max) => min + NextFloat() * (max - min);
        public bool Chance(float probability) => NextFloat() < probability;
        public T Pick<T>(IReadOnlyList<T> list) => list[(int)(NextULong() % (ulong)list.Count)];
        public Vector2 InsideUnitCircle() { float a = NextFloat() * Mathf.PI * 2f; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Sqrt(NextFloat()); }

        ulong NextULong()
        {
            _state ^= _state << 13; _state ^= _state >> 7; _state ^= _state << 17;
            return _state;
        }
    }

    /// <summary>Compact 2D simplex noise. Seedable, allocation-free sampling for terrain generation.</summary>
    public sealed class Noise
    {
        readonly byte[] _perm = new byte[512];
        static readonly int[][] Grad2 = { new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 }, new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

        public Noise(int seed)
        {
            var rng = new Rng(seed);
            var p = new byte[256];
            for (int i = 0; i < 256; i++) p[i] = (byte)i;
            for (int i = 255; i > 0; i--) { int j = rng.Range(0, i + 1); (p[i], p[j]) = (p[j], p[i]); }
            for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
        }

        /// <summary>Returns noise in roughly [-1, 1].</summary>
        public float Sample(float x, float y)
        {
            const float F2 = 0.3660254f, G2 = 0.2113249f;
            int i = Mathf.FloorToInt(x + (x + y) * F2);
            int j = Mathf.FloorToInt(y + (x + y) * F2);
            float t = (i + j) * G2;
            float x0 = x - (i - t), y0 = y - (j - t);
            int i1 = x0 > y0 ? 1 : 0, j1 = x0 > y0 ? 0 : 1;
            float x1 = x0 - i1 + G2, y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2, y2 = y0 - 1f + 2f * G2;
            int ii = i & 255, jj = j & 255;
            float n = 0f;
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 > 0) { int g = _perm[ii + _perm[jj]] & 7; t0 *= t0; n += t0 * t0 * (Grad2[g][0] * x0 + Grad2[g][1] * y0); }
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 > 0) { int g = _perm[ii + i1 + _perm[(jj + j1) & 255]] & 7; t1 *= t1; n += t1 * t1 * (Grad2[g][0] * x1 + Grad2[g][1] * y1); }
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 > 0) { int g = _perm[ii + 1 + _perm[(jj + 1) & 255]] & 7; t2 *= t2; n += t2 * t2 * (Grad2[g][0] * x2 + Grad2[g][1] * y2); }
            return 70f * n;
        }

        /// <summary>Fractal Brownian motion, several octaves, output roughly [-1, 1].</summary>
        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float persistence = 0.5f)
        {
            float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Sample(x * freq, y * freq);
                norm += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            return norm > 0f ? sum / norm : 0f;
        }
    }

    /// <summary>Generic object pool for repeated create/destroy patterns (NPC visuals, particles, vehicles).</summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Func<T> _factory;
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;
        readonly Stack<T> _stack = new Stack<T>();
        public int CountAll { get; private set; }
        public int CountInactive => _stack.Count;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null, int prewarm = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet; _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++) _stack.Push(_factory());
            CountAll = prewarm;
        }

        public T Get()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _factory();
            CountAll++;
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            _stack.Push(item);
        }
    }

    public static class CoreMath
    {
        public static float Remap(float value, float fromA, float fromB, float toA, float toB)
        {
            float t = Mathf.InverseLerp(fromA, fromB, value);
            return Mathf.LerpUnclamped(toA, toB, t);
        }

        public static float Damp(float current, float target, float smoothing, float dt)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Pow(smoothing, dt));
        }

        public static Vector3 Damp(Vector3 current, Vector3 target, float smoothing, float dt)
        {
            return Vector3.Lerp(current, target, 1f - Mathf.Pow(smoothing, dt));
        }

        public static Color Damp(Color current, Color target, float smoothing, float dt)
        {
            return Color.Lerp(current, target, 1f - Mathf.Pow(smoothing, dt));
        }

        public static string Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return hex;
            Log.Warn("Math", $"invalid hex color '{hex}', using white");
            return "#FFFFFF";
        }

        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.white;
        }
    }

    /// <summary>Shared layer indices - kept in one place so physics masks never drift.</summary>
    public static class GameLayers
    {
        public const int Player = 8;
        public const int Interactable = 9;
        public const int Vehicle = 10;
        public const int Prop = 11;
        public const int Npc = 12;
        public const int Wildlife = 13;

        public static int DefaultMask => ~(1 << 5); // everything except UI
    }
}
