using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// One-shot candidate operation. Never called by Generate or the physical build path.
public static partial class CityMap {
    const string PreparationScene = "Assets/Scenes/RestaurantCity.unity";
    const string PreparationBaseHash = "eebf2188c793627ab26c6f9cab16a3d4da0d204ed8ef8fa5ab6b4d6f888172bb";
    const long PreparationCity = 200433816, PreparationParent = 1988237334, PreparationPole = 1421236397;

    [Serializable] class PreparationAudit {
        public string operation = "market-row-units-v1";
        public string scene_guid, before_sha256, after_sha256, meta_sha256;
        public string[] removed_records, added_records, changed_records;
        public string preservation = "All other serialized records byte-equivalent after newline normalization; record order ignored. Shop buildings, Reserved source and assets not regenerated.";
        public bool passed;
    }
    class PreparationRecord {
        public long id; public int type; public string body;
    }
    static string PreparationHash(byte[] bytes) {
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    static void PreparationRequire(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static Dictionary<long, PreparationRecord> PreparationRecords(string text) {
        text = text.Replace("\r\n", "\n");
        var headers = Regex.Matches(text, @"(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\n");
        PreparationRequire(headers.Count > 0 && text.StartsWith("%YAML 1.1\n"), "Unsupported scene serialization.");
        var result = new Dictionary<long, PreparationRecord>();
        for (int i = 0; i < headers.Count; i++) {
            var h = headers[i]; long id = long.Parse(h.Groups[2].Value);
            int end = i + 1 < headers.Count ? headers[i + 1].Index : text.Length;
            PreparationRequire(!result.ContainsKey(id), "Duplicate serialized record ID.");
            result.Add(id, new PreparationRecord { id = id, type = int.Parse(h.Groups[1].Value), body = text.Substring(h.Index, end - h.Index) });
        }
        return result;
    }
    static long PreparationField(PreparationRecord record, string field) {
        var m = Regex.Match(record.body, @"(?m)^  " + field + @": \{fileID: (-?\d+)\}$");
        return m.Success ? long.Parse(m.Groups[1].Value) : 0;
    }
    static long[] PreparationChildren(PreparationRecord record) {
        var m = Regex.Match(record.body, @"(?m)^  m_Children:(?: \[\])?\n((?:  - \{fileID: -?\d+\}\n)*)");
        PreparationRequire(m.Success, "Missing serialized child list.");
        return Regex.Matches(m.Groups[1].Value, @"fileID: (-?\d+)").Cast<Match>().Select(x => long.Parse(x.Groups[1].Value)).ToArray();
    }
    // Ownership closure only, never arbitrary reference traversal (materials/assets are not owned).
    static HashSet<long> PreparationClosure(Dictionary<long, PreparationRecord> records, IEnumerable<long> roots) {
        var owned = new HashSet<long>(roots); bool changed;
        do {
            int count = owned.Count;
            foreach (var r in records.Values) {
                long go = PreparationField(r, "m_GameObject"), instance = PreparationField(r, "m_PrefabInstance");
                if ((go != 0 && owned.Contains(go)) || (instance != 0 && owned.Contains(instance))) owned.Add(r.id);
                if (!owned.Contains(r.id)) continue;
                if (go != 0) owned.Add(go);
                if (instance != 0) owned.Add(instance);
                if (r.type == 4 && r.body.Contains("  m_Children:")) foreach (long child in PreparationChildren(r)) owned.Add(child);
                if (r.type == 1) foreach (Match m in Regex.Matches(r.body, @"component: \{fileID: (-?\d+)\}")) owned.Add(long.Parse(m.Groups[1].Value));
            }
            changed = owned.Count != count;
        } while (changed);
        PreparationRequire(owned.All(records.ContainsKey), "Ownership closure has unresolved records.");
        return owned;
    }
    static string PreparationWithoutChildren(string body) => Regex.Replace(body, @"(?m)^  m_Children:(?: \[\])?\n(?:  - \{fileID: -?\d+\}\n)*", "");
    static PreparationAudit PreparationCompare(string before, string after, string metaHash) {
        var a = PreparationRecords(before); var b = PreparationRecords(after);
        var removed = PreparationClosure(a, new[] { PreparationPole, 655633823L, 1142114067L });
        var actualRemoved = new HashSet<long>(a.Keys.Except(b.Keys));
        PreparationRequire(removed.SetEquals(actualRemoved), "Removed records differ from confirmed dressing ownership closure.");
        var roots = PreparationChildren(b[PreparationParent]);
        PreparationRequire(roots.Length == 18, "Expected two shutters and sixteen letter roots.");
        var added = PreparationClosure(b, roots);
        PreparationRequire(added.SetEquals(b.Keys.Except(a.Keys)), "New records are not exclusively new closed-shop dressing.");
        PreparationRequire(PreparationChildren(b[PreparationCity]).SequenceEqual(PreparationChildren(a[PreparationCity]).Where(x => x != PreparationPole)), "Unexpected city child list changes.");
        foreach (long id in a.Keys.Intersect(b.Keys)) {
            string oldBody = a[id].body, newBody = b[id].body;
            if (id == PreparationParent || id == PreparationCity) { oldBody = PreparationWithoutChildren(oldBody); newBody = PreparationWithoutChildren(newBody); }
            if (id == 1988237333L) oldBody = oldBody.Replace("  m_Name: The Tin Diner\n", "  m_Name: Market Row closed shops\n");
            PreparationRequire(oldBody == newBody, "Unexpected serialization/semantic change in record " + id + ". No broad churn accepted.");
        }
        // No surviving unrelated record may reference a removed local object.
        foreach (var r in a.Values.Where(x => !removed.Contains(x.id) && x.id != PreparationCity && x.id != PreparationParent))
            foreach (Match m in Regex.Matches(r.body, @"\{fileID: (-?\d+)\}"))
                PreparationRequire(!removed.Contains(long.Parse(m.Groups[1].Value)), "External reference into dressing from " + r.id);
        PreparationRequire(!Regex.IsMatch(after, "Tin Diner|TinDiner|THE TIN", RegexOptions.IgnoreCase), "Obsolete scene text remains.");
        return new PreparationAudit {
            scene_guid = AssetDatabase.AssetPathToGUID(PreparationScene), before_sha256 = PreparationHash(Encoding.UTF8.GetBytes(before)),
            after_sha256 = PreparationHash(Encoding.UTF8.GetBytes(after)), meta_sha256 = metaHash,
            removed_records = removed.OrderBy(x => x).Select(x => x.ToString()).ToArray(),
            added_records = added.OrderBy(x => x).Select(x => x.ToString()).ToArray(),
            changed_records = new[] { PreparationCity.ToString(), "1988237333", PreparationParent.ToString() }, passed = true
        };
    }
    static Transform PreparationUnique(Transform[] all, string name) {
        var matches = all.Where(x => x.name == name).ToArray();
        PreparationRequire(matches.Length == 1, "Missing/ambiguous hierarchy: " + name); return matches[0];
    }
    static void PreparationPrefab(Transform t, string guid, Vector3 position) {
        var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
        PreparationRequire(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && AssetDatabase.AssetPathToGUID(path) == guid && Vector3.Distance(t.position, position) < .0001f, "Unexpected dressing prefab/position.");
    }
    static void PreparationAssets() {
        var paths = new List<string> { "Assets/Synty/PolygonShops/Prefabs/Buildings/SM_Bld_Wall_Shutter_01.prefab" };
        foreach (char ch in "FORLEASE".Distinct()) paths.Add("Assets/Synty/PolygonShops/Prefabs/Signs/SM_Sign_3dText_Letter_" + ch + ".prefab");
        foreach (string path in paths) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PreparationRequire(prefab && prefab.GetComponentsInChildren<Renderer>().Length > 0, "Required licensed prefab/renderers missing: " + path);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true)) PreparationRequire(filter.sharedMesh, "Missing prefab mesh: " + path);
            var bounds = RendererBounds(prefab);
            PreparationRequire(bounds.size.y > 0 && !float.IsNaN(bounds.size.y) && !float.IsInfinity(bounds.size.y), "Required prefab has invalid letter/shutter bounds: " + path);
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Interior_LeaseCard.mat");
        PreparationRequire(material && material.shader && material.HasProperty("_Smoothness"), "Existing LeaseCard material/shader required.");
        ColorUtility.TryParseHtmlString("#F2EBDD", out var paint);
        PreparationRequire(Vector4.Distance(material.color, paint) < .00001f && Mathf.Abs(material.GetFloat("_Smoothness") - .25f) < .00001f, "LeaseCard would require a material change.");
    }
    static bool PreparationFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    static bool PreparationFinite(Vector3 value) => PreparationFinite(value.x) && PreparationFinite(value.y) && PreparationFinite(value.z);
    static void PreparationBounds(Bounds bounds) {
        PreparationRequire(PreparationFinite(bounds.center) && PreparationFinite(bounds.size) && bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0, "Nonfinite/degenerate dressing bounds.");
    }
    static void PreparationDressing(Transform parent) {
        const string letters = "FOR LEASE", dir = "Assets/Synty/PolygonShops/Prefabs/Signs/SM_Sign_3dText_Letter_";
        PreparationRequire(parent.childCount == 18 && parent.GetComponentsInChildren<Collider>(true).Length == 0, "Incomplete closed-shop dressing or colliders.");
        PreparationRequire(Vector3.Distance(parent.lossyScale, Vector3.one) < .0001f && Quaternion.Angle(parent.rotation, Quaternion.identity) < .001f, "Unexpected dressing parent transform.");
        foreach (var t in parent.GetComponentsInChildren<Transform>(true)) {
            var q = t.localRotation;
            PreparationRequire(PreparationFinite(t.position) && PreparationFinite(t.localPosition) && PreparationFinite(t.localScale) && PreparationFinite(t.lossyScale) && PreparationFinite(q.x) && PreparationFinite(q.y) && PreparationFinite(q.z) && PreparationFinite(q.w), "Nonfinite dressing transform.");
        }
        foreach (var r in parent.GetComponentsInChildren<Renderer>(true)) PreparationBounds(r.bounds);
        var paint = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Interior_LeaseCard.mat");
        for (int group = 0; group < 2; group++) {
            float left = -25 + 5 * group;
            var shutter = parent.GetChild(group * 9);
            string shutterPath = "Assets/Synty/PolygonShops/Prefabs/Buildings/SM_Bld_Wall_Shutter_01.prefab";
            PreparationPrefab(shutter, AssetDatabase.AssetPathToGUID(shutterPath), new Vector3(left, 0, 40.03f));
            PreparationRequire(shutter.name == "Roller shutter (to let)" && Vector3.Distance(shutter.localScale, new Vector3(2, .93f, 1)) < .0001f && Quaternion.Angle(shutter.rotation, Quaternion.Euler(0, 180, 0)) < .001f, "Unexpected shutter placement/scale.");
            // Independently compute Letters3D's height scaling, spacing and fit-to-width.
            var sources = new List<GameObject>(); var widths = new List<float>();
            float total = 0;
            foreach (char ch in letters) {
                if (ch == ' ') { sources.Add(null); widths.Add(.26f * .45f); total += widths.Last(); continue; }
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(dir + ch + ".prefab");
                PreparationRequire(src && Quaternion.Angle(src.transform.rotation, Quaternion.identity) < .001f, "Unexpected letter source rotation.");
                var bounds = RendererBounds(src); PreparationBounds(bounds);
                float width = bounds.size.x * (.26f / bounds.size.y);
                sources.Add(src); widths.Add(width); total += width + .03f;
            }
            total -= .03f; float fit = total > 4 ? 4 / total : 1, cursor = -total * fit / 2;
            PreparationRequire(PreparationFinite(total) && total > 0 && PreparationFinite(fit) && fit > 0, "Invalid word layout.");
            int child = group * 9 + 1;
            for (int i = 0; i < letters.Length; i++) {
                if (!sources[i]) { cursor += widths[i] * fit; continue; }
                var t = parent.GetChild(child++); var src = sources[i]; var sourceBounds = RendererBounds(src); var bounds = RendererBounds(t.gameObject);
                string path = dir + letters[i] + ".prefab";
                PreparationRequire(t.name == "Letter " + letters[i] && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)) == AssetDatabase.AssetPathToGUID(path), "Wrong ordered letter prefab identity.");
                var centre = new Vector3(left + 2.5f - (cursor + widths[i] * fit / 2), 1.9f, 40.06f);
                var scale = src.transform.localScale * (.26f / sourceBounds.size.y) * fit;
                PreparationRequire(Vector3.Distance(bounds.center, centre) < .0001f && Vector3.Distance(t.localScale, scale) < .0001f && Quaternion.Angle(t.rotation, Quaternion.identity) < .001f && Mathf.Abs(bounds.size.y - .26f * fit) < .0001f && Mathf.Abs(bounds.size.x - widths[i] * fit) < .0001f, "Wrong letter centre/orientation/scale/width/height.");
                var renderers = t.GetComponentsInChildren<Renderer>(); var originals = src.GetComponentsInChildren<Renderer>();
                PreparationRequire(renderers.Length == originals.Length, "Unexpected letter renderers.");
                for (int r = 0; r < renderers.Length; r++) {
                    var actual = renderers[r].sharedMaterials; var expected = originals[r].sharedMaterials;
                    PreparationRequire(actual.Length == expected.Length && actual.Length > 0 && actual[0] == paint, "Missing LeaseCard binding.");
                    for (int slot = 1; slot < actual.Length; slot++) PreparationRequire(actual[slot] == expected[slot], "Unexpected secondary letter material.");
                }
                cursor += widths[i] * fit + .03f * fit;
            }
            PreparationRequire(Mathf.Abs(cursor - (total * fit / 2 + .03f * fit)) < .0001f && total * fit <= 4.0001f, "Wrong complete word centre/width.");
        }
    }
    public static void PrepareMarketRowUnits() {
        string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-scenePreparationOutput");
        PreparationRequire(Application.isBatchMode && index >= 0 && index + 1 < args.Length, "Batchmode and explicit local audit output required.");
        string output = Path.GetFullPath(args[index + 1]);
        string logs = Path.GetFullPath("Logs") + Path.DirectorySeparatorChar;
        PreparationRequire(output.StartsWith(logs, StringComparison.OrdinalIgnoreCase) && Directory.Exists(output), "Audit output must be an existing dedicated project Logs job directory.");
        var beforeBytes = File.ReadAllBytes(PreparationScene); string before = Encoding.UTF8.GetString(beforeBytes);
        PreparationRequire(PreparationHash(beforeBytes) == PreparationBaseHash, "Unreviewed base scene. Operation is pinned to the inspected scene bytes.");
        string metaHash = PreparationHash(File.ReadAllBytes(PreparationScene + ".meta"));
        PreparationRequire(AssetDatabase.AssetPathToGUID(PreparationScene) == "8701a29b903efb34eb5e50c600b2cbbb", "Unexpected scene GUID.");
        PreparationAssets();
        var scene = EditorSceneManager.OpenScene(PreparationScene, OpenSceneMode.Single);
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var parent = PreparationUnique(all, "The Tin Diner"); var pole = PreparationUnique(all, "Pole sign / THE TIN DINER");
        PreparationRequire(parent.parent && parent.parent == pole.parent && parent.childCount == 2 && pole.childCount == 5, "Unexpected diner hierarchy.");
        PreparationRequire(GlobalObjectId.GetGlobalObjectIdSlow(parent).targetObjectId == (ulong)PreparationParent && GlobalObjectId.GetGlobalObjectIdSlow(pole).targetObjectId == (ulong)PreparationPole, "Unexpected persistent root identity.");
        var awning = parent.Cast<Transform>().SingleOrDefault(x => AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(x.gameObject)) == "01d36092439d4f64da930deab65f52b1");
        var milkshake = parent.Cast<Transform>().SingleOrDefault(x => AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(x.gameObject)) == "6f80e919ce3f15944905cac373e3b447");
        PreparationRequire(awning && milkshake, "Confirmed awning/milkshake missing.");
        PreparationPrefab(awning, "01d36092439d4f64da930deab65f52b1", new Vector3(-20, -5.3596582f, 40.05f));
        PreparationPrefab(milkshake, "6f80e919ce3f15944905cac373e3b447", new Vector3(-20, 3.6f, 37.8f));
        // Ground pivots are cell+5, not the (-25,35)/(-20,35) cell origins.
        foreach (float x in new[] { -20f, -15f }) {
            var shops = all.Where(t => t.parent && t.parent.name == "Building" && t.parent.parent == parent.parent && Vector3.Distance(t.position, new Vector3(x, 0, 40)) < .0001f && t.name.StartsWith("SM_Bld_Shop_")).ToArray();
            PreparationRequire(shops.Length == 1, "Missing/ambiguous retained shop at " + x);
            PreparationRequire(shops[0].parent.Cast<Transform>().Any(t => t.name.StartsWith("SM_Bld_Apartment_Roof_") && Mathf.Abs(t.position.y - 3) < .0001f), "Missing retained one-storey roof.");
        }
        // Preflight reference safety before changing any object.
        var records = PreparationRecords(before); var removed = PreparationClosure(records, new[] { PreparationPole, 655633823L, 1142114067L });
        foreach (var r in records.Values.Where(r => !removed.Contains(r.id) && r.id != PreparationCity && r.id != PreparationParent))
            foreach (Match m in Regex.Matches(r.body, @"\{fileID: (-?\d+)\}")) PreparationRequire(!removed.Contains(long.Parse(m.Groups[1].Value)), "External dressing reference.");
        Object.DestroyImmediate(awning.gameObject); Object.DestroyImmediate(milkshake.gameObject); Object.DestroyImmediate(pole.gameObject);
        parent.name = "Market Row closed shops";
        ShutteredUnit(-25, 5, 40.03f, parent); ShutteredUnit(-20, 5, 40.03f, parent);
        PreparationDressing(parent);
        PreparationRequire(EditorSceneManager.SaveScene(scene, PreparationScene), "Scene save failed.");
        // No SaveAssets: existing shared materials must never become an export.
        PreparationRequire(metaHash == PreparationHash(File.ReadAllBytes(PreparationScene + ".meta")), "Scene metadata changed.");
        string after = File.ReadAllText(PreparationScene); var audit = PreparationCompare(before, after, metaHash);
        var reopened = EditorSceneManager.OpenScene(PreparationScene, OpenSceneMode.Single);
        var reopenedTransforms = reopened.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        PreparationDressing(PreparationUnique(reopenedTransforms, "Market Row closed shops"));
        PreparationRequire(after == File.ReadAllText(PreparationScene), "Persisted scene changed on reopen.");
        File.WriteAllText(Path.Combine(output, "structural-audit.json"), JsonUtility.ToJson(audit, true), new UTF8Encoding(false));
        Debug.Log("SCENE_PREPARATION_PASSED market-row-units-v1");
    }
}
