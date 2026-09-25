using System;
namespace RestaurantCity {
    // Reputation rank (see claude/world-map.md): every dollar of sales is one reputation point, and each rank opens a district.
    // Stars stay a per-restaurant quality rating; rank is your standing across the whole city.
    public static class Reputation {
        public static readonly string[] Titles = { "Street Cook", "Line Cook", "Sous Chef", "Head Chef", "Restaurateur", "Mogul" };
        public static readonly int[] Thresholds = { 0, 300, 1000, 2400, 5000, 9000 };
        public static int Rank(int xp) { int r = 0; for (int i = 1; i < Thresholds.Length; i++) if (xp >= Thresholds[i]) r = i; return r; }
        public static string Title(int xp) => Titles[Rank(xp)];
        public static bool IsMax(int xp) => Rank(xp) == Thresholds.Length - 1;
        public static int NextThreshold(int xp) { int r = Rank(xp); return r + 1 < Thresholds.Length ? Thresholds[r + 1] : Thresholds[r]; }
        public static float Progress(int xp) {
            int r = Rank(xp); if (r + 1 >= Thresholds.Length) return 1;
            return Math.Max(0, Math.Min(1, (xp - Thresholds[r]) / (float)(Thresholds[r + 1] - Thresholds[r])));
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
        public static bool Unlocked(CityDistrict d, int xp) => d == null || Reputation.Rank(xp) >= d.Rank;
        public static CityDistrict OpenedAt(int rank) { foreach (var d in All) if (d.Rank == rank) return d; return null; }
        public static readonly CityPlace[] Places = {
            new CityPlace("Your stand", "you", 0, 8), new CityPlace("Milo's", "supply", -12, 10), new CityPlace("The Odd Table", "restaurant", -10, -15),
            new CityPlace("Gilded Orbit", "rival", 19, 22), new CityPlace("Rival Alley", "recipe", 11.6f, 24),
            new CityPlace("Fishmarket", "supply", -100, -130), new CityPlace("The Salty Hatch", "restaurant", 10, -222), new CityPlace("Captain Krill's", "rival", 120, -222), new CityPlace("Container 13", "recipe", 240, -228),
            new CityPlace("Lucky Comet casino", "service", 225, 25), new CityPlace("Burger Baron", "rival", 225, -16), new CityPlace("Night Owl Noodles", "restaurant", 350, -16),
            new CityPlace("Farmers' Co-op", "supply", -100, 150), new CityPlace("Starlite Drive-In", "restaurant", 110, 150), new CityPlace("Grandma Opal's", "recipe", -170, 248), new CityPlace("Mama Grill's", "rival", 330, 248),
            new CityPlace("Bayline Motors", "service", 530, 30), new CityPlace("Terrace 9", "restaurant", 470, 200), new CityPlace("Marchetti Imports", "supply", 540, 250), new CityPlace("Gilded Flagship", "rival", 545, -70),
            new CityPlace("Nebula Bazaar", "supply", 400, -228), new CityPlace("Crater Kitchen", "restaurant", 500, -215), new CityPlace("The Void", "rival", 505, -248),
        };
    }
}
