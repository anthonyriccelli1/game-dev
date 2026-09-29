using System.IO;
using UnityEditor;
using UnityEngine;

// Hooks third-party art packs into the generated scene. Everything here is optional: if a pack folder is
// missing (e.g. a teammate without the pack), the game falls back to the code-built look.
public static class ArtPackDressing {
    const string Generic = "Assets/Synty/PolygonGeneric/Prefabs/";
    const string City = "Assets/Synty/PolygonCity/Prefabs/";
    const string Starter = "Assets/Synty/PolygonStarter/Prefabs/";
    const string Shops = "Assets/Synty/PolygonShops/Prefabs/";

    // Shops-pack slots built from one or more pack prefabs at real-world scale. Each part: source, local position,
    // and scale (x,y,z) relative to the pack's own size. Stations keep our gameplay points; only the look changes.
    static readonly (string cat, string id, (string src, Vector3 pos, Vector3 scale)[] parts)[] Built = {
        ("Furniture", "grill", new[] { (Shops + "Props/SM_Prop_Kitchen_Grill_01.prefab", Vector3.zero, Vector3.one * 1.05f) }),
        ("Furniture", "stove", new[] { (Shops + "Props/SM_Prop_Kitchen_Stove_Oven_01.prefab", Vector3.zero, new Vector3(.55f, .9f, .82f)) }),
        ("Furniture", "prep_bench", new[] { (Shops + "Props/SM_Prop_Kitchen_Prep_Table_01.prefab", Vector3.zero, new Vector3(1.08f, .95f, .85f)),
                                             (Shops + "Props/SM_Prop_Kitchen_Chopping_Board_01.prefab", new Vector3(.35f, 1.0f, 0), Vector3.one) }),
        ("Furniture", "assembly", new[] { (Shops + "Props/SM_Prop_Kitchen_Prep_Table_03.prefab", Vector3.zero, new Vector3(1.08f, .95f, .85f)) }),
        ("Furniture", "counter", new[] { (Shops + "Props/SM_Prop_Kitchen_Prep_Table_02.prefab", Vector3.zero, new Vector3(.86f, .95f, .85f)) }),
        ("Furniture", "cutting_board", new[] { (Shops + "Props/SM_Prop_Kitchen_Prep_Table_02.prefab", Vector3.zero, new Vector3(.86f, .95f, .85f)),
                                                (Shops + "Props/SM_Prop_Kitchen_Chopping_Board_01.prefab", new Vector3(0, 1.0f, 0), new Vector3(.9f, 1, .9f)) }),
        ("Furniture", "sink", new[] { (Shops + "Props/SM_Prop_Kitchen_Sink_01.prefab", Vector3.zero, new Vector3(.36f, .6f, .85f)) }),
        ("Items", "PreparedPatty", new[] { (Shops + "Food/SM_Prop_Food_Meat_Patty_Raw_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Items", "CookedPatty", new[] { (Shops + "Food/SM_Prop_Food_Meat_Patty_Cooked_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Items", "BurntPatty", new[] { (Shops + "Food/SM_Prop_Food_Meat_Patty_Burnt_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Items", "Bun", new[] { (Shops + "Food/SM_Prop_Food_Bun_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Items", "RawGreens", new[] { (Shops + "Food/SM_Prop_Food_Lettuce_Whole_01.prefab", Vector3.zero, Vector3.one) }),
        ("Items", "ChoppedGreens", new[] { (Shops + "Food/SM_Prop_Food_Lettuce_Leaves_01.prefab", new Vector3(0, .03f, 0), Vector3.one) }),
        // Dish parts, stacked at runtime by FoodLooks (Resources/ArtOverrides/Parts/<name>).
        ("Parts", "Plate", new[] { (Shops + "Food/SM_Prop_Food_Plate_01.prefab", Vector3.zero, Vector3.one * .95f) }),
        ("Parts", "BunBottom", new[] { (Shops + "Food/SM_Prop_Food_Bun_Bottom_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Parts", "BunTop", new[] { (Shops + "Food/SM_Prop_Food_Bun_Top_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Parts", "Patty", new[] { (Shops + "Food/SM_Prop_Food_Meat_Patty_Cooked_01.prefab", Vector3.zero, Vector3.one * 1.15f) }),
        ("Parts", "Cheese", new[] { (Shops + "Food/SM_Prop_Food_Cheese_Slice_01.prefab", new Vector3(0, .01f, 0), Vector3.one * 1.25f) }),
        ("Parts", "Lettuce", new[] { (Shops + "Food/SM_Prop_Food_Lettuce_Leaves_01.prefab", new Vector3(0, .03f, 0), Vector3.one * 1.05f) }),
        ("Parts", "Tomato", new[] { (Shops + "Food/SM_Prop_Food_Tomato_Slice_01.prefab", Vector3.zero, Vector3.one) }),
        ("Parts", "Bowl", new[] { (Shops + "Food/SM_Prop_Food_Bowl_01.prefab", Vector3.zero, Vector3.one * 1.1f) }),
        ("Parts", "Pot", new[] { (Shops + "Props/SM_Prop_Kitchen_Pot_01.prefab", Vector3.zero, Vector3.one * .7f) }),
    };

    // Model slots filled from the pack: (category, slot id, source prefab, largest dimension in meters)
    static readonly (string cat, string id, string src, float size)[] Slots = {
        ("Items", "RawProtein", Generic + "Props/SM_Gen_Prop_Food_Meat_01.prefab", .24f),
        ("Furniture", "potted_palm", City + "Props/SM_Prop_PotPlant_02.prefab", 1.6f),
        ("Furniture", "flower_pot", City + "Props/SM_Prop_PotPlant_01.prefab", .8f),
        ("Furniture", "planter_box", City + "Props/SM_Prop_Planter_02.prefab", 1.4f),
        ("Furniture", "display_shelf", City + "Props/SM_Prop_ShopInterior_Shelf_01.prefab", 1.8f),
        ("Furniture", "bottle_shelf", Generic + "Props/SM_Gen_Prop_Shelf_02.prefab", 1.6f),
        ("Furniture", "rustic_crates", Generic + "Props/SM_Gen_Prop_Crate_Preset_01.prefab", 1.2f),
        ("Furniture", "flour_sacks", Generic + "Props/SM_Gen_Prop_Sack_Stack_01.prefab", 1f),
        ("Furniture", "oak_barrel", Generic + "Props/SM_Gen_Prop_Barrel_Wood_01.prefab", 1f),
        ("Furniture", "lounge_couch", City + "Props/SM_Prop_Couch_01.prefab", 1.9f),
        ("Furniture", "statue", Generic + "Props/SM_Gen_Prop_Statue_02.prefab", 1.5f),
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
        foreach (var slot in Built) {
            string outDir = "Assets/Resources/ArtOverrides/" + slot.cat, outPath = outDir + "/" + slot.id + ".prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Synty/PolygonShops")) { if (File.Exists(outPath) && slot.parts[0].src.StartsWith(Shops)) AssetDatabase.DeleteAsset(outPath); continue; }
            Directory.CreateDirectory(outDir);
            var root = new GameObject(slot.id); bool any = false;
            foreach (var part in slot.parts) {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(part.src); if (!src) continue;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(src); model.transform.SetParent(root.transform, false);
                model.transform.localPosition = part.pos; model.transform.localScale = Vector3.Scale(model.transform.localScale, part.scale); any = true;
            }
            if (any) PrefabUtility.SaveAsPrefabAsset(root, outPath);
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
        if (!CityMap.Available) {   // in the city build these would block Milo's door
            Put(Generic + "Props/SM_Gen_Prop_Crate_Preset_01.prefab", new Vector3(-16.3f, 0, 8.8f), 15, root);
            Put(Generic + "Props/SM_Gen_Prop_Barrel_Wood_01.prefab", new Vector3(-15.4f, 0, 10.6f), 0, root);
        }
        // (The sack stack and cardboard box that sat by the sidewalk tables are gone: those tables are now for customers.)
        Put(Generic + "Props/SM_Gen_Prop_Barrel_Metal_01.prefab", new Vector3(9.4f, 0, 15.2f), 0, root);
        Put(Starter + "SM_Generic_Tree_01.prefab", new Vector3(-6.6f, 0, 12.2f), 0, root);
        Put(Starter + "SM_Generic_Tree_02.prefab", new Vector3(7.2f, 0, 12.2f), 90, root);
        Put(Starter + "SM_Generic_Tree_03.prefab", new Vector3(-18.5f, 0, -7.6f), 0, root);
        Put(Starter + "SM_Generic_Tree_04.prefab", new Vector3(13.5f, 0, -7.6f), 45, root);
    }
}
