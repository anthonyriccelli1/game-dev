using System;
using System.IO;
using UnityEngine;

namespace RestaurantCity {
    public class CityGame : MonoBehaviour {
        public GameState State = new GameState();
        public FirstPersonPlayer Player;
        public StreetGuard Guard;
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
        string SavePath => Path.Combine(Application.persistentDataPath, "restaurant-city-v1.json");

        void Awake() {
            SmokeMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            if (!SmokeMode) Load();
            SetPaused(true);
            if (Customer) customerPosition = Customer.transform.position;
        }
        void Start() {
            SyncWorld();
            if (SmokeMode) gameObject.AddComponent<PrototypeSmokeTest>().Game = this;
        }
        void Update() {
            if (Time.unscaledTime > noticeUntil) Notice = "";
            if (!Paused && !SmokeMode) {
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
        public bool Interact(InteractionKind kind) {
            bool success = false;
            switch (kind) {
                case InteractionKind.Supplier:
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
                    success = State.ClaimRecipe(Guard.Defeated);
                    Notify(success ? "MIDNIGHT BURGER unlocked. Every new burger now earns more!" : !State.IsNight ? "Come back after dusk. Watch for the rival." : State.RecipeUnlocked ? "You already know this recipe." : "The rival is guarding the stash. Three spatula hits will stagger them.", 6); break;
                case InteractionKind.FutureRestaurant:
                    Notify(State.Cash >= 150 ? "You've earned your restaurant fund! Interior building is the next milestone." : "A place of your own. Save $150 to reach the prototype goal.", 6); break;
            }
            if (success) Save();
            SyncWorld(); return success;
        }
        public void Hurt(int amount) {
            State.Health = Mathf.Max(0, State.Health - amount);
            if (State.Health > 0) { Notify("Hit! Back out of the alley to escape."); return; }
            State.Respawn(); Player.Teleport(SpawnPoint); Guard.ResetGuard(); Save();
            Notify("Back at your stand. Lost up to $10; your stand and recipes are safe.", 7);
        }
        public void SyncWorld() {
            Stand.SetActive(State.StandBuilt); SetupMarker.SetActive(!State.StandBuilt);
            if (State.HasOrder && !lastOrder) Customer.transform.position = customerPosition + new Vector3(8, 0, -2);
            Customer.SetActive(State.HasOrder);
            if (State.HasOrder) Customer.transform.position = Vector3.MoveTowards(Customer.transform.position, customerPosition, Time.deltaTime * 2.5f);
            lastOrder = State.HasOrder;
            GrillFood.SetActive(State.Food == FoodStage.Cooking);
            HandFood.SetActive(State.Food == FoodStage.Prepared || State.Food == FoodStage.Plated);
            RecipeGlow.SetActive(State.IsNight && !State.RecipeUnlocked);
            float dusk = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(125, 160, State.Clock));
            if (State.Clock > 220) dusk = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(220, 240, State.Clock));
            Sun.intensity = Mathf.Lerp(1.25f, .14f, dusk);
            Sun.color = Color.Lerp(new Color(1, .91f, .77f), new Color(.52f, .64f, 1), dusk);
            RenderSettings.ambientLight = Color.Lerp(new Color(.66f, .74f, .78f), new Color(.20f, .25f, .39f), dusk);
            Player.View.backgroundColor = Color.Lerp(new Color(.61f, .80f, .83f), new Color(.055f, .075f, .16f), dusk);
            RenderSettings.fogColor = Player.View.backgroundColor;
            foreach (var lamp in Lamps) lamp.intensity = Mathf.Lerp(.2f, 4, dusk);
        }
        public string Objective {
            get {
                if (!State.StandBuilt) return "Make it yours\nSet up the coral food stand for $10.";
                if (State.Stock == 0 && State.Food == FoodStage.Empty) return "Stock the kitchen\nBuy ingredients at the green supplier.";
                if (State.Served == 0) return "Your first customer\nPrep > grill > plate > serve.";
                if (!State.RecipeUnlocked) return State.IsNight ? "A recipe after dark\nExplore the marked rival alley. You can retreat." : "Build your reputation\nKeep serving. The alley stash opens at night.";
                if (State.Cash < 150) return "A place of your own\nSell midnight burgers. Save $150 for your future restaurant.";
                return "From a stand to a dream\nYou reached $150! Visit the future restaurant sign.";
            }
        }
        public void NewGame() {
            State = new GameState(); Player.Teleport(SpawnPoint); Guard.ResetGuard();
            Save(); SetPaused(false); Notify("A new beginning. Your food stand is just ahead.");
        }
        public void Save() {
            if (SmokeMode) return;
            try {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(State, true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
                else File.Move(temporary, SavePath);
                SaveStatus = "Progress saved";
            } catch (Exception e) { SaveStatus = "Could not save progress"; Debug.LogWarning("Save failed: " + e.Message); }
        }
        void Load() {
            if (!File.Exists(SavePath)) return;
            try {
                var loaded = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                if (loaded == null || loaded.Version != 1) throw new InvalidDataException("Unsupported save version");
                loaded.SanitizeAfterLoad(); State = loaded; SaveStatus = "Saved progress loaded";
            } catch (Exception e) { SaveStatus = "Save unreadable; starting fresh"; Debug.LogWarning("Load failed: " + e.Message); }
        }
        void OnApplicationQuit() { Save(); }
    }
}
