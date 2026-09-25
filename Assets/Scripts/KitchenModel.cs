using System;
using System.Collections.Generic;
using System.Linq;
namespace RestaurantCity {
 public enum KitchenItemKind { RawProtein,PreparedPatty,CookedPatty,BurntPatty,RawGreens,ChoppedGreens,RawSauce,MidnightSauce,Bun,Plate,DirtyPlate }
 [Serializable] public class KitchenItem { public int Id,Parts,TableInstanceId; public KitchenItemKind Kind; public float Quality=1,Age; public string Holder; }
 [Serializable] public class KitchenStation { public int InstanceId; public string CatalogId,WorkOwner,InputAction; public float Progress; }
 [Serializable] public class ShiftReport { public int GrossSales,Wages,Net,Served,Lost,StarsBefore,StarsAfter; public float IngredientCosts,Satisfaction; public List<string> Comments=new List<string>(); public string StaffSummary; }
 [Serializable] public class KitchenState {
  public List<KitchenItem> Items=new List<KitchenItem>(); public List<KitchenStation> Stations=new List<KitchenStation>();
  public int CleanPlates=6,NextItemId=1,PoorShifts; public ShiftReport LastReport; public bool ShiftActive,ShiftNight;
  public int ShiftServed,ShiftLost,ShiftStars,ShiftGross,ShiftWages; public float ShiftCosts;
  static bool Fail(string text,out string message){message=text;return false;}
  public KitchenItem Hold(string actor)=>Items.Find(i=>i.Holder==actor);
  public KitchenItem At(int station)=>Hold("station:"+station);
  KitchenItem Create(KitchenItemKind kind,string holder){var item=new KitchenItem{Id=NextItemId++,Kind=kind,Holder=holder};Items.Add(item);return item;}
  public void EnsureStations(RestaurantState restaurant){
   restaurant.EnsurePhysicalKit();
   var layout=restaurant.Layout.Where(p=>RestaurantCatalog.Find(p.CatalogId)?.Category==CatalogCategory.Kitchen).ToList();
   foreach(var p in layout)if(!Stations.Exists(s=>s.InstanceId==p.InstanceId))Stations.Add(new KitchenStation{InstanceId=p.InstanceId,CatalogId=p.CatalogId});
   foreach(var stale in Stations.Where(s=>!layout.Exists(p=>p.InstanceId==s.InstanceId)).ToList()){
    var item=At(stale.InstanceId);if(item!=null){if(item.Kind==KitchenItemKind.Plate||item.Kind==KitchenItemKind.DirtyPlate){item.Kind=KitchenItemKind.DirtyPlate;item.Parts=0;item.Holder="table:returned"+item.Id;item.TableInstanceId=restaurant.Layout.Find(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0)?.InstanceId??0;}else Items.Remove(item);}Stations.Remove(stale);
   }
  }
  static bool Raw(KitchenItemKind k)=>k==KitchenItemKind.RawProtein||k==KitchenItemKind.RawGreens||k==KitchenItemKind.RawSauce;
  public bool Act(GameState game,string actor,int stationId,string action,out string message){
   if(!game.Restaurant.Owned)return Fail("Buy the restaurant first.",out message);
   EnsureStations(game.Restaurant);var s=Stations.Find(x=>x.InstanceId==stationId);if(s==null)return Fail("Station unavailable.",out message);
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Fail("Someone is working here.",out message);
   var hand=Hold(actor);var item=At(stationId);
   if(s.CatalogId=="pantry"){
    if(hand!=null)return Fail("Your hands are full.",out message);string choice=string.IsNullOrEmpty(action)?"protein":action;
    if(!new[]{"protein","greens","bun","sauce"}.Contains(choice))return Fail("Choose protein, greens, bun or sauce.",out message);
    if(choice=="sauce"&&!game.RecipeUnlocked)return Fail("Find the midnight recipe first.",out message);
    int protein=choice=="protein"||choice=="sauce"?1:0,produce=choice=="greens"?2:choice=="bun"?1:0;
    if(game.Restaurant.Protein<protein||game.Restaurant.Produce<produce)return Fail("Out of ingredients. Visit the city supplier.",out message);
    game.Restaurant.Protein-=protein;game.Restaurant.Produce-=produce;ShiftCosts+=protein*10f/6+produce;
    Create(choice=="protein"?KitchenItemKind.RawProtein:choice=="greens"?KitchenItemKind.RawGreens:choice=="bun"?KitchenItemKind.Bun:KitchenItemKind.RawSauce,actor);message="Picked up "+Label(Hold(actor));return true;
   }
   if(s.CatalogId=="plate_rack"){
    if(hand!=null)return Fail("Your hands are full.",out message);if(CleanPlates<=0)return Fail("No clean plates. Clear a table and wash a dirty plate.",out message);
    CleanPlates--;Create(KitchenItemKind.Plate,actor);message="Clean plate collected.";return true;
   }
   if(item!=null&&hand==null){
    if(s.CatalogId=="prep_bench"&&Raw(item.Kind))return Fail("Hold interact to prepare.",out message);
    if(s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate)return Fail("Hold interact to wash.",out message);
    item.Holder=actor;s.Progress=0;s.WorkOwner=null;message="Picked up "+Label(item);return true;
   }
   if(hand==null)return Fail("Bring an ingredient or plate here.",out message);
   if(item!=null){
    if(s.CatalogId!="assembly"||item.Kind!=KitchenItemKind.Plate)return Fail("Station occupied.",out message);
    int part=hand.Kind==KitchenItemKind.CookedPatty?1:hand.Kind==KitchenItemKind.Bun?2:hand.Kind==KitchenItemKind.ChoppedGreens?4:hand.Kind==KitchenItemKind.MidnightSauce?8:0,result=item.Parts|part;
    if(part==0)return Fail("Prepare the ingredient first. Discard burnt food.",out message);
    if((item.Parts&part)!=0||((result&4)!=0&&result!=4))return Fail("These ingredients do not match. Use another plate.",out message);
    item.Parts=result;item.Quality=Math.Min(item.Quality,hand.Quality);Items.Remove(hand);message="Added ingredient: "+Label(item);return true;
   }
   bool allowed=s.CatalogId=="prep_bench"&&Raw(hand.Kind)||(s.CatalogId=="grill"||s.CatalogId=="oven")&&hand.Kind==KitchenItemKind.PreparedPatty||s.CatalogId=="assembly"&&hand.Kind==KitchenItemKind.Plate||s.CatalogId=="sink"&&hand.Kind==KitchenItemKind.DirtyPlate;
   if(!allowed)return Fail("Use the correct station for this item.",out message);
   hand.Holder="station:"+stationId;hand.Age=0;s.Progress=0;s.WorkOwner=null;message="Placed "+Label(hand);return true;
  }
  public bool Work(GameState game,string actor,int stationId,float seconds,out string message){
   var s=Stations.Find(x=>x.InstanceId==stationId);var item=At(stationId);
   if(s==null||item==null||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return Fail("Place an ingredient or dirty plate here first.",out message);
   if(Hold(actor)!=null)return Fail("Free your hands before working.",out message);
   bool prep=s.CatalogId=="prep_bench"&&Raw(item.Kind),wash=s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate;
   if(!prep&&!wash)return Fail("Nothing to prepare.",out message);
   if(!string.IsNullOrEmpty(s.WorkOwner)&&s.WorkOwner!=actor)return Fail("Someone else is working here.",out message);
   s.WorkOwner=actor;s.Progress+=seconds;float duration=wash?6:item.Kind==KitchenItemKind.RawSauce?4:3;if(prep&&game.FluxResearch)duration*=.65f;
   if(s.Progress<duration){message=(wash?"Washing ":"Preparing ")+(int)(s.Progress/duration*100)+"%";return true;}
   if(wash){Items.Remove(item);CleanPlates++;game.Restaurant.Cleanliness=Math.Min(100,game.Restaurant.Cleanliness+5);message="Clean plate returned to rack.";}
   else{item.Kind=item.Kind==KitchenItemKind.RawProtein?KitchenItemKind.PreparedPatty:item.Kind==KitchenItemKind.RawGreens?KitchenItemKind.ChoppedGreens:KitchenItemKind.MidnightSauce;message=Label(item)+" ready.";}
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
   if(item.Kind==KitchenItemKind.Plate&&item.Parts!=0){item.Kind=KitchenItemKind.DirtyPlate;item.Parts=0;message="Food discarded. Wash the plate.";return true;}
   if(item.Kind==KitchenItemKind.Plate)CleanPlates++;Items.Remove(item);message="Item discarded; clean plates return to the rack.";return true;
  }
  public string RecipeOf(KitchenItem item)=>item==null||item.Kind!=KitchenItemKind.Plate?"":item.Parts==3?"burger":item.Parts==4?"salad":item.Parts==11?"midnight":"";
  public string Label(KitchenItem item){if(item==null)return "Empty hands";string dish=RecipeOf(item);if(dish!="")return RestaurantCatalog.Dish(dish).Name;switch(item.Kind){case KitchenItemKind.RawProtein:return "Raw protein";case KitchenItemKind.PreparedPatty:return "Prepared patty";case KitchenItemKind.CookedPatty:return "Cooked patty";case KitchenItemKind.BurntPatty:return "Burnt patty";case KitchenItemKind.RawGreens:return "Uncut greens";case KitchenItemKind.ChoppedGreens:return "Chopped greens";case KitchenItemKind.RawSauce:return "Midnight ingredients";case KitchenItemKind.MidnightSauce:return "Midnight sauce";case KitchenItemKind.DirtyPlate:return "Dirty plate";case KitchenItemKind.Plate:return item.Parts==0?"Clean plate":"Partly assembled dish";default:return "Bun";}}
  public void Tick(GameState game,float seconds){
   if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
   foreach(var s in Stations){var item=At(s.InstanceId);if((s.CatalogId!="grill"&&s.CatalogId!="oven")||item==null)continue;if(item.Kind==KitchenItemKind.PreparedPatty||item.Kind==KitchenItemKind.CookedPatty){s.Progress+=seconds;item.Age+=seconds;if(s.Progress>=24){item.Kind=KitchenItemKind.BurntPatty;item.Quality=0;}else if(s.Progress>=(s.CatalogId=="oven"?6:8))item.Kind=KitchenItemKind.CookedPatty;}}
   foreach(var item in Items){if(!item.Holder.StartsWith("table:"))continue;int id;if(!int.TryParse(item.Holder.Substring(6),out id))continue;var order=game.Restaurant.Orders.Find(o=>o.Id==id);if(order==null||order.Stage==RestaurantOrderStage.Leaving){item.Kind=KitchenItemKind.DirtyPlate;item.Parts=0;}}
   foreach(var item in Items)if(!item.Holder.StartsWith("station:")&&(item.Kind==KitchenItemKind.CookedPatty||item.Kind==KitchenItemKind.Plate&&item.Parts!=0)){item.Age+=seconds;item.Quality=Math.Min(item.Quality,Math.Max(.4f,1-Math.Max(0,item.Age-40)*.008f));}
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
   foreach(var item in Items)if(item.Holder.StartsWith("table:")){item.Kind=KitchenItemKind.DirtyPlate;item.Parts=0;}
   Stations.RemoveAll(s=>s==null);Stations=Stations.GroupBy(s=>s.InstanceId).Select(g=>g.First()).ToList();foreach(var s in Stations){s.WorkOwner=null;if(float.IsNaN(s.Progress)||float.IsInfinity(s.Progress))s.Progress=0;}
   EnsureStations(game.Restaurant);
   foreach(var item in Items.Where(i=>i.Holder.StartsWith("player:")||i.Holder.StartsWith("staff:")).ToList()){
    var free=Stations.Find(s=>At(s.InstanceId)==null&&((item.Kind==KitchenItemKind.DirtyPlate&&s.CatalogId=="sink")||(item.Kind==KitchenItemKind.Plate&&s.CatalogId=="assembly")||(item.Kind!=KitchenItemKind.DirtyPlate&&item.Kind!=KitchenItemKind.Plate&&s.CatalogId=="prep_bench")));
    if(free!=null)item.Holder="station:"+free.InstanceId;
    else if(item.Kind==KitchenItemKind.Plate&&item.Parts==0){Items.Remove(item);CleanPlates++;}
    else if(item.Kind==KitchenItemKind.Plate||item.Kind==KitchenItemKind.DirtyPlate){item.Kind=KitchenItemKind.DirtyPlate;item.Parts=0;item.Holder="table:returned"+item.Id;item.TableInstanceId=game.Restaurant.Layout.Find(p=>RestaurantCatalog.Find(p.CatalogId).Seats>0)?.InstanceId??0;}
    else Items.Remove(item);
   }
   CleanPlates=Math.Max(0,Math.Min(6-Items.Count(i=>i.Kind==KitchenItemKind.Plate||i.Kind==KitchenItemKind.DirtyPlate),CleanPlates));ShiftActive=false;
  }
 }
}


