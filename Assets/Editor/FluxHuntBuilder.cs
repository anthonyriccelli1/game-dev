using UnityEditor;
using UnityEngine;
using RestaurantCity;

// The Flux hunt in the scene: the 12 hiding spots (FluxHunt.Hints, same order) and the two case prefabs.
// Stand-ins until the rendered hazard case arrives: an orange hard-shell case with the glowing Flux vial clipped on
// top, and a briefcase for the Midnight recipe strongbox. Both live in Resources/Flux so runtime can spawn them.
public static class FluxHuntBuilder {
    public static void Build(Transform world) {
        MakeCase(); MakeRecipeBox();
        var spots = new GameObject("Flux spots").transform; spots.SetParent(world, false);
        Vector3 Anchor(string name, Vector3 fallback) { var a = GameObject.Find(name); return a ? a.transform.position : fallback; }
        var aRoof = Anchor("Climb A roof", new Vector3(-7.5f, 30.5f, 37.5f)); var bRoof = Anchor("Climb B roof", new Vector3(-7.5f, 12.5f, -37.5f));
        var positions = new[] {
            new Vector3(-24.2f, 0, -36.6f),                 // graffiti alley
            new Vector3(-32.6f, 0, -8.7f),                  // beside the bus shelter
            new Vector3(22.6f, 0, -28.4f),                  // behind Gus's truck
            new Vector3(6.3f, 0, 38.8f),                    // the Alchemist's courtyard
            new Vector3(-50f, 0, -116f),                    // harbour promenade
            new Vector3(65.8f, 0, -29.6f),                  // the park statue
            new Vector3(22f, 0, -91f),                      // Truck Park bike racks
            new Vector3(21.6f, AlchemistBuilder.Storey, 30.7f),   // The Alchemist's lab balcony
            Anchor("Climb A landing 4", new Vector3(-8f, 12.15f, 40.3f)),   // fire-escape landing, North Avenue (~12 m)
            Anchor("Climb B landing 2", new Vector3(-8f, 6.15f, -40.3f)),   // fire-escape landing, South Avenue (~6 m)
            aRoof + new Vector3(-1.4f, 0, 0),               // the tallest roof
            bRoof + new Vector3(1.2f, 0, 0),                // the low roof
        };
        for (int i = 0; i < positions.Length; i++) {
            var s = new GameObject("Flux spot " + i).transform; s.SetParent(spots, false);
            s.SetPositionAndRotation(positions[i], Quaternion.Euler(0, i * 53, 0));
        }
        // On the fire-escape landings the case sits square against the wall, off the walking line.
        foreach (var (i, anchor) in new[] { (8, "Climb A landing 4"), (9, "Climb B landing 2") }) { var a = GameObject.Find(anchor); if (a) spots.GetChild(i).rotation = a.transform.rotation; }
        var m = new GameObject("Midnight case spot").transform; m.SetParent(world, false);
        m.SetPositionAndRotation(aRoof + new Vector3(1.4f, 0, .4f), Quaternion.Euler(0, 200, 0));
    }

    static void MakeCase() {
        const string path = "Assets/Resources/Flux/FluxCase.prefab";
        var root = new GameObject("FluxCase");
        var shell = Place("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Chest_02.prefab", root.transform, .72f);
        float top = shell ? Top(shell) : .35f;
        var vial = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Flux/FluxVial.prefab");
        if (vial) {
            var v = (GameObject)PrefabUtility.InstantiatePrefab(vial, root.transform); v.name = "Flux vial";
            v.transform.localPosition = new Vector3(0, top, 0); v.transform.localScale = Vector3.one * (.34f / FluxVial.ModelHeight);
            v.AddComponent<FluxVial>().Bob = false;
        }
        PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
    }
    static void MakeRecipeBox() {
        const string path = "Assets/Resources/Flux/RecipeBox.prefab";
        var root = new GameObject("RecipeBox");
        Place("Assets/Synty/PolygonShops/Prefabs/Props/SM_Prop_Clothes_Briefcase_01.prefab", root.transform, .55f);
        PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
    }
    // A pack prop scaled to `width` across, resting on y = 0, colliders stripped.
    static GameObject Place(string asset, Transform parent, float width) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(asset); if (!src) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var b = Bounds(go); go.transform.localScale *= width / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z));
        b = Bounds(go); go.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
        return go;
    }
    static float Top(GameObject go) => Bounds(go).max.y;
    static Bounds Bounds(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
}
