using System;
using System.Collections.Generic;
using System.Linq;
namespace RestaurantCity {
 public enum KitchenItemKind { RawProtein,PreparedPatty,CookedPatty,BurntPatty,RawGreens,ChoppedGreens,RawSauce,MidnightSauce,Bun,Plate,DirtyPlate,GroceryBag,SoupVeg,SoupPot,Soup,ScorchedSoup,RawSausage,CookedSausage,BurntSausage,FloatCup,RawEgg,FriedEgg,BurntEgg }
 public enum KitchenActionKind { None,Tap,Hold }
 // Preview describes exactly what a single interact press will do; Act performs it by calling Preview and
 // invoking the very same Apply callback it returned, so the on-screen prompt and the executed action can
 // never disagree (Stage A / A1).
 public struct KitchenAction { public bool Allowed; public string Label; public KitchenActionKind Kind; public string FailReason; public Func<string> Apply; }
 [Serializable] public class KitchenItem { public int Id,Parts,TableInstanceId,SeatNumber; public KitchenItemKind Kind; public float Quality=1,Age; public string Holder; public List<string> Components=new List<string>(); public bool Disposable,StandPlate; public float Stir; }   // SeatNumber: one-based, zero for legacy plates. Stir: seconds since a simmering pot was last stirred
 [Serializable] public class KitchenStation { public int InstanceId; public string CatalogId,WorkOwner,InputAction; public float Progress; public int WasteCount; }
 [Serializable] public class ShiftReport { public int GrossSales,Wages,Net,Served,Lost,StarsBefore,StarsAfter; public float IngredientCosts,Satisfaction; public List<string> Comments=new List<string>(); public string StaffSummary; }
 [Serializable] public class KitchenState {
  public List<KitchenItem> Items=new List<KitchenItem>(); public List<KitchenStation> Stations=new List<KitchenStation>();
  public int CleanPlates=4,NextItemId=1,PoorShifts,SinkPile; public ShiftReport LastReport; public bool ShiftActive,ShiftNight;
  public int ShiftServed,ShiftLost,ShiftStars,ShiftGross,ShiftWages; public float ShiftCosts;
  static bool Fail(string text,out string message){message=text;return false;}
  static KitchenAction Blocked(string reason)=>new KitchenAction{Allowed=false,Kind=KitchenActionKind.None,FailReason=reason,Label=reason};
  static KitchenAction NeedsHold(string label)=>new KitchenAction{Allowed=false,Kind=KitchenActionKind.Hold,FailReason=label,Label=label};
  static KitchenAction Tap(string label,Func<string> apply)=>new KitchenAction{Allowed=true,Kind=KitchenActionKind.Tap,Label=label,FailReason="",Apply=apply};
  public const float WashSeconds=3.5f;
  // Restaurant plates: 4 per plate rack you own. Dirty plates stack beside the sink and are washed one at a time.
  public static int PlateCapacity(RestaurantState r){int n=r.Layout.Where(p=>p.CatalogId=="plate_rack").Sum(p=>StationUpgrades.Plates(Math.Max(1,p.Level)));return Math.Max(4,n);}
  int PlatesInPlay=>Items.Count(i=>!i.Disposable&&!i.StandPlate&&(i.Kind==KitchenItemKind.Plate||i.Kind==KitchenItemKind.DirtyPlate));
  void BalancePlates(RestaurantState r){if(!r.Owned)return;int missing=PlateCapacity(r)-(CleanPlates+SinkPile+PlatesInPlay);if(missing>0)CleanPlates+=missing;else if(missing<0)CleanPlates=Math.Max(0,CleanPlates+missing);}
  // The street food stand is a small fixed kitchen that uses the same stations and rules as the restaurant.
  public const int StandBase=90000;
  public static readonly string[] StandKit={"pantry","grill","counter","stand_plates","sink","trash","prep_bench"};   // prep_bench = the stand's cutting board (StandBase+7)
  public static bool IsStandStation(int id)=>id>StandBase&&id<=StandBase+StandKit.Length;
  public void EnsureStandStations(){
   for(int i=0;i<StandKit.Length;i++){int id=StandBase+1+i;var st=Stations.Find(s=>s.InstanceId==id);if(st==null)Stations.Add(new KitchenStation{InstanceId=id,CatalogId=StandKit[i]});else if(st.CatalogId!=StandKit[i]){var stray=At(id);if(stray!=null)Items.Remove(stray);st.CatalogId=StandKit[i];st.Progress=0;}}
  }
  public KitchenItem Hold(string actor)=>Items.Find(i=>i.Holder==actor);
  public KitchenItem At(int station)=>Hold("station:"+station);
  KitchenItem Create(KitchenItemKind kind,string holder){var item=new KitchenItem{Id=NextItemId++,Kind=kind,Holder=holder};Items.Add(item);return item;}
  public void EnsureStations(RestaurantState restaurant){
   restaurant.EnsurePhysicalKit();
   var layout=restaurant.Layout.Where(p=>RestaurantCatalog.Find(p.CatalogId)?.Category==CatalogCategory.Kitchen).ToList();
   foreach(var p in layout)if(!Stations.Exists(s=>s.InstanceId==p.InstanceId))Stations.Add(new KitchenStation{InstanceId=p.InstanceId,CatalogId=p.CatalogId});
   EnsureStandStations();
   foreach(var stale in Stations.Where(s=>!IsStandStation(s.InstanceId)&&!layout.Exists(p=>p.InstanceId==s.InstanceId)).ToList()){
    var item=At(stale.InstanceId);if(item!=null){if(item.Kind==KitchenItemKind.Plate||item.Kind==KitchenItemKind.DirtyPlate){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();item.Holder="table:returned"+item.Id;item.TableInstanceId=restaurant.Layout.Find(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0)?.InstanceId??0;}else Items.Remove(item);}Stations.Remove(stale);
   }
  }
  // Raw protein now goes straight from the pantry to the grill (no chopping step); the prep bench only
  // handles ingredients that genuinely need hand-prep: greens and midnight sauce.
  static bool Raw(KitchenItemKind k)=>k==KitchenItemKind.RawGreens||k==KitchenItemKind.RawSauce;
  // A finished ingredient sitting on any station (grill, prep bench, counter) slides onto a held plate.
  static string DirectComponent(string stationId,KitchenItemKind kind){
   switch(kind){
    case KitchenItemKind.CookedPatty:return "cooked_patty";
    case KitchenItemKind.CookedSausage:return "cooked_sausage";
    case KitchenItemKind.FriedEgg:return "fried_egg";
    case KitchenItemKind.ChoppedGreens:return "chopped_greens";
    case KitchenItemKind.MidnightSauce:return "midnight_sauce";
    case KitchenItemKind.Bun:return "bun";
    case KitchenItemKind.Soup:return "soup";
    default:return null;
   }
  }
  static bool IsCounter(string id)=>id=="assembly"||id=="counter";
  static string ComponentLabel(string id)=>id=="fried_egg"?"the fried egg":id=="cooked_sausage"?"the cooked sausage":id=="float"?"the float":id=="bun"?"a bun":id=="cooked_patty"?"the cooked patty":id=="chopped_greens"?"chopped greens":id=="midnight_sauce"?"midnight sauce":id;
  static string PantryLabel(string choice)=>choice=="egg"?"an egg":choice=="sausage"?"a raw sausage":choice=="soup"?"soup vegetables":choice=="protein"?"a raw patty":choice=="greens"?"greens":choice=="bun"?"a bun":"sauce ingredients";
  bool CanAddComponent(KitchenItem plate,string component,out string message){
   if(plate.Components.Contains(component))return Fail("This plate already has "+ComponentLabel(component)+".",out message);
   if(RecipeBook.Recipes.Any(r=>r.Components.Contains(component)&&plate.Components.All(c=>r.Components.Contains(c)))){message="";return true;}
   var building=RecipeBook.Recipes.FirstOrDefault(r=>plate.Components.Count>0&&plate.Components.All(c=>r.Components.Contains(c)));
   string name=building!=null?RestaurantCatalog.Dish(building.DishId).Name:"This plate";
   return Fail(name+" doesn't use "+ComponentLabel(component)+".",out message);
  }
  // Each pantry shelf holds one real ingredient; stock is counted and runs out per ingredient.
  static KitchenItemKind ShelfKind(string shelf)=>shelf=="egg"?KitchenItemKind.RawEgg:shelf=="sausage"?KitchenItemKind.RawSausage:shelf=="soup"?KitchenItemKind.SoupVeg:shelf=="protein"?KitchenItemKind.RawProtein:shelf=="greens"?KitchenItemKind.RawGreens:shelf=="bun"?KitchenItemKind.Bun:KitchenItemKind.RawSauce;
  static string OutOf(IngredientDef ing)=>ing.Source==Ingredients.Stash?"Out of "+ing.Name.ToLower()+". Call Zeeb on your phone (P).":"Out of "+ing.Name.ToLower()+". Buy more at Milo's.";
  // Which shelves a storage station holds: the fridge keeps cold food; a restaurant pantry keeps dry goods once there is
  // a fridge (until then it holds everything); the street stand's cart pantry always holds everything.
  public static string[] ShelvesOf(RestaurantState r,int instanceId,string catalogId){
   if(catalogId=="fridge")return Ingredients.ColdShelves;
   if(IsStandStation(instanceId)||r==null||!r.HasEquipment("fridge"))return Ingredients.ColdShelves.Concat(Ingredients.DryShelves).ToArray();
   return Ingredients.DryShelves;
  }
  KitchenAction PreviewPantry(GameState game,string actor,KitchenItem hand,string subId,KitchenStation station){
   var r=game.Restaurant;var shelves=ShelvesOf(r,station.InstanceId,station.CatalogId);
   // Groceries from Milo's are unpacked into the pantry with one press, whichever shelf you look at.
   if(hand!=null&&hand.Kind==KitchenItemKind.GroceryBag){
    var bag=hand;int fits=bag.Components.Count(id=>r.Room(id)>0);
    if(fits==0)return Blocked("The pantry is full for what's in this bag. Cook some of it first.");
    return Tap("Unpack groceries ("+bag.Components.Count+" items)",()=>{var left=new List<string>();foreach(var id in bag.Components)if(r.AddStock(id,1)==0)left.Add(id);bag.Components=left;if(left.Count==0)Items.Remove(bag);return left.Count==0?"Groceries unpacked.":"Unpacked what fits; "+left.Count+" items stay in the bag.";});
   }
   string choice=string.IsNullOrEmpty(subId)?shelves[0]:subId;
   var ing=Ingredients.ForShelf(choice);
   if(ing!=null&&!shelves.Contains(choice))return Blocked(ing.Name+(ing.Cold?" are kept in the fridge.":" are kept in the pantry."));
   if(ing==null)return Blocked("Look at a shelf: patties, greens, buns, sauce or soup veg.");
   if(choice=="sauce"&&!game.Knows("midnight"))return Blocked("Needs the midnight recipe.");
   if(!string.IsNullOrEmpty(ing.Recipe)&&choice!="sauce"&&!game.Knows(ing.Recipe))return Blocked(DistrictCookbook.Find(ing.Recipe)?.Source==RecipeSource.Cookbook||DistrictCookbook.Find(ing.Recipe)==null?"Buy the "+RestaurantCatalog.Dish(ing.Recipe).Name+" recipe in the Cookbook first.":"You don't know the "+RestaurantCatalog.Dish(ing.Recipe).Name+" yet. "+DistrictCookbook.Find(ing.Recipe).Hint);
   if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&choice=="bun"){
    // Fewer-press shortcut: a bun needs no prep, so it can go straight onto a plate you are already carrying.
    if(!CanAddComponent(hand,"bun",out string reason))return Blocked(reason);
    if(r.Stock("bun")<1)return Blocked(OutOf(ing));
    var plate=hand;
    return Tap("Add bun to plate",()=>{r.UseStock("bun");ShiftCosts+=ing.UnitCost;plate.Components.Add("bun");return "Added a bun to the plate.";});
   }
   if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&choice=="sauce"){
    // Midnight sauce is squeezed straight onto a held burger plate (works at the stand, which has no prep bench).
    if(!CanAddComponent(hand,"midnight_sauce",out string reason))return Blocked(reason);
    if(r.Stock("midnight_sauce")<1)return Blocked(OutOf(ing));
    var plate=hand;
    return Tap("Add midnight sauce to plate",()=>{r.UseStock("midnight_sauce");plate.Components.Add("midnight_sauce");return "Midnight sauce added. That's a midnight burger if it has a bun and patty!";});
   }
   // Changed your mind? Put an untouched ingredient back on its own shelf.
   if(hand!=null){
    if(hand.Kind!=ShelfKind(choice))return Blocked("Your hands are full. Put it on a counter or its own shelf.");
    var back=hand;
    return Tap("Put "+Label(hand).ToLower()+" back",()=>{r.AddStock(ing.Id,1);ShiftCosts=Math.Max(0,ShiftCosts-ing.UnitCost);Items.Remove(back);return "Put it back.";});
   }
   if(r.Stock(ing.Id)<1)return Blocked(OutOf(ing));
   return Tap("Take "+PantryLabel(choice)+"  ("+r.Stock(ing.Id)+" left)",()=>{r.UseStock(ing.Id);ShiftCosts+=ing.UnitCost;var created=Create(ShelfKind(choice),actor);return "Picked up "+Label(created);});
  }
  // Tonight's secret stash: the sauce goes in a bag you carry home (and can lose on the way), not straight into the pantry.
  public bool CollectStash(GameState game,string actor,out string message){
   if(!game.StashActive)return Fail("Nothing here. The stash is gone.",out message);
   var hand=Hold(actor);if(hand!=null&&hand.Kind!=KitchenItemKind.GroceryBag)return Fail("Free your hands to grab the stash.",out message);
   int bottles=game.DropBottles;var bag=hand??Hotbar.CarriedBag(game,actor)??Create(KitchenItemKind.GroceryBag,actor);bag.Holder=actor;for(int i=0;i<bottles;i++)bag.Components.Add("midnight_sauce");
   game.DropBottles=0;game.DropPlaced=false;game.StashSpot=-1;
   message="Found Zeeb's drop: "+bottles+" bottles of Midnight sauce. Get them home to your pantry"+(game.ZeebDebt>0?", and don't forget you owe him $"+game.ZeebDebt+".":".");return true;
  }
  // Milo's shop: pay for the cart and carry it home in a grocery bag (merges with a bag you're already holding).
  public bool BuyGroceries(GameState game,string actor,List<StockLine> cart,out string message){
   var r=game.Restaurant;var hand=Hold(actor);
   if(!r.Owned&&!game.StandBuilt)return Fail("Set up your food stand first ($10).",out message);
   if(cart==null||cart.All(c=>c.Count<=0))return Fail("Your cart is empty.",out message);
   if(hand!=null&&hand.Kind!=KitchenItemKind.GroceryBag)return Fail("Your hands are full. Put down what you're carrying first.",out message);
   int total=0;
   foreach(var line in cart){if(line.Count<=0)continue;var d=Ingredients.Get(line.Id);if(d==null)return Fail("Unknown item.",out message);string why=r.IngredientLock(game,d);if(why!=null)return Fail(d.Name+": "+why+".",out message);total+=d.PackPrice*line.Count;}
   if(game.Cash<total)return Fail("That's $"+total+". You have $"+game.Cash+".",out message);
   game.Cash-=total;var bag=hand??Hotbar.CarriedBag(game,actor)??Create(KitchenItemKind.GroceryBag,actor);bag.Holder=actor;int units=0;
   foreach(var line in cart){var d=Ingredients.Get(line.Id);for(int i=0;i<line.Count*d.PackSize;i++){bag.Components.Add(d.Id);units++;}}
   message="Paid $"+total+". "+units+" items in your grocery bag: unpack them at your pantry.";return true;
  }
  public KitchenAction Preview(GameState game,string actor,int stationId,string subId){
   bool stand=IsStandStation(stationId);
   if(stand?!game.StandBuilt:!game.Restaurant.Owned)return Blocked(stand?"Set up your stand first.":"Buy the restaurant first.");
   EnsureStations(game.Restaurant);
   var s=Stations.Find(x=>x.InstanceId==stationId);
   if(s==null)return Blocked("Station unavailable.");
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Blocked("Someone is working here.");
   var hand=Hold(actor);var item=At(stationId);
   if(s.CatalogId=="pantry"||s.CatalogId=="fridge")return PreviewPantry(game,actor,hand,subId,s);
   if(s.CatalogId=="trash"){
    if(hand==null){
     if(s.WasteCount>0)return Tap("Empty trash",()=>{s.WasteCount=0;game.Emit("emptybin:"+s.InstanceId);return "Trash emptied.";});
     return Blocked("Trash can is empty. Bring food you want to throw away.");
    }
    if(hand.Kind==KitchenItemKind.DirtyPlate)return Blocked("Wash dirty plates at the sink.");
    if(hand.Kind==KitchenItemKind.Plate&&hand.Components.Count==0&&!hand.Disposable)return Blocked("That plate is clean. Keep it or set it down.");
    string what=hand.Kind==KitchenItemKind.Plate?"the food (keep the plate)":Label(hand).ToLower();
    return Tap("Throw away "+what,()=>{if(Discard(game,actor,out string msg)){s.WasteCount=Math.Min(6,s.WasteCount+1);game.Emit("discard:"+s.InstanceId);}return msg;});
   }
   if(s.CatalogId=="plate_rack"){
    if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&hand.Components.Count==0&&!hand.StandPlate&&!hand.Disposable){var back=hand;return Tap("Put the clean plate back",()=>{Items.Remove(back);CleanPlates++;return "Plate back on the rack.";});}
    if(hand!=null)return Blocked("Your hands are full.");
    if(CleanPlates<=0)return Blocked("No clean plates. Clear a table and wash a dirty plate.");
    return Tap("Take a clean plate",()=>{CleanPlates--;Create(KitchenItemKind.Plate,actor);return "Clean plate collected.";});
   }
   if(s.CatalogId=="stand_plates"){
    if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&hand.Components.Count==0&&hand.StandPlate){var back=hand;return Tap("Put the clean plate back",()=>{Items.Remove(back);game.StandClean++;return "Plate back on the stack.";});}
    if(hand!=null)return Blocked("Your hands are full.");
    if(game.StandClean<=0)return Blocked("No clean plates! Wash dirty ones at the stand sink.");
    return Tap("Take a plate ("+game.StandClean+" clean)",()=>{game.StandClean--;var p=Create(KitchenItemKind.Plate,actor);p.StandPlate=true;return "Plate ready. Build it: bun + cooked patty.";});
   }
   if(!stand&&s.CatalogId=="sink"){
    if(hand!=null&&hand.Kind==KitchenItemKind.DirtyPlate&&!hand.StandPlate&&item!=null){var d=hand;return Tap("Stack the dirty plate by the sink ("+(SinkPile+1)+" waiting)",()=>{Items.Remove(d);SinkPile++;return "Stacked. Wash them one at a time.";});}
    if(hand==null&&item==null&&SinkPile>0)return Tap("Put a dirty plate in the sink ("+SinkPile+" waiting)",()=>{SinkPile--;Create(KitchenItemKind.DirtyPlate,"station:"+stationId);s.Progress=0;return "Hold E to wash it.";});
   }
   if(stand&&s.CatalogId=="sink"&&hand!=null&&hand.Kind==KitchenItemKind.DirtyPlate&&hand.StandPlate&&item!=null){
    // Sink busy: stack the plate on the dirty pile beside it.
    var d=hand;return Tap("Stack the dirty plate by the sink ("+(game.StandDirty+1)+" waiting)",()=>{Items.Remove(d);game.StandDirty++;return "Stacked. Wash them one at a time.";});
   }
   if(stand&&s.CatalogId=="sink"&&item==null&&hand==null){
    // Dirty plates pile up beside the stand sink; move them in one at a time, then hold to wash.
    if(game.StandDirty<=0)return Blocked("No dirty plates right now.");
    return Tap("Put a dirty plate in the sink ("+game.StandDirty+" waiting)",()=>{game.StandDirty--;var d=Create(KitchenItemKind.DirtyPlate,"station:"+stationId);d.StandPlate=true;s.Progress=0;return "Hold E to wash it.";});
   }
   // Swirl & Fizz machine: set a cup under the nozzle (uses one moonberry), hold to pour, then grab the float.
   if(s.CatalogId=="drink_machine"){
    if(item!=null&&item.Kind==KitchenItemKind.FloatCup&&hand==null)return NeedsHold("Pour the Moonberry Float");
    if(item==null&&hand==null){
     if(!game.Knows("float"))return Blocked("Buy the Moonberry Float recipe in the Cookbook first.");
     var berry=Ingredients.Get("moonberry");if(game.Restaurant.Stock("moonberry")<1)return Blocked(OutOf(berry));
     return Tap("Set a cup under the nozzle",()=>{game.Restaurant.UseStock("moonberry");ShiftCosts+=berry.UnitCost;Create(KitchenItemKind.FloatCup,"station:"+stationId);s.Progress=0;return "Hold E to pour the float.";});
    }
    if(item==null)return Blocked("Free your hands to pour a float.");
   }
   // Fewer-press shortcuts: while carrying a plate, a finished ingredient at its own station slides
   // straight onto it instead of needing a separate pickup-then-assembly trip.
   if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&item!=null){
    string direct=DirectComponent(s.CatalogId,item.Kind);
    if(direct!=null){
     if(!CanAddComponent(hand,direct,out string reason))return Blocked(reason);
     var plate=hand;var source=item;
     return Tap("Add "+ComponentLabel(direct)+" to plate",()=>{plate.Components.Add(direct);plate.Quality=Math.Min(plate.Quality,source.Quality);Items.Remove(source);return "Added "+ComponentLabel(direct)+" to the plate.";});
    }
   }
   if(item!=null&&hand==null&&s.CatalogId=="stove"&&item.Kind==KitchenItemKind.SoupPot){
    var pot=item;return Tap(pot.Stir>=StirWarning?"Stir the soup NOW (it's catching!)":"Stir the soup",()=>{pot.Stir=0;game.Emit("stir:"+stationId);return "Stirred. Keep an eye on it until it's ready.";});
   }
   if(item!=null&&hand==null){
    if(s.CatalogId=="prep_bench"&&Raw(item.Kind))return NeedsHold("Chop "+Label(item).ToLower());
    if(s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate)return NeedsHold("Wash the plate");
    var pick=item;
    return Tap("Take "+Label(item).ToLower(),()=>{pick.Holder=actor;s.Progress=0;s.WorkOwner=null;return "Picked up "+Label(pick);});
   }
   if(hand==null)return Blocked("Bring an ingredient or plate here.");
   if(item!=null){
    if(!IsCounter(s.CatalogId)||item.Kind!=KitchenItemKind.Plate)return Blocked(IsCounter(s.CatalogId)?"Counter is full. Put a plate here to build on it.":"Station occupied.");
    string part=hand.Kind==KitchenItemKind.Soup?"soup":hand.Kind==KitchenItemKind.FriedEgg?"fried_egg":hand.Kind==KitchenItemKind.CookedSausage?"cooked_sausage":hand.Kind==KitchenItemKind.CookedPatty?"cooked_patty":hand.Kind==KitchenItemKind.Bun?"bun":hand.Kind==KitchenItemKind.ChoppedGreens?"chopped_greens":hand.Kind==KitchenItemKind.MidnightSauce?"midnight_sauce":null;
    if(part==null)return Blocked(hand.Kind==KitchenItemKind.RawSauce?"Raw sauce ingredients: trash these, then hold the PLATE and press E on the sauce shelf.":"Prepare the ingredient first. Discard burnt food.");
    if(!CanAddComponent(item,part,out string why))return Blocked(why);
    var plate=item;var carried=hand;
    return Tap("Add "+ComponentLabel(part)+" to plate",()=>{plate.Components.Add(part);plate.Quality=Math.Min(plate.Quality,carried.Quality);Items.Remove(carried);return "Added "+ComponentLabel(part)+" to the plate.";});
   }
   bool allowed=s.CatalogId=="prep_bench"&&Raw(hand.Kind)||(s.CatalogId=="grill"||s.CatalogId=="oven")&&(hand.Kind==KitchenItemKind.RawProtein||s.CatalogId=="grill"&&(hand.Kind==KitchenItemKind.RawSausage||hand.Kind==KitchenItemKind.RawEgg))||s.CatalogId=="stove"&&hand.Kind==KitchenItemKind.SoupVeg||IsCounter(s.CatalogId)||s.CatalogId=="sink"&&hand.Kind==KitchenItemKind.DirtyPlate;
   if(!allowed)return Blocked("Use the correct station for this item.");
   var carriedItem=hand;
   return Tap("Place "+Label(hand).ToLower(),()=>{carriedItem.Holder="station:"+stationId;carriedItem.Age=0;s.Progress=0;s.WorkOwner=null;return "Placed "+Label(carriedItem);});
  }
  public bool Act(GameState game,string actor,int stationId,string subId,out string message){
   var preview=Preview(game,actor,stationId,subId);
   if(!preview.Allowed){message=preview.FailReason;return false;}
   message=preview.Apply();game.Emit("act:"+stationId);return true;
  }
  public bool Work(GameState game,string actor,int stationId,float seconds,out string message){
   var s=Stations.Find(x=>x.InstanceId==stationId);var item=At(stationId);
   if(s==null||item==null||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return Fail("Place an ingredient or dirty plate here first.",out message);
   if(Hold(actor)!=null)return Fail("Free your hands before working.",out message);
   bool prep=s.CatalogId=="prep_bench"&&Raw(item.Kind),wash=s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate,pour=s.CatalogId=="drink_machine"&&item.Kind==KitchenItemKind.FloatCup;
   if(pour){
    if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Fail("Someone else is pouring.",out message);
    s.WorkOwner=actor;s.Progress+=seconds;game.Emit("pour:"+stationId);
    if(s.Progress<PourSeconds){message="Pouring "+(int)(s.Progress/PourSeconds*100)+"%";return true;}
    item.Kind=KitchenItemKind.Plate;item.Disposable=true;item.Components=new List<string>{"float"};item.Age=0;item.Quality=1;
    s.Progress=0;s.WorkOwner=null;message="Moonberry Float ready. Serve it before it melts!";return true;
   }
   if(!prep&&!wash)return Fail("Nothing to prepare.",out message);
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Fail("Someone else is working here.",out message);
   s.WorkOwner=actor;s.Progress+=seconds;game.Emit(wash?"wash:"+stationId:"chop:"+stationId);float duration=wash?WashSeconds:item.Kind==KitchenItemKind.RawSauce?4:3;if(prep&&game.FluxResearch)duration*=.65f;duration*=StationUpgrades.WorkScale(game.Restaurant.LevelOf(stationId));
   if(s.Progress<duration){message=(wash?"Washing ":"Preparing ")+(int)(s.Progress/duration*100)+"%";return true;}
   if(wash){Items.Remove(item);if(item.StandPlate){game.StandClean++;message="Clean plate back on the stand stack.";}else{CleanPlates++;game.Restaurant.Cleanliness=Math.Min(100,game.Restaurant.Cleanliness+5);message="Clean plate returned to rack.";}}
   else{item.Kind=item.Kind==KitchenItemKind.RawGreens?KitchenItemKind.ChoppedGreens:KitchenItemKind.MidnightSauce;message=Label(item)+" ready.";}
   s.Progress=0;s.WorkOwner=null;return true;
  }
  public void ReleaseWork(string actor){foreach(var s in Stations)if(s.WorkOwner==actor)s.WorkOwner=null;}
  public bool Serve(GameState game,string actor,int orderId,out string message){
   var item=Hold(actor);var order=game.Restaurant.Orders.Find(o=>o.Id==orderId);
   if(item==null||item.Kind!=KitchenItemKind.Plate)return Fail("Carry a plated dish to the guest.",out message);
   if(order==null||order.Stage!=RestaurantOrderStage.Waiting)return Fail("This guest is not waiting for food.",out message);
   if(RecipeOf(item)!=order.DishId)return Fail("They ordered "+RestaurantCatalog.Dish(order.DishId).Name+". Bring the matching dish.",out message);
   order.Quality=item.Quality;order.Stage=RestaurantOrderStage.Ready;order.StageTime=0;int before=game.Cash,wages=game.Restaurant.WagesPerOrder;
   if(!game.Restaurant.CompleteServing(game,orderId,out message)){order.Stage=RestaurantOrderStage.Waiting;return false;}
   int gross=game.Cash-before+wages,bonus=ShiftNight?(int)Math.Round(gross*.3f):0;game.Cash+=bonus;game.Restaurant.Earnings+=bonus;ShiftGross+=gross+bonus;ShiftWages+=wages;
   if(bonus>0)message+=" Night premium +$"+bonus+".";
   if(item.Disposable||item.StandPlate){if(item.StandPlate)game.StandDirty++;Items.Remove(item);return true;}
   item.Holder="table:"+orderId;item.TableInstanceId=order.SeatInstanceId;item.SeatNumber=order.SeatNumber;return true;
  }
  // --- The street stand: guests sit at two sidewalk tables; you bring the food to them, then clear and wash. ---
  static string DishName(string dish)=>dish=="midnight"?"midnight burger":dish=="salad"?"salad":"burger";
  static string HowTo(string dish)=>dish=="salad"?"chop greens on the board, then put them on a plate":"plate + bun + cooked patty"+(dish=="midnight"?" + midnight sauce":"");
  public StandOrder SeatedAt(GameState game,int table)=>game.StandQueue.Find(o=>o.Stage>0&&o.Table==table);
  public string StandGuestPreview(GameState game,string actor,StandOrder o){
   if(o==null)return "They've left.";
   if(o.Stage==0)return "Waiting for a free table"+(game.StandTableDirty.Contains(true)?". Clear the dirty one!":".");
   if(o.Stage==2)return "Eating. They'll leave the plate on the table.";
   string dish=RecipeOf(Hold(actor));
   if(dish==o.Dish)return "E / A  Serve the "+DishName(o.Dish)+"  /  $"+StandPrice(game,o.Dish);
   if(dish!="")return "They ordered a "+DishName(o.Dish)+", not a "+DishName(dish)+".";
   return "Wants a "+DishName(o.Dish)+"\nBuild it: "+HowTo(o.Dish)+", then bring it here.";
  }
  // A table has two seats (seat = table*2 and table*2+1). Clearing comes first, then serving whoever is waiting.
  public int DirtySeatAt(GameState game,int table){for(int s=table*2;s<table*2+2&&s<game.StandTableDirty.Count;s++)if(game.StandTableDirty[s])return s;return -1;}
  public StandOrder WaitingAtTable(GameState game,string actor,int table){string dish=RecipeOf(Hold(actor));var waiting=game.StandQueue.FindAll(o=>o.Stage==1&&o.Table/2==table);return waiting.Find(o=>o.Dish==dish)??(waiting.Count>0?waiting[0]:null);}
  public string StandTablePreview(GameState game,string actor,int table){
   int dirty=DirtySeatAt(game,table);
   if(dirty>=0&&Hold(actor)==null)return "E / A  Clear the dirty plate";
   var o=WaitingAtTable(game,actor,table);
   if(o!=null)return StandGuestPreview(game,actor,o);
   if(dirty>=0)return "Dirty plate. Free your hands to clear it.";
   return game.StandQueue.Exists(x=>x.Stage==2&&x.Table/2==table)?"Guests eating":"Free table";
  }
  public bool ServeStandGuest(GameState game,string actor,int orderId,out string message){
   var item=Hold(actor);string dish=RecipeOf(item);var order=game.StandQueue.Find(o=>o.Id==orderId);
   if(order==null)return Fail("That customer has left.",out message);
   if(order.Stage==0)return Fail("They're still waiting for a free table. Clear a dirty one.",out message);
   if(order.Stage==2)return Fail("They're already eating.",out message);
   if(dish!=order.Dish)return Fail(dish==""?"Make a "+DishName(order.Dish)+" first: "+HowTo(order.Dish)+".":"They ordered a "+DishName(order.Dish)+".",out message);
   int price=StandPrice(game,dish);bool fast=order.Patience>order.MaxPatience*.6f;if(fast)price+=2;
   game.Cash+=price;if(fast)game.GainReputation(Reputation.StandFast,"Stand sales");game.Served++;game.Emit("stand_served:"+order.Id+":"+(fast?2:0));bool met=game.MeetResident(order.ResidentId);
   order.Stage=2;order.EatLeft=GameState.StandEatSeconds;Items.Remove(item);   // the plate stays on their table until they finish
   message="+$"+price+(fast?" (incl. $2 speed tip)  +"+Reputation.StandFast+" rep":"")+(met?"  NEW: "+ResidentCast.Get(order.ResidentId).Name+" joined your People book!":"  Enjoy! Clear the plate when they're done.");return true;
  }
  // Serve whichever seated guest ordered what you're carrying (the table/guest target picks exactly; this is the fallback).
  public bool ServeStand(GameState game,string actor,out string message){
   string dish=RecipeOf(Hold(actor));
   var o=game.StandQueue.Find(x=>x.Stage==1&&x.Dish==dish)??game.StandQueue.Find(x=>x.Stage==1);
   if(o==null)return Fail(game.StandQueue.Count==0?"No one is waiting yet. Open the stand with the sign.":"Nobody is seated yet. Clear a dirty table.",out message);
   return ServeStandGuest(game,actor,o.Id,out message);
  }
  public bool ClearStandTable(GameState game,string actor,int table,out string message){
   if(Hold(actor)!=null)return Fail("Your hands are full.",out message);
   if(table<0||table>=game.StandTableDirty.Count||!game.StandTableDirty[table])return Fail("Nothing to clear here.",out message);
   game.StandTableDirty[table]=false;var d=Create(KitchenItemKind.DirtyPlate,actor);d.StandPlate=true;
   message="Dirty plate. Wash it at the stand sink.";return true;
  }
  public static int StandPrice(GameState game,string dish)=>(dish=="midnight"?18:dish=="salad"?10:12)*(game.IsNight?3:2)/2;
  public int DirtyAtTable(int table)=>Items.Count(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.TableInstanceId==table&&i.Holder.StartsWith("table:"));
  public bool ClearTable(GameState game,string actor,int table,out string message,int seatNumber=0){
   if(Hold(actor)!=null)return Fail("Your hands are full.",out message);var item=Items.Find(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.TableInstanceId==table&&i.Holder.StartsWith("table:")&&(seatNumber==0||i.SeatNumber==0||i.SeatNumber==seatNumber));
   if(item==null)return Fail("No dirty plate yet.",out message);item.Holder=actor;item.TableInstanceId=0;item.SeatNumber=0;message="Take the dirty plate to the sink.";return true;
  }
  public bool Discard(GameState game,string actor,out string message){
   var item=Hold(actor);if(item==null)return Fail("Your hands are empty.",out message);
   if(item.Kind==KitchenItemKind.GroceryBag)return Fail("Unpack your groceries at the pantry.",out message);
   if(item.Kind==KitchenItemKind.DirtyPlate)return Fail("Wash the dirty plate at the sink.",out message);
   if(item.Kind==KitchenItemKind.Plate&&item.StandPlate){Items.Remove(item);if(item.Components.Count>0)game.StandDirty++;else game.StandClean++;message=item.Components.Count>0?"Food tossed. The plate goes to the stand sink.":"Plate returned to the stand stack.";return true;}
   if(item.Kind==KitchenItemKind.Plate&&item.Disposable){Items.Remove(item);message=item.Components.Count>0?"Food and paper plate tossed.":"Paper plate tossed.";return true;}
   if(item.Kind==KitchenItemKind.Plate&&item.Components.Count>0){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();message="Food discarded. Wash the plate.";return true;}
   if(item.Kind==KitchenItemKind.Plate)CleanPlates++;Items.Remove(item);message="Item discarded; clean plates return to the rack.";return true;
  }
  // Moonberry Float: poured in PourSeconds, then melts: full value for MeltStart seconds, then fades to a slush.
  public const float PourSeconds=2.5f,MeltStart=18,MeltRate=.04f;
  public static bool Melting(KitchenItem i)=>i!=null&&i.Components!=null&&i.Components.Contains("float")&&i.Age>MeltStart;
  public string RecipeOf(KitchenItem item)=>item==null||item.Kind!=KitchenItemKind.Plate?"":RecipeBook.Match(item.Components);
  public string Label(KitchenItem item){if(item==null)return "Empty hands";string dish=RecipeOf(item);if(dish!="")return RestaurantCatalog.Dish(dish).Name+(Melting(item)?" (melting!)":"");switch(item.Kind){case KitchenItemKind.RawEgg:return "Raw egg";case KitchenItemKind.FriedEgg:return "Fried egg";case KitchenItemKind.BurntEgg:return "Burnt egg";case KitchenItemKind.RawSausage:return "Raw sausage";case KitchenItemKind.CookedSausage:return "Cooked sausage";case KitchenItemKind.BurntSausage:return "Burnt sausage";case KitchenItemKind.FloatCup:return "Cup under the nozzle";case KitchenItemKind.GroceryBag:return "Grocery bag ("+item.Components.Count+" items)";case KitchenItemKind.SoupVeg:return "Soup vegetables";case KitchenItemKind.SoupPot:return "Simmering soup";case KitchenItemKind.Soup:return "Pot of Planet soup";case KitchenItemKind.ScorchedSoup:return "Scorched soup";case KitchenItemKind.RawProtein:return "Raw patty";case KitchenItemKind.PreparedPatty:return "Prepared patty";case KitchenItemKind.CookedPatty:return "Cooked patty";case KitchenItemKind.BurntPatty:return "Burnt patty";case KitchenItemKind.RawGreens:return "Uncut greens";case KitchenItemKind.ChoppedGreens:return "Chopped greens";case KitchenItemKind.RawSauce:return "Midnight ingredients";case KitchenItemKind.MidnightSauce:return "Midnight sauce";case KitchenItemKind.DirtyPlate:return "Dirty plate";case KitchenItemKind.Plate:return item.Components.Count==0?(item.Disposable?"Paper plate":"Clean plate"):"Partly assembled dish";default:return "Bun";}}
  // Planet soup on the stove: veg in the pot simmers to soup in SoupSeconds, but scorches if nobody stirs for ScorchSeconds.
  public const float SoupSeconds=14,ScorchSeconds=9,StirWarning=5,SausageCook=.6f,SausageBurn=.5f,EggCook=.45f,EggBurn=.55f;
  public static bool Sausage(KitchenItemKind k)=>k==KitchenItemKind.RawSausage||k==KitchenItemKind.CookedSausage||k==KitchenItemKind.BurntSausage;
  public static bool Egg(KitchenItemKind k)=>k==KitchenItemKind.RawEgg||k==KitchenItemKind.FriedEgg||k==KitchenItemKind.BurntEgg;
  // Grill food that is never flipped: sausages roll themselves, the Cyclops egg is sunny side up.
  public static bool NoFlip(KitchenItemKind k)=>Sausage(k)||Egg(k);
  public static bool GrillRaw(KitchenItemKind k)=>k==KitchenItemKind.RawProtein||k==KitchenItemKind.RawSausage||k==KitchenItemKind.RawEgg;
  public static bool GrillDone(KitchenItemKind k)=>k==KitchenItemKind.CookedPatty||k==KitchenItemKind.CookedSausage||k==KitchenItemKind.FriedEgg;
  public static bool GrillBurnt(KitchenItemKind k)=>k==KitchenItemKind.BurntPatty||k==KitchenItemKind.BurntSausage||k==KitchenItemKind.BurntEgg;
  // Seconds until this item is cooked and until it burns on this grill/oven (level and food type included).
  public static (float cook,float burn) GrillTimes(string catalogId,int level,KitchenItemKind kind){float c=StationUpgrades.CookSeconds(catalogId,level),b=StationUpgrades.BurnSeconds(level);return Sausage(kind)?(c*SausageCook,b*SausageBurn):Egg(kind)?(c*EggCook,b*EggBurn):(c,b);}
  public void StirPot(int stationId){var item=At(stationId);if(item!=null&&item.Kind==KitchenItemKind.SoupPot)item.Stir=0;}
  void TickStove(GameState game,KitchenStation s,KitchenItem item,float seconds){
   if(item.Kind==KitchenItemKind.SoupVeg){item.Kind=KitchenItemKind.SoupPot;item.Stir=0;s.Progress=0;}
   if(item.Kind!=KitchenItemKind.SoupPot)return;
   s.Progress+=seconds;float before=item.Stir;item.Stir+=seconds;if(before<StirWarning&&item.Stir>=StirWarning)game.Emit("stirwarn:"+s.InstanceId);
   if(item.Stir>=ScorchSeconds){item.Kind=KitchenItemKind.ScorchedSoup;item.Quality=0;game.Emit("scorch:"+s.InstanceId);}
   else if(s.Progress>=SoupSeconds)item.Kind=KitchenItemKind.Soup;
  }
  public void Tick(GameState game,float seconds){
   if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
   BalancePlates(game.Restaurant);
   foreach(var s in Stations){if(s.CatalogId!="stove")continue;var pot=At(s.InstanceId);if(pot!=null)TickStove(game,s,pot,seconds*StationUpgrades.SoupSpeed(game.Restaurant.LevelOf(s.InstanceId)));}
   foreach(var s in Stations){var item=At(s.InstanceId);if((s.CatalogId!="grill"&&s.CatalogId!="oven")||item==null)continue;if(item.Kind==KitchenItemKind.RawProtein||item.Kind==KitchenItemKind.CookedPatty){int lvl=game.Restaurant.LevelOf(s.InstanceId);s.Progress+=seconds;item.Age+=seconds;if(s.Progress>=StationUpgrades.BurnSeconds(lvl)){if(item.Kind!=KitchenItemKind.BurntPatty)game.Emit("burn:"+s.InstanceId);item.Kind=KitchenItemKind.BurntPatty;item.Quality=0;}else if(s.Progress>=StationUpgrades.CookSeconds(s.CatalogId,lvl))item.Kind=KitchenItemKind.CookedPatty;}
    // Comet Dog sausages: hot as a comet. They cook in 60% of a patty's time and burn in half the time.
    else if(item.Kind==KitchenItemKind.RawSausage||item.Kind==KitchenItemKind.CookedSausage){int lvl=game.Restaurant.LevelOf(s.InstanceId);s.Progress+=seconds;item.Age+=seconds;if(s.Progress>=StationUpgrades.BurnSeconds(lvl)*SausageBurn){if(item.Kind!=KitchenItemKind.BurntSausage)game.Emit("burn:"+s.InstanceId);item.Kind=KitchenItemKind.BurntSausage;item.Quality=0;}else if(s.Progress>=StationUpgrades.CookSeconds(s.CatalogId,lvl)*SausageCook)item.Kind=KitchenItemKind.CookedSausage;}
    // The Cyclops egg: fries in under half a patty's time; take it off before the yolk goes hard and black.
    else if(item.Kind==KitchenItemKind.RawEgg||item.Kind==KitchenItemKind.FriedEgg){int lvl=game.Restaurant.LevelOf(s.InstanceId);s.Progress+=seconds;item.Age+=seconds;if(s.Progress>=StationUpgrades.BurnSeconds(lvl)*EggBurn){game.Emit("burn:"+s.InstanceId);item.Kind=KitchenItemKind.BurntEgg;item.Quality=0;}else if(s.Progress>=StationUpgrades.CookSeconds(s.CatalogId,lvl)*EggCook)item.Kind=KitchenItemKind.FriedEgg;}}
   foreach(var item in Items)if(item.Components!=null&&item.Components.Contains("float")){if(item.Holder.StartsWith("station:"))item.Age+=seconds;if(item.Age>MeltStart)item.Quality=Math.Min(item.Quality,Math.Max(.35f,1-(item.Age-MeltStart)*MeltRate));}
   foreach(var item in Items){if(!item.Holder.StartsWith("table:"))continue;int id;if(!int.TryParse(item.Holder.Substring(6),out id))continue;var order=game.Restaurant.Orders.Find(o=>o.Id==id);if(order==null||order.Stage==RestaurantOrderStage.Leaving){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();}}
   foreach(var item in Items)if(!item.Holder.StartsWith("station:")&&(item.Kind==KitchenItemKind.CookedPatty||item.Kind==KitchenItemKind.Plate&&item.Components.Count>0)){item.Age+=seconds;item.Quality=Math.Min(item.Quality,Math.Max(.4f,1-Math.Max(0,item.Age-40)*.008f));}
   foreach(var worker in game.Restaurant.Workers)if(worker.Job==StaffJob.Off||(!game.Restaurant.Open&&worker.Job!=StaffJob.Stand))worker.Energy=Math.Min(100,worker.Energy+seconds*.6f);
  }
  public bool SpendFlux(GameState game,string choice,out string message){
   if(choice=="research"){if(game.FluxResearch)return Fail("Research already unlocked.",out message);if(game.Flux<3)return Fail("Prep research costs 3 Flux.",out message);game.Flux-=3;game.FluxResearch=true;message="Permanent research: prep is 35% faster.";return true;}
   var worker=game.Restaurant.Workers.Find(w=>w.Id==choice);if(worker==null)return Fail("Choose a hired worker or research.",out message);if(game.Flux<1||worker.Energy>=100)return Fail("Boost requires 1 Flux and a tired worker.",out message);game.Flux--;worker.Energy=Math.Min(100,worker.Energy+35);message="Restored 35 energy to "+choice;return true;
  }
  public void StartShift(GameState game){if(ShiftActive)return;ShiftActive=true;ShiftNight=game.IsNight;ShiftServed=game.Restaurant.Served;ShiftLost=game.Restaurant.Lost;ShiftStars=game.Restaurant.Stars;ShiftGross=ShiftWages=0;ShiftCosts=0;}
  public ShiftReport FinishShift(GameState game){
   if(!ShiftActive)return LastReport;ShiftActive=false;var r=game.Restaurant;
   LastReport=new ShiftReport{GrossSales=ShiftGross,IngredientCosts=ShiftCosts,Wages=ShiftWages,Net=(int)Math.Round(ShiftGross-ShiftWages-ShiftCosts),Served=r.Served-ShiftServed,Lost=r.Lost-ShiftLost,Satisfaction=r.Satisfaction,StarsBefore=ShiftStars,StarsAfter=r.Stars,StaffSummary=string.Join(" • ",r.Workers.Select(w=>w.Id+": "+w.TasksCompleted+" tasks, "+(int)w.Energy+" energy"))};
   if(LastReport.Served+LastReport.Lost>0){int lvl=r.ShiftLevel;r.ShiftsRun++;LastReport.Comments.Add("Shift "+r.ShiftsRun+" done (difficulty "+lvl+"). Word is spreading: next shift brings "+ShiftDifficulty.Describe(r.ShiftLevel,false)+".");}
   if(LastReport.Served+LastReport.Lost>0){PoorShifts=r.Satisfaction<45?PoorShifts+1:0;if(PoorShifts>=2&&r.Rank>0){r.Rank--;PoorShifts=0;LastReport.Comments.Add("Lost a star after two shifts below 45% satisfaction. Dishes that need it are greyed out until you win it back.");}else if(PoorShifts==1&&r.Rank>0)LastReport.Comments.Add("Your "+StarText.Words(r.Rank)+" rating is at risk: one more shift below 45% satisfaction loses a star.");}
   LastReport.StarsAfter=r.Stars;LastReport.Comments.AddRange(r.Reviews.Take(3).Select(x=>x.Comment));if(LastReport.Comments.Count==0)LastReport.Comments.Add("No guests served. Open the doors and prepare the first orders.");return LastReport;
  }
  public void SanitizeAfterLoad(GameState game){
   Items=Items??new List<KitchenItem>();Stations=Stations??new List<KitchenStation>();Items.RemoveAll(i=>i==null||string.IsNullOrEmpty(i.Holder)||!Enum.IsDefined(typeof(KitchenItemKind),i.Kind));
   var ids=new HashSet<int>();var holders=new HashSet<string>();Items.RemoveAll(i=>!ids.Add(i.Id)||!holders.Add(i.Holder));NextItemId=Math.Max(1,Items.Count==0?NextItemId:Math.Max(NextItemId,Items.Max(i=>i.Id)+1));
   game.StandClean+=Items.RemoveAll(i=>i.StandPlate);if(game.StandClean+game.StandDirty>GameState.StandPlates)game.StandClean=Math.Max(0,GameState.StandPlates-game.StandDirty);
   foreach(var item in Items){
    item.Components=item.Components??new List<string>();
    // Migrate the retired bitmask save format (1 patty / 2 bun / 4 greens / 8 midnight sauce) into named components.
    if(item.Kind==KitchenItemKind.Plate&&item.Components.Count==0&&item.Parts!=0){
     if((item.Parts&1)!=0)item.Components.Add("cooked_patty");if((item.Parts&2)!=0)item.Components.Add("bun");
     if((item.Parts&4)!=0)item.Components.Add("chopped_greens");if((item.Parts&8)!=0)item.Components.Add("midnight_sauce");
    }
    item.Parts=0;
    // The prep bench no longer chops protein; a mid-shift prepared patty from an old save returns to raw.
    if(item.Kind==KitchenItemKind.PreparedPatty)item.Kind=KitchenItemKind.RawProtein;
    if(item.Holder.StartsWith("table:")){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();}
   }
   Stations.RemoveAll(s=>s==null);Stations=Stations.GroupBy(s=>s.InstanceId).Select(g=>g.First()).ToList();foreach(var s in Stations){s.WorkOwner=null;s.WasteCount=Math.Max(0,Math.Min(6,s.WasteCount));if(float.IsNaN(s.Progress)||float.IsInfinity(s.Progress))s.Progress=0;}
   EnsureStations(game.Restaurant);
   foreach(var item in Items.Where(i=>i.Holder.StartsWith("player:")||i.Holder.StartsWith("staff:")).ToList()){
    var free=Stations.Find(s=>At(s.InstanceId)==null&&((item.Kind==KitchenItemKind.DirtyPlate&&s.CatalogId=="sink")||(item.Kind==KitchenItemKind.Plate&&s.CatalogId=="assembly")||(item.Kind!=KitchenItemKind.DirtyPlate&&item.Kind!=KitchenItemKind.Plate&&s.CatalogId=="prep_bench")));
    if(free!=null)item.Holder="station:"+free.InstanceId;
    else if(item.Disposable)Items.Remove(item);
    else if(item.Kind==KitchenItemKind.Plate&&item.Components.Count==0){Items.Remove(item);CleanPlates++;}
    else if(item.Kind==KitchenItemKind.Plate||item.Kind==KitchenItemKind.DirtyPlate){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();item.Holder="table:returned"+item.Id;item.TableInstanceId=game.Restaurant.Layout.Find(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0)?.InstanceId??0;}
    else Items.Remove(item);
   }
   SinkPile=Math.Max(0,SinkPile);CleanPlates=Math.Max(0,Math.Min(PlateCapacity(game.Restaurant)-SinkPile-PlatesInPlay,CleanPlates));BalancePlates(game.Restaurant);ShiftActive=false;
  }
 }
}
