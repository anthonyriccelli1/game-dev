using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace RestaurantCity {
 public class PhysicalHud:MonoBehaviour {
  public CityGame Game;
  sealed class View {public Canvas Canvas;public Text Top,Tickets,Prompt,Notice;}
  readonly List<View> views=new List<View>();
  static readonly Color Ink=new Color(.035f,.095f,.12f,.62f), Cream=new Color(1,.95f,.82f);
  // Compact HUD: small status chip top-left, ticket column top-right (hidden when empty),
  // and a background-free prompt just under the crosshair. Controls live in the pause menu.
  static Text Card(Transform parent,string name,Vector2 min,Vector2 max,int size,bool background,TextAnchor align){
   var box=new GameObject(name,typeof(RectTransform));box.transform.SetParent(parent,false);var rt=(RectTransform)box.transform;rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(10,6);rt.offsetMax=new Vector2(-10,-6);
   if(background){var img=box.AddComponent<Image>();img.color=Ink;img.raycastTarget=false;}
   var label=new GameObject("Text",typeof(RectTransform),typeof(Text),typeof(Outline));label.transform.SetParent(box.transform,false);var tr=(RectTransform)label.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(10,6);tr.offsetMax=new Vector2(-10,-6);
   var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=Cream;text.raycastTarget=false;text.supportRichText=true;text.alignment=align;
   label.GetComponent<Outline>().effectColor=new Color(0,0,0,.85f);label.GetComponent<Outline>().effectDistance=new Vector2(1.5f,-1.5f);
   return text;
  }
  void Build(){
   foreach(var v in views)Destroy(v.Canvas.gameObject);views.Clear();
   foreach(var p in Game.CoOp.Players){
    var go=new GameObject("Player "+p.PlayerId+" HUD",typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
    var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=p.View;c.planeDistance=.4f;c.sortingOrder=60;
    var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(Game.CoOp.PlayerCount>1?720:1440,900);scaler.matchWidthOrHeight=1;
    var v=new View{Canvas=c,
     Top=Card(go.transform,"Status",new Vector2(0,.9f),new Vector2(.3f,1),15,true,TextAnchor.MiddleLeft),
     Tickets=Card(go.transform,"Tickets",new Vector2(.7f,.5f),new Vector2(1,1),14,true,TextAnchor.UpperLeft),
     Prompt=Card(go.transform,"Prompt",new Vector2(.25f,.3f),new Vector2(.75f,.46f),19,false,TextAnchor.UpperCenter),
     Notice=Card(go.transform,"Notice",new Vector2(.2f,.02f),new Vector2(.8f,.1f),15,false,TextAnchor.LowerCenter)};
    views.Add(v);
    var dot=new GameObject("Aim",typeof(RectTransform),typeof(Text),typeof(Outline));dot.transform.SetParent(go.transform,false);var dr=(RectTransform)dot.transform;dr.anchorMin=dr.anchorMax=new Vector2(.5f,.5f);dr.sizeDelta=new Vector2(48,48);dr.anchoredPosition=Vector2.zero;
    var dt=dot.GetComponent<Text>();dt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");dt.fontSize=24;dt.text="+";dt.alignment=TextAnchor.MiddleCenter;dt.color=Cream;dt.raycastTarget=false;dot.GetComponent<Outline>().effectColor=new Color(.03f,.1f,.13f,.9f);
    foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=25+p.PlayerId;
   }
  }
  void LateUpdate(){
   if(!Game||!Game.CoOp)return;
   if(views.Count!=Game.CoOp.PlayerCount)Build();
   var r=Game.State.Restaurant;var k=Game.State.Kitchen;
   for(int i=0;i<views.Count;i++){var v=views[i];var p=Game.CoOp.Players[i];v.Canvas.enabled=Game.Started&&!Game.Paused&&!Game.Restaurant.PanelOpen&&!Game.Restaurant.PlacementActive;if(!v.Canvas.enabled)continue;
    string phase=r.Open?"<color=#4FCB7A>OPEN</color>":r.Orders.Count>0?"<color=#E8C34A>LAST GUESTS</color>":"CLOSED";
    v.Top.text="$"+Game.State.Cash+"   "+r.Stars+" star   "+phase+"\n<size=12>Plates "+k.CleanPlates+"/"+KitchenState.PlateCapacity(r)+(k.SinkPile>0?" ("+k.SinkPile+" dirty)":"")+"  |  Protein "+r.Protein+"  |  Produce "+r.Produce+(Game.State.FluxIntroduced?"  |  Flux "+Game.State.Flux:"")+(Game.State.StandBuilt?"  |  P phone":"")+"</size>";
    string tickets=!r.Owned?StandTicket(Game.State):TicketRail(r)+(Game.State.StandWorker!=null?(TicketRail(r)==""?"":"\n")+"<size=12><color=#9FD8C8>Stand: "+Game.State.StandWorkerStatus+" (+$"+Game.State.StandWorkerEarned+")</color></size>":"");
    v.Tickets.transform.parent.gameObject.SetActive(tickets!="");v.Tickets.text=tickets;
    string prompt=Game.Restaurant.PromptFor(p.ActorId);if(prompt==""&&p.Target)prompt="E / A  "+p.Target.Prompt(Game);
    var held=k.Hold(p.ActorId);string checklist=Game.Restaurant.HeldPlateChecklist(p.ActorId);
    string holding=held==null?"":"<size=14><color=#9FD8C8>Holding: "+k.Label(held)+(checklist==""?"":"  |  "+checklist)+"</color></size>\n";
    v.Prompt.text=holding+prompt;
    v.Notice.text=Game.Notice;
   }
  }
  static string StandTicket(GameState s){
   if(!s.StandBuilt)return "Set up your food stand ($10)\nthen buy patties & buns at Milo's.";
   string goal="<size=12>Goal: save $150 for the restaurant across the street</size>";
   if(!s.HasOrder)return (s.StandOpen?"Stand OPEN: a customer is on the way...":"Stand CLOSED: press E on the stand sign to open")+"\n"+goal;
   var lines=new List<string>();
   foreach(var o in s.StandQueue){
    float ratio=Mathf.Clamp01(o.Patience/Mathf.Max(1,o.MaxPatience));string c=ratio>.55f?"#4FCB7A":ratio>.25f?"#E8C34A":"#E1543B";int f=Mathf.Max(1,Mathf.CeilToInt(ratio*8));
    lines.Add((o.Dish=="midnight"?"Midnight burger [bun+patty+sauce]":"Burger [bun+patty]")+"  <color="+c+">"+new string('|',f)+"</color>"+new string('.',8-f)+" "+(int)o.Patience+"s");
   }
   lines.Add("<size=12>Plates: "+s.StandClean+" clean, "+s.StandDirty+" dirty</size>");if(s.StandWorker!=null)lines.Add("<size=12><color=#9FD8C8>"+s.StandWorkerStatus+"  (+$"+s.StandWorkerEarned+")</color></size>");lines.Add(goal);
   return string.Join("\n",lines);
  }
  // Ticket rail (A4): one card per waiting order with table, dish, its components, and a colored patience bar.
  static string TicketRail(RestaurantState r){
   var waiting=r.Orders.Where(o=>o.Stage==RestaurantOrderStage.Waiting).ToList();
   if(waiting.Count==0)return "";
   var lines=new List<string>();
   foreach(var o in waiting.Take(5)){
    var customer=RestaurantCatalog.Customers[o.CustomerType];var dish=RestaurantCatalog.Dish(o.DishId);var recipe=RecipeBook.Find(o.DishId);
    float left=Mathf.Max(0,customer.Patience-o.Wait),ratio=Mathf.Clamp01(left/customer.Patience);
    string barColor=ratio>.55f?"#4FCB7A":ratio>.25f?"#E8C34A":"#E1543B";
    int filled=Mathf.CeilToInt(ratio*8);string bar="<color="+barColor+">"+new string('|',Mathf.Max(1,filled))+"</color>"+new string('.',8-Mathf.Max(1,filled));
    string parts=recipe==null?"":string.Join("+",recipe.Components.Select(c=>c=="cooked_patty"?"patty":c=="chopped_greens"?"greens":c=="midnight_sauce"?"sauce":c));
    lines.Add("#"+o.Id+" Table "+o.SeatInstanceId+" — "+dish.Name+" ["+parts+"]  "+bar+" "+(int)left+"s");
   }
   if(waiting.Count>5)lines.Add("+"+(waiting.Count-5)+" more waiting");
   return string.Join("\n",lines);
  }
 }
}
