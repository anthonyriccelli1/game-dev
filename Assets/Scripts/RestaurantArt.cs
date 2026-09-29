using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    /// <summary>Original runtime art. Create these models at runtime: generated meshes are intentionally not editor assets.</summary>
    public static partial class RestaurantArt {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Color Cream = C("F5DBAA"), Coral = C("D96555"), Teal = C("317E79"), Ink = C("233B46"), Brass = C("CA9B53"), Wood = C("895343"), White = C("FFF1D1"), Green = C("73A566");
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static GameObject Group(string n, Transform parent, Vector3 pos = default(Vector3)) { var g = new GameObject(n); g.transform.SetParent(parent, false); g.transform.localPosition = pos; return g; }
        static Material Mat(string name, Color c, float metal = 0, bool glow = false, string texture = null) {
            if (materials.TryGetValue(name, out var existing) && existing) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var m = new Material(shader) { name = "Original_" + name, color = c };
            m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", metal > .1f ? .36f : .18f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.8f); }
            if (texture != null) {
                m.mainTexture = Texture(texture);
                float repeats = texture == "checker" ? 3 : texture == "worn" ? 4 : texture == "wallpaper" ? 2 : 1;
                m.mainTextureScale = new Vector2(repeats, repeats);
            }
            materials[name] = m; return m;
        }
        static Texture2D Texture(string kind) {
            const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGB24, true) { name = "Original_" + kind, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var pixels = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) {
                float noise = Mathf.PerlinNoise(x * .19f + 42, y * .19f + 17), v = .9f + noise * .1f;
                if (kind == "checker") v = ((x / 64 + y / 64) % 2 == 0) ? .94f : .32f;
                if (kind == "worn") {
                    // Old olive linoleum: subtle mismatched squares, broad stains, fine grout; no checker motif.
                    float tile = Mathf.PerlinNoise((x / 64) * 1.9f + 11, (y / 64) * 2.1f + 9);
                    v = .67f + tile * .18f + noise * .025f;
                    v -= Mathf.Max(0, .54f - Mathf.PerlinNoise(x * .021f + 4, y * .022f + 9)) * .36f;
                    if (x % 64 < 2 || y % 64 < 2) v *= .86f;
                }
                if (kind == "wood") { v = .7f + .22f * Mathf.PerlinNoise(x * .0125f, y * .19f); if (x % 64 < 2 || (y + ((x / 64) % 2) * 128) % 256 < 2) v = .46f; }
                if (kind == "wallpaper") { float stripe = x % 64 < 6 ? .84f : .98f; v = stripe * (.9f + noise * .1f); if (Mathf.Abs((x % 64) - 32) + Mathf.Abs((y % 64) - 32) < 7) v *= .86f; }
                if (kind == "fabric") v = .94f + noise * .06f;
                pixels[y * N + x] = new Color(v, v, v);
            }
            t.SetPixels(pixels); t.Apply(); return t;
        }
        static Material Solid(Color c) { return Mat(ColorUtility.ToHtmlStringRGB(c), c); }
        static GameObject Shape(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Material mat) {
            var g = Group(name, parent, pos); g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat; return g;
        }
        static Mesh Profile(string key, float[] ys, float[] radii, int segments = 10, bool flat = true) {
            if (meshes.TryGetValue(key, out var found) && found) return found;
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int y = 0; y < ys.Length - 1; y++) for (int i = 0; i < segments; i++) {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                int start = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a) * radii[y], ys[y], Mathf.Sin(a) * radii[y]));
                verts.Add(new Vector3(Mathf.Cos(a) * radii[y + 1], ys[y + 1], Mathf.Sin(a) * radii[y + 1]));
                verts.Add(new Vector3(Mathf.Cos(b) * radii[y + 1], ys[y + 1], Mathf.Sin(b) * radii[y + 1]));
                verts.Add(new Vector3(Mathf.Cos(b) * radii[y], ys[y], Mathf.Sin(b) * radii[y]));
                uv.Add(new Vector2((float)i / segments, ys[y])); uv.Add(new Vector2((float)i / segments, ys[y + 1])); uv.Add(new Vector2((float)(i + 1) / segments, ys[y + 1])); uv.Add(new Vector2((float)(i + 1) / segments, ys[y]));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            var mesh = new Mesh { name = "Original_" + key }; mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes[key] = mesh; return mesh;
        }
        static Mesh RoundedBox() {
            const string key = "beveled_block"; if (meshes.TryGetValue(key, out var m) && m) return m;
            float b = .09f; var ys = new[] { -.5f, -.5f + b, .5f - b, .5f };
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            Vector2[] outline = { new Vector2(-.5f + b, -.5f), new Vector2(.5f - b, -.5f), new Vector2(.5f, -.5f + b), new Vector2(.5f, .5f - b), new Vector2(.5f - b, .5f), new Vector2(-.5f + b, .5f), new Vector2(-.5f, .5f - b), new Vector2(-.5f, -.5f + b) };
            for (int layer = 0; layer < 3; layer++) for (int i = 0; i < 8; i++) {
                int j = (i + 1) % 8; float s0 = layer == 0 ? .83f : 1, s1 = layer == 2 ? .83f : 1;
                Quad(verts, uv, tris, new Vector3(outline[i].x*s0,ys[layer],outline[i].y*s0),new Vector3(outline[i].x*s1,ys[layer+1],outline[i].y*s1),new Vector3(outline[j].x*s1,ys[layer+1],outline[j].y*s1),new Vector3(outline[j].x*s0,ys[layer],outline[j].y*s0));
            }
            for (int i = 0; i < 8; i++) {
                int j = (i + 1) % 8; int s = verts.Count;
                verts.Add(new Vector3(0,.5f,0)); verts.Add(new Vector3(outline[j].x*.83f,.5f,outline[j].y*.83f)); verts.Add(new Vector3(outline[i].x*.83f,.5f,outline[i].y*.83f));
                uv.Add(new Vector2(.5f,.5f)); uv.Add(outline[j] + Vector2.one*.5f); uv.Add(outline[i] + Vector2.one*.5f); tris.Add(s);tris.Add(s+1);tris.Add(s+2);
                s=verts.Count; verts.Add(new Vector3(0,-.5f,0));verts.Add(new Vector3(outline[i].x*.83f,-.5f,outline[i].y*.83f));verts.Add(new Vector3(outline[j].x*.83f,-.5f,outline[j].y*.83f));
                uv.Add(new Vector2(.5f,.5f)); uv.Add(outline[i]+Vector2.one*.5f);uv.Add(outline[j]+Vector2.one*.5f);tris.Add(s);tris.Add(s+1);tris.Add(s+2);
            }
            m=new Mesh {name="Original_beveled_block"};m.SetVertices(verts);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();meshes[key]=m;return m;
        }
        static void Quad(List<Vector3> v,List<Vector2> u,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d) {
            int s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);u.Add(Vector2.zero);u.Add(Vector2.up);u.Add(Vector2.one);u.Add(Vector2.right);t.Add(s);t.Add(s+1);t.Add(s+2);t.Add(s);t.Add(s+2);t.Add(s+3);
        }
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Material m,bool collision=false) {
            var g=Shape(name,p,RoundedBox(),pos,scale,m);if(collision)g.AddComponent<BoxCollider>();return g;
        }
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Color c,bool collision=false) {return Box(n,p,pos,scale,Solid(c),collision);}
        static GameObject Slab(string n,Transform p,Vector3 pos,Vector3 scale,Material material,bool collision=false) {
            if(!meshes.TryGetValue("flat_architecture",out var mesh)||!mesh) {
                var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
                Quad(v,uv,tris,new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f));
                Quad(v,uv,tris,new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,-.5f,-.5f));
                Quad(v,uv,tris,new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f));
                Quad(v,uv,tris,new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f));
                Quad(v,uv,tris,new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f));
                Quad(v,uv,tris,new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f));
                mesh=new Mesh{name="Original_flat_architecture"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes["flat_architecture"]=mesh;
            }
            var g=Shape(n,p,mesh,pos,scale,material);if(collision)g.AddComponent<BoxCollider>();return g;
        }
        static GameObject Scar(string name,Transform parent,Vector3 pos,Vector2 size,Color color,bool floor) {
            var vertices=new List<Vector3>{Vector3.zero};var uvs=new List<Vector2>{new Vector2(.5f,.5f)};var tri=new List<int>();
            for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9,r=i%3==0?.39f:i%3==1?.5f:.44f;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));uvs.Add(new Vector2(Mathf.Cos(a)*r+.5f,Mathf.Sin(a)*r+.5f));}
            for(int i=0;i<9;i++){tri.Add(0);tri.Add(i+1);tri.Add((i+1)%9+1);}
            var mesh=new Mesh{name="Original_plaster_scar"};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var scar=Shape(name,parent,mesh,pos,new Vector3(size.x,size.y,1),Solid(color));if(floor)scar.transform.localRotation=Quaternion.Euler(-90,0,0);return scar;
        }
        static GameObject Round(string n,Transform p,Vector3 pos,Vector3 scale,Color c) {return Shape(n,p,Profile("gem",new[]{-.5f,-.35f,0,.35f,.5f},new[]{0f,.38f,.5f,.38f,0f},10),pos,scale,Solid(c));}
        static GameObject Lathe(string n,Transform p,Vector3 pos,float[] ys,float[] rs,Material m,int segments=12) {return Shape(n,p,Profile(n,ys,rs,segments),pos,Vector3.one,m);}
        static void Rod(string n,Transform p,Vector3 a,Vector3 b,float width,Color c) {var g=Box(n,p,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),c);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void LightAt(string n,Transform p,Vector3 pos,Color c,float intensity,float range) {var g=Group(n,p,pos);var light=g.AddComponent<Light>();light.type=LightType.Point;light.color=c;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;}
        static void Label(string value,Transform p,Vector3 pos,float size,Color color) {
            var g=Group("Sign_"+value,p,pos);g.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=g.AddComponent<TextMesh>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=60;text.characterSize=size*.25f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            var shader=Shader.Find("RestaurantCity/WorldText");if(shader){var m=new Material(shader);m.mainTexture=text.font.material.mainTexture;g.GetComponent<MeshRenderer>().sharedMaterial=m;var sync=g.AddComponent<WorldTextFont>();sync.Font=text.font;sync.Material=m;}
        }
        public static string RestaurantName = "THE ODD TABLE";
        public static GameObject BuildRoom(Transform parent) {
            var room=Group("RestaurantInterior",parent);var p=room.transform;
            var wallpaper=ShabbyWall();var worn=ShabbyFloor();var brick=Mat("FacadeBrick",C("A36553"));
            Slab("FloorFinish",p,new Vector3(-10,.0f,-15.5f),new Vector3(13,.12f,13),worn,true);
            Slab("WallFinish_Back",p,new Vector3(-10,1.9f,-22),new Vector3(13,3.8f,.25f),wallpaper,true);
            Slab("WallFinish_Left",p,new Vector3(-16.5f,1.9f,-15.5f),new Vector3(.25f,3.8f,13),wallpaper,true);
            Slab("WallFinish_Right",p,new Vector3(-3.5f,1.9f,-15.5f),new Vector3(.25f,3.8f,13),wallpaper,true);
            var wallWear=Group("ShabbyWallWear",p).transform;var floorWear=Group("ShabbyFloorWear",p).transform;
            foreach(var entry in new[]{new Vector3(-14.2f,.8f,-21.86f),new Vector3(-6.2f,2.7f,-21.86f),new Vector3(-12.7f,2.9f,-21.86f)}) {
                Scar("PeelingPaper",wallWear,entry,new Vector2(1.1f,.66f),C("AFA38D"),false);
                Scar("ExposedPlaster",wallWear,entry+new Vector3(.08f,-.03f,.005f),new Vector2(.79f,.47f),C("C9BFAB"),false);
            }
            foreach(var entry in new[]{new Vector3(-16.36f,.74f,-12.8f),new Vector3(-16.36f,2.6f,-18f),new Vector3(-3.64f,1.73f,-14f)}) {
                var scar=Scar("WallWearPatch",wallWear,entry,new Vector2(1.3f,.62f),C("B6AA95"),false);scar.transform.localRotation=Quaternion.Euler(0,entry.x<-10?90:-90,0);
            }
            foreach(var entry in new[]{new Vector3(-10.3f,.069f,-10.8f),new Vector3(-12.8f,.069f,-17.6f),new Vector3(-6.1f,.069f,-20.1f),new Vector3(-14.6f,.069f,-13.7f)}) {
                Scar("OldFloorStain",floorWear,entry,new Vector2(1.4f,.7f),C("938769"),true);
                Scar("StainWornCenter",floorWear,entry+new Vector3(.14f,.002f,.05f),new Vector2(.92f,.42f),C("A69B7B"),true);
            }
            for(int i=0;i<7;i++){var scuff=Box("FloorScuff",floorWear,new Vector3(-10.5f+(i%3)*.39f,.074f,-10.4f-i*.34f),new Vector3(.28f,.006f,.035f),C("8D846C"));scuff.transform.localRotation=Quaternion.Euler(0,i*37,0);}
            // The entrance is 2.8m wide and has no invisible door collider.
            Box("FrontLeft",p,new Vector3(-14,1.9f,-9),new Vector3(5,3.8f,.3f),brick,true);Box("FrontRight",p,new Vector3(-6,1.9f,-9),new Vector3(5,3.8f,.3f),brick,true);
            Box("DoorLintel",p,new Vector3(-10,3.28f,-9),new Vector3(3,1.04f,.3f),brick,true);
            Box("Ceiling",p,new Vector3(-10,3.91f,-15.5f),new Vector3(13,.16f,13),C("514F47"));
            foreach(float x in new[]{-16.33f,-3.67f}) {Box("Baseboard",p,new Vector3(x,.19f,-15.5f),new Vector3(.12f,.28f,12.7f),Ink);Box("ChairRail",p,new Vector3(x,1.12f,-15.5f),new Vector3(.09f,.1f,12.7f),Wood);Box("Crown",p,new Vector3(x,3.64f,-15.5f),new Vector3(.12f,.2f,12.7f),Cream);}
            Box("BackBaseboard",p,new Vector3(-10,.19f,-21.84f),new Vector3(12.7f,.28f,.12f),Ink);Box("BackRail",p,new Vector3(-10,1.12f,-21.83f),new Vector3(12.7f,.1f,.08f),Wood);
            foreach(float x in new[]{-13.9f,-6.1f}) {
                Box("WindowRecess",p,new Vector3(x,1.9f,-8.805f),new Vector3(3.5f,2.18f,.06f),Ink);
                Box("WindowAmber",p,new Vector3(x,1.92f,-8.755f),new Vector3(3.24f,1.92f,.05f),Mat("AmberGlass",C("DBAD65"),.15f,true));
                Box("WindowCross",p,new Vector3(x,1.9f,-8.7f),new Vector3(.1f,2.05f,.08f),Cream);Box("WindowCross",p,new Vector3(x,1.9f,-8.7f),new Vector3(3.35f,.1f,.08f),Cream);
                Box("WindowSill",p,new Vector3(x,.8f,-8.7f),new Vector3(3.65f,.16f,.35f),Cream);
                Box("InsideWindowFrame",p,new Vector3(x,2,-9.19f),new Vector3(3.6f,2.2f,.06f),Wood);
                Box("InsideWindow",p,new Vector3(x,2,-9.23f),new Vector3(3.35f,1.96f,.05f),Mat("BlueGlass",C("83B6B1"),.1f,true));
                Box("InsideMullion",p,new Vector3(x,2,-9.28f),new Vector3(.1f,2.02f,.06f),Cream);
            }
            // A modest name on the fascia leaves the shop windows and the pack's brickwork in view.
            Box("HeaderBacking",p,new Vector3(-10,3.48f,-8.73f),new Vector3(4.8f,.48f,.12f),Teal);
            Label(RestaurantName,p,new Vector3(-10,3.48f,-8.62f),.22f,White);
            // Layered storefront, a shallow parapet, brick courses, pilasters, and period lamps.
            Box("FacadeCornice",p,new Vector3(-10,3.95f,-8.96f),new Vector3(13.35f,.2f,.7f),Cream);
            Box("FacadeParapet",p,new Vector3(-10,4.17f,-9.1f),new Vector3(13.15f,.34f,.28f),Wood);
            Box("FacadeCoping",p,new Vector3(-10,4.36f,-9.1f),new Vector3(13.35f,.1f,.42f),Cream);
            foreach(float x in new[]{-16.28f,-11.67f,-8.33f,-3.72f}) {
                Box("FacadePilaster",p,new Vector3(x,1.63f,-8.73f),new Vector3(.27f,3.2f,.33f),C("C49572"));
                Box("PilasterFoot",p,new Vector3(x,.25f,-8.67f),new Vector3(.42f,.4f,.45f),Cream);
                Box("PilasterCapital",p,new Vector3(x,3.08f,-8.65f),new Vector3(.44f,.17f,.48f),Cream);
            }
            foreach(float x in new[]{-13.9f,-6.1f}) {
                Box("FacadeLowerPanel",p,new Vector3(x,.42f,-8.72f),new Vector3(3.61f,.55f,.19f),Teal);
                Box("FacadePanelInset",p,new Vector3(x,.42f,-8.61f),new Vector3(3.25f,.3f,.05f),C("28645F"));
                Rod("SconceArm",p,new Vector3(x,2.92f,-8.66f),new Vector3(x,2.92f,-8.24f),.055f,Brass);
                Lathe("FacadeShade",p,new Vector3(x,2.81f,-8.24f),new[]{0f,.06f,.2f},new[]{.25f,.21f,.08f},Solid(Ink));
                Round("FacadeBulb",p,new Vector3(x,2.8f,-8.24f),Vector3.one*.13f,Cream);LightAt("FacadeSconce",p,new Vector3(x,2.71f,-8.18f),C("FFE1AB"),.8f,4);
            }
            foreach(float edge in new[]{-16.05f,-4.02f})for(int row=0;row<6;row++)Box("ExposedBrick",p,new Vector3(edge+(row%2)*.07f,.92f+row*.31f,-8.817f),new Vector3(.44f,.14f,.035f),C("BF8066"));
            Rod("FacadeDownpipe",p,new Vector3(-16.66f,.15f,-8.93f),new Vector3(-16.66f,3.89f,-8.93f),.08f,Ink);
            foreach(float x in new[]{-11.53f,-8.47f})Box("DoorTrim",p,new Vector3(x,1.5f,-8.79f),new Vector3(.14f,3,.25f),Brass);
            Box("Threshold",p,new Vector3(-10,.06f,-8.94f),new Vector3(2.8f,.05f,.6f),Brass);
            var awning=Group("UpgradeAwning",p);
            for(int i=0;i<14;i++){var panel=Box("CanvasStripe",awning.transform,new Vector3(-15.5f+i*.85f,3.07f,-8.19f),new Vector3(.84f,.1f,1.3f),i%2==0?Coral:Cream);panel.transform.localRotation=Quaternion.Euler(-12,0,0);Box("ScallopedHem",awning.transform,new Vector3(-15.5f+i*.85f,2.89f,-7.58f),new Vector3(.84f,.25f,.07f),i%2==0?Coral:Cream);}awning.SetActive(false);
            var neon=Group("UpgradeNeon",p);Box("NeonPlate",neon.transform,new Vector3(-10,4.28f,-8.8f),new Vector3(5.2f,.65f,.16f),Ink);Label("ODD FOOD / GOOD MOOD",neon.transform,new Vector3(-10,4.28f,-8.65f),.095f,C("FFCC86"));LightAt("SignGlow",neon.transform,new Vector3(-10,4,-8),Coral,2,6);neon.SetActive(false);
            // Exposed pipes, decorative rafters and warm pools make the starter room an actual place.
            foreach(float z in new[]{-12f,-17f,-21f}) {Box("Rafter",p,new Vector3(-10,3.62f,z),new Vector3(12.6f,.22f,.18f),Wood);LightAt("WarmRoomLight",p,new Vector3(-10,3.2f,z),C("FFE1AD"),1.45f,8);}
            Rod("CopperWaterPipe",p,new Vector3(-16.16f,.3f,-21.64f),new Vector3(-16.16f,3.5f,-21.64f),.075f,Brass);
            Rod("CopperCeilingPipe",p,new Vector3(-16.16f,3.45f,-21.64f),new Vector3(-4,3.45f,-21.64f),.075f,Brass);
            Box("OldMenuBoard",p,new Vector3(-10,2.45f,-21.72f),new Vector3(2.8f,1.3f,.12f),Wood);Box("Slate",p,new Vector3(-10,2.45f,-21.62f),new Vector3(2.55f,1.09f,.05f),Ink);
            Label("TODAY AT "+RestaurantName+"\nGOOD FOOD. ODD COMPANY.",p,new Vector3(-10,2.45f,-21.57f),.069f,Cream);
            ApplyPackShell(room);
            return room;
        }
        internal static Material ShabbyWall()=>PackFinish("shabby_wall")??Mat("OldWallpaper",C("C9BA95"),0,false,"wallpaper");
        static Material ShabbyFloor()=>PackFinish("shabby_floor")??Mat("OldTile",C("B6AA87"),0,false,"worn");
        // With the POLYGON Shops pack: its storefront, mouldings and awnings replace the code-built
        // look. Our own colliders (walls, doorway) stay exactly where they were; only renderers are swapped.
        static readonly HashSet<string> packShellReplaces=new HashSet<string>{"FrontLeft","FrontRight","DoorLintel","WindowRecess","WindowAmber","WindowCross","WindowSill",
            "InsideWindowFrame","InsideWindow","InsideMullion","FacadeCornice","FacadeParapet","FacadeCoping","FacadePilaster","PilasterFoot","PilasterCapital",
            "FacadeLowerPanel","FacadePanelInset","ExposedBrick","DoorTrim","Baseboard","ChairRail","Crown","BackBaseboard","BackRail",
            "HeaderBacking","SconceArm","FacadeShade","FacadeBulb"};
        static void ApplyPackShell(GameObject room) {
            var facade=ArtOverrides.Find("Shell","facade");if(!facade)return;
            var p=room.transform;
            foreach(Transform child in p) if(packShellReplaces.Contains(child.name)){var r=child.GetComponent<Renderer>();if(r)r.enabled=false;}
            PackPiece(facade,p,"Pack storefront");
            // Replace the flat placeholder with individually colored, dimensional Shops-pack letters.
            var sign=p.Find("Sign_"+RestaurantName);
            var packSign=ArtOverrides.Find("Shell","storefront_sign");
            if(packSign){if(sign)sign.gameObject.SetActive(false);PackPiece(packSign,p,"Pack storefront sign");}
            // The old code-built menu board is illegible beside the pack architecture. Players can
            // furnish the wall with the catalog's proper menu screen or their own decorations.
            foreach(Transform child in p) if(child.name=="OldMenuBoard"||child.name=="Slate"||child.name.StartsWith("Sign_TODAY AT "))child.gameObject.SetActive(false);
            var trim=ArtOverrides.Find("Shell","interior_trim");if(trim)PackPiece(trim,p,"Pack interior trim");
            var awning=p.Find("UpgradeAwning");var packAwning=ArtOverrides.Find("Shell","awning");
            if(awning&&packAwning){foreach(var r in awning.GetComponentsInChildren<Renderer>(true))r.enabled=false;PackPiece(packAwning,awning,"Pack awning");}
            // The neon upgrade becomes a rooftop sign above the cornice.
            var neon=p.Find("UpgradeNeon");if(neon)neon.localPosition=new Vector3(0,.72f,0);
        }
        static void PackPiece(GameObject prefab,Transform parent,string name) {
            var go=Object.Instantiate(prefab,parent,false);go.name=name;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        }
        public static void UpdateFinishes(GameObject room,string wallId,string floorId,bool awning,bool neon) {
            if(!room)return;
            bool shabbyWall=string.IsNullOrEmpty(wallId)||wallId=="wall_shabby",shabbyFloor=string.IsNullOrEmpty(floorId)||floorId=="floor_shabby";
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>(true)) {
                if(r.name.StartsWith("WallFinish")) {
                    r.sharedMaterial=shabbyWall?ShabbyWall():FinishMaterial(wallId)??FinishMaterial("wall_cream");
                    if(!shabbyWall||PackFinish("shabby_wall"))FinishBaseUV(r,false);
                    else if(meshes.TryGetValue("flat_architecture",out var originalWall))r.GetComponent<MeshFilter>().sharedMesh=originalWall;
                }
                if(r.name=="FloorFinish") {
                    r.sharedMaterial=shabbyFloor?ShabbyFloor():FinishMaterial(floorId)??FinishMaterial("floor_checker");
                    if(!shabbyFloor||PackFinish("shabby_floor"))FinishBaseUV(r,true);
                    else if(meshes.TryGetValue("flat_architecture",out var originalFloor))r.GetComponent<MeshFilter>().sharedMesh=originalFloor;
                }
            }
            var wallWear=room.transform.Find("ShabbyWallWear");if(wallWear)wallWear.gameObject.SetActive(shabbyWall);
            var floorWear=room.transform.Find("ShabbyFloorWear");if(floorWear)floorWear.gameObject.SetActive(shabbyFloor);
            var a=room.transform.Find("UpgradeAwning");if(a)a.gameObject.SetActive(awning);var n=room.transform.Find("UpgradeNeon");if(n)n.gameObject.SetActive(neon);
        }
        static void FridgeShelf(Transform p,string subId,float yLow,float yHigh) {
            var zone=Group("Pantry shelf "+subId,p,new Vector3(0,(yLow+yHigh)*.5f,.32f));
            var box=zone.AddComponent<BoxCollider>();box.size=new Vector3(.8f,yHigh-yLow,.4f);
            var target=zone.AddComponent<RestaurantTarget>();target.Kind="Furniture";target.SubId=subId;
        }
        static void Feet(Transform p,float width,float depth,float height,Color c) {foreach(float x in new[]{-width*.4f,width*.4f})foreach(float z in new[]{-depth*.4f,depth*.4f})Box("TaperedFoot",p,new Vector3(x,height*.5f,z),new Vector3(.1f,height,.1f),c);}
        static void Seat(Transform parent,int index,Vector3 pos,float yaw) {var seat=Group("Seat_"+index,parent,pos);seat.transform.localRotation=Quaternion.Euler(0,yaw,0);}
        static void Chair(Transform p,Vector3 pos,float yaw,bool stool=false) {
            var c=Group("Chair",p,pos);c.transform.localRotation=Quaternion.Euler(0,yaw,0);Feet(c.transform,.56f,.56f,.43f,Wood);
            Box("UpholsteredSeat",c.transform,new Vector3(0,.47f,0),new Vector3(.65f,.16f,.61f),Mat("TealFabric",Teal,0,false,"fabric"));
            if(!stool){Rod("BackUpright",c.transform,new Vector3(-.25f,.4f,-.23f),new Vector3(-.25f,.98f,-.3f),.07f,Wood);Rod("BackUpright",c.transform,new Vector3(.25f,.4f,-.23f),new Vector3(.25f,.98f,-.3f),.07f,Wood);Box("CurvedBack",c.transform,new Vector3(0,.88f,-.28f),new Vector3(.65f,.35f,.11f),Teal);}
        }
        static void Table(Transform p,Vector3 pos,float width,float depth) {
            var t=Group("DiningTable",p,pos);Lathe("TurnedTableLeg",t.transform,Vector3.zero,new[]{0f,.06f,.14f,.22f,.57f,.7f},new[]{.32f,.35f,.16f,.08f,.09f,.23f},Solid(Ink));
            Box("OakTableTop",t.transform,new Vector3(0,.76f,0),new Vector3(width,.13f,depth),Mat("DiningOak",C("C99B69"),0,false,"wood"));
            Box("Napkin",t.transform,new Vector3(-width*.25f,.835f,0),new Vector3(.22f,.012f,.25f),Cream);
            Lathe("BudVase",t.transform,new Vector3(0,.83f,0),new[]{0f,.1f,.2f,.24f},new[]{.075f,.09f,.035f,.04f},Solid(Coral));Rod("FlowerStem",t.transform,new Vector3(0,1,0),new Vector3(.02f,1.15f,0),.015f,Green);Round("Flower",t.transform,new Vector3(.02f,1.16f,0),Vector3.one*.1f,Brass);
        }
        public static GameObject CreateFurniture(string id,Transform parent) {
            if(FinishCatalog.Find(id)!=null) {
                var sample=Group("Furniture_"+id,parent);
                Slab("FinishSample",sample.transform,new Vector3(0,.06f,0),new Vector3(.9f,.12f,.9f),FinishMaterial(id));
                return sample;
            }
            var g=Group("Furniture_"+id,parent);var p=g.transform;float w=1,d=1,h=1;bool collide=true;Vector3? elevatedColliderCenter=null;Vector3? elevatedColliderSize=null;
            var steel=Mat("EnamelSteel",C("AAC0BE"),.55f);var dark=Solid(Ink);var wood=Mat("CounterOak",C("BB8E61"),0,false,"wood");
            switch(id) {
            case "prep_bench":
                w=2;Feet(p,1.8f,.82f,.83f,Ink);Box("Cabinet",p,new Vector3(0,.49f,0),new Vector3(1.7f,.58f,.73f),Teal);Box("ButcherBlock",p,new Vector3(0,.91f,0),new Vector3(1.93f,.16f,.92f),wood);
                for(int i=0;i<3;i++){Box("Drawer",p,new Vector3(-.57f+i*.57f,.59f,.38f),new Vector3(.5f,.36f,.05f),Cream);Box("BrassPull",p,new Vector3(-.57f+i*.57f,.63f,.425f),new Vector3(.2f,.045f,.035f),Brass);}
                Box("ChoppingBoard",p,new Vector3(-.42f,1.01f,0),new Vector3(.6f,.04f,.4f),C("D7B97C"));Box("KnifeBlade",p,new Vector3(-.35f,1.04f,.04f),new Vector3(.28f,.014f,.055f),steel);Box("KnifeHandle",p,new Vector3(-.14f,1.04f,.04f),new Vector3(.15f,.027f,.06f),Ink);
                Round("Cabbage",p,new Vector3(.51f,1.11f,0),Vector3.one*.28f,Green);break;
            case "grill":
                w=2;Feet(p,1.7f,.74f,.78f,Ink);Box("Firebox",p,new Vector3(0,.72f,0),new Vector3(1.82f,.48f,.8f),Ink);Box("FrontEnamel",p,new Vector3(0,.72f,.42f),new Vector3(1.8f,.29f,.06f),Coral);
                Box("FireGlow",p,new Vector3(0,.966f,0),new Vector3(1.6f,.015f,.57f),Mat("Coals",C("F49348"),0,true));for(int i=0;i<13;i++)Box("IronGrate",p,new Vector3(-.77f+i*.128f,.99f,0),new Vector3(.035f,.04f,.64f),Ink);
                for(int i=0;i<4;i++)Round("Dial",p,new Vector3(-.6f+i*.4f,.75f,.47f),new Vector3(.13f,.13f,.07f),Brass);Box("GrillHood",p,new Vector3(0,1.13f,-.35f),new Vector3(1.8f,.3f,.2f),Teal);break;
            case "stove":
                h=1.15f;Feet(p,.86f,.82f,.2f,Ink);Box("StoveBody",p,new Vector3(0,.55f,0),new Vector3(.88f,.8f,.82f),Cream);Box("Cooktop",p,new Vector3(0,.97f,0),new Vector3(.94f,.09f,.91f),steel);
                foreach(float x in new[]{-.23f,.23f})foreach(float z in new[]{-.23f,.23f})Lathe("Burner",p,new Vector3(x,1.02f,z),new[]{0f,.03f,.04f},new[]{.15f,.15f,0},dark);
                Lathe("StockPot",p,new Vector3(-.23f,1.05f,-.23f),new[]{0f,.03f,.23f,.25f},new[]{.16f,.2f,.2f,.19f},steel);Round("PotLid",p,new Vector3(-.23f,1.31f,-.23f),new Vector3(.4f,.05f,.4f),Brass);
                Box("OvenGlass",p,new Vector3(0,.53f,.435f),new Vector3(.66f,.4f,.03f),Ink);Box("Handle",p,new Vector3(0,.79f,.48f),new Vector3(.57f,.055f,.07f),Brass);break;
            case "oven":
                w=2;h=1.75f;Feet(p,1.75f,.78f,.24f,Ink);Box("OvenBody",p,new Vector3(0,.95f,0),new Vector3(1.8f,1.45f,.84f),Coral);Box("SteelCrown",p,new Vector3(0,1.7f,0),new Vector3(1.92f,.12f,.93f),steel);
                foreach(float y in new[]{.61f,1.23f}){Box("OvenFrame",p,new Vector3(-.1f,y,.441f),new Vector3(1.4f,.53f,.055f),Brass);Box("OvenWindow",p,new Vector3(-.1f,y,.48f),new Vector3(1.15f,.32f,.04f),Ink);Box("OvenBar",p,new Vector3(-.1f,y+.2f,.57f),new Vector3(.91f,.06f,.07f),Cream);Round("Dial",p,new Vector3(.74f,y,.47f),new Vector3(.13f,.13f,.08f),Ink);}break;
            case "fridge":
                h=2;Box("RetroFridge",p,new Vector3(0,.99f,0),new Vector3(.91f,1.96f,.83f),Teal);Box("FridgeDoor",p,new Vector3(0,.78f,.43f),new Vector3(.84f,1.35f,.08f),Cream);Box("FreezerDoor",p,new Vector3(0,1.72f,.43f),new Vector3(.84f,.45f,.08f),Cream);Box("Handle",p,new Vector3(.3f,1.05f,.53f),new Vector3(.05f,.45f,.06f),Brass);Box("Magnet",p,new Vector3(-.16f,1.14f,.49f),new Vector3(.21f,.27f,.01f),Coral);Label("COLD",p,new Vector3(0,1.74f,.49f),.052f,Teal);
                // Aimable cold shelves (patties, greens, soup veg) at the glass-door fridge's shelf heights; stock is drawn by PantryDisplay.
                FridgeShelf(p,"protein",.56f,.9f);FridgeShelf(p,"greens",.9f,1.24f);FridgeShelf(p,"soup",1.24f,1.6f);break;
            case "stool_pair":case "cafe_table":case "bistro_table":case "patio_table":
                w=2;d=2;Table(p,Vector3.zero,.82f,.77f);Chair(p,new Vector3(0,0,-.65f),0,id=="stool_pair");Chair(p,new Vector3(0,0,.65f),180,id=="stool_pair");Seat(p,0,new Vector3(0,0,-.65f),0);Seat(p,1,new Vector3(0,0,.65f),180);break;
            case "booth_teal":case "booth_coral":
                w=3;d=2;h=1.22f;Color upholstery=id=="booth_teal"?Teal:Coral;
                foreach(float z in new[]{-.73f,.73f}){Box("BoothPlinth",p,new Vector3(0,.23f,z),new Vector3(2.7f,.4f,.49f),Ink);Box("SeatCushion",p,new Vector3(0,.51f,z),new Vector3(2.74f,.22f,.61f),Mat(id+"Velvet",upholstery,0,false,"fabric"));Box("CurvedBack",p,new Vector3(0,.91f,z+Mathf.Sign(z)*.17f),new Vector3(2.83f,.64f,.22f),upholstery);for(int i=0;i<7;i++)Box("CushionPleat",p,new Vector3(-1.14f+i*.38f,.9f,z+Mathf.Sign(z)*.039f),new Vector3(.035f,.49f,.014f),Cream);}
                Table(p,Vector3.zero,2.3f,.59f);for(int i=0;i<4;i++)Seat(p,i,new Vector3(i%2==0?-.65f:.65f,0,i<2?-.66f:.66f),i<2?0:180);break;
            case "communal_table":
                w=4;d=2;Table(p,Vector3.zero,3.6f,.7f);for(int i=0;i<6;i++){float x=-1.2f+(i%3)*1.2f,z=i<3?-.67f:.67f;Chair(p,new Vector3(x,0,z),i<3?0:180,true);Seat(p,i,new Vector3(x,0,z),i<3?0:180);}break;
            case "pendant_amber":
                h=3.8f;Lathe("CeilingRose",p,new Vector3(0,3.73f,0),new[]{0f,.06f,.09f},new[]{.11f,.2f,.2f},dark);Rod("PendantCable",p,new Vector3(0,2.87f,0),new Vector3(0,3.76f,0),.018f,Ink);Lathe("PleatedShade",p,new Vector3(0,2.34f,0),new[]{0f,.06f,.53f,.56f},new[]{.46f,.45f,.21f,.18f},Solid(Coral),12);Lathe("ShadeLip",p,new Vector3(0,2.33f,0),new[]{0f,.03f},new[]{.47f,.47f},Solid(Brass),12);Round("AmberBulb",p,new Vector3(0,2.39f,0),Vector3.one*.2f,Cream);LightAt("WarmLamp",p,new Vector3(0,2.25f,0),C("FFD58C"),1.3f,5);elevatedColliderCenter=new Vector3(0,2.63f,0);elevatedColliderSize=new Vector3(.95f,.63f,.95f);break;
            case "globe_lamp":
                h=1.9f;Lathe("GlobeFoot",p,Vector3.zero,new[]{0f,.07f,.13f},new[]{.28f,.3f,.11f},Solid(Brass));Rod("GlobeStem",p,new Vector3(0,.09f,0),new Vector3(0,1.4f,0),.055f,Brass);Shape("MilkGlass",p,Profile("gem",new[]{-.5f,-.35f,0,.35f,.5f},new[]{0f,.38f,.5f,.38f,0f}),new Vector3(0,1.57f,0),Vector3.one*.54f,Mat("MilkGlass",Cream,0,true));LightAt("GlobeLight",p,new Vector3(0,1.75f,0),C("FFE5B5"),1.5f,4.5f);break;
            case "fern":
                h=1.5f;Lathe("TerracottaPot",p,Vector3.zero,new[]{0f,.06f,.49f,.52f,.56f},new[]{.24f,.27f,.34f,.36f,.36f},Solid(Coral));Lathe("PotSoil",p,new Vector3(0,.55f,0),new[]{0f,.02f},new[]{.31f,0},Solid(Wood));
                for(int i=0;i<9;i++){float a=i*2.4f;Vector3 top=new Vector3(Mathf.Cos(a)*.37f,.8f+(i%3)*.25f,Mathf.Sin(a)*.37f);Rod("FernStem",p,new Vector3(0,.54f,0),top,.018f,Green);for(int j=0;j<3;j++){var leaf=Round("FacetedLeaf",p,Vector3.Lerp(new Vector3(0,.6f,0),top,.4f+j*.22f),new Vector3(.16f,.065f,.35f),i%2==0?Green:Teal);leaf.transform.localRotation=Quaternion.Euler(-25,a*Mathf.Rad2Deg+j*26,20);}}break;
            case "rug_sunset":
                w=2;d=2;h=.095f;Box("WovenRug",p,new Vector3(0,.078f,0),new Vector3(1.96f,.025f,1.96f),Mat("RugFabric",Coral,0,false,"fabric"));for(int i=0;i<5;i++)Box("RugBand",p,new Vector3(0,.094f,-.7f+i*.35f),new Vector3(1.74f,.008f,.1f),i%2==0?Brass:Teal);elevatedColliderCenter=new Vector3(0,.085f,0);elevatedColliderSize=new Vector3(1.96f,.035f,1.96f);break;
            case "art_orbit":
                h=3.8f;foreach(float x in new[]{-.29f,.29f})Rod("GallerySuspension",p,new Vector3(x,3.2f,0),new Vector3(x,3.8f,0),.015f,Ink);Box("GalleryFrame",p,new Vector3(0,2.6f,0),new Vector3(.9f,1.25f,.1f),Brass);Box("ArtCanvas",p,new Vector3(0,2.6f,.061f),new Vector3(.76f,1.1f,.03f),Ink);Round("AbstractSun",p,new Vector3(-.13f,2.81f,.09f),new Vector3(.37f,.37f,.035f),Coral);Round("AbstractMoon",p,new Vector3(.17f,2.54f,.1f),new Vector3(.38f,.38f,.03f),Cream);Rod("OrbitLine",p,new Vector3(-.3f,2.31f,.11f),new Vector3(.3f,2.75f,.11f),.025f,Brass);elevatedColliderCenter=new Vector3(0,2.6f,.03f);elevatedColliderSize=new Vector3(.93f,1.3f,.2f);break;
            case "neon_moon":
                h=3.8f;foreach(float x in new[]{-.23f,.23f})Rod("NeonSuspension",p,new Vector3(x,3f,0),new Vector3(x,3.8f,0),.018f,Brass);Round("NeonBacking",p,new Vector3(0,2.7f,0),new Vector3(.85f,.85f,.15f),Ink);for(int i=0;i<14;i++){float a=(i*18+55)*Mathf.Deg2Rad;Shape("MoonNeon",p,Profile("gem",new[]{-.5f,-.35f,0,.35f,.5f},new[]{0f,.38f,.5f,.38f,0f}),new Vector3(Mathf.Cos(a)*.3f,2.7f+Mathf.Sin(a)*.3f,.11f),Vector3.one*.09f,Mat("MoonGlow",C("F8C37D"),0,true));}LightAt("MoonAura",p,new Vector3(0,2.7f,.3f),Coral,.7f,3);elevatedColliderCenter=new Vector3(0,2.7f,.025f);elevatedColliderSize=new Vector3(.9f,.9f,.25f);break;
            case "jukebox":
                h=1.65f;Box("JukeboxBase",p,new Vector3(0,.67f,0),new Vector3(.92f,1.28f,.69f),Wood);Round("RoundedCrown",p,new Vector3(0,1.29f,0),new Vector3(.92f,.71f,.68f),Coral);Box("Speaker",p,new Vector3(0,.55f,.365f),new Vector3(.63f,.67f,.03f),Ink);for(int i=0;i<7;i++)Box("SpeakerGrille",p,new Vector3(-.25f+i*.083f,.55f,.391f),new Vector3(.023f,.65f,.018f),Brass);Box("SongWindow",p,new Vector3(0,1.14f,.36f),new Vector3(.61f,.29f,.04f),Cream);for(int i=0;i<5;i++)Box("SongKey",p,new Vector3(-.24f+i*.12f,.89f,.43f),new Vector3(.065f,.055f,.1f),Teal);LightAt("JukeboxGlow",p,new Vector3(0,1.3f,.45f),Coral,.6f,2);break;
            default:
                // Finish/exterior IDs can be previewed in a catalog, but their effect is applied through UpdateFinishes.
                // Pack-only decor (look comes from ArtOverrides) uses its catalog footprint; wall/ceiling pieces don't block walking.
                {var ci=RestaurantCatalog.Find(id);if(ci!=null){w=ci.Width;d=ci.Depth;if(ci.WallOrCeiling)collide=false;}}
                Box("FinishSample",p,new Vector3(0,.06f,0),new Vector3(.9f,.12f,.9f),id.Contains("teal")?Teal:id.Contains("rose")||id.Contains("coral")?Coral:Cream);break;
            }
            if(collide){var box=g.AddComponent<BoxCollider>();box.size=elevatedColliderSize??new Vector3(w*.94f,h,d*.92f);box.center=elevatedColliderCenter??new Vector3(0,h*.5f,0);}
            if(id=="grill"||id=="prep_bench"||id=="stove"||id=="oven"||id=="fridge")Group("WorkPoint",p,new Vector3(0,0,1.05f));
            return g;
        }
        static Transform Limb(Transform body,string name,Vector3 pos,Color color,float length,float width) {var joint=Group(name,body,pos).transform;Round("Sleeve",joint,new Vector3(0,-length*.22f,0),new Vector3(width,length*.65f,width),color);Round("Hand",joint,new Vector3(0,-length*.62f,0),new Vector3(width*.8f,width,width*.8f),color*.9f);return joint;}
        static void Eyes(Transform head,Color eye,float width=.12f,float separation=.19f,float z=.28f) {
            foreach(float x in new[]{-separation,separation}) {Round("EyeWhite",head,new Vector3(x,.035f,z),new Vector3(width*.95f,width*1.2f,.07f),Cream);Round("Pupil",head,new Vector3(x,.035f,z+.035f),new Vector3(width*.48f,width*.71f,.035f),eye);Round("EyeGlint",head,new Vector3(x-.013f,.055f,z+.058f),Vector3.one*.02f,White);}
            Box("Mouth",head,new Vector3(0,-.135f,z+.014f),new Vector3(.15f,.035f,.025f),Ink);
        }
        public static GameObject CreateCharacter(int type,Transform parent) {
            string[] names={"Commuter","Long Ear Voyager","Velvet Moth","Tin Tourist","Jelly Regular","Mushroom Scholar","Horned Gourmand","Tentacle Poet","Ember Kitchen Automaton","Moss Service Sprite"};
            type=Mathf.Clamp(type,0,9);var g=Group(names[type],parent);var b=Group("BodyRig",g.transform).transform;Color skin=type==0?C("BA805C"):type==1?C("AAA1CF"):type==2?C("E0B98B"):type==3?C("A4C3BE"):type==4?C("77C5A6"):type==5?C("E6D4A3"):type==6?C("B57170"):type==7?C("719FB6"):type==8?C("E28D53"):C("8DAD68");
            Color clothes=type==0?C("467E93"):type==1?Coral:type==2?C("795A78"):type==3?Teal:type==4?Cream:type==5?C("866747"):type==6?C("586354"):type==7?C("564772"):type==8?Cream:Teal;
            float bodyWidth=type==6?.83f:type==4?.8f:.55f,headHeight=type==1?1.65f:type==6?1.65f:type==4?1.07f:1.5f;
            if(type!=4) {
                var torso=Lathe("TailoredTorso_"+type,b,new Vector3(0,.72f,0),new[]{0f,.09f,.47f,.6f},new[]{bodyWidth*.45f,bodyWidth*.54f,bodyWidth*.49f,bodyWidth*.28f},Solid(clothes),8);torso.transform.localScale=new Vector3(1,1,.77f);
                foreach(int side in new[]{-1,1}) {
                    var leg=Group(side<0?"LegL":"LegR",b,new Vector3(side*(type==6?.22f:.15f),.73f,0)).transform;
                    Box("TrouserThigh",leg,new Vector3(0,-.145f,0),new Vector3(type==6?.24f:.16f,.29f,.19f),type==8?Ink:clothes*.74f);
                    var knee=Group("Knee",leg,new Vector3(0,-.29f,0)).transform;
                    Box("TrouserShin",knee,new Vector3(0,-.145f,0),new Vector3(type==6?.22f:.15f,.29f,.18f),type==8?Ink:clothes*.74f);Box("Shoe",knee,new Vector3(0,-.36f,.08f),new Vector3(.22f,.15f,.34f),Ink);
                    Limb(b,side<0?"ArmL":"ArmR",new Vector3(side*(bodyWidth*.57f),1.22f,0),skin,.6f,type==6?.26f:.17f);
                }
            }
            var head=Group("HeadRig",b,new Vector3(0,headHeight,0)).transform;
            Round("FacetedHead",head,Vector3.zero,new Vector3(type==6?.66f:.53f,type==1?.65f:.55f,.52f),skin);Eyes(head,type==8?Coral:Ink,type==6?.105f:.12f,type==6?.18f:.14f);
            switch(type) {
            case 0:
                // Rolled sleeves, tie, side-part hair, nose and messenger bag.
                Round("Hair",head,new Vector3(0,.22f,-.015f),new Vector3(.55f,.22f,.49f),C("493F38"));Box("HairPart",head,new Vector3(-.13f,.2f,.21f),new Vector3(.28f,.18f,.13f),C("493F38"));Round("Nose",head,new Vector3(0,-.025f,.29f),new Vector3(.08f,.1f,.12f),skin);
                Box("Collar",b,new Vector3(0,1.27f,.23f),new Vector3(.26f,.08f,.04f),Cream);Box("Tie",b,new Vector3(0,1.06f,.238f),new Vector3(.065f,.28f,.03f),Coral);Rod("SatchelStrap",b,new Vector3(-.26f,1.29f,.2f),new Vector3(.29f,.76f,.23f),.052f,Wood);Box("Satchel",b,new Vector3(.31f,.71f,.13f),new Vector3(.26f,.31f,.16f),Wood);break;
            case 1:
                foreach(int side in new[]{-1,1}){var ear=Round("LongPointedEar",head,new Vector3(side*.3f,.37f,0),new Vector3(.14f,.71f,.14f),skin);ear.transform.localRotation=Quaternion.Euler(0,0,-side*16);var inner=Round("EarInner",head,new Vector3(side*.31f,.38f,.06f),new Vector3(.06f,.49f,.04f),Coral);inner.transform.localRotation=ear.transform.localRotation;}
                Lathe("ScarfRing",b,new Vector3(0,1.31f,0),new[]{0f,.1f},new[]{.24f,.24f},Solid(Cream));Box("ScarfTail",b,new Vector3(.13f,1.14f,.255f),new Vector3(.14f,.37f,.055f),Cream);Round("ForeheadGem",head,new Vector3(0,.2f,.24f),new Vector3(.09f,.13f,.03f),Brass);break;
            case 2:
                foreach(int side in new[]{-1,1}){var wing=Round("MothVelvetWing",b,new Vector3(side*.48f,1.18f,-.13f),new Vector3(.66f,1.07f,.16f),clothes);wing.transform.localRotation=Quaternion.Euler(0,side*15,-side*27);Round("WingEyespot",b,new Vector3(side*.58f,1.26f,-.24f),new Vector3(.22f,.28f,.04f),Coral);Round("WingDot",b,new Vector3(side*.58f,1.26f,-.265f),new Vector3(.11f,.16f,.025f),Cream);Rod("Antenna",head,new Vector3(side*.12f,.23f,0),new Vector3(side*.26f,.6f,.04f),.035f,Ink);Round("AntennaTip",head,new Vector3(side*.26f,.6f,.04f),Vector3.one*.1f,Brass);}
                Round("FluffyCollar",b,new Vector3(0,1.34f,0),new Vector3(.69f,.21f,.43f),Cream);break;
            case 3:case 8:
                // Boxy screen head and riveted limbs produce a mechanical silhouette, distinct from all organic guests.
                head.Find("FacetedHead").gameObject.SetActive(false);Box("RobotHead",head,Vector3.zero,new Vector3(.61f,.47f,.45f),skin);Box("Screen",head,new Vector3(0,.01f,.248f),new Vector3(.48f,.3f,.04f),Ink);
                foreach(float x in new[]{-.14f,.14f})Round("LEDEye",head,new Vector3(x,.035f,.293f),new Vector3(.09f,.12f,.04f),type==8?Coral:Green);
                foreach(int side in new[]{-1,1}){Round("EarBolt",head,new Vector3(side*.34f,0,0),Vector3.one*.13f,Brass);Round("ShoulderBolt",b,new Vector3(side*.34f,1.19f,0),Vector3.one*.2f,Brass);}
                Box("ChestPanel",b,new Vector3(0,1.07f,.25f),new Vector3(.35f,.23f,.04f),Ink);for(int i=0;i<3;i++)Round("ChargeLight",b,new Vector3(-.1f+i*.1f,1.1f,.28f),Vector3.one*.045f,Coral);
                if(type==8){Box("Apron",b,new Vector3(0,.89f,.252f),new Vector3(.47f,.55f,.035f),Cream);Lathe("ChefHatBrim",head,new Vector3(0,.22f,0),new[]{0f,.17f},new[]{.28f,.28f},Solid(Cream));for(int i=0;i<3;i++)Round("ChefHatPuff",head,new Vector3((i-1)*.17f,.46f,0),new Vector3(.29f,.36f,.37f),Cream);Box("ApronPocket",b,new Vector3(0,.81f,.279f),new Vector3(.23f,.13f,.02f),Coral);}else{Rod("RadioAntenna",head,new Vector3(.16f,.24f,0),new Vector3(.19f,.57f,0),.025f,Brass);Round("SignalTip",head,new Vector3(.19f,.58f,0),Vector3.one*.07f,Coral);}break;
            case 4:
                head.Find("FacetedHead").gameObject.SetActive(false);Shape("JellyBody",b,Profile("blob",new[]{0f,.08f,.22f,.58f,.98f,1.22f,1.37f},new[]{.31f,.49f,.5f,.45f,.39f,.24f,0f},12),Vector3.zero,new Vector3(1,1,.86f),Solid(skin));
                head.localPosition=new Vector3(0,1.02f,.09f);foreach(int side in new[]{-1,1})Round("JellyFlipper",b,new Vector3(side*.44f,.57f,0),new Vector3(.24f,.17f,.26f),skin);
                Lathe("TinyHat",head,new Vector3(.09f,.32f,0),new[]{0f,.045f,.07f,.24f,.26f},new[]{.24f,.24f,.14f,.14f,.12f},Solid(Ink));Box("BowTieL",b,new Vector3(-.11f,.76f,.37f),new Vector3(.17f,.11f,.06f),Coral);Box("BowTieR",b,new Vector3(.11f,.76f,.37f),new Vector3(.17f,.11f,.06f),Coral);break;
            case 5:
                Lathe("MushroomCap",head,new Vector3(0,.12f,0),new[]{0f,.05f,.14f,.31f,.45f},new[]{.55f,.58f,.5f,.35f,0f},Solid(Coral),12);
                for(int i=0;i<6;i++){float a=i*1.05f;Round("CapSpot",head,new Vector3(Mathf.Cos(a)*.31f,.4f,Mathf.Sin(a)*.31f),new Vector3(.13f,.05f,.12f),Cream);}
                foreach(float x in new[]{-.14f,.14f}){Box("SquareSpectacles",head,new Vector3(x,.035f,.328f),new Vector3(.21f,.17f,.015f),Brass);Box("Lens",head,new Vector3(x,.035f,.34f),new Vector3(.16f,.11f,.02f),Ink);Round("SpectacleGlint",head,new Vector3(x,.055f,.355f),new Vector3(.045f,.055f,.012f),Cream);}Box("BookSatchel",b,new Vector3(-.35f,.83f,0),new Vector3(.16f,.4f,.28f),Teal);break;
            case 6:
                foreach(int side in new[]{-1,1}){var horn=Lathe("CurvedHorn",head,new Vector3(side*.23f,.21f,0),new[]{0f,.16f,.31f,.42f},new[]{.13f,.105f,.055f,0f},Solid(Cream),7);horn.transform.localRotation=Quaternion.Euler(0,0,-side*31);Round("HeavyBrow",head,new Vector3(side*.15f,.13f,.23f),new Vector3(.27f,.12f,.13f),skin*.8f);Round("Ear",head,new Vector3(side*.38f,0,0),new Vector3(.2f,.24f,.16f),skin);}
                Box("VestLapel",b,new Vector3(-.16f,1.1f,.32f),new Vector3(.13f,.39f,.045f),Cream);Box("VestLapel",b,new Vector3(.16f,1.1f,.32f),new Vector3(.13f,.39f,.045f),Cream);for(int i=0;i<3;i++)Round("VestButton",b,new Vector3(0,.85f+i*.13f,.343f),Vector3.one*.046f,Brass);break;
            case 7:
                Round("Mantle",head,new Vector3(0,.14f,-.055f),new Vector3(.62f,.64f,.55f),skin);for(int i=0;i<5;i++){float x=(i-2)*.09f;var tentacle=Lathe("FaceTentacle"+i,head,new Vector3(x,-.1f,.24f),new[]{-.4f,-.31f,-.16f,0f},new[]{0f,.038f,.06f,.063f},Solid(skin),6);tentacle.transform.localRotation=Quaternion.Euler(-15+(i%2)*22,0,(i-2)*-12);}
                Lathe("PoetBeret",head,new Vector3(0,.37f,0),new[]{0f,.08f,.16f,.19f},new[]{.29f,.36f,.26f,0f},Solid(Ink),10);Box("PoetScarf",b,new Vector3(-.13f,1.13f,.25f),new Vector3(.16f,.37f,.06f),Coral);break;
            case 9:
                for(int i=0;i<7;i++){float a=i*.9f;var leaf=Round("LeafCrown",head,new Vector3(Mathf.Cos(a)*.24f,.29f,Mathf.Sin(a)*.19f),new Vector3(.19f,.42f,.12f),i%2==0?Green:Teal);leaf.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,Mathf.Cos(a)*-28);}
                Box("ServerApron",b,new Vector3(0,.89f,.25f),new Vector3(.43f,.54f,.045f),Cream);Box("ApronPocket",b,new Vector3(0,.86f,.282f),new Vector3(.23f,.15f,.018f),Coral);Round("CheekL",head,new Vector3(-.22f,-.08f,.19f),new Vector3(.1f,.065f,.055f),Coral);Round("CheekR",head,new Vector3(.22f,-.08f,.19f),new Vector3(.1f,.065f,.055f),Coral);
                var hand=b.Find("ArmR");Lathe("ServiceTray",hand,new Vector3(.02f,-.42f,.14f),new[]{0f,.035f},new[]{.25f,.25f},Solid(Brass));break;
            }
            g.AddComponent<CharacterMotion>();ArtOverrides.Apply(g,"Characters",names[type]);return g;
        }
        // Luxury pieces only the rival restaurant has (for now).
        public static GameObject CreateLuxury(string id,Transform parent) {
            var g=Group("Luxury_"+id,parent);var p=g.transform;var gold=Mat("LuxGold",C("E2AE48"),.9f);var obsidian=Mat("LuxObsidian",C("1C1B2B"),.3f);
            switch(id){
            case "chandelier":
                Rod("ChandelierChain",p,new Vector3(0,3.7f,0),new Vector3(0,4.5f,0),.03f,C("E2AE48"));
                for(int t=0;t<3;t++){float r=.95f-t*.3f,y=3.35f+t*.28f;Lathe("ChandelierRing"+t,p,new Vector3(0,y,0),new[]{0f,.05f},new[]{r,r},gold,24);
                    int n=10-t*3;for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;Shape("Crystal",p,Profile("gem",new[]{-.5f,-.35f,0,.35f,.5f},new[]{0f,.38f,.5f,.38f,0f}),new Vector3(Mathf.Cos(a)*r,y-.12f,Mathf.Sin(a)*r),new Vector3(.08f,.2f,.08f),Mat("Crystal",C("FFF1C9"),0,true));}}
                break;
            case "gold_column":
                Lathe("ColumnBase",p,Vector3.zero,new[]{0f,.15f,.25f},new[]{.38f,.38f,.28f},gold);
                Lathe("ColumnShaft",p,new Vector3(0,.25f,0),new[]{0f,4f},new[]{.24f,.2f},obsidian,10);
                Lathe("ColumnCapital",p,new Vector3(0,4.2f,0),new[]{0f,.12f,.3f},new[]{.24f,.36f,.38f},gold);
                for(int i=0;i<3;i++)Lathe("ColumnBand"+i,p,new Vector3(0,1.2f+i*1.1f,0),new[]{0f,.05f},new[]{.23f,.23f},gold,12);
                break;
            case "wine_wall":
                Box("WineCabinet",p,new Vector3(0,1.5f,0),new Vector3(1.9f,3f,.45f),obsidian);
                Box("Backlight",p,new Vector3(0,1.55f,.2f),new Vector3(1.7f,2.7f,.03f),Mat("WineGlow",C("FFB85C"),0,true));
                for(int row=0;row<5;row++){Box("WineShelf",p,new Vector3(0,.35f+row*.55f,.25f),new Vector3(1.8f,.04f,.3f),gold);
                    for(int i=0;i<7;i++)Lathe("WineBottle",p,new Vector3(-.75f+i*.25f,.37f+row*.55f,.28f),new[]{0f,.24f,.3f,.4f},new[]{.055f,.055f,.022f,.022f},Mat(i%3==0?"Rose":"Bordeaux",i%3==0?C("E27A8C"):C("5A0F24"),.5f),8);}
                break;
            case "aquarium":
                Box("TankStand",p,new Vector3(0,.45f,0),new Vector3(1.9f,.9f,.6f),obsidian);
                Box("TankWater",p,new Vector3(0,1.55f,0),new Vector3(1.8f,1.3f,.5f),Mat("TankWater",C("2FB5C9"),0,true));
                Box("TankLid",p,new Vector3(0,2.25f,0),new Vector3(1.95f,.1f,.65f),gold);
                for(int i=0;i<6;i++)Round("GlowFish",p,new Vector3(-.6f+i*.25f,1.2f+(i%3)*.3f,.28f),new Vector3(.2f,.1f,.06f),i%2==0?C("FF8A5C"):C("FFE066"));
                for(int i=0;i<4;i++)Lathe("Seaweed"+i,p,new Vector3(-.7f+i*.45f,.92f,0),new[]{0f,.5f},new[]{.05f,0f},Mat("Seaweed",C("3FAE6B")),6);
                break;
            case "statue":
                Box("Plinth",p,new Vector3(0,.5f,0),new Vector3(.8f,1f,.8f),obsidian);
                Lathe("OrbitRing",p,new Vector3(0,1.6f,0),new[]{0f,.06f},new[]{.45f,.45f},gold,24).transform.localRotation=Quaternion.Euler(70,0,20);
                Round("OrbitCore",p,new Vector3(0,1.6f,0),Vector3.one*.4f,C("E2AE48"));
                Round("OrbitMoon",p,new Vector3(.42f,1.85f,0),Vector3.one*.14f,C("FFF1C9"));
                break;
            case "velvet_curtain":
                for(int i=0;i<4;i++)Box("CurtainFold",p,new Vector3(-.3f+i*.2f,2.1f,(i%2)*.06f),new Vector3(.2f,4.2f,.1f),Mat("Velvet",C("5B1E4F")));
                Box("CurtainTie",p,new Vector3(0,1.4f,.1f),new Vector3(.85f,.08f,.14f),gold);
                break;
            }
            ArtOverrides.Apply(g,"Furniture",id);
            return g;
        }
        // Rival restaurant staff: built on the base bodies, restyled so nobody in the player's crew looks like them.
        public static GameObject CreateEliteCharacter(int variant,Transform parent) {
            int[] bases={6,2,8,1,7,3,0};variant=Mathf.Clamp(variant,0,6);
            var g=CreateCharacter(bases[variant],parent);var b=g.transform.Find("BodyRig");var head=b?b.Find("HeadRig"):null;
            string[] names={"Maestro Vey / head chef","Nyx / sommelier","K-9 / line cook","Aurora / maitre d'","Seraphine / pastry chef","Obsidian Titan / doorman","Lumen / mixologist"};g.name=names[variant];
            string[] slots={"Elite_MaestroVey","Elite_Nyx","Elite_K9","Elite_Aurora","Elite_Seraphine","Elite_ObsidianTitan","Elite_Lumen"};
            Color[] coats={C("F7F4EE"),C("15121F"),C("B9C3CC"),C("3A1D3F"),C("FFF6F0"),C("0E0D12"),C("F2F2F7")};
            var coat=Mat("EliteCoat"+variant,coats[variant],variant==2?.85f:0);var trousers=Mat("EliteTrousers",C("121017"));
            var gold=Mat("EliteGold",C("E2AE48"),.9f);Color[] glow={C("B77CFF"),C("6EF2FF"),C("39F5C9"),C("FFD36E"),C("FF9BD2"),C("FF3B3B"),C("4FE3FF")};var neon=Mat("EliteGlow"+variant,glow[variant],0,true);
            foreach(var r in g.GetComponentsInChildren<MeshRenderer>()){
                string n=r.gameObject.name;
                if(n.StartsWith("TailoredTorso"))r.sharedMaterial=coat;
                else if(n.StartsWith("Trouser"))r.sharedMaterial=trousers;
                else if(n=="Pupil")r.sharedMaterial=neon;
            }
            if(!b||!head)return g;
            switch(variant){
            case 0: // Towering gold toque, double-breasted gold buttons, epaulettes.
                Lathe("EliteToque",head,new Vector3(0,.3f,0),new[]{0f,.08f,.5f,.62f},new[]{.25f,.27f,.33f,.26f},gold);
                for(int i=0;i<3;i++)for(int s=-1;s<=1;s+=2)Round("CoatButton",b,new Vector3(s*.1f,1.2f-i*.14f,.3f),Vector3.one*.06f,C("E2AE48"));
                for(int s=-1;s<=1;s+=2)Box("Epaulette",b,new Vector3(s*.38f,1.34f,0),new Vector3(.2f,.05f,.26f),gold);
                break;
            case 1: // Floating crystal halo, gold chain, a bottle of something rare.
                Lathe("EliteHalo",head,new Vector3(0,.55f,0),new[]{0f,.03f},new[]{.34f,.34f},neon,20);
                Box("GoldChain",b,new Vector3(0,1.18f,.27f),new Vector3(.3f,.035f,.03f),gold);
                var hand=b.Find("ArmR");if(hand)Lathe("RareBottle",hand,new Vector3(0,-.45f,.12f),new[]{0f,.22f,.28f,.38f},new[]{.07f,.07f,.03f,.03f},Mat("BottleGlass",C("3D1030"),.6f));
                break;
            case 2: // Chrome automaton with a neon visor and a second pair of arms.
                Box("NeonVisor",head,new Vector3(0,.03f,.27f),new Vector3(.46f,.1f,.05f),neon);
                for(int s=-1;s<=1;s+=2)Limb(b,s<0?"ExtraArmL":"ExtraArmR",new Vector3(s*.36f,1.0f,.05f),C("B9C3CC"),.5f,.14f);
                break;
            case 3: // Tux with gold lapels, bow tie and a glowing monocle.
                for(int s=-1;s<=1;s+=2)Box("GoldLapel",b,new Vector3(s*.09f,1.16f,.27f),new Vector3(.07f,.28f,.03f),gold);
                Box("BowTie",b,new Vector3(0,1.3f,.29f),new Vector3(.18f,.07f,.04f),neon);
                Lathe("Monocle",head,new Vector3(.14f,.04f,.28f),new[]{0f,.02f},new[]{.07f,.07f},neon,14).transform.localRotation=Quaternion.Euler(90,0,0);
                break;
            case 4: // Gilded wings, pearl coat, a glowing whisk.
                for(int s=-1;s<=1;s+=2){var wing=Round("GoldWing",b,new Vector3(s*.42f,1.35f,-.28f),new Vector3(.18f,.9f,.5f),C("E2AE48"));wing.transform.localRotation=Quaternion.Euler(-15,0,s*-35);}
                Lathe("PastryToque",head,new Vector3(0,.3f,0),new[]{0f,.06f,.32f,.4f},new[]{.22f,.24f,.3f,.22f},Mat("PearlToque",C("FFF6F0")));
                var whiskHand=b.Find("ArmR");if(whiskHand)Lathe("GlowWhisk",whiskHand,new Vector3(0,-.5f,.1f),new[]{0f,.08f,.25f},new[]{.02f,.07f,0f},neon,8);
                break;
            case 5: // Towering obsidian doorman: gold crown horns, pauldrons, red eyes.
                g.transform.localScale=Vector3.one*1.25f;
                for(int s=-1;s<=1;s+=2){var horn=Lathe("CrownHorn",head,new Vector3(s*.2f,.28f,0),new[]{0f,.35f},new[]{.07f,0f},gold,8);horn.transform.localRotation=Quaternion.Euler(0,0,s*-25);Round("Pauldron",b,new Vector3(s*.42f,1.3f,0),new Vector3(.34f,.2f,.36f),C("1C1B2B"));}
                Box("GoldSash",b,new Vector3(0,1.0f,.27f),new Vector3(.08f,.6f,.03f),gold);
                break;
            case 6: // White suit, neon crest, cocktail shaker.
                for(int i=0;i<5;i++)Round("NeonCrest",head,new Vector3(0,.3f+i*.02f,.15f-i*.09f),new Vector3(.08f,.26f-i*.03f,.12f),glow[6]).GetComponent<MeshRenderer>().sharedMaterial=neon;
                var shakerHand=b.Find("ArmR");if(shakerHand)Lathe("Shaker",shakerHand,new Vector3(0,-.5f,.1f),new[]{0f,.2f,.26f},new[]{.06f,.06f,.03f},Mat("Chrome",C("DDE3E8"),.9f));
                Box("BlackTie",b,new Vector3(0,1.25f,.29f),new Vector3(.05f,.25f,.03f),Mat("TieBlack",C("0E0D12")));
                break;
            }
            ArtOverrides.Apply(g,"Characters",slots[variant]);
            return g;
        }
    }
}
