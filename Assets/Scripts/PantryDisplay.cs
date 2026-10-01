using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Every pantry shows what it really holds: one shelf spot per ingredient, food that thins out as you
    // cook, and a count label that turns red at zero. You can read your stock at a glance, no prompt needed.
    public partial class RestaurantController {
        sealed class ShelfView { public bool Focused; public int Shown = -1, Count = -1; public readonly List<GameObject> Items = new List<GameObject>(); public TextMesh Label; }
        // Each spot: shelf, ingredient, item art, x/y on the unit, the width it may use, and its tag text.
        static readonly (string shelf, string ingredient, string kind, float x, float y, float w, string title)[] PantrySpots = {
            ("protein", "patty", "RawProtein", -.48f, .265f, .85f, "Patties"), ("greens", "greens", "RawGreens", .48f, .265f, .85f, "Greens"),
            ("bun", "bun", "Bun", -.48f, .805f, .85f, "Buns"), ("sauce", "midnight_sauce", "RawSauce", .48f, .805f, .85f, "Sauce"),
            ("soup", "soup_veg", "SoupVeg", -.62f, 1.345f, .56f, "Soup veg"), ("sausage", "sausage", "RawSausage", 0f, 1.345f, .56f, "Sausages"), ("egg", "egg", "EggWhole", .62f, 1.345f, .56f, "Eggs"),
        };
        // The glass-door fridge: one shelf per cold food, heaviest at the bottom; the top shelf is split for the small stuff.
        static readonly (string shelf, string ingredient, string kind, float x, float y, float w, string title)[] FridgeSpots = {
            ("protein", "patty", "RawProtein", 0f, .565f, .76f, "Patties"), ("greens", "greens", "RawGreens", 0f, .905f, .76f, "Greens"),
            ("soup", "soup_veg", "SoupVeg", 0f, 1.245f, .76f, "Soup veg"), ("sausage", "sausage", "RawSausage", -.2f, 1.605f, .36f, "Sausages"), ("egg", "egg", "EggWhole", .2f, 1.605f, .36f, "Eggs"),
        };
        // Aim picks the ingredient on display nearest the crosshair (by angle), not whichever shelf hitbox the ray
        // happens to enter first: looking down at the patties used to clip the bun shelf's box in front of them.
        // The current pick wins ties (a little stickiness), and its shelf tag lights up so you know what E will grab.
        readonly Dictionary<string, string> pantryAim = new Dictionary<string, string>();
        readonly Dictionary<GameObject, (string shelf, int frame)> pantryFocus = new Dictionary<GameObject, (string, int)>();
        public string PantryAim(int stationId, string catalogId, Ray ray, string fallback, string actor) {
            var obj = StationObject(stationId); if (!obj) return fallback;
            var holds = KitchenState.ShelvesOf(Data, stationId, catalogId);
            pantryAim.TryGetValue(actor, out var last);
            string best = fallback; float bestScore = float.MaxValue;
            foreach (var spot in catalogId == "fridge" ? FridgeSpots : PantrySpots) {
                if (System.Array.IndexOf(holds, spot.shelf) < 0) continue;
                var ingDef = Ingredients.Get(spot.ingredient);
                if (ingDef != null && !string.IsNullOrEmpty(ingDef.Recipe) && !Game.State.Knows(ingDef.Recipe) && Data.Stock(spot.ingredient) == 0) continue;
                var centre = obj.transform.TransformPoint(new Vector3(spot.x, spot.y + .06f, 0));
                var v = centre - ray.origin; float along = Vector3.Dot(v, ray.direction); if (along < .05f) continue;
                float score = Vector3.Cross(ray.direction, v).magnitude / along;   // angle off the crosshair
                if (spot.shelf == last) score *= .8f;
                if (score < bestScore) { bestScore = score; best = spot.shelf; }
            }
            pantryAim[actor] = best; pantryFocus[obj] = (best, Time.frameCount);
            return best;
        }
        readonly Dictionary<GameObject, Dictionary<string, ShelfView>> pantryViews = new Dictionary<GameObject, Dictionary<string, ShelfView>>();
        IEnumerable<(GameObject obj, int id, string catalogId)> PantryObjects() {
            foreach (var p in Data.Layout) if ((p.CatalogId == "pantry" || p.CatalogId == "fridge") && Furnishings.TryGetValue(p.InstanceId, out var o) && o) yield return (o, p.InstanceId, p.CatalogId);
            if (standObjects.TryGetValue(KitchenState.StandBase + 1, out var stand) && stand) yield return (stand, KitchenState.StandBase + 1, "pantry");
        }
        // The fridge door swings open while someone stands in front of it, and closes when they walk away.
        readonly Dictionary<GameObject, (Transform door, Quaternion shut, float open)> fridgeDoors = new Dictionary<GameObject, (Transform, Quaternion, float)>();
        void SwingFridgeDoor(GameObject fridge) {
            if (!fridgeDoors.TryGetValue(fridge, out var d)) {
                Transform door = null; foreach (var t in fridge.GetComponentsInChildren<Transform>()) if (t.name.EndsWith("_Door_01")) { door = t; break; }
                fridgeDoors[fridge] = d = (door, door ? door.localRotation : Quaternion.identity, 0f);
            }
            if (!d.door) return;
            bool near = false;
            foreach (var p in FindObjectsByType<FirstPersonPlayer>(FindObjectsSortMode.None)) {
                var local = fridge.transform.InverseTransformPoint(p.transform.position);
                if (local.z > 0 && local.z < 2.8f && Mathf.Abs(local.x) < 1.3f) near = true;
            }
            d.open = Mathf.MoveTowards(d.open, near ? 1 : 0, Time.deltaTime * 3f); fridgeDoors[fridge] = d;
            d.door.localRotation = d.shut * Quaternion.Euler(0, -105f * Mathf.SmoothStep(0, 1, d.open), 0);
        }
        // A cool light inside the fridge so its shelves read even in a dim kitchen at night.
        static void FridgeLight(GameObject fridge) {
            if (fridge.transform.Find("Fridge light")) return;
            var l = new GameObject("Fridge light").AddComponent<Light>(); l.transform.SetParent(fridge.transform, false);
            l.transform.localPosition = new Vector3(0, 1.7f, .15f); l.type = LightType.Point; l.color = new Color(.85f, .93f, 1f); l.intensity = 1.4f; l.range = 1.6f; l.shadows = LightShadows.None;
        }
        void TickPantryDisplays() {
            foreach (var key in new List<GameObject>(pantryViews.Keys)) if (!key) pantryViews.Remove(key);
            foreach (var (pantry, pantryId, catalogId) in PantryObjects()) {
                if (!pantryViews.TryGetValue(pantry, out var views)) pantryViews[pantry] = views = new Dictionary<string, ShelfView>();
                var holds = KitchenState.ShelvesOf(Data, pantryId, catalogId);
                if (catalogId == "fridge") { SwingFridgeDoor(pantry); FridgeLight(pantry); }
                foreach (var spot in catalogId == "fridge" ? FridgeSpots : PantrySpots) {
                    // Shelves this storage doesn't hold (a pantry's cold shelves once you own a fridge) are emptied and can't be aimed at.
                    bool held = System.Array.IndexOf(holds, spot.shelf) >= 0;
                    var zone = pantry.transform.Find("Pantry shelf " + spot.shelf); if (zone) foreach (var c in zone.GetComponents<Collider>()) c.enabled = held;
                    if (!held) {
                        if (views.TryGetValue(spot.shelf, out var gone)) { foreach (var o in gone.Items) if (o) Destroy(o); gone.Items.Clear(); gone.Shown = 0; gone.Count = -1; if (gone.Label) gone.Label.gameObject.SetActive(false); }
                        continue;
                    }
                    var ingDef = Ingredients.Get(spot.ingredient);
                    if (ingDef != null && !string.IsNullOrEmpty(ingDef.Recipe) && !Game.State.Knows(ingDef.Recipe) && Data.Stock(spot.ingredient) == 0) continue;
                    if (!views.TryGetValue(spot.shelf, out var v)) views[spot.shelf] = v = new ShelfView();
                    int count = Data.Stock(spot.ingredient);
                    // Items are spaced to the shelf's width (2 across on a narrow shelf, 3 on a wide one), two rows deep.
                    int cols = spot.w < .5f ? 2 : 3; float spread = spot.w / (cols + .3f);
                    int show = count == 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(count / 4f), 1, cols * 2);
                    if (show != v.Shown) {
                        foreach (var o in v.Items) if (o) Destroy(o); v.Items.Clear();
                        for (int i = 0; i < show; i++) {
                            var food = KitchenArt.CreateItem(spot.kind, pantry.transform);
                            int col = i % cols, row = i / cols;
                            food.transform.localPosition = new Vector3(spot.x + (col - (cols - 1) / 2f) * spread, spot.y + row * (spot.kind == "RawSauce" ? 0f : .05f), row == 0 ? .1f : -.12f);
                            food.transform.localRotation = Quaternion.Euler(0, i * 37, 0);
                            var ps = pantry.transform.localScale; food.transform.localScale = new Vector3(1 / Mathf.Max(.01f, ps.x), 1 / Mathf.Max(.01f, ps.y), 1 / Mathf.Max(.01f, ps.z));   // pantries can be squashed on X
                            foreach (var c in food.GetComponentsInChildren<Collider>()) c.enabled = false;   // the shelf hitbox stays the target
                            v.Items.Add(food);
                        }
                        v.Shown = show;
                    }
                    // Labels live outside the (possibly squashed) pantry so they never shear; they are sized to the shelf,
                    // sit on the shelf edge nearest you, face you, and only show within a few metres.
                    // A small shelf-edge tag per ingredient (name and count), like a price tag, so a full fridge still reads.
                    if (!v.Label) v.Label = WorldCaption(transform, "", Vector3.zero, .0068f);
                    bool focused = pantryFocus.TryGetValue(pantry, out var f) && f.shelf == spot.shelf && Time.frameCount - f.frame <= 2;
                    if (count != v.Count || focused != v.Focused) {
                        v.Count = count; v.Focused = focused;
                        string tag = count == 0 ? "<color=#E1543B>" + spot.title.ToUpper() + "  OUT</color>" : spot.title.ToUpper() + "  <b>" + count + "</b>";
                        v.Label.text = focused ? "<color=#F2C94C>> " + tag + " <</color>" : tag;
                        // The aimed-at items lift a touch off the shelf.
                        foreach (var o in v.Items) if (o) { var lp = o.transform.localPosition; lp.y = spot.y + (o.transform.localPosition.y - spot.y > .03f ? .05f : 0) + (focused ? .025f : 0); o.transform.localPosition = lp; }
                    }
                    if (Game.Player && Game.Player.View) {
                        var eye = Game.Player.View.transform.position; var toEye = pantry.transform.InverseTransformPoint(eye);
                        float front = catalogId == "fridge" ? .45f : .5f;
                        var at = pantry.transform.TransformPoint(new Vector3(spot.x, spot.y - .04f, toEye.z >= 0 ? front : -front));
                        v.Label.transform.position = at; v.Label.transform.rotation = Quaternion.LookRotation(at - eye);
                        float width = pantry.transform.lossyScale.x; v.Label.transform.localScale = Vector3.one * Mathf.Clamp(width, .45f, 1f) * (v.Focused ? 1.25f : 1);
                        v.Label.gameObject.SetActive((at - eye).sqrMagnitude < 5.5f * 5.5f);
                    }
                }
            }
        }
    }
}
