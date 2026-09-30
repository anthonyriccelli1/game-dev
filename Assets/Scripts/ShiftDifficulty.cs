using System;
namespace RestaurantCity {
    // Shift difficulty is ADAPTIVE (2026-09-30): "Heat" goes up one after a shift you kept up with (few walk-outs)
    // and down one after a shift where many guests left, so a struggling kitchen gets breathing room instead of an
    // ever-growing crowd. Stars raise both the floor and the ceiling: better restaurants draw bigger crowds.
    // All tuning lives here so it can be balanced in one place.
    public static class ShiftDifficulty {
        public static int HeatCap(RestaurantState r) => 3 + Math.Max(0, r.Stars) * 3;
        public static int Heat(RestaurantState r) => r.Heat < 0 ? Math.Min(r.ShiftsRun, 2) : r.Heat;   // older saves start near the bottom
        public static int Level(RestaurantState r) => r == null ? 0 : Math.Max(0, Heat(r) + r.Stars);
        // Shift length in seconds (service closes to new guests when it's over).
        public static float Length(int level) => Math.Min(240f, 130f + level * 10f);
        // Guests who will arrive this shift (night brings a bigger crowd).
        public static int Guests(int level, bool night) => (int)Math.Round(Math.Min(26f, 6f + level * 1.6f) * (night ? 1.25f : 1f));
        // Multiplies every guest's base patience. Floors at 70%.
        public static float PatienceScale(int level) => Math.Max(.7f, 1f - level * .03f);
        // Chance an arrival brings a friend (two guests at once) from level 3.
        public static float PairChance(int level) => level < 3 ? 0f : Math.Min(.4f, .08f + level * .03f);
        public static int QueueMax(int level) => Math.Min(6, 3 + level / 3);
        public static string Describe(int level, bool night) =>
            Guests(level, night) + " guests over " + (int)Length(level) + "s, patience " + (int)Math.Round(PatienceScale(level) * 100) + "%" + (PairChance(level) > 0 ? ", some in pairs" : "");
        // After a shift: returns the line for the report.
        public static string AfterShift(RestaurantState r, int served, int lost) {
            int heat = Heat(r), total = served + lost; float lostShare = total == 0 ? 0 : lost / (float)total;
            string why;
            if (lostShare > .3f) { heat = Math.Max(0, heat - 1); why = "Too many guests walked out, so word is the kitchen's slow: the next shift will be calmer."; }
            else if (lostShare <= .1f && heat < HeatCap(r)) { heat++; why = "You kept up! Word is spreading: the next shift will be busier."; }
            else if (lostShare <= .1f) why = "You kept up, and that's as busy as a " + StarText.Words(r.Stars) + " restaurant gets. Earn a star to draw bigger crowds.";
            else why = "A few guests left. The next shift stays about the same.";
            r.Heat = heat;
            return why + " Next: " + Describe(Level(r), false) + ".";
        }
    }
}
