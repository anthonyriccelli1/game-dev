using System.IO;
using UnityEditor;
using UnityEngine;

// Hooks third-party art packs into the generated scene. Everything here is optional: if a pack folder is
// missing (e.g. a teammate without the pack), the game falls back to the code-built look.
public static class ArtPackDressing {
    const string Generic = "Assets/Synty/PolygonGeneric/Prefabs/";
    const string Starter = "Assets/Synty/PolygonStarter/Prefabs/";

    // Model slots filled from the pack: (category, slot id, source prefab, largest dimension in meters)
    static readonly (string cat, string id, string src, float size)[] Slots = {
        ("Items", "RawProtein", Generic + "Props/SM_Gen_Prop_Food_Meat_01.prefab", .24f),
        ("Items", "Bun", Generic + "Props/SM_Gen_Prop_Food_Bread_01.prefab", .2f),
        ("Items", "RawGreens", Generic + "Props/SM_Gen_Prop_Food_Vegetable_01.prefab", .22f),
    };

    public static void GenerateOverrides() {
        foreach (var slot in Slots) {
            string outDir = "Assets/Resources/ArtOverrides/" + slot.cat, outPath = outDir + "/" + slot.id + ".prefab";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(slot.src);
            if (!src) { if (File.Exists(outPath)) AssetDatabase.DeleteAsset(outPath); continue; }
            Directory.CreateDirectory(outDir);
            var root = new GameObject(slot.id);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(src); model.transform.SetParent(root.transform, false);
            Fit(model, slot.size);
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }

    // Scale so the largest side is `size`, centred on X/Z, resting on Y = 0.
    static void Fit(GameObject model, float size) {
        var renderers = model.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) return;
        var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
        float largest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)); if (largest < .0001f) return;
        model.transform.localScale *= size / largest;
        b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
        model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
    }

    static GameObject Put(string path, Vector3 position, float yaw, Transform parent, float scale = 1) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!src) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); go.transform.localScale *= scale;
        return go;
    }

    // Street dressing: skyline behind both sides of the block, props at Milo's and the stand, extra trees.
    public static void Dress(Transform world) {
        if (!AssetDatabase.IsValidFolder("Assets/Synty")) return;
        var root = new GameObject("Art pack dressing").transform; root.SetParent(world, false);
        if (!CityMap.Available) for (int i = 0; i < 6; i++) {
            Put(Generic + "Building/SM_Gen_Bld_Background_" + (i + 1).ToString("00") + ".prefab", new Vector3(-28 + i * 11, 0, 42), 180, root);
            Put(Generic + "Building/SM_Gen_Bld_Background_" + (i + 6).ToString("00") + ".prefab", new Vector3(-28 + i * 11, 0, -34), 0, root);
        }
        Put(Generic + "Props/SM_Gen_Prop_Crate_Preset_01.prefab", new Vector3(-16.3f, 0, 8.8f), 15, root);
        Put(Generic + "Props/SM_Gen_Prop_Barrel_Wood_01.prefab", new Vector3(-15.4f, 0, 10.6f), 0, root);
        Put(Generic + "Props/SM_Gen_Prop_Sack_Stack_01.prefab", new Vector3(-8.3f, 0, 9.8f), -20, root);
        Put(Generic + "Props/SM_Gen_Prop_Cardboard_Box_Preset_01.prefab", new Vector3(-4.9f, 0, 9.6f), 10, root);
        Put(Generic + "Props/SM_Gen_Prop_Barrel_Metal_01.prefab", new Vector3(9.4f, 0, 15.2f), 0, root);
        Put(Starter + "SM_Generic_Tree_01.prefab", new Vector3(-6.6f, 0, 12.2f), 0, root);
        Put(Starter + "SM_Generic_Tree_02.prefab", new Vector3(7.2f, 0, 12.2f), 90, root);
        Put(Starter + "SM_Generic_Tree_03.prefab", new Vector3(-18.5f, 0, -7.6f), 0, root);
        Put(Starter + "SM_Generic_Tree_04.prefab", new Vector3(13.5f, 0, -7.6f), 45, root);
    }
}
