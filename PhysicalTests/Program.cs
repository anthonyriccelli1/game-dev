using RestaurantCity;
using System.Text.Json;
int count=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
var g=new GameState{Cash=500};var r=g.Restaurant;var k=g.Kitchen;
Check(r.BuyRestaurant(g,out _),"purchase");k.EnsureStations(r);
int S(string id)=>k.Stations.First(s=>s.CatalogId==id).InstanceId;
int table=r.Layout.First(p=>p.CatalogId=="cafe_table").InstanceId;
void Act(string actor,string station,string action=""){Check(k.Act(g,actor,S(station),action,out var message),"act "+station+" "+action+": "+message);}
void Work(string actor,string station,float sec){Check(k.Work(g,actor,S(station),sec,out var msg),"work "+station+": "+msg);}
void Patty(){Act("player:0","pantry","protein");Act("player:0","prep_bench");Work("player:0","prep_bench",3);Act("player:0","prep_bench");Act("player:0","grill");k.Tick(g,8);Act("player:0","grill");}
Check(k.Stations.Count==6&&r.Layout.Count==7,"complete starter kit");
Act("player:0","pantry","protein");Act("player:0","prep_bench");Work("player:0","prep_bench",1);
Check(!k.Work(g,"player:1",S("prep_bench"),3,out _),"exclusive prep claim prevents doubling");
k.ReleaseWork("player:0");Work("player:1","prep_bench",2);Act("player:1","prep_bench");
Check(!k.Act(g,"player:0",S("prep_bench"),"",out _),"exclusive pickup");Act("player:1","grill");k.Tick(g,25);Act("player:0","grill");
Check(k.Hold("player:0").Kind==KitchenItemKind.BurntPatty,"burned protein visible");Check(k.Discard(g,"player:0",out _),"burn recovery");
Check(r.StartService(g,out _),"open service");k.StartShift(g);var order=r.AddCustomer(g,0,table,out _);Check(order!=null&&order.DishId=="burger","burger guest");
Act("player:1","plate_rack");Act("player:1","assembly");Patty();Act("player:0","assembly");Act("player:1","pantry","bun");Act("player:1","assembly");Act("player:0","assembly");
Check(k.RecipeOf(k.Hold("player:0"))=="burger","burger assembled");int before=g.Cash;
Check(k.Serve(g,"player:0",order.Id,out _),"physical serving");Check(g.Cash>before,"payment");int after=g.Cash;Check(!k.Serve(g,"player:1",order.Id,out _)&&g.Cash==after,"no double serving or payment");
r.Tick(g,9);k.Tick(g,.1f);Check(k.DirtyAtTable(table)==1,"meal produces dirty plate");Check(k.ClearTable(g,"player:1",table,out _),"clear dirty plate");Check(!k.ClearTable(g,"player:0",table,out _),"no double clearing");Act("player:1","sink");Work("player:1","sink",6);Check(k.CleanPlates==6,"finite plates conserved after wash");
r.Tick(g,8);g.RecipeUnlocked=true;Check(r.ToggleDish(g,"midnight",out _),"midnight recipe enters menu");var midnight=r.AddCustomer(g,1,table,out _);Check(midnight.DishId=="midnight","night guest preference");
Act("player:1","plate_rack");Act("player:1","assembly");Patty();Act("player:0","assembly");Act("player:0","pantry","bun");Act("player:0","assembly");Act("player:0","pantry","sauce");Act("player:0","prep_bench");Work("player:0","prep_bench",4);Act("player:0","prep_bench");Act("player:0","assembly");Act("player:0","assembly");Check(k.RecipeOf(k.Hold("player:0"))=="midnight","midnight physical recipe");Check(k.Serve(g,"player:0",midnight.Id,out _),"midnight served");
var report=k.FinishShift(g);Check(report.Served==2&&report.GrossSales>0&&report.IngredientCosts>0&&report.Comments.Count>0,"explained shift report");Check(object.ReferenceEquals(report,k.FinishShift(g)),"report idempotence");
var json=JsonSerializer.Serialize(g,new JsonSerializerOptions{IncludeFields=true});var loaded=JsonSerializer.Deserialize<GameState>(json,new JsonSerializerOptions{IncludeFields=true});loaded.SanitizeAfterLoad();Check(loaded.Version==3&&loaded.Kitchen.Items.Count>0,"physical state survives migration");Check(loaded.Flux==3&&loaded.FluxIntroduced,"old recipe receives one Flux reward");loaded.SanitizeAfterLoad();Check(loaded.Flux==3,"Flux migration cannot repeat");Check(loaded.Kitchen.SpendFlux(loaded,"research",out _)&&loaded.FluxResearch&&loaded.Flux==0,"permanent research");
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
Console.WriteLine($"PHYSICAL FINAL PASSED {count}");


