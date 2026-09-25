using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
 public partial class RestaurantController {
  public bool ServiceInProgress=>Data.Open||Data.Orders.Count>0;
  public bool ManagementPauses=>PanelOpen&&!ServiceInProgress;
  readonly Dictionary<string,string> prompts=new Dictionary<string,string>();
  // Only used for Milo's city market stall (protein vs produce); the in-kitchen pantry Q cycle is gone,
  // replaced by aiming at a physical ingredient shelf (KitchenArt.PantryShelf / RestaurantTarget.SubId).
  readonly Dictionary<string,int> supplierChoice=new Dictionary<string,int>();
  readonly Dictionary<int,GameObject> physicalItems=new Dictionary<int,GameObject>();
  readonly Dictionary<int,string> itemLooks=new Dictionary<int,string>();
  float shiftTime;
  public string PromptFor(string actor)=>prompts.TryGetValue(actor,out var p)?p:"";
  // Held-plate checklist for the HUD (A4): what recipe this plate could become and which components remain.
  public string HeldPlateChecklist(string actor){
   var hand=Game.State.Kitchen.Hold(actor);
   if(hand==null||hand.Kind!=KitchenItemKind.Plate)return "";
   string dish=Game.State.Kitchen.RecipeOf(hand);
   if(dish!="")return "Ready: "+RestaurantCatalog.Dish(dish).Name;
   if(hand.Components.Count==0)return "Empty plate. Add ingredients at the assembly counter.";
   var candidates=RecipeBook.Recipes.Where(r=>hand.Components.All(c=>r.Components.Contains(c))).ToList();
   if(candidates.Count==0)return "Mismatched ingredients. Discard and start again.";
   var target=candidates.OrderBy(r=>r.Components.Count).First();
   var parts=target.Components.Select(c=>(hand.Components.Contains(c)?"[x] ":"[ ] ")+ComponentDisplay(c));
   return "Building "+RestaurantCatalog.Dish(target.DishId).Name+": "+string.Join("  ",parts);
  }
  static string ComponentDisplay(string id)=>id=="bun"?"bun":id=="cooked_patty"?"cooked patty":id=="chopped_greens"?"chopped greens":id=="midnight_sauce"?"midnight sauce":id;
  GameObject CreateFurnishing(string id,Transform parent)=>new[]{"pantry","plate_rack","sink","assembly"}.Contains(id)?KitchenArt.CreateStation(id,parent):RestaurantArt.CreateFurniture(id,parent);
  GameObject menuBoard;
  void PhysicalSetup(){Game.State.Kitchen.EnsureStations(Data);RebuildLayout();KitchenArt.DecorateStreet(transform);RefreshMenuBoard();}
  // A4: a wall board prop showing the active-menu recipes, alongside the Cookbook management tab.
  public void RefreshMenuBoard(){
   if(menuBoard)Destroy(menuBoard);
   menuBoard=new GameObject("Kitchen menu board");menuBoard.transform.SetParent(transform,false);menuBoard.transform.position=new Vector3(-5.4f,2.35f,-10.9f);
   var lines=Data.ActiveMenu.Select(id=>{var recipe=RecipeBook.Find(id);var dish=RestaurantCatalog.Dish(id);return recipe==null||dish==null?"":dish.Name+": "+string.Join("+",recipe.Components.Select(ComponentDisplay));});
   WorldCaption(menuBoard.transform,"TONIGHT'S MENU\n"+string.Join("\n",lines),Vector3.zero,.02f);
  }
  public void ClearPlayerFocus(FirstPersonPlayer p){prompts[p.ActorId]="";if(!p.InteractHeld)Game.State.Kitchen.ReleaseWork(p.ActorId);}
  public bool HandlePlayerInput(FirstPersonPlayer p,bool pressed,bool held,bool secondary,bool menu,bool build){
   if(PanelOpen||PlacementActive)return true;
   if(menu&&!ServiceInProgress){ShowPanel("Service");return true;}
   if(build&&!ServiceInProgress){
    int id=-1;
    if(p.TryResolveInteractionHit(out var hit)){var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(target&&target.Kind=="Furniture")id=target.InstanceId;}
    SelectedInstanceId=id;ShowPanel(id>0||Inside?"Furniture":"Catalog");return true;
   }
   if(secondary){
    // Q / B is drop-or-discard only in the kitchen (A1). At Milo's city market, with empty hands, it still
    // toggles which supply you are about to buy there.
    if(Game.State.Kitchen.Hold(p.ActorId)!=null){Game.State.Kitchen.Discard(Game.State,p.ActorId,out var m);Feedback(m);}
    else{supplierChoice.TryGetValue(p.ActorId,out int c);supplierChoice[p.ActorId]=(c+1)%2;Feedback("Milo's market choice: "+(supplierChoice[p.ActorId]==0?"protein":"produce")+".");}
   }
   return false;
  }
  public bool InspectPlayerRay(FirstPersonPlayer p,RaycastHit hit,bool pressed,bool held){
   var city=hit.collider.GetComponentInParent<Interactable>();
   if(city&&city.Kind==InteractionKind.Supplier&&Data.Owned){supplierChoice.TryGetValue(p.ActorId,out int choice);bool protein=choice%2==0;prompts[p.ActorId]="Milo's market / E or A: buy 6 "+(protein?"protein / $10":"produce / $6")+"\nQ / B switches supplies. Your partner keeps working.";if(pressed){if(!Data.Restock(Game.State,protein,out var m)&&!protein&&Data.Produce==0&&Game.State.Cash<6)Data.RequestSupplyHelp(Game.State,out m);Feedback(m);Game.Save();}return true;}
   var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
   string actor=p.ActorId,message="";var k=Game.State.Kitchen;
   if(!Data.Owned){prompts[actor]="Buy this restaurant at the front sign / $150";return true;}
   if(target.Kind=="Management"){prompts[actor]=Data.Open?"E / A: stop new arrivals":k.Hold(actor)!=null?"Carrying "+k.Label(k.Hold(actor))+". Tab for management after placing it.":"E / A: manage restaurant";if(pressed){if(Data.Open)ToggleService();else if(k.Hold(actor)==null)ShowPanel("Service");}return true;}
   if(target.Kind=="Customer"){var o=Data.Orders.Find(x=>x.Id==target.OrderId);prompts[actor]=o==null?"Guest leaving":"E / A: serve #"+o.Id+" "+RestaurantCatalog.Dish(o.DishId).Name;if(pressed){k.Serve(Game.State,actor,target.OrderId,out message);Feedback(message);}return true;}
   var station=k.Stations.Find(s=>s.InstanceId==target.InstanceId);
   if(station!=null){
    // A1: the prompt comes only from Preview, and Act is implemented as "call Preview, then run what it
    // returned" — so what is shown here is always exactly what a press of E will do.
    var preview=k.Preview(Game.State,actor,station.InstanceId,target.SubId);
    string glyph=preview.Kind==KitchenActionKind.Hold?"Hold E / A  ":"E / A  ";
    string line=preview.Kind==KitchenActionKind.None?preview.FailReason:glyph+(preview.Allowed?preview.Label:preview.FailReason);
    prompts[actor]=station.CatalogId.Replace('_',' ')+(string.IsNullOrEmpty(line)?"":"\n"+line);
    if(pressed&&preview.Kind==KitchenActionKind.Tap){k.Act(Game.State,actor,station.InstanceId,target.SubId,out message);Feedback(message);}
    if(held&&k.Hold(actor)==null)k.Work(Game.State,actor,station.InstanceId,Time.deltaTime,out _);
   }else{bool dirty=k.DirtyAtTable(target.InstanceId)>0;prompts[actor]=dirty?"E / A: clear dirty plate | B / D-pad up: edit furniture":"B / D-pad up: edit furniture";if(pressed&&dirty){k.ClearTable(Game.State,actor,target.InstanceId,out message);Feedback(message);}}
   return true;
  }
  void TickPhysicalService(float dt){var k=Game.State.Kitchen;if(k.ShiftActive){shiftTime+=dt;if(Data.Open&&shiftTime>=120)Data.EndService(out _);if(!Data.Open&&Data.Orders.Count==0){k.FinishShift(Game.State);shiftTime=0;ShowPanel("Service");Game.Save();}}DrawKitchenItems();}
  void DrawKitchenItems(){
   var k=Game.State.Kitchen;
   foreach(var id in physicalItems.Keys.ToArray())if(!k.Items.Any(i=>i.Id==id)){Destroy(physicalItems[id]);physicalItems.Remove(id);itemLooks.Remove(id);}
   foreach(var item in k.Items){string look=item.Kind+":"+string.Join(",",item.Components.OrderBy(c=>c));
    if(!physicalItems.TryGetValue(item.Id,out var obj)||itemLooks[item.Id]!=look){if(obj)Destroy(obj);obj=KitchenArt.CreateItem(item.Kind.ToString(),item.Components,transform);physicalItems[item.Id]=obj;itemLooks[item.Id]=look;}
    Transform parent=transform;Vector3 pos=Vector3.zero;bool found=false;
    if(item.Holder.StartsWith("player:")){var p=Game.CoOp?.Players.FirstOrDefault(v=>v.ActorId==item.Holder);if(p){parent=p.Elevated?p.transform:p.View.transform;pos=p.Elevated?new Vector3(.3f,1,.6f):new Vector3(.32f,-.32f,.7f);found=true;}}
    else if(item.Holder.StartsWith("staff:")){if(employees.TryGetValue(item.Holder.Substring(6),out var w)){parent=w.Root.transform;pos=new Vector3(.25f,1,.45f);found=true;}}
    else{int id;if(item.Holder.StartsWith("station:")&&int.TryParse(item.Holder.Substring(8),out id)&&Furnishings.TryGetValue(id,out var s)){parent=s.transform;pos=new Vector3(0,1.08f,0);found=true;}else if(item.TableInstanceId>0&&Furnishings.TryGetValue(item.TableInstanceId,out var t)){parent=t.transform;pos=new Vector3((item.Id%2-.5f)*.4f,.94f,0);found=true;}}
    obj.SetActive(found);obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;
   }
  }
 }
}
