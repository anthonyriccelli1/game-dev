using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Every Old Market shopfront to the Main Street standard (see CityMainStreet.cs): a glass shop module is either a real
// shop you can look into (a furnished room from the Shops pack behind clear glass, its name in pack 3D letters) or a
// roller-shuttered unit to let. Corner shops get the same treatment on both street faces. Off Main Street the names
// are painted letters, never neon. Themes rotate along the street so neighbours differ.
public static partial class CityMap {
    class StreetTheme {
        public string[] Names; public string Wall, Floor, Sign; public Color Light; public bool CornerSafe;
        // Centred placement: the prop's footprint is centred on (lat, depth) and it stands on y (0 = the shop floor).
        public (string prop, float lat, float depth, float y, float yaw, float scale)[] Items;
    }
    static readonly StreetTheme[] StreetThemes = {
        new StreetTheme { Names = new[] { "BLACK BEAN", "MORNING MUG", "DRIP" }, Wall = "F2E6D3", Floor = "6E4B33", Sign = "B8652F", Light = new Color(1f, .86f, .68f), CornerSafe = true, Items = new[] {
            ("Kitchen_Counter_01", 1.1f, 4.15f, 0f, 0f, 1f), ("Shop_Table_02", -1.25f, 4.25f, 0f, 0f, 1f), ("Cafe_Machine_01", -1.25f, 4.3f, 1.05f, 0f, 1f),
            ("Shop_Table_02", 0f, 1.15f, 0f, 180f, 1f), ("Bar_Stool_01", -.55f, 1.95f, 0f, 0f, 1f), ("Bar_Stool_01", .55f, 1.95f, 0f, 0f, 1f) } },
        new StreetTheme { Names = new[] { "CORNER MART", "DAILY FRESH", "QUICK STOP" }, Wall = "EDEBE3", Floor = "8F9089", Sign = "2E8E55", Light = new Color(.95f, .97f, 1f), CornerSafe = true, Items = new[] {
            ("Market_Food_Display_01", 0f, 4.43f, 0f, 0f, 1f), ("Market_Aisle_Preset_01", -.95f, 2.65f, 0f, 90f, 1f), ("Market_Aisle_Preset_02", .95f, 2.65f, 0f, 90f, 1f),
            ("Market_Checkout_Shelf_01", 1.8f, 1.0f, 0f, 90f, 1f) } },
        new StreetTheme { Names = new[] { "FULL COURT", "GAME ON", "BIG LEAGUE" }, Wall = "E8EDF0", Floor = "4A6B8A", Sign = "D9502A", Light = new Color(.95f, .96f, 1f), Items = new[] {
            ("Shop_Shelf_01", 0f, 4.5f, 0f, 0f, 1f), ("Shop_Table_02", 0f, 2.7f, 0f, 0f, 1f),
            ("Sport_Skateboard_01", -.5f, 2.7f, 1.05f, 0f, 1f), ("Sport_Skateboard_02", 0f, 2.7f, 1.05f, 0f, 1f), ("Sport_Skateboard_03", .5f, 2.7f, 1.05f, 0f, 1f),
            ("Shop_Plinth_01_Large", -1.5f, 1.3f, 0f, 0f, 1f), ("Sport_Basketball_01", -1.5f, 1.3f, .8f, 0f, 1f),
            ("Shop_Plinth_01_Large", 1.5f, 1.3f, 0f, 0f, 1f), ("Sport_Helmet_01", 1.5f, 1.3f, .8f, 0f, 1f) } },
        new StreetTheme { Names = new[] { "WILD TRAIL", "BASECAMP", "OUTPOST" }, Wall = "D9CDB5", Floor = "5B4A3A", Sign = "4E7E33", Light = new Color(1f, .9f, .74f), Items = new[] {
            ("Hunting_Shelf_01_Preset", -1.0f, 4.5f, 0f, 0f, 1f), ("Hunting_Fishing_Rod_Rack_Preset_01", 1.6f, 4.65f, 0f, 0f, .9f),
            ("Hunting_Tent_Dome_01", -.6f, 2.0f, 0f, 20f, .55f), ("Hunting_Backpack_01", 1.6f, 1.3f, 0f, 200f, 1f), ("Hunting_Backpack_02", 1.2f, 1.5f, 0f, 160f, 1f) } },
        new StreetTheme { Names = new[] { "HOT SLICE", "PIE HOLE", "DOUGH BOYS" }, Wall = "F0E0C8", Floor = "3B2F2A", Sign = "C9402A", Light = new Color(1f, .82f, .6f), CornerSafe = true, Items = new[] {
            ("Kitchen_Pizza_Oven_01", -1.2f, 4.05f, 0f, 0f, .62f), ("Kitchen_Prep_Table_01", 1.2f, 4.25f, 0f, 0f, 1f),
            ("Shop_Table_02", 0f, 1.15f, 0f, 180f, 1f), ("Bar_Stool_01", -.55f, 1.95f, 0f, 0f, 1f), ("Bar_Stool_01", .55f, 1.95f, 0f, 0f, 1f) } },
        new StreetTheme { Names = new[] { "THE TAP ROOM", "LAST ORDERS", "NIGHTCAP" }, Wall = "3A2A22", Floor = "2A1E18", Sign = "D99A3A", Light = new Color(1f, .74f, .45f), CornerSafe = true, Items = new[] {
            ("Bar_Shelf_01", 0f, 4.56f, 0f, 0f, 1f), ("Bar_Bench_01", 0f, 3.2f, 0f, 0f, 1f),
            ("Bar_Stool_01", -1f, 2.25f, 0f, 0f, 1f), ("Bar_Stool_01", 0f, 2.25f, 0f, 0f, 1f), ("Bar_Stool_01", 1f, 2.25f, 0f, 0f, 1f) } },
        new StreetTheme { Names = new[] { "GALLERY NINE", "OPEN STUDIO", "FRAMED" }, Wall = "F7F4EE", Floor = "CFC6B8", Sign = "2B2B2B", Light = new Color(1f, .97f, .92f), Items = new[] {
            ("Art_01", -1.2f, 4.8f, 1.1f, 0f, 1f), ("Art_03", 1.2f, 4.8f, 1.1f, 0f, 1f), ("Art_05", 2.38f, 2.6f, 1.1f, -90f, 1f), ("Art_06", -2.38f, 2.6f, 1.1f, 90f, 1f),
            ("Clothes_Bench_01", 0f, 2.7f, 0f, 0f, 1f) } },
        new StreetTheme { Names = new[] { "SOLE STORE", "STEP UP", "KICKS" }, Wall = "EDE6DD", Floor = "7A6656", Sign = "7C4AB0", Light = new Color(1f, .93f, .84f), Items = new[] {
            ("Clothes_Display_Shelf_Green_01", -1.4f, 4.68f, 0f, 0f, 1f), ("Clothes_Display_Shelf_Green_01", 0f, 4.68f, 0f, 0f, 1f), ("Clothes_Display_Shelf_Green_01", 1.4f, 4.68f, 0f, 0f, 1f),
            ("Clothes_Bench_01", 0f, 2.6f, 0f, 0f, 1f), ("Clothes_Mirror_01", 2.38f, 2.6f, 0f, -90f, 1f),
            ("Shoe_Box_Open_01", .7f, 1.9f, 0f, 20f, 1f), ("Shoe_Box_Open_02", -.7f, 2.0f, 0f, -15f, 1f) } },
        new StreetTheme { Names = new[] { "SNAPSHOT", "F STOP", "SHUTTERBUG" }, Wall = "E4E2DE", Floor = "2F2F33", Sign = "E0A52C", Light = new Color(.96f, .97f, 1f), Items = new[] {
            ("Shop_Shelf_01", 0f, 4.5f, 0f, 0f, 1f), ("Shop_Case_01", -1.4f, 2.4f, 0f, 0f, 1f), ("Shop_Case_01", 0f, 2.4f, 0f, 0f, 1f), ("Shop_Case_01", 1.4f, 2.4f, 0f, 0f, 1f),
            ("Computer_Camera_DSLR_01", -1.4f, 2.4f, 1.54f, 30f, 1f), ("Computer_Camera_Mirrorless_01", 0f, 2.4f, 1.54f, -20f, 1f), ("Computer_Camera_Tripod_01", 1.9f, 1.2f, 0f, 0f, 1f) } },
    };
    // The Main Street themes (pivot-placed, CityMainStreet.cs) reused elsewhere under other names.
    static readonly (string key, string[] names)[] ReusedThemes = {
        ("barber", new[] { "CLIP JOINT", "FADE HOUSE" }), ("records", new[] { "VINYL VAULT", "B SIDE" }), ("florist", new[] { "PETAL POST", "STEM" }),
        ("gadgets", new[] { "BYTE SHOP", "PLUGGED IN" }), ("arcade", new[] { "HIGH SCORE", "EXTRA LIFE" }), ("threads", new[] { "STITCH", "WARDROBE" }), ("gym", new[] { "LIFT CLUB", "REP HOUSE" }),
    };
    static int themeTurn;
    public static int StreetShopCount, StreetLeaseCount, StreetShopLights;

