using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
 public partial class RestaurantController {
  public bool ServiceInProgress=>Data.Open||Data.Orders.Count>0;
  public bool ManagementPauses=>PanelOpen&&!ServiceInProgress;
  readonly Dictionary<string,string> prompts=new Dictionary<string,string>();
  readonly Dictionary<string,int> choices=new Dictionary<string,int>();
  readonly Dictionary<int,GameObject> physicalItems=new Dictionary<int,GameObject>();
  readonly Dictionary<int,string> itemLooks=new Dictionary<int,string>();
  float shiftTime;
  static readonly string[] pantryChoices={"protein","greens","bun","sauce"};
  public string PromptFor(string actor)=>prompts.TryGetValue(actor,out var p)?p:"";
  public string GuidanceFor(string actor){
   var hand=Game.State.Kitchen.Hold(actor);
   if(hand==null)return "Empty hands: pantry E takes protein, greens, bun or sauce; Q changes the pantry choice.";
   switch(hand.Kind){
    case KitchenItemKind.RawProtein:return "Raw protein: E at prep bench, then hold E to chop. Q discards it.";
    case KitchenItemKind.PreparedPatty:return "Prepared patty: E at grill; wait 8 seconds, then E to collect. Q discards it.";
    case KitchenItemKind.CookedPatty:return "Cooked patty: E at assembly to add it to a plate. Q discards it.";
    case KitchenItemKind.RawGreens:case KitchenItemKind.RawSauce:return kGuidance(hand.Kind);
    case KitchenItemKind.ChoppedGreens:case KitchenItemKind.MidnightSauce:case KitchenItemKind.Bun:return "Ingredient: E at assembly to add it to a plate. Q discards it.";
    case KitchenItemKind.DirtyPlate:return "Dirty plate: E at sink, then hold E to wash.";
    case KitchenItemKind.Plate:
     if(hand.Parts==0)return "Clean plate: E at assembly to set it down. Q returns it to the rack.";
     if(Game.State.Kitchen.RecipeOf(hand)!="")return "Finished dish: E at the matching guest to serve. Q discards food; wash the plate.";
     return "Partly assembled dish: E at assembly to set it down; add the missing ingredient.";
   }
   return "Q discards held food.";
  }
  static string kGuidance(KitchenItemKind kind)=>kind==KitchenItemKind.RawGreens?"Greens: E at prep bench, then hold E to chop.":"Sauce ingredients: E at prep bench, then hold E to prepare.";
  GameObject CreateFurnishing(string id,Transform parent)=>new[]{"pantry","plate_rack","sink","assembly"}.Contains(id)?KitchenArt.CreateStation(id,parent):RestaurantArt.CreateFurniture(id,parent);
  void PhysicalSetup(){Game.State.Kitchen.EnsureStations(Data);RebuildLayout();KitchenArt.DecorateStreet(transform);}
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
    if(Game.State.Kitchen.Hold(p.ActorId)!=null){Game.State.Kitchen.Discard(Game.State,p.ActorId,out var m);Feedback(m);}
    else{choices.TryGetValue(p.ActorId,out int c);choices[p.ActorId]=(c+1)%4;Feedback("Pantry choice: "+pantryChoices[choices[p.ActorId]]+". E at pantry to take it.");}
   }
   return false;
  }
  public bool InspectPlayerRay(FirstPersonPlayer p,RaycastHit hit,bool pressed,bool held){
   var city=hit.collider.GetComponentInParent<Interactable>();
   if(city&&city.Kind==InteractionKind.Supplier&&Data.Owned){choices.TryGetValue(p.ActorId,out int choice);bool protein=choice%2==0;prompts[p.ActorId]="Milo's market / E or A: buy 6 "+(protein?"protein / $10":"produce / $6")+"\nQ / B switches supplies. Your partner keeps working.";if(pressed){if(!Data.Restock(Game.State,protein,out var m)&&!protein&&Data.Produce==0&&Game.State.Cash<6)Data.RequestSupplyHelp(Game.State,out m);Feedback(m);Game.Save();}return true;}
   var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
   string actor=p.ActorId,message="";var k=Game.State.Kitchen;
   if(!Data.Owned){prompts[actor]="Buy this restaurant at the front sign / $150";return true;}
   if(target.Kind=="Management"){prompts[actor]=Data.Open?"E / A: stop new arrivals":k.Hold(actor)!=null?"Carrying "+k.Label(k.Hold(actor))+". Tab for management after placing it.":"E / A: manage restaurant";if(pressed){if(Data.Open)ToggleService();else if(k.Hold(actor)==null)ShowPanel("Service");}return true;}
   if(target.Kind=="Customer"){var o=Data.Orders.Find(x=>x.Id==target.OrderId);prompts[actor]=o==null?"Guest leaving":"E / A: serve #"+o.Id+" "+RestaurantCatalog.Dish(o.DishId).Name;if(pressed){k.Serve(Game.State,actor,target.OrderId,out message);Feedback(message);}return true;}
   var station=k.Stations.Find(s=>s.InstanceId==target.InstanceId);
   if(station!=null){choices.TryGetValue(actor,out int c);var food=k.At(station.InstanceId);
    string action=station.CatalogId=="pantry"?"E / A: take "+pantryChoices[c]+" | Q / B: change choice":station.CatalogId=="plate_rack"?"E / A: take clean plate":food!=null&&station.CatalogId=="prep_bench"?"Hold E / A to prepare; tap to collect when ready":food!=null&&station.CatalogId=="sink"?"Hold E / A to wash":food!=null&&station.CatalogId=="grill"?"Cooking; tap E / A after 8 seconds":k.Hold(actor)!=null?"E / A: place or add held item":"E / A: take or place an item";
    prompts[actor]=station.CatalogId.Replace('_',' ')+" | "+action+(food==null?"":"\n"+k.Label(food)+"  "+station.Progress.ToString("0.0")+"s");
    if(pressed){k.Act(Game.State,actor,station.InstanceId,pantryChoices[c],out message);Feedback(message);}
    if(held&&k.Hold(actor)==null)k.Work(Game.State,actor,station.InstanceId,Time.deltaTime,out _);
   }else{bool dirty=k.DirtyAtTable(target.InstanceId)>0;prompts[actor]=dirty?"E / A: clear dirty plate | B / D-pad up: edit furniture":"B / D-pad up: edit furniture";if(pressed&&dirty){k.ClearTable(Game.State,actor,target.InstanceId,out message);Feedback(message);}}
   return true;
  }
  void TickPhysicalService(float dt){var k=Game.State.Kitchen;if(k.ShiftActive){shiftTime+=dt;if(Data.Open&&shiftTime>=120)Data.EndService(out _);if(!Data.Open&&Data.Orders.Count==0){k.FinishShift(Game.State);shiftTime=0;ShowPanel("Service");Game.Save();}}DrawKitchenItems();}
  void DrawKitchenItems(){
   var k=Game.State.Kitchen;
   foreach(var id in physicalItems.Keys.ToArray())if(!k.Items.Any(i=>i.Id==id)){Destroy(physicalItems[id]);physicalItems.Remove(id);itemLooks.Remove(id);}
   foreach(var item in k.Items){string look=item.Kind+":"+item.Parts;
    if(!physicalItems.TryGetValue(item.Id,out var obj)||itemLooks[item.Id]!=look){if(obj)Destroy(obj);obj=KitchenArt.CreateItem(item.Kind.ToString(),item.Parts,transform);physicalItems[item.Id]=obj;itemLooks[item.Id]=look;}
    Transform parent=transform;Vector3 pos=Vector3.zero;bool found=false;
    if(item.Holder.StartsWith("player:")){var p=Game.CoOp?.Players.FirstOrDefault(v=>v.ActorId==item.Holder);if(p){parent=p.Elevated?p.transform:p.View.transform;pos=p.Elevated?new Vector3(.3f,1,.6f):new Vector3(.32f,-.32f,.7f);found=true;}}
    else if(item.Holder.StartsWith("staff:")){if(employees.TryGetValue(item.Holder.Substring(6),out var w)){parent=w.Root.transform;pos=new Vector3(.25f,1,.45f);found=true;}}
    else{int id;if(item.Holder.StartsWith("station:")&&int.TryParse(item.Holder.Substring(8),out id)&&Furnishings.TryGetValue(id,out var s)){parent=s.transform;pos=new Vector3(0,1.08f,0);found=true;}else if(item.TableInstanceId>0&&Furnishings.TryGetValue(item.TableInstanceId,out var t)){parent=t.transform;pos=new Vector3((item.Id%2-.5f)*.4f,.94f,0);found=true;}}
    obj.SetActive(found);obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;
   }
  }
 }
}

