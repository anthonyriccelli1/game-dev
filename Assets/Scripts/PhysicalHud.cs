using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace RestaurantCity {
 public class PhysicalHud:MonoBehaviour {
  public CityGame Game;
  sealed class View {public Canvas Canvas;public Text Top,Goal,Tickets,Prompt,Notice,HealthText,WeaponText;public Image[] Slots;public Text[] SlotText;public RectTransform HealthFill,ChargeFill;public Image Hurt;}
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
     Goal=Card(go.transform,"Goal",new Vector2(0,.79f),new Vector2(.3f,.9f),13,true,TextAnchor.UpperLeft),
     Tickets=Card(go.transform,"Tickets",new Vector2(.7f,.5f),new Vector2(1,1),14,true,TextAnchor.UpperLeft),
     Prompt=Card(go.transform,"Prompt",new Vector2(.25f,.3f),new Vector2(.75f,.46f),19,false,TextAnchor.UpperCenter),
     Notice=Card(go.transform,"Notice",new Vector2(.2f,.15f),new Vector2(.8f,.23f),15,false,TextAnchor.LowerCenter)};
    BuildHotbar(go.transform,v);
    views.Add(v);
    var dot=new GameObject("Aim",typeof(RectTransform),typeof(Text),typeof(Outline));dot.transform.SetParent(go.transform,false);var dr=(RectTransform)dot.transform;dr.anchorMin=dr.anchorMax=new Vector2(.5f,.5f);dr.sizeDelta=new Vector2(48,48);dr.anchoredPosition=Vector2.zero;
    var dt=dot.GetComponent<Text>();dt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");dt.fontSize=24;dt.text="+";dt.alignment=TextAnchor.MiddleCenter;dt.color=Cream;dt.raycastTarget=false;dot.GetComponent<Outline>().effectColor=new Color(.03f,.1f,.13f,.9f);
    foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=25+p.PlayerId;
   }
  }
  // Schedule I-style hotbar along the bottom: numbered slots, the selected one lit, health above it,
  // a charge meter for heavy hits and a red edge when you get hit.
  static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color){
   var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,0);rt.anchoredPosition=pos;rt.sizeDelta=size;
   var img=go.GetComponent<Image>();img.color=color;img.raycastTarget=false;return rt;
  }
  static Text Label(Transform parent,string name,int size,TextAnchor align){
   var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Outline));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(3,2);rt.offsetMax=new Vector2(-3,-2);
   var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=Cream;t.alignment=align;t.raycastTarget=false;t.supportRichText=true;go.GetComponent<Outline>().effectColor=new Color(0,0,0,.8f);return t;
  }
  void BuildHotbar(Transform canvas,View v){
   int n=PlayerInventory.Size;float w=62,gap=6,total=n*w+(n-1)*gap;
   v.Slots=new Image[n];v.SlotText=new Text[n];
   for(int i=0;i<n;i++){
    var slot=Rect(canvas,"Slot "+(i+1),new Vector2(.5f,0),new Vector2(-total/2+w/2+i*(w+gap),14),new Vector2(w,w),Ink);
    v.Slots[i]=slot.GetComponent<Image>();
    var num=Label(slot,"Number",11,TextAnchor.UpperLeft);num.text=(i+1).ToString();num.color=new Color(1,1,1,.55f);
    v.SlotText[i]=Label(slot,"Item",12,TextAnchor.MiddleCenter);
   }
   var back=Rect(canvas,"Health back",new Vector2(.5f,0),new Vector2(-total/2+130,88),new Vector2(260,12),new Color(0,0,0,.55f));
   v.HealthFill=Rect(back,"Health fill",new Vector2(0,0),Vector2.zero,new Vector2(260,12),new Color(.86f,.24f,.22f));v.HealthFill.pivot=new Vector2(0,0);v.HealthFill.anchoredPosition=Vector2.zero;
   var ht=Rect(canvas,"Health label",new Vector2(.5f,0),new Vector2(-total/2+130,100),new Vector2(260,20),new Color(0,0,0,0));v.HealthText=Label(ht,"Text",13,TextAnchor.LowerLeft);
   var wt=Rect(canvas,"Weapon label",new Vector2(.5f,0),new Vector2(total/2-130,88),new Vector2(260,30),new Color(0,0,0,0));v.WeaponText=Label(wt,"Text",14,TextAnchor.LowerRight);
   var charge=Rect(canvas,"Charge",new Vector2(.5f,.5f),new Vector2(0,-34),new Vector2(0,5),new Color(1f,.7f,.25f,.9f));v.ChargeFill=charge;
   var hurt=new GameObject("Hurt",typeof(RectTransform),typeof(Image));hurt.transform.SetParent(canvas,false);var hr=(RectTransform)hurt.transform;hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
   v.Hurt=hurt.GetComponent<Image>();v.Hurt.color=new Color(.8f,.05f,.05f,0);v.Hurt.raycastTarget=false;hurt.transform.SetAsFirstSibling();
  }
  void UpdateHotbar(View v,FirstPersonPlayer p){
   if(v.Slots==null)return;var inv=Hotbar.For(Game.State,p.PlayerId);var combat=p.GetComponent<PlayerCombat>();
   for(int i=0;i<v.Slots.Length;i++){bool sel=i==inv.Selected;var s=inv.Slots[i];
    v.Slots[i].color=sel?new Color(.95f,.72f,.3f,.9f):Ink;v.Slots[i].rectTransform.sizeDelta=sel?new Vector2(68,68):new Vector2(62,62);
    string label=Hotbar.SlotLabel(Game.State,s);v.SlotText[i].text=label==""&&sel?"<color=#FFFFFF88>FISTS</color>":label;v.SlotText[i].color=sel?new Color(.1f,.08f,.06f):Cream;}
   float hp=Mathf.Clamp(p.Health,0,100);v.HealthFill.sizeDelta=new Vector2(260*hp/100f,12);v.HealthText.text="HEALTH  "+Mathf.CeilToInt(hp);
   var weapon=inv.Weapon;var food=Hotbar.HandFood(Game.State,p.PlayerId);
   v.WeaponText.text=food!=null?"Hands full":combat&&combat.Blocking?"<color=#9FD8C8>BLOCKING</color>":weapon.Name+(p.AssignedGamepad!=null?"  <size=11>RT jab / hold heavy / LT block</size>":"  <size=11>click jab / hold heavy / right-click block</size>");
   float c=combat?combat.Charge01:0;v.ChargeFill.sizeDelta=new Vector2(c*120,5);v.ChargeFill.GetComponent<Image>().color=c>=1?new Color(1f,.35f,.2f,.95f):new Color(1f,.75f,.3f,.85f);
   v.Hurt.color=new Color(.8f,.05f,.05f,combat?combat.HurtFlash*.35f:0);
  }
  void LateUpdate(){
   if(!Game||!Game.CoOp)return;
   if(views.Count!=Game.CoOp.PlayerCount)Build();
   var r=Game.State.Restaurant;var k=Game.State.Kitchen;
   for(int i=0;i<views.Count;i++){var v=views[i];var p=Game.CoOp.Players[i];v.Canvas.enabled=Game.Started&&!Game.Paused&&!Game.Restaurant.PanelOpen&&!Game.Restaurant.PlacementActive;if(!v.Canvas.enabled)continue;
    string phase=!r.Owned?(Game.State.StandOpen?(Game.State.StandRush?"<color=#E1543B>RUSH!</color>":"<color=#4FCB7A>"+(Game.State.StandNightShift?"NIGHT SHIFT":"TRUCK OPEN")+"</color>"):Game.State.StandLastCall?"<color=#E8C34A>LAST CALL</color>":"TRUCK CLOSED"):r.Open?(Game.Restaurant&&Game.Restaurant.Rush?"<color=#E1543B>RUSH!</color>":"<color=#4FCB7A>OPEN</color>"):r.Orders.Count>0?"<color=#E8C34A>LAST GUESTS</color>":"CLOSED";
    int xp=Game.State.Xp,rk=Game.State.RankEarned;v.Top.text="$"+Game.State.Cash+"   <color=#F2C27A>"+StarText.Of(r.Stars)+"</color>   "+phase+"   <color=#F2C27A>"+Reputation.Titles[rk]+(Reputation.IsMax(rk)?"":"  "+xp+"/"+Reputation.Thresholds[rk+1]+" rep")+"</color>\n<size=12>Plates "+k.CleanPlates+"/"+KitchenState.PlateCapacity(r)+(k.SinkPile>0?" ("+k.SinkPile+" dirty)":"")+"  |  Patties "+r.Stock("patty")+"  |  Buns "+r.Stock("bun")+"  |  Greens "+r.Stock("greens")+(r.Stock("midnight_sauce")>0||Game.State.Knows("midnight")?"  |  Sauce "+r.Stock("midnight_sauce"):"")+(Game.State.FluxIntroduced?"  |  Flux "+Game.State.Flux:"")+(Game.State.StandBuilt?(Game.Player.AssignedGamepad != null?"  |  View phone":"  |  P phone  M map"):"")+"</size>";
    // What to do next, always visible under the status chip (the truck-to-restaurant goal ladder).
    string obj=Game.Objective;int cut=obj.IndexOf('\n');v.Goal.text=obj==""?"":"<color=#F2C27A><b>GOAL: "+(cut<0?obj:obj.Substring(0,cut)).ToUpper()+"</b></color>"+(cut<0?"":"\n"+obj.Substring(cut+1));v.Goal.transform.parent.gameObject.SetActive(obj!="");
    string tickets=!r.Owned?StandTicket(Game.State):TicketRail(r)+(Game.State.StandWorker!=null?(TicketRail(r)==""?"":"\n")+"<size=12><color=#9FD8C8>Stand: "+Game.State.StandWorkerStatus+" (+$"+Game.State.StandWorkerEarned+")</color></size>":"");
    v.Tickets.transform.parent.gameObject.SetActive(tickets!="");v.Tickets.text=tickets;
    // Size the ticket card to its text instead of a fixed half-screen box.
    int lineCount=tickets==""?0:tickets.Split('\n').Length;var card=(RectTransform)v.Tickets.transform.parent;card.anchorMin=new Vector2(.7f,Mathf.Max(.45f,1-(.035f*lineCount+.03f)));
    var promptRect=(RectTransform)v.Prompt.transform.parent;var ps=p.GetComponent<PlateScrub>();var pc=p.GetComponent<PrepChop>();bool scrubbing=ps&&ps.Active||pc&&pc.Active;promptRect.anchorMin=scrubbing?new Vector2(.3f,.82f):new Vector2(.25f,.3f);promptRect.anchorMax=scrubbing?new Vector2(.7f,.97f):new Vector2(.75f,.46f);var aim=v.Canvas.transform.Find("Aim");if(aim)aim.gameObject.SetActive(!scrubbing);
    string prompt=Game.Restaurant.PromptFor(p.ActorId);if(prompt==""&&p.Target)prompt="E / A  "+p.Target.Prompt(Game);
    var held=k.Hold(p.ActorId);string checklist=Game.Restaurant.HeldPlateChecklist(p.ActorId);
    string holding=held==null?"":"<size=14><color=#9FD8C8>Holding: "+k.Label(held)+(checklist==""?"":"  |  "+checklist)+"</color></size>\n";
    v.Prompt.text=holding+prompt;
    v.Notice.text=Game.Notice;
    UpdateHotbar(v,p);
   }
  }
  static string StandTicket(GameState s){
   if(!s.StandBuilt)return "Fire up Little Flame in Truck Park ($10)\nthen buy patties & buns at Milo's cart beside it.";
   string pace=s.StandPace=="rush"?"<color=#E1543B>RUSH: customers every few seconds</color>":s.StandRushSoon?"<color=#E8C34A>Rush coming soon!</color>":s.StandPace=="calm"?"<color=#9FD8C8>Quiet spell: catch up on plates</color>":"";
   string goal=pace==""?"":"<size=12>"+pace+"</size>";
   if(!s.HasOrder&&!s.StandOpen&&s.LastStandShift!=null){
    // The shift report stays on the card until the truck opens again.
    var r=s.LastStandShift;var g=RestaurantState.StarGoals[System.Math.Min(RestaurantState.StarGoals.Length-1,s.Restaurant.Rank+1)];
    return "<color=#F2C27A><b>"+r.Name.ToUpper()+" REPORT</b></color>  <size=12>day "+r.Day+"</size>\nServed "+r.Served+(r.Walked>0?"   <color=#E1543B>Walked out "+r.Walked+"</color>":"   Nobody walked out")+"\nEarned <b>$"+r.Earned+"</b>   Satisfaction "+r.Satisfaction.ToString("0")+"%"
     +(s.Restaurant.Rank+1<RestaurantState.StarGoals.Length?"\n<size=12>Next star: "+System.Math.Min(s.Restaurant.Served,g.served)+"/"+g.served+" served, "+s.Restaurant.Satisfaction.ToString("0")+"/"+g.satisfaction+" satisfaction</size>":"")
     +"\n<size=12>"+(r.Name=="Day shift"&&s.IsNight?"E on the menu board: night shift (+50% pay)":"E on the menu board to open again")+"</size>";
   }
   if(!s.HasOrder)return (s.StandOpen?"Truck OPEN: a customer is on the way...":"Truck CLOSED: press E on the menu board to open")+(goal==""?"":"\n"+goal);
   var lines=new List<string>();
   foreach(var o in s.StandQueue){
    if(o.Stage==2){lines.Add("<color=#9FD8C8>Seat "+(o.Table+1)+": eating</color>");continue;}
    string where=o.Stage==1?"Seat "+(o.Table+1)+": ":"In line: ";
    float ratio=Mathf.Clamp01(o.Patience/Mathf.Max(1,o.MaxPatience));string c=ratio>.55f?"#4FCB7A":ratio>.25f?"#E8C34A":"#E1543B";int f=Mathf.Max(1,Mathf.CeilToInt(ratio*8));
    lines.Add(where+(o.Dish=="midnight"?"Midnight burger [bun+patty+sauce]":o.Dish=="salad"?"Salad [chopped greens]":"Burger [bun+patty]")+"  <color="+c+">"+new string('|',f)+"</color>"+new string('.',8-f)+" "+(int)o.Patience+"s");
   }
   int dirtyTables=s.StandTableDirty==null?0:s.StandTableDirty.FindAll(d=>d).Count;
   lines.Add("<size=12>Plates: "+s.StandClean+" clean, "+s.StandDirty+" in the sink pile"+(dirtyTables>0?", <color=#E8C34A>"+dirtyTables+" dirty on tables: clear them!</color>":"")+"</size>");if(s.StandWorker!=null)lines.Add("<size=12><color=#9FD8C8>"+s.StandWorkerStatus+"  (+$"+s.StandWorkerEarned+")</color></size>");if(goal!="")lines.Add(goal);
   return string.Join("\n",lines);
  }
  // Ticket rail (A4): one card per waiting order with table, dish, its components, and a colored patience bar.
  static string TicketRail(RestaurantState r){
   var waiting=r.Orders.Where(o=>o.Stage==RestaurantOrderStage.Waiting).ToList();
   if(waiting.Count==0)return "";
   var lines=new List<string>();
   foreach(var o in waiting.Take(5)){
    var customer=RestaurantCatalog.Customers[o.CustomerType];var dish=RestaurantCatalog.Dish(o.DishId);var recipe=RecipeBook.Find(o.DishId);
    float pat=r.PatienceOf(o),left=Mathf.Max(0,pat-o.Wait),ratio=Mathf.Clamp01(left/pat);
    string barColor=ratio>.55f?"#4FCB7A":ratio>.25f?"#E8C34A":"#E1543B";
    int filled=Mathf.CeilToInt(ratio*8);string bar="<color="+barColor+">"+new string('|',Mathf.Max(1,filled))+"</color>"+new string('.',8-Mathf.Max(1,filled));
    string parts=recipe==null?"":string.Join("+",recipe.Components.Select(c=>c=="cooked_patty"?"patty":c=="chopped_greens"?"greens":c=="midnight_sauce"?"sauce":c));
    var who=ResidentCast.Get(o.ResidentId);string guest=who!=null?who.Name:customer.Name;int table=r.TableNumber(o.SeatInstanceId);
    lines.Add("<b>TABLE "+(table>0?table.ToString():"?")+"</b>  "+guest+" — "+dish.Name+" ["+parts+"]  "+bar+" "+(int)left+"s");
   }
   if(waiting.Count>5)lines.Add("+"+(waiting.Count-5)+" more waiting");
   return string.Join("\n",lines);
  }
 }
}
