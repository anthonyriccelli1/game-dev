using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Rough full-size layout of Saffron Bay (see claude/world-map.md): every district at its real size so the scale,
// travel times and landmark positions can be felt before any district gets its final art. Old Market's dressed
// 160 m block (CityMap) sits inside this; everything else is simple massing that gets replaced district by district.
public static class CityGreybox {
    public struct District {
        public string Id, Name, Rank; public Rect Area; public string Hex; public int Style;
        public District(string id, string name, string rank, Rect area, string hex, int style) { Id = id; Name = name; Rank = rank; Area = area; Hex = hex; Style = style; }
    }
    // Style: 0 old city, 1 docks, 2 neon, 3 suburbs, 4 gold towers, 5 sci-fi island
    public static readonly District[] Districts = {
        new District("market", "OLD MARKET", "Street Cook", Rect.MinMaxRect(-220, -110, 160, 130), "B9705A", 0),
        new District("docks", "THE DOCKS", "Line Cook", Rect.MinMaxRect(-220, -280, 300, -110), "4F8F99", 1),
        new District("neon", "NEON ROW", "Sous Chef", Rect.MinMaxRect(160, -110, 420, 130), "9C4F84", 2),
        new District("greenleaf", "GREENLEAF", "Head Chef", Rect.MinMaxRect(-220, 130, 420, 330), "D8CDB8", 3),
        new District("gold", "GOLD COAST", "Restaurateur", Rect.MinMaxRect(420, -120, 580, 330), "C9B48A", 4),
        new District("nebula", "LITTLE NEBULA", "Mogul", Rect.MinMaxRect(360, -265, 540, -190), "6E58B8", 5),
    };
    static readonly Rect Built = Rect.MinMaxRect(-80, -80, 80, 80);        // CityMap's dressed block
    static readonly Rect Notch = Rect.MinMaxRect(300, -280, 580, -120);     // bay inlet around the island
    static readonly Rect Island = Rect.MinMaxRect(360, -265, 540, -190);
    static readonly Rect Bridge = Rect.MinMaxRect(434, -190, 446, -120);
    const float RoadW = 10, Walk = 3;
    static Transform root; static readonly List<Rect> roads = new List<Rect>(), reserved = new List<Rect>();
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    static Material M(string name, string hex, float glow = 0) {
        if (mats.TryGetValue(name, out var m)) return m;
        string path = "Assets/Generated/Greybox_" + name + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out var c); m.color = c; m.SetFloat("_Smoothness", .2f);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); } else m.DisableKeyword("_EMISSION");
        mats[name] = m; return m;
    }
    static GameObject Box(string n, Vector3 center, Vector3 size, Material m, Transform parent, bool collide = true) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = n; go.transform.SetParent(parent, false);
        go.transform.position = center; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }
    static GameObject Prim(PrimitiveType t, string n, Vector3 center, Vector3 size, Material m, Transform parent) {
        var go = GameObject.CreatePrimitive(t); go.name = n; go.transform.SetParent(parent, false);
        go.transform.position = center; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = m; return go;
    }
    static void Slab(Rect r, float top, float thick, Material m, Transform parent) =>
        Box("Ground", new Vector3(r.center.x, top - thick / 2, r.center.y), new Vector3(r.width, thick, r.height), m, parent);
    static IEnumerable<Rect> Minus(Rect r, Rect hole) {
        if (!r.Overlaps(hole)) { yield return r; yield break; }
        if (r.yMin < hole.yMin) yield return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, hole.yMin);
        if (r.yMax > hole.yMax) yield return Rect.MinMaxRect(r.xMin, hole.yMax, r.xMax, r.yMax);
        float y0 = Mathf.Max(r.yMin, hole.yMin), y1 = Mathf.Min(r.yMax, hole.yMax);
        if (r.xMin < hole.xMin) yield return Rect.MinMaxRect(r.xMin, y0, hole.xMin, y1);
        if (r.xMax > hole.xMax) yield return Rect.MinMaxRect(hole.xMax, y0, r.xMax, y1);
    }
    static bool IsLand(Rect r) {
        bool main = Rect.MinMaxRect(-220, -280, 580, 330).Contains(r.min) && Rect.MinMaxRect(-220, -280, 580, 330).Contains(r.max) && !r.Overlaps(Notch);
        bool island = Island.Contains(r.min) && Island.Contains(r.max);
        return main || island;
    }
    static bool Free(Rect r) {
        if (!IsLand(r) || r.Overlaps(Expand(Built, 12))) return false;
        foreach (var q in roads) if (q.Overlaps(Expand(r, Walk))) return false;
        foreach (var q in reserved) if (q.Overlaps(r)) return false;
        return true;
    }
    static Rect Expand(Rect r, float d) => Rect.MinMaxRect(r.xMin - d, r.yMin - d, r.xMax + d, r.yMax + d);
    static int H(int a, int b, int c = 0) { unchecked { int h = a * 73856093 ^ b * 19349663 ^ c * 83492791 ^ 4441; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h & 0x7fffffff; } }
    static float R01(int a, int b, int c = 0) => H(a, b, c) % 1000 / 1000f;
    static District At(Vector2 p) { foreach (var d in Districts) if (d.Area.Contains(p)) return d; return Districts[0]; }

    // Text that reads from outside a face pointing along facing (0:+Z 1:+X 2:-Z 3:-X).
    static void Sign(string text, Vector3 pos, int facing, float size, Color color, Transform parent) {
        var go = PrototypeBuilder.Label(text, pos, size, color, parent);
        go.transform.rotation = Quaternion.Euler(0, new[] { 180, 270, 0, 90 }[facing], 0);
    }
    static readonly Vector3[] Fwd = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    public static void Build(Transform world) {
        root = new GameObject("Saffron Bay (greybox districts)").transform; root.SetParent(world, false);
        roads.Clear(); reserved.Clear(); mats.Clear();
        var concrete = M("Concrete", "A9ADAE"); var lawn = M("Lawn", "7FA35C"); var asphalt = M("Asphalt", "34383D");
        var quay = M("Quay", "6F6A62"); var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Misc/Water_01.mat") ?? M("Water", "2A6A80");

        // Water and land.
        Box("Bay", new Vector3(180, -1.25f, 0), new Vector3(3000, .1f, 3000), water, root, false);
        var land = new GameObject("Land").transform; land.SetParent(root, false);
        foreach (var piece in new[] { Rect.MinMaxRect(-220, -120, 580, 330), Rect.MinMaxRect(-220, -280, 300, -120) })
            foreach (var r in Minus(piece, Built)) {
                bool green = r.center.y > 130;
                Slab(r, 0, 3, green ? lawn : concrete, land);
            }
        Slab(Island, 0, 3, M("IslandGround", "8E86A8"), land);
        Box("Bridge deck", new Vector3(Bridge.center.x, -.25f, Bridge.center.y), new Vector3(Bridge.width, .5f, Bridge.height), quay, land);
        foreach (float x in new[] { Bridge.xMin, Bridge.xMax })
            Box("Bridge rail", new Vector3(x, .6f, Bridge.center.y), new Vector3(.3f, 1.2f, Bridge.height), M("Rail", "D9D4C8"), land);
        for (float z = -180; z <= -130; z += 25) foreach (float x in new[] { Bridge.xMin, Bridge.xMax })
            Box("Bridge tower", new Vector3(x, 6, z), new Vector3(1, 12, 1), M("Rail", "D9D4C8"), land, false);

        // Roads (10 m) outside the dressed block, including a ring road around it.
        var h = new List<(float z, float x0, float x1)> { (0, -220, 580), (50, -220, 420), (-50, -220, 300), (130, -220, 580), (230, -220, 420), (-110, -220, 580), (-200, -220, 300), (88, -93, 93), (-88, -93, 93) };
        var v = new List<(float x, float z0, float z1)> { (-140, -280, 330), (-40, -280, 330), (40, -280, 330), (160, -280, 330), (290, -280, 330), (420, -120, 330), (500, -120, 330), (88, -93, 93), (-88, -93, 93), (450, -265, -190) };
        var roadRoot = new GameObject("Roads").transform; roadRoot.SetParent(root, false);
        foreach (var (z, x0, x1) in h) foreach (var r in Minus(Rect.MinMaxRect(x0, z - RoadW / 2, x1, z + RoadW / 2), Built)) AddRoad(r, .012f, asphalt, roadRoot);
        foreach (var (x, z0, z1) in v) foreach (var r in Minus(Rect.MinMaxRect(x - RoadW / 2, z0, x + RoadW / 2, z1), Built)) {
            var clipped = r; if (!IsLand(Rect.MinMaxRect(r.xMin, r.yMin + .1f, r.xMax, r.yMax - .1f)) && !r.Overlaps(Island)) {
                // Keep only the parts over land (roads stop at the inlet).
                if (r.Overlaps(Notch)) clipped = Rect.MinMaxRect(r.xMin, Mathf.Max(r.yMin, Notch.yMax), r.xMax, r.yMax);
            }
            AddRoad(clipped, .016f, asphalt, roadRoot);
        }

        Landmarks();
        Massing();
        Signs();
        Coast();
        LockBarriers();
    }
    static void AddRoad(Rect r, float top, Material m, Transform parent) {
        if (r.width < .5f || r.height < .5f) return;
        roads.Add(r);
        Box("Road", new Vector3(r.center.x, top - .02f, r.center.y), new Vector3(r.width, .04f, r.height), m, parent, false);
        // Centre dashes along the long axis.
        var dash = M("Dash", "E8E2CF"); bool alongX = r.width > r.height;
        float len = alongX ? r.width : r.height;
        for (float t = 3; t < len - 3; t += 12) {
            var p = alongX ? new Vector3(r.xMin + t, top + .002f, r.center.y) : new Vector3(r.center.x, top + .002f, r.yMin + t);
            Box("Lane dash", p, alongX ? new Vector3(3, .01f, .25f) : new Vector3(.25f, .01f, 3), dash, parent, false);
        }
    }

    // Placeholder buildings for the places named in the world plan, each with a sign facing its street.
    static void Landmarks() {
        var lm = new GameObject("Landmarks").transform; lm.SetParent(root, false);
        void Place(string name, string sub, float x, float z, float w, float d, float hgt, int facing, string hex, float glow = 0) {
            var r = Rect.MinMaxRect(x - w / 2, z - d / 2, x + w / 2, z + d / 2); reserved.Add(Expand(r, 2));
            var g = new GameObject(name).transform; g.SetParent(lm, false);
            Box(name, new Vector3(x, hgt / 2, z), new Vector3(w, hgt, d), M("LM_" + hex, hex, glow), g);
            float half = facing % 2 == 0 ? d / 2 : w / 2; var f = Fwd[facing];
            var face = new Vector3(x, 0, z) + f * (half + .06f);
            Sign(name, face + Vector3.up * Mathf.Min(hgt - 1.2f, 5.5f), facing, 1.6f, Color.white, g);
            Sign(sub, face + Vector3.up * Mathf.Min(hgt - 2.6f, 4.1f), facing, .7f, new Color(1, .9f, .7f), g);
            var mat = M("LM_Door", "22262B");
            var doorPos = new Vector3(x, 1.4f, z) + f * (half + .02f);
            Box("Door", doorPos, facing % 2 == 0 ? new Vector3(2.4f, 2.8f, .1f) : new Vector3(.1f, 2.8f, 2.4f), mat, g, false);
        }
        // Docks
        Place("FISHMARKET", "Seafood supplier  /  The Docks", -100, -130, 34, 20, 9, 0, "5E8C94");
        Place("THE SALTY HATCH", "Restaurant for sale  /  The Docks", 10, -222, 16, 12, 5, 0, "3E7C88");
        Place("CAPTAIN KRILL'S", "Rival  /  The Docks", 120, -222, 18, 14, 6, 0, "C0664B");
        Place("CONTAINER 13", "Hidden recipe  /  night", 240, -228, 6, 2.6f, 2.6f, 0, "B8402F");
        // Neon Row
        Place("THE LUCKY COMET", "Casino  /  Neon Row", 225, 25, 44, 32, 22, 2, "7A2E6A", .6f);
        Place("BURGER BARON", "Rival  /  Neon Row", 225, -16, 20, 14, 7, 0, "D35C2F");
        Place("NIGHT OWL NOODLES", "Restaurant for sale  /  Neon Row", 350, -16, 18, 14, 6, 0, "2F4F8F", .35f);
        // Greenleaf
        Place("FARMERS' CO-OP", "Supplier  /  Greenleaf", -100, 150, 34, 20, 8, 2, "8C6A3F");
        Place("STARLITE DRIVE-IN", "Restaurant for sale  /  Greenleaf", 110, 150, 22, 14, 6, 2, "D84A5B", .3f);
        Place("GAS 'N GO", "Gas station  /  Greenleaf", 250, 150, 16, 12, 5, 2, "3D7F4E");
        Place("GRANDMA OPAL'S", "Hidden recipe  /  Greenleaf", -170, 248, 12, 12, 7, 2, "C9A27E");
        Place("MAMA GRILL'S", "Rival  /  Greenleaf", 330, 248, 20, 16, 6, 2, "B55A3C");
        // Gold Coast
        Place("BAYLINE MOTORS", "Dealership  /  Gold Coast", 530, 30, 26, 40, 8, 3, "2C4E6E", .2f);
        Place("TERRACE 9", "Restaurant for sale  /  Gold Coast", 470, 200, 30, 30, 70, 1, "C8B07A");
        Place("MARCHETTI IMPORTS", "Supplier  /  Gold Coast", 540, 250, 24, 24, 10, 3, "6E5A45");
        Place("GILDED ORBIT FLAGSHIP", "Rival HQ  /  Gold Coast", 545, -70, 30, 30, 60, 3, "B8912F", .25f);
        // Little Nebula
        Place("NEBULA BAZAAR", "Flux supplier  /  night only", 400, -228, 30, 20, 7, 1, "5B3FA0", .8f);
        Place("CRATER KITCHEN", "Restaurant for sale  /  Little Nebula", 500, -215, 20, 16, 8, 3, "3FA08C", .6f);
        Place("THE VOID", "Rival  /  Little Nebula", 505, -248, 16, 14, 14, 3, "0E0E14");
        // The Bayside: the second starter restaurant (built at runtime by RestaurantController) on an open waterfront.
        reserved.Add(Rect.MinMaxRect(-220, -36, -170, -2));
        var bay = new GameObject("Bayside waterfront").transform; bay.SetParent(lm, false);
        Box("Quay rail", new Vector3(-219.4f, .55f, -19), new Vector3(.2f, 1.1f, 34), M("Rail", "D9D4C8"), bay);
        for (float z = -32; z <= -6; z += 8) {
            CityMap.Prefab("Props/SM_Prop_ParkBench_01", new Vector3(-216.5f, 0, z), 270, bay);
            CityMap.Prefab("Props/SM_Prop_LightPole_Base_02", new Vector3(-218.6f, 0, z + 4), 90, bay);
        }
        foreach (var t in new[] { new Vector3(-205, 0, -30), new Vector3(-176, 0, -30), new Vector3(-210, 0, -8), new Vector3(-174, 0, -6) })
            CityMap.Prefab("Environments/SM_Env_Tree_0" + (Mathf.Abs((int)t.x) % 3 + 1), t, t.z * 11, bay);
        CityMap.Prefab("Props/SM_Prop_Umbrella_01", new Vector3(-201, 1.3f, -14), 0, bay);
        CityMap.Prefab("Props/SM_Prop_PicnicTable_01", new Vector3(-201, 0, -14), 90, bay);
        // Docks set dressing: container stacks and cranes on the quay; the Drive-In lot.
        var dock = new GameObject("Container yard").transform; dock.SetParent(lm, false);
        string[] cols = { "B8402F", "2F6DB8", "D1A33A", "3F8F5A", "7A7F86" };
        for (int i = 0; i < 9; i++) for (int j = 0; j < 4; j++) for (int k = 0; k <= H(i, j) % 3; k++) {
            var p = new Vector3(185 + i * 11.5f, 1.3f + k * 2.6f, -265 + j * 7);
            if (Mathf.Abs(p.x - 240) < 7 && Mathf.Abs(p.z - -228) < 5) continue;
            Box("Container", p, new Vector3(6, 2.6f, 2.4f), M("Box" + cols[H(i, j, k) % 5], cols[H(i, j, k) % 5]), dock);
        }
        reserved.Add(Rect.MinMaxRect(178, -272, 292, -236));
        foreach (float x in new[] { -120f, -20, 80 }) {
            var c = M("Crane", "D8A032");
            foreach (float dx in new[] { -4f, 4 }) foreach (float dz in new[] { -270f, -262 }) Box("Crane leg", new Vector3(x + dx, 10, dz), new Vector3(.8f, 20, .8f), c, dock);
            Box("Crane beam", new Vector3(x, 20.5f, -262), new Vector3(1.4f, 1.4f, 34), c, dock, false);
            reserved.Add(Rect.MinMaxRect(x - 6, -274, x + 6, -258));
        }
        var lot = Rect.MinMaxRect(90, 170, 130, 205); reserved.Add(lot);
        Box("Drive-In lot", new Vector3(lot.center.x, .01f, lot.center.y), new Vector3(lot.width, .02f, lot.height), M("Lot", "4A4E53"), lm, false);
        Box("Drive-In screen", new Vector3(110, 7, 204), new Vector3(24, 10, .6f), M("Screen", "EDEDE4", .3f), lm);
    }

    // Fill every free lot with a simple building mass whose size and height match its district.
    static void Massing() {
        var mass = new GameObject("Massing").transform; mass.SetParent(root, false);
        foreach (var d in Districts) {
            float lotSize = d.Style == 1 ? 32 : d.Style == 3 ? 20 : d.Style == 4 ? 28 : 18;
            var r = d.Area;
            for (float x = r.xMin; x + lotSize <= r.xMax + .01f; x += lotSize)
                for (float z = r.yMin; z + lotSize <= r.yMax + .01f; z += lotSize) {
                    int ix = (int)x, iz = (int)z; float rnd = R01(ix, iz), rnd2 = R01(iz, ix, 7);
                    var cell = Rect.MinMaxRect(x + 1, z + 1, x + lotSize - 1, z + lotSize - 1);
                    if (!Free(cell)) {
                        // Try a smaller footprint that clears the sidewalk.
                        cell = Rect.MinMaxRect(x + 4, z + 4, x + lotSize - 4, z + lotSize - 4);
                        if (cell.width < 6 || !Free(cell)) continue;
                    }
                    var c = cell.center; string shade = Shade(d.Hex, H(ix, iz) % 3);
                    switch (d.Style) {
                        case 1: // warehouses
                            Box("Warehouse", new Vector3(c.x, 4.5f + rnd * 3, c.y), new Vector3(cell.width, 9 + rnd * 6, cell.height * .8f), M("Docks" + shade, shade), mass); break;
                        case 3: // houses with pitched roofs and a tree
                            if (rnd < .12f) { CityMap.Prefab("Environments/SM_Env_Tree_0" + (H(ix, iz) % 3 + 1), new Vector3(c.x, 0, c.y), rnd * 360, mass); break; }
                            float hw = 9 + rnd * 3, hd = 8 + rnd2 * 3;
                            Box("House", new Vector3(c.x, 2.8f, c.y), new Vector3(hw, 5.6f, hd), M("House" + shade, shade), mass);
                            var roof = Box("Roof", new Vector3(c.x, 5.6f, c.y), new Vector3(hw + .6f, hd * .72f, hd * .72f), M("Roof" + H(ix, iz) % 3, new[] { "8A4B3C", "4E5A63", "6B5A4A" }[H(ix, iz) % 3]), mass, false);
                            roof.transform.rotation = Quaternion.Euler(45, 0, 0);
                            CityMap.Prefab("Environments/SM_Env_Tree_0" + (H(iz, ix) % 3 + 1), new Vector3(cell.xMax - 1.5f, 0, cell.yMin + 1.5f), rnd2 * 360, mass);
                            break;
                        case 5: // odd domes and spires
                            if (rnd < .5f) Prim(PrimitiveType.Sphere, "Dome", new Vector3(c.x, 0, c.y), new Vector3(cell.width, cell.width * .8f, cell.height), M("Neb" + shade, shade, .25f), mass);
                            else Prim(PrimitiveType.Cylinder, "Spire", new Vector3(c.x, 6 + rnd * 10, c.y), new Vector3(cell.width * .5f, 6 + rnd * 10, cell.height * .5f), M("Neb" + shade, shade, .25f), mass);
                            break;
                        default: {
                            float lo = d.Style == 0 ? 9 : d.Style == 2 ? 16 : 30, hi = d.Style == 0 ? 24 : d.Style == 2 ? 48 : 80;
                            float hgt = Mathf.Lerp(lo, hi, rnd * rnd);
                            Box("Building", new Vector3(c.x, hgt / 2, c.y), new Vector3(cell.width, hgt, cell.height), M(d.Id + shade, shade, d.Style == 2 ? .08f : 0), mass);
                            break;
                        }
                    }
                }
        }
    }
    static string Shade(string hex, int k) {
        ColorUtility.TryParseHtmlString("#" + hex, out var c); float f = new[] { .85f, 1f, 1.12f }[k];
        return ColorUtility.ToHtmlStringRGB(new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f)));
    }
    // "Welcome to" signs where the main roads cross into each district.
    static void Signs() {
        var s = new GameObject("District signs").transform; s.SetParent(root, false);
        void Gate(string id, float x, float z, int facing) {
            District d = default; foreach (var q in Districts) if (q.Id == id) d = q;
            var board = M("SignBoard_" + d.Hex, d.Hex);
            var p = new Vector3(x, 0, z);
            Box("Sign post", p + Vector3.up * 2.5f, new Vector3(.3f, 5, .3f), M("Post", "2A2E33"), s);
            var face = Fwd[facing]; var side = new Vector3(face.z, 0, -face.x);
            Box("Sign board", p + Vector3.up * 5.4f, facing % 2 == 0 ? new Vector3(11, 2.6f, .3f) : new Vector3(.3f, 2.6f, 11), board, s, false);
            Sign("WELCOME TO " + d.Name, p + Vector3.up * 5.9f + face * .2f, facing, .55f, Color.white, s);
            Sign(id == "market" ? "Your home turf" : "Unlocks at " + d.Rank.ToUpper(), p + Vector3.up * 4.9f + face * .2f, facing, .38f, new Color(1, .92f, .75f), s);
        }
        Gate("docks", -48, -118, 0); Gate("docks", 48, -118, 0);
        Gate("neon", 167, -8, 3); Gate("neon", 167, 43, 3);
        Gate("greenleaf", -48, 138, 2); Gate("greenleaf", 48, 138, 2);
        Gate("gold", 427, -8, 3); Gate("gold", 427, 123, 3);
        Gate("nebula", 429, -116, 0);
        Gate("market", 153, 8, 1); Gate("market", -48, -102, 2);
    }
    // Police tape and barriers where roads enter a district. DistrictLocks hides a district's set once its rank is reached.
    static void LockBarriers() {
        var all = new GameObject("District locks").transform; all.SetParent(root, false);
        var tape = M("Tape", "F2C230", .2f);
        foreach (var d in RestaurantCity.CityDistricts.All) {
            if (d.Rank == 0) continue;
            var g = new GameObject("Lock_" + d.Id).transform; g.SetParent(all, false);
            void Line(Vector3 center, bool acrossX, float length) {
                var dir = acrossX ? Vector3.right : Vector3.forward; float yaw = acrossX ? 0 : 90;
                for (float t = -length / 2 + .8f; t <= length / 2 - .7f; t += 1.6f) CityMap.Prefab("Props/SM_Prop_Barrier_01", center + dir * t, yaw, g);
                Box("Police tape", center + Vector3.up * 1.15f, acrossX ? new Vector3(length + 1, .12f, .04f) : new Vector3(.04f, .12f, length + 1), tape, g, false);
                foreach (float e in new[] { -length / 2 - .6f, length / 2 + .6f }) CityMap.Prefab("Props/SM_Prop_Cone_01", center + dir * e, 0, g);
            }
            foreach (var r in roads) {
                bool vertical = r.height > r.width;
                if (vertical) {
                    if (r.xMin < d.X0 - .5f || r.xMax > d.X1 + .5f) continue;
                    foreach (float z in new[] { d.Z0, d.Z1 }) if (r.yMin < z - 1 && r.yMax > z + 1) Line(new Vector3(r.center.x, 0, z + (z == d.Z0 ? 1.5f : -1.5f)), true, r.width);
                } else {
                    if (r.yMin < d.Z0 - .5f || r.yMax > d.Z1 + .5f) continue;
                    foreach (float x in new[] { d.X0, d.X1 }) if (r.xMin < x - 1 && r.xMax > x + 1) Line(new Vector3(x + (x == d.X0 ? 1.5f : -1.5f), 0, r.center.y), false, r.height);
                }
            }
            if (d.Id == "nebula") Line(new Vector3(Bridge.center.x, 0, -128), true, Bridge.width - .6f);
        }
    }
    // Invisible walls along the shoreline (the bridge stays open).
    static void Coast() {
        var c = new GameObject("Shoreline").transform; c.SetParent(root, false);
        void Wall(float x0, float z0, float x1, float z1) {
            var go = new GameObject("Shore wall"); go.transform.SetParent(c, false);
            go.transform.position = new Vector3((x0 + x1) / 2, 2, (z0 + z1) / 2);
            go.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(.6f, Mathf.Abs(x1 - x0)), 4, Mathf.Max(.6f, Mathf.Abs(z1 - z0)));
        }
        Wall(-220, -280, 300, -280); Wall(300, -280, 300, -120); Wall(300, -120, Bridge.xMin, -120); Wall(Bridge.xMax, -120, 580, -120);
        Wall(580, -120, 580, 330); Wall(580, 330, -220, 330); Wall(-220, 330, -220, -280);
        Wall(Island.xMin, Island.yMin, Island.xMax, Island.yMin); Wall(Island.xMin, Island.yMin, Island.xMin, Island.yMax); Wall(Island.xMax, Island.yMin, Island.xMax, Island.yMax);
        Wall(Island.xMin, Island.yMax, Bridge.xMin, Island.yMax); Wall(Bridge.xMax, Island.yMax, Island.xMax, Island.yMax);
    }
}
