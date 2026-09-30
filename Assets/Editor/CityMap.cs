using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds the explorable city from POLYGON City modules on a 5 m grid:
// a 160 m x 160 m district with three east-west avenues, two north-south streets, full sidewalks,
// modular shop/apartment rows, landmark towers, street furniture and a 360-degree skyline.
// Gameplay locations (stand, Milo's, your restaurant, The Gilded Orbit, rival alley) keep their spots on Main Street.
// This block IS Old Market (one restaurant per district): Market Row to the north, the home street in the middle,
// The Flats to the south. Street ends are barricaded; the Docks gate waits at the bottom of West Street.
public static class CityMap {
    const string City = "Assets/Synty/PolygonCity/Prefabs/";
    public static bool Available => AssetDatabase.IsValidFolder("Assets/Synty/PolygonCity");
    const float Cell = 5, RoadY = -.16f, WalkY = -.07f, Half = 80;

    // Road rectangles (x0, x1, z0, z1)
    static readonly Rect[] Roads = {
        Rect.MinMaxRect(-Half, -5, Half, 5),      // Main Street
        Rect.MinMaxRect(-Half, 45, Half, 55),     // North Avenue
        Rect.MinMaxRect(-Half, -55, Half, -45),   // South Avenue
        Rect.MinMaxRect(-45, -Half, -35, Half),   // West Street
        Rect.MinMaxRect(35, -Half, 45, Half),     // East Street
    };
    // Areas the generator must leave alone (gameplay buildings and interiors).
    static readonly Rect[] Reserved = {
        Rect.MinMaxRect(-19.5f, 5, 25, 32),       // Milo's, apartments, alley, rival restaurant, stash
        Rect.MinMaxRect(-17.5f, -26, -2.5f, -5),  // your restaurant and its interior
        Rect.MinMaxRect(-20, 60, 20, 70),         // Market Row: the street-market square off North Avenue
        Rect.MinMaxRect(-25, -40, -20, -25),      // The Flats: graffiti alley off South Avenue
        Rect.MinMaxRect(10, -40, 25, -25),        // The Flats: vacant lot where Greasy Gus parks his truck
        Rect.MinMaxRect(-25, 35, -15, 40),        // Market Row: The Tin Diner (a one-storey chrome diner)
    };
    static Transform root; static int seed;

    static bool IsRoad(float x, float z) { foreach (var r in Roads) if (r.Contains(new Vector2(x, z))) return true; return false; }
    static bool IsReserved(Rect r) { foreach (var q in Reserved) if (q.Overlaps(r)) return true; return false; }

