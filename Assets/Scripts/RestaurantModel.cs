using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantCity {
    public enum CatalogCategory { Kitchen, Seating, Finishes, Lighting, Decor, Exterior }
    public enum RestaurantOrderStage { Waiting, Cooking, Ready, Eating, Leaving }
    public enum StaffJob { Off, Cook, Serve, Clean }

    public class CatalogItem {
        public string Id, Name, Description;
        public CatalogCategory Category;
        public int Price, Width, Depth, Seats, Ambience, RequiredStars;
        public bool IsFinish => Category == CatalogCategory.Finishes;
        public bool IsExterior => Category == CatalogCategory.Exterior;
        public bool OccupiesFloor => !IsFinish && !IsExterior && Id != "pendant_amber" && Id != "neon_moon" && Id != "art_orbit" && Id != "rug_sunset";
        public CatalogItem(string id,string name,CatalogCategory category,int price,int width,int depth,int seats,int ambience,string description,int stars=1) {
            Id=id;Name=name;Category=category;Price=price;Width=width;Depth=depth;Seats=seats;Ambience=ambience;Description=description;RequiredStars=stars;
        }
    }
    public class DishDefinition {
        public string Id,Name,Equipment,Description;
        public int Price,ProteinCost,ProduceCost,RequiredStars;
        public float CookSeconds;
        public bool RequiresMidnight;
        public DishDefinition(string id,string name,int price,int protein,int produce,float cook,string equipment,int stars,bool midnight,string description) {
            Id=id;Name=name;Price=price;ProteinCost=protein;ProduceCost=produce;CookSeconds=cook;Equipment=equipment;RequiredStars=stars;RequiresMidnight=midnight;Description=description;
        }
    }
    public class CustomerDefinition {
        public int Id; public string Name,FavoriteDish,Description;
        public float Patience,AmbienceWeight,CleanlinessWeight;
        public CustomerDefinition(int id,string name,string favorite,float patience,float ambience,float clean,string description) {
            Id=id;Name=name;FavoriteDish=favorite;Patience=patience;AmbienceWeight=ambience;CleanlinessWeight=clean;Description=description;
        }
    }
    public class StaffDefinition {
        public string Id,Name,Description; public StaffJob Role; public int Cost;
        public StaffDefinition(string id,string name,StaffJob role,int cost,string description) {Id=id;Name=name;Role=role;Cost=cost;Description=description;}
    }
    public static class RestaurantCatalog {
        public static readonly CatalogItem[] Items = {
            new CatalogItem("prep_bench","Steel prep bench",CatalogCategory.Kitchen,28,2,1,0,0,"Adds prep capacity; salads cook 25% faster per extra bench."),
            new CatalogItem("grill","Comet grill",CatalogCategory.Kitchen,45,2,1,0,1,"Burgers and midnight buns. Extra grills add a cooking slot."),
            new CatalogItem("stove","Little red stove",CatalogCategory.Kitchen,55,1,1,0,1,"Unlocks planet soup and adds one cooking slot."),
            new CatalogItem("oven","Starlight oven",CatalogCategory.Kitchen,100,2,1,0,2,"TWO STARS: unlocks moonberry tart and adds a cooking slot.",2),
            new CatalogItem("fridge","Mint refrigerator",CatalogCategory.Kitchen,40,1,1,0,1,"Raises each stock limit from 24 to 48; keeps ready dishes fresh longer."),
            new CatalogItem("stool_pair","Counter stools",CatalogCategory.Seating,20,2,2,2,1,"Two inexpensive customer seats."),
            new CatalogItem("cafe_table","Daisy cafe table",CatalogCategory.Seating,30,2,2,2,2,"Two seats and a cheery tabletop."),
            new CatalogItem("booth_teal","Teal diner booth",CatalogCategory.Seating,65,3,2,4,4,"Four seats; a proper neighborhood hangout."),
            new CatalogItem("booth_coral","Coral diner booth",CatalogCategory.Seating,65,3,2,4,4,"Four seats with warm coral upholstery."),
            new CatalogItem("communal_table","Community table",CatalogCategory.Seating,90,4,2,6,3,"Six seats; makes rushes busier and more profitable."),
            new CatalogItem("wall_cream","Cream plaster",CatalogCategory.Finishes,18,1,1,0,2,"Repaints all interior walls; replaces the current paint."),
            new CatalogItem("wall_teal","Lagoon walls",CatalogCategory.Finishes,24,1,1,0,3,"Repaints all interior walls in saturated teal."),
            new CatalogItem("wall_rose","Rose walls",CatalogCategory.Finishes,24,1,1,0,3,"Repaints all interior walls in dusty rose."),
            new CatalogItem("floor_checker","Diner checkerboard",CatalogCategory.Finishes,25,1,1,0,3,"Replaces the worn floor with crisp diner tiles."),
            new CatalogItem("floor_wood","Honey wood floor",CatalogCategory.Finishes,30,1,1,0,4,"Replaces the worn floor with warm wood planks."),
            new CatalogItem("pendant_amber","Amber pendant",CatalogCategory.Lighting,16,1,1,0,2,"Warm overhead light; can hang over furniture."),
            new CatalogItem("globe_lamp","Lunar floor lamp",CatalogCategory.Lighting,22,1,1,0,3,"Soft globe lighting for a cozy dining corner."),
            new CatalogItem("neon_moon","Crescent neon",CatalogCategory.Lighting,42,1,1,0,4,"A glowing moon sculpture for late-night diners."),
            new CatalogItem("fern","Giant fern",CatalogCategory.Decor,12,1,1,0,2,"Leafy, cheerful ambience in a clay pot."),
            new CatalogItem("art_orbit","Orbit print",CatalogCategory.Decor,15,1,1,0,2,"An original space-travel print; mounts above furniture."),
            new CatalogItem("rug_sunset","Sunset rug",CatalogCategory.Decor,20,2,2,0,2,"A woven coral rug; fits under tables."),
            new CatalogItem("jukebox","Rocket jukebox",CatalogCategory.Decor,110,1,1,0,6,"TWO STARS: the district's most coveted statement piece.",2),
            new CatalogItem("awning_coral","Coral street awning",CatalogCategory.Exterior,35,1,1,0,3,"Transforms the restaurant frontage with striped canvas."),
            new CatalogItem("sign_neon","Orbit Cafe neon sign",CatalogCategory.Exterior,85,1,1,0,5,"TWO STARS: your restaurant becomes a glowing local landmark.",2)
        };
        public static readonly DishDefinition[] Dishes = {
            new DishDefinition("burger","Comet burger",14,1,1,9,"grill",1,false,"The reliable favorite. Uses 1 protein + 1 produce."),
            new DishDefinition("salad","Garden galaxy",11,0,2,5,"prep_bench",1,false,"Quick vegetarian salad. Uses 2 produce."),
            new DishDefinition("soup","Planet soup",18,1,1,12,"stove",1,false,"Comfort food for mushroom folk. Needs a stove."),
            new DishDefinition("midnight","Midnight bun",25,2,1,11,"grill",1,true,"Rare city recipe. Aliens and night owls seek it out."),
            new DishDefinition("dessert","Moonberry tart",24,0,2,13,"oven",2,false,"Two-star showpiece. Needs the Starlight oven.")
        };
        public static readonly CustomerDefinition[] Customers = {
            new CustomerDefinition(0,"Rush-hour Rae","burger",70,.7f,1,"Human commuter. Loves burgers; impatient on busy days."),
            new CustomerDefinition(1,"Zeli of the moons","midnight",105,1.2f,.8f,"Long-eared alien. Seeks the rare midnight bun."),
            new CustomerDefinition(2,"Velvet the moth","salad",110,1.8f,.8f,"Moth artist. Loves colorful rooms and fresh salads."),
            new CustomerDefinition(3,"Unit P-04","burger",90,.4f,1.8f,"Robot inspector. Particularly sensitive to dirty tables."),
            new CustomerDefinition(4,"Gloop","soup",130,.7f,.5f,"Jelly creature. Relaxed, patient, and fond of soup."),
            new CustomerDefinition(5,"Morel","soup",100,1.1f,1.5f,"Mushroom gardener. Enjoys comforting food and clean spaces."),
            new CustomerDefinition(6,"Bront","burger",85,.6f,.8f,"Broad horned dockworker. Hungry for a hearty burger."),
            new CustomerDefinition(7,"Professor Ink","midnight",115,1.5f,1.2f,"Tentacled scholar. Savors rare recipes and good ambience.")
        };
        public static readonly StaffDefinition[] Staff = {
            new StaffDefinition("ember","Ember / brass robot",StaffJob.Cook,70,"Fast cook: starts a dish every 3 seconds. $1 wage per served order while assigned."),
            new StaffDefinition("moss","Moss / mushroom sprite",StaffJob.Serve,55,"Quick server and cleaner: a task every 3 seconds. $1 wage per served order while assigned.")
        };
        public static CatalogItem Find(string id) => Array.Find(Items,i=>i.Id==id);
        public static DishDefinition Dish(string id) => Array.Find(Dishes,i=>i.Id==id);
        public static StaffDefinition Worker(string id) => Array.Find(Staff,i=>i.Id==id);
    }

    [Serializable] public class PlacedItem { public int InstanceId,X,Z,Rotation,Paid; public string CatalogId; }
    [Serializable] public class RestaurantOrder {
        public int Id,CustomerType,SeatInstanceId; public string DishId;
        public RestaurantOrderStage Stage; public float Wait,CookProgress,Quality=1,StageTime;
    }
    [Serializable] public class RestaurantReview { public string Customer,Comment; public float Score; public string DishId; }
    [Serializable] public class WorkerState { public string Id; public StaffJob Job; public int TasksCompleted; }
    [Serializable] public class RestaurantState {
        public bool Owned,Open;
        public int Produce,Protein,Served,Lost,Earnings,Rank=1,NextInstanceId=1,NextOrderId=1;
        public float Cleanliness=42,Satisfaction=50,ServiceSeconds;
        public List<PlacedItem> Layout=new List<PlacedItem>();
        public List<string> ActiveMenu=new List<string>{"burger","salad"};
        public List<RestaurantOrder> Orders=new List<RestaurantOrder>();
        public List<RestaurantReview> Reviews=new List<RestaurantReview>();
        public List<WorkerState> Workers=new List<WorkerState>();
        public int Stars => Rank;
        public bool CanCustomize => Owned && !Open && Orders.Count==0;
        public int Seats => Layout.Sum(p=>RestaurantCatalog.Find(p.CatalogId)?.Seats??0);
        public int Ambience => Math.Min(40,Layout.Sum(p=>RestaurantCatalog.Find(p.CatalogId)?.Ambience??0));
        public int CookSlots => Math.Max(1,Layout.Count(p=>p.CatalogId=="grill"||p.CatalogId=="stove"||p.CatalogId=="oven"));
        public int StockLimit => HasEquipment("fridge")?48:24;
        public int WagesPerOrder => Workers.Count(w=>w.Job!=StaffJob.Off);
        public string WallId => Layout.FindLast(p=>p.CatalogId.StartsWith("wall_"))?.CatalogId??"wall_shabby";
        public string FloorId => Layout.FindLast(p=>p.CatalogId.StartsWith("floor_"))?.CatalogId??"floor_shabby";
        public string StarProgress => Rank>=2?"TWO STARS • oven, moonberry tart, jukebox & neon sign unlocked":$"Two stars: {Served}/20 served • {Satisfaction:0}/75 satisfaction • {Ambience}/12 ambience";
        public bool HasEquipment(string id) => Layout.Exists(p=>p.CatalogId==id);
        static bool Fail(string text,out string message) {message=text;return false;}
        public bool BuyRestaurant(GameState wallet,out string message) {
            if(Owned)return Fail("This restaurant already belongs to you.",out message);
            if(wallet.Cash<150)return Fail("The lease costs $150. Keep earning at your stand.",out message);
            wallet.Cash-=150;Owned=true;Produce=12;Protein=8;
            AddPlaced("prep_bench",0,0,0,0);AddPlaced("grill",3,0,0,0);AddPlaced("cafe_table",0,3,0,0);
            message="Your first restaurant! Starter equipment and 20 ingredients included. Open service when ready.";return true;
        }
        void AddPlaced(string id,int x,int z,int rotation,int paid) {Layout.Add(new PlacedItem{InstanceId=NextInstanceId++,CatalogId=id,X=x,Z=z,Rotation=rotation%4,Paid=paid});}
        public bool CanPlace(string id,int x,int z,int rotation,int ignoreInstance,out string message) {
            var item=RestaurantCatalog.Find(id);
            if(item==null)return Fail("Unknown catalog item.",out message);
            if(rotation<0||rotation>3)return Fail("Rotation must be 0, 1, 2, or 3.",out message);
            if(item.RequiredStars>Stars)return Fail("Reach two stars to unlock this item.",out message);
            if(item.IsFinish||item.IsExterior){message="Ready to install.";return true;}
            int width=rotation%2==0?item.Width:item.Depth,depth=rotation%2==0?item.Depth:item.Width;
            if(x<0||z<0||x+width>12||z+depth>10)return Fail("Keep the furnishing inside the restaurant.",out message);
            if(item.OccupiesFloor&&z+depth>2&&x<7&&x+width>5)return Fail("Leave the central aisle clear for customers.",out message);
            foreach(var p in Layout) {
                if(p.InstanceId==ignoreInstance)continue;
                var other=RestaurantCatalog.Find(p.CatalogId);if(other==null||other.IsFinish||other.IsExterior)continue;
                if(!item.OccupiesFloor||!other.OccupiesFloor) {if(item.Id!=other.Id)continue;}
                int w=p.Rotation%2==0?other.Width:other.Depth,d=p.Rotation%2==0?other.Depth:other.Width;
                if(x<p.X+w&&x+width>p.X&&z<p.Z+d&&z+depth>p.Z)return Fail("That space is occupied.",out message);
            }
            if(!HasLayoutAccess(id,x,z,rotation,ignoreInstance))return Fail("Leave an accessible side of every table and kitchen station.",out message);
            message="Clear space. Ready to place.";return true;
        }
        bool HasLayoutAccess(string id,int x,int z,int rotation,int ignoreInstance) {
            var candidate=new PlacedItem{CatalogId=id,X=x,Z=z,Rotation=rotation};
            var placed=Layout.Where(p=>p.InstanceId!=ignoreInstance).ToList();placed.Add(candidate);
            bool[,] blocked=new bool[12,10],visited=new bool[12,10];
            foreach(var p in placed){var d=RestaurantCatalog.Find(p.CatalogId);if(!d.OccupiesFloor)continue;int w=p.Rotation%2==0?d.Width:d.Depth,h=p.Rotation%2==0?d.Depth:d.Width;for(int xx=p.X;xx<p.X+w;xx++)for(int zz=p.Z;zz<p.Z+h;zz++){if(xx<0||xx>=12||zz<0||zz>=10)return false;blocked[xx,zz]=true;}}
            var queue=new Queue<int>();if(blocked[5,9])return false;queue.Enqueue(5+9*12);visited[5,9]=true;
            int[] dx={-1,1,0,0},dz={0,0,-1,1};
            while(queue.Count>0){int cell=queue.Dequeue(),cx=cell%12,cz=cell/12;for(int k=0;k<4;k++){int nx=cx+dx[k],nz=cz+dz[k];if(nx<0||nx>=12||nz<0||nz>=10||blocked[nx,nz]||visited[nx,nz])continue;visited[nx,nz]=true;queue.Enqueue(nx+nz*12);}}
            foreach(var p in placed){var d=RestaurantCatalog.Find(p.CatalogId);if(d.Seats==0&&d.Category!=CatalogCategory.Kitchen)continue;int w=p.Rotation%2==0?d.Width:d.Depth,h=p.Rotation%2==0?d.Depth:d.Width;bool reachable=false;
                for(int xx=p.X-1;xx<=p.X+w;xx++)for(int zz=p.Z-1;zz<=p.Z+h;zz++){
                    bool side=(xx>=p.X&&xx<p.X+w&&(zz==p.Z-1||zz==p.Z+h))||(zz>=p.Z&&zz<p.Z+h&&(xx==p.X-1||xx==p.X+w));
                    if(side&&xx>=0&&xx<12&&zz>=0&&zz<10&&visited[xx,zz])reachable=true;
                }
                if(!reachable)return false;
            }return true;
        }
        public bool Place(GameState wallet,string id,int x,int z,int rotation,out string message) {
            if(!CanCustomize)return Fail("Close service and wait for customers to leave before renovating.",out message);
            if(!CanPlace(id,x,z,rotation,-1,out message))return false;
            var item=RestaurantCatalog.Find(id);
            if(wallet.Cash<item.Price)return Fail($"You need ${item.Price} for {item.Name}.",out message);
            if((item.IsFinish||item.IsExterior)&&HasEquipment(id))return Fail("This improvement is already installed.",out message);
            wallet.Cash-=item.Price;
            if(item.IsFinish){string prefix=id.StartsWith("wall_")?"wall_":"floor_";Layout.RemoveAll(p=>p.CatalogId.StartsWith(prefix));}
            AddPlaced(id,x,z,rotation,item.Price);UpdateRank();message=$"Installed {item.Name}. {item.Description}";return true;
        }
        public bool Move(int instanceId,int x,int z,int rotation,out string message) {
            if(!CanCustomize)return Fail("Close service and wait for customers to leave before moving furnishings.",out message);
            var p=Layout.Find(i=>i.InstanceId==instanceId);if(p==null)return Fail("That furnishing no longer exists.",out message);
            if(RestaurantCatalog.Find(p.CatalogId).IsFinish||RestaurantCatalog.Find(p.CatalogId).IsExterior)return Fail("This improvement applies to the whole building.",out message);
            if(!CanPlace(p.CatalogId,x,z,rotation,instanceId,out message))return false;
            p.X=x;p.Z=z;p.Rotation=rotation;message="Furnishing moved.";return true;
        }
        public bool Sell(GameState wallet,int instanceId,out string message) {
            if(!CanCustomize)return Fail("Close service and let customers leave before selling furnishings.",out message);
            var p=Layout.Find(i=>i.InstanceId==instanceId);if(p==null)return Fail("That furnishing no longer exists.",out message);
            if((p.CatalogId=="prep_bench"||p.CatalogId=="grill")&&Layout.Count(i=>i.CatalogId==p.CatalogId)<=1)return Fail("Install a replacement before selling your last essential kitchen station.",out message);
            if(RestaurantCatalog.Find(p.CatalogId).Seats>0&&Seats<=RestaurantCatalog.Find(p.CatalogId).Seats)return Fail("Install replacement seating before selling your last table.",out message);
            int refund=p.Paid/2;Layout.Remove(p);wallet.Cash+=refund;message=$"Sold for ${refund}. Starter furnishings have no resale value.";return true;
        }
        public bool IsDishAvailable(GameState wallet,string id) {var d=RestaurantCatalog.Dish(id);return d!=null&&Stars>=d.RequiredStars&&(!d.RequiresMidnight||wallet.RecipeUnlocked)&&HasEquipment(d.Equipment);}
        public bool ToggleDish(GameState wallet,string id,out string message) {
            if(!Owned)return Fail("Buy the restaurant first.",out message);
            if(ActiveMenu.Contains(id)){if(ActiveMenu.Count<=1)return Fail("Keep at least one dish on the menu.",out message);ActiveMenu.Remove(id);message="Dish removed from tomorrow's orders.";return true;}
            if(!IsDishAvailable(wallet,id))return Fail("This dish needs its recipe, equipment, and star rank unlocked.",out message);
            ActiveMenu.Add(id);message="Dish added to the menu. New customers can order it.";return true;
        }
        public bool Restock(GameState wallet,bool protein,out string message) {
            if(!Owned)return Fail("Buy the restaurant first.",out message);
            int stock=protein?Protein:Produce,price=protein?10:6;
            if(stock+6>StockLimit)return Fail($"Not enough storage; limit {StockLimit}. Buy a fridge for 48.",out message);
            if(wallet.Cash<price)return Fail($"Six portions cost ${price}.",out message);
            wallet.Cash-=price;if(protein)Protein+=6;else Produce+=6;message=$"Stocked 6 {(protein?"protein":"produce")} portions for ${price}.";return true;
        }
        public bool RequestSupplyHelp(GameState wallet,out string message) {
            if(!Owned||wallet.Cash>=6||Produce!=0||!HasEquipment("prep_bench")||Orders.Any(o=>o.Stage==RestaurantOrderStage.Cooking||o.Stage==RestaurantOrderStage.Ready))return Fail("Emergency produce is available when you are out of produce, have no dishes cooking, and cannot afford a supply pack.",out message);
            Produce=2;if(!ActiveMenu.Contains("salad"))ActiveMenu.Add("salad");
            // Waiting customers can accept the emergency dish so a depleted burger order cannot trap a new owner.
            foreach(var order in Orders.Where(o=>o.Stage==RestaurantOrderStage.Waiting)){order.DishId="salad";break;}
            message="The supplier gives you 2 emergency produce. Garden galaxy is on your menu; serve a salad to get back on your feet.";return true;
        }
        public bool StartService(GameState wallet,out string message) {
            if(!Owned||Open)return Fail("Service is unavailable or already open.",out message);
            if(Seats<1)return Fail("Install customer seating before opening.",out message);
            if(!ActiveMenu.Any(id=>IsDishAvailable(wallet,id)))return Fail("Install equipment for at least one menu dish.",out message);
            Open=true;ServiceSeconds=0;message="OPEN! Customers can arrive. Close service at any time to stop new arrivals.";return true;
        }
        public bool EndService(out string message) {if(!Open)return Fail("Service is already closed.",out message);Open=false;message="CLOSED to new arrivals. Finish current orders; renovate once everyone leaves.";return true;}
        public RestaurantOrder AddCustomer(GameState wallet,int type,int seatInstanceId,out string message) {
            if(!Open||type<0||type>=RestaurantCatalog.Customers.Length){message="Restaurant is closed or customer is unavailable.";return null;}
            var seat=Layout.Find(p=>p.InstanceId==seatInstanceId);int capacity=seat==null?0:RestaurantCatalog.Find(seat.CatalogId).Seats;
            if(capacity<=Orders.Count(o=>o.SeatInstanceId==seatInstanceId&&o.Stage!=RestaurantOrderStage.Leaving)){message="All seats here are occupied.";return null;}
            var available=ActiveMenu.Where(id=>IsDishAvailable(wallet,id)).ToList();if(available.Count==0){message="No available menu dishes.";return null;}
            var customer=RestaurantCatalog.Customers[type];string dish=available.Contains(customer.FavoriteDish)?customer.FavoriteDish:available[(NextOrderId+type)%available.Count];
            var order=new RestaurantOrder{Id=NextOrderId++,CustomerType=type,DishId=dish,SeatInstanceId=seatInstanceId};Orders.Add(order);message=$"{customer.Name} ordered {RestaurantCatalog.Dish(dish).Name}.";return order;
        }
        public float CookTime(RestaurantOrder order) {var d=RestaurantCatalog.Dish(order.DishId);return d.CookSeconds*(d.Id=="salad"?Math.Max(.5f,1-.25f*(Layout.Count(p=>p.CatalogId=="prep_bench")-1)):1);}
        public bool BeginCooking(int id,out string message) {
            var o=Orders.Find(x=>x.Id==id);if(o==null||o.Stage!=RestaurantOrderStage.Waiting)return Fail("Choose a waiting order.",out message);
            var d=RestaurantCatalog.Dish(o.DishId);if(!HasEquipment(d.Equipment))return Fail("The dish's required equipment is missing.",out message);
            if(Orders.Count(x=>x.Stage==RestaurantOrderStage.Cooking)>=CookSlots)return Fail("All cooking slots are busy. Extra grills and stoves add capacity.",out message);
            if(Protein<d.ProteinCost||Produce<d.ProduceCost)return Fail("Insufficient ingredients. Restock at the city supplier.",out message);
            Protein-=d.ProteinCost;Produce-=d.ProduceCost;o.Stage=RestaurantOrderStage.Cooking;o.StageTime=0;o.CookProgress=0;message=$"Cooking {d.Name}.";return true;
        }
        public bool CompleteServing(GameState wallet,int id,out string message) {
            var o=Orders.Find(x=>x.Id==id);if(o==null||o.Stage!=RestaurantOrderStage.Ready)return Fail("Choose a ready dish to serve.",out message);
            var c=RestaurantCatalog.Customers[o.CustomerType];var d=RestaurantCatalog.Dish(o.DishId);
            float waitRatio=o.Wait/c.Patience,score=45+o.Quality*30+(waitRatio<.35f?10:waitRatio>.7f?-15:0)+Math.Min(10,Ambience*.55f)*c.AmbienceWeight+(Cleanliness-60)*.15f*c.CleanlinessWeight+(c.FavoriteDish==o.DishId?5:0);
            score=Clamp(score,0,100);int tip=score>=85?3:score>=70?1:0,wage=WagesPerOrder,revenue=Math.Max(0,d.Price+tip-wage);
            wallet.Cash+=revenue;Earnings+=revenue;Served++;Cleanliness=Math.Max(0,Cleanliness-4);
            string food=o.Quality>.85f?"Food fresh":o.Quality>.6f?"Food cooled":"Food sat too long";
            string wait=waitRatio<.35f?"short wait":waitRatio>.7f?"long wait":"reasonable wait";
            string room=Cleanliness<45?"dirty tables":Cleanliness>75?"spotless room":"room could be cleaner";
            AddReview(o,score,$"{food}; {wait}; {room}; {(Ambience>=12?"lovely ambience":"plain surroundings")}{(c.FavoriteDish==o.DishId?"; my favorite dish!":".")}");
            o.Stage=RestaurantOrderStage.Eating;o.StageTime=0;UpdateRank();message=$"{c.Name}: {score:0}% satisfaction. +${d.Price+tip} ({(wage>0?$"${wage} staff wages":"no wages")}).";return true;
        }
        void AddReview(RestaurantOrder o,float score,string comment) {
            Reviews.Insert(0,new RestaurantReview{Customer=RestaurantCatalog.Customers[o.CustomerType].Name,Score=score,Comment=comment,DishId=o.DishId});
            if(Reviews.Count>12)Reviews.RemoveAt(Reviews.Count-1);Satisfaction=Reviews.Average(r=>r.Score);
        }
        void UpdateRank(){if(Rank<2&&Served>=20&&Satisfaction>=75&&Ambience>=12)Rank=2;}
        public bool Clean(out string message) {if(!Owned)return Fail("Buy the restaurant first.",out message);if(Cleanliness>=100)return Fail("The restaurant is already spotless.",out message);Cleanliness=Math.Min(100,Cleanliness+25);message=$"Tables wiped. Cleanliness {Cleanliness:0}%.";return true;}
        public bool Hire(GameState wallet,string id,out string message) {
            var d=RestaurantCatalog.Worker(id);if(!Owned||d==null)return Fail("Worker unavailable.",out message);
            if(Workers.Exists(w=>w.Id==id))return Fail("This worker already works here.",out message);
            if(wallet.Cash<d.Cost)return Fail($"Hiring {d.Name} costs ${d.Cost}.",out message);
            wallet.Cash-=d.Cost;Workers.Add(new WorkerState{Id=id,Job=d.Role});message=$"Hired {d.Name}. Assigned {d.Role}. $1 per served order while assigned.";return true;
        }
        public bool Assign(string id,StaffJob job,out string message) {
            var w=Workers.Find(x=>x.Id==id);if(w==null)return Fail("Hire this worker first.",out message);
            if(!Enum.IsDefined(typeof(StaffJob),job))return Fail("Choose a valid job.",out message);
            w.Job=job;message=$"{RestaurantCatalog.Worker(id).Name}: {job}.";return true;
        }
        public float WorkerActionSeconds(StaffJob job) {
            var assigned=Workers.Where(w=>w.Job==job).ToList();if(assigned.Count==0)return 8;
            return assigned.Any(w=>(w.Id=="ember"&&job==StaffJob.Cook)||(w.Id=="moss"&&(job==StaffJob.Serve||job==StaffJob.Clean)))?3:7;
        }
        public void Tick(GameState wallet,float seconds) {
            if(!Owned||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            if(Open)ServiceSeconds+=seconds;
            foreach(var o in Orders) {
                o.StageTime+=seconds;
                if(o.Stage==RestaurantOrderStage.Leaving)continue;
                if(o.Stage==RestaurantOrderStage.Eating){if(o.StageTime>=8){o.Stage=RestaurantOrderStage.Leaving;o.StageTime=0;}continue;}
                o.Wait+=seconds;
                if(o.Stage==RestaurantOrderStage.Cooking){o.CookProgress+=seconds;if(o.CookProgress>=CookTime(o)){o.Stage=RestaurantOrderStage.Ready;o.StageTime=0;}}
                if(o.Stage==RestaurantOrderStage.Ready)o.Quality=Clamp(1-Math.Max(0,o.StageTime-(HasEquipment("fridge")?40:20))*.015f,.35f,1);
                if(o.Wait>RestaurantCatalog.Customers[o.CustomerType].Patience){Lost++;AddReview(o,15,"Waited too long and left hungry. Start cooking sooner, add equipment, or hire help.");o.Stage=RestaurantOrderStage.Leaving;o.StageTime=0;}
            }
            Orders.RemoveAll(o=>o.Stage==RestaurantOrderStage.Leaving&&o.StageTime>=6);UpdateRank();
        }
        static float Clamp(float value,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?min:Math.Max(min,Math.Min(max,value));
        public void SanitizeAfterLoad() {
            Layout=Layout??new List<PlacedItem>();ActiveMenu=ActiveMenu??new List<string>{"burger","salad"};Orders=Orders??new List<RestaurantOrder>();Reviews=Reviews??new List<RestaurantReview>();Workers=Workers??new List<WorkerState>();
            Rank=Math.Max(1,Math.Min(2,Rank));
            Layout.RemoveAll(p=>p==null||RestaurantCatalog.Find(p.CatalogId)==null);Workers.RemoveAll(w=>w==null||RestaurantCatalog.Worker(w.Id)==null);
            var incoming=Layout;Layout=new List<PlacedItem>();var seenIds=new HashSet<int>();
            NextInstanceId=Math.Max(1,NextInstanceId);
            foreach(var p in incoming){p.Paid=Math.Max(0,Math.Min(p.Paid,RestaurantCatalog.Find(p.CatalogId).Price));p.Rotation=(p.Rotation%4+4)%4;
                if(!CanPlace(p.CatalogId,p.X,p.Z,p.Rotation,-1,out _))continue;
                var def=RestaurantCatalog.Find(p.CatalogId);
                if(def.IsFinish){string prefix=p.CatalogId.StartsWith("wall_")?"wall_":"floor_";Layout.RemoveAll(existing=>existing.CatalogId.StartsWith(prefix));}
                if(def.IsExterior&&HasEquipment(p.CatalogId))continue;
                if(p.InstanceId<1||seenIds.Contains(p.InstanceId)){while(seenIds.Contains(NextInstanceId))NextInstanceId++;p.InstanceId=NextInstanceId++;}
                seenIds.Add(p.InstanceId);Layout.Add(p);
            }
            NextInstanceId=Math.Max(NextInstanceId,Layout.Count==0?1:Layout.Max(p=>p.InstanceId)+1);
            ActiveMenu=ActiveMenu.Where(id=>RestaurantCatalog.Dish(id)!=null).Distinct().ToList();if(ActiveMenu.Count==0)ActiveMenu.Add("burger");
            // Unfinished service ends on load; durable restaurant layout, finances, menu, stock, reviews and workers survive.
            Orders.Clear();Open=false;Produce=Math.Max(0,Math.Min(StockLimit,Produce));Protein=Math.Max(0,Math.Min(StockLimit,Protein));
            Cleanliness=Clamp(Cleanliness,0,100);Satisfaction=Clamp(Satisfaction,0,100);Rank=Math.Max(1,Math.Min(2,Rank));Served=Math.Max(0,Served);Lost=Math.Max(0,Lost);UpdateRank();
        }
    }
}
