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
            ("05_overview_day", new Vector3(0, 30, -45), 0, 28, 60),
            ("06_street_night", new Vector3(-2, 0, -3), 20, 0, 190),
            ("07_overview_night", new Vector3(0, 30, -45), 0, 28, 190),
        };

        IEnumerator Start() {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Snapshots"));
            if (Application.isEditor) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Snapshots"));
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(2);
            Game.State.StandBuilt = true; Game.SetPaused(false); Game.SyncWorld();
            foreach (var shot in Shots) {
                Game.State.Clock = shot.clock; Game.SyncWorld();
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
