using System.Collections.Generic;
namespace RestaurantCity {
    // Every resident (and every legacy recruit) has four stats from 1 to 5. No perks: the stats are the whole story
    // (the Perk field stays for old code paths and is always None).
    //   Cooking: speed at food stations.   Speed: walking, and washing at the sink.
    //   Stamina: how slowly energy drains.  Brawn: damage in raids (and raid health with Stamina).
    // Commons total 10 points, uncommons 12, rares 14 (PhysicalAcceptance checks it).
    public enum Perk { None, Sprinter, QuickHands, Tireless, NightOwl, Brawler, Tough, Rally, Steady }
    public struct ResidentStats {
        public int Cooking, Speed, Stamina, Brawn; public Perk Perk;
        public ResidentStats(int cooking, int speed, int stamina, int brawn, Perk perk) { Cooking = cooking; Speed = speed; Stamina = stamina; Brawn = brawn; Perk = perk; }
        public int Total => Cooking + Speed + Stamina + Brawn;
    }
    public static class StaffStats {
        public static readonly ResidentStats Default = new ResidentStats(2, 2, 2, 2, Perk.None);
        static readonly Dictionary<string, ResidentStats> table = new Dictionary<string, ResidentStats> {
            // Old Market commons
            { "003_Jimmy", new ResidentStats(1, 4, 3, 2, Perk.None) },
            { "006_Cappy", new ResidentStats(2, 2, 4, 2, Perk.None) },
            { "208_TripoConstruction", new ResidentStats(3, 1, 2, 4, Perk.None) },   // Buck
            { "209_TripoFootball", new ResidentStats(1, 4, 2, 3, Perk.None) },     // Blitz
            { "070_Robert", new ResidentStats(4, 2, 2, 2, Perk.None) },
            { "012_Chill", new ResidentStats(2, 2, 4, 2, Perk.None) },
            { "128_RandomBoi", new ResidentStats(2, 3, 2, 3, Perk.None) },
            { "038_Kate", new ResidentStats(4, 2, 3, 1, Perk.None) },
            { "053_Erika", new ResidentStats(2, 4, 3, 1, Perk.None) },
            { "207_TripoGothGirl", new ResidentStats(2, 3, 3, 2, Perk.None) },     // Raven
            { "206_TripoCheerleader", new ResidentStats(1, 4, 2, 3, Perk.None) },   // Pepper: fast on the floor, fires up a raid crew
            // uncommons
            { "071_LilBro", new ResidentStats(2, 4, 3, 3, Perk.None) },
            { "091_BigBro_a", new ResidentStats(3, 2, 3, 4, Perk.None) },
            { "074_Baldman", new ResidentStats(2, 3, 3, 4, Perk.None) },
            { "210_TripoClown", new ResidentStats(3, 4, 3, 2, Perk.None) },           // Bonkers
            { "054_Lydia", new ResidentStats(5, 2, 3, 2, Perk.None) },
            { "136_SlugPerson", new ResidentStats(3, 1, 5, 3, Perk.None) },
            { "044_Zombie", new ResidentStats(3, 1, 5, 3, Perk.None) },
            // rares
            { "139_CoolHydrant", new ResidentStats(3, 3, 5, 3, Perk.None) },
            { "146_CoolTrash", new ResidentStats(2, 3, 5, 4, Perk.None) },
            { "201_TripoAlien", new ResidentStats(2, 5, 4, 3, Perk.None) },
            { "204_TripoReaper", new ResidentStats(2, 3, 4, 5, Perk.None) },   // Grim: the best brawler in Old Market
            { "211_TripoPumpkin", new ResidentStats(4, 2, 5, 3, Perk.None) },   // Jack
            { "212_TripoFrank", new ResidentStats(3, 1, 5, 5, Perk.None) },     // Frank: slow, tireless, hits like a truck
            { "205_TripoVampire", new ResidentStats(3, 5, 3, 3, Perk.None) },
            // legacy special recruits
            { "ember", new ResidentStats(4, 2, 4, 2, Perk.None) },
            { "moss", new ResidentStats(2, 4, 3, 1, Perk.None) },
            { "velvet", new ResidentStats(2, 5, 3, 2, Perk.None) },
            { "p04", new ResidentStats(3, 3, 5, 2, Perk.None) },
            { "bront", new ResidentStats(4, 2, 3, 5, Perk.None) },
            { "ink", new ResidentStats(5, 2, 4, 3, Perk.None) },
        };
        public static ResidentStats For(string id) { id = ResidentCast.Current(id); return id != null && table.TryGetValue(id, out var s) ? s : Default; }
        public static bool Has(string id) { id = ResidentCast.Current(id); return id != null && table.ContainsKey(id); }

        public static string PerkName(Perk p) => p switch {
            Perk.Sprinter => "Sprinter", Perk.QuickHands => "Quick hands", Perk.Tireless => "Tireless", Perk.NightOwl => "Night owl",
            Perk.Brawler => "Brawler", Perk.Tough => "Tough", Perk.Rally => "Rally", Perk.Steady => "Steady", _ => "",
        };
        public static string PerkText(Perk p) => p switch {
            Perk.Sprinter => "walks 25% faster",
            Perk.QuickHands => "works stations 25% faster",
            Perk.Tireless => "loses energy at under half the usual rate",
            Perk.NightOwl => "20% faster at everything after dark",
            Perk.Brawler => "+1 Brawn in raids",
            Perk.Tough => "takes 30% less damage in raids",
            Perk.Rally => "the whole raid crew hits 15% harder",
            Perk.Steady => "keeps full speed even when tired",
            _ => "",
        };

        // Kitchen multipliers. A 3 in the relevant stat is the old baseline (1.0).
        public static bool Exhausted(ResidentStats s, float energy) => energy < 25 && s.Perk != Perk.Steady;
        public static float WorkMultiplier(ResidentStats s, string station, bool night) {
            float m = .7f + .1f * (station == "sink" ? s.Speed : s.Cooking);
            if (s.Perk == Perk.QuickHands) m *= 1.25f;
            if (s.Perk == Perk.NightOwl && night) m *= 1.2f;
            return m;
        }
        public static float WalkMultiplier(ResidentStats s, bool night) {
            float m = .7f + .1f * s.Speed;
            if (s.Perk == Perk.Sprinter) m *= 1.25f;
            if (s.Perk == Perk.NightOwl && night) m *= 1.2f;
            return m;
        }
        public static float DrainMultiplier(ResidentStats s) => (1.3f - .1f * s.Stamina) * (s.Perk == Perk.Tireless ? .4f : 1f);

        // Raid numbers (used by Raids): health, damage per hit, seconds between hits.
        public static int RaidBrawn(ResidentStats s) => s.Brawn + (s.Perk == Perk.Brawler ? 1 : 0);
        public static float RaidHealth(ResidentStats s) => 40 + 12 * s.Stamina;
        public static float RaidDamage(ResidentStats s) => 6 + 4 * RaidBrawn(s);
        public static float RaidInterval(ResidentStats s) => 1.7f - .15f * s.Speed;
    }
}
