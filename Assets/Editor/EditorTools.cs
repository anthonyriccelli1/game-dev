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
}
