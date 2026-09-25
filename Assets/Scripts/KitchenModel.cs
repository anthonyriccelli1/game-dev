using System;
using System.Collections.Generic;
using System.Linq;
namespace RestaurantCity {
 public enum KitchenItemKind { RawProtein,PreparedPatty,CookedPatty,BurntPatty,RawGreens,ChoppedGreens,RawSauce,MidnightSauce,Bun,Plate,DirtyPlate }
 public enum KitchenActionKind { None,Tap,Hold }
 // Preview describes exactly what a single interact press will do; Act performs it by calling Preview and
 // invoking the very same Apply callback it returned, so the on-screen prompt and the executed action can
 // never disagree (Stage A / A1).
 public struct KitchenAction { public bool Allowed; public string Label; public KitchenActionKind Kind; public string FailReason; public Func<string> Apply; }
 [Serializable] public class KitchenItem { public int Id,Parts,TableInstanceId; public KitchenItemKind Kind; public float Quality=1,Age; public string Holder; public List<string> Components=new List<string>(); }
 [Serializable] public class KitchenStation { public int InstanceId; public string CatalogId,WorkOwner,InputAction; public float Progress; }
 [Serializable] public class ShiftReport { public int GrossSales,Wages,Net,Served,Lost,StarsBefore,StarsAfter; public float IngredientCosts,Satisfaction; public List<string> Comments=new List<string>(); public string StaffSummary; }
 [Serializable] public class KitchenState {
  public List<KitchenItem> Items=new List<KitchenItem>(); public List<KitchenStation> Stations=new List<KitchenStation>();
  public int CleanPlates=6,NextItemId=1,PoorShifts; public ShiftReport LastReport; public bool ShiftActive,ShiftNight;
  public int ShiftServed,ShiftLost,ShiftStars,ShiftGross,ShiftWages; public float ShiftCosts;
  static bool Fail(string text,out string message){message=text;return false;}
  static KitchenAction Blocked(string reason)=>new KitchenAction{Allowed=false,Kind=KitchenActionKind.None,FailReason=reason,Label=reason};
  static KitchenAction NeedsHold(string label)=>new KitchenAction{Allowed=false,Kind=KitchenActionKind.Hold,FailReason=label,Label=label};
  static KitchenAction Tap(string label,Func<string> apply)=>new KitchenAction{Allowed=true,Kind=KitchenActionKind.Tap,Label=label,FailReason="",Apply=apply};
  public const float WashSeconds=3.5f;
  public KitchenItem Hold(string actor)=>Items.Find(i=>i.Holder==actor);
  public KitchenItem At(int station)=>Hold("station:"+station);
  KitchenItem Create(KitchenItemKind kind,string holder){var item=new KitchenItem{Id=NextItemId++,Kind=kind,Holder=holder};Items.Add(item);return item;}
  public void EnsureStations(RestaurantState restaurant){
   restaurant.EnsurePhysicalKit();
   var layout=restaurant.Layout.Where(p=>RestaurantCatalog.Find(p.CatalogId)?.Category==CatalogCategory.Kitchen).ToList();
   foreach(var p in layout)if(!Stations.Exists(s=>s.InstanceId==p.InstanceId))Stations.Add(new KitchenStation{InstanceId=p.InstanceId,CatalogId=p.CatalogId});
   foreach(var stale in Stations.Where(s=>!layout.Exists(p=>p.InstanceId==s.InstanceId)).ToList()){
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
    case KitchenItemKind.ChoppedGreens:return "chopped_greens";
    case KitchenItemKind.MidnightSauce:return "midnight_sauce";
    case KitchenItemKind.Bun:return "bun";
    default:return null;
   }
  }
  static bool IsCounter(string id)=>id=="assembly"||id=="counter";
  static string ComponentLabel(string id)=>id=="bun"?"a bun":id=="cooked_patty"?"the cooked patty":id=="chopped_greens"?"chopped greens":id=="midnight_sauce"?"midnight sauce":id;
  static string PantryLabel(string choice)=>choice=="protein"?"a raw patty":choice=="greens"?"greens":choice=="bun"?"a bun":"sauce ingredients";
  bool CanAddComponent(KitchenItem plate,string component,out string message){
   if(plate.Components.Contains(component))return Fail("This plate already has "+ComponentLabel(component)+".",out message);
   if(RecipeBook.Recipes.Any(r=>r.Components.Contains(component)&&plate.Components.All(c=>r.Components.Contains(c)))){message="";return true;}
   var building=RecipeBook.Recipes.FirstOrDefault(r=>plate.Components.Count>0&&plate.Components.All(c=>r.Components.Contains(c)));
   string name=building!=null?RestaurantCatalog.Dish(building.DishId).Name:"This plate";
   return Fail(name+" doesn't use "+ComponentLabel(component)+".",out message);
  }
  KitchenAction PreviewPantry(GameState game,string actor,KitchenItem hand,string subId){
   string choice=string.IsNullOrEmpty(subId)?"protein":subId;
   if(!new[]{"protein","greens","bun","sauce"}.Contains(choice))return Blocked("Choose protein, greens, bun or sauce.");
   if(choice=="sauce"&&!game.RecipeUnlocked)return Blocked("Needs the midnight recipe.");
   if(hand!=null&&hand.Kind==KitchenItemKind.Plate&&choice=="bun"){
    // Fewer-press shortcut: a bun needs no prep, so it can go straight onto a plate you are already carrying.
    if(!CanAddComponent(hand,"bun",out string reason))return Blocked(reason);
    if(game.Restaurant.Produce<1)return Blocked("Out of buns. Visit the city supplier.");
    var plate=hand;
    return Tap("Add bun to plate",()=>{game.Restaurant.Produce--;ShiftCosts+=1;plate.Components.Add("bun");return "Added a bun to the plate.";});
   }
   if(hand!=null)return Blocked("Your hands are full.");
   int protein=choice=="protein"||choice=="sauce"?1:0,produce=choice=="greens"?2:choice=="bun"?1:0;
   if(game.Restaurant.Protein<protein||game.Restaurant.Produce<produce)return Blocked("Out of ingredients. Visit the city supplier.");
   var kind=choice=="protein"?KitchenItemKind.RawProtein:choice=="greens"?KitchenItemKind.RawGreens:choice=="bun"?KitchenItemKind.Bun:KitchenItemKind.RawSauce;
   return Tap("Take "+PantryLabel(choice),()=>{game.Restaurant.Protein-=protein;game.Restaurant.Produce-=produce;ShiftCosts+=protein*10f/6+produce;var created=Create(kind,actor);return "Picked up "+Label(created);});
  }
  public KitchenAction Preview(GameState game,string actor,int stationId,string subId){
   if(!game.Restaurant.Owned)return Blocked("Buy the restaurant first.");
   EnsureStations(game.Restaurant);
   var s=Stations.Find(x=>x.InstanceId==stationId);
   if(s==null)return Blocked("Station unavailable.");
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Blocked("Someone is working here.");
   var hand=Hold(actor);var item=At(stationId);
   if(s.CatalogId=="pantry")return PreviewPantry(game,actor,hand,subId);
   if(s.CatalogId=="trash"){
    if(hand==null)return Blocked("Trash can. Bring food you want to throw away.");
    if(hand.Kind==KitchenItemKind.DirtyPlate)return Blocked("Wash dirty plates at the sink.");
    if(hand.Kind==KitchenItemKind.Plate&&hand.Components.Count==0)return Blocked("That plate is clean. Keep it or set it down.");
    string what=hand.Kind==KitchenItemKind.Plate?"the food (keep the plate)":Label(hand).ToLower();
    return Tap("Throw away "+what,()=>{Discard(game,actor,out string msg);return msg;});
   }
   if(s.CatalogId=="plate_rack"){
    if(hand!=null)return Blocked("Your hands are full.");
    if(CleanPlates<=0)return Blocked("No clean plates. Clear a table and wash a dirty plate.");
    return Tap("Take a clean plate",()=>{CleanPlates--;Create(KitchenItemKind.Plate,actor);return "Clean plate collected.";});
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
   if(item!=null&&hand==null){
    if(s.CatalogId=="prep_bench"&&Raw(item.Kind))return NeedsHold("Chop "+Label(item).ToLower());
    if(s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate)return NeedsHold("Wash the plate");
    var pick=item;
    return Tap("Take "+Label(item).ToLower(),()=>{pick.Holder=actor;s.Progress=0;s.WorkOwner=null;return "Picked up "+Label(pick);});
   }
   if(hand==null)return Blocked("Bring an ingredient or plate here.");
   if(item!=null){
    if(!IsCounter(s.CatalogId)||item.Kind!=KitchenItemKind.Plate)return Blocked(IsCounter(s.CatalogId)?"Counter is full. Put a plate here to build on it.":"Station occupied.");
    string part=hand.Kind==KitchenItemKind.CookedPatty?"cooked_patty":hand.Kind==KitchenItemKind.Bun?"bun":hand.Kind==KitchenItemKind.ChoppedGreens?"chopped_greens":hand.Kind==KitchenItemKind.MidnightSauce?"midnight_sauce":null;
    if(part==null)return Blocked("Prepare the ingredient first. Discard burnt food.");
    if(!CanAddComponent(item,part,out string why))return Blocked(why);
    var plate=item;var carried=hand;
    return Tap("Add "+ComponentLabel(part)+" to plate",()=>{plate.Components.Add(part);plate.Quality=Math.Min(plate.Quality,carried.Quality);Items.Remove(carried);return "Added "+ComponentLabel(part)+" to the plate.";});
   }
   bool allowed=s.CatalogId=="prep_bench"&&Raw(hand.Kind)||(s.CatalogId=="grill"||s.CatalogId=="oven")&&hand.Kind==KitchenItemKind.RawProtein||IsCounter(s.CatalogId)||s.CatalogId=="sink"&&hand.Kind==KitchenItemKind.DirtyPlate;
   if(!allowed)return Blocked("Use the correct station for this item.");
   var carriedItem=hand;
   return Tap("Place "+Label(hand).ToLower(),()=>{carriedItem.Holder="station:"+stationId;carriedItem.Age=0;s.Progress=0;s.WorkOwner=null;return "Placed "+Label(carriedItem);});
  }
  public bool Act(GameState game,string actor,int stationId,string subId,out string message){
   var preview=Preview(game,actor,stationId,subId);
   if(!preview.Allowed){message=preview.FailReason;return false;}
   message=preview.Apply();return true;
  }
  public bool Work(GameState game,string actor,int stationId,float seconds,out string message){
   var s=Stations.Find(x=>x.InstanceId==stationId);var item=At(stationId);
   if(s==null||item==null||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return Fail("Place an ingredient or dirty plate here first.",out message);
   if(Hold(actor)!=null)return Fail("Free your hands before working.",out message);
   bool prep=s.CatalogId=="prep_bench"&&Raw(item.Kind),wash=s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate;
   if(!prep&&!wash)return Fail("Nothing to prepare.",out message);
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Fail("Someone else is working here.",out message);
   s.WorkOwner=actor;s.Progress+=seconds;float duration=wash?WashSeconds:item.Kind==KitchenItemKind.RawSauce?4:3;if(prep&&game.FluxResearch)duration*=.65f;
   if(s.Progress<duration){message=(wash?"Washing ":"Preparing ")+(int)(s.Progress/duration*100)+"%";return true;}
   if(wash){Items.Remove(item);CleanPlates++;game.Restaurant.Cleanliness=Math.Min(100,game.Restaurant.Cleanliness+5);message="Clean plate returned to rack.";}
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
   item.Holder="table:"+orderId;item.TableInstanceId=order.SeatInstanceId;return true;
  }
  public int DirtyAtTable(int table)=>Items.Count(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.TableInstanceId==table&&i.Holder.StartsWith("table:"));
  public bool ClearTable(GameState game,string actor,int table,out string message){
   if(Hold(actor)!=null)return Fail("Your hands are full.",out message);var item=Items.Find(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.TableInstanceId==table&&i.Holder.StartsWith("table:"));
   if(item==null)return Fail("No dirty plate yet.",out message);item.Holder=actor;item.TableInstanceId=0;message="Take the dirty plate to the sink.";return true;
  }
  public bool Discard(GameState game,string actor,out string message){
   var item=Hold(actor);if(item==null)return Fail("Your hands are empty.",out message);
   if(item.Kind==KitchenItemKind.DirtyPlate)return Fail("Wash the dirty plate at the sink.",out message);
   if(item.Kind==KitchenItemKind.Plate&&item.Components.Count>0){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();message="Food discarded. Wash the plate.";return true;}
   if(item.Kind==KitchenItemKind.Plate)CleanPlates++;Items.Remove(item);message="Item discarded; clean plates return to the rack.";return true;
  }
  public string RecipeOf(KitchenItem item)=>item==null||item.Kind!=KitchenItemKind.Plate?"":RecipeBook.Match(item.Components);
  public string Label(KitchenItem item){if(item==null)return "Empty hands";string dish=RecipeOf(item);if(dish!="")return RestaurantCatalog.Dish(dish).Name;switch(item.Kind){case KitchenItemKind.RawProtein:return "Raw patty";case KitchenItemKind.PreparedPatty:return "Prepared patty";case KitchenItemKind.CookedPatty:return "Cooked patty";case KitchenItemKind.BurntPatty:return "Burnt patty";case KitchenItemKind.RawGreens:return "Uncut greens";case KitchenItemKind.ChoppedGreens:return "Chopped greens";case KitchenItemKind.RawSauce:return "Midnight ingredients";case KitchenItemKind.MidnightSauce:return "Midnight sauce";case KitchenItemKind.DirtyPlate:return "Dirty plate";case KitchenItemKind.Plate:return item.Components.Count==0?"Clean plate":"Partly assembled dish";default:return "Bun";}}
  public void Tick(GameState game,float seconds){
   if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
   foreach(var s in Stations){var item=At(s.InstanceId);if((s.CatalogId!="grill"&&s.CatalogId!="oven")||item==null)continue;if(item.Kind==KitchenItemKind.RawProtein||item.Kind==KitchenItemKind.CookedPatty){s.Progress+=seconds;item.Age+=seconds;if(s.Progress>=24){item.Kind=KitchenItemKind.BurntPatty;item.Quality=0;}else if(s.Progress>=(s.CatalogId=="oven"?6:8))item.Kind=KitchenItemKind.CookedPatty;}}
   foreach(var item in Items){if(!item.Holder.StartsWith("table:"))continue;int id;if(!int.TryParse(item.Holder.Substring(6),out id))continue;var order=game.Restaurant.Orders.Find(o=>o.Id==id);if(order==null||order.Stage==RestaurantOrderStage.Leaving){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();}}
   foreach(var item in Items)if(!item.Holder.StartsWith("station:")&&(item.Kind==KitchenItemKind.CookedPatty||item.Kind==KitchenItemKind.Plate&&item.Components.Count>0)){item.Age+=seconds;item.Quality=Math.Min(item.Quality,Math.Max(.4f,1-Math.Max(0,item.Age-40)*.008f));}
   foreach(var worker in game.Restaurant.Workers)if(worker.Job==StaffJob.Off||!game.Restaurant.Open)worker.Energy=Math.Min(100,worker.Energy+seconds*.6f);
  }
  public bool SpendFlux(GameState game,string choice,out string message){
   if(choice=="research"){if(game.FluxResearch)return Fail("Research already unlocked.",out message);if(game.Flux<3)return Fail("Prep research costs 3 Flux.",out message);game.Flux-=3;game.FluxResearch=true;message="Permanent research: prep is 35% faster.";return true;}
   var worker=game.Restaurant.Workers.Find(w=>w.Id==choice);if(worker==null)return Fail("Choose a hired worker or research.",out message);if(game.Flux<1||worker.Energy>=100)return Fail("Boost requires 1 Flux and a tired worker.",out message);game.Flux--;worker.Energy=Math.Min(100,worker.Energy+35);message="Restored 35 energy to "+choice;return true;
  }
  public void StartShift(GameState game){if(ShiftActive)return;ShiftActive=true;ShiftNight=game.IsNight;ShiftServed=game.Restaurant.Served;ShiftLost=game.Restaurant.Lost;ShiftStars=game.Restaurant.Stars;ShiftGross=ShiftWages=0;ShiftCosts=0;}
  public ShiftReport FinishShift(GameState game){
   if(!ShiftActive)return LastReport;ShiftActive=false;var r=game.Restaurant;
   LastReport=new ShiftReport{GrossSales=ShiftGross,IngredientCosts=ShiftCosts,Wages=ShiftWages,Net=(int)Math.Round(ShiftGross-ShiftWages-ShiftCosts),Served=r.Served-ShiftServed,Lost=r.Lost-ShiftLost,Satisfaction=r.Satisfaction,StarsBefore=ShiftStars,StarsAfter=r.Stars,StaffSummary=string.Join(" • ",r.Workers.Select(w=>w.Id+": "+w.TasksCompleted+" tasks, "+(int)w.Energy+" energy"))};
   if(LastReport.Served+LastReport.Lost>0){PoorShifts=r.Satisfaction<45?PoorShifts+1:0;if(PoorShifts>=2&&r.Rank>1){r.Rank--;PoorShifts=0;LastReport.Comments.Add("Lost a star after two consecutive shifts below 45% satisfaction. Improve waits, food and cleanliness.");}}
   LastReport.StarsAfter=r.Stars;LastReport.Comments.AddRange(r.Reviews.Take(3).Select(x=>x.Comment));if(LastReport.Comments.Count==0)LastReport.Comments.Add("No guests served. Open the doors and prepare the first orders.");return LastReport;
  }
  public void SanitizeAfterLoad(GameState game){
   Items=Items??new List<KitchenItem>();Stations=Stations??new List<KitchenStation>();Items.RemoveAll(i=>i==null||string.IsNullOrEmpty(i.Holder)||!Enum.IsDefined(typeof(KitchenItemKind),i.Kind));
   var ids=new HashSet<int>();var holders=new HashSet<string>();Items.RemoveAll(i=>!ids.Add(i.Id)||!holders.Add(i.Holder));NextItemId=Math.Max(1,Items.Count==0?NextItemId:Math.Max(NextItemId,Items.Max(i=>i.Id)+1));
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
   Stations.RemoveAll(s=>s==null);Stations=Stations.GroupBy(s=>s.InstanceId).Select(g=>g.First()).ToList();foreach(var s in Stations){s.WorkOwner=null;if(float.IsNaN(s.Progress)||float.IsInfinity(s.Progress))s.Progress=0;}
   EnsureStations(game.Restaurant);
   foreach(var item in Items.Where(i=>i.Holder.StartsWith("player:")||i.Holder.StartsWith("staff:")).ToList()){
    var free=Stations.Find(s=>At(s.InstanceId)==null&&((item.Kind==KitchenItemKind.DirtyPlate&&s.CatalogId=="sink")||(item.Kind==KitchenItemKind.Plate&&s.CatalogId=="assembly")||(item.Kind!=KitchenItemKind.DirtyPlate&&item.Kind!=KitchenItemKind.Plate&&s.CatalogId=="prep_bench")));
    if(free!=null)item.Holder="station:"+free.InstanceId;
    else if(item.Kind==KitchenItemKind.Plate&&item.Components.Count==0){Items.Remove(item);CleanPlates++;}
    else if(item.Kind==KitchenItemKind.Plate||item.Kind==KitchenItemKind.DirtyPlate){item.Kind=KitchenItemKind.DirtyPlate;item.Components.Clear();item.Holder="table:returned"+item.Id;item.TableInstanceId=game.Restaurant.Layout.Find(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0)?.InstanceId??0;}
    else Items.Remove(item);
   }
   CleanPlates=Math.Max(0,Math.Min(6-Items.Count(i=>i.Kind==KitchenItemKind.Plate||i.Kind==KitchenItemKind.DirtyPlate),CleanPlates));ShiftActive=false;
  }
 }
}
