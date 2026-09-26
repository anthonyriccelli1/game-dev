using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    /// <summary>Original faceted kitchen props. Runtime generated and shared within a process.</summary>
    public static class KitchenArt {
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        static Material M(string hex, bool metal = false) {
            string key = hex + metal;
            if (Mats.TryGetValue(key, out var found) && found) return found;
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (!shader) shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = "Kitchen_original_" + key, color = color };
            mat.SetFloat("_Metallic", metal ? .65f : 0); mat.SetFloat("_Smoothness", metal ? .38f : .2f);
            Mats[key] = mat; return mat;
        }
        static GameObject G(string name, Transform parent, Vector3 p = default(Vector3)) {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = p; return g;
        }
        static GameObject S(string name, Transform parent, Mesh mesh, Vector3 p, Vector3 scale, Material material) {
            var g = G(name, parent, p); g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = material; return g;
        }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            int n = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(n); t.Add(n+1); t.Add(n+2); t.Add(n); t.Add(n+2); t.Add(n+3);
        }
        static Mesh Finish(string key, List<Vector3> v, List<int> t) {
            var m = new Mesh { name = "Kitchen_original_" + key }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); Meshes[key] = m; return m;
        }
        // All revolved shapes use split face vertices for crisp, deliberately faceted silhouettes.
        static Mesh Profile(string key, float[] heights, float[] radii, int sides = 12) {
            if (Meshes.TryGetValue(key, out var m) && m) return m;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int y=0; y<heights.Length-1; y++) for (int i=0; i<sides; i++) {
                float a=i*Mathf.PI*2/sides, b=(i+1)*Mathf.PI*2/sides;
                Quad(v,t,new Vector3(Mathf.Cos(a)*radii[y],heights[y],Mathf.Sin(a)*radii[y]),new Vector3(Mathf.Cos(a)*radii[y+1],heights[y+1],Mathf.Sin(a)*radii[y+1]),new Vector3(Mathf.Cos(b)*radii[y+1],heights[y+1],Mathf.Sin(b)*radii[y+1]),new Vector3(Mathf.Cos(b)*radii[y],heights[y],Mathf.Sin(b)*radii[y]));
            }
            return Finish(key,v,t);
        }
        static Mesh Block() {
            const string key="chamfer"; if(Meshes.TryGetValue(key,out var m)&&m)return m;
            Vector2[] ring={new Vector2(-.42f,-.5f),new Vector2(.42f,-.5f),new Vector2(.5f,-.42f),new Vector2(.5f,.42f),new Vector2(.42f,.5f),new Vector2(-.42f,.5f),new Vector2(-.5f,.42f),new Vector2(-.5f,-.42f)};
            float[] ys={-.5f,-.42f,.42f,.5f}, scales={.88f,1,1,.88f}; var v=new List<Vector3>();var t=new List<int>();
            for(int layer=0;layer<3;layer++)for(int i=0;i<8;i++){int j=(i+1)%8;Quad(v,t,new Vector3(ring[i].x*scales[layer],ys[layer],ring[i].y*scales[layer]),new Vector3(ring[i].x*scales[layer+1],ys[layer+1],ring[i].y*scales[layer+1]),new Vector3(ring[j].x*scales[layer+1],ys[layer+1],ring[j].y*scales[layer+1]),new Vector3(ring[j].x*scales[layer],ys[layer],ring[j].y*scales[layer]));}
            for(int i=0;i<8;i++){int j=(i+1)%8;Quad(v,t,new Vector3(0,.5f,0),new Vector3(ring[j].x*.88f,.5f,ring[j].y*.88f),new Vector3(ring[i].x*.88f,.5f,ring[i].y*.88f),new Vector3(0,.5f,0));Quad(v,t,new Vector3(0,-.5f,0),new Vector3(ring[i].x*.88f,-.5f,ring[i].y*.88f),new Vector3(ring[j].x*.88f,-.5f,ring[j].y*.88f),new Vector3(0,-.5f,0));}
            return Finish(key,v,t);
        }
        static GameObject Box(string name,Transform p,Vector3 position,Vector3 scale,string color) { return S(name,p,Block(),position,scale,M(color)); }
        static GameObject Disk(string name,Transform p,Vector3 position,Vector3 scale,string color) { return S(name,p,Profile("disc",new[]{0f,.05f,.22f,.78f,.95f,1f},new[]{0f,.46f,.5f,.5f,.46f,0f}),position,scale,M(color)); }
        static void Bar(string name,Transform p,Vector3 a,Vector3 b,float thickness,string color) {
            var bar=Box(name,p,(a+b)*.5f,new Vector3(thickness,(b-a).magnitude,thickness),color);bar.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        }
        static void Bench(Transform p,float width,string color) {
            Box("Enamel cabinet",p,new Vector3(0,.49f,0),new Vector3(width-.12f,.84f,.83f),color);
            Box("Rounded steel worktop",p,new Vector3(0,.94f,0),new Vector3(width,.12f,.97f),"B5C7C1");
            for(int side=-1;side<=1;side+=2){Box("Recessed door",p,new Vector3(side*width*.235f,.51f,.431f),new Vector3(width*.43f,.67f,.035f),"286764");Bar("Brass door pull",p,new Vector3(side*width*.235f-.12f,.7f,.473f),new Vector3(side*width*.235f+.12f,.7f,.473f),.035f,"CA9B53");}
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Rubber cabinet foot",p,new Vector3(x*(width*.5f-.16f),.06f,z*.3f),new Vector3(.12f,.12f,.12f),"233B46");
        }
        public static GameObject CreateStation(string id,Transform parent) {
            var root=G("Kitchen_"+id,parent); var p=root.transform;float width=id=="plate_rack"? .9f:id=="counter"||id=="trash"||id=="cutting_board"? .95f:1.9f;
            if(id=="pantry") {
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Shelf upright",p,new Vector3(x*.87f,.87f,z*.35f),new Vector3(.085f,1.74f,.085f),"317E79");
                for(int n=0;n<3;n++)Box("Oak supply shelf",p,new Vector3(0,.22f+n*.54f,0),new Vector3(1.9f,.085f,.85f),"895343");
                // The food on the shelves is live stock, drawn by RestaurantController.TickPantryDisplays.
                Box("Cream shelf canopy",p,new Vector3(0,1.85f,0),new Vector3(1.95f,.12f,.9f),"F5DBAA");
                // One aimable hitbox per ingredient shelf (Stage A / A2): the sub-id lets Preview/Act
                // resolve exactly which ingredient the player is looking at, replacing the hidden Q cycle.
                PantryShelf(p,"protein",-.48f,0,.62f); PantryShelf(p,"greens",.48f,0,.62f);
                PantryShelf(p,"bun",-.48f,.62f,1.12f); PantryShelf(p,"sauce",.48f,.62f,1.12f);
            } else if(id=="plate_rack") {
                Bench(p,width,"317E79");
                for(int stack=0;stack<2;stack++)for(int n=0;n<5;n++)Plate(p,new Vector3(stack==0?-.21f:.21f,1+n*.04f,0),.8f);
                Box("Rack back",p,new Vector3(0,1.39f,-.36f),new Vector3(.86f,.72f,.08f),"895343");
                for(int n=0;n<7;n++)Bar("Plate divider",p,new Vector3(-.32f+n*.105f,1.42f,-.28f),new Vector3(-.32f+n*.105f,1.68f,-.09f),.025f,"CA9B53");
                for(int n=0;n<4;n++){var plate=Plate(p,new Vector3(-.26f+n*.15f,1.5f,-.2f),.7f);plate.transform.localRotation=Quaternion.Euler(76,0,0);}
            } else if(id=="counter"||id=="cutting_board") {
                // Plain one-tile square counter: a place to set plates and ingredients down.
                // The stand's cutting board is the same counter with a chopping board and knife on top.
                if(id=="cutting_board"){Box("Chopping board",p,new Vector3(0,1.045f,0),new Vector3(.62f,.04f,.44f),"E2B878");Box("Knife blade",p,new Vector3(.22f,1.075f,-.12f),new Vector3(.2f,.01f,.04f),"D8DEE0");Box("Knife handle",p,new Vector3(.36f,1.078f,-.12f),new Vector3(.09f,.02f,.035f),"302824");}
                Box("Counter top",p,new Vector3(0,.99f,0),new Vector3(.92f,.07f,.92f),"CA9B53");
                Box("Counter apron",p,new Vector3(0,.91f,0),new Vector3(.86f,.1f,.86f),"317E79");
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Counter leg",p,new Vector3(x*.38f,.45f,z*.38f),new Vector3(.08f,.9f,.08f),"317E79");
                Box("Counter shelf",p,new Vector3(0,.25f,0),new Vector3(.8f,.04f,.8f),"895343");
            } else if(id=="trash") {
                // Round kitchen bin with a pedal and a swing lid.
                S("Bin body",p,Profile("bin",new[]{0f,.05f,.75f,.8f},new[]{.26f,.3f,.33f,.33f},14),Vector3.zero,Vector3.one,M("5E7F86"));
                S("Bin lid",p,Profile("binlid",new[]{0f,.04f,.08f},new[]{.35f,.35f,.2f},14),new Vector3(0,.8f,0),Vector3.one,M("C8D6D2"));
                Box("Bin pedal",p,new Vector3(0,.05f,.34f),new Vector3(.18f,.04f,.12f),"1B2A30");
                Box("Bin band",p,new Vector3(0,.55f,0),new Vector3(.62f,.05f,.62f),"E1543B");
            } else if(id=="sink") {
                // Open basin is built from its rim, sloped sides, and lowered bottom.
                Bench(p,width,"317E79");
                Box("Dark basin interior",p,new Vector3(-.3f,1.012f,0),new Vector3(.87f,.023f,.67f),"344D56");
                Box("Basin bottom",p,new Vector3(-.3f,1.025f,0),new Vector3(.68f,.02f,.47f),"738D96");
                for(int sign=-1;sign<=1;sign+=2){Box("Basin side rim",p,new Vector3(-.3f+sign*.43f,1.07f,0),new Vector3(.055f,.12f,.73f),"CEE0DC");Box("Basin front rim",p,new Vector3(-.3f,1.07f,sign*.34f),new Vector3(.85f,.12f,.055f),"CEE0DC");}
                Disk("Drain",p,new Vector3(-.3f,1.04f,0),new Vector3(.13f,.014f,.13f),"233B46");
                Bar("Faucet stem",p,new Vector3(-.3f,1,-.4f),new Vector3(-.3f,1.47f,-.4f),.065f,"CA9B53");Bar("Faucet arch",p,new Vector3(-.3f,1.47f,-.4f),new Vector3(-.3f,1.47f,-.03f),.065f,"CA9B53");Bar("Faucet spout",p,new Vector3(-.3f,1.47f,-.03f),new Vector3(-.3f,1.36f,-.03f),.07f,"CA9B53");
                for(int n=0;n<5;n++)Bar("Drying rack rail",p,new Vector3(.29f+n*.1f,1.075f,-.29f),new Vector3(.29f+n*.1f,1.075f,.29f),.025f,"CA9B53");
                Box("Folded dish cloth",p,new Vector3(.67f,1.1f,.23f),new Vector3(.3f,.035f,.2f),"D96555");
                var soap=CreateItem("RawSauce",p);soap.name="Soap dispenser";soap.transform.localPosition=new Vector3(.72f,1.02f,-.29f);soap.transform.localScale=Vector3.one*.65f;
            } else {
                Bench(p,width,"317E79");
                Box("Maple assembly board",p,new Vector3(-.31f,1.026f,.06f),new Vector3(.92f,.04f,.67f),"CA9B53");
                Crate(p,new Vector3(.62f,1.025f,-.05f),"buns",.58f);
                for(int side=-1;side<=1;side+=2)Bar("Serving pass upright",p,new Vector3(side*.85f,1,-.35f),new Vector3(side*.85f,1.8f,-.35f),.045f,"CA9B53");
                Box("Serving pass shelf",p,new Vector3(0,1.78f,-.23f),new Vector3(1.9f,.055f,.41f),"F5DBAA");
                Disk("Service bell base",p,new Vector3(.6f,1.012f,.29f),new Vector3(.17f,.025f,.17f),"233B46");
                S("Brass service bell",p,Profile("bell",new[]{0f,.05f,.1f,.13f},new[]{.075f,.075f,.045f,0f}),new Vector3(.6f,1.04f,.29f),Vector3.one,M("CA9B53",true));
            }
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,id=="pantry"?.9f:.51f,0);collider.size=new Vector3(width,id=="pantry"?1.8f:1.02f,.9f);
            G("WorkPoint",p,new Vector3(0,0,1.05f));G("ItemPoint",p,new Vector3(-.3f,1.06f,.05f)); if(id=="pantry"||id=="sink")root.transform.localScale=new Vector3(.5f,1,1); return root;
        }
        // World-space progress bar shown above a station (cooking, chopping, washing). No colliders.
        public static GameObject ProgressBar(Transform parent) {
            var root=G("Progress bar",parent,new Vector3(0,2.05f,0));
            S("Bar frame",root.transform,Block(),Vector3.zero,new Vector3(.98f,.16f,.05f),M("1B2A30"));
            var fill=S("Bar fill",root.transform,Block(),new Vector3(0,0,-.03f),new Vector3(.9f,.1f,.05f),M("E8C34A"));
            return root;
        }
        public static void SetProgress(GameObject bar,float ratio,string hex) {
            ratio=Mathf.Clamp01(ratio);var fill=bar.transform.Find("Bar fill");if(!fill)return;
            fill.localScale=new Vector3(Mathf.Max(.001f,.9f*ratio),.1f,.05f);fill.localPosition=new Vector3(-.45f+.45f*ratio,0,-.03f);
            var r=fill.GetComponent<MeshRenderer>();var m=M(hex);if(r.sharedMaterial!=m)r.sharedMaterial=m;
        }
        static GameObject Plate(Transform p,Vector3 position,float scale=1) {
            return S("Glazed cream plate",p,Profile("plate",new[]{0f,.012f,.024f,.045f,.055f,.049f,.033f,.025f,.025f},new[]{0f,.11f,.17f,.22f,.235f,.24f,.215f,.165f,0f},16),position,Vector3.one*scale,M("FFF1D1"));
        }
        static void Greens(Transform p,Vector3 position,bool chopped) {
            if(chopped){for(int i=0;i<9;i++){float a=i*2.399f;var leaf=Box("Chopped leaf",p,position+new Vector3(Mathf.Cos(a)*.055f*(i%3),.018f*(i%2),Mathf.Sin(a)*.055f*(i%3)),new Vector3(.07f,.027f,.055f),i%2==0?"73A566":"A0C77D");leaf.transform.localRotation=Quaternion.Euler(0,i*41,9);}}
            else for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var leaf=S("Folded cabbage leaf",p,Profile("leaf",new[]{0f,.06f,.13f,.22f},new[]{0f,.067f,.055f,0f},5),position+new Vector3(Mathf.Cos(a)*.04f,0,Mathf.Sin(a)*.04f),new Vector3(1,1,.65f),M(i%2==0?"73A566":"A0C77D"));leaf.transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*27,i*60,Mathf.Sin(a)*27);}
        }
        static void Patty(Transform p,Vector3 position,string kind) {
            string color=kind=="BurntPatty"?"302824":kind=="CookedPatty"?"89513B":"C96C64";
            Disk("Faceted patty",p,position,new Vector3(.27f,.06f,.25f),color);
            if(kind=="CookedPatty"||kind=="BurntPatty")for(int i=0;i<3;i++){var line=Box("Seared grill stripe",p,position+new Vector3((i-1)*.064f,.063f,0),new Vector3(.017f,.006f,.18f),"422C2B");line.transform.localRotation=Quaternion.Euler(0,-18,0);}
            else for(int i=0;i<6;i++)Box("Marbled fat fleck",p,position+new Vector3(Mathf.Cos(i*2.4f)*.075f,.06f,Mathf.Sin(i*2.4f)*.075f),new Vector3(.025f,.004f,.011f),"F0BC9D");
        }
        static void Bun(Transform p,Vector3 position) {
            S("Golden sesame bun",p,Profile("bun",new[]{0f,.02f,.07f,.105f,.12f},new[]{0f,.135f,.13f,.085f,0f}),position,Vector3.one,M("D9A45E"));
            for(int i=0;i<7;i++){float a=i*2.399f;var seed=Box("Sesame seed",p,position+new Vector3(Mathf.Cos(a)*.064f,.107f,Mathf.Sin(a)*.064f),new Vector3(.021f,.007f,.009f),"FFF1D1");seed.transform.localRotation=Quaternion.Euler(0,i*32,0);}
        }
        public static GameObject CreateItem(string kind,Transform parent) => CreateItem(kind,null,parent);
        public static GameObject CreateItem(string kind,List<string> components,Transform parent) {
            var root=G("Food_"+kind,parent);var p=root.transform;
            bool Has(string id)=>components!=null&&components.Contains(id);
            if(kind=="Plate"||kind=="DirtyPlate") {
                Plate(p,Vector3.zero);
                if(kind=="DirtyPlate"){Disk("Sauce stain",p,new Vector3(.04f,.029f,0),new Vector3(.17f,.003f,.11f),"89513B");for(int i=0;i<4;i++)Box("Plate crumb",p,new Vector3(-.1f+i*.055f,.036f,.06f),new Vector3(.025f,.018f,.023f),"CA9B53");}
                else {
                    bool bun=Has("bun"),patty=Has("cooked_patty");
                    if(bun)Disk("Bottom bun",p,new Vector3(0,.03f,0),new Vector3(.28f,.045f,.28f),"D9A45E");
                    if(Has("chopped_greens"))Greens(p,new Vector3(0,.08f,0),true);
                    if(patty)Patty(p,new Vector3(0,bun?.105f:.04f,0),"CookedPatty");
                    if(Has("midnight_sauce")){Disk("Midnight glaze",p,new Vector3(0,.17f,0),new Vector3(.24f,.012f,.22f),"824DA1");for(int i=0;i<3;i++)Box("Glaze glint",p,new Vector3(-.065f+i*.055f,.187f,0),new Vector3(.023f,.006f,.07f),"B492CB");}
                    if(bun)Bun(p,new Vector3(0,patty?.18f:.09f,0));
                }
            } else if(kind=="RawGreens"||kind=="ChoppedGreens")Greens(p,Vector3.zero,kind=="ChoppedGreens");
            else if(kind=="RawSauce"||kind=="MidnightSauce") {
                S("Faceted sauce bottle",p,Profile("bottle",new[]{0f,.02f,.2f,.24f,.29f,.3f},new[]{0f,.065f,.065f,.025f,.025f,0f},8),Vector3.zero,Vector3.one,M(kind=="MidnightSauce"?"824DA1":"D96555"));
                Disk("Bottle cap",p,new Vector3(0,.28f,0),new Vector3(.067f,.037f,.067f),"CA9B53");Box("Cream bottle band",p,new Vector3(0,.12f,.062f),new Vector3(.082f,.079f,.005f),"F5DBAA");
            } else if(kind=="GroceryBag") {
                // Milo's paper grocery bag: kraft paper, a folded rim, and a bun and greens peeking out.
                Box("Kraft bag",p,new Vector3(0,.17f,0),new Vector3(.3f,.34f,.2f),"C9A26B");Box("Folded rim",p,new Vector3(0,.345f,0),new Vector3(.31f,.03f,.21f),"B08A55");
                Box("Milo stamp",p,new Vector3(0,.2f,.101f),new Vector3(.14f,.09f,.003f),"317E79");Greens(p,new Vector3(-.06f,.33f,0),false);Bun(p,new Vector3(.07f,.35f,0));
            } else if(kind=="Bun")Bun(p,Vector3.zero);
            else if(kind=="RawProtein") {var meat=Box("Raw cut of protein",p,new Vector3(0,.045f,0),new Vector3(.24f,.085f,.18f),"C96C64");meat.transform.localRotation=Quaternion.Euler(0,15,0);for(int i=0;i<3;i++){var fat=Box("Raw marbling",p,new Vector3((i-1)*.057f,.09f,0),new Vector3(.014f,.004f,.13f),"F0BC9D");fat.transform.localRotation=Quaternion.Euler(0,-22,0);}}
            else Patty(p,Vector3.zero,kind);
            return ArtOverrides.Apply(root,"Items",kind);
        }
        static void PantryShelf(Transform parent,string subId,float x,float yLow,float yHigh) {
            var zone=G("Pantry shelf "+subId,parent,new Vector3(x,(yLow+yHigh)*.5f,.44f));
            var box=zone.AddComponent<BoxCollider>(); box.size=new Vector3(.85f,yHigh-yLow,.42f);
            var target=zone.AddComponent<RestaurantTarget>(); target.Kind="Furniture"; target.SubId=subId;
        }
        static void Crate(Transform p,Vector3 position,string contents,float scale=1) {
            var root=G("Slatted ingredient crate",p,position);root.transform.localScale=Vector3.one*scale;p=root.transform;
            Box("Crate base",p,new Vector3(0,.025f,0),new Vector3(.79f,.05f,.65f),"895343");
            for(int n=0;n<2;n++)for(int side=-1;side<=1;side+=2){Box("Crate long slat",p,new Vector3(0,.09f+n*.12f,side*.31f),new Vector3(.8f,.095f,.035f),"AD7851");Box("Crate end slat",p,new Vector3(side*.38f,.09f+n*.12f,0),new Vector3(.04f,.095f,.62f),"AD7851");}
            for(int i=0;i<3;i++){var food=CreateItem(contents=="protein"?"RawProtein":contents=="buns"?"Bun":"RawGreens",p);food.transform.localPosition=new Vector3(-.23f+i*.23f,.13f,0);}
        }
        // Market display crate for Milo's shop: a big crate heaped with one kind of ingredient.
        public static Material Material(string hex) => M(hex);
        public static GameObject SupplyCrate(Transform parent,string contents) {
            var root=G(contents=="protein"?"Meat crate":"Produce crate",parent);
            Box("Crate stand",root.transform,new Vector3(0,.45f,0),new Vector3(1.1f,.9f,.9f),"317E79");
            Crate(root.transform,new Vector3(0,.9f,0),contents,1.25f);
            Crate(root.transform,new Vector3(0,1.08f,.05f),contents=="protein"?"protein":"buns",1.0f);
            if(contents!="protein")Crate(root.transform,new Vector3(0,1.26f,-.05f),"greens",.8f);
            return root;
        }
        public static void DecorateStreet(Transform parent) {
            var root=G("Original market and restaurant details",parent);
            bool walkIn=GameObject.Find("Milo's walk-in");   // Milo works inside his store in the city build: no street stall props.
            // Produce rests above the existing supplier crates; the pavement and counter interaction remain clear.
            if(!walkIn)for(int i=0;i<3;i++){var food=CreateItem(i==1?"RawProtein":"RawGreens",root.transform);food.transform.localPosition=new Vector3(-13+i,1.59f,9);food.transform.localScale=Vector3.one*1.8f;}
            if(!walkIn){Crate(root.transform,new Vector3(-14.75f,.05f,10.35f),"greens");Crate(root.transform,new Vector3(-14.75f,.36f,10.35f),"buns");}
            // Narrow herb planter sits against the restaurant facade, outside its doorway.
            var herbs=G("Window herb trough",root.transform,new Vector3(-13.2f,1.55f,-8.82f));
            Box("Terracotta trough",herbs.transform,Vector3.zero,new Vector3(1.5f,.24f,.32f),"D96555");Box("Trough soil",herbs.transform,new Vector3(0,.13f,0),new Vector3(1.35f,.015f,.23f),"514234");
            for(int i=0;i<5;i++)Greens(herbs.transform,new Vector3(-.52f+i*.26f,.14f,0),false);
        }
    }
}

