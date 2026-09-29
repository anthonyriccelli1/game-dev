using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantCity {
    public sealed class FinishDefinition {
        public string Id,Name,Pattern,PrimaryHex,SecondaryHex,AccentHex;
        // Price buys one patch. Ambience is the contribution at complete family coverage.
        public int Price,Ambience,RequiredStars,Tier;
        public bool IsWall => Id.StartsWith("wall_",StringComparison.Ordinal);
        public FinishDefinition(string id,string name,string pattern,string primary,string secondary,string accent,int price,int ambience,int stars=1,int tier=0) {
            Id=id;Name=name;Pattern=pattern;PrimaryHex=primary;SecondaryHex=secondary;AccentHex=accent;
            Price=price;Ambience=ambience;RequiredStars=stars;Tier=tier;
        }
    }
    public static class FinishCatalog {
        public const int FloorWidth=12,FloorDepth=10,WallSections=12;
        public static readonly FinishDefinition[] All = {
            new FinishDefinition("wall_cream","Cream plaster","plaster","E7D8B4","CBBE9F","F4E8CC",1,2),
            new FinishDefinition("wall_teal","Lagoon paint","paint","388F91","277377","7DBAB1",2,3),
            new FinishDefinition("wall_rose","Rose paint","paint","CA858C","AF656E","E5ABB0",2,3),
            new FinishDefinition("wall_sage","Sage plaster","plaster","A0B18A","7F956C","C9D3B7",1,2),
            new FinishDefinition("wall_sky","Sky paint","paint","A2C8D9","7CA8BF","D5E6EA",1,2),
            new FinishDefinition("wall_stripe","Seaside stripes","stripe","EFE1BF","6EA9AE","C5D4BE",2,3),
            new FinishDefinition("wall_flower","Meadow wallpaper","floral","F0DFBD","809C72","D18A7C",3,4),
            new FinishDefinition("wall_geo","Orbit wallpaper","geometric","253F5F","DCB96A","C56A73",4,5,2,1),
            new FinishDefinition("wall_brick","Market brick","brick","AC6852","814934","D9C1A1",3,4),
            new FinishDefinition("wall_whitebrick","Loft white brick","brick","E4E1DA","BDB8AE","F3F1EC",2,3),
            new FinishDefinition("wall_panel","Walnut paneling","panel","71513E","4B352B","AB8158",4,5,2,1),
            new FinishDefinition("wall_scallop","Moon scallops","scallop","214B54","61A6A0","DDBB72",5,6,2,2),
            new FinishDefinition("wall_midnight","Midnight tile","ceramic","1E3349","314B63","BC965E",6,6,2,3),
            new FinishDefinition("floor_checker","Diner checkerboard","checker","E9D8B1","364F56","B6AB8B",2,3),
            new FinishDefinition("floor_wood","Honey wood floor","wood","B68957","8F603D","D2AC73",2,4),
            new FinishDefinition("floor_ceramic","Milk ceramic","ceramic","E3DBC5","C9BEA7","F2ECD9",1,2),
            new FinishDefinition("floor_clay","Terracotta tile","ceramic","C5825B","A46747","E2B190",2,3),
            new FinishDefinition("floor_stone","Market flagstone","ceramic","C9C1B0","A69E8E","DDD6C6",2,3),
            new FinishDefinition("floor_terrazzo","Confetti terrazzo","terrazzo","D9CAB1","5A8D86","BD7767",3,4),
            new FinishDefinition("floor_blue","Harbor ceramic","ceramic","6E9BAE","4F7D94","D8D0B8",2,3),
            new FinishDefinition("floor_parquet","Walnut parquet","parquet","916B4F","5D4234","BF9363",4,5,2,1),
            new FinishDefinition("floor_mosaic","Sunburst mosaic","mosaic","DAAF64","497779","EEE0B9",4,5,2,1),
            new FinishDefinition("floor_marble","Moon marble","marble","ECE8DD","A4B1B4","C9BA9B",6,6,2,3),
            new FinishDefinition("floor_slate","Night slate","slate","3D505B","273B47","71828A",5,6,2,2)
        };
        public static FinishDefinition Find(string id) => Array.Find(All,d=>d.Id==id);
        public static bool TryPieceKey(string key,out int instanceId) {
            instanceId=0;
            if(string.IsNullOrEmpty(key)||!key.StartsWith("piece:",StringComparison.Ordinal))return false;
            return int.TryParse(key.Substring(6),out instanceId)&&instanceId>0&&key=="piece:"+instanceId;
        }
        public static bool ValidSurfaceKey(string key,bool isWall) {
            if(string.IsNullOrEmpty(key))return false;
            var parts=key.Split(':');if(parts.Length!=3)return false;
            if(isWall) {
                if(parts[0]!="wall"||(parts[1]!="back"&&parts[1]!="left"&&parts[1]!="right"))return false;
                return int.TryParse(parts[2],out int section)&&section>=0&&section<WallSections&&parts[2]==section.ToString();
            }
            return parts[0]=="floor"&&int.TryParse(parts[1],out int x)&&int.TryParse(parts[2],out int z)
                &&x>=0&&x<FloorWidth&&z>=0&&z<FloorDepth&&parts[1]==x.ToString()&&parts[2]==z.ToString();
        }
        public static IEnumerable<string> SurfaceKeys(bool isWall) {
            if(isWall){foreach(string side in new[]{"back","left","right"})for(int n=0;n<WallSections;n++)yield return "wall:"+side+":"+n;}
            else for(int z=0;z<FloorDepth;z++)for(int x=0;x<FloorWidth;x++)yield return "floor:"+x+":"+z;
        }
        public static int WholeRoomPrice(string id) {
            switch(id){case "wall_cream":return 18;case "wall_teal":case "wall_rose":return 24;case "floor_checker":return 25;case "floor_wood":return 30;}
            var d=Find(id);return d==null?-1:d.Price*(d.IsWall?36:120);
        }
        public static CatalogItem[] ShopItems() => All.Select(d=>new CatalogItem(d.Id,d.Name,CatalogCategory.Finishes,d.Price,1,1,0,d.Ambience,
            "Brush a "+(d.IsWall?"wall section":"floor tile")+" for $"+d.Price+". Whole-room fill prices only changed patches.",d.RequiredStars,d.Tier)).ToArray();
    }
    [Serializable] public class SurfaceFinish { public string Key,CatalogId; }
    public partial class RestaurantState {
        public List<SurfaceFinish> SurfaceFinishes=new List<SurfaceFinish>();
        public bool ValidFinishTarget(string key,bool wall) {
            if(FinishCatalog.ValidSurfaceKey(key,wall))return true;
            if(!wall||!FinishCatalog.TryPieceKey(key,out int id))return false;
            return Layout!=null&&Layout.Any(p=>p!=null&&p.InstanceId==id&&(p.CatalogId=="partition_wall"||p.CatalogId=="service_window"));
        }
        public IEnumerable<string> FinishTargets(bool wall) {
            foreach(var key in FinishCatalog.SurfaceKeys(wall))yield return key;
            if(wall&&Layout!=null)foreach(var p in Layout)
                if(p!=null&&(p.CatalogId=="partition_wall"||p.CatalogId=="service_window"))yield return "piece:"+p.InstanceId;
        }
        public string FinishAt(string key) {
            bool wall=ValidFinishTarget(key,true);
            if(!wall&&!ValidFinishTarget(key,false))return "";
            var patch=SurfaceFinishes?.FindLast(p=>p!=null&&p.Key==key&&FinishCatalog.Find(p.CatalogId)?.IsWall==wall);
            return patch?.CatalogId??(FinishCatalog.TryPieceKey(key,out _)?"":wall?WallId:FloorId);
        }
        public int FinishPrice(string id,string key,bool fill) {
            var d=FinishCatalog.Find(id);if(d==null||!ValidFinishTarget(key,d.IsWall))return -1;
            return d.Price*(fill?FinishTargets(d.IsWall).Count(k=>FinishAt(k)!=id):(FinishAt(key)==id?0:1));
        }
        public bool ApplyFinish(GameState wallet,string id,string key,bool fill,out string reason) {
            if(!CanCustomize)return Fail("Close service and wait for customers to leave before renovating.",out reason);
            var d=FinishCatalog.Find(id);int price=FinishPrice(id,key,fill);
            if(price<0)return Fail("Choose a valid "+(d!=null&&d.IsWall?"wall section":"floor tile")+" for this finish.",out reason);
            if(wallet==null)return Fail("Your wallet is unavailable.",out reason);
            if(price==0){reason="This finish is already applied. No charge.";return true;}
            if(ShopStars<d.RequiredStars)return Fail("Requires "+StarText.Words(d.RequiredStars)+".",out reason);
            if(wallet.RankEarned<d.Tier)return Fail("Unlocks at "+Reputation.Titles[d.Tier]+" reputation.",out reason);
            if(wallet.Cash<price)return Fail("You need $"+price+" for "+d.Name+".",out reason);
            var keys=(fill?FinishTargets(d.IsWall):new[]{key}).Where(k=>FinishAt(k)!=id).ToArray();
            SurfaceFinishes=SurfaceFinishes??new List<SurfaceFinish>();
            foreach(string target in keys){SurfaceFinishes.RemoveAll(p=>p!=null&&p.Key==target);SurfaceFinishes.Add(new SurfaceFinish{Key=target,CatalogId=id});}
            wallet.Cash-=price;UpdateRank();reason="Applied "+d.Name+" for $"+price+".";return true;
        }
        public void SanitizeFinishes() {
            var valid=new Dictionary<string,SurfaceFinish>();
            foreach(var p in SurfaceFinishes??new List<SurfaceFinish>()) {
                var d=p==null?null:FinishCatalog.Find(p.CatalogId);
                if(d!=null&&ValidFinishTarget(p.Key,d.IsWall))valid[p.Key]=p;
            }
            SurfaceFinishes=valid.Values.OrderBy(p=>p.Key,StringComparer.Ordinal).ToList();
        }
        int FinishAmbience {
            get {
                double total=0;
                foreach(bool wall in new[]{true,false}) {
                    var keys=FinishCatalog.SurfaceKeys(wall).ToArray();
                    total+=keys.Sum(k=>FinishCatalog.Find(FinishAt(k))?.Ambience??0)/(double)keys.Length;
                }
                return (int)Math.Floor(total+0.000001);
            }
        }
    }
}
