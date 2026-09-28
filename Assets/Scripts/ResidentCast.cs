using System.Collections.Generic;
namespace RestaurantCity {
    public enum Gait { Standard, Light, Zombie }
    // One resident of Saffron Bay. Adding a resident = one line here plus its FBX/PNG in Resources/Residents.
    public class ResidentDef {
        public string Id, Name; public float Height; public int Tier; public bool NightOnly; public Gait Gait;
        public ResidentDef(string id, string name, float height, int tier = 0, Gait gait = Gait.Standard, bool night = false) { Id = id; Name = name; Height = height; Tier = tier; Gait = gait; NightOnly = night; }
    }
    public static class ResidentCast {
        // Story characters: never recruitable, never in the visitor pool.
        public static readonly ResidentDef Milo = new ResidentDef("004_OldMoustache", "Milo", 1.68f);

        // Old Market: the everyday street crowd (tier 0 common, 1 uncommon, 2 rare). Night-only residents come out after dark.
        public static readonly ResidentDef[] OldMarket = {
            new ResidentDef("003_Jimmy", "Jimmy", 1.55f),
            new ResidentDef("006_Cappy", "Cappy", 1.7f),
            new ResidentDef("008_Hugo", "Hugo", 1.78f),
            new ResidentDef("069_Kyle", "Kyle", 1.72f),
            new ResidentDef("070_Robert", "Robert", 1.8f),
            new ResidentDef("012_Chill", "Chill", 1.7f),
            new ResidentDef("128_RandomBoi", "Random Boi", 1.62f),
            new ResidentDef("038_Kate", "Kate", 1.64f, 0, Gait.Light),
            new ResidentDef("053_Erika", "Erika", 1.66f, 0, Gait.Light),
            new ResidentDef("056_Olivia", "Olivia", 1.6f, 0, Gait.Light),
            new ResidentDef("052_Jennifer", "Jennifer", 1.68f, 0, Gait.Light),
            new ResidentDef("071_LilBro", "Lil Bro", 1.38f, 1),
            new ResidentDef("091_BigBro_a", "Big Bro", 2.05f, 1),
            new ResidentDef("074_Baldman", "Baldman", 1.82f, 1),
            new ResidentDef("102_BizDude", "Biz Dude", 1.8f, 1),
            new ResidentDef("057_Rose", "Rose", 1.66f, 1, Gait.Light),
            new ResidentDef("054_Lydia", "Lydia", 1.62f, 1, Gait.Light),
            new ResidentDef("136_SlugPerson", "Slug", 1.45f, 1),
            new ResidentDef("046_Mafiossini", "Mafiossini", 1.8f, 2),
            new ResidentDef("139_CoolHydrant", "Hydrant", 1.3f, 2),
            new ResidentDef("146_CoolTrash", "Trash Can", 1.4f, 2),
            new ResidentDef("044_Zombie", "Zombie", 1.76f, 1, Gait.Zombie, true),
            new ResidentDef("033_Franky", "Franky", 2.0f, 2, Gait.Zombie, true),
            new ResidentDef("043_Dracula", "Dracula", 1.86f, 2, Gait.Standard, true),
            new ResidentDef("035_Wolfman", "Wolfman", 1.9f, 2, Gait.Standard, true),
        };

        static readonly Dictionary<string, ResidentDef> byId = new Dictionary<string, ResidentDef>();
        public static ResidentDef Get(string id) {
            if (byId.Count == 0) { byId[Milo.Id] = Milo; foreach (var r in OldMarket) byId[r.Id] = r; }
            return byId.TryGetValue(id, out var d) ? d : null;
        }
        // A visitor for a given guest seed. Commons come most often, rares seldom; the night crowd only after dark.
        public static ResidentDef Visitor(int seed, bool night) {
            var pool = new List<ResidentDef>();
            foreach (var r in OldMarket) {
                if (r.NightOnly && !night) continue;
                int weight = r.Tier == 0 ? 6 : r.Tier == 1 ? 3 : 1;
                if (r.NightOnly) weight += 3;   // after dark the spooky crowd is the point
                for (int i = 0; i < weight; i++) pool.Add(r);
            }
            uint h = (uint)seed * 2654435761u; h ^= h >> 13;
            return pool[(int)(h % (uint)pool.Count)];
        }
        // Staff look: a stable resident per worker id, so the same worker always looks the same.
        public static ResidentDef ForWorker(string workerId) {
            int h = 17; foreach (char c in workerId ?? "") h = h * 31 + c;
            var day = System.Array.FindAll(OldMarket, r => !r.NightOnly);
            return day[(h & 0x7fffffff) % day.Length];
        }
    }
}
