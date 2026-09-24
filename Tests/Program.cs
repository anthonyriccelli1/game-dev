using RestaurantCity;

int failed = 0, total = 0;
void Test(string name, Action action) {
    total++;
    try { action(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
}
void Check(bool value, string message) { if (!value) throw new Exception(message); }
GameState Ready() { var s = new GameState(); s.SetUpStand(); s.BuyIngredients(); return s; }
void Plate(GameState s, float seconds = 5) { s.Prepare(); s.UseGrill(); s.Tick(seconds); s.UseGrill(); }

Test("new player can afford stand and supplies", () => {
    var s = Ready(); Check(s.StandBuilt && s.Stock == 3 && s.Cash == 14, "expected stand, 3 ingredients, $14");
});
Test("stand cannot be purchased twice", () => {
    var s = Ready(); int cash = s.Cash; Check(!s.SetUpStand() && s.Cash == cash, "duplicate purchase charged");
});
Test("insufficient funds cannot create stock", () => {
    var s = new GameState { Cash = 5 }; Check(!s.BuyIngredients() && s.Stock == 0 && s.Cash == 5, "purchase must be rejected");
});
Test("prep consumes one ingredient once", () => {
    var s = Ready(); Check(s.Prepare() && !s.Prepare() && s.Stock == 2, "prep consumed wrong amount");
});
Test("grill refuses undercooked pickup", () => {
    var s = Ready(); s.Prepare(); s.UseGrill(); s.Tick(2); Check(!s.UseGrill() && s.Food == FoodStage.Cooking, "raw dish was collected");
});
Test("perfect burger pays once", () => {
    var s = Ready(); Plate(s); Check(s.Serve() && s.Cash == 26 && s.Served == 1, "perfect burger must earn $12");
    Check(!s.Serve() && s.Cash == 26, "duplicate payout");
});
Test("burned burger cannot be served and can be discarded", () => {
    var s = Ready(); Plate(s, 12); Check(!s.Serve(), "burned food served"); s.Discard(); Check(s.Food == FoodStage.Empty, "discard failed");
});
Test("customers leave when patience expires", () => {
    var s = Ready(); s.Tick(1); s.Tick(70); Check(s.Missed == 1 && !s.HasOrder, "expired order survived");
});
Test("night order earns premium", () => {
    var s = Ready(); s.Clock = 160; Plate(s); s.Serve(); Check(s.Cash == 32, "night burger must pay $18");
});
Test("recipe requires night and defeated guard", () => {
    var s = Ready(); Check(!s.ClaimRecipe(true), "day recipe claim allowed"); s.Clock = 170;
    Check(!s.ClaimRecipe(false), "guard bypass allowed"); Check(s.ClaimRecipe(true) && !s.ClaimRecipe(true), "recipe claim is not one-time");
});
Test("recipe increases next burger value", () => {
    var s = Ready(); s.Clock = 170; s.ClaimRecipe(true); Plate(s); s.Serve(); Check(s.Cash == 41, "signature night burger must pay $27");
});
Test("respawn preserves permanent unlocks and limits loss", () => {
    var s = Ready(); s.Cash = 7; s.RecipeUnlocked = true; s.Respawn();
    Check(s.Cash == 0 && s.StandBuilt && s.RecipeUnlocked && s.Health == 100 && !s.HasOrder, "respawn violated recovery rules");
});
Test("emergency stock prevents an economic dead end", () => {
    var s = new GameState { Cash = 0, StandBuilt = true }; Check(s.RequestHelp() && s.Stock == 1, "help did not provide one ingredient");
    Check(!s.RequestHelp(), "help could be farmed");
});
Test("load validation removes invalid and transient state", () => {
    var s = new GameState { StandBuilt = true, Cash = -50, Stock = -1, Clock = float.NaN, Health = -1, Food = FoodStage.Plated, HasOrder = true };
    s.SanitizeAfterLoad(); Check(s.Cash == 0 && s.Stock == 0 && s.Clock == 0 && s.Health == 100 && s.Food == FoodStage.Empty && !s.HasOrder, "invalid save accepted");
});
Test("time wraps between nights and mornings", () => {
    var s = Ready(); s.Clock = 239; s.Tick(2); Check(!s.IsNight && s.Day == 2 && Math.Abs(s.Clock - 1) < .01f, "day rollover incorrect");
});
Test("shopping reserves enough cash to build the stand", () => {
    var s = new GameState(); for (int i = 0; i < 8; i++) s.BuyIngredients();
    Check(s.Cash >= 10 && s.SetUpStand(), "shopping trapped player without setup funds");
});
Test("pre-stand death preserves setup funds", () => {
    var s = new GameState(); for (int i = 0; i < 8; i++) s.Respawn();
    Check(s.Cash >= 10 && s.SetUpStand(), "death trapped player without setup funds");
});
Test("rival territory excludes warehouse and street", () => {
    Check(!EncounterRules.InTerritory(16, 15.9f), "warehouse front is unsafe");
    Check(!EncounterRules.InTerritory(11.6f, 11), "street is unsafe");
    Check(EncounterRules.InTerritory(11.6f, 18), "marked alley is safe");
});
Console.WriteLine($"RESULT: {total - failed}/{total} passed");
return failed == 0 ? 0 : 1;
