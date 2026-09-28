using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RestaurantCity {
    // People-book portraits: a head-and-shoulders photo of each resident in their idle pose, taken off-screen and cached.
    // Residents you haven't fed yet get a dark silhouette instead.
    public static class ResidentIcons {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        const int Layer = 31, Size = 192;
        static int shots;
        public static Texture2D Get(ResidentDef def, bool silhouette) {
            string key = def.Id + (silhouette ? "#s" : "");
            if (cache.TryGetValue(key, out var cached) && cached) return cached;
            var stage = new GameObject("Portrait stage " + key); stage.transform.position = new Vector3(60 * ++shots, -600, 0);
            var root = ResidentModels.Create(def, stage.transform);
            var anim = root.GetComponentInChildren<ResidentAnimator>(); if (anim) anim.Pose(.9f);
            foreach (var t in stage.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var smr in stage.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            if (silhouette) {
                var dark = new Material(Shader.Find("Universal Render Pipeline/Lit")); dark.SetColor("_BaseColor", new Color(.1f, .13f, .15f)); dark.SetFloat("_Smoothness", 0);   // Lit: the Unlit shader is stripped from builds
                foreach (var r in stage.GetComponentsInChildren<Renderer>()) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = dark; r.sharedMaterials = ms; }
            }
            var camObj = new GameObject("Portrait camera"); var cam = camObj.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = silhouette ? new Color(.72f, .74f, .68f) : new Color(.93f, .86f, .7f);
            cam.fieldOfView = 24; cam.nearClipPlane = .05f; cam.farClipPlane = 30;
            // Frame the top ~60% of the body: short residents get the whole figure, tall ones head and torso.
            float span = Mathf.Max(.7f, def.Height * .5f), centerY = def.Height - span * .5f + .04f;
            var center = stage.transform.position + Vector3.up * centerY;
            float dist = span * .5f / Mathf.Tan(12 * Mathf.Deg2Rad) * 1.08f;
            cam.transform.position = center + new Vector3(.25f, .12f, 1).normalized * dist; cam.transform.LookAt(center);
            var light = new GameObject("Portrait light").AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << Layer; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(30, 200, 0);
            var rt = new RenderTexture(Size, Size, 24); rt.Create(); cam.targetTexture = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            try {
                RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                var previous = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); tex.Apply(); RenderTexture.active = previous;
            } catch (System.Exception e) { Debug.LogWarning("Portrait failed for " + def.Id + ": " + e.Message); }
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camObj); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(stage);
            cache[key] = tex; return tex;
        }
    }
}
