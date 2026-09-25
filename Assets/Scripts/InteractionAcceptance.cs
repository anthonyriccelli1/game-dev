using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RestaurantCity {
    // Run only with --interaction-acceptance. CityGame starts with an isolated state and disables saves.
    public class InteractionAcceptance : MonoBehaviour {
        public CityGame Game;
        int checks, failures;
        RestaurantController R => Game.Restaurant;
        FirstPersonPlayer P => Game.Player;
        KitchenState K => Game.State.Kitchen;

        IEnumerator Start() {
            Application.logMessageReceived += OnLog;
            yield return null;
                Game.State.Cash = 300;
                Require(R.Data.BuyRestaurant(Game.State, out _), "isolated restaurant acquired");
                R.RebuildLayout();
                Game.SetPaused(false);
                R.ClosePanel();
                yield return null;
                var aim = Game.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "Aim");
                Check(aim != null && ((RectTransform)aim.transform).rect.height >= aim.preferredHeight + 2,
                    "center reticle has enough height for generated glyph");
                var prep = Station("prep_bench");
                var rack = Station("plate_rack");
                var pantry = Station("pantry");
                var assembly = Station("assembly");
                var grill = Station("grill");
                var table = R.Data.Layout.First(x => RestaurantCatalog.Find(x.CatalogId).Seats > 0).InstanceId;

                // A level eye ray aimed at a visible bench should resolve the interactive object, consistently
                // frame after frame (this used to flip between a direct ray and a second, lower-angled ray).
                var benchCenter = R.Furnishings[prep].GetComponentInChildren<Collider>().bounds.center;
                P.Teleport(new Vector3(benchCenter.x, .15f, benchCenter.z + 2));
                Physics.SyncTransforms();
                P.LookAt(new Vector3(benchCenter.x, P.View.transform.position.y, benchCenter.z));
                bool stable = true;
                for (int i = 0; i < 6; i++) stable &= P.TryResolveInteractionHit(out var repeat) && TargetId(repeat) == prep;
                Check(stable, "level eye ray resolves the same kitchen bench on every consecutive query");

                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "Interaction test obstruction";
                blocker.transform.position = P.InteractionRay.origin + P.InteractionRay.direction * 1.05f;
                blocker.transform.localScale = new Vector3(2, 3, .35f);
                Physics.SyncTransforms();
                Check(P.TryResolveInteractionHit(out var blockedHit) && blockedHit.collider == blocker.GetComponent<Collider>(),
                    "a closer wall-like obstruction reports immediately, never the target behind it");
                blocker.SetActive(false);
                Destroy(blocker);
                Physics.SyncTransforms();
                Check(P.TryResolveInteractionHit(out var restored) && TargetId(restored) == prep,
                    "removing the obstruction immediately restores the bench, no stale block");

                // B is handled before the usual ray pass in FirstPersonPlayer.Update.
                R.ClearPlayerFocus(P);
                R.HandlePlayerInput(P, false, false, false, false, true);
                Check(R.PanelOpen && R.Panel == "Furniture" && R.SelectedInstanceId == prep,
                    "build input selects current aim, without one-frame-old focus");
                if (R.PanelOpen) R.ClosePanel();

                R.ClearPlayerFocus(P);
                R.HandlePlayerInput(P, false, false, false, false, true);
                if (R.PanelOpen) R.ClosePanel();
                R.ShowPanel("Furniture");
                var itemName = RestaurantCatalog.Find("prep_bench").Name;
                var choice = R.UI.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name.Contains(itemName));
                Check(choice != null, "closed edit panel lists existing furnishing by name");
                if (choice) {
                    choice.onClick.Invoke();
                    Check(R.SelectedInstanceId == prep, "closed list selects obscured furnishing");
                    var move = R.UI.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Move furnishing");
                    Check(move != null, "selected furnishing offers move action");
                    if (move) {
                        move.onClick.Invoke();
                        Check(R.PlacementActive, "move action enters overhead placement");
                        if (R.PlacementActive) {
                            var placed = R.Data.Layout.First(x => x.InstanceId == prep);
                            int oldX = placed.X, oldZ = placed.Z;
                            var definition = RestaurantCatalog.Find(placed.CatalogId);
                            bool moved = false;
                            for (int z = 0; z < 10 && !moved; z++) for (int x = 0; x < 12 && !moved; x++) {
                                if (x == placed.X && z == placed.Z || !R.Data.CanPlace(placed.CatalogId, x, z, placed.Rotation, prep, out _)) continue;
                                var world = RestaurantController.CellCenter(x, z, definition.Width, definition.Depth);
                                var pointer = P.View.WorldToScreenPoint(world);
                                if (R.UpdatePreviewFromPointer(pointer) && R.PreviewCell == new Vector2Int(x, z)) {
                                    Check(R.ConfirmPlacement(R.PreviewCell.x, R.PreviewCell.y), "pointer-mapped placement commits moved furnishing");
                                    moved = true;
                                }
                            }
                            Check(moved && (R.Data.Layout.First(x => x.InstanceId == prep).X != oldX || R.Data.Layout.First(x => x.InstanceId == prep).Z != oldZ),
                                "moved furnishing has new saved layout coordinates");
                            if (R.PlacementActive) R.CancelPlacement();
                        }
                    }
                }
                if (R.PanelOpen) R.ClosePanel();

                Check(AimAt(rack), "ray targets plate rack");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.Plate, "E picks up a clean plate through ray interaction");
                Check(AimAt(assembly), "ray targets assembly counter");
                P.ResolveAndInteract(true, false);
                Check(K.At(assembly)?.Kind == KitchenItemKind.Plate && K.Hold(P.ActorId) == null,
                    "E places clean plate on assembly counter");

                Check(AimAtShelf(pantry, "protein"), "ray targets the pantry's raw-patty shelf");
                Check(PromptFor().Contains("raw patty"), "pantry prompt names the sub-id ingredient (A2)");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein, "E takes raw patty from its own shelf, not a hidden Q cycle");
                bool tableFocused = AimAt(table);
                Check(tableFocused, "ray targets dining table");
                if (tableFocused) P.ResolveAndInteract(true, false);
                Check(tableFocused && !R.PanelOpen && K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein,
                    "E on adjacent table while carrying food never opens furniture menu");
                if (R.PanelOpen) R.ClosePanel();

                // Stage A workflow: raw patty goes straight to the grill (no prep-bench chop step).
                Check(AimAt(grill), "ray targets grill");
                P.ResolveAndInteract(true, false);
                Check(K.At(grill)?.Kind == KitchenItemKind.RawProtein, "E places the raw patty directly on the grill");
                K.Tick(Game.State, 8.1f);
                Check(K.At(grill)?.Kind == KitchenItemKind.CookedPatty, "grill cooks patty");

                Check(AimAt(assembly), "ray targets assembly for the held plate");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.Plate, "E picks the clean plate back up to carry it");
                Check(AimAt(grill), "ray targets grill while carrying a plate");
                Check(PromptFor().Contains("cooked patty"), "Preview offers the fewer-press shortcut onto a held plate (A3)");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Components.Contains("cooked_patty") == true && K.At(grill) == null,
                    "E slides the cooked patty straight onto the held plate");

                Check(AimAtShelf(pantry, "bun"), "ray targets the pantry's bun shelf");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Components.Contains("bun") == true, "E adds a bun straight onto the held plate from its shelf");
                Check(K.RecipeOf(K.Hold(P.ActorId)) == "burger", "components-based recipe match recognizes the finished burger");

                Check(AimAtShelf(pantry, "sauce"), "ray targets the pantry's sauce shelf");
                Check(!PromptFor().Contains("E  Take"), "midnight sauce shelf is locked before the recipe is learned");
                P.Teleport(new Vector3(0, .15f, 0));
                Check(Game.CoOp.Join(), "second player joins isolated input test");
                var second = Game.CoOp.SecondPlayer;
                Check(second && second.gameObject.layer == 29 && second.OwnBodyMask == 1 << 29,
                    "second player's controller uses its own excluded interaction layer");
            Debug.Log("INTERACTION_RUNTIME_" + (failures == 0 ? "PASS " : "FAIL ") + checks + " checks, " + failures + " failures");
            Application.logMessageReceived -= OnLog;
            Application.Quit(failures == 0 ? 0 : 1);
        }
        int Station(string id) => R.Data.Layout.First(x => x.CatalogId == id).InstanceId;
        static int TargetId(RaycastHit hit) { var t = hit.collider ? hit.collider.GetComponentInParent<RestaurantTarget>() : null; return t ? t.InstanceId : -1; }
        string PromptFor() => R.PromptFor(P.ActorId);
        bool AimAt(int id) {
            if (!R.Furnishings.TryGetValue(id, out var furnishing)) return false;
            var collider = furnishing.GetComponentInChildren<Collider>();
            if (!collider) return false;
            var center = collider.bounds.center;
            string misses = "";
            foreach (var offset in new[] { new Vector3(0, 0, 1.7f), new Vector3(-1.7f, 0, 0), new Vector3(1.7f, 0, 0), new Vector3(0, 0, -1.7f) }) {
                var at = center + offset;
                if (at.x < -16 || at.x > -4 || at.z < -21 || at.z > -9) continue;
                P.Teleport(new Vector3(at.x, .15f, at.z));
                P.LookAt(center);
                Physics.SyncTransforms();
                bool found = P.TryResolveInteractionHit(out var hit);
                if (found && TargetId(hit) == id) return true;
                misses += " offset=" + offset + " eye=" + P.InteractionRay.origin + " direction=" + P.InteractionRay.direction + " hit=" + (found ? hit.collider.name + ":" + TargetId(hit) + "@" + hit.distance.ToString("0.00") : "none");
            }
            Debug.Log("INTERACTION_AIM_MISS id=" + id + " collider=" + collider.name + " bounds=" + collider.bounds + misses);
            return false;
        }
        // Aims at one named pantry shelf (A2's per-ingredient hitbox) rather than the furnishing's general bounds.
        bool AimAtShelf(int pantryId, string subId) {
            if (!R.Furnishings.TryGetValue(pantryId, out var furnishing)) return false;
            var shelf = furnishing.transform.Find("Pantry shelf " + subId);
            var collider = shelf ? shelf.GetComponent<Collider>() : null;
            if (!collider) return false;
            var center = collider.bounds.center;
            foreach (var offset in new[] { new Vector3(0, 0, 1.7f), new Vector3(0, .3f, 1.6f) }) {
                P.Teleport(new Vector3(center.x + offset.x, .15f + offset.y, center.z + offset.z));
                P.LookAt(center);
                Physics.SyncTransforms();
                if (P.TryResolveInteractionHit(out var hit) && hit.collider == collider) return true;
            }
            return false;
        }
        void Check(bool result, string label) { checks++; if (result) Debug.Log("INTERACTION_CHECK_PASS " + label); else { failures++; Debug.LogWarning("INTERACTION_CHECK_FAIL " + label); } }
        void Require(bool result, string label) { Check(result, label); if (!result) throw new InvalidOperationException(label); }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Assert) { Application.logMessageReceived -= OnLog; Application.Quit(1); } }
    }
}
