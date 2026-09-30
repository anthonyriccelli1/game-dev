using System;
namespace RestaurantCity {
    // Night health inspectors (Schedule I-style police). Rules only; InspectorPatrol draws and moves them.
    // Contraband = Zeeb's Midnight sauce you're still carrying (once it's in your pantry it's safe).
    // Stop and get searched: carried contraband is confiscated and fined; clean players walk away free.
    // Run and get caught: contraband confiscated, a bigger fine, and you're sent home. The restaurant is never touched.
    public static class Inspections {
        public const string Contraband = "midnight_sauce";
        public const int StopFine = 15, PerBottle = 4, RunFine = 40;
        public static int Carried(GameState g, string actor) {
            // Anything on you counts: the bag in your hand or stowed in your hotbar.
            var h = Hotbar.CarriedBag(g, actor);
            return h != null ? h.Components.FindAll(c => c == Contraband).Count : 0;
        }
        // Returns the fine actually taken (never more than the cash you have).
        public static int Search(GameState g, string actor, bool caughtRunning, out string message) {
            int bottles = Carried(g, actor), fine = 0;
            if (bottles > 0) {
                var bag = g.Kitchen.Hold(actor); bag.Components.RemoveAll(c => c == Contraband);
                if (bag.Components.Count == 0) g.Kitchen.Items.Remove(bag);
                fine = StopFine + PerBottle * bottles;
            }
            if (caughtRunning) fine += RunFine;
            fine = Math.Min(fine, Math.Max(0, g.Cash)); g.Cash -= fine;
            if (bottles == 0 && !caughtRunning) { message = "Inspector: \"All clear. Move along.\""; g.Emit("inspect_clear"); return 0; }
            message = (caughtRunning ? "Caught! " : "Searched. ") + (bottles > 0 ? "The inspector confiscated " + bottles + " bottle" + (bottles == 1 ? "" : "s") + " of Midnight sauce. " : "") +
                      "Fined $" + fine + "." + (caughtRunning ? " You've been sent home." : "");
            g.Emit("inspect_fine:" + fine + ":" + bottles + ":" + (caughtRunning ? 1 : 0));
            return fine;
        }
    }
}
