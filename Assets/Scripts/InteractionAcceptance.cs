using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RestaurantCity {
    // Run only with --interaction-acceptance. CityGame starts with an isolated state and disables saves.
    public partial class InteractionAcceptance : MonoBehaviour {
        public CityGame Game;
        int checks, failures;
        RestaurantController R => Game.Restaurant;
        FirstPersonPlayer P => Game.Player;
        KitchenState K => Game.State.Kitchen;

        IEnumerator Start() {
            Application.logMessageReceived += OnLog;
            yield return null;
                Game.State.Cash = RestaurantSites.StarterPrice + 150;
                Require(R.BuyRestaurant(P), "isolated restaurant acquired through the real lease interaction");
                Check(GameObject.Find("Future restaurant sign") == null, "owned restaurant removes the lease board interaction");
                Check(GameObject.Find("Pole sign / THE ODD TABLE") == null, "duplicate oversized street sign is retired");
                System.IO.Directory.CreateDirectory("InteractionEvidence");
                var so = R.StandOrigin; P.Teleport(so + new Vector3(0, .15f, 3.5f)); P.LookAt(so + new Vector3(0, 1.9f, 8));
                CapturePlacement("street-stand.png");
                P.Teleport(new Vector3(-10, .15f, 2.5f)); P.LookAt(new Vector3(-10, 2, -9));
                CapturePlacement("architecture-front.png");
                P.Teleport(new Vector3(-10, .15f, -10.7f)); P.LookAt(new Vector3(-10, 1.5f, -18));
                CapturePlacement("architecture-starter.png");
                // Buying now gives an empty room: install the kitchen the way a player would.
                Game.State.Cash = 2000;
                foreach (var (id, x, z) in new[] { ("pantry", 0, 0), ("plate_rack", 2, 0), ("prep_bench", 4, 0), ("assembly", 8, 0), ("grill", 0, 4), ("sink", 9, 4), ("cafe_table", 8, 7) })
                    if (!R.Data.Place(Game.State, id, x, z, 0, out _)) InstallFirstFree(id);
                foreach (var i in new[] { "patty", "bun", "greens" }) R.Data.AddStock(i, 20); // a fresh lease has an empty pantry
                Game.State.Kitchen.EnsureStations(R.Data);
                R.RebuildLayout();
                Physics.SyncTransforms(); // fresh furnishings report stale (origin) collider bounds until physics syncs
                Game.SetPaused(false);
                R.ClosePanel();
                yield return null;
                Physics.SyncTransforms();
                var aim = Game.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "Aim");
                Check(aim != null && ((RectTransform)aim.transform).rect.height >= aim.preferredHeight + 2,
                    "center reticle has enough height for generated glyph");
                var prep = Station("prep_bench");
                Check(typeof(RestaurantController).Assembly.GetType("RestaurantCity.ChoppingFeedback") != null,
                    "prep stations provide accepted-work chopping presentation");
                foreach (var feedbackType in new[] { "GrillFeedback", "WashFeedback", "BinFeedback" })
                    Check(typeof(RestaurantController).Assembly.GetType("RestaurantCity." + feedbackType) != null, feedbackType + " presentation exists");
                Check(typeof(KitchenStation).GetField("WasteCount") != null, "bin contents have backward-compatible station storage");
                var rack = Station("plate_rack");
                var pantry = Station("pantry");
                var assembly = Station("assembly");
                var grill = Station("grill");
                RunStationFeedbackChecks(grill, Station("sink"));
                RunChoppingChecks(prep);
                RunScrubChecks(Station("sink"));
                RunPattyChecks(grill);
                RunPantryAimChecks(pantry);
                RunChopChecks(prep);
                yield return null; // Rebuild cleanup is deferred until the end of the frame.
                Physics.SyncTransforms();
                var table = R.Data.Layout.First(x => RestaurantCatalog.Find(x.CatalogId).Seats > 0).InstanceId;

                // A level eye ray aimed at a visible bench should resolve the interactive object, consistently
                // frame after frame (this used to flip between a direct ray and a second, lower-angled ray).
                var benchCenter = R.Furnishings[prep].GetComponentInChildren<Collider>().bounds.center;
                P.Teleport(new Vector3(benchCenter.x, .15f, benchCenter.z + 2));
                Physics.SyncTransforms();
                P.LookAt(benchCenter); // eye height clears a 1 m bench on a level ray; players look down at counters
                bool stable = true;
                for (int i = 0; i < 6; i++) stable &= P.TryResolveInteractionHit(out var repeat) && TargetId(repeat) == prep;
                if (!stable) Debug.Log("INTERACTION_BENCH_MISS eye=" + P.InteractionRay.origin + " dir=" + P.InteractionRay.direction + " bench=" + benchCenter + " hit=" + (P.TryResolveInteractionHit(out var dbg) ? dbg.collider.name + "/" + (dbg.collider.transform.parent ? dbg.collider.transform.parent.name : "") + ":" + TargetId(dbg) + "@" + dbg.distance.ToString("0.00") : "none"));
                Check(stable, "eye ray at the bench resolves the same bench on every consecutive query");

                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "Interaction test obstruction";
                blocker.transform.position = P.InteractionRay.origin + P.InteractionRay.direction * 1.05f;
                blocker.transform.localScale = new Vector3(2, 3, .35f);
                Physics.SyncTransforms();
                Check(P.TryResolveInteractionHit(out var blockedHit) && blockedHit.collider == blocker.GetComponent<Collider>(),
                    "a closer wall-like obstruction reports immediately, never the target behind it");
                blocker.SetActive(false);
                Destroy(blocker);
                Physics.SyncTransforms();
                Check(P.TryResolveInteractionHit(out var restored) && TargetId(restored) == prep,
                    "removing the obstruction immediately restores the bench, no stale block");

                // B is handled before the usual ray pass in FirstPersonPlayer.Update.
                R.ClearPlayerFocus(P);
                R.HandlePlayerInput(P, false, false, false, false, true);
                Check(R.PanelOpen && R.Panel == "Furniture" && R.SelectedInstanceId == prep,
                    "build input selects current aim, without one-frame-old focus");
                if (R.PanelOpen) R.ClosePanel();

                R.ClearPlayerFocus(P);
                R.HandlePlayerInput(P, false, false, false, false, true);
                if (R.PanelOpen) R.ClosePanel();
                R.ShowPanel("Furniture");
                var itemName = RestaurantCatalog.Find("prep_bench").Name;
                var choice = R.UI.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name.Contains(itemName));
                Check(choice != null, "closed edit panel lists existing furnishing by name");
                if (choice) {
                    choice.onClick.Invoke();
                    Check(R.SelectedInstanceId == prep, "closed list selects obscured furnishing");
                    var move = R.UI.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Move furnishing");
                    Check(move != null, "selected furnishing offers move action");
                    if (move) {
                        move.onClick.Invoke();
                        Check(R.PlacementActive, "move action enters overhead placement");
                        var upperBuilding = GameObject.Find("Your restaurant's building");
                        Check(upperBuilding != null, "restaurant apartment shell exists for overhead regression");
                        Check(upperBuilding == null || upperBuilding.GetComponentsInChildren<Renderer>().All(r => !r.enabled || (P.View.cullingMask & (1 << r.gameObject.layer)) == 0), "placement view excludes apartment floors and roof above restaurant");
                        yield return new WaitForEndOfFrame();
                        System.IO.Directory.CreateDirectory("InteractionEvidence");
                        CapturePlacement();
                        if (R.PlacementActive) {
                            var placed = R.Data.Layout.First(x => x.InstanceId == prep);
                            int oldX = placed.X, oldZ = placed.Z;
                            var definition = RestaurantCatalog.Find(placed.CatalogId);
                            bool moved = false;
                            for (int z = 0; z < RestaurantState.GridD && !moved; z++) for (int x = 0; x < 12 && !moved; x++) {
                                if (x == placed.X && z == placed.Z || !R.Data.CanPlace(placed.CatalogId, x, z, placed.Rotation, prep, out _)) continue;
                                var world = RestaurantController.CellCenter(x, z, definition.Width, definition.Depth);
                                var pointer = P.View.WorldToScreenPoint(world);
                                if (R.UpdatePreviewFromPointer(pointer) && R.PreviewCell == new Vector2Int(x, z)) {
                                    Check(R.ConfirmPlacement(R.PreviewCell.x, R.PreviewCell.y), "pointer-mapped placement commits moved furnishing");
                                    moved = true;
                                }
                            }
                            Check(moved && (R.Data.Layout.First(x => x.InstanceId == prep).X != oldX || R.Data.Layout.First(x => x.InstanceId == prep).Z != oldZ),
                                "moved furnishing has new saved layout coordinates");
                            if (R.PlacementActive) R.CancelPlacement();
                        }
                    }
                }
                if (R.PanelOpen) R.ClosePanel();

                Check(AimAt(rack), "ray targets plate rack");
                Check((P.View.cullingMask & (1 << 27)) != 0, "first person view restores upper building after placement");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.Plate, "E picks up a clean plate through ray interaction");
                Check(AimAt(assembly), "ray targets assembly counter");
                P.ResolveAndInteract(true, false);
                Check(K.At(assembly)?.Kind == KitchenItemKind.Plate && K.Hold(P.ActorId) == null,
                    "E places clean plate on assembly counter");

                Check(AimAtShelf(pantry, "protein"), "ray targets the pantry's raw-patty shelf");
                Check(PromptFor().Contains("raw patty"), "pantry prompt names the sub-id ingredient (A2)");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein, "E takes raw patty from its own shelf, not a hidden Q cycle");
                bool tableFocused = AimAt(table);
                Check(tableFocused, "ray targets dining table");
                if (tableFocused) P.ResolveAndInteract(true, false);
                Check(tableFocused && !R.PanelOpen && K.Hold(P.ActorId)?.Kind == KitchenItemKind.RawProtein,
                    "E on adjacent table while carrying food never opens furniture menu");
                if (R.PanelOpen) R.ClosePanel();

                // Stage A workflow: raw patty goes straight to the grill (no prep-bench chop step).
                Check(AimAt(grill), "ray targets grill");
                P.ResolveAndInteract(true, false);
                Check(K.At(grill)?.Kind == KitchenItemKind.RawProtein, "E places the raw patty directly on the grill");
                K.Tick(Game.State, 8.1f);
                Check(K.At(grill)?.Kind == KitchenItemKind.CookedPatty, "grill cooks patty");

                Check(AimAt(assembly), "ray targets assembly for the held plate");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Kind == KitchenItemKind.Plate, "E picks the clean plate back up to carry it");
                Check(AimAt(grill), "ray targets grill while carrying a plate");
                Check(PromptFor().Contains("cooked patty"), "Preview offers the fewer-press shortcut onto a held plate (A3)");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Components.Contains("cooked_patty") == true && K.At(grill) == null,
                    "E slides the cooked patty straight onto the held plate");

                Check(AimAtShelf(pantry, "bun"), "ray targets the pantry's bun shelf");
                P.ResolveAndInteract(true, false);
                Check(K.Hold(P.ActorId)?.Components.Contains("bun") == true, "E adds a bun straight onto the held plate from its shelf");
                Check(K.RecipeOf(K.Hold(P.ActorId)) == "burger", "components-based recipe match recognizes the finished burger");

                // A placed two-seat table must accept food at either place setting through its own collider.
                R.Data.Layout.First(x => x.InstanceId == table).Rotation = 1;
                R.RebuildLayout(); Physics.SyncTransforms();
                R.Data.Open = true;
                R.Data.ActiveMenu.Clear(); R.Data.ActiveMenu.Add("burger");
                var firstGuest = R.Data.AddCustomer(Game.State, 0, table, out _);
                var secondGuest = R.Data.AddCustomer(Game.State, 0, table, out _);
                Require(firstGuest != null && secondGuest != null, "two guests can order at the placed table");
                var tableHits = new RaycastHit[2];
                foreach (var order in new[] { firstGuest, secondGuest }) {
                    if (K.Hold(P.ActorId) == null) K.Items.Add(new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.Plate, Holder = P.ActorId, Components = new System.Collections.Generic.List<string> { "bun", "cooked_patty" } });
                    int seatIndex = order == firstGuest ? 0 : 1;
                    var furniture = R.Furnishings[table];
                    var seat = furniture.transform.Find("Seat_" + seatIndex);
                    var local = seat.localPosition * .4f; local.y = .85f;
                    var point = furniture.transform.TransformPoint(local);
                    var outward = (seat.position - furniture.transform.position).normalized;
                    var origin = point + outward * 1.8f + Vector3.up * .15f;
                    Physics.SyncTransforms();
                    bool found = Physics.Raycast(origin, point - origin, out var serveHit, 2.5f) && TargetId(serveHit) == table;
                    Check(found, "table collider exposes serving spot " + seatIndex);
                    if (found) {
                        tableHits[seatIndex] = serveHit;
                        R.InspectPlayerRay(P, serveHit, false, false);
                        Check(R.PromptFor(P.ActorId).Contains("serve #" + order.Id), "table spot prompts its own guest " + seatIndex);
                        R.InspectPlayerRay(P, serveHit, true, false);
                    }
                    Check(order.Stage == RestaurantOrderStage.Eating && K.Hold(P.ActorId) == null, "table spot serves matching guest " + seatIndex);
                    Check(K.Items.Any(i => i.Holder == "table:" + order.Id && i.SeatNumber == seatIndex + 1), "served plate remembers its own seat " + seatIndex);
                }
                firstGuest.Stage = secondGuest.Stage = RestaurantOrderStage.Leaving;
                K.Tick(Game.State, .1f);
                Check(K.DirtyAtTable(table) == 2, "both place settings retain their dirty plates");
                // Clear the second setting first, so a table-wide first-item lookup cannot pass accidentally.
                foreach (int seatIndex in new[] { 1, 0 }) {
                    if (tableHits[seatIndex].collider) R.InspectPlayerRay(P, tableHits[seatIndex], true, false);
                    var dirty = K.Hold(P.ActorId);
                    int expectedId = seatIndex == 0 ? firstGuest.Id : secondGuest.Id;
                    Check(dirty?.Kind == KitchenItemKind.DirtyPlate && dirty.Holder == P.ActorId && !K.Items.Any(i => i.Holder == "table:" + expectedId), "clearing takes the aimed seat's plate " + seatIndex);
                    Check(K.DirtyAtTable(table) == (seatIndex == 1 ? 1 : 0), "clearing one setting leaves the other intact " + seatIndex);
                    if (dirty != null) { K.Items.Remove(dirty); K.CleanPlates++; }
                }
                R.Data.Open = false;

                Check(AimAtShelf(pantry, "sauce"), "ray targets the pantry's sauce shelf");
                Check(!PromptFor().Contains("E  Take"), "midnight sauce shelf is locked before the recipe is learned");
                P.Teleport(new Vector3(0, .15f, 0));
                Check(Game.CoOp.Join(), "second player joins isolated input test");
                var second = Game.CoOp.SecondPlayer;
                Check(second && second.gameObject.layer == 29 && second.OwnBodyMask == 1 << 29,
                    "second player's controller uses its own excluded interaction layer");
            RunFinishChecks();
            RunArchitectureChecks();
            Debug.Log("INTERACTION_RUNTIME_" + (failures == 0 ? "PASS " : "FAIL ") + checks + " checks, " + failures + " failures");
            Application.logMessageReceived -= OnLog;
            Application.Quit(failures == 0 ? 0 : 1);
        }
        void InstallFirstFree(string id) {
            for (int z = 0; z < RestaurantState.GridD; z++) for (int x = 0; x < 12; x++) if (R.Data.CanPlace(id, x, z, 0, -1, out _)) { R.Data.Place(Game.State, id, x, z, 0, out _); return; }
        }
        void RunArchitectureChecks() {
            R.ClosePanel(); R.Data.Open = false; Game.State.Cash = 2000;
            foreach (var id in new[] { "partition_wall", "service_window", "service_counter" }) {
                int before = R.Data.Layout.Count;
                InstallFirstFree(id);
                Check(R.Data.Layout.Count == before + 1, id + " can be placed through the catalog layout rules");
            }
            R.RebuildLayout();
            Physics.SyncTransforms();
            foreach (var id in new[] { "partition_wall", "service_window", "service_counter" }) {
                var placed = R.Data.Layout.LastOrDefault(x => x.CatalogId == id);
                Check(placed != null && R.Furnishings.ContainsKey(placed.InstanceId) &&
                    R.Furnishings[placed.InstanceId].GetComponentsInChildren<Renderer>().Any(r => r.enabled),
                    id + " has a visible, saved room object");
            }
            R.SelectCatalogItem("wall_teal");
            foreach (var id in new[] { "partition_wall", "service_window" }) {
                int instanceId = R.Data.Layout.Last(x => x.CatalogId == id).InstanceId;
                string key = "piece:" + instanceId;
                Check(R.ChooseFinishSurface(key) && R.ApplySelectedFinish(false) && R.Data.FinishAt(key) == "wall_teal",
                    id + " accepts the wall brush");
                Check(R.Furnishings[instanceId].GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled && r.sharedMaterial == RestaurantArt.FinishMaterial("wall_teal")) &&
                    !R.Furnishings[instanceId].transform.Find("Painted architecture faces"),
                    id + " finishes its full model rather than showing an inset paint patch");
            }
            R.CancelPlacement(false);
            P.Teleport(new Vector3(-10, .15f, -10.7f));
            P.LookAt(new Vector3(-10, 1.5f, -18));
            System.IO.Directory.CreateDirectory("InteractionEvidence");
            CapturePlacement("architecture-room.png");
        }
        void RunChoppingChecks(int prep) {
            var type = typeof(RestaurantController).Assembly.GetType("RestaurantCity.ChoppingFeedback");
            if (type == null) return; // The initial red check reports missing presentation without a compile error.
            var tick = typeof(RestaurantController).GetMethod("TickChoppingFeedback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Require(tick != null, "chopping presentation can reconcile accepted work");
            void Present(float dt = .1f) => tick.Invoke(R, new object[] { dt });
            Component View(int id) => R.StationObject(id).GetComponent(type);
            T Read<T>(Component v, string property) => (T)type.GetProperty(property).GetValue(v);
            bool standBuilt=Game.State.StandBuilt;
            Game.State.StandBuilt=true; Game.SyncWorld();
            int stand = K.Stations.First(s => s.CatalogId == "prep_bench" && KitchenState.IsStandStation(s.InstanceId)).InstanceId;
            var greens = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.RawGreens, Holder = "station:" + prep };
            var other = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.RawGreens, Holder = "station:" + stand };
            K.Items.Add(greens); K.Items.Add(other); Present();
            var view = View(prep); var standView = View(stand);
            Require(view && standView, "restaurant and stand each bind their own chopping presentation");
            Check(R.StationObject(prep).transform.Find("Chopping feedback").GetComponentsInChildren<Collider>().Length == 0,
                "chopping presentation adds no interaction colliders");
            var board = R.StationObject(prep).transform.Find("ChoppingBoard");
            P.Teleport(board.position + R.StationObject(prep).transform.forward * 1.1f + Vector3.up * -.85f);
            P.LookAt(board.position + Vector3.up * .1f);
            System.IO.Directory.CreateDirectory("InteractionEvidence"); CapturePlacement("chop-whole.png");
            Check(K.Work(Game.State, P.ActorId, prep, .8f, out _), "player accepted preparation work"); Present(.16f);
            Check(Read<bool>(view, "Active") && Read<float>(view, "Ratio") > .2f, "accepted work animates knife and progressively cuts greens");
            var knife = R.StationObject(prep).transform.Find("Chopping feedback/Knife");
            Check(knife && knife.localPosition.y > .04f, "productive stroke lifts knife off board");
            CapturePlacement("chop-working.png");
            float partial = Read<float>(view, "Ratio"); int strokes = Read<int>(view, "StrokeCount");
            Present(.3f);
            Check(!Read<bool>(view, "Active") && Read<int>(view, "StrokeCount") == strokes && Read<float>(view, "Ratio") == partial,
                "lookaway or release stops immediately even with stale WorkOwner and retains partial cuts");
            K.ReleaseWork(P.ActorId);
            K.Work(Game.State,P.ActorId,prep,.1f,out _); K.ReleaseWork(P.ActorId); Present();
            Check(!Read<bool>(view,"Active"), "release cancels a queued accepted stroke in the same presentation frame");
            Check(K.Work(Game.State, "staff:test", prep, .2f, out _) && K.Work(Game.State, "player:second-test", stand, .4f, out _), "staff and second player accept work independently"); Present(.35f);
            Check(Read<bool>(view, "Active") && Read<bool>(standView, "Active"), "two station knives run independently for staff and players");
            var prepSound=R.StationObject(prep).transform.Find("Chopping feedback").GetComponent<AudioSource>();
            var standSound=R.StationObject(stand).transform.Find("Chopping feedback").GetComponent<AudioSource>();
            Check(Read<int>(view,"StrokeCount")>strokes && prepSound && standSound && prepSound!=standSound && prepSound.spatialBlend==1,
                "knife impact produces its own spatial stroke sound with independent station sources");
            Check(!K.Work(Game.State, P.ActorId, prep, .4f, out _), "second actor cannot double progress owned ingredient"); Present(.2f);
            Check(!Read<bool>(view, "Active"), "rejected work produces no stroke");
            Check(!prepSound.isPlaying, "stopped work stops cutting audio");
            K.Work(Game.State, "staff:test", prep, .2f, out _); Game.SetPaused(true); Present(.2f);
            Check(!Read<bool>(view, "Active"), "pause suppresses accepted pending chopping"); Game.SetPaused(false);
            K.Work(Game.State, "staff:test", prep, 4, out _); Present(.2f);
            Check(greens.Kind == KitchenItemKind.ChoppedGreens && !Read<bool>(view, "Active") && Read<float>(view, "Ratio") == 1,
                "completed greens stay chopped and knife rests"); CapturePlacement("chop-finished.png");
            R.Data.Layout.First(x => x.InstanceId == prep).Rotation = 1; var oldView = view;
            R.RebuildLayout(); Present();
            Check(View(prep) && View(prep) != oldView, "rotated rebuilt furnishing rebinds presentation");
            K.Items.Remove(greens); K.Items.Remove(other); K.ReleaseWork("staff:test"); K.ReleaseWork("player:second-test"); Present();
            Check(Read<float>(View(prep), "Ratio") == 0 && !Read<bool>(View(prep), "Active"), "removed ingredient clears chopping presentation");
            Game.State.StandBuilt=standBuilt; Game.SyncWorld();
        }
        void CapturePlacement(string file = "placement.png") {
            // Render directly: an acceptance player may run in a hidden window.
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var previousRect = P.View.rect;
            try {
                P.View.rect = new Rect(0, 0, 1, 1);
                Canvas.ForceUpdateCanvases();
                target.Create();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(P.View, new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("InteractionEvidence/" + file), pixels.EncodeToPNG());
            } finally { P.View.rect = previousRect; Canvas.ForceUpdateCanvases(); RenderTexture.active = previous; target.Release(); Destroy(target); Destroy(pixels); }
        }
        // Hands-on washing: lift, scrub, put down (keeps progress), and only scrubbing the grime off finishes the plate.
        void RunScrubChecks(int sink) {
            foreach (var it in K.Items.Where(i => i.Holder == "station:" + sink).ToList()) K.Items.Remove(it);
            K.ReleaseWork(P.ActorId); var st = K.Stations.First(s => s.InstanceId == sink); st.Progress = 0; st.WorkOwner = null;
            K.Items.Add(new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.DirtyPlate, Holder = "station:" + sink });
            int clean0 = K.CleanPlates; var scrub = PlateScrub.Of(P);
            Check(scrub.Begin(sink, out var m) && scrub.Active && PlateScrub.HeldAt(sink), "E at the sink lifts the dirty plate up to scrub " + m);
            for (int i = 0; i < 10; i++) scrub.Tick(null, null, null, .1f);
            Check(st.Progress == 0, "holding the plate without scrubbing cleans nothing");
            scrub.ScrubPath(new Vector2(.3f, .45f), new Vector2(.7f, .5f), new Vector2(.3f, .55f)); scrub.Tick(null, null, null, .02f);
            float part = st.Progress;
            Check(part > 0 && K.At(sink) != null && scrub.CleanRatio > 0 && scrub.CleanRatio < .96f, "a few strokes clean part of the plate (" + scrub.CleanRatio + ")");
            scrub.End();
            Check(!scrub.Active && K.At(sink) != null && Mathf.Approximately(st.Progress, part) && string.IsNullOrEmpty(st.WorkOwner), "putting it down keeps the partial progress");
            Check(scrub.Begin(sink, out _), "pick it back up");
            Check(!K.Work(Game.State, "staff:test", sink, 1, out _), "nobody else can wash the plate you're holding");
            var path = new System.Collections.Generic.List<Vector2>();
            for (int pass = 0; pass < 5; pass++) for (float y = .06f; y < .95f; y += .03f) { bool odd = Mathf.RoundToInt(y / .03f) % 2 == 1; path.Add(new Vector2(odd ? .05f : .95f, y)); path.Add(new Vector2(odd ? .95f : .05f, y)); }
            scrub.ScrubPath(path.ToArray()); scrub.Tick(null, null, null, .02f);
            Check(!scrub.Active && K.At(sink) == null && K.CleanPlates == clean0 + 1 && !PlateScrub.HeldAt(sink), "scrubbing it clean returns exactly one clean plate (" + scrub.CleanRatio + ")");
        }
        // Two-sided patties: flip at golden for a perfect patty; never flipping still cooks (pale top); flips can't double up.
        void RunPattyChecks(int grill) {
            float g = KitchenState.SideGolden("grill", R.Data.LevelOf(grill)); var st = K.Stations.First(s => s.InstanceId == grill);
            KitchenItem Fresh() { foreach (var it in K.Items.Where(i => i.Holder == "station:" + grill).ToList()) K.Items.Remove(it); st.Progress = 0; var p = new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.RawProtein, Holder = "station:" + grill }; K.Items.Add(p); return p; }
            var patty = Fresh(); Game.State.Events.Clear();
            K.Tick(Game.State, g * 1.1f);
            Check(Game.State.Events.Exists(e => e == "flipready:" + grill), "the grill calls for a flip once the underside is golden");
            Check(K.FlipPatty(Game.State, grill, out var grade) && grade == "perfect", "flipping a golden underside is a perfect flip (" + grade + ")");
            Check(!K.FlipPatty(Game.State, grill, out _), "a second flip can't land on top of the first");
            K.Tick(Game.State, g * 1.1f);
            Check(patty.Kind == KitchenItemKind.CookedPatty && patty.Quality >= .99f, "golden on both sides makes a perfect patty (" + patty.Quality + ")");
            K.Tick(Game.State, g * 1.2f);
            Check(patty.Kind == KitchenItemKind.CookedPatty && patty.Quality > .8f && patty.Quality < .99f, "leaving it past golden drops to good, not ruined (" + patty.Quality + ")");
            patty = Fresh(); K.Tick(Game.State, g * 2.05f);
            Check(patty.Kind == KitchenItemKind.CookedPatty && patty.Quality < .8f, "never flipping still cooks, but pale on top (" + patty.Quality + ")");
            K.Tick(Game.State, StationUpgrades.BurnSeconds(R.Data.LevelOf(grill)) * KitchenState.SideBurnShare);
            Check(patty.Kind == KitchenItemKind.BurntPatty, "forgetting it on one side burns it");
            Fresh(); K.Tick(Game.State, g * .3f);
            Check(K.FlipPatty(Game.State, grill, out grade) && grade == "early", "flipping a raw underside is graded early");
            foreach (var it in K.Items.Where(i => i.Holder == "station:" + grill).ToList()) K.Items.Remove(it); st.Progress = 0;
        }
        // Standing close and looking DOWN at the lower shelf must pick what you look at, not the shelf box in front of it.
        void RunPantryAimChecks(int pantry) {
            var obj = R.StationObject(pantry); if (!obj) { Check(false, "pantry object exists"); return; }
            foreach (var (shelf, x, y) in new[] { ("protein", -.48f, .265f), ("greens", .48f, .265f), ("bun", -.48f, .805f) }) {
                var target = obj.transform.TransformPoint(new Vector3(x, y + .06f, 0));
                var flat = obj.transform.forward; flat.y = 0; flat.Normalize();
                P.Teleport(new Vector3(target.x, .15f, target.z) + flat * 1.05f); P.LookAt(target); Physics.SyncTransforms();
                string got = R.PantryAim(pantry, "pantry", P.InteractionRay, "", P.ActorId);
                Check(got == shelf, "looking down at the " + shelf + " picks " + shelf + " (got " + got + ")");
            }
        }
        // Hands-on chopping: each slice is cut once, putting it down keeps the cuts, six cuts make chopped greens.
        void RunChopChecks(int prep) {
            var st = K.Stations.First(s => s.InstanceId == prep);
            foreach (var it in K.Items.Where(i => i.Holder == "station:" + prep).ToList()) K.Items.Remove(it);
            K.ReleaseWork(P.ActorId); st.Progress = 0; st.WorkOwner = null;
            K.Items.Add(new KitchenItem { Id = K.NextItemId++, Kind = KitchenItemKind.RawGreens, Holder = "station:" + prep });
            var chop = PrepChop.Of(P);
            Check(chop.Begin(prep, out var m) && chop.Active && PrepChop.HeldAt(prep), "E at the cutting board brings the lettuce up to chop " + m);
            for (int i = 0; i < 10; i++) chop.Tick(null, null, null, .1f);
            Check(st.Progress == 0, "holding the knife without chopping does nothing");
            chop.ChopAt(-.1f, -.1f); chop.Tick(null, null, null, .02f);
            Check(chop.Cuts == 1 && st.Progress > 0 && K.At(prep)?.Kind == KitchenItemKind.RawGreens, "chopping the same slice twice cuts it once (" + chop.Cuts + ")");
            Check(!K.Work(Game.State, "staff:test", prep, 1, out _), "nobody else can chop the lettuce you're working on");
            float part = st.Progress; chop.End();
            Check(!chop.Active && Mathf.Approximately(st.Progress, part) && string.IsNullOrEmpty(st.WorkOwner), "putting it down keeps the cut slices");
            Check(chop.Begin(prep, out _) && chop.Cuts == 1, "picking it back up resumes with one slice cut");
            chop.ChopAt(-.1f, -.06f, -.02f, .02f, .06f, .1f); chop.Tick(null, null, null, .02f);
            Check(!chop.Active && K.At(prep)?.Kind == KitchenItemKind.ChoppedGreens, "six cuts make chopped greens");
            foreach (var it in K.Items.Where(i => i.Holder == "station:" + prep).ToList()) K.Items.Remove(it); st.Progress = 0;
        }
        int Station(string id) => R.Data.Layout.First(x => x.CatalogId == id).InstanceId;
        static int TargetId(RaycastHit hit) { var t = hit.collider ? hit.collider.GetComponentInParent<RestaurantTarget>() : null; return t ? t.InstanceId : -1; }
        // The HUD prompt follows the focus a normal frame sets; refresh it without pressing anything.
        string PromptFor() { P.ResolveAndInteract(false, false); return R.PromptFor(P.ActorId); }
        bool AimAt(int id) {
            if (!R.Furnishings.TryGetValue(id, out var furnishing)) return false;
            var collider = furnishing.GetComponentInChildren<Collider>();
            if (!collider) return false;
            Physics.SyncTransforms();
            var center = collider.bounds.center;
            string misses = "";
            foreach (var offset in new[] { new Vector3(0, 0, 1.7f), new Vector3(-1.7f, 0, 0), new Vector3(1.7f, 0, 0), new Vector3(0, 0, -1.7f) }) {
                var at = center + offset;
                if (at.x < -16 || at.x > -4 || at.z < -21 || at.z > -9) continue;
                P.Teleport(new Vector3(at.x, .15f, at.z));
                P.LookAt(center);
                Physics.SyncTransforms();
                bool found = P.TryResolveInteractionHit(out var hit);
                if (found && TargetId(hit) == id) return true;
                misses += " offset=" + offset + " eye=" + P.InteractionRay.origin + " direction=" + P.InteractionRay.direction + " hit=" + (found ? hit.collider.name + ":" + TargetId(hit) + "@" + hit.distance.ToString("0.00") : "none");
            }
            Debug.Log("INTERACTION_AIM_MISS id=" + id + " collider=" + collider.name + " bounds=" + collider.bounds + misses);
            return false;
        }
        // Aims at one named pantry shelf (A2's per-ingredient hitbox) rather than the furnishing's general bounds.
        bool AimAtShelf(int pantryId, string subId) {
            if (!R.Furnishings.TryGetValue(pantryId, out var furnishing)) return false;
            var shelf = furnishing.transform.Find("Pantry shelf " + subId);
            var collider = shelf ? shelf.GetComponent<Collider>() : null;
            if (!collider) return false;
            Physics.SyncTransforms();
            var center = collider.bounds.center;
            foreach (var offset in new[] { new Vector3(0, 0, 1.7f), new Vector3(0, .3f, 1.6f) }) {
                P.Teleport(new Vector3(center.x + offset.x, .15f + offset.y, center.z + offset.z));
                P.LookAt(center);
                Physics.SyncTransforms();
                if (P.TryResolveInteractionHit(out var hit) && hit.collider == collider) return true;
            }
            return false;
        }
        void Check(bool result, string label) { checks++; if (result) Debug.Log("INTERACTION_CHECK_PASS " + label); else { failures++; Debug.LogWarning("INTERACTION_CHECK_FAIL " + label); } }
        void Require(bool result, string label) { Check(result, label); if (!result) throw new InvalidOperationException(label); }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Assert) { Application.logMessageReceived -= OnLog; Application.Quit(1); } }
    }
}
