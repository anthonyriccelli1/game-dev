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
        };

        IEnumerator Start() {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Snapshots"));
            if (Application.isEditor) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Snapshots"));
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(2);
            Game.State.StandBuilt = true; Game.SetPaused(false); Game.SyncWorld();
            foreach (var shot in Shots) {
                Game.State.Clock = shot.clock; Game.SyncWorld();
                RenderSettings.fog = !(shot.name.Contains("map") || shot.name.Contains("aerial"));
                var p = Game.Player; p.Teleport(shot.pos + Vector3.up * .1f);
                var cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
                p.transform.position = shot.pos; p.transform.rotation = Quaternion.Euler(0, shot.yaw, 0);
                p.View.transform.localRotation = Quaternion.Euler(shot.pitch, 0, 0);
                for (int i = 0; i < 20; i++) yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, shot.name + ".png"));
                for (int i = 0; i < 5; i++) yield return null;
            }
            yield return new WaitForSeconds(1);
            Application.Quit();
        }
    }
}
