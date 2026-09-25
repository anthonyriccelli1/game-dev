using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Builds the explorable city from POLYGON City modules on a 5 m grid:
// a 160 m x 160 m district with three east-west avenues, two north-south streets, full sidewalks,
// modular shop/apartment rows, landmark towers, street furniture and a 360-degree skyline.
// Gameplay locations (stand, Milo's, your restaurant, The Gilded Orbit, rival alley) keep their spots on Main Street.
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
    };
    static Transform root; static int seed;

    static bool IsRoad(float x, float z) { foreach (var r in Roads) if (r.Contains(new Vector2(x, z))) return true; return false; }
    static bool IsReserved(Rect r) { foreach (var q in Reserved) if (q.Overlaps(r)) return true; return false; }

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

    // One 5 m-wide building: shop or apartment ground floor, N apartment stacks (3 floors each), roof.
    static void Tower(float x0, float z0, int facing, int stacks, bool shop) {
        var r = new Rect(x0, z0, 5, 5); if (IsReserved(r)) return;
        int s = seed++; int family = s % 3 + 1;
        var parent = new GameObject("Building").transform; parent.SetParent(root, false);
        string ground = shop ? "Buildings/SM_Bld_Shop_0" + new[] { 1, 2, 4, 5, 6 }[s % 5] : "Buildings/SM_Bld_Apartment_0" + family;
        Module(ground, x0, z0, facing, 0, parent);
        for (int i = 0; i < stacks; i++) Module("Buildings/SM_Bld_Apartment_Stack_0" + family, x0, z0, facing, 3 + 9 * i, parent);
        Module("Buildings/SM_Bld_Apartment_Roof_0" + family, x0, z0, facing, 3 + 9 * stacks, parent);
    }
    static void ForceTower(float x0, float z0, int facing, int stacks, bool shop) { Tower2(x0, z0, facing, stacks, shop); }
    static void Tower2(float x0, float z0, int facing, int stacks, bool shop) {
        int s = seed++; int family = s % 3 + 1;
        var parent = new GameObject("Building").transform; parent.SetParent(root, false);
        Module(shop ? "Buildings/SM_Bld_Shop_0" + new[] { 1, 2, 4, 5, 6 }[s % 5] : "Buildings/SM_Bld_Apartment_0" + family, x0, z0, facing, 0, parent);
        for (int i = 0; i < stacks; i++) Module("Buildings/SM_Bld_Apartment_Stack_0" + family, x0, z0, facing, 3 + 9 * i, parent);
        Module("Buildings/SM_Bld_Apartment_Roof_0" + family, x0, z0, facing, 3 + 9 * stacks, parent);
    }
    // A row of towers along a line. Rows facing +/-Z run along X; rows facing +/-X run along Z.
    static void Row(float from, float to, float fixedCoord, int facing, int minStacks, int maxStacks, bool shops) {
        for (float a = from; a + 5 <= to + .01f; a += 5) {
            bool alongX = facing == 0 || facing == 2;
            float x0 = alongX ? a : fixedCoord, z0 = alongX ? fixedCoord : a;
            if (IsRoad(x0 + 2.5f, z0 + 2.5f)) continue;
            int span = maxStacks - minStacks + 1, stacks = minStacks + ((seed * 7 + (int)a) % span + span) % span;
            Tower(x0, z0, facing, stacks, shops);
        }
    }

    public static void Build(Transform world) {
        root = new GameObject("City (POLYGON City)").transform; root.SetParent(world, false); seed = 3;
        var ground = new GameObject("Ground and streets").transform; ground.SetParent(root, false);
        // Roads and sidewalks on the 5 m grid.
        for (float x = -Half; x < Half; x += Cell) for (float z = -Half; z < Half; z += Cell) {
            float cx = x + 2.5f, cz = z + 2.5f;
            if (IsRoad(cx, cz)) { Put("Environments/SM_Env_Road_01", new Vector3(x + 5, RoadY, z + 5), 0, ground); continue; }
            // Pave everywhere; under your restaurant the paving sits 2 cm lower so its own floor wins.
            float y = Reserved[1].Overlaps(new Rect(x + .5f, z + .5f, 4, 4)) ? WalkY - .02f : WalkY;
            bool n = IsRoad(cx, cz + 5), s = IsRoad(cx, cz - 5), e = IsRoad(cx + 5, cz), w = IsRoad(cx - 5, cz);
            int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);
            if (count == 1) { int facing = n ? 0 : e ? 1 : s ? 2 : 3; Module("Environments/SM_Env_Sidewalk_Straight_01", x, z, facing, y, ground); }
            else if (count >= 2) Module("Environments/SM_Env_Sidewalk_Corner_01", x, z, n && e ? 0 : e && s ? 1 : s && w ? 2 : 3, y, ground);
            else Module("Environments/SM_Env_Sidewalk_01", x, z, 0, y, ground);
        }
        // Real facades for Milo's and the apartments next to the stand (inside the gameplay strip).
        foreach (var x0 in new[] { -18f, -13f }) ForceTower(x0, 13.5f, 2, 1, true);
        foreach (var x0 in new[] { -5f, 0f }) ForceTower(x0, 15f, 2, 2, false);
        // Main Street's north side (outside the gameplay strip) and south side shop rows.
        Row(-35, -20, 13.5f, 2, 1, 2, true);   Row(25, 35, 10, 2, 1, 2, true);
        Row(-35, 35, -16.5f, 0, 1, 2, true);
        Row(-80, -45, 10, 2, 1, 3, true);      Row(45, 80, 10, 2, 1, 3, true);
        Row(-80, -45, -15, 0, 1, 3, true);     Row(45, 80, -15, 0, 1, 3, true);
        // Block backs facing North / South Avenues, and side rows on West / East Streets.
        Row(-35, 35, 35, 0, 1, 2, false);      Row(-35, 35, -40, 2, 1, 2, false);
        Row(20, 35, -35, 3, 1, 2, false);      Row(20, 35, 30, 1, 1, 2, false);
        Row(-30, -20, -35, 3, 1, 2, false);    Row(15, 30, -30, 3, 0, 1, false);
        // Outer ring: tall apartment rows along the avenues.
        Row(-80, 80, 60, 2, 2, 3, false);      Row(-80, 80, -65, 0, 2, 3, false);
        Row(-80, -45, 40, 0, 1, 2, false);     Row(45, 80, 40, 0, 1, 2, false);
        Row(-80, -45, -45 + 5, 2, 1, 2, false); Row(45, 80, -45 + 5, 2, 1, 2, false);
        // Landmarks in the outer blocks.
        Put("Buildings/SM_Bld_CityHall_01", new Vector3(-62, 0, 26), 180);
        Put("Buildings/SM_Bld_OfficeRound_01", new Vector3(-62, 0, -28), 0);
        Put("Buildings/SM_Bld_OfficeSquare_01", new Vector3(70, 0, 34), 0);
        Put("Buildings/SM_Bld_OfficeOld_Large_01", new Vector3(70, 0, -18), 0);
        Put("Buildings/SM_Bld_OfficeOctagon_01", new Vector3(-62, 0, 72), 0);
        Put("Buildings/SM_Bld_OfficeOld_Small_01", new Vector3(20, 0, 76), 0);
        Put("Buildings/SM_Bld_Station_01", new Vector3(0, 0, -30), 0);
        Put("Environments/Custom/SM_Env_Skyline_01", Vector3.zero, 0);
        // Street life: parked cars, trees, benches, hydrants, a hotdog cart, bus stop, rooftop signs.
        string[] cars = { "SM_Veh_Car_Sedan_01", "SM_Veh_Car_Taxi_01", "SM_Veh_Car_Van_01", "SM_Veh_Car_Small_01", "SM_Veh_Car_Medium_01", "SM_Veh_Car_Muscle_01" };
        int c = 0;
        foreach (float x in new[] { -75f, -62, -52, 28, 55, 68 }) Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, x > 0 ? 3.3f : -3.3f), x > 0 ? 90 : 270);
        foreach (float z in new[] { -70f, -30, 20, 70 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(-43.3f, RoadY, z), 0); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(43.3f, RoadY, z + 8), 180); }
        foreach (float x in new[] { -70f, -30, 10, 50 }) { Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x, RoadY, 46.7f), 90); Put("Vehicles/" + cars[c++ % cars.Length], new Vector3(x + 12, RoadY, -46.7f), 270); }
        int t = 0;
        for (float x = -75; x <= 75; x += 15) {
            if (Mathf.Abs(x) < 30 || Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue;
            Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(x, 0, 7.5f), 0); Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(x + 5, 0, -7.5f), 0);
        }
        for (float x = -75; x <= 75; x += 20) { if (Mathf.Abs(Mathf.Abs(x) - 40) < 6) continue; Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(x, 0, 57.5f), 0); Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(x, 0, -57.5f), 0); }
        for (float z = -75; z <= 75; z += 20) { if (Mathf.Abs(z) < 12 || Mathf.Abs(Mathf.Abs(z) - 50) < 6) continue; Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(-32.5f, 0, z), 0); Put("Environments/SM_Env_Tree_0" + (t++ % 3 + 1), new Vector3(32.5f, 0, z), 0); }
        Put("Props/SM_Prop_HotdogStand_01", new Vector3(30, 0, 8), 180);
        Put("Props/SM_Prop_BusStop_01", new Vector3(-30, 0, -7.2f), 0);
        foreach (float x in new[] { -28f, 27, -55, 60 }) { Put("Props/SM_Prop_ParkBench_01", new Vector3(x, 0, x > 0 ? 8.6f : -8.6f), x > 0 ? 180 : 0); }
        foreach (float x in new[] { -34f, 34, -48, 48 }) Put("Props/SM_Prop_Hydrant_01", new Vector3(x, 0, x > 0 ? 6 : -6), 0);
        foreach (float x in new[] { -26f, 26, -60, 60 }) Put("Props/SM_Prop_Trashbin_01", new Vector3(x, 0, -6.2f), 0);
        foreach (float x in new[] { -60f, -20, 20, 60 }) { Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, 44.2f), 180); Put("Props/SM_Prop_LightPole_Base_01", new Vector3(x, 0, -44.2f), 0); }
        Put("Props/SM_Prop_LargeSign_Burger_01", new Vector3(-27.5f, 17, 16), 0);
        Put("Props/SM_Prop_Billboard_Roof_01", new Vector3(5, 12.8f, -14), 180);
        // Invisible edge of the playable city.
        foreach (var (pos, size) in new[] { (new Vector3(0, 5, Half + 1), new Vector3(2 * Half, 10, 1)), (new Vector3(0, 5, -Half - 1), new Vector3(2 * Half, 10, 1)), (new Vector3(Half + 1, 5, 0), new Vector3(1, 10, 2 * Half)), (new Vector3(-Half - 1, 5, 0), new Vector3(1, 10, 2 * Half)) }) {
            var wall = new GameObject("City edge"); wall.transform.SetParent(root, false); wall.transform.position = pos; wall.AddComponent<BoxCollider>().size = size;
        }
    }
}
