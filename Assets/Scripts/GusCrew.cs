using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Greasy Gus's truck is open for business, so you can see your competition: Gus works the window, two imps cook
    // at the grill and the prep bench, and a few residents buy his special (the dish you can win from him in a raid)
    // and eat it at his two tables in the lot. Ambient only: nothing here touches money, orders or the save. It runs
    // while a player is near the lot and steps aside during a raid on Gus (the raid spawns its own fighters).
    public partial class RestaurantController {
        // Gus's truck frame: same model as Little Flame, parked at the rival's spot and turned 90 degrees (CityMap.GusTruck).
        // Truck-local x runs along the truck (window x -2.72..0), z across it (window wall -1.42 inside, back wall 1.83).
        static Vector3 GusOrigin => new Vector3(Rivals.GreasyGus.X, 0, Rivals.GreasyGus.Z);
        static readonly Quaternion GusTurn = Quaternion.Euler(0, 90, 0);
        const float TruckFloorY = .816f, GusNear = 55;
        static Vector3 GusAt(float x, float y, float z) => GusOrigin + GusTurn * new Vector3(x, y, z);
        static readonly Vector3 GusLotGate = new Vector3(15.2f, 0, -40.2f), GusStreet = new Vector3(17f, 0, -44.5f);
        static readonly Vector3[] GusQueue = { new Vector3(14.95f, 0, -30.1f), new Vector3(14.95f, 0, -31.6f), new Vector3(14.95f, 0, -33.1f) };
        static readonly Vector3[] GusTableSpots = { new Vector3(12.5f, 0, -31.3f), new Vector3(12.5f, 0, -34.8f) };

        class GusGuest { public GameObject Go; public CharacterMotion Motion; public TextMesh Bubble; public int Stage, Seat = -1; public float Timer; public readonly Queue<Vector3> Path = new Queue<Vector3>(); }
        Transform gusRoot; GameObject gusBoss; readonly List<GameObject> gusImps = new List<GameObject>();
        readonly List<GusGuest> gusGuests = new List<GusGuest>(); readonly List<Transform> gusSeats = new List<Transform>(); readonly List<Transform> gusTables = new List<Transform>();
        readonly Dictionary<int, GameObject> gusPlates = new Dictionary<int, GameObject>();
        float gusNextGuest = 3, gusServing; int gusSeed;

        void BuildGusCrew() {
            if (gusRoot) return;
            gusRoot = new GameObject("Greasy Gus's crew (ambient)").transform; gusRoot.SetParent(transform, false);
            // Kitchen: a counter at the window (Gus serves across it), a grill and a prep bench on the back wall.
            GusStation("counter", -1.4f, false); GusStation("grill", -2.45f, true); GusStation("prep_bench", -.55f, true);
            gusBoss = GusCharacter(Rivals.GreasyGus.BossModel, Rivals.GreasyGus.Boss, 5, GusAt(-1.4f, TruckFloorY, -.25f), 270);
            foreach (var (x, i) in new[] { (-2.45f, 0), (-.55f, 1) }) {
                var rf = Rivals.GreasyGus.Roster[i];
                gusImps.Add(GusCharacter(rf.Model, rf.Name, 3, GusAt(x, TruckFloorY, .5f), 90));
            }
            for (int t = 0; t < GusTableSpots.Length; t++) {
                var table = CreateFurnishing("patio_table", gusRoot); table.name = "Gus's table " + t;
                table.transform.SetPositionAndRotation(GusTableSpots[t], Quaternion.identity);
                gusTables.Add(table.transform); gusSeats.Add(table.transform.Find("Seat_0")); gusSeats.Add(table.transform.Find("Seat_1"));
            }
        }
        GameObject GusCharacter(string model, string name, int legacy, Vector3 at, float yaw) {
            var go = People.UseResidents ? ResidentModels.Create(new ResidentDef(model, name, ResidentCast.CustomResidentHeight, 0, StaffJob.Cook, ""), gusRoot) : RestaurantArt.CreateCharacter(legacy, gusRoot);
            go.name = "Gus crew / " + name; go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }
        // A station prop inside Gus's truck, squeezed and slid so it sits between its wall and the lane.
        void GusStation(string kind, float x, bool backWall) {
            var obj = kind == "prep_bench" ? KitchenArt.CreateStation("cutting_board", gusRoot) : CreateFurnishing(kind, gusRoot);
            obj.name = "Gus's " + kind; StationLooks.ApplyLevel(obj, 1);
            foreach (var c in obj.GetComponentsInChildren<Collider>()) Destroy(c);
            obj.transform.SetPositionAndRotation(GusAt(x, TruckFloorY, backWall ? 1.3f : -.95f), GusTurn * Quaternion.Euler(0, backWall ? 180 : 0, 0));
            float lo = backWall ? .85f : -1.4f, hi = backWall ? 1.8f : -.55f;
            var inv = Quaternion.Inverse(GusTurn);
            (float min, float max) Span() {
                float mn = float.MaxValue, mx = float.MinValue;
                foreach (var r in obj.GetComponentsInChildren<Renderer>()) {
                    if (!r.enabled || r is ParticleSystemRenderer || r.GetComponent<TextMesh>()) continue;
                    var b = r.bounds;
                    for (int k = 0; k < 8; k++) {
                        var corner = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
                        float z = (inv * (corner - GusOrigin)).z; mn = Mathf.Min(mn, z); mx = Mathf.Max(mx, z);
                    }
                }
                return (mn, mx);
            }
            var (a, bmax) = Span(); if (a > bmax) return;
            if (bmax - a > hi - lo) { var sc = obj.transform.localScale; sc.z *= (hi - lo) / (bmax - a); obj.transform.localScale = sc; (a, bmax) = Span(); }
            float dz = a < lo ? lo - a : bmax > hi ? hi - bmax : 0;
            obj.transform.position += GusTurn * new Vector3(0, 0, dz);
        }

        void TickGusCrew(float seconds) {
            if (!Game || !Game.Player) return;
            bool near = Vector3.Distance(Game.Player.transform.position, GusOrigin) < GusNear;
            if (!near && Game.CoOp) foreach (var p in Game.CoOp.Players) if (p && Vector3.Distance(p.transform.position, GusOrigin) < GusNear) near = true;
            bool raid = ActiveRaid && ActiveRaid.Rival == Rivals.GreasyGus;
            if (!near && !gusRoot) return;
            BuildGusCrew();
            // The raid brings out its own Gus and imps; the regulars clear out.
            bool show = near && !raid;
            if (gusRoot.gameObject.activeSelf != show) {
                gusRoot.gameObject.SetActive(show);
                if (!show) { foreach (var g in gusGuests) if (g.Go) Destroy(g.Go); gusGuests.Clear(); foreach (var p in gusPlates.Values) if (p) Destroy(p); gusPlates.Clear(); }
            }
            if (!show) return;

            // New customers wander in from South Avenue, up to three at a time.
            gusNextGuest -= seconds;
            if (gusNextGuest <= 0 && gusGuests.Count < 3) {
                gusNextGuest = Random.Range(9f, 16f);
                var go = People.Visitor(9000 + ++gusSeed * 13, gusSeed % 6, gusRoot, Game.State.IsNight); go.name = "Gus's customer " + gusSeed;
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                go.transform.position = GusStreet;
                var guest = new GusGuest { Go = go, Motion = go.GetComponent<CharacterMotion>(), Bubble = WorldCaption(go.transform, "", new Vector3(0, 2.4f, 0), .018f) };
                guest.Path.Enqueue(GusLotGate); gusGuests.Add(guest);
            }
            var special = RestaurantCatalog.Dish(Rivals.GreasyGus.RecipeId)?.Name ?? "special";
            bool cooking = false;
            for (int i = gusGuests.Count - 1; i >= 0; i--) {
                var g = gusGuests[i]; if (!g.Go) { gusGuests.RemoveAt(i); continue; }
                int place = gusGuests.FindAll(o => o.Stage <= 1).IndexOf(g);
                string bubble = "";
                switch (g.Stage) {
                    case 0:   // walking in, then in line at the window
                        var spot = GusQueue[Mathf.Clamp(place, 0, GusQueue.Length - 1)];
                        var off = g.Go.transform.position - spot; off.y = 0;
                        if (g.Path.Count == 0 && off.sqrMagnitude > .01f) g.Path.Enqueue(spot);
                        if (!GusWalk(g, seconds)) { g.Go.transform.rotation = Quaternion.Euler(0, 90, 0); if (place == 0) { g.Stage = 1; g.Timer = 4.5f; } }
                        break;
                    case 1:   // ordering: Gus and the imps go to work
                        cooking = true; g.Timer -= seconds; bubble = "One " + special + ", Gus!";
                        g.Go.transform.rotation = Quaternion.Euler(0, 90, 0);
                        if (g.Timer <= 0) {
                            g.Seat = FreeGusSeat();
                            if (g.Seat < 0) { g.Timer = 1; break; }
                            g.Stage = 2; var seat = gusSeats[g.Seat];
                            g.Path.Enqueue(new Vector3(14.1f, 0, seat.position.z)); g.Path.Enqueue(seat.position);
                        }
                        break;
                    case 2:   // to the table
                        bubble = special;
                        if (!GusWalk(g, seconds)) { g.Stage = 3; g.Timer = Random.Range(12f, 18f); GusPlate(g.Seat, true); }
                        break;
                    case 3:   // eating
                        g.Timer -= seconds; bubble = g.Timer > 6 ? "Mmm. " + special + "." : "Gus knows grease.";
                        var s = gusSeats[g.Seat]; g.Go.transform.SetPositionAndRotation(s.position, s.rotation);
                        if (g.Timer <= 0) { g.Stage = 4; GusPlate(g.Seat, false); g.Path.Enqueue(new Vector3(14.1f, 0, s.position.z)); g.Path.Enqueue(GusLotGate); g.Path.Enqueue(GusStreet); }
                        break;
                    case 4:   // home
                        if (!GusWalk(g, seconds)) { Destroy(g.Go); gusGuests.RemoveAt(i); continue; }
                        break;
                }
                if (g.Motion) { g.Motion.Seated = g.Stage == 3; g.Motion.Eating = g.Stage == 3; g.Motion.SetMood(.85f); }
                SetBubble(g.Bubble, bubble);
            }
            // Gus faces his customers; the imps keep the grill going while there is an order.
            foreach (var imp in gusImps) { var m = imp.GetComponent<CharacterMotion>(); if (m) { m.Working = cooking || gusGuests.Count > 0; m.Walking = false; } }
            var gm = gusBoss.GetComponent<CharacterMotion>(); if (gm) { gm.Working = cooking; gm.Walking = false; gm.SetMood(.9f); }
        }
        bool GusWalk(GusGuest g, float seconds) {
            if (g.Path.Count == 0) { if (g.Motion) g.Motion.Walking = false; return false; }
            // Compare on the ground plane only: the walk animation can bob the root up and down.
            var before = g.Go.transform.position; var target = g.Path.Peek(); target.y = before.y;
            g.Go.transform.position = Vector3.MoveTowards(before, target, seconds * 1.9f);
            var step = g.Go.transform.position - before; step.y = 0;
            if (step.sqrMagnitude > 1e-6f) g.Go.transform.rotation = Quaternion.LookRotation(step);
            var left = g.Go.transform.position - target; left.y = 0;
            if (left.sqrMagnitude < .0025f) g.Path.Dequeue();
            if (g.Motion) g.Motion.Walking = true;
            return true;
        }
        int FreeGusSeat() {
            for (int s = 0; s < gusSeats.Count; s++) if (gusSeats[s] && !gusGuests.Exists(o => o.Seat == s && o.Stage >= 2)) return s;
            return -1;
        }
        // His special on the table in front of whoever is eating it.
        void GusPlate(int seat, bool on) {
            if (gusPlates.TryGetValue(seat, out var old) && old) Destroy(old); gusPlates.Remove(seat);
            if (!on || seat < 0) return;
            var recipe = RecipeBook.Find(Rivals.GreasyGus.RecipeId); var t = gusTables[seat / 2];
            var plate = KitchenArt.CreateItem("Plate", recipe != null ? new List<string>(recipe.Components) : new List<string> { "bun", "cooked_patty" }, gusRoot);
            var local = new Vector3(0, 0, seat % 2 == 0 ? -.2f : .2f);
            plate.transform.position = t.position + t.rotation * local + Vector3.up * (SurfaceY(t, local, .84f) + .005f); plate.transform.localScale = Vector3.one * .8f;
            gusPlates[seat] = plate;
        }
    }
}
