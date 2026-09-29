using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace RestaurantCity {
    // Input devices belong to a player explicitly; Gamepad.current is never shared.
    [DefaultExecutionOrder(-80)]
    public class LocalCoop : MonoBehaviour {
        public static LocalCoop Instance { get; private set; }
        readonly List<FirstPersonPlayer> players = new List<FirstPersonPlayer>();
        readonly Dictionary<int, GameObject> bodies = new Dictionary<int, GameObject>();
        public IReadOnlyList<FirstPersonPlayer> Players => players;
        public FirstPersonPlayer SecondPlayer => players.Count > 1 ? players[1] : null;
        public int PlayerCount => players.Count;
        CityGame game;
        public void Initialize(CityGame owner) {
            Instance = this; game = owner;
            players.Clear(); game.Player.PlayerId = 0; players.Add(game.Player);
            game.Player.InitializeCamera(); AddBody(game.Player);
            InputSystem.onDeviceChange += DeviceChanged;
            if (game.Restaurant && game.Restaurant.Room) {
                foreach (var part in game.Restaurant.Room.GetComponentsInChildren<Transform>())
                    if (part.name.IndexOf("Ceiling", System.StringComparison.OrdinalIgnoreCase) >= 0 || part.name.IndexOf("Roof", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        foreach (var roofPart in part.GetComponentsInChildren<Transform>()) roofPart.gameObject.layer = 27;
            }
            // The city shell includes apartment floors above the runtime room. Cut the whole shell
            // away in overhead views, not just meshes whose names contain "roof".
            var upperBuilding = GameObject.Find("Your restaurant's building");
            if (upperBuilding)
                foreach (var part in upperBuilding.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 27;
            LogDevices(); RefreshViews();
        }
        void OnDestroy() { InputSystem.onDeviceChange -= DeviceChanged; if (Instance == this) Instance = null; }
        void DeviceChanged(InputDevice device, InputDeviceChange change) {
            if (!(device is Gamepad)) return;
            Debug.Log("LOCAL_COOP device " + change + ": " + device.displayName + " (" + device.deviceId + ")");
            if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed) {
                foreach (var player in players) if (player.AssignedGamepad == device) {
                    player.AssignedGamepad = null;
                    game.Notify("Player " + (player.PlayerId + 1) + " controller disconnected. Reconnect and press Start.", 8);
                }
            }
            LogDevices();
        }
        void LogDevices() {
            Debug.Log("LOCAL_COOP detected " + Gamepad.all.Count + " gamepad(s).");
            foreach (var pad in Gamepad.all) Debug.Log("LOCAL_COOP gamepad id=" + pad.deviceId + " name=" + pad.displayName + " layout=" + pad.layout);
        }
        void Update() {
            if (!game) return;
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame && !SecondPlayer) Join();
            // Dev shortcuts for trying night features: F8 skips to nightfall, F9 puts 3 bottles of Zeeb's sauce in your hands.
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) { game.State.Clock = 151; game.Notify("DEV: skipped to nightfall.", 4); }
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) {
                var st = game.State; st.DropBottles = 3; st.DropPlaced = true; st.StashSpot = 0;
                bool ok = st.Kitchen.CollectStash(st, game.Player.ActorId, out var m);
                if (!ok) { st.DropBottles = 0; st.DropPlaced = false; st.StashSpot = -1; }
                game.Notify(ok ? "DEV: you're carrying 3 bottles of Zeeb's sauce. Watch out for inspectors." : "DEV: free your hands first.", 5);
            }
            foreach (var pad in Gamepad.all) {
                if (!pad.startButton.wasPressedThisFrame) continue;
                bool assigned = false;
                foreach (var p in players) if (p.AssignedGamepad == pad) assigned = true;
                if (assigned) continue;
                if (!SecondPlayer) { Join(pad); continue; }
                if (SecondPlayer.AssignedGamepad == null) {
                    SecondPlayer.AssignedGamepad = pad; SecondPlayer.SuppressInputFrame = Time.frameCount;
                    game.Notify("Player 2 controller connected.", 4);
                } else if (game.Player.AssignedGamepad == null) {
                    game.Player.AssignedGamepad = pad; game.Player.SuppressInputFrame = Time.frameCount;
                    game.Notify("Second controller assigned to Player 1. Keyboard still works.", 5);
                }
            }
            foreach (var player in players) {
                if (!bodies.TryGetValue(player.PlayerId, out var body) || !body) continue;
                var motion = body.GetComponent<CharacterMotion>();
                if (!motion) continue;
                var v = player.GetComponent<CharacterController>().velocity; v.y = 0;
                motion.Walking = v.sqrMagnitude > .1f && !game.Paused; motion.Running = player.Sprinting;
                if (bodyJumps.TryGetValue(player.PlayerId, out var seen) ? seen != player.LastJumpTime : player.LastJumpTime > 0) { bodyJumps[player.PlayerId] = player.LastJumpTime; if (player.LastJumpTime > 0) motion.Jump(); }
            }
        }
        public bool Join(Gamepad pad = null) {
            if (!game || SecondPlayer || pad != null && game.Player.AssignedGamepad == pad) return false;
            var root = new GameObject("Player 2 / Local co-op");
            var cc = root.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .28f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .3f;
            var p = root.AddComponent<FirstPersonPlayer>(); p.Game = game; p.PlayerId = 1; p.AssignedGamepad = pad;
            var cameraObject = new GameObject("Player 2 camera"); cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
            p.View = cameraObject.AddComponent<Camera>(); p.View.CopyFrom(game.Player.View); p.View.depth = game.Player.View.depth + 1;
            cameraObject.transform.localPosition=new Vector3(0,1.55f,0);cameraObject.transform.localRotation=Quaternion.identity;
            p.View.ResetWorldToCameraMatrix();p.View.ResetProjectionMatrix();p.View.tag = "Untagged";
            var urp = p.View.GetUniversalAdditionalCameraData();
            var hostUrp = game.Player.View.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = hostUrp.renderPostProcessing; urp.antialiasing = hostUrp.antialiasing;
            if (game.Player.Spatula) {
                p.Spatula = Instantiate(game.Player.Spatula, cameraObject.transform, false);
                p.Spatula.localPosition = game.Player.Spatula.localPosition; p.Spatula.localRotation = game.Player.Spatula.localRotation;
            }
            p.InitializeCamera();
            p.Teleport(game.Player.transform.position + game.Player.transform.right * .9f);
            p.transform.rotation = game.Player.transform.rotation;
            p.SuppressInputFrame = Time.frameCount;
            players.Add(p); AddBody(p);
            Physics.IgnoreCollision(game.Player.GetComponent<CharacterController>(), cc, true);
            RefreshViews();
            game.Notify(pad != null ? "Player 2 joined! Left stick move / A interact / hold X work / Y camera." : "Developer preview: Player 2 body added. Connect a controller and press Start to play.", 8);
            Debug.Log("LOCAL_COOP joined player 2 device=" + (pad == null ? "none (developer preview)" : pad.displayName + " id=" + pad.deviceId));
            return true;
        }
        public void LeaveSecond() {
            if (!SecondPlayer) return;
            var second = SecondPlayer; players.RemoveAt(1); bodies.Remove(1); Destroy(second.gameObject); RefreshViews();
        }
        public static readonly ResidentDef PlayerOneLook = new ResidentDef("003_Jimmy", "You", 1.72f, 0, StaffJob.Any, "");
        public static readonly ResidentDef PlayerTwoLook = new ResidentDef("056_Olivia", "Player 2", 1.66f, 0, StaffJob.Any, "", Gait.Light);
        readonly System.Collections.Generic.Dictionary<int, float> bodyJumps = new System.Collections.Generic.Dictionary<int, float>();
        void AddBody(FirstPersonPlayer player) {
            if (bodies.ContainsKey(player.PlayerId)) return;
            // Players get a resident body too (animated walk/run/jump) when the Mixamo clips are installed.
            var body = People.UseResidents ? ResidentModels.Create(player.PlayerId == 0 ? PlayerOneLook : PlayerTwoLook, player.transform) : RestaurantArt.CreateCharacter(player.PlayerId == 0 ? 0 : 1, player.transform);
            body.name = "Player " + (player.PlayerId + 1) + " visible body";
            body.transform.localPosition = Vector3.zero; body.transform.localRotation = Quaternion.identity;
            foreach (var child in body.GetComponentsInChildren<Transform>()) child.gameObject.layer = player.PlayerId == 0 ? 28 : 29;
            foreach (var collider in body.GetComponentsInChildren<Collider>()) collider.enabled = false;
            bodies[player.PlayerId] = body;
        }
        public void SetElevated(FirstPersonPlayer player, bool elevated) {
            if (!player || !players.Contains(player)) return;
            if (player.PlayerId == 0 && game.Restaurant && game.Restaurant.PlacementActive) return;
            player.Elevated = elevated; player.RefreshCameraPose();
        }
        public void ToggleElevated(FirstPersonPlayer player) { if (player) SetElevated(player, !player.Elevated); }
        public void RefreshViews() {
            if (!game || !game.Player) return;
            bool modal = game.Restaurant && (game.Restaurant.PanelOpen || game.Restaurant.PlacementActive);
            bool split = SecondPlayer && !modal;
            game.Player.View.rect = split ? new Rect(0, 0, .5f, 1) : new Rect(0, 0, 1, 1);
            game.Player.View.fieldOfView = split ? 70 : 65;
            if (!(game.Restaurant && game.Restaurant.PlacementActive)) game.Player.RefreshCameraPose();
            if (SecondPlayer) {
                SecondPlayer.View.enabled = split; SecondPlayer.View.rect = new Rect(.5f, 0, .5f, 1);
                SecondPlayer.View.fieldOfView = 70; SecondPlayer.RefreshCameraPose();
            }
        }
    }
}

