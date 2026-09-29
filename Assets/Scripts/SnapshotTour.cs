using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RestaurantCity {
    // "--snapshots": a scripted camera tour that saves screenshots to <project>/Snapshots/ and quits.
    // Runs on a fresh in-memory game (never touches the player's save).
    public class SnapshotTour : MonoBehaviour {
        public CityGame Game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() {
            if (!Array.Exists(Environment.GetCommandLineArgs(), a => a == "--snapshots")) return;
            var game = FindFirstObjectByType<CityGame>(); if (!game) return;
            game.gameObject.AddComponent<SnapshotTour>().Game = game;
        }

        // name, position, yaw, pitch, clock (0-240; >150 is night)
        public static (string name, Vector3 pos, float yaw, float pitch, float clock)[] Shots = {
            ("01_street_day", new Vector3(-2, 0, -3), 20, 0, 60),
            ("02_stand_day", new Vector3(0, 0, 3.5f), 0, 8, 60),
            ("03_milo_day", new Vector3(-12, 0, 3.5f), 0, 5, 60),
            ("04_rival_day", new Vector3(16, 0, 8), 35, -8, 60),
            ("05_overview_day", new Vector3(0, 46, -24), 0, 36, 60),
            ("06_restaurant_day", new Vector3(-9, 0, -2), 180, 0, 60),
            ("08_intersection_day", new Vector3(-33, 0, -3), 60, 0, 60),
            ("09_north_ave_day", new Vector3(-20, 0, 50), 90, 0, 60),
            ("10_east_st_day", new Vector3(40, 0, -30), 0, 0, 60),
            ("11_street_night", new Vector3(-2, 0, -3), 20, 0, 190),
            ("12_north_ave_night", new Vector3(-20, 0, 50), 90, 0, 190),
            ("07_overview_night", new Vector3(0, 46, -24), 0, 36, 190),
            ("13_park_day", new Vector3(47, 0, -6), 140, 4, 60),
            ("14_gilded_day", new Vector3(19, 0, 3), 0, -24, 60),
            ("15_west_st_day", new Vector3(-40, 0, -75), 0, 0, 60),
            ("16_main_east_day", new Vector3(30, 0, -3), 90, 0, 60),
            ("17_alley_night", new Vector3(8, 0, 11), 25, -6, 190),
            ("18_restaurant_street_day", new Vector3(-10, 0, 4), 180, -14, 60),
            ("00_city_map_day", new Vector3(180, 620, 25), 0, 90, 60),
            ("24_aerial_day", new Vector3(-260, 200, -330), 42, 28, 60),
            ("19_docks_day", new Vector3(-40, 0, -150), 180, 0, 60),
            ("20_neon_night", new Vector3(172, 0, -2), 90, 0, 190),
            ("21_greenleaf_day", new Vector3(40, 0, 150), 0, 0, 60),
            ("22_gold_day", new Vector3(420, 0, 60), 0, -6, 60),
            ("23_bridge_day", new Vector3(440, 0, -100), 180, 4, 60),
            ("25_docks_lock_day", new Vector3(-40, 0, -96), 180, 6, 60),
            ("26_phone_map", new Vector3(0, 0, 3), 0, 0, 60),
            ("27_bayside_day", new Vector3(-182, 0, 5), 200, -8, 60),
            ("28_bayside_night", new Vector3(-182, 0, 5), 200, -8, 190),
            ("29_milo_front_day", new Vector3(-15.5f, 0, 8), 0, -4, 60),
            ("30_milo_inside_day", new Vector3(-15.5f, 0, 14.2f), 0, 6, 60),
            ("31_bayside_listing", new Vector3(-186, 0, -6), 180, 4, 60),
            ("32_stand_tables", new Vector3(-6.7f, 0, 7.4f), 0, 18, 60),
            ("33_milo_shop", new Vector3(-15.5f, 0, 14.2f), 0, 6, 60),
            ("34_stand_pantry", new Vector3(-2.4f, 0, 10.4f), 180, 14, 60),
            ("35_stand_front", new Vector3(0, 0, 3.4f), 0, 4, 60),
            ("36_pantry_from_street", new Vector3(-2.2f, 0, 6.0f), 0, 16, 60),
            ("37_menu_tab", new Vector3(-10, 0, -11), 180, 0, 60),
            ("38_cookbook_tab", new Vector3(-10, 0, -11), 180, 0, 60),
            ("39_stash_park", new Vector3(65, 0, -31), 0, 14, 195),
            ("40_stash_waterfront", new Vector3(-210, 0, -37), 0, 14, 195),
            ("41_stash_main_west", new Vector3(-120, 0, 2.5f), 0, 14, 195),
            ("42_stash_main_east", new Vector3(70, 0, -13.5f), 0, 14, 195),
            ("43_phone_zeeb", new Vector3(0, 0, 3), 0, 0, 60),
            ("44_style_kit_new", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("45_style_kit_old", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("46_style_kit_closeup", new Vector3(-8.2f, 0, 1.5f), 180, 10, 70),
            ("47_style_residents_pm", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("48_style_residents_closeup", new Vector3(-10.6f, 0, 1.6f), 180, 10, 70),
            ("49_cast_oldmarket_a", new Vector3(-9.5f, 0, 5.2f), 180, 6, 70),
            ("50_cast_oldmarket_b", new Vector3(-9.5f, 0, 5.2f), 180, 6, 70),
            ("51_cast_closeup_anim", new Vector3(-12.6f, 0, 1.4f), 180, 10, 70),
            ("57_food_showcase", new Vector3(-10.1f, 0, .75f), 180, 30, 70),
            ("58_grill_levels", new Vector3(-11f, 0, 3.4f), 180, 16, 70),
            ("59_grill_levels_night", new Vector3(-11f, 0, 3.4f), 180, 16, 190),
            ("55_night_inspector", new Vector3(-30, 0, -1.5f), 90, 4, 190),
            ("56_inspector_chase", new Vector3(-30, 0, -1.5f), 90, 4, 190),
            ("52_people_book", new Vector3(0, 0, 3), 0, 0, 60),
            ("53_people_book_top", new Vector3(0, 0, 3), 0, 0, 60),
        };

        IEnumerator Start() {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Snapshots"));
            if (Application.isEditor) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Snapshots"));
            Directory.CreateDirectory(dir);
            DistrictLocks.Suspended = true;
            // Probe: can a player-sized capsule walk from the sidewalk through Milo's door?
            if (GameObject.Find("Milo's walk-in")) {
                Physics.SyncTransforms();
                bool blocked = Physics.CapsuleCast(new Vector3(-15.5f, .45f, 10.5f), new Vector3(-15.5f, 1.45f, 10.5f), .3f, Vector3.forward, out var hit, 5.5f);
                Debug.LogWarning("DOOR_PROBE " + (blocked ? "blocked by " + hit.collider.name + " at z=" + hit.point.z.ToString("0.00") : "clear"));
            }
            yield return new WaitForSeconds(2);
            Game.State.StandBuilt = true; Game.SetPaused(false); Game.SyncWorld();
            foreach (var shot in Shots) {
                Game.State.Clock = shot.clock; Game.SyncWorld();
                RenderSettings.fog = !(shot.name.Contains("map") || shot.name.Contains("aerial"));
                var p = Game.Player; p.Teleport(shot.pos + Vector3.up * .1f);
                var cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
                p.transform.position = shot.pos; p.transform.rotation = Quaternion.Euler(0, shot.yaw, 0);
                p.View.transform.localRotation = Quaternion.Euler(shot.pitch, 0, 0);
                var rc = FindFirstObjectByType<RestaurantController>();
                if (rc && shot.name.Contains("phone_map")) rc.ShowPanel("Map");
                if (rc && (shot.name.Contains("stand_pantry") || shot.name.Contains("pantry_from_street"))) { foreach (var i in new[] { "patty", "bun" }) rc.Data.AddStock(i, 14); rc.Advance(.05f); }
                if (rc && (shot.name.Contains("menu_tab") || shot.name.Contains("cookbook_tab"))) { rc.Data.Owned = true; Game.State.Cash = 120; rc.ShowPanel(shot.name.Contains("menu") ? "Menu" : "Cookbook"); }
                if (rc && shot.name.Contains("stash_")) {
                    int spot = shot.name.Contains("park") ? 0 : shot.name.Contains("waterfront") ? 1 : shot.name.Contains("west") ? 2 : 3;
                    Game.State.Learn("midnight"); Game.State.DropBottles = 6; Game.State.DropPlaced = true; Game.State.StashSpot = spot; rc.Advance(.01f);
                    yield return null;
                    var crate = GameObject.Find("Night stash"); if (crate) { var c = crate.transform.position; p.transform.position = new Vector3(c.x, 0, c.z - 5.5f); Debug.LogWarning("STASH_SPOT " + spot + " at " + c); }
                }
                if (rc && shot.name.Contains("phone_zeeb")) { Game.State.Learn("midnight"); Game.State.DropBottles = 0; Game.State.ZeebDebt = 33; Game.State.Cash = 60; rc.Advance(.01f); rc.ShowPanel("Phone"); }
                if (shot.name.Contains("style_residents")) {
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform;
                    string[] ids = { "003_Jimmy", "043_Dracula", "044_Zombie", "002_CoolAlien", "051_Polybot", "049_CaptainLobster", "087_HotDog", "033_Franky" };
                    for (int i = 0; i < ids.Length; i++) { var c = ResidentModels.Spawn(ids[i], line); if (c) { c.transform.position = new Vector3(-14.4f + i * 1.4f, 0, -1.2f); c.transform.rotation = Quaternion.identity; } }
                    Game.State.StandOpen = false;
                }
                if (rc && (shot.name.Contains("night_inspector") || shot.name.Contains("inspector_chase"))) {
                    // An inspector on Main Street at night, flashlight on, calling STOP on a player carrying Zeeb's sauce.
                    var g = Game.State; g.Clock = 160; g.DropBottles = 2; g.DropPlaced = true; g.StashSpot = 0; var h = g.Kitchen.Hold(p.ActorId); if (h != null) g.Kitchen.Items.Remove(h); g.Kitchen.CollectStash(g, p.ActorId, out _);
                    rc.Advance(.05f); var insp = rc.Inspectors[0]; insp.Root.transform.position = new Vector3(-22, 0, -1.5f); insp.Root.transform.rotation = Quaternion.Euler(0, 270, 0);
                    for (int i = 0; i < 40; i++) { g.Clock = 160; p.transform.position = shot.pos; rc.Advance(.05f); if (insp.Mode == RestaurantController.InspectorMode.Stop) break; yield return null; }
                    if (shot.name.Contains("chase")) { insp.Mode = RestaurantController.InspectorMode.Chase; insp.Target = p; insp.Root.transform.position = shot.pos + new Vector3(12, 0, 0);
                        for (int i = 0; i < 25; i++) { g.Clock = 160; p.transform.position = shot.pos; insp.Lost = 0; rc.Advance(.04f); yield return null; } }
                }
                if (shot.name.Contains("food_showcase") || shot.name.Contains("grill_levels")) {
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform; Game.State.StandOpen = false;
                    if (shot.name.Contains("food")) {
                        foreach (float tx in new[] { -10.95f, -9.25f }) { var t = ArtOverrides.Find("Furniture", "assembly"); if (t) { var tb = Instantiate(t, line); tb.transform.position = new Vector3(tx, 0, -1.2f); } }
                        var dishes = new (string kind, string[] parts)[] {
                            ("PreparedPatty", null), ("CookedPatty", null), ("BurntPatty", null), ("Bun", null), ("ChoppedGreens", null),
                            ("Plate", new[] { "bun", "cooked_patty" }), ("Plate", new[] { "bun", "cooked_patty", "midnight_sauce" }), ("Plate", new[] { "chopped_greens" }), ("Plate", new[] { "soup" }), ("Soup", null) };
                        for (int i = 0; i < dishes.Length; i++) {
                            var it = KitchenArt.CreateItem(dishes[i].kind, dishes[i].parts == null ? null : new System.Collections.Generic.List<string>(dishes[i].parts), line);
                            bool plate = i >= 5; it.transform.position = new Vector3(plate ? -11.3f + (i - 5) * .55f : -11.05f + i * .55f, 1.05f, plate ? -1.0f : -1.55f);
                        }
                    } else {
                        for (int lv = 1; lv <= 3; lv++) {
                            var g = RestaurantArt.CreateFurniture("grill", line); ArtOverrides.Apply(g, "Furniture", "grill");
                            g.transform.position = new Vector3(-13.2f + (lv - 1) * 2.2f, 0, -1.2f); StationLooks.ApplyLevel(g, lv);
                            var pat = KitchenArt.CreateItem("CookedPatty", g.transform); pat.transform.localPosition = new Vector3(0, 1.0f, 0);
                        }
                    }
                    for (int i = 0; i < 20; i++) yield return null;
                }
                if (shot.name.Contains("inspector_candidates")) {
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform;
                    string[] ids = { "007_Observer", "068_AlwaysWatching", "075_Expol", "167_Mister_Contract", "047_David", "119_CaptainLantern" };
                    for (int i = 0; i < ids.Length; i++) {
                        var c = ResidentModels.Create(new ResidentDef(ids[i], ids[i], 1.8f, 0, StaffJob.Any, ""), line);
                        c.transform.position = new Vector3(-14f + i * 1.25f, 0, -1.2f); c.transform.rotation = Quaternion.identity;
                    }
                    Game.State.StandOpen = false;
                    for (int i = 0; i < 30; i++) yield return null;
                }
                if (shot.name.Contains("cast_")) {
                    // The Old Market cast, animated, at their real heights. Half of them walk in place, half idle.
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform;
                    var cast = ResidentCast.OldMarket; bool b = shot.name.Contains("_b");
                    int start = b ? 13 : 0, end = b ? cast.Length : 13;
                    for (int i = start; i < end; i++) {
                        var c = ResidentModels.Create(cast[i], line);
                        c.transform.position = new Vector3(-15f + (i - start) * .85f, 0, -1.2f); c.transform.rotation = Quaternion.identity;
                        var m = c.GetComponent<CharacterMotion>(); if (m) m.Walking = shot.name.Contains("closeup") && i % 2 == 1;
                        Debug.LogWarning("CAST " + cast[i].Name + " anim=" + (c.GetComponentInChildren<ResidentAnimator>() != null));
                    }
                    Game.State.StandOpen = false;
                    for (int i = 0; i < 40; i++) yield return null;
                }
                if (shot.name.Contains("style_kit")) {
                    // Character style test: the new kit cast vs. today's hand-coded characters, lined up on Main Street.
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform; bool legacy = shot.name.Contains("old");
                    int n = legacy ? 8 : RestaurantArt.StyleTestCast.Length;
                    for (int i = 0; i < n; i++) {
                        var c = legacy ? RestaurantArt.CreateCharacter(i, line) : RestaurantArt.BuildCharacter(RestaurantArt.StyleTestCast[i], line);
                        c.transform.position = new Vector3(-14.4f + i * 1.4f, 0, -1.2f); c.transform.rotation = Quaternion.identity;
                        var m = c.GetComponent<CharacterMotion>(); if (m) m.SetMood(.9f);
                    }
                    Game.State.StandOpen = false;
                }
                if (rc && shot.name.Contains("people_book")) {
                    var gs = Game.State; gs.StandBuilt = true; gs.Flux = 4; gs.MetResidents.Clear();
                    foreach (var id in new[] { "003_Jimmy", "038_Kate", "008_Hugo", "091_BigBro_a", "012_Chill", "046_Mafiossini" }) gs.MetResidents.Add(id);
                    if (!gs.Restaurant.Workers.Exists(w => w.Id == "038_Kate")) gs.Restaurant.Workers.Add(new WorkerState { Id = "038_Kate", Job = StaffJob.Cook });
                    rc.ShowPanel("Staff");
                    for (int i = 0; i < 3; i++) yield return null;
                    var sr = FindAnyObjectByType<UnityEngine.UI.ScrollRect>(); if (sr) sr.verticalNormalizedPosition = shot.name.Contains("top") ? 1f : .45f;
                    Debug.LogWarning("PEOPLE_BOOK scroll=" + (sr ? sr.verticalNormalizedPosition : -1));
                }
                if (rc && shot.name.Contains("milo_shop")) { Game.State.Cash = 95; rc.ShowPanel("Supplies"); }
                if (rc && shot.name.Contains("listing")) { Game.State.Cash = 95; rc.BuyRestaurant(Game.Player, "bayside"); }
                if (rc && shot.name.Contains("stand_tables")) {
                    // Two seated stand guests (one served and eating) and a dirty plate: walk them in, then shoot.
                    var st = Game.State; st.StandOpen = false; st.StandQueue.Clear();
                    st.StandQueue.Add(new StandOrder { Id = 901, Type = 3, Stage = 1, Table = 1, Patience = 999, MaxPatience = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 903, Type = 1, Stage = 2, Table = 0, Dish = "midnight", Patience = 999, MaxPatience = 999, EatLeft = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 902, Type = 6, Stage = 2, Table = 3, Dish = "salad", Patience = 999, MaxPatience = 999, EatLeft = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 904, Type = 2, Stage = 0, Dish = "burger", Patience = 999, MaxPatience = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 905, Type = 4, Stage = 0, Dish = "salad", Patience = 999, MaxPatience = 999 });
                    for (int i = 0; i < 70; i++) { rc.Advance(.15f); p.transform.position = shot.pos; yield return null; }
                }
                for (int i = 0; i < 20; i++) yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, shot.name + ".png"));
                for (int i = 0; i < 5; i++) yield return null;
                if (rc && rc.PanelOpen) rc.ClosePanel();
            }
            yield return new WaitForSeconds(1);
            Application.Quit();
        }
    }
}
