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
        // City windows light up after dark (about half of them, like people are home).
        readonly System.Collections.Generic.List<Renderer> windows = new System.Collections.Generic.List<Renderer>();
        readonly System.Collections.Generic.List<Material> windowDay = new System.Collections.Generic.List<Material>();
        Material windowNight; bool nightWindows;
        // Art-pack building materials that ship an emissive window map: switched on after dark.
        readonly System.Collections.Generic.List<Material> packGlow = new System.Collections.Generic.List<Material>();

        public static void Install(CityGame game, Transform parent) {
            if (!game || FindFirstObjectByType<Atmosphere>()) return;   // only one look/lighting rig, even if setup runs again
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
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader) {
                windowNight = new Material(shader) { name = "Lit window (night)", color = new Color(1f, .8f, .5f) };
                windowNight.EnableKeyword("_EMISSION"); windowNight.SetColor("_EmissionColor", new Color(1f, .72f, .38f) * 1.6f);
                windowNight.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                int n = 0;
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
                    if (r.gameObject.name != "Window" && r.gameObject.name != "Lit window") continue;
                    if (r.gameObject.name == "Window" && (n++ * 7 + 3) % 10 >= 5) continue;
                    windows.Add(r); windowDay.Add(r.sharedMaterial);
                }
            }
            var seen = new System.Collections.Generic.HashSet<Material>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials) if (m && seen.Add(m) && m.HasProperty("_Enable_Emission") && m.name.StartsWith("PolygonCity_0")) packGlow.Add(m);
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
            grade.postExposure.value = Mathf.Lerp(.25f, .75f, dusk);
            bool night = dusk > .55f;
            if (night != nightWindows) foreach (var m in packGlow) {
                m.SetFloat("_Enable_Emission", night ? 1 : 0);
                var glow = night ? new Color(1f, .78f, .45f) * 1.4f : Color.black;
                if (m.HasProperty("_Emission_Color")) m.SetColor("_Emission_Color", glow);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", glow);
            }
            if (night != nightWindows && windowNight) { nightWindows = night; for (int i = 0; i < windows.Count; i++) if (windows[i]) windows[i].sharedMaterial = night ? windowNight : windowDay[i]; }
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
