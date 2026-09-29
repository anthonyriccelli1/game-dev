using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // The street food stand and Milo's market, built from the same stations and art as the restaurant.
    public partial class RestaurantController {
        readonly Dictionary<int, GameObject> standObjects = new Dictionary<int, GameObject>();
        static readonly float[] StandX = { -2.45f, -.95f, .6f, 1.68f, 2.78f, 3.75f, -3.55f };   // pantry, grill, counter, plates, sink, trash, cutting board (left end): all under the awning
        const float StandZ = 8.3f;

        public GameObject StationObject(int id) {
            if (Furnishings.TryGetValue(id, out var obj) && obj) return obj;
            return standObjects.TryGetValue(id, out obj) ? obj : null;
        }

        void BuildStreetKitchen() {
            Game.State.Kitchen.EnsureStandStations();
            if (!Game.Stand) return;
            foreach (var old in new[] { "Service counter", "Service surface", "Prep station", "Cutting board", "Grill station", "Grill bar" })
                foreach (Transform child in Game.Stand.transform) if (child.name == old) child.gameObject.SetActive(false);
            foreach (Transform child in Game.Stand.transform) if (child.GetComponent<TextMesh>() && (child.GetComponent<TextMesh>().text.Contains("PREP") || child.GetComponent<TextMesh>().text.Contains("GRILL") || child.GetComponent<TextMesh>().text.Contains("SERVE"))) child.gameObject.SetActive(false);
            var world = Game.Stand.transform.parent;
            var bin = world ? world.Find("Discard bin") : null; if (bin) bin.gameObject.SetActive(false);
            foreach (Transform child in world) if (child.name == "BIN" || (child.GetComponent<TextMesh>() && child.GetComponent<TextMesh>().text == "BIN")) child.gameObject.SetActive(false);
            BuildStandShell();

            for (int i = 0; i < KitchenState.StandKit.Length; i++) {
                int id = KitchenState.StandBase + 1 + i; string kind = KitchenState.StandKit[i];
                if (standObjects.TryGetValue(id, out var existing) && existing) continue;
                var obj = kind == "stand_plates" ? KitchenArt.CreateStation("plate_rack", Game.Stand.transform) : kind == "prep_bench" ? KitchenArt.CreateStation("cutting_board", Game.Stand.transform) : CreateFurnishing(kind, Game.Stand.transform); StationLooks.ApplyLevel(obj, 1);   // the street stand is all Flats hand-me-downs
                // The rack's decorative plates are replaced by a live stack showing the real count.
                if (kind == "stand_plates") foreach (Transform part in obj.GetComponentsInChildren<Transform>(true)) if (part.name == "Glazed cream plate") part.gameObject.SetActive(false);
                obj.name = "Stand " + kind;
                obj.transform.position = new Vector3(StandX[i], 0, StandZ);
                // Stations face the cook, who works from the sidewalk side (z 9.35); customers stay on the street side.
                obj.transform.rotation = Quaternion.identity;
                var target = obj.AddComponent<RestaurantTarget>(); target.InstanceId = id;
                foreach (var child in obj.GetComponentsInChildren<RestaurantTarget>()) child.InstanceId = id;
                if (obj.GetComponentsInChildren<Collider>().Length == 0) { var c = obj.AddComponent<BoxCollider>(); c.center = new Vector3(0, .65f, 0); c.size = new Vector3(kind == "grill" ? 1.8f : .9f, 1.3f, .9f); }
                standObjects[id] = obj;
            }
            BuildMilo(world);
            BuildStandSign();
            BuildStandTables(world);
        }

        // The stand art is a visual shell. The original scene objects keep their colliders and the
        // station objects below keep their exact positions, targets, work points and item points.
        void BuildStandShell() {
            if (Game.Stand.transform.Find("POLYGON Shops stand shell")) return;
            var shell = ArtOverrides.Find("Shell", "street_stand");
            if (!shell) return; // Projects without the Shops pack retain the original stand.
            foreach (Transform child in Game.Stand.transform) {
                bool legacy = child.name == "Canopy post" || child.name == "Striped awning" ||
                    child.name == "LITTLE FLAME" || child.name == "LITTLE FLAME signboard" || child.name == "Burger";
                if (!legacy) continue;
                foreach (var renderer in child.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            }
            var art = Instantiate(shell, Game.Stand.transform);
            art.name = "POLYGON Shops stand shell";
            art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity;
        }

        // The stand's two sidewalk tables are the same cafe tables guests sit at in the restaurant (same chairs,
        // same seat point, same sitting pose), so the stand already looks and plays like a tiny restaurant.
        static readonly Vector3[] StandTablePositions = { new Vector3(-5.2f, 0, 10.6f), new Vector3(-8.2f, 0, 10.6f) };
        readonly List<Transform> standSeats = new List<Transform>();          // four seats: seat / 2 = table, seat % 2 = near/far chair
        readonly List<Vector3> standTableSpots = new List<Vector3>();
        readonly List<Transform> standTables = new List<Transform>();
        readonly Dictionary<int, GameObject> tablePlates = new Dictionary<int, GameObject>();
        readonly Dictionary<int, string> tablePlateState = new Dictionary<int, string>();   // per seat: "", "dirty" or the dish being eaten
        readonly Dictionary<int, int> guestLeg = new Dictionary<int, int>();   // how far along the walk to their seat each guest is
        static readonly Vector3 StandCorner = new Vector3(-4.9f, 0, 6.2f);  // guests walk round the stand's end, not through it
        void BuildStandTables(Transform world) {
            if (standTableSpots.Count > 0 || !world) return;
            for (int t = 0; t < GameState.StandTables; t++) {
                var table = CreateFurnishing("patio_table", world); table.name = "Stand table " + t;
                table.transform.SetPositionAndRotation(StandTablePositions[t], Quaternion.identity);
                if (table.GetComponentsInChildren<Collider>().Length == 0) { var box = table.AddComponent<BoxCollider>(); box.center = new Vector3(0, .45f, 0); box.size = new Vector3(1.2f, .9f, 1.9f); }
                var it = table.AddComponent<Interactable>(); it.Kind = InteractionKind.StandTable; it.Index = t;
                standTableSpots.Add(StandTablePositions[t]); standTables.Add(table.transform);
                standSeats.Add(table.transform.Find("Seat_0")); standSeats.Add(table.transform.Find("Seat_1"));
            }
        }
        Transform SeatOf(int seat) => seat >= 0 && seat < standSeats.Count ? standSeats[seat] : null;
        // The walk to a seat: round the stand's end, then (for the far chair) round the side of the table, then sit.
        Vector3[] SeatRoute(int seat) {
            var s = SeatOf(seat); var target = s ? s.position : StandFront;
            if (seat % 2 == 0 || seat / 2 >= standTableSpots.Count) return new[] { StandCorner, target };
            var table = standTableSpots[seat / 2];
            return new[] { StandCorner, new Vector3(table.x - 1.25f, 0, table.z - 1.2f), new Vector3(table.x - 1.25f, 0, target.z), target };
        }
        void SyncTablePlate(int seat, string state) {
            tablePlateState.TryGetValue(seat, out var shown);
            if (shown == state && (state == "" || tablePlates.ContainsKey(seat) && tablePlates[seat])) return;
            if (tablePlates.TryGetValue(seat, out var old) && old) Destroy(old);
            tablePlates.Remove(seat); tablePlateState[seat] = state;
            if (state == "" || seat / 2 >= standTableSpots.Count) return;
            // The plate shows the dish they actually ordered (salad, burger or midnight burger).
            var recipe = RecipeBook.Find(state);
            var plate = state == "dirty" ? KitchenArt.CreateItem("DirtyPlate", transform) : KitchenArt.CreateItem("Plate", recipe != null ? new List<string>(recipe.Components) : new List<string> { "bun", "cooked_patty" }, transform);
            // Plates rest on the real tabletop (the wooden patio table is lower than the old cafe table).
            var local = new Vector3(0, 0, seat % 2 == 0 ? -.2f : .2f); var t = seat / 2 < standTables.Count ? standTables[seat / 2] : null;
            float top = t ? SurfaceY(t, local, .84f) + .005f : .84f;
            plate.transform.position = standTableSpots[seat / 2] + new Vector3(0, top, local.z); plate.transform.localScale = Vector3.one * .8f;
            tablePlates[seat] = plate;
        }


        TextMesh standSignText;

        void BuildStandSign() {
            if (standSignText) return;
            var sign = new GameObject("Stand open sign"); sign.transform.SetParent(Game.Stand.transform, false);
            sign.transform.position = new Vector3(4.95f, 0, 7.1f);   // outside the right post, beside the customer line; never in front of a station
            var menuArt = ArtOverrides.Find("Shell", "street_menu_board");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.name = "Chalkboard"; body.transform.SetParent(sign.transform, false);
            body.transform.localPosition = new Vector3(0, .75f, 0); body.transform.localScale = new Vector3(.9f, 1.1f, .08f);
            body.GetComponent<Renderer>().sharedMaterial = KitchenArt.Material("1B2A30");
            if (menuArt) body.GetComponent<Renderer>().enabled = false;
            var legs = GameObject.CreatePrimitive(PrimitiveType.Cube); legs.name = "Sign legs"; legs.transform.SetParent(sign.transform, false);
            legs.transform.localPosition = new Vector3(0, .1f, 0); legs.transform.localScale = new Vector3(.95f, .2f, .3f);
            legs.GetComponent<Renderer>().sharedMaterial = KitchenArt.Material("895343");
            if (menuArt) {
                legs.GetComponent<Renderer>().enabled = false;
                var model = Instantiate(menuArt, sign.transform);
                model.name = "POLYGON Shops menu board";
                model.transform.localPosition = Vector3.zero;
            }
            sign.AddComponent<Interactable>().Kind = InteractionKind.StandSign;
            standSignText = WorldCaption(sign.transform, "", new Vector3(0, .8f, menuArt ? -.13f : -.06f), .02f);
            standSignText.transform.rotation = Quaternion.identity;
        }

        // Stand customers are the same odd residents who visit the restaurant. Up to three line up at once.
        readonly Dictionary<int, GameObject> standGuests = new Dictionary<int, GameObject>();
        readonly Dictionary<int, TextMesh> standBubbles = new Dictionary<int, TextMesh>();
        TextMesh standPlatesText, standSinkText;
        static readonly Vector3 StandFront = new Vector3(2.2f, 0, 5.9f);
        readonly List<GameObject> cleanStack = new List<GameObject>(), dirtyStack = new List<GameObject>();
        GameObject standWorkerView; string standWorkerViewId; TextMesh standWorkerBubble;

        // The worker running the stand stands behind the grill and works while there are orders.
        void TickStandWorkerView(GameState s) {
            var w = s.StandWorker;
            if (w == null || !s.StandBuilt) { if (standWorkerView) standWorkerView.SetActive(false); return; }
            if (!standWorkerView || standWorkerViewId != w.Id) {
                if (standWorkerView) Destroy(standWorkerView);
                standWorkerView = People.Worker(w.Id, RestaurantCatalog.Worker(w.Id)?.ModelType ?? 8, Game.Stand.transform);
                standWorkerView.name = "Stand worker"; standWorkerViewId = w.Id;
                standWorkerView.transform.SetPositionAndRotation(new Vector3(-1.0f, 0, 9.35f), Quaternion.Euler(0, 180, 0));
                standWorkerBubble = WorldCaption(standWorkerView.transform, "", new Vector3(0, 2.4f, 0), .016f);
            }
            standWorkerView.SetActive(true);
            // Walk the stand like a player: pantry -> grill -> plate rack -> serving spot, or to the sink to wash.
            float p = s.StandWorkerProgress;
            float x = !s.StandWorkerActive ? -1.0f : s.StandWorkerWashing ? 2.6f : p < .15f ? -2.6f : p < .7f ? -1.0f : p < .85f ? 1.6f : 2.2f;
            var target = new Vector3(x, 0, 9.35f); var before = standWorkerView.transform.position;
            standWorkerView.transform.position = Vector3.MoveTowards(before, target, Time.deltaTime * 3f);
            bool moving = (standWorkerView.transform.position - before).sqrMagnitude > .000001f;
            standWorkerView.transform.rotation = Quaternion.Euler(0, moving ? (target.x > before.x ? 90 : 270) : 180, 0);
            var motion = standWorkerView.GetComponent<CharacterMotion>();
            if (motion) { motion.Walking = moving; motion.Working = !moving && s.StandWorkerActive; motion.SetMood(w.Energy / 100f); }
            SetBubble(standWorkerBubble, s.StandWorkerStatus + "\n<size=48>Energy " + (int)w.Energy + "  |  Your cut so far $" + s.StandWorkerEarned + " (they keep 40%)</size>");
        }

        // Visible plate stacks: clean plates on the plate counter, dirty ones piled on the sink.
        void SyncPlateStack(List<GameObject> stack, int stationId, int count, string kind, Vector3 basePos) {
            if (standObjects.TryGetValue(stationId, out var station)) SyncPlateStack(stack, station, count, kind, basePos);
        }
        void SyncPlateStack(List<GameObject> stack, GameObject station, int count, string kind, Vector3 basePos) {
            if (!station) { foreach (var o in stack) if (o) Destroy(o); stack.Clear(); return; }
            if (stack.Count > 0 && stack[0] && stack[0].transform.parent != station.transform.parent) { foreach (var o in stack) if (o) Destroy(o); stack.Clear(); }
            stack.RemoveAll(o => !o);
            while (stack.Count < count) {
                // Parent beside the station (some stations are scaled) and place in its local frame.
                var plate = KitchenArt.CreateItem(kind, station.transform.parent);
                plate.transform.localScale = Vector3.one * .7f;
                plate.transform.position = station.transform.TransformPoint(basePos) + Vector3.up * (.04f * stack.Count);
                stack.Add(plate);
            }
            while (stack.Count > count) { Destroy(stack[stack.Count - 1]); stack.RemoveAt(stack.Count - 1); }
        }

        void TickStreet(float seconds) {
            var s = Game.State;
            TickPantryDisplays();
            TickNightStash();
            TickInspectors(seconds);
            TickGameFeel(seconds);
            s.Players = Game.CoOp ? Mathf.Max(1, Game.CoOp.PlayerCount) : 1;
            if (standSignText) standSignText.text = s.StandOpen ? "<color=#4FCB7A>OPEN</color>\nBurgers\n& Salad" + (s.Knows("midnight") ? "\n+ Midnight" : "") : "<color=#E1543B>CLOSED</color>";
            if (!standPlatesText && standObjects.TryGetValue(KitchenState.StandBase + 4, out var rack) && rack) { standPlatesText = WorldCaption(rack.transform, "", new Vector3(0, 2.1f, 0), .014f); }
            if (!standSinkText && standObjects.TryGetValue(KitchenState.StandBase + 5, out var sink) && sink) { standSinkText = WorldCaption(sink.transform, "", new Vector3(0, 2.1f, 0), .014f); }
            if (standPlatesText) { standPlatesText.text = "Clean plates: " + s.StandClean; standPlatesText.transform.rotation = Quaternion.identity; }
            if (standSinkText) { standSinkText.text = s.StandDirty > 0 ? "<color=#E8C34A>Dirty pile: " + s.StandDirty + "</color>" : "Sink"; standSinkText.transform.rotation = Quaternion.identity; }
            if (Game.Customer && Game.Customer.activeSelf) Game.Customer.SetActive(false);
            TickStandWorkerView(s);
            SyncPlateStack(cleanStack, KitchenState.StandBase + 4, s.StandClean, "Plate", new Vector3(0, 1.0f, .05f));
            SyncPlateStack(dirtyStack, KitchenState.StandBase + 5, s.StandDirty, "DirtyPlate", new Vector3(.75f, 1.08f, .2f));
            var world = Game.Stand ? Game.Stand.transform.parent : transform;
            var live = new HashSet<int>(); int lineSpot = 0;
            for (int seat = 0; seat < standSeats.Count; seat++) { var eating = s.StandQueue.Find(o => o.Stage == 2 && o.Table == seat); SyncTablePlate(seat, seat < s.StandTableDirty.Count && s.StandTableDirty[seat] ? "dirty" : eating != null ? eating.Dish : ""); }
            for (int i = 0; i < s.StandQueue.Count; i++) {
                var order = s.StandQueue[i]; live.Add(order.Id);
                if (!standGuests.TryGetValue(order.Id, out var guest) || !guest) {
                    guest = string.IsNullOrEmpty(order.ResidentId) ? People.Visitor(order.Id, order.Type, world, Game.State.IsNight) : People.Resident(order.ResidentId, order.Type, world); guest.name = "Stand guest " + order.Id;
                    guest.transform.position = new Vector3(11, 0, 4.5f);
                    var capsule = guest.AddComponent<CapsuleCollider>(); capsule.radius = .3f; capsule.height = 1.6f; capsule.center = Vector3.up * .83f;
                    guest.AddComponent<Interactable>().Kind = InteractionKind.Serve;
                    standGuests[order.Id] = guest; standBubbles[order.Id] = WorldCaption(guest.transform, "", new Vector3(0, 2.4f, 0), .02f);
                }
                // In line at the window until a table frees up; then round the end of the stand and onto a stool.
                Vector3 spot;
                if (order.Stage == 0) spot = StandFront + new Vector3(1.4f * lineSpot++, 0, 0);
                else {
                    var route = SeatRoute(order.Table); guestLeg.TryGetValue(order.Id, out int leg); leg = Mathf.Min(leg, route.Length - 1);
                    spot = route[leg];
                    if (leg < route.Length - 1 && (guest.transform.position - spot).sqrMagnitude < .05f) guestLeg[order.Id] = leg + 1;
                }
                var before = guest.transform.position;
                guest.transform.position = Vector3.MoveTowards(before, spot, seconds * 2.4f);
                var step = guest.transform.position - before; bool walking = step.sqrMagnitude > .000001f;
                var seat = order.Stage > 0 ? SeatOf(order.Table) : null;
                bool seated = seat && !walking && (guest.transform.position - seat.position).sqrMagnitude < .01f;
                guest.transform.rotation = walking ? Quaternion.LookRotation(new Vector3(step.x, 0, step.z)) : seated ? seat.rotation : Quaternion.identity;
                var motion = guest.GetComponent<CharacterMotion>();
                if (motion) { motion.Seated = seated; motion.Eating = seated && order.Stage == 2; }
                float ratio = order.Stage == 2 ? 1 : Mathf.Clamp01(order.Patience / Mathf.Max(1, order.MaxPatience));
                if (motion) { motion.Walking = walking; motion.SetMood(ratio); }
                int filled = Mathf.Max(1, Mathf.CeilToInt(ratio * 8)); string color = ratio > .55f ? "#4FCB7A" : ratio > .25f ? "#E8C34A" : "#E1543B";
                var name = People.NameOf(guest, RestaurantCatalog.Customers[Mathf.Clamp(order.Type, 0, RestaurantCatalog.Customers.Length - 1)].Name);
                string want = order.Dish == "midnight" ? "Midnight burger!" : order.Dish == "salad" ? "Salad, please!" : "Burger, please!";
                SetBubble(standBubbles[order.Id], name + (ResidentCast.Get(order.ResidentId) != null && !Game.State.HasMet(order.ResidentId) ? "  <color=#E8C34A>NEW!</color>" : "") + "\n" + (order.Stage == 2 ? "<color=#4FCB7A>Mmm!</color>" : (order.Stage == 0 ? "Waiting for a table\n" : "") + want + "\n<color=" + color + ">" + new string('|', filled) + "</color>"));
            }
            foreach (var id in new List<int>(standGuests.Keys)) if (!live.Contains(id)) { if (standGuests[id]) Destroy(standGuests[id]); standGuests.Remove(id); standBubbles.Remove(id); guestLeg.Remove(id); }
        }

        public static Vector2 MiloSpot = new Vector2(-12, 9); public static float MiloRadius = 4.5f;
        void BuildMilo(Transform world) {
            if (!world || world.Find("Milo shopkeeper")) return;
            var old = world.Find("Milo"); if (old) old.gameObject.SetActive(false);
            // Clear the old placeholder produce and lower the counter so Milo is visible.
            foreach (Transform child in world) {
                if (child.name == "Produce crate" || child.name == "Produce") child.gameObject.SetActive(false);
                var tm = child.GetComponent<TextMesh>(); if (tm && tm.text.StartsWith("FRESH PACKS")) child.gameObject.SetActive(false);
            }
            // In the city build Milo works inside his store (CityMap made a walk-in room); otherwise at the street stall.
            bool walkIn = GameObject.Find("Milo's walk-in");
            MiloSpot = walkIn ? new Vector2(-15.5f, 16) : new Vector2(-12, 9); MiloRadius = walkIn ? 3.2f : 4.5f;
            var counter = world.Find("Supplier counter");
            if (counter && walkIn) { var legacy = counter.GetComponent<Interactable>(); if (legacy) Destroy(legacy); counter.position = new Vector3(-15.5f, .45f, 17.25f); counter.localScale = new Vector3(2.4f, .9f, 1.2f); counter.GetComponent<Renderer>().enabled = false; counter = null; }
            if (counter) { var legacy = counter.GetComponent<Interactable>(); if (legacy) Destroy(legacy); counter.localScale = new Vector3(counter.localScale.x, .8f, counter.localScale.z); counter.position = new Vector3(counter.position.x, .4f, counter.position.z); }
            var milo = People.Story(ResidentCast.Milo, 5, world); milo.name = "Milo shopkeeper";
            milo.transform.position = walkIn ? new Vector3(-15.5f, .06f, 18) : new Vector3(-12, 0, 10.3f); milo.transform.rotation = Quaternion.Euler(0, 180, 0);
            var motion = milo.GetComponent<CharacterMotion>(); if (motion) motion.SetMood(.9f);
            var talk = milo.AddComponent<CapsuleCollider>(); talk.radius = .45f; talk.height = 1.8f; talk.center = Vector3.up * .9f;
            milo.AddComponent<Interactable>().Kind = InteractionKind.Supplier;
            var caption = WorldCaption(milo.transform, "MILO\nFresh every morning", new Vector3(0, 2.35f, 0), .02f);
            caption.transform.rotation = Quaternion.Euler(0, 0, 0);
            MakeCrate(world, "protein", walkIn ? new Vector3(-17.15f, .06f, 15.4f) : new Vector3(-13.3f, 0, 7.6f), "FRESH MEAT");
            MakeCrate(world, "produce", walkIn ? new Vector3(-13.85f, .06f, 15.4f) : new Vector3(-10.7f, 0, 7.6f), "PRODUCE");
        }

        void MakeCrate(Transform world, string contents, Vector3 position, string label) {
            var crate = KitchenArt.SupplyCrate(world, contents);
            crate.transform.position = position;
            var box = crate.AddComponent<BoxCollider>(); box.center = new Vector3(0, .75f, 0); box.size = new Vector3(1.2f, 1.5f, 1f);
            bool indoor = GameObject.Find("Milo's walk-in"); var text = WorldCaption(crate.transform, label, new Vector3(0, indoor ? 1.55f : 1.95f, 0), indoor ? .011f : .018f);
            text.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Prompt + action for Milo's crates and the stand customer. Returns true when handled.
        bool InspectStreet(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (InspectNightStash(p, city, pressed)) return true;
            var k = Game.State.Kitchen; string actor = p.ActorId;
            if (city.Kind == InteractionKind.Supplier && city.name == "Milo shopkeeper") {
                var held = k.Hold(actor);
                prompts[actor] = "Milo\n" + (!Game.State.StandBuilt && !Data.Owned ? "Set up your food stand first" : held != null && held.Kind != KitchenItemKind.GroceryBag ? "Free your hands to shop" : "E / A  Shop");
                if (pressed && (Game.State.StandBuilt || Data.Owned)) ShowPanel("Supplies");
                return true;
            }
            if (city.Kind == InteractionKind.StandSign) {
                var st = Game.State;
                prompts[actor] = "Stand sign\nE / A  " + (st.StandOpen ? "Close the stand (no new customers)" : "Open the stand for customers");
                if (pressed) { st.StandOpen = !st.StandOpen; if (st.StandOpen && !st.HasOrder) st.NextCustomer = Mathf.Min(st.NextCustomer, 3); Feedback(st.StandOpen ? "Stand open! Customers will start walking up." : "Stand closed. Finish the current customer."); Game.Save(); }
                return true;
            }
            if (city.Kind == InteractionKind.Serve && city.name.StartsWith("Stand guest") && int.TryParse(city.name.Substring(12), out int guestId)) {
                var order = Game.State.StandQueue.Find(o => o.Id == guestId);
                var who = order == null ? "Customer" : RestaurantCatalog.Customers[Mathf.Clamp(order.Type, 0, RestaurantCatalog.Customers.Length - 1)].Name;
                prompts[actor] = who + "\n" + k.StandGuestPreview(Game.State, actor, order);
                if (pressed && order != null && order.Stage == 1) { bool ok = k.ServeStandGuest(Game.State, actor, guestId, out var m); Feedback(m); if (ok) { PlayChime(false); Game.Save(); } }
                return true;
            }
            if (city.Kind == InteractionKind.StandTable) {
                int t = city.Index; var st = Game.State;
                prompts[actor] = "Sidewalk table " + (t + 1) + "\n" + k.StandTablePreview(st, actor, t);
                if (pressed) {
                    string m = null; bool ok = false; int dirty = k.DirtySeatAt(st, t); var waiting = k.WaitingAtTable(st, actor, t);
                    if (dirty >= 0 && k.Hold(actor) == null) ok = k.ClearStandTable(st, actor, dirty, out m);
                    else if (waiting != null) { ok = k.ServeStandGuest(st, actor, waiting.Id, out m); if (ok) PlayChime(false); }
                    if (m != null) Feedback(m); if (ok) Game.Save();
                }
                return true;
            }
            if (city.Kind == InteractionKind.Serve && city.gameObject == Game.Customer) return true;
            return false;
        }
    }
}
