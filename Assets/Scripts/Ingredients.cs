using System;
using System.Collections.Generic;
namespace RestaurantCity {
    // Real ingredients, each stocked and used up separately (replaces the old "protein / produce" totals).
    // Milo sells the everyday ones; secret-recipe ingredients are never sold and come from night stashes.
    public class IngredientDef {
        public string Id, Name, Unit, Category, Source, Recipe, Blurb, Shelf;
        public int PackSize, PackPrice, RequiredStars;
        public float UnitCost => PackSize > 0 ? PackPrice / (float)PackSize : 0;
        // Recipe-only ingredients (Recipe = one or more dish ids, comma-separated) show up once you know any of those dishes.
        public bool Unlocked(GameState g) => string.IsNullOrEmpty(Recipe) || System.Array.Exists(Recipe.Split(','), g.Knows);
        // Kept cold: lives in the fridge once the restaurant has one (meat, eggs, dairy, produce). Dry goods stay in the pantry.
        public bool Cold => Shelf == "protein" || Shelf == "greens" || Shelf == "soup" || Shelf == "sausage" || Shelf == "egg";
    }
    [Serializable] public class StockLine { public string Id; public int Count; }
    public static class Ingredients {
        public const string Milo = "milo", Stash = "stash";
        public static readonly string[] Categories = { "Basics", "Produce", "Stove", "Specials" };
        public static readonly IngredientDef[] All = {
            new IngredientDef { Id = "patty", Name = "Raw patties", Unit = "patty", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 10, Shelf = "protein", Blurb = "For burgers. Grill them, don't burn them." },
            new IngredientDef { Id = "bun", Name = "Buns", Unit = "bun", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 4, Shelf = "bun", Blurb = "Soft and toasty. Every burger needs one." },
            new IngredientDef { Id = "sausage", Name = "Raw sausages", Unit = "sausage", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 9, Recipe = "cometdog", Shelf = "sausage", Blurb = "For the Comet Dog. They cook fast and burn fast: stay close to the grill." },
            new IngredientDef { Id = "egg", Name = "Eggs", Unit = "egg", Category = "Basics", Source = Milo, PackSize = 6, PackPrice = 7, Recipe = "cyclops,philosopher", Shelf = "egg", Blurb = "For the Cyclops Stack and the Philosopher's Stack. Fry one on the grill next to the patty; it cooks fast." },
            new IngredientDef { Id = "greens", Name = "Salad greens", Unit = "head of greens", Category = "Produce", Source = Milo, PackSize = 6, PackPrice = 6, Shelf = "greens", Blurb = "Chop on the board for a Garden galaxy salad." },
            new IngredientDef { Id = "soup_veg", Name = "Soup vegetables", Unit = "bag of soup veg", Category = "Stove", Source = Milo, PackSize = 6, PackPrice = 9, Recipe = "soup", Shelf = "soup", Blurb = "The base of Planet soup. Needs a stove." },
            new IngredientDef { Id = "moonberry", Name = "Moonberries", Unit = "punnet of moonberries", Category = "Specials", Source = Milo, PackSize = 6, PackPrice = 14, Recipe = "float", RequiredStars = 2, Blurb = "For the Moonberry Float. Milo only sells them to 2-star kitchens." },
            new IngredientDef { Id = "midnight_sauce", Name = "Midnight sauce", Unit = "bottle of midnight sauce", Category = "Specials", Source = Stash, PackSize = 3, PackPrice = 0, Recipe = "midnight", Shelf = "sauce", Blurb = "Never sold in shops. Call Zeeb on your phone; he stashes it around the city after dark." },
        };
        public static IngredientDef Get(string id) => Array.Find(All, i => i.Id == id);
        public static IngredientDef ForShelf(string shelf) => Array.Find(All, i => i.Shelf == shelf);
        public static readonly string[] ColdShelves = { "protein", "greens", "soup", "sausage", "egg" }, DryShelves = { "bun", "sauce" };
        // The street stand only stocks what it cooks: burgers, salad, and the Midnight burger once you know it.
        public static readonly string[] StandShelves = { "protein", "greens", "bun" };   // the truck only sells the starters (burger, salad)
        // What one portion of each dish uses from the pantry.
        public static string[] For(string dish) =>
            dish == "burger" ? new[] { "patty", "bun" } : dish == "salad" ? new[] { "greens" } : dish == "soup" ? new[] { "soup_veg" } :
            dish == "midnight" ? new[] { "patty", "bun", "midnight_sauce" } : dish == "cometdog" ? new[] { "sausage", "bun" } : dish == "cyclops" ? new[] { "patty", "egg", "bun" } : dish == "philosopher" ? new[] { "patty", "egg", "greens", "bun" } : dish == "float" ? new[] { "moonberry" } : dish == "dessert" ? new[] { "moonberry", "moonberry" } : new string[0];
        public static string Name(string id) { var d = Get(id); return d != null ? d.Name.ToLower() : id; }
    }
}
