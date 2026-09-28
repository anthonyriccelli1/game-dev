using UnityEngine;

namespace RestaurantCity {
    // Furnishing-bound presentation. Cooking progress, item identity and quality are read-only inputs.
    public sealed class GrillFeedback : MonoBehaviour {
        Transform root, patty, spatula;
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

        public static Vector3 GrillTop(Transform station) {
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
            foreach(var look in looks)look.SetActive(false);
            foreach(var puff in steam)puff.gameObject.SetActive(false);
        }
        public void Present(KitchenStation station, KitchenItem item, bool isPaused, float dt) {
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
                tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);bodies[state].SetPropertyBlock(tint);
            }
            if(isPaused) { Active=false;flipTime=0;Rest(); }
            if(Active) {
                flipTime+=Mathf.Max(0,dt);
                float t=Mathf.Clamp01(flipTime/FlipDuration), lift=Mathf.Sin(t*Mathf.PI);
                patty.localPosition=new Vector3(0,.03f+lift*.26f,0);
                patty.localRotation=Quaternion.Euler(Mathf.Lerp((turns-1)*180,turns*180,Mathf.SmoothStep(0,1,t)),0,0);
                spatula.localPosition=Vector3.Lerp(new Vector3(.48f,.026f,.08f),new Vector3(0,.025f,0),Mathf.Sin(t*Mathf.PI));
                spatula.localPosition+=Vector3.up*(lift*.21f);
                spatula.localRotation=Quaternion.Euler(-lift*28,0,0);
                if(t>=1) { Active=false;Rest(); }
            }
            bool heating=OwnsFood&&state!=2&&!isPaused;
            if(heating)steamTime+=Mathf.Max(0,dt);
            for(int n=0;n<3;n++) {
                steam[n].gameObject.SetActive(heating);
                float cycle=Mathf.Repeat(steamTime*.7f+n/3f,1), size=Mathf.Sin(cycle*Mathf.PI)*.022f;
                steam[n].localPosition=new Vector3((n-1)*.065f+Mathf.Sin(steamTime+n)*.016f,.1f+cycle*.28f,.025f);
                steam[n].localScale=new Vector3(size*.6f,size*1.9f,size*.6f);
            }
        }
        public bool TryFlip() {
            if(!OwnsFood||paused||Active)return false;
            Active=true;flipTime=0;turns++;FlipCount++;return true;
        }
        void Rest() {
            patty.localPosition=Vector3.up*.03f;patty.localRotation=Quaternion.Euler(turns*180,0,0);
            spatula.localPosition=new Vector3(.48f,.026f,.08f);spatula.localRotation=Quaternion.Euler(0,-18,0);
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
            return view.TryFlip();
        }
        public void TickGrillFeedback(float dt) {
            if(!Game||Game.State==null)return;
            bool paused=Game.Paused||ManagementPauses||PlacementActive;
            foreach(var station in Game.State.Kitchen.Stations)if(station.CatalogId=="grill") {
                var obj=StationObject(station.InstanceId);if(!obj||!obj.activeInHierarchy)continue;
                var view=obj.GetComponent<GrillFeedback>();if(!view)view=obj.AddComponent<GrillFeedback>();
                var item=Game.State.Kitchen.At(station.InstanceId);
                view.Present(station,item,paused,dt);
                if(view.OwnsFood&&physicalItems.TryGetValue(item.Id,out var food)&&food)food.SetActive(false);
            }
        }
    }
}
