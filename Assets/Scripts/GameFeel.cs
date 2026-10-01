using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Turns kitchen and service moments (GameState.Events) into sound, floating pop-ups and guest reactions,
    // runs the sizzle/bubble loops on cooking stations, and announces the rush.
    public partial class RestaurantController {
        AudioSource fx; float clinkCooldown; string lastPhase = ""; bool lastStandRush, lastRushSoon;
        readonly Dictionary<int, AudioSource> stationLoops = new Dictionary<int, AudioSource>();
        sealed class FloatText { public TextMesh Text; public float Age; public Vector3 Start; }
        readonly List<FloatText> floaters = new List<FloatText>();

        void Fx(AudioClip clip, float volume = 1) {
            if (!fx) { fx = gameObject.AddComponent<AudioSource>(); fx.spatialBlend = 0; fx.volume = .9f; }
            if (clip) fx.PlayOneShot(clip, volume);
        }
        void Pop(Vector3 at, string text, string hex, float size = .03f) {
            var t = WorldCaption(transform, "<color=#" + hex + ">" + text + "</color>", Vector3.zero, size);
            t.transform.position = at; floaters.Add(new FloatText { Text = t, Start = at });
        }
        GameObject GuestFor(int orderId) => guests.TryGetValue(orderId, out var g) && g != null && g.Root ? g.Root : null;
        GameObject StandGuestFor(int orderId) => standGuests.TryGetValue(orderId, out var g) && g ? g : null;

        void TickGameFeel(float dt) {
            clinkCooldown -= dt;
            var s = Game.State;
            if (s.Events != null && s.Events.Count > 0) {
                var batch = new List<string>(s.Events); s.Events.Clear();
                foreach (var e in batch) Play(e);
            }
            // The rush announces itself; the wind-down tells you to finish strong.
            string phase = ShiftPhase;
            if (phase != lastPhase) {
                if (phase == "rush") { Fx(SoundFx.Horn, .9f); Game.Notify("RUSH HOUR! Guests are pouring in" + (s.IsNight ? " (night rush: every order pays 30% more)." : "."), 4); }
                else if (phase == "wind" && lastPhase == "rush") Game.Notify("The rush is over. Finish the last guests strong.", 4);
                lastPhase = phase;
            }
            // The stand's lunch and night rushes announce themselves too.
            if (s.StandRush != lastStandRush) {
                if (s.StandRush) { Fx(SoundFx.Horn, .9f); Game.Notify((s.IsNight ? "NIGHT RUSH" : "LUNCH RUSH") + " at the truck! Customers are lining up.", 4); }
                else if (s.StandOpen) Game.Notify("The rush is over. Catch your breath: wash plates, clear tables, restock at Milo's cart.", 5);
                lastStandRush = s.StandRush;
            }
            if (s.StandRushSoon != lastRushSoon) {
                if (s.StandRushSoon) { Fx(SoundFx.Doorbell, .6f); Game.Notify((s.IsNight ? "Night" : "Lunch") + " rush coming! Get plates clean and patties ready.", 4); }
                lastRushSoon = s.StandRushSoon;
            }
            TickStationLoops();
            for (int i = floaters.Count - 1; i >= 0; i--) {
                var f = floaters[i]; f.Age += dt;
                if (!f.Text || f.Age > 1.8f) { if (f.Text) Destroy(f.Text.gameObject); floaters.RemoveAt(i); continue; }
                f.Text.transform.position = f.Start + Vector3.up * f.Age * .6f;
                if (Game.Player && Game.Player.View) f.Text.transform.rotation = Quaternion.LookRotation(f.Text.transform.position - Game.Player.View.transform.position);
            }
        }

        void Play(string e) {
            var parts = e.Split(':'); string kind = parts[0];
            int A(int i) => parts.Length > i && int.TryParse(parts[i], out var v) ? v : -1;
            switch (kind) {
                case "served": {
                    int score = A(2), tip = A(3); var guest = GuestFor(A(1));
                    Fx(SoundFx.Register, .8f);
                    if (guest) {
                        var m = guest.GetComponent<CharacterMotion>(); var head = guest.transform.position + Vector3.up * 2.1f;
                        if (score >= 85) { if (m) m.Cheer(); Pop(head, "Delicious!", "4FCB7A"); }
                        else if (score >= 60) Pop(head, "Tasty.", "F5E6B8", .025f);
                        else { if (m) m.Stomp(); Pop(head, "Meh...", "E8973A", .025f); }
                        if (tip > 0) { Fx(SoundFx.Tip, .8f); Pop(head + Vector3.up * .45f, "+$" + tip + " tip!", "F2C94C", .034f); }
                    }
                    break;
                }
                case "stand_served": {
                    Fx(SoundFx.Register, .7f); var guest = StandGuestFor(A(1));
                    if (guest) { var m = guest.GetComponent<CharacterMotion>(); if (m) m.Cheer(); if (A(2) > 0) { Fx(SoundFx.Tip, .7f); Pop(guest.transform.position + Vector3.up * 2.5f, "+$" + A(2) + " tip!", "F2C94C", .03f); } }
                    break;
                }
                case "walkout": {
                    Fx(SoundFx.Huff, .9f); var guest = GuestFor(A(1));
                    if (guest) { var m = guest.GetComponent<CharacterMotion>(); if (m) m.Stomp(); Pop(guest.transform.position + Vector3.up * 2.1f, "Too slow! I'm leaving.", "E1543B", .026f); }
                    break;
                }
                case "met": {
                    // First meal for a resident: they join the People book (recruitable with Flux from then on).
                    var def = ResidentCast.Get(parts.Length > 1 ? parts[1] : ""); if (def == null) break;
                    Fx(SoundFx.Tip, 1f);
                    foreach (var tag in FindObjectsByType<ResidentTag>(FindObjectsSortMode.None))
                        if (tag.Def == def) { Pop(tag.transform.position + Vector3.up * 2.95f, "NEW! " + def.Name + " added to your People book", "F2C94C", .03f); break; }
                    break;
                }
                case "upgrade": Fx(SoundFx.Tip, 1f); Fx(SoundFx.Register, .6f); break;
                case "queue_walkout": case "stand_walkout": Fx(SoundFx.Huff, .8f); break;
                case "arrive": Fx(SoundFx.Doorbell, .45f); break;
                case "chop": AcceptedChop(A(1)); break;
                case "wash": AcceptedWash(A(1)); break;
                case "discard": AcceptedDiscard(A(1)); break;
                case "emptybin": AcceptedEmptyBin(A(1)); break;
                case "stir": Fx(SoundFx.Stir, .8f); break;
                case "act": if (clinkCooldown <= 0) { clinkCooldown = .08f; var st = Game.State.Kitchen.Stations.Find(x => x.InstanceId == A(1)); Fx(st != null && (st.CatalogId == "pantry" || st.CatalogId == "trash") ? SoundFx.Thud : SoundFx.Clink, .5f); } break;
                case "burn": case "scorch": {
                    Fx(SoundFx.Warning, .9f); var obj = StationObject(A(1));
                    if (obj) Pop(obj.transform.position + Vector3.up * 1.9f, kind == "burn" ? "BURNT!" : "SCORCHED!", "E1543B", .032f);
                    break;
                }
                case "flipready": { Fx(SoundFx.Clink, .55f); var obj = StationObject(A(1)); if (obj) Pop(obj.transform.position + Vector3.up * 1.7f, "Flip it!", "F2C94C", .03f); break; }
                case "flip": {
                    string g = parts.Length > 2 ? parts[2] : ""; Fx(SoundFx.Thud, .55f); var obj = StationObject(A(1)); if (!obj) break;
                    var at = obj.transform.position + Vector3.up * 1.7f;
                    if (g == "perfect") { Fx(SoundFx.Tip, .6f); Pop(at, "Perfect flip!", "4FCB7A", .03f); }
                    else if (g == "almost") Pop(at, "Just a touch early", "E8C34A", .026f);
                    else if (g == "early") Pop(at, "Too early!", "E8973A", .026f);
                    else if (g == "late") Pop(at, "A little dark", "E8973A", .026f);
                    else if (g == "charred") Pop(at, "Charred!", "E1543B", .028f);
                    break;
                }
                case "pattydone": {
                    string g = parts.Length > 2 ? parts[2] : ""; var obj = StationObject(A(1)); if (!obj) break; var at = obj.transform.position + Vector3.up * 1.9f;
                    if (g == "perfect") { Fx(SoundFx.Tip, .9f); Pop(at, "PERFECT PATTY!", "F2C94C", .034f); }
                    else if (g == "good") { Fx(SoundFx.Clink, .6f); Pop(at, "Ready!", "4FCB7A", .028f); }
                    else Pop(at, g == "pale" ? "Ready, but pale on top. Flip next time!" : "Ready, but overdone", "E8973A", .024f);
                    break;
                }
                case "stirwarn": { Fx(SoundFx.Warning, .6f); var obj = StationObject(A(1)); if (obj) Pop(obj.transform.position + Vector3.up * 1.9f, "Stir it!", "E8973A", .028f); break; }
            }
        }

        // Grills sizzle while something cooks on them; stoves bubble while soup simmers. 3D, so you hear the station you're near.
        void TickStationLoops() {
            var k = Game.State.Kitchen;
            foreach (var st in k.Stations) {
                if (st.CatalogId != "grill" && st.CatalogId != "oven" && st.CatalogId != "stove") continue;
                var obj = StationObject(st.InstanceId); var item = k.At(st.InstanceId);
                bool on = obj && item != null && (KitchenState.GrillRaw(item.Kind) || KitchenState.GrillDone(item.Kind) || item.Kind == KitchenItemKind.SoupPot || item.Kind == KitchenItemKind.Soup);
                stationLoops.TryGetValue(st.InstanceId, out var src);
                if (on && !src) {
                    src = obj.AddComponent<AudioSource>(); src.loop = true; src.spatialBlend = 1; src.minDistance = 1.5f; src.maxDistance = 14; src.rolloffMode = AudioRolloffMode.Linear;
                    stationLoops[st.InstanceId] = src;
                }
                if (!src) continue;
                var clip = st.CatalogId == "stove" ? SoundFx.Bubble : SoundFx.Sizzle;
                if (on) {
                    if (src.clip != clip) src.clip = clip; src.volume = item.Kind == KitchenItemKind.CookedPatty ? .75f : .5f; src.pitch = 1;
                    if (st.CatalogId == "grill" && KitchenState.Flippable(item)) { float r = KitchenState.DownSide(item) / KitchenState.SideGolden("grill", Game.State.Restaurant.LevelOf(st.InstanceId)); src.volume = r < 1 ? .45f + r * .2f : r <= KitchenState.GoodHigh ? .85f : 1f; src.pitch = r < 1 ? 1 : r <= KitchenState.PerfectSide ? 1.12f : .85f; }
                    if (!src.isPlaying) src.Play();
                }
                else if (src.isPlaying) src.Stop();
            }
        }
    }
}
