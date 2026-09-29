using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantCity {
    public enum CatalogCategory { Kitchen, Seating, Finishes, Lighting, Decor, Exterior }
    public enum RestaurantOrderStage { Waiting, Cooking, Ready, Eating, Leaving }
    public enum StaffJob { Off, Cook, Serve, Clean, Stand, Any }

    public class CatalogItem {
        public string Id, Name, Description;
        public CatalogCategory Category;
        public int Price, Width, Depth, Seats, Ambience, RequiredStars, Tier;   // Tier = reputation rank needed (0 Old Market, 1 Docks, ...)
        public bool IsFinish => Category == CatalogCategory.Finishes;
        public bool IsExterior => Category == CatalogCategory.Exterior;
        public bool OccupiesFloor => !IsFinish && !IsExterior && Id != "pendant_amber" && Id != "neon_moon" && Id != "art_orbit" && Id != "rug_sunset" && !WallOrCeiling;
        public bool WallOrCeiling => Id == "industrial_pendant" || Id == "wall_sconce" || Id == "poster_wall" || Id == "art_abstract" || Id == "menu_screen" || Id == "wall_tv" || Id == "sign_burger";
        public CatalogItem(string id,string name,CatalogCategory category,int price,int width,int depth,int seats,int ambience,string description,int stars=1,int tier=0) {
            Tier=tier;Id=id;Name=name;Category=category;Price=price;Width=width;Depth=depth;Seats=seats;Ambience=ambience;Description=description;RequiredStars=stars;
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
    // Stage A: recipes as data. Order-independent plate composition replaces the old bit-flag "Parts" field.
    public class RecipeDefinition {
        public string DishId; public List<string> Components; public List<string> Steps;
        public RecipeDefinition(string dishId,string[] components,string[] steps) { DishId=dishId;Components=new List<string>(components);Steps=new List<string>(steps); }
    }
    public static class RecipeBook {
        public static readonly RecipeDefinition[] Recipes = {
            new RecipeDefinition("burger",new[]{"bun","cooked_patty"},new[]{
                "Take a raw patty from the pantry.",
                "Place it on the grill and wait for it to cook.",
                "Take a clean plate (or carry it with you) to the assembly counter.",
                "Add the cooked patty and a bun.",
                "Carry the finished plate to the matching guest."}),
            new RecipeDefinition("salad",new[]{"chopped_greens"},new[]{
                "Take greens from the pantry.",
                "Chop them at the prep bench (hold interact).",
                "Add the chopped greens to a plate.",
                "Carry the finished plate to the matching guest."}),
            new RecipeDefinition("midnight",new[]{"bun","cooked_patty","midnight_sauce"},new[]{
                "Requires the midnight recipe from a night city outing.",
                "Cook a patty like a burger.",
                "Take sauce ingredients from the pantry and prepare them at the prep bench.",
                "Add the cooked patty, a bun, and the midnight sauce.",
                "Carry the finished plate to the matching guest."}),
            new RecipeDefinition("soup",new[]{"soup"},new[]{
                "Buy the recipe in the Cookbook, and soup vegetables from Milo.",
                "Take soup vegetables from the pantry's top shelf and put them in the pot on the stove.",
                "It simmers for 14 seconds. Stir it (E) at least every 9 seconds or it scorches.",
                "Carry a clean plate to the stove and ladle the soup onto it.",
                "Carry the finished soup to the matching guest."})
        };
        // Recipes you can buy in the Cookbook (cash, and the rank that sells them). Others are starters or found in the city.
        public static readonly (string dish,int price,int rank)[] ForSale = { ("soup",60,0) };
        public static string HowToGet(string dish)=>dish=="burger"||dish=="salad"?"Starter recipe":dish=="midnight"?"Beat the rival in the alley at night and open his stash":Array.Exists(ForSale,f=>f.dish==dish)?"Buy it in the Cookbook":"Not available yet";
        public static RecipeDefinition Find(string dishId) => Array.Find(Recipes,r=>r.DishId==dishId);
        public static string Match(List<string> components) {
            if(components==null||components.Count==0)return "";
            foreach(var r in Recipes)if(components.Count==r.Components.Count&&r.Components.All(components.Contains))return r.DishId;
            return "";
        }
    }
    public class CustomerDefinition {
        public int Id; public string Name,FavoriteDish,Description;
        public float Patience,AmbienceWeight,CleanlinessWeight;
        public CustomerDefinition(int id,string name,string favorite,float patience,float ambience,float clean,string description) {
            Id=id;Name=name;FavoriteDish=favorite;Patience=patience*.65f;   // base seconds before they give up; scaled down by ShiftDifficulty as shifts get harderAmbienceWeight=ambience;CleanlinessWeight=clean;Description=description;
        }
    }
    public class StaffDefinition {
        public string Id,Name,Description; public StaffJob Role; public int Cost;
        // Special recruits are customers you win over: serve them RequiredServes times, then spend Flux.
        public int FluxCost,CustomerType=-1,RequiredServes,ModelType;
        public bool Special=>FluxCost>0;
        public StaffDefinition(string id,string name,StaffJob role,int cost,string description,int model) {Id=id;Name=name;Role=role;Cost=cost;Description=description;ModelType=model;}
        public StaffDefinition(string id,string name,StaffJob role,int fluxCost,int customerType,int serves,string description) {Id=id;Name=name;Role=role;FluxCost=fluxCost;CustomerType=customerType;RequiredServes=serves;ModelType=customerType;Description=description;}
    }
    public static class RestaurantCatalog {
        public static readonly CatalogItem[] Items = new CatalogItem[] {
            new CatalogItem("pantry","Ingredient pantry",CatalogCategory.Kitchen,25,1,1,0,0,"Take protein, greens, buns or midnight sauce ingredients."),
            new CatalogItem("plate_rack","Plate rack",CatalogCategory.Kitchen,18,1,1,0,0,"Holds 4 more reusable plates. More seats need more plates: return dirty ones to the sink."),
            new CatalogItem("assembly","Assembly station",CatalogCategory.Kitchen,25,2,1,0,0,"Place a clean plate, then add prepared ingredients."),
            new CatalogItem("counter","Pass counter",CatalogCategory.Kitchen,15,1,1,0,0,"Set anything down here: plates, patties, buns, sauce. Build dishes on it."),
            new CatalogItem("trash","Trash can",CatalogCategory.Kitchen,10,1,1,0,0,"Throw away burnt or unwanted food. Plates keep; food scraps go."),
            new CatalogItem("sink","Deep washing sink",CatalogCategory.Kitchen,30,1,1,0,0,"Wash dirty plates to refill the rack. Upgrade it to wash faster."),
            new CatalogItem("prep_bench","Steel prep bench",CatalogCategory.Kitchen,28,2,1,0,0,"Prepare one ingredient at a time. Extra benches let partners prep together."),
            new CatalogItem("grill","Comet grill",CatalogCategory.Kitchen,45,2,1,0,1,"Burgers and midnight buns. Extra grills add a cooking slot."),
            new CatalogItem("stove","Little red stove",CatalogCategory.Kitchen,55,1,1,0,1,"Simmers Planet soup. Stir it or it scorches.",1,0),
            new CatalogItem("oven","Starlight oven",CatalogCategory.Kitchen,100,2,1,0,2,"LINE COOK gear: cooks patties in six seconds instead of eight.",2,1),
            new CatalogItem("fridge","Mint refrigerator",CatalogCategory.Kitchen,40,1,1,0,1,"Raises each stock limit from 24 to 48; keeps ready dishes fresh longer."),
            new CatalogItem("stool_pair","Counter stools",CatalogCategory.Seating,20,2,2,2,1,"Two inexpensive customer seats."),
            new CatalogItem("cafe_table","Daisy cafe table",CatalogCategory.Seating,30,2,2,2,2,"Two seats and a cheery tabletop."),
            new CatalogItem("booth_teal","Teal diner booth",CatalogCategory.Seating,65,3,2,4,4,"Four seats; a proper neighborhood hangout."),
            new CatalogItem("booth_coral","Coral diner booth",CatalogCategory.Seating,65,3,2,4,4,"Four seats with warm coral upholstery."),
            new CatalogItem("communal_table","Community table",CatalogCategory.Seating,90,4,2,6,3,"Six seats; makes rushes busier and more profitable."),
            new CatalogItem("pendant_amber","Amber pendant",CatalogCategory.Lighting,16,1,1,0,2,"Warm overhead light; can hang over furniture."),
            new CatalogItem("globe_lamp","Lunar floor lamp",CatalogCategory.Lighting,22,1,1,0,3,"Soft globe lighting for a cozy dining corner."),
            new CatalogItem("neon_moon","Crescent neon",CatalogCategory.Lighting,42,1,1,0,4,"A glowing moon sculpture for late-night diners."),
            new CatalogItem("fern","Giant fern",CatalogCategory.Decor,12,1,1,0,2,"Leafy, cheerful ambience in a clay pot."),
            new CatalogItem("art_orbit","Orbit print",CatalogCategory.Decor,15,1,1,0,2,"An original space-travel print; mounts above furniture."),
            new CatalogItem("rug_sunset","Sunset rug",CatalogCategory.Decor,20,2,2,0,2,"A woven coral rug; fits under tables."),
            new CatalogItem("potted_palm","Potted palm",CatalogCategory.Decor,14,1,1,0,2,"A tall palm for a bare corner."),
            new CatalogItem("flower_pot","Little flower pot",CatalogCategory.Decor,8,1,1,0,1,"Cheap, cheerful and hard to kill."),
            new CatalogItem("planter_box","Window planter",CatalogCategory.Decor,10,1,1,0,1,"A long box of greenery."),
            new CatalogItem("display_shelf","Grocery display",CatalogCategory.Decor,22,1,1,0,2,"Stocked shelves that make the place feel busy."),
            new CatalogItem("bottle_shelf","Bottle shelf",CatalogCategory.Decor,18,1,1,0,2,"Old bottles and jars, for character."),
            new CatalogItem("rustic_crates","Rustic crates",CatalogCategory.Decor,10,1,1,0,1,"Market crates stacked as decoration."),
            new CatalogItem("flour_sacks","Flour sacks",CatalogCategory.Decor,8,1,1,0,1,"A pile of sacks: it looks like real cooking happens here."),
            new CatalogItem("oak_barrel","Oak barrel",CatalogCategory.Decor,9,1,1,0,1,"A rustic barrel in the corner."),
            new CatalogItem("lounge_couch","Waiting couch",CatalogCategory.Decor,40,2,1,0,3,"Somewhere comfy for the line to wait."),
            new CatalogItem("statue","Odd little statue",CatalogCategory.Decor,35,1,1,0,3,"Nobody knows who it is. Everyone loves it."),
            new CatalogItem("bistro_table","Checkered bistro table",CatalogCategory.Seating,45,2,2,2,4,"Two seats at a red-checkered tablecloth. Old-school charm."),
            new CatalogItem("industrial_pendant","Edison bulb pendant",CatalogCategory.Lighting,24,1,1,0,3,"A bare filament bulb on a long cord; warm and a little hip."),
            new CatalogItem("wall_sconce","Wall sconce",CatalogCategory.Lighting,18,1,1,0,2,"A slim wall light; mounts on the wall behind its tile."),
            new CatalogItem("poster_wall","Gig poster",CatalogCategory.Decor,10,1,1,0,1,"A band poster from last summer. Mounts on the wall."),
            new CatalogItem("art_abstract","Abstract canvas",CatalogCategory.Decor,28,1,1,0,3,"Big bold colour in a black frame. Mounts on the wall."),
            new CatalogItem("flower_stand","Flower stand",CatalogCategory.Decor,34,2,1,0,4,"Tiered buckets of fresh bouquets by the door."),
            new CatalogItem("cafe_divider","Slatted divider",CatalogCategory.Decor,25,2,1,0,2,"A wooden screen that makes a dining nook."),
            new CatalogItem("menu_screen","Digital menu board",CatalogCategory.Decor,55,2,1,0,4,"A lit menu over the pass. Customers love knowing what's good.",2),
            new CatalogItem("wall_tv","Wall TV",CatalogCategory.Decor,70,2,1,0,4,"The game is on. Guests linger happily.",2),
            new CatalogItem("sign_burger","BURGER neon letters",CatalogCategory.Decor,95,3,1,0,6,"TWO STARS: giant red letters that tell the whole block what you cook.",2),
            new CatalogItem("jukebox","Rocket jukebox",CatalogCategory.Decor,110,1,1,0,6,"TWO STARS: the district's most coveted statement piece.",2),
            new CatalogItem("awning_coral","Coral street awning",CatalogCategory.Exterior,35,1,1,0,3,"Transforms the restaurant frontage with striped canvas."),
            new CatalogItem("sign_neon","Orbit Cafe neon sign",CatalogCategory.Exterior,85,1,1,0,5,"TWO STARS: your restaurant becomes a glowing local landmark.",2)
        }.Concat(FinishCatalog.ShopItems()).ToArray();
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
            new CustomerDefinition(7,"Professor Ink","midnight",115,1.5f,1.2f,"Tentacled scholar. Savors rare recipes and good ambience."),
            new CustomerDefinition(8,"Ember","burger",95,.8f,1.1f,"Brass kitchen automaton on its day off. Critiques every sear."),
            new CustomerDefinition(9,"Moss","salad",120,1.3f,.9f,"Mushroom sprite who loves fresh greens and tidy tables.")
        };
        public static readonly StaffDefinition[] Staff = {
            new StaffDefinition("ember","Ember",StaffJob.Cook,2,8,0,"Brass automaton. Specialty: grill work. Can do any job."),
            new StaffDefinition("moss","Moss",StaffJob.Serve,2,9,0,"Mushroom sprite. Specialty: serving and clearing. Can do any job."),
            new StaffDefinition("velvet","Velvet",StaffJob.Serve,3,2,0,"Moth artist. Specialty: the fastest server in the city. Can do any job."),
            new StaffDefinition("p04","Unit P-04",StaffJob.Clean,3,3,0,"Robot inspector. Specialty: dishes and tables at machine speed. Can do any job."),
            new StaffDefinition("bront","Bront",StaffJob.Cook,3,6,0,"Horned dockworker. Specialty: a powerhouse cook. Can do any job."),
            new StaffDefinition("ink","Professor Ink",StaffJob.Cook,5,7,0,"Tentacled scholar. Specialty: eight arms and a master of midnight burgers. Can do any job.")
        };
        public static CatalogItem Find(string id) => Array.Find(Items,i=>i.Id==id);
        public static DishDefinition Dish(string id) => Array.Find(Dishes,i=>i.Id==id);
        public static StaffDefinition Worker(string id) => Array.Find(Staff,i=>i.Id==id)??ResidentCast.Staff(id);
    }

    [Serializable] public class PlacedItem { public int InstanceId,X,Z,Rotation,Paid,Level=1; public string CatalogId; }
    [Serializable] public class RestaurantOrder {
        public int Id,CustomerType,SeatInstanceId,SeatNumber; public string DishId,ResidentId=""; public float Patience; // SeatNumber is one-based; zero means an older save. Patience 0 = older save (use the customer default).
        public RestaurantOrderStage Stage; public float Wait,CookProgress,Quality=1,StageTime;
    }
    [Serializable] public class RestaurantReview { public string Customer,Comment; public float Score; public string DishId; }
    [Serializable] public class WorkerState { public string Id; public StaffJob Job; public int TasksCompleted; public float Energy=100; }
    [Serializable] public partial class RestaurantState {
        public bool Owned,Open,PhysicalKitInstalled,CounterInstalled,TrashInstalled;
        public int Produce,Protein; // retired: migrated into Pantry on load
        public int Served,Lost,Earnings,Rank=1,NextInstanceId=1,NextOrderId=1;[NonSerialized]public List<RepGain> PendingRep=new List<RepGain>();
        public void Rep(string source,int amount){if(amount==0)return;(PendingRep??(PendingRep=new List<RepGain>())).Add(new RepGain{Source=source,Amount=amount});}
        public float Cleanliness=42,Satisfaction=50,ServiceSeconds;
        public List<PlacedItem> Layout=new List<PlacedItem>();
        public List<string> ActiveMenu=new List<string>{"burger","salad"};
        public List<RestaurantOrder> Orders=new List<RestaurantOrder>();
        public List<RestaurantReview> Reviews=new List<RestaurantReview>();
        public List<WorkerState> Workers=new List<WorkerState>();
        public int[] ServedByType=new int[10];
        public int Stars => Rank;
        public bool CanCustomize => Owned && !Open && Orders.Count==0;
        public int Seats => Layout.Sum(p=>RestaurantCatalog.Find(p.CatalogId)?.Seats??0);
        public int ShiftsRun; public int ShiftLevel=>ShiftDifficulty.Level(this);
        public float PatienceOf(RestaurantOrder o)=>o.Patience>0?o.Patience:RestaurantCatalog.Customers[o.CustomerType].Patience;
        [NonSerialized]public int PlayerRank;public string SiteId="oddtable";public int SiteAmbience=>SiteId=="bayside"?4:0;
        public int Ambience => Math.Min(40,SiteAmbience+FinishAmbience+Layout.Sum(p=>RestaurantCatalog.Find(p.CatalogId)?.IsFinish==true?0:RestaurantCatalog.Find(p.CatalogId)?.Ambience??0));
        public int CookSlots => Math.Max(1,Layout.Count(p=>p.CatalogId=="grill"||p.CatalogId=="stove"||p.CatalogId=="oven"));
        public int StockLimit => HasEquipment("fridge")?48:24;
        // One shared pantry (stand and restaurant) with a count per ingredient; the limit applies to each ingredient.
        public List<StockLine> Pantry=new List<StockLine>();
        public int Stock(string id){var l=Pantry?.Find(x=>x.Id==id);return l==null?0:l.Count;}
        public int Room(string id)=>Math.Max(0,StockLimit-Stock(id));
        public int AddStock(string id,int n){Pantry=Pantry??new List<StockLine>();if(n<=0)return 0;var l=Pantry.Find(x=>x.Id==id);if(l==null)Pantry.Add(l=new StockLine{Id=id});int add=Math.Min(n,StockLimit-l.Count);if(add<=0)return 0;l.Count+=add;return add;}
        public bool UseStock(string id,int n=1){var l=Pantry?.Find(x=>x.Id==id);if(l==null||l.Count<n)return false;l.Count-=n;return true;}
        public bool HasFor(string dish){foreach(var g in Ingredients.For(dish).GroupBy(x=>x))if(Stock(g.Key)<g.Count())return false;return true;}
        public bool UseFor(string dish){if(!HasFor(dish))return false;foreach(var i in Ingredients.For(dish))UseStock(i);return true;}
        public int WagesPerOrder => Workers.Count(w=>w.Job!=StaffJob.Off);
        public string WallId => Layout.FindLast(p=>p.CatalogId.StartsWith("wall_"))?.CatalogId??"wall_shabby";
        public string FloorId => Layout.FindLast(p=>p.CatalogId.StartsWith("floor_"))?.CatalogId??"floor_shabby";
        public string StarProgress => Rank>=2?"TWO STARS • faster oven, jukebox & neon sign unlocked":$"Two stars: {Served}/20 served • {Satisfaction:0}/75 satisfaction • {Ambience}/12 ambience";
        public bool HasEquipment(string id) => Layout.Exists(p=>p.CatalogId==id);
        static bool Fail(string text,out string message) {message=text;return false;}
        public bool BuyRestaurant(GameState wallet,out string message) {
            if(Owned)return Fail("This restaurant already belongs to you.",out message);
            if(wallet.Cash<150)return Fail("The lease costs $150. Keep earning at your stand.",out message);
            // The lease is an empty, shabby room. Keep working the stand and buy the kitchen piece by piece.
            wallet.Cash-=150;Owned=true;PhysicalKitInstalled=true;CounterInstalled=true;TrashInstalled=true;
            message="It's yours: four walls and a lot of dust. Keep running your stand, then press B inside to buy a pantry, grill, plate rack, assembly station, sink and tables.";return true;
        }
        void AddPlaced(string id,int x,int z,int rotation,int paid) {Layout.Add(new PlacedItem{InstanceId=NextInstanceId++,CatalogId=id,X=x,Z=z,Rotation=rotation%4,Paid=paid});}
        public void EnsurePhysicalKit() {
            if(Owned&&!CounterInstalled){
                // Every kitchen gets one free counter so players can set dishes down instead of discarding them.
                if(!HasEquipment("counter")){for(int z=0;z<10&&!CounterInstalled;z++)for(int x=0;x<12&&!CounterInstalled;x++)if(CanPlace("counter",x,z,0,-1,out _)){AddPlaced("counter",x,z,0,0);CounterInstalled=true;}}
                else CounterInstalled=true;
            }
            if(Owned&&!TrashInstalled){
                if(!HasEquipment("trash")){for(int z=0;z<10&&!TrashInstalled;z++)for(int x=0;x<12&&!TrashInstalled;x++)if(CanPlace("trash",x,z,0,-1,out _)){AddPlaced("trash",x,z,0,0);TrashInstalled=true;}}
                else TrashInstalled=true;
            }
            if(!Owned||PhysicalKitInstalled)return;
            foreach(string id in new[]{"pantry","plate_rack","assembly","sink"}) {
                if(HasEquipment(id))continue;
                bool added=false;
                for(int z=0;z<10&&!added;z++)for(int x=7;x<12&&!added;x++)if(CanPlace(id,x,z,0,-1,out _)){AddPlaced(id,x,z,0,0);added=true;}
                for(int z=0;z<10&&!added;z++)for(int x=0;x<5&&!added;x++)if(CanPlace(id,x,z,0,-1,out _)){AddPlaced(id,x,z,0,0);added=true;}
            }
            PhysicalKitInstalled=new[]{"pantry","plate_rack","assembly","sink"}.All(HasEquipment);
        }
        public bool CanPlace(string id,int x,int z,int rotation,int ignoreInstance,out string message) {
            var item=RestaurantCatalog.Find(id);
            if(item==null)return Fail("Unknown catalog item.",out message);
            if(rotation<0||rotation>3)return Fail("Rotation must be 0, 1, 2, or 3.",out message);
            if(item.RequiredStars>Stars)return Fail("Reach two stars to unlock this item.",out message);
            if(item.Tier>PlayerRank)return Fail("Unlocks at "+Reputation.Titles[item.Tier]+": better gear arrives with each district.",out message);
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
            int price=item.IsFinish?FinishCatalog.WholeRoomPrice(id):item.Price;
            if(wallet.Cash<price)return Fail($"You need ${price} for {item.Name}.",out message);
            if((item.IsFinish||item.IsExterior)&&HasEquipment(id))return Fail("This improvement is already installed.",out message);
            wallet.Cash-=price;
            if(item.IsFinish){string prefix=id.StartsWith("wall_")?"wall_":"floor_";Layout.RemoveAll(p=>p.CatalogId.StartsWith(prefix));SurfaceFinishes?.RemoveAll(p=>p!=null&&p.Key!=null&&p.Key.StartsWith(prefix=="wall_"?"wall:":"floor:"));}
            AddPlaced(id,x,z,rotation,price);UpdateRank();message=$"Installed {item.Name}. {item.Description}";return true;
        }
        public bool Move(int instanceId,int x,int z,int rotation,out string message) {
            if(!CanCustomize)return Fail("Close service and wait for customers to leave before moving furnishings.",out message);
            var p=Layout.Find(i=>i.InstanceId==instanceId);if(p==null)return Fail("That furnishing no longer exists.",out message);
            if(RestaurantCatalog.Find(p.CatalogId).IsFinish||RestaurantCatalog.Find(p.CatalogId).IsExterior)return Fail("This improvement applies to the whole building.",out message);
            if(!CanPlace(p.CatalogId,x,z,rotation,instanceId,out message))return false;
            p.X=x;p.Z=z;p.Rotation=rotation;message="Furnishing moved.";return true;
        }
        public int LevelOf(int instanceId){var p=Layout.Find(x=>x.InstanceId==instanceId);return p==null?1:Math.Max(1,Math.Min(StationUpgrades.MaxLevel,p.Level));}
        public bool Upgrade(GameState wallet,int instanceId,out string message){
            var p=Layout.Find(x=>x.InstanceId==instanceId);var c=p==null?null:RestaurantCatalog.Find(p.CatalogId);
            if(c==null||!StationUpgrades.CanUpgrade(c.Id))return Fail("This can't be upgraded.",out message);
            int next=Math.Max(1,p.Level)+1;if(next>StationUpgrades.MaxLevel)return Fail(c.Name+" is already fully upgraded.",out message);
            if(Stars<StationUpgrades.StarsNeeded(next))return Fail("Level "+next+" needs "+StationUpgrades.StarsNeeded(next)+" stars.",out message);
            int cost=StationUpgrades.Cost(c,next);if(wallet.Cash<cost)return Fail("Upgrading costs $"+cost+".",out message);
            wallet.Cash-=cost;p.Level=next;p.Paid+=cost;wallet.Emit("upgrade:"+instanceId+":"+next);
            message=c.Name+" upgraded to "+StationUpgrades.LevelName(next)+"! "+StationUpgrades.Effect(c.Id,next)+".";return true;
        }
        public bool Sell(GameState wallet,int instanceId,out string message) {
            if(!CanCustomize)return Fail("Close service and let customers leave before selling furnishings.",out message);
            var p=Layout.Find(i=>i.InstanceId==instanceId);if(p==null)return Fail("That furnishing no longer exists.",out message);
            if(new[]{"prep_bench","grill","pantry","plate_rack","assembly","sink"}.Contains(p.CatalogId)&&Layout.Count(i=>i.CatalogId==p.CatalogId)<=1)return Fail("Install a replacement before selling your last essential kitchen station.",out message);
            if(RestaurantCatalog.Find(p.CatalogId).Seats>0&&Seats<=RestaurantCatalog.Find(p.CatalogId).Seats)return Fail("Install replacement seating before selling your last table.",out message);
            int refund=p.Paid/2;Layout.Remove(p);wallet.Cash+=refund;message=$"Sold for ${refund}. Starter furnishings have no resale value.";return true;
        }
        // Only dishes with hands-on steps (RecipeBook) and a known recipe can go on the menu.
        public bool IsDishAvailable(GameState wallet,string id) {var d=RestaurantCatalog.Dish(id);return d!=null&&Stars>=d.RequiredStars&&wallet.Knows(id)&&HasEquipment(d.Equipment)&&RecipeBook.Recipes.Any(r=>r.DishId==id);}
        public bool ToggleDish(GameState wallet,string id,out string message) {
            if(!Owned)return Fail("Buy the restaurant first.",out message);
            if(ActiveMenu.Contains(id)){if(ActiveMenu.Count<=1)return Fail("Keep at least one dish on the menu.",out message);ActiveMenu.Remove(id);message="Dish removed from tomorrow's orders.";return true;}
            if(!IsDishAvailable(wallet,id))return Fail("This dish needs its recipe, equipment, and star rank unlocked.",out message);
            ActiveMenu.Add(id);message="Dish added to the menu. New customers can order it.";return true;
        }
        // Can this ingredient be bought at Milo's right now? (Secret ones never are; recipe ones need the recipe.)
        public string IngredientLock(GameState wallet,IngredientDef d){
            if(d.Source!=Ingredients.Milo)return "Only from Zeeb (phone)";
            if(!string.IsNullOrEmpty(d.Recipe)&&!wallet.Knows(d.Recipe))return "Learn "+RestaurantCatalog.Dish(d.Recipe).Name+" first";
            if(Stars<d.RequiredStars)return "Needs a "+d.RequiredStars+"-star restaurant";
            return null;
        }
        // Anti-softlock: broke and can't cook? Once a day Milo fronts you three of each basic, enough to earn your way back.
        public static bool NeedsMiloHelp(GameState wallet,RestaurantState r)=>wallet.Cash<14&&(r.Stock("patty")<1||r.Stock("bun")<1)&&r.Stock("greens")<1;
        public bool RequestSupplyHelp(GameState wallet,out string message) {
            if(!Owned&&!wallet.StandBuilt)return Fail("Set up your food stand first ($10).",out message);
            if(!NeedsMiloHelp(wallet,this))return Fail("You can still cook or afford a pack. Sell what you have first.",out message);
            if(wallet.LastMiloHelpDay==wallet.Day)return Fail("\"I already helped you today, friend. Sell something and come back.\"",out message);
            foreach(var id in new[]{"patty","bun","greens"})AddStock(id,3-Math.Min(3,Stock(id)));wallet.LastMiloHelpDay=wallet.Day;
            message="\"On the house. Pay it forward.\" Milo fronts you 3 patties, 3 buns and 3 greens.";return true;
        }
        public bool StartService(GameState wallet,out string message) {
            if(!Owned||Open)return Fail("Service is unavailable or already open.",out message);
            var missing=new[]{"pantry","plate_rack","sink"}.Where(id=>!HasEquipment(id)).Select(id=>RestaurantCatalog.Find(id).Name).ToList();
            if(!HasEquipment("assembly")&&!HasEquipment("counter"))missing.Add("an assembly station or counter");
            if(!HasEquipment("grill")&&!HasEquipment("prep_bench"))missing.Add("a grill or prep bench");
            if(Seats<1)missing.Add("a table");
            if(missing.Count>0)return Fail("Not ready to open. Still need: "+string.Join(", ",missing)+". Press B inside to shop.",out message);
            if(!ActiveMenu.Any(id=>IsDishAvailable(wallet,id)))return Fail("Install equipment for at least one menu dish.",out message);
            Open=true;ServiceSeconds=0;message="OPEN! Customers can arrive. Close service at any time to stop new arrivals.";return true;
        }
        public bool EndService(out string message) {if(!Open)return Fail("Service is already closed.",out message);Open=false;message="CLOSED to new arrivals. Finish current orders; renovate once everyone leaves.";return true;}
        public RestaurantOrder AddCustomer(GameState wallet,int type,int seatInstanceId,out string message,string residentId="") {
            if(!Open||type<0||type>=RestaurantCatalog.Customers.Length){message="Restaurant is closed or customer is unavailable.";return null;}
            var seat=Layout.Find(p=>p.InstanceId==seatInstanceId);int capacity=seat==null?0:RestaurantCatalog.Find(seat.CatalogId).Seats;
            capacity-=wallet.Kitchen?.DirtyAtTable(seatInstanceId)??0;
            if(capacity<=Orders.Count(o=>o.SeatInstanceId==seatInstanceId&&o.Stage!=RestaurantOrderStage.Leaving)){message="All seats here are occupied.";return null;}
            var available=ActiveMenu.Where(id=>IsDishAvailable(wallet,id)).ToList();if(available.Count==0){message="No available menu dishes.";return null;}
            var customer=RestaurantCatalog.Customers[type];string dish=available.Contains(customer.FavoriteDish)?customer.FavoriteDish:available[(NextOrderId+type)%available.Count];
            var occupied=new HashSet<int>(Orders.Where(o=>o.SeatInstanceId==seatInstanceId&&o.Stage!=RestaurantOrderStage.Leaving).Select(o=>o.SeatNumber));
            if(wallet.Kitchen!=null)foreach(var plate in wallet.Kitchen.Items.Where(i=>i.TableInstanceId==seatInstanceId&&i.Holder.StartsWith("table:")))occupied.Add(plate.SeatNumber);
            int seatNumber=Enumerable.Range(1,RestaurantCatalog.Find(seat.CatalogId).Seats).FirstOrDefault(n=>!occupied.Contains(n));
            if(seatNumber==0){message="All seats here are occupied.";return null;}
            var who=ResidentCast.Get(residentId);var order=new RestaurantOrder{Id=NextOrderId++,CustomerType=type,DishId=dish,SeatInstanceId=seatInstanceId,SeatNumber=seatNumber,ResidentId=residentId??"",Patience=customer.Patience*ShiftDifficulty.PatienceScale(ShiftLevel)};Orders.Add(order);message=$"{(who!=null?who.Name:customer.Name)} ordered {RestaurantCatalog.Dish(dish).Name}.";return order;
        }
        public float CookTime(RestaurantOrder order) {var d=RestaurantCatalog.Dish(order.DishId);return d.CookSeconds*(d.Id=="salad"?Math.Max(.5f,1-.25f*(Layout.Count(p=>p.CatalogId=="prep_bench")-1)):1);}
        public bool BeginCooking(int id,out string message) {
            var o=Orders.Find(x=>x.Id==id);if(o==null||o.Stage!=RestaurantOrderStage.Waiting)return Fail("Choose a waiting order.",out message);
            var d=RestaurantCatalog.Dish(o.DishId);if(!HasEquipment(d.Equipment))return Fail("The dish's required equipment is missing.",out message);
            if(Orders.Count(x=>x.Stage==RestaurantOrderStage.Cooking)>=CookSlots)return Fail("All cooking slots are busy. Extra grills and stoves add capacity.",out message);
            if(!UseFor(d.Id))return Fail("Out of ingredients. Restock at Milo's.",out message);
            o.Stage=RestaurantOrderStage.Cooking;o.StageTime=0;o.CookProgress=0;message=$"Cooking {d.Name}.";return true;
        }
        public bool CompleteServing(GameState wallet,int id,out string message) {
            var o=Orders.Find(x=>x.Id==id);if(o==null||o.Stage!=RestaurantOrderStage.Ready)return Fail("Choose a ready dish to serve.",out message);
            var c=RestaurantCatalog.Customers[o.CustomerType];var d=RestaurantCatalog.Dish(o.DishId);
            float waitRatio=o.Wait/PatienceOf(o),score=45+o.Quality*30+(waitRatio<.35f?10:waitRatio>.7f?-15:0)+Math.Min(10,Ambience*.55f)*c.AmbienceWeight+(Cleanliness-60)*.15f*c.CleanlinessWeight+(c.FavoriteDish==o.DishId?5:0);
            score=Clamp(score,0,100);int tip=score>=85?4:score>=70?2:0,wage=WagesPerOrder,revenue=Math.Max(0,d.Price+tip-wage);
            wallet.Cash+=revenue;Earnings+=revenue;Served++;int rep=score>=70?Reputation.HappyCustomer:score>=45?Reputation.OkCustomer:0;if(score>=60&&o.CustomerType>=0&&o.CustomerType<ServedByType.Length){ServedByType[o.CustomerType]++;}bool met=wallet.MeetResident(o.ResidentId,Reputation.NewResident);Rep(rep>Reputation.HappyCustomer?"New kinds of guests":"Restaurant guests",rep);Cleanliness=Math.Max(0,Cleanliness-4);
            string food=o.Quality>.85f?"Food fresh":o.Quality>.6f?"Food cooled":"Food sat too long";
            string wait=waitRatio<.35f?"short wait":waitRatio>.7f?"long wait":"reasonable wait";
            string room=Cleanliness<45?"dirty tables":Cleanliness>75?"spotless room":"room could be cleaner";
            AddReview(o,score,$"{food}; {wait}; {room}; {(Ambience>=12?"lovely ambience":"plain surroundings")}{(c.FavoriteDish==o.DishId?"; my favorite dish!":".")}");
            wallet.Emit("served:"+o.Id+":"+(int)score+":"+tip);o.Stage=RestaurantOrderStage.Eating;o.StageTime=0;UpdateRank();var who=ResidentCast.Get(o.ResidentId);message=$"{(who!=null?who.Name:c.Name)}: {score:0}% satisfaction. +${d.Price+tip} ({(wage>0?$"${wage} staff wages":"no wages")}){(rep>0?$"  +{rep} rep":"")}."+(met?$"  NEW: {who.Name} joined your People book!":"");return true;
        }
        void AddReview(RestaurantOrder o,float score,string comment) {
            Reviews.Insert(0,new RestaurantReview{Customer=ResidentCast.Get(o.ResidentId)?.Name??RestaurantCatalog.Customers[o.CustomerType].Name,Score=score,Comment=comment,DishId=o.DishId});
            if(Reviews.Count>12)Reviews.RemoveAt(Reviews.Count-1);Satisfaction=Reviews.Average(r=>r.Score);
        }
        public void RecordQueueLoss(int customerType){Lost++;Rep("Guests who left",Reputation.LostCustomer);AddReview(new RestaurantOrder{CustomerType=customerType,DishId="burger"},20,"No clean table became available. Clear and wash dishes, or add seating.");}
        void UpdateRank(){if(Rank<2&&Served>=20&&Satisfaction>=75&&Ambience>=12){Rank=2;Rep("New stars",Reputation.NewStar);}}
        public bool Clean(out string message) {if(!Owned)return Fail("Buy the restaurant first.",out message);if(Cleanliness>=100)return Fail("The restaurant is already spotless.",out message);Cleanliness=Math.Min(100,Cleanliness+25);message=$"Tables wiped. Cleanliness {Cleanliness:0}%.";return true;}
        public bool Hire(GameState wallet,string id,out string message) {
            var d=RestaurantCatalog.Worker(id);if((!Owned&&!wallet.StandBuilt)||d==null)return Fail("Set up your food stand first.",out message);
            if(Workers.Exists(w=>w.Id==id))return Fail("This worker already works here.",out message);
            if(d.Special){
                var resident=ResidentCast.Get(id);
                if(resident!=null&&!wallet.HasMet(id))return Fail($"Feed {resident.Name} once and they'll join your People book. Then you can recruit them.",out message);
                if(wallet.Flux<d.FluxCost)return Fail($"Recruiting {d.Name} costs {d.FluxCost} Flux. Earn Flux from the rival's stash at night.",out message);
                wallet.Flux-=d.FluxCost;Workers.Add(new WorkerState{Id=id,Job=d.Role});wallet.GainReputation(Reputation.Recruit,"Recruits");message=$"{d.Name} joined your crew for {d.FluxCost} Flux! Specialty: {d.Role}. Assign any job in the Staff tab.";return true;
            }
            if(wallet.Cash<d.Cost)return Fail($"Hiring {d.Name} costs ${d.Cost}.",out message);
            wallet.Cash-=d.Cost;Workers.Add(new WorkerState{Id=id,Job=d.Role});wallet.GainReputation(Reputation.Recruit,"Recruits");message=$"Hired {d.Name}. Assigned {d.Role}. $1 per served order while assigned.";return true;
        }
        public bool Assign(string id,StaffJob job,out string message) {
            var w=Workers.Find(x=>x.Id==id);if(w==null)return Fail("Hire this worker first.",out message);
            if(!Enum.IsDefined(typeof(StaffJob),job)||job==StaffJob.Any)return Fail("Choose one job: cook, serve, wash or run the stand.",out message);
            // Only one worker can run the street stand at a time.
            if(job==StaffJob.Stand)foreach(var other in Workers)if(other!=w&&other.Job==StaffJob.Stand)other.Job=StaffJob.Off;
            w.Job=job;message=job==StaffJob.Stand?$"{RestaurantCatalog.Worker(id).Name} is running your food stand. They use your pantry and keep the money coming.":$"{RestaurantCatalog.Worker(id).Name}: {job}.";return true;
        }
        public float WorkerActionSeconds(StaffJob job) {
            var assigned=Workers.Where(w=>w.Job==job).ToList();if(assigned.Count==0)return 8;
            Func<StaffDefinition,bool> Specialty=d=>d.Role==job||(d.Role==StaffJob.Serve&&job==StaffJob.Clean);
            var defs=assigned.Select(w=>RestaurantCatalog.Worker(w.Id)).Where(d=>d!=null).ToList();
            // Everyone can do every job; specialists are much faster at their own.
            return defs.Any(Specialty)?1.5f:3.5f;
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
                if(o.Wait>PatienceOf(o)){Lost++;wallet.Emit("walkout:"+o.Id);Rep("Guests who left",Reputation.LostCustomer);AddReview(o,15,"Waited too long and left hungry. Start cooking sooner, add equipment, or hire help.");o.Stage=RestaurantOrderStage.Leaving;o.StageTime=0;}
            }
            Orders.RemoveAll(o=>o.Stage==RestaurantOrderStage.Leaving&&o.StageTime>=6);UpdateRank();
        }
        static float Clamp(float value,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?min:Math.Max(min,Math.Min(max,value));
        public void SanitizeAfterLoad() {
            Layout=Layout??new List<PlacedItem>();ActiveMenu=ActiveMenu??new List<string>{"burger","salad"};Orders=Orders??new List<RestaurantOrder>();Reviews=Reviews??new List<RestaurantReview>();Workers=Workers??new List<WorkerState>();if(ServedByType==null||ServedByType.Length<10){var grown=new int[10];if(ServedByType!=null)Array.Copy(ServedByType,grown,ServedByType.Length);ServedByType=grown;}
            Rank=Math.Max(1,Math.Min(2,Rank));
            SiteId="oddtable"; // v7: the same layout moves to The Odd Table (it is room-relative); The Bayside is bought later as a second restaurant
            Layout.RemoveAll(p=>p==null||RestaurantCatalog.Find(p.CatalogId)==null);Workers.RemoveAll(w=>w==null||RestaurantCatalog.Worker(w.Id)==null);
            var incoming=Layout;Layout=new List<PlacedItem>();var seenIds=new HashSet<int>();
            NextInstanceId=Math.Max(1,NextInstanceId);
            foreach(var p in incoming){var savedDefinition=RestaurantCatalog.Find(p.CatalogId);p.Paid=Math.Max(0,Math.Min(p.Paid,savedDefinition.IsFinish?FinishCatalog.WholeRoomPrice(p.CatalogId):savedDefinition.Price));p.Rotation=(p.Rotation%4+4)%4;
                // Finish ownership survives load independently of the nonserialized city rank.
                if(!savedDefinition.IsFinish&&!CanPlace(p.CatalogId,p.X,p.Z,p.Rotation,-1,out _))continue;
                var def=RestaurantCatalog.Find(p.CatalogId);
                if(def.IsFinish){string prefix=p.CatalogId.StartsWith("wall_")?"wall_":"floor_";Layout.RemoveAll(existing=>existing.CatalogId.StartsWith(prefix));}
                if(def.IsExterior&&HasEquipment(p.CatalogId))continue;
                if(p.InstanceId<1||seenIds.Contains(p.InstanceId)){while(seenIds.Contains(NextInstanceId))NextInstanceId++;p.InstanceId=NextInstanceId++;}
                seenIds.Add(p.InstanceId);Layout.Add(p);
            }
            NextInstanceId=Math.Max(NextInstanceId,Layout.Count==0?1:Layout.Max(p=>p.InstanceId)+1);
            ActiveMenu=ActiveMenu.Where(id=>id=="burger"||id=="salad"||id=="midnight").Distinct().ToList();if(ActiveMenu.Count==0)ActiveMenu.Add("burger");
            // Unfinished service ends on load; durable restaurant layout, finances, menu, stock, reviews and workers survive.
            Orders.Clear();Open=false;
            // v7: the old protein/produce totals become real ingredients.
            Pantry=Pantry??new List<StockLine>();if(Protein>0||Produce>0){AddStock("patty",Protein);AddStock("bun",Produce);AddStock("greens",Produce/2);Protein=0;Produce=0;}
            Pantry.RemoveAll(l=>l==null||Ingredients.Get(l.Id)==null);foreach(var l in Pantry)l.Count=Math.Max(0,Math.Min(StockLimit,l.Count));
            if(SiteId!="oddtable"&&SiteId!="bayside")SiteId="oddtable";Cleanliness=Clamp(Cleanliness,0,100);Satisfaction=Clamp(Satisfaction,0,100);Rank=Math.Max(1,Math.Min(2,Rank));Served=Math.Max(0,Served);Lost=Math.Max(0,Lost);UpdateRank();
            foreach(var worker in Workers){worker.Energy=Clamp(worker.Energy,0,100);if(worker.Job==StaffJob.Any)worker.Job=RestaurantCatalog.Worker(worker.Id)?.Role??StaffJob.Cook;}
            SanitizeFinishes();
            EnsurePhysicalKit();
        }
    }
}
