using RestaurantCity;
using System.Text.Json;
int count=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
var g=new GameState{Cash=500};var r=g.Restaurant;var k=g.Kitchen;
Check(r.BuyRestaurant(g,out _),"purchase");k.EnsureStations(r);
int S(string id)=>k.Stations.First(s=>s.CatalogId==id).InstanceId;
int table=r.Layout.First(p=>p.CatalogId=="cafe_table").InstanceId;
void Act(string actor,string station,string action=""){Check(k.Act(g,actor,S(station),action,out var message),"act "+station+" "+action+": "+message);}
void Work(string actor,string station,float sec){Check(k.Work(g,actor,S(station),sec,out var msg),"work "+station+": "+msg);}
void Patty(){Act("player:0","pantry","protein");Act("player:0","grill");k.Tick(g,8);Act("player:0","grill");}
Check(k.Stations.Count==6&&r.Layout.Count==7,"complete starter kit");
Act("player:0","pantry","greens");Act("player:0","prep_bench");Work("player:0","prep_bench",1);
Check(!k.Work(g,"player:1",S("prep_bench"),3,out _),"exclusive prep claim prevents doubling");
k.ReleaseWork("player:0");Work("player:1","prep_bench",2);Act("player:1","prep_bench");
Check(!k.Act(g,"player:0",S("prep_bench"),"",out _),"exclusive pickup");Check(k.Discard(g,"player:1",out _),"discard chopped greens to reset for the burn test");
Act("player:1","pantry","protein");Act("player:1","grill");k.Tick(g,25);Act("player:0","grill");
Check(k.Hold("player:0").Kind==KitchenItemKind.BurntPatty,"burned protein visible");Check(k.Discard(g,"player:0",out _),"burn recovery");
Check(r.StartService(g,out _),"open service");k.StartShift(g);var order=r.AddCustomer(g,0,table,out _);Check(order!=null&&order.DishId=="burger","burger guest");
Act("player:1","plate_rack");Act("player:1","assembly");Patty();Act("player:0","assembly");Act("player:1","pantry","bun");Act("player:1","assembly");Act("player:0","assembly");
Check(k.RecipeOf(k.Hold("player:0"))=="burger","burger assembled");int before=g.Cash;
Check(k.Serve(g,"player:0",order.Id,out _),"physical serving");Check(g.Cash>before,"payment");int after=g.Cash;Check(!k.Serve(g,"player:1",order.Id,out _)&&g.Cash==after,"no double serving or payment");
r.Tick(g,9);k.Tick(g,.1f);Check(k.DirtyAtTable(table)==1,"meal produces dirty plate");Check(k.ClearTable(g,"player:1",table,out _),"clear dirty plate");Check(!k.ClearTable(g,"player:0",table,out _),"no double clearing");Act("player:1","sink");Work("player:1","sink",6);Check(k.CleanPlates==6,"finite plates conserved after wash");
r.Tick(g,8);g.RecipeUnlocked=true;Check(r.ToggleDish(g,"midnight",out _),"midnight recipe enters menu");var midnight=r.AddCustomer(g,1,table,out _);Check(midnight.DishId=="midnight","night guest preference");
Act("player:1","plate_rack");Act("player:1","assembly");Patty();Act("player:0","assembly");Act("player:0","pantry","bun");Act("player:0","assembly");Act("player:0","pantry","sauce");Act("player:0","prep_bench");Work("player:0","prep_bench",4);Act("player:0","prep_bench");Act("player:0","assembly");Act("player:0","assembly");Check(k.RecipeOf(k.Hold("player:0"))=="midnight","midnight physical recipe");Check(k.Serve(g,"player:0",midnight.Id,out _),"midnight served");
var report=k.FinishShift(g);Check(report.Served==2&&report.GrossSales>0&&report.IngredientCosts>0&&report.Comments.Count>0,"explained shift report");Check(object.ReferenceEquals(report,k.FinishShift(g)),"report idempotence");
var json=JsonSerializer.Serialize(g,new JsonSerializerOptions{IncludeFields=true});var loaded=JsonSerializer.Deserialize<GameState>(json,new JsonSerializerOptions{IncludeFields=true});loaded.SanitizeAfterLoad();Check(loaded.Version==4&&loaded.Kitchen.Items.Count>0,"physical state survives migration");Check(loaded.Flux==3&&loaded.FluxIntroduced,"old recipe receives one Flux reward");loaded.SanitizeAfterLoad();Check(loaded.Flux==3,"Flux migration cannot repeat");Check(loaded.Kitchen.SpendFlux(loaded,"research",out _)&&loaded.FluxResearch&&loaded.Flux==0,"permanent research");
var fresh=new GameState{Cash=150};fresh.Restaurant.BuyRestaurant(fresh,out _);fresh.Kitchen.EnsureStations(fresh.Restaurant);fresh.Restaurant.Produce=0;fresh.Restaurant.Protein=0;int pantry=fresh.Kitchen.Stations.First(s=>s.CatalogId=="pantry").InstanceId;
Check(!fresh.Kitchen.Act(fresh,"player:0",pantry,"protein",out _)&&fresh.Kitchen.Hold("player:0")==null,"empty stock fails without ghost item");Check(fresh.Restaurant.RequestSupplyHelp(fresh,out _),"emergency produce recovery");
var kr=fresh.Kitchen;int rack=kr.Stations.First(s=>s.CatalogId=="plate_rack").InstanceId,sink=kr.Stations.First(s=>s.CatalogId=="sink").InstanceId;
for(int i=0;i<6;i++){Check(kr.Act(fresh,"player:"+i,rack,"",out _),"finite plate pickup");var plate=kr.Hold("player:"+i);plate.Kind=KitchenItemKind.DirtyPlate;}
Check(!kr.Act(fresh,"player:7",rack,"",out _),"all plates dirty blocks rack");Check(kr.Act(fresh,"player:0",sink,"",out _),"dirty deposit");Check(kr.Work(fresh,"player:0",sink,6,out _)&&kr.CleanPlates==1,"wash restores exhausted plate supply");
fresh.Restaurant.Rank=2;fresh.Restaurant.Satisfaction=20;for(int i=0;i<2;i++){kr.StartShift(fresh);fresh.Restaurant.Lost++;kr.FinishShift(fresh);}Check(fresh.Restaurant.Rank==1&&kr.LastReport.Comments.Any(x=>x.Contains("two consecutive")),"rank drop only after two poor shifts with explanation");
var saladGame=new GameState{Cash=500,Clock=160};saladGame.Restaurant.BuyRestaurant(saladGame,out _);var sk=saladGame.Kitchen;var sr=saladGame.Restaurant;sk.EnsureStations(sr);sr.ActiveMenu=new List<string>{"salad"};sr.StartService(saladGame,out _);sk.StartShift(saladGame);
int Sid(string id)=>sk.Stations.First(s=>s.CatalogId==id).InstanceId;int saladTable=sr.Layout.First(p=>p.CatalogId=="cafe_table").InstanceId;
var saladOrder=sr.AddCustomer(saladGame,2,saladTable,out _);
Check(sk.Act(saladGame,"player:0",Sid("plate_rack"),"",out _)&&sk.Act(saladGame,"player:0",Sid("assembly"),"",out _),"salad plate setup");
Check(sk.Act(saladGame,"player:1",Sid("pantry"),"greens",out _)&&sk.Act(saladGame,"player:1",Sid("prep_bench"),"",out _)&&sk.Work(saladGame,"player:1",Sid("prep_bench"),3,out _)&&sk.Act(saladGame,"player:1",Sid("prep_bench"),"",out _)&&sk.Act(saladGame,"player:1",Sid("assembly"),"",out _),"salad ingredient chain");
Check(sk.Act(saladGame,"player:0",Sid("assembly"),"",out _)&&sk.RecipeOf(sk.Hold("player:0"))=="salad","salad pickup");
Check(sk.Serve(saladGame,"player:0",saladOrder.Id,out var saleMessage)&&saleMessage.Contains("Night premium"),"night shift premium");
Check(sr.Hire(saladGame,"ember",out _),"hire worker");var worker=sr.Workers[0];worker.Energy=10;saladGame.Flux=1;Check(sk.SpendFlux(saladGame,"ember",out _)&&worker.Energy==45&&saladGame.Flux==0,"Flux energy alternative");worker.Job=StaffJob.Off;sk.Tick(saladGame,10);Check(worker.Energy==51,"off duty recovery");
Console.WriteLine("PHYSICAL TESTS PASSED "+count);
var equipment=new GameState{Cash=1000};equipment.Restaurant.BuyRestaurant(equipment,out _);equipment.Kitchen.EnsureStations(equipment.Restaurant);
var kitRack=equipment.Restaurant.Layout.First(p=>p.CatalogId=="plate_rack");Check(!equipment.Restaurant.Sell(equipment,kitRack.InstanceId,out _),"last essential rack cannot be sold");int kitCount=equipment.Restaurant.Layout.Count;equipment.Kitchen.EnsureStations(equipment.Restaurant);Check(equipment.Restaurant.Layout.Count==kitCount,"starter kit grant is idempotent");
var hot=new KitchenItem{Id=900,Kind=KitchenItemKind.CookedPatty,Holder="player:0",Quality=1};equipment.Kitchen.Items.Add(hot);equipment.Kitchen.Tick(equipment,60);Check(hot.Quality<1,"carried cooked food cools with time");

