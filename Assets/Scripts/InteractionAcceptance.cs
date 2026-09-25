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

                // A level eye ray aimed at a visible bench should resolve the interactive object.
                var benchCenter = R.Furnishings[prep].GetComponentInChildren<Collider>().bounds.center;
                P.Teleport(new Vector3(benchCenter.x, .15f, benchCenter.z + 2));
                Physics.SyncTransforms();
                P.LookAt(new Vector3(benchCenter.x, P.View.transform.position.y, benchCenter.z));
                Check(P.TryResolveInteractionHit(out var levelHit) && TargetId(levelHit) == prep,
                    "level eye ray resolves visible kitchen bench");
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "Interaction test obstruction";
                blocker.transform.position = P.InteractionRay.origin + P.InteractionRay.direction * 1.05f;
                blocker.transform.localScale = new Vector3(2, 3, .35f);
                Physics.SyncTransforms();
                Check(P.TryResolveInteractionHit(out var blockedHit) && blockedHit.collider == blocker.GetComponent<Collider>(),
                    "target assist respects a closer wall-like obstruction");
                blocker.SetActive(false);
                Destroy(blocker);

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

                Check(AimAt(pantry), "ray targets pantry");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein, "E takes raw protein from pantry");
                bool tableFocused = AimAt(table);
                Check(tableFocused, "ray targets dining table");
                if (tableFocused) P.ResolveAndInteract(true, false);
                Check(tableFocused && !R.PanelOpen && K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein,
                    "E on adjacent table while carrying food never opens furniture menu");
                if (R.PanelOpen) R.ClosePanel();

                Check(AimAt(prep), "ray targets prep bench");
                P.ResolveAndInteract(true, false);
                Check(K.At(prep)?.Kind == KitchenItemKind.RawProtein, "E places protein on prep bench");
                float elapsed = 0;
                while (elapsed < 3.3f) {
                    P.ResolveAndInteract(false, true);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                Check(K.At(prep)?.Kind == KitchenItemKind.PreparedPatty, "holding E prepares patty through focused ray");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.PreparedPatty, "E retrieves prepared patty");
                Check(AimAt(grill), "ray targets grill");
                P.ResolveAndInteract(true, false);
                Check(K.At(grill)?.Kind == KitchenItemKind.PreparedPatty, "E places patty on grill");
                K.Tick(Game.State, 8.1f);
                Check(K.At(grill)?.Kind == KitchenItemKind.CookedPatty, "grill cooks patty");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.CookedPatty, "E takes cooked patty");
                Check(AimAt(assembly), "ray targets plated assembly");
                P.ResolveAndInteract(true, false);
                Check(K.At(assembly)?.Parts == 1 && K.Hold(P.ActorId) == null, "E adds cooked patty to plate");
                Check(AimAt(pantry), "ray targets pantry for bun");
                R.HandlePlayerInput(P, false, false, true, false, false);
                R.HandlePlayerInput(P, false, false, true, false, false);
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.Bun, "pantry choice yields separate bun");
                Check(AimAt(assembly), "ray targets assembly for bun");
                P.ResolveAndInteract(true, false);
                Check(K.At(assembly)?.Parts == 3 && K.Hold(P.ActorId) == null, "E finishes burger on plate");
                P.ResolveAndInteract(true, false);
                Check(K.RecipeOf(K.Hold(P.ActorId)) == "burger", "E takes completed burger to serve");
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
        void Check(bool result, string label) { checks++; if (result) Debug.Log("INTERACTION_CHECK_PASS " + label); else { failures++; Debug.LogWarning("INTERACTION_CHECK_FAIL " + label); } }
        void Require(bool result, string label) { Check(result, label); if (!result) throw new InvalidOperationException(label); }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Assert) { Application.logMessageReceived -= OnLog; Application.Quit(1); } }
    }
}
