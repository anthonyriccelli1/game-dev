using System;
using System.Collections.Generic;
namespace RestaurantCity {
    // Zombie Cafe-style raids on rival restaurants, played in first person: your picked crew brawls the rival's
    // workers while you take on the boss. Cash and Flux always drop; the rival's signature recipe drops by chance
    // (guaranteed by the Pity-th win). You can raid rivals up to one star above your restaurant, once a day each.
    public class RivalDef {
        public string Id, Name, Place, Boss, BossModel, RecipeId, Pitch;
        public int Stars, BossHealth, BossDamage, CashMin, CashMax, Flux, Pity;
        public float X, Z, DropChance, BossInterval = .85f; public bool NightOnly;
        public string GoonOne = "imp", Goons = "imps";   // what to call the rival's fighters in messages
        public UnityEngine.Vector3 Box = new UnityEngine.Vector3(2.6f, 2.4f, 5.4f);   // the raid trigger (centred on X, Z)
        public UnityEngine.Vector3 Arena;   // where the brawl centres; zero = in front of the trigger (X, Z - 5)
        public RivalFighter[] Roster;   // the rival's own crew, sent out in this order against yours
    }
    public class RivalFighter {
        public string Name, Model, Fallback; public ResidentStats Stats;
        public RivalFighter(string name, string model, ResidentStats stats, string fallback = null) { Name = name; Model = model; Stats = stats; Fallback = fallback; }
        // The model to use right now: a character still being rendered stands in with its fallback until its file lands.
        public string ModelNow => Fallback == null || UnityEngine.Resources.Load<UnityEngine.GameObject>("Residents/" + Model) ? Model : Fallback;
    }
    [Serializable] public class RaidRecord { public string RivalId; public int LastDay = -1, Wins, Attempts; }
    public class RaidLoot { public int Cash, Flux; public string Recipe; public string Message; }

    public static class Rivals {
        public static readonly RivalDef GreasyGus = new RivalDef {
            Id = "gus", Name = "Greasy Gus's truck", Place = "the vacant lot off South Avenue", Boss = "Greasy Gus", BossModel = "202_GreasyGus",
            Stars = 1, X = 17.5f, Z = -31.5f, NightOnly = true, BossHealth = 380, BossDamage = 24,
            // Gus's imps are commons-level fighters (10 stat points). A common of yours is an even fight; bring
            // someone with more Brawn or Stamina and you're favoured.
            Roster = new[] {
                new RivalFighter("Gus's Imp One", "203_GusImp", new ResidentStats(1, 2, 3, 4, Perk.None)),
                new RivalFighter("Gus's Imp Two", "203_GusImp", new ResidentStats(2, 3, 2, 3, Perk.None)),
                new RivalFighter("Gus's Imp Three", "203_GusImp", new ResidentStats(1, 4, 2, 3, Perk.None)),
            },
            CashMin = 45, CashMax = 70, Flux = 2, RecipeId = "cyclops", DropChance = .4f, Pity = 3,
            Pitch = "A one-star food truck that parks in the vacant lot after dark. Gus fights dirty and headbutts hard. He brings one imp for every crew member you bring, plus a bodyguard imp that comes straight for you.",
        };
        // The Alchemist: Old Market's two-star lab diner on Main Street. You can walk in by day and watch his stitched
        // staff serve the Philosopher's Stack; at night, once you have two stars, you can raid it. A real step up from
        // Gus: tougher boss, and his staff are uncommon-level fighters (14 stat points against the imps' 10).
        public static readonly RivalDef Alchemist = new RivalDef {
            Id = "alchemist", Name = "The Alchemist", Place = "Main Street", Boss = "The Alchemist", BossModel = "214_TripoAlchemist",
            Stars = 2, X = 14.5f, Z = 25.4f, NightOnly = true, BossHealth = 650, BossDamage = 30, BossInterval = .8f,
            Box = new UnityEngine.Vector3(2.3f, 1.4f, 1.2f), Arena = new UnityEngine.Vector3(15, 0, 20.5f),
            GoonOne = "stitched brute", Goons = "stitched staff",
            Roster = new[] {
                new RivalFighter("Frank", "212_TripoFrank", new ResidentStats(3, 1, 5, 5, Perk.None)),
                new RivalFighter("Frankie", "215_TripoFrankie", new ResidentStats(3, 4, 4, 3, Perk.None)),
                new RivalFighter("Frank's cousin", "212_TripoFrank", new ResidentStats(2, 2, 5, 5, Perk.None)),
            },
            CashMin = 90, CashMax = 140, Flux = 4, RecipeId = "philosopher", DropChance = .35f, Pity = 3,
            Pitch = "A two-star lab diner. The Alchemist hits hard and keeps coming; his stitched staff are bigger and tougher than Gus's imps. One of them for every crew member you bring, plus a bodyguard for you.",
        };
        public static readonly RivalDef[] All = { GreasyGus, Alchemist };
        public static RivalDef Get(string id) => Array.Find(All, r => r.Id == id);
    }