// Stage A verification: Preview==Act for every station/hand combination touched, old-save migration,
// two actors racing for the last resource, and order-independent recipe matching.
var pv=new GameState{Cash=1000};pv.Restaurant.BuyRestaurant(pv,out _);pv.Kitchen.EnsureStations(pv.Restaurant);
int PvS(string id)=>pv.Kitchen.Stations.First(s=>s.CatalogId==id).InstanceId;
void CheckPreviewMatchesAct(string actor,int station,string subId,string label){
    var preview=pv.Kitchen.Preview(pv,actor,station,subId);
    bool acted=pv.Kitchen.Act(pv,actor,station,subId,out var actMessage);
    Check(preview.Allowed==acted,"Preview.Allowed matches Act result: "+label);
    if(preview.Allowed)Check(!string.IsNullOrEmpty(actMessage),"Act executed a real outcome for: "+label);
    else Check(actMessage==preview.FailReason,"Act's failure message matches Preview.FailReason: "+label);
}
CheckPreviewMatchesAct("player:0",PvS("pantry"),"protein","take raw patty");
CheckPreviewMatchesAct("player:0",PvS("grill"),"","place raw patty on grill");
pv.Kitchen.Tick(pv,8);
CheckPreviewMatchesAct("player:0",PvS("plate_rack"),"","take clean plate while grill cooks");
CheckPreviewMatchesAct("player:0",PvS("grill"),"","slide cooked patty onto held plate");
CheckPreviewMatchesAct("player:0",PvS("pantry"),"bun","add bun onto held plate from shelf");
CheckPreviewMatchesAct("player:0",PvS("pantry"),"sauce","sauce shelf still locked");
CheckPreviewMatchesAct("player:1",PvS("pantry"),"protein","second actor can act independently");

