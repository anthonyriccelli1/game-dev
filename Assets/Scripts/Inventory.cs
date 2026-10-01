using System;
using System.Collections.Generic;
using System.Linq;
namespace RestaurantCity {
    // Weapons you can hold in a hotbar slot. An empty selected slot means bare fists.
    public class WeaponDef {
        public string Id, Name, Short, Blurb; public int Price;
        public float Damage, HeavyMultiplier = 2.1f, Cooldown, Reach, Knockback;
    }
    public static class Weapons {
        public static readonly WeaponDef Fists = new WeaponDef { Id = "fists", Name = "Fists", Short = "FISTS", Damage = 11, Cooldown = .34f, Reach = 1.8f, Knockback = .45f, Blurb = "Free. Fast jabs; hold to wind up a haymaker." };
        public static readonly WeaponDef[] Shop = {
            new WeaponDef { Id = "knuckles", Name = "Brass knuckles", Short = "KNUCKS", Price = 60, Damage = 17, Cooldown = .36f, Reach = 1.8f, Knockback = .6f, Blurb = "Fists, but they hurt. Still fast." },
            new WeaponDef { Id = "pan", Name = "Frying pan", Short = "PAN", Price = 35, Damage = 22, Cooldown = .55f, Reach = 2.1f, Knockback = 1f, Blurb = "A cast-iron classic. Slower, rings their bell." },
            new WeaponDef { Id = "bat", Name = "Baseball bat", Short = "BAT", Price = 85, Damage = 29, Cooldown = .72f, Reach = 2.5f, Knockback = 1.4f, Blurb = "Long reach, big swings. Commit to every hit." },
        };
        public static WeaponDef Get(string id) => string.IsNullOrEmpty(id) ? null : id == "fists" ? Fists : Array.Find(Shop, w => w.Id == id);
    }

    // One hotbar slot: a weapon (Item) or a carried city item (a grocery bag, by kitchen item id).
    [Serializable] public class InvSlot { public string Item = ""; public int KitchenItemId = -1; public bool Empty => string.IsNullOrEmpty(Item) && KitchenItemId < 0; }
    [Serializable] public class PlayerInventory {
        public int PlayerId, Selected; public List<InvSlot> Slots = new List<InvSlot>();
        public const int Size = 8;
        public InvSlot Current => Slots[Math.Max(0, Math.Min(Size - 1, Selected))];
        public WeaponDef Weapon => Weapons.Get(Current.Item) ?? Weapons.Fists;
        public int FirstEmpty() => Slots.FindIndex(s => s.Empty);
        public bool Has(string item) => Slots.Any(s => s.Item == item);
    }

    // Schedule I-style hotbar rules. City items (weapons, Milo's grocery bag, Zeeb's sauce) live in slots; the selected
    // slot is what you hold. Kitchen food stays one-at-a-time in your hands (PlateUp rule): while you hold food you
    // can't switch slots. A bag in an unselected slot is "stowed" (holder "stow:<actor>").
    public static class Hotbar {
        public static string Stowed(string actor) => "stow:" + actor;
        public static PlayerInventory For(GameState g, int playerId) {
            g.Inventories = g.Inventories ?? new List<PlayerInventory>();
            var inv = g.Inventories.Find(i => i.PlayerId == playerId);
            if (inv == null) g.Inventories.Add(inv = new PlayerInventory { PlayerId = playerId });
            while (inv.Slots.Count < PlayerInventory.Size) inv.Slots.Add(new InvSlot());
            if (inv.Slots.Count > PlayerInventory.Size) inv.Slots.RemoveRange(PlayerInventory.Size, inv.Slots.Count - PlayerInventory.Size);
            inv.Selected = Math.Max(0, Math.Min(PlayerInventory.Size - 1, inv.Selected));
            return inv;
        }
        static string Actor(int playerId) => "player:" + playerId;
        public static bool IsBag(KitchenItem i) => i != null && i.Kind == KitchenItemKind.GroceryBag;
        // The kitchen item in your hands that is NOT a hotbar item (food, a plate): it blocks switching.
        public static KitchenItem HandFood(GameState g, int playerId) { var h = g.Kitchen?.Hold(Actor(playerId)); return h != null && !IsBag(h) ? h : null; }