    public static class RaidRules {
        public const int MaxCrew = 3, MinEnergy = 30;
        public static RaidRecord Record(GameState g, string rivalId) {
            g.Raids = g.Raids ?? new List<RaidRecord>();
            var r = g.Raids.Find(x => x.RivalId == rivalId); if (r == null) g.Raids.Add(r = new RaidRecord { RivalId = rivalId });
            return r;
        }
        public static bool CanRaid(GameState g, RivalDef rival, bool serviceRunning, out string why) {
            int stars = g.Restaurant != null ? g.Restaurant.Stars : 0;
            if (rival.NightOnly && !g.IsNight) { why = rival.Boss + " only takes on challengers after dark. Come back at night."; return false; }
            if (serviceRunning) { why = "Your restaurant is mid-service. Close up and finish the last guest first."; return false; }
            if (rival.Stars > stars + 1) { why = "Too big for you: raid rivals up to one star above your restaurant (" + StarText.Words(stars) + " now)."; return false; }
            if (Record(g, rival.Id).LastDay == g.Day) { why = rival.Boss + " is on guard after tonight's raid. Try again tomorrow night."; return false; }
            why = ""; return true;
        }
        public static bool CanFight(WorkerState w) => w != null && w.Energy >= MinEnergy && w.Job != StaffJob.Stand;
        // A raid attempt uses up the day, win or lose.
        public static void Begin(GameState g, RivalDef rival) { var r = Record(g, rival.Id); r.Attempts++; r.LastDay = g.Day; }
        public static RaidLoot Win(GameState g, RivalDef rival, int seed) {
            var r = Record(g, rival.Id); r.Wins++;
            uint h = (uint)(seed * 2654435761u ^ (uint)(g.Day * 40503) ^ (uint)r.Wins); h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            var loot = new RaidLoot { Cash = rival.CashMin + (int)(h % (uint)(rival.CashMax - rival.CashMin + 1)), Flux = r.Wins == 1 ? rival.Flux : 0 };   // Flux only for the first defeat
            float roll = (h >> 8) % 1000 / 1000f;
            if (!g.Knows(rival.RecipeId) && (roll < rival.DropChance || r.Wins >= rival.Pity)) { loot.Recipe = rival.RecipeId; g.Learn(rival.RecipeId); }
            g.Cash += loot.Cash; g.Flux += loot.Flux; g.FluxIntroduced = true;
            g.GainReputation(r.Wins == 1 ? Reputation.BeatRival : Reputation.RematchRival, "Beating rivals");
            string dish = loot.Recipe != null ? RestaurantCatalog.Dish(loot.Recipe).Name : null;
            loot.Message = "RAID WON!  +$" + loot.Cash + (loot.Flux > 0 ? "  +" + loot.Flux + " Flux" : "") + (dish != null ? "  +RECIPE: " + dish.ToUpper() + " (check the Cookbook and Menu)" :
                g.Knows(rival.RecipeId) ? "" : "  No recipe this time (" + Math.Max(0, rival.Pity - r.Wins) + " more win" + (rival.Pity - r.Wins == 1 ? "" : "s") + " guarantees it).");
            return loot;
        }
        // Knocked out in a raid: you drop 20% of your cash (up to $100) and every crew member who came is spent.
        public const int LossCap = 100;
        public static int CashLoss(int cash) => Math.Min(LossCap, (Math.Max(0, cash) * 20 + 99) / 100);   // 20%, rounded up, in whole dollars
        public static int Lose(GameState g, IEnumerable<WorkerState> crew) {
            int loss = CashLoss(g.Cash); g.Cash -= loss;
            foreach (var w in crew) if (w != null) w.Energy = 0;
            return loss;
        }
        // After the brawl: everyone who fought is tired; anyone knocked out is spent and needs rest.
        public static void CrewAfter(WorkerState w, float healthLeft01, bool knockedOut) {
            if (w == null) return;
            w.Energy = knockedOut ? 0 : Math.Max(0, w.Energy - 15 - (1 - healthLeft01) * 45);
        }
        // One-on-one preview: how much faster your fighter drops theirs than the other way round (>1 favours you).
        public static float Duel(ResidentStats mine, ResidentStats theirs, bool rally = false) {
            float myDps = StaffStats.RaidDamage(mine) * (rally ? 1.15f : 1) / StaffStats.RaidInterval(mine), theirDps = StaffStats.RaidDamage(theirs) / StaffStats.RaidInterval(theirs);
            float myHp = StaffStats.RaidHealth(mine) / (mine.Perk == Perk.Tough ? .7f : 1), theirHp = StaffStats.RaidHealth(theirs);
            return (myHp / theirDps) / (theirHp / myDps);
        }
        public static string DuelWord(float edge) => edge >= 1.15f ? "FAVOURED" : edge >= .87f ? "EVEN FIGHT" : "OUTMATCHED";
        // Rough fight preview: total crew damage per second vs the rival's goons.
        public static float Power(IEnumerable<ResidentStats> side) {
            float p = 0; bool rally = false; foreach (var s in side) if (s.Perk == Perk.Rally) rally = true;
            foreach (var s in side) p += StaffStats.RaidDamage(s) / StaffStats.RaidInterval(s) * (rally ? 1.15f : 1) * (StaffStats.RaidHealth(s) / 76f);
            return p;
        }
    }
}
