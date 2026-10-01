using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RestaurantCity {
    public class CityGame : MonoBehaviour {
        public GameState State = new GameState();
        public FirstPersonPlayer Player;
        public StreetGuard Guard;
        public RestaurantController Restaurant;
        public LocalCoop CoOp;
        public Light Sun;
        public Light[] Lamps;
        public GameObject Stand, SetupMarker, Customer, GrillFood, HandFood, RecipeGlow;
        public Vector3 SpawnPoint = new Vector3(0, .15f, -4);
        public bool Paused { get; private set; } = true;
        public bool Started { get; private set; }
        public bool SmokeMode { get; private set; }
        public bool PreviewMode { get; private set; }
        public string Notice { get; private set; }
        public string SaveStatus { get; private set; } = "Progress saves automatically";
        float noticeUntil, saveTimer;
        bool wasNight, lastOrder;
        Vector3 customerPosition;
        public string SavePath => Path.Combine(Application.persistentDataPath, "restaurant-city-v1.json");

        void Awake() {
            SmokeMode = Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "--smoke-test" || arg == "--snapshots" || arg.StartsWith("--physical-") || arg.StartsWith("--interaction-"));
            PreviewMode = Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "--dev-zilo");
            if (!SmokeMode) Load(); else State = new GameState();
            if (PreviewMode) SaveStatus = "Developer preview - progress is not saved";
            SetPaused(true);
            if (Customer) customerPosition = Customer.transform.position;
        }
        void Start() {
            Restaurant = gameObject.AddComponent<RestaurantController>(); Restaurant.Initialize(this);
            CoOp = gameObject.AddComponent<LocalCoop>(); CoOp.Initialize(this);
            gameObject.AddComponent<PhysicalHud>().Game = this;
            SyncWorld();
            if (PreviewMode) StartCoroutine(PreviewZilo());
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0) gameObject.AddComponent<PrototypeSmokeTest>().Game = this;
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg.StartsWith("--physical-"))) gameObject.AddComponent<PhysicalAcceptance>().Game = this;
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg.StartsWith("--interaction-"))) gameObject.AddComponent<InteractionAcceptance>().Game = this;
        }
        IEnumerator PreviewZilo() {
            yield return null;
            const string id = "201_TripoAlien";
            State.StandBuilt = true;
            State.Restaurant.Owned = true;
            if (!State.MetResidents.Contains(id)) State.MetResidents.Add(id);
            if (!State.Restaurant.Workers.Exists(w => w.Id == id))
                State.Restaurant.Workers.Add(new WorkerState { Id = id, Job = StaffJob.Serve, Energy = 100 });
            Restaurant.RebuildLayout();
            Restaurant.Advance(.05f);
            Player.Teleport(RestaurantController.W(-9f, .15f, -10.5f));
            Player.LookAt(RestaurantController.W(-9f, 1.3f, -12f));
            SetPaused(false);
            Notify("Developer preview: Zilo is here. This session will not change your save.", 10);
            Debug.Log("ZILO_DEV_PREVIEW_READY worker=" + State.Restaurant.Workers.Exists(w => w.Id == id) + " savingDisabled=" + PreviewMode);
        }
        void Update() {
            if (Time.unscaledTime > noticeUntil) Notice = "";
            if (!Paused && !SmokeMode && (!Restaurant || !Restaurant.ManagementPauses && !Restaurant.PlacementActive)) {
                int missed = State.Missed;
                State.Tick(Time.deltaTime);
                if (State.Missed > missed) Notify("Customer left. The next one arrives in a moment.");
                if (State.IsNight != wasNight) Notify(State.IsNight ? "Nightfall. Orders pay 50% more. A recipe waits in the rival alley." : "Morning. The street is safe again.", 7);
                saveTimer += Time.deltaTime;
                if (saveTimer >= 20) { Save(); saveTimer = 0; }
            }
            wasNight = State.IsNight;
            SyncWorld();
        }
        public void SetPaused(bool value) {
            Paused = value;
            if (!value) Started = true;
            Cursor.lockState = value || SmokeMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value || SmokeMode;
            if (value && Started) Save();
        }
        public void Notify(string message, float duration = 4) { Notice = message; noticeUntil = Time.unscaledTime + duration; }
        public bool InteractForPlayer(InteractionKind kind, FirstPersonPlayer player, string site = null) {
            if (kind == InteractionKind.FutureRestaurant) return site == null ? Restaurant.BuyRestaurant(player) : Restaurant.BuyRestaurant(player, site);
            return Interact(kind);
        }
        public bool Interact(InteractionKind kind) {
            bool success = false;
            switch (kind) {
                case InteractionKind.Supplier:
                    if (State.Restaurant.Owned) { Restaurant.ShowPanel("Supplies"); return true; }
                    success = State.BuyIngredients();
                    if (success) Notify("3 fresh ingredients packed. Back to your stand!");
                    else if (State.RequestHelp()) { success = true; Notify("The supplier spots you one ingredient. Get back on your feet."); }
                    else Notify(State.Stock > 96 ? "Your stock is full." : !State.StandBuilt ? "Keep $10 for your stand. Set it up before buying more supplies." : "You need $6 for a pack of 3 ingredients.");
                    break;
                case InteractionKind.Stand:
                    success = State.SetUpStand(); Notify(success ? "Your first kitchen. Prep, grill, then serve." : "Stand setup costs $10."); break;
                case InteractionKind.Prep:
                    success = State.Prepare(); Notify(success ? "Burger prepared. Put it on the grill." : State.Stock == 0 ? "No ingredients. Buy patties and buns at Milo's cart beside your truck." : "Finish or discard your current dish first."); break;
                case InteractionKind.Grill:
                    success = State.UseGrill();
                    Notify(success ? State.Food == FoodStage.Cooking ? "Grilling. Plate it between 4 and 10 seconds." : State.IsBurnt ? "Burned! Discard it at the bin." : "Ready to serve. Find your waiting customer." : "Prepare a burger first, or wait until it finishes cooking."); break;
                case InteractionKind.Serve:
                    int price = State.SalePrice; success = State.Serve();
                    Notify(success ? "+$" + price + "  /  Another happy customer!" : State.IsBurnt ? "That burger is burned. Use the bin and try again." : !State.HasOrder ? "A customer will arrive soon." : "Prep, grill, and plate a burger before serving."); break;
                case InteractionKind.Bin: State.Discard(); success = true; Notify("Dish discarded. Ready for a fresh start."); break;
                case InteractionKind.Recipe:
                    bool firstRecipe = !State.RecipeUnlocked;
                    success = State.ClaimRecipe(Guard.Defeated);
                    Notify(success ? (firstRecipe ? "MIDNIGHT RECIPE + 3 FLUX! Use Flux in the Staff tab to recruit customers you've won over." : "+2 FLUX from the rival's stash. Recruit special customers in the Staff tab.") : !State.IsNight ? "Come back after dusk. Watch for the rival." : State.LastStashDay == State.Day ? "Already raided tonight. The rival restocks tomorrow night." : "The rival is guarding the stash. Three spatula hits will stagger them.", 8); break;
                case InteractionKind.FutureRestaurant:
                    return Restaurant.BuyRestaurant();
            }
            if (success) Save();
            SyncWorld(); return success;
        }
        public void Hurt(int amount) {
            HurtPlayer(Player, amount);
        }
        public void HurtPlayer(FirstPersonPlayer player, int amount) => HurtPlayer(player, amount, null);
        // Returns true when the player parried the blow (the attacker should stagger).
        public bool HurtPlayer(FirstPersonPlayer player, float amount, Vector3? from) {
            float taken = PlayerCombat.Of(player).Incoming(amount, from, out bool parried);
            if (taken <= 0) return parried;
            player.Health = Mathf.Max(0, (player.PlayerId == 0 ? Mathf.Min(State.Health, player.Health) : player.Health) - taken);
            if (player.PlayerId == 0) State.Health = Mathf.CeilToInt(player.Health);
            // Knocked out in a raid: the raid is lost right away (RaidBattle handles the wake-up and the losses).
            if (player.Health <= 0 && Restaurant && Restaurant.ActiveRaid && !Restaurant.ActiveRaid.Over) { Restaurant.ActiveRaid.PlayerDowned(player); return false; }
            if (player.Health > 0 && Restaurant && Restaurant.ActiveRaid && !Restaurant.ActiveRaid.Over) return false;
            if (player.Health > 0) { Notify("Player " + (player.PlayerId + 1) + " hit! Back out of the alley to escape."); return false; }
            if (player.PlayerId == 0) State.Respawn(); else State.Cash = Mathf.Max(0, State.Cash - 10);
            player.Health = 100; player.Teleport(SpawnPoint + Vector3.right * player.PlayerId * 1.5f); Guard.ResetGuard(); Save();
            Notify("Back at your stand. Lost up to $10; your stand and recipes are safe.", 7);
            return false;
        }
        public void SyncWorld() {
            Stand.SetActive(State.StandBuilt); SetupMarker.SetActive(!State.StandBuilt);
            // Stand customers are drawn as a line of characters by RestaurantController (PhysicalStand).
            Customer.SetActive(false); lastOrder = State.HasOrder;
            GrillFood.SetActive(false);
            HandFood.SetActive(false);
            RecipeGlow.SetActive(State.IsNight && !State.RecipeUnlocked);
            float dusk = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(125, 160, State.Clock));
            if (State.Clock > 220) dusk = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(220, 240, State.Clock));
            Sun.intensity = Mathf.Lerp(1.25f, .24f, dusk);
            Sun.color = Color.Lerp(new Color(1, .91f, .77f), new Color(.52f, .64f, 1), dusk);
            RenderSettings.ambientLight = Color.Lerp(new Color(.66f, .74f, .78f), new Color(.30f, .34f, .52f), dusk);
            Player.View.backgroundColor = Color.Lerp(new Color(.61f, .80f, .83f), new Color(.055f, .075f, .16f), dusk);
            RenderSettings.fogColor = Player.View.backgroundColor;
            if (RenderSettings.skybox) { RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(1.1f, .05f, dusk)); RenderSettings.skybox.SetColor("_SkyTint", Color.Lerp(new Color(.52f, .56f, .6f), new Color(.2f, .25f, .5f), dusk)); }
            foreach (var lamp in Lamps) lamp.intensity = Mathf.Lerp(.2f, 4, dusk);
        }
        // The goal ladder. Truck phase: fire up, stock, open, first customer, first star, save $350, buy The Odd Table.
        // Restaurant phase: furnish, hire, then the city opens up (midnight recipe stash > Zeeb's sauce > raid Gus).
        // Nothing about raids or map secrets shows until you own a restaurant.
        public string Objective {
            get {
                var r = State.Restaurant; int price = RestaurantSites.StarterPrice;
                if (!r.Owned) {
                    if (!State.StandBuilt) return "Fire up Little Flame\nYour food truck is parked in Truck Park. Press E on it ($10).";
                    if (State.Kitchen.Items.Exists(i => Hotbar.IsBag(i) && i.Holder != null && i.Holder.Contains("player"))) return "Unpack your groceries\nCarry the bag into the truck and press E on the shelf to stock it.";
                    string shortage = State.StandShortage();
                    if (State.StandOpen && shortage != "") return "Restock now!\nOut of " + shortage + ". Run to Rose at Milo's cart, then unpack at the truck shelf. Customers are waiting!";
                    if (State.StandOpen || State.StandLastCall) {
                        // Teach the dish that's actually ordered (salad or burger) until you've made each a couple of times.
                        var front = State.StandQueue.Find(o => o.Stage == 1) ?? State.StandQueue.Find(o => o.Stage == 0);
                        if (front != null && (front.Dish == "salad" ? State.StandSalads < 2 : State.StandBurgers < 2))
                            return (State.Served == 0 ? "Serve your first customer" : front.Dish == "salad" ? "They want a salad" : "They want a burger") + "\n" + (front.Dish == "salad"
                                ? "Greens on the cutting board and chop them > clean plate > chopped greens > hand it over."
                                : "Patty on the grill, flip it when golden > clean plate > bun > patty > hand it over.");
                    }
                    if (State.StandLastCall) return "Last call\nNo new customers. Serve the ones still here, then your shift report.";
                    if (!State.StandOpen && shortage != "") return "Stock the truck\nBuy " + shortage + " from Rose at Milo's cart, right beside the truck, then unpack them at the truck shelf.";
                    if (!State.StandOpen && State.Served == 0) return "Open for business\nPress E on the menu board by the window.";
                    string next = r.Stars < 1 ? "Earn your first star" : State.Cash < price ? "Save $" + price + " for The Odd Table" : "Buy The Odd Table";
                    var rep = State.LastStandShift;
                    if (rep != null && !State.StandOpen) return rep.Name + " done!\nServed " + rep.Served + ", earned $" + rep.Earned + (rep.Walked > 0 ? ", " + rep.Walked + " walked out" : "") + ". "
                        + (rep.Name == "Day shift" && State.IsNight ? "Open the night shift at the menu board (busier, +50% pay) or close up and explore." : rep.Name == "Night shift" ? "Rest up; open again in the morning." : "Open the menu board again whenever you're ready.")
                        + "\nNext goal: " + next;
                    if (State.Served == 0) return "Serve your first customer\nThey'll walk up to the window soon.";
                    if (r.Stars < 1) { var g = RestaurantState.StarGoals[1]; return "Earn your first star\nServed " + Math.Min(r.Served, g.served) + "/" + g.served + "   Satisfaction " + r.Satisfaction.ToString("0") + "/" + g.satisfaction + "\nServe fast; walk-outs hurt. Tab: Stars."; }
                    if (State.Cash < price) return "Save $" + price + "\n$" + State.Cash + " / $" + price + " for The Odd Table on Main Street.\nThe truck's star is earned: your next star needs your own restaurant.";
                    return "Buy The Odd Table\nIt's on Main Street. Press E on its sign ($" + price + ").\nYour next star needs a restaurant you can decorate.";
                }
                if (r.Layout.Count < 5) return "Furnish your kitchen\nInside, press B: a pantry, grill, plate rack, assembly station, sink and a table.";
                if (r.Workers.Count == 0) return "Hire your first worker\nPress P > Crew. Feed a resident once and they join your People book; recruit them to cook, serve or wash.";
                if (!State.Knows("midnight")) return State.IsNight ? "Find the midnight recipe\nA rival hides a recipe stash in Rival Alley (purple on the map, M). Grab it. You can retreat." : "Find the midnight recipe\nA rival hides a recipe stash in Rival Alley (purple on the map, M). It opens after dark.";
                if (State.ZeebOrders == 0 && r.Stock("midnight_sauce") == 0) return "Call Zeeb\nPress P: Zeeb sells midnight sauce. Then put the Midnight Burger on your menu.";
                if (State.Raids == null || !State.Raids.Exists(x => x.RivalId == "gus" && x.Wins > 0)) return State.IsNight ? "Raid Greasy Gus\nHis truck is in the vacant lot tonight. Press E on it, pick a rested crew and win his Cyclops Stack." : "Raid Greasy Gus\nGus guards his Cyclops Stack recipe. Rest your crew and visit his truck after dark.";
                if (r.Stars + 1 < RestaurantState.StarGoals.Length) { var g = r.NextStarGoal; return "Earn " + StarText.Words(r.Stars + 1) + "\nServed " + Math.Min(r.Served, g.served) + "/" + g.served + "   Satisfaction " + r.Satisfaction.ToString("0") + "/" + g.satisfaction + (g.ambience > 0 ? "   Ambience " + r.Ambience + "/" + g.ambience : ""); }
                return "Your restaurant, your rules\nB to decorate. Tab to manage service, menu and staff.";
            }
        }
        public void NewGame() {
            State = new GameState(); Player.Teleport(SpawnPoint); Guard.ResetGuard();
            if (Restaurant) Restaurant.RebuildLayout();
            Save(); SetPaused(false); Notify("A new beginning. Little Flame is parked in Truck Park.");
        }
        public void Save() {
            if (SmokeMode || PreviewMode) return;
            SaveTo(SavePath);
        }
        public bool SaveTo(string path) {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(State, true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                SaveStatus = "Progress saved";
                return true;
            } catch (Exception e) { SaveStatus = "Could not save progress"; Debug.LogWarning("Save failed: " + e.Message); return false; }
        }
        void Load() {
            if (!File.Exists(SavePath)) return;
            LoadFrom(SavePath);
        }
        public bool LoadFrom(string path) {
            try {
                var loaded = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                if (loaded == null || loaded.Version < 1 || loaded.Version > GameState.CurrentVersion) throw new InvalidDataException("Unsupported save version");
                if (loaded.Version < 3 && !File.Exists(path + ".pre-physical-v2.bak")) File.Copy(path, path + ".pre-physical-v2.bak");
                if (loaded.Version < 4 && !File.Exists(path + ".pre-recipe-v4.bak")) File.Copy(path, path + ".pre-recipe-v4.bak");
                // v7: The Odd Table is everyone's first restaurant; The Bayside becomes a second restaurant bought later.
                if (loaded.Restaurant != null && loaded.Restaurant.SiteId != null && loaded.Restaurant.SiteId != "oddtable" && !File.Exists(path + ".pre-oddtable-v7.bak")) File.Copy(path, path + ".pre-oddtable-v7.bak");
                loaded.SanitizeAfterLoad(); State = loaded; SaveStatus = "Saved progress loaded";
                return true;
            } catch (Exception e) { SaveStatus = "Save unreadable; starting fresh"; Debug.LogWarning("Load failed: " + e.Message); return false; }
        }
        void OnApplicationQuit() { Save(); }
    }
}
