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
    struct Part { public string src; public Vector3 pos, scale; public float yaw; }
    static Part P(string src, Vector3 pos, Vector3 scale, float yaw) => new Part { src = src, pos = pos, scale = scale, yaw = yaw };
    static readonly (string cat, string id, Part[] parts)[] Built = {
        ("Furniture", "grill", new[] { P(Shops + "Props/SM_Prop_Kitchen_Grill_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.05f, 1.05f, 1.05f), 0f) }),
        ("Furniture", "stove", new[] { P(Shops + "Props/SM_Prop_Kitchen_Stove_Oven_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.9f, 0.82f), 0f) }),
        ("Furniture", "prep_bench", new[] { P(Shops + "Props/SM_Prop_Kitchen_Prep_Table_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.08f, 0.95f, 0.85f), 0f), P(Shops + "Props/SM_Prop_Kitchen_Chopping_Board_01.prefab", new Vector3(0.35f, 1.0f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
        ("Furniture", "assembly", new[] { P(Shops + "Props/SM_Prop_Kitchen_Prep_Table_03.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.08f, 0.95f, 0.85f), 0f),
            P(Shops + "Props/SM_Prop_Kitchen_Counter_Food_Insert_Large_02.prefab", new Vector3(-.62f, 1.0f, -.12f), new Vector3(1f, 1f, .6f), 90f),
            P(Shops + "Props/SM_Prop_Kitchen_Counter_Food_Insert_Small_03.prefab", new Vector3(-.62f, 1.0f, .22f), Vector3.one, 0f),
            P(Shops + "Food/SM_Prop_Food_Plastic_Tray_01.prefab", new Vector3(.45f, 1.0f, .05f), Vector3.one * 1.2f, 0f) }),
        ("Furniture", "counter", new[] { P(Shops + "Props/SM_Prop_Kitchen_Prep_Table_02.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.86f, 0.95f, 0.85f), 0f),
            P(Shops + "Props/SM_Prop_Cafe_Napkin_Holder_01.prefab", new Vector3(.3f, 1.0f, -.28f), Vector3.one, 0f),
            P(Shops + "Food/SM_Prop_Food_Sauce_Ketchup_01.prefab", new Vector3(.18f, 1.0f, -.3f), Vector3.one, 0f) }),
        ("Furniture", "trash", new[] { P("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_TrashCan_01.prefab", new Vector3(0f, .41f, 0f), Vector3.one * .95f, 0f) }),
        ("Furniture", "cutting_board", new[] { P(Shops + "Props/SM_Prop_Kitchen_Prep_Table_02.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.86f, 0.95f, 0.85f), 0f), P(Shops + "Props/SM_Prop_Kitchen_Chopping_Board_01.prefab", new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 1f, 0.9f), 0f) }),
        ("Furniture", "sink", new[] { P(Shops + "Props/SM_Prop_Kitchen_Sink_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.6f, 0.85f), 0f) }),
        ("Items", "PreparedPatty", new[] { P(Shops + "Food/SM_Prop_Food_Meat_Patty_Raw_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Items", "CookedPatty", new[] { P(Shops + "Food/SM_Prop_Food_Meat_Patty_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Items", "BurntPatty", new[] { P(Shops + "Food/SM_Prop_Food_Meat_Patty_Burnt_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Items", "Bun", new[] { P(Shops + "Food/SM_Prop_Food_Bun_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Items", "RawGreens", new[] { P(Shops + "Food/SM_Prop_Food_Lettuce_Whole_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
        ("Items", "ChoppedGreens", new[] { P(Shops + "Food/SM_Prop_Food_Lettuce_Leaves_01.prefab", new Vector3(0f, 0.03f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
        ("Parts", "Plate", new[] { P(Shops + "Food/SM_Prop_Food_Plate_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.95f, 0.95f, 0.95f), 0f) }),
        ("Parts", "BunBottom", new[] { P(Shops + "Food/SM_Prop_Food_Bun_Bottom_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Parts", "BunTop", new[] { P(Shops + "Food/SM_Prop_Food_Bun_Top_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Parts", "Patty", new[] { P(Shops + "Food/SM_Prop_Food_Meat_Patty_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.15f), 0f) }),
        ("Parts", "Cheese", new[] { P(Shops + "Food/SM_Prop_Food_Cheese_Slice_01.prefab", new Vector3(0f, 0.01f, 0f), new Vector3(1.25f, 1.25f, 1.25f), 0f) }),
        ("Parts", "Lettuce", new[] { P(Shops + "Food/SM_Prop_Food_Lettuce_Leaves_01.prefab", new Vector3(0f, 0.03f, 0f), new Vector3(1.05f, 1.05f, 1.05f), 0f) }),
        ("Parts", "Tomato", new[] { P(Shops + "Food/SM_Prop_Food_Tomato_Slice_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
        ("Parts", "Bowl", new[] { P(Shops + "Food/SM_Prop_Food_Bowl_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Parts", "Pot", new[] { P(Shops + "Props/SM_Prop_Kitchen_Pot_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.7f, 0.7f), 0f) }),
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

    // Rotated composites (seating sets face their Seat_N points: yaw 0 faces +Z).
    static (string src, Vector3 pos, Vector3 scale, float yaw) R(string src, float x, float y, float z, float yaw = 0, float s = 1) => (Shops + src + ".prefab", new Vector3(x, y, z), Vector3.one * s, yaw);
    static (string src, Vector3 pos, Vector3 scale, float yaw) RS(string src, float x, float y, float z, Vector3 scale, float yaw = 0) => (Shops + src + ".prefab", new Vector3(x, y, z), scale, yaw);
    static readonly (string cat, string id, (string src, Vector3 pos, Vector3 scale, float yaw)[] parts)[] Sets = {
        ("Furniture", "cafe_table", new[] { R("Props/SM_Prop_Cafe_Table_Small_01", 0, 0, 0, 0, .85f), R("Props/SM_Prop_Cafe_Chair_02", 0, 0, -.65f), R("Props/SM_Prop_Cafe_Chair_02", 0, 0, .65f, 180) }),
        ("Furniture", "stool_pair", new[] { R("Props/SM_Prop_Cafe_Table_Small_03", 0, 0, 0, 0, .78f), RS("Props/SM_Prop_Bar_Stool_01", 0, 0, -.62f, new Vector3(1.1f, .62f, 1.1f)), RS("Props/SM_Prop_Bar_Stool_01", 0, 0, .62f, new Vector3(1.1f, .62f, 1.1f), 180) }),
        ("Furniture", "booth_coral", new[] { R("Props/SM_Prop_Cafe_Booth_Seat_01", 0, 0, -.8f), R("Props/SM_Prop_Cafe_Booth_Seat_01", 0, 0, .8f, 180), RS("Props/SM_Prop_Cafe_Table_Large_01", 0, 0, 0, new Vector3(.85f, .7f, .5f)) }),
        ("Furniture", "booth_teal", new[] { R("Props/SM_Prop_Cafe_Booth_Seat_01", 0, 0, -.8f), R("Props/SM_Prop_Cafe_Booth_Seat_01", 0, 0, .8f, 180), RS("Props/SM_Prop_Cafe_Table_Large_01", 0, 0, 0, new Vector3(.85f, .7f, .5f)) }),
        ("Furniture", "communal_table", new[] { RS("Props/SM_Prop_Cafe_Table_Large_01", -.95f, 0, 0, new Vector3(.95f, .7f, .8f)), RS("Props/SM_Prop_Cafe_Table_Large_01", .95f, 0, 0, new Vector3(.95f, .7f, .8f)),
            R("Props/SM_Prop_Cafe_Chair_01", -1.2f, 0, -.72f), R("Props/SM_Prop_Cafe_Chair_01", 0, 0, -.72f), R("Props/SM_Prop_Cafe_Chair_01", 1.2f, 0, -.72f),
            R("Props/SM_Prop_Cafe_Chair_01", -1.2f, 0, .72f, 180), R("Props/SM_Prop_Cafe_Chair_01", 0, 0, .72f, 180), R("Props/SM_Prop_Cafe_Chair_01", 1.2f, 0, .72f, 180) }),
        ("Furniture", "pendant_amber", new[] { R("Props/SM_Prop_Lighting_Ceiling_Shaded_03", 0, 2.6f, 0) }),
        ("Furniture", "art_orbit", new[] { R("Props/SM_Prop_Art_Frame_01", 0, 2.05f, -.4f), R("Props/SM_Prop_Art_03", 0, 2.1f, -.37f) }),
        ("Furniture", "fern", new[] { R("Environments/SM_Env_Plant_03", 0, 0, 0, 0, .75f) }),
        ("Furniture", "bistro_table", new[] { R("Props/SM_Prop_Cafe_Table_Small_02", 0, 0, 0, 0, .9f), R("Props/SM_Prop_Cafe_Table_Cloth_01", 0, .79f, 0, 0, 1.02f), R("Props/SM_Prop_Cafe_Chair_01", 0, 0, -.65f), R("Props/SM_Prop_Cafe_Chair_01", 0, 0, .65f, 180) }),
        ("Furniture", "industrial_pendant", new[] { R("Props/SM_Prop_Lighting_Ceiling_Industrial_01", 0, 3.8f, 0) }),
        ("Furniture", "wall_sconce", new[] { R("Props/SM_Prop_Lighting_Wall_02", 0, 2.1f, -.32f, 180, 1.3f) }),
        ("Furniture", "poster_wall", new[] { R("Signs/SM_Prop_Poster_03", 0, 1.7f, -.4f, 180, 1.6f) }),
        ("Furniture", "art_abstract", new[] { R("Props/SM_Prop_Art_Frame_02", 0, 1.9f, -.38f, 180), R("Props/SM_Prop_Art_03", 0, 1.9f, -.4f, 180, 1.15f) }),
        ("Furniture", "flower_stand", new[] { R("Props/SM_Prop_Flower_Stand_Preset_01", 0, 0, -.4f, 0, .9f) }),
        ("Furniture", "cafe_divider", new[] { R("Props/SM_Prop_Cafe_Divider_01", 0, .24f, 0, 0, .9f) }),
        ("Furniture", "menu_screen", new[] { R("Props/SM_Prop_Kitchen_Menu_Screen_01", 0, 2.4f, -.45f, 180) }),
        ("Furniture", "wall_tv", new[] { R("Props/SM_Prop_Computer_TV_Wall_01", 0, 2.1f, -.45f, 180) }),
        ("Furniture", "sign_burger", new[] { R("Signs/SM_Sign_3dText_Burger_01", 0, 2.55f, -.44f, 180, .95f) }),
        ("Furniture", "oven", new[] { R("Props/SM_Prop_Kitchen_Stove_Oven_01", 0, 0, 0, 0, 1.05f) }),
        ("Furniture", "plate_rack", new[] { RS("Props/SM_Prop_Kitchen_ServingShelf_01", 0, 0, 0, new Vector3(.64f, 1f, .85f)),
            R("Food/SM_Prop_Food_Plate_01", -.2f, 1.16f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", -.2f, 1.185f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", -.2f, 1.21f, 0, 0, .8f),
            R("Food/SM_Prop_Food_Plate_01", .2f, 1.16f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", .2f, 1.185f, 0, 0, .8f) }),
    };
    // Slots we deliberately went back to the code-built (grimy-levelled) look for.
    static readonly string[] Retired = { "Furniture/pantry", "Furniture/fridge" };
    public static void GenerateOverrides() {
        foreach (var r in Retired) { string path = "Assets/Resources/ArtOverrides/" + r + ".prefab"; if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) AssetDatabase.DeleteAsset(path); }
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
                model.transform.localPosition = part.pos; model.transform.localRotation = Quaternion.Euler(0, part.yaw, 0); model.transform.localScale = Vector3.Scale(model.transform.localScale, part.scale); any = true;
            }
            if (any) PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);
        }
        foreach (var slot in Sets) {
            string outDir = "Assets/Resources/ArtOverrides/" + slot.cat, outPath = outDir + "/" + slot.id + ".prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Synty/PolygonShops")) { if (File.Exists(outPath)) AssetDatabase.DeleteAsset(outPath); continue; }
            Directory.CreateDirectory(outDir);
            var root = new GameObject(slot.id); bool any = false;
            foreach (var part in slot.parts) {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(part.src); if (!src) continue;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(src); model.transform.SetParent(root.transform, false);
                model.transform.localPosition = part.pos; model.transform.localRotation = Quaternion.Euler(0, part.yaw, 0);
                model.transform.localScale = Vector3.Scale(model.transform.localScale, part.scale); any = true;
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
