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
  // Restaurant stations first; the street stand's cart only stocks starter food.
  int Station(string id)=>(Game.State.Kitchen.Stations.FirstOrDefault(s=>s.CatalogId==id&&!KitchenState.IsStandStation(s.InstanceId))??Game.State.Kitchen.Stations.First(s=>s.CatalogId==id)).InstanceId;
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
    // Resident stats: every recruitable resident has four 1-5 stats and a perk, totals by rarity, and the stats change real work.
    {bool statsOk=true;foreach(var r in ResidentCast.OldMarket){var rs=StaffStats.For(r.Id);int want=r.Tier==0?10:r.Tier==1?12:14;
      if(!StaffStats.Has(r.Id)||rs.Perk!=Perk.None||rs.Total!=want||rs.Cooking<1||rs.Cooking>5||rs.Speed<1||rs.Speed>5||rs.Stamina<1||rs.Stamina>5||rs.Brawn<1||rs.Brawn>5){statsOk=false;Debug.LogWarning("STATS_BAD "+r.Id+" total "+rs.Total);}}
     Check(statsOk,"every Old Market resident has 1-5 stats, no perk, and the rarity's point total");
     {var old=new GameState();old.MetResidents.Add("043_Dracula");old.MetResidents.Add("205_TripoVampire");old.Restaurant.Workers.Add(new WorkerState{Id="035_Wolfman"});old.SanitizeAfterLoad();
      Check(old.MetResidents.Count(m=>m=="205_TripoVampire")==1&&!old.MetResidents.Contains("043_Dracula")&&old.Restaurant.Workers.Exists(w=>w.Id=="204_TripoReaper")&&ResidentCast.Get("046_Mafiossini")?.Name=="Zilo","a save that knew a placeholder resident gets the custom character in its place");}
     var lydia=new WorkerState{Id="054_Lydia",Energy=100};var jimmy=new WorkerState{Id="003_Jimmy",Energy=100};
     Check(RestaurantController.WorkerSpeed(lydia,"grill")>RestaurantController.WorkerSpeed(jimmy,"grill")*1.5f,"a Cooking-5 chef works the grill much faster than a Cooking-1 server");
     Check(StaffStats.WalkMultiplier(StaffStats.For("003_Jimmy"),false)>StaffStats.WalkMultiplier(StaffStats.For("211_TripoPumpkin"),false),"Speed 4 Jimmy walks faster than Speed 2 Jack");
     Check(StaffStats.DrainMultiplier(StaffStats.For("211_TripoPumpkin"))<.8f*StaffStats.DrainMultiplier(StaffStats.For("208_TripoConstruction")),"Stamina 5 Jack drains energy slower than Stamina 2 Buck");
     var tired=new WorkerState{Id="070_Robert",Energy=10};var rested=new WorkerState{Id="070_Robert",Energy=100};
     Check(RestaurantController.WorkerSpeed(rested,"grill")>.9f&&RestaurantController.WorkerSpeed(tired,"grill")<.5f,"an exhausted worker slows right down until they rest");}
    // Raids: Greasy Gus (1 star) after dark. He brings one imp per crew member; pairs fight one-on-one; the player beats Gus.
    {var g=st;var rc=Game.Restaurant;var gus=Rivals.GreasyGus;float clock0=g.Clock;int cash0=g.Cash,flux0=g.Flux,rank0=rc.Data.Rank,xp0=g.Xp;bool knew=g.Knows("cyclops");
     rc.Data.Rank=0;g.Clock=60;Check(!RaidRules.CanRaid(g,gus,false,out _),"Gus's truck is only there after dark");
     g.Clock=180;Check(!RaidRules.CanRaid(g,gus,true,out _),"no raids while your restaurant is mid-service");
     Check(RaidRules.CanRaid(g,gus,false,out var why0),"a zero-star restaurant may raid one-star Gus: "+why0);
     Check(GameObject.Find("Greasy Gus's truck")?.GetComponent<Interactable>()?.Kind==InteractionKind.Raid,"Gus's food truck in the vacant lot opens the raid planner");
     string[] crewIds={"091_BigBro_a","204_TripoReaper"};
     foreach(var id in crewIds)if(!rc.Data.Workers.Exists(w=>w.Id==id))rc.Data.Workers.Add(new WorkerState{Id=id,Job=StaffJob.Cook,Energy=100});
     rc.RaidCrew.Clear();rc.OpenRaid("gus");rc.UI.Refresh();Check(GameObject.Find("Raid prize")!=null,"the raid planner shows the prize");
     foreach(var id in crewIds)rc.ToggleRaidCrew(id);Check(rc.RaidCrew.Count==2,"pick a crew of two");
     Check(rc.StartRaid(out var rm)&&rc.ActiveRaid!=null,"the raid starts: "+rm);
     var b=rc.ActiveRaid;b.BossPassive=true;b.enabled=false;
     Check(b.Fighters.Count(f=>f.Ours)==2&&b.Fighters.Count(f=>!f.Ours&&!f.Boss)==2&&b.BossFighter!=null,"two crew bring out exactly two imps, plus Gus");
     Check(b.Fighters.Where(f=>!f.Ours&&!f.Boss).All(f=>f.GetComponentInChildren<Animator>()!=null&&f.Name.Contains("Imp")),"both rival workers use the imp model");
     Check(b.Fighters.Where(f=>f.Ours).All(f=>f.Opponent!=null&&f.Opponent.Opponent==f),"each crew member is paired with one imp");
     for(int i=0;i<1200&&b.Fighters.Any(f=>!f.Ours&&!f.Boss&&!f.Down);i++)b.Step(.05f);
     Check(b.Fighters.Where(f=>!f.Ours&&!f.Boss).All(f=>f.Down),"a strong crew wins their one-on-ones");
     int blows=0;while(!b.BossFighter.Down&&blows<40){b.PlayerHit(b.BossFighter,Game.Player,Weapons.Fists.Damage*2,Vector3.zero,true);blows++;}b.Step(.05f);
     Check(blows>=10&&blows<=13,"Gus soaks about a dozen heavy punches ("+blows+")");
     Check(RaidRules.Duel(StaffStats.For("204_TripoReaper"),gus.Roster[0].Stats)>=1.15f&&RaidRules.Duel(StaffStats.For("003_Jimmy"),gus.Roster[0].Stats)<1,"Grim (Brawn 5) is favoured against Gus's first imp; Jimmy is not");
     Check(b.Over&&b.Won&&g.Cash>=cash0+gus.CashMin&&g.Flux==flux0+gus.Flux,"beating Gus pays cash and Flux");
     Check(!RaidRules.CanRaid(g,gus,false,out _),"one raid per rival per day");
     Check(rc.Data.Workers.Where(w=>crewIds.Contains(w.Id)).All(w=>w.Energy<100),"the crew comes back tired");
     for(int i=0;i<3&&!g.Knows("cyclops");i++)RaidRules.Win(g,gus,i);Check(g.Knows("cyclops"),"the Cyclops Stack drops by the third win at the latest");
     b.Cleanup();Check(rc.ActiveRaid==null,"the raid is over and its fighters are cleared away");
     // Going down in a raid: lose 20% of your cash (capped) and your crew's energy, and wake up away from the lot.
     g.Raids.Clear();foreach(var w in rc.Data.Workers.Where(w=>crewIds.Contains(w.Id)))w.Energy=100;g.Cash=300;
     rc.RaidCrew.Clear();rc.ToggleRaidCrew(crewIds[0]);Check(rc.StartRaid(out _),"a second raid starts");var b2=rc.ActiveRaid;b2.enabled=false;
     Check(b2.Fighters.Count(f=>!f.Ours&&!f.Boss)==1,"one crew member brings out one imp");
     Game.Player.Teleport(b2.Center);Game.HurtPlayer(Game.Player,500,null);
     Check(b2.Over&&!b2.Won&&b2.CashLost==60&&RaidRules.CashLoss(301)==61&&g.Cash<=240&&rc.Data.Workers.Find(w=>w.Id==crewIds[0]).Energy==0,"knocked out: lose $60 of $300 and the crew's energy (over "+b2.Over+" won "+b2.Won+" cash "+g.Cash+" energy "+rc.Data.Workers.Find(w=>w.Id==crewIds[0]).Energy+" hp "+Game.Player.Health+")");
     Check(Vector3.Distance(Game.Player.transform.position,b2.Center)>10&&Game.Player.Health>0,"you wake up away from the lot");
     Check(RaidRules.CashLoss(5000)==100,"the raid cash loss is capped at $100");
     b2.Cleanup();PlayerCombat.Of(Game.Player).Health=100;
     rc.Data.Workers.RemoveAll(w=>crewIds.Contains(w.Id));g.Clock=clock0;g.Cash=cash0;g.Flux=flux0;rc.Data.Rank=rank0;g.Raids.Clear();if(!knew)g.KnownRecipes.Remove("cyclops");g.Xp=xp0;g.RepSources.RemoveAll(r=>r.Source=="Beating rivals");}
    // Hotbar: weapons and carried city items live in slots; kitchen food in your hands blocks switching.
    {var g=st;var inv=Hotbar.For(g,0);var saved=inv.Slots.Select(x=>new InvSlot{Item=x.Item,KitchenItemId=x.KitchenItemId}).ToList();int sel0=inv.Selected;
     foreach(var sl in inv.Slots){sl.Item="";sl.KitchenItemId=-1;}inv.Selected=0;
     Check(inv.Weapon==Weapons.Fists,"an empty hotbar means bare fists");
     Check(Hotbar.Give(g,0,"bat",out _)&&inv.Weapon.Id=="bat"&&inv.Has("bat"),"a bought bat goes into the hotbar and into your hands");
     Check(Hotbar.Select(g,0,3,out _)&&inv.Weapon==Weapons.Fists,"pressing 4 on an empty slot switches to fists");
     var bag=new KitchenItem{Id=g.Kitchen.NextItemId++,Kind=KitchenItemKind.GroceryBag,Holder="player:0"};bag.Components.Add("patty");g.Kitchen.Items.Add(bag);
     Hotbar.Sync(g,0);Check(inv.Current.KitchenItemId==bag.Id&&g.Kitchen.Hold("player:0")==bag,"a grocery bag takes the selected hotbar slot");
     int batSlot=inv.Slots.FindIndex(x=>x.Item=="bat");Check(Hotbar.Select(g,0,batSlot,out _)&&bag.Holder==Hotbar.Stowed("player:0")&&g.Kitchen.Hold("player:0")==null,"switching to the bat stows the bag");
     Check(Hotbar.CarriedBag(g,"player:0")==bag,"a stowed bag still counts as carried (inspectors can find it)");
     var patty=new KitchenItem{Id=g.Kitchen.NextItemId++,Kind=KitchenItemKind.RawProtein,Holder="player:0"};g.Kitchen.Items.Add(patty);
     Check(!Hotbar.Select(g,0,(batSlot+1)%PlayerInventory.Size,out var busy)&&busy.Contains("Hands full"),"holding food blocks switching slots: "+busy);
     g.Kitchen.Items.Remove(patty);g.Kitchen.Items.Remove(bag);Hotbar.Sync(g,0);
     // Hock-9's pawn counter sells weapons into the hotbar.
     Check(GameObject.Find("Pawn counter")?.GetComponent<Interactable>()?.Kind==InteractionKind.Pawn,"the pawn shop counter is on East Street");
     int cashP=g.Cash;g.Cash=500;Game.Restaurant.PawnBuyer=0;Check(Game.Restaurant.BuyWeapon("pan",out var pm)&&inv.Has("pan")&&g.Cash==500-35,"buy a frying pan at the pawn shop: "+pm);
     Check(!Game.Restaurant.BuyWeapon("pan",out _),"you only need one frying pan");g.Cash=cashP;
     for(int i=0;i<inv.Slots.Count;i++){inv.Slots[i].Item=saved[i].Item;inv.Slots[i].KitchenItemId=saved[i].KitchenItemId;}inv.Selected=sel0;}
    {var standShelves=KitchenState.ShelvesOf(st.Restaurant,KitchenState.StandBase+1,"pantry");
     Check(standShelves.Contains("protein")&&standShelves.Contains("bun")&&standShelves.Contains("greens")&&!standShelves.Contains("egg")&&!standShelves.Contains("soup")&&!standShelves.Contains("sausage"),"the stand only stocks starter food (patties, buns, greens, sauce)");}
    // One restaurant per district: Old Market is the dressed 160 m block only (no greybox, no second site).
    Check(RestaurantSites.All.Length==1&&GameObject.Find("Saffron Bay (greybox districts)")==null&&GameObject.Find("Property for lease / The Bayside")==null,"Old Market has one restaurant and no greybox around it");
    Check(CityDistricts.At(0,0)?.Id=="market"&&CityDistricts.At(-190,-15)==null&&System.Array.TrueForAll(NightStashes.Spots,sp=>System.Math.Abs(sp.X)<78&&sp.Z<78&&sp.Z>-119),"every stash spot and place is inside the block");
    // Pacing: the stand pays cash, not reputation. Earning a lease-and-renovation budget there must stay far from Line Cook (400).
    Check(st.Xp<=60,"stand alone earns only token reputation ("+st.Xp+")");Check(st.RepSources.Exists(r=>r.Source=="Stand sales"),"reputation sources are tracked");Check(Game.Restaurant.BuyRestaurant(),"purchase integration");Game.Restaurant.ClosePanel();
    Check(Game.Restaurant.Data.SiteId=="oddtable","The Odd Table is the starter restaurant");
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
   try{Check(ShiftDifficulty.Guests(10,false)>ShiftDifficulty.Guests(0,false)*2&&ShiftDifficulty.PatienceScale(10)<ShiftDifficulty.PatienceScale(0)&&ShiftDifficulty.PatienceScale(99)>=.7f,"shifts get busier and less patient as you level: "+ShiftDifficulty.Describe(0,false)+" -> "+ShiftDifficulty.Describe(10,false));
    Check(Game.State.Restaurant.ShiftsRun>=1,"finished shifts are counted ("+Game.State.Restaurant.ShiftsRun+")");
    {var hr=new RestaurantState{Heat=3,Rank=1};ShiftDifficulty.AfterShift(hr,3,7);Check(hr.Heat==2,"a shift where most guests walked out makes the next one calmer");
     ShiftDifficulty.AfterShift(hr,10,0);Check(hr.Heat==3,"a clean shift makes the next one busier");hr.Heat=ShiftDifficulty.HeatCap(hr);ShiftDifficulty.AfterShift(hr,10,0);Check(hr.Heat==ShiftDifficulty.HeatCap(hr),"stars cap how busy it gets");
     var rs=Game.State.Restaurant;var tables=rs.Layout.Where(p=>(RestaurantCatalog.Find(p.CatalogId)?.Seats??0)>0).OrderBy(p=>p.InstanceId).ToList();
     Check(tables.Count==0||rs.TableNumber(tables[0].InstanceId)==1&&Game.Restaurant.Furnishings[tables[0].InstanceId].transform.Find("Table number 1")!=null,"tables are numbered from 1 with a number card");}
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



