using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Where things are inside The Alchemist (built by the editor's AlchemistBuilder from these same numbers).
    // Hall x 5..25, z 14..31.5; door at x 16.25 on the street side; the counter/pass at z 25.4; kitchen behind it.
    public static class AlchemistLayout {
        public static readonly Vector3 Centre = new Vector3(15, 0, 22);
        public static readonly Vector3 Street = new Vector3(16.25f, 0, 11.2f), Door = new Vector3(16.25f, 0, 15.6f);
        public static readonly Vector3 HeadCook = new Vector3(14.5f, 0, 29.05f);
        public static readonly Vector3[] Waiting = { new Vector3(13.6f, 0, 24.15f), new Vector3(15.4f, 0, 24.15f) };   // staff wait at the pass
        // Walking lanes: across the front, down the west aisle, the middle aisle, the east aisle, and along the counter.
        public const float FrontLane = 15.6f, WestLane = 8.0f, MidLane = 15.2f, EastLane = 22.1f, CounterLane = 23.6f;
        public const float TableTop = .87f;
        // Booth tables (x, z): three bays down the west wall, two down the east (the stairs take the rest).
        public static readonly Vector2[] WestBooths = { new Vector2(6.3f, 16.67f), new Vector2(6.3f, 19.6f), new Vector2(6.3f, 22.5f) };
        public static readonly Vector2[] EastBooths = { new Vector2(23.7f, 16.67f), new Vector2(23.7f, 19.6f) };
        public static readonly Vector2[] FloorTables = { new Vector2(10.5f, 17.2f), new Vector2(10.5f, 21.2f), new Vector2(20.2f, 17.4f), new Vector2(20.2f, 21.4f) };
        public struct Seat { public Vector3 At, Table; public float Yaw, Lane; }
        static Seat[] seats;
        public static Seat[] Seats {
            get {
                if (seats != null) return seats;
                var list = new List<Seat>();
                foreach (var b in WestBooths) { var t = new Vector3(b.x, 0, b.y); list.Add(S(new Vector3(6.2f, 0, b.y - .92f), 0, WestLane, t)); list.Add(S(new Vector3(6.2f, 0, b.y + .92f), 180, WestLane, t)); }
                foreach (var b in EastBooths) { var t = new Vector3(b.x, 0, b.y); list.Add(S(new Vector3(23.8f, 0, b.y - .92f), 0, EastLane, t)); list.Add(S(new Vector3(23.8f, 0, b.y + .92f), 180, EastLane, t)); }
                foreach (var f in FloorTables) {
                    var t = new Vector3(f.x, 0, f.y); bool west = f.x < 15;
                    list.Add(S(new Vector3(f.x - .8f, 0, f.y), 90, west ? WestLane : MidLane, t)); list.Add(S(new Vector3(f.x + .8f, 0, f.y), 270, west ? MidLane : EastLane, t));
                }
                return seats = list.ToArray();
            }
        }
        static Seat S(Vector3 at, float yaw, float lane, Vector3 table) => new Seat { At = at, Yaw = yaw, Lane = lane, Table = table };
    }

    // The Alchemist is open for business, so you can see what you're up against: the Alchemist himself works the bench
    // behind the counter, his stitched staff carry the Philosopher's Stack out to the tables, and residents come in,
    // eat it, and leave happy. Ambient only: nothing here touches money, orders or the save. It runs while a player is
    // near, stops seating new guests after dark (the crew stays, on guard), and clears out during a raid on it.
    public partial class RestaurantController {
        const float AlchemistNear = 45;
        class LabGuest { public GameObject Go; public CharacterMotion Motion; public TextMesh Bubble; public int Stage, Seat = -1; public float Timer; public bool Served, Claimed; public readonly Queue<Vector3> Path = new Queue<Vector3>(); }
        class LabWaiter { public GameObject Go; public CharacterMotion Motion; public ResidentAnimator Anim; public int Home; public LabGuest Guest; public GameObject Plate; public int Stage; public float Timer; public readonly Queue<Vector3> Path = new Queue<Vector3>(); }
        Transform labRoot; GameObject labBoss; readonly List<LabWaiter> labWaiters = new List<LabWaiter>(); readonly List<LabGuest> labGuests = new List<LabGuest>();
        readonly Dictionary<int, GameObject> labPlates = new Dictionary<int, GameObject>();
        float labNextGuest = 2; int labSeed; bool labWarm;
        // The Alchemist moves round his kitchen: grill, stove, fryer, prep table, and the bench at the pass, where he
        // plates every Philosopher's Stack his staff carry out.
        static readonly (float x, float yaw, string what)[] LabStations = { (10.5f, 0, "grill"), (12.4f, 0, "stove"), (14.5f, 180, "bench"), (16.6f, 0, "fryer"), (18.6f, 0, "prep") };
        const float KitchenLane = 29.3f;
        readonly Queue<Vector3> bossPath = new Queue<Vector3>(); int bossAt = 2, bossGoing = -1; float bossTimer = 3;
        bool BossAtBench => bossGoing < 0 && bossAt == 2;

        void BuildRivalInterior() { }   // built on demand, the first time someone walks near (see AnimateRival)

        GameObject LabCharacter(string model, string name, Vector3 at, float yaw) {
            var go = People.UseResidents ? ResidentModels.Create(new ResidentDef(model, name, ResidentCast.CustomResidentHeight, 0, StaffJob.Cook, ""), labRoot) : RestaurantArt.CreateCharacter(5, labRoot);
            go.name = "Alchemist crew / " + name; go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }
        void BuildAlchemistCrew() {
            if (labRoot) return;
            var rival = Rivals.Alchemist;
            labRoot = new GameObject("The Alchemist's crew (ambient)").transform; labRoot.SetParent(transform, false);
            labBoss = LabCharacter(rival.BossModel, rival.Boss, AlchemistLayout.HeadCook, 180);
            for (int i = 0; i < AlchemistLayout.Waiting.Length; i++) {
                var rf = rival.Roster[i % rival.Roster.Length];
                var w = new LabWaiter { Home = i, Go = LabCharacter(rf.ModelNow, rf.Name, AlchemistLayout.Waiting[i], 180) };
                w.Motion = w.Go.GetComponent<CharacterMotion>(); w.Anim = w.Go.GetComponentInChildren<ResidentAnimator>(); labWaiters.Add(w);
            }
        }

        void AnimateRival(float seconds) {
            if (!Game || !Game.Player || Rivals.Alchemist == null) return;
            var centre = AlchemistLayout.Centre;
            bool near = Vector3.Distance(Game.Player.transform.position, centre) < AlchemistNear;
            if (!near && Game.CoOp) foreach (var p in Game.CoOp.Players) if (p && Vector3.Distance(p.transform.position, centre) < AlchemistNear) near = true;
            if (!near && !labRoot) return;
            BuildAlchemistCrew();
            bool raid = ActiveRaid && ActiveRaid.Rival == Rivals.Alchemist;
            bool show = near && !raid;
            if (labRoot.gameObject.activeSelf != show) { labRoot.gameObject.SetActive(show); if (!show) ClearLab(); }
            if (!show) return;
            bool night = Game.State.IsNight;
            // Walk in on a dining room that is already busy: a few guests seated at different stages.
            if (!labWarm && !night) { labWarm = true; for (int k = 0; k < 4; k++) { var g = NewLabGuest(); if (g == null) break; SeatNow(g, k); } }

            labNextGuest -= seconds;
            if (!night && labNextGuest <= 0 && labGuests.Count < 7) {
                labNextGuest = Random.Range(6f, 11f);
                var g = NewLabGuest();
                if (g != null) {
                    var s = AlchemistLayout.Seats[g.Seat];
                    g.Go.transform.position = AlchemistLayout.Street;
                    g.Path.Enqueue(AlchemistLayout.Door); g.Path.Enqueue(new Vector3(s.Lane, 0, AlchemistLayout.FrontLane)); g.Path.Enqueue(new Vector3(s.Lane, 0, s.At.z)); g.Path.Enqueue(s.At);
                }
            }
            string dish = RestaurantCatalog.Dish(Rivals.Alchemist.RecipeId)?.Name ?? "special";
            for (int i = labGuests.Count - 1; i >= 0; i--) {
                var g = labGuests[i]; if (!g.Go) { labGuests.RemoveAt(i); continue; }
                var seat = AlchemistLayout.Seats[g.Seat]; string bubble = "";
                switch (g.Stage) {
                    case 0:   // walking to the seat
                        if (!LabWalk(g.Go, g.Motion, g.Path, seconds, 1.8f)) { g.Stage = 1; g.Timer = 5; }
                        break;
                    case 1:   // seated, waiting for the dish
                        Sit(g, seat); g.Timer -= seconds;
                        bubble = g.Timer > 0 ? "One " + dish + "!" : "...";
                        break;
                    case 2:   // eating
                        Sit(g, seat); g.Timer -= seconds;
                        bubble = g.Timer > 8 ? "It's... glowing." : g.Timer > 3 ? "Mmm. Pure gold." : "";
                        if (g.Timer <= 0) {
                            LabPlate(g.Seat, false); g.Stage = 3;
                            g.Path.Enqueue(new Vector3(seat.Lane, 0, seat.At.z)); g.Path.Enqueue(new Vector3(seat.Lane, 0, AlchemistLayout.FrontLane)); g.Path.Enqueue(AlchemistLayout.Door); g.Path.Enqueue(AlchemistLayout.Street);
                        }
                        break;
                    case 3:   // home
                        if (!LabWalk(g.Go, g.Motion, g.Path, seconds, 1.8f)) { Destroy(g.Go); labGuests.RemoveAt(i); continue; }
                        break;
                }
                if (g.Motion) { g.Motion.Seated = g.Stage == 1 || g.Stage == 2; g.Motion.Eating = g.Stage == 2; g.Motion.SetMood(g.Stage == 1 && g.Timer < -8 ? .55f : .9f); }
                SetBubble(g.Bubble, bubble);
            }
            // The stitched staff: wait at the pass, take a plate to whoever has waited longest, come back.
            bool busy = false;
            foreach (var w in labWaiters) {
                if (!w.Go) continue;
                switch (w.Stage) {
                    case 0: {   // at the pass
                        var home = AlchemistLayout.Waiting[w.Home];
                        if (LabWalk(w.Go, w.Motion, w.Path, seconds, 1.5f)) break;
                        w.Go.transform.rotation = Quaternion.Euler(0, 180, 0);
                        var next = labGuests.Find(o => o.Stage == 1 && !o.Claimed && o.Timer <= 0);
                        if (next != null) { next.Claimed = true; w.Guest = next; w.Stage = 1; w.Timer = 2.2f; }
                        else if ((w.Go.transform.position - home).sqrMagnitude > .01f) w.Path.Enqueue(home);
                        break;
                    }
                    case 1:   // the Alchemist comes to the bench and plates it up
                        busy = true; if (BossAtBench) w.Timer -= seconds; w.Go.transform.rotation = Quaternion.Euler(0, 0, 0);
                        if (w.Timer <= 0) {
                            if (w.Guest == null || !w.Guest.Go || w.Guest.Stage != 1) { w.Stage = 0; w.Guest = null; break; }
                            w.Plate = KitchenArt.CreateItem("Plate", Recipe(Rivals.Alchemist.RecipeId), labRoot); w.Plate.name = "Alchemist plate (carried)"; w.Plate.transform.localScale = Vector3.one * .8f;
                            if (w.Anim) w.Anim.Carried = w.Plate.transform;   // held on the palms, arms out
                            var s = AlchemistLayout.Seats[w.Guest.Seat];
                            w.Path.Enqueue(new Vector3(s.Lane, 0, AlchemistLayout.CounterLane)); w.Path.Enqueue(new Vector3(s.Lane, 0, s.At.z));
                            w.Path.Enqueue(Vector3.Lerp(new Vector3(s.Lane, 0, s.At.z), s.At, s.Lane == AlchemistLayout.MidLane ? .6f : .55f));
                            w.Stage = 2;
                        }
                        break;
                    case 2:   // out to the table, plate held in front
                        if (w.Plate && !w.Anim) w.Plate.transform.position = w.Go.transform.position + w.Go.transform.forward * .42f + Vector3.up * 1.05f;
                        if (!LabWalk(w.Go, w.Motion, w.Path, seconds, 1.5f)) {
                            if (w.Anim) w.Anim.Carried = null; if (w.Plate) Destroy(w.Plate);
                            if (w.Guest != null && w.Guest.Go && w.Guest.Stage == 1) { LabPlate(w.Guest.Seat, true); w.Guest.Stage = 2; w.Guest.Timer = Random.Range(14f, 20f); }
                            var s = AlchemistLayout.Seats[w.Guest != null ? w.Guest.Seat : 0];
                            w.Path.Enqueue(new Vector3(s.Lane, 0, s.At.z)); w.Path.Enqueue(new Vector3(s.Lane, 0, AlchemistLayout.CounterLane)); w.Path.Enqueue(AlchemistLayout.Waiting[w.Home]);
                            w.Guest = null; w.Stage = 0;
                        }
                        break;
                }
                if (w.Motion) { w.Motion.Working = w.Stage == 1; w.Motion.SetMood(.8f); }
            }
            BossRound(seconds, busy);
        }
        void BossRound(float seconds, bool plating) {
            if (!labBoss) return; var bm = labBoss.GetComponent<CharacterMotion>();
            if (bossGoing >= 0) {
                if (!LabWalk(labBoss, bm, bossPath, seconds, 1.6f)) { bossAt = bossGoing; bossGoing = -1; bossTimer = plating && bossAt == 2 ? 1.5f : Random.Range(3.5f, 6.5f); }
                else { if (bm) bm.Working = false; return; }
            }
            labBoss.transform.rotation = Quaternion.RotateTowards(labBoss.transform.rotation, Quaternion.Euler(0, LabStations[bossAt].yaw, 0), seconds * 360);
            if (bm) { bm.Walking = false; bm.Working = true; bm.SetMood(.95f); }
            bossTimer -= seconds;
            int next = -1;
            if (plating && bossAt != 2) next = 2;                          // an order is up: back to the bench
            else if (bossTimer <= 0 && !plating) { next = Random.Range(0, LabStations.Length); if (next == bossAt) next = (next + 1) % LabStations.Length; }
            if (next < 0) return;
            bossGoing = next; bossPath.Clear();
            bossPath.Enqueue(new Vector3(labBoss.transform.position.x, 0, KitchenLane)); bossPath.Enqueue(new Vector3(LabStations[next].x, 0, KitchenLane));
            bossPath.Enqueue(new Vector3(LabStations[next].x, 0, next == 2 ? AlchemistLayout.HeadCook.z : 29.75f));
        }
        static List<string> Recipe(string id) { var r = RecipeBook.Find(id); return r != null ? new List<string>(r.Components) : new List<string> { "bun", "cooked_patty" }; }
        LabGuest NewLabGuest() {
            var free = new List<int>();
            for (int s = 0; s < AlchemistLayout.Seats.Length; s++) if (!labGuests.Exists(o => o.Seat == s)) free.Add(s);
            if (free.Count == 0) return null;
            var go = People.Visitor(7000 + ++labSeed * 17, labSeed % 6, labRoot, Game.State.IsNight); go.name = "Alchemist guest " + labSeed;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            var g = new LabGuest { Go = go, Motion = go.GetComponent<CharacterMotion>(), Seat = free[Random.Range(0, free.Count)], Bubble = WorldCaption(go.transform, "", new Vector3(0, 2.4f, 0), .018f) };
            labGuests.Add(g); return g;
        }
        // Warm start: put a guest straight in their seat, some still waiting and some already eating.
        void SeatNow(LabGuest g, int k) {
            var s = AlchemistLayout.Seats[g.Seat]; g.Go.transform.SetPositionAndRotation(s.At, Quaternion.Euler(0, s.Yaw, 0));
            if (k % 2 == 0) { g.Stage = 2; g.Timer = Random.Range(6f, 16f); LabPlate(g.Seat, true); } else { g.Stage = 1; g.Timer = Random.Range(0f, 3f); }
        }
        void Sit(LabGuest g, AlchemistLayout.Seat s) { g.Go.transform.SetPositionAndRotation(s.At, Quaternion.Euler(0, s.Yaw, 0)); }
        static bool LabWalk(GameObject go, CharacterMotion motion, Queue<Vector3> path, float seconds, float speed) {
            if (path.Count == 0) { if (motion) motion.Walking = false; return false; }
            var before = go.transform.position; var target = path.Peek(); target.y = before.y;
            go.transform.position = Vector3.MoveTowards(before, target, seconds * speed);
            var step = go.transform.position - before; step.y = 0;
            if (step.sqrMagnitude > 1e-6f) go.transform.rotation = Quaternion.LookRotation(step);
            var left = go.transform.position - target; left.y = 0;
            if (left.sqrMagnitude < .0025f) path.Dequeue();
            if (motion) motion.Walking = true;
            return true;
        }
        // The dish on the table in front of the guest.
        void LabPlate(int seat, bool on) {
            if (labPlates.TryGetValue(seat, out var old) && old) Destroy(old); labPlates.Remove(seat);
            if (!on || seat < 0) return;
            var s = AlchemistLayout.Seats[seat];
            var plate = KitchenArt.CreateItem("Plate", Recipe(Rivals.Alchemist.RecipeId), labRoot); plate.name = "Alchemist plate";
            var toSeat = s.At - s.Table; toSeat.y = 0;
            plate.transform.position = s.Table + toSeat.normalized * .2f + Vector3.up * (AlchemistLayout.TableTop + .005f);
            plate.transform.localScale = Vector3.one * .8f; labPlates[seat] = plate;
        }
        void ClearLab() {
            foreach (var g in labGuests) if (g.Go) Destroy(g.Go); labGuests.Clear();
            foreach (var p in labPlates.Values) if (p) Destroy(p); labPlates.Clear();
            foreach (var w in labWaiters) { if (w.Anim) w.Anim.Carried = null; if (w.Plate) Destroy(w.Plate); w.Path.Clear(); w.Guest = null; w.Stage = 0; if (w.Go) w.Go.transform.SetPositionAndRotation(AlchemistLayout.Waiting[w.Home], Quaternion.Euler(0, 180, 0)); }
            labWarm = false; bossPath.Clear(); bossGoing = -1; bossAt = 2;
            if (labBoss) labBoss.transform.SetPositionAndRotation(AlchemistLayout.HeadCook, Quaternion.Euler(0, 180, 0));
        }
    }
}
