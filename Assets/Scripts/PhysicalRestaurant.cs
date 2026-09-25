using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
 public partial class RestaurantController {
  public bool ServiceInProgress=>Data.Open||Data.Orders.Count>0;
  public bool ManagementPauses=>PanelOpen&&!ServiceInProgress;
  readonly Dictionary<string,string> prompts=new Dictionary<string,string>();
  readonly Dictionary<string,int> focused=new Dictionary<string,int>();
  readonly Dictionary<string,int> choices=new Dictionary<string,int>();
  readonly Dictionary<int,GameObject> physicalItems=new Dictionary<int,GameObject>();
  readonly Dictionary<int,string> itemLooks=new Dictionary<int,string>();
  float shiftTime;
  static readonly string[] pantryChoices={"protein","greens","bun","sauce"};
  public string PromptFor(string actor)=>prompts.TryGetValue(actor,out var p)?p:"";
  GameObject CreateFurnishing(string id,Transform parent)=>new[]{"pantry","plate_rack","sink","assembly"}.Contains(id)?KitchenArt.CreateStation(id,parent):RestaurantArt.CreateFurniture(id,parent);
  void PhysicalSetup(){Game.State.Kitchen.EnsureStations(Data);RebuildLayout();KitchenArt.DecorateStreet(transform);}
  public void ClearPlayerFocus(FirstPersonPlayer p){prompts[p.ActorId]="";focused[p.ActorId]=-1;if(!p.InteractHeld)Game.State.Kitchen.ReleaseWork(p.ActorId);}
  public bool HandlePlayerInput(FirstPersonPlayer p,bool pressed,bool held,bool secondary,bool menu,bool build){
   if(PanelOpen||PlacementActive)return true;
   if(menu&&!ServiceInProgress){ShowPanel("Service");return true;}
   if(build&&!ServiceInProgress){focused.TryGetValue(p.ActorId,out int id);SelectedInstanceId=id;ShowPanel(id>0?"Furniture":"Catalog");return true;}
   if(secondary){choices.TryGetValue(p.ActorId,out int c);choices[p.ActorId]=(c+1)%4;if(Game.State.Kitchen.Hold(p.ActorId)!=null){Game.State.Kitchen.Discard(Game.State,p.ActorId,out var m);Feedback(m);}}
   return false;
  }
  public bool InspectPlayerRay(FirstPersonPlayer p,RaycastHit hit,bool pressed,bool held){
   var city=hit.collider.GetComponentInParent<Interactable>();
   if(city&&city.Kind==InteractionKind.Supplier&&Data.Owned){choices.TryGetValue(p.ActorId,out int choice);bool protein=choice%2==0;prompts[p.ActorId]="Milo's market / E or A: buy 6 "+(protein?"protein / $10":"produce / $6")+"\nQ / B switches supplies. Your partner keeps working.";if(pressed){if(!Data.Restock(Game.State,protein,out var m)&&!protein&&Data.Produce==0&&Game.State.Cash<6)Data.RequestSupplyHelp(Game.State,out m);Feedback(m);Game.Save();}return true;}
   var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
   string actor=p.ActorId,message="";var k=Game.State.Kitchen;
   if(!Data.Owned){prompts[actor]="Buy this restaurant at the front sign / $150";return true;}
   if(target.Kind=="Management"){prompts[actor]=Data.Open?"E / A: stop new arrivals":"E / A: manage restaurant";if(pressed){if(Data.Open)ToggleService();else ShowPanel("Service");}return true;}
   if(target.Kind=="Customer"){var o=Data.Orders.Find(x=>x.Id==target.OrderId);prompts[actor]=o==null?"Guest leaving":"E / A: serve #"+o.Id+" "+RestaurantCatalog.Dish(o.DishId).Name;if(pressed){k.Serve(Game.State,actor,target.OrderId,out message);Feedback(message);}return true;}
   focused[actor]=target.InstanceId;var station=k.Stations.Find(s=>s.InstanceId==target.InstanceId);
   if(station!=null){choices.TryGetValue(actor,out int c);var food=k.At(station.InstanceId);
    prompts[actor]=station.CatalogId.Replace('_',' ')+" | E / A: take / place"+(station.CatalogId=="pantry"?"\nTaking "+pantryChoices[c]+" | Q / B: change ingredient":"\nHold E / A to prepare or wash")+(food==null?"":"\n"+k.Label(food)+"  "+station.Progress.ToString("0.0")+"s");
    if(pressed){k.Act(Game.State,actor,station.InstanceId,pantryChoices[c],out message);Feedback(message);}
    if(held&&k.Hold(actor)==null)k.Work(Game.State,actor,station.InstanceId,Time.deltaTime,out _);
   }else{prompts[actor]="E / A: clear dirty plate / inspect furniture";if(pressed){if(k.DirtyAtTable(target.InstanceId)>0){k.ClearTable(Game.State,actor,target.InstanceId,out message);Feedback(message);}else if(!ServiceInProgress){SelectedInstanceId=target.InstanceId;ShowPanel("Furniture");}}}
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

