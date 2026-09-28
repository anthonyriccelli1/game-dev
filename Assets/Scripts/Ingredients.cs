using System;
using System.Collections.Generic;
namespace RestaurantCity {
    // Real ingredients, each stocked and used up separately (replaces the old "protein / produce" totals).
    // Milo sells the everyday ones; secret-recipe ingredients are never sold and come from night stashes.
    public class IngredientDef {
        public string Id, Name, Unit, Category, Source, Recipe, Blurb, Shelf;
        public int PackSize, PackPrice, RequiredStars;
        public float UnitCost => PackSize > 0 ? PackPrice / (float)PackSize : 0;
    }
    [Serializable] public class StockLine { public string Id; public int Count; }
    public static class Ingredients {
        public const string Milo = "milo", Stash = "stash";
        public static readonly string[] Categories = { "Basics", "Produce", "Stove", "Specials" };
        public static readonly IngredientDef[] All = {
            new IngredientDef { Id = "patty", Name = "Raw patties", Unit = "patty", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 10, Shelf = "protein", Blurb = "For burgers. Grill them, don't burn them." },
            new IngredientDef { Id = "bun", Name = "Buns", Unit = "bun", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 4, Shelf = "bun", Blurb = "Soft and toasty. Every burger needs one." },
            new IngredientDef { Id = "greens", Name = "Salad greens", Unit = "head of greens", Category = "Produce", Source = Milo, PackSize = 6, PackPrice = 6, Shelf = "greens", Blurb = "Chop on the board for a Garden galaxy salad." },
            new IngredientDef { Id = "soup_veg", Name = "Soup vegetables", Unit = "bag of soup veg", Category = "Stove", Source = Milo, PackSize = 6, PackPrice = 9, Recipe = "soup", Shelf = "soup", Blurb = "The base of Planet soup. Needs a stove." },
            new IngredientDef { Id = "moonberry", Name = "Moonberries", Unit = "punnet of moonberries", Category = "Specials", Source = Milo, PackSize = 6, PackPrice = 14, Recipe = "dessert", RequiredStars = 2, Blurb = "For Moonberry tart. Milo only sells them to 2-star kitchens." },
            new IngredientDef { Id = "midnight_sauce", Name = "Midnight sauce", Unit = "bottle of midnight sauce", Category = "Specials", Source = Stash, PackSize = 3, PackPrice = 0, Recipe = "midnight", Shelf = "sauce", Blurb = "Never sold. Found in night stashes around the city." },
        };
        public static IngredientDef Get(string id) => Array.Find(All, i => i.Id == id);
        public static IngredientDef ForShelf(string shelf) => Array.Find(All, i => i.Shelf == shelf);
        // What one portion of each dish uses from the pantry.
        public static string[] For(string dish) =>
            dish == "burger" ? new[] { "patty", "bun" } : dish == "salad" ? new[] { "greens" } : dish == "soup" ? new[] { "soup_veg" } :
            dish == "midnight" ? new[] { "patty", "bun", "midnight_sauce" } : dish == "dessert" ? new[] { "moonberry", "moonberry" } : new string[0];
        public static string Name(string id) { var d = Get(id); return d != null ? d.Name.ToLower() : id; }
    }
}
