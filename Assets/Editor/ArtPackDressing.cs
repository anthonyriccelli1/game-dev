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
        ("Items", "RawSausage", new[] { P(Shops + "Food/SM_Prop_Food_Sausage_Raw_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Items", "CookedSausage", new[] { P(Shops + "Food/SM_Prop_Food_Sausage_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Items", "BurntSausage", new[] { P(Shops + "Food/SM_Prop_Food_Sausage_Burnt_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Parts", "HotDog", new[] { P(Shops + "Food/SM_Prop_Food_Hot_Dog_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Parts", "Sausage", new[] { P(Shops + "Food/SM_Prop_Food_Sausage_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Items", "RawEgg", new[] { P(Shops + "Food/SM_Prop_Food_Egg_Raw_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f), 0f) }),
        ("Items", "FriedEgg", new[] { P(Shops + "Food/SM_Prop_Food_Egg_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f), 0f) }),
        ("Items", "BurntEgg", new[] { P(Shops + "Food/SM_Prop_Food_Egg_Burnt_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f), 0f) }),
        ("Items", "EggWhole", new[] { P(Shops + "Food/SM_Prop_Food_Egg_Whole_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 1.1f, 1.1f), 0f) }),
        ("Parts", "Egg", new[] { P(Shops + "Food/SM_Prop_Food_Egg_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1.2f, 1.2f, 1.2f), 0f) }),
        ("Parts", "Bacon", new[] { P(Shops + "Food/SM_Prop_Food_Bacon_Cooked_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
        ("Parts", "Cup", new[] { P(Shops + "Food/SM_Prop_Food_Cup_01.prefab", new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), 0f) }),
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
        // Wooden folding bistro set for the sidewalk (the stand's tables) and patios.
        ("Furniture", "patio_table", new[] { R("Props/SM_Prop_Cafe_Table_Folding_01", 0, 0, 0, 0, 1.12f), R("Props/SM_Prop_Cafe_Chair_Folding_01", 0, 0, -.62f), R("Props/SM_Prop_Cafe_Chair_Folding_01", 0, 0, .62f, 180) }),
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
        // Glass-door reach-in fridge: you can see the stock on its shelves (PantryDisplay uses these shelf heights).
        ("Furniture", "fridge", new[] { R("Props/SM_Prop_Market_Drinks_Fridge_02", 0, 0, 0) }),
        // Swirl & Fizz machine (soft serve + soda). The cup's pour point is set by RestaurantArt ("PourPoint").
        ("Furniture", "drink_machine", new[] { RS("Props/SM_Prop_Kitchen_Prep_Table_02", 0, 0, 0, new Vector3(.86f, .95f, .85f)), R("Props/SM_Prop_Kitchen_Ice_Cream_Machine_01", 0, 1.0f, -.12f, 0, .92f),
            R("Food/SM_Prop_Food_Cup_01", .36f, 1.0f, .22f, 0, .8f), R("Food/SM_Prop_Food_Cup_01", .36f, 1.2f, .22f, 0, .8f) }),
        ("Furniture", "oven", new[] { R("Props/SM_Prop_Kitchen_Stove_Oven_01", 0, 0, 0, 0, 1.05f) }),
        ("Furniture", "plate_rack", new[] { RS("Props/SM_Prop_Kitchen_ServingShelf_01", 0, 0, 0, new Vector3(.64f, 1f, .85f)),
            R("Food/SM_Prop_Food_Plate_01", -.2f, 1.16f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", -.2f, 1.185f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", -.2f, 1.21f, 0, 0, .8f),
            R("Food/SM_Prop_Food_Plate_01", .2f, 1.16f, 0, 0, .8f), R("Food/SM_Prop_Food_Plate_01", .2f, 1.185f, 0, 0, .8f) }),
    };
    // Slots we deliberately went back to the code-built (grimy-levelled) look for.
    static readonly string[] Retired = { "Furniture/pantry", "Furniture/partition_wall", "Furniture/service_window" };
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
        GenerateFinishes();
        GenerateShell();
        GenerateArchitecture();
        StreetStandArt.Generate();
        AssetDatabase.SaveAssets();
    }

    // ---- Restaurant shell: pack-textured floors/walls, a real storefront, trims and awnings ----
    const string Tex = "Assets/Synty/PolygonShops/Textures/PolygonShops_Building_";
    const string Bld = "Assets/Synty/PolygonShops/Prefabs/Buildings/";
    const string ShellDir = "Assets/Resources/ArtOverrides/Shell", FinishDir = "Assets/Resources/ArtOverrides/Finishes";
    // (finish id, pack texture, tint, repeats per metre, smoothness). UVs on our walls/floors are in metres.
    static readonly (string id, string tex, Color tint, float tiling, float smooth)[] Finishes = {
        ("floor_checker", "Tile_03", Color.white, .5f, .32f),
        ("floor_wood", "Wood_02", new Color(1f, .95f, .88f), .8f, .22f),
        ("floor_ceramic", "Tile_02", Color.white, .75f, .42f),
        ("floor_clay", "Tile_01", new Color(1f, .7f, .55f), .8f, .25f),
        ("floor_blue", "Tile_05", new Color(.72f, .88f, 1f), .5f, .42f),
        ("floor_parquet", "Wood_01", Color.white, .8f, .28f),
        ("floor_marble", "Marble_01", Color.white, .5f, .6f),
        ("floor_slate", "Tile_04", new Color(.85f, .95f, 1f), .8f, .3f),
        ("floor_stone", "Tile_01", Color.white, .8f, .25f),
        ("wall_brick", "Brick_Coloured_01", new Color(1f, .72f, .6f), .8f, .05f),
        ("wall_whitebrick", "Brick_White_01", Color.white, .8f, .05f),
        ("wall_panel", "Wood_01", new Color(.8f, .65f, .55f), .8f, .2f),
        ("wall_midnight", "Tile_04", new Color(.6f, .72f, 1f), .8f, .4f),
        // The starter room before you renovate: grubby stone floor, tired whitewashed brick.
        ("shabby_floor", "Tile_01", new Color(.8f, .74f, .62f), .8f, .1f),
        ("shabby_wall", "Brick_White_01", new Color(.84f, .78f, .68f), .8f, .02f),
    };
    static Material SaveMat(string path, Texture tex, Color tint, Vector2 tiling, float smooth) {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); m.mainTexture = tex; m.mainTextureScale = tiling;
        m.SetColor("_BaseColor", tint); m.color = tint; m.SetFloat("_Smoothness", smooth);
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { existing.shader = m.shader; existing.CopyPropertiesFromMaterial(m); EditorUtility.SetDirty(existing); Object.DestroyImmediate(m); return existing; }
        AssetDatabase.CreateAsset(m, path); return m;
    }
    static void GenerateFinishes() {
        bool pack = AssetDatabase.IsValidFolder("Assets/Synty/PolygonShops");
        Directory.CreateDirectory(FinishDir);
        foreach (var f in Finishes) {
            string path = FinishDir + "/" + f.id + ".mat";
            var tex = pack ? AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + f.tex + ".png") : null;
            if (!tex) { if (File.Exists(path)) AssetDatabase.DeleteAsset(path); continue; }
            SaveMat(path, tex, f.tint, Vector2.one * f.tiling, f.smooth);
        }
    }

    // Instantiates a pack prefab at a yaw (0/90/180/-90), stretches it along the flagged world axes to fill [min,max],
    // then snaps it so its bounds start at min (or end at max where alignMax is set). Pack colliders are removed.
    static GameObject FitBox(Transform root, string src, float yaw, Vector3 min, Vector3 max, Vector3 fit, Vector3 alignMax) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(src); if (!prefab) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(root, false);
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var b = Measure(go);
        Vector3 world = new Vector3(fit.x > 0 ? (max.x - min.x) / b.size.x : 1, fit.y > 0 ? (max.y - min.y) / b.size.y : 1, fit.z > 0 ? (max.z - min.z) / b.size.z : 1);
        bool side = Mathf.Abs(Mathf.Repeat(yaw, 180f) - 90f) < 1f;
        go.transform.localScale = Vector3.Scale(go.transform.localScale, side ? new Vector3(world.z, world.y, world.x) : world);
        b = Measure(go);
        go.transform.localPosition += new Vector3(alignMax.x > 0 ? max.x - b.max.x : min.x - b.min.x, alignMax.y > 0 ? max.y - b.max.y : min.y - b.min.y, alignMax.z > 0 ? max.z - b.max.z : min.z - b.min.z);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        return go;
    }
    static Bounds Measure(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

    // Placeable 2x1-tile room modules. Their roots sit at floor centre; the code-built modules own collision.
    // The wall meshes are 2.5m wide in the pack and the cafe counter is 2.07m wide, so fit the art to
    // the shared 2m span without changing the collision footprint or creating a lip across the aisle.
    static void GenerateArchitecture() {
        // Partition walls and service windows are code-built with metre UVs so wall finishes paint them like
        // the room walls (see ArchitectureArt); only the counter uses pack art.
        MakeArchitecture("service_counter", Shops + "Props/SM_Prop_Cafe_Counter_Outdoor_02.prefab", new Vector3(2f, .98f, .5f), false);
    }
    static void MakeArchitecture(string id, string source, Vector3 size, bool openWindow) {
        const string dir = "Assets/Resources/ArtOverrides/Furniture";
        string path = dir + "/" + id + ".prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(source)) {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            return;
        }
        Directory.CreateDirectory(dir);
        var root = new GameObject(id);
        var model = FitBox(root.transform, source, 0, new Vector3(-size.x * .5f, 0, -size.z * .5f),
            new Vector3(size.x * .5f, size.y, size.z * .5f), Vector3.one, Vector3.zero);
        if (openWindow) {
            // A solid glass pane would make the serving opening look closed even though the gameplay
            // collider deliberately blocks walking through it. Keep the frame and sill, remove only glass.
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name.EndsWith("_Glass")) Object.DestroyImmediate(renderer.gameObject);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
    // Repeats a trim strip along one wall: `count` equal pieces between a and b (world X or Z), at height y.
    static void Run(Transform root, string src, int count, bool alongX, float a, float b, float y, float wall, float yaw, bool towardMax) {
        float step = (b - a) / count;
        for (int i = 0; i < count; i++) {
            float s0 = a + step * i, s1 = s0 + step;
            var min = alongX ? new Vector3(s0, y, wall) : new Vector3(wall, y, s0);
            var max = alongX ? new Vector3(s1, y, wall) : new Vector3(wall, y, s1);
            FitBox(root, src, yaw, min, max, alongX ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1), alongX ? new Vector3(0, 0, towardMax ? 1 : 0) : new Vector3(towardMax ? 1 : 0, 0, 0));
        }
    }
    static GameObject Block(Transform root, string name, Vector3 center, Vector3 size, Material m) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(root, false); g.transform.localPosition = center; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m; return g;
    }
    static void Save(GameObject root, string id) { Directory.CreateDirectory(ShellDir); PrefabUtility.SaveAsPrefabAsset(root, ShellDir + "/" + id + ".prefab"); Object.DestroyImmediate(root); }

    // Room-local coordinates (see RestaurantArt.BuildRoom): room x -16.5..-3.5, back wall z -22, street front z -9
    // (the street is +Z), walk-in doorway x -11.5..-8.5 kept clear by our own invisible colliders.
    static void GenerateShell() {
        if (!AssetDatabase.IsValidFolder("Assets/Synty/PolygonShops")) {
            foreach (var id in new[] { "facade", "interior_trim", "awning", "storefront_sign" }) { string path = ShellDir + "/" + id + ".prefab"; if (File.Exists(path)) AssetDatabase.DeleteAsset(path); }
            return;
        }
        Directory.CreateDirectory(ShellDir);
        Vector3 X = new Vector3(1, 0, 0), None = Vector3.zero, Zmax = new Vector3(0, 0, 1);
        // Storefront: two three-pane shop windows either side of the doorway, brick pillars, a brick sign band,
        // a cornice and a roof edge. Our code-built sign, lamps and address plaque sit on top of it.
        var f = new GameObject("facade").transform;
        FitBox(f, Bld + "SM_Bld_ShopFront_01.prefab", 0, new Vector3(-16.5f, 0, -9.15f), new Vector3(-11.5f, 0, 0), X, None);
        FitBox(f, Bld + "SM_Bld_ShopFront_01.prefab", 0, new Vector3(-8.5f, 0, -9.15f), new Vector3(-3.5f, 0, 0), X, None);
        foreach (float x in new[] { -11.5f, -8.5f, -16.5f, -3.5f }) FitBox(f, Bld + "SM_Bld_Base_Pillar_01.prefab", 0, new Vector3(x - .22f, 0, -9.2f), new Vector3(x + .22f, 2.9f, 0), new Vector3(1, 1, 0), None);
        var brick = SaveMat(ShellDir + "/fascia_brick.mat", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "Brick_Coloured_01.png"), new Color(1f, .72f, .6f), new Vector2(10.6f, 1.3f), .05f);
        Block(f, "Sign band (brick)", new Vector3(-10, 3.58f, -8.98f), new Vector3(13.3f, 1.64f, .36f), brick);
        Run(f, Bld + "SM_Bld_Trim_Wall_High_02.prefab", 6, true, -16.7f, -3.3f, 4.02f, -8.62f, 0, true);
        Run(f, Bld + "SM_Bld_Roof_Edge_01.prefab", 6, true, -16.7f, -3.3f, 4.38f, -8.55f, 0, true);
        Run(f, Bld + "SM_Bld_Trim_Wall_Low_01.prefab", 2, true, -16.5f, -11.7f, 0, -8.62f, 0, true);
        Run(f, Bld + "SM_Bld_Trim_Wall_Low_01.prefab", 2, true, -8.3f, -3.5f, 0, -8.62f, 0, true);
        // Glass double doors, propped open onto the sidewalk (decor only; the doorway stays clear).
        FitBox(f, Bld + "SM_Bld_Shopfront_Door_01.prefab", 90, new Vector3(-11.3f, 0, -8.78f), new Vector3(0, 2.6f, 0), new Vector3(0, 1, 0), None);
        FitBox(f, Bld + "SM_Bld_Shopfront_Door_01.prefab", -90, new Vector3(0, 0, -8.78f), new Vector3(-8.7f, 2.6f, 0), new Vector3(0, 1, 0), X);
        Save(f.gameObject, "facade");

        GenerateStorefrontSign();

        // Awning upgrade: two striped canvas awnings over each shop window.
        var a = new GameObject("awning").transform;
        foreach (float x0 in new[] { -16.3f, -13.95f, -8.3f, -5.95f })
            FitBox(a, Bld + "SM_Bld_Awning_04.prefab", 0, new Vector3(x0, 2.2f, -8.84f), new Vector3(x0 + 2.3f, 0, 0), X, None);
        Save(a.gameObject, "awning");

        // Interior: a wainscot base, a chair rail and a crown moulding on the back and side walls.
        var t = new GameObject("interior_trim").transform;
        const float back = -21.875f, left = -16.375f, right = -3.625f, front = -9.15f;
        foreach (var (src, y) in new[] { ("SM_Bld_Trim_Wall_Low_01", 0f), ("SM_Bld_Trim_Wall_High_01", 1.02f), ("SM_Bld_Trim_Ceiling_01", 3.47f) }) {
            Run(t, Bld + src + ".prefab", 5, true, left, right, y, back, 0, false);
            Run(t, Bld + src + ".prefab", 5, false, back, front, y, left, 90, false);
            Run(t, Bld + src + ".prefab", 5, false, back, front, y, right, -90, true);
        }
        Save(t.gameObject, "interior_trim");
    }

    // The pack supplies a separate mesh for every letter. Build the restaurant name from those
    // meshes so it has real depth, consistent spacing, and word-by-word color on the street.
    static void GenerateStorefrontSign() {
        var backingSource = AssetDatabase.LoadAssetAtPath<GameObject>(Shops + "Signs/SM_Sign_Backing_Long_01.prefab");
        if (!backingSource) return;
        var dark = SaveMat(ShellDir + "/sign_ink.mat", null, new Color(.085f, .18f, .19f), Vector2.one, .13f);
        var cream = SaveMat(ShellDir + "/sign_cream.mat", null, new Color(1f, .87f, .65f), Vector2.one, .3f);
        var coral = SaveMat(ShellDir + "/sign_coral.mat", null, new Color(.96f, .39f, .29f), Vector2.one, .3f);
        var brass = SaveMat(ShellDir + "/sign_brass.mat", null, new Color(.89f, .65f, .34f), Vector2.one, .37f);
        // The letters glow softly so the name still reads on Main Street at night.
        foreach (var m in new[] { cream, coral, brass }) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", m.color * .55f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(m); }
        var root = new GameObject("storefront_sign");
        var backing = (GameObject)PrefabUtility.InstantiatePrefab(backingSource, root.transform);
        backing.name = "POLYGON Shops sign backing";
        backing.transform.localPosition = new Vector3(-10f, 3.46f, -8.65f);
        backing.transform.localScale = Vector3.Scale(backing.transform.localScale, new Vector3(1f, .88f, 1f));
        foreach (var renderer in backing.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = dark;
        foreach (var collider in backing.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);

        const string name = "THE ODD TABLE";
        var letters = new System.Collections.Generic.List<(GameObject model, float width)>();
        float totalWidth = 0f;
        for (int i = 0; i < name.Length; i++) {
            char c = name[i];
            if (c == ' ') { totalWidth += .24f; continue; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Shops + "Signs/SM_Sign_3dText_Letter_" + c + ".prefab");
            if (!source) { Object.DestroyImmediate(root); return; }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "POLYGON Shops letter " + c + " " + i;
            // Pack letters face +Z (the street) unrotated; turning them 180 showed their backs, mirrored.
            model.transform.localRotation = Quaternion.identity;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
            float scale = .86f / mesh.bounds.size.y;
            model.transform.localScale *= scale;
            float width = Measure(model).size.x;
            letters.Add((model, width));
            totalWidth += width + .055f;
            var color = i < 3 ? brass : i < 7 ? coral : cream;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = color;
        }
        totalWidth -= .055f;
        // Seen from the street (looking toward -Z), reading left to right runs toward -X.
        float cursor = -10f + totalWidth * .5f;
        int letterIndex = 0;
        foreach (char c in name) {
            if (c == ' ') { cursor -= .24f; continue; }
            var (model, width) = letters[letterIndex++];
            var bounds = Measure(model);
            model.transform.localPosition += new Vector3(cursor - bounds.max.x,
                3.07f - bounds.min.y, -8.53f - bounds.center.z);
            cursor -= width + .055f;
        }
        Save(root, "storefront_sign");
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
        if (!CityMap.Available) {   // the city build dresses Main Street itself (CityMainStreet.cs): no trees mid-sidewalk
            Put(Generic + "Props/SM_Gen_Prop_Barrel_Metal_01.prefab", new Vector3(9.4f, 0, 15.2f), 0, root);
            Put(Starter + "SM_Generic_Tree_01.prefab", new Vector3(-6.6f, 0, 12.2f), 0, root);
            Put(Starter + "SM_Generic_Tree_02.prefab", new Vector3(7.2f, 0, 12.2f), 90, root);
            Put(Starter + "SM_Generic_Tree_03.prefab", new Vector3(-18.5f, 0, -7.6f), 0, root);
            Put(Starter + "SM_Generic_Tree_04.prefab", new Vector3(13.5f, 0, -7.6f), 45, root);
        }
    }
}
