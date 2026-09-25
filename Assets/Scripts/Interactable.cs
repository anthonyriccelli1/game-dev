using UnityEngine;

namespace RestaurantCity {
    public enum InteractionKind { Supplier, Stand, Prep, Grill, Serve, Bin, Recipe, FutureRestaurant }
    public class Interactable : MonoBehaviour {
        public InteractionKind Kind;
        public string Prompt(CityGame game) {
            var s = game.State;
            switch (Kind) {
                case InteractionKind.Supplier: return s.Cash < 6 && s.Stock == 0 ? "Ask for a starter ingredient" : "Buy 3 ingredients  /  $6";
                case InteractionKind.Stand: return "Set up your food stand  /  $10";
                case InteractionKind.Prep: return s.Food == FoodStage.Empty ? "Prepare " + (s.RecipeUnlocked ? "a midnight burger" : "a burger") + "  /  1 ingredient" : "Your hands are full";
                case InteractionKind.Grill:
                    if (s.Food == FoodStage.Cooking) return s.IsBurnt ? "Pick up burned burger  /  discard at bin" : s.CookSeconds >= 4 ? "Plate your burger  /  ready!" : "Cooking... wait for the green zone";
                    return "Put prepared burger on the grill";
                case InteractionKind.Serve: return s.HasOrder ? "Serve customer  /  $" + s.SalePrice : "Next customer arriving soon";
                case InteractionKind.Bin: return "Discard current dish";
                case InteractionKind.Recipe: return s.RecipeUnlocked ? "Midnight recipe already learned" : !s.IsNight ? "Recipe stash opens at night" : game.Guard.Defeated ? "Learn the midnight burger recipe" : "Defeat the rival before opening the stash";
                default: return s.Restaurant.Owned ? "Manage your restaurant" : "Buy your own restaurant  /  $150";
            }
        }
    }
}
