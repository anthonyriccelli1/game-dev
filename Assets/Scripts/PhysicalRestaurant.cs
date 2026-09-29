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
  GameObject CreateFurnishing(string id,Transform parent)=>ArtOverrides.Apply(ArchitectureArt.IsPiece(id)?ArchitectureArt.Create(id,parent):id=="counter"||id=="trash"?KitchenArt.CreateStation(id,parent):new[]{"pantry","plate_rack","sink","assembly"}.Contains(id)?KitchenArt.CreateStation(id,parent):RestaurantArt.CreateFurniture(id,parent),"Furniture",id);
  GameObject menuBoard;
  void PhysicalSetup(){Game.State.Kitchen.EnsureStations(Data);RebuildLayout();BuildStreetKitchen();BuildRivalInterior();Atmosphere.Install(Game,transform);KitchenArt.DecorateStreet(transform);RefreshMenuBoard();}
  // A4: a wall board prop showing the active-menu recipes, alongside the Cookbook management tab.
  public void RefreshMenuBoard(){
   if(menuBoard)Destroy(menuBoard);
   menuBoard=new GameObject("Kitchen menu board");menuBoard.transform.SetParent(transform,false);menuBoard.transform.position=W(-7.1f,2.45f,-10.9f);
   var lines=Data.ActiveMenu.Select(id=>{var recipe=RecipeBook.Find(id);var dish=RestaurantCatalog.Dish(id);return recipe==null||dish==null?"":dish.Name+": "+string.Join("+",recipe.Components.Select(ComponentDisplay));});
   WorldCaption(menuBoard.transform,"TONIGHT'S MENU\n"+string.Join("\n",lines),Vector3.zero,.02f);
  }
  public void ClearPlayerFocus(FirstPersonPlayer p){prompts[p.ActorId]="";if(!p.InteractHeld)Game.State.Kitchen.ReleaseWork(p.ActorId);}
  public bool HandlePlayerInput(FirstPersonPlayer p,bool pressed,bool held,bool secondary,bool menu,bool build){
   if(PanelOpen||PlacementActive)return true;
   if(menu){ShowPanel("Service");return true;}
   if(build&&!ServiceInProgress){
    int id=-1;
    if(p.TryResolveInteractionHit(out var hit)){var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(target&&target.Kind=="Furniture")id=target.InstanceId;}
    SelectedInstanceId=id;ShowPanel(id>0||Inside?"Furniture":"Catalog");return true;
   }
   if(secondary){
    // Q / B is drop-or-discard only in the kitchen (A1). At Milo's city market, with empty hands, it still
    // toggles which supply you are about to buy there.
    if(Game.State.Kitchen.Hold(p.ActorId)!=null)Feedback("Use the trash can to throw food away, or set it on a counter.");
    else{supplierChoice.TryGetValue(p.ActorId,out int c);supplierChoice[p.ActorId]=(c+1)%2;Feedback("Milo's market choice: "+(supplierChoice[p.ActorId]==0?"protein":"produce")+".");}
   }
   return false;
  }
  public bool InspectPlayerRay(FirstPersonPlayer p,RaycastHit hit,bool pressed,bool held){
   if(!hit.collider)return false;
   var city=hit.collider.GetComponentInParent<Interactable>();
   if(city&&InspectStreet(p,city,pressed))return true;
   var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
   string actor=p.ActorId,message="";var k=Game.State.Kitchen;
   if(!Data.Owned&&!KitchenState.IsStandStation(target.InstanceId)){prompts[actor]="Buy this restaurant at the front sign / $150";return true;}
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
    prompts[actor]+=CookStatus(station,k.At(station.InstanceId));
    if(station.CatalogId=="grill"&&k.At(station.InstanceId)!=null)prompts[actor]+="\nLeft click / RB: flip patty";
    if(station.CatalogId=="trash")prompts[actor]+="\nContents "+station.WasteCount+" / 6";
    if(pressed&&preview.Kind==KitchenActionKind.Tap){k.Act(Game.State,actor,station.InstanceId,target.SubId,out message);Feedback(message);}
    if(held&&k.Hold(actor)==null)k.Work(Game.State,actor,station.InstanceId,Time.deltaTime,out _);
   }else{
    int seat=TableSeatAt(target.InstanceId,hit.point);
    bool dirty=k.Items.Any(i=>i.Kind==KitchenItemKind.DirtyPlate&&i.TableInstanceId==target.InstanceId&&i.Holder.StartsWith("table:")&&(seat==0||i.SeatNumber==0||i.SeatNumber==seat));
    var order=Data.Orders.FirstOrDefault(o=>o.SeatInstanceId==target.InstanceId&&o.SeatNumber==seat&&seat>0&&o.Stage!=RestaurantOrderStage.Leaving);
    if(dirty){prompts[actor]="E / A: clear dirty plate | B / D-pad up: edit furniture";if(pressed){k.ClearTable(Game.State,actor,target.InstanceId,out message,seat);Feedback(message);}}
    else if(order!=null){prompts[actor]=order.Stage==RestaurantOrderStage.Waiting?"E / A: serve #"+order.Id+" "+RestaurantCatalog.Dish(order.DishId).Name:"Guest eating";if(pressed&&order.Stage==RestaurantOrderStage.Waiting){k.Serve(Game.State,actor,order.Id,out message);Feedback(message);}}
    else prompts[actor]=seat>0?"Empty seat | B / D-pad up: edit furniture":"B / D-pad up: edit furniture";
   }
   return true;
  }
  // Use the actual table hit in its rotated local coordinates; opposite place settings stay independent.
  int TableSeatAt(int instanceId,Vector3 point){
   if(!Furnishings.TryGetValue(instanceId,out var table)||!table)return 0;
   var local=table.transform.InverseTransformPoint(point);local.y=0;
   int nearest=0;float distance=float.PositiveInfinity;
   for(int n=0;;n++){var seat=table.transform.Find("Seat_"+n);if(!seat)break;var delta=seat.localPosition-local;delta.y=0;if(delta.sqrMagnitude<distance){distance=delta.sqrMagnitude;nearest=n+1;}}
   return nearest;
  }
  Vector3 TablePlatePosition(GameObject table,int seatNumber,int itemId){
   var seat=seatNumber>0?table.transform.Find("Seat_"+(seatNumber-1)):null;
   var pos=seat?seat.localPosition:new Vector3((itemId%2-.5f)*.4f,0,0);if(seat)pos.z*=.4f;
   pos.y=SurfaceY(table.transform,pos,.94f)+.005f;return pos;
  }
  // Height of the real surface under a point (pack tables and counters differ from the old code-built ones), so plates
  // sit on the tabletop instead of floating. Skips food, characters, level dressing and tall parts (hoods, backsplashes).
  readonly Dictionary<string,float> surfaceCache=new Dictionary<string,float>();
  float SurfaceY(Transform furn,Vector3 local,float fallback){
   string key=furn.GetHashCode()+":"+Mathf.RoundToInt(local.x*20)+":"+Mathf.RoundToInt(local.z*20);
   if(surfaceCache.TryGetValue(key,out var cached))return cached;
   var world=furn.TransformPoint(new Vector3(local.x,0,local.z));float best=float.MinValue;
   foreach(var r in furn.GetComponentsInChildren<Renderer>()){
    if(!r.enabled)continue;bool skip=false;
    for(var q=r.transform;q&&q!=furn;q=q.parent)if(q.name.StartsWith("Food_")||q.name=="Level dressing"||q.GetComponent<CharacterMotion>()){skip=true;break;}
    if(skip)continue;var b=r.bounds;
    if(world.x<b.min.x||world.x>b.max.x||world.z<b.min.z||world.z>b.max.z)continue;
    if(b.max.y-furn.position.y>1.35f)continue;
    best=Mathf.Max(best,b.max.y);
   }
   float y=best==float.MinValue?fallback:furn.InverseTransformPoint(new Vector3(world.x,best,world.z)).y;
   if(y<.3f)y=fallback;surfaceCache[key]=y;return y;
  }
  // Grill feedback: a small bar that fills while cooking, turns green when done, red when burning.
  static string CookStatus(KitchenStation s,KitchenItem item){
   if(item!=null&&s.CatalogId=="stove"){
    if(item.Kind==KitchenItemKind.ScorchedSoup)return "\n<color=#E1543B>SCORCHED - take the pot and throw it away</color>";
    if(item.Kind==KitchenItemKind.Soup)return "\n<color=#4FCB7A>SOUP'S READY - bring a plate and ladle it</color>";
    if(item.Kind==KitchenItemKind.SoupPot){int f=Mathf.Clamp(Mathf.FloorToInt(s.Progress/KitchenState.SoupSeconds*10),0,10);return "\n"+(item.Stir>=KitchenState.StirWarning?"<color=#E1543B>STIR IT! Scorching in "+Mathf.CeilToInt(KitchenState.ScorchSeconds-item.Stir)+"s</color>":"<color=#E8C34A>Simmering ["+new string('#',f)+new string('-',10-f)+"]</color>");}
   }
   if(item==null||(s.CatalogId!="grill"&&s.CatalogId!="oven"))return "";
   float done=s.CatalogId=="oven"?6:8,burn=24,t=s.Progress;
   if(item.Kind==KitchenItemKind.BurntPatty)return "\n<color=#E1543B>BURNT - take it and press Q to discard</color>";
   if(item.Kind==KitchenItemKind.CookedPatty){int left=Mathf.Max(0,Mathf.CeilToInt(burn-t));return "\n<color="+(left<6?"#E1543B":"#4FCB7A")+">READY"+(left<6?" - burning in "+left+"s!":"")+"</color>";}
   if(item.Kind!=KitchenItemKind.RawProtein)return "";
   int filled=Mathf.Clamp(Mathf.FloorToInt(t/done*10),0,10);
   return "\n<color=#E8C34A>Cooking ["+new string('#',filled)+new string('-',10-filled)+"]</color>";
  }
  void TickPhysicalService(float dt){var k=Game.State.Kitchen;if(k.ShiftActive){shiftTime+=dt;if(Data.Open&&shiftTime>=ShiftLength)Data.EndService(out _);if(!Data.Open&&Data.Orders.Count==0){k.FinishShift(Game.State);shiftTime=0;ShowPanel("Service");Game.Save();}}DrawKitchenItems();}
  void DrawKitchenItems(){
   var k=Game.State.Kitchen;
   foreach(var id in physicalItems.Keys.ToArray())if(!k.Items.Any(i=>i.Id==id)){Destroy(physicalItems[id]);physicalItems.Remove(id);itemLooks.Remove(id);}
   foreach(var item in k.Items){string look=item.Kind+":"+string.Join(",",item.Components.OrderBy(c=>c));
    // A layout rebuild destroys furniture and any item models parented to it; recreate those.
    if(!physicalItems.TryGetValue(item.Id,out var obj)||!obj||itemLooks[item.Id]!=look){if(obj)Destroy(obj);obj=KitchenArt.CreateItem(item.Kind.ToString(),item.Components,transform);physicalItems[item.Id]=obj;itemLooks[item.Id]=look;}
    Transform parent=transform;Vector3 pos=Vector3.zero;bool found=false;
    if(item.Holder.StartsWith("player:")){var p=Game.CoOp?.Players.FirstOrDefault(v=>v.ActorId==item.Holder);if(p){parent=p.Elevated?p.transform:p.View.transform;pos=p.Elevated?new Vector3(.3f,1,.6f):new Vector3(.32f,-.32f,.7f);found=true;}}
    else if(item.Holder.StartsWith("staff:")){if(employees.TryGetValue(item.Holder.Substring(6),out var w)){parent=w.Root.transform;pos=new Vector3(.25f,1,.45f);found=true;}}
    else{int id;if(item.Holder.StartsWith("station:")&&int.TryParse(item.Holder.Substring(8),out id)&&StationObject(id) is GameObject s&&s){parent=s.transform;pos=new Vector3(0,SurfaceY(s.transform,Vector3.zero,1.08f)+.01f,0);found=true;}else if(item.TableInstanceId>0&&Furnishings.TryGetValue(item.TableInstanceId,out var t)){parent=t.transform;pos=TablePlatePosition(t,item.SeatNumber,item.Id);found=true;}}
    // Prep greens are rendered by the board presentation, including retained partial cuts.
    bool boardGreens=item.Holder.StartsWith("station:")&&(item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.ChoppedGreens)&&parent.GetComponent<ChoppingFeedback>();
    if(item.Holder.StartsWith("station:")&&parent.GetComponent<ChoppingFeedback>())pos=ChoppingFeedback.BoardTop(parent);
    obj.SetActive(found&&!boardGreens);obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;
   }
   DrawProgressBars();
   DrawRestaurantPlates();
  }
  readonly List<List<GameObject>> rackStacks=new List<List<GameObject>>();readonly List<GameObject> sinkPileStack=new List<GameObject>();
  // Same plate rules as the stand: a visible stack per rack (4 each) and a visible dirty pile by the first sink.
  void DrawRestaurantPlates(){
   var k=Game.State.Kitchen;int left=k.CleanPlates;int n=0;
   foreach(var st in k.Stations.Where(x=>x.CatalogId=="plate_rack"&&!KitchenState.IsStandStation(x.InstanceId))){
    var rack=StationObject(st.InstanceId);if(!rack)continue;
    if(!rack.name.EndsWith("(stack)")){foreach(Transform part in rack.GetComponentsInChildren<Transform>(true))if(part.name=="Glazed cream plate")part.gameObject.SetActive(false);rack.name+=" (stack)";}
    while(rackStacks.Count<=n)rackStacks.Add(new List<GameObject>());
    int here=Mathf.Min(4,left);left-=here;SyncPlateStack(rackStacks[n],rack,here,"Plate",new Vector3(0,1f,.05f));n++;
   }
   for(int i=n;i<rackStacks.Count;i++)SyncPlateStack(rackStacks[i],(GameObject)null,0,"Plate",Vector3.zero);
   var sink=k.Stations.FirstOrDefault(x=>x.CatalogId=="sink"&&!KitchenState.IsStandStation(x.InstanceId));
   SyncPlateStack(sinkPileStack,sink==null?null:StationObject(sink.InstanceId),k.SinkPile,"DirtyPlate",new Vector3(.75f,1.08f,.2f));
  }
  readonly Dictionary<int,GameObject> progressBars=new Dictionary<int,GameObject>();
  // Cooking-game style bars over stations: yellow fills while cooking/chopping, green = ready,
  // turns red as a cooked patty approaches burning; blue while washing.
  void DrawProgressBars(){
   var k=Game.State.Kitchen;var eye=Game.Player&&Game.Player.View?Game.Player.View.transform.position:Vector3.zero;
   foreach(var s in k.Stations){
    var station=StationObject(s.InstanceId);
    if(!station||!station.activeInHierarchy){if(progressBars.TryGetValue(s.InstanceId,out var stale)&&stale)stale.SetActive(false);continue;}
    var item=k.At(s.InstanceId);float ratio=-1;string color="E8C34A";
    if(item!=null&&(s.CatalogId=="grill"||s.CatalogId=="oven")){
     float done=s.CatalogId=="oven"?6:8,burn=24;
     if(item.Kind==KitchenItemKind.RawProtein)ratio=s.Progress/done;
     else if(item.Kind==KitchenItemKind.CookedPatty){float heat=(s.Progress-done)/(burn-done);ratio=1;color=heat<.5f?"4FCB7A":heat<.8f?"E8973A":"E1543B";}
     else if(item.Kind==KitchenItemKind.BurntPatty){ratio=1;color="3A2A26";}
    }else if(item!=null&&s.CatalogId=="stove"){
     if(item.Kind==KitchenItemKind.SoupPot){ratio=s.Progress/KitchenState.SoupSeconds;color=item.Stir>=KitchenState.StirWarning?"E1543B":"E8C34A";}
     else if(item.Kind==KitchenItemKind.Soup){ratio=1;color="4FCB7A";}else if(item.Kind==KitchenItemKind.ScorchedSoup){ratio=1;color="3A2A26";}
    }else if(item!=null&&s.CatalogId=="prep_bench"&&(item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.RawSauce)){
     float d=(item.Kind==KitchenItemKind.RawSauce?4:3)*(Game.State.FluxResearch?.65f:1);ratio=s.Progress/d;
    }else if(item!=null&&s.CatalogId=="prep_bench"&&(item.Kind==KitchenItemKind.ChoppedGreens||item.Kind==KitchenItemKind.MidnightSauce)){ratio=1;color="4FCB7A";}
    else if(item!=null&&s.CatalogId=="sink"&&item.Kind==KitchenItemKind.DirtyPlate){ratio=s.Progress/KitchenState.WashSeconds;color="4FA3E1";}
    progressBars.TryGetValue(s.InstanceId,out var bar);
    if(ratio<0){if(bar)bar.SetActive(false);continue;}
    if(!bar){bar=KitchenArt.ProgressBar(transform);progressBars[s.InstanceId]=bar;}
    bar.SetActive(true);bar.transform.position=station.transform.position+Vector3.up*2.05f;KitchenArt.SetProgress(bar,ratio,color);
    var look=bar.transform.position-eye;look.y=0;if(look.sqrMagnitude>.001f)bar.transform.rotation=Quaternion.LookRotation(look);
   }
  }
 }
}
