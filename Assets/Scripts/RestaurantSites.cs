using UnityEngine;
namespace RestaurantCity {
    // The shabby restaurants you can choose between in a district (Schedule I-style "which house?" choice).
    // Every site uses the same room layout, shifted by Offset; the choice is look, view and location.
    public class RestaurantSite { public string Id, Title, Sign, Pitch; public Vector3 Offset; }
    public static class RestaurantSites {
        public static readonly RestaurantSite[] All = {
            new RestaurantSite { Id = "oddtable", Title = "The Odd Table", Sign = "THE ODD TABLE", Offset = Vector3.zero,
                Pitch = "17 Main Street. A busy corner next to Milo's and your stand, with apartments upstairs." },
            new RestaurantSite { Id = "bayside", Title = "The Bayside", Sign = "THE BAYSIDE", Offset = new Vector3(-180, 0, 0),
                Pitch = "West end of Main Street, right on the water. A long walk from Milo's, but guests love the view (+4 ambience)." },
        };
        public static RestaurantSite Get(string id) { foreach (var s in All) if (s.Id == id) return s; return All[0]; }
    }
}
