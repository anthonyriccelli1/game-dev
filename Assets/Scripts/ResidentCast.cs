using System.Collections.Generic;
namespace RestaurantCity {
    public enum Gait { Standard, Light, Zombie }
    // One resident of Saffron Bay. Adding a resident = one line here plus its FBX/PNG in Resources/Residents.
    public class ResidentDef {
        public string Id, Name, Blurb, Model; public float Height; public int Tier, MinAmbience; public bool NightOnly; public Gait Gait; public StaffJob Job;
        public ResidentDef(string id, string name, float height, int tier, StaffJob job, string blurb, Gait gait = Gait.Standard, bool night = false) {
            Id = id; Model = id; Name = name; Height = height; Tier = tier; Job = job; Blurb = blurb; Gait = gait; NightOnly = night;
            MinAmbience = tier == 0 ? 0 : tier == 1 ? 6 : 14;
        }
        // Pickier residents only visit restaurants with at least this much ambience (decor, finishes, lighting).
        public ResidentDef Picky(int ambience) { MinAmbience = ambience; return this; }
        // A new look for an existing resident: the id (saves, People book, stats) stays, the model file changes.
        public ResidentDef Uses(string model, float height) { Model = model; Height = height; return this; }
        // Recruiting price in Flux by rarity. Rares are a real goal: several nights of raids and stashes.
        public int FluxCost => Tier == 0 ? 3 : Tier == 1 ? 5 : 10;
        public string Rarity => Tier == 0 ? "Common" : Tier == 1 ? "Uncommon" : "Rare";
    }
    // Residents show up on their own (weighted by rarity and your stars). Feed one once and they join your People book;
    // from then on you can recruit them with Flux.
    public static class ResidentCast {
        // Standard height for newly imported custom characters; the existing cast keeps its authored variety.
        public const float CustomResidentHeight = 2.0f;
        // Story characters: never recruitable, never in the visitor pool.
        public static readonly ResidentDef Milo = new ResidentDef("004_OldMoustache", "Milo", 1.68f, 0, StaffJob.Any, "Runs the market.").Uses("213_TripoMilo", 1.68f);   // Tripo mushroom man; id kept for saves

        public static readonly ResidentDef[] OldMarket = {
            new ResidentDef("003_Jimmy", "Jimmy", 1.55f, 0, StaffJob.Serve, "Knows every shortcut on the block."),
            new ResidentDef("006_Cappy", "Cappy", 1.7f, 0, StaffJob.Clean, "Never takes the cap off. Scrubs like he means it."),
            new ResidentDef("208_TripoConstruction", "Buck", CustomResidentHeight, 0, StaffJob.Cook, "Builds burgers like he builds walls: square, solid, and ready by noon."),
            new ResidentDef("209_TripoFootball", "Blitz", CustomResidentHeight, 0, StaffJob.Serve, "Runs plates like he runs routes. Nothing gets dropped on his watch."),
            new ResidentDef("070_Robert", "Robert", 1.8f, 0, StaffJob.Cook, "Grill-side philosopher."),
            new ResidentDef("012_Chill", "Chill", 1.7f, 0, StaffJob.Clean, "Nothing rattles Chill. Not even a sink full of plates."),
            new ResidentDef("128_RandomBoi", "Random Boi", 1.62f, 0, StaffJob.Serve, "Rolls with whatever the night brings."),
            new ResidentDef("038_Kate", "Kate", 1.64f, 0, StaffJob.Cook, "Night-school chef. Plates like it's an exam.", Gait.Light),
            new ResidentDef("053_Erika", "Erika", 1.66f, 0, StaffJob.Serve, "Remembers every order without writing it down.", Gait.Light),
            new ResidentDef("207_TripoGothGirl", "Raven", CustomResidentHeight, 0, StaffJob.Clean, "Black nails, black coffee, spotless dishes. Livens up after dark.", Gait.Light),
            new ResidentDef("206_TripoCheerleader", "Pepper", CustomResidentHeight, 0, StaffJob.Serve, "Captain of the Old Market squad. Cheers every plate out the door.", Gait.Light),
            new ResidentDef("071_LilBro", "Lil Bro", 1.38f, 1, StaffJob.Clean, "Small bottle, big attitude."),
            new ResidentDef("091_BigBro_a", "Big Bro", 2.05f, 1, StaffJob.Cook, "Lil Bro's big brother. Carries four plates at once."),
            new ResidentDef("074_Baldman", "Baldman", 1.82f, 1, StaffJob.Serve, "Caped, confident, and weirdly good at refills."),
            new ResidentDef("210_TripoClown", "Bonkers", CustomResidentHeight, 1, StaffJob.Serve, "Juggles three plates and a joke at every table. The crowd loves him."),
            new ResidentDef("054_Lydia", "Lydia", 1.62f, 1, StaffJob.Cook, "Came for one burger. Stayed for the kitchen.", Gait.Light),
            new ResidentDef("136_SlugPerson", "Slug", 1.45f, 1, StaffJob.Clean, "Slow walker. Leaves every floor shining."),
            new ResidentDef("139_CoolHydrant", "Hydrant", 1.3f, 2, StaffJob.Clean, "Built-in water pressure. The ultimate dishwasher.").Picky(10),
            new ResidentDef("146_CoolTrash", "Trash Can", 1.4f, 2, StaffJob.Clean, "One man's trash is this can's whole personality.").Picky(0),
            new ResidentDef("201_TripoAlien", "Zilo", CustomResidentHeight, 2, StaffJob.Serve, "A sharp-eyed visitor who gets hot plates to the right table fast."),
            new ResidentDef("044_Zombie", "Zombie", 1.76f, 1, StaffJob.Cook, "Only comes out at night. Doesn't mind the heat.", Gait.Zombie, true).Picky(0),
            new ResidentDef("211_TripoPumpkin", "Jack", CustomResidentHeight, 2, StaffJob.Cook, "Carved grin, glowing eyes, never tired. Works the grill from dusk till the candle burns out.", Gait.Standard, true),
            new ResidentDef("205_TripoVampire", "Dracula", CustomResidentHeight, 2, StaffJob.Serve, "Charming night-shift host. Hates garlic orders.", Gait.Standard, true).Picky(18),
            new ResidentDef("204_TripoReaper", "Grim", CustomResidentHeight, 2, StaffJob.Clean, "Never late, never rushed. Clears every table, eventually all of them. Comes out at night.", Gait.Standard, true).Picky(16),
        };

