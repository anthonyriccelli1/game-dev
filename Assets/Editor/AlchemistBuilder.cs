using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RestaurantCity;

// The Alchemist: Old Market's two-star rival, a mad-scientist lab diner on Main Street (the old Gilded Orbit lot plus
// the old alley). Built entirely from art-pack pieces on the Shops pack's 2.5 m building grid, no code-built shapes:
//   hall    x 5..25, z 14..31.5, two storeys (6 m), a dining room open to the roof
//   loft    a lab balcony across the back (z 26.5..31.5) at first-floor height, reached by stairs on the east wall
//   kitchen open, under the balcony, behind a long counter
//   yard    a fenced courtyard behind the hall to North Avenue (z 31.5..40), empty for now (future worker rest)
// Gameplay hooks (staff, customers, raid) are runtime; named anchors here tell them where things are.
public static class AlchemistBuilder {
    public const float X0 = 5, X1 = 25, Z0 = 14, Z1 = 31.5f, Storey = 3.01f, Loft = 26.5f, YardZ = 40, DoorX = 16.25f;
    const string Shops = "Assets/Synty/PolygonShops/Prefabs/", City = "Assets/Synty/PolygonCity/Prefabs/", Gen = "Assets/Synty/PolygonGeneric/Prefabs/";
    static Transform root, shell, inside, loft, yard, colliders;
    static Material brickOut, brickIn, floorMat, roofMat, ceilingMat;

    public static void Build(Transform world) {
        root = new GameObject("Rival restaurant / The Alchemist").transform; root.SetParent(world, false);
        shell = Group("Shell"); inside = Group("Dining room"); loft = Group("Lab balcony"); yard = Group("Courtyard"); colliders = Group("Colliders");
        brickOut = Mat("Base_Brick_Red_01"); brickIn = brickOut; floorMat = Mat("Base_Tile_01"); roofMat = Mat("Concrete_01"); ceilingMat = Mat("Concrete_01");
        Shell(); Balcony(); Sign(); Dining(); Kitchen(); Lab(); Atmosphere(); Courtyard();
        Anchor("Door", new Vector3(DoorX, 0, Z0 - .6f)); Anchor("Pass", new Vector3(14.5f, 0, 25.6f)); Anchor("Head cook", new Vector3(14.5f, 0, 28.6f));
        Anchor("Raid centre", new Vector3(15, 0, 20));
    }

