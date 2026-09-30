using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RestaurantCity {
    // Opt-in executable smoke path. It never loads or writes a player's save.
    public class PrototypeSmokeTest : MonoBehaviour {
        public CityGame Game;
        string output;
        int assertions;
        bool failed;
        IEnumerator Start() {
            output = Path.Combine(Application.dataPath, "..", "SmokeEvidence");
            Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            yield return new WaitForSecondsRealtime(2);
            CaptureFrame("01-arrival.png");
            yield return new WaitForSecondsRealtime(.5f);
            try {
                Game.SetPaused(false);
                // The stand frame is parked inside the food truck in the city: work it from the cook lane (z 9.6), facing the hatch.
                var o = Game.Stand ? Game.Stand.transform.position : Vector3.zero; bool truck = o.sqrMagnitude > 1; float cz = truck ? 9.6f : 5;
                Use(InteractionKind.Stand, o + new Vector3(0, .15f, cz), o + new Vector3(0, .65f, 8));
                Use(InteractionKind.Supplier, new Vector3(-12, .15f, 6), new Vector3(-12, .8f, 9));
                Use(InteractionKind.Prep, o + new Vector3(-2.2f, .15f, cz), o + new Vector3(-2.2f, 1, 8));
                Use(InteractionKind.Grill, o + new Vector3(0, .15f, cz), o + new Vector3(0, 1, 8));
                Game.State.Tick(5);
                Use(InteractionKind.Grill, o + new Vector3(0, .15f, cz), o + new Vector3(0, 1, 8));
                Use(InteractionKind.Serve, o + new Vector3(2.2f, .15f, cz), o + new Vector3(2.2f, 1, 8));
                Check(Game.State.Cash == 26 && Game.State.Served == 1, "first service pays $12");
                Game.Player.Teleport(o + new Vector3(-4, .15f, 1)); Game.Player.LookAt(o + new Vector3(-.5f, 1.8f, 8));
                Game.State.Tick(7); Game.SyncWorld();
                Check(Game.State.HasOrder, "second customer arrives");
            } catch (Exception e) { Fail(e); }
            yield return new WaitForSecondsRealtime(2);
            CaptureFrame("02-day-service.png");
            yield return new WaitForSecondsRealtime(.5f);
            try {
                Game.State.Clock = 170; Game.SyncWorld();
                Game.Player.Teleport(new Vector3(11.6f, .15f, 16)); Game.Player.LookAt(new Vector3(11.6f, 1.5f, 21));
            } catch (Exception e) { Fail(e); }
            yield return new WaitForSecondsRealtime(.5f);
            CaptureFrame("03-night-alley.png");
            yield return new WaitForSecondsRealtime(.5f);
            try {
                Check(!Game.State.ClaimRecipe(false), "guard blocks recipe");
                Game.State.Health = 100;
                Game.Guard.ResetGuard();
                Game.Player.Teleport(Game.Guard.transform.position + new Vector3(0, .15f, -1.7f));
            } catch (Exception e) { Fail(e); }
            yield return new WaitForSecondsRealtime(3.3f);
            try {
                Check(Game.State.Health == 75, "live rival windup deals one telegraphed hit");
                Game.Player.Teleport(new Vector3(11.6f, .15f, 11));
            } catch (Exception e) { Fail(e); }
            yield return new WaitForSecondsRealtime(1.3f);
            try { Check(Game.State.Health == 75, "retreat to street prevents further damage"); } catch (Exception e) { Fail(e); }
            for (int i = 0; i < 3; i++) {
                try {
                    Game.Player.Teleport(Game.Guard.transform.position + new Vector3(0, .15f, -2.6f));
                    Game.Player.LookAt(Game.Guard.transform.position + Vector3.up * 1.2f);
                    Physics.SyncTransforms();
                    Check(Game.Player.Swing(), "spatula physics hits rival " + (i + 1));
                    Check(!Game.Player.Swing(), "spatula cooldown blocks repeated immediate hit");
                } catch (Exception e) { Fail(e); }
                yield return new WaitForSecondsRealtime(.6f);
            }
            try {
                Check(Game.Guard.Defeated, "three hits defeat rival");
                Use(InteractionKind.Recipe, new Vector3(11.6f, .15f, 23), new Vector3(11.6f, .7f, 26));
                Check(Game.State.RecipeUnlocked, "recipe acquired through scene interaction");
                Game.Hurt(100);
                Check(Game.State.Health == 100 && Game.State.RecipeUnlocked && Game.State.StandBuilt, "death preserves permanent progress");
                Check(Vector3.Distance(Game.Player.transform.position, Game.SpawnPoint) < .1f, "death returns to spawn");
                string json = JsonUtility.ToJson(Game.State);
                var restored = JsonUtility.FromJson<GameState>(json); restored.SanitizeAfterLoad();
                Check(restored.RecipeUnlocked && restored.Cash == Game.State.Cash, "Unity JSON round trip preserves progression");
                Game.Player.LookAt(new Vector3(0, 1.6f, 8));
                Game.SyncWorld();
            } catch (Exception e) { Fail(e); }
            yield return new WaitForSecondsRealtime(.5f);
            CaptureFrame("04-night-reward.png");
            yield return new WaitForSecondsRealtime(1);
            string result = (failed ? "FAIL" : "PASS") + " / " + assertions + " assertions / scene interaction, service, night reward, respawn, JSON";
            File.WriteAllText(Path.Combine(output, "result.txt"), result);
            Debug.Log("RESTAURANT_CITY_SMOKE_" + result);
            Application.logMessageReceived -= OnLog;
            Application.Quit(failed ? 1 : 0);
        }
        void Use(InteractionKind kind, Vector3 position, Vector3 lookAt) {
            Game.Player.Teleport(position); Game.Player.LookAt(lookAt); Physics.SyncTransforms();
            Check(Physics.Raycast(Game.Player.View.transform.position, Game.Player.View.transform.forward, out var hit, 3.6f, ~0, QueryTriggerInteraction.Ignore), "ray reaches " + kind);
            var target = hit.collider.GetComponentInParent<Interactable>();
            Check(target && target.Kind == kind, "correct scene target " + kind + " (hit " + hit.collider.name + ")");
            Check(Game.Interact(kind), "successful action " + kind);
        }
        void CaptureFrame(string filename) {
            var target = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try {
                target.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                Check(RenderPipeline.SupportsRenderRequest(Game.Player.View, request), "camera supports offscreen rendering");
                RenderPipeline.SubmitRenderRequest(Game.Player.View, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); pixels.Apply();
                int lit = 0;
                for (int x = 80; x < 1440; x += 160) for (int y = 50; y < 900; y += 100) {
                    if (pixels.GetPixel(x, y).maxColorComponent > .08f) lit++;
                }
                Check(lit > 20, "rendered frame contains visible scene content");
                File.WriteAllBytes(Path.Combine(output, filename), pixels.EncodeToPNG());
            } catch (Exception e) { Fail(e); }
            finally { RenderTexture.active = previous; target.Release(); Destroy(target); Destroy(pixels); }
        }
        void Check(bool condition, string description) {
            if (!condition) throw new Exception(description);
            assertions++; Debug.Log("SMOKE PASS: " + description);
        }
        void Fail(Exception e) { failed = true; Debug.LogError("SMOKE FAILURE: " + e.Message); }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true; }
    }
}
