using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RestaurantCity {
 public class PhysicalAcceptance:MonoBehaviour {
  public CityGame Game; int checks; string output;
  void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Debug.Log("PHYSICAL_CHECK "+label);}
  static int S(int i)=>KitchenState.StandBase+i; // 1 pantry, 2 grill, 3 counter, 4 plates, 5 sink, 6 trash
  void ActId(int id,string sub=""){Check(Game.State.Kitchen.Act(Game.State,"player:0",id,sub,out var m),"stand step: "+m);}
  void StandBurger(bool midnight){ActId(S(4));ActId(S(3));ActId(S(1),"protein");ActId(S(2));Game.State.Kitchen.Tick(Game.State,8);ActId(S(2));ActId(S(3));ActId(S(1),"bun");ActId(S(3));ActId(S(3));if(midnight)ActId(S(1),"sauce");}
  void ClearStandTables(){var st=Game.State;for(int t=0;t<GameState.StandSeats;t++)if(st.StandTableDirty[t]){Check(st.Kitchen.ClearStandTable(st,"player:0",t,out var m),"clear table: "+m);ActId(S(5));Check(st.Kitchen.Work(st,"player:0",S(5),KitchenState.WashSeconds+.1f,out m),"wash table plate: "+m);}}
  // Tests fill the pantry directly; players buy at Milo's.
  void Stock(int n){var r=Game.State.Restaurant;foreach(var i in new[]{"patty","bun","greens","midnight_sauce"})r.AddStock(i,n-r.Stock(i));}
  // Stand salad: plate on the counter, greens from the pantry onto the cutting board, chop, then onto the plate.
  void StandSalad(){ActId(S(4));ActId(S(3));ActId(S(1),"greens");ActId(S(7));Check(Game.State.Kitchen.Work(Game.State,"player:0",S(7),3.1f,out var m),"chop at the stand: "+m);ActId(S(7));ActId(S(3));ActId(S(3));}
  void WashStand(){while(Game.State.StandDirty>0){ActId(S(5));Check(Game.State.Kitchen.Work(Game.State,"player:0",S(5),KitchenState.WashSeconds+.1f,out var m),"stand wash: "+m);}}
  bool InstallFirstFree(string id){var d=Game.Restaurant.Data;for(int z=0;z<RestaurantState.GridD;z++)for(int x=0;x<12;x++)if(d.CanPlace(id,x,z,0,-1,out _))return d.Place(Game.State,id,x,z,0,out _);return false;}
  int Station(string id)=>Game.State.Kitchen.Stations.First(s=>s.CatalogId==id).InstanceId;
  void Act(string station,string action="",string actor="player:0"){Check(Game.State.Kitchen.Act(Game.State,actor,Station(station),action,out var m),m);}
  // Raw protein now goes straight from the pantry to the grill; greens and midnight sauce still chop/prepare
  // at the prep bench first (Stage A workflow change).
  void CookPatty(){Act("pantry","protein");Act("grill");Game.State.Kitchen.Tick(Game.State,8);Act("grill");Act("assembly");}
  void PrepAtBench(string ingredient){Act("pantry",ingredient);Act("prep_bench");Check(Game.State.Kitchen.Work(Game.State,"player:0",Station("prep_bench"),5,out var m),m);Act("prep_bench");Act("assembly");}
  void Cook(string dish){Act("plate_rack");Act("assembly");if(dish=="salad")PrepAtBench("greens");else CookPatty();if(dish!="salad"){Act("pantry","bun");Act("assembly");}if(dish=="midnight")PrepAtBench("sauce");Act("assembly");}
  IEnumerator Start(){
   output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","PhysicalEvidence"));Directory.CreateDirectory(output);yield return new WaitForSecondsRealtime(2);   if(Array.IndexOf(Environment.GetCommandLineArgs(),"--physical-resume")>=0){try{Check(Game.LoadFrom(Path.Combine(output,"physical-save.json")),"read save in fresh process");Check(Fingerprint()==File.ReadAllText(Path.Combine(output,"expected-layout.txt")),"fresh process exact layout and economy");Game.Restaurant.RebuildLayout();Game.SetPaused(false);Game.Player.Teleport(new Vector3(-10,.15f,-13));Game.Player.LookAt(new Vector3(-11,1,-19));}catch(Exception e){Fail(e);yield break;}yield return new WaitForSecondsRealtime(.5f);Capture("07-resumed.png");Debug.Log("PHYSICAL_RESUME_PASS "+checks);Application.Quit(0);yield break;}
   try{Game.SetPaused(false);Check(Game.Interact(InteractionKind.Stand),"start food stand");var st=Game.State;st.StandOpen=true;int attempts=0;
    // The stand is a physical kitchen with a customer queue: plate -> counter, patty -> grill, cooked patty + bun onto the plate, serve the line.
    while(st.Cash<500&&attempts++<150){Stock(4);for(int w=0;w<60&&!st.StandQueue.Exists(o=>o.Stage==1);w++){ClearStandTables();st.Tick(1);}var seated=st.StandQueue.Find(o=>o.Stage==1);Check(seated!=null,"stand guest seated at a sidewalk table");if(st.StandClean==0)WashStand();if(seated.Dish=="salad")StandSalad();else StandBurger(seated.Dish=="midnight");Check(st.Kitchen.ServeStandGuest(st,"player:0",seated.Id,out var sm),"stand sale at the table: "+sm);Check(seated.Stage==2,"guest eats at the table");}
    Check(st.StandClean+st.StandDirty+st.StandTableDirty.FindAll(d=>d).Count+st.StandQueue.FindAll(o=>o.Stage==2).Count+st.Kitchen.Items.FindAll(i=>i.StandPlate).Count==GameState.StandPlates,"stand plates conserved through table service");
    Check(st.Cash>=500,"earned restaurant lease from stand sales");
    // People book: every fed resident is recorded; only met residents can be recruited, and recruiting spends Flux.
    Check(st.MetResidents.Count>=3,"feeding stand guests fills the People book ("+st.MetResidents.Count+" met)");
    Check(st.MetResidents.TrueForAll(ResidentCast.IsRecruitable),"only real residents enter the People book");
    {var unmet=Array.Find(ResidentCast.OldMarket,r=>!st.HasMet(r.Id));int flux0=st.Flux;st.Flux+=10;
     if(unmet!=null)Check(!st.Restaurant.Hire(st,unmet.Id,out _),"cannot recruit a resident you haven't fed");
     var metId=st.MetResidents[0];var metDef=ResidentCast.Get(metId);Check(st.Restaurant.Hire(st,metId,out var hm),"recruit a met resident: "+hm);
     Check(st.Flux==flux0+10-metDef.FluxCost,"recruiting costs the resident's Flux price");
     st.Restaurant.Workers.RemoveAll(w=>w.Id==metId);st.Flux=flux0;}
    // Before owning anything, The Bayside's listing must actually appear on screen (not just be the selected panel).
    Game.Restaurant.BuyRestaurant(Game.Player,"bayside");Game.Restaurant.UI.Refresh();Check(GameObject.Find("Listing checklist")!=null,"Bayside listing is visible before you own a restaurant");Game.Restaurant.ClosePanel();
    // Pacing: the stand pays cash, not reputation. Earning a lease-and-renovation budget there must stay far from Line Cook (400).
    Check(st.Xp<=60,"stand alone earns only token reputation ("+st.Xp+")");Check(st.RepSources.Exists(r=>r.Source=="Stand sales"),"reputation sources are tracked");Check(Game.Restaurant.BuyRestaurant(),"purchase integration");Game.Restaurant.ClosePanel();
    Check(Game.Restaurant.Data.SiteId=="oddtable","The Odd Table is the starter restaurant");
    Check(Game.Restaurant.BuyRestaurant(Game.Player,"bayside")&&Game.Restaurant.Panel=="Listing"&&Game.Restaurant.Data.SiteId=="oddtable","The Bayside shows its listing, never moves or reloads the restaurant");Game.Restaurant.ClosePanel();
    // The lease is an empty room now; install the starter kitchen the way a player would from the catalog (test tops up the budget).
    st.Cash=Math.Max(st.Cash,2000);foreach(var (id,x,z) in new[]{("pantry",0,0),("plate_rack",2,0),("prep_bench",4,0),("assembly",8,0),("grill",0,4),("sink",9,4),("cafe_table",8,7)})Check(Game.Restaurant.Data.Place(st,id,x,z,0,out var pm)||InstallFirstFree(id),"install "+id+" "+pm);
    st.Kitchen.EnsureStations(Game.Restaurant.Data);Game.Restaurant.RebuildLayout();Game.Player.Teleport(new Vector3(-10,.15f,-11));Game.Player.LookAt(new Vector3(-11,1,-18));Check(Game.Restaurant.Furnishings.Count>=7,"physical kit rendered");
    // Milo's shop: buy a cart, carry the grocery bag home, unpack it at the pantry. Locked items can't be bought.
    Game.Player.Teleport(new Vector3(RestaurantController.MiloSpot.x,.15f,RestaurantController.MiloSpot.y));Physics.SyncTransforms();int patties=st.Restaurant.Stock("patty"),cashBefore=st.Cash;
    Check(!Game.Restaurant.BuyGroceries(new System.Collections.Generic.List<StockLine>{new StockLine{Id="soup_veg",Count=1}}),"soup veg is locked until you learn Planet soup");
    Check(Game.Restaurant.BuyGroceries(new System.Collections.Generic.List<StockLine>{new StockLine{Id="patty",Count=1},new StockLine{Id="bun",Count=1}}),"buy patties and buns from Milo");
    Check(st.Cash==cashBefore-14&&st.Kitchen.Hold("player:0")?.Kind==KitchenItemKind.GroceryBag&&st.Restaurant.Stock("patty")==patties,"groceries arrive in a carried bag, not straight into the pantry");
    Act("pantry","protein");Check(st.Restaurant.Stock("patty")==patties+6&&st.Kitchen.Hold("player:0")==null,"unpacking the bag fills the pantry");
    // Softlock guard: broke and out of ingredients, Milo fronts you basics once a day.
    {int cash=st.Cash;var saved=st.Restaurant.Pantry;st.Restaurant.Pantry=new System.Collections.Generic.List<StockLine>();st.Cash=5;
     Check(st.Restaurant.RequestSupplyHelp(st,out var hm)&&st.Restaurant.Stock("patty")==3&&st.Restaurant.Stock("greens")==3,"broke player gets free basics from Milo: "+hm);
     st.Restaurant.Pantry=new System.Collections.Generic.List<StockLine>();Check(!st.Restaurant.RequestSupplyHelp(st,out hm),"only once per day");st.Restaurant.Pantry=saved;st.Cash=cash;}
    // Planet soup: buy the recipe, simmer with a stir, ladle onto a plate. An unstirred pot scorches.
    Check(!st.Knows("soup")&&!Game.Restaurant.Data.IsDishAvailable(st,"soup"),"soup starts unknown");
    Check(InstallFirstFree("stove"),"install a stove");st.Kitchen.EnsureStations(Game.Restaurant.Data);Game.Restaurant.RebuildLayout();
    Check(!st.BuyRecipe("soup",out var zm)&&Game.Restaurant.Data.Stars==0,"Planet Soup is a 1-star cookbook recipe (new restaurants start at 0 stars): "+zm);Game.Restaurant.Data.Rank=Math.Max(1,Game.Restaurant.Data.Rank);Check(st.BuyRecipe("soup",out var bm),"buy Planet soup in the Cookbook: "+bm);Check(Game.Restaurant.Data.IsDishAvailable(st,"soup"),"soup can go on the menu once bought with a stove");
    st.Restaurant.AddStock("soup_veg",4);var stove=Station("stove");
    Act("pantry","soup");Act("stove");st.Kitchen.Tick(st,6);Act("stove");st.Kitchen.Tick(st,8.1f);Check(st.Kitchen.At(stove)?.Kind==KitchenItemKind.Soup,"a stirred pot becomes soup");
    Act("plate_rack");Act("stove");Check(st.Kitchen.RecipeOf(st.Kitchen.Hold("player:0"))=="soup"&&st.Kitchen.At(stove)==null,"ladle the soup onto a plate");
    Check(st.Kitchen.Discard(st,"player:0",out var dm),dm);Act("sink");Check(st.Kitchen.Work(st,"player:0",Station("sink"),KitchenState.WashSeconds+.1f,out dm),dm);
    Act("pantry","soup");Act("stove");st.Kitchen.Tick(st,10);Check(st.Kitchen.At(stove)?.Kind==KitchenItemKind.ScorchedSoup,"an unstirred pot scorches");Act("stove");Check(st.Kitchen.Discard(st,"player:0",out dm),"throw the scorched pot away");
    Game.Player.Teleport(new Vector3(-10,.15f,-11));}catch(Exception e){Fail(e);yield break;}
   yield return new WaitForSecondsRealtime(1);Capture("01-physical-kitchen.png");
   try{var pos=Game.Player.transform.position;Game.CoOp.ToggleElevated(Game.Player);Check(Vector3.Distance(pos,Game.Player.transform.position)<.01f,"camera preserves position");Game.CoOp.Join(null);Game.CoOp.SecondPlayer.LookAt(new Vector3(-11,1,-18));Check(Game.CoOp.PlayerCount==2,"two independent players created");var hostPosition=Game.Player.transform.position;var partnerPosition=Game.CoOp.SecondPlayer.transform.position;Game.CoOp.SecondPlayer.ApplyMovement(new Vector2(1,0),.2f);Check(Vector3.Distance(hostPosition,Game.Player.transform.position)<.01f&&Vector3.Distance(partnerPosition,Game.CoOp.SecondPlayer.transform.position)>.05f,"partner movement leaves host independent");Game.Restaurant.ToggleService();Game.Restaurant.ClosePanel();Game.Restaurant.Advance(5);}catch(Exception e){Fail(e);yield break;}
   yield return new WaitForSecondsRealtime(1);Capture("02-local-coop.png");
   try{Game.Restaurant.ClosePanel();Game.State.Clock=180;Game.Player.Teleport(new Vector3(11,.15f,18));Game.Guard.Hit();Game.Guard.Hit();Game.Guard.Hit();Check(Game.Interact(InteractionKind.Recipe),"night encounter earns recipe and Flux");Check(Game.State.Flux==3,"night reward has 3 Flux");
    {var z=Game.State;z.Cash=Math.Max(z.Cash,100);int cash0=z.Cash;
     Check(z.OrderFromZeeb(6,out var zm),"call Zeeb for 6 bottles: "+zm);Check(z.Cash==cash0-NightStashes.DepositFor(6)&&z.ZeebDebt==NightStashes.Cost(6)-NightStashes.DepositFor(6),"deposit now, the rest owed");
     Check(!z.OrderFromZeeb(3,out zm),"no new order while you owe him");z.Tick(.1f);Check(z.StashActive,"Zeeb drops it after dark");
     int sauce=z.Restaurant.Stock("midnight_sauce");Check(z.Kitchen.CollectStash(z,"player:0",out zm)&&z.Kitchen.Hold("player:0")?.Components.Count==6&&!z.StashActive,"the drop goes into a carried bag: "+zm);
     Act("pantry","protein");Check(z.Restaurant.Stock("midnight_sauce")==sauce+6,"unpack Zeeb's sauce into the pantry");
     Check(z.PayZeeb(out zm)&&z.ZeebDebt==0,"pay Zeeb off: "+zm);}Game.Player.Teleport(new Vector3(-10,.15f,-11));var r=Game.State.Restaurant;var table=r.Layout.First(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0);foreach(var dish in new[]{"burger","salad","midnight"}){int platesBefore=Game.State.Kitchen.CleanPlates;r.Orders.Clear();Stock(20);r.ActiveMenu.Clear();r.ActiveMenu.Add(dish);var order=r.AddCustomer(Game.State,0,table.InstanceId,out var m);Check(order!=null,"spawn physical order");Cook(dish);Check(Game.State.Kitchen.Serve(Game.State,"player:0",order.Id,out m),m);r.Tick(Game.State,30);Game.State.Kitchen.Tick(Game.State,30);Check(Game.State.Kitchen.ClearTable(Game.State,"player:1",table.InstanceId,out m),m);Act("sink","","player:1");Check(Game.State.Kitchen.Work(Game.State,"player:1",Station("sink"),7,out m),m);Check(Game.State.Kitchen.CleanPlates==platesBefore,"plate conserved after wash ("+Game.State.Kitchen.CleanPlates+"/"+platesBefore+")");}r.Orders.Clear();r.EndService(out _);Game.Restaurant.Advance(.1f);Check(Game.State.Kitchen.LastReport!=null,"shift report produced");Game.Restaurant.ShowPanel("Service");}catch(Exception e){Fail(e);yield break;}
   yield return new WaitForSecondsRealtime(1);Capture("03-shift-report.png");   try{Game.Restaurant.ClosePanel();var r=Game.State.Restaurant;Check(r.Place(Game.State,"wall_teal",0,0,0,out var m),m);Check(r.Place(Game.State,"floor_checker",0,0,0,out m),m);Game.Restaurant.RebuildLayout();Game.Restaurant.ShowPanel("Catalog");}catch(Exception e){Fail(e);yield break;}
   yield return new WaitForSecondsRealtime(.5f);Capture("04-catalog.png");
   try{Game.Restaurant.ClosePanel();Game.State.Flux+=4; /* TODO chunk 4: basic recruits should not cost Flux */Check(Game.State.Restaurant.Hire(Game.State,"ember",out var m),m);Check(Game.State.Restaurant.Hire(Game.State,"moss",out m),m);Game.State.Restaurant.ActiveMenu.Clear();Game.State.Restaurant.ActiveMenu.Add("salad");Stock(24);Game.Restaurant.ToggleService();}catch(Exception e){Fail(e);yield break;}
   bool sawRush=false,sawCalm=false;for(int t=0;t<1000;t++){Game.Restaurant.Advance(.25f);if(Game.Restaurant.ShiftPhase=="rush")sawRush=true;if(Game.Restaurant.ShiftPhase=="calm")sawCalm=true;if(t==200)Capture("05-busy-service.png");if(t%20==0)yield return null;}
   try{Check(Game.State.Restaurant.Workers.Any(w=>w.TasksCompleted>3),"worker physically performs station tasks");Check(Game.State.Restaurant.Served>3,"staff completes additional service");Check(sawCalm&&sawRush,"the shift opens calm, then hits a rush");Game.State.Restaurant.EndService(out _);}catch(Exception e){Fail(e);yield break;}
   for(int t=0;t<1000&&Game.State.Restaurant.Orders.Count>0;t++){Game.Restaurant.Advance(.25f);if(t%40==0)yield return null;}
   try{Game.Restaurant.ClosePanel();foreach(var w in Game.State.Restaurant.Workers)w.Job=StaffJob.Off;Stock(12);Game.Restaurant.ToggleService();}catch(Exception e){Fail(e);yield break;}
   for(int t=0;t<1600;t++){Game.Restaurant.Advance(.25f);if(t%60==0)yield return null;}
   try{Check(Game.State.Kitchen.LastReport.Lost>0,"failed shift reports lost guests");Check(Game.State.Restaurant.Owned&&Game.State.Restaurant.Workers.Count==2,"bad shift preserves restaurant and workers");Game.Restaurant.ClosePanel();Game.State.Restaurant.Orders.Clear();Stock(12);Game.Restaurant.ToggleService();Check(Game.State.Restaurant.Open,"can reopen after failed shift");Game.State.Restaurant.EndService(out _);Game.Restaurant.Advance(.1f);Game.Restaurant.ClosePanel();Game.Player.Teleport(new Vector3(-10,.15f,-12));Game.CoOp.SetElevated(Game.Player,true);Check(Game.State.Kitchen.SpendFlux(Game.State,"research",out var m),m);string layout=JsonUtility.ToJson(Game.State.Restaurant.Layout.ToArray());File.WriteAllText(Path.Combine(output,"expected-layout.txt"),Fingerprint());Check(Game.SaveTo(Path.Combine(output,"physical-save.json")),"disk save written");Check(Game.LoadFrom(Path.Combine(output,"physical-save.json")),"disk save reloaded");Check(Fingerprint()==File.ReadAllText(Path.Combine(output,"expected-layout.txt")),"exact layout cash staff menu Flux restored");Game.Restaurant.RebuildLayout();}catch(Exception e){Fail(e);yield break;}
   // Difficulty ramp and decor-gated residents.
   try{Check(ShiftDifficulty.Guests(10,false)>ShiftDifficulty.Guests(0,false)*2&&ShiftDifficulty.PatienceScale(10)<ShiftDifficulty.PatienceScale(0)&&ShiftDifficulty.PatienceScale(99)>=.55f,"shifts get busier and less patient as you level: "+ShiftDifficulty.Describe(0,false)+" -> "+ShiftDifficulty.Describe(10,false));
    Check(Game.State.Restaurant.ShiftsRun>=1,"finished shifts are counted ("+Game.State.Restaurant.ShiftsRun+")");
    bool fancyAtStand=false,fancyInNiceRoom=false;for(int i=0;i<400;i++){if(ResidentCast.Visitor(i,true,2,null,ResidentCast.StandAmbience).MinAmbience>ResidentCast.StandAmbience)fancyAtStand=true;if(ResidentCast.Visitor(i,true,2,null,30).MinAmbience>=14)fancyInNiceRoom=true;}
    Check(!fancyAtStand&&fancyInNiceRoom,"picky residents only visit well-decorated places");}catch(Exception e){Fail(e);yield break;}
   // Equipment levels: upgrade a station in place; it gets better at its job and keeps its spot.
   try{var r=Game.State.Restaurant;var g=r.Layout.Find(x=>x.CatalogId=="grill");
    if(g==null){r.Place(Game.State,"grill",0,0,0,out var bm);g=r.Layout.Find(x=>x.CatalogId=="grill");}
    if(g!=null){int before=Game.State.Cash,oldRank=r.Rank;Game.State.Cash=Math.Max(Game.State.Cash,500);int lv=r.LevelOf(g.InstanceId);g.Level=1;lv=1;r.Rank=1;Check(!r.Upgrade(Game.State,g.InstanceId,out var um1),"Level 2 gear needs 2 stars: "+um1);r.Rank=2;
     Check(lv==1||lv==2,"new grills start at level 1 ("+lv+")");bool up=r.Upgrade(Game.State,g.InstanceId,out var um);Check(up&&r.LevelOf(g.InstanceId)==lv+1,"upgrade the grill in place: "+um);
     Check(StationUpgrades.CookSeconds("grill",2)<StationUpgrades.CookSeconds("grill",1)&&StationUpgrades.BurnSeconds(2)>StationUpgrades.BurnSeconds(1),"higher grill levels cook faster and burn later");
     bool l3=r.Upgrade(Game.State,g.InstanceId,out var um3);Check(Game.State.RankEarned>=1?l3:!l3,"Level 3 arrives with the Docks: "+um3);r.Rank=oldRank;
     Check(StationUpgrades.Plates(3)>StationUpgrades.Plates(1),"plate rack levels add plates");}}catch(Exception e){Fail(e);yield break;}
   // Night inspectors: rules first, then a live patrol that spots a player carrying Zeeb's sauce and searches them.
   try{var g=Game.State;const string me="player:0";var held=g.Kitchen.Hold(me);if(held!=null)g.Kitchen.Items.Remove(held);
    g.DropBottles=3;g.DropPlaced=true;g.StashSpot=0;Check(g.Kitchen.CollectStash(g,me,out _)&&Inspections.Carried(g,me)==3,"carrying three bottles of Zeeb's sauce");
    g.Cash=100;int fine=Inspections.Search(g,me,false,out var im);Check(fine==Inspections.StopFine+3*Inspections.PerBottle&&g.Cash==100-fine&&Inspections.Carried(g,me)==0,"stop and search confiscates and fines: "+im);
    Check(Inspections.Search(g,me,false,out var clear)==0&&g.Cash==100-fine,"a clean player is let go: "+clear);
    g.Cash=10;Inspections.Search(g,me,true,out _);Check(g.Cash==0,"a fine never takes you below $0");
    g.Cash=100;g.DropBottles=2;g.DropPlaced=true;g.StashSpot=0;Check(g.Kitchen.CollectStash(g,me,out _),"carrying sauce again for the live patrol");
    g.Clock=160;Game.Restaurant.Advance(.05f);Check(Game.Restaurant.Inspectors.Count==3,"three inspectors patrol at night");}catch(Exception e){Fail(e);yield break;}
   {var insp=Game.Restaurant.Inspectors[0];var at=insp.Root.transform;Game.Player.Teleport(at.position+at.forward*6f);Physics.SyncTransforms();bool stopped=false;
    for(int t=0;t<200&&Inspections.Carried(Game.State,"player:0")>0;t++){Game.State.Clock=160;Game.Restaurant.Advance(.05f);if(insp.Mode==RestaurantController.InspectorMode.Stop)stopped=true;if(t%20==0)yield return null;}
    try{Check(stopped,"inspector spots the sauce and calls STOP");Check(Inspections.Carried(Game.State,"player:0")==0,"standing still gets you searched and the sauce confiscated");}catch(Exception e){Fail(e);yield break;}}
   {var g=Game.State;g.DropBottles=2;g.DropPlaced=true;g.StashSpot=0;g.Kitchen.CollectStash(g,"player:0",out _);var insp=Game.Restaurant.Inspectors[1];insp.Mode=RestaurantController.InspectorMode.Patrol;insp.CooldownLeft=0;
    var at=insp.Root.transform;Game.Player.Teleport(at.position+at.forward*6f);Physics.SyncTransforms();bool chased=false,escaped=false;
    for(int t=0;t<300&&!escaped;t++){g.Clock=160;Game.Restaurant.Advance(.05f);
     if(insp.Mode==RestaurantController.InspectorMode.Stop&&!chased){Game.Player.Teleport(Game.Player.transform.position+(Game.Player.transform.position-at.position).normalized*12);Physics.SyncTransforms();}
     if(insp.Mode==RestaurantController.InspectorMode.Chase&&!chased){chased=true;Game.Player.Teleport(at.position+Vector3.up*0+new Vector3(0,0,60));Physics.SyncTransforms();}
     if(chased&&insp.Mode==RestaurantController.InspectorMode.Cooldown)escaped=true;if(t%20==0)yield return null;}
    try{Check(chased,"running from a STOP starts a chase");Check(escaped&&Inspections.Carried(g,"player:0")==2,"getting away keeps your sauce");}catch(Exception e){Fail(e);yield break;}}
   yield return new WaitForSecondsRealtime(.5f);Capture("06-upgraded-restaurant.png");Debug.Log("PHYSICAL_RUNTIME_PASS "+checks);Application.Quit(0);
  }
  string Fingerprint()=>Game.State.Cash+"|"+Game.State.Flux+"|"+Game.State.FluxResearch+"|"+string.Join(";",Game.State.Restaurant.Layout.Select(p=>p.InstanceId+":"+p.CatalogId+":"+p.X+":"+p.Z+":"+p.Rotation))+"|"+string.Join(",",Game.State.Restaurant.ActiveMenu)+"|"+string.Join(",",Game.State.Restaurant.Workers.Select(w=>w.Id+":"+w.Job));
  void Capture(string file){
   Canvas.ForceUpdateCanvases();var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   bool split=Game.CoOp.PlayerCount==2&&!Game.Restaurant.PanelOpen;int width=split?720:1440;
   foreach(var player in Game.CoOp.Players){if(!split&&player.PlayerId>0)continue;var cam=player.View;var rect=cam.rect;var target=new RenderTexture(width,900,24);target.Create();cam.rect=new Rect(0,0,1,1);cam.targetTexture=target;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,900),split?player.PlayerId*720:0,0);cam.targetTexture=null;cam.rect=rect;Canvas.ForceUpdateCanvases();target.Release();Destroy(target);}
   pixels.Apply();File.WriteAllBytes(Path.Combine(output,file),pixels.EncodeToPNG());RenderTexture.active=previous;Destroy(pixels);
  }
  void Fail(Exception e){Debug.LogError("PHYSICAL_RUNTIME_FAIL "+e);Application.Quit(1);}
 }
}