    // ---------- shell ----------
    static void Shell() {
        // Floor tiles, ceiling under the roof, and a flat roof on top.
        for (int i = 0; i < 8; i++) for (int j = 0; j < 7; j++) {
            float px = X0 + 2.5f * (i + 1), pz = Z0 + 2.5f * j;
            Skin(S("Buildings/SM_Bld_Base_Floor_01", new Vector3(px, .012f, pz), 0, shell), floorMat);
            Skin(S("Buildings/SM_Bld_Base_Ceiling_01", new Vector3(px, 2 * Storey - .01f, pz), 0, shell), ceilingMat);
            Skin(S("Buildings/SM_Bld_Base_Floor_01", new Vector3(px, 2 * Storey + .03f, pz), 0, shell), roofMat);
        }
        // Street front: factory windows either side of a wide open double doorway (x 15..17.5).
        Front("Buildings/SM_Bld_ShopFront_03", 0, 0); Front("Buildings/SM_Bld_ShopFront_03", 2, 0);
        Front("Buildings/SM_Bld_Base_Wall_Door_Double_Large_01", 4, 0); Front("Buildings/SM_Bld_ShopFront_06", 5, 0); Front("Buildings/SM_Bld_ShopFront_03", 6, 0);
        for (int k = 0; k < 8; k++) Front("Buildings/SM_Bld_Base_Wall_Window_Double_01", k, Storey);
        // Back: a door out to the courtyard; lab windows upstairs.
        for (int k = 0; k < 8; k++) { Back(k == 3 ? "Buildings/SM_Bld_Base_Wall_Door_Double_01" : "Buildings/SM_Bld_Base_Wall_01", k, 0); Back("Buildings/SM_Bld_Base_Wall_Window_01", k, Storey); }
        // Sides: solid brick downstairs; high windows upstairs on the west.
        for (int k = 0; k < 7; k++) {
            Side(false, "Buildings/SM_Bld_Base_Wall_01", k, 0); Side(false, k % 2 == 0 ? "Buildings/SM_Bld_Base_Wall_Window_01" : "Buildings/SM_Bld_Base_Wall_01", k, Storey);
            Side(true, "Buildings/SM_Bld_Base_Wall_01", k, 0); Side(true, "Buildings/SM_Bld_Base_Wall_01", k, Storey);
        }
        // Corner pillars and a roof edge all round.
        foreach (var c in new[] { new Vector3(X0, 0, Z0), new Vector3(X1, 0, Z0), new Vector3(X0, 0, Z1), new Vector3(X1, 0, Z1) })
            for (int y = 0; y < 2; y++) Skin(S("Buildings/SM_Bld_Base_Pillar_01", c + Vector3.up * y * Storey, 0, shell), brickOut);
        for (int k = 0; k < 8; k++) { Edge(new Vector3(X0 + 2.5f * (k + 1), 2 * Storey, Z0), 0); Edge(new Vector3(X0 + 2.5f * k, 2 * Storey, Z1), 180); }
        for (int k = 0; k < 7; k++) { Edge(new Vector3(X0, 2 * Storey, Z0 + 2.5f * k), 90); Edge(new Vector3(X1, 2 * Storey, Z0 + 2.5f * (k + 1)), 270); }
        // Roof clutter: a water tower and vents, so the silhouette reads as an old works building.
        P(City + "Buildings/SM_Prop_Water_Tower_01", new Vector3(21.5f, 2 * Storey + .03f, 28.5f), 20, shell);
        P(City + "Props/SM_Prop_Roof_Aircon_02", new Vector3(8.5f, 2 * Storey + .03f, 27.5f), 0, shell);
        P(City + "Props/SM_Prop_Roof_Aircon_03", new Vector3(11.5f, 2 * Storey + .03f, 29.5f), 90, shell);
        // Collision: walls (door gaps open), balcony, stairs.
        Box("Front wall west", new Vector3((X0 + 15) / 2, 3, Z0), new Vector3(15 - X0, 6, .3f));
        Box("Front wall east", new Vector3((17.5f + X1) / 2, 3, Z0), new Vector3(X1 - 17.5f, 6, .3f));
        Box("Front wall over door", new Vector3(DoorX, 4.5f, Z0), new Vector3(2.5f, 3, .3f));
        Box("Back wall west", new Vector3((X0 + 12.5f) / 2, 3, Z1), new Vector3(12.5f - X0, 6, .3f));
        Box("Back wall east", new Vector3((15 + X1) / 2, 3, Z1), new Vector3(X1 - 15, 6, .3f));
        Box("Back wall over door", new Vector3(13.75f, 4.5f, Z1), new Vector3(2.5f, 3, .3f));
        Box("West wall", new Vector3(X0, 3, (Z0 + Z1) / 2), new Vector3(.3f, 6, Z1 - Z0));
        Box("East wall", new Vector3(X1, 3, (Z0 + Z1) / 2), new Vector3(.3f, 6, Z1 - Z0));
        Box("Roof", new Vector3((X0 + X1) / 2, 2 * Storey + .1f, (Z0 + Z1) / 2), new Vector3(X1 - X0, .2f, Z1 - Z0));
    }
    static void Front(string rel, int k, float y) => Wall(S(rel, new Vector3(X0 + 2.5f * (k + (rel.Contains("ShopFront_03") ? 2 : 1)), y, Z0), 0, shell));
    static void Back(string rel, int k, float y) => Wall(S(rel, new Vector3(X0 + 2.5f * k, y, Z1), 180, shell));
    static void Side(bool east, string rel, int k, float y) => Wall(east ? S(rel, new Vector3(X1, y, Z0 + 2.5f * (k + 1)), 270, shell) : S(rel, new Vector3(X0, y, Z0 + 2.5f * k), 90, shell));
    static void Edge(Vector3 at, float yaw) => Skin(S("Buildings/SM_Bld_Roof_Edge_01", at, yaw, shell), roofMat);

