using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Every pantry shows what it really holds: one shelf spot per ingredient, food that thins out as you
    // cook, and a count label that turns red at zero. You can read your stock at a glance, no prompt needed.
    public partial class RestaurantController {
        sealed class ShelfView { public int Shown = -1, Count = -1; public readonly List<GameObject> Items = new List<GameObject>(); public TextMesh Label; }
        static readonly (string shelf, string ingredient, string kind, float x, float y, string title)[] PantrySpots = {
            ("protein", "patty", "RawProtein", -.48f, .265f, "Patties"), ("greens", "greens", "RawGreens", .48f, .265f, "Greens"),
            ("bun", "bun", "Bun", -.48f, .805f, "Buns"), ("sauce", "midnight_sauce", "RawSauce", .48f, .805f, "Midnight sauce"),
        };
        readonly Dictionary<GameObject, Dictionary<string, ShelfView>> pantryViews = new Dictionary<GameObject, Dictionary<string, ShelfView>>();
        IEnumerable<GameObject> PantryObjects() {
            foreach (var p in Data.Layout) if (p.CatalogId == "pantry" && Furnishings.TryGetValue(p.InstanceId, out var o) && o) yield return o;
            if (standObjects.TryGetValue(KitchenState.StandBase + 1, out var stand) && stand) yield return stand;
        }
        void TickPantryDisplays() {
            foreach (var key in new List<GameObject>(pantryViews.Keys)) if (!key) pantryViews.Remove(key);
            foreach (var pantry in PantryObjects()) {
                if (!pantryViews.TryGetValue(pantry, out var views)) pantryViews[pantry] = views = new Dictionary<string, ShelfView>();
                foreach (var spot in PantrySpots) {
                    if (spot.shelf == "sauce" && !Game.State.Knows("midnight") && Data.Stock(spot.ingredient) == 0) continue;
                    if (!views.TryGetValue(spot.shelf, out var v)) views[spot.shelf] = v = new ShelfView();
                    int count = Data.Stock(spot.ingredient); int show = count == 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(count / 4f), 1, 6);
                    if (show != v.Shown) {
                        foreach (var o in v.Items) if (o) Destroy(o); v.Items.Clear();
                        for (int i = 0; i < show; i++) {
                            var food = KitchenArt.CreateItem(spot.kind, pantry.transform);
                            food.transform.localPosition = new Vector3(spot.x - .27f + (i % 3) * .27f, spot.y + (i / 3) * (spot.kind == "RawSauce" ? .0f : .07f), i / 3 == 0 ? .1f : -.14f);
                            food.transform.localRotation = Quaternion.Euler(0, i * 37, 0);
                            var ps = pantry.transform.localScale; food.transform.localScale = new Vector3(1 / Mathf.Max(.01f, ps.x), 1 / Mathf.Max(.01f, ps.y), 1 / Mathf.Max(.01f, ps.z));   // pantries can be squashed on X
                            foreach (var c in food.GetComponentsInChildren<Collider>()) c.enabled = false;   // the shelf hitbox stays the target
                            v.Items.Add(food);
                        }
                        v.Shown = show;
                    }
                    if (!v.Label) {
                        var ps = pantry.transform.localScale;
                        v.Label = WorldCaption(pantry.transform, "", new Vector3(spot.x, spot.y + .33f, 0), .011f); v.Label.transform.localScale = new Vector3(1 / Mathf.Max(.01f, ps.x), 1 / Mathf.Max(.01f, ps.y), 1 / Mathf.Max(.01f, ps.z));
                    }
                    if (count != v.Count) { v.Count = count; v.Label.text = count == 0 ? "<color=#E1543B>" + spot.title + ": OUT</color>" : spot.title + ": " + count; }
                    // One label per shelf that turns to face you, readable from the stand's back or the kitchen's front.
                    // It sits on the shelf's edge nearest you, so the shelf above never hides it.
                    if (Game.Player && Game.Player.View) {
                        var eye = Game.Player.View.transform.position; var toEye = pantry.transform.InverseTransformPoint(eye);
                        v.Label.transform.localPosition = new Vector3(spot.x, spot.y + .2f, toEye.z >= 0 ? .5f : -.5f);
                        v.Label.transform.rotation = Quaternion.LookRotation(v.Label.transform.position - eye);
                    }
                }
            }
        }
    }
}