    public static GameObject Prefab(string rel, Vector3 pos, float yaw, Transform parent) => Put(rel, pos, yaw, parent);
    static GameObject Put(string rel, Vector3 pos, float yaw, Transform parent = null) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(City + rel + ".prefab"); if (!src) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent ? parent : root);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); return go;
    }
    // Synty modules occupy [-5,0] x [-5,0] around their pivot and face +Z. Place one into cell [x0,x0+5] x [z0,z0+5]
    // facing 0:+Z 1:+X 2:-Z 3:-X.
    static GameObject Module(string rel, float x0, float z0, int facing, float y, Transform parent = null) {
        Vector3 p; float yaw;
        switch (facing) {
            case 1: p = new Vector3(x0 + 5, y, z0); yaw = 90; break;
            case 2: p = new Vector3(x0, y, z0); yaw = 180; break;
            case 3: p = new Vector3(x0, y, z0 + 5); yaw = 270; break;
            default: p = new Vector3(x0 + 5, y, z0 + 5); yaw = 0; break;
        }
        return Put(rel, p, yaw, parent);
    }

    // ---------- Building dressing ----------
    const int SeedBase = 911;
    static int H(int a, int b, int c = 0) { unchecked { int h = a * 73856093 ^ b * 19349663 ^ c * 83492791 ^ SeedBase; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h & 0x7fffffff; } }
    static readonly Vector3[] Fwd = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
    // A point on the street face of cell [x0,x0+5]x[z0,z0+5] facing `facing`: `lateral` along the facade (to the right when
    // looking at it from the street is negative), `outward` metres out from the facade line.
    static Vector3 OnFace(float x0, float z0, int facing, float lateral, float outward, float y) {
        var f = Fwd[facing]; var r = new Vector3(f.z, 0, -f.x);
        return new Vector3(x0 + 2.5f, y, z0 + 2.5f) + f * (2.5f + outward) + r * lateral;
    }
    static GameObject PutGeneric(string rel, Vector3 pos, float yaw, Transform parent = null) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/" + rel + ".prefab"); if (!src) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent ? parent : root);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); return go;
    }
    static GameObject PutTilted(string rel, Vector3 pos, float yaw, float pitch, Transform parent) {
        var go = Put(rel, pos, 0, parent); if (go) go.transform.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, 0); return go;
    }

    // Recolor with one of the pack's 12 palettes (0 = the default red-brick 01_A), so neighbours don't read as clones.
    static Material basePal; static readonly Dictionary<int, Material> pals = new Dictionary<int, Material>();
    static void Recolor(Transform b, int pick) {
        if (!basePal) basePal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Alts/PolygonCity_01_A.mat");
        pick %= 12; if (pick == 0 || !basePal) return;
        if (!pals.TryGetValue(pick, out var m)) {
            m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Alts/PolygonCity_0" + (pick / 3 + 1) + "_" + "ABC"[pick % 3] + ".mat");
            pals[pick] = m;
        }
        if (!m) return;
        foreach (var r in b.GetComponentsInChildren<Renderer>()) {
            var mats = r.sharedMaterials; bool hit = false;
            for (int i = 0; i < mats.Length; i++) if (mats[i] == basePal) { mats[i] = m; hit = true; }
            if (hit) r.sharedMaterials = mats;
        }
    }
    // Water towers, roof doors and AC units on taller roofs.
    static void RoofClutter(Transform b, float x0, float z0, float roofY, int s) {
        var c = new Vector3(x0 + 2.5f, roofY, z0 + 2.5f);
        switch (s % 5) {
            case 0: Put("Buildings/SM_Prop_Water_Tower_01", c + new Vector3(.6f, 0, -.4f), s * 37, b); break;
            case 1: Put("Buildings/SM_Bld_Roof_Access_01", c + new Vector3(-.8f, 0, -.8f), 90 * (s % 4), b); Put("Props/SM_Prop_Roof_Aircon_02", c + new Vector3(1.2f, 0, 1f), 0, b); break;
            case 2: Put("Props/SM_Prop_Roof_Aircon_03", c + new Vector3(-1, 0, .5f), 0, b); Put("Props/SM_Prop_Roof_Aircon_01", c + new Vector3(1.2f, 0, -1), 90, b); break;
            case 3: Put("Props/SM_Prop_Roof_Aircon_02", c, s * 90, b); Put("Props/SM_Prop_SatDish_01", c + new Vector3(-1.4f, 0, 1.2f), s * 50, b); break;
        }
    }
    // Giant food signs and billboards on one-storey shops (the pizza-slice / burger look of a real food street).
    static bool RoofSign(Transform b, float x0, float z0, int facing, float roofY, int s) {
        float yaw = 90 * facing;
        switch (H(s, 13) % 9) {
            case 0: Put("Props/SM_Prop_LargeSign_Burger_01", OnFace(x0, z0, facing, 0, -2.2f, roofY + 1.1f), yaw, b); return true;
            case 1: Put("Props/SM_Prop_LargeSign_Coffee_01", OnFace(x0, z0, facing, 0, -2f, roofY), yaw + 30, b); return true;
            case 2: Put("Props/SM_Prop_LargeSign_Taco_01", OnFace(x0, z0, facing, 0, -2.2f, roofY), yaw + 90, b); return true;
            case 3: Put("Props/SM_Prop_LargeSign_Noodles_01", OnFace(x0, z0, facing, 0, -2.2f, roofY + .1f), yaw, b); return true;
            case 4: Put("Props/SM_Prop_LargeSign_Hotdog_01", OnFace(x0, z0, facing, 0, -2.2f, roofY), yaw + 90, b); return true;
            case 5: PutTilted("Props/SM_Prop_LargeSign_Pizza_01", OnFace(x0, z0, facing, 0, -1.2f, roofY + 1.6f), yaw, 90, b); return true;
            case 6: PutTilted("Props/SM_Prop_LargeSign_Donut_01", OnFace(x0, z0, facing, 0, -1.2f, roofY + 2f), yaw, 90, b); return true;
            case 7: Put("Props/SM_Prop_Sign_Pizza_01", OnFace(x0, z0, facing, 0, -.5f, roofY), yaw, b); return true;
            default: Put("Props/SM_Prop_Billboard_Roof_01", OnFace(x0, z0, facing, 0, -2f, roofY), yaw, b); return true;
        }
    }
    // Awnings, shop signs and the odd ATM on a street-level shop.
    static void Storefront(Transform b, float x0, float z0, int facing, int s) {
        float yaw = 90 * facing; int k = H(s, 7) % 10, g = H(s, 11) % 12;
        if (k < 4) Put("Buildings/SM_Bld_Shop_Cover_03", OnFace(x0, z0, facing, 0, 2.2f, 2.55f), yaw, b);
        else if (k < 6) Put("Buildings/SM_Bld_Shop_Cover_04", OnFace(x0, z0, facing, 0, .45f, 3f), yaw, b);
        string[] flat = { "Sign_Cafe_01", "Sign_Pub_01", "Sign_Bar_01", "Sign_Chinese_Noodles_01" };
        if (g < 4 && k >= 4) Put("Props/SM_Prop_" + flat[g], OnFace(x0, z0, facing, 0, .7f, 2.75f), yaw, b);
        else if (g == 5) Put("Props/SM_Prop_ATM_01", OnFace(x0, z0, facing, 1.8f, .55f, 1.05f), yaw, b);
        else if (g == 6) Put("Props/SM_Prop_Sign_DeliPizza_01", OnFace(x0, z0, facing, -2.3f, .9f, 2.6f), yaw + 90, b);
        else if (g == 7) Put("Props/SM_Prop_Sign_Barber_01", OnFace(x0, z0, facing, -2.2f, .55f, 1.3f), yaw, b);
        if (H(s, 17) % 4 == 0) Put("Props/SM_Prop_Planter_02", OnFace(x0, z0, facing, 1.4f, .9f, 0), yaw, b);
    }
    // Fire escapes climbing the front of an apartment stack.
    static void FireEscapes(Transform b, float x0, float z0, int facing, int stacks) {
        int floors = stacks * 3;
        for (int i = 0; i < floors; i++)
            Put(i == floors - 1 ? "Buildings/SM_Bld_FireEscape_01" : "Buildings/SM_Bld_FireEscape_02", OnFace(x0, z0, facing, 1.7f, .2f, 3.3f + 3 * i), 90 * facing, b);
    }

    static readonly int[] ShopGround = { 1, 2, 4, 5, 6 };
    // One 5 m-wide building: shop or apartment ground floor, N apartment stacks (3 floors each), roof.
    static void Tower(float x0, float z0, int facing, int stacks, bool shop, string groundOverride = null, int palette = -1) {
        int s = seed++; int family = s % 3 + 1;
        var parent = new GameObject("Building").transform; parent.SetParent(root, false);
        string ground = groundOverride ?? (shop ? "Buildings/SM_Bld_Shop_0" + ShopGround[s % 5]
            : H(s, 3) % 3 == 0 ? "Buildings/SM_Bld_Apartment_Door_0" + (s % 2 + 1) : "Buildings/SM_Bld_Apartment_0" + family);
        Module(ground, x0, z0, facing, 0, parent);
        for (int i = 0; i < stacks; i++) Module("Buildings/SM_Bld_Apartment_Stack_0" + family, x0, z0, facing, 3 + 9 * i, parent);
        float roofY = 3 + 9 * stacks;
        Module("Buildings/SM_Bld_Apartment_Roof_0" + family, x0, z0, facing, roofY, parent);
        Recolor(parent, palette >= 0 ? palette : H(s, 1) % 12);
        if (shop && groundOverride == null) Storefront(parent, x0, z0, facing, s);
        if (stacks == 0 && shop) { if (H(s, 19) % 3 == 0) RoofSign(parent, x0, z0, facing, roofY + .5f, s); }
        else RoofClutter(parent, x0, z0, roofY + .5f, s);
        if (stacks > 0 && groundOverride == null && H(s, 23) % 3 == 0) FireEscapes(parent, x0, z0, facing, stacks);
        if (!shop && H(s, 29) % 2 == 0) foreach (float lat in new[] { -1.7f, 1.7f }) {
            var b = PutGeneric("Environment/SM_Gen_Env_Bush_0" + (H(s, 31) % 4 + 1), OnFace(x0, z0, facing, lat, .7f, 0), s * 33, parent);
            if (b) b.transform.localScale = Vector3.one * .75f;
        }
    }
    // Ring a block with buildings on the street sides. Heights change every 10 m so the skyline steps up and down;
    // lowPct of those runs are one-storey shops.
    static void Block(float xa, float xb, float za, float zb, bool n, bool so, bool e, bool w, bool shops, int minStacks, int maxStacks, int lowPct) {
        int id = (int)xa * 7 + (int)za * 13;
        for (float x = xa; x < xb - .01f; x += 5) for (float z = za; z < zb - .01f; z += 5) {
            if (IsReserved(new Rect(x, z, 5, 5))) continue;
            bool onN = n && z >= zb - 5.01f, onS = so && z <= za + .01f, onE = e && x >= xb - 5.01f, onW = w && x <= xa + .01f;
            if (!(onN || onS || onE || onW)) continue;
            bool ns = onN || onS; int side = onN ? 0 : onS ? 1 : onE ? 2 : 3, run = ns ? (int)((x - xa) / 10) : (int)((z - za) / 10);
            int roll = H(id, side, run) % 100;
            int stacks = roll < lowPct ? 0 : minStacks + roll % (maxStacks - minStacks + 1);
            if (ns && (onE || onW)) { Corner(x, z, onN && onE ? 0 : onS && onE ? 1 : onS && onW ? 2 : 3, stacks, shops); continue; }
            Tower(x, z, onN ? 0 : onS ? 2 : onE ? 1 : 3, stacks, shops);
        }
    }
    // Corner building with facades on two street sides. facing 0 = +Z/+X, 1 = +X/-Z, 2 = -Z/-X, 3 = -X/+Z.
    static void Corner(float x0, float z0, int facing, int stacks, bool shop) {
        int s = seed++; int family = s % 3 + 1;
        var parent = new GameObject("Corner building").transform; parent.SetParent(root, false);
        Module(shop ? "Buildings/SM_Bld_Shop_Corner_0" + (s % 2 + 1) : "Buildings/SM_Bld_Apartment_Corner_0" + family, x0, z0, facing, 0, parent);
        for (int i = 0; i < stacks * 3; i++) Module("Buildings/SM_Bld_Apartment_Corner_0" + family, x0, z0, facing, 3 + 3 * i, parent);
        Module("Buildings/SM_Bld_Apartment_Roof_Corner_0" + family, x0, z0, facing, 3 + 9 * stacks, parent);
        Recolor(parent, H(s, 1) % 12);
        RoofClutter(parent, x0, z0, 3 + 9 * stacks + .5f, s);
    }
    // A fenced neighbourhood park: lawns, a crossing path with a statue, trees, benches and picnic tables.
    static void Park(float xa, float xb, float za, float zb, float pathX, float pathZ) {
        var p = new GameObject("Park").transform; p.SetParent(root, false);
        for (float x = xa; x < xb - .01f; x += 5) for (float z = za; z < zb - .01f; z += 5) {
            if (Mathf.Abs(x - pathX) < .1f || Mathf.Abs(z - pathZ) < .1f) continue;
            Put("Environments/SM_Env_Grass_01", new Vector3(x, WalkY + .03f, z + 5), 0, p);
            int r = H((int)x, (int)z, 41) % 10; var c = new Vector3(x + 2.5f + (r - 5) * .2f, 0, z + 2.5f + (r % 3 - 1) * .8f);
            if (r < 5) Put("Environments/SM_Env_Tree_0" + (r % 3 + 1), c, r * 40, p);
            else if (r < 7) PutGeneric("Environment/SM_Gen_Env_Bush_Large_0" + (r % 4 + 1), c, r * 55, p);
            else if (r == 7) Put("Props/SM_Prop_PicnicTable_01", c, r * 20, p);
        }
        // Fence with gates where the paths meet the street.
        for (float x = xa; x < xb - .01f; x += 5) {
            if (Mathf.Abs(x - pathX) < .1f) continue;
            Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, zb - .4f), 0, p); Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, za + .4f), 0, p);
        }
        for (float z = za; z < zb - .01f; z += 5) {
            if (Mathf.Abs(z - pathZ) < .1f) continue;
            Put("Environments/SM_Env_Fence_01", new Vector3(xa + .4f, 0, z + 5), 90, p); Put("Environments/SM_Env_Fence_01", new Vector3(xb - .4f, 0, z + 5), 90, p);
        }
        foreach (var post in new[] { new Vector3(xa + .4f, 0, za + .4f), new Vector3(xb - .4f, 0, za + .4f), new Vector3(xa + .4f, 0, zb - .4f), new Vector3(xb - .4f, 0, zb - .4f),
                                     new Vector3(pathX, 0, zb - .4f), new Vector3(pathX + 5, 0, zb - .4f), new Vector3(pathX, 0, za + .4f), new Vector3(pathX + 5, 0, za + .4f),
                                     new Vector3(xa + .4f, 0, pathZ), new Vector3(xa + .4f, 0, pathZ + 5), new Vector3(xb - .4f, 0, pathZ), new Vector3(xb - .4f, 0, pathZ + 5) })
            Put("Environments/SM_Env_Fence_End_01", post, 0, p);
        var mid = new Vector3(pathX + 2.5f, 0, pathZ + 2.5f);
        PutGeneric("Props/SM_Gen_Prop_Statue_01", mid, 180, p);
        for (float z = za + 2.5f; z < zb; z += 10) if (Mathf.Abs(z - mid.z) > 4) { Put("Props/SM_Prop_ParkBench_01", new Vector3(pathX - .2f, 0, z), 90, p); Put("Props/SM_Prop_ParkBench_01", new Vector3(pathX + 5.2f, 0, z + 5), 270, p); }
        for (float x = xa + 2.5f; x < xb; x += 10) if (Mathf.Abs(x - mid.x) > 4) { Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, pathZ - .2f), 0, p); Put("Props/SM_Prop_Trashbin_01", new Vector3(x + 2, 0, pathZ + 5.3f), 0, p); }
        foreach (var d in new[] { new Vector3(-3.2f, 0, -3.2f), new Vector3(3.2f, 0, 3.2f) }) Put("Props/SM_Prop_LightPole_Base_02", mid + d, 45, p);
    }
    // Everyday street clutter on the sidewalk tiles: newspaper boxes, meters, payphones, bins, bags, roadworks.
    static void Clutter(List<(float x, float z, int f)> tiles) {
        var p = new GameObject("Street clutter").transform; p.SetParent(root, false);
        foreach (var (x, z, f) in tiles) {
            if (x > -25 && x < 30 && z > -14 && z < 12) continue;    // hand-dressed gameplay stretch of Main Street
            if (IsReserved(new Rect(x, z, 5, 5))) continue;
            int r = H((int)x, (int)z, 5) % 100; float yaw = 90 * f;
            if (r < 6) { Put("Props/SM_Prop_Newspaper_02", OnFace(x, z, f, -1, -.8f, 0), yaw + 180, p); Put("Props/SM_Prop_Mailbox_01", OnFace(x, z, f, .2f, -.8f, 0), yaw + 180, p); }
            else if (r < 11) { Put("Props/SM_Prop_ParkingMeter_01", OnFace(x, z, f, -1.2f, -.5f, 0), yaw, p); Put("Props/SM_Prop_ParkingMeter_01", OnFace(x, z, f, 1.3f, -.5f, 0), yaw, p); }
            else if (r < 14) Put("Props/SM_Prop_Phones_01", OnFace(x, z, f, 0, -4.3f, 0), yaw, p);
            else if (r < 19) { Put("Props/SM_Prop_TrashBag_01", OnFace(x, z, f, -1.4f, -4.4f, 0), r * 30, p); Put("Props/SM_Prop_TrashBag_03", OnFace(x, z, f, -.8f, -4.2f, 0), r * 70, p); Put("Props/SM_Prop_TrashCan_01", OnFace(x, z, f, .3f, -4.4f, 0), 0, p); }
            else if (r < 23) Put("Props/SM_Prop_Trashbin_02", OnFace(x, z, f, 0, -.7f, 0), yaw, p);
            else if (r < 25) {
                for (int i = -1; i <= 1; i++) Put("Props/SM_Prop_Cone_01", OnFace(x, z, f, i * 1.3f, .7f, RoadY + .12f), 0, p);
                Put("Props/SM_Prop_Barrier_01", OnFace(x, z, f, 0, 2.1f, RoadY + .12f), yaw, p);
                Put("Props/SM_Prop_Manhole_02", OnFace(x, z, f, 0, 1.4f, RoadY + .15f), 0, p);
            }
            else if (r < 28) Put("Props/SM_Prop_PowerBox_01", OnFace(x, z, f, 0, -4.5f, 0), yaw, p);
            else if (r < 31) Put("Props/SM_Prop_Skip_02", OnFace(x, z, f, 0, -4.2f, 0), yaw, p);
            else if (r < 34) Put("Props/SM_Prop_Planter_01", OnFace(x, z, f, 0, -4.5f, 0), yaw, p);
            else if (r < 37) Put("Props/SM_Prop_Hydrant_01", OnFace(x, z, f, 0, -.6f, 0), yaw, p);
        }
    }
    // The Gilded Orbit: the city pack's corner apartments in charcoal on top of the walk-in restaurant shell, crowned by a spire.
    static void GildedTower() {
        var parent = new GameObject("The Gilded Orbit tower").transform; parent.SetParent(root, false);
        foreach (var (x0, z0, f) in new[] { (14.3f, 16.5f, 2), (19.3f, 16.5f, 1), (14.3f, 21.5f, 3), (19.3f, 21.5f, 0) }) {
            for (int i = 0; i < 3; i++) Module("Buildings/SM_Bld_Apartment_Corner_02", x0, z0, f, 4.6f + 3 * i, parent);
            Module("Buildings/SM_Bld_Apartment_Roof_Corner_02", x0, z0, f, 13.6f, parent);
        }
        // Clad the walk-in ground floor's alley and back walls (thin slices of apartment modules, 4.6 m tall).
        var clad = new Vector3(.86f, 1.53f, .08f);
        foreach (float pz in new[] { 20.8f, 25.1f, 29.4f }) ModuleS("Buildings/SM_Bld_Apartment_01", 13.6f, pz - 5, 3, 0, clad, parent);
        foreach (float x0 in new[] { 14.15f, 19.3f }) ModuleS("Buildings/SM_Bld_Apartment_01", x0, 29.8f - 5, 0, 0, new Vector3(1.03f, 1.53f, .08f), parent);
        StripColliders(parent);
        Recolor(parent, 4);
        Put("Buildings/SM_Bld_Spire_01", new Vector3(19.3f, 14.2f, 21.5f), 0, parent);
        Put("Props/SM_Prop_Roof_Aircon_03", new Vector3(16, 14.2f, 25), 0, parent);
    }
    // ---------- Wrapping our hand-built gameplay buildings in city-pack architecture ----------
    static GameObject ModuleS(string rel, float x0, float z0, int facing, float y, Vector3 scale, Transform parent) {
        var go = Module(rel, x0, z0, facing, y, parent); if (go) go.transform.localScale = scale; return go;
    }
    static void StripColliders(Transform t) { foreach (var c in t.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); }
    // Places a PolygonGeneric prop scaled so its footprint width is `width`, standing on y = pos.y.
    public static GameObject FitGeneric(string rel, Vector3 pos, float yaw, float width, Transform parent) {
        var go = PutGeneric(rel, pos, yaw, parent); if (!go) return null;
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return go;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        float s = width / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z)); go.transform.localScale *= s;
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (pos.y - b.min.y); StripColliders(go.transform); return go;
    }
    // Your restaurant becomes the ground floor of a city building: three apartment floors above it and two narrow
    // neighbours closing the gaps at either side, so no blank walls show from Main Street.
    static void RestaurantBuilding() {
        var p = new GameObject("Your restaurant's building").transform; p.SetParent(root, false);
        var up = new Vector3(.88f, 1, 1);
        foreach (float px in new[] { -3.4f, -7.8f, -12.2f }) {
            ModuleS("Buildings/SM_Bld_Apartment_Stack_02", px - 5, -14, 0, 4.42f, up, p);
            ModuleS("Buildings/SM_Bld_Apartment_Roof_02", px - 5, -14, 0, 13.42f, up, p);
        }
        foreach (float x0 in new[] { -16.6f, -12.2f, -7.8f }) {
            ModuleS("Buildings/SM_Bld_Apartment_Stack_02", x0, -19, 2, 4.42f, up, p);
            ModuleS("Buildings/SM_Bld_Apartment_Roof_02", x0, -19, 2, 13.42f, up, p);
        }
        Put("Buildings/SM_Prop_Water_Tower_01", new Vector3(-6.5f, 13.9f, -15), 20, p);
        Put("Props/SM_Prop_Roof_Aircon_02", new Vector3(-13, 13.9f, -12), 0, p);
        Recolor(p, 6);
        StripColliders(p);
        Narrow(-21.6f, -15, 0, .68f, 1, true, 2);
        Narrow(-5f, -15, 0, .68f, 1, true, 9);
    }
    // A slim building squeezed into a gap: modules scaled along the facade (sx).
    static void Narrow(float x0, float z0, int facing, float sx, int stacks, bool shop, int palette) {
        int s = seed++; int family = s % 3 + 1; var sc = new Vector3(sx, 1, 1);
        var parent = new GameObject("Narrow building").transform; parent.SetParent(root, false);
        ModuleS(shop ? "Buildings/SM_Bld_Shop_0" + ShopGround[s % 5] : "Buildings/SM_Bld_Apartment_0" + family, x0, z0, facing, 0, sc, parent);
        for (int i = 0; i < stacks; i++) ModuleS("Buildings/SM_Bld_Apartment_Stack_0" + family, x0, z0, facing, 3 + 9 * i, sc, parent);
        ModuleS("Buildings/SM_Bld_Apartment_Roof_0" + family, x0, z0, facing, 3 + 9 * stacks, sc, parent);
        Recolor(parent, palette);
    }
    // Rival Alley: a slim corner building forms its west wall; dumpsters, bags, crates and a chest-stash dress the dead end.
    static void RivalAlley() {
        var p = new GameObject("Rival alley dressing").transform; p.SetParent(root, false);
        var sc = new Vector3(1, 1, .78f);
        ModuleS("Buildings/SM_Bld_Apartment_Corner_03", 3.9f, 15, 1, 0, sc, p);
        for (int i = 0; i < 6; i++) ModuleS("Buildings/SM_Bld_Apartment_Corner_03", 3.9f, 15, 1, 3 + 3 * i, sc, p);
        ModuleS("Buildings/SM_Bld_Apartment_Roof_Corner_03", 3.9f, 15, 1, 21, sc, p);
        var fe = Put("Buildings/SM_Bld_FireEscape_02", new Vector3(8.95f, 3.3f, 17.8f), 90, p);
        Recolor(p, 11);
        // Dead-end clutter kept out of the fight corridor (x 9.35-13.85, z 14-24).
        Put("Props/SM_Prop_Skip_01", new Vector3(12.4f, 0, 28), 90, p);
        Put("Props/SM_Prop_Skip_02", new Vector3(9.9f, 0, 29.2f), 0, p);
        foreach (var (x, z, k) in new[] { (12.9f, 25.2f, 1), (12.5f, 25.6f, 2), (13.1f, 26.1f, 3), (9.6f, 25.5f, 1), (9.3f, 26.2f, 3) })
            Put("Props/SM_Prop_TrashBag_0" + k, new Vector3(x, 0, z), x * 40, p);
        Put("Props/SM_Prop_CardboardBox_03", new Vector3(9.5f, 0, 27.6f), 15, p);
        Put("Props/SM_Prop_CardboardBox_01", new Vector3(9.6f, .78f, 27.5f), 40, p);
        Put("Props/SM_Prop_Pallet_01", new Vector3(8.2f, 0, 23.5f), 80, p);
        Put("Props/SM_Prop_Pipe_Small_01", new Vector3(8.95f, 0, 21.4f), 90, p);
    }
    // Milo's corner store: glass shopfronts with stocked shelves visible inside.
    static void MilosStore() {
        // Left module (x -18..-13): the walk-in store. Right module: a display window with stocked shelves.
        Tower(-18, 13.5f, 2, 1, true, "Buildings/SM_Bld_Shop_01", 0);
        var shopBuilding = root.GetChild(root.childCount - 1);
        Tower(-13, 13.5f, 2, 1, true, "Buildings/SM_Bld_Shop_04", 0);
        foreach (float lat in new[] { -1.2f, 1.2f }) Put("Props/SM_Prop_ShopInterior_Shelf_01", OnFace(-13, 13.5f, 2, lat, -3.9f, .05f), 180, root);
        Put("Props/SM_Prop_ShopInterior_Display_02", OnFace(-13, 13.5f, 2, 0, -2f, .05f), 0, root);
        MilosWalkIn(shopBuilding);
    }

    // ---------- Old Market places (see claude/world-map.md) ----------
    static GameObject FitPack(string pack, string rel, Vector3 pos, float yaw, float width, Transform parent) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/" + pack + "/Prefabs/" + rel + ".prefab"); if (!src) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent ? parent : root);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return go;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.localScale *= width / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z));
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (pos.y - b.min.y); return go;
    }
    // Text on a sign that reads from the side its face points to (0:+Z 1:+X 2:-Z 3:-X).
    static void SignText(string text, Vector3 pos, int facing, float size, Color color, Transform parent) {
        var go = PrototypeBuilder.Label(text, pos, size, color, parent);
        go.transform.rotation = Quaternion.Euler(0, new[] { 180, 270, 0, 90 }[facing], 0);
    }
    // Market Row: a square of produce, flower and snack stalls opening onto North Avenue.
    static void MarketStalls() {
        var p = new GameObject("Market stalls").transform; p.SetParent(root, false);
        string[] displays = { "Props/SM_Prop_Market_Food_Display_01", "Props/SM_Prop_Flower_Stand_Preset_01", "Props/SM_Prop_Market_Food_Display_03",
                              "Props/SM_Prop_Market_Food_Display_05", "Props/SM_Prop_Flower_Stand_Preset_02", "Props/SM_Prop_Market_Food_Display_02" };
        for (int i = 0; i < displays.Length; i++) {
            float x = -15 + i * 6;
            FitPack("PolygonShops", displays[i], new Vector3(x, WalkY, 64.5f), 180, 2.6f, p);
            var shade = FitPack("PolygonShops", "Props/SM_Prop_Cafe_Parasol_0" + (i % 3 + 1), new Vector3(x, WalkY, 65.6f), 0, 3.2f, p);
            if (shade) StripColliders(shade.transform);
            FitPack("PolygonGeneric", "Props/SM_Gen_Prop_Crate_0" + (i % 3 + 1), new Vector3(x + 1.7f, WalkY, 66.2f), i * 30, .8f, p);
        }
        Put("Props/SM_Prop_HotdogStand_01", new Vector3(17.5f, WalkY, 67.5f), 180, p);
        foreach (float x in new[] { -18.5f, 18.5f }) Put("Props/SM_Prop_LightPole_Base_02", new Vector3(x, 0, 61), 0, p);
        foreach (float x in new[] { -12f, 0, 12 }) Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, 69.2f), 180, p);
        PoleSign("MARKET ROW", new Vector3(-21.5f, 0, 57.6f), "B9703C");
    }
    // Market Row: The Tin Diner, a chrome one-storey diner and the two-star rival you can raid.
    static void TinDiner() {
        Tower(-25, 35, 0, 0, true, "Buildings/SM_Bld_Shop_02", 7);
        Tower(-20, 35, 0, 0, true, "Buildings/SM_Bld_Shop_05", 7);
        var p = new GameObject("The Tin Diner").transform; p.SetParent(root, false);
        Put("Buildings/SM_Bld_Shop_Cover_03", OnFace(-25, 35, 0, 2.5f, 2.2f, 2.55f), 0, p);
        Put("Props/SM_Prop_LargeSign_Milkshake_01", OnFace(-25, 35, 0, 2.5f, -2.2f, 3.6f), 0, p);
        PoleSign("THE TIN DINER", new Vector3(-29, 0, 42.6f), "8FA9BD");
    }
    // The Flats: a dead-end alley off South Avenue behind your restaurant, tagged by Zeeb.
    static void GraffitiAlley() {
        var p = new GameObject("Graffiti alley").transform; p.SetParent(root, false);
        var paint = new[] { InteriorMat("TagPink", "E0479A"), InteriorMat("TagTeal", "2FB7A8"), InteriorMat("TagPurple", "8D6AE0"), InteriorMat("TagYellow", "F2C230") };
        for (int i = 0; i < 6; i++) {
            float z = -38 + i * 2.2f, h = .9f + (H(i, 3) % 5) * .15f;
            Slab("Paint", new Vector3(-24.93f, .6f + h / 2 + (i % 2) * .5f, z), new Vector3(.02f, h, 1.4f + (H(i, 5) % 3) * .4f), paint[i % 4], p, false);
        }
        SignText("ZEEB WUZ HERE", new Vector3(-24.9f, 2.3f, -33), 1, .22f, new Color(.8f, .65f, 1f), p);
        Put("Props/SM_Prop_Skip_01", new Vector3(-21.4f, 0, -29.5f), 90, p);
        foreach (var (x, z, k) in new[] { (-23.9f, -27.2f, 1), (-23.4f, -26.6f, 2), (-24.1f, -36.5f, 3), (-21.2f, -37.5f, 1) })
            Put("Props/SM_Prop_TrashBag_0" + k, new Vector3(x, 0, z), x * 40, p);
        Put("Props/SM_Prop_Pallet_01", new Vector3(-21.3f, 0, -34), 80, p);
        Put("Props/SM_Prop_CardboardBox_03", new Vector3(-23.9f, 0, -31), 20, p);
        Put("Props/SM_Prop_LightPole_Base_02", new Vector3(-24.3f, 0, -38.8f), 90, p);
        for (float x = -25; x < -20.01f; x += 5) Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, -25.4f), 0, p);
    }
    // The Flats: a fenced vacant lot where Greasy Gus's food truck (the one-star rival) parks.
    static void VacantLot() {
        var p = new GameObject("Vacant lot").transform; p.SetParent(root, false);
        for (float x = 10; x < 24.99f; x += 5) Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, -25.4f), 0, p);
        for (float z = -35; z < -25.01f; z += 5) { Put("Environments/SM_Env_Fence_01", new Vector3(10.4f, 0, z + 5), 90, p); Put("Environments/SM_Env_Fence_01", new Vector3(24.6f, 0, z + 5), 90, p); }
        var truck = new GameObject("Greasy Gus's truck").transform; truck.SetParent(p, false);
        Put("Vehicles/SM_Veh_Car_Van_01", new Vector3(17.5f, WalkY, -31.5f), 180, truck);
        Put("Props/SM_Prop_LargeSign_Burger_01", new Vector3(17.5f, 2.35f, -31.2f), 180, truck);
        foreach (int side in new[] { -1, 1 }) SignText("GREASY GUS", new Vector3(17.5f + side * 1.08f, 1.45f, -31.5f), side > 0 ? 1 : 3, .16f, new Color(1, .86f, .3f), truck);
        Put("Props/SM_Prop_PicnicTable_01", new Vector3(13.5f, WalkY, -35.5f), 0, p);
        Put("Props/SM_Prop_Trashbin_02", new Vector3(21.8f, WalkY, -36.5f), 0, p);
        Put("Props/SM_Prop_Skip_02", new Vector3(21.5f, 0, -27.3f), 0, p);
        foreach (var (x, z) in new[] { (12.2f, -27.2f), (13.1f, -28.1f) }) FitPack("PolygonGeneric", "Props/SM_Gen_Prop_Crate_02", new Vector3(x, WalkY, z), x * 30, .8f, p);
        Put("Props/SM_Prop_LightPole_Base_02", new Vector3(10.8f, 0, -38.8f), 90, p);
    }
    // Barricades where streets leave the block, and the gate to The Docks at the bottom of West Street.
    static void StreetEnds() {
        var p = new GameObject("Street ends").transform; p.SetParent(root, false);
        void Line(Vector3 c, bool acrossX, float length) {
            var dir = acrossX ? Vector3.right : Vector3.forward; float yaw = acrossX ? 0 : 90;
            for (float t = -length / 2 + .8f; t <= length / 2 - .7f; t += 1.6f) Put("Props/SM_Prop_Barrier_01", c + dir * t + Vector3.up * RoadY, yaw, p);
            foreach (float e in new[] { -length / 2 - .6f, length / 2 + .6f }) Put("Props/SM_Prop_Cone_01", c + dir * e + Vector3.up * RoadY, 0, p);
        }
        foreach (float z in new[] { 0f, 50, -50 }) foreach (int side in new[] { -1, 1 }) Line(new Vector3(side * 77.5f, 0, z), false, 10);
        Line(new Vector3(-40, 0, 77.5f), true, 10); Line(new Vector3(40, 0, 77.5f), true, 10); Line(new Vector3(40, 0, -77.5f), true, 10);
        // The Docks gate: locked until the district is built and you reach Line Cook.
        var gate = new GameObject("Gate to The Docks").transform; gate.SetParent(p, false);
        Line(new Vector3(-40, 0, -77.5f), true, 10);
        var post = InteriorMat("GatePost", "2A2E33"); var board = GlowMat("DOCKSGATE", "3AA0B0", .25f);
        foreach (float x in new[] { -45.4f, -34.6f }) Slab("Gate post", new Vector3(x, 2.6f, -77.5f), new Vector3(.35f, 5.2f, .35f), post, gate, true);
        Slab("Gate board", new Vector3(-40, 4.6f, -77.5f), new Vector3(10.4f, 1.6f, .25f), board, gate, false);
        SignText("THE DOCKS", new Vector3(-40, 4.95f, -77.35f), 0, .5f, Color.white, gate);
        SignText("Gate opens at LINE COOK", new Vector3(-40, 4.2f, -77.35f), 0, .3f, new Color(1, .92f, .75f), gate);
    }
    // A street tree in its own planted square: grass and a couple of bushes (the "lived-in" street look).
    public static void StreetTree(Vector3 p, int k, Transform parent = null) {
        Put("Environments/SM_Env_Tree_0" + (k % 3 + 1), p, k * 47, parent);
        var g = Put("Environments/SM_Env_Grass_01", new Vector3(p.x - 1.25f, .005f, p.z + 1.25f), 0, parent); if (g) g.transform.localScale = new Vector3(.5f, 1, .5f);
        for (int i = 0; i < 2; i++) {
            var b = PutGeneric("Environment/SM_Gen_Env_Bush_0" + ((k + i) % 4 + 1), p + new Vector3(i == 0 ? -.8f : .7f, 0, i == 0 ? .6f : -.7f), k * 90 + i * 130, parent);
            if (b) b.transform.localScale = Vector3.one * .6f;
        }
    }
    static Material GlowMat(string name, string hex, float glow) {
        string path = "Assets/Generated/Sign_" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out var c); m.color = c; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); return m;
    }
    // A tall roadside pole sign, readable from both directions along the street.
    public static void PoleSign(string text, Vector3 p, string hex) {
        var s = new GameObject("Pole sign / " + text).transform; s.SetParent(root, false);
        Slab("Pole", p + Vector3.up * 3.2f, new Vector3(.28f, 6.4f, .28f), InteriorMat("PolePaint", "2F3A36"), s, true);
        Slab("Sign box", p + Vector3.up * 7.2f, new Vector3(.5f, 1.6f, 5.4f), GlowMat(text.Replace(" ", ""), hex, .35f), s, false);
        Slab("Sign trim", p + Vector3.up * 8.06f, new Vector3(.6f, .14f, 5.6f), InteriorMat("SignTrim", "F2E6C8"), s, false);
        foreach (int side in new[] { -1, 1 }) {
            var l = PrototypeBuilder.Label(text, p + new Vector3(side * .27f, 7.2f, 0), .34f, new Color(1, .96f, .86f), s);
            l.transform.rotation = Quaternion.Euler(0, side > 0 ? 270 : 90, 0);
        }
    }
    // ---------- Walk-in interiors ----------
    // City pack modules are outer shells: no inside faces and a solid door. A walk-in store gets a doorway cut into the
    // module mesh (and its collider) plus its own inner room: floor, walls, ceiling, light.
    static Mesh CutDoor(Mesh src, float x0, float x1, float y1, float z0, float z1, string path) {
        var m = Object.Instantiate(src); m.name = src.name + "_Door"; var v = m.vertices;
        for (int sm = 0; sm < m.subMeshCount; sm++) {
            var t = m.GetTriangles(sm); var keep = new List<int>();
            for (int i = 0; i < t.Length; i += 3) {
                var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3;
                if (c.x > x0 && c.x < x1 && c.y < y1 && c.z > z0 && c.z < z1) continue;
                // Also drop the fake "shop interior" box behind the glass, so the real room shows through.
                if (c.x > -4.85f && c.x < -.15f && c.y > .05f && c.y < 2.95f && c.z > -4.85f && c.z < -.25f) continue;
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }
            m.SetTriangles(keep, sm);
        }
        m.RecalculateBounds();
        System.IO.Directory.CreateDirectory("Assets/Generated/Meshes");
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path); return m;
    }
    static Material InteriorMat(string name, string hex) {
        string path = "Assets/Generated/Interior_" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out var c); m.color = c; m.SetFloat("_Smoothness", .25f); return m;
    }
    static GameObject Slab(string n, Vector3 center, Vector3 size, Material m, Transform parent, bool collide) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = n; go.transform.SetParent(parent, false);
        go.transform.position = center; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    static void MilosWalkIn(Transform building) {
        // Door: local x -3.35..-1.65 (centred), up to 2.45 m, through the front 1.3 m of the module.
        var cuts = new Dictionary<Mesh, Mesh>();
        foreach (var mf in building.GetComponentsInChildren<MeshFilter>()) {
            var src = mf.sharedMesh; if (!src || !src.name.StartsWith("SM_Bld_Shop_01")) continue;
            if (!cuts.TryGetValue(src, out var cut)) cuts[src] = cut = CutDoor(src, -3.35f, -1.65f, 2.45f, -1.3f, 1f, "Assets/Generated/Meshes/" + src.name + "_Door.asset");
            mf.sharedMesh = cut;
        }
        // The module's mesh colliders seal the doorway; replace them on the ground floor with simple walls around the door.
        foreach (var mc in building.GetComponentsInChildren<MeshCollider>()) if (mc.sharedMesh && mc.sharedMesh.name.StartsWith("SM_Bld_Shop_01")) Object.DestroyImmediate(mc);
        var room = new GameObject("Milo's walk-in").transform; room.SetParent(root, false);
        var clear = InteriorMat("MiloWall", "E8DCC0");
        foreach (var (cx, w) in new[] { (-17.15f, 1.6f), (-13.85f, 1.6f) }) { var f = Slab("Front wall collider", new Vector3(cx, 1.5f, 13.6f), new Vector3(w, 3, .3f), clear, room, true); f.GetComponent<Renderer>().enabled = false; }
        var top = Slab("Door top collider", new Vector3(-15.5f, 2.75f, 13.6f), new Vector3(1.8f, .5f, .3f), clear, room, true); top.GetComponent<Renderer>().enabled = false;
        var wall = InteriorMat("MiloWall", "E8DCC0"); var floor = InteriorMat("MiloFloor", "8C6B4F"); var trim = InteriorMat("MiloTrim", "2F6B5E");
        Slab("Floor", new Vector3(-15.5f, .03f, 16), new Vector3(4.8f, .06f, 4.6f), floor, room, true);
        Slab("Wall left", new Vector3(-17.86f, 1.5f, 16), new Vector3(.06f, 3, 4.6f), wall, room, true);
        Slab("Wall right", new Vector3(-13.14f, 1.5f, 16), new Vector3(.06f, 3, 4.6f), wall, room, true);
        Slab("Wall back", new Vector3(-15.5f, 1.5f, 18.3f), new Vector3(4.8f, 3, .06f), wall, room, true);
        Slab("Ceiling", new Vector3(-15.5f, 2.97f, 16), new Vector3(4.8f, .06f, 4.6f), wall, room, false);
        Slab("Header left", new Vector3(-17.1f, 2.55f, 13.75f), new Vector3(1.6f, .9f, .06f), wall, room, false);
        Slab("Header right", new Vector3(-13.9f, 2.55f, 13.75f), new Vector3(1.6f, .9f, .06f), wall, room, false);
        Slab("Door header", new Vector3(-15.5f, 2.73f, 13.75f), new Vector3(1.6f, .54f, .06f), wall, room, false);
        // Outside: a clean door frame over the cut edges.
        Slab("Door header outside", new Vector3(-15.5f, 2.5f, 13.38f), new Vector3(2.0f, .55f, .12f), trim, room, false);
        foreach (float x in new[] { -16.42f, -14.58f }) Slab("Door post", new Vector3(x, 1.15f, 13.4f), new Vector3(.14f, 2.3f, .14f), trim, room, true);
        Slab("Chair rail", new Vector3(-15.5f, 1, 18.26f), new Vector3(4.8f, .08f, .03f), trim, room, false);
        foreach (float y in new[] { .4f, 1.2f, 2 }) Slab("Wall shelf", new Vector3(-15.5f, y, 18.1f), new Vector3(1.6f, .05f, .35f), trim, room, false);
        Put("Props/SM_Prop_ShopInterior_Shelf_01", new Vector3(-17.3f, .06f, 17.4f), 90, room);
        Put("Props/SM_Prop_ShopInterior_Shelf_01", new Vector3(-13.7f, .06f, 17.4f), 270, room);
        Put("Props/SM_Prop_ShopInterior_Desk_02", new Vector3(-15.5f, .06f, 17.25f), 180, room);
        var light = new GameObject("Shop light").AddComponent<Light>(); light.transform.SetParent(room, false); light.transform.position = new Vector3(-15.5f, 2.6f, 16);
        light.type = LightType.Point; light.color = new Color(1, .86f, .66f); light.intensity = 1.6f; light.range = 7;
    }

    public static void Build(Transform world) {
        root = new GameObject("City (POLYGON City)").transform; root.SetParent(world, false); seed = 3;
        var ground = new GameObject("Ground and streets").transform; ground.SetParent(root, false);
        var curbTiles = new List<(float x, float z, int f)>();
        // Roads and sidewalks on the 5 m grid.
        for (float x = -Half; x < Half; x += Cell) for (float z = -Half; z < Half; z += Cell) {
            float cx = x + 2.5f, cz = z + 2.5f;
            if (IsRoad(cx, cz)) { Put("Environments/SM_Env_Road_01", new Vector3(x + 5, RoadY, z + 5), 0, ground); continue; }
            // Pave everywhere; under your restaurant the paving sits 2 cm lower so its own floor wins.
            float y = Reserved[1].Overlaps(new Rect(x + .5f, z + .5f, 4, 4)) ? WalkY - .02f : WalkY;
            bool n = IsRoad(cx, cz + 5), s = IsRoad(cx, cz - 5), e = IsRoad(cx + 5, cz), w = IsRoad(cx - 5, cz);
            int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);
            if (count == 1) { int facing = n ? 0 : e ? 1 : s ? 2 : 3; Module("Environments/SM_Env_Sidewalk_Straight_01", x, z, facing, y, ground); curbTiles.Add((x, z, facing)); }
            else if (count >= 2) Module("Environments/SM_Env_Sidewalk_Corner_01", x, z, n && e ? 0 : e && s ? 1 : s && w ? 2 : 3, y, ground);
            else Module("Environments/SM_Env_Sidewalk_01", x, z, 0, y, ground);
        }
        // Real facades for Milo's and the apartments next to the stand (inside the gameplay strip).
        MilosStore();
        foreach (var x0 in new[] { -5f, 0f }) Tower(x0, 15f, 2, 2, false, "Buildings/SM_Bld_Apartment_Door_0" + (x0 < -1 ? 1 : 2), 0);
        GildedTower();
        RestaurantBuilding();
        PoleSign("THE ODD TABLE", new Vector3(-1.6f, 0, -7.6f), "C8553D");
        RivalAlley();
        // City blocks: storefront rows on every street side, corner buildings where two streets meet.
        //      x range        z range      N      S      E      W     shops  stacks  low-rise %
        Block(-30, 30,   10, 40,  true,  true,  true,  true,  true,  1, 2, 35);   // north of Main Street (gameplay strip reserved)
        Block(-30, 30,  -40, -10, true,  true,  true,  true,  true,  1, 2, 35);   // south of Main Street (your restaurant reserved)
        Block(-80, -50,  10, 40,  false, true,  true,  false, true,  1, 2, 30);   // city hall block (street sides only)
        Block(-80, -50, -40, -10, true,  false, true,  false, true,  1, 2, 30);
        Block( 50, 80,   10, 40,  false, true,  false, true,  true,  1, 3, 25);
        Park ( 50, 80,  -40, -10, 65, -30);                                         // fenced park off Main Street
        Block(-80, -50,  60, 80,  false, true,  true,  false, false, 1, 3, 0);
        Block(-30, 30,   60, 80,  false, true,  true,  true,  true,  1, 3, 20);
        Block( 50, 80,   60, 80,  true,  true,  true,  true,  false, 1, 3, 0);
        Block(-80, -50, -80, -60, true,  true,  true,  true,  false, 1, 3, 0);
        Block(-30, 30,  -80, -60, true,  true,  true,  true,  true,  1, 3, 20);
        Block( 50, 80,  -80, -60, true,  true,  true,  true,  false, 1, 3, 0);
        // Landmarks in the outer blocks.
        Put("Buildings/SM_Bld_CityHall_01", new Vector3(-67, 0, 25), 180);
        Put("Buildings/SM_Bld_OfficeRound_01", new Vector3(-67, 0, -25), 0);
        Put("Buildings/SM_Bld_OfficeSquare_01", new Vector3(80, 0, 35), 0);
        Put("Buildings/SM_Bld_OfficeOctagon_01", new Vector3(-67, 0, 72), 0);
        Put("Buildings/SM_Bld_OfficeOld_Small_01", new Vector3(20, 0, 80), 0);
        Put("Buildings/SM_Bld_Station_01", new Vector3(0, 0, -30), 0);
        Put("Environments/Custom/SM_Env_Skyline_01", Vector3.zero, 0);
        // Old Market's own places (Market Row, The Flats) and the street ends.
        MarketStalls(); TinDiner(); GraffitiAlley(); VacantLot(); StreetEnds();
        // Street life: parked cars, trees, benches, hydrants, a hotdog cart, bus stop, rooftop signs.
        string[] cars = { "SM_Veh_Car_Sedan_01", "SM_Veh_Car_Taxi_01", "SM_Veh_Car_Van_01", "SM_Veh_Car_Small_01", "SM_Veh_Car_Medium_01", "SM_Veh_Car_Muscle_01" };
        int c = 0;
        foreach (float x in new[] { -75f, -62, -52, 28, 55, 68 }) Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, x > 0 ? 3.3f : -3.3f), x > 0 ? 90 : 270);
        foreach (float z in new[] { -70f, -30, 20, 70 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(-43.3f, RoadY, z), 0); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(43.3f, RoadY, z + 8), 180); }
        foreach (float x in new[] { -70f, -30, 10, 50 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, 46.7f), 90); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x + 12, RoadY, -46.7f), 270); }
        int t = 0;
        for (float x = -75; x <= 75; x += 15) {
            if (Mathf.Abs(x) < 30 || Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue;
            StreetTree(new Vector3(x, 0, 7.5f), t++); StreetTree(new Vector3(x + 5, 0, -7.5f), t++);
        }
        for (float x = -75; x <= 75; x += 20) { if (Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue; StreetTree(new Vector3(x, 0, 57.5f), t++); StreetTree(new Vector3(x, 0, -57.5f), t++); }
        for (float z = -75; z <= 75; z += 20) { if (Mathf.Abs(z) < 12 || Mathf.Abs(Mathf.Abs(z) - 50) < 6) continue; StreetTree(new Vector3(-32.5f, 0, z), t++); StreetTree(new Vector3(32.5f, 0, z), t++); }
        Put("Props/SM_Prop_HotdogStand_01", new Vector3(30, 0, 8), 180);
        Put("Props/SM_Prop_BusStop_01", new Vector3(-30, 0, -7.2f), 0);
        foreach (float x in new[] { -28f, 27, -55, 60 }) { Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, x > 0 ? 8.6f : -8.6f), x > 0 ? 180 : 0); }
        foreach (float x in new[] { -34f, 34, -48, 48 }) Put("Props/SM_Prop_Hydrant_01", new Vector3(x, 0, x > 0 ? 6 : -6), 0);
        foreach (float x in new[] { -26f, 26, -60, 60 }) Put("Props/SM_Prop_Trashbin_01", new Vector3(x, 0, -6.2f), 0);
        foreach (float x in new[] { -60f, -20, 20, 60 }) { Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, 44.2f), 0); Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, -44.2f), 180); }
        Clutter(curbTiles);
        // Flat ground beyond the block (under the skyline) and the invisible edge of the playable city.
        Slab("Outskirts", new Vector3(0, -.36f, 0), new Vector3(900, .2f, 900), InteriorMat("Outskirts", "8E8A82"), root, false);
        foreach (var (pos, size) in new[] { (new Vector3(0, 5, Half + 1), new Vector3(2 * Half, 10, 1)), (new Vector3(0, 5, -Half - 1), new Vector3(2 * Half, 10, 1)), (new Vector3(Half + 1, 5, 0), new Vector3(1, 10, 2 * Half)), (new Vector3(-Half - 1, 5, 0), new Vector3(1, 10, 2 * Half)) }) {
            var wall = new GameObject("City edge"); wall.transform.SetParent(root, false); wall.transform.position = pos; wall.AddComponent<BoxCollider>().size = size;
        }
    }
}
