using System;
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
        public string Notice { get; private set; }
        public string SaveStatus { get; private set; } = "Progress saves automatically";
        float noticeUntil, saveTimer;
        bool wasNight, lastOrder;
        Vector3 customerPosition;
        public string SavePath => Path.Combine(Application.persistentDataPath, "restaurant-city-v1.json");

        void Awake() {
            SmokeMode = Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "--smoke-test" || arg == "--snapshots" || arg.StartsWith("--restaurant-") || arg.StartsWith("--physical-") || arg.StartsWith("--interaction-"));
            if (!SmokeMode) Load(); else State = new GameState();
            State.Version = 4;
            SetPaused(true);
            if (Customer) customerPosition = Customer.transform.position;
        }
        void Start() {
            Restaurant = gameObject.AddComponent<RestaurantController>(); Restaurant.Initialize(this);
            CoOp = gameObject.AddComponent<LocalCoop>(); CoOp.Initialize(this);
            gameObject.AddComponent<PhysicalHud>().Game = this;
            SyncWorld();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0) gameObject.AddComponent<PrototypeSmokeTest>().Game = this;
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg.StartsWith("--restaurant-"))) gameObject.AddComponent<RestaurantAcceptance>().Game = this;
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg.StartsWith("--physical-"))) gameObject.AddComponent<PhysicalAcceptance>().Game = this;
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg.StartsWith("--interaction-"))) gameObject.AddComponent<InteractionAcceptance>().Game = this;
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
        public bool InteractForPlayer(InteractionKind kind, FirstPersonPlayer player) {
            if (kind == InteractionKind.FutureRestaurant) return Restaurant.BuyRestaurant(player);
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
                    success = State.Prepare(); Notify(success ? "Burger prepared. Put it on the grill." : State.Stock == 0 ? "No ingredients. Visit the green supplier across the street." : "Finish or discard your current dish first."); break;
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
        public void HurtPlayer(FirstPersonPlayer player, int amount) {
            player.Health = Mathf.Max(0, (player.PlayerId == 0 ? State.Health : player.Health) - amount);
            if (player.PlayerId == 0) State.Health = Mathf.RoundToInt(player.Health);
            if (player.Health > 0) { Notify("Player " + (player.PlayerId + 1) + " hit! Back out of the alley to escape."); return; }
            if (player.PlayerId == 0) State.Respawn(); else State.Cash = Mathf.Max(0, State.Cash - 10);
            player.Health = 100; player.Teleport(SpawnPoint + Vector3.right * player.PlayerId * 1.5f); Guard.ResetGuard(); Save();
            Notify("Back at your stand. Lost up to $10; your stand and recipes are safe.", 7);
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
            foreach (var lamp in Lamps) lamp.intensity = Mathf.Lerp(.2f, 4, dusk);
        }
        public string Objective {
            get {
                if (State.Restaurant.Owned && State.Restaurant.Layout.Count < 5) return "Furnish your kitchen\nKeep the stand running. Inside, press B to buy a pantry, grill, plate rack, assembly station, sink and a table.";
                if (State.Restaurant.Owned) return "Your restaurant, your rules\nB to decorate inside. Tab to manage service, menu and staff.";
                if (!State.StandBuilt) return "Make it yours\nSet up the coral food stand for $10.";
                if (State.Restaurant.Protein == 0 || State.Restaurant.Produce == 0) return "Stock the kitchen\nBuy patties and buns from Milo's crates across the street.";
                if (!State.StandOpen && State.Served == 0) return "Open for business\nPress E on the sign by your stand to start serving.";
                if (State.Served == 0) return "Your first customer\nPatty on the grill > paper plate > bun > cooked patty > serve.";
                if (!State.RecipeUnlocked) return State.IsNight ? "A recipe after dark\nExplore the marked rival alley. You can retreat." : "Build your reputation\nKeep serving. The alley stash opens at night.";
                if (State.Cash < 150) return "A place of your own\nSell midnight burgers. Save $150 for your future restaurant.";
                return "A place of your own\nBuy the $150 restaurant across the street.";
            }
        }
        public void NewGame() {
            State = new GameState(); Player.Teleport(SpawnPoint); Guard.ResetGuard();
            if (Restaurant) Restaurant.RebuildLayout();
            Save(); SetPaused(false); Notify("A new beginning. Your food stand is just ahead.");
        }
        public void Save() {
            if (SmokeMode) return;
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
                if (loaded == null || loaded.Version < 1 || loaded.Version > 4) throw new InvalidDataException("Unsupported save version");
                if (loaded.Version < 3 && !File.Exists(path + ".pre-physical-v2.bak")) File.Copy(path, path + ".pre-physical-v2.bak");
                if (loaded.Version < 4 && !File.Exists(path + ".pre-recipe-v4.bak")) File.Copy(path, path + ".pre-recipe-v4.bak");
                loaded.SanitizeAfterLoad(); State = loaded; SaveStatus = "Saved progress loaded";
                return true;
            } catch (Exception e) { SaveStatus = "Save unreadable; starting fresh"; Debug.LogWarning("Load failed: " + e.Message); return false; }
        }
        void OnApplicationQuit() { Save(); }
    }
}
