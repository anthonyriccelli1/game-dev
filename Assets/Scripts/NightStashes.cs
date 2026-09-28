using System;
namespace RestaurantCity {
    // Zeeb, a laid-back alien food dealer from Little Nebula, sells secret-recipe ingredients by phone.
    // You pay a deposit, owe him the rest, and he stashes the order somewhere in Old Market after dark.
    public class StashSpot { public string Hint; public float X, Z; public StashSpot(string hint, float x, float z) { Hint = hint; X = x; Z = z; } }
    public static class NightStashes {
        public const string Dealer = "Zeeb";
        public static readonly StashSpot[] Spots = {
            new StashSpot("the park", 65, -25),
            new StashSpot("the Bayside waterfront", -210, -31),
            new StashSpot("the west end of Main Street", -120, 8.5f),
            new StashSpot("the east end of Main Street", 70, -7.5f),
        };
        public const int SaucePrice = 8;          // per bottle
        public const float Deposit = .3f;         // paid up front; the rest is owed
        public static readonly int[] Orders = { 3, 6, 9 };
        public static int Cost(int bottles) => bottles * SaucePrice;
        public static int DepositFor(int bottles) => (int)Math.Ceiling(Cost(bottles) * Deposit);
    }
}
