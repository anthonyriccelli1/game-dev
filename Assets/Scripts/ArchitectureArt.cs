using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Placeable room modules (partition wall, service window, service counter). Walls and windows are built
    // from boxes whose UVs are measured in metres, exactly like the room's own walls, so any wall finish
    // (paint, wallpaper, pack brick or tile) covers every face at the same scale. They reach the ceiling.
    public static class ArchitectureArt {
        public const float WallHeight = 3.82f;   // room ceiling underside is at 3.83
        static Material timber, metal, counterTop;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        static Material Surface(ref Material cached, Color color) {
            if (cached) return cached;
            cached = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cached.color = color;
            return cached;
        }

        // A box mesh with metre UVs; the piece-space centre is included so neighbouring parts line up.
        static Mesh MeterBox(Vector3 center, Vector3 size) {
            string key = center.ToString("F3") + size.ToString("F3");
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            var half = size * .5f;
            foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back }) {
                var d = -n; var up = Mathf.Abs(n.y) > .5f ? Vector3.forward : Vector3.up;
                var u = Vector3.Cross(up, d).normalized; var v = up;
                float hu = Mathf.Abs(u.x) * half.x + Mathf.Abs(u.y) * half.y + Mathf.Abs(u.z) * half.z;
                float hv = Mathf.Abs(v.x) * half.x + Mathf.Abs(v.y) * half.y + Mathf.Abs(v.z) * half.z;
                float hn = Mathf.Abs(n.x) * half.x + Mathf.Abs(n.y) * half.y + Mathf.Abs(n.z) * half.z;
                var c = n * hn; int start = verts.Count;
                foreach (var corner in new[] { c - u * hu - v * hv, c - u * hu + v * hv, c + u * hu + v * hv, c + u * hu - v * hv }) {
                    verts.Add(corner); var p = corner + center; uvs.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v)));
                }
                tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "MetreBox" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            meshes[key] = mesh; return mesh;
        }

        static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Material material) {
            var block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = center;
            block.AddComponent<MeshFilter>().sharedMesh = MeterBox(center, size);
            block.AddComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        public static bool IsPiece(string id) => id == "partition_wall" || id == "service_window" || id == "service_counter";
        public static bool IsPaintable(string id) => id == "partition_wall" || id == "service_window";

        // The placement grid stops a little short of the room walls. A wall whose end lands near a room wall
        // is extended to meet it, so a partition across the room closes off cleanly. roomMin/roomMax are the
        // room's inner wall faces in world X/Z.
        public static void ReachRoomWalls(GameObject piece, string id, Vector2 roomMin, Vector2 roomMax) {
            if (!piece || !IsPaintable(id)) return;
            var old = piece.transform.Find("Paint / wall extension -1"); if (old) Object.Destroy(old.gameObject);
            old = piece.transform.Find("Paint / wall extension 1"); if (old) Object.Destroy(old.gameObject);
            foreach (float side in new[] { -1f, 1f }) {
                var end = piece.transform.TransformPoint(new Vector3(side, 0, -.5f));
                var dir = piece.transform.TransformDirection(new Vector3(side, 0, 0));
                float gap = Mathf.Abs(dir.x) > .5f ? (dir.x > 0 ? roomMax.x - end.x : end.x - roomMin.x) : (dir.z > 0 ? roomMax.y - end.z : end.z - roomMin.y);
                if (gap <= .02f || gap > .8f) continue;
                Block(piece.transform, "Paint / wall extension " + side, new Vector3(side * (1f + gap * .5f), WallHeight * .5f, -.5f), new Vector3(gap, WallHeight, .18f), RestaurantArt.PieceBaseMaterial(null));
            }
        }

        public static GameObject Create(string id, Transform parent) {
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            var wall = RestaurantArt.PieceBaseMaterial(null);   // replaced by RenderPieceFinish with the real finish
            var wood = Surface(ref timber, new Color(.32f, .21f, .15f));
            var steel = Surface(ref metal, new Color(.29f, .34f, .36f));
            var top = Surface(ref counterTop, new Color(.73f, .61f, .44f));
            float h = WallHeight;

            // Walls run along a grid line (the footprint's back edge, local z = -0.5), so walls, windows and
            // corners from neighbouring cells meet exactly. A post at each end closes corners and T-joints.
            const float e = -.5f;
            if (id == "partition_wall") {
                Block(root.transform, "Paint / wall", new Vector3(0, h * .5f, e), new Vector3(2f, h, .18f), wall);
                foreach (float z in new[] { -.1f, .1f }) Block(root.transform, "Timber skirting", new Vector3(0, .1f, e + z), new Vector3(1.76f, .2f, .03f), wood);
            } else if (id == "service_window") {
                const float sill = 1.04f, lintel = 2.45f;
                Block(root.transform, "Paint / sill wall", new Vector3(0, sill * .5f, e), new Vector3(2f, sill, .2f), wall);
                Block(root.transform, "Paint / left jamb", new Vector3(-.85f, (sill + lintel) * .5f, e), new Vector3(.3f, lintel - sill, .2f), wall);
                Block(root.transform, "Paint / right jamb", new Vector3(.85f, (sill + lintel) * .5f, e), new Vector3(.3f, lintel - sill, .2f), wall);
                Block(root.transform, "Paint / lintel wall", new Vector3(0, (lintel + h) * .5f, e), new Vector3(2f, h - lintel, .2f), wall);
                Block(root.transform, "Serving ledge", new Vector3(0, sill + .04f, e), new Vector3(1.4f, .08f, .45f), top);
                Block(root.transform, "Window head trim", new Vector3(0, lintel - .04f, e), new Vector3(1.4f, .08f, .24f), wood);
                foreach (float z in new[] { -.11f, .11f }) Block(root.transform, "Timber skirting", new Vector3(0, .1f, e + z), new Vector3(1.76f, .2f, .03f), wood);
            }
            if (IsPaintable(id)) foreach (float x in new[] { -1f, 1f }) {
                Block(root.transform, "Paint / corner post", new Vector3(x, h * .5f, e), new Vector3(.24f, h, .24f), wall);
                Block(root.transform, "Timber post foot", new Vector3(x, .1f, e), new Vector3(.27f, .2f, .27f), wood);
            }
            if (id == "service_counter") {
                Block(root.transform, "Counter body", new Vector3(0, .47f, 0), new Vector3(1.96f, .94f, .65f), steel);
                Block(root.transform, "Counter top", new Vector3(0, .98f, 0), new Vector3(1.98f, .12f, .82f), top);
                Block(root.transform, "Front trim", new Vector3(0, .55f, -.345f), new Vector3(1.82f, .06f, .025f), wood);
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = IsPaintable(id) ? new Vector3(0, h * .5f, -.5f) : new Vector3(0, .52f, 0);
            collider.size = id == "partition_wall" ? new Vector3(1.98f, h, .2f) : id == "service_window" ? new Vector3(1.98f, h, .24f) : new Vector3(1.98f, 1.04f, .82f);
            return root;
        }
    }
}
