using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RestaurantCity {
    public class RestaurantTarget : MonoBehaviour {
        public string Kind = "Furniture";
        public int InstanceId = -1;
        public int OrderId = -1;
        // Set on a child hitbox (e.g. one pantry ingredient shelf) to say which ingredient it offers.
        // Empty means "the furnishing generally" (Preview falls back to a default sub-choice).
        public string SubId = "";
    }

    public partial class RestaurantController : MonoBehaviour {
        public CityGame Game;
        public RestaurantState Data => Game.State.Restaurant;
        public bool Inside => Game.Player.transform.position.x > -17 && Game.Player.transform.position.x < -3 && Game.Player.transform.position.z < -8;
        public bool AtSupplier => Vector2.Distance(new Vector2(Game.Player.transform.position.x, Game.Player.transform.position.z), new Vector2(-12, 9)) < 4.5f;
        public bool PanelOpen { get; private set; }
        public string Panel { get; private set; } = "Catalog";
        public int SelectedInstanceId { get; private set; } = -1;
        public bool PlacementActive { get; private set; }
        public string SelectedCatalogId { get; private set; }
        public int PreviewRotation { get; private set; }
        public Vector2Int PreviewCell => new Vector2Int(previewX, previewZ);
        public string Hint { get; private set; } = "";
        public string FocusPrompt { get; private set; } = "";
        public int CarriedOrderId { get; private set; } = -1;
        public GameObject Room { get; private set; }
        public RestaurantUI UI { get; private set; }
        public readonly Dictionary<int, GameObject> Furnishings = new Dictionary<int, GameObject>();
        readonly Dictionary<string, Texture> thumbnails = new Dictionary<string, Texture>();
        GameObject preview, footprint;
        readonly List<Renderer> hiddenRoof = new List<Renderer>();
        Vector3 savedPosition, savedViewLocal, savedLook;
        Quaternion savedViewRotation;
        int previewX, previewZ, movingId = -1;
        float hudTimer;
        bool previewValid;
        public bool Rush => Data.Open && (Game.State.Clock > 65 && Game.State.Clock < 115 || Game.State.Clock > 175 && Game.State.Clock < 215);

        public void Initialize(CityGame game) {
            Game = game;
            if (Game.State.Restaurant == null) Game.State.Restaurant = new RestaurantState();
            var previous = GameObject.Find("Future Restaurant"); if (previous) previous.SetActive(false);
            Room = RestaurantArt.BuildRoom(transform);
            Room.name = "Little Flame / Your restaurant";
            RebuildLayout();
            var desk = new GameObject("Restaurant management"); desk.transform.SetParent(Room.transform);
            desk.transform.position = new Vector3(-5.4f, 1, -10.4f);
            var deskCollider = desk.AddComponent<BoxCollider>(); deskCollider.size = new Vector3(1.2f, 1.8f, .7f);
            desk.AddComponent<RestaurantTarget>().Kind = "Management";
            WorldCaption(desk.transform, "LITTLE FLAME\n[E] MANAGE", new Vector3(0, 1.15f, 0), .024f);
            UI = gameObject.AddComponent<RestaurantUI>(); UI.Owner = this; UI.Rebuild();
            PhysicalSetup();
        }
        void Update() {
            if (!Game || !Room) return;
            hudTimer -= Time.unscaledDeltaTime;
            if (hudTimer <= 0) { hudTimer = .35f; UI.Refresh(); }
            if (!Game.Paused && !ManagementPauses && !PlacementActive && !Game.SmokeMode) Advance(Time.deltaTime);
            // "Phone": P opens your crew list from anywhere in the city.
            if (!Game.Paused && Game.Started && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && !PlacementActive && (Data.Owned || Game.State.StandBuilt)) { if (PanelOpen) ClosePanel(); else ShowPanel("Staff"); }
        }
        public void Feedback(string message) { Hint = message; Game.Notify(message, 5); if (UI) UI.Refresh(); }
        public bool BuyRestaurant() => BuyRestaurant(Game.Player);
        public bool BuyRestaurant(FirstPersonPlayer buyer) {
            if (Data.Owned) { ShowPanel("Service"); return true; }
            bool result = Data.BuyRestaurant(Game.State, out string message);
            Feedback(message);
            if (result) { RebuildLayout(); PlayChime(true); Game.Save(); buyer.Teleport(new Vector3(-10, .15f, -10.7f)); buyer.LookAt(new Vector3(-10, 1.5f, -18)); ShowPanel("Service"); }
            return result;
        }
        public void ShowPanel(string panel) {
            bool phone = panel == "Staff" && Game.State.StandBuilt;
            if (!Data.Owned && !phone) { Feedback("Earn $150 and buy the restaurant at its front sign."); return; }
            if (ServiceInProgress && panel != "Staff" && panel != "Service") { Feedback("Service is live. Use the stations; E at the door sign stops new arrivals. Management is available after the last guest leaves."); return; }
            if (PlacementActive) CancelPlacement(false);
            Panel = panel; PanelOpen = true;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            UI.Rebuild();
        }
        public void ClosePanel() {
            PanelOpen = false;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            Cursor.lockState = Game.Paused || Game.SmokeMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Game.Paused || Game.SmokeMode;
            UI.Rebuild(); Game.Save();
        }
        public bool HandleInput(Keyboard keys, Mouse mouse) {
            if (PlacementActive) {
                if (keys != null && (keys.escapeKey.wasPressedThisFrame || keys.bKey.wasPressedThisFrame) || mouse != null && mouse.rightButton.wasPressedThisFrame) { CancelPlacement(); return true; }
                if (keys != null && keys.rKey.wasPressedThisFrame) PreviewRotation = (PreviewRotation + 1) % 4;
                if (mouse != null) {
                    UpdatePreviewFromPointer(mouse.position.ReadValue());
                    if (mouse.leftButton.wasPressedThisFrame) ConfirmPlacement(previewX, previewZ);
                }
                return true;
            }
            if (PanelOpen) { if (keys != null && keys.escapeKey.wasPressedThisFrame) ClosePanel(); return true; }
            if (!Game.Paused && keys != null && Data.Owned && Inside) {
                if (keys.bKey.wasPressedThisFrame) { ShowPanel("Catalog"); return true; }
                if (keys.tabKey.wasPressedThisFrame) { ShowPanel("Service"); return true; }
            }
            return false;
        }
        public bool UpdatePreviewFromPointer(Vector2 screenPosition) {
            if (!PlacementActive) return false;
            var ray = Game.Player.View.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return false;
            var p = ray.GetPoint(distance); var item = RestaurantCatalog.Find(SelectedCatalogId);
            int w = PreviewRotation % 2 == 0 ? item.Width : item.Depth;
            int d = PreviewRotation % 2 == 0 ? item.Depth : item.Width;
            previewX = Mathf.RoundToInt(p.x + 15.5f - (w - 1) * .5f);
            previewZ = Mathf.RoundToInt(p.z + 20.5f - (d - 1) * .5f);
            UpdatePreview();
            return true;
        }
        public bool InspectRay(RaycastHit hit, bool activate) {
            FocusPrompt = "";
            var target = hit.collider.GetComponentInParent<RestaurantTarget>();
            if (!target) return false;
            if (!Data.Owned) { FocusPrompt = "Buy this restaurant at the front sign  /  $150"; return true; }
            if (target.Kind == "Customer") {
                FocusPrompt = CarriedOrderId == target.OrderId ? "E  Serve this guest" : "E  View guest's order and preference";
                if (activate) ServeGuest(target.OrderId);
            } else if (target.Kind == "Management") {
                FocusPrompt = "E  Manage restaurant  /  B  Decorate"; if (activate) ShowPanel("Service");
            } else {
                var placed = Data.Layout.Find(p => p.InstanceId == target.InstanceId);
                var item = placed == null ? null : RestaurantCatalog.Find(placed.CatalogId);
                if (item != null && item.Category == CatalogCategory.Kitchen) {
                    if (Data.CanCustomize) { FocusPrompt = "E  Move or sell kitchen equipment  /  Tab manage"; if (activate) { SelectedInstanceId = target.InstanceId; ShowPanel("Furniture"); } return true; }
                    FocusPrompt = "E  Kitchen tickets / collect a ready dish";
                    if (activate) { var ready = Data.Orders.Find(o => o.Stage == RestaurantOrderStage.Ready); if (ready == null || !CollectDish(ready.Id)) ShowPanel("Service"); }
                } else {
                    FocusPrompt = "E  Inspect " + (item == null ? "furnishing" : item.Name);
                    if (activate) { SelectedInstanceId = target.InstanceId; ShowPanel("Furniture"); }
                }
            }
            return true;
        }
        public void ClearFocus() { FocusPrompt = ""; }
        public void SelectCatalogItem(string id) {
            if (!Data.CanCustomize) { Feedback("Close service and finish your remaining guests before remodeling."); return; }
            var item = RestaurantCatalog.Find(id); if (item == null) return;
            if (Data.Stars < item.RequiredStars) { Feedback("Earn two stars to unlock " + item.Name + "."); return; }
            if (item.IsFinish || item.IsExterior) {
                bool bought = Data.Place(Game.State, id, 0, 0, 0, out string reason); Feedback(reason);
                if (bought) { RebuildLayout(); PlayChime(true); Game.Save(); UI.Rebuild(); }
                return;
            }
            if (Game.State.Cash < item.Price) { Feedback("You need $" + item.Price + " for " + item.Name + "."); return; }
            BeginPlacement(id, -1, 0);
        }
        void BeginPlacement(string id, int instanceId, int rotation) {
            SelectedCatalogId = id; movingId = instanceId; PreviewRotation = rotation;
            PanelOpen = false; PlacementActive = true;
            savedPosition = Game.Player.transform.position; savedViewLocal = Game.Player.View.transform.localPosition;
            savedViewRotation = Game.Player.View.transform.localRotation;
            savedLook = Game.Player.View.transform.position + Game.Player.View.transform.forward * 10;
            Game.Player.View.transform.position = new Vector3(-10, 14, -15.5f);
            Game.Player.View.transform.rotation = Quaternion.Euler(90, 0, 0); Game.Player.View.orthographic = true; Game.Player.View.orthographicSize = 8;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            Game.Player.Spatula.gameObject.SetActive(false); Game.HandFood.SetActive(false);
            foreach (var renderer in Room.GetComponentsInChildren<Renderer>()) if (renderer.name.IndexOf("ceiling", StringComparison.OrdinalIgnoreCase) >= 0 || renderer.name.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0) { if (renderer.enabled) { hiddenRoof.Add(renderer); renderer.enabled = false; } }
            preview = CreateFurnishing(id, transform); preview.name = "Placement preview";
            foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
            footprint = GameObject.CreatePrimitive(PrimitiveType.Cube); footprint.name = "Placement validity"; Destroy(footprint.GetComponent<Collider>());
            footprint.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (movingId >= 0 && Furnishings.TryGetValue(movingId, out var original)) original.SetActive(false);
            previewX = 0; previewZ = 4; UpdatePreview();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; UI.Rebuild();
        }
        void UpdatePreview() {
            var item = RestaurantCatalog.Find(SelectedCatalogId);
            int width = PreviewRotation % 2 == 0 ? item.Width : item.Depth, depth = PreviewRotation % 2 == 0 ? item.Depth : item.Width;
            var point = CellCenter(previewX, previewZ, width, depth);
            preview.transform.position = point; preview.transform.rotation = Quaternion.Euler(0, PreviewRotation * 90, 0);
            footprint.transform.position = point + Vector3.up * .045f; footprint.transform.localScale = new Vector3(width * .98f, .035f, depth * .98f);
            previewValid = Data.CanPlace(SelectedCatalogId, previewX, previewZ, PreviewRotation, movingId, out string why);
            footprint.GetComponent<Renderer>().material.color = previewValid ? new Color(.25f, .9f, .65f) : new Color(.95f, .24f, .22f);
            Hint = previewValid ? "Click to place  /  R rotate  /  Right click cancel" : why;
        }
        public bool ConfirmPlacement(int x, int z) {
            if (!PlacementActive) return false;
            bool result = movingId >= 0 ? Data.Move(movingId, x, z, PreviewRotation, out string reason) : Data.Place(Game.State, SelectedCatalogId, x, z, PreviewRotation, out reason);
            Feedback(reason);
            if (result) { CancelPlacement(false); RebuildLayout(); PlayChime(true); Game.Save(); ShowPanel("Catalog"); }
            return result;
        }
        public void CancelPlacement(bool reopen = true) {
            if (!PlacementActive) return;
            if (preview) Destroy(preview); if (footprint) Destroy(footprint);
            foreach (var r in hiddenRoof) if (r) r.enabled = true; hiddenRoof.Clear();
            Game.Player.View.orthographic = false; Game.Player.View.transform.localPosition = savedViewLocal;
            Game.Player.View.transform.localRotation = savedViewRotation; Game.Player.Spatula.gameObject.SetActive(true);
            if (movingId >= 0 && Furnishings.TryGetValue(movingId, out var original)) original.SetActive(true);
            PlacementActive = false; movingId = -1;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            if (reopen) ShowPanel("Catalog"); else { Cursor.lockState = Game.SmokeMode ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = Game.SmokeMode; UI.Rebuild(); }
        }
        public void MoveItem(int id) {
            if (!Data.CanCustomize) { Feedback("Finish the service before moving furniture."); return; }
            var item = Data.Layout.Find(p => p.InstanceId == id); if (item == null) return;
            BeginPlacement(item.CatalogId, id, item.Rotation);
        }
        public void SelectFurnitureItem(int id) {
            if (!Data.Layout.Any(p => p.InstanceId == id)) return;
            SelectedInstanceId = id;
            if (Panel != "Furniture" || !PanelOpen) ShowPanel("Furniture");
            else UI.Refresh();
        }
        public void SellItem(int id) {
            if (Game.State.Kitchen.At(id) != null) { Feedback("Clear this station before selling it."); return; }
            bool sold = Data.Sell(Game.State, id, out string reason); Feedback(reason);
            if (sold) { SelectedInstanceId = -1; RebuildLayout(); Game.Save(); ShowPanel("Catalog"); }
        }
        public static Vector3 CellCenter(int x, int z, int width = 1, int depth = 1) => new Vector3(-15.5f + x + (width - 1) * .5f, .055f, -20.5f + z + (depth - 1) * .5f);
        public void RebuildLayout() {
            Game.State.Kitchen.EnsureStations(Data);
            foreach (var obj in Furnishings.Values) if (obj) { obj.SetActive(false); Destroy(obj); }
            Furnishings.Clear();
            if (!Data.Owned) { RestaurantArt.UpdateFinishes(Room, "wall_shabby", "floor_shabby", false, false); return; }
            foreach (var p in Data.Layout) {
                var item = RestaurantCatalog.Find(p.CatalogId);
                if (item == null || item.IsFinish || item.IsExterior) continue;
                var obj = CreateFurnishing(p.CatalogId, Room.transform);
                int w = p.Rotation % 2 == 0 ? item.Width : item.Depth, d = p.Rotation % 2 == 0 ? item.Depth : item.Width;
                obj.transform.position = CellCenter(p.X, p.Z, w, d); obj.transform.rotation = Quaternion.Euler(0, p.Rotation * 90, 0);
                var target = obj.AddComponent<RestaurantTarget>(); target.InstanceId = p.InstanceId;
                if (obj.GetComponentsInChildren<Collider>().Length == 0) { var collider = obj.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .65f, 0); collider.size = new Vector3(item.Width * .9f, 1.3f, item.Depth * .9f); }
                // Child hitboxes (pantry ingredient shelves) share the furnishing's instance id so a
                // station lookup by InstanceId works no matter which collider the interaction ray hit.
                foreach (var child in obj.GetComponentsInChildren<RestaurantTarget>()) child.InstanceId = p.InstanceId;
                Furnishings[p.InstanceId] = obj;
            }
            RestaurantArt.UpdateFinishes(Room, Data.WallId, Data.FloorId, Data.Layout.Any(p => p.CatalogId == "awning_coral"), Data.Layout.Any(p => p.CatalogId == "sign_neon"));
            Game.State.Kitchen.EnsureStations(Data);
        }
        public Texture GetCatalogIcon(string id) {
            if (thumbnails.TryGetValue(id, out Texture found)) return found;
            var stage = new GameObject("Catalog photo stage"); stage.transform.position = new Vector3(800, 0, 800);
            var obj = CreateFurnishing(id, stage.transform); obj.transform.localPosition = Vector3.zero;
            foreach (var t in obj.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;
            var cam = new GameObject("Catalog camera").AddComponent<Camera>(); cam.enabled = false; cam.cullingMask = 1 << 30;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.87f, .86f, .77f); cam.orthographic = true;
            var renderers = obj.GetComponentsInChildren<Renderer>(); Bounds bounds = new Bounds(obj.transform.position + Vector3.up * .65f, Vector3.one);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            cam.orthographicSize = Mathf.Max(1.3f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * .7f);
            cam.transform.position = bounds.center + new Vector3(5, 4, 6); cam.transform.LookAt(bounds.center); cam.nearClipPlane = .05f; cam.farClipPlane = 30;
            var fill = new GameObject("Catalog light").AddComponent<Light>(); fill.type = LightType.Directional; fill.cullingMask = 1 << 30; fill.intensity = 1.3f; fill.transform.rotation = Quaternion.Euler(35, -30, 0);
            var texture = new RenderTexture(320, 240, 24); texture.Create();
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            thumbnails[id] = texture; stage.SetActive(false); Destroy(stage); Destroy(cam.gameObject); Destroy(fill.gameObject); return texture;
        }
        public TextMesh WorldCaption(Transform parent, string caption, Vector3 local, float size = .021f) {
            var go = new GameObject("Guest reaction"); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var text = go.AddComponent<TextMesh>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 64; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(1, .94f, .76f); text.text = caption;
            var binding = Game.GetComponent<WorldTextFont>(); if (binding) text.GetComponent<MeshRenderer>().sharedMaterial = binding.Material;
            return text;
        }

        public void ToggleService() {
            bool result = Data.Open ? Data.EndService(out string message) : Data.StartService(Game.State, out message);
            if (result && Data.Open) { Game.State.Kitchen.StartShift(Game.State); ClosePanel(); }
            Feedback(message); if (result) Game.Save(); UI.Rebuild();
        }
        public void TryCook(int id) { Feedback("Cooking happens at the stations: pantry, prep, grill, then assemble on a plate. Follow your order card."); }
        public void Restock(bool protein) {
            if (!AtSupplier) { Feedback("Visit Milo's green supplier counter across the street to stock up."); return; }
            if (!Data.Restock(Game.State, protein, out string message) && Game.State.Cash < 6 && Data.Produce == 0) Data.RequestSupplyHelp(Game.State, out message);
            Feedback(message); Game.Save(); UI.Rebuild();
        }
        public void RestockStand() {
            if (!AtSupplier) return;
            Feedback(Game.State.BuyIngredients() ? "Packed 3 stand ingredients for $6." : "Stand ingredients cost $6."); Game.Save(); UI.Refresh();
        }
        public void Clean() { BeginCleaning(); }
        public void Hire(string id) { bool hired = Data.Hire(Game.State, id, out string message); Feedback(message); if (hired) PlayChime(true); Game.Save(); UI.Rebuild(); }
        public void Assign(string id, StaffJob job) { bool ok = Data.Assign(id, job, out string message); if (ok && job == StaffJob.Stand) Game.State.StandOpen = true; Feedback(message); Game.Save(); UI.Rebuild(); }
        public void ToggleDish(string id) { Data.ToggleDish(Game.State, id, out string message); Feedback(message); RefreshMenuBoard(); Game.Save(); UI.Rebuild(); }
        public void ServeGuest(int id) {
            if (Game.State.Kitchen.Serve(Game.State, Game.Player.ActorId, id, out string message)) PlayChime(false);
            Feedback(message); Game.Save();
        }
        // Stage two adds arrivals, world navigation and visible staff tasks.
        public void Advance(float seconds) { Data.Tick(Game.State, seconds); Game.State.Kitchen.Tick(Game.State, seconds); TickServiceActors(seconds); TickPhysicalService(seconds); AnimateRival(seconds); TickStreet(seconds); }
        partial void TickServiceActors(float seconds);
    }
}
