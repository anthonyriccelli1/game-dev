using UnityEngine;
namespace RestaurantCity {
    // Your restaurants, Schedule I-style: The Odd Table is the rough starter everyone leases first (the "motel");
    // The Bayside is the step up, a little bigger and nicer, bought later and run alongside it.
    // Every site uses the same room layout shifted by Offset.
    public class RestaurantSite { public string Id, Title, Sign, Pitch; public Vector3 Offset; public int Price; public bool Starter; }
    public static class RestaurantSites {
        public static readonly RestaurantSite[] All = {
            new RestaurantSite { Id = "oddtable", Title = "The Odd Table", Sign = "THE ODD TABLE", Offset = Vector3.zero, Price = 150, Starter = true,
                Pitch = "17 Main Street. Rough, cramped, and yours. Next door to Milo's and your stand." },
            new RestaurantSite { Id = "bayside", Title = "The Bayside", Sign = "THE BAYSIDE", Offset = new Vector3(-180, 0, 0), Price = 1200,
                Pitch = "West end of Main Street, on the water. A bigger dining room with a view: the step up from The Odd Table." },
        };
        public static RestaurantSite Get(string id) { foreach (var s in All) if (s.Id == id) return s; return All[0]; }
        // What it takes to buy the second restaurant: your first one has to be able to run without you.
        public const int SecondSiteStars = 2, SecondSiteCrew = 2;
    }
}
