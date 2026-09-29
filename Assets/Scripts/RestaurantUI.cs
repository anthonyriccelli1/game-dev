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
        RectTransform finishControls, fillConfirmGroup, fillBeginGroup;
        Text finishBrushTitle, finishBrushTarget, finishBrushCost, finishFillText;
        Button finishFillConfirm;
        Text rank, status, cash, hints, notice, stock, orderSummary, cityRank;
        ScrollRect scroll;
        string category = "All", signature = "";
        string finishFilter = "All finishes";
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
            BuildFinishControls();
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
            stock.text = "Pantry: " + string.Join(", ", s.Pantry.Where(l => l.Count > 0).Select(l => l.Count + " " + Ingredients.Name(l.Id))) + "\n" + s.Seats + " seats    |    " + s.CookSlots + " cooking stations";
            cash.text = "$" + Owner.Game.State.Cash;
            hints.text = Owner.FinishBrushActive ? "Click one patch    /    F Quote all walls or entire floor    /    Esc, B or right click Return to catalog"
                : Owner.PlacementActive ? "Place with left click    /    R Rotate " + (Owner.PreviewRotation * 90) + " degrees    /    Esc Cancel    /    B Return to catalog" : "B  Catalog     /     Tab  Manage restaurant     /     E  Interact     /     Esc  Pause";
            RefreshFinishControls();
            notice.text = Owner.PlacementActive ? Owner.Hint : !string.IsNullOrEmpty(Owner.FocusPrompt) ? Owner.FocusPrompt : Owner.Game.Notice;
            var summary = new StringBuilder(s.Open ? "Service is open\n" : "Doors closed to new guests\n");
            int waiting = 0, cooking = 0, ready = 0;
            foreach (var order in s.Orders) { if (order.Stage == RestaurantOrderStage.Waiting) waiting++; if (order.Stage == RestaurantOrderStage.Cooking) cooking++; if (order.Stage == RestaurantOrderStage.Ready) ready++; }
            summary.Append(waiting).Append(" waiting  /  ").Append(cooking).Append(" cooking  /  ").Append(ready).Append(" ready\nTab to manage orders");
            orderSummary.text = summary.ToString();
            // The crew "phone" (Staff tab) works before you own the restaurant, as soon as the stand is up.
            bool panelVisible = visible || ((Owner.Panel == "Map" || Owner.Panel == "Listing" || Owner.Panel == "Supplies" || Owner.Panel == "Phone" || (Owner.Panel == "Staff" && Owner.Game.State.StandBuilt)) && Owner.Game.Started && !Owner.Game.Paused);
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
            var key = new StringBuilder(Owner.Panel).Append('|').Append(category).Append('|').Append(finishFilter).Append('|').Append(Owner.Game.State.Cash).Append('|').Append(s.Stars).Append('|').Append(s.Open).Append('|').Append(string.Join(",", s.Pantry.Select(l => l.Id + l.Count))).Append(shopCategory).Append(string.Join(",", cart.Select(c => c.Id + c.Count))).Append('|').Append(s.Layout.Count).Append('|').Append(s.Reviews.Count).Append('|').Append(s.Served).Append('|').Append(Owner.SelectedInstanceId).Append('|').Append(Owner.Game.State.RecipeUnlocked).Append('|').Append(Owner.Game.State.Xp).Append('|').Append(s.CanCustomize).Append(Owner.AtSupplier);
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
            Label(sheet, Owner.Panel == "Phone" ? "Your phone." : Owner.Panel == "Listing" ? "For sale later." : Owner.Panel == "Supplies" ? "Milo's Market." : Owner.Panel == "Catalog" ? "Make this place yours." : Owner.Panel == "Service" ? "On the pass." : Owner.Panel == "Menu" ? "What's cooking?" : Owner.Panel == "Cookbook" ? "Recipes." : Owner.Panel == "Staff" ? "Crew and People book." : Owner.Panel == "Map" ? "Saffron Bay." : Owner.Panel == "Furniture" ? "Give it a new home." : "Word on the street.", 30, 22, 785, 45, 31, ink, true);
            Button(sheet, "Close  x", 1060, 24, 130, 36, () => Owner.ClosePanel(), ink, paper);
            Label(sheet, "Time pauses while management is open.", 823, 62, 365, 19, 12, muted, false, TextAnchor.MiddleRight);
            string[] panels = { "Catalog", "Service", "Menu", "Cookbook", "Staff", "Map", "Reviews", "Furniture" };
            // Milo's shop and property listings are places in the city, not restaurant management: no tabs.
            if (Owner.Panel == "Supplies" || Owner.Panel == "Listing" || Owner.Panel == "Phone") panels = new string[0];
            for (int i = 0; i < panels.Length; i++) {
                string tab = panels[i]; bool active = Owner.Panel == tab;
                Button(sheet, tab == "Catalog" ? "Shop" : tab == "Furniture" ? "Arrange" : tab, 30 + i * 114, 78, 108, 35, () => { Owner.ShowPanel(tab); signature = ""; Refresh(); }, active ? teal : pale, active ? white : ink);
            }
            Label(sheet, "Your budget  $" + Owner.Game.State.Cash, 950, 82, 236, 28, 17, ink, true, TextAnchor.MiddleRight);
            Block(sheet, "Rule", 30, 124, 1160, 2, pale);
            if (Owner.Panel == "Phone") BuildPhone(sheet);
            else if (Owner.Panel == "Listing") BuildListing(sheet);
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
            if (category == "Finishes") {
                string[] filters = { "All finishes", "Walls", "Floors" };
                for (int i = 0; i < filters.Length; i++) {
                    string filter = filters[i]; bool active = filter == finishFilter;
                    Button(sheet, filter, 30 + i * 164, 181, 154, 28, () => { finishFilter = filter; signature = ""; if (scroll) scroll.verticalNormalizedPosition = 1; Refresh(); }, active ? teal : pale, active ? white : ink);
                }
                Label(sheet, "Brush one section from $1. Fill shows its total before purchase.", 535, 184, 650, 22, 14, muted);
            }
            bool Visible(CatalogItem item) {
                if (category != "All" && item.Category.ToString() != category) return false;
                var finish = FinishCatalog.Find(item.Id);
                return category != "Finishes" || finishFilter == "All finishes" || finish != null && (finishFilter == "Walls" ? finish.IsWall : !finish.IsWall);
            }
            int count = RestaurantCatalog.Items.Count(Visible);
            float top = category == "Finishes" ? 218 : 186;
            var content = Scroller(sheet, 30, top, 1160, 652 - top, Mathf.CeilToInt(count / 3f) * 232);
            int index = 0;
            foreach (var item in RestaurantCatalog.Items) {
                if (!Visible(item)) continue;
                var entry = item; int col = index % 3, row = index / 3; index++;
                var finish = FinishCatalog.Find(entry.Id);
                var card = Block(content, entry.Name, col * 386, row * 232, 372, 219, white);
                bool tierLocked = entry.Tier > Owner.Game.State.RankEarned, locked = tierLocked || entry.RequiredStars > Owner.Data.Stars;
                Block(card, "Swatch", 0, 0, 372, 5, locked ? muted : teal);
                var thumbnail = Box(card, "Product picture", 12, 17, 105, 106);
                var raw = thumbnail.gameObject.AddComponent<RawImage>();
                raw.texture = Owner.GetCatalogIcon(entry.Id); raw.color = locked ? new Color(.76f,.76f,.76f,1) : Color.white; raw.raycastTarget = false;
                Label(card, entry.Name, 128, 17, 227, 46, 21, ink, true);
                Label(card, "$" + entry.Price + (finish == null ? "" : finish.IsWall ? " / section" : " / tile"), 128, 67, 227, 30, 23, locked ? muted : teal, true);
                Label(card, finish != null ? (finish.IsWall ? "Walls" : "Floors") + " / Full coverage: " + finish.Ambience + " ambience"
                    : entry.Category + (StationUpgrades.CanUpgrade(entry.Id) ? "  /  upgradeable" : "") + (entry.Seats > 0 ? "  /  " + entry.Seats + " seats" : "") + (entry.Ambience > 0 ? "  /  +" + entry.Ambience + " ambience" : ""), 128, 103, 230, 28, 12, muted);
                Label(card, entry.Description, 14, 134, 344, 43, 14, ink);
                if (StationUpgrades.CanUpgrade(entry.Id)) UpgradeLadder(card, entry.Id, 1, 250, 52, 50, false);
                string action = tierLocked ? "Unlocks at " + Reputation.Titles[entry.Tier] : locked ? "Unlock at " + entry.RequiredStars + " stars" : finish != null ? "Preview brush" : entry.Category == CatalogCategory.Exterior ? "Install for $" + entry.Price : "Preview & place";
                bool can = !locked && Owner.Data.CanCustomize && (finish != null || Owner.Game.State.Cash >= entry.Price);
                if (finish == null && !locked && Owner.Game.State.Cash < entry.Price) action = "Save $" + (entry.Price - Owner.Game.State.Cash) + " more";
                Button(card, action, 14, 178, 344, 30, () => Owner.SelectCatalogItem(entry.Id), can ? teal : pale, can ? white : muted, can);
            }
        }

        void BuildFinishControls() {
            finishControls = Block(canvas.transform, "Finish brush controls", 1035, 242, 379, 337, paper);
            // Blank panel space consumes pointer clicks, too.
            finishControls.GetComponent<Image>().raycastTarget = true;
            Block(finishControls, "Brush accent", 0, 0, 379, 6, teal);
            var swatch = Box(finishControls, "Selected material swatch", 18, 20, 74, 74).gameObject.AddComponent<RawImage>();
            swatch.raycastTarget = false;
            if (Owner.FinishBrushActive) swatch.texture = RestaurantArt.FinishSwatch(Owner.SelectedCatalogId);
            finishBrushTitle = Label(finishControls, "", 107, 21, 250, 62, 22, ink, true);
            finishBrushTarget = Label(finishControls, "", 18, 108, 343, 48, 18, ink, true);
            finishBrushCost = Label(finishControls, "", 18, 159, 343, 32, 18, teal, true);
            fillBeginGroup = Box(finishControls, "Fill request", 18, 200, 343, 76);
            Button(fillBeginGroup, "Quote all walls / entire floor  [F]", 0, 0, 343, 36, () => Owner.RequestFinishFill(), teal, white);
            Label(fillBeginGroup, "Click a patch to buy. Keep painting with the same brush.", 0, 44, 343, 35, 13, muted);
            fillConfirmGroup = Box(finishControls, "Fill confirmation", 18, 196, 343, 81);
            finishFillText = Label(fillConfirmGroup, "", 0, 0, 343, 38, 17, ink, true);
            Button(fillConfirmGroup, "Confirm fill", 0, 44, 214, 34, () => Owner.ConfirmFinishFill(), coral, white);
            finishFillConfirm = fillConfirmGroup.GetComponentInChildren<Button>();
            Button(fillConfirmGroup, "Back", 224, 44, 119, 34, () => Owner.CancelFinishFill(), pale, ink);
            Button(finishControls, "Return to catalog  [Esc]", 18, 288, 343, 32, () => Owner.CancelPlacement(), ink, paper);
            finishControls.gameObject.SetActive(Owner.FinishBrushActive);
        }
        void RefreshFinishControls() {
            if (!finishControls) return;
            finishControls.gameObject.SetActive(Owner.FinishBrushActive);
            if (!Owner.FinishBrushActive) return;
            var finish = FinishCatalog.Find(Owner.SelectedCatalogId); if (finish == null) return;
            finishBrushTitle.text = finish.Name + "\n" + (finish.IsWall ? "Wall section brush" : "Floor tile brush");
            finishBrushTarget.text = Owner.FinishTargetLabel;
            int price = Owner.Data.FinishPrice(Owner.SelectedCatalogId, Owner.FinishSurfaceKey, false);
            finishBrushCost.text = price < 0 ? "Choose a surface to preview" : price == 0 ? "Already applied / $0" : "$" + price + " for this patch";
            fillConfirmGroup.gameObject.SetActive(Owner.FinishFillPending); fillBeginGroup.gameObject.SetActive(!Owner.FinishFillPending);
            finishFillText.text = "Fill " + (finish.IsWall ? "all 36 wall sections" : "all 120 floor tiles") + " for $" + Owner.FinishFillQuote + "?";
            finishFillConfirm.interactable = Owner.Game.State.Cash >= Owner.FinishFillQuote && Owner.Data.CanCustomize;
        }

        // Menu: every dish with hands-on steps. Starters (burger, salad) are always known; others are bought or found.
        void BuildMenu(RectTransform sheet) {
            Label(sheet, "Offer fewer dishes for an easier shift, or a wider menu for more favorites and better takings.", 30, 141, 1148, 29, 16, ink);
            var dishes = RecipeBook.Recipes.Select(r => RestaurantCatalog.Dish(r.DishId)).Where(x => x != null).ToList();
            var content = Scroller(sheet, 30, 182, 1160, 390, Mathf.CeilToInt(dishes.Count / 2f) * 155);
            var st = Owner.Game.State;
            for (int index = 0; index < dishes.Count; index++) {
                var entry = dishes[index]; int col = index % 2, row = index / 2;
                bool known = st.Knows(entry.Id), hasGear = string.IsNullOrEmpty(entry.Equipment) || Owner.Data.HasEquipment(entry.Equipment);
                bool available = Owner.Data.IsDishAvailable(st, entry.Id), active = Owner.Data.ActiveMenu.Contains(entry.Id);
                var card = Block(content, entry.Name, col * 580, row * 155, 562, 142, known ? white : new Color(.92f, .91f, .86f));
                Label(card, entry.Name, 18, 13, 300, 30, 23, known ? ink : muted, true);
                string badge = entry.Id == "burger" || entry.Id == "salad" ? "STARTER" : !known ? "LOCKED" : entry.Id == "midnight" ? "FOUND" : "BOUGHT";
                Label(card, badge, 318, 19, 110, 22, 13, known ? teal : coral, true, TextAnchor.MiddleRight);
                Label(card, "$" + entry.Price, 441, 13, 101, 30, 23, teal, true, TextAnchor.MiddleRight);
                Label(card, "Uses " + string.Join(" + ", Ingredients.For(entry.Id).Select(Ingredients.Name)) + "   /   " + (string.IsNullOrEmpty(entry.Equipment) ? "" : "on the " + EquipmentName(entry.Equipment)), 18, 50, 521, 25, 15, muted);
                string help = !known ? RecipeBook.HowToGet(entry.Id) + "." : !hasGear ? "You know it. Now buy a " + EquipmentName(entry.Equipment) + " in the Shop." : entry.RequiredStars > Owner.Data.Stars ? "Reach " + entry.RequiredStars + " stars to serve it." : active ? "Guests can order this dish." : "Add it so guests can order it.";
                Label(card, help, 18, 85, 335, 43, 14, known ? ink : coral);
                Button(card, !available ? (known ? "Needs gear" : "Locked") : active ? "On menu" : "Add to menu", 372, 87, 171, 35, () => Owner.ToggleDish(entry.Id), available && active ? teal : pale, available && active ? white : available ? ink : muted, available);
            }
            var pantry = Block(sheet, "Pantry", 30, 589, 1160, 61, ink);
            Label(pantry, "Pantry: " + string.Join("   ", Ingredients.All.Where(i => Owner.Data.Stock(i.Id) > 0 || i.Source == Ingredients.Milo && i.Recipe == null).Select(i => Ingredients.Name(i.Id) + " " + Owner.Data.Stock(i.Id))), 16, 16, 760, 33, 17, paper, true);
            Label(pantry, "Restock by talking to Milo.", 800, 16, 344, 35, 16, paper, false, TextAnchor.MiddleRight);
        }

        // Cookbook: how every dish is built, and where new recipes come from (bought here, or found in the city).
        void BuildCookbook(RectTransform sheet) {
            var st = Owner.Game.State;
            Label(sheet, "Buy new recipes here. Rare ones can't be bought: they're hidden in the city.", 30, 141, 1148, 29, 16, ink);
            var content = Scroller(sheet, 30, 182, 1160, 466, RecipeBook.Recipes.Length * 165);
            for (int i = 0; i < RecipeBook.Recipes.Length; i++) {
                var recipe = RecipeBook.Recipes[i]; var dish = RestaurantCatalog.Dish(recipe.DishId); string id = dish.Id;
                bool known = st.Knows(id); var offer = Array.Find(RecipeBook.ForSale, f => f.dish == id);
                var card = Block(content, dish.Name, 0, i * 165, 1141, 152, known ? white : new Color(.92f, .91f, .86f));
                Label(card, dish.Name + (known ? "" : "  (not learned)"), 18, 12, 700, 30, 22, known ? ink : muted, true);
                Label(card, "Needs: " + string.Join(" + ", recipe.Components.Select(ComponentName)) + "   /   sells for $" + dish.Price, 18, 44, 800, 24, 15, teal, true);
                Label(card, known ? string.Join("   >   ", recipe.Steps) : RecipeBook.HowToGet(id) + ".", 18, 70, offer.dish != null && !known ? 820 : 1100, 76, 14, muted);
                if (offer.dish != null && !known) {
                    bool rankOk = st.RankEarned >= offer.rank, cash = st.Cash >= offer.price;
                    Button(card, !rankOk ? "Needs " + Reputation.Titles[offer.rank] : "Buy recipe  /  $" + offer.price, 870, 50, 250, 44, () => Owner.BuyRecipe(id), rankOk && cash ? teal : pale, rankOk && cash ? white : muted, rankOk && cash);
                }
            }
        }
        static string ComponentName(string id) => id == "soup" ? "a ladle of soup" : id == "bun" ? "bun" : id == "cooked_patty" ? "cooked patty" : id == "chopped_greens" ? "chopped greens" : id == "midnight_sauce" ? "midnight sauce" : id;
        // Phone (P): your crew and the map, plus contacts. Zeeb sells secret-recipe ingredients once you know one.
        void BuildPhone(RectTransform sheet) {
            var st = Owner.Game.State; var purple = new Color(.45f, .3f, .7f);
            Label(sheet, "APPS", 35, 140, 300, 22, 13, muted, true);
            Button(sheet, "Crew  (your workers)", 35, 166, 300, 50, () => Owner.ShowPanel("Staff"), teal, white);
            Button(sheet, "Map  (M)", 35, 226, 300, 50, () => Owner.ShowPanel("Map"), teal, white);
            if (Owner.Data.Owned) Button(sheet, "My restaurant  (Tab)", 35, 286, 300, 50, () => Owner.ShowPanel("Service"), teal, white);
            Label(sheet, "CONTACTS", 365, 140, 300, 22, 13, muted, true);
            var milo = Block(sheet, "Contact Milo", 365, 166, 825, 70, white);
            Label(milo, "Milo", 20, 10, 300, 28, 21, ink, true); Label(milo, "Everyday ingredients. Visit his shop in person.", 20, 40, 780, 22, 14, muted);
            var card = Block(sheet, "Contact Zeeb", 365, 248, 825, 400, st.Knows("midnight") ? ink : new Color(.25f, .25f, .28f));
            var face = Block(card, "Zeeb avatar", 20, 20, 90, 90, st.Knows("midnight") ? purple : new Color(.35f, .35f, .38f));
            Label(face, st.Knows("midnight") ? "Z" : "?", 0, 0, 90, 90, 48, white, true, TextAnchor.MiddleCenter);
            if (!st.Knows("midnight")) {
                Label(card, "Unknown number", 130, 22, 660, 32, 24, paper, true);
                Label(card, "Word is someone out of Little Nebula sells ingredients you can't buy anywhere. They only deal with cooks who know a rare recipe. Win one first.", 130, 62, 660, 80, 16, new Color(.8f, .8f, .82f));
                return;
            }
            Label(card, NightStashes.Dealer, 130, 18, 660, 34, 26, paper, true);
            Label(card, "Alien, Little Nebula. Secret ingredients, no questions. Pays later is cool, not paying is not.", 130, 56, 660, 44, 15, new Color(.8f, .8f, .82f));
            string status = st.DropBottles > 0 ? (st.DropPlaced ? "Your drop (" + st.DropBottles + " bottles) is waiting near " + NightStashes.Spots[st.StashSpot].Hint + ". Look for the purple glow." : "Order on the way: " + st.DropBottles + " bottles. He drops it after dark and texts you where.") : "No order out.";
            Label(card, status, 20, 126, 785, 44, 16, new Color(.85f, .75f, 1f), true);
            Label(card, st.ZeebDebt > 0 ? "You owe Zeeb $" + st.ZeebDebt + ". Pay it before you order again." : "You're square with Zeeb.", 20, 172, 785, 26, 16, st.ZeebDebt > 0 ? coral : teal, true);
            Label(card, "ORDER MIDNIGHT SAUCE  ($" + NightStashes.SaucePrice + " a bottle, " + Mathf.RoundToInt(NightStashes.Deposit * 100) + "% up front)", 20, 214, 785, 22, 13, new Color(.75f, .8f, .8f), true);
            bool canOrder = st.ZeebDebt == 0 && st.DropBottles == 0;
            for (int i = 0; i < NightStashes.Orders.Length; i++) {
                int n = NightStashes.Orders[i], dep = NightStashes.DepositFor(n); bool ok = canOrder && st.Cash >= dep;
                Button(card, n + " bottles:  $" + dep + " now + $" + (NightStashes.Cost(n) - dep) + " later", 20 + i * 262, 242, 250, 48, () => Owner.OrderZeeb(n), ok ? purple : pale, ok ? white : muted, ok);
            }
            if (st.ZeebDebt > 0) Button(card, "Pay Zeeb $" + Mathf.Min(st.Cash, st.ZeebDebt) + (st.Cash < st.ZeebDebt ? " (all you have)" : ""), 20, 306, 380, 44, () => Owner.PayZeeb(), st.Cash > 0 ? teal : pale, st.Cash > 0 ? white : muted, st.Cash > 0);
        }

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

        // Milo's shop: picture cards by category, a cart, and one Purchase at the counter. Groceries go home in a bag.
        string shopCategory = "All";
        readonly List<StockLine> cart = new List<StockLine>();
        int CartCount(string id) { var l = cart.Find(c => c.Id == id); return l == null ? 0 : l.Count; }
        void AddToCart(string id, int delta) { var l = cart.Find(c => c.Id == id); if (l == null) cart.Add(l = new StockLine { Id = id }); l.Count = Mathf.Max(0, l.Count + delta); cart.RemoveAll(c => c.Count <= 0); signature = ""; }
        void BuildSupplies(RectTransform sheet) {
            var st = Owner.Game.State; var d = Owner.Data;
            Label(sheet, "\"Fresh every morning. What'll it be?\"  - Milo", 35, 136, 780, 28, 17, muted);
            var cats = new List<string> { "All" }; cats.AddRange(Ingredients.Categories);
            for (int i = 0; i < cats.Count; i++) { string c = cats[i]; bool on = shopCategory == c; Button(sheet, c, 35 + i * 128, 170, 120, 32, () => { shopCategory = c; signature = ""; }, on ? teal : pale, on ? white : ink); }
            var items = Ingredients.All.Where(x => shopCategory == "All" || x.Category == shopCategory).ToList();
            var grid = Scroller(sheet, 35, 212, 790, 445, Mathf.CeilToInt(items.Count / 4f) * 234);
            for (int i = 0; i < items.Count; i++) {
                var ing = items[i]; string id = ing.Id; string why = d.IngredientLock(st, ing);
                var card = Block(grid, "Shop " + ing.Name, (i % 4) * 196, (i / 4) * 234, 186, 224, why == null ? white : new Color(.92f, .91f, .86f));
                var icon = Box(card, "Picture", 38, 8, 110, 110); var raw = icon.gameObject.AddComponent<RawImage>(); raw.texture = FoodIcons.Get(id, white); raw.raycastTarget = false; if (why != null) raw.color = new Color(.55f, .55f, .55f);
                Label(card, ing.Name, 8, 120, 170, 24, 16, ink, true, TextAnchor.MiddleCenter);
                Label(card, (ing.PackPrice > 0 ? ing.PackSize + " for $" + ing.PackPrice : "Not for sale") + "   /   have " + d.Stock(id), 8, 144, 170, 20, 13, muted, false, TextAnchor.MiddleCenter);
                if (why != null) Label(card, "LOCKED\n" + why, 8, 168, 170, 50, 13, coral, true, TextAnchor.MiddleCenter);
                else Button(card, CartCount(id) > 0 ? "+ Add  (" + CartCount(id) + " in cart)" : "+ Add to cart", 8, 182, 170, 32, () => AddToCart(id, 1), teal, white);
            }
            var box = Block(sheet, "Cart", 845, 170, 345, 487, ink);
            Label(box, "Cart", 18, 12, 300, 32, 24, paper, true);
            int total = 0; float y = 56;
            foreach (var line in cart.ToList()) {
                var ing = Ingredients.Get(line.Id); if (ing == null) continue; string id = line.Id; int cost = ing.PackPrice * line.Count; total += cost;
                Label(box, line.Count + " x " + ing.Name, 18, y, 190, 30, 16, paper);
                Label(box, "$" + cost, 206, y, 60, 30, 16, paper, true, TextAnchor.MiddleRight);
                Button(box, "-", 280, y, 44, 30, () => AddToCart(id, -1), pale, ink);
                y += 38;
            }
            if (cart.Count == 0) Label(box, "Add something from the shelves.", 18, 60, 300, 30, 15, new Color(.75f, .8f, .8f));
            Label(box, "Total  $" + total, 18, 340, 305, 32, 22, paper, true, TextAnchor.MiddleRight);
            bool can = Owner.AtSupplier && total > 0 && total <= st.Cash;
            Button(box, "Purchase", 18, 384, 305, 46, () => { if (Owner.BuyGroceries(cart)) { cart.Clear(); signature = ""; } }, can ? teal : pale, can ? white : muted, can);
            if (RestaurantState.NeedsMiloHelp(st, d)) Button(box, st.LastMiloHelpDay == st.Day ? "Milo already helped today" : "\"Milo, I'm broke...\"  (free basics)", 18, 290, 305, 38, () => Owner.AskMiloForHelp(), coral, white, Owner.AtSupplier && st.LastMiloHelpDay != st.Day);
            Label(box, !Owner.AtSupplier ? "Talk to Milo in person to buy." : total > st.Cash ? "Not enough cash." : "Groceries go in a bag you carry home. Unpack at your pantry.", 18, 436, 305, 44, 13, new Color(.75f, .8f, .8f));
        }

        void BuildService(RectTransform sheet) {
            Label(sheet,"Physical kitchen / Everyone can cook. No worker required.",30,139,1100,40,23,ink,true);
            Button(sheet,Owner.Data.Open?"Close to new guests":"Open for service",848,188,339,40,()=>Owner.ToggleService(),Owner.Data.Open?ink:teal,Owner.Data.Open?paper:white);
            Label(sheet,"BURGER: Patty on the grill (ready at 8s, burns at 24s) > plate from the rack > E bun shelf > E grill to add the patty > serve.\nSALAD: Greens on the prep bench, hold E to chop > onto a plate.  MIDNIGHT: burger + E the sauce shelf while holding the plate.\nDirty plates: clear tables, stack them at the sink, hold E to wash one at a time.",30,190,790,200,16,muted);
            Button(sheet,"Wait until night (+30% sales)",848,242,339,40,()=>{Owner.Game.State.Clock=180;Owner.Feedback("Night service pays 30% more. Watch the rush.");},pale,ink);
            Button(sheet,"Prep research / 3 Flux",848,297,339,40,()=>{Owner.Game.State.Kitchen.SpendFlux(Owner.Game.State,"research",out var m);Owner.Feedback(m);},pale,ink);
            int i=0;foreach(var w in Owner.Data.Workers){string id=w.Id;Button(sheet,id+" energy boost / 1 Flux",848,351+i++*48,339,40,()=>{Owner.Game.State.Kitchen.SpendFlux(Owner.Game.State,id,out var m);Owner.Feedback(m);},pale,ink);}
            var report=Owner.Game.State.Kitchen.LastReport;
            if(report!=null)Label(sheet,"LAST SHIFT   Sales $"+report.GrossSales+" - ingredients $"+report.IngredientCosts.ToString("0.0")+" - wages $"+report.Wages+" = net $"+report.Net+"\nServed "+report.Served+" / lost "+report.Lost+" | Satisfaction "+report.Satisfaction.ToString("0")+"% | Stars "+report.StarsBefore+" > "+report.StarsAfter+"\n"+string.Join(" / ",report.Comments)+"\n"+report.StaffSummary+(Owner.Data.Stars<StationUpgrades.StarsNeeded(2)?"\nNEXT GOAL: "+StationUpgrades.StarsNeeded(2)+" stars unlocks chrome Level 2 kitchen gear (faster grill, sink and prep).":""),30,506,1157,143,16,ink);
            else Label(sheet,"A shift takes arrivals for two minutes, then lets you finish remaining guests.\nUse the door sign to end arrivals early. Tab opens management between shifts.\nController Start joins player two. Management is a shared screen while closed.",30,523,1157,110,18,muted);
        }
        void BuildFurniture(RectTransform sheet) {
            Label(sheet, "Choose an owned furnishing to move. This list includes stations hidden behind other furniture.", 30, 137, 1150, 28, 16, ink);
            var owned = Owner.Data.Layout.FindAll(p => { var item = RestaurantCatalog.Find(p.CatalogId); return item != null && !item.IsFinish && !item.IsExterior; });
            var content = Scroller(sheet, 30, 175, 365, 465, Mathf.Max(465, owned.Count * 58));
            for (int i = 0; i < owned.Count; i++) {
                var placed = owned[i];
                var definition = RestaurantCatalog.Find(placed.CatalogId);
                string level = "";
                if (StationUpgrades.CanUpgrade(placed.CatalogId)) { int lv = Owner.Data.LevelOf(placed.InstanceId); bool ready = lv < StationUpgrades.MaxLevel && Owner.Data.Stars >= StationUpgrades.StarsNeeded(lv + 1) && Owner.Game.State.RankEarned >= StationUpgrades.RankNeeded(lv + 1); level = "  L" + lv + (ready ? " > UPGRADE" : ""); }
                Button(content, definition.Name + level + "  #" + placed.InstanceId + "  (" + placed.X + ", " + placed.Z + ")", 6, i * 58 + 3, 340, 50,
                    () => Owner.SelectFurnitureItem(placed.InstanceId), placed.InstanceId == Owner.SelectedInstanceId ? teal : pale,
                    placed.InstanceId == Owner.SelectedInstanceId ? white : ink);
            }
            PlacedItem selected = null;
            foreach (var item in Owner.Data.Layout) if (item.InstanceId == Owner.SelectedInstanceId) selected = item;
            if (selected == null) { Label(sheet, "Select any owned furnishing from the list. B while aiming at one opens it here directly.", 430, 270, 740, 90, 24, ink, true, TextAnchor.MiddleCenter); return; }
            var entry = RestaurantCatalog.Find(selected.CatalogId); int id = selected.InstanceId;
            var picture = Box(sheet, "Selected furnishing", 430, 175, 275, 235).gameObject.AddComponent<RawImage>(); picture.texture = Owner.GetCatalogIcon(selected.CatalogId, StationUpgrades.CanUpgrade(selected.CatalogId) ? Owner.Data.LevelOf(selected.InstanceId) : 1); picture.raycastTarget = false;
            Label(sheet, entry.Name, 730, 180, 430, 58, 31, ink, true);
            bool ladder = StationUpgrades.CanUpgrade(selected.CatalogId);
            Label(sheet, entry.Description, 730, 245, 425, ladder ? 60 : 90, ladder ? 15 : 18, muted);
            if (ladder) UpgradeLadder(sheet, selected.CatalogId, Owner.Data.LevelOf(selected.InstanceId), 730, 300, 76, true);
            Label(sheet, "Move: choose a new floor cell, then click. R rotates. Esc cancels.", 430, 429, 735, 46, 18, ink);
            Button(sheet, "Move furnishing", 430, 490, 350, 52, () => Owner.MoveItem(id), teal, white, Owner.Data.CanCustomize);
            Button(sheet, "Sell for $" + (selected.Paid / 2), 805, 490, 350, 52, () => Owner.SellItem(id), coral, white, Owner.Data.CanCustomize);
            if (StationUpgrades.CanUpgrade(selected.CatalogId)) {
                int lv = Owner.Data.LevelOf(id), next = lv + 1;
                Label(sheet, StationUpgrades.LevelName(lv) + ":  " + StationUpgrades.Effect(selected.CatalogId, lv), 430, 552, 735, 26, 17, ink, true);
                if (next <= StationUpgrades.MaxLevel) {
                    int cost = StationUpgrades.Cost(entry, next); int stars = StationUpgrades.StarsNeeded(next); int rank = StationUpgrades.RankNeeded(next); bool starsOk = Owner.Data.Stars >= stars && Owner.Game.State.RankEarned >= rank;
                    string label = starsOk ? "UPGRADE to Level " + next + "  /  $" + cost + "   (" + StationUpgrades.Effect(selected.CatalogId, next) + ")" : Owner.Data.Stars < stars ? "Level " + next + " unlocks at " + stars + " stars" : "Level " + next + " arrives with the Docks (" + Reputation.Titles[rank] + ")";
                    Button(sheet, label, 430, 584, 725, 48, () => Owner.UpgradeItem(id), teal, white, starsOk && Owner.Game.State.Cash >= cost && Owner.Data.CanCustomize);
                } else Label(sheet, "Fully upgraded.", 430, 590, 735, 30, 17, teal, true);
            }
        }

        // Level tiles: the next chrome level is shown for real (so players know it exists and want it);
        // premium Level 3 stays a locked "?" until you own it.
        void UpgradeLadder(RectTransform parent, string id, int current, float x, float y, float size, bool withCurrent) {
            int first = withCurrent ? 1 : 2, tagSize = size >= 60 ? 13 : 11;
            for (int level = first; level <= StationUpgrades.MaxLevel; level++) {
                float left = x + (level - first) * (size + 8);
                bool starsOk = Owner.Data.Stars >= StationUpgrades.StarsNeeded(level), rankOk = Owner.Game.State.RankEarned >= StationUpgrades.RankNeeded(level);
                bool mystery = level >= 3 && current < 3;
                if (mystery) {
                    var tile = Block(parent, "Level " + level + " mystery", left, y, size, size, ink);
                    Label(tile, "?", 0, 0, size, size - 12, (int)(size * .5f), paper, true, TextAnchor.MiddleCenter);
                } else {
                    Block(parent, "Level " + level + " frame", left - 2, y - 2, size + 4, size + 4, level == current ? teal : level == 2 ? new Color(.62f, .7f, .74f) : pale);
                    var pic = Box(parent, "Level " + level + " preview", left, y, size, size).gameObject.AddComponent<RawImage>();
                    pic.texture = Owner.GetCatalogIcon(id, level); pic.raycastTarget = false; pic.color = level > current && !starsOk ? new Color(.82f, .82f, .82f) : Color.white;
                }
                string tag = level == current ? "NOW" : level < current ? "L" + level : level == 2 ? (starsOk ? "READY" : "2 STARS") : (rankOk && starsOk ? "READY" : "DOCKS");
                var strip = Block(parent, "Level " + level + " tag", left, y + size - tagSize - 4, size, tagSize + 4, level == current ? teal : ink);
                Label(strip, tag, 0, 0, size, tagSize + 4, tagSize, paper, true, TextAnchor.MiddleCenter);
            }
        }

        void BuildStaff(RectTransform sheet) {
            var gs = Owner.Game.State; bool people = People.UseResidents;
            Label(sheet, "Your Flux: " + gs.Flux + ".  Feed a resident once and they join your People book; then recruit them with Flux. Anyone can do any job." + (gs.StandWorker != null ? "   Stand: " + gs.StandWorkerStatus + "  Earned $" + gs.StandWorkerEarned : ""), 30, 140, 1144, 35, 15, ink);
            string[] rivals = { "Maestro Vey|Head chef. Gold toque, glowing eyes. Runs a kitchen like an orchestra.", "Nyx|Sommelier with a crystal halo. Guests tip double when she pours.", "K-9|Chrome line cook with four arms and a neon visor. Never tires.", "Aurora|Maitre d'. Her monocle sees every empty seat before you do.", "Seraphine|Winged pastry chef. Desserts so good customers float out.", "Obsidian Titan|Doorman. Nobody makes a scene with him at the door.", "Lumen|Mixologist with a neon crest. Every drink glows." };
            // Crew cards: cash hires, plus anyone already recruited. Legacy special recruits only show once hired.
            var crew = new List<StaffDefinition>();
            foreach (var w in RestaurantCatalog.Staff) if (!w.Special || !people || Owner.Data.Workers.Exists(x => x.Id == w.Id)) crew.Add(w);
            foreach (var w in Owner.Data.Workers) { var d = ResidentCast.Staff(w.Id); if (d != null) crew.Add(d); }
            var cast = ResidentCast.OldMarket; var book = new List<ResidentDef>();
            if (people) foreach (var r in cast) if (!Owner.Data.Workers.Exists(x => x.Id == r.Id)) book.Add(r);
            int met = 0; foreach (var r in cast) if (gs.HasMet(r.Id)) met++;
            float crewH = Mathf.CeilToInt(crew.Count / 2f) * 320, bookTop = crewH + 10, bookH = people ? 60 + Mathf.CeilToInt(book.Count / 4f) * 300 : 0, rivalTop = bookTop + bookH;
            var content = Scroller(sheet, 30, 189, 1160, 462, rivalTop + 60 + Mathf.CeilToInt(rivals.Length / 2f) * 320 + 20);
            int index = 0;
            foreach (var worker in crew) {
                var entry = worker;
                var card = Block(content, entry.Name, (index % 2) * 580, (index / 2) * 320, 562, 303, entry.Special ? new Color(1f, .96f, .86f) : white); index++;
                var def = ResidentCast.Get(entry.Id); float textX = 25;
                if (def != null && people) { var pic = Box(card, "Portrait", 20, 20, 130, 130).gameObject.AddComponent<RawImage>(); pic.texture = ResidentIcons.Get(def, false); pic.raycastTarget = false; textX = 165; }
                Label(card, entry.Name, textX, 24, 520 - textX, 45, 26, ink, true);
                Label(card, entry.Description, textX, 78, 532 - textX, 90, 17, muted);
                string price = def != null ? def.Rarity + " resident" : entry.Special ? "Recruit for " + entry.FluxCost + " Flux" : "Hire for $" + entry.Cost;
                Label(card, "Specialty: " + entry.Role + "    /    " + price, 25, 172, 520, 28, 16, ink, true);
                BuildWorkerActions(card, entry);
            }
            if (people) {
                Label(content, "PEOPLE BOOK  /  Old Market  /  " + met + " of " + cast.Length + " met", 22, bookTop + 8, 1110, 40, 24, ink, true);
                for (int i = 0; i < book.Count; i++) {
                    var r = book[i]; bool known = gs.HasMet(r.Id);
                    var card = Block(content, r.Name, (i % 4) * 285, bookTop + 60 + (i / 4) * 300, 270, 285, known ? new Color(1f, .96f, .86f) : pale);
                    var pic = Box(card, "Portrait", 65, 12, 140, 140).gameObject.AddComponent<RawImage>(); pic.texture = ResidentIcons.Get(r, !known); pic.raycastTarget = false;
                    Label(card, known ? r.Name : "???", 10, 158, 250, 30, 21, ink, true, TextAnchor.MiddleCenter);
                    Label(card, known ? r.Rarity + "  /  " + r.Job : r.Rarity + (r.NightOnly ? "  /  night only" : "") + (r.MinAmbience > 0 ? "  /  needs ambience " + r.MinAmbience : ""), 5, 188, 260, 24, 13, muted, false, TextAnchor.MiddleCenter);
                    string id = r.Id;
                    if (known) Button(card, "Recruit  /  " + r.FluxCost + " Flux", 15, 222, 240, 46, () => Owner.Hire(id), teal, white, gs.Flux >= r.FluxCost);
                    else Label(card, "Serve them a meal to meet them", 10, 225, 250, 40, 14, muted, false, TextAnchor.MiddleCenter);
                }
            }
            // The Gilded Orbit's elite crew: visible, desirable, and not available yet.
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
            // Tonight's stash: a purple circle around the rough area, not the exact spot.
            if (st.StashActive) {
                var spot = NightStashes.Spots[st.StashSpot]; var c = P(spot.X + 9, spot.Z - 7); float r = 26 * sc;
                Block(sheet, "Stash area", c.x - r, c.y - r, r * 2, r * 2, new Color(.62f, .45f, .9f, .35f));
                Label(sheet, "Zeeb's drop: near " + spot.Hint, c.x + r + 2, c.y - 8, 190, 16, 10, new Color(.85f, .75f, 1f), true);
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
