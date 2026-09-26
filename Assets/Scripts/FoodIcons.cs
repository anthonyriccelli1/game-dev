using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RestaurantCity {
    // Shop pictures are photos of the game's own food models, taken once off-screen and cached.
    public static class FoodIcons {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        const int Layer = 31, Size = 160;
        static string KindFor(string id) => id == "patty" ? "RawProtein" : id == "bun" ? "Bun" : id == "greens" || id == "soup_veg" ? "RawGreens" : id == "midnight_sauce" || id == "moonberry" ? "RawSauce" : "Plate";
        public static Texture2D Get(string id, Color background) {
            if (cache.TryGetValue(id, out var cached) && cached) return cached;
            var stage = new GameObject("Icon stage " + id); stage.transform.position = new Vector3(40 * cache.Count, -400, 0);   // each photo gets its own spot
            var item = KitchenArt.CreateItem(KindFor(id), stage.transform); item.transform.localPosition = Vector3.zero;
            if (id == "patty" || id == "bun" || id == "greens") for (int i = 1; i < 3; i++) { var more = KitchenArt.CreateItem(KindFor(id), stage.transform); more.transform.localPosition = new Vector3((i - 1.5f) * .22f, i == 2 ? .05f : 0, .12f * i); }
            foreach (var t in stage.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            var renderers = stage.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(stage.transform.position, Vector3.one * .3f);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var camObj = new GameObject("Icon camera"); var cam = camObj.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = background; cam.fieldOfView = 24; cam.nearClipPlane = .01f; cam.farClipPlane = 20;
            float dist = Mathf.Max(.2f, bounds.extents.magnitude) / Mathf.Tan(12 * Mathf.Deg2Rad) * 1.05f;
            cam.transform.position = bounds.center + new Vector3(0, .55f, -1).normalized * dist; cam.transform.LookAt(bounds.center);
            var light = new GameObject("Icon light").AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << Layer; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(40, -30, 0);
            var rt = new RenderTexture(Size, Size, 24); rt.Create(); cam.targetTexture = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            try {
                RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                var previous = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); tex.Apply(); RenderTexture.active = previous;
            } catch (System.Exception e) { Debug.LogWarning("Food icon failed for " + id + ": " + e.Message); }
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camObj); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(stage);   // immediate: the next photo must not see this food
            cache[id] = tex; return tex;
        }
    }
}
