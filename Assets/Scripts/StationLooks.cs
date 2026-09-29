using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Equipment levels you can see. Level 1 is the Flats: grimy, dented, greasy. Level 2 is clean chrome (the pack as-is).
    // Level 3 is premium: a brand stripe, neon underglow and live flames. Applied on top of any station model.
    public static class StationLooks {
        public const string LevelTag = "Station level ";
        public static void ApplyLevel(GameObject station, int level) {
            if (!station) return;
            var old = station.transform.Find("Level dressing"); if (old) Object.Destroy(old.gameObject);
            var dress = new GameObject("Level dressing").transform; dress.SetParent(station.transform, false);
            var art = station.GetComponentsInChildren<Renderer>();
            foreach (var r in art) {
                if (r.transform.IsChildOf(dress) || !r.enabled) continue;
                var shared = r.sharedMaterials; var mats = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++) {
                    if (!shared[i]) continue;
                    var m = new Material(shared[i]);
                    var tint = level <= 1 ? new Color(.66f, .55f, .42f) : level >= 3 ? new Color(1.05f, 1.05f, 1.1f) : Color.white;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * tint);
                    else if (m.HasProperty("_Color")) m.color *= tint;
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
            var b = Bounds(station, dress); var lb = new Bounds(station.transform.InverseTransformPoint(b.center), Vector3.Scale(b.size, Inv(station.transform.lossyScale)));
            float top = lb.max.y, front = lb.max.z;
            if (level <= 1) {
                // Flats hand-me-down: grease drips running down the front and a scorched, stained patch.
                var grease = FoodLooks.Mat(new Color(.26f, .19f, .12f)); var stain = FoodLooks.Mat(new Color(.42f, .33f, .22f));
                for (int i = 0; i < 4; i++) {
                    float x = lb.min.x + lb.size.x * (.18f + i * .21f), y = lb.min.y + lb.size.y * (.72f - (i % 2) * .08f);
                    Blob(dress, new Vector3(x, y - .06f - (i % 3) * .03f, front + .004f), new Vector3(.022f, .09f + (i % 3) * .05f, .008f), grease, PrimitiveType.Capsule);
                }
                Blob(dress, new Vector3(lb.min.x + lb.size.x * .7f, lb.min.y + lb.size.y * .45f, front + .003f), new Vector3(lb.size.x * .22f, lb.size.y * .12f, .006f), stain, PrimitiveType.Sphere);
            }
            if (level >= 3) {
                // Brand stripe, neon underglow and flames licking up off the cooking surface.
                Blob(dress, new Vector3(lb.center.x, lb.min.y + lb.size.y * .62f, front + .006f), new Vector3(lb.size.x * .96f, .05f, .01f), FoodLooks.Mat(new Color(.95f, .18f, .24f), .9f));
                Blob(dress, new Vector3(lb.center.x, .03f, lb.center.z), new Vector3(lb.size.x * .9f, .01f, lb.size.z * .85f), FoodLooks.Mat(new Color(.2f, .95f, 1f), 1.4f));
                var glow = new GameObject("Underglow").AddComponent<Light>(); glow.transform.SetParent(dress, false); glow.transform.localPosition = new Vector3(lb.center.x, .15f, lb.center.z + lb.extents.z);
                glow.type = LightType.Point; glow.color = new Color(.2f, .95f, 1f); glow.range = 2f; glow.intensity = 1.1f;
                var flameMat = FoodLooks.Mat(new Color(1f, .42f, .05f), 1.5f); var tipMat = FoodLooks.Mat(new Color(1f, .85f, .2f), 1.5f);
                for (int i = 0; i < 6; i++) {
                    var f = Blob(dress, new Vector3(lb.min.x + lb.size.x * (.15f + i * .14f), top - .02f, lb.center.z + ((i % 2) - .5f) * .2f), new Vector3(.05f, .075f, .05f), i % 2 == 0 ? flameMat : tipMat, PrimitiveType.Capsule);
                    var fx = f.AddComponent<Flicker>(); fx.Phase = i * .9f;
                }
            }
        }
        static Vector3 Inv(Vector3 v) => new Vector3(1 / Mathf.Max(.0001f, v.x), 1 / Mathf.Max(.0001f, v.y), 1 / Mathf.Max(.0001f, v.z));
        static Bounds Bounds(GameObject go, Transform skip) {
            var rs = go.GetComponentsInChildren<Renderer>(); var b = new Bounds(go.transform.position, Vector3.zero); bool first = true;
            foreach (var r in rs) { if (!r.enabled || r.transform.IsChildOf(skip)) continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            return b;
        }
        static GameObject Blob(Transform p, Vector3 pos, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube) {
            var go = GameObject.CreatePrimitive(type); Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(p, false);
            go.transform.localPosition = pos; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = m; return go;
        }
    }
    public class Flicker : MonoBehaviour {
        public float Phase; Vector3 baseScale; bool set;
        void Update() {
            if (!set) { baseScale = transform.localScale; set = true; }
            float t = Time.time * 9 + Phase; float k = .75f + .35f * Mathf.PerlinNoise(t, Phase);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * k, baseScale.z);
        }
    }
}
