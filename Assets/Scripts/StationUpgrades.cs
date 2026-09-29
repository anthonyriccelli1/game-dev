using System;
namespace RestaurantCity {
    // Zombie Cafe-style equipment levels. Every upgradeable station starts at Level 1 (grimy Flats hand-me-down),
    // can be upgraded in place to Level 2 (clean chrome) and Level 3 (premium). Each level makes that station
    // genuinely better at its job. Looks: StationLooks.ApplyLevel. Rules only here (no Unity).
    public static class StationUpgrades {
        public const int MaxLevel = 3;
        static readonly string[] upgradeable = { "grill", "oven", "stove", "sink", "prep_bench", "plate_rack", "pantry", "assembly", "counter", "trash", "fridge" };
        public static bool CanUpgrade(string catalogId) => Array.IndexOf(upgradeable, catalogId) >= 0;
        public static int Cost(CatalogItem c, int toLevel) => c == null ? 0 : toLevel <= 2 ? c.Price * 2 : c.Price * 5;
        public static int StarsNeeded(int toLevel) => toLevel >= 2 ? 2 : 1;
        // Level 3 (premium, lit-up) gear arrives with the Docks: you need the Line Cook reputation rank.
        public static int RankNeeded(int toLevel) => toLevel >= 3 ? 1 : 0;
        // Pantry levels hold more stock; cleaner, shinier kitchen gear adds a little ambience.
        public static float StockScale(int level) => level >= 3 ? 2f : level == 2 ? 1.5f : 1f;
        public static int AmbienceBonus(int level) => Math.Max(0, level - 1);
        public static string LevelName(int level) => level <= 1 ? "Level 1 (beat-up)" : level == 2 ? "Level 2 (chrome)" : "Level 3 (premium)";

        // Grill/oven: seconds until cooked, and until it burns (higher levels cook faster and forgive more).
        public static float CookSeconds(string id, int level) => (id == "oven" ? 6f : 8f) * (level >= 3 ? .55f : level == 2 ? .75f : 1f);
        public static float BurnSeconds(int level) => level >= 3 ? 36f : level == 2 ? 30f : 24f;
        // Sink and prep: how long one plate / one ingredient takes (multiplier on the base time).
        public static float WorkScale(int level) => level >= 3 ? .45f : level == 2 ? .7f : 1f;
        // Stove: soup simmers faster and scorches slower.
        public static float SoupSpeed(int level) => level >= 3 ? 1.8f : level == 2 ? 1.35f : 1f;
        // Plate rack: plates it adds to the restaurant.
        public static int Plates(int level) => level >= 3 ? 8 : level == 2 ? 6 : 4;

        public static string Effect(string id, int level) {
            switch (id) {
                case "grill": case "oven": return "Cooks in " + CookSeconds(id, level).ToString("0.#") + "s, burns after " + BurnSeconds(level) + "s";
                case "sink": return "Washes a plate in " + (KitchenState.WashSeconds * WorkScale(level)).ToString("0.#") + "s";
                case "prep_bench": return "Chops/prepares " + (int)Math.Round(100 / WorkScale(level)) + "% speed";
                case "stove": return "Soup simmers at " + (int)Math.Round(SoupSpeed(level) * 100) + "% speed";
                case "plate_rack": return "Holds " + Plates(level) + " plates";
                case "pantry": return "Holds " + (int)Math.Round(StockScale(level) * 100) + "% stock";
                case "assembly": case "counter": case "trash": case "fridge": return level <= 1 ? "Grimy but working" : "Spotless: +" + AmbienceBonus(level) + " ambience";
            }
            return "";
        }
    }
}
