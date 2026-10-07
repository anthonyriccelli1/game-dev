using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds the explorable city from POLYGON City modules on a 5 m grid:
// a 160 m x 160 m district with three east-west avenues, two north-south streets, full sidewalks,
// modular shop/apartment rows, landmark towers, street furniture and a 360-degree skyline.
// Gameplay locations (stand, Milo's, your restaurant, The Alchemist) keep their spots on Main Street.
// This block IS Old Market (one restaurant per district): Market Row to the north, the home street in the middle,
// The Flats to the south. Street ends are barricaded; the Docks gate waits at the bottom of West Street.
public static partial class CityMap {
    const string City = "Assets/Synty/PolygonCity/Prefabs/";
    public static bool Available => AssetDatabase.IsValidFolder("Assets/Synty/PolygonCity");
    const float Cell = 5, RoadY = -.16f, WalkY = -.07f, Half = 80;
    // v3 (2026-09-30): the block runs south past Harbor Road to a harbour promenade; water beyond.
    const float South = -120, HarborZ = -105;

    // Road rectangles (x0, x1, z0, z1)
    static readonly Rect[] Roads = {
        Rect.MinMaxRect(-Half, -5, Half, 5),      // Main Street
        Rect.MinMaxRect(-Half, 45, Half, 55),     // North Avenue
        Rect.MinMaxRect(-Half, -55, Half, -45),   // South Avenue
        Rect.MinMaxRect(-45, -110, -35, Half),    // West Street
        Rect.MinMaxRect(35, -110, 45, Half),      // East Street
        Rect.MinMaxRect(-Half, -110, Half, -100), // Harbor Road (new): behind Gus's lot, Truck Park to its north
    };
    // Areas the generator must leave alone (gameplay buildings and interiors).
    static readonly Rect[] Reserved = {
        Rect.MinMaxRect(-19.5f, 5, 25, 32),       // Milo's, apartments, The Alchemist
        Rect.MinMaxRect(5, 31, 25, 40),           // The Alchemist's back courtyard (to North Avenue)
        Rect.MinMaxRect(-17.5f, -26, -2.5f, -5),  // your restaurant and its interior
        Rect.MinMaxRect(-20, 60, 20, 70),         // Market Row: the street-market square off North Avenue
        Rect.MinMaxRect(-25, -40, -20, -25),      // The Flats: graffiti alley off South Avenue
        Rect.MinMaxRect(10, -40, 25, -25),        // The Flats: vacant lot where Greasy Gus parks his truck
        Rect.MinMaxRect(-25, 35, -15, 40),        // Market Row: two one-storey shop units, shuttered and to let
        Rect.MinMaxRect(25, -30, 30, -20),        // The Flats: the pawn shop on East Street
        Rect.MinMaxRect(-30, -95, 30, -60),       // Truck Park: the plaza around your food truck
        Rect.MinMaxRect(50, -95, 80, -60),        // Corner courts
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
        // Street rule: awnings hang on the shopfront above head height; they never reach across the sidewalk.
        if (k < 4) ShopAwning(b, x0, z0, facing, s);
        else if (k < 6) Put("Buildings/SM_Bld_Shop_Cover_04", OnFace(x0, z0, facing, 0, .45f, 3f), yaw, b);
        string[] flat = { "Sign_Cafe_01", "Sign_Pub_01", "Sign_Bar_01", "Sign_Chinese_Noodles_01" };
        if (g < 4 && k >= 4) Put("Props/SM_Prop_" + flat[g], OnFace(x0, z0, facing, 0, .7f, 2.75f), yaw, b);
        else if (g == 5 && !ClimbTower(x0, z0, out _, out _)) Put("Props/SM_Prop_ATM_01", OnFace(x0, z0, facing, 1.8f, .55f, 1.05f), yaw, b);
        else if (g == 6) Put("Props/SM_Prop_Sign_DeliPizza_01", OnFace(x0, z0, facing, -2.3f, .9f, 2.6f), yaw + 90, b);
        else if (g == 7) Put("Props/SM_Prop_Sign_Barber_01", OnFace(x0, z0, facing, -2.2f, .55f, 1.3f), yaw, b);
        if (H(s, 17) % 4 == 0 && !ClimbTower(x0, z0, out _, out _)) Put("Props/SM_Prop_Planter_02",   // never in front of a climbable fire escape
             OnFace(x0, z0, facing, 1.4f, .9f, 0), yaw, b);
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
        bool climb = ClimbTower(x0, z0, out string climbId, out int climbStacks); if (climb) stacks = climbStacks;
        var parent = new GameObject("Building").transform; parent.SetParent(root, false);
        string ground = groundOverride ?? (shop ? "Buildings/SM_Bld_Shop_0" + ShopGround[s % 5]
            : H(s, 3) % 3 == 0 ? "Buildings/SM_Bld_Apartment_Door_0" + (s % 2 + 1) : "Buildings/SM_Bld_Apartment_0" + family);
        if (shop && groundOverride == null && IsMainStreetShop(x0, z0, facing)) ground = MainShopGround(x0, z0);
        if (climb && shop && groundOverride == null) ground = "Buildings/SM_Bld_Shop_05";   // flat front: nothing juts out under the fire escape   // a glass front (never the roller-shutter unit)
        Module(ground, x0, z0, facing, 0, parent);
        for (int i = 0; i < stacks; i++) Module("Buildings/SM_Bld_Apartment_Stack_0" + family, x0, z0, facing, 3 + 9 * i, parent);
        float roofY = 3 + 9 * stacks;
        Module("Buildings/SM_Bld_Apartment_Roof_0" + family, x0, z0, facing, roofY, parent);
        Recolor(parent, palette >= 0 ? palette : H(s, 1) % 12);
        if (shop && groundOverride == null) { if (IsMainStreetShop(x0, z0, facing)) MainStreetShop(parent, x0, z0, facing); else Storefront(parent, x0, z0, facing, s); }
        if (stacks == 0 && shop) { if (H(s, 19) % 3 == 0) RoofSign(parent, x0, z0, facing, roofY + .5f, s); }
        else RoofClutter(parent, x0, z0, roofY + .5f, s);
        if (climb) ClimbableFireEscape(parent, x0, z0, facing, stacks, climbId);
        else if (stacks > 0 && groundOverride == null && H(s, 23) % 3 == 0) FireEscapes(parent, x0, z0, facing, stacks);
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
            if (x > -36 && x < 36 && z > -14 && z < 12) continue;    // hand-dressed Main Street (CityMainStreet.cs)
            if (z < -99 || (x > -31 && x < 30 && z > -61 && z < -54)) continue;   // the promenade and the front of Truck Park stay clear
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
        ShutteredUnit(-20f, 3.4f, -9.27f, p); ShutteredUnit(-3.4f, 3.4f, -9.27f, p);
    }
    // The Alchemist belongs to the block: three floors of apartments over the double-height hall (like your restaurant),
    // facades to Main Street and to the courtyard, a deep middle row so the roof is closed. The hall, its sign below the
    // first apartment floor, and the open courtyard are untouched; roof clutter is placed by AlchemistBuilder.
    static void AlchemistBuilding() {
        var p = new GameObject("The Alchemist's building").transform; p.SetParent(root, false);
        float y = AlchemistBuilder.StackBase, x0 = AlchemistBuilder.X0, z0 = AlchemistBuilder.Z0, z1 = AlchemistBuilder.Z1;
        var deep = new Vector3(1, 1, (z1 - z0 - 10) / 5);   // the middle row fills what the front and back rows leave
        for (float x = x0; x < AlchemistBuilder.X1 - .1f; x += 5) {
            Module("Buildings/SM_Bld_Apartment_Stack_01", x, z0, 2, y, p); Module("Buildings/SM_Bld_Apartment_Roof_01", x, z0, 2, y + 9, p);
            ModuleS("Buildings/SM_Bld_Apartment_Stack_01", x, z0 + 5, 2, y, deep, p); ModuleS("Buildings/SM_Bld_Apartment_Roof_01", x, z0 + 5, 2, y + 9, deep, p);
            Module("Buildings/SM_Bld_Apartment_Stack_01", x, z1 - 5, 0, y, p); Module("Buildings/SM_Bld_Apartment_Roof_01", x, z1 - 5, 0, y + 9, p);
        }
        Recolor(p, 4);
        StripColliders(p);
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
    // Market Row: two one-storey shop units facing North Avenue, roller shutters down and to let.
    static void MarketRowUnits() {
        Tower(-25, 35, 0, 0, true, "Buildings/SM_Bld_Shop_05", 7);
        Tower(-20, 35, 0, 0, true, "Buildings/SM_Bld_Shop_05", 7);
        var p = new GameObject("Market Row units (to let)").transform; p.SetParent(root, false);
        foreach (float x in new[] { -25f, -20 }) ShutteredUnit(x + .5f, 4f, 40.4f, p);
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
        GusTruck(new Vector3(17.5f, 0, -31.5f), p);
        // Gus's two customer tables are placed at runtime (GusCrew.cs), where his regulars eat his special.
        Put("Props/SM_Prop_Trashbin_02", new Vector3(21.8f, WalkY, -36.5f), 0, p);
        Put("Props/SM_Prop_Skip_02", new Vector3(21.5f, 0, -27.3f), 0, p);
        foreach (var (x, z) in new[] { (12.2f, -27.2f), (13.1f, -28.1f) }) FitPack("PolygonGeneric", "Props/SM_Gen_Prop_Crate_02", new Vector3(x, WalkY, z), x * 30, .8f, p);
        Put("Props/SM_Prop_LightPole_Base_02", new Vector3(10.8f, 0, -38.8f), 90, p);
    }
    // The Flats: a pawn shop on East Street. Its counter sells weapons (the pawnbroker is spawned at runtime).
    static void PawnShop() {
        Tower(25, -30, 1, 1, true, "Buildings/SM_Bld_Shop_03", 5);
        Tower(25, -25, 1, 1, true, "Buildings/SM_Bld_Shop_04", 5);
        var p = new GameObject("Pawn shop").transform; p.SetParent(root, false);
        var pa = FitPack("PolygonShops", "Buildings/SM_Bld_Awning_04_Small", OnFace(25, -30, 1, -2.5f, .05f, 2.7f), 90, 6f, p); if (pa) StripColliders(pa.transform);
        PoleSign("PAWN", new Vector3(33.6f, 0, -21.5f), "3D7F4E");
        var counter = FitPack("PolygonShops", "Props/SM_Prop_Market_Checkout_Large_01", new Vector3(31.4f, WalkY, -25), 90, 2.2f, p);
        if (!counter) counter = Slab("Pawn counter", new Vector3(31.4f, .5f, -25), new Vector3(.9f, 1f, 2.2f), InteriorMat("PawnCounter", "5C4633"), p, true);
        counter.name = "Pawn counter";
        FitPack("PolygonShops", "Props/SM_Prop_Kitchen_Pan_01", new Vector3(31.3f, 1.02f, -25.6f), 30, .45f, p);
        FitPack("PolygonShops", "Props/SM_Prop_Police_Baton_01", new Vector3(31.4f, 1.02f, -24.5f), 70, .6f, p);
        SignText("WEAPONS  /  CASH ONLY", new Vector3(31.9f, 1.55f, -25), 1, .07f, new Color(1f, .9f, .6f), p);
    }

    static void ShopAwning(Transform b, float x0, float z0, int facing, int s) {
        var a = FitPack("PolygonShops", "Buildings/SM_Bld_Awning_0" + (H(s, 41) % 5 + 1) + "_Small", OnFace(x0, z0, facing, 0, .05f, 2.7f), 90 * facing, 3.6f, b);
        if (a) StripColliders(a.transform);
    }

    // ---------- v3: Truck Park, Harbor Road, the harbour (see the Old Market layout map v3) ----------
    // Truck Park: the plaza between South Avenue and Harbor Road, laid out around your food truck. The truck parks
    // along the north edge (z -62.5) with its hatch facing south; the four picnic tables sit south of it, then a lawn.
    public static readonly Vector3 TruckSpot = new Vector3(0, 0, -62.5f);
    // The stand's own frame (stations along z 8.3, cook side z 9.35) is parked inside Little Flame by this offset;
    // y lifts it onto the truck's raised floor.
    public static readonly Vector3 StandOffset = new Vector3(0, TruckFloor, -71.75f);
    public const float TruckFloor = .816f;
    const string TruckModel = "Assets/Art/LittleFlame/LittleFlame.obj";
    // Little Flame: your walk-in food truck (Tripo AI model, reduced and painted in teal/cream/coral; see
    // docs/ASSET_SOURCES.md). Serving window and door face the plaza (south), cab at the east end. The mesh is
    // visual only: the walls, raised floor and the ramp up to the door are simple colliders so walking is smooth.
    // Inside, PhysicalStand puts the board, grill, serving counter and plate rack along the window and the pantry,
    // sink and bin on the back wall, with the cook lane between.
    // The truck mesh is painted in named regions (LittleFlame.obj: LF_Teal = lower body, LF_Cream = upper body and
    // roof, LF_Coral = the band, LF_Pin = a pinstripe under it, awning stripes alternate Coral/Cream, plus trim,
    // underside, tyres, floor and interior). Each truck maps those regions to its own colours.
    static readonly (string region, string hex)[] FlamePaint = { ("Pin", "2E8C8A"), ("Under", "24282C"), ("Trim", "5E666E"), ("Teal", "2E8C8A"), ("Cream", "F1E6CF"), ("Coral", "E0603C"), ("Chrome", "C9CED3"), ("Tyre", "1B1C1F"), ("Floor", "6B5646"), ("InnerLow", "2E8C8A"), ("Inner", "EFE4CC") };
    // Greasy Gus: fire-engine red on top (and on the awning), a charcoal lower body, a flame-orange band over a
    // yellow pinstripe, a smoky interior.
    static readonly (string region, string hex)[] GusPaint = { ("Pin", "F2C230"), ("Under", "24282C"), ("Trim", "5E666E"), ("Teal", "2B2A2E"), ("Cream", "B3261E"), ("Coral", "F0641E"), ("Chrome", "C9CED3"), ("Tyre", "1B1C1F"), ("Floor", "3A2E2C"), ("InnerLow", "2B2A2E"), ("Inner", "4A3A36") };
    static GameObject TruckBody(Transform t, string name, string matPrefix, (string region, string hex)[] paint) {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(TruckModel);
        if (!model) { Debug.LogWarning("Truck model missing at " + TruckModel + "; " + name + " has colliders but no body."); return null; }
        var m = (GameObject)Object.Instantiate(model, t); m.name = name; m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
        StripColliders(m.transform);
        foreach (var r in m.GetComponentsInChildren<Renderer>()) {
            var mats = r.sharedMaterials;
            // Longest region names first ("InnerLow" before "Inner").
            for (int k = 0; k < mats.Length; k++) foreach (var (region, hex) in paint) if (mats[k] && mats[k].name.Contains("LF_" + region)) { mats[k] = InteriorMat(matPrefix + region, hex); break; }
            r.sharedMaterials = mats;
        }
        return m;
    }
    // A slab placed in a (possibly rotated) truck's own frame.
    static GameObject SlabIn(Transform frame, string n, Vector3 local, Vector3 size, Material m, Transform parent) {
        var g = Slab(n, frame.TransformPoint(local), size, m, parent, false); g.transform.rotation = frame.rotation; return g;
    }
    // Greasy Gus's truck: the same truck as yours in his colours, parked along the lot with its window and door
    // facing into the lot (-X) and its cab toward South Avenue. "Greasy Gus's truck" stays an unrotated root at the
    // origin: the raid target collider (PrototypeBuilder / RaidFight) is centred on the rival's X/Z from it.
    static void GusTruck(Vector3 c, Transform parent) {
        var t = new GameObject("Greasy Gus's truck").transform; t.SetParent(parent, false);
        var body = new GameObject("Gus truck body").transform; body.SetParent(t, false); body.SetPositionAndRotation(c, Quaternion.Euler(0, 90, 0));
        TruckBody(body, "Gus truck model", "Gus", GusPaint);
        var solid = body.gameObject.AddComponent<BoxCollider>(); solid.center = new Vector3(0, 1.7f, .17f); solid.size = new Vector3(8.2f, 3.4f, 3.4f);
        var charcoal = InteriorMat("GusTeal", "2B2A2E"); var orange = InteriorMat("GusCoral", "F0641E"); var yellow = InteriorMat("GusPin", "F2C230"); var cream = InteriorMat("FlameCream", "F1E6CF");
        // Roof sign over the window: charcoal board, orange frame, GREASY in yellow and GUS in orange.
        SlabIn(body, "Roof sign", new Vector3(-1f, 4.15f, -.9f), new Vector3(4.4f, .72f, .08f), charcoal, t);
        SlabIn(body, "Roof sign edge", new Vector3(-1f, 4.15f, -.86f), new Vector3(4.6f, .86f, .04f), orange, t);
        foreach (float x in new[] { -2.6f, .6f }) SlabIn(body, "Roof sign post", new Vector3(x, 3.75f, -.84f), new Vector3(.06f, .5f, .06f), orange, t);
        float yaw = body.eulerAngles.y;
        Letters3D("GREASY GUS", body.TransformPoint(new Vector3(-1f, 4.15f, -1.02f)), yaw + 180, .46f, 4.1f, yellow, orange, t);
        // The name again along the red upper body on the street (east) side, cream on red.
        Letters3D("GREASY GUS", body.TransformPoint(new Vector3(-.5f, 2.6f, 2.03f)), yaw, .42f, 4.6f, yellow, cream, t);
        // A hot, smoky glow in the window and bulbs along the awning.
        var glow = new GameObject("Gus hatch light").AddComponent<Light>(); glow.transform.SetParent(t, false); glow.transform.position = body.TransformPoint(new Vector3(-1.5f, 3.0f, .2f));
        glow.type = LightType.Point; glow.color = new Color(1f, .45f, .22f); glow.intensity = 1.8f; glow.range = 7.5f;
        var bulb = GlowMat("GUSBULB", "FF8A3A", 1.5f);
        for (int i = 0; i < 7; i++) { var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Bulb"; Object.DestroyImmediate(b.GetComponent<Collider>()); b.transform.SetParent(t, false); b.transform.position = body.TransformPoint(new Vector3(-2.65f + i * .43f, 2.85f, -2.35f)); b.transform.localScale = Vector3.one * .09f; b.GetComponent<Renderer>().sharedMaterial = bulb; }
        // His menu A-frame out in the lot by the window.
        FitPack("PolygonShops", "Props/SM_Prop_Cafe_Sign_Outdoor_01", body.TransformPoint(new Vector3(-3.4f, WalkY, -2.9f)), 90, .8f, t);
    }
    static void LittleFlame() {
        var t = new GameObject("Little Flame (your food truck)").transform; t.SetParent(root, false); t.position = TruckSpot;
        TruckBody(t, "Little Flame model", "Flame", FlamePaint);
        var inv = InteriorMat("TruckCollider", "808080");
        void Box(string n, float x0, float x1, float y0, float y1, float z0, float z1) {
            var b = Slab(n, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2) + TruckSpot, new Vector3(x1 - x0, y1 - y0, z1 - z0), inv, t, true);
            b.GetComponent<Renderer>().enabled = false;
        }
        float f = TruckFloor, top = 3.4f, zs0 = -1.53f, zs1 = -1.42f, zn = 1.83f;   // window wall (outer/inner) and back wall, truck-local z
        Box("Chassis and floor", -4.05f, 4.05f, 0, f, zs0, zn + .1f);
        Box("Wall under window", -3.9f, .94f, f, 1.74f, zs0, zs1);
        Box("Wall west of window", -3.9f, -2.72f, f, top, zs0, zs1);
        Box("Wall above window", -2.72f, 0, 3.15f, top, zs0, zs1);
        Box("Wall window-to-door", 0, .94f, f, top, zs0, zs1);
        Box("Cab wall", 1.91f, 3.9f, f, top, zs0, zs1);
        Box("Back wall", -3.9f, 3.9f, f, top, zn, zn + .12f);
        Box("West end", -3.95f, -3.83f, f, top, zs0, zn);
        Box("Cab front", 3.5f, 3.62f, f, top, zs0, zn);
        // Ramp from the plaza up to the door (1.93 m run for the .82 m rise, about 23 degrees).
        var ramp = Slab("Door ramp", Vector3.zero, new Vector3(.97f, .1f, 2.1f), inv, t, true); ramp.GetComponent<Renderer>().enabled = false;
        float run = 1.93f; ramp.transform.position = TruckSpot + new Vector3(1.42f, f / 2 - .05f, zs0 - run / 2);
        ramp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(f, run) * Mathf.Rad2Deg, 0, 0);
        // Warm light inside, bulbs along the awning, and the name board on the roof facing the plaza.
        var inside = new GameObject("Truck light").AddComponent<Light>(); inside.transform.SetParent(t, false); inside.transform.localPosition = new Vector3(-1.5f, 3.0f, .2f);
        inside.type = LightType.Point; inside.color = new Color(1f, .86f, .62f); inside.intensity = 1.6f; inside.range = 7.5f;
        var bulb = GlowMat("FLAMEBULB", "FFE3A0", 1.5f);
        for (int i = 0; i < 7; i++) { var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Bulb"; Object.DestroyImmediate(b.GetComponent<Collider>()); b.transform.SetParent(t, false); b.transform.localPosition = new Vector3(-2.65f + i * .43f, 2.85f, -2.35f); b.transform.localScale = Vector3.one * .09f; b.GetComponent<Renderer>().sharedMaterial = bulb; }
        var cream = InteriorMat("FlameCream", "F1E6CF"); var coral = InteriorMat("FlameCoral", "E0603C");
        // Roof sign in the truck's colours: cream board, teal frame, 3D pack letters ("LITTLE" teal, "FLAME" coral).
        var tealM = InteriorMat("FlameTeal", "2E8C8A");
        Slab("Roof sign", TruckSpot + new Vector3(-1f, 4.15f, -.9f), new Vector3(4.4f, .72f, .08f), cream, t, false);
        Slab("Roof sign edge", TruckSpot + new Vector3(-1f, 4.15f, -.86f), new Vector3(4.6f, .86f, .04f), tealM, t, false);
        foreach (float x in new[] { -2.6f, .6f }) Slab("Roof sign post", TruckSpot + new Vector3(x, 3.75f, -.84f), new Vector3(.06f, .5f, .06f), tealM, t, false);
        if (!Letters3D("LITTLE FLAME", TruckSpot + new Vector3(-1f, 4.15f, -1.02f), 180, .46f, 4.1f, tealM, coral, t))
            SignText("LITTLE FLAME", TruckSpot + new Vector3(-1f, 4.17f, -.95f), 2, .27f, new Color(.72f, .2f, .1f), t);
        // Street side: the name in letters across the cream band, the menu line under it.
        if (!Letters3D("LITTLE FLAME", TruckSpot + new Vector3(-.5f, 2.55f, zn + .2f), 0, .42f, 4.6f, tealM, coral, t))
            SignText("LITTLE FLAME", TruckSpot + new Vector3(-.5f, 2.55f, zn + .2f), 0, .2f, new Color(.85f, .3f, .18f), t);
        SignText("BURGERS  /  SALAD", TruckSpot + new Vector3(-.5f, 2.0f, zn + .2f), 0, .09f, new Color(.16f, .45f, .44f), t);
    }
    // A word in POLYGON Shops 3D letters, centred on `centre` and readable from direction `faceYaw` (0 = from +Z,
    // 180 = from -Z, 90 = from +X). The first word takes `first`, the rest `rest`. Shrinks to fit `maxWidth`.
    // False if the pack is missing.
    internal static bool Letters3D(string text, Vector3 centre, float faceYaw, float height, float maxWidth, Material first, Material rest, Transform parent) {
        const string dir = "Assets/Synty/PolygonShops/Prefabs/Signs/SM_Sign_3dText_Letter_";
        var face = Quaternion.Euler(0, faceYaw, 0); var toViewer = face * Vector3.forward;
        var right = Vector3.Cross(Vector3.up, -toViewer).normalized;   // the reader's right
        var made = new List<(GameObject g, float w)>(); float gap = .03f, space = height * .45f, total = 0; bool firstWord = true;
        float Across(Bounds b) => Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(right.x), 0, Mathf.Abs(right.z))));
        foreach (char ch in text) {
            if (ch == ' ') { made.Add((null, space)); total += space; firstWord = false; continue; }
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(dir + ch + ".prefab"); if (!src) { foreach (var m in made) if (m.g) Object.DestroyImmediate(m.g); return false; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(src, parent); g.name = "Letter " + ch;
            StripColliders(g.transform);
            g.transform.rotation = face;   // pack letters read from +Z unrotated
            var b = RendererBounds(g); g.transform.localScale *= height / b.size.y; b = RendererBounds(g);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = firstWord ? first : rest;
            made.Add((g, Across(b))); total += Across(b) + gap;
        }
        total -= gap; float k = total > maxWidth ? maxWidth / total : 1, cursor = -total * k / 2;
        foreach (var (g, w) in made) {
            if (!g) { cursor += w * k; continue; }
            g.transform.localScale *= k; var b = RendererBounds(g); float wk = w * k;
            g.transform.position += centre + right * (cursor + wk / 2) - b.center;
            cursor += wk + gap * k;
        }
        return true;
    }
    static Bounds RendererBounds(GameObject g) {
        var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    // Milo's cart: a produce stand beside Little Flame selling only the truck's basics (patties, buns, greens), so a new
    // player can restock without crossing two blocks. Rose, Milo's niece, works it (spawned at runtime by PhysicalStand).
    public static readonly Vector3 CartSpot = new Vector3(-10.5f, 0, -63.4f);
    static void MarketCart() {
        var p = new GameObject("Milo's cart").transform; p.SetParent(root, false); p.position = CartSpot;
        var stand = FitPack("PolygonShops", "Props/SM_Prop_Market_Produce_Stand_01", CartSpot + new Vector3(0, WalkY, 0), 180, 2.4f, p);
        if (stand) { var box = stand.AddComponent<BoxCollider>(); var b = RendererBounds(stand); box.center = stand.transform.InverseTransformPoint(b.center); box.size = new Vector3(b.size.x, b.size.y, b.size.z) / Mathf.Max(.01f, stand.transform.lossyScale.x); }
        else Slab("Cart counter", CartSpot + new Vector3(0, .5f, 0), new Vector3(2.2f, 1f, .9f), InteriorMat("CartWood", "8A5A3C"), p, true);
        // The pack's produce insert fills the stand's crates (same transform as the stand), under a cafe parasol.
        if (stand) { var ins = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonShops/Prefabs/Props/SM_Prop_Market_Produce_Stand_01_Insert_01.prefab");
            if (ins) { var g = (GameObject)PrefabUtility.InstantiatePrefab(ins, p); g.transform.SetPositionAndRotation(stand.transform.position, stand.transform.rotation); g.transform.localScale = stand.transform.localScale; StripColliders(g.transform); } }
        FitPack("PolygonShops", "Props/SM_Prop_Cafe_Parasol_01", CartSpot + new Vector3(.4f, WalkY, .9f), 0, 2.8f, p);
        // Price board facing the plaza.
        var board = Slab("Cart sign", CartSpot + new Vector3(-1.55f, 1.05f, -.2f), new Vector3(.08f, .9f, .9f), InteriorMat("CartBoard", "22262B"), p, false);
        board.transform.rotation = Quaternion.Euler(0, 90, 0);
        SignText("MILO'S CART\nPATTIES  BUNS\nGREENS", CartSpot + new Vector3(-1.55f, 1.1f, -.26f), 2, .055f, new Color(1f, .95f, .8f), p);
    }
    static void TruckPark() {
        var p = new GameObject("Truck Park").transform; p.SetParent(root, false);
        for (float x = -20; x < 20; x += 5) for (float z = -90; z < -75; z += 5)
            Put("Environments/SM_Env_Grass_01", new Vector3(x, WalkY + .03f, z + 5), 0, p);
        int k = 40;
        foreach (float z in new[] { -64f, -78, -92 }) { StreetTree(new Vector3(-27.5f, 0, z), k++, p); StreetTree(new Vector3(27.5f, 0, z), k++, p); }
        foreach (float x in new[] { -12f, 12 }) StreetTree(new Vector3(x, 0, -93), k++, p);
        foreach (float x in new[] { -10f, 10 }) Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, -74.3f), 180, p);
        foreach (var (x, z) in new[] { (-24f, -61f), (24f, -61f), (-24f, -94f), (24f, -94f) }) Put("Props/SM_Prop_Trashbin_01", new Vector3(x, 0, z), 0, p);
        // String lights: two warm catenaries between four poles, plus two soft lights.
        var pole = InteriorMat("LightPole", "2A2E33"); var bulb = GlowMat("PARKBULB", "FFD98A", 1.6f); var cable = InteriorMat("Cable", "1E1F22");
        foreach (float z in new[] { -76f, -89 }) {   // south of the tables, so the cables never cross the truck's roof sign
            foreach (float x in new[] { -24f, 24 }) Slab("String light pole", new Vector3(x, 2.4f, z), new Vector3(.14f, 4.8f, .14f), pole, p, true);
            Vector3 prev = new Vector3(-24, 4.6f, z);
            for (int i = 1; i <= 16; i++) {
                float t = i / 16f; var pt = new Vector3(Mathf.Lerp(-24, 24, t), 4.6f - Mathf.Sin(t * Mathf.PI) * 1.1f, z);
                var seg = Slab("Cable", (prev + pt) / 2, new Vector3(.035f, .035f, Vector3.Distance(prev, pt)), cable, p, false); seg.transform.rotation = Quaternion.LookRotation(pt - prev);
                var bl = GameObject.CreatePrimitive(PrimitiveType.Sphere); bl.name = "Bulb"; Object.DestroyImmediate(bl.GetComponent<Collider>()); bl.transform.SetParent(p, false);
                bl.transform.position = pt + Vector3.down * .12f; bl.transform.localScale = Vector3.one * .16f; bl.GetComponent<Renderer>().sharedMaterial = bulb; prev = pt;
            }
            var glow = new GameObject("String light glow").AddComponent<Light>(); glow.transform.SetParent(p, false); glow.transform.position = new Vector3(0, 3.6f, z);
            glow.type = LightType.Point; glow.color = new Color(1f, .83f, .55f); glow.intensity = 1.6f; glow.range = 16;
        }
        // Mural wall at the south-west corner, bollards along the south edge.
        var wallMat = InteriorMat("MuralWall", "C9B8A0"); Slab("Mural wall", new Vector3(-21, 1.6f, -93), new Vector3(9, 3.2f, .4f), wallMat, p, true);
        var paint = new[] { InteriorMat("TagPink", "E0479A"), InteriorMat("TagTeal", "2FB7A8"), InteriorMat("TagYellow", "F2C230"), InteriorMat("TagPurple", "8D6AE0") };
        for (int i = 0; i < 5; i++) Slab("Mural paint", new Vector3(-24.6f + i * 1.8f, 1.5f + (i % 2) * .4f, -92.78f), new Vector3(1.6f, 1.6f + (i % 3) * .4f, .03f), paint[i % 4], p, false);
        SignText("EAT LATE  /  OLD MARKET", new Vector3(-21, 2.85f, -92.76f), 0, .16f, new Color(.16f, .12f, .1f), p);
        for (float x = 16; x <= 25; x += 1.5f) Slab("Bollard", new Vector3(x, .45f, -93.5f), new Vector3(.22f, .9f, .22f), pole, p, true);
    }
    // Corner courts: a fenced basketball court with benches, where residents hang out.
    static void CornerCourts() {
        var p = new GameObject("Corner courts").transform; p.SetParent(root, false);
        var court = InteriorMat("Court", "3F7F7A"); var line = InteriorMat("CourtLine", "EDE8DA"); var steel = InteriorMat("HoopSteel", "C9CED3"); var board = InteriorMat("Backboard", "F4F1E8"); var rim = InteriorMat("Rim", "E0692E");
        var c = new Vector3(65, 0, -78);
        Slab("Court", c + Vector3.up * -.03f, new Vector3(26, .06f, 15), court, p, false);
        foreach (var (o, s) in new[] { (new Vector3(0, 0, 7.4f), new Vector3(26, .01f, .12f)), (new Vector3(0, 0, -7.4f), new Vector3(26, .01f, .12f)), (new Vector3(12.9f, 0, 0), new Vector3(.12f, .01f, 15)), (new Vector3(-12.9f, 0, 0), new Vector3(.12f, .01f, 15)), (Vector3.zero, new Vector3(.12f, .01f, 15)) })
            Slab("Court line", c + o + Vector3.up * .005f, s, line, p, false);
        foreach (int side in new[] { -1, 1 }) {
            var basePos = c + new Vector3(side * 12.3f, 0, 0);
            Slab("Hoop pole", basePos + Vector3.up * 1.6f, new Vector3(.18f, 3.2f, .18f), steel, p, true);
            Slab("Backboard", basePos + new Vector3(-side * .5f, 3.3f, 0), new Vector3(.08f, 1.1f, 1.8f), board, p, false);
            var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder); r.name = "Rim"; Object.DestroyImmediate(r.GetComponent<Collider>()); r.transform.SetParent(p, false);
            r.transform.position = basePos + new Vector3(-side * .9f, 3.05f, 0); r.transform.localScale = new Vector3(.46f, .02f, .46f); r.GetComponent<Renderer>().sharedMaterial = rim;
        }
        for (float x = 50; x < 79.99f; x += 5) { Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, -69.6f), 0, p); Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, -86.4f), 0, p); }
        foreach (float x in new[] { 56f, 66, 74 }) Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, -67.6f), 180, p);
        int k = 70; foreach (float x in new[] { 53f, 77 }) StreetTree(new Vector3(x, 0, -91), k++, p);
        SignText("CORNER COURTS", new Vector3(65, 2.2f, -69.4f), 0, .14f, new Color(1f, .9f, .6f), p);
    }
    // The harbour: promenade railing, benches and lamps along the quay; water; the Docks' cranes across it; and the
    // locked bridge to The Docks at the west end of Harbor Road.
    static void Harbour() {
        var p = new GameObject("Harbour").transform; p.SetParent(root, false);
        var stone = InteriorMat("QuayStone", "6F6A62");
        Slab("Quay wall", new Vector3(0, -.75f, -120.3f), new Vector3(2 * Half + 60, 1.4f, .6f), stone, p, true);
        var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Misc/Water_01.mat") ?? InteriorMat("Water", "2A6A80");
        Slab("Bay water", new Vector3(0, -1.25f, -320), new Vector3(1000, .1f, 400), water, p, false);
        for (float x = -Half; x < Half - .01f; x += 5) Put("Environments/SM_Env_Fence_01", new Vector3(x, 0, -119.5f), 0, p);
        for (float x = -70; x <= 70; x += 20) { Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, -117.6f), 180, p); Put("Props/SM_Prop_LightPole_Base_02", new Vector3(x + 10, 0, -110.8f), 180, p); }
        // The Docks across the water: cranes and container stacks, just shapes on the horizon.
        var crane = InteriorMat("Crane", "D8A032"); string[] cols = { "B8402F", "2F6DB8", "D1A33A", "3F8F5A", "7A7F86" };
        foreach (float x in new[] { -60f, -10, 45 }) {
            foreach (float dx in new[] { -4f, 4 }) foreach (float dz in new[] { -205f, -197 }) Slab("Crane leg", new Vector3(x + dx, 9, dz), new Vector3(.8f, 20, .8f), crane, p, false);
            Slab("Crane beam", new Vector3(x, 19.5f, -190), new Vector3(1.4f, 1.4f, 34), crane, p, false);
        }
        for (int i = 0; i < 10; i++) for (int j = 0; j <= H(i, 3) % 3; j++)
            Slab("Container", new Vector3(-75 + i * 15, .1f + j * 2.6f, -215 + (i % 2) * 6), new Vector3(6, 2.6f, 2.4f), InteriorMat("Box" + cols[H(i, j) % 5], cols[H(i, j) % 5]), p, false);
        Slab("Docks ground", new Vector3(0, -1.1f, -225), new Vector3(220, .4f, 50), stone, p, false);
        // Bridge to The Docks off the west end of Harbor Road (locked; the gate is in StreetEnds).
        Slab("Bridge deck", new Vector3(-110, -.2f, HarborZ), new Vector3(60, .4f, 10), stone, p, false);
        foreach (float dz in new[] { -5f, 5 }) Slab("Bridge rail", new Vector3(-110, .5f, HarborZ + dz), new Vector3(60, 1, .2f), InteriorMat("BridgeRail", "D9D4C8"), p, false);
        Put("Props/SM_Prop_BusStop_01", new Vector3(-65, 0, -97.2f), 180, p);
    }
    // Zebra crossings at every corner (street rule), plus the mid-block one from Truck Park to Gus's lot.
    static void Crosswalks() {
        var p = new GameObject("Crosswalks").transform; p.SetParent(root, false); var paint = InteriorMat("Crosswalk", "E8E4D8");
        void Band(float cx, float cz, bool alongX) {
            // alongX: the crossing runs across an east-west road (bars lie along x, stacked across z).
            for (float o = -4f; o <= 4.01f; o += 1.1f)
                Slab("Zebra", new Vector3(cx + (alongX ? 0 : o), -.03f, cz + (alongX ? o : 0)), alongX ? new Vector3(3f, .01f, .55f) : new Vector3(.55f, .01f, 3f), paint, p, false);
        }
        foreach (float ix in new[] { -40f, 40 }) foreach (float iz in new[] { 50f, 0, -50, HarborZ }) {
            Band(ix - 7.5f, iz, true); Band(ix + 7.5f, iz, true);
            if (iz != HarborZ) Band(ix, iz - 7.5f, false);
            Band(ix, iz + 7.5f, false);
        }
        Band(19, -50, true);
    }

    // Barricades where streets leave the block, and the gate to The Docks at the west end of Harbor Road.
    static void StreetEnds() {
        var p = new GameObject("Street ends").transform; p.SetParent(root, false);
        void Line(Vector3 c, bool acrossX, float length) {
            var dir = acrossX ? Vector3.right : Vector3.forward; float yaw = acrossX ? 0 : 90;
            for (float t = -length / 2 + .8f; t <= length / 2 - .7f; t += 1.6f) Put("Props/SM_Prop_Barrier_01", c + dir * t + Vector3.up * RoadY, yaw, p);
            foreach (float e in new[] { -length / 2 - .6f, length / 2 + .6f }) Put("Props/SM_Prop_Cone_01", c + dir * e + Vector3.up * RoadY, 0, p);
        }
        foreach (float z in new[] { 0f, 50, -50 }) foreach (int side in new[] { -1, 1 }) Line(new Vector3(side * 77.5f, 0, z), false, 10);
        Line(new Vector3(-40, 0, 77.5f), true, 10); Line(new Vector3(40, 0, 77.5f), true, 10); Line(new Vector3(77.5f, 0, HarborZ), false, 10);
        // The Docks gate at the west end of Harbor Road, before the bridge: locked until you reach Line Cook.
        var gate = new GameObject("Gate to The Docks").transform; gate.SetParent(p, false);
        Line(new Vector3(-77.5f, 0, HarborZ), false, 10);
        var post = InteriorMat("GatePost", "2A2E33"); var board = GlowMat("DOCKSGATE", "3AA0B0", .25f);
        foreach (float z in new[] { HarborZ - 5.4f, HarborZ + 5.4f }) Slab("Gate post", new Vector3(-77.5f, 2.6f, z), new Vector3(.35f, 5.2f, .35f), post, gate, true);
        Slab("Gate board", new Vector3(-77.5f, 4.6f, HarborZ), new Vector3(.25f, 1.6f, 10.4f), board, gate, false);
        SignText("THE DOCKS", new Vector3(-77.35f, 4.95f, HarborZ), 1, .5f, Color.white, gate);
        SignText("Bridge opens at LINE COOK", new Vector3(-77.35f, 4.2f, HarborZ), 1, .3f, new Color(1, .92f, .75f), gate);
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
        for (float x = -Half; x < Half; x += Cell) for (float z = South; z < Half; z += Cell) {
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
        RestaurantBuilding();
        AlchemistBuilding();
        PoleSign("THE ODD TABLE", new Vector3(-1.6f, 0, -7.6f), "C8553D");
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
        Block(-80, -50, -95, -60, true,  true,  true,  false, false, 1, 2, 0);   // Cannery lofts (south of South Avenue)
        MainStreetSidewalks();
        // Truck Park (-30..30) and the corner courts (50..80) fill the other southern blocks.
        // Landmarks in the outer blocks.
        Put("Buildings/SM_Bld_CityHall_01", new Vector3(-67, 0, 25), 180);
        Put("Buildings/SM_Bld_OfficeRound_01", new Vector3(-67, 0, -25), 0);
        Put("Buildings/SM_Bld_OfficeSquare_01", new Vector3(80, 0, 35), 0);
        Put("Buildings/SM_Bld_OfficeOctagon_01", new Vector3(-67, 0, 72), 0);
        Put("Buildings/SM_Bld_OfficeOld_Small_01", new Vector3(20, 0, 80), 0);
        Put("Buildings/SM_Bld_Station_01", new Vector3(0, 0, -30), 0);
        Put("Environments/Custom/SM_Env_Skyline_01", Vector3.zero, 0);
        // Old Market's own places (Market Row, The Flats) and the street ends.
        MarketStalls(); MarketRowUnits(); GraffitiAlley(); VacantLot(); PawnShop(); TruckPark(); LittleFlame(); MarketCart(); CornerCourts(); Harbour(); Crosswalks(); StreetEnds(); StreetSigns();
        // Street life: parked cars, trees, benches, hydrants, a hotdog cart, bus stop, rooftop signs.
        string[] cars = { "SM_Veh_Car_Sedan_01", "SM_Veh_Car_Taxi_01", "SM_Veh_Car_Van_01", "SM_Veh_Car_Small_01", "SM_Veh_Car_Medium_01", "SM_Veh_Car_Muscle_01" };
        int c = 0;
        foreach (float x in new[] { -75f, -62, -52, 28, 55, 68 }) Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, x > 0 ? 3.3f : -3.3f), x > 0 ? 90 : 270);
        foreach (float z in new[] { -70f, -30, 20, 70 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(-43.3f, RoadY, z), 0); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(43.3f, RoadY, z + 8), 180); }
        foreach (float x in new[] { -70f, -30, 10, 50 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, 46.7f), 90); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x + 12, RoadY, -46.7f), 270); }
        int t = 0;
        for (float x = -75; x <= 75; x += 15) {
            if (Mathf.Abs(x) < 30 || Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue;
            StreetTree(new Vector3(x, 0, 6.3f), t++); StreetTree(new Vector3(x + 5, 0, -6.3f), t++);   // street rule: trees in pits at the curb
        }
        for (float x = -75; x <= 75; x += 20) { if (Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue; StreetTree(new Vector3(x, 0, 56.3f), t++); if (Mathf.Abs(x) > 32) StreetTree(new Vector3(x, 0, -56.3f), t++); }
        for (float z = -95; z <= 75; z += 20) { if (Mathf.Abs(z) < 12 || Mathf.Abs(Mathf.Abs(z) - 50) < 6) continue; StreetTree(new Vector3(-33.7f, 0, z), t++); StreetTree(new Vector3(33.7f, 0, z), t++); }
        Put("Props/SM_Prop_HotdogStand_01", new Vector3(30, 0, 8), 180);
        Put("Props/SM_Prop_BusStop_01", new Vector3(-30, 0, -7.2f), 0);
        foreach (float x in new[] { -28f, 27, -55, 60 }) { Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, x > 0 ? 8.6f : -8.6f), x > 0 ? 180 : 0); }
        foreach (float x in new[] { -34f, 34, -48, 48 }) Put("Props/SM_Prop_Hydrant_01", new Vector3(x, 0, x > 0 ? 6 : -6), 0);
        foreach (float x in new[] { -26f, 26, -60, 60 }) Put("Props/SM_Prop_Trashbin_01", new Vector3(x, 0, -6.2f), 0);
        foreach (float x in new[] { -60f, -20, 20, 60 }) { Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, 44.2f), 0); Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, -44.2f), 180); }
        Clutter(curbTiles);
        // Flat ground beyond the block (under the skyline) and the invisible edge of the playable city.
        Slab("Outskirts", new Vector3(0, -.36f, 165), new Vector3(900, .2f, 570), InteriorMat("Outskirts", "8E8A82"), root, false);   // stops at the quay; water beyond
        foreach (var (pos, size) in new[] { (new Vector3(0, 5, Half + 1), new Vector3(2 * Half, 10, 1)), (new Vector3(0, 5, South - .8f), new Vector3(2 * Half, 10, 1)), (new Vector3(Half + 1, 5, (Half + South) / 2), new Vector3(1, 10, Half - South)), (new Vector3(-Half - 1, 5, (Half + South) / 2), new Vector3(1, 10, Half - South)) }) {
            var wall = new GameObject("City edge"); wall.transform.SetParent(root, false); wall.transform.position = pos; wall.AddComponent<BoxCollider>().size = size;
        }
    }
}
