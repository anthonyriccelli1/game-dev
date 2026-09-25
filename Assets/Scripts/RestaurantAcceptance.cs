using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RestaurantCity {
    public partial class RestaurantAcceptance : MonoBehaviour {
        public CityGame Game;
        public string Output;
        int checks;
        bool failed;
        RestaurantController R => Game.Restaurant;
        IEnumerator Start() {
            Output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "RestaurantEvidence")); Directory.CreateDirectory(Output);
            Application.logMessageReceived += RecordError;
            yield return new WaitForSecondsRealtime(2);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--restaurant-resume") >= 0) {
                Try(() => {
                    Check(Game.LoadFrom(Path.Combine(Output, "acceptance-save.json")), "read actual disk save in new process"); R.RebuildLayout();
                    Check(R.Data.Owned && R.Data.Layout.Count > 3, "restored ownership and customized layout");
                    Check(R.Data.WallId != "wall_shabby" && R.Data.FloorId != "floor_shabby", "restored finishes");
                    string expected = File.ReadAllText(Path.Combine(Output, "expected-layout.txt"));
                    Check(LayoutFingerprint() == expected, "cash, layout, menu, workers, rank and recipe match previous process");
                    Check(!R.Data.Open && R.Data.Orders.Count == 0, "restored restaurant safely closed and ready for another service");
                    Game.SetPaused(false); Game.Player.Teleport(new Vector3(-10, .15f, -10.8f)); Game.Player.LookAt(new Vector3(-11, 1.2f, -18));
                });
                yield return new WaitForSecondsRealtime(1); Capture("05-returned-restaurant.png"); Finish("resume"); yield break;
            }
            Try(() => {
                Game.SetPaused(false);
                Check(Game.State.Cash == 30 && !R.Data.Owned, "new save starts without a restaurant");
                Check(Game.Interact(InteractionKind.Stand), "set up food stand");
                int loops = 0;
                while (Game.State.Cash < 310 && loops++ < 50) {
                    if (Game.State.Stock == 0) Check(Game.Interact(InteractionKind.Supplier), "buy stand ingredients");
                    Game.State.Tick(7);
                    Check(Game.Interact(InteractionKind.Prep), "prepare stand burger");
                    Check(Game.Interact(InteractionKind.Grill), "start stand grill"); Game.State.Tick(5);
                    Check(Game.Interact(InteractionKind.Grill), "plate stand burger"); Check(Game.Interact(InteractionKind.Serve), "earn sale through stand");
                }
                Check(Game.State.Cash >= 310, "earned ownership and initial upgrade budget without injected cash");
                int before = Game.State.Cash; Check(R.BuyRestaurant(), "buy restaurant through scene action"); Check(Game.State.Cash == before - 150, "lease charges exactly $150");
                R.ClosePanel(); Game.Player.Teleport(new Vector3(-9.8f, .15f, -10.6f)); Game.Player.LookAt(new Vector3(-11.6f, 1.3f, -17));
            });
            yield return new WaitForSecondsRealtime(1); Capture("01-initial-restaurant.png");
            Try(() => { Check(RestaurantCatalog.Items.Length >= 20, "substantial categorized catalog"); R.ShowPanel("Catalog"); });
            yield return new WaitForSecondsRealtime(1); Capture("02-catalog.png");
            Try(() => {
                R.SelectCatalogItem("fern"); Check(R.PlacementActive, "catalog opens placement preview");
                Check(R.ConfirmPlacement(10, 1), "place furnishing on valid floor cell");
                var fern = R.Data.Layout.Last(p => p.CatalogId == "fern");
                R.MoveItem(fern.InstanceId); Check(R.PlacementActive, "move uses preview");
                Check(R.ConfirmPlacement(9, 1), "move existing furnishing without repurchase");
                int oldCash = Game.State.Cash; R.SellItem(fern.InstanceId); Check(Game.State.Cash == oldCash + 6, "sell refunds half purchase price");
                R.SelectCatalogItem("wall_teal"); R.SelectCatalogItem("floor_checker");
                R.SelectCatalogItem("pendant_amber"); Check(R.ConfirmPlacement(8, 5), "place warm pendant");
                R.SelectCatalogItem("fern"); Check(R.ConfirmPlacement(10, 1), "place plant for ambience");
                R.ClosePanel(); Check(R.Data.WallId == "wall_teal" && R.Data.FloorId == "floor_checker", "whole-room finishes installed");
                Game.Player.Teleport(new Vector3(-9.8f, .15f, -10.6f)); Game.Player.LookAt(new Vector3(-11.6f, 1.3f, -17));
            });
            yield return new WaitForSecondsRealtime(1); Capture("stage1-customized.png");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--restaurant-stage2") >= 0) { yield return RunServiceAcceptance(); }
            Try(() => {
                Check(Game.SaveTo(Path.Combine(Output, "acceptance-save.json")), "save to isolated acceptance file");
                File.WriteAllText(Path.Combine(Output, "expected-layout.txt"), LayoutFingerprint());
            });
            Finish(Array.IndexOf(Environment.GetCommandLineArgs(), "--restaurant-stage2") >= 0 ? "stage2" : "stage1");
        }
        IEnumerator RunServiceAcceptance() { yield return RunGrowth(); }
        IEnumerator RunGrowth() {
            // Advance the existing city clock and earn the recipe from its guarded night encounter.
            Try(() => { while (!Game.State.IsNight) Game.State.Tick(1); Game.SyncWorld(); Game.Guard.ResetGuard(); });
            yield return null;
            for (int i = 0; i < 3; i++) {
                int strike = i;
                Try(() => {
                    Game.Player.Teleport(Game.Guard.transform.position + new Vector3(0, .15f, -2.6f));
                    Game.Player.LookAt(Game.Guard.transform.position + Vector3.up * 1.2f); Physics.SyncTransforms();
                    Check(Game.Player.Swing(), "night encounter hit " + (strike + 1) + " through spatula physics");
                });
                yield return new WaitForSecondsRealtime(.65f);
            }
            Try(() => {
                Check(Game.Guard.Defeated && Game.Interact(InteractionKind.Recipe), "city encounter awards midnight recipe");
                Check(R.Data.ToggleDish(Game.State, "midnight", out _), "earned recipe joins restaurant menu");
                Game.Player.Teleport(new Vector3(-10, .15f, -10.7f));
                Check(R.Data.Hire(Game.State, "ember", out _), "hire Ember for $70");
                Check(R.Data.StartService(Game.State, out _), "open first restaurant service");
            });
            bool manuallyServed = false, midnightServed = false, hiredMoss = false;
            for (int tick = 0; tick < 850 && R.Data.Served < 12; tick++) {
                Try(() => {
                    if (R.Data.Protein < 4 || R.Data.Produce < 5) {
                        Game.Player.Teleport(new Vector3(-12, .15f, 8));
                        if (R.Data.Protein < 4) R.Restock(true);
                        if (R.Data.Produce < 5) R.Restock(false);
                        Game.Player.Teleport(new Vector3(-10, .15f, -10.7f));
                    }
                    if (R.Data.Served >= 3 && !hiredMoss && Game.State.Cash >= 55) {
                        R.Hire("moss"); hiredMoss = R.Data.Workers.Any(w => w.Id == "moss");
                    }
                    if (R.Data.Cleanliness < 68) R.Clean();
                    if (!hiredMoss) {
                        var waiting = R.Data.Orders.FirstOrDefault(o => o.Stage == RestaurantOrderStage.Waiting);
                        if (waiting != null) R.TryCook(waiting.Id);
                        var ready = R.Data.Orders.FirstOrDefault(o => o.Stage == RestaurantOrderStage.Ready);
                        if (ready != null && R.CollectDish(ready.Id)) {
                            R.ServeGuest(ready.Id); manuallyServed |= R.Data.Served > 0;
                            if (ready.DishId == "midnight") midnightServed = true;
                        }
                    }
                    if (R.Data.Reviews.Any(r => r.DishId == "midnight")) midnightServed = true;
                    R.Advance(.5f); Game.State.Tick(.5f);
                });
                if (tick % 5 == 0) yield return null;
            }
            Try(() => {
                Check(manuallyServed, "player collects and serves a cooked dish");
                Check(hiredMoss && R.VisibleWorkers == 2, "two distinct employees visibly join service");
                Check(R.Data.Workers.Any(w => w.Id == "ember" && w.TasksCompleted > 0), "Ember performs cooking work");
                Check(R.Data.Workers.Any(w => w.Id == "moss" && w.TasksCompleted > 0), "Moss performs service work");
                Check(midnightServed, "night recipe is ordered and served in restaurant");
                Check(R.Data.Served >= 12, "first shift serves at least twelve guests");
                R.Data.EndService(out _);
            });
            for (int i = 0; i < 100 && R.Data.Orders.Count > 0; i++) { R.Advance(.5f); if (i % 5 == 0) yield return null; }
            Try(() => {
                Check(R.Data.CanCustomize, "closed shift permits remodeling");
                Check(R.Data.Place(Game.State, "booth_teal", 8, 4, 0, out _), "earn and install four-seat booth");
                R.RebuildLayout();
                Check(R.Data.Seats >= 6, "restaurant seating expands for a busy service");
                Check(R.Data.StartService(Game.State, out _), "second service opens");
            });
            bool capturedBusy = false;
            for (int tick = 0; tick < 1450 && R.Data.Served < 30; tick++) {
                Try(() => {
                    if (R.Data.Protein < 4 || R.Data.Produce < 5) {
                        Game.Player.Teleport(new Vector3(-12, .15f, 8));
                        if (R.Data.Protein < 4) R.Restock(true);
                        if (R.Data.Produce < 5) R.Restock(false);
                        Game.Player.Teleport(new Vector3(-10, .15f, -10.7f));
                    }
                    if (R.Data.Cleanliness < 70) R.Clean();
                    R.Advance(.5f); Game.State.Tick(.5f);
                });
                if (!capturedBusy && R.Data.Orders.Count >= 3 && R.VisibleWorkers == 2) {
                    Game.Player.Teleport(new Vector3(-9.8f, .15f, -10.6f)); Game.Player.LookAt(new Vector3(-11.6f, 1.3f, -17));
                    Capture("03-busy-service.png"); capturedBusy = true;
                }
                if (tick % 5 == 0) yield return null;
            }
            Try(() => {
                Check(capturedBusy, "busy service with several customers captured");
                Check(R.ObservedCustomerTypes >= 8, "all eight distinctive regulars arrived");
                Check(R.Data.Served >= 20 && R.Data.Stars == 2, "earn two stars from meals satisfaction and ambience");
                Check(R.Data.Reviews.Count > 1 && R.Data.Reviews[0].Comment.Contains("wait"), "service feedback explains wait food and condition");
                R.Data.EndService(out _);
            });
            for (int i = 0; i < 280 && R.Data.Orders.Count > 0; i++) { R.Advance(.5f); if (i % 5 == 0) yield return null; }
            Try(() => {
                Check(R.Data.CanCustomize, "second shift finishes cleanly");
                Check(R.Data.Place(Game.State, "oven", 9, 0, 0, out _), "two-star oven unlock is affordable and placeable");
                Check(R.Data.ToggleDish(Game.State, "dessert", out _), "moonberry tart joins menu at two stars");
                Check(R.Data.Place(Game.State, "sign_neon", 0, 0, 0, out _), "two-star exterior neon is installed");
                R.RebuildLayout(); Game.Player.Teleport(new Vector3(-9.8f, .15f, -10.6f)); Game.Player.LookAt(new Vector3(-11.6f, 1.3f, -17));
            });
            yield return new WaitForSecondsRealtime(.5f); Capture("04-upgraded-restaurant.png");
        }
        string LayoutFingerprint() => Game.State.Cash + "|" + Game.State.RecipeUnlocked + "|" + R.Data.Stars + "|" + string.Join(";", R.Data.Layout.Select(p => $"{p.InstanceId}:{p.CatalogId}:{p.X}:{p.Z}:{p.Rotation}:{p.Paid}")) + "|" + string.Join(",", R.Data.ActiveMenu) + "|" + string.Join(",", R.Data.Workers.Select(w => w.Id + ":" + w.Job));
        void Try(Action action) { try { action(); } catch (Exception e) { failed = true; Debug.LogError("RESTAURANT CHECK FAILED: " + e); } }
        void Check(bool condition, string description) { if (!condition) throw new Exception(description); checks++; Debug.Log("RESTAURANT CHECK PASS: " + description); }
        void RecordError(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true; }
        void Finish(string stage) {
            string summary = (failed ? "FAIL" : "PASS") + " / " + checks + " assertions / " + stage;
            File.WriteAllText(Path.Combine(Output, stage + "-result.txt"), summary); Debug.Log("RESTAURANT_ACCEPTANCE_" + summary);
            Application.logMessageReceived -= RecordError; Application.Quit(failed ? 1 : 0);
        }
        void Capture(string filename) {
            var target = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1440, 900, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            Try(() => {
                R.UI.Refresh(); Canvas.ForceUpdateCanvases(); target.Create();
                RenderPipeline.SubmitRenderRequest(Game.Player.View, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output, filename), pixels.EncodeToPNG());
            });
            RenderTexture.active = previous; target.Release(); Destroy(target); Destroy(pixels);
        }
    }
}
