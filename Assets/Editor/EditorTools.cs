using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Utilities invoked through editor-command.request (see PrototypeBuildRequest).
public static class EditorTools {
    // Writes the bounding box of every POLYGON City prefab so layouts can be planned without opening Unity.
    public static string DumpCityBounds() {
        Directory.CreateDirectory("EditorOutput");
        var sb = new StringBuilder("prefab\tsize_x\tsize_y\tsize_z\tmin_x\tmin_y\tmin_z\tmax_x\tmax_y\tmax_z\n");
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Synty/PolygonCity/Prefabs" })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!go) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                sb.AppendLine(string.Join("\t", new[] { path.Replace("Assets/Synty/PolygonCity/Prefabs/", ""), b.size.x.ToString("0.00"), b.size.y.ToString("0.00"), b.size.z.ToString("0.00"), b.min.x.ToString("0.00"), b.min.y.ToString("0.00"), b.min.z.ToString("0.00"), b.max.x.ToString("0.00"), b.max.y.ToString("0.00"), b.max.z.ToString("0.00") }));
                n++;
            }
            Object.DestroyImmediate(inst);
        }
        File.WriteAllText("EditorOutput/city-bounds.tsv", sb.ToString());
        return n + " prefabs measured";
    }

    // Launches the built game in snapshot mode; it photographs a tour of the city into Snapshots/ and quits.
    public static string RunSnapshots() {
        var exe = Path.GetFullPath("Builds/Windows/RestaurantCity.exe");
        if (!File.Exists(exe)) return "no build";
        if (Directory.Exists("Snapshots")) foreach (var f in Directory.GetFiles("Snapshots", "*.png")) File.Delete(f);
        System.Diagnostics.Process.Start(exe, "--snapshots -screen-fullscreen 0 -screen-width 1600 -screen-height 900");
        return "launched";
    }

    // Renders prefabs listed in EditorOutput/render-list.txt (paths relative to Assets/Synty/, one per line; an optional
    // "back" suffix renders from behind) to EditorOutput/previews/*.png, so art can be checked without opening the Editor.
    public static string RenderPrefabs() {
        var lines = File.ReadAllLines("EditorOutput/render-list.txt").Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        Directory.CreateDirectory("EditorOutput/previews");
        var util = new PreviewRenderUtility(); int done = 0;
        int cols = 6, tile = 300; var sheet = new Texture2D(cols * tile, Mathf.CeilToInt(lines.Length / (float)cols) * tile, TextureFormat.RGB24, false);
        try {
            util.camera.fieldOfView = 30; util.camera.clearFlags = CameraClearFlags.SolidColor; util.camera.backgroundColor = new Color(.62f, .7f, .78f);
            util.lights[0].intensity = 1.3f; util.lights[0].transform.rotation = Quaternion.Euler(40, 30, 0);
            util.lights[1].intensity = .7f; util.ambientColor = new Color(.45f, .45f, .5f);
            foreach (var line in lines) {
                var parts = line.Split(' '); bool back = parts.Length > 1 && parts[1] == "back";
                var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/" + parts[0] + ".prefab"); if (!src) continue;
                var go = util.InstantiatePrefabInScene(src);
                var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var swap = parts.FirstOrDefault(x => x.StartsWith("mat="));
                if (swap != null) {
                    var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Alts/" + swap.Substring(4) + ".mat");
                    foreach (var r in rs) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith("PolygonCity_0")) ms[i] = m; r.sharedMaterials = ms; }
                }
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var dir = new Vector3(.55f, .4f, back ? -1 : 1).normalized; float dist = b.extents.magnitude / Mathf.Tan(15 * Mathf.Deg2Rad) * 1.05f;
                util.camera.transform.position = b.center + dir * dist; util.camera.transform.LookAt(b.center);
                util.camera.nearClipPlane = .05f; util.camera.farClipPlane = dist * 4;
                var rect = new Rect(0, 0, 400, 400);
                util.BeginStaticPreview(rect); util.Render(true); var tex = util.EndStaticPreview();
                File.WriteAllBytes("EditorOutput/previews/" + Path.GetFileName(parts[0]) + (back ? "_back" : "") + (swap != null ? "_" + swap.Substring(4) : "") + ".png", tex.EncodeToPNG());
                var rt = RenderTexture.GetTemporary(tile, tile); Graphics.Blit(tex, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
                int col = done % cols, row = sheet.height / tile - 1 - done / cols;
                sheet.ReadPixels(new Rect(0, 0, tile, tile), col * tile, row * tile); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(go); done++;
            }
        } finally { util.Cleanup(); }
        sheet.Apply(); File.WriteAllBytes("EditorOutput/previews/_sheet.png", sheet.EncodeToPNG());
        return done + " previews";
    }

    // Reports the Shop_01 module's mesh layout: submeshes, and triangles by position/normal, to plan a walk-in doorway.
    public static string InspectShopMesh() {
        var sb = new StringBuilder();
        foreach (var name in new[] { "SM_Bld_Shop_01", "SM_Bld_Shop_04", "SM_Bld_Shop_02" }) {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Buildings/" + name + ".prefab");
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) {
                var m = mf.sharedMesh; sb.AppendLine(name + " / " + mf.name + " mesh " + m.name + " verts " + m.vertexCount + " sub " + m.subMeshCount + " bounds " + m.bounds + " local " + mf.transform.localPosition + " rot " + mf.transform.localEulerAngles);
                var r = mf.GetComponent<Renderer>(); sb.AppendLine("  mats: " + string.Join(",", r.sharedMaterials.Select(x => x ? x.name : "null")));
                var v = m.vertices;
                for (int sm = 0; sm < m.subMeshCount; sm++) {
                    var t = m.GetTriangles(sm); int inward = 0, backIn = 0, floorUp = 0, frontOut = 0;
                    for (int i = 0; i < t.Length; i += 3) {
                        Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]]; var n = Vector3.Cross(b - a, c - a).normalized; var ctr = (a + b + c) / 3;
                        if (ctr.z < -4.7f && n.z > .7f) backIn++;
                        if (ctr.y < .2f && n.y > .7f && ctr.z < -.5f) floorUp++;
                        if (ctr.z > -.2f && n.z > .7f) frontOut++;
                        if (ctr.x > -4.7f && ctr.x < -.3f && ctr.z < -.5f && ctr.z > -4.7f && ctr.y > .2f && ctr.y < 2.8f) inward++;
                    }
                    sb.AppendLine("  sub" + sm + " tris " + t.Length / 3 + " backwallFacingIn " + backIn + " floorUp " + floorUp + " frontOut " + frontOut + " interiorVolume " + inward);
                }
            }
            foreach (var col in go.GetComponentsInChildren<Collider>()) sb.AppendLine("  collider " + col.GetType().Name + " on " + col.name);
        }
        File.WriteAllText("EditorOutput/shop-mesh.txt", sb.ToString());
        return "ok";
    }

    // Acceptance runs of the Windows build; each writes its player log to Acceptance/<name>.log.
    static string RunBuild(string name, string args) {
        var exe = Path.GetFullPath("Builds/Windows/RestaurantCity.exe"); if (!File.Exists(exe)) return "no build";
        Directory.CreateDirectory("Acceptance"); var log = Path.GetFullPath("Acceptance/" + name + ".log"); if (File.Exists(log)) File.Delete(log);
        System.Diagnostics.Process.Start(exe, args + " -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile \"" + log + "\"");
        return "launched " + name;
    }
    public static string RunInteractionAcceptance() => RunBuild("interaction", "--interaction-test");
    public static string RunPhysicalAcceptance() => RunBuild("physical", "--physical-test");
    public static string RunRestaurantAcceptance() => RunBuild("restaurant", "--restaurant-stage2");
}
