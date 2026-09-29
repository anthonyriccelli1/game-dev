using System;
namespace RestaurantCity {
    // PlateUp-style escalation: every shift you complete at a restaurant (and every star you earn) makes the next one
    // busier and less patient. Level 0 is a gentle first shift; around level 10 you need good layout, gear and staff.
    // All tuning lives here so it can be balanced in one place.
    public static class ShiftDifficulty {
        public static int Level(RestaurantState r) => r == null ? 0 : Math.Max(0, r.ShiftsRun + (r.Stars - 1) * 2);
        // Shift length in seconds (service closes to new guests when it's over).
        public static float Length(int level) => Math.Min(240f, 120f + level * 10f);
        // Guests who will arrive this shift (night brings a bigger crowd).
        public static int Guests(int level, bool night) => (int)Math.Round(Math.Min(34f, 7f + level * 2.2f) * (night ? 1.25f : 1f));
        // Multiplies every guest's base patience. Floors at 55%.
        public static float PatienceScale(int level) => Math.Max(.55f, 1f - level * .04f);
        // Chance an arrival brings a friend (two guests at once) from level 2.
        public static float PairChance(int level) => level < 2 ? 0f : Math.Min(.45f, .1f + level * .035f);
        public static int QueueMax(int level) => Math.Min(6, 3 + level / 3);
        public static string Describe(int level, bool night) =>
            Guests(level, night) + " guests over " + (int)Length(level) + "s, patience " + (int)Math.Round(PatienceScale(level) * 100) + "%" + (PairChance(level) > 0 ? ", some in pairs" : "");
    }
}
