using System;
namespace RestaurantCity {
    public static class EncounterRules {
        // Keep pursuit inside the clear corridor, away from the warehouse and stash.
        public static bool InTerritory(float x, float z) => x > 9.35f && x < 13.85f && z > 14 && z < 24;
    }
    public enum FoodStage { Empty, Prepared, Cooking, Plated }
    [Serializable] public class GameState {
        public int Version = 3, Cash = 30, Stock, Served, Missed, Health = 100, Day = 1, Flux;
        public bool FluxResearch, FluxIntroduced;
        public KitchenState Kitchen = new KitchenState();
        public RestaurantState Restaurant = new RestaurantState();
        public bool StandBuilt, RecipeUnlocked, HasOrder;
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
            if (!StandBuilt || Stock < 1 || Food != FoodStage.Empty) return false;
            Stock--; Food = FoodStage.Prepared; CookSeconds = 0; SignatureDish = RecipeUnlocked; return true;
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
            if (!IsNight || !guardDefeated || RecipeUnlocked) return false;
            RecipeUnlocked = true; Flux += 3; FluxIntroduced=true; return true;
        }
        public bool RequestHelp() {
            if (!StandBuilt || Cash >= 6 || Stock != 0 || Food != FoodStage.Empty) return false;
            Stock = 1; return true;
        }
        public void Discard() { Food = FoodStage.Empty; CookSeconds = 0; }
        public void Respawn() {
            Cash = Math.Max(StandBuilt ? 0 : 10, Cash - 10); Health = 100; Discard(); HasOrder = false; NextCustomer = 8;
        }
        public void SanitizeAfterLoad() {
            Version = 3; Restaurant = Restaurant ?? new RestaurantState(); Restaurant.SanitizeAfterLoad();
            Kitchen = Kitchen ?? new KitchenState(); Kitchen.SanitizeAfterLoad(this); Flux = Math.Max(0, Flux);
            if(RecipeUnlocked&&!FluxIntroduced){Flux+=3;FluxIntroduced=true;}
            Cash = Math.Max(StandBuilt ? 0 : 10, Math.Min(999999, Cash)); Stock = Math.Max(0, Math.Min(99, Stock));
            Served = Math.Max(0, Served); Missed = Math.Max(0, Missed); Day = Math.Max(1, Day);
            Clock = float.IsNaN(Clock) || float.IsInfinity(Clock) ? 0 : Math.Max(0, Clock) % 240;
            Health = 100; HasOrder = false; NextCustomer = 2; Discard();
        }
        public void Tick(float seconds) {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Clock += seconds;
            while (Clock >= 240) { Clock -= 240; Day++; }
            if (Food == FoodStage.Cooking) CookSeconds += seconds;
            if (Restaurant != null && Restaurant.Open) { HasOrder = false; return; }
            if (HasOrder) {
                Patience -= seconds;
                if (Patience <= 0) { HasOrder = false; Missed++; NextCustomer = 6; }
            } else if (StandBuilt) {
                NextCustomer -= seconds;
                if (NextCustomer <= 0) { HasOrder = true; Patience = 65; }
            }
        }
    }
}
