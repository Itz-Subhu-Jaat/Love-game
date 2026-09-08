using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoveGame.Audio
{
    /// <summary>
    /// Synthesizes every sound effect and the ambient music loop at runtime
    /// with AudioClip.Create — the project ships zero audio files.
    /// All clips are cached by name.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int Rate = 44100;

        private static readonly Dictionary<string, AudioClip> Cache =
            new Dictionary<string, AudioClip>();

        /// <summary>Returns (and lazily synthesizes) the named clip.</summary>
        public static AudioClip Get(string name)
        {
            AudioClip clip;
            if (Cache.TryGetValue(name, out clip)) return clip;

            clip = Synthesize(name);
            if (clip != null) Cache[name] = clip;
            return clip;
        }

        private static AudioClip Synthesize(string name)
        {
            switch (name)
            {
                case "arrow":  return Sweep("arrow", 950f, 380f, 0.12f, 0.5f, 1);
                case "mend":   return TwoTone("mend", 660f, 990f, 0.22f, 0.42f);
                case "hurt":   return SquareTone("hurt", 130f, 0.28f, 0.45f);
                case "heal":   return Arp("heal", new[] { 523f, 659f, 784f }, 0.07f, 0.42f);
                case "power":  return Arp("power", new[] { 392f, 523f, 659f, 880f }, 0.06f, 0.38f);
                case "wave":   return TwoTone("wave", 880f, 1320f, 0.5f, 0.28f);
                case "click":  return Sweep("click", 1800f, 1400f, 0.05f, 0.3f, 1);
                case "over":   return Arp("over", new[] { 330f, 262f, 196f }, 0.22f, 0.42f);
                case "win":    return Arp("win", new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.12f, 0.36f);
                case "music":  return MusicPad();
                default:       return null;
            }
        }

        // ------------------------------------------------------------- builders

        private static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Cosine attack/release envelope of length n.</summary>
        private static float[] Envelope(int n, float attackFrac, float releaseFrac)
        {
            var env = new float[n];
            int attack = Math.Max(1, (int)(n * attackFrac));
            int release = Math.Max(1, (int)(n * releaseFrac));
            for (int i = 0; i < n; i++)
            {
                float a = 1f;
                if (i < attack) a *= 0.5f * (1f - (float)Math.Cos(Math.PI * i / attack));
                if (i > n - release) a *= 0.5f * (1f - (float)Math.Cos(Math.PI * (n - i) / release));
                env[i] = a;
            }
            return env;
        }

        private static AudioClip Sweep(string name, float f0, float f1,
                                       float duration, float volume, int harmonics)
        {
            int n = (int)(duration * Rate);
            var data = new float[n];
            var env = Envelope(n, 0.04f, 0.5f);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = f0 + (f1 - f0) * t;
                phase += 2.0 * Math.PI * f / Rate;
                double sample = Math.Sin(phase) * volume;
                for (int k = 2; k <= harmonics; k++)
                {
                    sample += Math.Sin(phase * k) * (volume / (k * 2));
                }
                data[i] = (float)sample * env[i];
            }
            return MakeClip(name, data);
        }

        private static AudioClip TwoTone(string name, float f1, float f2,
                                         float duration, float volume)
        {
            int n = (int)(duration * Rate);
            var data = new float[n];
            var env = Envelope(n, 0.12f, 0.6f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float second = Mathf.Clamp01((t - duration * 0.25f) / (duration * 0.25f));
                double sample = Math.Sin(2.0 * Math.PI * f1 * t) * volume +
                                Math.Sin(2.0 * Math.PI * f2 * t) * volume * 0.8 * second;
                data[i] = (float)sample * env[i];
            }
            return MakeClip(name, data);
        }

        private static AudioClip SquareTone(string name, float freq,
                                             float duration, float volume)
        {
            int n = (int)(duration * Rate);
            var data = new float[n];
            var env = Envelope(n, 0.02f, 0.7f);
            double phase = 0.0;
            double smooth = 0.0;
            for (int i = 0; i < n; i++)
            {
                phase += 2.0 * Math.PI * freq / Rate;
                double raw = Math.Sign(Math.Sin(phase)) * volume;
                smooth += (raw - smooth) * 0.18;   // soften the square
                data[i] = (float)smooth * env[i];
            }
            return MakeClip(name, data);
        }

        private static AudioClip Arp(string name, float[] freqs,
                                     float noteDuration, float volume)
        {
            int noteSamples = (int)(noteDuration * Rate);
            int n = noteSamples * freqs.Length;
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                var env = Envelope(noteSamples, 0.08f, 0.55f);
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / Rate;
                    double sample = Math.Sin(2.0 * Math.PI * freqs[k] * t) * volume +
                                    Math.Sin(2.0 * Math.PI * freqs[k] * 2 * t) * (volume * 0.25);
                    data[k * noteSamples + i] = (float)sample * env[i];
                }
            }
            return MakeClip(name, data);
        }

        /// <summary>
        /// 16-second ambient pad: C - Am - F - G, four seconds each,
        /// soft detuned sines with slow attack. Loops seamlessly.
        /// </summary>
        private static AudioClip MusicPad()
        {
            const float barDuration = 4f;
            var chords = new[]
            {
                new[] { 261.63f, 329.63f, 392.00f },   // C major
                new[] { 220.00f, 261.63f, 329.63f },   // A minor
                new[] { 174.61f, 220.00f, 261.63f },   // F major
                new[] { 196.00f, 246.94f, 293.66f },   // G major
            };

            int barSamples = (int)(barDuration * Rate);
            int n = barSamples * chords.Length;
            var data = new float[n];

            for (int bar = 0; bar < chords.Length; bar++)
            {
                var env = Envelope(barSamples, 0.14f, 0.2f);
                for (int i = 0; i < barSamples; i++)
                {
                    float t = (float)i / Rate;
                    double sample = 0.0;
                    for (int v = 0; v < chords[bar].Length; v++)
                    {
                        double f = chords[bar][v];
                        sample += Math.Sin(2.0 * Math.PI * f * t) * 0.11 +
                                  Math.Sin(2.0 * Math.PI * f * 1.003 * t) * 0.05 +
                                  Math.Sin(2.0 * Math.PI * f * 0.5 * t) * 0.04;
                    }
                    data[bar * barSamples + i] = (float)sample * env[i];
                }
            }
            return MakeClip("music", data);
        }
    }
}