        // The 100 Avatars cast are placeholders. Each new custom character takes over a placeholder's slot (same rarity,
        // ideally the same job and day/night), and saves that met or hired the placeholder get the new character.
        public static readonly Dictionary<string, string> Replaced = new Dictionary<string, string> {
            { "046_Mafiossini", "201_TripoAlien" },      // rare, day, server     -> Zilo
            { "035_Wolfman", "204_TripoReaper" },        // rare, night, cleaner  -> Grim
            { "043_Dracula", "205_TripoVampire" },       // rare, night, server   -> Dracula (new look)
            { "052_Jennifer", "206_TripoCheerleader" },  // common, server        -> Pepper
            { "056_Olivia", "207_TripoGothGirl" },       // common, cleaner       -> Raven
            { "008_Hugo", "208_TripoConstruction" },     // common, cook, brawler -> Buck
            { "069_Kyle", "209_TripoFootball" },         // common, server, fast  -> Blitz
            { "102_BizDude", "210_TripoClown" },         // uncommon, server      -> Bonkers
            { "033_Franky", "211_TripoPumpkin" },        // rare, night, cook     -> Jack
        };
        public static string Current(string id) => id != null && Replaced.TryGetValue(id, out var to) ? to : id;
        static readonly Dictionary<string, ResidentDef> byId = new Dictionary<string, ResidentDef>();
        public static ResidentDef Get(string id) {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId.Count == 0) { byId[Milo.Id] = Milo; foreach (var r in OldMarket) byId[r.Id] = r; }
            return byId.TryGetValue(Current(id), out var d) ? d : null;
        }
        public static bool IsRecruitable(string id) { var d = Get(id); return d != null && d != Milo; }

        // Who walks in next. Only residents whose taste your place meets (ambience) can come. Commons are most likely; uncommons and rares get likelier as your stars rise; night brings
        // out the night crowd. People you haven't fed yet get a small boost so the book fills steadily.
        // The food stand counts as a bare-bones venue.
        public const int StandAmbience = 3;
        public static ResidentDef Visitor(int seed, bool night, int stars, ICollection<string> met, int ambience = 40) {
            var pool = new List<ResidentDef>();
            foreach (var r in OldMarket) {
                if (r.NightOnly && !night) continue;
                if (ambience < r.MinAmbience) continue;   // too fancy for this place: decorate to attract them
                int weight = r.Tier == 0 ? 6 : r.Tier == 1 ? 2 + stars : stars >= 2 ? 2 : 1;
                if (r.NightOnly) weight += 3;
                if (met != null && !met.Contains(r.Id)) weight += 2;
                for (int i = 0; i < weight; i++) pool.Add(r);
            }
            uint h = (uint)seed * 2654435761u; h ^= h >> 13; h *= 2246822519u; h ^= h >> 16;
            return pool[(int)(h % (uint)pool.Count)];
        }
        // Default look for staff that aren't residents (basic hires, legacy saves): a stable resident per worker id.
        public static ResidentDef ForWorker(string workerId) {
            var direct = Get(workerId); if (direct != null) return direct;
            int h = 17; foreach (char c in workerId ?? "") h = h * 31 + c;
            var day = System.Array.FindAll(OldMarket, r => !r.NightOnly);
            return day[(h & 0x7fffffff) % day.Length];
        }
        static readonly Dictionary<string, StaffDefinition> staff = new Dictionary<string, StaffDefinition>();
        public static StaffDefinition Staff(string id) {
            if (staff.TryGetValue(id ?? "", out var s)) return s;
            var d = Get(id); if (d == null || !IsRecruitable(id)) return null;
            s = new StaffDefinition(d.Id, d.Name, d.Job, d.FluxCost, -1, 0, d.Blurb + " Specialty: " + d.Job + ". Can do any job.");
            staff[id] = s; return s;
        }
    }
}
