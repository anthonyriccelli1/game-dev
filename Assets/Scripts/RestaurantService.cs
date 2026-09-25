using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RestaurantCity {
    public partial class RestaurantController {
        sealed class GuestView {
            public GameObject Root, Plate;
            public CharacterMotion Motion;
            public TextMesh Bubble;
            public Transform Seat;
            public int Type, OrderId = -1, SeatIndex;
            public bool Seated, Leaving;
            public float QueueWait;
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
        }
        sealed class EmployeeView {
            public GameObject Root, Plate;
            public CharacterMotion Motion;
            public TextMesh Bubble;
            public StaffJob Job;
            public int OrderId = -1, Phase;
            public float Work;
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
        }
        readonly Dictionary<int, GuestView> guests = new Dictionary<int, GuestView>();
        readonly List<GuestView> queue = new List<GuestView>();
        readonly Dictionary<string, EmployeeView> employees = new Dictionary<string, EmployeeView>();
        readonly List<GameObject> crumbs = new List<GameObject>();
        readonly Dictionary<int, GameObject> kitchenPlates = new Dictionary<int, GameObject>();
        readonly HashSet<int> observedTypes = new HashSet<int>();
        GameObject heldPlate;
        RestaurantState actorState;
        float arrival = 3, cleaning, sparkleTime;
        int nextType, lastRank = 1;
        AudioSource chime;
        AudioClip purchaseTone, saleTone;
        public int VisibleGuests => guests.Count + queue.Count;
        public int ObservedCustomerTypes => observedTypes.Count;
        public int VisibleWorkers => employees.Count;

        partial void TickServiceActors(float seconds) {
            if (actorState != Data) ResetActors();
            if (!Data.Owned) return;
            if (CarriedOrderId >= 0 && !Data.Orders.Any(o => o.Id == CarriedOrderId && o.Stage == RestaurantOrderStage.Ready)) CarriedOrderId = -1;
            if (cleaning > 0) {
                cleaning -= seconds;
                if (cleaning <= 0) { Data.Clean(out var msg); Feedback(msg); PlayChime(false); Game.Save(); }
            }
            if (Data.Open) {
                arrival -= seconds;
                if (arrival <= 0) {
                    arrival = Rush ? 7 : Game.State.RecipeUnlocked && Data.ActiveMenu.Contains("midnight") ? 13 : 17;
                    if (queue.Count < 3) {
                        var guest = NewGuest(nextType++ % RestaurantCatalog.Customers.Length);
                        guest.Root.transform.position = new Vector3(-8.7f, .055f, -5.4f + queue.Count);
                        queue.Add(guest);
                    }
                }
            } else arrival = Mathf.Min(arrival, 4);
            for (int i = queue.Count - 1; i >= 0; i--) {
                var view = queue[i]; view.QueueWait += seconds;
                var place = Data.Layout.FirstOrDefault(p => RestaurantCatalog.Find(p.CatalogId).Seats > Data.Orders.Count(o => o.SeatInstanceId == p.InstanceId && o.Stage != RestaurantOrderStage.Leaving));
                if (Data.Open && place != null) {
                    int capacity = RestaurantCatalog.Find(place.CatalogId).Seats;
                    var occupied = new HashSet<int>(guests.Values.Where(g => g.Seat && g.Seat.parent == Furnishings[place.InstanceId].transform &&
                        Data.Orders.Any(o => o.Id == g.OrderId && o.Stage != RestaurantOrderStage.Leaving)).Select(g => g.SeatIndex));
                    int seatIndex = Enumerable.Range(0, capacity).Where(n => !occupied.Contains(n)).DefaultIfEmpty(-1).First();
                    if (seatIndex < 0) continue;
                    var order = Data.AddCustomer(Game.State, view.Type, place.InstanceId, out var message);
                    if (order != null) {
                        queue.RemoveAt(i); view.OrderId = order.Id;
                        view.SeatIndex = seatIndex;
                        view.Seat = Furnishings[place.InstanceId].transform.Find("Seat_" + view.SeatIndex);
                        view.Root.GetComponent<RestaurantTarget>().OrderId = order.Id;
                        view.Path.Enqueue(new Vector3(-10, .055f, -8));
                        AppendRoute(view.Path, new Vector3(-10, .055f, -10.5f), view.Seat.position);
                        view.Path.Enqueue(view.Seat.position); guests[order.Id] = view;
                        observedTypes.Add(view.Type); Feedback(message);
                        continue;
                    }
                }
                if (!Data.Open || view.QueueWait > 32) { Destroy(view.Root); queue.RemoveAt(i); continue; }
                Vector3 spot = new Vector3(-8.5f, .055f, -7.4f + i * .95f);
                view.Motion.Walking = StepTo(view.Root.transform, spot, seconds * 1.9f);
                SetBubble(view.Bubble, "Waiting for a table\n" + RestaurantCatalog.Customers[view.Type].Name);
            }
            foreach (var pair in guests.ToArray()) {
                var view = pair.Value; var order = Data.Orders.Find(o => o.Id == pair.Key);
                if (order == null || order.Stage == RestaurantOrderStage.Leaving) {
                    if (!view.Leaving) {
                        view.Leaving = true; view.Seated = false; view.Path.Clear();
                        AppendRoute(view.Path, view.Root.transform.position, new Vector3(-10, .055f, -11));
                        view.Path.Enqueue(new Vector3(-10, .055f, -7)); view.Path.Enqueue(new Vector3(-3, .055f, -5.5f));
                    }
                    view.Motion.Seated = false; view.Motion.Walking = Follow(view.Root.transform, view.Path, seconds * 2.5f);
                    SetBubble(view.Bubble, "See you around!");
                    if (view.Path.Count == 0) { Destroy(view.Root); guests.Remove(pair.Key); }
                    continue;
                }
                if (!view.Seated) {
                    view.Motion.Walking = Follow(view.Root.transform, view.Path, seconds * 2.2f);
                    if (view.Path.Count == 0) { view.Seated = true; view.Root.transform.SetPositionAndRotation(view.Seat.position, view.Seat.rotation); }
                }
                view.Motion.Seated = view.Seated;
                var def = RestaurantCatalog.Customers[order.CustomerType];
                string dish = RestaurantCatalog.Dish(order.DishId).Name;
                if (order.Stage == RestaurantOrderStage.Eating) {
                    var review = Data.Reviews.FirstOrDefault(r => r.Customer == def.Name);
                    float score = review == null ? 70 : review.Score; view.Motion.SetMood(score);
                    SetBubble(view.Bubble, (score >= 85 ? "<3  Delicious!" : score >= 65 ? "That hit the spot." : "Could be better...") + "\n" + Mathf.RoundToInt(score) + "%  /  " + (score >= 85 ? "+$3 tip" : "Thanks for dinner"));
                    if (!view.Plate) { view.Plate = MakeDish(order.DishId, view.Root.transform); view.Plate.transform.localPosition = new Vector3(0, .85f, .4f); }
                } else {
                    float patience = Mathf.Clamp01(1 - order.Wait / def.Patience); view.Motion.SetMood(patience);
                    string status = order.Stage == RestaurantOrderStage.Ready ? "Dish ready!" : patience < .35f ? "I'm getting hungry..." : order.Stage == RestaurantOrderStage.Cooking ? "Smells good!" : "I'd like " + dish;
                    SetBubble(view.Bubble, def.Name + "\n" + status);
                }
            }
            UpdateEmployees(seconds);
            UpdateDishes();
            if (crumbs.Count == 0) for (int i = 0; i < 14; i++) {
                var crumb = SmallShape("Service crumbs", PrimitiveType.Cylinder, transform, new Vector3(-14.5f + i % 4 * 2.7f, .071f, -17.3f + i / 4 * 2), new Vector3(.12f + i % 3 * .04f, .006f, .08f), new Color(.37f,.24f,.16f)); crumbs.Add(crumb);
            }
            for (int i = 0; i < crumbs.Count; i++) crumbs[i].SetActive(i < (100 - Data.Cleanliness) / 6);
            if (Data.Stars > lastRank) { Feedback("TWO STARS! The Starlight oven, moonberry tart, jukebox and neon sign are yours to unlock."); PlayChime(true); Game.Save(); }
            lastRank = Data.Stars;
            sparkleTime = Mathf.Max(0, sparkleTime - seconds);
        }
        void ResetActors() {
            foreach (var view in guests.Values) if(view.Root) Destroy(view.Root);
            foreach (var view in queue) if(view.Root) Destroy(view.Root);
            foreach (var view in employees.Values) if(view.Root) Destroy(view.Root);
            foreach (var plate in kitchenPlates.Values) if(plate) Destroy(plate);
            foreach (var crumb in crumbs) if(crumb) Destroy(crumb);
            guests.Clear(); queue.Clear(); employees.Clear(); kitchenPlates.Clear(); crumbs.Clear(); observedTypes.Clear();
            if(heldPlate)Destroy(heldPlate); CarriedOrderId = -1; arrival = 3; nextType = 0; actorState = Data; lastRank = Data.Stars;
        }
        GuestView NewGuest(int type) {
            var root = RestaurantArt.CreateCharacter(type, transform);
            var collider = root.AddComponent<CapsuleCollider>(); collider.radius = .29f; collider.height = 1.6f; collider.center = Vector3.up * .83f;
            root.AddComponent<RestaurantTarget>().Kind = "Customer";
            return new GuestView { Root = root, Type = type, Motion = root.GetComponent<CharacterMotion>(), Bubble = WorldCaption(root.transform, "", new Vector3(0, 2.4f, 0), .025f) };
        }
        void SetBubble(TextMesh text, string value) {
            text.text = value;
            text.transform.rotation = Quaternion.LookRotation(text.transform.position - Game.Player.View.transform.position);
            text.gameObject.SetActive(Vector3.Distance(text.transform.position, Game.Player.View.transform.position) < 13);
        }
        void UpdateEmployees(float dt) {
            foreach (var worker in Data.Workers) {
                if (!employees.TryGetValue(worker.Id, out var view)) {
                    var root = RestaurantArt.CreateCharacter(worker.Id == "ember" ? 8 : 9, transform); root.transform.position = new Vector3(-9, .055f, -12);
                    view = new EmployeeView { Root=root, Motion=root.GetComponent<CharacterMotion>(), Bubble=WorldCaption(root.transform,"",new Vector3(0,2.4f,0),.023f) }; employees[worker.Id]=view;
                }
                if (view.Job != worker.Job) { view.Job=worker.Job; view.OrderId=-1; view.Phase=0;view.Path.Clear();view.Work=0;if(view.Plate)Destroy(view.Plate); }
                view.Motion.Working=false;
                float taskSeconds = worker.Id == "ember" && worker.Job == StaffJob.Cook || worker.Id == "moss" && (worker.Job == StaffJob.Serve || worker.Job == StaffJob.Clean) ? 3 : 7;
                if (worker.Job == StaffJob.Off) { SetBubble(view.Bubble, worker.Id=="ember"?"Ember / on break":"Moss / on break"); view.Motion.Walking=false;continue; }
                if (view.Path.Count > 0) { view.Motion.Walking=Follow(view.Root.transform,view.Path,dt*2.7f);SetBubble(view.Bubble,worker.Id+" / "+(view.Phase==2?"carrying a dish":worker.Job.ToString()));continue; }
                view.Motion.Walking=false;
                var order=Data.Orders.Find(o=>o.Id==view.OrderId);
                if (worker.Job==StaffJob.Clean) {
                    if(Data.Cleanliness>=92){SetBubble(view.Bubble,worker.Id+" / keeping things tidy");continue;}
                    if(view.Phase==0){view.Phase=1;AppendRoute(view.Path,view.Root.transform.position,new Vector3(-12,.055f,-16));continue;}
                    view.Motion.Working=true;view.Work+=dt;SetBubble(view.Bubble,worker.Id+" / wiping tables");
                    if(view.Work>=taskSeconds){Data.Clean(out _);worker.TasksCompleted++;view.Work=0;view.Phase=0;}continue;
                }
                if (view.OrderId<0 || order==null || (worker.Job==StaffJob.Cook && order.Stage!=RestaurantOrderStage.Waiting) || (worker.Job==StaffJob.Serve && order.Stage!=RestaurantOrderStage.Ready)) {
                    if(view.Plate)Destroy(view.Plate);view.OrderId=-1;view.Phase=0;view.Work=0;
                    order=Data.Orders.FirstOrDefault(o=>(worker.Job==StaffJob.Cook?o.Stage==RestaurantOrderStage.Waiting:o.Stage==RestaurantOrderStage.Ready && o.Id!=CarriedOrderId) && !employees.Values.Any(other=>other!=view && other.OrderId==o.Id));
                    if(order!=null){view.OrderId=order.Id;AppendRoute(view.Path,view.Root.transform.position,KitchenPosition(order));view.Phase=1;}
                    SetBubble(view.Bubble,worker.Id+" / "+(order==null?"ready to help":worker.Job.ToString()));continue;
                }
                view.Motion.Working=true;view.Work+=dt;
                if(worker.Job==StaffJob.Cook){
                    SetBubble(view.Bubble,"Ember"+(worker.Id=="moss"?"'s helper":"")+" / preparing\n"+RestaurantCatalog.Dish(order.DishId).Name);
                    if(view.Work>=taskSeconds){if(Data.BeginCooking(order.Id,out _)){worker.TasksCompleted++;view.OrderId=-1;}view.Work=0;}
                } else if(view.Phase==1) {
                    if(view.Work>=1){view.Work=0;view.Phase=2;view.Plate=MakeDish(order.DishId,view.Root.transform);view.Plate.transform.localPosition=new Vector3(0,1,.45f);
                        if(guests.TryGetValue(order.Id,out var guest))AppendRoute(view.Path,view.Root.transform.position,guest.Seat.position);}
                } else {
                    SetBubble(view.Bubble,worker.Id+" / serving\n"+RestaurantCatalog.Dish(order.DishId).Name);
                    if(view.Work>=taskSeconds && guests.TryGetValue(order.Id,out var guest) && guest.Seated){
                        if(Data.CompleteServing(Game.State,order.Id,out var msg)){worker.TasksCompleted++;Feedback(msg);PlayChime(false);}
                        if(view.Plate)Destroy(view.Plate);view.OrderId=-1;view.Work=0;view.Phase=0;
                    }
                }
            }
        }
        Vector3 KitchenPosition(RestaurantOrder order) {
            var item=Data.Layout.FirstOrDefault(p=>p.CatalogId==RestaurantCatalog.Dish(order.DishId).Equipment);
            if(item!=null&&Furnishings.TryGetValue(item.InstanceId,out var obj)){var work=obj.transform.Find("WorkPoint");if(work)return work.position;return obj.transform.position+Vector3.forward;}
            return new Vector3(-12,.055f,-19);
        }
        void UpdateDishes() {
            foreach(var pair in kitchenPlates.ToArray()) if(!Data.Orders.Any(o=>o.Id==pair.Key&&(o.Stage==RestaurantOrderStage.Cooking||o.Stage==RestaurantOrderStage.Ready)&&o.Id!=CarriedOrderId&&!employees.Values.Any(w=>w.OrderId==o.Id&&w.Phase==2))){Destroy(pair.Value);kitchenPlates.Remove(pair.Key);}
            foreach(var o in Data.Orders.Where(o=>(o.Stage==RestaurantOrderStage.Cooking||o.Stage==RestaurantOrderStage.Ready)&&o.Id!=CarriedOrderId&&!employees.Values.Any(w=>w.OrderId==o.Id&&w.Phase==2))) {
                if(!kitchenPlates.TryGetValue(o.Id,out var plate)){plate=MakeDish(o.DishId,transform);kitchenPlates[o.Id]=plate;}
                plate.transform.position=KitchenPosition(o)+new Vector3((o.Id%2-.5f)*.48f,1.05f,-1.05f);
                plate.transform.localScale=Vector3.one*(o.Stage==RestaurantOrderStage.Cooking?.85f:1);
            }
            if(CarriedOrderId>=0&&!heldPlate){var o=Data.Orders.Find(v=>v.Id==CarriedOrderId);if(o!=null){heldPlate=MakeDish(o.DishId,Game.Player.View.transform);heldPlate.transform.localPosition=new Vector3(.35f,-.33f,.75f);heldPlate.transform.localScale=Vector3.one*.8f;}}
            if(CarriedOrderId<0&&heldPlate)Destroy(heldPlate);
        }
        public bool CollectDish(int id) {
            if(CarriedOrderId>=0){Feedback("Serve the dish you are holding first.");return false;}
            if(!Data.Orders.Any(o=>o.Id==id&&o.Stage==RestaurantOrderStage.Ready)||employees.Values.Any(w=>w.OrderId==id&&w.Phase==2))return false;
            CarriedOrderId=id;Feedback("Dish collected. Take it to its guest.");return true;
        }
        public void BeginCleaning() {
            if(!Inside){Feedback("Return to the restaurant to wipe the tables.");return;}
            if(cleaning>0)return;cleaning=4;ClosePanel();Feedback("Wiping tables... four seconds. Staff can do this for you.");
        }
        static bool StepTo(Transform actor,Vector3 destination,float step) {
            destination.y=.055f;var delta=destination-actor.position;delta.y=0;
            if(delta.sqrMagnitude<.01f)return false;
            actor.rotation=Quaternion.RotateTowards(actor.rotation,Quaternion.LookRotation(delta),step*220);
            actor.position=Vector3.MoveTowards(actor.position,destination,step);return Vector3.Distance(actor.position,destination)>.08f;
        }
        static bool Follow(Transform actor,Queue<Vector3> path,float step) {
            if(path.Count==0)return false;
            if(!StepTo(actor,path.Peek(),step))path.Dequeue();return path.Count>0;
        }
        void AppendRoute(Queue<Vector3> path,Vector3 from,Vector3 to) {
            bool[] blocked=new bool[120];
            foreach(var p in Data.Layout){var d=RestaurantCatalog.Find(p.CatalogId);if(!d.OccupiesFloor)continue;int w=p.Rotation%2==0?d.Width:d.Depth,h=p.Rotation%2==0?d.Depth:d.Width;for(int x=p.X;x<p.X+w;x++)for(int z=p.Z;z<p.Z+h;z++)if(x>=0&&x<12&&z>=0&&z<10)blocked[x+z*12]=true;}
            int start=NearestFree(from,blocked),goal=NearestFree(to,blocked);int[] previous=Enumerable.Repeat(-1,120).ToArray();var frontier=new Queue<int>();frontier.Enqueue(start);previous[start]=start;
            int[] offsets={-1,1,-12,12};
            while(frontier.Count>0){int c=frontier.Dequeue();if(c==goal)break;foreach(int offset in offsets){int n=c+offset;if(n<0||n>=120||Mathf.Abs(n%12-c%12)>1||blocked[n]||previous[n]>=0)continue;previous[n]=c;frontier.Enqueue(n);}}
            if(previous[goal]<0)return;
            var reverse=new List<int>();for(int c=goal;c!=start;c=previous[c])reverse.Add(c);reverse.Reverse();
            path.Enqueue(CellCenter(start%12,start/12));foreach(int c in reverse)path.Enqueue(CellCenter(c%12,c/12));
        }
        static int NearestFree(Vector3 point,bool[] blocked) {int best=0;float distance=float.MaxValue;for(int i=0;i<120;i++){if(blocked[i])continue;float d=(CellCenter(i%12,i/12)-point).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best;}
        static GameObject SmallShape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Color color) {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;Destroy(g.GetComponent<Collider>());
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;g.GetComponent<Renderer>().sharedMaterial=mat;return g;
        }
        static GameObject MakeDish(string id,Transform parent) {
            var root=new GameObject("Plated "+id);root.transform.SetParent(parent,false);
            SmallShape("Ceramic plate",PrimitiveType.Cylinder,root.transform,Vector3.zero,new Vector3(.46f,.025f,.46f),new Color(.96f,.89f,.7f));
            if(id=="burger"||id=="midnight"){
                var bun=id=="midnight"?new Color(.32f,.23f,.49f):new Color(.82f,.52f,.2f);
                SmallShape("Lower bun",PrimitiveType.Cylinder,root.transform,new Vector3(0,.065f,0),new Vector3(.3f,.045f,.3f),bun);
                SmallShape("Patty",PrimitiveType.Cylinder,root.transform,new Vector3(0,.12f,0),new Vector3(.32f,.03f,.32f),new Color(.28f,.13f,.08f));
                SmallShape("Lettuce",PrimitiveType.Cylinder,root.transform,new Vector3(0,.15f,0),new Vector3(.34f,.012f,.34f),new Color(.32f,.56f,.24f));
                SmallShape("Domed bun",PrimitiveType.Sphere,root.transform,new Vector3(0,.2f,0),new Vector3(.31f,.15f,.31f),bun);
            }else{
                SmallShape("Bowl",PrimitiveType.Sphere,root.transform,new Vector3(0,.07f,0),new Vector3(.34f,.14f,.34f),new Color(.87f,.49f,.32f));
                SmallShape("Dish filling",PrimitiveType.Cylinder,root.transform,new Vector3(0,.135f,0),new Vector3(.31f,.012f,.31f),id=="salad"?new Color(.31f,.57f,.22f):id=="dessert"?new Color(.48f,.22f,.51f):new Color(.82f,.49f,.17f));
                for(int i=0;i<4;i++)SmallShape("Garnish",PrimitiveType.Sphere,root.transform,new Vector3(Mathf.Cos(i*1.5f)*.085f,.16f,Mathf.Sin(i*1.5f)*.085f),Vector3.one*.065f,new Color(.87f,.25f,.15f));
            }return root;
        }
        public void PlayChime(bool purchase) {
            if(!chime){chime=gameObject.AddComponent<AudioSource>();chime.volume=.12f;purchaseTone=Tone(660,990);saleTone=Tone(440,660);}
            chime.PlayOneShot(purchase?purchaseTone:saleTone);
        }
        static AudioClip Tone(float first,float second) {
            int count=11025;float[] values=new float[count];for(int i=0;i<count;i++){float t=i/22050f;values[i]=Mathf.Sin(2*Mathf.PI*(t<.16f?first:second)*t)*Mathf.Exp(-t*8)*.4f;}
            var clip=AudioClip.Create("Original register chime",count,1,22050,false);clip.SetData(values,0);return clip;
        }
    }
}
