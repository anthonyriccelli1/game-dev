using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Main Street, brought up to the standard of The Alchemist and The Odd Table: every ground-floor shop on the street is
// a real place you can look into (its own name in 3D letters, a lit room behind the glass furnished from the Shops
// pack), the two slim units beside your restaurant are shuttered and to let, and the sidewalks follow one rule: trees
// in planted pits at the curb, lamps and bins in the same strip, a clear walking lane against the shopfronts.
public static partial class CityMap {
    const string ShopProps = "Assets/Synty/PolygonShops/Prefabs/Props/SM_Prop_";
    // The Main Street shopfront cells (x0 of the 5 m cell; z0 = 10 faces south onto the street, z0 = -15 faces north).
    static readonly Dictionary<(int x, int z), string> MainShops = new Dictionary<(int, int), string> {
        { (-25, 10), "records" }, { (-25, -15), "barber" }, { (0, -15), "florist" }, { (5, -15), "gadgets" },
        { (10, -15), "arcade" }, { (15, -15), "threads" }, { (20, -15), "gym" },
    };
    public static bool IsMainStreetShop(float x0, float z0, int facing) =>
        (facing == 2 && Mathf.Approximately(z0, 10) || facing == 0 && Mathf.Approximately(z0, -15)) && MainShops.ContainsKey(((int)x0, (int)z0));

    // Each Main Street shop gets a glass-fronted City module, chosen per shop so neighbours differ.
    static readonly Dictionary<string, string> ThemeGround = new Dictionary<string, string> {
        { "records", "01" }, { "barber", "05" }, { "florist", "05" }, { "gadgets", "01" }, { "arcade", "02" }, { "threads", "01" }, { "gym", "05" } };
    static string MainShopGround(float x0, float z0) => "Buildings/SM_Bld_Shop_" + ThemeGround[MainShops[((int)x0, (int)z0)]];
    class ShopTheme {
        public string Name, Wall, Floor, Sign; public Color Light; public bool Neon;
        public (string prop, float lat, float depth, float y, float yaw)[] Items;
    }
    // lat: across the shop (+ is the shopper's left looking in); depth: metres in from the street edge of the cell;
    // yaw: relative to facing the street (180 faces the back wall; -90 hangs on the +lat side wall).
    static readonly Dictionary<string, ShopTheme> Themes = new Dictionary<string, ShopTheme> {
        { "barber", new ShopTheme { Name = "SHARP CUTS", Wall = "E9E2D3", Floor = "34302C", Sign = "E04A3A", Light = new Color(1f, .9f, .78f), Items = new[] {
            ("Barber_Mirror_01", 1.2f, 4.83f, 0f, 0f), ("Barber_Mirror_01", -1.2f, 4.83f, 0f, 0f),
            ("Barber_Chair_01", 1.2f, 3.2f, 0f, 180f), ("Barber_Chair_01", -1.2f, 3.2f, 0f, 180f),
            ("Barber_Shelf_01", 2.4f, 2.2f, .9f, -90f), ("Barber_Salon_Hair_Dryer_01", -1.9f, 1.4f, 0f, 30f),
            ("Barber_Poles_01", 2.2f, -.46f, 1.5f, 0f) } } },
        { "records", new ShopTheme { Name = "SPIN RECORDS", Neon = true, Wall = "2E2A3A", Floor = "6B4E3A", Sign = "B07CFF", Light = new Color(1f, .78f, .6f), Items = new[] {
            ("Shop_Table_01", 0f, 2.3f, 0f, 0f), ("Music_Turntable_01", .25f, 2.4f, 1.05f, 0f), ("Music_Record_01", -.4f, 2.2f, 1.05f, 25f),
            ("Music_Amplifier_01", -1.7f, 4.4f, 0f, 0f), ("Music_Drums_Kit_01", 1.15f, 4.0f, 0f, 0f),
            ("Music_Framed_Record_01", -1.5f, 4.83f, 1.9f, 0f), ("Music_Framed_Record_01", -.5f, 4.83f, 1.9f, 0f),
            ("Music_Framed_Record_01", 2.38f, 2.4f, 1.8f, -90f), ("Music_Guitar_Stand_01", -1.8f, 1.2f, 0f, 0f) } } },
        { "florist", new ShopTheme { Name = "BLOOM", Wall = "F3EEE4", Floor = "7E9B76", Sign = "FF6FA3", Light = new Color(1f, .93f, .82f), Items = new[] {
            ("Flower_Stand_Preset_01", 0f, 1.45f, 0f, 0f), ("Flower_Stand_Preset_02", 0f, 4.75f, 0f, 0f),
            ("Flower_Bucket_01", -1.8f, 2.4f, 0f, 0f), ("Flower_Bucket_02", -1.5f, 2.9f, 0f, 40f), ("Flower_Bucket_01", 1.7f, 2.5f, 0f, 70f),
            ("Flower_Bucket_02", 1.9f, 3.2f, 0f, 10f), ("Flower_Bucket_Small_01", -1.9f, 3.6f, 0f, 0f),
            ("Flower_Bucket_01", 2.0f, -.95f, 0f, 0f), ("Flower_Bucket_02", 1.55f, -.9f, 0f, 30f) } } },
        { "gadgets", new ShopTheme { Name = "GADGET HUB", Wall = "DDE3E8", Floor = "3A4048", Sign = "3D8BFF", Light = new Color(.86f, .93f, 1f), Items = new[] {
            ("Computer_TV_Wall_01", -1.1f, 4.83f, 1.7f, 0f), ("Computer_TV_Wall_01", 1.1f, 4.83f, 1.7f, 0f),
            ("Shop_Table_01", 0f, 2.4f, 0f, 0f), ("Computer_Laptop_01", -.35f, 2.4f, 1.05f, 0f), ("Computer_Monitor_01", .4f, 2.55f, 1.05f, 0f),
            ("Computer_Speaker_Bluetooth_01", .1f, 2.1f, 1.05f, 20f), ("Computer_Gaming_Console_01", -1.9f, 1.3f, 0f, 0f) } } },
        { "arcade", new ShopTheme { Name = "PIXEL ARCADE", Neon = true, Wall = "1B1530", Floor = "2A2245", Sign = "33E0FF", Light = new Color(.75f, .6f, 1f), Items = new[] {
            ("Arcade_Machine_Space_01", -1.65f, 4.3f, 0f, 0f), ("Arcade_Machine_Zombie_01", -.55f, 4.3f, 0f, 0f),
            ("Arcade_Machine_Snow_01", .55f, 4.3f, 0f, 0f), ("Arcade_Machine_Basic_01", 1.65f, 4.3f, 0f, 0f),
            ("Arcade_Claw_01", -1.55f, 1.6f, 0f, 0f), ("Arcade_Claw_01", 1.55f, 1.6f, 0f, 0f) } } },
        { "threads", new ShopTheme { Name = "THREADS", Wall = "F1E9DD", Floor = "B9A68F", Sign = "2FB7A8", Light = new Color(1f, .92f, .8f), Items = new[] {
            ("Mannequin_Female_01", -1.2f, 1.05f, 0f, 0f), ("Mannequin_Male_01", 1.2f, 1.05f, 0f, 0f),
            ("Clothes_Rack_01", 0f, 2.9f, 0f, 0f), ("Clothes_Shelf_01", -1.3f, 4.62f, 0f, 0f), ("Clothes_Shelf_01", 1.3f, 4.62f, 0f, 0f) } } },
        { "gym", new ShopTheme { Name = "IRON GYM", Wall = "3A3A3A", Floor = "1F1F1F", Sign = "FF7A1A", Light = new Color(1f, .86f, .7f), Items = new[] {
            ("Gym_Tredmill_01", -1.2f, 2.0f, 0f, 0f), ("Gym_Tredmill_01", 1.2f, 2.0f, 0f, 0f),
            ("Gym_Dumbbell_Rack_01", 0f, 4.45f, 0f, 0f), ("Gym_Boxing_Bag_01", 1.7f, 3.8f, 2.9f, 0f), ("Gym_Swiss_Ball_01", -1.9f, 4.0f, 0f, 0f) } } },
    };

