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
    if(!employees.TryGetValue(worker.Id,out var view)){var root=RestaurantArt.CreateCharacter(worker.Id=="ember"?8:9,transform);root.transform.position=new Vector3(-9,.055f,-12);view=new EmployeeView{Root=root,Motion=root.GetComponent<CharacterMotion>(),Bubble=WorldCaption(root.transform,"",new Vector3(0,2.4f,0),.023f)};employees[worker.Id]=view;}
    if(!workerPlans.TryGetValue(worker.Id,out var plan)){plan=new Queue<KitchenTask>();workerPlans[worker.Id]=plan;}
    if(view.Job!=worker.Job){view.Job=worker.Job;plan.Clear();view.Path.Clear();k.ReleaseWork(actor);}
    view.Motion.Working=false;view.Motion.Walking=false;
    if(worker.Job==StaffJob.Off||worker.Energy<=2||!ServiceInProgress){SetBubble(view.Bubble,worker.Id+" / resting / "+(int)worker.Energy+" energy");continue;}
    if(plan.Count==0){
     var hand=k.Hold(actor);
     if(hand!=null){if(hand.Kind==KitchenItemKind.DirtyPlate){plan.Enqueue(new KitchenTask("sink"));plan.Enqueue(new KitchenTask("sink","work"));}else{var order=Data.Orders.FirstOrDefault(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(hand));if(order!=null)plan.Enqueue(new KitchenTask("guest","serve",order.Id));else if(hand.Kind==KitchenItemKind.Plate)plan.Enqueue(new KitchenTask("assembly"));else k.Discard(Game.State,actor,out _);}}
     else if(worker.Job==StaffJob.Clean){var dirty=k.Items.FirstOrDefault(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.Holder.StartsWith("table:"));if(dirty!=null){plan.Enqueue(new KitchenTask("table","clear",dirty.TableInstanceId));plan.Enqueue(new KitchenTask("sink"));plan.Enqueue(new KitchenTask("sink","work"));}}
     else if(worker.Job==StaffJob.Serve){var ready=k.Items.FirstOrDefault(i=>i.Holder.StartsWith("station:")&&k.RecipeOf(i)!=""&&Data.Orders.Any(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(i)));if(ready!=null){int station=int.Parse(ready.Holder.Substring(8));var order=Data.Orders.First(o=>o.Stage==RestaurantOrderStage.Waiting&&o.DishId==k.RecipeOf(ready));plan.Enqueue(new KitchenTask("assembly","",station));plan.Enqueue(new KitchenTask("guest","serve",order.Id));}}
     else if(worker.Job==StaffJob.Cook){var order=Data.Orders.FirstOrDefault(o=>o.Stage==RestaurantOrderStage.Waiting&&!employees.Values.Any(v=>v!=view&&v.OrderId==o.Id)&&!k.Items.Any(i=>k.RecipeOf(i)==o.DishId));if(order!=null&&k.Stations.Any(s=>s.CatalogId=="assembly"&&k.At(s.InstanceId)==null)){view.OrderId=order.Id;plan.Enqueue(new KitchenTask("plate_rack"));plan.Enqueue(new KitchenTask("assembly"));AddIngredient(plan,order.DishId=="salad"?"greens":"protein",order.DishId!="salad");if(order.DishId!="salad"){plan.Enqueue(new KitchenTask("pantry","bun"));plan.Enqueue(new KitchenTask("assembly"));}if(order.DishId=="midnight")AddIngredient(plan,"sauce",false);}}
    }
    if(plan.Count==0){SetBubble(view.Bubble,worker.Id+" / ready / "+(int)worker.Energy+" energy");continue;}
    var task=plan.Peek();Vector3 destination;KitchenStation stationData=null;
    if(task.Station=="guest"){if(!guests.TryGetValue(task.Target,out var guest)||!guest.Seat){plan.Clear();continue;}destination=guest.Seat.position;}
    else if(task.Station=="table"){if(!Furnishings.TryGetValue(task.Target,out var table)){plan.Clear();continue;}destination=table.transform.position+Vector3.forward;}
    else{stationData=k.Stations.FirstOrDefault(s=>s.CatalogId==task.Station&&(task.Target==0||s.InstanceId==task.Target));if(stationData==null)continue;var furniture=Furnishings[stationData.InstanceId];var wp=furniture.transform.Find("WorkPoint");destination=wp?wp.position:furniture.transform.position+Vector3.forward;}
    destination.y=.055f;worker.Energy=Mathf.Max(0,worker.Energy-dt*.3f);
    SetBubble(view.Bubble,worker.Id+" / "+task.Station.Replace('_',' ')+" / "+(int)worker.Energy+" energy");
    if(Vector3.Distance(view.Root.transform.position,destination)>1.2f){if(view.Path.Count==0)AppendRoute(view.Path,view.Root.transform.position,destination);view.Motion.Walking=Follow(view.Root.transform,view.Path,dt*(worker.Energy<25?1.5f:2.7f));continue;}
    view.Path.Clear();view.Motion.Working=true;bool done=false;
    if(task.Action=="work"){k.Work(Game.State,actor,stationData.InstanceId,dt*(worker.Energy<25?.5f:worker.Id=="ember"&&task.Station=="prep_bench"||worker.Id=="moss"&&task.Station=="sink"?1.35f:1),out _);var item=k.At(stationData.InstanceId);done=item==null||!(item.Kind==KitchenItemKind.RawProtein||item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.RawSauce||item.Kind==KitchenItemKind.DirtyPlate);}
    else if(task.Action=="wait"){var item=k.At(stationData.InstanceId);done=item!=null&&(item.Kind==KitchenItemKind.CookedPatty||item.Kind==KitchenItemKind.BurntPatty);}
    else if(task.Action=="serve"){done=k.Serve(Game.State,actor,task.Target,out _);if(!Data.Orders.Any(o=>o.Id==task.Target&&o.Stage==RestaurantOrderStage.Waiting))done=true;}
    else if(task.Action=="clear")done=k.ClearTable(Game.State,actor,task.Target,out _);
    else done=k.Act(Game.State,actor,stationData.InstanceId,task.Action,out _);
    if(done){plan.Dequeue();worker.TasksCompleted++;if(plan.Count==0)view.OrderId=-1;}
   }
  }
  static void AddIngredient(Queue<KitchenTask> plan,string ingredient,bool grill){plan.Enqueue(new KitchenTask("pantry",ingredient));plan.Enqueue(new KitchenTask("prep_bench"));plan.Enqueue(new KitchenTask("prep_bench","work"));plan.Enqueue(new KitchenTask("prep_bench"));if(grill){plan.Enqueue(new KitchenTask("grill"));plan.Enqueue(new KitchenTask("grill","wait"));plan.Enqueue(new KitchenTask("grill"));}plan.Enqueue(new KitchenTask("assembly"));}
 }
}

