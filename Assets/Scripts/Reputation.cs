using System;
namespace RestaurantCity {
    // Reputation rank (see claude/world-map.md): every dollar of sales is one reputation point, and each rank opens a district.
    // Stars stay a per-restaurant quality rating; rank is your standing across the whole city.
    // Reputation rank (see claude/world-map.md). Rank measures how well you run things, not how much money you make:
    // happy customers, stars, hidden recipes, rivals and recruits earn reputation, and every rank also needs one keystone goal.
    // Money buys things (transport, tools, decor, properties) but never rank.
    public static class Reputation {
        public static readonly string[] Titles = { "Street Cook", "Line Cook", "Sous Chef", "Head Chef", "Restaurateur", "Mogul" };
        public static readonly int[] Thresholds = { 0, 400, 1200, 2500, 5000, 9000 };
        public static readonly string[] Keystones = { "", "The Odd Table reaches 2 stars", "Beat Captain Krill at the Docks", "Win the Neon Row cook-off", "Any restaurant reaches 4 stars", "Beat the Gilded Orbit flagship" };
        public static readonly string[] KeystoneGoal = { "", "", "beat_krill", "win_cookoff", "", "beat_flagship" };
        // Reputation awards.
        // The street stand pays cash, not reputation: a fast sale earns a token +1, a slow one nothing.
        // Reputation is earned by the restaurant, so The Odd Table alone gets you most of the way to Line Cook,
        // and running a second restaurant is what finishes the climb.
        public const int StandFast = 1;
        public const int HappyCustomer = 3, OkCustomer = 1, LostCustomer = -1, NewStar = 50, HiddenRecipe = 40, NightlyStash = 10, BeatRival = 40, RematchRival = 10, Recruit = 20, NewResident = 10;
        public static bool IsMax(int rank) => rank >= Thresholds.Length - 1;
        public static float Progress(int rep, int rank) {
            if (IsMax(rank)) return 1;
            return Math.Max(0, Math.Min(1, (rep - Thresholds[rank]) / (float)(Thresholds[rank + 1] - Thresholds[rank])));
        }
    }
    public class CityDistrict {
        public string Id, Name, Hex; public int Rank; public float X0, Z0, X1, Z1;
        public bool Contains(float x, float z) => x >= X0 && x <= X1 && z >= Z0 && z <= Z1;
    }
    public class CityPlace { public string Name, Kind; public float X, Z; public CityPlace(string n, string k, float x, float z) { Name = n; Kind = k; X = x; Z = z; } }
    public static class CityDistricts {
        public const float MinX = -220, MaxX = 580, MinZ = -280, MaxZ = 330;
        // Boundary roads belong to neither side (the gaps between rects), so they stay drivable.
        // Order matters where districts share an edge: the first match wins.
        public static readonly CityDistrict[] All = {
            new CityDistrict { Id = "market", Name = "Old Market", Rank = 0, X0 = -220, Z0 = -110, X1 = 160, Z1 = 130, Hex = "C8553D" },
            new CityDistrict { Id = "docks", Name = "The Docks", Rank = 1, X0 = -220, Z0 = -280, X1 = 300, Z1 = -116, Hex = "3AA0B0" },
            new CityDistrict { Id = "neon", Name = "Neon Row", Rank = 2, X0 = 166, Z0 = -120, X1 = 414, Z1 = 130, Hex = "E0479A" },
            new CityDistrict { Id = "greenleaf", Name = "Greenleaf", Rank = 3, X0 = -220, Z0 = 136, X1 = 414, Z1 = 330, Hex = "74B35A" },
            new CityDistrict { Id = "gold", Name = "Gold Coast", Rank = 4, X0 = 426, Z0 = -120, X1 = 580, Z1 = 330, Hex = "E0AD45" },
            new CityDistrict { Id = "nebula", Name = "Little Nebula", Rank = 5, X0 = 360, Z0 = -265, X1 = 540, Z1 = -126, Hex = "8D6AE0" },
        };
        public static CityDistrict At(float x, float z) {
            // The bridge and island belong to Little Nebula; check it before the mainland.
            var nebula = All[5]; if (nebula.Contains(x, z) && x > 300) return nebula;
            foreach (var d in All) if (d.Contains(x, z)) return d;
            return null;
        }
        public static CityDistrict ById(string id) { foreach (var d in All) if (d.Id == id) return d; return null; }
        public static bool Unlocked(CityDistrict d, int rank) => d == null || rank >= d.Rank;
        public static CityDistrict OpenedAt(int rank) { foreach (var d in All) if (d.Rank == rank) return d; return null; }
        public static readonly CityPlace[] Places = {
            new CityPlace("Your stand", "you", 0, 8), new CityPlace("Milo's", "supply", -12, 10), new CityPlace("The Odd Table", "restaurant", -10, -15), new CityPlace("The Bayside", "restaurant", -190, -15),
            new CityPlace("Gilded Orbit", "rival", 19, 22), new CityPlace("Rival Alley", "recipe", 11.6f, 24),
            new CityPlace("Fishmarket", "supply", -100, -130), new CityPlace("The Salty Hatch", "restaurant", 10, -222), new CityPlace("Captain Krill's", "rival", 120, -222), new CityPlace("Container 13", "recipe", 240, -228),
            new CityPlace("Lucky Comet casino", "service", 225, 25), new CityPlace("Burger Baron", "rival", 225, -16), new CityPlace("Night Owl Noodles", "restaurant", 350, -16),
            new CityPlace("Farmers' Co-op", "supply", -100, 150), new CityPlace("Starlite Drive-In", "restaurant", 110, 150), new CityPlace("Grandma Opal's", "recipe", -170, 248), new CityPlace("Mama Grill's", "rival", 330, 248),
            new CityPlace("Bayline Motors", "service", 530, 30), new CityPlace("Terrace 9", "restaurant", 470, 200), new CityPlace("Marchetti Imports", "supply", 540, 250), new CityPlace("Gilded Flagship", "rival", 545, -70),
            new CityPlace("Nebula Bazaar", "supply", 400, -228), new CityPlace("Crater Kitchen", "restaurant", 500, -215), new CityPlace("The Void", "rival", 505, -248),
        };
    }
    // One line of the "where did my reputation come from" breakdown.
    [System.Serializable] public class RepGain { public string Source; public int Amount, Count; }
}
