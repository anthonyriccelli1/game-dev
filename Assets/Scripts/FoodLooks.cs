using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Plated dishes built from POLYGON Shops food parts (Resources/ArtOverrides/Parts), each with a Saffron Bay twist:
    // the Flats Burger's melting cheese, Midnight sauce that glows and drips with stars rising off it, Planet Soup
    // with an orbiting ring, the Stoop Salad's paper umbrella. Falls back to the code-built look without the pack.
    public static class FoodLooks {
        static GameObject Part(string name) => ArtOverrides.Find("Parts", name);
        public static bool Available => Part("Plate") != null;

        // Places a pack part at height y (local to p) and returns the height of its top.
        static float Put(string name, Transform p, float y, float yaw = 0, Vector3? offset = null, float scale = 1) {
            var prefab = Part(name); if (!prefab) return y;
            var go = Object.Instantiate(prefab, p, false); go.name = name;
            go.transform.localPosition = new Vector3(0, y, 0) + (offset ?? Vector3.zero); go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = prefab.transform.localScale * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            float top = y;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, p.InverseTransformPoint(r.bounds.max).y);
            return top;
        }

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static Material Mat(Color c, float glow = 0) {
            string key = ColorUtility.ToHtmlStringRGB(c) + glow;
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "FoodLook " + key };
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", .35f);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            mats[key] = m; return m;
        }
        static GameObject Shape(PrimitiveType type, string name, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null) {
            var go = GameObject.CreatePrimitive(type); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(p, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = m; return go;
        }
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        public static void BuildPlate(List<string> components, Transform p, bool dirty) {
            bool Has(string id) => components != null && components.Contains(id);
            if (!dirty && Has("float")) { MoonberryFloat(p); return; }   // served in its own cup, no plate
            float y = Put("Plate", p, 0);
            if (dirty) {
                Shape(PrimitiveType.Cylinder, "Sauce smear", p, new Vector3(.04f, y + .002f, .02f), new Vector3(.17f, .002f, .1f), Mat(C("7A3E2A")));
                for (int i = 0; i < 4; i++) Shape(PrimitiveType.Cube, "Crumb", p, new Vector3(-.09f + i * .05f, y + .01f, .07f), Vector3.one * .02f, Mat(C("C49A5A")));
                return;
            }
            bool bun = Has("bun"), patty = Has("cooked_patty"), greens = Has("chopped_greens"), soup = Has("soup"), midnight = Has("midnight_sauce");
            if (soup) { PlanetSoupBowl(p, y); return; }
            if (Has("cooked_sausage")) { CometDog(p, y, bun); return; }
            if (!bun && greens) { StoopSalad(p, y); return; }
            if (bun) y = Put("BunBottom", p, y);
            if (greens) y = Put("Lettuce", p, y - .02f) - .01f;
            if (patty) {
                y = Put("Patty", p, y) - .005f;
                // Flats Burger signature: a cheese slice melting over the edge.
                if (bun) { y = Put("Cheese", p, y, 45) - .004f; }
            }
            if (midnight) y = MidnightGlaze(p, y);
            if (bun) Put("BunTop", p, y - .005f, 20);
        }

        // Glowing purple sauce layer with drips down the side and little stars floating up.
        static float MidnightGlaze(Transform p, float y) {
            var glow = Mat(C("7A2BE8"), 1.1f);
            Shape(PrimitiveType.Cylinder, "Midnight glaze", p, new Vector3(0, y + .006f, 0), new Vector3(.25f, .006f, .25f), glow);
            for (int i = 0; i < 4; i++) {
                float a = i * 1.7f + .4f; var at = new Vector3(Mathf.Cos(a) * .118f, y - .018f - (i % 2) * .01f, Mathf.Sin(a) * .118f);
                Shape(PrimitiveType.Capsule, "Midnight drip", p, at, new Vector3(.018f, .026f + (i % 2) * .012f, .018f), glow);
            }
            var stars = new GameObject("Midnight stars").transform; stars.SetParent(p, false); stars.localPosition = new Vector3(0, y + .13f, 0);
            var starMat = Mat(C("D9B8FF"), 1.6f);
            for (int i = 0; i < 5; i++) {
                float a = i * 1.26f; var s = Shape(PrimitiveType.Cube, "Star", stars, new Vector3(Mathf.Cos(a) * .09f, i * .025f, Mathf.Sin(a) * .09f), Vector3.one * .018f, starMat, new Vector3(45, 0, 45));
                s.AddComponent<FoodFx>().Mode = FoodFx.Kind.Float; s.GetComponent<FoodFx>().Phase = i * .7f;
            }
            return y + .012f;
        }

        // Planet Soup: a bowl of deep-space broth with a swirl and a glowing ring orbiting the bowl like Saturn.
        static void PlanetSoupBowl(Transform p, float y) {
            float top = Put("Bowl", p, y);
            float surf = Mathf.Lerp(y, top, .72f);
            Shape(PrimitiveType.Cylinder, "Planet broth", p, new Vector3(0, surf, 0), new Vector3(.24f, .004f, .24f), Mat(C("3B2A8C"), .35f));
            Shape(PrimitiveType.Cylinder, "Broth swirl", p, new Vector3(.03f, surf + .003f, .02f), new Vector3(.13f, .003f, .07f), Mat(C("E88A3C"), .6f), new Vector3(0, 30, 0));
            Shape(PrimitiveType.Sphere, "Tiny moon", p, new Vector3(-.05f, surf + .012f, -.03f), Vector3.one * .03f, Mat(C("F2E6C8"), .4f));
            var ring = new GameObject("Planet ring").transform; ring.SetParent(p, false); ring.localPosition = new Vector3(0, top + .015f, 0); ring.localRotation = Quaternion.Euler(18, 0, 8);
            var ringMat = Mat(C("5FD0F5"), 1.3f);
            for (int i = 0; i < 18; i++) { float a = i * Mathf.PI * 2 / 18; Shape(PrimitiveType.Cube, "Ring rock", ring, new Vector3(Mathf.Cos(a) * .19f, 0, Mathf.Sin(a) * .19f), new Vector3(.02f, .008f, .03f), ringMat, new Vector3(0, -a * Mathf.Rad2Deg, 0)); }
            ring.gameObject.AddComponent<FoodFx>().Mode = FoodFx.Kind.Spin;
        }

        // Comet Dog: a sausage in its bun, trailing a glowing tail of orange sauce with star sprinkles, like a comet.
        static void CometDog(Transform p, float y, bool bun) {
            float top = bun ? Put("HotDog", p, y, 90) : Put("Sausage", p, y, 90);
            var tail = Mat(C("FF8A1E"), 1.4f); var tip = Mat(C("FFD24A"), 1.8f);
            for (int i = 0; i < 5; i++) {
                float t = i / 4f; var at = new Vector3(-.2f - t * .12f, top - .01f + t * .03f, Mathf.Sin(t * 3f) * .02f);
                Shape(PrimitiveType.Capsule, "Comet tail", p, at, new Vector3(.04f * (1 - t * .7f), .05f * (1 - t * .6f), .04f * (1 - t * .7f)), i == 0 ? tip : tail, new Vector3(0, 0, 90));
            }
            Shape(PrimitiveType.Capsule, "Sauce streak", p, new Vector3(0, top + .004f, 0), new Vector3(.02f, .16f, .02f), tail, new Vector3(0, 0, 90));
            var sprinkles = new GameObject("Star sprinkles").transform; sprinkles.SetParent(p, false); sprinkles.localPosition = new Vector3(0, top + .03f, 0);
            for (int i = 0; i < 6; i++) {
                var s = Shape(PrimitiveType.Cube, "Star sprinkle", sprinkles, new Vector3(-.28f + i * .1f, (i % 3) * .015f, ((i % 2) - .5f) * .06f), Vector3.one * .014f, tip, new Vector3(45, 0, 45));
                s.AddComponent<FoodFx>().Mode = FoodFx.Kind.Float; s.GetComponent<FoodFx>().Phase = i * .9f;
            }
        }

        // Moonberry Float: a tall icy-blue cup of fizzing moonberry soda, a lilac soft-serve swirl on top,
        // a straw, a crescent-moon garnish and bubbles rising out of it.
        static void MoonberryFloat(Transform p) {
            var cupPart = Part("Cup"); float top = .26f;
            if (cupPart) {
                var cup = Object.Instantiate(cupPart, p, false); cup.name = "Float cup"; cup.transform.localScale = cupPart.transform.localScale * 1.05f;
                foreach (var c in cup.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var r in cup.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Mat(C("5E7BE0"), .25f);
                top = 0; foreach (var r in cup.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, p.InverseTransformPoint(r.bounds.max).y);
            } else Shape(PrimitiveType.Cylinder, "Float cup", p, new Vector3(0, .13f, 0), new Vector3(.17f, .13f, .17f), Mat(C("5E7BE0"), .25f));
            Shape(PrimitiveType.Cylinder, "Moonberry soda", p, new Vector3(0, top - .012f, 0), new Vector3(.16f, .004f, .16f), Mat(C("B04AD9"), .8f));
            var swirl = Mat(C("E9D5FF"), .35f);
            Shape(PrimitiveType.Sphere, "Soft serve", p, new Vector3(0, top + .02f, 0), new Vector3(.17f, .07f, .17f), swirl);
            Shape(PrimitiveType.Sphere, "Soft serve", p, new Vector3(0, top + .065f, 0), new Vector3(.12f, .06f, .12f), swirl);
            Shape(PrimitiveType.Sphere, "Soft serve tip", p, new Vector3(0, top + .1f, 0), new Vector3(.06f, .05f, .06f), swirl);
            Shape(PrimitiveType.Cylinder, "Straw", p, new Vector3(.045f, top + .08f, -.02f), new Vector3(.012f, .1f, .012f), Mat(C("FF5FA2")), new Vector3(0, 0, -12));
            var moon = Mat(C("FFE27A"), 1.6f);
            Shape(PrimitiveType.Sphere, "Crescent moon", p, new Vector3(-.055f, top + .08f, .03f), new Vector3(.035f, .035f, .012f), moon);
            Shape(PrimitiveType.Sphere, "Crescent bite", p, new Vector3(-.045f, top + .085f, .032f), new Vector3(.03f, .03f, .014f), swirl);
            var fizz = new GameObject("Fizz").transform; fizz.SetParent(p, false); fizz.localPosition = new Vector3(0, top + .05f, 0);
            var bubble = Mat(C("E3B8FF"), 1.4f);
            for (int i = 0; i < 5; i++) {
                float a = i * 1.26f; var b = Shape(PrimitiveType.Sphere, "Fizz bubble", fizz, new Vector3(Mathf.Cos(a) * .07f, i * .02f, Mathf.Sin(a) * .07f), Vector3.one * .014f, bubble);
                b.AddComponent<FoodFx>().Mode = FoodFx.Kind.Float; b.GetComponent<FoodFx>().Phase = i * .6f;
            }
        }

        // Stoop Salad: greens and tomato in a bowl with a little paper cocktail umbrella.
        static void StoopSalad(Transform p, float y) {
            float top = Put("Bowl", p, y);
            Put("Lettuce", p, Mathf.Lerp(y, top, .5f), 0, null, .95f); Put("Lettuce", p, Mathf.Lerp(y, top, .6f), 70, new Vector3(.02f, 0, -.02f), .8f);
            for (int i = 0; i < 3; i++) Put("Tomato", p, top - .005f, i * 40, new Vector3(Mathf.Cos(i * 2.1f) * .06f, 0, Mathf.Sin(i * 2.1f) * .06f), .7f);
            var stick = Shape(PrimitiveType.Cylinder, "Umbrella stick", p, new Vector3(.05f, top + .05f, .02f), new Vector3(.006f, .07f, .006f), Mat(C("F5E6C8")), new Vector3(0, 0, -14));
            Shape(PrimitiveType.Sphere, "Umbrella canopy", stick.transform, new Vector3(0, 1f, 0), new Vector3(14f, .3f, 14f), Mat(C("E8537A")));
        }

        // Soup pot on the stove (raw veg, finished soup, or scorched).
        public static bool BuildPot(string kind, Transform p) {
            if (!Part("Pot")) return false;
            float top = Put("Pot", p, 0);
            var color = kind == "ScorchedSoup" ? C("2E2320") : kind == "Soup" ? C("3B2A8C") : C("C9A25A");
            Shape(PrimitiveType.Cylinder, "Soup surface", p, new Vector3(0, top - .03f, 0), new Vector3(.3f, .004f, .3f), Mat(color, kind == "Soup" ? .4f : 0));
            if (kind == "Soup") for (int i = 0; i < 3; i++) { var s = Shape(PrimitiveType.Sphere, "Steam", p, new Vector3(-.06f + i * .06f, top + .06f + i * .03f, 0), Vector3.one * .06f, Mat(C("F2F2F2"))); s.AddComponent<FoodFx>().Mode = FoodFx.Kind.Steam; s.GetComponent<FoodFx>().Phase = i; }
            return true;
        }
    }

    // Tiny living touches on food: spinning rings, floating stars, rising steam.
    public class FoodFx : MonoBehaviour {
        public enum Kind { Spin, Float, Steam }
        public Kind Mode; public float Phase;
        Vector3 origin; bool set;
        void Update() {
            if (!set) { origin = transform.localPosition; set = true; }
            float t = Time.time + Phase;
            if (Mode == Kind.Spin) transform.Rotate(0, 40 * Time.deltaTime, 0, Space.Self);
            else if (Mode == Kind.Float) { transform.localPosition = origin + Vector3.up * (Mathf.Repeat(t * .05f, .08f)); transform.Rotate(0, 90 * Time.deltaTime, 0); }
            else { float k = Mathf.Repeat(t * .4f, 1); transform.localPosition = origin + Vector3.up * k * .12f; transform.localScale = Vector3.one * .06f * (1 - k * .6f); }
        }
    }
}
