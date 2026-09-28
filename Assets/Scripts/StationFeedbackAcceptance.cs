using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RestaurantCity {
    public partial class InteractionAcceptance {
        void RunStationFeedbackChecks(int grill, int sink) {
            var assembly = typeof(RestaurantController).Assembly;
            var grillType = assembly.GetType("RestaurantCity.GrillFeedback");
            var washType = assembly.GetType("RestaurantCity.WashFeedback");
            var binType = assembly.GetType("RestaurantCity.BinFeedback");
            var waste = typeof(KitchenStation).GetField("WasteCount");
            if (grillType == null || washType == null || binType == null || waste == null) return;
            object Call(string method, params object[] args) => typeof(RestaurantController).GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(R, args);
            T Read<T>(Component v, string property) => (T)v.GetType().GetProperty(property).GetValue(v);
            void Present(float dt = .1f) { Call("TickGrillFeedback", dt); Call("TickWashBinFeedback", dt); }
            void Face(int id, Vector3 local) {
                var station = R.StationObject(id);
                var point = station.transform.TransformPoint(local);
                P.Teleport(new Vector3(point.x, .15f, point.z) + station.transform.forward * 1.6f);
                P.LookAt(point); Physics.SyncTransforms();
            }
            int platesBefore = K.CleanPlates;
            bool builtBefore = Game.State.StandBuilt;
            Game.State.StandBuilt = true; Game.SyncWorld();
            var patty = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.RawProtein, Holder = "station:" + grill };
            K.Items.Add(patty); Present();
            var grillView = R.StationObject(grill).GetComponent(grillType);
            Require(grillView, "grill binds its own visual feedback");
            Call("DrawKitchenItems"); Present();
            var genericFood = (System.Collections.Generic.Dictionary<int, GameObject>)typeof(RestaurantController).GetField("physicalItems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(R);
            Check(genericFood.ContainsKey(patty.Id) && !genericFood[patty.Id].activeSelf, "grill owns food presentation without a generic raw ingredient duplicate");
            Check(grillView.GetComponentsInChildren<Collider>().Length == R.StationObject(grill).GetComponents<Collider>().Length,
                "grill effect introduces no extra interaction collider");
            Face(grill, new Vector3(0, 1.03f, 0));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = P.InteractionRay.origin + P.InteractionRay.direction * .7f;
            wall.transform.localScale = Vector3.one * .5f; Physics.SyncTransforms();
            Check(!(bool)Call("TryFlipStation", P), "flip cannot pass through an obstruction");
            wall.SetActive(false); Destroy(wall); Physics.SyncTransforms();
            Require((bool)Call("TryFlipStation", P), "player aims at grill and requests a visual flip");
            int flips = Read<int>(grillView, "FlipCount");
            Present(.2f);
            Check(Read<bool>(grillView, "Active") && flips == 1, "spatula visibly lifts the patty during a flip");
            Check(!(bool)Call("TryFlipStation", P), "repeated input cannot restart an active flip");
            Check(K.At(grill) == patty && patty.Quality == 1 && K.Stations.First(s => s.InstanceId == grill).Progress == 0,
                "flipping preserves food identity quality and cooking time");
            System.IO.Directory.CreateDirectory("InteractionEvidence"); CapturePlacement("grill-flip.png");
            Present(1);
            K.Tick(Game.State, 4); Present();
            Check(Read<float>(grillView, "HeatRatio") > .4f, "grill browning follows real cooking progress");
            K.Tick(Game.State, 4.1f); Present();
            Check(patty.Kind == KitchenItemKind.CookedPatty, "visual flips preserve normal ready time");
            CapturePlacement("grill-ready.png");
            K.Tick(Game.State, 16); Present();
            Check(patty.Kind == KitchenItemKind.BurntPatty, "normal burn state remains intact");
            K.Items.Remove(patty); Present();
            Check(!Read<bool>(grillView, "OwnsFood") && !(bool)Call("TryFlipStation", P), "empty grill offers no phantom patty or flip");

            var dirty = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.DirtyPlate, Holder = "station:" + sink };
            K.Items.Add(dirty); Present();
            var wash = R.StationObject(sink).GetComponent(washType);
            Require(wash, "sink binds its own washing feedback");
            Face(sink, new Vector3(-.3f, 1.05f, 0));
            Check(K.Work(Game.State, P.ActorId, sink, 1.2f, out _), "accepted player work washes a plate"); Present(.3f);
            Check(Read<bool>(wash, "Active") && Read<float>(wash, "CleanRatio") > .3f, "sponge scrubs while real dirt fades");
            CapturePlacement("sink-scrubbing.png");
            float partial = Read<float>(wash, "CleanRatio");
            Present(.3f);
            Check(!Read<bool>(wash, "Active") && Read<float>(wash, "CleanRatio") == partial, "lookaway stops scrub and retains partial cleanliness");
            K.ReleaseWork(P.ActorId); K.Work(Game.State, P.ActorId, sink, .1f, out _); K.ReleaseWork(P.ActorId); Present();
            Check(!Read<bool>(wash, "Active"), "release cancels pending washing motion");
            int otherSink = K.Stations.First(s => s.CatalogId == "sink" && KitchenState.IsStandStation(s.InstanceId)).InstanceId;
            var otherPlate = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.DirtyPlate, StandPlate = true, Holder = "station:" + otherSink };
            K.Items.Add(otherPlate); Present();
            K.Work(Game.State, "staff:test", sink, .2f, out _); K.Work(Game.State, "player:test", otherSink, .4f, out _); Present(.3f);
            Check(Read<bool>(wash, "Active") && Read<bool>(R.StationObject(otherSink).GetComponent(washType), "Active"), "staff and players wash at independent sinks");
            var standWash = R.StationObject(otherSink).GetComponent(washType);
            Check(standWash.GetComponentsInChildren<Collider>().Length == R.StationObject(otherSink).GetComponents<Collider>().Length, "washing effect adds no interaction collider");
            float progress = K.Stations.First(s => s.InstanceId == sink).Progress;
            Check(!K.Work(Game.State, P.ActorId, sink, 1, out _) && K.Stations.First(s => s.InstanceId == sink).Progress == progress, "second actor cannot double washing progress");
            Game.SetPaused(true); Present(); Check(!Read<bool>(wash, "Active"), "pause suppresses washing motion"); Game.SetPaused(false);
            K.Work(Game.State, "staff:test", sink, 4, out _); Present();
            Check(K.At(sink) == null && K.CleanPlates == platesBefore + 1 && !Read<bool>(wash, "OwnsFood"), "completed wash returns exactly one clean plate");
            K.Items.Remove(otherPlate); K.ReleaseWork("player:test"); K.CleanPlates = platesBefore;

            // Bins store only a bounded visual fill level; failed disposal keeps both the item and count.
            int bin = K.Stations.First(s => s.CatalogId == "trash" && KitchenState.IsStandStation(s.InstanceId)).InstanceId;
            var binStation = K.Stations.First(s => s.InstanceId == bin);
            waste.SetValue(binStation, 0); Present();
            var binView = R.StationObject(bin).GetComponent(binType); Require(binView, "bin binds its own discard feedback");
            var scrap = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.BurntPatty, Holder = P.ActorId };
            K.Items.Add(scrap); Check(K.Act(Game.State, P.ActorId, bin, null, out _), "food discards at the aimed bin"); Present();
            Check(Read<int>(binView, "FillLevel") == 1 && !K.Items.Contains(scrap), "successful discard grows visible bin contents");
            Face(bin, new Vector3(0, .65f, 0)); CapturePlacement("bin-filling.png");
            var bag = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.GroceryBag, Holder = P.ActorId };
            K.Items.Add(bag); K.Act(Game.State, P.ActorId, bin, null, out _); Present();
            Check((int)waste.GetValue(binStation) == 1 && K.Items.Contains(bag), "failed grocery disposal never fills the bin"); K.Items.Remove(bag);
            for (int i = 0; i < 8; i++) {
                K.Items.Add(new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.Bun, Holder = P.ActorId }); K.Act(Game.State, P.ActorId, bin, null, out _);
            }
            Present(); Check(Read<int>(binView, "FillLevel") == 6, "bin fill is bounded without blocking disposal");
            CapturePlacement("bin-full.png");
            var restored = JsonUtility.FromJson<KitchenStation>(JsonUtility.ToJson(binStation));
            Check((int)waste.GetValue(restored) == 6 && (int)waste.GetValue(JsonUtility.FromJson<KitchenStation>("{\"InstanceId\":1,\"CatalogId\":\"trash\"}")) == 0,
                "bin fill saves and older saves default empty");
            Check(K.Act(Game.State, P.ActorId, bin, null, out _), "empty hands can empty a filled bin"); Present();
            Check(Read<int>(binView, "FillLevel") == 0, "emptying clears visible bin contents");
            var oldWash = wash; R.RebuildLayout(); Present();
            Check(R.StationObject(sink).GetComponent(washType) != oldWash, "rebuilt furniture rebinds washing feedback");
            Game.State.StandBuilt = builtBefore; Game.SyncWorld();
        }
    }
}