    // ---------- the lab balcony ----------
    static void Balcony() {
        for (int i = 0; i < 8; i++) for (int j = 5; j < 7; j++) S("Buildings/SM_Bld_Balcony_Floor_01", new Vector3(X0 + 2.5f * (i + 1), Storey, Z0 + 2.5f * j), 0, loft);
        for (int k = 0; k < 7; k++) S("Buildings/SM_Bld_Balcony_Railing_01", new Vector3(X0 + 2.5f * (k + 1), Storey, Loft), 0, loft);
        // Two flights up the east wall (x 22.5..25), z 21.5 -> 26.5.
        S("Buildings/SM_Bld_Base_Stairs_01", new Vector3(X1, 0, 24), 0, loft);
        S("Buildings/SM_Bld_Base_Stairs_01", new Vector3(X1, 1.5f, 26.5f), 0, loft);
        Box("Balcony floor", new Vector3((X0 + X1) / 2, Storey - .1f, (Loft + Z1) / 2), new Vector3(X1 - X0, .2f, Z1 - Loft));
        Box("Balcony rail", new Vector3((X0 + 22.5f) / 2, Storey + .55f, Loft), new Vector3(22.5f - X0, 1.1f, .12f));
        var ramp = Box("Stairs ramp", new Vector3(23.75f, Storey / 2, 24), new Vector3(2.5f, .2f, Mathf.Sqrt(25 + Storey * Storey)));
        ramp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(Storey, 5) * Mathf.Rad2Deg, 0, 0);
    }

    // ---------- sign ----------
    static void Sign() {
        var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonShops/Materials/PolygonShops_Mat_01_A.mat");
        const string glowPath = "Assets/Generated/Alchemist_SignGlow.mat";
        var glow = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
        if (!glow) { glow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Alchemist sign glow" }; AssetDatabase.CreateAsset(glow, glowPath); }
        glow.SetColor("_BaseColor", new Color(.2f, .95f, .4f)); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.2f, .95f, .4f) * 2.2f); EditorUtility.SetDirty(glow);
        CityMap.Letters3D("THE ALCHEMIST", new Vector3(DoorX - 1.25f, Storey * 2 - .95f, Z0 - .25f), 180, .62f, 11, glow, glow, shell);
        var bulb = new GameObject("Sign glow").AddComponent<Light>(); bulb.transform.SetParent(shell, false); bulb.transform.position = new Vector3(DoorX - 1.25f, 5.1f, Z0 - 1.4f);
        bulb.type = LightType.Point; bulb.color = new Color(.3f, 1f, .5f); bulb.range = 7; bulb.intensity = 1.6f; bulb.shadows = LightShadows.None;
    }

    // ---------- dining room ----------
    static void Dining() {
        // Booths down the west wall, a second row east of the door, small tables between.
        foreach (float z in new[] { 16.4f, 19.6f, 22.8f }) { Solid(S("Props/SM_Prop_Cafe_Booth_Wall_Seat_Double_01", new Vector3(6.05f, 0, z), 90, inside)); }
        foreach (float z in new[] { 16.4f, 19.6f }) { Solid(S("Props/SM_Prop_Cafe_Booth_Wall_Seat_Double_01", new Vector3(23.95f, 0, z), 270, inside)); }
        foreach (var (x, z) in new[] { (10.5f, 17.2f), (10.5f, 21.2f), (20.2f, 17.4f), (20.2f, 21.4f) }) {
            Solid(S("Props/SM_Prop_Cafe_Table_Small_01", new Vector3(x, 0, z), 0, inside));
            S("Props/SM_Prop_Cafe_Chair_01", new Vector3(x - .8f, 0, z), 90, inside); S("Props/SM_Prop_Cafe_Chair_01", new Vector3(x + .8f, 0, z), 270, inside);
        }
        // The long counter in front of the open kitchen, stools facing it.
        foreach (float x in new[] { 8.7f, 11.6f, 17.4f, 20.3f }) Solid(S("Props/SM_Prop_Bar_Bench_01", new Vector3(x, 0, 25.4f), 180, inside));
        Solid(S("Props/SM_Prop_Bar_Bench_02", new Vector3(14.5f, 0, 25.4f), 180, inside));   // the pass, where staff pick up plates
        for (float x = 7.6f; x < 21.6f; x += 1.2f) if (Mathf.Abs(x - 14.5f) > .9f) S("Props/SM_Prop_Bar_Stool_01", new Vector3(x, 0, 24.4f), 0, inside);
        // Specimen tanks: the Flux canister grown to 2.4 m, glowing in the front corners (seen from the street) and
        // flanking the kitchen.
        foreach (var t in new[] { new Vector3(6.2f, 0, 15.2f), new Vector3(23.8f, 0, 15.2f), new Vector3(6.4f, 0, 25.6f), new Vector3(22, 0, 27.6f) }) Tank(t, 2.4f, inside);
    }

    // ---------- open kitchen under the balcony ----------
    static void Kitchen() {
        var k = Group("Open kitchen", inside);
        Solid(S("Props/SM_Prop_Kitchen_Grill_01", new Vector3(10.5f, 0, 30.6f), 180, k)); Solid(S("Props/SM_Prop_Kitchen_Stove_Oven_01", new Vector3(12.4f, 0, 30.6f), 180, k));
        Solid(S("Props/SM_Prop_Kitchen_Deep_Fryer_01", new Vector3(16.6f, 0, 30.8f), 180, k)); Solid(S("Props/SM_Prop_Kitchen_Prep_Table_01", new Vector3(18.6f, 0, 30.6f), 180, k));
        foreach (float x in new[] { 10.5f, 12.4f }) S("Props/SM_Prop_Kitchen_Extractor_01", new Vector3(x, 2.05f, 31.4f), 180, k);
        Solid(S("Props/SM_Prop_Kitchen_Prep_Table_01", new Vector3(14.5f, 0, 28.2f), 0, k));   // the head cook's bench, facing the room
        S("Props/SM_Prop_Kitchen_ServingShelf_01", new Vector3(14.5f, 1.12f, 25.4f), 0, k);
        S("Props/SM_Prop_Kitchen_Menu_Screen_01", new Vector3(14.5f, 2.35f, 26.35f), 180, k);
        S("Props/SM_Prop_Kitchen_Pot_01", new Vector3(12.2f, 1.22f, 30.5f), 0, k); S("Props/SM_Prop_Kitchen_Pan_01", new Vector3(10.2f, 1.35f, 30.5f), 40, k);
        Solid(S("Props/SM_Prop_Bar_Shelf_01", new Vector3(7.6f, 0, 31.1f), 180, k));   // potion shelf by the back door
        foreach (var (x, y, n) in new[] { (6.6f, 1.05f, 1), (7.0f, 1.05f, 3), (7.5f, 1.05f, 6), (8.1f, 1.05f, 2), (8.6f, 1.05f, 7), (6.9f, 1.66f, 4), (7.7f, 1.66f, 8), (8.4f, 1.66f, 1) })
            S("Props/SM_Prop_Bar_Bottle_0" + n, new Vector3(x, y, 31.05f), x * 50, k);
        P(Gen + "Props/SM_Gen_Prop_Skull_01", new Vector3(7.3f, 2.3f, 31.05f), 160, k);
    }

    // ---------- the lab on the balcony ----------
    static void Lab() {
        float y = Storey;
        Solid(S("Props/SM_Prop_Cafe_Table_Large_01", new Vector3(9, y, 29.4f), 0, loft)); Solid(S("Props/SM_Prop_Cafe_Table_Large_01", new Vector3(19.5f, y, 29.4f), 0, loft));
        foreach (var (x, n) in new[] { (8.4f, 1), (8.8f, 3), (9.3f, 6), (9.6f, 4), (19f, 7), (19.4f, 2), (19.9f, 8) }) S("Props/SM_Prop_Bar_Bottle_0" + n, new Vector3(x, y + 1.1f, 29.3f + (n % 2) * .2f), n * 40, loft);
        S("Props/SM_Prop_Computer_Monitor_01", new Vector3(20.2f, y + 1.1f, 29.7f), 180, loft); S("Props/SM_Prop_Computer_Monitor_03", new Vector3(8.2f, y + 1.1f, 29.7f), 180, loft);
        P(Gen + "Props/SM_Gen_Prop_Skull_01", new Vector3(9.8f, y + 1.1f, 29.1f), 200, loft);
        foreach (var t in new[] { new Vector3(6.3f, y, 30.6f), new Vector3(12, y, 30.9f), new Vector3(17, y, 30.9f) }) Tank(t, 1.9f, loft);
        foreach (var b in new[] { new Vector3(11.2f, y, 28.2f), new Vector3(11.9f, y, 28.6f) }) Solid(P(Gen + "Props/SM_Gen_Prop_Barrel_Metal_01", b, b.x * 60, loft));
        Anchor("Tesla coil", new Vector3(14.5f, y, 29.4f));   // reserved for the rendered Tesla coil
    }

    // ---------- pipes, chains, lights ----------
    static void Atmosphere() {
        var a = Group("Pipes and lights");
        for (int k = 0; k < 7; k++) foreach (float x in new[] { 7.2f, 22.8f }) S("Props/SM_Prop_BigPipe_02", new Vector3(x, 5.45f, Z0 + .2f + 2.5f * k), 0, a);
        foreach (var (x, z) in new[] { (9f, 17f), (15f, 17f), (21f, 17f), (9f, 21.5f), (15f, 21.5f), (21f, 21.5f) }) S("Props/SM_Prop_Lighting_Ceiling_Cage_01", new Vector3(x, 2 * Storey - .02f, z), 0, a);
        foreach (var (x, z) in new[] { (7.5f, 26.1f), (10.5f, 26.3f), (18.5f, 26.3f), (21.5f, 26.1f) }) P(Gen + "Props/SM_Gen_Prop_Chain_01", new Vector3(x, 2 * Storey - .02f, z), x * 30, a);
        for (float x = 8; x < 21.5f; x += 1.5f) S("Props/SM_Prop_Lighting_Cable_Bulb_01", new Vector3(x, Storey - .5f, 26.2f), 0, a);
        // Light: warm pools over the tables, green from the tanks and the lab.
        foreach (var (x, z) in new[] { (9f, 19f), (15f, 19f), (21f, 19f) }) Lamp(new Vector3(x, 4.6f, z), new Color(1f, .74f, .45f), 1f, 7, a);
        foreach (var p in new[] { new Vector3(6.5f, 2.2f, 16), new Vector3(23.5f, 2.2f, 16), new Vector3(14.5f, 2.2f, 28.5f), new Vector3(14.5f, 4.8f, 29.5f) }) Lamp(p, new Color(.3f, 1f, .45f), 1.4f, 7, a);
    }

    // ---------- the courtyard ----------
    static void Courtyard() {
        // Grass behind the hall, ringed by tall iron fencing, a gate onto North Avenue. Empty: future rest chambers.
        var grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Grass.mat") ?? Mat("Cobblestone_01");
        for (int i = 0; i < 8; i++) for (int j = 0; j < 4; j++) {
            float pz = Z1 + 2.5f * j; if (pz >= YardZ) continue;
            Skin(S("Buildings/SM_Bld_Base_Floor_01", new Vector3(X0 + 2.5f * (i + 1), -.03f, pz), 0, yard), grass);
        }
        for (int k = 0; k < 8; k++) { if (k == 3 || k == 4) continue; S("Buildings/SM_Bld_Metal_Fence_01", new Vector3(X0 + 2.5f * (k + 1), 0, YardZ - .1f), 0, yard); }
        for (int k = 0; k < 4; k++) { float z0 = Z1 + 2.5f * k; if (z0 >= YardZ - .1f) continue; S("Buildings/SM_Bld_Metal_Fence_01", new Vector3(X0, 0, z0), 90, yard); S("Buildings/SM_Bld_Metal_Fence_01", new Vector3(X1, 0, z0 + 2.5f), 270, yard); }
        for (int k = 0; k <= 8; k++) S("Buildings/SM_Bld_Metal_Fence_Post_01", new Vector3(X0 + 2.5f * k, 0, YardZ - .1f), 0, yard);
        Box("Yard fence north west", new Vector3((X0 + 12.5f) / 2, 1.4f, YardZ - .1f), new Vector3(12.5f - X0, 2.8f, .15f));
        Box("Yard fence north east", new Vector3((17.5f + X1) / 2, 1.4f, YardZ - .1f), new Vector3(X1 - 17.5f, 2.8f, .15f));
        Box("Yard fence west", new Vector3(X0, 1.4f, (Z1 + YardZ) / 2), new Vector3(.15f, 2.8f, YardZ - Z1));
        Box("Yard fence east", new Vector3(X1, 1.4f, (Z1 + YardZ) / 2), new Vector3(.15f, 2.8f, YardZ - Z1));
        Anchor("Courtyard", new Vector3(15, 0, 35.5f));
    }

    // ---------- helpers ----------
    static Transform Group(string name, Transform parent = null) { var t = new GameObject(name).transform; t.SetParent(parent ? parent : root, false); return t; }
    static void Anchor(string name, Vector3 at) { var t = Group("Anchor: " + name); t.position = at; }
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonShops/Materials/Building_Mats/PolygonShops_Mat_" + name + ".mat");
    static GameObject S(string rel, Vector3 pos, float yaw, Transform parent) => P(Shops + rel, pos, yaw, parent);
    static GameObject P(string path, Vector3 pos, float yaw, Transform parent) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab"); if (!src) { Debug.LogWarning("Alchemist: missing " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return go;
    }
    static bool Surface(Material m) => m && AssetDatabase.GetAssetPath(m).Contains("/Building_Mats/");
    // Re-skin every Shops building surface (brick, tile, ceiling...) on a piece with one material.
    static GameObject Skin(GameObject go, Material m) {
        if (!go || !m) return go;
        foreach (var r in go.GetComponentsInChildren<Renderer>()) {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (Surface(mats[i])) mats[i] = m;
            r.sharedMaterials = mats;
        }
        return go;
    }
    // Wall pieces carry two surface slots: the street side and the room side (both old red brick for now).
    static GameObject Wall(GameObject go) {
        if (!go) return go;
        foreach (var r in go.GetComponentsInChildren<Renderer>()) {
            var mats = r.sharedMaterials; int seen = 0;
            for (int i = 0; i < mats.Length; i++) if (Surface(mats[i])) mats[i] = seen++ == 0 ? brickOut : brickIn;
            r.sharedMaterials = mats;
        }
        return go;
    }
    static GameObject Box(string name, Vector3 centre, Vector3 size) {
        var go = new GameObject(name); go.transform.SetParent(colliders, false); go.transform.position = centre;
        go.AddComponent<BoxCollider>().size = size; return go;
    }
    // A box collider around a piece of furniture so players and NPCs don't walk through it.
    static void Solid(GameObject go) {
        if (!go) return; var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var c = go.AddComponent<BoxCollider>(); c.center = go.transform.InverseTransformPoint(b.center);
        var s = go.transform.InverseTransformVector(b.size); c.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }
    static void Tank(Vector3 at, float height, Transform parent) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Flux/FluxVial.prefab"); if (!src) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent); go.name = "Specimen tank";
        go.transform.position = at; go.transform.localScale = Vector3.one * (height / FluxVial.ModelHeight);
        go.AddComponent<FluxVial>().Bob = false;
        var c = go.AddComponent<CapsuleCollider>(); c.center = new Vector3(0, .5f, 0); c.height = 1; c.radius = .2f;
    }
    static void Lamp(Vector3 at, Color color, float intensity, float range, Transform parent) {
        var l = new GameObject("Lamp").AddComponent<Light>(); l.transform.SetParent(parent, false); l.transform.position = at;
        l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
    }
}
