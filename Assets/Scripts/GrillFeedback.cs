using UnityEngine;

namespace RestaurantCity {
    // Furnishing-bound presentation. Cooking progress, item identity and quality are read-only inputs.
    public sealed class GrillFeedback : MonoBehaviour {
        Transform root, patty, spatula, sear;
        Renderer searBody; readonly GameObject[] marks = new GameObject[3]; MaterialPropertyBlock smoke;
        readonly GameObject[] looks = new GameObject[3];
        readonly Transform[] steam = new Transform[3];
        Renderer[] bodies;
        MaterialPropertyBlock tint;
        static Mesh blockMesh, puffMesh;
        static Material steel, handle, vapor;
        int itemId = -1, turns;
        float flipTime, steamTime;
        bool paused;
        const float FlipDuration = .7f;
        public bool Active { get; private set; }
        public bool OwnsFood { get; private set; }
        public int FlipCount { get; private set; }
        public float HeatRatio { get; private set; }

        // Where food sits on this grill, in the station's local space. The art-pack grill (PolygonShops) is half open
        // grate (-X, bar tops at y 1.02-1.04, x -0.76..0, z -0.51..0.45) and half flat griddle (+X, y 1.05): food goes
        // in the middle of the grate. The hidden code-built grill under it is only the fallback.
        static readonly Vector3 PackGrateCentre = new Vector3(-.38f, 1.045f, -.03f);
        public static Vector3 GrillTop(Transform station) {
            foreach (var t in station.GetComponentsInChildren<Transform>(true))
                if (t.name == "SM_Prop_Kitchen_Grill_01") return station.InverseTransformPoint(t.TransformPoint(PackGrateCentre));
            var grate = station.Find("IronGrate");
            return grate ? new Vector3(0, grate.localPosition.y + grate.localScale.y * .5f + .006f, 0) : new Vector3(0,1.02f,0);
        }
        void Awake() {
            tint = new MaterialPropertyBlock();
            root = Group("Grill feedback", transform); root.localPosition = GrillTop(transform);
            patty = Group("Patty pivot", root);
            bodies = new Renderer[3];
            for (int n=0;n<3;n++) {
                looks[n] = KitchenArt.CreateItem(n==0?"PreparedPatty":n==1?"CookedPatty":"BurntPatty",patty);
                looks[n].transform.localPosition = Vector3.down*.03f;
                bodies[n] = looks[n].transform.Find("Faceted patty").GetComponent<Renderer>();
                // Both faces carry the same detail, so an actual half turn leaves a readable patty.
                foreach (Transform part in looks[n].GetComponentsInChildren<Transform>()) if (part!=looks[n].transform && part.name!="Faceted patty") {
                    var underside = Instantiate(part.gameObject,looks[n].transform).transform;
                    underside.name = part.name + " underside";
                    underside.localPosition = new Vector3(part.localPosition.x,.06f-part.localPosition.y,part.localPosition.z);
                }
            }
            EnsureResources();
            spatula=Group("Spatula",root);
            Shape("Steel slotted blade",spatula,blockMesh,new Vector3(0,0,0),new Vector3(.22f,.014f,.22f),steel);
            for(int n=0;n<3;n++)Shape("Blade slot",spatula,blockMesh,new Vector3((n-1)*.05f,.008f,0),new Vector3(.014f,.002f,.12f),handle);
            Shape("Spatula neck",spatula,blockMesh,new Vector3(0,.007f,.21f),new Vector3(.035f,.025f,.23f),steel);
            Shape("Spatula grip",spatula,blockMesh,new Vector3(0,.014f,.39f),new Vector3(.055f,.045f,.19f),handle);
            for(int n=0;n<3;n++)steam[n]=Shape("Heat wisp",root,puffMesh,Vector3.zero,Vector3.one*.015f,vapor).transform;
            Rest();
            // The top face shows the side that has already been on the grate (golden with grill marks after a flip).
            // Measured from the mesh through the transforms (works even while the station is inactive, e.g. the parked truck).
            var mb=bodies[0].GetComponent<MeshFilter>().sharedMesh.bounds; var toRoot=root.worldToLocalMatrix*bodies[0].transform.localToWorldMatrix;
            float top=toRoot.MultiplyPoint3x4(mb.center+Vector3.up*mb.extents.y).y;
            // The visible top is the highest point of any part of the patty model (crust details sit above the body).
            foreach(var mf in looks[1].GetComponentsInChildren<MeshFilter>(true)){var bb=mf.sharedMesh.bounds;var m2=root.worldToLocalMatrix*mf.transform.localToWorldMatrix;
                for(int c=0;c<8;c++){var corner=bb.center+Vector3.Scale(bb.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1));top=Mathf.Max(top,m2.MultiplyPoint3x4(corner).y);}}
            top-=.004f;   // sink the sear into the crust so it reads as the patty surface, not a lid
            var toWorld=bodies[0].transform.localToWorldMatrix;
            float rx=toWorld.MultiplyVector(Vector3.right*mb.extents.x).magnitude,rz=toWorld.MultiplyVector(Vector3.forward*mb.extents.z).magnitude;
            if(rx<.01f||rz<.01f){rx=rz=.06f;}
            sear=FeedbackArt.Group("Top sear",root); sear.localPosition=new Vector3(0,top,0);
            var ls=root.lossyScale; sear.localScale=new Vector3(1/Mathf.Max(.01f,ls.x),1/Mathf.Max(.01f,ls.y),1/Mathf.Max(.01f,ls.z));   // children in metres
            searBody=FeedbackArt.Disc("Seared face",sear,Vector3.zero,new Vector3(rx*1.62f,.006f,rz*1.62f),"B3703D").GetComponent<Renderer>();
            for(int n=0;n<3;n++)marks[n]=FeedbackArt.Box("Grill mark",sear,new Vector3((n-1)*rx*.42f,.0062f,0),new Vector3(rx*.1f,.0012f,rz*1.15f),"3A2418");
            smoke=new MaterialPropertyBlock();
            sear.gameObject.SetActive(false);
            foreach(var look in looks)look.SetActive(false);
            foreach(var puff in steam)puff.gameObject.SetActive(false);
        }
        static Color Doneness(float r) {
            Color raw=new Color(.788f,.424f,.392f),gold=new Color(.70f,.44f,.24f),dark=new Color(.42f,.25f,.15f),burnt=new Color(.16f,.13f,.12f);
            return r<1?Color.Lerp(raw,gold,r):r<=KitchenState.PerfectSide?gold:r<=KitchenState.GoodHigh?Color.Lerp(gold,dark,(r-KitchenState.PerfectSide)/(KitchenState.GoodHigh-KitchenState.PerfectSide)):Color.Lerp(dark,burnt,Mathf.Clamp01((r-KitchenState.GoodHigh)/.8f));
        }
        public float UpRatio { get; private set; }
        public float DownRatio { get; private set; }
        public void Present(KitchenStation station, KitchenItem item, bool isPaused, float dt, float golden = 4) {
            paused=isPaused;
            OwnsFood=station!=null&&station.CatalogId=="grill"&&item!=null&&(item.Kind==KitchenItemKind.RawProtein||item.Kind==KitchenItemKind.CookedPatty||item.Kind==KitchenItemKind.BurntPatty);
            if(!OwnsFood||item.Id!=itemId) { Active=false; flipTime=0; turns=0; Rest(); }
            itemId=OwnsFood?item.Id:-1;
            int state=!OwnsFood?-1:item.Kind==KitchenItemKind.BurntPatty?2:item.Kind==KitchenItemKind.CookedPatty?1:0;
            HeatRatio=OwnsFood?Mathf.Clamp01(station.Progress/8):0;
            for(int n=0;n<3;n++)looks[n].SetActive(n==state);
            if(OwnsFood) {
                Color raw=new Color(.788f,.424f,.392f), cooked=new Color(.537f,.318f,.231f), burnt=new Color(.188f,.157f,.141f);
                Color color=state==0?Color.Lerp(raw,cooked,HeatRatio*.85f):state==2?burnt:Color.Lerp(cooked,burnt,Mathf.Clamp01((station.Progress-8)/16)*.7f);
                // Two-sided patties: the body mixes both faces, the top face shows the up side.
                UpRatio=KitchenState.UpSide(item)/Mathf.Max(.1f,golden); DownRatio=KitchenState.DownSide(item)/Mathf.Max(.1f,golden);
                if(state!=2&&item.SideA+item.SideB>0)color=Color.Lerp(Doneness(UpRatio),Doneness(DownRatio),.5f);
                tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);bodies[state].SetPropertyBlock(tint);
                var top=state==2?burnt:Doneness(UpRatio);tint.SetColor("_BaseColor",top);tint.SetColor("_Color",top);searBody.SetPropertyBlock(tint);
                foreach(var m in marks)m.SetActive(UpRatio>=.6f||state==2);
            }
            if(isPaused) { Active=false;flipTime=0;Rest(); }
            if(Active) {
                flipTime+=Mathf.Max(0,dt);
                float t=Mathf.Clamp01(flipTime/FlipDuration), lift=Mathf.Sin(t*Mathf.PI);
                patty.localPosition=new Vector3(0,.03f+lift*.26f,0);
                patty.localRotation=Quaternion.Euler(Mathf.Lerp((turns-1)*180,turns*180,Mathf.SmoothStep(0,1,t)),0,0);
                spatula.localPosition=Vector3.Lerp(new Vector3(.4f,.03f,.08f),new Vector3(0,.025f,0),Mathf.Sin(t*Mathf.PI));
                spatula.localPosition+=Vector3.up*(lift*.21f);
                spatula.localRotation=Quaternion.Euler(-lift*28,0,0);
                if(t>=1) { Active=false;Rest(); }
            }
            sear.gameObject.SetActive(OwnsFood&&!Active&&(state==2||item.SideA+item.SideB>0));
            bool heating=OwnsFood&&state!=2&&!isPaused;
            // Smoke says when to flip: thin while browning, big puffs once the underside is golden, dark when it's overdone.
            float puff=!OwnsFood?1:DownRatio>=1?1.9f:1; Color smokeColor=OwnsFood&&DownRatio>KitchenState.GoodHigh?new Color(.33f,.31f,.3f):new Color(.82f,.83f,.77f);
            smoke.SetColor("_BaseColor",smokeColor);smoke.SetColor("_Color",smokeColor);
            if(heating)steamTime+=Mathf.Max(0,dt);
            for(int n=0;n<3;n++) {
                steam[n].gameObject.SetActive(heating);
                float cycle=Mathf.Repeat(steamTime*.7f+n/3f,1), size=Mathf.Sin(cycle*Mathf.PI)*.022f*puff;
                steam[n].GetComponent<Renderer>().SetPropertyBlock(smoke);
                steam[n].localPosition=new Vector3((n-1)*.065f+Mathf.Sin(steamTime+n)*.016f,.1f+cycle*.28f,.025f);
                steam[n].localScale=new Vector3(size*.6f,size*1.9f,size*.6f);
            }
        }
        public bool CanFlip=>OwnsFood&&!paused&&!Active;
        public bool TryFlip() {
            if(!OwnsFood||paused||Active)return false;
            Active=true;flipTime=0;turns++;FlipCount++;return true;
        }
        void Rest() {
            patty.localPosition=Vector3.up*.03f;patty.localRotation=Quaternion.Euler(turns*180,0,0);
            spatula.localPosition=new Vector3(.4f,.03f,.08f);spatula.localRotation=Quaternion.Euler(0,-18,0);
        }
        static Transform Group(string name,Transform parent) {var obj=new GameObject(name);obj.transform.SetParent(parent,false);return obj.transform;}
        static GameObject Shape(string name,Transform parent,Mesh mesh,Vector3 position,Vector3 scale,Material material) {
            var t=Group(name,parent);t.localPosition=position;t.localScale=scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t.gameObject;
        }
        static Material Material(Color color) {
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
            return new Material(shader){color=color,name="Original grill feedback"};
        }
        static void EnsureResources() {
            if(blockMesh)return;
            // Closed original meshes, with no primitive colliders to create or remove.
            blockMesh=new Mesh{name="Original spatula block"};
            blockMesh.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
            blockMesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};blockMesh.RecalculateNormals();blockMesh.RecalculateBounds();
            puffMesh=new Mesh{name="Original heat wisp"};puffMesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};puffMesh.triangles=new[]{0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};puffMesh.RecalculateNormals();puffMesh.RecalculateBounds();
            steel=Material(new Color(.74f,.8f,.79f));handle=Material(new Color(.12f,.18f,.2f));vapor=Material(new Color(.82f,.83f,.77f));
        }
    }

    public partial class RestaurantController {
        public bool TryFlipStation(FirstPersonPlayer player) {
            if(!player||!Game||Game.State==null||Game.Paused||ManagementPauses||PlacementActive)return false;
            if(!player.TryResolveInteractionHit(out var hit)||!hit.collider)return false;
            var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
            var station=Game.State.Kitchen.Stations.Find(s=>s.InstanceId==target.InstanceId);
            if(station==null||station.CatalogId!="grill")return false;
            var obj=StationObject(station.InstanceId);if(!obj||!obj.activeInHierarchy)return false;
            var view=obj.GetComponent<GrillFeedback>();if(!view)view=obj.AddComponent<GrillFeedback>();
            view.Present(station,Game.State.Kitchen.At(station.InstanceId),false,0);
            if(!view.CanFlip||!Game.State.Kitchen.FlipPatty(Game.State,station.InstanceId,out _))return false;
            return view.TryFlip();
        }
        // Flick the mouse up while looking at a patty whose underside has started to brown: the same flip as a click.
        public bool TryFlickFlip(FirstPersonPlayer player) {
            if(!player||!player.TryResolveInteractionHit(out var hit)||!hit.collider)return false;
            var target=hit.collider.GetComponentInParent<RestaurantTarget>();if(!target)return false;
            var station=Game.State.Kitchen.Stations.Find(s=>s.InstanceId==target.InstanceId);var item=station==null?null:Game.State.Kitchen.At(station.InstanceId);
            if(station==null||station.CatalogId!="grill"||!KitchenState.Flippable(item))return false;
            if(KitchenState.DownSide(item)<KitchenState.SideGolden("grill",Data.LevelOf(station.InstanceId))*KitchenState.GoodLow)return false;
            return TryFlipStation(player);
        }
        public void TickGrillFeedback(float dt) {
            if(!Game||Game.State==null)return;
            bool paused=Game.Paused||ManagementPauses||PlacementActive;
            foreach(var station in Game.State.Kitchen.Stations)if(station.CatalogId=="grill") {
                var obj=StationObject(station.InstanceId);if(!obj||!obj.activeInHierarchy)continue;
                var view=obj.GetComponent<GrillFeedback>();if(!view)view=obj.AddComponent<GrillFeedback>();
                var item=Game.State.Kitchen.At(station.InstanceId);
                view.Present(station,item,paused,dt,KitchenState.SideGolden("grill",Data.LevelOf(station.InstanceId)));
                if(view.OwnsFood&&physicalItems.TryGetValue(item.Id,out var food)&&food)food.SetActive(false);
            }
        }
    }
}
