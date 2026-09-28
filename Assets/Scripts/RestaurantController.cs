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
        public bool Inside => InRestaurant(Game.Player.transform.position);
        // All restaurant coordinates are written for The Odd Table; Site shifts them to whichever property you chose.
        public static Vector3 Site; public static bool ArriveAfterReload;
        public static Vector3 W(float x, float y, float z) => new Vector3(x, y, z) + Site;
        static bool InRestaurant(Vector3 p) { p -= Site; return p.x > -17 && p.x < -3 && p.z < -8 && p.z > -23; }
        bool anyoneInsideLastFrame = true;
        // When the last player walks out mid-service, flash a 3-second warning for any job nobody covers.
        void CheckLeavingStaffing() {
            bool anyoneInside = Game.CoOp ? Game.CoOp.Players.Any(p => p && InRestaurant(p.transform.position)) : Inside;
            if (!anyoneInside && anyoneInsideLastFrame && Data.Owned && ServiceInProgress) {
                var missing = new List<string>();
                if (!Data.Workers.Any(w => w.Job == StaffJob.Cook && w.Energy > 2)) missing.Add("cook");
                if (!Data.Workers.Any(w => w.Job == StaffJob.Serve && w.Energy > 2)) missing.Add("server");
                if (!Data.Workers.Any(w => w.Job == StaffJob.Clean && w.Energy > 2)) missing.Add("dishwasher");
                if (missing.Count > 0) Game.Notify("Heads up: you're leaving without a " + string.Join(", ", missing) + ". Those jobs stop while you're gone.", 3);
            }
            anyoneInsideLastFrame = anyoneInside;
        }
        public bool AtSupplier => Vector2.Distance(new Vector2(Game.Player.transform.position.x, Game.Player.transform.position.z), MiloSpot) < MiloRadius;
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
        int savedPlacementMask;
        int previewX, previewZ, movingId = -1;
        float hudTimer;
        bool previewValid;
        // A shift has a shape: calm opening, a RUSH in the middle (harder at night), then a wind-down.
        public const float RushStart = 30, RushEnd = 80;
        public string ShiftPhase => !Game.State.Kitchen.ShiftActive || !Data.Open ? "" : shiftTime < RushStart ? "calm" : shiftTime < RushEnd ? "rush" : "wind";
        public bool Rush => ShiftPhase == "rush";

        public void Initialize(CityGame game) {
            Game = game;
            if (Game.State.Restaurant == null) Game.State.Restaurant = new RestaurantState();
            var previous = GameObject.Find("Future Restaurant"); if (previous) previous.SetActive(false);
            var site = RestaurantSites.Get(Data.SiteId); Site = site.Offset;
            var siteRoot = new GameObject("Restaurant site / " + site.Title).transform; siteRoot.SetParent(transform, false); siteRoot.localPosition = Site;
            RestaurantArt.RestaurantName = site.Sign;
            Room = RestaurantArt.BuildRoom(siteRoot);
            BuildShowrooms();
            Room.name = "Little Flame / Your restaurant";
            RebuildLayout();
            var desk = new GameObject("Restaurant management"); desk.transform.SetParent(Room.transform);
            desk.transform.position = W(-5.4f, 1, -10.4f);
            var deskCollider = desk.AddComponent<BoxCollider>(); deskCollider.size = new Vector3(1.2f, 1.8f, .7f);
            desk.AddComponent<RestaurantTarget>().Kind = "Management";
            WorldCaption(desk.transform, "LITTLE FLAME\n[E] MANAGE", new Vector3(0, 1.15f, 0), .024f);
            UI = gameObject.AddComponent<RestaurantUI>(); UI.Owner = this; UI.Rebuild();
            PhysicalSetup();
            if (ArriveAfterReload) { ArriveAfterReload = false; StartCoroutine(ArriveAtNewRestaurant()); }
        }
        System.Collections.IEnumerator ArriveAtNewRestaurant() {
            yield return null; yield return null;
            Game.Player.Teleport(W(-10, .15f, -10.7f)); Game.Player.LookAt(W(-10, 1.5f, -18)); PlayChime(true);
            Feedback("Welcome to " + RestaurantSites.Get(Data.SiteId).Title + ". Four walls and a lot of dust: press B inside to start buying your kitchen.");
        }
        // The properties you didn't pick stay on the map as shabby, empty shells with a lease sign.
        void BuildShowrooms() {
            foreach (var other in RestaurantSites.All) {
                if (other.Id == Data.SiteId) continue;
                var root = new GameObject("Property for lease / " + other.Title).transform; root.SetParent(transform, false); root.localPosition = other.Offset;
                RestaurantArt.RestaurantName = other.Sign; RestaurantArt.BuildRoom(root);
                if (other.Id != "oddtable") {
                    var sign = GameObject.CreatePrimitive(PrimitiveType.Cube); sign.name = "Lease sign / " + other.Title; sign.transform.SetParent(root, false);
                    sign.transform.localPosition = new Vector3(-6.2f, 1.25f, -8.7f); sign.transform.localScale = new Vector3(2.9f, 2.2f, .15f);
                    var signMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); signMat.color = new Color(.12f, .13f, .15f); sign.GetComponent<Renderer>().sharedMaterial = signMat;
                    var it = sign.AddComponent<Interactable>(); it.Kind = InteractionKind.FutureRestaurant; it.Site = other.Id;
                    var text = new GameObject("Lease text"); text.transform.SetParent(root, false); text.transform.localPosition = new Vector3(-6.2f, 1.35f, -8.6f); text.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    WorldCaption(text.transform, other.Sign + "\n\nFOR SALE LATER\nE: VIEW LISTING", Vector3.zero, .03f);
                }
            }
            RestaurantArt.RestaurantName = RestaurantSites.Get(Data.SiteId).Sign;
            // The scene's original lease sign belongs to The Odd Table.
            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None)) if (it.Kind == InteractionKind.FutureRestaurant && it.name == "Future restaurant sign") it.Site = "oddtable";
        }
        void Update() {
            if (!Game || !Room) return;
            hudTimer -= Time.unscaledDeltaTime;
            if (hudTimer <= 0) { hudTimer = .35f; UI.Refresh(); }
            if (!Game.Paused && !ManagementPauses && !PlacementActive && !Game.SmokeMode) { Advance(Time.deltaTime); CheckLeavingStaffing(); }
            // "Phone": P opens your crew list from anywhere in the city.
            if (!Game.Paused && Game.Started && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && !PlacementActive && (Data.Owned || Game.State.StandBuilt)) { if (PanelOpen) ClosePanel(); else ShowPanel("Phone"); }
            if (!Game.Paused && Game.Started && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame && !PlacementActive) { if (PanelOpen && Panel == "Map") ClosePanel(); else ShowPanel("Map"); }
        }
        public void Feedback(string message) { Hint = message; Game.Notify(message, 5); if (UI) UI.Refresh(); }
        public string ListingSite = "bayside";
        public bool BuyRestaurant() => BuyRestaurant(Game.Player);
        public bool BuyRestaurant(FirstPersonPlayer buyer) => BuyRestaurant(buyer, Data.SiteId);
        public bool BuyRestaurant(FirstPersonPlayer buyer, string siteId) {
            var chosen = RestaurantSites.Get(siteId);
            if (Data.Owned && chosen.Id == Data.SiteId) { ShowPanel("Service"); return true; }
            // The Bayside is the step up: show what it takes instead of buying it.
            if (!chosen.Starter) { ListingSite = chosen.Id; ShowPanel("Listing"); return true; }
            bool result = Data.BuyRestaurant(Game.State, out string message);
            Feedback(message);
            if (!result) return false;
            RebuildLayout(); PlayChime(true); Game.Save(); buyer.Teleport(W(-10, .15f, -10.7f)); buyer.LookAt(W(-10, 1.5f, -18)); ShowPanel("Service");
            return true;
        }
        public void ShowPanel(string panel) {
            bool phone = (panel == "Staff" && Game.State.StandBuilt) || panel == "Map" || panel == "Listing" || panel == "Supplies" && Game.State.StandBuilt || panel == "Phone" && Game.State.StandBuilt;
            if (!Data.Owned && !phone) { Feedback("Earn $150 and buy the restaurant at its front sign."); return; }
            if (ServiceInProgress && panel != "Staff" && panel != "Service" && panel != "Map" && panel != "Supplies" && panel != "Phone") { Feedback("Service is live. Use the stations; E at the door sign stops new arrivals. Management is available after the last guest leaves."); return; }
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
            var p = ray.GetPoint(distance) - Site; var item = RestaurantCatalog.Find(SelectedCatalogId);
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
            if (item.Tier > Game.State.RankEarned) { Feedback(item.Name + " unlocks at " + Reputation.Titles[item.Tier] + ". Better gear arrives with each district."); return; }
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
            savedPlacementMask = Game.Player.View.cullingMask;
            savedLook = Game.Player.View.transform.position + Game.Player.View.transform.forward * 10;
            Game.Player.View.transform.position = W(-10, 14, -15.5f);
            Game.Player.View.transform.rotation = Quaternion.Euler(90, 0, 0); Game.Player.View.orthographic = true; Game.Player.View.orthographicSize = 8;
            if (Game.CoOp) Game.CoOp.RefreshViews();
            Game.Player.View.cullingMask &= ~(1 << 27);
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
            Game.Player.View.cullingMask = savedPlacementMask;
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
        public static Vector3 CellCenter(int x, int z, int width = 1, int depth = 1) => new Vector3(-15.5f + x + (width - 1) * .5f, .055f, -20.5f + z + (depth - 1) * .5f) + Site;
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
        // Milo's shop: pay at the counter and carry the groceries home in a bag.
        public void OrderZeeb(int bottles) { Game.State.OrderFromZeeb(bottles, out string m); Feedback(m); Game.Save(); UI.Rebuild(); }
        public void PayZeeb() { Game.State.PayZeeb(out string m); Feedback(m); Game.Save(); UI.Rebuild(); }
        public void BuyRecipe(string dish) { Game.State.BuyRecipe(dish, out string message); Feedback(message); Game.Save(); UI.Rebuild(); }
        public void AskMiloForHelp() {
            if (!AtSupplier) { Feedback("Talk to Milo in his shop."); return; }
            Data.RequestSupplyHelp(Game.State, out string message); Feedback(message); Game.Save(); UI.Rebuild();
        }
        public bool BuyGroceries(List<StockLine> cart) {
            if (!AtSupplier) { Feedback("Talk to Milo in his shop to buy."); return false; }
            bool ok = Game.State.Kitchen.BuyGroceries(Game.State, Game.Player.ActorId, cart, out string message);
            Feedback(message); if (ok) { Game.Save(); ClosePanel(); } else UI.Rebuild();
            return ok;
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
