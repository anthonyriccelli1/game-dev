using System;
using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Placeholder sound effects, synthesized in code (no downloaded audio). They mark where every sound belongs;
    // real recorded sounds can replace any of them later by name.
    public static class SoundFx {
        const int Rate = 22050;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly System.Random rng = new System.Random(7);
        static float Noise() => (float)(rng.NextDouble() * 2 - 1);
        static AudioClip Make(string name, float seconds, Func<float, float> wave, bool loop = false) {
            if (cache.TryGetValue(name, out var c) && c) return c;
            int n = (int)(Rate * seconds); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1, 1);
            if (loop) { int fade = Rate / 50; for (int i = 0; i < fade; i++) { float k = i / (float)fade; data[i] *= k; data[n - 1 - i] *= k; } }
            c = AudioClip.Create("Fx " + name, n, 1, Rate, false); c.SetData(data, 0); cache[name] = c; return c;
        }
        static float Sine(float f, float t) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float last;
        static float Crackle(float amount) { float x = Noise(); float hp = x - last * .6f; last = x; return hp * amount; }
        public static AudioClip Sizzle => Make("sizzle", 1.2f, t => Crackle(.22f) * (.7f + .3f * Mathf.PerlinNoise(t * 9, 0)) + (rng.NextDouble() < .002 ? .5f : 0), true);
        public static AudioClip Bubble => Make("bubble", 1.6f, t => { float p = (t * 5.3f) % 1f; return Sine(180 + 260 * p, t) * Mathf.Exp(-p * 9) * .35f; }, true);
        public static AudioClip Chop => Make("chop", .12f, t => Noise() * Mathf.Exp(-t * 60) * .6f + Sine(130, t) * Mathf.Exp(-t * 30) * .5f);
        public static AudioClip Clink => Make("clink", .3f, t => (Sine(1850, t) + .6f * Sine(2790, t)) * Mathf.Exp(-t * 14) * .25f);
        public static AudioClip Thud => Make("thud", .15f, t => Sine(95 - t * 120, t) * Mathf.Exp(-t * 25) * .6f);
        public static AudioClip Register => Make("register", .6f, t => (t < .05f ? Noise() * .3f : 0) + (Sine(1320, t) + Sine(1760, t) * (t > .08f ? 1 : 0)) * Mathf.Exp(-t * 6) * .22f);
        public static AudioClip Doorbell => Make("doorbell", .7f, t => Sine(t < .32f ? 784 : 622, t) * Mathf.Exp(-(t % .32f) * 7) * .28f);
        public static AudioClip Huff => Make("huff", .5f, t => (Noise() * .35f + Sine(170 - t * 150, t) * .4f) * Mathf.Exp(-t * 6));
        public static AudioClip Horn => Make("horn", .9f, t => { float v = 1 + .01f * Sine(6, t); float s = Mathf.Sign(Sine(330 * v, t)) + Mathf.Sign(Sine(415 * v, t)) + Mathf.Sign(Sine(494 * v, t)); return s * .07f * Mathf.Min(1, t * 20) * Mathf.Min(1, (.9f - t) * 6); });
        public static AudioClip Tip => Make("tip", .45f, t => Sine(t < .1f ? 1047 : t < .2f ? 1319 : 1568, t) * Mathf.Exp(-(t % .1f) * 12) * .25f);
        public static AudioClip Warning => Make("warning", .4f, t => ((t % .2f) < .09f ? Sine(1050, t) : 0) * .22f);
        public static AudioClip Wash => Make("wash", .45f, t => Crackle(.3f) * Mathf.Sin(Mathf.PI * t / .45f));
        public static AudioClip Stir => Make("stir", .35f, t => Crackle(.18f) * Mathf.Sin(Mathf.PI * t / .35f) + Sine(260, t) * .05f);
    }
}
