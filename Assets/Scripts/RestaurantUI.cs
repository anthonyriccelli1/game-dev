using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RestaurantCity {
    // Camera-space UI intentionally travels through the same render path as the game.
    // No modal is recreated just because an order timer ticks: mouse-down and scrolling survive Refresh.
    public class RestaurantUI : MonoBehaviour {
        public RestaurantController Owner;
        readonly Color ink = new Color(.075f, .16f, .20f);
        readonly Color paper = new Color(.97f, .94f, .84f);
        readonly Color white = new Color(1f, .985f, .94f);
        readonly Color teal = new Color(.16f, .56f, .48f);
        readonly Color coral = new Color(.9f, .34f, .23f);
        readonly Color muted = new Color(.39f, .46f, .45f);
        readonly Color pale = new Color(.88f, .88f, .79f);
        Canvas canvas;
        Font font;
        RectTransform hud, modal, footer, cityBadge, serviceBadge;
        Text rank, status, cash, hints, notice, stock, orderSummary, cityRank;
        ScrollRect scroll;
        string category = "All", signature = "";
        readonly List<Action> tickLabels = new List<Action>();
        readonly string[] categories = { "All", "Kitchen", "Seating", "Finishes", "Lighting", "Decor", "Exterior" };

        public void Rebuild() {
            if (!Owner || !Owner.Game || !Owner.Game.Player || !Owner.Game.Player.View) return;
            if (canvas) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var obj = new GameObject("Little Flame / restaurant interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(transform, false);
            canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Owner.Game.Player.View;
            canvas.planeDistance = Mathf.Max(.4f, canvas.worldCamera.nearClipPlane + .1f);
            canvas.sortingOrder = 50;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            if (!EventSystem.current) {
                var events = new GameObject("Restaurant input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            hud = Block(canvas.transform, "Restaurant status", 24, 22, 1392, 78, ink);
            Block(hud, "Accent", 0, 0, 7, 78, teal);
            Label(hud, "Little Flame", 24, 10, 242, 32, 27, paper, true);
            rank = Label(hud, "One star / a new beginning", 24, 46, 310, 22, 14, paper);
            status = Label(hud, "", 364, 14, 460, 52, 17, paper);
            stock = Label(hud, "", 850, 12, 294, 55, 15, paper);
            cash = Label(hud, "", 1170, 10, 194, 52, 32, paper, true, TextAnchor.MiddleRight);
            footer = Block(canvas.transform, "Controls", 24, 848, 1392, 35, ink);
            hints = Label(footer, "", 16, 5, 1360, 27, 14, paper);
            notice = Label(canvas.transform, "", 350, 756, 740, 74, 20, paper, true, TextAnchor.MiddleCenter);
            serviceBadge = Block(canvas.transform, "Live service", 1046, 119, 368, 104, new Color(ink.r, ink.g, ink.b, .9f));
            orderSummary = Label(serviceBadge, "", 15, 12, 338, 82, 16, paper, true, TextAnchor.UpperRight);
            cityBadge = Block(canvas.transform, "Restaurant reputation in the city", 363, 26, 565, 79, ink);
            cityRank = Label(cityBadge, "", 20, 11, 525, 62, 18, paper, true);
            signature = ""; modal = null; tickLabels.Clear();
            Refresh();
        }

        public void Refresh() {
            if (!Owner || Owner.Data == null || !Owner.Game) return;
            if (!canvas) { Rebuild(); return; }
            var s = Owner.Data;
            bool visible = s.Owned && Owner.Game.Started && !Owner.Game.Paused;
            bool full = Owner.Inside || Owner.PanelOpen || Owner.PlacementActive;
            hud.gameObject.SetActive(visible && full);
            cityBadge.gameObject.SetActive(visible && !full);
            cityRank.text = "Little Flame  /  " + (s.Stars >= 2 ? "Two stars" : "One star") + "\n" + s.Satisfaction.ToString("0") + "% satisfaction   /   " + s.Served + " happy memories served";
            footer.gameObject.SetActive(visible && full);
            notice.gameObject.SetActive(visible && full && !Owner.PanelOpen);
            serviceBadge.gameObject.SetActive(visible && Owner.Inside && !Owner.PanelOpen && !Owner.PlacementActive);
            rank.text = s.Stars >= 2 ? "Two stars / neighborhood favorite" : "One star / a new beginning";
            status.text = "Satisfaction " + s.Satisfaction.ToString("0") + "%    |    " + (s.Open ? "Open for service" : "Closed for arrivals") + "\n" + s.Served + " served    |    Ambience " + s.Ambience + "    |    Cleanliness " + s.Cleanliness.ToString("0") + "%";
            stock.text = "Pantry: " + s.Produce + " produce / " + s.Protein + " protein\n" + s.Seats + " seats    |    " + s.CookSlots + " cooking stations";
            cash.text = "$" + Owner.Game.State.Cash;
            hints.text = Owner.PlacementActive ? "Place with left click    /    R Rotate " + (Owner.PreviewRotation * 90) + " degrees    /    Esc Cancel    /    B Return to catalog" : "B  Catalog     /     Tab  Manage restaurant     /     E  Interact     /     Esc  Pause";
            notice.text = Owner.PlacementActive ? Owner.Hint : !string.IsNullOrEmpty(Owner.FocusPrompt) ? Owner.FocusPrompt : Owner.Game.Notice;
            var summary = new StringBuilder(s.Open ? "Service is open\n" : "Doors closed to new guests\n");
            int waiting = 0, cooking = 0, ready = 0;
            foreach (var order in s.Orders) { if (order.Stage == RestaurantOrderStage.Waiting) waiting++; if (order.Stage == RestaurantOrderStage.Cooking) cooking++; if (order.Stage == RestaurantOrderStage.Ready) ready++; }
            summary.Append(waiting).Append(" waiting  /  ").Append(cooking).Append(" cooking  /  ").Append(ready).Append(" ready\nTab to manage orders");
            orderSummary.text = summary.ToString();
            if (!Owner.PanelOpen || !visible) {
                if (modal) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; }
                tickLabels.Clear(); signature = ""; return;
            }
            string next = Signature();
            if (next != signature || !modal) { BuildPanel(); signature = next; }
            foreach (var update in tickLabels) update();
        }

        string Signature() {
            var s = Owner.Data;
            var key = new StringBuilder(Owner.Panel).Append('|').Append(category).Append('|').Append(Owner.Game.State.Cash).Append('|').Append(s.Stars).Append('|').Append(s.Open).Append('|').Append(s.Produce).Append('|').Append(s.Protein).Append('|').Append(s.Layout.Count).Append('|').Append(s.Reviews.Count).Append('|').Append(s.Served).Append('|').Append(Owner.SelectedInstanceId).Append('|').Append(Owner.Game.State.RecipeUnlocked).Append('|').Append(s.CanCustomize).Append(Owner.AtSupplier);
            foreach (var dish in s.ActiveMenu) key.Append(dish);
            key.Append('|').Append(Owner.Game.State.Stock);
            foreach (var order in s.Orders) key.Append('|').Append(order.Id).Append(':').Append(order.Stage);
            // Worker assignments and purchases are infrequent, but must immediately update controls.
            foreach (var worker in s.Workers) key.Append(worker.Id).Append(worker.Job);
            return key.ToString();
        }

        void BuildPanel() {
            float previousScroll = scroll ? scroll.verticalNormalizedPosition : 1;
            if (modal) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); }
            tickLabels.Clear(); scroll = null;
            modal = Block(canvas.transform, "Restaurant management", 0, 105, 1440, 735, new Color(ink.r, ink.g, ink.b, .60f));
            var sheet = Block(modal, "Order pad", 110, 12, 1220, 710, paper);
            Block(sheet, "Top accent", 0, 0, 1220, 7, teal);
            Label(sheet, Owner.Panel == "Supplies" ? "Milo's market pantry." : Owner.Panel == "Catalog" ? "Make this place yours." : Owner.Panel == "Service" ? "On the pass." : Owner.Panel == "Menu" ? "What's cooking?" : Owner.Panel == "Staff" ? "A very unusual crew." : Owner.Panel == "Furniture" ? "Give it a new home." : "Word on the street.", 30, 22, 785, 45, 31, ink, true);
            Button(sheet, "Close  x", 1060, 24, 130, 36, () => Owner.ClosePanel(), ink, paper);
            Label(sheet, "Time pauses while management is open.", 823, 62, 365, 19, 12, muted, false, TextAnchor.MiddleRight);
            string[] panels = { "Catalog", "Service", "Menu", "Staff", "Reviews" };
            for (int i = 0; i < panels.Length; i++) {
                string tab = panels[i]; bool active = Owner.Panel == tab;
                Button(sheet, tab == "Catalog" ? "Shop" : tab, 30 + i * 145, 78, 136, 35, () => { Owner.ShowPanel(tab); signature = ""; Refresh(); }, active ? teal : pale, active ? white : ink);
            }
            Label(sheet, "Your budget  $" + Owner.Game.State.Cash, 831, 82, 355, 28, 17, ink, true, TextAnchor.MiddleRight);
            Block(sheet, "Rule", 30, 124, 1160, 2, pale);
            if (Owner.Panel == "Supplies") BuildSupplies(sheet);
            else if (Owner.Panel == "Catalog") BuildCatalog(sheet);
            else if (Owner.Panel == "Menu") BuildMenu(sheet);
            else if (Owner.Panel == "Staff") BuildStaff(sheet);
            else if (Owner.Panel == "Reviews") BuildReviews(sheet);
            else if (Owner.Panel == "Furniture") BuildFurniture(sheet);
            else BuildService(sheet);
            Text panelNotice = Label(sheet, "", 30, 667, 1156, 29, 14, muted);
            tickLabels.Add(() => { if (panelNotice) panelNotice.text = !string.IsNullOrEmpty(Owner.Game.Notice) ? Owner.Game.Notice : Owner.Panel == "Catalog" ? (Owner.Data.CanCustomize ? "Purchases and layouts save automatically. Choose an item to see it in your restaurant." : "Close service and let the last guest leave before renovating.") : "B Catalog / Tab Manage / Esc Close    |    Your progress saves automatically."; });
            if (scroll) scroll.verticalNormalizedPosition = previousScroll;
        }

        void BuildCatalog(RectTransform sheet) {
            for (int i = 0; i < categories.Length; i++) {
                string cat = categories[i]; bool active = cat == category;
                Button(sheet, cat, 30 + i * 167, 139, 158, 32, () => { category = cat; signature = ""; if (scroll) scroll.verticalNormalizedPosition = 1; Refresh(); }, active ? ink : pale, active ? paper : ink);
            }
            int count = 0;
            foreach (var item in RestaurantCatalog.Items) if (category == "All" || category == item.Category.ToString()) count++;
            var content = Scroller(sheet, 30, 186, 1160, 466, Mathf.CeilToInt(count / 3f) * 232);
            int index = 0;
            foreach (var item in RestaurantCatalog.Items) {
                if (category != "All" && item.Category.ToString() != category) continue;
                var entry = item; int col = index % 3, row = index / 3; index++;
                var card = Block(content, entry.Name, col * 386, row * 232, 372, 219, white);
                bool locked = entry.RequiredStars > Owner.Data.Stars;
                Block(card, "Swatch", 0, 0, 372, 5, locked ? muted : teal);
                var thumbnail = Box(card, "Product picture", 12, 17, 105, 106);
                var raw = thumbnail.gameObject.AddComponent<RawImage>();
                raw.texture = Owner.GetCatalogIcon(entry.Id); raw.color = locked ? new Color(.76f,.76f,.76f,1) : Color.white; raw.raycastTarget = false;
                Label(card, entry.Name, 128, 17, 227, 46, 21, ink, true);
                Label(card, "$" + entry.Price, 128, 67, 227, 30, 23, locked ? muted : teal, true);
                Label(card, entry.Category + (entry.Seats > 0 ? "  /  " + entry.Seats + " seats" : "") + (entry.Ambience > 0 ? "  /  +" + entry.Ambience + " ambience" : ""), 128, 103, 230, 28, 12, muted);
                Label(card, entry.Description, 14, 134, 344, 43, 14, ink);
                string action = locked ? "Unlock at " + entry.RequiredStars + " stars" : entry.Category == CatalogCategory.Finishes || entry.Category == CatalogCategory.Exterior ? "Install for $" + entry.Price : "Preview & place";
                bool can = !locked && Owner.Data.CanCustomize && Owner.Game.State.Cash >= entry.Price;
                if (!locked && Owner.Game.State.Cash < entry.Price) action = "Save $" + (entry.Price - Owner.Game.State.Cash) + " more";
                Button(card, action, 14, 178, 344, 30, () => Owner.SelectCatalogItem(entry.Id), can ? teal : pale, can ? white : muted, can);
            }
        }

        void BuildMenu(RectTransform sheet) {
            Label(sheet, "Offer fewer dishes for an easier shift, or a wider menu for more favorites and better takings.", 30, 141, 1148, 29, 16, ink);
            int count = RestaurantCatalog.Dishes.Length;
            var content = Scroller(sheet, 30, 182, 1160, 390, Mathf.CeilToInt(count / 2f) * 155);
            int index = 0;
            foreach (var dish in RestaurantCatalog.Dishes) {
                var entry = dish; int col = index % 2, row = index / 2; index++;
                var card = Block(content, entry.Name, col * 580, row * 155, 562, 142, white);
                bool midnight = entry.RequiresMidnight && !Owner.Game.State.RecipeUnlocked;
                bool locked = midnight || entry.RequiredStars > Owner.Data.Stars || (!string.IsNullOrEmpty(entry.Equipment) && !Owner.Data.HasEquipment(entry.Equipment));
                bool active = Owner.Data.ActiveMenu.Contains(entry.Id);
                Label(card, entry.Name, 18, 13, 364, 30, 23, ink, true);
                Label(card, "$" + entry.Price, 441, 13, 101, 30, 23, teal, true, TextAnchor.MiddleRight);
                Label(card, entry.ProduceCost + " produce  +  " + entry.ProteinCost + " protein   /   " + entry.CookSeconds.ToString("0") + "s to cook", 18, 50, 521, 25, 15, muted);
                string help = midnight ? "Find the midnight recipe in the rival alley after dark." : entry.RequiredStars > Owner.Data.Stars ? "Reach " + entry.RequiredStars + " stars to unlock this recipe." : locked ? "Needs " + EquipmentName(entry.Equipment) + " from the shop." : active ? "Guests can order this dish." : "Add this dish to offer it to arriving guests.";
                Label(card, help, 18, 85, 335, 43, 14, ink);
                Button(card, locked ? "Locked" : active ? "On menu" : "Add to menu", 372, 87, 171, 35, () => Owner.ToggleDish(entry.Id), active ? teal : pale, active ? white : ink, !locked);
            }
            var pantry = Block(sheet, "Pantry", 30, 589, 1160, 61, ink);
            Label(pantry, "Pantry: " + Owner.Data.Produce + " produce / " + Owner.Data.Protein + " protein", 16, 16, 470, 33, 18, paper, true);
            if (Owner.AtSupplier) {
                Button(pantry, "6 produce / $6", 585, 13, 253, 35, () => Owner.Restock(false), teal, white);
                Button(pantry, "6 protein / $10", 856, 13, 287, 35, () => Owner.Restock(true), teal, white);
            } else Label(pantry, "Restock at Milo's green market across the street.", 524, 16, 620, 35, 17, paper, true);
        }

        void BuildSupplies(RectTransform sheet) {
            Label(sheet, "Bring something good back to your kitchen.", 35, 146, 1120, 45, 25, ink, true);
            Label(sheet, "Buy six portions at a time. A refrigerator doubles pantry storage from 24 to 48 of each ingredient.", 35, 200, 1120, 40, 18, muted);
            var produce = Block(sheet, "Market produce", 35, 272, 553, 237, white);
            Label(produce, "Fresh produce", 24, 23, 495, 42, 31, teal, true);
            Label(produce, "Salads, soups, burger toppings and moonberry tarts.\nIn your pantry: " + Owner.Data.Produce + " / " + Owner.Data.StockLimit, 24, 80, 495, 69, 19, ink);
            Button(produce, "6 portions / $6", 24, 171, 503, 44, () => Owner.Restock(false), teal, white, Owner.AtSupplier);
            var protein = Block(sheet, "Market protein", 608, 272, 577, 237, white);
            Label(protein, "Kitchen protein", 24, 23, 524, 42, 31, coral, true);
            Label(protein, "Hearty burgers, comforting soup and rare midnight buns.\nIn your pantry: " + Owner.Data.Protein + " / " + Owner.Data.StockLimit, 24, 80, 524, 69, 19, ink);
            Button(protein, "6 portions / $10", 24, 171, 527, 44, () => Owner.Restock(true), teal, white, Owner.AtSupplier);
            var stand = Block(sheet, "Street stand supplies", 35, 531, 1150, 109, ink);
            Label(stand, "Keep the street stand cooking", 21, 15, 765, 32, 23, paper, true);
            Label(stand, "Stand ingredients: " + Owner.Game.State.Stock + " / 99   |   Separate from your restaurant pantry.", 21, 58, 767, 34, 16, paper);
            Button(stand, "3 stand ingredients / $6", 816, 34, 313, 43, () => Owner.RestockStand(), teal, white, Owner.AtSupplier);
        }

        void BuildService(RectTransform sheet) {
            Label(sheet, "Cook a ticket, then close this menu. E at a kitchen station collects a ready dish;\nE at the matching guest serves it. Your crew can automate jobs as you grow.", 30, 140, 798, 47, 16, ink);
            Button(sheet, Owner.Data.Open ? "Close to new arrivals" : "Open for service", 848, 139, 339, 37, () => Owner.ToggleService(), Owner.Data.Open ? coral : teal, white);
            bool canCookMenu = false;
            foreach (var dishId in Owner.Data.ActiveMenu) {
                var dish = RestaurantCatalog.Dish(dishId);
                if (dish != null && Owner.Data.IsDishAvailable(Owner.Game.State, dishId) && Owner.Data.Produce >= dish.ProduceCost && Owner.Data.Protein >= dish.ProteinCost) canCookMenu = true;
            }
            string pantryHint = Owner.Data.Open ? "Service resumes when you close this panel. Watch the pantry before the next rush." : "Choose a menu, stock up, and open your doors. Closing this panel resumes the city.";
            if (!canCookMenu) pantryHint = Owner.Data.Produce == 0 && Owner.Game.State.Cash < 6
                ? "Out of cash and produce? Finish ready dishes, then ask Milo for produce. His emergency salad supplies can restart service."
                : Owner.Data.Protein == 0 && Owner.Data.Produce >= 2 && Owner.Data.HasEquipment("prep_bench")
                ? "No protein? Add Garden galaxy in Menu. It only needs 2 produce. Milo sells more supplies across the street."
                : "Pantry exhausted for this menu. Close new arrivals, finish ready dishes, then visit Milo's green market to restock.";
            var pantryNote = Block(sheet, "Service guidance", 30, 195, 1160, 43, canCookMenu ? pale : new Color(1f, .84f, .64f));
            Label(pantryNote, pantryHint, 13, 7, 1134, 32, 14, ink);
            var content = Scroller(sheet, 30, 250, 1160, 315, Mathf.Max(307, Mathf.CeilToInt(Owner.Data.Orders.Count / 3f) * 178));
            if (Owner.Data.Orders.Count == 0) {
                Label(content, Owner.Data.Open ? "The first guests are on their way." : "A quiet kitchen. A fresh start.", 45, 73, 1060, 52, 30, ink, true, TextAnchor.MiddleCenter);
                Label(content, "Choose your menu, stock ingredients and assign your crew.\nCooking stations let you prepare several orders at once.", 150, 148, 840, 81, 19, muted, false, TextAnchor.MiddleCenter);
            }
            int index = 0;
            foreach (var order in Owner.Data.Orders) {
                var entry = order; int col = index % 3, row = index / 3; index++;
                var card = Block(content, "Order " + entry.Id, col * 386, row * 178, 372, 166, white);
                Color stageColor = entry.Stage == RestaurantOrderStage.Ready ? coral : entry.Stage == RestaurantOrderStage.Cooking ? teal : pale;
                Block(card, "Ticket stripe", 0, 0, 7, 166, stageColor);
                Label(card, "#" + entry.Id + "   " + CustomerName(entry.CustomerType), 20, 12, 335, 25, 16, muted, true);
                Label(card, DishName(entry.DishId), 20, 42, 335, 31, 22, ink, true);
                Text timer = Label(card, "", 20, 81, 334, 25, 15, ink);
                tickLabels.Add(() => { if (timer) timer.text = entry.Stage == RestaurantOrderStage.Cooking ? "Cooking  " + entry.CookProgress.ToString("0.0") + "s" : entry.Stage == RestaurantOrderStage.Waiting ? "Patience: " + Mathf.Max(0, RestaurantCatalog.Customers[entry.CustomerType].Patience - entry.Wait).ToString("0") + "s" : entry.Stage == RestaurantOrderStage.Ready ? "Ready - collect at a kitchen station" : entry.Stage == RestaurantOrderStage.Eating ? "Enjoying the meal" : "Heading home"; });
                bool canAct = entry.Stage == RestaurantOrderStage.Waiting;
                Button(card, entry.Stage == RestaurantOrderStage.Waiting ? "Cook this order" : entry.Stage == RestaurantOrderStage.Ready ? "Collect at kitchen" : entry.Stage == RestaurantOrderStage.Cooking ? "On the stove" : "Bon appetit!", 20, 117, 332, 34, () => Owner.TryCook(entry.Id), canAct ? teal : pale, canAct ? white : muted, canAct);
            }
            var bar = Block(sheet, "Service tools", 30, 582, 1160, 68, ink);
            Text cleaning = Label(bar, "", 16, 13, 359, 48, 16, paper);
            tickLabels.Add(() => { if (cleaning) cleaning.text = "Cleanliness " + Owner.Data.Cleanliness.ToString("0") + "%\n" + Owner.Data.CookSlots + " cooking stations / " + Owner.Data.Seats + " seats"; });
            Button(bar, "Clean restaurant", 378, 15, 244, 38, () => Owner.Clean(), teal, white);
            Button(bar, "Stock pantry", 640, 15, 235, 38, () => Owner.ShowPanel("Menu"), pale, ink);
            Button(bar, "Assign crew", 893, 15, 251, 38, () => Owner.ShowPanel("Staff"), pale, ink);
        }

        void BuildFurniture(RectTransform sheet) {
            PlacedItem selected = null;
            foreach (var item in Owner.Data.Layout) if (item.InstanceId == Owner.SelectedInstanceId) selected = item;
            if (selected == null) { Label(sheet, "Look at a furnishing and press E to move or sell it.", 70, 270, 1080, 75, 25, ink, true, TextAnchor.MiddleCenter); return; }
            var entry = RestaurantCatalog.Find(selected.CatalogId); int id = selected.InstanceId;
            var picture = Box(sheet, "Selected furnishing", 55, 168, 380, 390).gameObject.AddComponent<RawImage>(); picture.texture = Owner.GetCatalogIcon(selected.CatalogId); picture.raycastTarget = false;
            Label(sheet, entry.Name, 491, 183, 650, 63, 35, ink, true);
            Label(sheet, entry.Description, 493, 263, 640, 81, 20, muted);
            Label(sheet, "Move it to a new position and rotate with R.\nSelling returns part of the purchase price.", 493, 358, 640, 84, 18, ink);
            Button(sheet, "Move furnishing", 493, 476, 309, 52, () => Owner.MoveItem(id), teal, white, Owner.Data.CanCustomize);
            Button(sheet, "Sell for $" + (selected.Paid / 2), 820, 476, 309, 52, () => Owner.SellItem(id), coral, white, Owner.Data.CanCustomize);
        }

        void BuildStaff(RectTransform sheet) {
            Label(sheet, "Hire a familiar face, choose their job, and watch them work alongside you during service.", 30, 140, 1144, 35, 16, ink);
            var content = Scroller(sheet, 30, 189, 1160, 462, 460);
            int index = 0;
            foreach (var worker in RestaurantCatalog.Staff) {
                var entry = worker;
                var card = Block(content, entry.Name, (index % 2) * 580, (index / 2) * 320, 562, 303, white); index++;
                Label(card, entry.Name, 25, 24, 495, 45, 29, ink, true);
                Label(card, entry.Description, 25, 83, 507, 92, 18, muted);
                Label(card, "Specialty: " + entry.Role + "    /    Hire for $" + entry.Cost, 25, 178, 507, 28, 17, ink, true);
                BuildWorkerActions(card, entry.Id, entry.Cost);
            }
            Label(content, "Crew work while you are out on the restaurant floor. Assign a cook for parallel orders,\na server for the pass, or a cleaner to keep demanding guests happy.", 22, 347, 1110, 78, 18, muted);
        }

        void BuildWorkerActions(RectTransform card, string workerId, int cost) {
            WorkerState hired = null;
            foreach (var worker in Owner.Data.Workers) if (WorkerId(worker) == workerId) { hired = worker; break; }
            if (hired == null) { Button(card, "Hire for $" + cost, 25, 233, 508, 43, () => Owner.Hire(workerId), teal, white, Owner.Game.State.Cash >= cost); return; }
            var current = WorkerJob(hired);
            Text workStatus = Label(card, "", 25, 207, 508, 24, 14, muted);
            tickLabels.Add(() => { if (workStatus) workStatus.text = "Hired / " + hired.TasksCompleted + " tasks completed"; });
            StaffJob[] jobs = { StaffJob.Off, StaffJob.Cook, StaffJob.Serve, StaffJob.Clean };
            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i]; bool active = current == job;
                Button(card, job == StaffJob.Off ? "Rest" : job.ToString(), 25 + i * 129, 233, 121, 43, () => Owner.Assign(workerId, job), active ? teal : pale, active ? white : ink);
            }
        }

        void BuildReviews(RectTransform sheet) {
            var s = Owner.Data;
            var banner = Block(sheet, "Next star", 30, 141, 1160, 133, ink);
            Label(banner, s.Stars >= 2 ? "Two stars. The neighborhood noticed." : "The road to two stars", 20, 15, 1096, 36, 27, paper, true);
            Label(banner, "Serve 20 guests    " + Mathf.Min(s.Served, 20) + "/20          Satisfaction    " + s.Satisfaction.ToString("0") + "/75          Ambience    " + s.Ambience + "/12", 20, 62, 1097, 28, 19, paper);
            Label(banner, "Your next star unlocks premium furnishings and a new dish. Food, speed, cleanliness and atmosphere all matter.", 20, 99, 1117, 25, 14, paper);
            int guestStart = Mathf.Max(244, s.Reviews.Count * 105 + 16);
            var content = Scroller(sheet, 30, 292, 1160, 357, guestStart + 354);
            if (s.Reviews.Count == 0) {
                Label(content, "Your story starts with the first plate.", 40, 46, 1082, 55, 27, ink, true, TextAnchor.MiddleCenter);
                Label(content, "Guests leave feedback after their visit. Favorite dishes, short waits and a welcoming\nroom create good reviews; cold food, dirt and long queues lose you regulars.", 80, 123, 995, 85, 19, muted, false, TextAnchor.MiddleCenter);
            }
            int row = 0;
            for (int i = s.Reviews.Count - 1; i >= 0; i--) {
                var review = s.Reviews[i];
                var card = Block(content, "Guest review", 0, row++ * 105, 1141, 94, white);
                Label(card, ReviewTitle(review), 18, 12, 1104, 29, 20, ink, true);
                Label(card, ReviewBody(review), 18, 46, 1104, 40, 16, muted);
            }
            Label(content, "Know your regulars", 0, guestStart, 1134, 34, 24, ink, true);
            for (int i = 0; i < RestaurantCatalog.Customers.Length; i++) {
                var guest = RestaurantCatalog.Customers[i];
                var card = Block(content, guest.Name, (i % 4) * 286, guestStart + 48 + (i / 4) * 147, 273, 135, white);
                Label(card, guest.Name, 12, 10, 249, 34, 17, ink, true);
                Label(card, guest.Description, 12, 48, 249, 76, 14, muted);
            }
        }

        string EquipmentName(string id) { var item = RestaurantCatalog.Find(id); return item == null ? id : item.Name; }
        string DishName(string id) { foreach (var dish in RestaurantCatalog.Dishes) if (dish.Id == id) return dish.Name; return id; }
        string CustomerName(int type) { return type >= 0 && type < RestaurantCatalog.Customers.Length ? RestaurantCatalog.Customers[type].Name : "Guest"; }
        // These accessors stay isolated from presentation so save-model additions do not alter the UI layout.
        string WorkerId(WorkerState worker) { return worker.Id; }
        StaffJob WorkerJob(WorkerState worker) { return worker.Job; }
        string ReviewTitle(RestaurantReview review) { return review.Customer + "   /   " + review.Score.ToString("0") + "% satisfied"; }
        string ReviewBody(RestaurantReview review) { return review.Comment; }

        RectTransform Scroller(Transform parent, float x, float y, float width, float height, float contentHeight) {
            var root = Box(parent, "Scroll area", x, y, width, height);
            scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var viewport = Box(root, "Viewport", 0, 0, width - 15, height);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewImage = viewport.gameObject.AddComponent<Image>(); viewImage.color = new Color(1,1,1,.001f);
            var content = Box(viewport, "Contents", 0, 0, width - 15, Mathf.Max(height, contentHeight));
            scroll.viewport = viewport; scroll.content = content;
            var track = Block(root, "Scroll track", width - 9, 0, 7, height, pale);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var handle = Block(track, "Scroll handle", 0, 0, 7, 40, teal);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            scrollbar.handleRect = handle; scrollbar.targetGraphic = handle.GetComponent<Image>(); scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }
        RectTransform Box(Transform parent, string name, float x, float y, float width, float height) {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        RectTransform Block(Transform parent, string name, float x, float y, float width, float height, Color color) {
            var rect = Box(parent, name, x, y, width, height); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        Text Label(Transform parent, string text, float x, float y, float width, float height, int size, Color color, bool bold = false, TextAnchor align = TextAnchor.UpperLeft) {
            var rect = Box(parent, "Text", x, y, width, height); var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = align; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false; return label;
        }
        void Button(Transform parent, string label, float x, float y, float width, float height, Action clicked, Color background, Color foreground, bool enabled = true) {
            var rect = Block(parent, label, x, y, width, height, background); var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f,1.12f,1.12f); colors.pressedColor = new Color(.78f,.85f,.80f); colors.disabledColor = new Color(.85f,.85f,.85f); button.colors = colors;
            button.onClick.AddListener(() => { clicked(); Refresh(); });
            Label(rect, label, 7, 0, width - 14, height, 16, foreground, true, TextAnchor.MiddleCenter);
        }
    }
}
