using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RestaurantCity {
    // Hands-on dish washing (Schedule I style): at a sink, E lifts the dirty plate up in front of you and you scrub the
    // grime off with the mouse or a stick. Only moving the sponge over dirt cleans it. This is input and presentation
    // only: cleaning turns into KitchenState.Work seconds, so the kitchen model stays the one authority over plates
    // (no duplicate plates, a second player can't wash the same plate, partial progress survives putting it down).
    public sealed class PlateScrub : MonoBehaviour {
        public FirstPersonPlayer Player;
        public bool Active => station >= 0;
        public float CleanRatio { get; private set; }
        public int StationId => station;
        public bool HandsBusy => station != -1;   // scrubbing, or lowering a finished plate: the fists stay hidden

        const int Res = 160;
        const float Diameter = .30f, Brush = .085f, Rate = 4.2f, DoneAt = .96f, Speck = .05f;
        static readonly Dictionary<int, PlateScrub> holders = new Dictionary<int, PlateScrub>();
        public static bool HeldAt(int stationId) => holders.TryGetValue(stationId, out var s) && s && s.Active;
        public static PlateScrub Of(FirstPersonPlayer p) { if (!p) return null; var s = p.GetComponent<PlateScrub>(); if (!s) { s = p.gameObject.AddComponent<PlateScrub>(); s.Player = p; } return s; }

        int station = -1, itemId = -1;
        float startProgress, duration, initialDirt, lift, finishTimer, soundCooldown, speed;
        bool finished, texDirty;
        Vector2 spongeUv = new Vector2(.66f, .38f);
        Texture2D tex; Color32[] pixels; float[] dirt, foam; Color[] dirtColor; Material faceMat;
        Transform rig, plate, sponge; AudioSource sound;
        readonly List<(Transform t, float age, Vector3 drift)> bubbles = new List<(Transform, float, Vector3)>();
        static readonly Color Glaze = new Color(.86f, .84f, .79f), Band = new Color(.42f, .66f, .71f), Foam = new Color(.85f, .93f, .97f);
        static Material skin, cuff;

        CityGame Game => Player ? Player.Game : null;

        // Start scrubbing the dirty plate in this sink. Fails (and says why) if someone else is already washing it.
        public bool Begin(int stationId, out string message) {
            message = "";
            var g = Game.State; var k = g.Kitchen; var s = k.Stations.Find(x => x.InstanceId == stationId); var item = k.At(stationId);
            if (Active || s == null || item == null || item.Kind != KitchenItemKind.DirtyPlate) return false;
            if (k.Hold(Player.ActorId) != null) { message = "Free your hands to wash."; return false; }
            if (!string.IsNullOrEmpty(s.WorkOwner) && s.WorkOwner != Player.ActorId) { message = "Someone else is washing that plate."; return false; }
            station = stationId; itemId = item.Id; holders[stationId] = this; s.WorkOwner = Player.ActorId;   // claim the sink: nobody else works this plate while it is in your hands
            duration = KitchenState.WashDuration(g, stationId); startProgress = Mathf.Min(s.Progress, duration * .9f);
            finished = false; finishTimer = 0; lift = 0; speed = 0; spongeUv = new Vector2(.66f, .38f);
            MakeDirt(item.Id, 1 - startProgress / duration);
            BuildRig();
            
            return true;
        }

        // Put it back down: the plate stays in the sink with whatever progress was made.
        public void End(string message = null) {
            if (station >= 0) { holders.Remove(station); Game.State.Kitchen.ReleaseWork(Player.ActorId); }
            station = -1; itemId = -1;
            if (rig) Destroy(rig.gameObject); rig = null; bubbles.Clear();
            
            if (!string.IsNullOrEmpty(message)) Game.Restaurant?.Feedback(message);
        }

        void OnDisable() { if (Active) End(); }

        public void Tick(Keyboard keys, Mouse mouse, Gamepad pad, float dt) {
            if (!Active) return;
            var g = Game.State; var k = g.Kitchen; var s = k.Stations.Find(x => x.InstanceId == station); var item = k.At(station);
            lift = Mathf.MoveTowards(lift, 1, dt * 5f);
            if (s == null || item == null || item.Id != itemId) { End(); return; }
            if (keys != null && (keys.qKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame) || pad != null && pad.buttonEast.wasPressedThisFrame) {
                End(CleanRatio > .02f ? "Plate back in the sink, " + Mathf.RoundToInt(CleanRatio * 100) + "% clean." : "Plate back in the sink."); return;
            }
            // Move the sponge: the mouse moves it directly; either stick pushes it around (stick scrubbing is a little stronger).
            Vector2 delta = Vector2.zero; float power = 1;
            if (mouse != null) delta += mouse.delta.ReadValue() * .0021f;
            if (pad != null) { var stick = pad.rightStick.ReadValue() + pad.leftStick.ReadValue(); if (stick.sqrMagnitude > .02f) { delta += Vector2.ClampMagnitude(stick, 1) * 3.4f * dt; power = 1.3f; } }
            if (lift < .9f) delta = Vector2.zero;   // still lifting it out of the sink
            delta = Vector2.ClampMagnitude(delta, .35f);
            var from = spongeUv; var to = from + delta;
            var c = to - new Vector2(.5f, .5f); if (c.magnitude > .46f) to = new Vector2(.5f, .5f) + c.normalized * .46f;
            spongeUv = to;
            float moved = (to - from).magnitude;
            speed = Mathf.Lerp(speed, dt > 0 ? moved / dt : 0, .35f);
            if (moved > .0005f) Scrub(from, to, power);
            // Turn cleaning into authoritative work: progress tracks how clean the plate is.
            float target = startProgress + (duration - startProgress) * Mathf.Clamp01(CleanRatio / DoneAt);
            if (CleanRatio >= DoneAt) {
                float need = duration - s.Progress + .01f;
                if (k.Work(g, Player.ActorId, station, need, out var done)) {
                    finished = true; holders.Remove(station);
                    for (int i = 0; i < dirt.Length; i++) dirt[i] = 0; texDirty = true;
                    sound.PlayOneShot(SoundFx.Clink, .7f); sound.PlayOneShot(SoundFx.Tip, .45f);
                    Game.Restaurant?.Feedback("Sparkling! " + done);
                    Game.Restaurant?.SetPrompt(Player.ActorId, "");
                    station = -2;   // keep the visuals for the lowering animation (Update); control returns to the player now
                } else { End(done); return; }
            } else if (target - s.Progress > .0001f) {
                if (!k.Work(g, Player.ActorId, station, target - s.Progress, out var why)) { End(why); return; }
            }
            if (station >= 0) {
                int pct = Mathf.RoundToInt(CleanRatio / DoneAt * 100); int bars = Mathf.Clamp(pct / 10, 0, 10);
                Game.Restaurant?.SetPrompt(Player.ActorId, "Scrub the grime off!  " + (mouse != null ? "Move the mouse" : "Move a stick") + " over the dirt\n<color=#4FCB7A>" + new string('|', bars) + "</color>" + new string('.', 10 - bars) + "  " + Mathf.Min(99, pct) + "% clean     Q / B  put it down");
            }
            Present(dt, moved);
        }

        // Finished plates keep their rig alive for a moment (station == -2) so the plate can drop out of view.
        void Update() { if (station == -2 && Player && !Game.Paused) { finishTimer += Time.deltaTime; lift = Mathf.MoveTowards(lift, 0, Time.deltaTime * 2.2f); Present(Time.deltaTime, 0); if (finishTimer > .7f) { station = -1; if (rig) Destroy(rig.gameObject); rig = null; bubbles.Clear();  } } }

        // Tests and snapshots: drag the sponge along a path as if the player scrubbed it (then Tick turns it into work).
        public void ScrubPath(params Vector2[] path) { lift = 1; speed = 3; for (int i = 0; i + 1 < path.Length; i++) Scrub(path[i], path[i + 1], 1); spongeUv = path[path.Length - 1]; Present(0, 0); }
        void Scrub(Vector2 from, Vector2 to, float power) {
            float len = (to - from).magnitude; int steps = Mathf.Max(1, Mathf.CeilToInt(len / .01f)); float stepLen = len / steps;
            int rad = Mathf.CeilToInt(Brush * Res);
            for (int n = 1; n <= steps; n++) {
                var p = Vector2.Lerp(from, to, n / (float)steps); int cx = Mathf.RoundToInt(p.x * Res), cy = Mathf.RoundToInt(p.y * Res);
                for (int y = Mathf.Max(0, cy - rad); y < Mathf.Min(Res, cy + rad + 1); y++)
                    for (int x = Mathf.Max(0, cx - rad); x < Mathf.Min(Res, cx + rad + 1); x++) {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / Res; if (d > Brush) continue;
                        float w = 1 - d / Brush; w = w * w * (3 - 2 * w);
                        int i = y * Res + x;
                        if (dirt[i] > 0) dirt[i] = Mathf.Max(0, dirt[i] - Rate * power * w * stepLen);
                        foam[i] = Mathf.Min(1, foam[i] + w * stepLen * 9);
                    }
            }
            float left = 0; for (int i = 0; i < dirt.Length; i++) if (dirt[i] > Speck) left += dirt[i];
            CleanRatio = initialDirt <= 0 ? 1 : Mathf.Clamp01(1 - left / initialDirt);
            texDirty = true;
            // Suds and the scrub sound follow how hard you're going.
            soundCooldown -= Time.deltaTime;
            if (soundCooldown <= 0 && speed > .4f) { sound.pitch = Random.Range(.9f, 1.15f); sound.PlayOneShot(SoundFx.Wash, Mathf.Clamp(.25f + speed * .12f, .25f, .8f)); soundCooldown = .18f; }
            if (bubbles.Count < 26 && Random.value < Mathf.Clamp01(speed * .35f)) {
                var b = FeedbackArt.Facet("Suds", plate, UvToLocal(spongeUv) + new Vector3(Random.Range(-.03f, .03f), .02f, Random.Range(-.03f, .03f)), Vector3.one * Random.Range(.008f, .016f), Random.value < .6f ? "F5F8FA" : "CFE9F0");
                b.layer = rig.gameObject.layer; bubbles.Add((b.transform, 0, new Vector3(Random.Range(-.04f, .04f), Random.Range(.02f, .05f), Random.Range(.03f, .08f))));
            }
        }

        void Present(float dt, float moved) {
            if (!rig) return;
            // Lift the plate up out of the sink, then hold it a little left of centre with a slight wobble.
            float e = 1 - (1 - lift) * (1 - lift);
            rig.localPosition = Vector3.Lerp(new Vector3(.02f, -.42f, .42f), new Vector3(-.015f, -.055f, .40f), e);
            rig.localRotation = Quaternion.Euler(Mathf.Lerp(-10, -74, e) + Mathf.Sin(Time.time * 7) * speed * .25f, Mathf.Sin(Time.time * 9) * Mathf.Min(2.5f, speed * .6f), Mathf.Lerp(-18, -4, e));
            // The sponge rides the plate, squashes and twists as you scrub.
            float squash = Mathf.Clamp01(speed * .15f);
            sponge.localPosition = UvToLocal(spongeUv) + Vector3.up * (.016f - squash * .004f);
            sponge.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 18) * 14 * squash - 20, 0);
            sponge.localScale = new Vector3(1 + squash * .08f, 1 - squash * .2f, 1 + squash * .08f);
            for (int i = 0; i < foam.Length; i++) if (foam[i] > 0) { foam[i] = Mathf.Max(0, foam[i] - dt * .45f); texDirty = true; }
            for (int n = bubbles.Count - 1; n >= 0; n--) {
                var (t, age, drift) = bubbles[n]; age += dt;
                if (!t || age > .9f) { if (t) Destroy(t.gameObject); bubbles.RemoveAt(n); continue; }
                t.localPosition += drift * dt; t.localScale *= 1 - dt * .9f; bubbles[n] = (t, age, drift);
            }
            if (texDirty) Paint();
        }

        Vector3 UvToLocal(Vector2 uv) => new Vector3((uv.x - .5f) * Diameter, 0, (uv.y - .5f) * Diameter);

        // Grime: sauce smears, ketchup, grease, a couple of baked-on spots (take extra passes) and crumbs.
        void MakeDirt(int seed, float amount) {
            int n = Res * Res; dirt = new float[n]; foam = new float[n]; dirtColor = new Color[n]; pixels = new Color32[n];
            var weight = new float[n]; var rnd = new System.Random(seed * 7919 + 13);
            float R() => (float)rnd.NextDouble();
            var palette = new[] { new Color(.54f, .29f, .18f), new Color(.63f, .2f, .14f), new Color(.72f, .57f, .23f), new Color(.36f, .24f, .14f) };
            void Blob(Vector2 at, float radius, float stretch, float angle, float strength, Color col) {
                float a1 = R() * 6.28f, a2 = R() * 6.28f; int rad = Mathf.CeilToInt(radius * Res * Mathf.Max(1, stretch) * 1.4f);
                int cx = Mathf.RoundToInt(at.x * Res), cy = Mathf.RoundToInt(at.y * Res); float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
                for (int y = Mathf.Max(0, cy - rad); y < Mathf.Min(Res, cy + rad + 1); y++)
                    for (int x = Mathf.Max(0, cx - rad); x < Mathf.Min(Res, cx + rad + 1); x++) {
                        float dx = (x - cx) / (float)Res, dy = (y - cy) / (float)Res;
                        float u = (dx * ca + dy * sa) / stretch, v = -dx * sa + dy * ca;
                        float th = Mathf.Atan2(v, u), edge = radius * (1 + .25f * Mathf.Sin(3 * th + a1) + .14f * Mathf.Sin(5 * th + a2));
                        float r = Mathf.Sqrt(u * u + v * v); if (r > edge) continue;
                        float w = Mathf.Clamp01((edge - r) / (edge * .35f)) * strength;
                        int i = y * Res + x; dirt[i] = Mathf.Min(1.8f, dirt[i] + w); dirtColor[i] += col * w; weight[i] += w;
                    }
            }
            Vector2 Spot(float maxR) { float a = R() * 6.28f, r = Mathf.Sqrt(R()) * maxR; return new Vector2(.5f + Mathf.Cos(a) * r, .5f + Mathf.Sin(a) * r); }
            int smears = 5 + rnd.Next(3);
            for (int b = 0; b < smears; b++) Blob(Spot(.32f), .06f + R() * .08f, 1 + R() * 1.4f, R() * 3.14f, .75f + R() * .3f, palette[rnd.Next(3)]);
            for (int b = 0; b < 2; b++) Blob(Spot(.3f), .035f + R() * .025f, 1, 0, 1.7f, palette[0] * .7f);          // baked-on
            for (int b = 0; b < 28; b++) Blob(Spot(.42f), .008f + R() * .01f, 1, 0, 1f, palette[3]);                   // crumbs
            initialDirt = 0;
            for (int i = 0; i < n; i++) {
                dirtColor[i] = weight[i] > 0 ? dirtColor[i] / weight[i] : palette[0];
                dirt[i] *= Mathf.Clamp01(amount); if (dirt[i] > Speck) initialDirt += dirt[i];
            }
            CleanRatio = 0;
            if (!tex) { tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { name = "Plate grime", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear }; }
            texDirty = true; Paint();
        }

        void Paint() {
            texDirty = false;
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++) {
                    int i = y * Res + x; float dx = x / (float)Res - .5f, dy = y / (float)Res - .5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    // A diner plate: cream glaze, a teal band near the rim, a soft shadowed well in the middle.
                    Color c = Glaze * (r < .3f ? .985f : 1);
                    if (r > .405f && r < .422f) c = Band;
                    if (r > .46f) c *= .96f;
                    c = Color.Lerp(c, dirtColor[i], Mathf.Clamp01(dirt[i]) * .93f);
                    c = Color.Lerp(c, Foam, foam[i] * .55f);
                    if (finished) c = Color.Lerp(c, Color.white, .25f);
                    pixels[i] = c;
                }
            tex.SetPixels32(pixels); tex.Apply(false);
        }

        void BuildRig() {
            if (rig) Destroy(rig.gameObject);
            if (!sound) { sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 0; sound.playOnAwake = false; }
            rig = new GameObject("Plate scrub").transform; rig.SetParent(Player.View.transform, false);
            plate = new GameObject("Held plate").transform; plate.SetParent(rig, false);
            if (!faceMat) { faceMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Plate grime face" }; faceMat.SetFloat("_Smoothness", .32f); faceMat.SetFloat("_SpecularHighlights", 0); faceMat.SetFloat("_EnvironmentReflections", 0); }
            faceMat.mainTexture = tex; faceMat.SetTexture("_BaseMap", tex); faceMat.color = Color.white;
            var face = new GameObject("Plate face", typeof(MeshFilter), typeof(MeshRenderer)); face.transform.SetParent(plate, false);
            face.GetComponent<MeshFilter>().sharedMesh = PlateMesh(); face.GetComponent<MeshRenderer>().sharedMaterial = faceMat;
            face.transform.localScale = new Vector3(Diameter, 1, Diameter);
            var rim = new GameObject("Plate rim", typeof(MeshFilter), typeof(MeshRenderer)); rim.transform.SetParent(plate, false);
            rim.GetComponent<MeshFilter>().sharedMesh = RimMesh(); rim.GetComponent<MeshRenderer>().sharedMaterial = KitchenArt.Material("D9D0BC");
            rim.transform.localScale = new Vector3(Diameter, Diameter, Diameter);
            // Sponge in the right hand.
            sponge = new GameObject("Sponge").transform; sponge.SetParent(plate, false);
            FeedbackArt.Box("Green scourer", sponge, new Vector3(0, .004f, 0), new Vector3(.08f, .008f, .055f), "317E79");
            FeedbackArt.Box("Yellow sponge", sponge, new Vector3(0, .022f, 0), new Vector3(.08f, .028f, .055f), "E8C34A");
            for (int n = 0; n < 4; n++) FeedbackArt.Box("Sponge pore", sponge, new Vector3(-.026f + n * .017f, .037f, .008f * (n % 2 * 2 - 1)), new Vector3(.007f, .002f, .007f), "CA9B53");
            if (!skin) { skin = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.85f, .64f, .5f) }; skin.SetFloat("_Smoothness", .25f); cuff = KitchenArt.Material("F2EBD9"); }
            var sleeve = KitchenArt.Material(Player.PlayerId == 0 ? "C74D38" : "3373C7");
            Hand(sponge, new Vector3(.004f, .05f, -.004f), new Vector3(.45f, .3f, -.95f), sleeve, true);
            // Left hand grips the rim at the bottom left, thumb over the edge.
            var grip = new GameObject("Left grip").transform; grip.SetParent(plate, false); grip.localPosition = new Vector3(-.11f, 0, -.105f);
            Hand(grip, new Vector3(0, -.012f, 0), new Vector3(-.5f, .25f, -.95f), sleeve, false);
            FeedbackArt.Box("Thumb", grip, new Vector3(.018f, .012f, .016f), new Vector3(.022f, .014f, .046f), "D9A380").GetComponent<Renderer>().sharedMaterial = skin;
            int layer = 25 + Player.PlayerId;
            foreach (var t in rig.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Present(0, 0);
        }

        // A blocky hand like the fists, with the forearm running back toward the bottom corner of the screen.
        void Hand(Transform parent, Vector3 at, Vector3 armDir, Material sleeve, bool right) {
            var palm = FeedbackArt.Box(right ? "Right hand" : "Left hand", parent, at, new Vector3(.05f, .026f, .058f), "D9A380"); palm.GetComponent<Renderer>().sharedMaterial = skin;
            var dir = armDir.normalized;
            var wrist = FeedbackArt.Box("Cuff", parent, at + dir * .05f, new Vector3(.05f, .05f, .02f), "F2EBD9"); wrist.transform.localRotation = Quaternion.LookRotation(dir, Vector3.forward); wrist.GetComponent<Renderer>().sharedMaterial = cuff;
            var arm = FeedbackArt.Box("Sleeve", parent, at + dir * .16f, new Vector3(.055f, .055f, .2f), "C74D38"); arm.transform.localRotation = Quaternion.LookRotation(dir, Vector3.forward); arm.GetComponent<Renderer>().sharedMaterial = sleeve;
        }

        static Mesh face, rimMesh;
        static Mesh PlateMesh() {
            if (face) return face;
            const int seg = 48; var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { new Vector2(.5f, .5f) }; var t = new List<int>();
            for (int n = 0; n <= seg; n++) { float a = n * Mathf.PI * 2 / seg; var p = new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f); v.Add(p); uv.Add(new Vector2(p.x + .5f, p.z + .5f)); }
            for (int n = 1; n <= seg; n++) { t.Add(0); t.Add(n + 1); t.Add(n); }
            face = new Mesh { name = "Scrub plate face" }; face.SetVertices(v); face.SetUVs(0, uv); face.SetTriangles(t, 0); face.RecalculateNormals(); face.RecalculateBounds(); return face;
        }
        static Mesh RimMesh() {
            if (rimMesh) return rimMesh;
            const int seg = 48; var v = new List<Vector3>(); var t = new List<int>();
            float[] rr = { .5f, .56f, .58f, .5f }, yy = { 0, .05f, .035f, -.03f };   // inner lip up and out, top edge, then the underside back in
            for (int n = 0; n <= seg; n++) { float a = n * Mathf.PI * 2 / seg; for (int k = 0; k < rr.Length; k++) v.Add(new Vector3(Mathf.Cos(a) * rr[k], yy[k], Mathf.Sin(a) * rr[k])); }
            for (int n = 0; n < seg; n++) for (int k = 0; k < rr.Length - 1; k++) { int a = n * rr.Length + k, b = a + rr.Length; t.Add(a); t.Add(b + 1); t.Add(b); t.Add(a); t.Add(a + 1); t.Add(b + 1); }
            // Underside so the plate is solid from any angle while it lifts.
            int c = v.Count; v.Add(new Vector3(0, -.03f, 0)); for (int n = 0; n <= seg; n++) { float a = n * Mathf.PI * 2 / seg; v.Add(new Vector3(Mathf.Cos(a) * .5f, -.03f, Mathf.Sin(a) * .5f)); }
            for (int n = 1; n <= seg; n++) { t.Add(c); t.Add(c + n); t.Add(c + n + 1); }
            rimMesh = new Mesh { name = "Scrub plate rim" }; rimMesh.SetVertices(v); rimMesh.SetTriangles(t, 0); rimMesh.RecalculateNormals(); rimMesh.RecalculateBounds(); return rimMesh;
        }
    }
}
