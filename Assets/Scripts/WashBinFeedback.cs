using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Presentation follows accepted work and authoritative progress. No simulation or save writes.
    public sealed class WashFeedback : MonoBehaviour {
        Transform root, sponge;
        AudioSource sound;
        GameObject plate, water;
        readonly GameObject[] stains = new GameObject[5], bubbles = new GameObject[8];
        readonly Vector3[] stainSizes = new Vector3[5];
        MaterialPropertyBlock stainColor;
        int previousItem = -1;
        float previousProgress, phase, soundCooldown;
        public bool Active { get; private set; }
        public bool OwnsFood { get; private set; }
        public float CleanRatio { get; private set; }
        public int ScrubCount { get; private set; }

        // Coordinates are in the actual sink's local space, including its half-width X scale.
        public static Vector3 BasinPlatePoint(Transform station) {
            var bottom = station.Find("Basin bottom");
            return bottom ? bottom.localPosition + Vector3.up * (bottom.localScale.y * .5f + .012f) : new Vector3(-.3f, 1.047f, 0);
        }
        void Awake() {
            stainColor = new MaterialPropertyBlock();
            root = FeedbackArt.Group("Washing feedback", transform);
            root.localPosition = BasinPlatePoint(transform);
            sound = root.gameObject.AddComponent<AudioSource>();
            sound.spatialBlend = 1; sound.minDistance = 1.5f; sound.maxDistance = 12; sound.rolloffMode = AudioRolloffMode.Linear;
            plate = FeedbackArt.Group("Washing plate", root).gameObject;
            // A .30m round plate fits the .34m-wide basin after the furnishing's X scale.
            FeedbackArt.Disc("Glazed plate rim", plate.transform, Vector3.zero, new Vector3(.6f, .023f, .3f), "FFF1D1");
            FeedbackArt.Disc("Plate center", plate.transform, new Vector3(0, .023f, 0), new Vector3(.49f, .003f, .245f), "F5F1E8");
            for (int n = 0; n < stains.Length; n++) {
                float a = n * 2.399f;
                stainSizes[n] = new Vector3(.115f + n % 2 * .045f, .003f, .055f + n % 3 * .012f);
                stains[n] = FeedbackArt.Disc("Washable sauce stain", plate.transform, new Vector3(Mathf.Cos(a) * .105f, .027f, Mathf.Sin(a) * .065f), stainSizes[n], "89513B");
            }
            sponge = FeedbackArt.Group("Scrubbing sponge", root);
            FeedbackArt.Box("Yellow sponge", sponge, new Vector3(0, .022f, 0), new Vector3(.2f, .043f, .10f), "E8C34A");
            FeedbackArt.Box("Green scourer", sponge, Vector3.zero, new Vector3(.2f, .009f, .10f), "317E79");
            for (int n = 0; n < 4; n++) FeedbackArt.Box("Sponge pore", sponge, new Vector3(-.055f + n * .036f, .045f, .01f * (n % 2)), new Vector3(.012f, .003f, .012f), "CA9B53");
            water = FeedbackArt.Disc("Running water", root, new Vector3(0, .054f, -.03f), new Vector3(.035f, 1.36f - root.localPosition.y - .054f, .021f), "A6DCE6");
            for (int n = 0; n < bubbles.Length; n++) bubbles[n] = FeedbackArt.Facet("Wash bubble", root, Vector3.zero, Vector3.one * .02f, n % 2 == 0 ? "F5F1E8" : "A6DCE6");
            Stop(); plate.SetActive(false);
        }
        internal void Present(KitchenStation station, KitchenItem item, bool accepted, bool paused, float dt) {
            OwnsFood = item != null && item.Kind == KitchenItemKind.DirtyPlate;
            CleanRatio = OwnsFood ? Mathf.Clamp01(station.Progress / KitchenState.WashSeconds) : 0;
            bool same = item != null && item.Id == previousItem;
            bool activeBefore = Active;
            Active = OwnsFood && !paused && accepted && !string.IsNullOrEmpty(station.WorkOwner)
                && (same ? station.Progress > previousProgress : station.Progress > 0);
            plate.SetActive(OwnsFood);
            ColorUtility.TryParseHtmlString("#89513B", out var dirt);
            ColorUtility.TryParseHtmlString("#F5F1E8", out var glaze);
            var color = Color.Lerp(dirt, glaze, CleanRatio);
            stainColor.SetColor("_BaseColor", color); stainColor.SetColor("_Color", color);
            for (int n = 0; n < stains.Length; n++) {
                stains[n].SetActive(OwnsFood && CleanRatio < .995f);
                float left = Mathf.Max(.001f, 1 - CleanRatio);
                stains[n].transform.localScale = Vector3.Scale(stainSizes[n], new Vector3(left, 1, left));
                stains[n].GetComponent<Renderer>().SetPropertyBlock(stainColor);
            }
            if (Active) {
                soundCooldown -= Mathf.Max(0, dt);
                if (soundCooldown <= 0) { sound.PlayOneShot(SoundFx.Wash, .6f); soundCooldown = .45f; }
                float before = phase;
                phase += Mathf.Max(0, dt) * 4;
                if (!activeBefore) ScrubCount++;
                ScrubCount += Mathf.Max(0, Mathf.FloorToInt(phase) - Mathf.FloorToInt(before));
                sponge.localPosition = new Vector3(Mathf.Sin(phase * Mathf.PI * 2) * .16f, .03f, Mathf.Sin(phase * Mathf.PI) * .065f);
                sponge.localRotation = Quaternion.Euler(0, Mathf.Sin(phase * 2) * 24, 0);
                water.SetActive(true);
                for (int n = 0; n < bubbles.Length; n++) {
                    float a = n * 2.399f + phase * .35f;
                    bubbles[n].SetActive(true);
                    bubbles[n].transform.localPosition = new Vector3(Mathf.Cos(a) * .19f, .035f + Mathf.Abs(Mathf.Sin(phase * 3 + n)) * .018f, Mathf.Sin(a) * .10f);
                    bubbles[n].transform.localScale = new Vector3(.032f, .017f, .016f) * (.75f + .25f * Mathf.Sin(phase * 4 + n));
                }
            } else Stop();
            previousItem = item?.Id ?? -1; previousProgress = station.Progress;
        }
        void Stop() {
            Active = false;
            soundCooldown = 0;
            if (sound && sound.isPlaying) sound.Stop();
            if (sponge) { sponge.localPosition = new Vector3(.27f, .022f, .17f); sponge.localRotation = Quaternion.Euler(0, 18, 0); }
            if (water) water.SetActive(false);
            foreach (var bubble in bubbles) if (bubble) bubble.SetActive(false);
        }
        void OnDisable() { Stop(); }
    }

    public sealed class BinFeedback : MonoBehaviour {
        Transform lidPivot;
        readonly GameObject[] refuse = new GameObject[6];
        float motionAge = 10;
        public int FillLevel { get; private set; }
        public float FillRatio => FillLevel / 6f;
        public float LidAmount { get; private set; }
        public bool LidOpen => LidAmount > .15f;
        void Awake() {
            var root = FeedbackArt.Group("Bin feedback", transform);
            lidPivot = FeedbackArt.Group("Bin lid hinge", root);
            lidPivot.localPosition = new Vector3(0, .8f, -.33f);
            var lid = transform.Find("Bin lid");
            if (lid) { lid.SetParent(lidPivot, false); lid.localPosition = new Vector3(0, 0, .33f); }
            else FeedbackArt.Disc("Bin lid", lidPivot, new Vector3(0, 0, .33f), new Vector3(.7f, .06f, .7f), "C8D6D2");
            for (int n = 0; n < refuse.Length; n++) {
                // Small scraps peek over the front rim even when the lid has settled closed.
                float a = (n - 2.5f) * .37f;
                refuse[n] = FeedbackArt.Facet("Discarded kitchen scrap", root, new Vector3(Mathf.Sin(a) * .31f, .837f, Mathf.Cos(a) * .335f), new Vector3(.10f, .07f, .11f), n % 3 == 0 ? "CA9B53" : n % 3 == 1 ? "73A566" : "89513B");
                refuse[n].transform.localRotation = Quaternion.Euler(n * 11, n * 47, n * 13);
                refuse[n].SetActive(false);
            }
        }
        internal void Present(KitchenStation station, bool discarded, bool emptied, bool paused, float dt) {
            FillLevel = Mathf.Clamp(station.WasteCount, 0, 6);
            for (int n = 0; n < refuse.Length; n++) refuse[n].SetActive(n < FillLevel);
            if (paused) { SetLid(FillLevel == 6 ? .13f : 0); motionAge = 10; return; }
            if (discarded || emptied) motionAge = 0;
            motionAge += Mathf.Max(0, dt);
            float open = motionAge < .13f ? Mathf.SmoothStep(0, 1, motionAge / .13f)
                : motionAge < .35f ? 1 : 1 - Mathf.SmoothStep(0, 1, (motionAge - .35f) / .55f);
            SetLid(Mathf.Max(FillLevel == 6 ? .13f : 0, open));
        }
        void SetLid(float amount) { LidAmount = Mathf.Clamp01(amount); if (lidPivot) lidPivot.localRotation = Quaternion.Euler(-78 * LidAmount, 0, 0); }
        void OnDisable() { motionAge = 10; SetLid(FillLevel == 6 ? .13f : 0); }
    }

    public partial class RestaurantController {
        readonly HashSet<int> acceptedWashes = new HashSet<int>(), acceptedDiscards = new HashSet<int>(), acceptedBinEmpties = new HashSet<int>();
        void AcceptedWash(int stationId) { acceptedWashes.Add(stationId); }
        void AcceptedDiscard(int stationId) { acceptedDiscards.Add(stationId); }
        void AcceptedEmptyBin(int stationId) { acceptedBinEmpties.Add(stationId); }
        void TickWashBinFeedback(float dt) {
            var events = Game.State.Events;
            // Late-frame work receipts can arrive after GameFeel.Update. Drain only our own events.
            if (events != null) for (int n = events.Count - 1; n >= 0; n--) {
                var receipt = events[n]; if (string.IsNullOrEmpty(receipt)) continue;
                int colon = receipt.IndexOf(':'); if (colon < 0) continue;
                string kind = receipt.Substring(0, colon);
                if (kind != "wash" && kind != "discard" && kind != "emptybin") continue;
                if (int.TryParse(receipt.Substring(colon + 1), out int id)) {
                    if (kind == "wash") AcceptedWash(id); else if (kind == "discard") AcceptedDiscard(id); else AcceptedEmptyBin(id);
                }
                events.RemoveAt(n);
            }
            bool paused = Game.Paused || ManagementPauses || PlacementActive;
            foreach (var station in Game.State.Kitchen.Stations) {
                if (station.CatalogId != "sink" && station.CatalogId != "trash") continue;
                var obj = StationObject(station.InstanceId); if (!obj || !obj.activeInHierarchy) continue;
                if (station.CatalogId == "sink") {
                    var view = obj.GetComponent<WashFeedback>(); if (!view) view = obj.AddComponent<WashFeedback>();
                    var item = Game.State.Kitchen.At(station.InstanceId);
                    view.Present(station, item, acceptedWashes.Contains(station.InstanceId), paused, dt);
                    if (view.OwnsFood && item != null && physicalItems.TryGetValue(item.Id, out var food) && food) food.SetActive(false);
                } else {
                    var view = obj.GetComponent<BinFeedback>(); if (!view) view = obj.AddComponent<BinFeedback>();
                    view.Present(station, acceptedDiscards.Contains(station.InstanceId), acceptedBinEmpties.Contains(station.InstanceId), paused, dt);
                }
            }
            acceptedWashes.Clear(); acceptedDiscards.Clear(); acceptedBinEmpties.Clear();
        }
    }

    // Three shared original meshes; renderer-only objects never introduce interaction colliders.
    static class FeedbackArt {
        static Mesh cube, disc, facet;
        internal static Transform Group(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform; }
        static GameObject Shape(string name, Transform parent, Vector3 position, Vector3 scale, string color, Mesh mesh) {
            var root = Group(name, parent); root.localPosition = position; root.localScale = scale;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = KitchenArt.Material(color);
            return root.gameObject;
        }
        internal static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, string color) {
            if (!cube) {
                var v = new List<Vector3>(); var t = new List<int>();
                var a = new Vector3(-.5f, -.5f, -.5f); var b = new Vector3(.5f, -.5f, -.5f); var c = new Vector3(.5f, .5f, -.5f); var d = new Vector3(-.5f, .5f, -.5f);
                var e = new Vector3(-.5f, -.5f, .5f); var f = new Vector3(.5f, -.5f, .5f); var g = new Vector3(.5f, .5f, .5f); var h = new Vector3(-.5f, .5f, .5f);
                Quad(v,t,a,d,c,b); Quad(v,t,e,f,g,h); Quad(v,t,a,e,h,d); Quad(v,t,b,c,g,f); Quad(v,t,d,h,g,c); Quad(v,t,a,b,f,e);
                cube = Mesh("Feedback sponge block", v, t);
            }
            return Shape(name,parent,position,scale,color,cube);
        }
        internal static GameObject Disc(string name, Transform parent, Vector3 position, Vector3 scale, string color) {
            if (!disc) {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int n = 0; n < 16; n++) {
                    float a = n * Mathf.PI / 8, b = (n + 1) * Mathf.PI / 8;
                    var p = new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f); var q = new Vector3(Mathf.Cos(b) * .5f, 0, Mathf.Sin(b) * .5f);
                    Quad(v,t,p,p+Vector3.up,q+Vector3.up,q);
                    Quad(v,t,Vector3.up,q+Vector3.up,p+Vector3.up,Vector3.up); Quad(v,t,Vector3.zero,p,q,Vector3.zero);
                }
                disc = Mesh("Feedback faceted disc",v,t);
            }
            return Shape(name,parent,position,scale,color,disc);
        }
        internal static GameObject Facet(string name, Transform parent, Vector3 position, Vector3 scale, string color) {
            if (!facet) {
                var v = new List<Vector3>(); var t = new List<int>();
                var ring = new[] { Vector3.forward * .5f, Vector3.right * .5f, Vector3.back * .5f, Vector3.left * .5f };
                for (int n=0;n<4;n++) { Quad(v,t,Vector3.up*.5f,ring[n],ring[(n+1)%4],Vector3.up*.5f); Quad(v,t,Vector3.down*.5f,ring[(n+1)%4],ring[n],Vector3.down*.5f); }
                facet = Mesh("Feedback crumpled facet",v,t);
            }
            return Shape(name,parent,position,scale,color,facet);
        }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3); }
        static Mesh Mesh(string name, List<Vector3> v, List<int> t) { var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh; }
    }
}
