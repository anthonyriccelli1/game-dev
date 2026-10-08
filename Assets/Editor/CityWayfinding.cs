using UnityEditor;
using UnityEngine;

// In-world wayfinding: street-name blades on a lamp post at every intersection (two diagonal corners each), and
// finger signs on the way between Truck Park and Main Street, where new players got lost. The blades are POLYGON
// Shops sign backings painted street-sign green with the names in Shops 3D letters on both faces; finger signs add
// a chevron (the pack's letter V turned on its side) at the end that points the way.
public static partial class CityMap {
    const string ShopsSigns = "Assets/Synty/PolygonShops/Prefabs/Signs/";
    static Material bladeMat, bladeInk;

    static void StreetSigns() {
        var p = new GameObject("Street signs").transform; p.SetParent(root, false);
        bladeMat = InteriorMat("StreetSignGreen", "1D5A3E"); bladeInk = GlowMat("StreetSignInk", "F4F1E6", .3f);
        var avenues = new (float z, string name)[] { (50, "NORTH AVE"), (0, "MAIN ST"), (-50, "SOUTH AVE"), (HarborZ, "HARBOR RD") };
        foreach (float ix in new[] { -40f, 40 }) foreach (var av in avenues) {
            string street = ix < 0 ? "WEST ST" : "EAST ST";
            foreach (var c in new[] { new Vector2(1, 1), new Vector2(-1, -1) }) {
                var post = new Vector3(ix + c.x * 5.7f, 0, av.z + c.y * 5.7f);
                var lamp = Put("Props/SM_Prop_LightPole_Base_01", post, Mathf.Atan2(-c.x, -c.y) * Mathf.Rad2Deg, p);
                if (lamp) lamp.name = "Street sign post " + av.name + " / " + street;
                // Each blade runs along the street it names, hanging out from the pole toward the crossing.
                Blade(p, post + Vector3.up * 3.0f, new Vector3(-c.x, 0, 0), av.name, 1.35f, false);
                Blade(p, post + Vector3.up * 3.3f, new Vector3(0, 0, -c.y), street, 1.35f, false);
            }
        }
        // Finger signs between Truck Park and Main Street (the way runs up West Street).
        Finger(p, new Vector3(-4f, 0, -57.6f), ("MAIN ST", Vector3.left), ("THE ODD TABLE", Vector3.left));
        Finger(p, new Vector3(-34.3f, 0, -57.2f), ("MAIN ST", Vector3.forward), ("TRUCK PARK", Vector3.right));
        Finger(p, new Vector3(-34.3f, 0, -7.2f), ("TRUCK PARK", Vector3.back), ("THE ODD TABLE", Vector3.right));
        Finger(p, new Vector3(-19f, 0, -5.7f), ("TRUCK PARK", Vector3.left), null);
        Debug.Log("STREET_FRONTS shops=" + StreetShopCount + " lights=" + StreetShopLights + " shuttered=" + StreetLeaseCount);
    }
    static void Finger(Transform p, Vector3 post, (string text, Vector3 dir) a, (string text, Vector3 dir)? b) {
        var lamp = Put("Props/SM_Prop_LightPole_Base_02", post, a.dir.x != 0 ? 0 : 90, p); if (lamp) lamp.name = "Finger post " + a.text;
        Blade(p, post + Vector3.up * 2.75f, a.dir, a.text, Mathf.Max(1.2f, .13f * a.text.Length + .55f), true);
        if (b.HasValue) Blade(p, post + Vector3.up * 2.4f, b.Value.dir, b.Value.text, Mathf.Max(1.2f, .13f * b.Value.text.Length + .55f), true);
    }
    // A blade starting at the pole (`at`) and running `length` along `along`, readable from both sides.
    static void Blade(Transform p, Vector3 at, Vector3 along, string text, float length, bool arrow) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(ShopsSigns + "SM_Sign_Backing_Long_03.prefab"); if (!src) return;
        along.Normalize(); var normal = Vector3.Cross(Vector3.up, along);
        var centre = at + along * (length / 2 + .14f);
        const float tall = .3f, thick = .05f;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(src, p); g.name = "Sign blade " + text; StripColliders(g.transform);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bladeMat;
        var b0 = RendererBounds(g); g.transform.localScale = Vector3.Scale(g.transform.localScale, new Vector3(length / b0.size.x, tall / b0.size.y, thick / b0.size.z));
        g.transform.rotation = Quaternion.LookRotation(normal);
        var b = RendererBounds(g); g.transform.position += centre - b.center;
        float textW = length - (arrow ? .55f : .2f); var textC = centre - along * (arrow ? .17f : 0);
        foreach (float side in new[] { 1f, -1f }) {
            var face = normal * side; float yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
            Letters3D(text, textC + face * (thick / 2 + .012f), yaw, .15f, textW, bladeInk, bladeInk, p);
            if (arrow) Chevron(p, centre + along * (length / 2 - .2f) + face * (thick / 2 + .012f), face, along);
        }
    }
    static void Chevron(Transform p, Vector3 at, Vector3 toViewer, Vector3 dir) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(ShopsSigns + "SM_Sign_3dText_Letter_V.prefab"); if (!src) return;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(src, p); g.name = "Chevron"; StripColliders(g.transform);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bladeInk;
        var b = RendererBounds(g); g.transform.localScale *= .2f / b.size.y;
        g.transform.rotation = Quaternion.LookRotation(toViewer, -dir);   // the V's point (its bottom) aims along dir
        b = RendererBounds(g); g.transform.position += at - b.center;
    }
}
