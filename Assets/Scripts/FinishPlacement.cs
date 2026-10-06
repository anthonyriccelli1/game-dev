using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RestaurantCity {
    public partial class RestaurantController {
        public bool FinishBrushActive { get; private set; }
        public string FinishSurfaceKey { get; private set; } = "";
        public bool FinishFillPending { get; private set; }
        public int FinishFillQuote { get; private set; }
        public string FinishTargetLabel {
            get {
                if (string.IsNullOrEmpty(FinishSurfaceKey)) return "Point at a surface";
                var p = FinishSurfaceKey.Split(':');
                if (p[0] == "piece") {
                    var placed = Data.Layout.Find(item => item.InstanceId == int.Parse(p[1]));
                    return (placed?.CatalogId == "service_window" ? "Service window" : "Partition wall") + " #" + p[1] + " / both faces";
                }
                return p[0] == "floor" ? "Floor tile " + (int.Parse(p[1]) + 1) + ", " + (int.Parse(p[2]) + 1)
                    : char.ToUpper(p[1][0]) + p[1].Substring(1) + " wall / section " + (int.Parse(p[2]) + 1);
            }
        }
        bool savedOrthographic, savedSpatulaActive, savedFoodActive, savedCursorVisible;
        float savedOrthographicSize;
        CursorLockMode savedCursorLock;
        GameObject finishBrushObjects, finishPatch, finishCap;
        LineRenderer finishOutline;
        Material finishOutlineMaterial;
        readonly Dictionary<string, Renderer> finishWallStrips = new Dictionary<string, Renderer>();
        readonly List<RaycastResult> finishUIHits = new List<RaycastResult>();
        const float WallSection = 13f / 12f;
        const float FloorTileDepth = 13f / 10f;

        // Both placement tools own the same temporary overhead camera session.
        void BeginPlacementView() {
            var cam = Game.Player.View;
            savedPosition = Game.Player.transform.position; savedViewLocal = cam.transform.localPosition;
            savedViewRotation = cam.transform.localRotation; savedPlacementMask = cam.cullingMask;
            savedOrthographic = cam.orthographic; savedOrthographicSize = cam.orthographicSize;
            savedCursorLock = Cursor.lockState; savedCursorVisible = Cursor.visible;
            savedSpatulaActive = Game.Player.Spatula && Game.Player.Spatula.gameObject.activeSelf;
            savedFoodActive = Game.HandFood && Game.HandFood.activeSelf;
            PanelOpen = false; PlacementActive = true;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            cam.transform.position = W(-10, 14, -15.5f); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true; cam.orthographicSize = 8;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            cam.cullingMask &= ~(1 << 27);
            if (Game.Player.Spatula) Game.Player.Spatula.gameObject.SetActive(false);
            if (Game.HandFood) Game.HandFood.SetActive(false);
            foreach (var renderer in Room.GetComponentsInChildren<Renderer>())
                if ((renderer.name.IndexOf("ceiling", System.StringComparison.OrdinalIgnoreCase) >= 0 || renderer.name.IndexOf("roof", System.StringComparison.OrdinalIgnoreCase) >= 0) && renderer.enabled) {
                    hiddenRoof.Add(renderer); renderer.enabled = false;
                }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void BeginFinishBrush(string id) {
            var finish = FinishCatalog.Find(id);
            if (finish == null) return;
            if (finish.Tier > Game.State.RankEarned) { Feedback(finish.Name + " unlocks at " + Reputation.Titles[finish.Tier] + "."); return; }
            if (finish.RequiredStars > Data.ShopStars) { Feedback(finish.Name + " unlocks at " + finish.RequiredStars + " stars."); return; }
            if (PlacementActive) CancelPlacement(false);
            SelectedCatalogId = id; movingId = -1; PreviewRotation = 0;
            BeginPlacementView(); FinishBrushActive = true;
            // Leave every wall strip clear of the HUD and the brush controls.
            Game.Player.View.transform.position = W(-8, 14, -15.5f);
            Game.Player.View.orthographicSize = 9;
            finishBrushObjects = new GameObject("Surface finish brush"); finishBrushObjects.transform.SetParent(transform, false);
            if (finish.IsWall) BuildFinishWallStrips();
            finishPatch = BrushSlab("Selected finish preview", Vector3.zero, Vector3.one, RestaurantArt.FinishMaterial(id));
            finishCap = BrushSlab("Selected wall top swatch", Vector3.zero, Vector3.one, RestaurantArt.FinishMaterial(id));
            var outlineObject = new GameObject("Selected surface outline"); outlineObject.transform.SetParent(finishBrushObjects.transform, false);
            finishOutline = outlineObject.AddComponent<LineRenderer>(); finishOutline.useWorldSpace = true;
            finishOutline.loop = true; finishOutline.positionCount = 4; finishOutline.widthMultiplier = .055f;
            // Lit is already included by the room's materials; an otherwise unused Unlit shader can be stripped in players.
            var outlineShader = Shader.Find("Universal Render Pipeline/Lit");
            if (!outlineShader) outlineShader = Shader.Find("Standard");
            finishOutlineMaterial = new Material(outlineShader) { color = new Color(1f, .82f, .23f) };
            finishOutline.sharedMaterial = finishOutlineMaterial;
            ChooseFinishSurface(finish.IsWall ? "wall:back:5" : "floor:5:4");
            Feedback(finish.Name + " brush: click one section or a placed wall. F quotes " + (finish.IsWall ? "all walls" : "the entire floor") + ".");
            UI.Rebuild();
        }

        GameObject BrushSlab(string name, Vector3 position, Vector3 size, Material material) {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube); slab.name = name;
            slab.transform.SetParent(finishBrushObjects.transform, false); slab.transform.position = position;
            slab.transform.localScale = size; var collider = slab.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            slab.GetComponent<Renderer>().sharedMaterial = material; return slab;
        }
        void BuildFinishWallStrips() {
            foreach (var key in FinishCatalog.SurfaceKeys(true)) {
                WallBrushGeometry(key, out var center, out var size);
                var id = Data.FinishAt(key); Material material = RestaurantArt.FinishMaterial(id);
                if (!material) foreach (var r in Room.GetComponentsInChildren<Renderer>())
                    if (r.name.StartsWith("WallFinish")) { material = r.sharedMaterial; break; }
                finishWallStrips[key] = BrushSlab("Wall target strip / " + key, center, size, material).GetComponent<Renderer>();
            }
        }
        static void WallBrushGeometry(string key, out Vector3 center, out Vector3 size) {
            var p = key.Split(':'); float along = int.Parse(p[2]) * WallSection + WallSection * .5f;
            bool back = p[1] == "back";
            center = back ? W(-16.5f + along, 3.91f, -21.93f) : W(p[1] == "left" ? -16.43f : -3.57f, 3.91f, -22 + along);
            size = back ? new Vector3(WallSection - .025f, .035f, .42f) : new Vector3(.42f, .035f, WallSection - .025f);
        }
        public bool ChooseFinishSurface(string key) {
            var finish = FinishCatalog.Find(SelectedCatalogId);
            if (!FinishBrushActive || finish == null || !Data.ValidFinishTarget(key, finish.IsWall)) return false;
            FinishSurfaceKey = key;
            Vector3 center, size;
            if (FinishCatalog.TryPieceKey(key, out int pieceId)) {
                if (!Furnishings.TryGetValue(pieceId, out var piece) || !piece) return false;
                var placed = Data.Layout.Find(item => item.InstanceId == pieceId);
                RestaurantArt.PieceFinishGeometry(piece, placed.CatalogId, out var localCenter, out var localSize);
                center = piece.transform.TransformPoint(localCenter);
                size = new Vector3(localSize.x, localSize.y, localSize.z + .015f);
                finishPatch.transform.position = center;
                finishPatch.transform.rotation = piece.transform.rotation;
                finishPatch.transform.localScale = size;
                finishCap.SetActive(false);
            } else if (finish.IsWall) {
                finishPatch.transform.rotation = Quaternion.identity;
                WallBrushGeometry(key, out center, out size);
                finishCap.SetActive(true); finishCap.transform.position = center + Vector3.up * .027f; finishCap.transform.localScale = size;
                var p = key.Split(':'); bool back = p[1] == "back";
                finishPatch.transform.position = new Vector3(center.x, Site.y + 1.9f, center.z) + (back ? Vector3.forward : p[1] == "left" ? Vector3.right : Vector3.left) * .072f;
                finishPatch.transform.localScale = back ? new Vector3(WallSection - .025f, 3.68f, .012f) : new Vector3(.012f, 3.68f, WallSection - .025f);
            } else {
                finishPatch.transform.rotation = Quaternion.identity;
                var p = key.Split(':');
                center = W(-16.5f + (int.Parse(p[1]) + .5f) * WallSection, .105f, -22 + (int.Parse(p[2]) + .5f) * FloorTileDepth);
                size = new Vector3(WallSection - .02f, .015f, FloorTileDepth - .02f);
                finishPatch.transform.position = center; finishPatch.transform.localScale = size; finishCap.SetActive(false);
            }
            finishPatch.SetActive(true); finishOutline.gameObject.SetActive(true);
            // A high outline remains visible even when a floor tile is underneath furniture.
            center.y = Site.y + (FinishCatalog.TryPieceKey(key, out _) ? 2.58f : 3.97f);
            float hx = size.x * .5f, hz = size.z * .5f;
            var rotate = FinishCatalog.TryPieceKey(key, out int selectedPiece) && Furnishings.TryGetValue(selectedPiece, out var selectedObject) ? selectedObject.transform.rotation : Quaternion.identity;
            finishOutline.SetPositions(new[] { center + rotate * new Vector3(-hx, 0, -hz), center + rotate * new Vector3(hx, 0, -hz), center + rotate * new Vector3(hx, 0, hz), center + rotate * new Vector3(-hx, 0, hz) });
            Hint = FinishTargetLabel + " / " + finish.Name + " / $" + Data.FinishPrice(SelectedCatalogId, key, false) + " per patch";
            return true;
        }

        bool PointerOverFinishUI(Vector2 position) {
            if (!EventSystem.current) return false;
            finishUIHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, finishUIHits);
            return finishUIHits.Count > 0;
        }
        bool HandleFinishBrushInput(Keyboard keys, Mouse mouse, Gamepad pad) {
            if (keys != null && keys.fKey.wasPressedThisFrame) RequestFinishFill();
            if (keys != null && keys.enterKey.wasPressedThisFrame && FinishFillPending) ConfirmFinishFill();
            if (pad != null) {
                if (pad.rightShoulder.wasPressedThisFrame) RequestFinishFill();
                if (PadPlacementStep(pad, out int dx, out int dz) && !FinishFillPending) {
                    var finish = FinishCatalog.Find(SelectedCatalogId);
                    var parts = FinishSurfaceKey.Split(':');
                    if (finish != null && finish.IsWall) {
                        int section = parts.Length == 3 && int.TryParse(parts[2], out int parsed) ? parsed : 5;
                        string side = parts.Length == 3 && parts[0] == "wall" ? parts[1] : "back";
                        if (dz != 0) side = side == "back" ? (dz > 0 ? "right" : "left") : "back";
                        ChooseFinishSurface("wall:" + side + ":" + Mathf.Clamp(section + dx, 0, 11));
                    } else if (finish != null) {
                        int x = parts.Length == 3 && int.TryParse(parts[1], out int parsedX) ? parsedX : 5;
                        int z = parts.Length == 3 && int.TryParse(parts[2], out int parsedZ) ? parsedZ : 4;
                        ChooseFinishSurface("floor:" + Mathf.Clamp(x + dx, 0, 11) + ":" + Mathf.Clamp(z + dz, 0, 9));
                    }
                }
                if (pad.buttonSouth.wasPressedThisFrame) {
                    if (FinishFillPending) ConfirmFinishFill(); else ApplySelectedFinish(false);
                }
            }
            if (mouse != null && !PointerOverFinishUI(mouse.position.ReadValue()) && !FinishFillPending) {
                bool target = UpdateFinishFromPointer(mouse.position.ReadValue());
                if (target && mouse.leftButton.wasPressedThisFrame) ApplySelectedFinish(false);
            }
            return true;
        }
        bool UpdateFinishFromPointer(Vector2 screen) {
            if (!FinishBrushActive || FinishFillPending || PointerOverFinishUI(screen)) return false;
            var ray = Game.Player.View.ScreenPointToRay(screen);
            var finish = FinishCatalog.Find(SelectedCatalogId);
            if (finish.IsWall) foreach (var hit in Physics.RaycastAll(ray, 100f)) {
                var target = hit.collider.GetComponentInParent<RestaurantTarget>();
                if (target && ChooseFinishSurface("piece:" + target.InstanceId)) return true;
            }
            var plane = new Plane(Vector3.up, W(0, finish.IsWall ? 3.91f : .055f, 0));
            if (!plane.Raycast(ray, out float distance)) return false;
            var p = ray.GetPoint(distance) - Site;
            string key = "";
            if (finish.IsWall) {
                if (p.x >= -16.5f && p.x < -3.5f && Mathf.Abs(p.z + 21.93f) <= .28f) key = "wall:back:" + Mathf.FloorToInt((p.x + 16.5f) / WallSection);
                else if (p.z >= -22 && p.z < -9 && Mathf.Abs(p.x + 16.43f) <= .28f) key = "wall:left:" + Mathf.FloorToInt((p.z + 22) / WallSection);
                else if (p.z >= -22 && p.z < -9 && Mathf.Abs(p.x + 3.57f) <= .28f) key = "wall:right:" + Mathf.FloorToInt((p.z + 22) / WallSection);
            } else if (p.x >= -16.5f && p.x < -3.5f && p.z >= -22 && p.z < -9)
                key = "floor:" + Mathf.FloorToInt((p.x + 16.5f) / WallSection) + ":" + Mathf.FloorToInt((p.z + 22) / FloorTileDepth);
            if (ChooseFinishSurface(key)) return true;
            FinishSurfaceKey = ""; finishPatch.SetActive(false); finishCap.SetActive(false); finishOutline.gameObject.SetActive(false);
            Hint = finish.IsWall ? "Point at a wall strip or a placed partition or service window." : "Point at a floor tile inside the room.";
            return false;
        }

        public bool ApplySelectedFinish(bool fill) {
            if (!FinishBrushActive || string.IsNullOrEmpty(FinishSurfaceKey)) return false;
            bool changed = Data.ApplyFinish(Game.State, SelectedCatalogId, FinishSurfaceKey, fill, out string message);
            Feedback(message);
            if (changed) {
                RestaurantArt.RenderSurfaceFinishes(Room, Data); PlayChime(true); Game.Save();
                foreach (var item in Data.Layout)
                    if ((item.CatalogId == "partition_wall" || item.CatalogId == "service_window") && Furnishings.TryGetValue(item.InstanceId, out var piece))
                        RestaurantArt.RenderPieceFinish(piece, item.CatalogId, Data.FinishAt("piece:" + item.InstanceId), Data.WallId);
                foreach (var strip in finishWallStrips) {
                    var material = RestaurantArt.FinishMaterial(Data.FinishAt(strip.Key));
                    if (material && strip.Value) strip.Value.sharedMaterial = material;
                }
                ChooseFinishSurface(FinishSurfaceKey);
            }
            return changed;
        }
        public void RequestFinishFill() {
            if (!FinishBrushActive) return;
            if (string.IsNullOrEmpty(FinishSurfaceKey)) ChooseFinishSurface(FinishCatalog.Find(SelectedCatalogId).IsWall ? "wall:back:5" : "floor:5:4");
            FinishFillQuote = Data.FinishPrice(SelectedCatalogId, FinishSurfaceKey, true);
            if (FinishFillQuote < 0) return;
            FinishFillPending = true; UI.Refresh();
        }
        public void CancelFinishFill() { FinishFillPending = false; UI.Refresh(); }
        public void ConfirmFinishFill() {
            if (!FinishFillPending) return;
            int current = Data.FinishPrice(SelectedCatalogId, FinishSurfaceKey, true);
            if (current != FinishFillQuote) { FinishFillQuote = current; Feedback("The fill total changed. Review the new price and confirm again."); return; }
            FinishFillPending = false; ApplySelectedFinish(true); UI.Refresh();
        }
        void EndFinishBrush() {
            if (finishBrushObjects) { finishBrushObjects.SetActive(false); Destroy(finishBrushObjects); }
            if (finishOutlineMaterial) Destroy(finishOutlineMaterial);
            finishWallStrips.Clear();
            FinishBrushActive = false; FinishFillPending = false; FinishSurfaceKey = "";
        }
    }
}
