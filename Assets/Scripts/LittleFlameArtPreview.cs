using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Little Flame's approved art pass is the default in the saved game scene.
    // It replaces only the rendered truck; colliders and kitchen gameplay stay intact.
    public sealed class LittleFlameArtPreview : MonoBehaviour {
        Transform truck;
        bool? exteriorLit;
        CityGame game;
        Light originalTruckLight;
        readonly List<Light> exteriorLights = new List<Light>();
        readonly List<Renderer> exteriorDiffusers = new List<Renderer>();
        readonly List<GameObject> awningBulbs = new List<GameObject>();
        Material daylightDiffuser;
        Material steel, darkSteel, brass, wood, charcoal, amber, primer, rubber, tile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() {
            var root = GameObject.Find("Little Flame (your food truck)");
            if (root && !root.GetComponent<LittleFlameArtPreview>()) root.AddComponent<LittleFlameArtPreview>();
            if (Array.Exists(Environment.GetCommandLineArgs(), a => a == "--snapshots") &&
                Array.Exists(Environment.GetCommandLineArgs(), a => a == "--truck-art-shots")) {
                var shots = SnapshotTour.Shots;
                int first = shots.Length;
                Array.Resize(ref shots, first + 3);
                shots[first] = ("truck_art_close", new Vector3(4.7f, 0, -67.1f), -45, 5, 60);
                shots[first + 1] = ("truck_art_hatch", new Vector3(-1.4f, 0, -67.3f), 0, 5, 60);
                shots[first + 2] = ("truck_art_reverse", new Vector3(-3.0f, .82f, -62.35f), 90, 8, 60);
                SnapshotTour.Shots = shots;
            }
        }

        void Start() {
            truck = transform;
            var source = Resources.Load<GameObject>("TruckArtPass/LittleFlamePatina");
            if (!source) { Debug.LogError("LITTLE_FLAME_ART: UV-mapped truck mesh missing"); return; }
            var old = truck.Find("Little Flame model");
            var shell = Instantiate(source, truck, false); shell.name = "Little Flame / weathered shell";
            shell.transform.localPosition = Vector3.zero; shell.transform.localRotation = Quaternion.identity;
            foreach (var collider in shell.GetComponentsInChildren<Collider>()) Destroy(collider);
            foreach (var renderer in shell.GetComponentsInChildren<Renderer>()) {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    var n = mats[i] ? mats[i].name : "";
                    var key = n.Contains("LF_InnerLow") ? "innerlow" : n.Contains("LF_Inner") ? "inner" :
                        n.Contains("LF_Floor") ? "floor" : n.Contains("LF_Cream") ? "cream" :
                        n.Contains("LF_Teal") ? "teal" : n.Contains("LF_Coral") ? "coral" :
                        n.Contains("LF_Chrome") || n.Contains("LF_Trim") ? "steel" : null;
                    if (key != null) mats[i] = Textured(key, n.Contains("Chrome") ? .48f : .08f);
                }
                renderer.sharedMaterials = mats;
            }
            if (old) old.gameObject.SetActive(false);

            steel = Solid("Preview aged steel", "747B78", .55f);
            darkSteel = Solid("Preview oxidized steel", "454D4A", .32f);
            brass = Solid("Preview worn brass", "A9895B", .52f);
            wood = Solid("Preview ash counter trim", "7A5E45", .05f);
            charcoal = Solid("Preview charcoal", "303A39", .05f);
            primer = Solid("Preview exposed primer", "747B73", .08f);
            rubber = Solid("Preview rubber", "343735", 0);
            tile = Textured("steel", .22f);
            amber = Solid("Preview warm diffuser", "FFE0A6", 0, true);
            daylightDiffuser = Solid("Preview unlit lamp glass", "5B5A51", 0);
            game = FindFirstObjectByType<CityGame>();

            // Remove the showroom roof billboard and its oversized duplicated lettering.
            foreach (Transform child in truck) {
                if (child == shell || child.name == "Little Flame model") continue;
                if (child.name.StartsWith("Roof sign") || child.name.StartsWith("Letter ")) child.gameObject.SetActive(false);
                if (child.name == "Truck light") { originalTruckLight = child.GetComponent<Light>(); if (originalTruckLight) originalTruckLight.range = 5.3f; }
                if (child.name == "Bulb") awningBulbs.Add(child.gameObject);
            }
            Exterior();
            Interior();
            SyncExteriorLighting();
            Debug.Log("LITTLE_FLAME_ART_ACTIVE: weathered truck installed; kitchen stations and colliders unchanged");
        }

        void LateUpdate() {
            SyncExteriorLighting();
        }

        void SyncExteriorLighting() {
            if (!game || game.State == null) return;
            bool night = game.State.IsNight;
            if (exteriorLit == night) return;
            exteriorLit = night;
            foreach (var bulb in awningBulbs) if (bulb) bulb.SetActive(night);
            foreach (var light in exteriorLights) if (light) light.enabled = night;
            foreach (var diffuser in exteriorDiffusers) if (diffuser) {
                diffuser.sharedMaterial = night ? amber : daylightDiffuser;
                diffuser.enabled = night;
            }
            if (originalTruckLight) originalTruckLight.intensity = night ? .72f : 0f;
            Debug.Log("LITTLE_FLAME_ART_LIGHTS " + (night ? "night on" : "day off"));
        }

        static Material Solid(string name, string hex, float metal, bool glow = false) {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { name = name, color = c };
            m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", metal > .2f ? .38f : .16f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.8f); }
            return m;
        }
        static Material Textured(string key, float metal) {
            var m = Solid("Preview patina / " + key, "FFFFFF", metal);
            var texture = Resources.Load<Texture2D>("TruckArtPass/" + key);
            if (texture) { m.mainTexture = texture; m.SetTexture("_BaseMap", texture); }
            return m;
        }
        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size;
            Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = mat; return g;
        }
        static GameObject Quad(Transform parent, string name, Vector3 pos, Vector3 size, Quaternion rotation, Material mat) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = rotation; g.transform.localScale = size;
            Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = mat; return g;
        }
        static void Rod(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material mat) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = (a + b) * .5f;
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            g.transform.localScale = new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2);
            Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = mat;
        }
        static Light Lamp(Transform parent, string name, Vector3 pos, float intensity, float range) {
            var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(parent, false); l.transform.localPosition = pos;
            l.type = LightType.Point; l.color = new Color(1f, .80f, .57f); l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            return l;
        }
        static Material ImageMat(string resource) {
            var m = Solid("Preview graphic / " + resource, "FFFFFF", 0);
            var texture = Resources.Load<Texture2D>("TruckArtPass/" + resource);
            m.mainTexture = texture; m.SetTexture("_BaseMap", texture);
            m.SetFloat("_Cull", 0); m.doubleSidedGI = true;
            return m;
        }

        void Exterior() {
            var p = new GameObject("Art preview / exterior fittings").transform; p.SetParent(truck, false);
            // A low, riveted enamel board belongs to the serving hatch, rather than a billboard above the roof.
            Box(p, "Riveted old sign back", new Vector3(-1.37f, 3.57f, -1.68f), new Vector3(3.32f, .76f, .075f), darkSteel);
            Quad(p, "Hand painted Little Flame", new Vector3(-1.37f, 3.57f, -1.73f), new Vector3(3.13f, .65f, 1), Quaternion.identity, ImageMat("nameboard"));
            foreach (float x in new[] { -2.94f, .2f }) foreach (float y in new[] { 3.27f, 3.87f })
                Rod(p, "Sign rivet", new Vector3(x, y, -1.738f), new Vector3(x, y, -1.75f), .019f, brass);
            // A smaller repaired service window lip has a rounded wood edge and battered steel underside.
            Box(p, "Service hatch sill", new Vector3(-1.37f, 1.77f, -1.68f), new Vector3(2.93f, .12f, .35f), darkSteel);
            Rod(p, "Warm wood sill nose", new Vector3(-2.8f, 1.82f, -1.88f), new Vector3(.06f, 1.82f, -1.88f), .035f, wood);
            foreach (float x in new[] { -2.75f, -.02f }) {
                Rod(p, "Awning brace", new Vector3(x, 3.15f, -1.64f), new Vector3(x, 2.81f, -2.18f), .025f, darkSteel);
                Box(p, "Awning mount", new Vector3(x, 3.17f, -1.63f), new Vector3(.17f, .12f, .10f), steel);
            }
            // The lower left replacement skin is a deliberate repair, with real fasteners and a stained edge.
            Box(p, "Mismatched lower panel", new Vector3(-3.48f, 1.19f, -1.65f), new Vector3(.59f, .77f, .035f), primer);
            foreach (float x in new[] { -3.72f, -3.24f }) foreach (float y in new[] { .88f, 1.5f })
                Rod(p, "Repair rivet", new Vector3(x, y, -1.69f), new Vector3(x, y, -1.705f), .018f, darkSteel);
            Rod(p, "Water stain at repair", new Vector3(-3.24f, .88f, -1.69f), new Vector3(-3.23f, .55f, -1.69f), .008f, charcoal);
            // Entry hardware and tread make the cab-side door feel used, while leaving its walkway unobstructed.
            Rod(p, "Door grab rail", new Vector3(.91f, 1.32f, -1.7f), new Vector3(.91f, 2.35f, -1.7f), .026f, darkSteel);
            foreach (float y in new[] { 1.36f, 2.3f }) Box(p, "Door rail fixing", new Vector3(.91f, y, -1.62f), new Vector3(.12f, .08f, .13f), steel);
            for (int i = 0; i < 6; i++) Box(p, "Worn step tread", new Vector3(1.44f, .84f, -2.08f + i * .11f), new Vector3(.72f, .012f, .023f), rubber);
            // The service light is the truck's warm focal point after dark.
            foreach (float x in new[] { -2.58f, -.32f }) {
                Box(p, "Service lamp shade", new Vector3(x, 2.83f, -2.08f), new Vector3(.29f, .075f, .22f), darkSteel);
                exteriorDiffusers.Add(Box(p, "Service lamp diffuser", new Vector3(x, 2.78f, -2.09f), new Vector3(.23f, .018f, .16f), amber).GetComponent<Renderer>());
                exteriorLights.Add(Lamp(p, "Service pool", new Vector3(x, 2.77f, -2.18f), .78f, 3.2f));
            }
        }

        void Interior() {
            var p = new GameObject("Art preview / fitted galley").transform; p.SetParent(truck, false);
            // Existing interactive pack stations remain in their exact positions. These pieces join them visually.
            Box(p, "Grease-darkened grill backsplash", new Vector3(-1.97f, 2.22f, -1.395f), new Vector3(1.22f, 1.02f, .038f), tile);
            for (float x = -2.5f; x < -1.45f; x += .22f) Rod(p, "Backsplash scored seam", new Vector3(x, 1.76f, -1.418f), new Vector3(x, 2.7f, -1.418f), .004f, darkSteel);
            // Service wall: continuous ledge and a shallow order rail tie grill, plate rack and hatch together.
            Box(p, "Continuous service rail", new Vector3(-1.16f, 2.10f, -1.39f), new Vector3(3.25f, .07f, .14f), wood);
            Rod(p, "Ticket rail", new Vector3(-2.65f, 2.47f, -1.34f), new Vector3(-.07f, 2.47f, -1.34f), .017f, brass);
            for (int i = 0; i < 3; i++) Box(p, "Order ticket", new Vector3(-1.92f + i * .49f, 2.38f, -1.37f), new Vector3(.22f, .21f, .012f), Solid("Preview ticket " + i, i == 1 ? "D7BE9B" : "E6DBC4", 0));

            // Sink-side splash protection, a narrow dish rail and a worn upper locker.
            Box(p, "Sink backsplash", new Vector3(-2.16f, 1.85f, 1.785f), new Vector3(1.35f, .62f, .035f), tile);
            Rod(p, "Dish towel rail", new Vector3(-2.75f, 2.26f, 1.70f), new Vector3(-1.63f, 2.26f, 1.70f), .018f, darkSteel);
            Box(p, "Towel", new Vector3(-2.52f, 2.04f, 1.69f), new Vector3(.25f, .45f, .025f), Solid("Preview used cotton", "BAAA8C", 0));
            Box(p, "Upper locker body", new Vector3(-.22f, 2.66f, 1.55f), new Vector3(1.23f, .61f, .42f), wood);
            Box(p, "Upper locker door", new Vector3(-.22f, 2.66f, 1.31f), new Vector3(1.16f, .54f, .035f), Textured("innerlow", .05f));
            Box(p, "Locker finger pull", new Vector3(-.22f, 2.48f, 1.28f), new Vector3(.24f, .024f, .026f), brass);
            Box(p, "Locker underlight", new Vector3(-.22f, 2.32f, 1.35f), new Vector3(.61f, .014f, .06f), amber);
            Lamp(p, "Sink worklight", new Vector3(-1.4f, 2.57f, 1.18f), .68f, 2.5f);

            // The previously blank end wall now tells the story of a small working kitchen.
            Box(p, "Old prep board surround", new Vector3(-3.76f, 2.40f, .10f), new Vector3(.075f, .91f, 1.16f), wood);
            Quad(p, "Shift board", new Vector3(-3.709f, 2.40f, .10f), new Vector3(1.03f, .78f, 1), Quaternion.Euler(0, -90, 0), ImageMat("shiftboard"));
            for (int i = 0; i < 3; i++) {
                Rod(p, "Tool hook", new Vector3(-3.72f, 1.78f, -.35f + i * .36f), new Vector3(-3.61f, 1.78f, -.35f + i * .36f), .018f, brass);
            }
            Rod(p, "Water pipe vertical", new Vector3(-3.72f, 1.04f, 1.30f), new Vector3(-3.72f, 2.03f, 1.30f), .029f, darkSteel);
            Rod(p, "Water pipe ceiling", new Vector3(-3.72f, 2.99f, 1.30f), new Vector3(-.80f, 2.99f, 1.30f), .026f, darkSteel);
            for (float x = -3.1f; x < 1.0f; x += 1.1f) Box(p, "Back wall vertical join", new Vector3(x, 2.42f, 1.793f), new Vector3(.028f, 1.72f, .018f), primer);
            var runner = Solid("Preview worn kitchen runner", "746E5D", 0);
            Box(p, "Non-slip work lane", new Vector3(-1.27f, .841f, .12f), new Vector3(4.35f, .018f, .68f), runner);
            for (int i = 0; i < 12; i++) Box(p, "Scuffed floor tread", new Vector3(-3.25f + i * .34f, .854f, .12f), new Vector3(.017f, .004f, .56f), darkSteel);
            // The cab end of the original shell has teal upward-facing floor faces.
            // Cover only that walkable floor section with the galley's worn linoleum.
            Box(p, "Front cab worn floor", new Vector3(2.52f, .882f, .18f), new Vector3(2.56f, .026f, 2.94f), Textured("floor", 0));
            Box(p, "Raised cab floor finish", new Vector3(3.04f, 1.146f, .18f), new Vector3(1.57f, .024f, 2.88f), Textured("floor", 0));

            // Small practical fixtures shape the ceiling instead of a single bright white wash.
            foreach (float x in new[] { -2.87f, -.24f }) {
                Box(p, "Ceiling fixture shell", new Vector3(x, 3.30f, .17f), new Vector3(.79f, .095f, .38f), darkSteel);
                Box(p, "Warm fluorescent diffuser", new Vector3(x, 3.235f, .17f), new Vector3(.67f, .018f, .29f), amber);
                Lamp(p, "Ceiling light pool", new Vector3(x, 3.18f, .17f), .72f, 3.3f);
            }
        }

    }
}
