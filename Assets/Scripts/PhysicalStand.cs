using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // The street food stand and Milo's market, built from the same stations and art as the restaurant.
    public partial class RestaurantController {
        readonly Dictionary<int, GameObject> standObjects = new Dictionary<int, GameObject>();
        static readonly float[] StandX = { -2.6f, -1.0f, .6f, 1.6f, 2.7f };
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
                var obj = kind == "paper_plates" ? KitchenArt.CreateStation("plate_rack", Game.Stand.transform) : CreateFurnishing(kind, Game.Stand.transform);
                obj.name = "Stand " + kind;
                obj.transform.position = new Vector3(StandX[i], 0, StandZ);
                obj.transform.rotation = Quaternion.Euler(0, 180, 0);
                var target = obj.AddComponent<RestaurantTarget>(); target.InstanceId = id;
                foreach (var child in obj.GetComponentsInChildren<RestaurantTarget>()) child.InstanceId = id;
                if (obj.GetComponentsInChildren<Collider>().Length == 0) { var c = obj.AddComponent<BoxCollider>(); c.center = new Vector3(0, .65f, 0); c.size = new Vector3(kind == "grill" ? 1.8f : .9f, 1.3f, .9f); }
                standObjects[id] = obj;
            }
            BuildMilo(world);
        }

        void BuildMilo(Transform world) {
            if (!world || world.Find("Milo shopkeeper")) return;
            var old = world.Find("Milo"); if (old) old.gameObject.SetActive(false);
            var counter = world.Find("Supplier counter");
            if (counter) { var legacy = counter.GetComponent<Interactable>(); if (legacy) Destroy(legacy); }
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
                    if (!Data.Restock(Game.State, protein, out m) && Game.State.Cash < (protein ? 10 : 6)) Data.RequestSupplyHelp(Game.State, out m);
                    Feedback(m); Game.Save();
                }
                return true;
            }
            if (city.Kind == InteractionKind.Serve && city.gameObject == Game.Customer) {
                prompts[actor] = "Stand customer\n" + k.StandPreview(Game.State, actor);
                if (pressed) { bool ok = k.ServeStand(Game.State, actor, out var m); Feedback(m); if (ok) { PlayChime(false); Game.Save(); } }
                return true;
            }
            return false;
        }
    }
}