    // A street shop cell built by Tower (not Main Street's named shops, not hand-placed gameplay buildings).
    static void StreetFront(Transform b, string ground, float x0, float z0, int facing, int s, bool climb) {
        if (ground.EndsWith("Shop_06")) { LeaseShutter(b, x0, z0, facing); return; }
        int pool = StreetThemes.Length + ReusedThemes.Length, i = (themeTurn++ * 5) % pool;
        if (i < StreetThemes.Length) { var t = StreetThemes[i]; ShopRoom(b, x0, z0, facing, s, false, climb, t.Names[H(s, 59) % t.Names.Length], t.Wall, t.Floor, t.Sign, t.Light, t.Items, null); }
        else {
            var (key, names) = ReusedThemes[i - StreetThemes.Length]; var t = Themes[key];
            ShopRoom(b, x0, z0, facing, s, false, climb, names[H(s, 59) % names.Length], t.Wall, t.Floor, t.Sign, t.Light, null, t.Items);
        }
    }
    // A corner shop: glass on two street faces (facing and facing+1), the room's walls on the two inner sides.
    static void CornerFront(Transform b, float x0, float z0, int facing, int s) {
        var safe = System.Array.FindAll(StreetThemes, t => t.CornerSafe); var t = safe[(themeTurn++ * 3 + H(s, 61)) % safe.Length];
        ShopRoom(b, x0, z0, facing, s, true, false, t.Names[H(s, 59) % t.Names.Length], t.Wall, t.Floor, t.Sign, t.Light, t.Items, null);
    }

