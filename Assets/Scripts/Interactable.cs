using UnityEngine;

namespace RestaurantCity {
    public enum InteractionKind { Supplier, Stand, Prep, Grill, Serve, Bin, Recipe, FutureRestaurant, SupplyProtein, SupplyProduce, StandSign, StandTable, NightStash, Raid }
    public class Interactable : MonoBehaviour {
        public InteractionKind Kind;
        public string Site = "oddtable";   // which restaurant a FutureRestaurant lease sign belongs to
        public int Index;                  // which sidewalk table a StandTable is
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
                case InteractionKind.Recipe: return s.LastStashDay == s.Day ? "Stash emptied tonight. The rival restocks tomorrow night" : !s.IsNight ? "Rival stash opens at night" : !game.Guard.Defeated ? "Defeat the rival before opening the stash" : s.RecipeUnlocked ? "Raid the stash  /  +2 Flux" : "Take the midnight recipe  /  +3 Flux";
                case InteractionKind.SupplyProtein: return "Buy 6 patties  /  $10";
                case InteractionKind.SupplyProduce: return "Buy 6 buns & greens  /  $6";
                case InteractionKind.StandTable: return "Sidewalk table";
                case InteractionKind.NightStash: return "Hidden stash";
                case InteractionKind.Raid: return (Rivals.Get(Site)?.Name ?? "Rival") + "  /  plan a raid";
                default: {
                    var site = RestaurantSites.Get(Site);
                    if (s.Restaurant.Owned) return "Manage your restaurant";
                    return "Lease " + site.Title + "  /  $" + site.Price;
                }
            }
        }
    }
}
