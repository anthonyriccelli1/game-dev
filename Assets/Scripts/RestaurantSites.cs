using UnityEngine;
namespace RestaurantCity {
    // One restaurant per district. Old Market's is The Odd Table, the rough starter everyone leases first.
    // Later districts each add their own site; every site uses the same room layout shifted by Offset.
    public class RestaurantSite { public string Id, Title, Sign, Pitch; public Vector3 Offset; public int Price; public bool Starter; }
    public static class RestaurantSites {
        // What The Odd Table costs. A few good days in the food truck, not one.
        public const int StarterPrice = 500;
        public static readonly RestaurantSite[] All = {
            new RestaurantSite { Id = "oddtable", Title = "The Odd Table", Sign = "THE ODD TABLE", Offset = Vector3.zero, Price = StarterPrice, Starter = true,
                Pitch = "17 Main Street. Rough, cramped, and yours. Next door to Milo's, two blocks up from Truck Park." },
        };
        public static RestaurantSite Get(string id) { foreach (var s in All) if (s.Id == id) return s; return All[0]; }
    }
}
