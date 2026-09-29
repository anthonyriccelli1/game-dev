using UnityEngine;

namespace RestaurantCity {
    // Simple playable shapes remain available when an art-pack override is absent.
    public static class ArchitectureArt {
        static Material plaster, timber, metal, counterTop;

        static Material Surface(ref Material cached, Color color) {
            if (cached) return cached;
            cached = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cached.color = color;
            return cached;
        }

        static void Block(Transform parent, string name, Vector3 center, Vector3 size, Material material) {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = center;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(block.GetComponent<Collider>());
        }

        public static bool IsPiece(string id) => id == "partition_wall" || id == "service_window" || id == "service_counter";

        public static GameObject Create(string id, Transform parent) {
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            var light = Surface(ref plaster, new Color(.72f, .69f, .62f));
            var wood = Surface(ref timber, new Color(.32f, .21f, .15f));
            var steel = Surface(ref metal, new Color(.29f, .34f, .36f));
            var top = Surface(ref counterTop, new Color(.73f, .61f, .44f));

            if (id == "partition_wall") {
                Block(root.transform, "Plaster partition", new Vector3(0, 1.28f, 0), new Vector3(1.98f, 2.56f, .18f), light);
                Block(root.transform, "Timber base", new Vector3(0, .13f, -.105f), new Vector3(1.98f, .2f, .06f), wood);
                Block(root.transform, "Timber cap", new Vector3(0, 2.53f, 0), new Vector3(1.98f, .08f, .22f), wood);
            } else if (id == "service_window") {
                Block(root.transform, "Window sill wall", new Vector3(0, .52f, 0), new Vector3(1.98f, 1.04f, .2f), light);
                Block(root.transform, "Serving ledge", new Vector3(0, 1.07f, 0), new Vector3(1.98f, .08f, .45f), top);
                Block(root.transform, "Left jamb", new Vector3(-.89f, 1.78f, 0), new Vector3(.2f, 1.4f, .2f), wood);
                Block(root.transform, "Right jamb", new Vector3(.89f, 1.78f, 0), new Vector3(.2f, 1.4f, .2f), wood);
                Block(root.transform, "Window lintel", new Vector3(0, 2.43f, 0), new Vector3(1.98f, .22f, .22f), wood);
            } else if (id == "service_counter") {
                Block(root.transform, "Counter body", new Vector3(0, .47f, 0), new Vector3(1.96f, .94f, .65f), steel);
                Block(root.transform, "Counter top", new Vector3(0, .98f, 0), new Vector3(1.98f, .12f, .82f), top);
                Block(root.transform, "Front trim", new Vector3(0, .55f, -.345f), new Vector3(1.82f, .06f, .025f), wood);
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = id == "partition_wall" ? new Vector3(0, 1.28f, 0) : id == "service_window" ? new Vector3(0, 1.28f, 0) : new Vector3(0, .52f, 0);
            collider.size = id == "partition_wall" ? new Vector3(1.98f, 2.56f, .2f) : id == "service_window" ? new Vector3(1.98f, 2.56f, .24f) : new Vector3(1.98f, 1.04f, .82f);
            return root;
        }
    }
}
