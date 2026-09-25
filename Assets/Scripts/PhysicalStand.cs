using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // The street food stand and Milo's market, built from the same stations and art as the restaurant.
    public partial class RestaurantController {
        readonly Dictionary<int, GameObject> standObjects = new Dictionary<int, GameObject>();
        static readonly float[] StandX = { -2.6f, -1.0f, .6f, 1.6f, 2.6f, 3.9f };
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

            for (int i = 0; i < KitchenState.StandKit.Length; i++) {
                int id = KitchenState.StandBase + 1 + i; string kind = KitchenState.StandKit[i];
                if (standObjects.TryGetValue(id, out var existing) && existing) continue;
                var obj = kind == "stand_plates" ? KitchenArt.CreateStation("plate_rack", Game.Stand.transform) : CreateFurnishing(kind, Game.Stand.transform);
                // The rack's decorative plates are replaced by a live stack showing the real count.
                if (kind == "stand_plates") foreach (Transform part in obj.GetComponentsInChildren<Transform>(true)) if (part.name == "Glazed cream plate") part.gameObject.SetActive(false);
                obj.name = "Stand " + kind;
                obj.transform.position = new Vector3(StandX[i], 0, StandZ);
                obj.transform.rotation = Quaternion.Euler(0, 180, 0);
                var target = obj.AddComponent<RestaurantTarget>(); target.InstanceId = id;
                foreach (var child in obj.GetComponentsInChildren<RestaurantTarget>()) child.InstanceId = id;
                if (obj.GetComponentsInChildren<Collider>().Length == 0) { var c = obj.AddComponent<BoxCollider>(); c.center = new Vector3(0, .65f, 0); c.size = new Vector3(kind == "grill" ? 1.8f : .9f, 1.3f, .9f); }
                standObjects[id] = obj;
            }
            BuildMilo(world);
            BuildStandSign();
        }

        TextMesh standSignText;

        void BuildStandSign() {
            if (standSignText) return;
            var sign = new GameObject("Stand open sign"); sign.transform.SetParent(Game.Stand.transform, false);
            sign.transform.position = new Vector3(-3.9f, 0, 7.4f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.name = "Chalkboard"; body.transform.SetParent(sign.transform, false);
            body.transform.localPosition = new Vector3(0, .75f, 0); body.transform.localScale = new Vector3(.9f, 1.1f, .08f);
            body.GetComponent<Renderer>().sharedMaterial = KitchenArt.Material("1B2A30");
            var legs = GameObject.CreatePrimitive(PrimitiveType.Cube); legs.name = "Sign legs"; legs.transform.SetParent(sign.transform, false);
            legs.transform.localPosition = new Vector3(0, .1f, 0); legs.transform.localScale = new Vector3(.95f, .2f, .3f);
            legs.GetComponent<Renderer>().sharedMaterial = KitchenArt.Material("895343");
            sign.AddComponent<Interactable>().Kind = InteractionKind.StandSign;
            standSignText = WorldCaption(sign.transform, "", new Vector3(0, .8f, -.06f), .02f);
            standSignText.transform.rotation = Quaternion.identity;
        }

        // Stand customers are the same odd residents who visit the restaurant. Up to three line up at once.
        readonly Dictionary<int, GameObject> standGuests = new Dictionary<int, GameObject>();
        readonly Dictionary<int, TextMesh> standBubbles = new Dictionary<int, TextMesh>();
        TextMesh standPlatesText, standSinkText;
        static readonly Vector3 StandFront = new Vector3(2.2f, 0, 5.9f);
        readonly List<GameObject> cleanStack = new List<GameObject>(), dirtyStack = new List<GameObject>();

        // Visible plate stacks: clean plates on the plate counter, dirty ones piled on the sink.
        void SyncPlateStack(List<GameObject> stack, int stationId, int count, string kind, Vector3 basePos) {
            if (!standObjects.TryGetValue(stationId, out var station) || !station) return;
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
            s.Players = Game.CoOp ? Mathf.Max(1, Game.CoOp.PlayerCount) : 1;
            if (standSignText) standSignText.text = s.StandOpen ? "<color=#4FCB7A>OPEN</color>\nBurgers" : "<color=#E1543B>CLOSED</color>";
            if (!standPlatesText && standObjects.TryGetValue(KitchenState.StandBase + 4, out var rack) && rack) { standPlatesText = WorldCaption(rack.transform, "", new Vector3(0, 2.1f, 0), .014f); }
            if (!standSinkText && standObjects.TryGetValue(KitchenState.StandBase + 5, out var sink) && sink) { standSinkText = WorldCaption(sink.transform, "", new Vector3(0, 2.1f, 0), .014f); }
            if (standPlatesText) { standPlatesText.text = "Clean plates: " + s.StandClean; standPlatesText.transform.rotation = Quaternion.identity; }
            if (standSinkText) { standSinkText.text = s.StandDirty > 0 ? "<color=#E8C34A>Dirty pile: " + s.StandDirty + "</color>" : "Sink"; standSinkText.transform.rotation = Quaternion.identity; }
            if (Game.Customer && Game.Customer.activeSelf) Game.Customer.SetActive(false);
            SyncPlateStack(cleanStack, KitchenState.StandBase + 4, s.StandClean, "Plate", new Vector3(0, 1.0f, .05f));
            SyncPlateStack(dirtyStack, KitchenState.StandBase + 5, s.StandDirty, "DirtyPlate", new Vector3(.75f, 1.08f, .2f));
            var world = Game.Stand ? Game.Stand.transform.parent : transform;
            var live = new HashSet<int>();
            for (int i = 0; i < s.StandQueue.Count; i++) {
                var order = s.StandQueue[i]; live.Add(order.Id);
                if (!standGuests.TryGetValue(order.Id, out var guest) || !guest) {
                    guest = RestaurantArt.CreateCharacter(order.Type, world); guest.name = "Stand guest " + order.Id;
                    guest.transform.position = new Vector3(11, 0, 4.5f);
                    var capsule = guest.AddComponent<CapsuleCollider>(); capsule.radius = .3f; capsule.height = 1.6f; capsule.center = Vector3.up * .83f;
                    guest.AddComponent<Interactable>().Kind = InteractionKind.Serve;
                    standGuests[order.Id] = guest; standBubbles[order.Id] = WorldCaption(guest.transform, "", new Vector3(0, 2.4f, 0), .02f);
                }
                var spot = StandFront + new Vector3(1.4f * i, 0, 0);
                var before = guest.transform.position;
                guest.transform.position = Vector3.MoveTowards(before, spot, seconds * 2.4f);
                bool walking = (guest.transform.position - before).sqrMagnitude > .000001f;
                guest.transform.rotation = Quaternion.Euler(0, walking ? 270 : 0, 0);
                var motion = guest.GetComponent<CharacterMotion>();
                float ratio = Mathf.Clamp01(order.Patience / Mathf.Max(1, order.MaxPatience));
                if (motion) { motion.Walking = walking; motion.SetMood(ratio); }
                int filled = Mathf.Max(1, Mathf.CeilToInt(ratio * 8)); string color = ratio > .55f ? "#4FCB7A" : ratio > .25f ? "#E8C34A" : "#E1543B";
                var name = RestaurantCatalog.Customers[Mathf.Clamp(order.Type, 0, RestaurantCatalog.Customers.Length - 1)].Name;
                SetBubble(standBubbles[order.Id], name + "\n" + (order.Dish == "midnight" ? "Midnight burger!" : "Burger, please!") + "\n<color=" + color + ">" + new string('|', filled) + "</color>");
            }
            foreach (var id in new List<int>(standGuests.Keys)) if (!live.Contains(id)) { if (standGuests[id]) Destroy(standGuests[id]); standGuests.Remove(id); standBubbles.Remove(id); }
        }

        void BuildMilo(Transform world) {
            if (!world || world.Find("Milo shopkeeper")) return;
            var old = world.Find("Milo"); if (old) old.gameObject.SetActive(false);
            // Clear the old placeholder produce and lower the counter so Milo is visible.
            foreach (Transform child in world) {
                if (child.name == "Produce crate" || child.name == "Produce") child.gameObject.SetActive(false);
                var tm = child.GetComponent<TextMesh>(); if (tm && tm.text.StartsWith("FRESH PACKS")) child.gameObject.SetActive(false);
            }
            var counter = world.Find("Supplier counter");
            if (counter) { var legacy = counter.GetComponent<Interactable>(); if (legacy) Destroy(legacy); counter.localScale = new Vector3(counter.localScale.x, .8f, counter.localScale.z); counter.position = new Vector3(counter.position.x, .4f, counter.position.z); }
            var milo = RestaurantArt.CreateCharacter(5, world); milo.name = "Milo shopkeeper";
            milo.transform.position = new Vector3(-12, 0, 10.3f); milo.transform.rotation = Quaternion.Euler(0, 180, 0);
            var motion = milo.GetComponent<CharacterMotion>(); if (motion) motion.SetMood(.9f);
            var caption = WorldCaption(milo.transform, "MILO\nFresh every morning", new Vector3(0, 2.35f, 0), .02f);
            caption.transform.rotation = Quaternion.Euler(0, 0, 0);
            MakeCrate(world, "protein", new Vector3(-13.3f, 0, 7.6f), "MEAT\n6 patties / $10");
            MakeCrate(world, "produce", new Vector3(-10.7f, 0, 7.6f), "PRODUCE\n6 buns & greens / $6");
        }

        void MakeCrate(Transform world, string contents, Vector3 position, string label) {
            var crate = KitchenArt.SupplyCrate(world, contents);
            crate.transform.position = position;
            var box = crate.AddComponent<BoxCollider>(); box.center = new Vector3(0, .75f, 0); box.size = new Vector3(1.2f, 1.5f, 1f);
            crate.AddComponent<Interactable>().Kind = contents == "protein" ? InteractionKind.SupplyProtein : InteractionKind.SupplyProduce;
            var text = WorldCaption(crate.transform, label, new Vector3(0, 1.95f, 0), .018f);
            text.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Prompt + action for Milo's crates and the stand customer. Returns true when handled.
        bool InspectStreet(FirstPersonPlayer p, Interactable city, bool pressed) {
            var k = Game.State.Kitchen; string actor = p.ActorId;
            if (city.Kind == InteractionKind.SupplyProtein || city.Kind == InteractionKind.SupplyProduce) {
                bool protein = city.Kind == InteractionKind.SupplyProtein;
                int have = protein ? Data.Protein : Data.Produce;
                prompts[actor] = (protein ? "Milo's meat crate" : "Milo's produce crate") + "  (you have " + have + ")\n" +
                    (!Game.State.StandBuilt && !Data.Owned ? "Set up your food stand first" : "E / A  Buy 6 " + (protein ? "patties  /  $10" : "buns & greens  /  $6"));
                if (pressed) {
                    string m;
                    if (!Data.Restock(Game.State, protein, out m) && Game.State.Cash < (protein ? 10 : 6)) Data.RequestSupplyHelp(Game.State, protein, out m);
                    Feedback(m); Game.Save();
                }
                return true;
            }
            if (city.Kind == InteractionKind.StandSign) {
                var st = Game.State;
                prompts[actor] = "Stand sign\nE / A  " + (st.StandOpen ? "Close the stand (no new customers)" : "Open the stand for customers");
                if (pressed) { st.StandOpen = !st.StandOpen; if (st.StandOpen && !st.HasOrder) st.NextCustomer = Mathf.Min(st.NextCustomer, 3); Feedback(st.StandOpen ? "Stand open! Customers will start walking up." : "Stand closed. Finish the current customer."); Game.Save(); }
                return true;
            }
            if (city.Kind == InteractionKind.Serve && (city.gameObject == Game.Customer || city.name.StartsWith("Stand guest"))) {
                prompts[actor] = "Stand line\n" + k.StandPreview(Game.State, actor);
                if (pressed) { bool ok = k.ServeStand(Game.State, actor, out var m); Feedback(m); if (ok) { PlayChime(false); Game.Save(); } }
                return true;
            }
            return false;
        }
    }
}
