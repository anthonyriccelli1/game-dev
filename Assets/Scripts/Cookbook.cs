using System;
using System.Linq;

namespace RestaurantCity {
    // Where a recipe comes from. Bought recipes need stars (lose the star, lose the dish until you earn it back);
    // base, raided and found recipes are yours to cook once you have them, whatever your stars.
    public enum RecipeSource { Base, Cookbook, Raid, Found }

    public class CookbookEntry {
        public string DishId, Name, Hint, Blurb;
        public RecipeSource Source; public int Stars, Price;
        public bool Ready;   // false = designed but not in the game yet (shown locked, can't be earned yet)
        public bool StarGated => Source == RecipeSource.Cookbook;
    }

    // One cookbook per district (see the project doc "Old Market Cookbook"). Every recipe shows its name; locked
    // ones show a blacked-out picture and a hint about how to get them.
    public static class DistrictCookbook {
        static CookbookEntry E(string id, string name, RecipeSource src, int stars, int price, string hint, string blurb, bool ready) =>
            new CookbookEntry { DishId = id, Name = name, Source = src, Stars = stars, Price = price, Hint = hint, Blurb = blurb, Ready = ready };
        public const string District = "Old Market";
        public static readonly CookbookEntry[] OldMarket = {
            E("burger", "Flats Burger", RecipeSource.Base, 0, 0, "Yours from day one.", "Patty on the grill, onto a bun. The humble burger of the Flats.", true),
            E("salad", "Stoop Salad", RecipeSource.Base, 0, 0, "Yours from day one.", "Chop greens on the board and plate them.", true),
            E("soup", "Planet Soup", RecipeSource.Cookbook, 1, 60, "Buy it here at 1 star.", "Simmer soup veg in the pot and keep stirring or it scorches.", true),
            E("cometdog", "Comet Dog", RecipeSource.Cookbook, 1, 70, "Buy it here at 1 star.", "A sausage trailing a glowing orange sauce tail and star sprinkles. Hot as a comet: cooks fast, burns fast.", false),
            E("twinmoons", "Twin Moons", RecipeSource.Cookbook, 2, 120, "Buy it here at 2 stars. Needs a chrome grill.", "Two patties, each under a slice of cheese glowing like a moon. Both cook at once.", false),
            E("cyclops", "Cyclops Stack", RecipeSource.Raid, 0, 0, "Won by raiding Greasy Gus's food truck.", "One huge fried egg staring up off the toast, bacon for eyelashes. Mornings only.", false),
            E("float", "Moonberry Float", RecipeSource.Raid, 0, 0, "Won by raiding The Tin Diner.", "Soft serve and moonberry soda. Serve it before it melts.", false),
            E("hoard", "Dragon's Hoard", RecipeSource.Raid, 0, 0, "Won by raiding The Gilded Orbit.", "The Gilded Orbit's showpiece: a mini burger, salad and soup piled on a gold tray with cheese coins.", false),
            E("midnight", "Midnight Burger", RecipeSource.Found, 0, 0, "Found in the city after dark. Zeeb sells the sauce.", "A burger with Zeeb's glowing Midnight Sauce. Contraband.", true),
            E("glowshroom", "Glowshroom Melt", RecipeSource.Found, 0, 0, "Something glows in the alley after midnight...", "Chopped glowing mushrooms melted over a patty. Contraband.", false),
        };
        public static CookbookEntry Find(string dish) => Array.Find(OldMarket, e => e.DishId == dish);
        public static int Known(GameState g) => OldMarket.Count(e => g.Knows(e.DishId));
        public static string SourceLabel(RecipeSource s) => s == RecipeSource.Base ? "STARTER" : s == RecipeSource.Cookbook ? "COOKBOOK" : s == RecipeSource.Raid ? "RAID" : "FOUND";
    }

    // Stars as symbols (filled and empty), e.g. 2 of 5.
    public static class StarText {
        public const int Max = 5;
        public static string Of(int stars) => new string('★', Math.Max(0, Math.Min(Max, stars))) + new string('☆', Max - Math.Max(0, Math.Min(Max, stars)));
        public static string Title(int stars) => stars <= 0 ? "a new beginning" : stars == 1 ? "on the map" : stars == 2 ? "neighborhood favorite" : stars == 3 ? "talk of the district" : stars == 4 ? "city landmark" : "legend";
        public static string Words(int stars) => stars == 1 ? "1 star" : stars + " stars";
    }
}