    // A Main Street shop: the City module stays (it matches the floors above), its painted-on fake interior comes out,
    // and a real room goes in behind the glass.
    static void MainStreetShop(Transform b, float x0, float z0, int facing) {
        var theme = Themes[MainShops[((int)x0, (int)z0)]];
        OpenShopfront(b);
        float yaw = 90 * facing;
        Vector3 At(float lat, float depth, float y) => OnFace(x0, z0, facing, lat, -depth, y);
        var room = new GameObject("Shop interior / " + theme.Name).transform; room.SetParent(b, false);
        Material wall = InteriorMat("Shop" + theme.Wall, theme.Wall), floor = InteriorMat("ShopFloor" + theme.Floor, theme.Floor);
        bool alongX = facing % 2 == 0;
        Vector3 Size(float lateral, float y, float depth) => alongX ? new Vector3(lateral, y, depth) : new Vector3(depth, y, lateral);
        Slab("Shop floor", At(0, 2.62f, .03f), Size(4.84f, .06f, 4.46f), floor, room, false);
        Slab("Shop ceiling", At(0, 2.62f, 2.93f), Size(4.84f, .06f, 4.46f), wall, room, false);
        Slab("Shop back wall", At(0, 4.86f, 1.48f), Size(4.84f, 2.96f, .06f), wall, room, false);
        foreach (int side in new[] { -1, 1 }) Slab("Shop side wall", At(side * 2.43f, 2.62f, 1.48f), Size(.06f, 2.96f, 4.46f), wall, room, false);
        foreach (var (prop, lat, depth, y, off) in theme.Items) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ShopProps + prop + ".prefab"); if (!src) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, room);
            go.transform.SetPositionAndRotation(At(lat, depth, y), Quaternion.Euler(0, yaw + off, 0)); StripColliders(go.transform);
        }
        var shade = AssetDatabase.LoadAssetAtPath<GameObject>(ShopProps + "Lighting_Ceiling_Shaded_01.prefab");
        if (shade) { var s = (GameObject)PrefabUtility.InstantiatePrefab(shade, room); s.transform.SetPositionAndRotation(At(0, 2.4f, 2.38f), Quaternion.Euler(0, yaw, 0)); StripColliders(s.transform); }
        var lamp = new GameObject("Shop light").AddComponent<Light>(); lamp.transform.SetParent(room, false); lamp.transform.position = At(0, 2.2f, 2.4f);
        lamp.type = LightType.Point; lamp.color = theme.Light; lamp.intensity = 1.5f; lamp.range = 6.5f; lamp.shadows = LightShadows.None;
        // The shop's name over the window, in the same pack letters as The Odd Table and The Alchemist. Only a couple of
        // shops (the arcade, the record store) run neon; the rest are plain painted letters, so the street isn't a strip.
        var glow = theme.Neon ? GlowMat("Main_" + theme.Name.Replace(" ", ""), theme.Sign, .9f) : InteriorMat("Letters" + theme.Sign, theme.Sign);
        Letters3D(theme.Name, OnFace(x0, z0, facing, 0, .52f, 2.86f), yaw, .4f, 4.3f, glow, glow, b);
    }
    // The City shop modules paint a shallow fake shop inside the glass; drop those faces so the real room shows through.
    static readonly Dictionary<Mesh, Mesh> openCuts = new Dictionary<Mesh, Mesh>();
    static void OpenShopfront(Transform building) {
        foreach (var mf in building.GetComponentsInChildren<MeshFilter>()) {
            var src = mf.sharedMesh; if (!src || !src.name.StartsWith("SM_Bld_Shop_0")) continue;
            if (src.name.EndsWith("_Glass")) { var r = mf.GetComponent<Renderer>(); if (r) r.sharedMaterial = ClearGlass(); continue; }   // see in, not a milky pane
            if (!openCuts.TryGetValue(src, out var cut)) {
                openCuts[src] = cut = CutDoor(src, 1, 1, 0, 1, 1, "Assets/Generated/Meshes/" + src.name + "_Open.asset");
            }
            mf.sharedMesh = cut;
        }
    }
    static Material clearGlass;
    static Material ClearGlass() {
        if (clearGlass) return clearGlass;
        const string path = "Assets/Generated/Main_ShopGlass.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", 0);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
        m.SetColor("_BaseColor", new Color(.75f, .88f, .92f, .14f)); m.SetFloat("_Smoothness", .92f); m.SetFloat("_Metallic", 0);
        EditorUtility.SetDirty(m); return clearGlass = m;
    }
    // The slim units squeezed in beside your restaurant: roller shutters down and a FOR LEASE notice.
    static void ShutteredUnit(float left, float width, float faceZ, Transform parent) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonShops/Prefabs/Buildings/SM_Bld_Wall_Shutter_01.prefab"); if (!src) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent); go.name = "Roller shutter (to let)";
        go.transform.SetPositionAndRotation(new Vector3(left, 0, faceZ), Quaternion.Euler(0, 180, 0));   // faces +Z (Main Street); spans x[left, left+2.5]
        go.transform.localScale = new Vector3(width / 2.5f, .93f, 1); StripColliders(go.transform);
        var paint = InteriorMat("LeaseCard", "F2EBDD");
        Letters3D("FOR LEASE", new Vector3(left + width / 2, 1.9f, faceZ + .03f), 0, .26f, width * .8f, paint, paint, parent);
    }

    // Sidewalks: one rule along the whole gameplay stretch (x -30..30).
    static void MainStreetSidewalks() {
        var p = new GameObject("Main Street sidewalks").transform; p.SetParent(root, false);
        int k = 40;
        // Trees in planted pits at the curb, never in front of a door or your restaurant's windows.
        foreach (float x in new[] { -25f, 5, 15, 25 }) StreetTree(new Vector3(x, 0, -6.3f), k++, p);
        foreach (float x in new[] { -25f, -5, 5, 25 }) StreetTree(new Vector3(x, 0, 6.3f), k++, p);
        // Bins and a hydrant in the same curb strip, next to the lamps.
        foreach (var (x, z) in new[] { (-11.2f, 6.2f), (11.2f, -6.2f), (21.2f, 6.2f) }) Put("Props/SM_Prop_Trashbin_01", new Vector3(x, 0, z), 0, p);
        foreach (var (x, z) in new[] { (-21.2f, 6.1f), (9f, -6.1f) }) Put("Props/SM_Prop_Hydrant_01", new Vector3(x, 0, z), 0, p);
        foreach (var (x, z, yaw) in new[] { (-28.5f, 6.4f, 180f), (28.5f, -6.4f, 0f) }) { Put("Props/SM_Prop_Newspaper_02", new Vector3(x, 0, z), yaw, p); }
    }
}
