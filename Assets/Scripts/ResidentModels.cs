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
            // Skinned-mesh bounds are unreliable before the first skinning pass, so measure the baked (bind-pose) mesh.
            float minY = float.MaxValue, maxY = float.MinValue; var baked = new Mesh();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                smr.BakeMesh(baked, false);
                foreach (var v in baked.vertices) { float y = smr.transform.TransformPoint(v).y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            }
            Object.Destroy(baked);
            float size = maxY - minY;
            if (size > .01f && size < 1000) {
                float k = height / size, feet = minY - go.transform.position.y;
                go.transform.localScale *= k; go.transform.position -= Vector3.up * feet * k;   // soles on the ground
            }
            else Debug.LogWarning("Resident height unknown: " + id);
            return go;
        }
        // A full resident: a root (moved/rotated by the game) holding the scaled model, animated by shared Mixamo clips.
        public static GameObject Create(ResidentDef def, Transform parent) {
            var root = new GameObject(def.Name); root.transform.SetParent(parent, false);
            var model = Spawn(def.Id, root.transform, def.Height);
            if (model) {
                var animator = model.GetComponent<Animator>();
                if (animator && animator.avatar && animator.avatar.isHuman && ResidentAnimator.Available) model.AddComponent<ResidentAnimator>().Init(animator, def.Gait);
            }
            root.AddComponent<ResidentTag>().Def = def;
            root.AddComponent<CharacterMotion>();
            return root;
        }
    }
    public static class People {
        // Residents need the Mixamo clips (not in Git); without them the older procedural characters stand in.
        public static bool UseResidents => ResidentAnimator.Available;
        static int guestSeed;
        public static GameObject Visitor(int seed, int legacyType, Transform parent, bool night) =>
            UseResidents ? ResidentModels.Create(ResidentCast.Visitor(seed, night), parent) : RestaurantArt.CreateCharacter(legacyType, parent);
        public static GameObject NextVisitor(int legacyType, Transform parent, bool night) => Visitor(++guestSeed * 7 + legacyType, legacyType, parent, night);
        public static GameObject Worker(string workerId, int legacyType, Transform parent) =>
            UseResidents ? ResidentModels.Create(ResidentCast.ForWorker(workerId), parent) : RestaurantArt.CreateCharacter(legacyType, parent);
        public static GameObject Story(ResidentDef def, int legacyType, Transform parent) =>
            UseResidents ? ResidentModels.Create(def, parent) : RestaurantArt.CreateCharacter(legacyType, parent);
        public static string NameOf(GameObject go, string fallback) { var t = go ? go.GetComponent<ResidentTag>() : null; return t != null && t.Def != null ? t.Def.Name : fallback; }
    }
    public class ResidentTag : MonoBehaviour { public ResidentDef Def; }
}
