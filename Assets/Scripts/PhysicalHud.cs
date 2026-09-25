using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RestaurantCity {
 public class PhysicalHud:MonoBehaviour {
  public CityGame Game;
  sealed class View {public Canvas Canvas;public Text Top,Tickets,Bottom;}
  readonly List<View> views=new List<View>();
  Text Card(Transform parent,string name,Vector2 min,Vector2 max,int size){
   var box=new GameObject(name,typeof(RectTransform),typeof(Image));box.transform.SetParent(parent,false);var rt=(RectTransform)box.transform;rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(12,8);rt.offsetMax=new Vector2(-12,-8);box.GetComponent<Image>().color=new Color(.035f,.095f,.12f,.92f);box.GetComponent<Image>().raycastTarget=false;
   var label=new GameObject("Text",typeof(RectTransform),typeof(Text));label.transform.SetParent(box.transform,false);var tr=(RectTransform)label.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,7);tr.offsetMax=new Vector2(-12,-7);var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=new Color(1,.95f,.82f);text.raycastTarget=false;return text;
  }
  void LateUpdate(){
   if(!Game||!Game.CoOp)return;
   if(views.Count!=Game.CoOp.PlayerCount){foreach(var v in views)Destroy(v.Canvas.gameObject);views.Clear();foreach(var p in Game.CoOp.Players){var go=new GameObject("Player "+p.PlayerId+" HUD",typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=p.View;c.planeDistance=.4f;c.sortingOrder=60;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(Game.CoOp.PlayerCount>1?720:1440,900);scaler.matchWidthOrHeight=1;
    var v=new View{Canvas=c,Top=Card(go.transform,"Status",new Vector2(0,.87f),Vector2.one,18),Tickets=Card(go.transform,"Orders",new Vector2(0,.69f),new Vector2(.64f,.87f),17),Bottom=Card(go.transform,"Interaction",Vector2.zero,new Vector2(1,.22f),17)};views.Add(v);var dot=new GameObject("Aim",typeof(RectTransform),typeof(Text));dot.transform.SetParent(go.transform,false);var dr=(RectTransform)dot.transform;dr.anchorMin=dr.anchorMax=new Vector2(.5f,.5f);dr.sizeDelta=new Vector2(24,24);dr.anchoredPosition=Vector2.zero;var dt=dot.GetComponent<Text>();dt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");dt.fontSize=22;dt.text="+";dt.alignment=TextAnchor.MiddleCenter;dt.color=new Color(1,.95f,.82f);dt.raycastTarget=false;foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=25+p.PlayerId;
   }}
   var r=Game.State.Restaurant;var k=Game.State.Kitchen;
   for(int i=0;i<views.Count;i++){var v=views[i];var p=Game.CoOp.Players[i];v.Canvas.enabled=Game.Started&&!Game.Paused&&!Game.Restaurant.PanelOpen&&!Game.Restaurant.PlacementActive;if(!v.Canvas.enabled)continue;
    v.Top.text="PLAYER "+(i+1)+"   $"+Game.State.Cash+"   "+r.Stars+" star / "+r.Satisfaction.ToString("0")+"%\n"+(r.Open?"SERVICE OPEN":r.Orders.Count>0?"FINISH REMAINING GUESTS":"CLOSED / PLAN & EXPLORE")+"   Clean plates "+k.CleanPlates+"\nPantry "+r.Protein+" protein / "+r.Produce+" produce"+(Game.State.FluxIntroduced?"   Flux "+Game.State.Flux:"");
    v.Tickets.text=string.Join("\n",r.Orders.Where(o=>o.Stage==RestaurantOrderStage.Waiting).Take(4).Select(o=>"#"+o.Id+" "+RestaurantCatalog.Dish(o.DishId).Name+" / "+Mathf.Max(0,RestaurantCatalog.Customers[o.CustomerType].Patience-o.Wait).ToString("0")+"s"));
    if(!r.Owned)v.Tickets.text="Earn $150 at your stand, then buy Little Flame.\nStand: prep > grill 4 seconds > collect > serve.\nStock "+Game.State.Stock+" | Food "+Game.State.Food+" | Health "+(i==0?Game.State.Health:p.Health);
    v.Tickets.transform.parent.gameObject.SetActive(v.Tickets.text!="");string prompt=Game.Restaurant.PromptFor(p.ActorId);if(prompt==""&&p.Target)prompt="E / A: "+p.Target.Kind;
    v.Bottom.text=k.Label(k.Hold(p.ActorId))+"\n"+prompt+"\n"+Game.Notice+"\n"+(i==0?"E interact / hold to work · Q choose/discard · V view\nTab manage · B edit/catalog · Controller Start joins":"A interact / hold to work · B choose/discard · Y view\nLeft stick move · Right stick look · Start pause");
   }
  }
 }
}

