using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RestaurantCity {
    // Hands-on chopping (the plate scrub's sibling): at a cutting board with a head of lettuce, E brings the board up
    // close. Slide the knife along the lettuce with the mouse or a stick and click (or RB / RT) to chop. Each slice must
    // be cut once, so you work your way along the head; chopping where it's already cut just thunks the board.
    // Input and presentation only: cuts turn into KitchenState.Work seconds, so the kitchen model stays the authority
    // (the chopper claims the board; partial progress survives putting it down).
    public sealed class PrepChop : MonoBehaviour {
        public FirstPersonPlayer Player;
        public bool Active => station >= 0;
        public bool HandsBusy => station != -1;   // chopping, or lowering a finished board
        public int Cuts { get; private set; }
        public const int Slices = 6;

        const float SliceGap = .04f, FirstSlice = -.1f, Reach = .022f, KnifeMin = -.14f, KnifeMax = .14f;
        static readonly Dictionary<int, PrepChop> holders = new Dictionary<int, PrepChop>();
        public static bool HeldAt(int stationId) => holders.TryGetValue(stationId, out var c) && c && c.Active;
        public static PrepChop Of(FirstPersonPlayer p) { if (!p) return null; var c = p.GetComponent<PrepChop>(); if (!c) { c = p.gameObject.AddComponent<PrepChop>(); c.Player = p; } return c; }

        int station = -1, itemId = -1;
        float duration, lift, knifeX = .12f, chopTime = -1, finishTimer, idleSway;
        readonly bool[] cut = new bool[Slices];
        Transform rig, board, knife; AudioSource sound;
        readonly GameObject[] whole = new GameObject[Slices], pieces = new GameObject[Slices];
        readonly List<(Transform t, float age, Vector3 v)> shreds = new List<(Transform, float, Vector3)>();
        static Material skin;
        CityGame Game => Player ? Player.Game : null;

        public static float Duration(GameState g, int stationId) => 3f * (g.FluxResearch ? .65f : 1f) * StationUpgrades.WorkScale(g.Restaurant.LevelOf(stationId));

        public bool Begin(int stationId, out string message) {
            message = "";
            var g = Game.State; var k = g.Kitchen; var s = k.Stations.Find(x => x.InstanceId == stationId); var item = k.At(stationId);
            if (Active || s == null || item == null || item.Kind != KitchenItemKind.RawGreens) return false;
            if (k.Hold(Player.ActorId) != null) { message = "Free your hands to chop."; return false; }
            if (!string.IsNullOrEmpty(s.WorkOwner) && s.WorkOwner != Player.ActorId) { message = "Someone else is chopping there."; return false; }
            station = stationId; itemId = item.Id; holders[stationId] = this; s.WorkOwner = Player.ActorId;
            duration = Duration(g, stationId);
            int already = Mathf.Clamp(Mathf.FloorToInt(s.Progress / duration * Slices + .001f), 0, Slices - 1);
            for (int i = 0; i < Slices; i++) cut[i] = i >= Slices - already;   // resume: the far end is already chopped
            Cuts = already; lift = 0; knifeX = .12f; chopTime = -1; finishTimer = 0;
            BuildRig();
            return true;
        }

        public void End(string message = null) {
            if (station >= 0) { holders.Remove(station); Game.State.Kitchen.ReleaseWork(Player.ActorId); }
            station = -1; itemId = -1;
            if (rig) Destroy(rig.gameObject); rig = null; shreds.Clear();
            if (!string.IsNullOrEmpty(message)) Game.Restaurant?.Feedback(message);
        }
        void OnDisable() { if (station != -1) End(); }

        public void Tick(Keyboard keys, Mouse mouse, Gamepad pad, float dt) {
            if (!Active) return;
            var g = Game.State; var k = g.Kitchen; var s = k.Stations.Find(x => x.InstanceId == station); var item = k.At(station);
            lift = Mathf.MoveTowards(lift, 1, dt * 5);
            if (s == null || item == null || item.Id != itemId) { End(); return; }
            if (keys != null && (keys.qKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame) || pad != null && pad.buttonEast.wasPressedThisFrame) {
                End(Cuts > 0 ? "Lettuce back on the board, " + Cuts + " of " + Slices + " slices cut." : "Lettuce back on the board."); return;
            }
            // Slide the knife along the head.
            if (lift > .9f) {
                if (mouse != null) knifeX += mouse.delta.ReadValue().x * .0016f;
                if (pad != null) { float sx = pad.rightStick.ReadValue().x + pad.leftStick.ReadValue().x; if (Mathf.Abs(sx) > .15f) knifeX += Mathf.Clamp(sx, -1, 1) * .32f * dt; }
                knifeX = Mathf.Clamp(knifeX, KnifeMin, KnifeMax);
                bool chop = mouse != null && mouse.leftButton.wasPressedThisFrame || pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame);
                if (chop && chopTime < 0) { chopTime = 0; Chop(); }
            }
            float target = duration * Cuts / (float)Slices;
            if (Cuts >= Slices) {
                if (k.Work(g, Player.ActorId, station, duration - s.Progress + .01f, out var done)) {
                    holders.Remove(station); station = -2; finishTimer = 0;
                    sound.PlayOneShot(SoundFx.Tip, .5f);
                    Game.Restaurant?.Feedback("Chopped! " + done);
                    Game.Restaurant?.SetPrompt(Player.ActorId, "");
                } else { End(done); return; }
            } else if (target - s.Progress > .0001f) {
                if (!k.Work(g, Player.ActorId, station, target - s.Progress, out var why)) { End(why); return; }
            }
            if (station >= 0) {
                int nearest = NearestUncut(knifeX);
                string aim = nearest >= 0 && Mathf.Abs(SliceX(nearest) - knifeX) <= Reach ? "<color=#4FCB7A>on a slice: chop!</color>" : "line the knife up with an uncut slice";
                Game.Restaurant?.SetPrompt(Player.ActorId, "Chop the lettuce!  " + (pad != null ? "Move a stick" : "Move the mouse") + " to slide the knife, " + (pad != null ? "RT / RB" : "click") + " to chop   " + aim
                    + "\n" + Cuts + " / " + Slices + " slices     " + (pad != null ? "B" : "Q") + "  put it down");
            }
            Present(dt);
        }

        void Update() {
            if (station != -2 || !Player || Game.Paused) return;
            finishTimer += Time.deltaTime; lift = Mathf.MoveTowards(lift, 0, Time.deltaTime * 2.4f); Present(Time.deltaTime);
            if (finishTimer > .65f) { station = -1; if (rig) Destroy(rig.gameObject); rig = null; shreds.Clear(); }
        }

        float SliceX(int i) => FirstSlice + i * SliceGap;
        int NearestUncut(float x) { int best = -1; float d = 9; for (int i = 0; i < Slices; i++) if (!cut[i] && Mathf.Abs(SliceX(i) - x) < d) { d = Mathf.Abs(SliceX(i) - x); best = i; } return best; }

        // Tests and snapshots: chop at board positions as if the player slid the knife there and clicked.
        public void ChopAt(params float[] xs) { lift = 1; foreach (var x in xs) { knifeX = Mathf.Clamp(x, KnifeMin, KnifeMax); Chop(); } }

        void Chop() {
            int n = NearestUncut(knifeX);
            if (n >= 0 && Mathf.Abs(SliceX(n) - knifeX) <= Reach) {
                cut[n] = true; Cuts++;
                if (sound) { sound.pitch = Random.Range(.92f, 1.1f); sound.PlayOneShot(SoundFx.Chop, .9f); }
                if (whole[n]) whole[n].SetActive(false);
                if (pieces[n]) pieces[n].SetActive(true);
                for (int i = 0; i < 4 && board; i++) {
                    var b = FeedbackArt.Facet("Lettuce shred", board, new Vector3(SliceX(n) + Random.Range(-.012f, .012f), .05f, Random.Range(-.03f, .03f)), Vector3.one * Random.Range(.008f, .014f), i % 2 == 0 ? "8CCB5E" : "5FA444");
                    b.layer = rig.gameObject.layer; shreds.Add((b.transform, 0, new Vector3(Random.Range(-.08f, .08f), Random.Range(.12f, .2f), Random.Range(-.06f, .06f))));
                }
            } else if (sound) { sound.pitch = .8f; sound.PlayOneShot(SoundFx.Thud, .5f); }
        }

        void Present(float dt) {
            if (!rig) return;
            float e = 1 - (1 - lift) * (1 - lift);
            rig.localPosition = Vector3.Lerp(new Vector3(0, -.45f, .45f), new Vector3(0, -.13f, .42f), e);
            rig.localRotation = Quaternion.Euler(Mathf.Lerp(-10, -46, e), 0, 0);
            // Knife: hovers over the board, snaps down on a chop and springs back up.
            float y = .085f;
            if (chopTime >= 0) { chopTime += dt; float t = chopTime / .17f; y = t < .35f ? Mathf.Lerp(.085f, .0f, t / .35f) : Mathf.Lerp(0, .085f, (t - .35f) / .65f); if (t >= 1) chopTime = -1; }
            idleSway += dt;
            knife.localPosition = new Vector3(knifeX, y + Mathf.Sin(idleSway * 2.2f) * .003f, -.005f);
            for (int i = shreds.Count - 1; i >= 0; i--) {
                var (t, age, v) = shreds[i]; age += dt;
                if (!t || age > .7f) { if (t) Destroy(t.gameObject); shreds.RemoveAt(i); continue; }
                v += Vector3.down * .55f * dt; t.localPosition += v * dt; if (t.localPosition.y < .02f) { var p = t.localPosition; p.y = .02f; t.localPosition = p; v = Vector3.zero; }
                shreds[i] = (t, age, v);
            }
        }

        void BuildRig() {
            if (rig) Destroy(rig.gameObject);
            if (!sound) { sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 0; sound.playOnAwake = false; }
            rig = new GameObject("Chop board").transform; rig.SetParent(Player.View.transform, false);
            board = new GameObject("Board").transform; board.SetParent(rig, false);
            FeedbackArt.Box("Cutting board", board, new Vector3(0, -.011f, 0), new Vector3(.38f, .022f, .22f), "B98B5A");
            FeedbackArt.Box("Board edge grain", board, new Vector3(0, .0005f, .095f), new Vector3(.37f, .001f, .006f), "A07548");
            FeedbackArt.Box("Board edge grain", board, new Vector3(0, .0005f, -.095f), new Vector3(.37f, .001f, .006f), "A07548");
            // The head of lettuce: six slices side by side, light leafy tops over a darker body.
            for (int i = 0; i < Slices; i++) {
                float x = SliceX(i), h = .05f + Mathf.Sin((i + .5f) / Slices * Mathf.PI) * .02f;
                var w = new GameObject("Lettuce slice " + i).transform; w.SetParent(board, false); w.localPosition = new Vector3(x, 0, 0);
                FeedbackArt.Box("Lettuce body", w, new Vector3(0, h * .5f, 0), new Vector3(SliceGap * .96f, h, .085f), i % 2 == 0 ? "4E9A3A" : "56A341");
                FeedbackArt.Facet("Leaf top", w, new Vector3(0, h + .004f, 0), new Vector3(SliceGap * 1.05f, .02f, .09f), "8CCB5E");
                FeedbackArt.Box("Core", w, new Vector3(0, h * .45f, 0), new Vector3(SliceGap * .97f, h * .5f, .03f), "C9E3A0");
                whole[i] = w.gameObject;
                var c = new GameObject("Chopped " + i).transform; c.SetParent(board, false); c.localPosition = new Vector3(x, 0, 0);
                for (int j = 0; j < 5; j++) {
                    var piece = FeedbackArt.Facet("Chopped lettuce", c, new Vector3(Random.Range(-.014f, .014f), .008f + j % 2 * .008f, Random.Range(-.035f, .035f)), new Vector3(.022f, .014f, .02f), j % 2 == 0 ? "8CCB5E" : "5FA444");
                    piece.transform.localRotation = Quaternion.Euler(Random.Range(0, 40), Random.Range(0, 360), Random.Range(0, 40));
                }
                pieces[i] = c.gameObject;
                whole[i].SetActive(!cut[i]); pieces[i].SetActive(cut[i]);
            }
            // Knife: the kitchen knife turned so the blade crosses the lettuce, handle toward you, in your right hand.
            knife = new GameObject("Knife pivot").transform; knife.SetParent(board, false);
            var k = KitchenArt.ChoppingKnife(knife).transform; k.localRotation = Quaternion.Euler(0, 90, 0); k.localPosition = Vector3.zero; k.localScale = Vector3.one * .72f;
            if (!skin) { skin = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.85f, .64f, .5f) }; skin.SetFloat("_Smoothness", .25f); }
            var sleeve = KitchenArt.Material(Player.PlayerId == 0 ? "C74D38" : "3373C7"); var cuff = KitchenArt.Material("F2EBD9");
            Hand(knife, new Vector3(0, .066f, -.145f), new Vector3(.35f, .45f, -.85f), sleeve, cuff);
            // Left hand steadies the far-left end of the head.
            var grip = new GameObject("Left hand").transform; grip.SetParent(board, false); grip.localPosition = new Vector3(FirstSlice - .065f, .06f, 0);
            Hand(grip, Vector3.zero, new Vector3(-.5f, .45f, -.8f), sleeve, cuff);
            int layer = 25 + Player.PlayerId;
            foreach (var t in rig.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var c in rig.GetComponentsInChildren<Collider>(true)) Destroy(c);
            Present(0);
        }

        static void Hand(Transform parent, Vector3 at, Vector3 armDir, Material sleeve, Material cuff) {
            var palm = FeedbackArt.Box("Hand", parent, at, new Vector3(.05f, .03f, .06f), "D9A380"); palm.GetComponent<Renderer>().sharedMaterial = skin;
            var dir = armDir.normalized;
            var wrist = FeedbackArt.Box("Cuff", parent, at + dir * .05f, new Vector3(.05f, .05f, .02f), "F2EBD9"); wrist.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up); wrist.GetComponent<Renderer>().sharedMaterial = cuff;
            var arm = FeedbackArt.Box("Sleeve", parent, at + dir * .16f, new Vector3(.055f, .055f, .2f), "C74D38"); arm.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up); arm.GetComponent<Renderer>().sharedMaterial = sleeve;
        }
    }
}
