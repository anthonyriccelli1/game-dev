using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
 public partial class RestaurantController {
  sealed class KitchenTask { public string Station,Action; public int Target; public KitchenTask(string s,string a="",int t=0){Station=s;Action=a;Target=t;} }
  readonly Dictionary<string,Queue<KitchenTask>> workerPlans=new Dictionary<string,Queue<KitchenTask>>();
  void UpdatePhysicalEmployees(float dt){
   var k=Game.State.Kitchen;
   foreach(var worker in Data.Workers){
    string actor="staff:"+worker.Id;
    string workerName=RestaurantCatalog.Worker(worker.Id)?.Name??worker.Id;
    if(!employees.TryGetValue(worker.Id,out var view)){var root=People.Worker(worker.Id,RestaurantCatalog.Worker(worker.Id)?.ModelType??8,transform);root.transform.position=W(-9,.055f,-12);view=new EmployeeView{Root=root,Motion=root.GetComponent<CharacterMotion>(),Bubble=WorldCaption(root.transform,"",new Vector3(0,2.4f,0),.023f)};employees[worker.Id]=view;}
    if(!workerPlans.TryGetValue(worker.Id,out var plan)){plan=new Queue<KitchenTask>();workerPlans[worker.Id]=plan;}
    if(view.Job!=worker.Job){
     // Moving a worker off the stand: they visibly walk from the stand, across the street and in through the door.
     if(view.Job==StaffJob.Stand&&worker.Job!=StaffJob.Stand){view.Root.transform.position=StandOrigin+(InTruck?new Vector3(-1.95f,.055f,9.45f):new Vector3(-1,.055f,9.35f));view.Commute.Clear();
      // From the truck: out of the side door, over South Avenue at the crosswalk, up East Street and along Main to the door.
      var route=InTruck?new[]{StandOrigin+new Vector3(1.42f,0,9.45f),StandOrigin+new Vector3(1.42f,0,7.85f),new Vector3(StandOrigin.x+1.42f,0,StandOrigin.z+5.8f),new Vector3(StandOrigin.x+5.2f,0,StandOrigin.z+5.8f),new Vector3(5.2f,0,-57.8f),new Vector3(19,0,-57.8f),new Vector3(19,0,-42.5f),new Vector3(32.5f,0,-42.5f),new Vector3(32.5f,0,-7.5f),W(-10,0,-7.5f),W(-10,0,-10)}
       :new[]{new Vector3(-4.3f,0,9.35f),new Vector3(-4.3f,0,2),W(-10,0,-6.5f),W(-10,0,-10)};
      foreach(var p in route)view.Commute.Enqueue(p);Feedback((RestaurantCatalog.Worker(worker.Id)?.Name??worker.Id)+(InTruck?" is walking over from the truck.":" is walking over from the stand."));}
     view.Job=worker.Job;plan.Clear();view.Path.Clear();k.ReleaseWork(actor);}
    view.Motion.Working=false;view.Motion.Walking=false;
    // Stand workers are drawn at the street stand (PhysicalStand), not in the restaurant.
    view.Root.SetActive(worker.Job!=StaffJob.Stand);if(worker.Job==StaffJob.Stand){view.Commute.Clear();k.ReleaseWork(actor);continue;}
    if(view.Commute.Count>0){view.Motion.Walking=Follow(view.Root.transform,view.Commute,dt*2.7f);SetBubble(view.Bubble,workerName+" / heading to the restaurant");continue;}
    if(worker.Job==StaffJob.Off||worker.Energy<=2||!ServiceInProgress){SetBubble(view.Bubble,workerName+" / resting / "+(int)worker.Energy+" energy");continue;}
    // Zombie Cafe rule: each worker does exactly the one job you assign.
    var job=worker.Job;
    if(plan.Count==0){
     var hand=k.Hold(actor);
     if(hand!=null){if(hand.Kind==KitchenItemKind.DirtyPlate){plan.Enqueue(new KitchenTask("sink"));plan.Enqueue(new KitchenTask("sink","work"));}else{var order=Data.Orders.FirstOrDefault(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(hand));if(order!=null)plan.Enqueue(new KitchenTask("guest","serve",order.Id));else if(hand.Kind==KitchenItemKind.Plate)plan.Enqueue(new KitchenTask("assembly"));else k.Discard(Game.State,actor,out _);}}
     else if(job==StaffJob.Clean){var dirty=k.Items.FirstOrDefault(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.Holder.StartsWith("table:"));if(dirty!=null){plan.Enqueue(new KitchenTask("table","clear",dirty.TableInstanceId));plan.Enqueue(new KitchenTask("sink"));plan.Enqueue(new KitchenTask("sink","work"));}}
     else if(job==StaffJob.Serve){var ready=k.Items.FirstOrDefault(i=>i.Holder.StartsWith("station:")&&k.RecipeOf(i)!=""&&Data.Orders.Any(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(i)));if(ready!=null){int station=int.Parse(ready.Holder.Substring(8));var order=Data.Orders.First(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(ready));plan.Enqueue(new KitchenTask("assembly","",station));plan.Enqueue(new KitchenTask("guest","serve",order.Id));}}
     else if(job==StaffJob.Cook){var order=Data.Orders.FirstOrDefault(o=>o.Stage==RestaurantOrderStage.Waiting&&!employees.Values.Any(v=>v!=view&&v.OrderId==o.Id)&&!k.Items.Any(i=>k.RecipeOf(i)==o.DishId));if(order!=null&&order.DishId=="float"){view.OrderId=order.Id;plan.Enqueue(new KitchenTask("drink_machine"));plan.Enqueue(new KitchenTask("drink_machine","work"));plan.Enqueue(new KitchenTask("drink_machine"));}
      else if(order!=null&&k.Stations.Any(s=>s.CatalogId=="assembly"&&k.At(s.InstanceId)==null)){view.OrderId=order.Id;plan.Enqueue(new KitchenTask("plate_rack"));plan.Enqueue(new KitchenTask("assembly"));AddIngredient(plan,order.DishId=="salad"?"greens":order.DishId=="soup"?"soup":order.DishId=="cometdog"?"sausage":"protein");if(order.DishId!="salad"&&order.DishId!="soup"){plan.Enqueue(new KitchenTask("pantry","bun"));plan.Enqueue(new KitchenTask("assembly"));}if(order.DishId=="midnight")AddIngredient(plan,"sauce");if(order.DishId=="cyclops")AddIngredient(plan,"egg");}}
    }
    if(plan.Count==0){SetBubble(view.Bubble,workerName+" / ready / "+(int)worker.Energy+" energy");continue;}
    var task=plan.Peek();Vector3 destination;KitchenStation stationData=null;
    if(task.Station=="guest"){if(!guests.TryGetValue(task.Target,out var guest)||!guest.Seat){plan.Clear();continue;}destination=guest.Seat.position;}
    else if(task.Station=="table"){if(!Furnishings.TryGetValue(task.Target,out var table)){plan.Clear();continue;}destination=table.transform.position+Vector3.forward;}
    else{string cat=task.Station;if(cat=="pantry"){var ing=Ingredients.ForShelf(task.Action);if(ing!=null&&ing.Cold&&Data.HasEquipment("fridge"))cat="fridge";}   // cold food is fetched from the fridge
     stationData=k.Stations.FirstOrDefault(s=>!KitchenState.IsStandStation(s.InstanceId)&&s.CatalogId==cat&&(task.Target==0||s.InstanceId==task.Target));if(stationData==null)continue;var furniture=Furnishings[stationData.InstanceId];var wp=furniture.transform.Find("WorkPoint");destination=wp?wp.position:furniture.transform.position+Vector3.forward;}
    destination.y=.055f;var stats=StaffStats.For(worker.Id);worker.Energy=Mathf.Max(0,worker.Energy-dt*WorkerDrain*StaffStats.DrainMultiplier(stats));
    SetBubble(view.Bubble,workerName+" / "+task.Station.Replace('_',' ')+" / "+(int)worker.Energy+" energy");
    if(Vector3.Distance(view.Root.transform.position,destination)>1.2f){if(view.Path.Count==0)AppendRoute(view.Path,view.Root.transform.position,destination);view.Motion.Walking=Follow(view.Root.transform,view.Path,dt*(StaffStats.Exhausted(stats,worker.Energy)?1.3f:2.3f)*StaffStats.WalkMultiplier(stats,Game.State.IsNight));continue;}
    view.Path.Clear();view.Motion.Working=true;bool done=false;
    if(task.Action=="work"){k.Work(Game.State,actor,stationData.InstanceId,dt*WorkerSpeed(worker,task.Station,Game.State.IsNight),out _);var item=k.At(stationData.InstanceId);done=item==null||!(item.Kind==KitchenItemKind.RawProtein||item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.RawSauce||item.Kind==KitchenItemKind.DirtyPlate||item.Kind==KitchenItemKind.FloatCup);}
    else if(task.Action=="simmer"){var pot=k.At(stationData.InstanceId);if(pot!=null&&pot.Kind==KitchenItemKind.SoupPot&&pot.Stir>=KitchenState.StirWarning)k.StirPot(stationData.InstanceId);done=pot==null||pot.Kind==KitchenItemKind.Soup||pot.Kind==KitchenItemKind.ScorchedSoup;}
    else if(task.Action=="wait"){var item=k.At(stationData.InstanceId);done=item!=null&&(KitchenState.GrillDone(item.Kind)||KitchenState.GrillBurnt(item.Kind));}
    else if(task.Action=="serve"){done=k.Serve(Game.State,actor,task.Target,out _);if(!Data.Orders.Any(o=>o.Id==task.Target&&o.Stage==RestaurantOrderStage.Waiting))done=true;}
    else if(task.Action=="clear")done=k.ClearTable(Game.State,actor,task.Target,out _);
    else done=k.Act(Game.State,actor,stationData.InstanceId,task.Action,out _);
    if(done){plan.Dequeue();worker.TasksCompleted++;if(plan.Count==0)view.OrderId=-1;}
   }
  }
  // Raw protein now goes straight from the pantry to the grill (no prep-bench chop step); greens and
  // midnight sauce still need the prep bench.
  static void AddIngredient(Queue<KitchenTask> plan,string ingredient){
   plan.Enqueue(new KitchenTask("pantry",ingredient));
   if(ingredient=="protein"||ingredient=="sausage"||ingredient=="egg"){plan.Enqueue(new KitchenTask("grill"));plan.Enqueue(new KitchenTask("grill","wait"));plan.Enqueue(new KitchenTask("grill"));}
   else if(ingredient=="soup"){plan.Enqueue(new KitchenTask("stove"));plan.Enqueue(new KitchenTask("stove","simmer"));plan.Enqueue(new KitchenTask("stove"));}
   else{plan.Enqueue(new KitchenTask("prep_bench"));plan.Enqueue(new KitchenTask("prep_bench","work"));plan.Enqueue(new KitchenTask("prep_bench"));}
   plan.Enqueue(new KitchenTask("assembly"));
  }
 
  // Staff help, they don't replace you: slower than a player off their specialty, and they tire during long shifts.
  // Their stats (ResidentStats) scale station work, walking and energy drain; a 3 is the baseline.
  public const float WorkerDrain=.75f;
  public static float WorkerSpeed(WorkerState w,string station,bool night=false){
   var stats=StaffStats.For(w.Id);
   if(StaffStats.Exhausted(stats,w.Energy))return .4f;
   var role=RestaurantCatalog.Worker(w.Id)?.Role??StaffJob.Any;
   var needs=station=="sink"?StaffJob.Clean:StaffJob.Cook;
   return (role==needs?1f:.65f)*StaffStats.WorkMultiplier(stats,station,night);
  }
}
}
