using System;
using System.Collections.Generic;
namespace RestaurantCity {
    // Stand guests: Stage 0 = in line for a table, 1 = seated and waiting for food, 2 = eating. Patience only runs before they eat.
    [Serializable] public class StandOrder { public int Id, Type, Stage, Table = -1; public string Dish = "burger", ResidentId = ""; public float Patience, MaxPatience, EatLeft; }
    public static class EncounterRules {
        // Keep pursuit inside the clear corridor, away from the warehouse and stash.
        public static bool InTerritory(float x, float z) => x > 9.35f && x < 13.85f && z > 14 && z < 24;
    }
    public enum FoodStage { Empty, Prepared, Cooking, Plated }
    [Serializable] public class GameState {
        public const int CurrentVersion = 7;
        public int Version = CurrentVersion, Cash = 30, Xp, RankEarned, Stock, Served, Missed, Health = 100, Day = 1, Flux, LastStashDay;
        public bool FluxResearch, FluxIntroduced;
        public KitchenState Kitchen = new KitchenState();
        public RestaurantState Restaurant = new RestaurantState();
        public bool StandBuilt, RecipeUnlocked, HasOrder, StandOpen;
        public int StandCustomerType; public string StandDish = "burger";
        // Up to StandQueueMax customers line up at the stand. HasOrder/Patience/StandDish mirror the front of the line.
        public List<StandOrder> StandQueue = new List<StandOrder>(); public int NextStandOrder = 1, StandClean = StandPlates, StandDirty;
        [NonSerialized] public int Players = 1;
        // Reputation (XP) from every sale; RankUpTo is set for one frame when a sale crosses a rank threshold.
        [NonSerialized] public int RankUpTo = -1;
        // Xp is reputation. It never drops below the floor of the rank you already hold, and a rank needs its keystone goal too.
        public bool BeatAlleyRival; public List<string> Goals = new List<string>();
        public List<RepGain> RepSources = new List<RepGain>();
        // Recipes you can cook. Burger and salad are known from the start; others are found, bought or taught.
        public List<string> KnownRecipes = new List<string> { "burger", "salad" };
        // The People book: residents you've fed at least once. Only they can be recruited (with Flux).
        public List<string> MetResidents = new List<string>();
        public int StarRating => Restaurant != null ? Math.Max(1, Restaurant.Stars) : 1;
        public string PickVisitor(int seed) => ResidentCast.Visitor(seed, IsNight, StarRating, MetResidents).Id;
        public bool HasMet(string id) => MetResidents.Contains(id);
        // Reputation only for first meals in a real restaurant: the stand stays a token-reputation tutorial.
        public bool MeetResident(string id, int reputation = 0) {
            if (!ResidentCast.IsRecruitable(id) || MetResidents.Contains(id)) return false;
            MetResidents.Add(id); if (reputation > 0) GainReputation(reputation, "New residents"); Emit("met:" + id); return true;
        }
        public int LastMiloHelpDay;
        // Moments the presentation layer turns into sound and pop-ups ("served:12:88:4", "walkout:12", "chop:5"...). Never saved.
        [NonSerialized] public List<string> Events = new List<string>();
        public void Emit(string e) { if (Events == null) Events = new List<string>(); Events.Add(e); if (Events.Count > 64) Events.RemoveAt(0); }
        // Zeeb's order (see NightStashes): bottles on the way, where he'll hide them, and what you still owe him.
        // StashTip is raised for one frame when the drop is placed, so the phone can buzz.
        public int DropBottles, StashSpot = -1, ZeebDebt, ZeebOrders; public bool DropPlaced; [NonSerialized] public bool StashTip;
        public bool StashActive => DropBottles > 0 && DropPlaced && StashSpot >= 0;
        void TickStash() {
            if (DropBottles <= 0 || DropPlaced || !IsNight) return;   // he only moves after dark
            DropPlaced = true; StashTip = true;
        }
        public bool OrderFromZeeb(int bottles, out string message) {
            if (!Knows("midnight")) { message = "You don't have anyone to call yet."; return false; }
            if (ZeebDebt > 0) { message = "\"Ayy, you still owe me $" + ZeebDebt + ", dawg. Square up first, then we talk.\""; return false; }
            if (DropBottles > 0) { message = "\"Relax, bruh, your last order's still out there. Go grab it.\""; return false; }
            int deposit = NightStashes.DepositFor(bottles);
            if (Cash < deposit) { message = "\"I need $" + deposit + " up front, that's just business.\""; return false; }
            Cash -= deposit; ZeebDebt = NightStashes.Cost(bottles) - deposit; DropBottles = bottles; DropPlaced = false;
            StashSpot = (ZeebOrders * 3 + Day) % NightStashes.Spots.Length; ZeebOrders++;
            message = "\"Say less. " + bottles + " bottles, $" + deposit + " now, $" + ZeebDebt + " later. I drop after dark, I'll text you where.\"";
            if (IsNight) TickStash();
            return true;
        }
        public bool PayZeeb(out string message) {
            if (ZeebDebt <= 0) { message = "You don't owe Zeeb anything."; return false; }
            int pay = Math.Min(Cash, ZeebDebt); if (pay <= 0) { message = "You're broke. Zeeb can wait... for now."; return false; }
            Cash -= pay; ZeebDebt -= pay;
            message = ZeebDebt == 0 ? "\"Pleasure doing business. Hit my line whenever.\"" : "Paid $" + pay + ". You still owe Zeeb $" + ZeebDebt + ".";
            return true;
        }
        public bool Knows(string dish) => KnownRecipes != null && KnownRecipes.Contains(dish);
        public bool BuyRecipe(string dish, out string message) {
            var offer = Array.Find(RecipeBook.ForSale, f => f.dish == dish);
            if (offer.dish == null) { message = "That recipe isn't for sale."; return false; }
            if (Knows(dish)) { message = "You already know it."; return false; }
            if (RankEarned < offer.rank) { message = "Sold to " + Reputation.Titles[offer.rank] + "s and up."; return false; }
            if (Cash < offer.price) { message = "The recipe costs $" + offer.price + "."; return false; }
            Cash -= offer.price; Learn(dish);
            message = "Learned " + RestaurantCatalog.Dish(dish).Name + "! Add it to your menu, and buy its ingredients from Milo."; return true;
        }
        public void Learn(string dish) { KnownRecipes = KnownRecipes ?? new List<string>(); if (!KnownRecipes.Contains(dish)) KnownRecipes.Add(dish); }
        public void GainReputation(int amount, string source = "Other") {
            if (amount == 0) return;
            int before = Xp;
            Xp = Math.Max(Reputation.Thresholds[RankEarned], Math.Min(999999, Xp + amount)); CheckRankUp();
            RepSources = RepSources ?? new List<RepGain>();
            var line = RepSources.Find(r => r.Source == source);
            if (line == null) RepSources.Add(line = new RepGain { Source = source });
            line.Amount += Xp - before; line.Count++;
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
            // A worker cooks for a seated guest; otherwise clears a sidewalk table or washes the dirty pile.
            StandOrder front = null; foreach (var o in StandQueue) if (o.Stage == 1) { front = o; break; }
            int dirtyTable = StandTableDirty.IndexOf(true);
            bool washing = front == null || StandClean == 0;
            if (washing && StandDirty == 0 && dirtyTable < 0) { StandWorkerStatus = name + (StandOpen ? " is waiting for customers." : ": stand is closed."); standWorkTimer = 0; return; }
            bool midnight = front != null && front.Dish == "midnight";
            if (!washing && !Restaurant.HasFor(front.Dish)) { StandWorkerStatus = name + " is out of ingredients! Restock at Milo's."; standWorkTimer = 0; return; }
            w.Energy = Math.Max(0, w.Energy - seconds * .5f);
            standWorkTimer += seconds;
            float need = washing ? 6 : def != null && def.Role == StaffJob.Cook ? 12 : 18;
            StandWorkerActive = true; StandWorkerWashing = washing; StandWorkerProgress = standWorkTimer / need;
            StandWorkerStatus = name + (washing ? (dirtyTable >= 0 ? " is clearing a table" : " is washing plates") : " is cooking a " + (midnight ? "midnight burger" : "burger")) + " (" + (int)(standWorkTimer / need * 100) + "%)";
            if (standWorkTimer < need) return;
            standWorkTimer = 0;
            if (washing) { if (dirtyTable >= 0) StandTableDirty[dirtyTable] = false; else StandDirty--; StandClean++; return; }
            Restaurant.UseFor(front.Dish);
            int earned = (int)Math.Round(KitchenState.StandPrice(this, front.Dish) * (1 - StandWorkerCut));
            Cash += earned; StandWorkerEarned += earned; Served++; w.TasksCompleted++;
            StandClean--; front.Stage = 2; front.EatLeft = StandEatSeconds; SyncStandFront();
        }
        // Two cafe tables x two chairs = four seats. StandTableDirty and StandOrder.Table are per SEAT (seat / 2 = table).
        public const int StandQueueMax = 5, StandPlates = 6, StandTables = 2, StandSeats = 4;
        public const float StandEatSeconds = 12, StandPatience = 75, StandFirstPatience = 100;
        public List<bool> StandTableDirty = new List<bool> { false, false, false, false };
        // The stand has a lunch rush and a (bigger-paying) night rush: guests arrive twice as fast.
        public bool StandRush => StandOpen && (Clock >= 70 && Clock < 110 || Clock >= 180 && Clock < 215);
        public float StandArrivalSeconds => (Players > 1 ? 14 : 22) * (StandRush ? .5f : 1);
        public int FreeStandTable() {
            for (int t = 0; t < StandSeats; t++) {
                if (StandTableDirty[t]) continue;
                bool taken = false; foreach (var o in StandQueue) if (o.Stage > 0 && o.Table == t) taken = true;
                if (!taken) return t;
            }
            return -1;
        }
        void SeatStandGuests() {
            foreach (var o in StandQueue) { if (o.Stage != 0) continue; int t = FreeStandTable(); if (t < 0) break; o.Stage = 1; o.Table = t; }
        }
        // Guests leaving without the table cycle finishing (respawn, reload): an eating guest's plate goes to the dirty pile.
        public void ClearStandGuests() {
            if (StandQueue == null) { StandQueue = new List<StandOrder>(); return; }
            foreach (var o in StandQueue) if (o.Stage == 2) StandDirty++;
            StandQueue.Clear();
        }
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
        // A full day/night cycle is 240 clock units. DayLengthSeconds sets how long that takes in real time
        // (night is the last 90 units, 37.5% of the day). Cooking, patience and arrivals still run in real seconds.
        public static float DayLengthSeconds = 720;
        public static float ClockRate => 240f / Math.Max(1f, DayLengthSeconds);
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
            else if (Restaurant != null && Restaurant.Owned && Restaurant.UseStock("patty")) { }
            else return false; Food = FoodStage.Prepared; CookSeconds = 0; SignatureDish = RecipeUnlocked; return true;
        }
        public bool UseGrill() {
            if (Food == FoodStage.Prepared) { Food = FoodStage.Cooking; return true; }
            if (Food != FoodStage.Cooking || CookSeconds < 4) return false;
            Food = FoodStage.Plated; return true;
        }
        public bool Serve() {
            if (!HasOrder || Food != FoodStage.Plated || IsBurnt) return false;
            Cash += SalePrice; Served++; Food = FoodStage.Empty; HasOrder = false; NextCustomer = 6; return true;
        }
        public bool ClaimRecipe(bool guardDefeated) {
            // The rival guards his stash every night. First win: the midnight recipe + 3 Flux. After that: +2 Flux per night.
            if (!IsNight || !guardDefeated || LastStashDay == Day) return false;
            GainReputation(RecipeUnlocked ? Reputation.NightlyStash : Reputation.HiddenRecipe, RecipeUnlocked ? "Rival stash raids" : "Midnight recipe");
            LastStashDay = Day; Flux += RecipeUnlocked ? 2 : 3; RecipeUnlocked = true; FluxIntroduced = true; Learn("midnight");
            Restaurant?.AddStock("midnight_sauce", 3);   // the stash holds the sauce, not just the recipe
            return true;
        }
        public bool RequestHelp() {
            if (!StandBuilt || Cash >= 6 || Stock != 0 || Food != FoodStage.Empty) return false;
            if (Restaurant != null && Restaurant.Owned && Restaurant.Stock("patty") > 0) return false;
            Stock = 3; return true;
        }
        public void Discard() { Food = FoodStage.Empty; CookSeconds = 0; }
        public void Respawn() {
            Cash = Math.Max(StandBuilt ? 0 : 10, Cash - 10); Health = 100; Discard(); HasOrder = false; ClearStandGuests(); NextCustomer = 8;
        }
        public void SanitizeAfterLoad() {
            // Saves from before reputation existed get credit for what their restaurant already earned.
            // v5 briefly counted dollars as reputation; v6 counts customers, stars and discoveries, so cap the carried-over amount.
            if (Version == 5) Xp = Math.Min(Xp, Reputation.Thresholds[1] - 1);
            bool preIngredients = Version < 7;
            Version = CurrentVersion; Restaurant = Restaurant ?? new RestaurantState(); Restaurant.SanitizeAfterLoad();
            KnownRecipes = KnownRecipes ?? new List<string>(); Learn("burger"); Learn("salad"); if (RecipeUnlocked) Learn("midnight");
            if (preIngredients && RecipeUnlocked) Restaurant.AddStock("midnight_sauce", 3);
            RepSources = RepSources ?? new List<RepGain>();
            Xp = Math.Max(0, Math.Min(999999, Xp)); RankUpTo = -1; Goals = Goals ?? new List<string>(); RankEarned = Math.Max(0, Math.Min(Reputation.Titles.Length - 1, RankEarned)); CheckRankUp(); Restaurant.PlayerRank = RankEarned;
            // Old saves kept stand "Stock" separately; it now lives in the one shared pantry.
            if (Stock > 0) { Restaurant.AddStock("patty", Stock); Restaurant.AddStock("bun", Stock); Stock = 0; }
            Kitchen = Kitchen ?? new KitchenState(); Kitchen.SanitizeAfterLoad(this); Flux = Math.Max(0, Flux); MetResidents = MetResidents ?? new List<string>();
            if(RecipeUnlocked&&!FluxIntroduced){Flux+=3;FluxIntroduced=true;}
            Cash = Math.Max(StandBuilt ? 0 : 10, Math.Min(999999, Cash)); Stock = Math.Max(0, Math.Min(99, Stock));
            Served = Math.Max(0, Served); Missed = Math.Max(0, Missed); Day = Math.Max(1, Day);
            Clock = float.IsNaN(Clock) || float.IsInfinity(Clock) ? 0 : Math.Max(0, Clock) % 240;
            Health = 100; HasOrder = false; NextCustomer = 2; Discard();
            // Plates left on the sidewalk tables go to the dirty pile, so no plate is ever lost across a reload.
            if (StandTableDirty == null || StandTableDirty.Count != StandSeats) StandTableDirty = new List<bool> { false, false, false, false };
            for (int t = 0; t < StandSeats; t++) if (StandTableDirty[t]) { StandDirty++; StandTableDirty[t] = false; }
            StandQueue = new List<StandOrder>(); StandDirty = Math.Max(0, Math.Min(StandPlates, StandDirty)); StandClean = StandPlates - StandDirty;
        }
        public void Tick(float seconds) {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Clock += seconds * ClockRate;
            if (Restaurant != null) Restaurant.PlayerRank = RankEarned;
            if (Restaurant != null && Restaurant.PendingRep != null && Restaurant.PendingRep.Count > 0) { foreach (var g in Restaurant.PendingRep) GainReputation(g.Amount, g.Source); Restaurant.PendingRep.Clear(); }
            else CheckRankUp();
            while (Clock >= 240) { Clock -= 240; Day++; }
            if (Food == FoodStage.Cooking) CookSeconds += seconds;
            StandQueue = StandQueue ?? new List<StandOrder>();
            if (StandTableDirty == null || StandTableDirty.Count != StandSeats) StandTableDirty = new List<bool> { false, false, false, false };
            foreach (var o in StandQueue) {
                if (o.Stage < 2) o.Patience -= seconds;
                else { o.EatLeft -= seconds; if (o.EatLeft <= 0 && o.Table >= 0) StandTableDirty[o.Table] = true; }
            }
            StandQueue.RemoveAll(o => o.Stage == 2 && o.EatLeft <= 0);   // finished: they leave the dirty plate on the table
            int walked = StandQueue.RemoveAll(o => o.Stage < 2 && o.Patience <= 0); if (walked > 0) Emit("stand_walkout"); Missed += walked; if (walked > 0) GainReputation(walked * Reputation.LostCustomer, "Stand walk-outs");
            if (StandBuilt && StandOpen && StandQueue.Count < StandQueueMax) {
                NextCustomer -= seconds;
                if (NextCustomer <= 0) {
                    int id = NextStandOrder++;
                    float patience = StandQueue.Count == 0 && Served == 0 ? StandFirstPatience : StandPatience;
                    StandQueue.Add(new StandOrder { Id = id, Type = (id * 3) % 10, ResidentId = PickVisitor(id * 7 + Day * 131), Dish = Knows("midnight") && id % 4 == 0 ? "midnight" : id % 3 == 1 ? "salad" : "burger", Patience = patience, MaxPatience = patience });
                    NextCustomer = StandArrivalSeconds + (id % 3) * 1.5f;
                }
            }
            TickStash();
            SeatStandGuests();
            TickStandWorker(seconds);
            SeatStandGuests();
            SyncStandFront();
        }
    }
}
