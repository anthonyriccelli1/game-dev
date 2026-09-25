using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RestaurantCity {
    // Look and mood: filmic tonemapping, bloom on lamps and neon, gentle color grading that shifts from warm
    // afternoon to cool night, and warm practical lights at the stand, Milo's and your restaurant.
    public class Atmosphere : MonoBehaviour {
        public CityGame Game;
        Volume volume; ColorAdjustments grade; Bloom bloom; Vignette vignette; WhiteBalance balance;
        Light[] warm;

        public static void Install(CityGame game, Transform parent) {
            if (!game || game.GetComponentInChildren<Atmosphere>()) return;
            var go = new GameObject("Atmosphere"); go.transform.SetParent(parent, false);
            var a = go.AddComponent<Atmosphere>(); a.Game = game; a.Build();
        }

        void Build() {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            bloom = profile.Add<Bloom>(true); bloom.threshold.Override(.95f); bloom.intensity.Override(.55f); bloom.scatter.Override(.72f);
            grade = profile.Add<ColorAdjustments>(true); grade.postExposure.Override(.25f); grade.contrast.Override(14f); grade.saturation.Override(12f);
            balance = profile.Add<WhiteBalance>(true); balance.temperature.Override(8f);
            vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.22f); vignette.smoothness.Override(.45f);
            volume = gameObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10; volume.profile = profile;
            warm = new[] {
                Warm(new Vector3(0, 3.0f, 8.1f), 2.2f, 7f),      // food stand, under the awning
                Warm(new Vector3(-12, 3.0f, 8.6f), 1.8f, 6f),    // Milo's market
                Warm(new Vector3(-13, 3.2f, -13), 2.4f, 7f),     // your restaurant
                Warm(new Vector3(-7.5f, 3.2f, -13), 2.4f, 7f),
                Warm(new Vector3(-10, 3.2f, -18), 2.2f, 7f),
            };
        }

        static void EnablePost(Camera cam) { var d = cam.GetUniversalAdditionalCameraData(); if (!d.renderPostProcessing) d.renderPostProcessing = true; }

        Light Warm(Vector3 position, float intensity, float range) {
            var go = new GameObject("Warm practical light"); go.transform.SetParent(transform, false); go.transform.position = position;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, .78f, .52f); l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            return l;
        }

        void LateUpdate() {
            if (!Game) return;
            float clock = Game.State.Clock;
            float dusk = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(125, 160, clock));
            if (clock > 220) dusk = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(220, 240, clock));
            // Golden hour just before night, cool blue night, crisp day.
            float golden = Mathf.Clamp01(1 - Mathf.Abs(clock - 140) / 22f);
            balance.temperature.value = Mathf.Lerp(8f, -18f, dusk) + golden * 14f;
            grade.postExposure.value = Mathf.Lerp(.25f, .45f, dusk);
            grade.colorFilter.Override(Color.Lerp(Color.white, new Color(.82f, .88f, 1f), dusk));
            bloom.intensity.value = Mathf.Lerp(.45f, 1.1f, dusk);
            vignette.intensity.value = Mathf.Lerp(.2f, .32f, dusk);
            foreach (var l in warm) if (l) l.intensity = Mathf.Lerp(.6f, 2.6f, dusk) * (l.range > 6.5f ? 1 : .85f);
            // Every player camera renders post-processing (split-screen included).
            if (Game.Player && Game.Player.View) EnablePost(Game.Player.View);
            if (Game.CoOp) { foreach (var p in Game.CoOp.Players) if (p && p.View) EnablePost(p.View); }
        }
    }
}
