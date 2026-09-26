using System;
using System.Collections.Generic;
using System.Linq;
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
            canvas.enabled = Owner.PanelOpen || Owner.PlacementActive;
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
            // The crew "phone" (Staff tab) works before you own the restaurant, as soon as the stand is up.
            bool panelVisible = visible || ((Owner.Panel == "Map" || (Owner.Panel == "Staff" && Owner.Game.State.StandBuilt)) && Owner.Game.Started && !Owner.Game.Paused);
            if (!Owner.PanelOpen || !panelVisible) {
                if (modal) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; }
                tickLabels.Clear(); signature = ""; return;
            }
            string next = Signature();
            if (next != signature || !modal) { BuildPanel(); signature = next; }
            foreach (var update in tickLabels) update();
        }

        string Signature() {
            var s = Owner.Data;
            var key = new StringBuilder(Owner.Panel).Append('|').Append(category).Append('|').Append(Owner.Game.State.Cash).Append('|').Append(s.Stars).Append('|').Append(s.Open).Append('|').Append(s.Produce).Append('|').Append(s.Protein).Append('|').Append(s.Layout.Count).Append('|').Append(s.Reviews.Count).Append('|').Append(s.Served).Append('|').Append(Owner.SelectedInstanceId).Append('|').Append(Owner.Game.State.RecipeUnlocked).Append('|').Append(Owner.Game.State.Xp).Append('|').Append(s.CanCustomize).Append(Owner.AtSupplier);
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
            Label(sheet, Owner.Panel == "Listing" ? "For sale later." : Owner.Panel == "Supplies" ? "Milo's market pantry." : Owner.Panel == "Catalog" ? "Make this place yours." : Owner.Panel == "Service" ? "On the pass." : Owner.Panel == "Menu" ? "What's cooking?" : Owner.Panel == "Cookbook" ? "How every dish is built." : Owner.Panel == "Staff" ? "A very unusual crew." : Owner.Panel == "Map" ? "Saffron Bay." : Owner.Panel == "Furniture" ? "Give it a new home." : "Word on the street.", 30, 22, 785, 45, 31, ink, true);
            Button(sheet, "Close  x", 1060, 24, 130, 36, () => Owner.ClosePanel(), ink, paper);
            Label(sheet, "Time pauses while management is open.", 823, 62, 365, 19, 12, muted, false, TextAnchor.MiddleRight);
            string[] panels = { "Catalog", "Service", "Menu", "Cookbook", "Staff", "Map", "Reviews", "Furniture" };
            for (int i = 0; i < panels.Length; i++) {
                string tab = panels[i]; bool active = Owner.Panel == tab;
                Button(sheet, tab == "Catalog" ? "Shop" : tab == "Furniture" ? "Arrange" : tab, 30 + i * 114, 78, 108, 35, () => { Owner.ShowPanel(tab); signature = ""; Refresh(); }, active ? teal : pale, active ? white : ink);
            }
            Label(sheet, "Your budget  $" + Owner.Game.State.Cash, 950, 82, 236, 28, 17, ink, true, TextAnchor.MiddleRight);
            Block(sheet, "Rule", 30, 124, 1160, 2, pale);
            if (Owner.Panel == "Listing") BuildListing(sheet);
            else if (Owner.Panel == "Supplies") BuildSupplies(sheet);
            else if (Owner.Panel == "Catalog") BuildCatalog(sheet);
            else if (Owner.Panel == "Menu") BuildMenu(sheet);
            else if (Owner.Panel == "Cookbook") BuildCookbook(sheet);
            else if (Owner.Panel == "Staff") BuildStaff(sheet);
            else if (Owner.Panel == "Map") BuildMap(sheet);
            else if (Owner.Panel == "Reviews") BuildReviews(sheet);
            else if (Owner.Panel == "Furniture") BuildFurniture(sheet);
            else BuildService(sheet);
            Text panelNotice = Label(sheet, "", 30, 667, 1156, 29, 14, muted);
            tickLabels.Add(() => { if (panelNotice) panelNotice.text = !string.IsNullOrEmpty(Owner.Game.Notice) ? Owner.Game.Notice : Owner.Panel == "Catalog" ? (Owner.Data.CanCustomize ? "Purchases and layouts save automatically. Choose an item to see it in your restaurant." : "Close service and let the last guest leave before renovating.") : "B Catalog / Tab Manage / Esc Close    |    Your progress saves automatically."; });
            if (scroll) scroll.verticalNormalizedPosition = previousScroll;
            if(EventSystem.current && !EventSystem.current.currentSelectedGameObject){var first=sheet.GetComponentInChildren<Button>();if(first)EventSystem.current.SetSelectedGameObject(first.gameObject);}
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
                bool tierLocked = entry.Tier > Owner.Game.State.RankEarned, locked = tierLocked || entry.RequiredStars > Owner.Data.Stars;
                Block(card, "Swatch", 0, 0, 372, 5, locked ? muted : teal);
                var thumbnail = Box(card, "Product picture", 12, 17, 105, 106);
                var raw = thumbnail.gameObject.AddComponent<RawImage>();
                raw.texture = Owner.GetCatalogIcon(entry.Id); raw.color = locked ? new Color(.76f,.76f,.76f,1) : Color.white; raw.raycastTarget = false;
                Label(card, entry.Name, 128, 17, 227, 46, 21, ink, true);
                Label(card, "$" + entry.Price, 128, 67, 227, 30, 23, locked ? muted : teal, true);
                Label(card, entry.Category + (entry.Seats > 0 ? "  /  " + entry.Seats + " seats" : "") + (entry.Ambience > 0 ? "  /  +" + entry.Ambience + " ambience" : ""), 128, 103, 230, 28, 12, muted);
                Label(card, entry.Description, 14, 134, 344, 43, 14, ink);
                string action = tierLocked ? "Unlocks at " + Reputation.Titles[entry.Tier] : locked ? "Unlock at " + entry.RequiredStars + " stars" : entry.Category == CatalogCategory.Finishes || entry.Category == CatalogCategory.Exterior ? "Install for $" + entry.Price : "Preview & place";
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
                if (dish.Id != "burger" && dish.Id != "salad" && dish.Id != "midnight") continue; var entry = dish; int col = index % 2, row = index / 2; index++;
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

        void BuildCookbook(RectTransform sheet) {
            Label(sheet, "Every playable recipe: what it needs and how to build it. Order does not matter.", 30, 141, 1148, 29, 16, ink);
            var content = Scroller(sheet, 30, 182, 1160, 466, RecipeBook.Recipes.Length * 165);
            for (int i = 0; i < RecipeBook.Recipes.Length; i++) {
                var recipe = RecipeBook.Recipes[i];
                var dish = RestaurantCatalog.Dish(recipe.DishId);
                bool locked = dish.RequiresMidnight && !Owner.Game.State.RecipeUnlocked;
                var card = Block(content, dish.Name, 0, i * 165, 1141, 152, white);
                Label(card, dish.Name + (locked ? "  (locked)" : ""), 18, 12, 700, 30, 22, locked ? muted : ink, true);
                Label(card, "Needs: " + string.Join(" + ", recipe.Components.Select(ComponentName)), 18, 44, 1100, 24, 15, teal, true);
                Label(card, locked ? "Find this recipe on a night city outing, then it is usable in service." : string.Join("   >   ", recipe.Steps), 18, 70, 1100, 76, 14, muted);
            }
        }
        static string ComponentName(string id) => id == "bun" ? "bun" : id == "cooked_patty" ? "cooked patty" : id == "chopped_greens" ? "chopped greens" : id == "midnight_sauce" ? "midnight sauce" : id;
        // The second restaurant's listing: what it is, and the checklist that gets you there.
        void BuildListing(RectTransform sheet) {
            var site = RestaurantSites.Get(Owner.ListingSite); var d = Owner.Data; var st = Owner.Game.State;
            Label(sheet, site.Title, 35, 146, 1120, 48, 34, ink, true);
            Label(sheet, site.Pitch, 35, 200, 1120, 50, 19, muted);
            var card = Block(sheet, "Listing checklist", 35, 262, 700, 330, white);
            Label(card, "To buy it, your first restaurant has to run without you:", 24, 20, 650, 30, 19, ink, true);
            var checks = new (bool done, string text)[] {
                (d.Owned, "Own The Odd Table"),
                (d.Stars >= RestaurantSites.SecondSiteStars, "The Odd Table at " + RestaurantSites.SecondSiteStars + " stars (now " + d.Stars + ")"),
                (d.Workers.Count >= RestaurantSites.SecondSiteCrew, "A crew of " + RestaurantSites.SecondSiteCrew + " to keep it running while you're away (now " + d.Workers.Count + ")"),
                (st.Cash >= site.Price, "$" + site.Price + " (you have $" + st.Cash + ")"),
            };
            float y = 68; foreach (var c in checks) { Label(card, (c.done ? "DONE    " : "TO DO   ") + c.text, 24, y, 650, 30, 18, c.done ? teal : coral, true); y += 44; }
            Label(card, "Running both restaurants is the fastest way to reach Line Cook.", 24, 262, 650, 40, 16, muted);
            var soon = Block(sheet, "Listing note", 755, 262, 430, 330, ink);
            Label(soon, "Coming soon", 24, 20, 380, 34, 24, paper, true);
            Label(soon, "Buying a second restaurant arrives once workers can keep The Odd Table running while you're across town. Everything on the checklist still counts toward it.", 24, 66, 382, 200, 17, paper);
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
            Label(sheet,"Physical kitchen / Everyone can cook. No worker required.",30,139,1100,40,23,ink,true);
            Button(sheet,Owner.Data.Open?"Close to new guests":"Open for service",848,188,339,40,()=>Owner.ToggleService(),Owner.Data.Open?ink:teal,Owner.Data.Open?paper:white);
            Label(sheet,"BURGER: Patty on the grill (ready at 8s, burns at 24s) > plate from the rack > E bun shelf > E grill to add the patty > serve.\nSALAD: Greens on the prep bench, hold E to chop > onto a plate.  MIDNIGHT: burger + E the sauce shelf while holding the plate.\nDirty plates: clear tables, stack them at the sink, hold E to wash one at a time.",30,190,790,200,16,muted);
            Button(sheet,"Wait until night (+30% sales)",848,242,339,40,()=>{Owner.Game.State.Clock=180;Owner.Feedback("Night service pays 30% more. Watch the rush.");},pale,ink);
            Button(sheet,"Prep research / 3 Flux",848,297,339,40,()=>{Owner.Game.State.Kitchen.SpendFlux(Owner.Game.State,"research",out var m);Owner.Feedback(m);},pale,ink);
            int i=0;foreach(var w in Owner.Data.Workers){string id=w.Id;Button(sheet,id+" energy boost / 1 Flux",848,351+i++*48,339,40,()=>{Owner.Game.State.Kitchen.SpendFlux(Owner.Game.State,id,out var m);Owner.Feedback(m);},pale,ink);}
            var report=Owner.Game.State.Kitchen.LastReport;
            if(report!=null)Label(sheet,"LAST SHIFT   Sales $"+report.GrossSales+" - ingredients $"+report.IngredientCosts.ToString("0.0")+" - wages $"+report.Wages+" = net $"+report.Net+"\nServed "+report.Served+" / lost "+report.Lost+" | Satisfaction "+report.Satisfaction.ToString("0")+"% | Stars "+report.StarsBefore+" > "+report.StarsAfter+"\n"+string.Join(" / ",report.Comments)+"\n"+report.StaffSummary,30,506,1157,143,16,ink);
            else Label(sheet,"A shift takes arrivals for two minutes, then lets you finish remaining guests.\nUse the door sign to end arrivals early. Tab opens management between shifts.\nController Start joins player two. Management is a shared screen while closed.",30,523,1157,110,18,muted);
        }
        void BuildFurniture(RectTransform sheet) {
            Label(sheet, "Choose an owned furnishing to move. This list includes stations hidden behind other furniture.", 30, 137, 1150, 28, 16, ink);
            var owned = Owner.Data.Layout.FindAll(p => { var item = RestaurantCatalog.Find(p.CatalogId); return item != null && !item.IsFinish && !item.IsExterior; });
            var content = Scroller(sheet, 30, 175, 365, 465, Mathf.Max(465, owned.Count * 58));
            for (int i = 0; i < owned.Count; i++) {
                var placed = owned[i];
                var definition = RestaurantCatalog.Find(placed.CatalogId);
                Button(content, definition.Name + "  #" + placed.InstanceId + "  (" + placed.X + ", " + placed.Z + ")", 6, i * 58 + 3, 340, 50,
                    () => Owner.SelectFurnitureItem(placed.InstanceId), placed.InstanceId == Owner.SelectedInstanceId ? teal : pale,
                    placed.InstanceId == Owner.SelectedInstanceId ? white : ink);
            }
            PlacedItem selected = null;
            foreach (var item in Owner.Data.Layout) if (item.InstanceId == Owner.SelectedInstanceId) selected = item;
            if (selected == null) { Label(sheet, "Select any owned furnishing from the list. B while aiming at one opens it here directly.", 430, 270, 740, 90, 24, ink, true, TextAnchor.MiddleCenter); return; }
            var entry = RestaurantCatalog.Find(selected.CatalogId); int id = selected.InstanceId;
            var picture = Box(sheet, "Selected furnishing", 430, 175, 275, 235).gameObject.AddComponent<RawImage>(); picture.texture = Owner.GetCatalogIcon(selected.CatalogId); picture.raycastTarget = false;
            Label(sheet, entry.Name, 730, 180, 430, 58, 31, ink, true);
            Label(sheet, entry.Description, 730, 253, 425, 90, 18, muted);
            Label(sheet, "Move: choose a new floor cell, then click. R rotates. Esc cancels.", 430, 429, 735, 46, 18, ink);
            Button(sheet, "Move furnishing", 430, 490, 350, 52, () => Owner.MoveItem(id), teal, white, Owner.Data.CanCustomize);
            Button(sheet, "Sell for $" + (selected.Paid / 2), 805, 490, 350, 52, () => Owner.SellItem(id), coral, white, Owner.Data.CanCustomize);
        }

        void BuildStaff(RectTransform sheet) {
            var gs = Owner.Game.State;
            Label(sheet, "Recruit with Flux (your Flux: " + gs.Flux + "). Anyone can do any job. \"Run stand\" = passive income from your food stand." + (gs.StandWorker != null ? "   Stand: " + gs.StandWorkerStatus + "  Earned $" + gs.StandWorkerEarned : ""), 30, 140, 1144, 35, 15, ink);
            string[] rivals = { "Maestro Vey|Head chef. Gold toque, glowing eyes. Runs a kitchen like an orchestra.", "Nyx|Sommelier with a crystal halo. Guests tip double when she pours.", "K-9|Chrome line cook with four arms and a neon visor. Never tires.", "Aurora|Maitre d'. Her monocle sees every empty seat before you do.", "Seraphine|Winged pastry chef. Desserts so good customers float out.", "Obsidian Titan|Doorman. Nobody makes a scene with him at the door.", "Lumen|Mixologist with a neon crest. Every drink glows." };
            int rows = Mathf.CeilToInt(RestaurantCatalog.Staff.Length / 2f) + Mathf.CeilToInt(rivals.Length / 2f) + 1;
            var content = Scroller(sheet, 30, 189, 1160, 462, rows * 320 + 20);
            int index = 0;
            foreach (var worker in RestaurantCatalog.Staff) {
                var entry = worker;
                var card = Block(content, entry.Name, (index % 2) * 580, (index / 2) * 320, 562, 303, entry.Special ? new Color(1f, .96f, .86f) : white); index++;
                Label(card, entry.Name, 25, 24, 495, 45, 26, ink, true);
                Label(card, entry.Description, 25, 78, 507, 70, 17, muted);
                string price = entry.Special ? "Recruit for " + entry.FluxCost + " Flux" : "Hire for $" + entry.Cost;
                string trust = "";
                Label(card, "Specialty: " + entry.Role + "    /    " + price + trust, 25, 172, 520, 28, 16, ink, true);
                BuildWorkerActions(card, entry);
            }
            // The Gilded Orbit's elite crew: visible, desirable, and not available yet.
            int rivalTop = Mathf.CeilToInt(RestaurantCatalog.Staff.Length / 2f) * 320;
            Label(content, "THE GILDED ORBIT'S CREW  /  Rival exclusives", 22, rivalTop + 10, 1110, 40, 24, ink, true);
            for (int i = 0; i < rivals.Length; i++) {
                var parts = rivals[i].Split('|');
                var card = Block(content, parts[0], (i % 2) * 580, rivalTop + 60 + (i / 2) * 320, 562, 303, ink);
                Label(card, parts[0], 25, 24, 495, 45, 26, paper, true);
                Label(card, parts[1], 25, 78, 507, 70, 17, paper);
                Label(card, "25 Flux    /    Rival exclusive", 25, 172, 520, 28, 16, paper, true);
                Button(card, "Locked: they only work at a four-star restaurant", 25, 233, 508, 43, () => { }, pale, ink, false);
            }
        }

        void BuildWorkerActions(RectTransform card, StaffDefinition entry) {
            string workerId = entry.Id;
            WorkerState hired = null;
            foreach (var worker in Owner.Data.Workers) if (WorkerId(worker) == workerId) { hired = worker; break; }
            if (hired == null) {
                if (entry.Special) {
                    Button(card, "Recruit for " + entry.FluxCost + " Flux", 25, 233, 508, 43, () => Owner.Hire(workerId), teal, white, Owner.Game.State.Flux >= entry.FluxCost);
                } else Button(card, "Hire for $" + entry.Cost, 25, 233, 508, 43, () => Owner.Hire(workerId), teal, white, Owner.Game.State.Cash >= entry.Cost);
                return;
            }
            var current = WorkerJob(hired);
            Text workStatus = Label(card, "", 25, 207, 508, 24, 14, muted);
            tickLabels.Add(() => { if (workStatus) workStatus.text = "Energy " + hired.Energy.ToString("0") + "/100 / " + hired.TasksCompleted + " tasks completed"; });
            StaffJob[] jobs = { StaffJob.Cook, StaffJob.Serve, StaffJob.Clean, StaffJob.Stand, StaffJob.Off };
            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i]; bool active = current == job;
                Button(card, job == StaffJob.Off ? "Rest" : job == StaffJob.Clean ? "Wash" : job == StaffJob.Stand ? "Stand" : job == StaffJob.Any ? "Any job" : job.ToString(), 25 + i * 102, 233, 97, 43, () => Owner.Assign(workerId, job), active ? teal : pale, active ? white : ink);
            }
        }

        // The phone map: districts (locked ones dimmed), key places, where every player is, and the reputation ladder.
        void BuildMap(RectTransform sheet) {
            var st = Owner.Game.State; int xp = st.Xp, rank = st.RankEarned;
            float mx = 30, my = 138, mh = 520, sc = mh / (CityDistricts.MaxZ - CityDistricts.MinZ), mw = (CityDistricts.MaxX - CityDistricts.MinX) * sc;
            Vector2 P(float x, float z) => new Vector2(mx + (x - CityDistricts.MinX) * sc, my + (CityDistricts.MaxZ - z) * sc);
            Block(sheet, "Bay", mx, my, mw, mh, new Color(.17f, .36f, .44f));
            foreach (var d in CityDistricts.All) {
                ColorUtility.TryParseHtmlString("#" + d.Hex, out var c); bool open = rank >= d.Rank;
                // Drawn shapes follow the plan (boundary roads included); lock areas are slightly smaller.
                float x0 = d.X0, x1 = d.X1, z0 = d.Z0, z1 = d.Z1;
                switch (d.Id) {
                    case "docks": z1 = -110; break;
                    case "neon": x0 = 160; x1 = 420; z0 = -110; break;
                    case "greenleaf": z0 = 130; x1 = 420; break;
                    case "gold": x0 = 420; break;
                    case "nebula": z1 = -190; break;
                }
                var a = P(x0, z1);
                var b = Block(sheet, d.Name, a.x, a.y, (x1 - x0) * sc, (z1 - z0) * sc, open ? Color.Lerp(c, white, .12f) : Color.Lerp(c, new Color(.16f, .18f, .21f), .7f));
                Label(b, d.Name.ToUpper(), 6, 4, 220, 20, 13, white, true);
                if (!open) Label(b, "LOCKED  /  " + Reputation.Titles[d.Rank], 6, 21, 230, 18, 11, new Color(1, .88f, .62f));
            }
            var bridgeA = P(434, -126); Block(sheet, "Bridge", bridgeA.x, bridgeA.y, 12 * sc, 64 * sc, new Color(.78f, .76f, .70f));
            foreach (var place in CityDistricts.Places) {
                var d = CityDistricts.At(place.X, place.Z); bool open = CityDistricts.Unlocked(d, rank);
                Color k = place.Kind == "rival" ? coral : place.Kind == "supply" ? teal : place.Kind == "restaurant" ? new Color(.95f, .76f, .3f) : place.Kind == "recipe" ? new Color(.62f, .45f, .9f) : place.Kind == "you" ? white : new Color(.35f, .55f, .9f);
                var q = P(place.X, place.Z);
                Block(sheet, place.Name, q.x - 4, q.y - 4, 8, 8, open ? k : new Color(k.r, k.g, k.b, .45f));
                // Your home block is crowded at this scale: label only The Odd Table there.
                bool home = Mathf.Abs(place.X) < 40 && Mathf.Abs(place.Z) < 40 && place.Name != "The Odd Table";
                bool right = place.X > 480;
                if ((open || place.Kind == "rival") && !home) Label(sheet, place.Name, right ? q.x - 136 : q.x + 6, q.y - 8, 130, 16, 10, open ? white : new Color(1, 1, 1, .55f), false, right ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            }
            var markers = new System.Collections.Generic.List<(RectTransform, Transform)>();
            if (LocalCoop.Instance != null && LocalCoop.Instance.PlayerCount > 0) { int n = 0; foreach (var pl in LocalCoop.Instance.Players) if (pl) markers.Add((Block(sheet, "P" + (++n), 0, 0, 12, 12, n == 1 ? coral : new Color(.3f, .8f, 1f)), pl.transform)); }
            else if (Owner.Game.Player) markers.Add((Block(sheet, "You", 0, 0, 12, 12, coral), Owner.Game.Player.transform));
            tickLabels.Add(() => { foreach (var (m, t) in markers) if (m && t) { var q = P(t.position.x, t.position.z); m.anchoredPosition = new Vector2(q.x - 6, -(q.y - 6)); } });
            tickLabels[tickLabels.Count - 1]();
            var homeSpot = P(-8, 12); Label(sheet, "Stand, Milo's, Gilded Orbit", homeSpot.x - 60, homeSpot.y - 30, 150, 16, 10, white, true);

            float rx = mx + mw + 26, rw = 1190 - rx;
            Label(sheet, "REPUTATION", rx, 140, rw, 20, 13, muted, true);
            Label(sheet, Reputation.Titles[rank], rx, 160, rw, 40, 30, ink, true);
            Block(sheet, "Rep bar", rx, 206, rw, 14, pale);
            Block(sheet, "Rep fill", rx, 206, Mathf.Max(4, rw * Reputation.Progress(xp, rank)), 14, teal);
            if (Reputation.IsMax(rank)) Label(sheet, xp + " reputation. The whole city is yours.", rx, 226, rw, 22, 14, ink);
            else {
                var next = CityDistricts.OpenedAt(rank + 1); bool keyDone = st.KeystoneMet(rank + 1);
                Label(sheet, "Next: " + Reputation.Titles[rank + 1] + (next != null ? " (opens " + next.Name + ")" : ""), rx, 224, rw, 20, 14, ink, true);
                Label(sheet, (xp >= Reputation.Thresholds[rank + 1] ? "DONE   " : "") + xp + " / " + Reputation.Thresholds[rank + 1] + " reputation", rx, 244, rw, 18, 13, xp >= Reputation.Thresholds[rank + 1] ? teal : ink);
                Label(sheet, (keyDone ? "DONE   " : "GOAL   ") + Reputation.Keystones[rank + 1], rx, 262, rw, 18, 13, keyDone ? teal : coral, true);
            }
            // Where your reputation actually came from (biggest first), so pacing can be tuned from real play.
            var sources = st.RepSources == null ? new System.Collections.Generic.List<RepGain>() : st.RepSources.Where(r => r.Amount != 0).OrderByDescending(r => System.Math.Abs(r.Amount)).Take(5).ToList();
            Label(sheet, sources.Count == 0 ? "Earn reputation with happy restaurant guests, new stars, hidden recipes, rivals and recruits." : "Earned from:  " + string.Join("   ", sources.Select(r => r.Source + " " + (r.Amount > 0 ? "+" : "") + r.Amount)), rx, 284, rw, 34, 11, muted);
            for (int i = 0; i < Reputation.Titles.Length; i++) {
                var d = CityDistricts.OpenedAt(i); bool got = rank >= i; float y = 322 + i * 44;
                Block(sheet, "Rank row", rx, y, rw, 38, got ? new Color(teal.r, teal.g, teal.b, .16f) : new Color(pale.r, pale.g, pale.b, .55f));
                Label(sheet, (got ? "OPEN   " : Reputation.Thresholds[i] + "   ") + Reputation.Titles[i], rx + 10, y + 3, rw - 20, 18, 14, got ? teal : ink, true);
                Label(sheet, d != null ? d.Name : "", rx + 10, y + 20, rw - 20, 16, 12, muted);
            }
            Label(sheet, "Map key:  red rival   green supplier   gold restaurant   purple recipe   blue service", rx, 588, rw, 36, 11, muted);
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
