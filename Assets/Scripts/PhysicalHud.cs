using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace RestaurantCity {
 public class PhysicalHud:MonoBehaviour {
  public CityGame Game;
  sealed class View {public Canvas Canvas;public Text Top,Tickets,Bottom;}
  readonly List<View> views=new List<View>();
  Text Card(Transform parent,string name,Vector2 min,Vector2 max,int size){
   var box=new GameObject(name,typeof(RectTransform),typeof(Image));box.transform.SetParent(parent,false);var rt=(RectTransform)box.transform;rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(12,8);rt.offsetMax=new Vector2(-12,-8);box.GetComponent<Image>().color=new Color(.035f,.095f,.12f,.92f);box.GetComponent<Image>().raycastTarget=false;
   var label=new GameObject("Text",typeof(RectTransform),typeof(Text));label.transform.SetParent(box.transform,false);var tr=(RectTransform)label.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,7);tr.offsetMax=new Vector2(-12,-7);var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=new Color(1,.95f,.82f);text.raycastTarget=false;text.supportRichText=true;return text;
  }
  void LateUpdate(){
   if(!Game||!Game.CoOp)return;
   if(views.Count!=Game.CoOp.PlayerCount){foreach(var v in views)Destroy(v.Canvas.gameObject);views.Clear();foreach(var p in Game.CoOp.Players){var go=new GameObject("Player "+p.PlayerId+" HUD",typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=p.View;c.planeDistance=.4f;c.sortingOrder=60;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(Game.CoOp.PlayerCount>1?720:1440,900);scaler.matchWidthOrHeight=1;
    var v=new View{Canvas=c,Top=Card(go.transform,"Status",new Vector2(0,.87f),Vector2.one,18),Tickets=Card(go.transform,"Orders",new Vector2(0,.62f),new Vector2(.64f,.87f),16),Bottom=Card(go.transform,"Interaction",Vector2.zero,new Vector2(1,.28f),17)};views.Add(v);var dot=new GameObject("Aim",typeof(RectTransform),typeof(Text),typeof(Outline));dot.transform.SetParent(go.transform,false);var dr=(RectTransform)dot.transform;dr.anchorMin=dr.anchorMax=new Vector2(.5f,.5f);dr.sizeDelta=new Vector2(48,48);dr.anchoredPosition=Vector2.zero;var dt=dot.GetComponent<Text>();dt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");dt.fontSize=24;dt.text="+";dt.alignment=TextAnchor.MiddleCenter;dt.color=new Color(1,.95f,.82f);dt.raycastTarget=false;dot.GetComponent<Outline>().effectColor=new Color(.03f,.1f,.13f,.9f);foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=25+p.PlayerId;
   }}
   var r=Game.State.Restaurant;var k=Game.State.Kitchen;
   for(int i=0;i<views.Count;i++){var v=views[i];var p=Game.CoOp.Players[i];v.Canvas.enabled=Game.Started&&!Game.Paused&&!Game.Restaurant.PanelOpen&&!Game.Restaurant.PlacementActive;if(!v.Canvas.enabled)continue;
    v.Top.text="PLAYER "+(i+1)+"   $"+Game.State.Cash+"   "+r.Stars+" star / "+r.Satisfaction.ToString("0")+"%\n"+(r.Open?"SERVICE OPEN":r.Orders.Count>0?"FINISH REMAINING GUESTS":"CLOSED / PLAN & EXPLORE")+"   Clean plates "+k.CleanPlates+"\nPantry "+r.Protein+" protein / "+r.Produce+" produce"+(Game.State.FluxIntroduced?"   Flux "+Game.State.Flux:"");
    v.Tickets.text=TicketRail(r);
    if(!r.Owned)v.Tickets.text="Earn $150 at your stand, then buy Little Flame.\nStand: prep > grill 4 seconds > collect > serve.\nStock "+Game.State.Stock+" | Food "+Game.State.Food+" | Health "+(i==0?Game.State.Health:p.Health);
    v.Tickets.transform.parent.gameObject.SetActive(v.Tickets.text!="");string prompt=Game.Restaurant.PromptFor(p.ActorId);if(prompt==""&&p.Target)prompt="E / A: "+p.Target.Kind;
    string checklist=Game.Restaurant.HeldPlateChecklist(p.ActorId);
    v.Bottom.text=k.Label(k.Hold(p.ActorId))+(checklist==""?"":"  |  "+checklist)+"\n"+prompt+"\n"+Game.Notice+"\n"+(i==0?"E interact / hold to work · Q discard held item · V view\nTab manage · B arrange furniture · Controller Start joins":"A interact / hold to work · B discard held item · Y view\nD-pad up arrange · Left stick move · Right stick look");
   }
  }
  // Ticket rail (A4): one card per waiting order with table, dish, its components, and a colored patience bar.
  static string TicketRail(RestaurantState r){
   var waiting=r.Orders.Where(o=>o.Stage==RestaurantOrderStage.Waiting).ToList();
   if(waiting.Count==0)return "No open tickets.";
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
