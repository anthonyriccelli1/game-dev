using System;
using System.Collections.Generic;
namespace RestaurantCity {
    [Serializable] public class StandOrder { public int Id, Type; public string Dish = "burger"; public float Patience, MaxPatience; }
    public static class EncounterRules {
        // Keep pursuit inside the clear corridor, away from the warehouse and stash.
        public static bool InTerritory(float x, float z) => x > 9.35f && x < 13.85f && z > 14 && z < 24;
    }
    public enum FoodStage { Empty, Prepared, Cooking, Plated }
    [Serializable] public class GameState {
        public int Version = 6, Cash = 30, Xp, RankEarned, Stock, Served, Missed, Health = 100, Day = 1, Flux, LastStashDay;
        public bool FluxResearch, FluxIntroduced;
        public KitchenState Kitchen = new KitchenState();
        public RestaurantState Restaurant = new RestaurantState();
        public bool StandBuilt, RecipeUnlocked, HasOrder, StandOpen;
        public int StandCustomerType; public string StandDish = "burger";
        // Up to StandQueueMax customers line up at the stand. HasOrder/Patience/StandDish mirror the front of the line.
        public List<StandOrder> StandQueue = new List<StandOrder>(); public int NextStandOrder = 1, StandClean = 4, StandDirty;
        [NonSerialized] public int Players = 1;
        // Reputation (XP) from every sale; RankUpTo is set for one frame when a sale crosses a rank threshold.
        [NonSerialized] public int RankUpTo = -1;
        // Xp is reputation. It never drops below the floor of the rank you already hold, and a rank needs its keystone goal too.
        public bool BeatAlleyRival; public List<string> Goals = new List<string>();
        public void GainReputation(int amount) {
            if (amount == 0) return;
            Xp = Math.Max(Reputation.Thresholds[RankEarned], Math.Min(999999, Xp + amount)); CheckRankUp();
        }
        public bool KeystoneMet(int rank) {
            if (rank == 1) return Restaurant != null && Restaurant.Stars >= 2;
            if (rank == 4) return Restaurant != null && Restaurant.Stars >= 4;
            var goal = Reputation.KeystoneGoal[rank]; return string.IsNullOrEmpty(goal) || (Goals != null && Goals.Contains(goal));
        }
        public void CheckRankUp() {
            while (!Reputation.IsMax(RankEarned) && Xp >= Reputation.Thresholds[RankEarned + 1] && KeystoneMet(RankEarned + 1)) { RankEarned++; RankUpTo = RankEarned; }
        }
        // Passive income: a recruited worker assigned to the stand cooks, serves and washes on their own.
        public int StandWorkerEarned; [NonSerialized] public string StandWorkerStatus = ""; [NonSerialized] float standWorkTimer;
        // Where the worker is in their cycle, so the stand view can walk them between stations.
        [NonSerialized] public float StandWorkerProgress; [NonSerialized] public bool StandWorkerWashing, StandWorkerActive;
        public const float StandWorkerCut = .4f;
        public WorkerState StandWorker => Restaurant?.Workers?.Find(w => w.Job == StaffJob.Stand);
        void TickStandWorker(float seconds) {
            var w = StandWorker; if (w == null || !StandBuilt) { StandWorkerStatus = ""; return; }
            var def = RestaurantCatalog.Worker(w.Id); string name = def != null ? def.Name : w.Id;
            StandWorkerActive = false;
            if (w.Energy <= 2) { StandWorkerStatus = name + " is exhausted. Set them to Rest."; standWorkTimer = 0; return; }
            // The sign still controls arrivals: closing it lets the worker finish the line, then idle.
            bool washing = StandQueue.Count == 0 || StandClean == 0;
            if (washing && StandDirty == 0) { StandWorkerStatus = name + (StandOpen ? " is waiting for customers." : ": stand is closed."); standWorkTimer = 0; return; }
            var front = StandQueue.Count > 0 ? StandQueue[0] : null;
            bool midnight = front != null && front.Dish == "midnight";
            if (!washing && (Restaurant.Protein < (midnight ? 2 : 1) || Restaurant.Produce < 1)) { StandWorkerStatus = name + " is out of ingredients! Restock at Milo's."; standWorkTimer = 0; return; }
            w.Energy = Math.Max(0, w.Energy - seconds * .5f);
            standWorkTimer += seconds;
            float need = washing ? 6 : def != null && def.Role == StaffJob.Cook ? 12 : 18;
            StandWorkerActive = true; StandWorkerWashing = washing; StandWorkerProgress = standWorkTimer / need;
            StandWorkerStatus = name + (washing ? " is washing plates" : " is cooking a " + (midnight ? "midnight burger" : "burger")) + " (" + (int)(standWorkTimer / need * 100) + "%)";
            if (standWorkTimer < need) return;
            standWorkTimer = 0;
            if (washing) { StandDirty--; StandClean++; return; }
            Restaurant.Protein -= midnight ? 2 : 1; Restaurant.Produce--;
            int earned = (int)Math.Round(KitchenState.StandPrice(this, front.Dish) * (1 - StandWorkerCut));
            Cash += earned; GainReputation(Reputation.OkCustomer); StandWorkerEarned += earned; Served++; w.TasksCompleted++;
            StandClean--; StandDirty++; StandQueue.RemoveAt(0); SyncStandFront();
        }
        public const int StandQueueMax = 3, StandPlates = 4;
        public float StandArrivalSeconds => Players > 1 ? 9 : 13;
        public void SyncStandFront() {
            HasOrder = StandQueue.Count > 0;
            if (!HasOrder) return;
            var f = StandQueue[0]; Patience = f.Patience; StandDish = f.Dish; StandCustomerType = f.Type;
        }
        public float Clock, CookSeconds, Patience;
        public float NextCustomer = 1;
        public bool SignatureDish;
        public FoodStage Food;
        public bool IsNight => Clock >= 150;
        public bool IsBurnt => CookSeconds > 10;
        public int SalePrice => (SignatureDish ? 18 : 12) * (IsNight ? 3 : 2) / 2;
        public bool SetUpStand() {
            if (StandBuilt || Cash < 10) return false;
            Cash -= 10; StandBuilt = true; return true;
        }
        public bool BuyIngredients() {
            if (Cash < (StandBuilt ? 6 : 16) || Stock > 96) return false;
            Cash -= 6; Stock += 3; return true;
        }
        public bool Prepare() {
            if (!StandBuilt || Food != FoodStage.Empty) return false;
            // The stand shares the restaurant pantry once you own it, so you are never stuck with stock in one place.
            if (Stock >= 1) Stock--;
            else if (Restaurant != null && Restaurant.Owned && Restaurant.Protein > 0) Restaurant.Protein--;
            else return false; Food = FoodStage.Prepared; CookSeconds = 0; SignatureDish = RecipeUnlocked; return true;
        }
        public bool UseGrill() {
            if (Food == FoodStage.Prepared) { Food = FoodStage.Cooking; return true; }
            if (Food != FoodStage.Cooking || CookSeconds < 4) return false;
            Food = FoodStage.Plated; return true;
        }
        public bool Serve() {
            if (!HasOrder || Food != FoodStage.Plated || IsBurnt) return false;
            Cash += SalePrice; GainReputation(Reputation.OkCustomer); Served++; Food = FoodStage.Empty; HasOrder = false; NextCustomer = 6; return true;
        }
        public bool ClaimRecipe(bool guardDefeated) {
            // The rival guards his stash every night. First win: the midnight recipe + 3 Flux. After that: +2 Flux per night.
            if (!IsNight || !guardDefeated || LastStashDay == Day) return false;
            GainReputation(RecipeUnlocked ? Reputation.NightlyStash : Reputation.HiddenRecipe);
            LastStashDay = Day; Flux += RecipeUnlocked ? 2 : 3; RecipeUnlocked = true; FluxIntroduced = true; return true;
        }
        public bool RequestHelp() {
            if (!StandBuilt || Cash >= 6 || Stock != 0 || Food != FoodStage.Empty) return false;
            if (Restaurant != null && Restaurant.Owned && Restaurant.Protein > 0) return false;
            Stock = 3; return true;
        }
        public void Discard() { Food = FoodStage.Empty; CookSeconds = 0; }
        public void Respawn() {
            Cash = Math.Max(StandBuilt ? 0 : 10, Cash - 10); Health = 100; Discard(); HasOrder = false; StandQueue?.Clear(); NextCustomer = 8;
        }
        public void SanitizeAfterLoad() {
            // Saves from before reputation existed get credit for what their restaurant already earned.
            // v5 briefly counted dollars as reputation; v6 counts customers, stars and discoveries, so cap the carried-over amount.
            if (Version == 5) Xp = Math.Min(Xp, Reputation.Thresholds[1] - 1);
            Version = 6; Restaurant = Restaurant ?? new RestaurantState(); Restaurant.SanitizeAfterLoad();
            Xp = Math.Max(0, Math.Min(999999, Xp)); RankUpTo = -1; Goals = Goals ?? new List<string>(); RankEarned = Math.Max(0, Math.Min(Reputation.Titles.Length - 1, RankEarned)); CheckRankUp();
            // Old saves kept stand "Stock" separately; it now lives in the one shared pantry.
            if (Stock > 0) { Restaurant.Protein += Stock; Restaurant.Produce += Stock; Stock = 0; }
            Kitchen = Kitchen ?? new KitchenState(); Kitchen.SanitizeAfterLoad(this); Flux = Math.Max(0, Flux);
            if(RecipeUnlocked&&!FluxIntroduced){Flux+=3;FluxIntroduced=true;}
            Cash = Math.Max(StandBuilt ? 0 : 10, Math.Min(999999, Cash)); Stock = Math.Max(0, Math.Min(99, Stock));
            Served = Math.Max(0, Served); Missed = Math.Max(0, Missed); Day = Math.Max(1, Day);
            Clock = float.IsNaN(Clock) || float.IsInfinity(Clock) ? 0 : Math.Max(0, Clock) % 240;
            Health = 100; HasOrder = false; NextCustomer = 2; Discard();
            StandQueue = new List<StandOrder>(); StandDirty = Math.Max(0, Math.Min(StandPlates, StandDirty)); StandClean = StandPlates - StandDirty;
        }
        public void Tick(float seconds) {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Clock += seconds;
            if (Restaurant != null && Restaurant.PendingReputation != 0) { int rep = Restaurant.PendingReputation; Restaurant.PendingReputation = 0; GainReputation(rep); }
            else CheckRankUp();
            while (Clock >= 240) { Clock -= 240; Day++; }
            if (Food == FoodStage.Cooking) CookSeconds += seconds;
            StandQueue = StandQueue ?? new List<StandOrder>();
            foreach (var o in StandQueue) o.Patience -= seconds;
            int walked = StandQueue.RemoveAll(o => o.Patience <= 0); Missed += walked; if (walked > 0) GainReputation(walked * Reputation.LostCustomer);
            if (StandBuilt && StandOpen && StandQueue.Count < StandQueueMax) {
                NextCustomer -= seconds;
                if (NextCustomer <= 0) {
                    int id = NextStandOrder++;
                    float patience = StandQueue.Count == 0 && Served == 0 ? 80 : 60;
                    StandQueue.Add(new StandOrder { Id = id, Type = (id * 3) % 10, Dish = RecipeUnlocked && id % 3 == 0 ? "midnight" : "burger", Patience = patience, MaxPatience = patience });
                    NextCustomer = StandArrivalSeconds + (id % 3) * 1.5f;
                }
            }
            TickStandWorker(seconds);
            SyncStandFront();
        }
    }
}