        public static bool Select(GameState g, int playerId, int slot, out string message) {
            var inv = For(g, playerId); message = "";
            slot = (slot % PlayerInventory.Size + PlayerInventory.Size) % PlayerInventory.Size;
            if (slot == inv.Selected) return true;
            var food = HandFood(g, playerId);
            if (food != null) { message = "Hands full: put down the " + g.Kitchen.Label(food).ToLower() + " first."; return false; }
            inv.Selected = slot; Apply(g, playerId); return true;
        }
        // Put a newly bought weapon in the first empty slot (and select it).
        public static bool Give(GameState g, int playerId, string item, out string message) {
            var inv = For(g, playerId); int i = inv.FirstEmpty();
            if (i < 0) { message = "Your hotbar is full. Stash something first."; return false; }
            inv.Slots[i].Item = item; message = "";
            if (HandFood(g, playerId) == null) { inv.Selected = i; Apply(g, playerId); }
            return true;
        }
        // Keeps slots and kitchen items in agreement: new bags in your hands take a slot, empty bags free theirs,
        // and only the selected slot's bag is actually "in hand".
        public static void Sync(GameState g, int playerId) {
            if (g.Kitchen == null) return;
            var inv = For(g, playerId); string actor = Actor(playerId), stow = Stowed(actor);
            foreach (var s in inv.Slots) if (s.KitchenItemId >= 0) {
                var item = g.Kitchen.Items.Find(x => x.Id == s.KitchenItemId);
                if (!IsBag(item) || (item.Holder != actor && item.Holder != stow)) s.KitchenItemId = -1;
            }
            // A stowed bag that game code just put back in your hands (you bought more at Milo's): select its slot.
            for (int i = 0; i < inv.Slots.Count; i++) {
                var s = inv.Slots[i]; if (s.KitchenItemId < 0 || i == inv.Selected) continue;
                var item = g.Kitchen.Items.Find(x => x.Id == s.KitchenItemId);
                if (item != null && item.Holder == actor && HandFood(g, playerId) == null) inv.Selected = i;
            }
            foreach (var bag in g.Kitchen.Items.Where(x => IsBag(x) && (x.Holder == actor || x.Holder == stow)).ToList()) {
                if (inv.Slots.Any(s => s.KitchenItemId == bag.Id)) continue;
                int i = inv.Current.Empty ? inv.Selected : inv.FirstEmpty();
                if (i < 0) { bag.Holder = actor; continue; }   // hotbar full: it simply stays in your hands
                inv.Slots[i].KitchenItemId = bag.Id;
                if (bag.Holder == actor && HandFood(g, playerId) == null) inv.Selected = i;
            }
            Apply(g, playerId);
        }
        static void Apply(GameState g, int playerId) {
            var inv = For(g, playerId); string actor = Actor(playerId), stow = Stowed(actor);
            bool food = HandFood(g, playerId) != null;
            for (int i = 0; i < inv.Slots.Count; i++) {
                var s = inv.Slots[i]; if (s.KitchenItemId < 0) continue;
                var bag = g.Kitchen.Items.Find(x => x.Id == s.KitchenItemId); if (bag == null) continue;
                bag.Holder = i == inv.Selected && !food ? actor : stow;
            }
        }
        // The bag you're carrying anywhere on you (in hand or stowed), so new groceries join it.
        public static KitchenItem CarriedBag(GameState g, string actor) => g.Kitchen?.Items.Find(x => IsBag(x) && (x.Holder == actor || x.Holder == Stowed(actor)));
        public static string SlotLabel(GameState g, InvSlot s) {
            if (!string.IsNullOrEmpty(s.Item)) return Weapons.Get(s.Item)?.Short ?? s.Item.ToUpper();
            if (s.KitchenItemId >= 0) {
                var bag = g.Kitchen?.Items.Find(x => x.Id == s.KitchenItemId); if (bag == null) return "";
                int sauce = bag.Components.Count(c => c == Inspections.Contraband);
                return sauce > 0 && sauce == bag.Components.Count ? "SAUCE x" + sauce : "BAG x" + bag.Components.Count;
            }
            return "";
        }
    }
}
