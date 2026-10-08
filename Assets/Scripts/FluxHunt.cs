using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Flux cases: The Alchemist's couriers hide sealed cases of Flux around Old Market at night. There are 12 hiding
    // spots (street level, up a little, and rooftops reached by fire escapes); each night 2 of them hold a case. A case
    // glows green and hums, so you find it by sight or by ear. Crack it open (three hits) for 1 Flux. Cases you don't
    // find are gone at sunrise. On the tallest roof on North Avenue a different box waits once you own a restaurant:
    // a locked strongbox with a purple glow, holding the Midnight Burger recipe.
    public static class FluxHunt {
        public const int SpotCount = 12, PerNight = 2, Hits = 3, MidnightHits = 2;
        public static readonly string[] Hints = {
            "the graffiti alley off South Avenue", "a fire-escape landing on East Street", "behind Gus's truck in the vacant lot",
            "the Alchemist's back courtyard", "the harbour promenade", "the park, by the statue", "a rooftop on West Street, by the graffiti alley",
            "the lab balcony inside The Alchemist", "a fire-escape landing on North Avenue", "a fire-escape landing on South Avenue",
            "the tallest roof on North Avenue", "a rooftop on South Avenue",
        };
        // Tonight's cases, rolled once per night (the same night always rolls the same spots).
        public static List<int> Roll(int day) {
            var picks = new List<int>(); uint h = (uint)(day * 2654435761u) ^ 0x5bd1e995u;
            while (picks.Count < PerNight) { h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15; int s = (int)(h % SpotCount); if (!picks.Contains(s)) picks.Add(s); }
            return picks;
        }
        // Returns true on the tick a new night's cases go out.
        public static bool Tick(GameState g) {
            g.FluxCases = g.FluxCases ?? new List<int>();
            if (g.IsNight) {
                if (g.FluxNight == g.Day) return false;
                g.FluxNight = g.Day; g.FluxCases = Roll(g.Day); return true;
            }
            if (g.FluxCases.Count > 0) g.FluxCases.Clear();   // sunrise: whatever wasn't found is gone
            return false;
        }
        public static bool Crack(GameState g, int spot, out string message) {
            if (g.FluxCases == null || !g.FluxCases.Remove(spot)) { message = "Nothing left in it."; return false; }
            g.Flux += 1; g.FluxIntroduced = true;
            message = "Cracked the case: +1 FLUX (" + g.Flux + " total)." + (g.FluxCases.Count > 0 ? " One more is humming somewhere in Old Market tonight." : " That's all of them tonight.");
            return true;
        }
        public static bool MidnightOut(GameState g) => g.IsNight && g.Restaurant != null && g.Restaurant.Owned && !g.Knows("midnight");
        public static bool OpenMidnight(GameState g, out string message) {
            if (!MidnightOut(g)) { message = "It's empty."; return false; }
            g.Learn("midnight"); g.MidnightCaseFound = true;
            message = "THE MIDNIGHT BURGER RECIPE! Zeeb's number is scribbled on the back: call him (P) for the sauce, then put it on your menu.";
            return true;
        }
    }

    public partial class RestaurantController {
        Transform[] fluxSpots; Transform midnightSpot; readonly Dictionary<int, GameObject> fluxShown = new Dictionary<int, GameObject>();
        readonly Dictionary<int, int> fluxHits = new Dictionary<int, int>(); GameObject midnightBox; int midnightHits;
        void FindFluxSpots() {
            if (fluxSpots != null) return;
            var root = GameObject.Find("Flux spots"); var list = new Transform[FluxHunt.SpotCount];
            if (root) for (int i = 0; i < list.Length; i++) list[i] = root.transform.Find("Flux spot " + i);
            fluxSpots = list; var m = GameObject.Find("Midnight case spot"); if (m) midnightSpot = m.transform;
        }
        static Vector3 OnSurface(Vector3 p) =>
            Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : p;
        void TickFluxHunt() {
            var g = Game.State; FindFluxSpots();
            if (FluxHunt.Tick(g) && Data.Owned) Game.Notify("Night falls. Two Flux cases are humming somewhere in Old Market. Look for a green glow, listen for the hum.", 8);
            // Cases out tonight.
            foreach (var spot in new List<int>(fluxShown.Keys)) if (!g.FluxCases.Contains(spot)) { if (fluxShown[spot]) Gone(fluxShown[spot]); fluxShown.Remove(spot); fluxHits.Remove(spot); }
            foreach (var spot in g.FluxCases) {
                if (fluxShown.ContainsKey(spot) || spot < 0 || spot >= fluxSpots.Length || !fluxSpots[spot]) continue;
                fluxShown[spot] = SpawnCase("Flux/FluxCase", "Flux case " + spot, OnSurface(fluxSpots[spot].position), fluxSpots[spot].rotation, InteractionKind.FluxCase, spot.ToString(), new Color(.2f, 1f, .4f), true);
            }
            // The Midnight Burger strongbox.
            bool want = FluxHunt.MidnightOut(g) && midnightSpot;
            if (want && !midnightBox) { midnightHits = 0; midnightBox = SpawnCase("Flux/RecipeBox", "Midnight recipe box", OnSurface(midnightSpot.position), midnightSpot.rotation, InteractionKind.RecipeBox, "midnight", new Color(.62f, .35f, 1f), false); }
            else if (!want && midnightBox) Gone(midnightBox);
        }
        // Hidden at once (so nothing can find or hit it this frame), destroyed at the end of the frame.
        static void Gone(GameObject go) { go.SetActive(false); Destroy(go); }
        GameObject SpawnCase(string prefab, string name, Vector3 at, Quaternion rot, InteractionKind kind, string site, Color glow, bool hum) {
            var src = Resources.Load<GameObject>(prefab);
            var go = src ? Instantiate(src) : new GameObject(); go.name = name;
            go.transform.SetPositionAndRotation(at, rot);
            if (!src) FluxVial.Create(go.transform, Vector3.zero, .4f);
            var box = go.AddComponent<BoxCollider>(); box.center = new Vector3(0, .32f, 0); box.size = new Vector3(.8f, .64f, .34f);   // slim: it sits against walls on fire-escape landings
            var it = go.AddComponent<Interactable>(); it.Kind = kind; it.Site = site;
            var l = new GameObject("Case glow").AddComponent<Light>(); l.transform.SetParent(go.transform, false); l.transform.localPosition = new Vector3(0, .9f, 0);
            l.type = LightType.Point; l.color = glow; l.range = hum ? 4f : 5f; l.intensity = hum ? 1.6f : 2.4f; l.shadows = LightShadows.None;
            if (hum) {
                var a = go.AddComponent<AudioSource>(); a.clip = SoundFx.Hum; a.loop = true; a.spatialBlend = 1; a.rolloffMode = AudioRolloffMode.Linear;
                a.minDistance = 1.2f; a.maxDistance = 16; a.volume = .85f; a.dopplerLevel = 0; a.Play();
            }
            return go;
        }
        // Prompt + action for a Flux case or the midnight strongbox: a few hits pry it open.
        bool InspectFluxCase(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (city.Kind == InteractionKind.FluxCase && int.TryParse(city.Site, out int spot)) {
                fluxHits.TryGetValue(spot, out int hits);
                prompts[p.ActorId] = "Flux case (sealed)\nE / A  Crack it open  " + new string('|', hits) + new string('.', FluxHunt.Hits - hits);
                if (pressed) CrackFluxCase(spot, p);
                return true;
            }
            if (city.Kind == InteractionKind.RecipeBox) {
                prompts[p.ActorId] = "A locked strongbox, glowing purple\nE / A  Pry it open  " + new string('|', midnightHits) + new string('.', FluxHunt.MidnightHits - midnightHits);
                if (pressed) PryMidnightBox(p);
                return true;
            }
            return false;
        }
        public bool CrackFluxCase(int spot, FirstPersonPlayer p) {
            fluxHits.TryGetValue(spot, out int hits); hits++; fluxHits[spot] = hits;
            fluxShown.TryGetValue(spot, out var go);
            if (go) { AudioSource.PlayClipAtPoint(hits < FluxHunt.Hits ? SoundFx.Thud : SoundFx.Clink, go.transform.position, .9f); go.transform.position += Random.insideUnitSphere * .03f; }
            if (hits < FluxHunt.Hits) return false;
            bool ok = FluxHunt.Crack(Game.State, spot, out var m); Feedback(m); Game.Notify(m, 6);
            if (ok) { PlayChime(true); if (go) Gone(go); fluxShown.Remove(spot); fluxHits.Remove(spot); Game.Save(); }
            return ok;
        }
        public bool PryMidnightBox(FirstPersonPlayer p) {
            midnightHits++;
            if (midnightBox) AudioSource.PlayClipAtPoint(SoundFx.Thud, midnightBox.transform.position, .9f);
            if (midnightHits < FluxHunt.MidnightHits) return false;
            bool ok = FluxHunt.OpenMidnight(Game.State, out var m); Feedback(m); Game.Notify(m, 10);
            if (ok) { PlayChime(true); if (midnightBox) Gone(midnightBox); Game.Save(); }
            return ok;
        }
    }
}
