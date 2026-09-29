using System.IO;
using UnityEditor;
using UnityEngine;

// Creates the visual-only street stand from POLYGON Shops pieces. Gameplay remains in the scene
// and PhysicalStand: no imported collision, station, or interaction component is saved here.
public static class StreetStandArt {
    const string Shops = "Assets/Synty/PolygonShops/Prefabs/";
    const string Out = "Assets/Resources/ArtOverrides/Shell/";

    [MenuItem("Restaurant City/Generate Street Stand Art")]
    public static void Generate() {
        if (!AssetDatabase.IsValidFolder("Assets/Synty/PolygonShops")) return;
        Directory.CreateDirectory(Out);
        var root = new GameObject("street_stand");
        var ink = Material("stand_ink", new Color(.09f, .19f, .2f));
        var cream = Material("stand_cream", new Color(.98f, .85f, .66f));
        var coral = Material("stand_coral", new Color(.83f, .34f, .28f));
        var brass = Material("stand_brass", new Color(.76f, .55f, .3f));

        // Five real fabric awnings form one long canopy over the seven station positions.
        // The proportions match the original 8.6 m stand; the customer path stays outside z=6.
        for (int i = 0; i < 5; i++)
            Piece(root.transform, "Buildings/SM_Bld_Awning_01", "Striped fabric awning " + i,
                new Vector3(-3.48f + i * 1.74f, 3.18f, 8.0f), new Vector3(1.76f, .73f, 2.85f));
        foreach (float x in new[] { -4.3f, 4.3f }) {
            Piece(root.transform, "Buildings/SM_Bld_Base_Pillar_Metal_01", "Cast metal canopy upright",
                new Vector3(x, 1.66f, 8.6f), new Vector3(.2f, 3.32f, .2f));
            Piece(root.transform, "Buildings/SM_Bld_Base_Pillar_Metal_02", "Front canopy upright",
                new Vector3(x, 1.66f, 6.83f), new Vector3(.2f, 3.32f, .2f));
        }
        // The cafe counter is panelled, capped and detailed in the pack. It forms a continuous
        // visual apron, but has no collision: guests, player and worker use the old gameplay layout.
        for (int i = 0; i < 3; i++)
            Piece(root.transform, i == 1 ? "Props/SM_Prop_Cafe_Counter_Outdoor_02" : "Props/SM_Prop_Cafe_Counter_Outdoor_01",
                "Panelled serving counter " + i, new Vector3(-2.75f + i * 2.75f, .55f, 7.04f), new Vector3(2.73f, 1.1f, .68f));
        Piece(root.transform, "Signs/SM_Sign_Backing_Long_01", "Enamel stand sign backing",
            new Vector3(0, 2.86f, 6.52f), new Vector3(5.05f, .68f, .18f), ink);
        Lettering(root.transform, "LITTLE FLAME", 2.86f, 6.405f, .45f, brass, coral, cream);

        // A few pack props make the young business feel assembled and lived in. Keep the counter
        // top mostly clear so the dynamic dishes and station feedback remain readable.
        Piece(root.transform, "Props/SM_Prop_Cafe_Napkin_Dispenser_01", "Napkins by serving window",
            new Vector3(3.32f, 1.23f, 7.12f), new Vector3(.16f, .22f, .16f));
        Piece(root.transform, "Props/SM_Prop_Bag_Paper_01", "Takeaway bags",
            new Vector3(-3.86f, .28f, 7.28f), new Vector3(.3f, .55f, .24f));
        Piece(root.transform, "Props/SM_Prop_Lighting_Cable_Bulb_01", "Warm service light left",
            new Vector3(-2.55f, 2.55f, 7.1f), new Vector3(.28f, .62f, .28f));
        Piece(root.transform, "Props/SM_Prop_Lighting_Cable_Bulb_01", "Warm service light right",
            new Vector3(2.55f, 2.55f, 7.1f), new Vector3(.28f, .62f, .28f));
        PrefabUtility.SaveAsPrefabAsset(root, Out + "street_stand.prefab");
        Object.DestroyImmediate(root);

        var board = new GameObject("street_menu_board");
        Piece(board.transform, "Props/SM_Prop_Cafe_Menu_Board_01", "Cafe menu board",
            new Vector3(0, .76f, 0), new Vector3(.9f, 1.3f, .16f));
        PrefabUtility.SaveAsPrefabAsset(board, Out + "street_menu_board.prefab");
        Object.DestroyImmediate(board);
        AssetDatabase.SaveAssets();
    }

    static GameObject Piece(Transform parent, string id, string name, Vector3 center, Vector3 size, Material color = null) {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Shops + id + ".prefab");
        if (!source) { Debug.LogWarning("Missing POLYGON Shops stand piece: " + id); return null; }
        var piece = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        piece.name = name;
        piece.transform.localPosition = Vector3.zero;
        piece.transform.localRotation = Quaternion.identity;
        var bounds = BoundsOf(piece);
        if (bounds.size.x > .001f && bounds.size.y > .001f && bounds.size.z > .001f) {
            var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            piece.transform.localScale = Vector3.Scale(piece.transform.localScale, scale);
            bounds = BoundsOf(piece);
            piece.transform.position += center - bounds.center;
        }
        if (color) foreach (var renderer in piece.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = color;
        foreach (var collider in piece.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        return piece;
    }

    static void Lettering(Transform parent, string title, float y, float z, float height,
        Material brass, Material coral, Material cream) {
        var letters = new System.Collections.Generic.List<(GameObject model, float width)>();
        float total = 0;
        for (int i = 0; i < title.Length; i++) {
            if (title[i] == ' ') { total += .2f; continue; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Shops + "Signs/SM_Sign_3dText_Letter_" + title[i] + ".prefab");
            if (!source) continue;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            model.name = "Cast letter " + title[i] + " " + i;
            // Pack letters face +Z unrotated; turned 180 they face the street (-Z), where customers line up.
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var bounds = BoundsOf(model);
            float scale = height / bounds.size.y;
            model.transform.localScale *= scale;
            bounds = BoundsOf(model);
            letters.Add((model, bounds.size.x));
            total += bounds.size.x + .035f;
            var material = i < 6 ? brass : i < 10 ? coral : cream;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }
        total -= .035f;
        float cursor = -total * .5f;
        int index = 0;
        foreach (char c in title) {
            if (c == ' ') { cursor += .2f; continue; }
            if (index >= letters.Count) break;
            var (model, width) = letters[index++];
            var bounds = BoundsOf(model);
            model.transform.position += new Vector3(cursor + width * .5f - bounds.center.x,
                y - bounds.center.y, z - bounds.center.z);
            cursor += width + .035f;
        }
    }

    static Bounds BoundsOf(GameObject obj) {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static Material Material(string id, Color color) {
        string path = Out + id + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}
