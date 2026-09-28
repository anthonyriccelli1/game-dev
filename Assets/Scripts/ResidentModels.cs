using UnityEngine;
namespace RestaurantCity {
    // Spawns a resident model (100 Avatars by Polygonal Mind, CC0) from Resources/Residents/<id>, with its painted
    // texture on a URP material, scaled to a consistent height. All share the Mixamo skeleton, so animations are shared.
    public static class ResidentModels {
        public const string Credits = "Characters based on 100 Avatars by Polygonal Mind (CC0)";
        public static GameObject Spawn(string id, Transform parent, float height = 1.75f) {
            var prefab = Resources.Load<GameObject>("Residents/" + id);
            if (!prefab) { Debug.LogWarning("Resident model missing: " + id); return null; }
            var go = Object.Instantiate(prefab, parent, false); go.name = "Resident " + id;
            var tex = Resources.Load<Texture2D>("Residents/" + id);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Resident " + id };
            if (tex) { mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; }
            mat.SetFloat("_Smoothness", .12f); mat.color = Color.white;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
            var rends = go.GetComponentsInChildren<Renderer>(); if (rends.Length > 0) {
                var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                if (b.size.y > .1f) go.transform.localScale *= height / b.size.y;
            }
            return go;
        }
    }
}
