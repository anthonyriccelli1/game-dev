using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RestaurantCity {
 public partial class InteractionAcceptance {
  void RunFinishChecks() {
   var catalog=typeof(RestaurantState).Assembly.GetType("RestaurantCity.FinishCatalog");
   var surface=typeof(RestaurantState).GetField("SurfaceFinishes");
   var apply=typeof(RestaurantState).GetMethod("ApplyFinish");
   Check(catalog!=null,"expanded finish catalog exists");
   Check(surface!=null&&apply!=null,"saved targeted surface finish system exists");
   Check(typeof(RestaurantController).GetProperty("FinishBrushActive")!=null,"finish selection provides a persistent preview brush");
   if(catalog==null||surface==null||apply==null)return;
   var all=(Array)catalog.GetField("All").GetValue(null);
   string Id(object d)=>(string)d.GetType().GetField("Id").GetValue(d);
   int Number(object d,string name)=>(int)d.GetType().GetField(name).GetValue(d);
   Check(all.Cast<object>().Count(d=>Id(d).StartsWith("wall_"))>=12&&all.Cast<object>().Count(d=>Id(d).StartsWith("floor_"))>=10,"shop has twelve walls and ten floors");
   Check(all.Cast<object>().Select(Id).Distinct().Count()==all.Length,"finish catalog IDs are unique");
   Check(all.Cast<object>().Count(d=>Number(d,"Tier")==0&&Number(d,"RequiredStars")==1)>=10,"starting shop has substantial accessible variety");
   var wallet=new GameState{Cash=1000};var data=wallet.Restaurant;data.Owned=true;
   bool Paint(string id,string key,bool fill=false){object[] args={wallet,id,key,fill,null};return (bool)apply.Invoke(data,args);}
   string At(string key)=>(string)typeof(RestaurantState).GetMethod("FinishAt").Invoke(data,new object[]{key});
   int Quote(string id,string key,bool fill)=>(int)typeof(RestaurantState).GetMethod("FinishPrice").Invoke(data,new object[]{id,key,fill});
   int before=wallet.Cash;string untouched=At("floor:1:0");
   Check(Paint("floor_checker","floor:0:0")&&wallet.Cash<before,"single floor tile is purchased");
   Check(At("floor:0:0")=="floor_checker"&&At("floor:1:0")==untouched,"painting preserves neighboring floor tiles");
   before=wallet.Cash;Paint("floor_checker","floor:0:0");Check(wallet.Cash==before,"identical finish never charges twice");
   Check(!Paint("wall_cream","floor:0:0")&&!Paint("floor_checker","floor:12:0")&&wallet.Cash==before,"invalid surface family and bounds reject without charge");
   Check(Paint("wall_rose","wall:left:3")&&At("wall:left:3")=="wall_rose"&&At("wall:right:3")!="wall_rose","individual wall section preserves other walls");
   var premium=all.Cast<object>().First(d=>Number(d,"Tier")>0||Number(d,"RequiredStars")>1);before=wallet.Cash;
   Check(!Paint(Id(premium),Id(premium).StartsWith("wall_")?"wall:back:0":"floor:0:1")&&wallet.Cash==before,"progression locks are enforced at purchase");
   data.Open=true;Check(!Paint("floor_wood","floor:1:1"),"service blocks finish remodeling");data.Open=false;
   int quote=Quote("floor_wood","floor:0:0",true);wallet.Cash=quote-1;string old=At("floor:0:0");
   Check(!Paint("floor_wood","floor:0:0",true)&&At("floor:0:0")==old&&wallet.Cash==quote-1,"unaffordable room fill is atomic");
   wallet.Cash=quote+100;Check(Paint("floor_wood","floor:0:0",true)&&wallet.Cash==100&&At("floor:11:9")=="floor_wood","fill charges its quote and covers the whole floor");
   Check(data.Ambience<=10,"surface coverage cannot farm ambience per tile");
   var restored=JsonUtility.FromJson<RestaurantState>(JsonUtility.ToJson(data));
   Check((string)typeof(RestaurantState).GetMethod("FinishAt").Invoke(restored,new object[]{"wall:left:3"})=="wall_rose"&&(string)typeof(RestaurantState).GetMethod("FinishAt").Invoke(restored,new object[]{"floor:11:9"})=="floor_wood","mixed finishes survive save serialization");
   var legacy=JsonUtility.FromJson<RestaurantState>("{\"Owned\":true}");legacy.SanitizeAfterLoad();
   Check(surface.GetValue(legacy)!=null,"older save defaults surface overrides safely");

   // Exercise the real catalog entry, camera restoration and repeated brush purchase APIs.
   Game.State.Cash=1000;R.Data.Open=false;R.Data.Orders.Clear();R.ClosePanel();
   var originalMask=P.View.cullingMask;var originalPosition=P.View.transform.position;
   R.SelectCatalogItem("floor_checker");
   bool Active()=>(bool)typeof(RestaurantController).GetProperty("FinishBrushActive").GetValue(R);
   bool Choose(string key)=>(bool)typeof(RestaurantController).GetMethod("ChooseFinishSurface").Invoke(R,new object[]{key});
   bool Apply(bool fill)=>(bool)typeof(RestaurantController).GetMethod("ApplySelectedFinish").Invoke(R,new object[]{fill});
   Check(Active()&&R.PlacementActive&&Game.State.Cash==1000,"catalog finish selection previews without spending");
   Check(Choose("floor:4:4")&&Apply(false)&&Active(),"single patch purchase leaves brush active");
   CapturePlacement("decor-floor-brush.png");R.CancelPlacement(false);
   Check(!Active()&&!R.PlacementActive&&P.View.cullingMask==originalMask&&Vector3.Distance(originalPosition,P.View.transform.position)<.01f,"brush cancel restores camera and mask");
   R.SelectCatalogItem("wall_rose");Check(Choose("wall:back:5")&&Apply(false),"wall section brush applies selected material");CapturePlacement("decor-wall-brush.png");R.CancelPlacement(false);
   R.RebuildLayout();
   Check(R.Room.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterial&&r.sharedMaterial.name.Contains("floor_checker")),"saved floor patch renders after layout rebuild");
   P.Teleport(new Vector3(-10,.15f,-13));P.LookAt(new Vector3(-10,1.8f,-21.6f));CapturePlacement("decor-mixed-room.png");
   R.ShowPanel("Catalog");CapturePlacement("decor-catalog.png");R.ClosePanel();
  }
 }
}
