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
    // Re-imports residents and Mixamo clips through ResidentImport (needed when files arrive before the script compiles).
    public static string ReimportCharacters() {
        var sb = new StringBuilder(); int n = 0;
        foreach (var dir in new[] { "Assets/Resources/Residents", "Assets/Resources/Mixamo" }) {
            if (!AssetDatabase.IsValidFolder(dir)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { dir })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                sb.Append(path).Append(" type=").Append(imp.animationType).Append(" human=").Append(avatar && avatar.isHuman).Append(" valid=").Append(avatar && avatar.isValid);
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()) if (!c.name.StartsWith("__preview__")) sb.Append(" clip=").Append(c.name).Append(" loop=").Append(c.isLooping).Append(" human=").Append(c.isHumanMotion);
                sb.Append('\n'); n++;
            }
        }
        Directory.CreateDirectory("EditorOutput"); File.WriteAllText("EditorOutput/characters.txt", sb.ToString());
        return n + " character assets reimported";
    }
    public static string RunSnapshots() {
        var exe = Path.GetFullPath("Builds/Windows/RestaurantCity.exe");
        if (!File.Exists(exe)) return "no build";
        if (Directory.Exists("Snapshots")) foreach (var f in Directory.GetFiles("Snapshots", "*.png")) File.Delete(f);
        // Optional: snapshot-only.txt (project root) limits the tour to shots whose name contains its text.
        string only = File.Exists("snapshot-only.txt") ? " --snapshot-only=" + File.ReadAllText("snapshot-only.txt").Trim() : "";
        System.Diagnostics.Process.Start(exe, "--snapshots -screen-fullscreen 0 -screen-width 1600 -screen-height 900" + only);
        return "launched";
    }

    // Renders prefabs listed in EditorOutput/render-list.txt (paths relative to Assets/Synty/, one per line; an optional
    // "back" suffix renders from behind) to EditorOutput/previews/*.png, so art can be checked without opening the Editor.
    // Writes each listed prefab's bounds (size and min/max) so pack models can be fitted to our station footprints.
    static string PrefabPath(string line) => line.StartsWith("Assets/") ? line + ".prefab" : "Assets/Synty/" + line + ".prefab";
    // Regenerates the art-pack overrides (models, finishes, restaurant shell) without a full build.
    public static string GenerateArt() { ArtPackDressing.GenerateOverrides(); return "art overrides generated"; }
    public static string GenerateStreetStandArt() { StreetStandArt.Generate(); return "street stand art generated"; }
    // Lists the heights of upward-facing surfaces (shelves, worktops) in each render-list prefab, with their materials,
    // so code can put stock on a pack model's own shelves.
    public static string DumpShelfHeights() {
        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines("EditorOutput/render-list.txt").Select(l => l.Trim().Split(' ')[0]).Where(l => l.Length > 0)) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(line)); if (!src) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src); sb.AppendLine(Path.GetFileName(line));
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) {
                var m = mf.sharedMesh; var mats = mf.GetComponent<Renderer>().sharedMaterials; var v = m.vertices;
                for (int sm = 0; sm < m.subMeshCount; sm++) {
                    var t = m.GetTriangles(sm); var heights = new System.Collections.Generic.SortedDictionary<float, float>();
                    for (int i = 0; i < t.Length; i += 3) {
                        Vector3 a = mf.transform.TransformPoint(v[t[i]]), b = mf.transform.TransformPoint(v[t[i + 1]]), c = mf.transform.TransformPoint(v[t[i + 2]]);
                        var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * .5f; if (area < .0001f || n.normalized.y < .9f) continue;
                        float y = Mathf.Round((a.y + b.y + c.y) / 3 * 50) / 50; heights[y] = (heights.TryGetValue(y, out var s0) ? s0 : 0) + area;
                    }
                    sb.AppendLine("  " + mf.name + " sub" + sm + " mat=" + (sm < mats.Length && mats[sm] ? mats[sm].name + " shader=" + mats[sm].shader.name + " queue=" + mats[sm].renderQueue : "?") + "  up: " + string.Join(" ", heights.Where(h => h.Value > .02f).Select(h => h.Key.ToString("0.00") + "(" + h.Value.ToString("0.00") + ")")));
                }
            }
            Object.DestroyImmediate(go);
        }
        File.WriteAllText("EditorOutput/shelf-heights.txt", sb.ToString()); return "shelves written";
    }
    // Up-facing surfaces of render-list prefabs, grouped by height, with their X/Z extents (finds a grill's grate area).
    public static string DumpTopAreas() {
        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines("EditorOutput/render-list.txt").Select(l => l.Trim().Split(' ')[0]).Where(l => l.Length > 0)) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(line)); if (!src) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src); sb.AppendLine(Path.GetFileName(line));
            var groups = new System.Collections.Generic.SortedDictionary<float, Vector4>(); var areas = new System.Collections.Generic.Dictionary<float, float>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) {
                var m = mf.sharedMesh; var v = m.vertices; var t = m.triangles;
                for (int i = 0; i < t.Length; i += 3) {
                    Vector3 a = mf.transform.TransformPoint(v[t[i]]), b = mf.transform.TransformPoint(v[t[i + 1]]), c = mf.transform.TransformPoint(v[t[i + 2]]);
                    var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * .5f; if (area < .00001f || n.normalized.y < .9f) continue;
                    float y = Mathf.Round((a.y + b.y + c.y) / 3 * 100) / 100;
                    var g = groups.TryGetValue(y, out var g0) ? g0 : new Vector4(9, -9, 9, -9);
                    foreach (var p in new[] { a, b, c }) g = new Vector4(Mathf.Min(g.x, p.x), Mathf.Max(g.y, p.x), Mathf.Min(g.z, p.z), Mathf.Max(g.w, p.z));
                    groups[y] = g; areas[y] = (areas.TryGetValue(y, out var s0) ? s0 : 0) + area;
                }
            }
            foreach (var kv in groups) sb.AppendLine("  y=" + kv.Key.ToString("F2") + " x[" + kv.Value.x.ToString("F3") + "," + kv.Value.y.ToString("F3") + "] z[" + kv.Value.z.ToString("F3") + "," + kv.Value.w.ToString("F3") + "] area=" + areas[kv.Key].ToString("F4"));
            Object.DestroyImmediate(go);
        }
        File.WriteAllText("EditorOutput/top-areas.txt", sb.ToString()); return "top areas written";
    }
    public static string DumpPrefabSizes() {
        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines("EditorOutput/render-list.txt").Select(l => l.Trim().Split(' ')[0]).Where(l => l.Length > 0)) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(line)); if (!src) { sb.AppendLine(line + " MISSING"); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src); var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); sb.AppendLine(Path.GetFileName(line) + "\tsize=" + b.size.ToString("F2") + "\tmin=" + b.min.ToString("F2") + "\tmax=" + b.max.ToString("F2")); }
            Object.DestroyImmediate(go);
        }
        Directory.CreateDirectory("EditorOutput"); File.WriteAllText("EditorOutput/prefab-sizes.txt", sb.ToString()); return "sizes written";
    }
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
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(parts[0])); if (!src) continue;
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
    // Runtime-only changes can build the saved scene without regenerating the entire city.
    public static string BuildCurrentWindows() {
        for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
            if (UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i).isDirty)
                throw new System.Exception("Save your current scene before building; no unsaved work was changed.");
        PrototypeBuilder.Build();
        return "saved scene validated and Windows player built";
    }
    public static string RunPhysicalAcceptance() => RunBuild("physical", "--physical-test");
}
