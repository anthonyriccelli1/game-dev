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
        public bool Built => X1 > X0;   // later districts are planned (rank ladder) but not on the map yet
        public bool Contains(float x, float z) => Built && x >= X0 && x <= X1 && z >= Z0 && z <= Z1;
    }
    public class CityPlace { public string Name, Kind; public float X, Z; public CityPlace(string n, string k, float x, float z) { Name = n; Kind = k; X = x; Z = z; } }
    // Saffron Bay is built one small, dense district at a time. Old Market is the dressed 160 m block (CityMap);
    // the other districts keep their place on the reputation ladder and open behind their gates once they exist.
    public static class CityDistricts {
        public const float MinX = -80, MaxX = 80, MinZ = -80, MaxZ = 80;
        public static readonly CityDistrict[] All = {
            new CityDistrict { Id = "market", Name = "Old Market", Rank = 0, X0 = -80, Z0 = -80, X1 = 80, Z1 = 80, Hex = "C8553D" },
            new CityDistrict { Id = "docks", Name = "The Docks", Rank = 1, Hex = "3AA0B0" },
            new CityDistrict { Id = "neon", Name = "Neon Row", Rank = 2, Hex = "E0479A" },
            new CityDistrict { Id = "greenleaf", Name = "Greenleaf", Rank = 3, Hex = "74B35A" },
            new CityDistrict { Id = "gold", Name = "Gold Coast", Rank = 4, Hex = "E0AD45" },
            new CityDistrict { Id = "nebula", Name = "Little Nebula", Rank = 5, Hex = "8D6AE0" },
        };
        public static CityDistrict At(float x, float z) { foreach (var d in All) if (d.Contains(x, z)) return d; return null; }
        public static CityDistrict ById(string id) { foreach (var d in All) if (d.Id == id) return d; return null; }
        public static bool Unlocked(CityDistrict d, int rank) => d == null || rank >= d.Rank;
        public static CityDistrict OpenedAt(int rank) { foreach (var d in All) if (d.Rank == rank) return d; return null; }
        // Old Market's three areas, north to south (drawn on the phone map).
        public static readonly (string name, float z0, float z1)[] Areas = { ("MARKET ROW", 45, 80), ("THE HOME STREET", -5, 45), ("THE FLATS", -80, -5) };
        // Streets as x0, z0, x1, z1 (matches CityMap.Roads).
        public static readonly (float x0, float z0, float x1, float z1)[] Roads = { (-80, -5, 80, 5), (-80, 45, 80, 55), (-80, -55, 80, -45), (-45, -80, -35, 80), (35, -80, 45, 80) };
        public static readonly CityPlace[] Places = {
            new CityPlace("Your stand", "you", 0, 8), new CityPlace("Milo's", "supply", -12, 10), new CityPlace("The Odd Table", "restaurant", -10, -15),
            new CityPlace("Gilded Orbit", "rival", 19, 22), new CityPlace("Rival Alley", "recipe", 11.6f, 24),
            new CityPlace("The Tin Diner", "rival", -22, 38), new CityPlace("Market stalls", "supply", 0, 64),
            new CityPlace("Greasy Gus's truck", "rival", 17, -31), new CityPlace("Graffiti alley", "recipe", -22.5f, -33),
            new CityPlace("The park", "service", 65, -25), new CityPlace("City Hall", "service", -67, 25), new CityPlace("Bus stop", "service", -30, -7),
            new CityPlace("Gate to The Docks", "gate", -40, -76),
        };
    }
    // One line of the "where did my reputation come from" breakdown.
    [System.Serializable] public class RepGain { public string Source; public int Amount, Count; }
}
