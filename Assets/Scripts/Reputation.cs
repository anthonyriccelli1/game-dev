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
        public static readonly string[] Keystones = { "", "The Odd Table reaches 2 stars", "Beat Captain Krill at the Docks", "Win the Neon Row cook-off", "Any restaurant reaches 4 stars", "Beat the city's flagship rival" };
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
    // A named place in the city: the one registry the phone map, the goal text and the on-screen goal marker all use.
    // X, Z is where the goal marker points (a door or a counter, not the middle of a building).
    public class CityPlace {
        public string Id, Name, Kind, Where; public float X, Z;
        public CityPlace(string id, string n, string k, string where, float x, float z) { Id = id; Name = n; Kind = k; Where = where; X = x; Z = z; }
        public UnityEngine.Vector3 Point => new UnityEngine.Vector3(X, 0, Z);
    }
    // Saffron Bay is built one small, dense district at a time. Old Market is the dressed 160 m block (CityMap);
    // the other districts keep their place on the reputation ladder and open behind their gates once they exist.
    public static class CityDistricts {
        public const float MinX = -80, MaxX = 80, MinZ = -120, MaxZ = 80;   // v3: down to the harbour promenade
        public static readonly CityDistrict[] All = {
            new CityDistrict { Id = "market", Name = "Old Market", Rank = 0, X0 = -80, Z0 = -120, X1 = 80, Z1 = 80, Hex = "C8553D" },
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
        public static readonly (string name, float z0, float z1)[] Areas = { ("MARKET ROW", 45, 80), ("THE HOME STREET", -5, 45), ("THE FLATS", -100, -5), ("THE HARBOUR", -120, -100) };
        // Streets as x0, z0, x1, z1 (matches CityMap.Roads).
        public static readonly (float x0, float z0, float x1, float z1)[] Roads = { (-80, -5, 80, 5), (-80, 45, 80, 55), (-80, -55, 80, -45), (-80, -110, 80, -100), (-45, -110, -35, 80), (35, -110, 45, 80) };
        public static readonly CityPlace[] Places = {
            new CityPlace("truck", "Little Flame", "you", "your food truck in Truck Park", 0, -62.5f),
            new CityPlace("rose", "Rose's cart", "supply", "Milo's cart right beside Little Flame", -10.5f, -63.4f),
            new CityPlace("milos", "Milo's Market", "supply", "Main Street, north side", -15.5f, 12.6f),
            new CityPlace("oddtable", "The Odd Table", "restaurant", "Main Street, south side", -10, -8.2f),
            new CityPlace("alchemist", "The Alchemist", "rival", "Main Street, north side", 16.25f, 13.2f),
            new CityPlace("market", "Market stalls", "supply", "off North Avenue", 0, 62),
            new CityPlace("gus", "Greasy Gus's truck", "rival", "the vacant lot off South Avenue", 15.2f, -39.4f),
            new CityPlace("tower", "North Avenue fire escape", "recipe", "the tall building on North Avenue", -4.6f, 41.4f),
            new CityPlace("alley", "Graffiti alley", "recipe", "off South Avenue", -22.5f, -33),
            new CityPlace("pawn", "Hock-9's pawn", "service", "East Street", 33.2f, -25),
            new CityPlace("park", "The park", "service", "east end of Main Street", 65, -25),
            new CityPlace("cityhall", "City Hall", "service", "west end of Main Street", -67, 25),
            new CityPlace("busstop", "Bus stop", "service", "Main Street", -30, -7),
            new CityPlace("truckpark", "Truck Park", "service", "south of South Avenue", 0, -80),
            new CityPlace("courts", "Corner courts", "service", "south-east corner", 65, -78),
            new CityPlace("docksgate", "Bridge to The Docks", "gate", "bottom of West Street", -76, -105),
        };
        public static CityPlace Get(string id) => id == null ? null : System.Array.Find(Places, p => p.Id == id);
    }
    // One line of the "where did my reputation come from" breakdown.
    [System.Serializable] public class RepGain { public string Source; public int Amount, Count; }
}
