using System;
using System.Collections;
using System.IO;
using System.Linq;
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
            ("160_sign_main_west", new Vector3(-28, 0, 2), 300, -10, 60),
            ("161_sign_truckpark", new Vector3(-7, 0, -63), 29, -8, 60),
            ("162_sign_west_south", new Vector3(-38, 0, -62), 38, -8, 60),
            ("163_sign_main_odd", new Vector3(-14, 0, -2), 250, -6, 60),
            ("164_sign_main_west_night", new Vector3(-28, 0, 2), 300, -10, 195),
            ("165_alcblock_aerial", new Vector3(15, 34, -22), 0, 34, 60),
            ("166_alcblock_east", new Vector3(36, 0, 2), 295, -12, 60),
            ("167_alcblock_west", new Vector3(-4, 0, 2), 62, -12, 60),
            ("168_alcblock_front", new Vector3(16.25f, 0, -3), 0, -18, 60),
            ("169_alcblock_front_night", new Vector3(16.25f, 0, -3), 0, -18, 195),
            ("170_alcblock_yard", new Vector3(15, 0, 50), 180, -14, 60),
            ("02_stand_day", new Vector3(3, 0, -.5f), -20, 8, 60),
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
            ("15_west_st_day", new Vector3(-40, 0, -75), 0, 0, 60),
            ("16_main_east_day", new Vector3(30, 0, -3), 90, 0, 60),
            ("17_alley_night", new Vector3(8, 0, 11), 25, -6, 190),
            ("18_restaurant_street_day", new Vector3(-10, 0, 4), 180, -14, 60),
            ("00_city_map_day", new Vector3(0, 240, -20), 0, 90, 60),
            ("24_aerial_day", new Vector3(-105, 75, -105), 45, 28, 60),
            ("19_docks_gate_day", new Vector3(-40, 0, -62), 180, -4, 60),
            ("26_phone_map", new Vector3(0, 0, 3), 0, 0, 60),
            ("27_market_row_day", new Vector3(0, 0, 52), 0, 2, 60),
            ("28_market_row_night", new Vector3(0, 0, 52), 0, 2, 190),
            ("29_milo_front_day", new Vector3(-15.5f, 0, 8), 0, -4, 60),
            ("30_milo_inside_day", new Vector3(-15.5f, 0, 14.2f), 0, 6, 60),
            ("31_tin_diner_day", new Vector3(-20, 0, 49), 180, -8, 60),
            ("32_stand_tables", new Vector3(-1.2f, 0, 5.3f), 180, 22, 60),
            ("33_milo_shop", new Vector3(-15.5f, 0, 14.2f), 0, 6, 60),
            ("34_stand_pantry", new Vector3(-2.9f, 0, 9.3f), 0, 22, 60),
            ("35_stand_front", new Vector3(0, 0, 3.4f), 0, 4, 60),
            ("36_pantry_from_street", new Vector3(-2.2f, 0, 6.0f), 0, 16, 60),
            ("37_menu_tab", new Vector3(-10, 0, -11), 180, 0, 60),
            ("38_cookbook_tab", new Vector3(-10, 0, -11), 180, 0, 60),
            ("39_stash_park", new Vector3(65, 0, -31), 0, 14, 195),
            ("40_stash_alley", new Vector3(-22.5f, 0, -41), 0, 14, 195),
            ("41_stash_main_west", new Vector3(-70, 0, 2.5f), 0, 14, 195),
            ("42_stash_main_east", new Vector3(70, 0, -13.5f), 0, 14, 195),
            ("80_stash_market", new Vector3(6, 0, 62), 0, 14, 195),
            ("77_graffiti_alley_day", new Vector3(-22.5f, 0, -43), 0, 2, 60),
            ("78_vacant_lot_day", new Vector3(17.5f, 0, -44), 0, 4, 60),
            ("79_street_end_day", new Vector3(58, 0, 0), 90, 2, 60),
            ("84_food_truck_day", new Vector3(10.5f, 0, -37.5f), 40, 2, 60),
            ("85_food_truck_night", new Vector3(10.5f, 0, -37.5f), 40, 2, 190),
            ("86_pawn_shop", new Vector3(38.5f, 0, -27f), 290, 4, 60),
            ("87_pawn_panel", new Vector3(33f, 0, -25f), 270, 4, 60),
            ("88_fists_view", new Vector3(-6, 0, 3), 90, 0, 60),
            ("89_bat_view", new Vector3(-6, 0, 3), 90, 0, 60),
            ("91_service_tables", new Vector3(-10f, 0, -10.3f), 180, 18, 70),
            ("94_truck_park_day", new Vector3(18, 0, -50), 200, 8, 60),
            ("95_truck_park_night", new Vector3(18, 0, -50), 200, 8, 190),
            ("96_harbour_day", new Vector3(10, 0, -112), 250, 4, 60),
            ("97_courts_day", new Vector3(48, 0, -64), 125, 8, 60),
            ("98_harbor_road_west", new Vector3(-40, 0, -98), 270, 2, 60),
            ("99_sidewalk_main", new Vector3(-22, 0, -8), 90, 4, 60),
            ("100_truck_plaza", new Vector3(9, 0, -72), -50, 4, 60),
            ("101_truck_inside", new Vector3(1.2f, .82f, -62.3f), 270, 12, 60),
            ("102_truck_night", new Vector3(9, 0, -72), -50, 4, 190),
            ("103_gus_truck_lot", new Vector3(11.5f, 0, -38.5f), 40, 6, 60),
            ("104_gus_truck_street", new Vector3(23.5f, 0, -40.5f), 330, 6, 60),
            ("105_gus_truck_window", new Vector3(12.6f, 0, -30.4f), 90, -6, 60),
            ("106_milo_cart", new Vector3(-8.2f, 0, -69.2f), -20, 6, 60),
            ("107_tripo_cast_a", new Vector3(-1.45f, 0, -71.4f), 180, 7, 60),
            ("107_tripo_cast_b", new Vector3(-1.45f, 0, -71.4f), 180, 7, 60),
            ("108_cart_shop", new Vector3(-10.5f, 0, -65.6f), 0, 6, 60),
            ("109_tripo_cast_back_a", new Vector3(-1.45f, 0, -71.4f), 180, 7, 60),
            ("109_tripo_cast_back_b", new Vector3(-1.45f, 0, -71.4f), 180, 7, 60),
            ("120_alchemist_front_day", new Vector3(16.25f, 0, 2.5f), 0, -12, 60),
            ("121_alchemist_front_night", new Vector3(16.25f, 0, 2.5f), 0, -12, 195),
            ("122_alchemist_inside", new Vector3(16.25f, 0, 15.3f), 0, 2, 60),
            ("123_alchemist_balcony", new Vector3(14.5f, 3.02f, 30.4f), 180, 22, 60),
            ("124_alchemist_kitchen", new Vector3(13.5f, 0, 21.5f), 15, 4, 195),
            ("125_alchemist_yard", new Vector3(15f, 0, 44f), 180, -6, 60),
            ("126_alchemist_booths", new Vector3(12.5f, 0, 19.4f), 250, 8, 62),
            ("127_alchemist_boss", new Vector3(16.6f, 0, 23.4f), 335, 4, 62),
            ("128_alchemist_stairs", new Vector3(19.5f, 0, 19.5f), 40, -12, 62),
            ("129_alchemist_staff", new Vector3(14.6f, 0, 21.6f), 0, 6, 62),
            ("130_main_south_walk", new Vector3(-29f, 0, -7.6f), 90, 2, 60),
            ("131_main_oddtable", new Vector3(-10f, 0, 2.5f), 180, -4, 60),
            ("132_main_south_shops", new Vector3(14f, 0, 2.5f), 200, -4, 60),
            ("133_main_north_west", new Vector3(-22f, 0, -2.5f), 20, -4, 60),
            ("134_main_north_walk", new Vector3(29f, 0, 7.6f), 270, 2, 60),
            ("135_main_oddtable_night", new Vector3(-10f, 0, 2.5f), 180, -4, 195),
            ("136_main_south_east", new Vector3(3f, 0, 2.5f), 150, -4, 60),
            ("137_main_overview", new Vector3(0f, 14f, -1f), 270, 22, 60),
            ("138_main_shop_window", new Vector3(12.5f, 0, -6.2f), 180, 0, 60),
            ("139_main_south_night", new Vector3(14f, 0, 2.5f), 200, -4, 195),
            ("140_main_barber", new Vector3(-22.5f, 0, -7.2f), 180, 6, 60),
            ("141_main_shop_florist", new Vector3(2.5f, 0, -7.2f), 180, 6, 60),
            ("142_main_shop_gadgets", new Vector3(7.5f, 0, -7.2f), 180, 6, 60),
            ("143_main_shop_threads", new Vector3(17.5f, 0, -7.2f), 180, 6, 60),
            ("144_main_shop_gym", new Vector3(22.5f, 0, -7.2f), 180, 6, 60),
            ("145_main_shop_records", new Vector3(-22.5f, 0, 7.2f), 0, 6, 60),
            ("150_flux_fireescape_day", new Vector3(-7.5f, 0, 51f), 180, -38, 60),
            ("151_flux_roof_night", new Vector3(-7.5f, 30.6f, 40.0f), 180, 28, 195),
            ("152_flux_landing_night", new Vector3(-9.0f, 12.2f, 40.6f), 90, 22, 195),
            ("153_flux_roof_view_day", new Vector3(-7.5f, 30.6f, 40.2f), 0, 8, 60),
            ("154_flux_climb_day", new Vector3(-8.6f, 6.2f, 40.5f), 90, -20, 60),
            ("118_flux_vial_night", new Vector3(9, 0, -72), -50, 4, 195),
            ("118_flux_vial_day", new Vector3(9, 0, -72), -50, 4, 60),
            ("116_chop_lettuce", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("115_grill_flip", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("114_scrub_plate", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("110_truck_ui_hud", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("111_truck_ui_stars", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("112_truck_ui_menu", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("113_truck_ui_phone", new Vector3(-1.2f, .82f, -62.3f), 90, 6, 66),
            ("81_raid_planner", new Vector3(17.5f, 0, -40), 0, 4, 190),
            ("82_raid_fight", new Vector3(17.5f, 0, -45.5f), 0, 6, 190),
            ("83_raid_ko", new Vector3(17.5f, 0, -45.5f), 0, 8, 190),
            ("43_phone_zeeb", new Vector3(0, 0, 3), 0, 0, 60),
            ("44_style_kit_new", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("45_style_kit_old", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("46_style_kit_closeup", new Vector3(-8.2f, 0, 1.5f), 180, 10, 70),
            ("47_style_residents_pm", new Vector3(-9.5f, 0, 4.6f), 180, 6, 70),
            ("48_style_residents_closeup", new Vector3(-10.6f, 0, 1.6f), 180, 10, 70),
            ("49_cast_oldmarket_a", new Vector3(-9.5f, 0, 5.2f), 180, 6, 70),
            ("50_cast_oldmarket_b", new Vector3(-9.5f, 0, 5.2f), 180, 6, 70),
            ("51_cast_closeup_anim", new Vector3(-12.6f, 0, 1.4f), 180, 10, 70),
            ("68_starter_room", new Vector3(-10f, 0, -10.2f), 180, 8, 70),
            ("69_shop_teaser", new Vector3(-10f, 0, -11.3f), 180, 14, 70),
            ("70_storefront", new Vector3(-10f, 0, -2.6f), 180, -10, 70),
            ("71_storefront_night", new Vector3(-10f, 0, -2.6f), 180, -10, 190),
            ("72_walls_snap", new Vector3(-7.2f, 0, -10.3f), 215, 12, 70),
            ("73_walls_front_row", new Vector3(-10f, 0, -17.2f), 0, 8, 70),
            ("74_fridge_pantry", new Vector3(-9.9f, 0, -18.9f), 180, 16, 70),
            ("90_fridge_pantry_night", new Vector3(-9.9f, 0, -18.9f), 180, 16, 190),
            ("76_drink_machine", new Vector3(-7.2f, 0, -18.4f), 180, 24, 70),
            ("62_restaurant_dressed", new Vector3(-10f, 0, -11.3f), 180, 14, 70),
            ("63_restaurant_dressed_back", new Vector3(-5.6f, 0, -19.4f), -40, 12, 70),
            ("64_upgrade_panel", new Vector3(-10f, 0, -11.3f), 180, 14, 70),
            ("65_kitchen_grimy", new Vector3(-10f, 0, -16.5f), 180, 18, 70),
            ("66_plates_on_tables", new Vector3(-10f, 0, -11.3f), 180, 30, 70),
            ("67_shop_kitchen", new Vector3(-10f, 0, -11.3f), 180, 14, 70),
            ("60_dining_sets", new Vector3(-9f, 0, 4.2f), 180, 22, 70),
            ("61_dining_close", new Vector3(-12.2f, 0, 1.6f), 180, 28, 70),
            ("57_food_showcase", new Vector3(-10.1f, 0, .75f), 180, 30, 70),
            ("75_new_dishes", new Vector3(-10.1f, 0, .35f), 180, 34, 70),
            ("58_grill_levels", new Vector3(-11f, 0, 3.4f), 180, 16, 70),
            ("59_grill_levels_night", new Vector3(-11f, 0, 3.4f), 180, 16, 190),
            ("55_night_inspector", new Vector3(-30, 0, -1.5f), 90, 4, 190),
            ("56_inspector_chase", new Vector3(-30, 0, -1.5f), 90, 4, 190),
            ("52_people_book", new Vector3(0, 0, 3), 0, 0, 60),
            ("53_people_book_top", new Vector3(0, 0, 3), 0, 0, 60),
            ("92_zilo_staff", new Vector3(-10f, 0, -10.2f), 180, 8, 70),
            ("93_zilo_closeup", new Vector3(-9f, 0, -10.4f), 180, 3, 70),
        };

        static bool dressed;
        bool truckOwned0; readonly System.Collections.Generic.List<GameObject> fluxShots = new System.Collections.Generic.List<GameObject>();
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
            string only = Array.Find(Environment.GetCommandLineArgs(), a => a.StartsWith("--snapshot-only="));
            if (only != null) only = only.Substring("--snapshot-only=".Length);
            foreach (var shot in Shots) {
                if (only != null && !Array.Exists(only.Split(','), o => shot.name.Contains(o))) continue;
                Game.State.Clock = shot.clock; Game.SyncWorld();
                RenderSettings.fog = !(shot.name.Contains("map") || shot.name.Contains("aerial"));
                // Stand shots are written in the stand's own frame, which sits inside Little Flame in the city.
                var pos = shot.pos; if (Game.Stand && (shot.name.Contains("stand") || shot.name.Contains("pantry_from_street"))) { var so = Game.Stand.transform.position; pos += new Vector3(so.x, shot.pos.z > 8.5f ? so.y : 0, so.z); }
                var p = Game.Player; p.Teleport(pos + Vector3.up * .1f);
                var cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
                p.transform.position = pos; p.transform.rotation = Quaternion.Euler(0, shot.yaw, 0);
                p.View.transform.localRotation = Quaternion.Euler(shot.pitch, 0, 0);
                var rc = FindFirstObjectByType<RestaurantController>();
                if (rc && shot.name.Contains("zilo_")) {
                    var d = Game.State.Restaurant;
                    d.Owned = true;
                    if (!Game.State.MetResidents.Contains("201_TripoAlien")) Game.State.MetResidents.Add("201_TripoAlien");
                    if (!d.Workers.Exists(w => w.Id == "201_TripoAlien")) {
                        Game.State.Flux = ResidentCast.Get("201_TripoAlien").FluxCost;
                        if (!d.Hire(Game.State, "201_TripoAlien", out var hireMessage) || Game.State.Flux != 0)
                            throw new Exception("Zilo five-Flux recruitment failed: " + hireMessage);
                        Debug.LogWarning("ZILO_HIRED " + hireMessage);
                    }
                    rc.RebuildLayout();
                    rc.Advance(.05f);
                    var zilo = GameObject.Find("Zilo");
                    if (zilo) {
                        zilo.transform.position = shot.name.Contains("closeup") ? new Vector3(-9f, .055f, -12.0f) : new Vector3(-9f, .055f, -13.1f);
                        zilo.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }
                    Debug.LogWarning("ZILO_STAFF " + (zilo ? "spawned" : "missing") + " cost=" + ResidentCast.Staff("201_TripoAlien")?.FluxCost);
                }
                if (rc && shot.name.Contains("phone_map")) rc.ShowPanel("Map");
                // Flux hunt shots: tonight's cases on the tallest roof and its fire-escape landing, and the midnight strongbox.
                if (rc && shot.name.Contains("flux")) { var fs = Game.State; rc.Data.Owned = true; fs.KnownRecipes.Remove("midnight"); fs.Clock = shot.clock; fs.FluxNight = fs.Day; fs.FluxCases = new System.Collections.Generic.List<int> { 10, 8 }; rc.Advance(.05f); }
                // The Alchemist's dining room comes alive as you walk in: let the crew and the first guests settle.
                if (rc && shot.name.Contains("alchemist") && !shot.name.Contains("front") && !shot.name.Contains("yard")) {
                    var alchP = Game.Player; for (int i = 0; i < 90; i++) { Game.State.Clock = shot.clock; alchP.transform.position = shot.pos; rc.Advance(.1f); yield return null; }
                }
                if (rc && (shot.name.Contains("stand_pantry") || shot.name.Contains("pantry_from_street"))) { foreach (var i in new[] { "patty", "bun" }) rc.Data.AddStock(i, 14); rc.Advance(.05f); }
                if (rc && (shot.name.Contains("menu_tab") || shot.name.Contains("cookbook_tab"))) { rc.Data.Owned = true; Game.State.Cash = 120; rc.ShowPanel(shot.name.Contains("menu") ? "Menu" : "Cookbook"); }
                if (rc && shot.name.Contains("stash_")) {
                    int spot = shot.name.Contains("park") ? 0 : shot.name.Contains("alley") ? 1 : shot.name.Contains("west") ? 2 : shot.name.Contains("market") ? 4 : 3;
                    Game.State.Learn("midnight"); Game.State.DropBottles = 6; Game.State.DropPlaced = true; Game.State.StashSpot = spot; rc.Advance(.01f);
                    yield return null;
                    var crate = GameObject.Find("Night stash"); if (crate) { var c = crate.transform.position; p.transform.position = new Vector3(c.x, 0, c.z - 5.5f); Debug.LogWarning("STASH_SPOT " + spot + " at " + c); }
                }
                if (rc && shot.name.Contains("phone_zeeb")) { Game.State.Learn("midnight"); Game.State.DropBottles = 0; Game.State.ZeebDebt = 33; Game.State.Cash = 60; rc.Advance(.01f); rc.ShowPanel("Phone"); }
                if (rc && shot.name.Contains("tripo_cast")) {
                    // The custom (Tripo) cast, six at a time, in their idle animation and named, from the front or the back.
                    var old = GameObject.Find("Tripo lineup"); if (old) Destroy(old);
                    var line = new GameObject("Tripo lineup").transform;
                    string[] all = { "206_TripoCheerleader", "207_TripoGothGirl", "208_TripoConstruction", "209_TripoFootball", "210_TripoClown", "201_TripoAlien",
                                     "205_TripoVampire", "204_TripoReaper", "211_TripoPumpkin", "212_TripoFrank", "214_TripoAlchemist", "215_TripoFrankie", "202_GreasyGus", "203_GusImp" };
                    bool second = shot.name.EndsWith("_b"), back = shot.name.Contains("back");
                    for (int i = 0; i < 6; i++) {
                        string id = all[i + (second ? 6 : 0)];
                        var def = ResidentCast.Get(id) ?? new ResidentDef(id, id == "202_GreasyGus" ? "Greasy Gus" : "Gus's imp", ResidentCast.CustomResidentHeight, 2, StaffJob.Cook, "");
                        var c = ResidentModels.Create(def, line);
                        c.transform.SetPositionAndRotation(new Vector3(-6.2f + i * 1.9f, 0, -77.4f), Quaternion.Euler(0, back ? 180 : 0, 0));
                        var label = rc.WorldCaption(c.transform, def.Name + "\n<size=40>" + (def.Rarity) + "</size>", new Vector3(0, def.Height + .35f, 0), .018f);
                        label.transform.rotation = Quaternion.Euler(0, 180, 0);
                    }
                    for (int i = 0; i < 40; i++) { rc.Advance(.05f); yield return null; }   // let the idle animation settle
                }
                if (shot.name.Contains("style_residents")) {
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform;
                    string[] ids = { "003_Jimmy", "205_TripoVampire", "044_Zombie", "002_CoolAlien", "051_Polybot", "049_CaptainLobster", "087_HotDog", "033_Franky" };
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
                if (rc && shot.name.Contains("stand_tables")) {
                    // Plates left on the sidewalk tables, to check they sit on the wooden tabletops.
                    var dirty = Game.State.StandTableDirty; while (dirty.Count < 4) dirty.Add(false); for (int i = 0; i < 4; i++) dirty[i] = true;
                    rc.Advance(.02f); for (int i = 0; i < 5; i++) yield return null;
                }
                if (rc && shot.name.Contains("fridge_pantry")) {
                    // Dry goods in the pantry, cold food behind the fridge's glass door (it opens as you step up).
                    var gs = Game.State; gs.Cash = 20000; var d = gs.Restaurant; d.Owned = true; d.Layout.Clear(); gs.Learn("soup"); gs.Learn("cyclops"); gs.Learn("cometdog");
                    Debug.LogWarning("FRIDGE place pantry=" + d.Place(gs, "pantry", 3, 0, 0, out var w1) + " " + w1 + " fridge=" + d.Place(gs, "fridge", 5, 0, 0, out var w2) + " " + w2);
                    foreach (var (id, n) in new[] { ("patty", 17), ("greens", 38), ("soup_veg", 21), ("bun", 18), ("midnight_sauce", 3), ("egg", 11), ("sausage", 4) }) d.AddStock(id, n);
                    rc.RebuildLayout(); for (int i = 0; i < 40; i++) { p.transform.position = shot.pos; rc.Advance(.02f); yield return null; }
                }
                if (rc && shot.name.Contains("drink_machine")) {
                    // The Swirl & Fizz machine pouring, beside the fridge with sausages on its top shelf.
                    var gs = Game.State; var d = gs.Restaurant; d.Rank = 2; gs.Learn("float"); gs.Learn("cometdog");
                    Debug.LogWarning("DRINK place=" + d.Place(gs, "drink_machine", 7, 0, 0, out var dw) + " " + dw);
                    d.AddStock("sausage", 8); d.AddStock("moonberry", 4); rc.RebuildLayout(); gs.Kitchen.EnsureStations(d);
                    var dm = d.Layout.Find(x => x.CatalogId == "drink_machine");
                    if (dm != null) { gs.Kitchen.Act(gs, "snap", dm.InstanceId, "", out var m1); gs.Kitchen.Work(gs, "snap", dm.InstanceId, KitchenState.PourSeconds + .1f, out var m2); Debug.LogWarning("DRINK " + m1 + " / " + m2); }
                    // A Comet Dog sausage cooking on the grill next to it, with its progress bar.
                    Debug.LogWarning("GRILL place=" + d.Place(gs, "grill", 9, 0, 0, out var gw) + " " + gw); rc.RebuildLayout(); gs.Kitchen.EnsureStations(d);
                    var fr = d.Layout.Find(x => x.CatalogId == "fridge"); var gr = d.Layout.Find(x => x.CatalogId == "grill");
                    if (fr != null && gr != null) { gs.Kitchen.Act(gs, "snap2", fr.InstanceId, "sausage", out var g1); gs.Kitchen.Act(gs, "snap2", gr.InstanceId, "", out var g2); Debug.LogWarning("GRILL " + g1 + " / " + g2); }
                    for (int i = 0; i < 70; i++) { p.transform.position = shot.pos; rc.Advance(.06f); yield return null; }
                    if (gr != null) Debug.LogWarning("GRILL after 4.2s: " + gs.Kitchen.At(gr.InstanceId)?.Kind + " progress " + gs.Kitchen.Stations.Find(x => x.InstanceId == gr.InstanceId)?.Progress);
                }
                if (rc && shot.name.Contains("walls_snap")) {
                    // A kitchen wall across the room (with a doorway gap), a service window, a T-joint wall, and
                    // furniture in the front row by the windows.
                    var gs = Game.State; gs.Cash = 20000; var d = gs.Restaurant; d.Owned = true; d.Layout.Clear(); d.SurfaceFinishes.Clear();
                    foreach (var (id, x, z, r) in new[] { ("partition_wall", 0, 6, 0), ("partition_wall", 2, 6, 0), ("service_window", 4, 6, 0), ("partition_wall", 6, 6, 0), ("partition_wall", 10, 6, 0), ("partition_wall", 3, 4, 1), ("cafe_table", 1, 10, 0), ("booth_teal", 8, 10, 0), ("fern", 11, 11, 0) })
                        Debug.LogWarning("WALLS " + id + " placed=" + d.Place(gs, id, x, z, r, out var why) + " " + why);
                    var wid = d.Layout.Find(q => q.CatalogId == "partition_wall"); if (wid != null) d.ApplyFinish(gs, "wall_teal", "piece:" + wid.InstanceId, false, out _);
                    rc.RebuildLayout(); for (int i = 0; i < 5; i++) yield return null;
                }
                if (rc && shot.name.Contains("starter_room")) {
                    // The room you get for $150, before any renovation: pack flagstone and whitewashed brick, still grubby.
                    var d = Game.State.Restaurant; d.Owned = true; d.Rank = 1; d.Layout.Clear(); rc.RebuildLayout(); for (int i = 0; i < 5; i++) yield return null;
                }
                if (rc && shot.name.Contains("shop_teaser")) { var d = Game.State.Restaurant; d.Rank = 1; foreach (var id in new[] { "grill", "sink", "prep_bench" }) d.Place(Game.State, id, 0, 0, 0, out _); rc.ShowPanel("Catalog"); for (int i = 0; i < 5; i++) yield return null; }
                if (rc && shot.name.Contains("restaurant_dressed") && !dressed) {
                    dressed = true;
                    // A fully dressed Odd Table: buy it, then place a pack-furnished dining room and level-2/3 gear.
                    var gs = Game.State; gs.Cash = 20000; var d = gs.Restaurant; d.Owned = true; d.Rank = 2; d.Layout.Clear();
                    string[] wants = { "grill", "stove", "prep_bench", "assembly", "sink", "pantry", "counter", "trash", "booth_teal", "bistro_table", "cafe_table", "booth_coral", "communal_table", "stool_pair", "flower_stand", "fern", "cafe_divider",
                                       "sign_burger", "menu_screen", "wall_tv", "art_abstract", "poster_wall", "wall_sconce", "wall_sconce", "industrial_pendant", "industrial_pendant", "pendant_amber", "pendant_amber", "plate_rack" };
                    foreach (var id in wants) {
                        bool done = false;
                        for (int z = 0; z < 10 && !done; z++) for (int x = 0; x < 12 && !done; x++) done = d.Place(gs, id, x, z, id.StartsWith("sign") || id.StartsWith("menu") || id.StartsWith("wall") || id.StartsWith("art") || id.StartsWith("poster") ? 2 : 0, out _);
                        Debug.LogWarning("DRESS " + id + " placed=" + done);
                    }
                    foreach (var item in d.Layout) if (StationUpgrades.CanUpgrade(item.CatalogId)) item.Level = item.CatalogId == "grill" ? 3 : 2;
                    rc.RebuildLayout(); for (int i = 0; i < 10; i++) yield return null;
                }
                if (rc && shot.name.Contains("kitchen_grimy")) { foreach (var f in Game.State.Restaurant.Layout) f.Level = 1; rc.RebuildLayout(); for (int i = 0; i < 5; i++) yield return null; }
                if (rc && shot.name.Contains("plates_on_tables")) {
                    var k = Game.State.Kitchen;
                    foreach (var f in Game.State.Restaurant.Layout) {
                        var c = RestaurantCatalog.Find(f.CatalogId); if (c == null || c.Seats <= 0) continue;
                        for (int seatNo = 1; seatNo <= c.Seats; seatNo++) k.Items.Add(new KitchenItem { Id = k.NextItemId++, Kind = seatNo % 2 == 0 ? KitchenItemKind.DirtyPlate : KitchenItemKind.Plate, Components = seatNo % 2 == 0 ? new System.Collections.Generic.List<string>() : new System.Collections.Generic.List<string> { "bun", "cooked_patty" }, Holder = "table:0", TableInstanceId = f.InstanceId, SeatNumber = seatNo });
                    }
                    rc.Advance(.02f); for (int i = 0; i < 5; i++) yield return null;
                }
                if (rc && shot.name.Contains("shop_kitchen")) { rc.ShowPanel("Catalog"); for (int i = 0; i < 5; i++) yield return null; }
                if (rc && shot.name.Contains("upgrade_panel")) {
                    var sink = Game.State.Restaurant.Layout.Find(x => x.CatalogId == "sink"); if (sink != null) { sink.Level = 1; Game.State.Cash = 400; rc.SelectFurnitureItem(sink.InstanceId); }
                    for (int i = 0; i < 5; i++) yield return null;
                }
                if (shot.name.Contains("dining_")) {
                    // Pack seating sets with residents actually sitting on their seats, to check chair height and facing.
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform; Game.State.StandOpen = false;
                    string[] sets = { "patio_table", "stool_pair", "booth_teal", "communal_table", "fern", "trash" };
                    float x = -15f; int who = 0;
                    foreach (var id in sets) {
                        var f = RestaurantArt.CreateFurniture(id, line); ArtOverrides.Apply(f, "Furniture", id);
                        var item = RestaurantCatalog.Find(id); float w = item != null ? item.Width : 1;
                        f.transform.position = new Vector3(x + w * .5f, 0, -1.2f); x += w + .4f;
                        for (int sIdx = 0; sIdx < 6; sIdx++) {
                            var seat = f.transform.Find("Seat_" + sIdx); if (!seat || sIdx % 2 == 1 && id == "communal_table") continue;
                            var r = ResidentModels.Create(ResidentCast.OldMarket[(who++ * 5) % ResidentCast.OldMarket.Length], line);
                            r.transform.SetPositionAndRotation(seat.position, seat.rotation); var m = r.GetComponent<CharacterMotion>(); if (m) m.Seated = true;
                        }
                    }
                    for (int i = 0; i < 30; i++) yield return null;
                }
                if (shot.name.Contains("new_dishes")) {
                    // Comet Dog and Moonberry Float, plated, with sausages raw / cooked / burnt.
                    var old = GameObject.Find("Style lineup"); if (old) Destroy(old);
                    var line = new GameObject("Style lineup").transform; Game.State.StandOpen = false;
                    var t = ArtOverrides.Find("Furniture", "assembly"); if (t) { var tb = Instantiate(t, line); tb.transform.position = new Vector3(-10.1f, 0, -1.2f); }
                    var parts = new (string kind, string[] comps, float x, float z)[] { ("Plate", new[] { "bun", "cooked_sausage" }, -10.55f, -1.05f), ("Plate", new[] { "float" }, -9.7f, -1.05f),
                        ("RawSausage", null, -10.7f, -1.5f), ("CookedSausage", null, -10.3f, -1.5f), ("BurntSausage", null, -9.9f, -1.5f) };
                    foreach (var q in parts) { var it = KitchenArt.CreateItem(q.kind, q.comps == null ? null : new System.Collections.Generic.List<string>(q.comps), line); it.transform.position = new Vector3(q.x, 1.05f, q.z); }
                    for (int i = 0; i < 20; i++) yield return null;
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
                    foreach (var id in new[] { "003_Jimmy", "038_Kate", "208_TripoConstruction", "091_BigBro_a", "012_Chill", "201_TripoAlien" }) gs.MetResidents.Add(id);
                    if (!gs.Restaurant.Workers.Exists(w => w.Id == "038_Kate")) gs.Restaurant.Workers.Add(new WorkerState { Id = "038_Kate", Job = StaffJob.Cook });
                    rc.ShowPanel("Staff");
                    for (int i = 0; i < 3; i++) yield return null;
                    var sr = FindAnyObjectByType<UnityEngine.UI.ScrollRect>(); if (sr) sr.verticalNormalizedPosition = shot.name.Contains("top") ? 1f : .45f;
                    Debug.LogWarning("PEOPLE_BOOK scroll=" + (sr ? sr.verticalNormalizedPosition : -1));
                }
                if (rc && shot.name.Contains("raid_")) {
                    var gs = Game.State; rc.Data.Rank = 0; string[] crewIds = { "091_BigBro_a", "204_TripoReaper", "201_TripoAlien" };
                    foreach (var id in crewIds) if (!rc.Data.Workers.Exists(w => w.Id == id)) rc.Data.Workers.Add(new WorkerState { Id = id, Job = StaffJob.Cook, Energy = 100 });
                    if (shot.name.Contains("planner")) { gs.Raids.Clear(); rc.RaidCrew.Clear(); rc.OpenRaid("gus"); rc.ToggleRaidCrew(crewIds[0]); rc.ToggleRaidCrew(crewIds[1]); }
                    else {
                        if (!rc.ActiveRaid) { gs.Raids.Clear(); rc.RaidCrew.Clear(); foreach (var id in crewIds) rc.ToggleRaidCrew(id); rc.StartRaid(out var rm); Debug.LogWarning("RAID_START " + rm); }
                        var b = rc.ActiveRaid;
                        if (b) {
                            b.BossPassive = true;
                            if (shot.name.Contains("fight")) yield return new WaitForSeconds(3.2f);
                            else { float t = 0; while (t < 30 && b.Fighters.Any(f => !f.Ours && !f.Boss && !f.Down)) { t += Time.deltaTime; yield return null; } yield return new WaitForSeconds(2.5f); }
                            Debug.LogWarning("RAID_STATE goons down " + b.Fighters.Count(f => !f.Ours && !f.Boss && f.Down) + " crew down " + b.Fighters.Count(f => f.Ours && f.Down));
                        }
                    }
                }
                if (rc && shot.name.Contains("pawn_panel")) { Game.State.Cash = 120; rc.PawnBuyer = 0; rc.ShowPanel("Pawn"); }
                if (shot.name.Contains("bat_view")) { var inv = Hotbar.For(Game.State, 0); if (!inv.Has("bat")) Hotbar.Give(Game.State, 0, "bat", out _); Hotbar.Give(Game.State, 0, "knuckles", out _); Hotbar.Select(Game.State, 0, inv.Slots.FindIndex(x => x.Item == "bat"), out _); }
                if (rc && shot.name.Contains("service_tables")) {
                    // Three numbered tables with seated guests: the number cards, bubbles and ticket rail should agree.
                    var gs = Game.State; var d = gs.Restaurant; gs.Cash = 5000; d.Owned = true; d.Layout.Clear(); d.Orders.Clear(); d.Open = false;
                    foreach (var (id, gx, gz) in new[] { ("pantry", 0, 0), ("grill", 3, 0), ("prep_bench", 6, 0), ("plate_rack", 9, 0) }) d.Place(gs, id, gx, gz, 0, out _);
                    foreach (var (tx, tz) in new[] { (1, 7), (5, 7), (9, 7) }) d.Place(gs, "cafe_table", tx, tz, 0, out _);
                    d.ActiveMenu = new System.Collections.Generic.List<string> { "burger", "salad" };
                    rc.RebuildLayout(); d.Open = true; int k = 0;
                    foreach (var t in d.Layout.Where(pl => pl.CatalogId == "cafe_table")) d.AddCustomer(gs, (k++ * 3) % RestaurantCatalog.Customers.Length, t.InstanceId, out _, ResidentCast.OldMarket[k * 2].Id);
                    for (int i = 0; i < 260; i++) { p.transform.position = shot.pos; rc.Advance(.12f); yield return null; }
                    Debug.LogWarning("SERVICE_SHOT orders " + d.Orders.Count);
                    if (d.Orders.Count > 0) d.Orders[0].Wait = d.PatienceOf(d.Orders[0]) * .8f;
                    for (int i = 0; i < 10; i++) { p.transform.position = shot.pos; rc.Advance(.02f); yield return null; }
                }
                if (rc && shot.name.Contains("gus_truck")) { for (int i = 0; i < 150; i++) { rc.Advance(.2f); p.transform.position = pos; yield return null; } }
                if (rc && shot.name.Contains("grill_flip")) {
                    // A patty flipped at golden: seared top with grill marks, underside nearly there, prompt says what to do.
                    var gs = Game.State; gs.StandBuilt = true; var k = gs.Kitchen;
                    var grill = k.Stations.Find(s => s.CatalogId == "grill" && KitchenState.IsStandStation(s.InstanceId));
                    if (grill != null) {
                        foreach (var it in k.Items.FindAll(i => i.Holder == "station:" + grill.InstanceId)) k.Items.Remove(it);
                        float g = KitchenState.SideGolden("grill", 1);
                        k.Items.Add(new KitchenItem { Id = k.NextItemId++, Kind = KitchenItemKind.RawProtein, Holder = "station:" + grill.InstanceId, SideA = g * 1.15f, SideB = g * .75f, Flips = 1 });
                        grill.Progress = g * 1.9f;
                        var obj = rc.StationObject(grill.InstanceId).transform; var top = obj.TransformPoint(GrillFeedback.GrillTop(obj));
                        p.Teleport(new Vector3(top.x, .82f, top.z) + Vector3.ProjectOnPlane(obj.forward, Vector3.up).normalized * .9f); p.LookAt(top);
                        pos = p.transform.position;
                        for (int i = 0; i < 25; i++) { p.transform.position = pos; yield return null; }
                    }
                }
                if (shot.name.Contains("flux_vial")) {
                    // A Flux vial at eye level a step in front of you, and one on the ground further off.
                    for (int i = 0; i < 6; i++) { p.transform.position = pos; yield return null; }
                    var cam = p.View.transform; var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                    fluxShots.Add(FluxVial.Create(null, cam.position + fwd * .75f + Vector3.down * .32f + cam.right * .08f, .32f));
                    fluxShots.Add(FluxVial.Create(null, new Vector3(cam.position.x, 0.02f, cam.position.z) + fwd * 3.2f - cam.right * 1.1f, .32f));
                    for (int i = 0; i < 30; i++) { p.transform.position = pos; yield return null; }
                }
                if (rc && shot.name.Contains("chop_lettuce")) {
                    // Lettuce up close, two slices chopped, knife lined up over the next.
                    var gs = Game.State; gs.StandBuilt = true; var k = gs.Kitchen;
                    var board = k.Stations.Find(s => s.CatalogId == "prep_bench" && KitchenState.IsStandStation(s.InstanceId));
                    if (board != null) {
                        foreach (var it in k.Items.FindAll(i => i.Holder == "station:" + board.InstanceId)) k.Items.Remove(it);
                        board.Progress = 0; board.WorkOwner = null;
                        k.Items.Add(new KitchenItem { Id = k.NextItemId++, Kind = KitchenItemKind.RawGreens, Holder = "station:" + board.InstanceId });
                        var chop = PrepChop.Of(p); chop.Begin(board.InstanceId, out _);
                        for (int i = 0; i < 10; i++) { p.transform.position = pos; chop.Tick(null, null, null, .05f); yield return null; }
                        chop.ChopAt(-.1f, -.06f); chop.ChopAt(-.025f); chop.Tick(null, null, null, .01f);
                        for (int i = 0; i < 4; i++) { chop.Tick(null, null, null, .01f); yield return null; }
                    }
                }
                if (rc && shot.name.Contains("scrub_plate")) {
                    // Lift a dirty plate out of the truck sink and scrub a few strokes: half grimy, half clean, suds on.
                    var gs = Game.State; gs.StandBuilt = true; var k = gs.Kitchen;
                    var sink = k.Stations.Find(s => s.CatalogId == "sink" && KitchenState.IsStandStation(s.InstanceId));
                    if (sink != null && k.At(sink.InstanceId) == null) { var d = new KitchenItem { Id = k.NextItemId++, Kind = KitchenItemKind.DirtyPlate, StandPlate = true, Holder = "station:" + sink.InstanceId }; k.Items.Add(d); }
                    var scrub = PlateScrub.Of(p); bool began = sink != null && scrub.Begin(sink.InstanceId, out var why);
                    Debug.LogWarning("SCRUB_SHOT began=" + began);
                    for (int i = 0; i < 12; i++) { p.transform.position = pos; scrub.Tick(null, null, null, .05f); yield return null; }
                    var path = new System.Collections.Generic.List<Vector2>(); for (int i = 0; i <= 40; i++) { float t = i / 40f; path.Add(new Vector2(.25f + t * .45f, .5f + Mathf.Sin(t * 30) * .17f)); }
                    scrub.ScrubPath(path.ToArray());
                    for (int i = 0; i < 3; i++) { scrub.Tick(null, null, null, .02f); yield return null; }
                    Debug.LogWarning("SCRUB_SHOT clean=" + scrub.CleanRatio);
                }
                if (rc && shot.name.Contains("truck_ui")) {
                    // Truck phase (no restaurant yet): the goal card, the road to the first star, the read-only menu, the phone.
                    var gs = Game.State; var d = gs.Restaurant; if (shot.name.Contains("hud")) truckOwned0 = d.Owned; d.Owned = false; gs.StandBuilt = true; gs.StandOpen = true; gs.Cash = 140;
                    if (d.Served < 5) { d.Served = 5; d.Reviews.Clear(); d.RecordTruckGuest("", "burger", .9f); d.RecordTruckGuest("", "salad", .5f); d.RecordTruckWalkout("", "burger"); d.Served = 5; d.Rank = 0; }
                    for (int i = 0; i < 10; i++) { rc.Advance(.05f); p.transform.position = pos; yield return null; }
                    if (shot.name.Contains("stars")) rc.ShowPanel("Reviews");
                    if (shot.name.Contains("menu")) rc.ShowPanel("Menu");
                    if (shot.name.Contains("phone")) rc.ShowPanel("Phone");
                }
                if (rc && shot.name.Contains("cart_shop")) { Game.State.Cash = 95; rc.ShowPanel("Supplies"); Debug.LogWarning("CART_SHOP atCart=" + rc.AtCart + " atSupplier=" + rc.AtSupplier); }
                if (rc && shot.name.Contains("milo_shop")) { Game.State.Cash = 95; rc.ShowPanel("Supplies"); }
                if (rc && shot.name.Contains("stand_tables")) {
                    // Two seated stand guests (one served and eating) and a dirty plate: walk them in, then shoot.
                    var st = Game.State; st.StandOpen = false; st.StandQueue.Clear();
                    st.StandQueue.Add(new StandOrder { Id = 901, Type = 3, Stage = 1, Table = 1, Patience = 999, MaxPatience = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 903, Type = 1, Stage = 2, Table = 0, Dish = "midnight", Patience = 999, MaxPatience = 999, EatLeft = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 902, Type = 6, Stage = 2, Table = 3, Dish = "salad", Patience = 999, MaxPatience = 999, EatLeft = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 904, Type = 2, Stage = 0, Dish = "burger", Patience = 999, MaxPatience = 999 });
                    st.StandQueue.Add(new StandOrder { Id = 905, Type = 4, Stage = 0, Dish = "salad", Patience = 999, MaxPatience = 999 });
                    for (int i = 0; i < 70; i++) { rc.Advance(.15f); p.transform.position = pos; yield return null; }
                }
                for (int i = 0; i < 20; i++) yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, shot.name + ".png"));
                for (int i = 0; i < 5; i++) yield return null;
                if (rc && rc.PanelOpen) rc.ClosePanel();
                if (shot.name.Contains("truck_ui_phone")) Game.State.Restaurant.Owned = truckOwned0;
                if (shot.name.Contains("scrub_plate")) PlateScrub.Of(p).End();
                if (shot.name.Contains("chop_lettuce")) PrepChop.Of(p).End();
                foreach (var v in fluxShots) if (v) Destroy(v); fluxShots.Clear();
            }
            yield return new WaitForSeconds(1);
            Application.Quit();
        }
    }
}
