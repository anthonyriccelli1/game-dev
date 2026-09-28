using UnityEngine;
namespace RestaurantCity {
    // Draws tonight's stash (a crate with a faint purple glow) and buzzes the phone with a tip at dusk.
    public partial class RestaurantController {
        GameObject stashCrate; int stashShownSpot = -2;
        static Vector3 StashGround(StashSpot spot) {
            // Nudge the crate onto open ground: never inside a wall, prop or kerb.
            var start = new Vector3(spot.X, 0, spot.Z);
            for (int ring = 0; ring < 8; ring++) for (int k = 0; k < Mathf.Max(1, ring * 6); k++) {
                float a = k * Mathf.PI * 2 / Mathf.Max(1, ring * 6); var p = start + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ring * .9f;
                float y = Physics.Raycast(p + Vector3.up * 30, Vector3.down, out var hit, 60, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0;
                if (y > 1.2f) continue;   // landed on a roof or a wall top
                var ground = new Vector3(p.x, y, p.z);
                if (!Physics.CheckBox(ground + Vector3.up * .65f, new Vector3(.45f, .5f, .45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return ground;
            }
            return start;
        }
        void TickNightStash() {
            var s = Game.State;
            if (s.StashTip) {
                s.StashTip = false;
                if (s.StashSpot >= 0 && s.StashSpot < NightStashes.Spots.Length)
                Game.Notify("ZEEB: \"Yo, your package is by " + NightStashes.Spots[s.StashSpot].Hint + ". Look for the purple glow. Don't get caught, bruh.\"  (M for map)", 10);
            }
            int want = s.StashActive && s.StashSpot < NightStashes.Spots.Length ? s.StashSpot : -1;
            if (want == stashShownSpot && (want < 0 || stashCrate)) return;
            if (stashCrate) { stashCrate.name = "Night stash (old)"; Destroy(stashCrate); }
            stashShownSpot = want; if (want < 0) return;
            stashCrate = new GameObject("Night stash");
            var crate = KitchenArt.SupplyCrate(stashCrate.transform, "protein"); crate.transform.localScale = Vector3.one * .6f;
            var glow = new GameObject("Stash glow").AddComponent<Light>(); glow.transform.SetParent(stashCrate.transform, false); glow.transform.localPosition = Vector3.up * 1.1f;
            glow.type = LightType.Point; glow.color = new Color(.62f, .38f, 1f); glow.range = 4.5f; glow.intensity = 2.2f;
            var box = stashCrate.AddComponent<BoxCollider>(); box.center = Vector3.up * .5f; box.size = new Vector3(.9f, 1f, .9f);
            stashCrate.AddComponent<Interactable>().Kind = InteractionKind.NightStash;
            stashCrate.transform.position = StashGround(NightStashes.Spots[want]);
            stashCrate.transform.rotation = Quaternion.Euler(0, want * 47, 0);
        }
        bool InspectNightStash(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (city.Kind != InteractionKind.NightStash) return false;
            var k = Game.State.Kitchen; var held = k.Hold(p.ActorId);
            prompts[p.ActorId] = "Zeeb's drop (" + Game.State.DropBottles + " bottles)\n" + (held != null && held.Kind != KitchenItemKind.GroceryBag ? "Free your hands to grab it" : "E / A  Take the Midnight sauce");
            if (pressed) { bool ok = k.CollectStash(Game.State, p.ActorId, out var m); Feedback(m); if (ok) { PlayChime(true); Game.Save(); } }
            return true;
        }
    }
}
