using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Swappable model slots. Every station, piece of furniture, food item and character is still built by code
    // (so gameplay points like WorkPoint, Seat_N and pantry shelf hitboxes always exist), but if a prefab exists at
    // Resources/ArtOverrides/<Category>/<Id>, its model is shown instead of the code-built look.
    // Dropping an asset-pack model into the right folder with the right name is all it takes. See docs/ART_PIPELINE.md.
    public static class ArtOverrides {
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        public static GameObject Find(string category, string id) {
            string key = category + "/" + id;
            if (!cache.TryGetValue(key, out var prefab)) { prefab = Resources.Load<GameObject>("ArtOverrides/" + key); cache[key] = prefab; }
            return prefab;
        }

        public static GameObject Apply(GameObject root, string category, string id) {
            if (!root) return root;
            var prefab = Find(category, id); if (!prefab) return root;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var model = Object.Instantiate(prefab, root.transform, false); model.name = "Art override: " + id;
            var s = root.transform.localScale;   // undo the code-built root's squash (pantry/sink are scaled on X)
            model.transform.localScale = new Vector3(Div(prefab.transform.localScale.x, s.x), Div(prefab.transform.localScale.y, s.y), Div(prefab.transform.localScale.z, s.z));
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;   // gameplay colliders stay on the root
            return root;
        }
        static float Div(float a, float b) => Mathf.Abs(b) < .0001f ? a : a / b;
    }
}