    static void ShopRoom(Transform b, float x0, float z0, int facing, int s, bool corner, bool climb, string name, string wallHex, string floorHex, string signHex, Color light,
                         (string prop, float lat, float depth, float y, float yaw, float scale)[] centred, (string prop, float lat, float depth, float y, float yaw)[] pivoted) {
        OpenAnyShopfront(b);
        float yaw = 90 * facing;
        Vector3 At(float lat, float depth, float y) => OnFace(x0, z0, facing, lat, -depth, y);
        var room = new GameObject("Shop interior / " + name).transform; room.SetParent(b, false);
        Material wall = InteriorMat("Shop" + wallHex, wallHex), floor = InteriorMat("ShopFloor" + floorHex, floorHex);
        bool alongX = facing % 2 == 0;
        Vector3 Size(float lateral, float y, float depth) => alongX ? new Vector3(lateral, y, depth) : new Vector3(depth, y, lateral);
        Slab("Shop floor", At(0, 2.62f, .03f), Size(4.84f, .06f, 4.46f), floor, room, false);
        Slab("Shop ceiling", At(0, 2.62f, 2.93f), Size(4.84f, .06f, 4.46f), wall, room, false);
        Slab("Shop back wall", At(0, 4.86f, 1.48f), Size(4.84f, 2.96f, .06f), wall, room, false);
        foreach (int side in corner ? new[] { -1 } : new[] { -1, 1 }) Slab("Shop side wall", At(side * 2.43f, 2.62f, 1.48f), Size(.06f, 2.96f, 4.46f), wall, room, false);
        if (centred != null) foreach (var it in centred) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ShopProps + it.prop + ".prefab"); if (!src) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, room); StripColliders(go.transform);
            go.transform.rotation = Quaternion.Euler(0, yaw + it.yaw, 0); go.transform.localScale *= it.scale;
            var bnd = RendererBounds(go); var target = At(it.lat, it.depth, it.y + .06f);
            go.transform.position += new Vector3(target.x - bnd.center.x, target.y - bnd.min.y, target.z - bnd.center.z);
        }
        if (pivoted != null) foreach (var (prop, lat, depth, y, off) in pivoted) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ShopProps + prop + ".prefab"); if (!src) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, room);
            go.transform.SetPositionAndRotation(At(lat, depth, y), Quaternion.Euler(0, yaw + off, 0)); StripColliders(go.transform);
        }
        var shade = AssetDatabase.LoadAssetAtPath<GameObject>(ShopProps + "Lighting_Ceiling_Shaded_01.prefab");
        if (shade) { var sh = (GameObject)PrefabUtility.InstantiatePrefab(shade, room); sh.transform.SetPositionAndRotation(At(0, 2.4f, 2.38f), Quaternion.Euler(0, yaw, 0)); StripColliders(sh.transform); }
        // One light per shop at most, and only in two shops of three (performance: the rest read as closed for the night).
        if (H(s, 67) % 3 != 0) {
            var lamp = new GameObject("Shop light").AddComponent<Light>(); lamp.transform.SetParent(room, false); lamp.transform.position = At(0, 2.2f, 2.4f);
            lamp.type = LightType.Point; lamp.color = light; lamp.intensity = 1.4f; lamp.range = 6f; lamp.shadows = LightShadows.None; StreetShopLights++;
        }
        var paint = InteriorMat("Letters" + signHex, signHex);
        if (!climb) Letters3D(name, OnFace(x0, z0, facing, 0, .52f, 2.86f), yaw, .4f, 4.3f, paint, paint, b);   // a fire escape hangs there on climb towers
        StreetShopCount++;
    }
    // A roller-shuttered unit: some say FOR LEASE, the rest are just closed up.
    static void LeaseShutter(Transform b, float x0, float z0, int facing) {
        StreetLeaseCount++;
        if (H((int)x0, (int)z0, 71) % 2 == 0) return;
        var paint = InteriorMat("LeaseCard", "F2EBDD");
        Letters3D("FOR LEASE", OnFace(x0, z0, facing, -.55f, .08f, 1.9f), 90 * facing, .24f, 2.3f, paint, paint, b);
    }
    // Like OpenShopfront, but for every City shop module, corner units included.
    static void OpenAnyShopfront(Transform building) {
        foreach (var mf in building.GetComponentsInChildren<MeshFilter>()) {
            var src = mf.sharedMesh; if (!src || !src.name.StartsWith("SM_Bld_Shop_")) continue;
            if (src.name.EndsWith("_Glass")) { var r = mf.GetComponent<Renderer>(); if (r) r.sharedMaterial = ClearGlass(); continue; }
            if (src.name.EndsWith("_Open")) continue;
            if (!openCuts.TryGetValue(src, out var cut)) openCuts[src] = cut = CutDoor(src, 1, 1, 0, 1, 1, "Assets/Generated/Meshes/" + src.name + "_Open.asset");
            mf.sharedMesh = cut;
        }
    }
}