// Migration: an old save serialized with the retired bitmask "Parts" field converts to named components.
var legacy=new GameState{Cash=200};legacy.Restaurant.BuyRestaurant(legacy,out _);legacy.Kitchen.EnsureStations(legacy.Restaurant);
int legacyAssembly=legacy.Kitchen.Stations.First(s=>s.CatalogId=="assembly").InstanceId;
legacy.Kitchen.Items.Add(new KitchenItem{Id=5000,Kind=KitchenItemKind.Plate,Parts=3,Holder="station:"+legacyAssembly});
legacy.Version=3;
var legacyJson=JsonSerializer.Serialize(legacy,new JsonSerializerOptions{IncludeFields=true});
var migrated=JsonSerializer.Deserialize<GameState>(legacyJson,new JsonSerializerOptions{IncludeFields=true});
migrated.SanitizeAfterLoad();
var migratedItem=migrated.Kitchen.Items.First(i=>i.Id==5000);
Check(migratedItem.Components.Contains("bun")&&migratedItem.Components.Contains("cooked_patty")&&migratedItem.Components.Count==2,"old Parts=3 bitmask migrates to bun+cooked_patty components");
Check(migrated.Kitchen.RecipeOf(migratedItem)=="burger","migrated components resolve to the same burger recipe");
Check(migrated.Version==4,"save version bumped after migration");

// Race: two actors going for the last clean plate must not duplicate it.
var race=new GameState{Cash=200};race.Restaurant.BuyRestaurant(race,out _);race.Kitchen.EnsureStations(race.Restaurant);
race.Kitchen.CleanPlates=1;
int raceRack=race.Kitchen.Stations.First(s=>s.CatalogId=="plate_rack").InstanceId;
bool firstTook=race.Kitchen.Act(race,"player:0",raceRack,"",out _);
bool secondTook=race.Kitchen.Act(race,"player:1",raceRack,"",out _);
Check(firstTook&&!secondTook,"only one actor gets the last clean plate, no duplication");
Check(race.Kitchen.Items.Count(i=>i.Kind==KitchenItemKind.Plate)==1,"exactly one plate item exists after the race");

// Race: two actors reaching for the same pantry shelf ingredient at once both succeed independently
// (stock allowing) without corrupting shared stock.
var shelfRace=new GameState{Cash=200};shelfRace.Restaurant.BuyRestaurant(shelfRace,out _);shelfRace.Kitchen.EnsureStations(shelfRace.Restaurant);
int shelfRacePantry=shelfRace.Kitchen.Stations.First(s=>s.CatalogId=="pantry").InstanceId;
int proteinBefore=shelfRace.Restaurant.Protein;
Check(shelfRace.Kitchen.Act(shelfRace,"player:0",shelfRacePantry,"protein",out _),"first actor takes protein from the shared shelf");
Check(shelfRace.Kitchen.Act(shelfRace,"player:1",shelfRacePantry,"protein",out _),"second actor independently takes protein from the same shelf");
Check(shelfRace.Restaurant.Protein==proteinBefore-2,"shared stock decremented exactly once per actor, no duplication");
Check(shelfRace.Kitchen.Hold("player:0")!=shelfRace.Kitchen.Hold("player:1")&&shelfRace.Kitchen.Hold("player:0").Id!=shelfRace.Kitchen.Hold("player:1").Id,"each actor holds their own distinct item");

// Recipe matching is order-independent: adding the same components in a different sequence still matches.
var orderA=new List<string>{"bun","cooked_patty"};var orderB=new List<string>{"cooked_patty","bun"};
Check(RecipeBook.Match(orderA)=="burger"&&RecipeBook.Match(orderB)=="burger","component order does not affect recipe matching");
Check(RecipeBook.Match(new List<string>{"midnight_sauce","bun","cooked_patty"})=="midnight","three-component midnight recipe matches regardless of add order");
Check(RecipeBook.Match(new List<string>{"chopped_greens"})=="salad"&&RecipeBook.Match(new List<string>{"bun"})=="","salad matches alone; a lone bun matches no recipe yet");

Console.WriteLine($"PHYSICAL FINAL PASSED {count}");


