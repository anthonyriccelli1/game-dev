using UnityEngine;
using UnityEngine.InputSystem;

namespace RestaurantCity {
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour {
        public Camera View;
        public CityGame Game;
        public int PlayerId;
        public string ActorId => "player:" + PlayerId;
        public Gamepad AssignedGamepad;
        public bool Elevated;
        public bool InteractHeld { get; private set; }
        public float Health = 100, Sensitivity = .11f;
        public Interactable Target { get; private set; }
        public Transform Spatula;
        public Ray InteractionRay => Elevated
            ? new Ray(transform.position + Vector3.up * 1.2f, transform.forward + Vector3.down * .12f)
            : new Ray(View.transform.position, View.transform.forward);
        public int OwnBodyMask => 1 << (PlayerId == 0 ? 28 : 29);
        public int SuppressInputFrame = -1;
        CharacterController controller;
        float pitch, gravity, swingTimer;
        Quaternion toolRotation;
        Vector3 eyePosition = new Vector3(0, 1.55f, 0);
        bool cameraInitialized;
        // Target stickiness (Stage A / A1): once a valid target is acquired, a DIFFERENT valid target only
        // takes over after being seen for >= StickySwitch seconds, and losing the target entirely only
        // clears it after >= StickyLose seconds. This is the only place interaction targets are resolved;
        // there is exactly one raycast query, so the prompt and the executed action can never disagree.
        const float StickySwitch = .12f, StickyLose = .2f;
        RaycastHit stickyHit; bool hasSticky; Component stickyKey, candidateKey; float candidateTimer, missTimer;
        void Awake() { controller = GetComponent<CharacterController>(); if (Spatula) toolRotation = Spatula.localRotation; }
        public void InitializeCamera() {
            // The CharacterController lives on this root, not on the visual body child.
            // Put it on the same excluded layer so eye rays and spatula casts cannot hit our own capsule.
            gameObject.layer = PlayerId == 0 ? 28 : 29;
            if (!View || cameraInitialized) return;
            eyePosition = View.transform.localPosition;
            if (eyePosition.y < .5f) eyePosition = new Vector3(0, 1.55f, 0);
            if (Spatula) toolRotation = Spatula.localRotation;
            cameraInitialized = true;
        }
        void Update() {
            if (!Game || !View) return;
            InitializeCamera();
            var keys = PlayerId == 0 ? Keyboard.current : null;
            var mouse = PlayerId == 0 ? Mouse.current : null;
            var pad = AssignedGamepad != null && AssignedGamepad.added ? AssignedGamepad : null;
            bool suppressed = SuppressInputFrame == Time.frameCount;
            InteractHeld = !suppressed && (keys != null && keys.eKey.isPressed || pad != null && (pad.buttonSouth.isPressed || pad.buttonWest.isPressed));
            if (PlayerId == 0 && Game.Restaurant && (Game.Restaurant.PanelOpen || Game.Restaurant.PlacementActive) && Game.Restaurant.HandleInput(keys, mouse)) { InteractHeld = false; return; }
            if (!suppressed && (keys != null && keys.escapeKey.wasPressedThisFrame || pad != null && pad.startButton.wasPressedThisFrame)) Game.SetPaused(!Game.Paused);
            Target = null;
            if (!Game.Paused) swingTimer = Mathf.Max(0, swingTimer - Time.deltaTime);
            if (Game.Paused || Game.SmokeMode || suppressed) { InteractHeld = false; return; }
            bool interact = keys != null && keys.eKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool secondary = keys != null && keys.qKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame;
            bool menu = keys != null && keys.tabKey.wasPressedThisFrame || pad != null && pad.leftShoulder.wasPressedThisFrame;
            bool build = keys != null && keys.bKey.wasPressedThisFrame || pad != null && pad.dpad.up.wasPressedThisFrame;
            if (Game.Restaurant && Game.Restaurant.HandlePlayerInput(this, interact, InteractHeld, secondary, menu, build)) return;
            if (keys != null && keys.vKey.wasPressedThisFrame || pad != null && pad.buttonNorth.wasPressedThisFrame) LocalCoop.Instance?.ToggleElevated(this);
            Vector2 move = Vector2.zero;
            if (keys != null) move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            if (pad != null) move += pad.leftStick.ReadValue();
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked && !Elevated) ApplyLook(mouse.delta.ReadValue() * Sensitivity);
            if (pad != null && !Elevated) ApplyLook(pad.rightStick.ReadValue() * (130 * Time.deltaTime));
            ApplyMovement(move, Time.deltaTime, keys != null && keys.leftShiftKey.isPressed || pad != null && pad.leftStickButton.isPressed);
            ResolveAndInteract(interact, InteractHeld);
            if (mouse != null && mouse.leftButton.wasPressedThisFrame || pad != null && pad.rightShoulder.wasPressedThisFrame) Swing();
            if (Spatula) Spatula.localRotation = toolRotation * Quaternion.Euler(Mathf.Sin(swingTimer / .55f * Mathf.PI) * -65, 0, 0);
            if (transform.position.y < -5) Teleport(Game.SpawnPoint + Vector3.right * PlayerId);
        }
        static Component TargetKey(RaycastHit hit) {
            if (!hit.collider) return null;
            Component key = hit.collider.GetComponentInParent<RestaurantTarget>();
            return key ? key : hit.collider.GetComponentInParent<Interactable>();
        }
        // Single interaction query: one raycast, optionally widened by ONE coaxial sphere-cast (never a
        // second, differently-angled ray) so a thin station edge still registers without ever letting two
        // simultaneous queries disagree about what the player is aiming at.
        //
        // Stickiness only smooths the choice between two DIFFERENT valid targets (e.g. the ray grazes the
        // boundary between two adjacent stations) and briefly bridges a true miss (looking at empty space).
        // A hit on real geometry that simply is not a target (a wall, an obstruction) is reported immediately
        // and never masked by a lingering old target, so "what's blocking my view" is always accurate.
        public bool TryResolveInteractionHit(out RaycastHit hit) {
            bool direct = Physics.Raycast(InteractionRay, out hit, 3.6f, ~OwnBodyMask, QueryTriggerInteraction.Ignore);
            bool hasTarget = direct && TargetKey(hit);
            if (!hasTarget) {
                var ray = InteractionRay;
                if (Physics.SphereCast(ray.origin, .15f, ray.direction, out var wideHit, 3.6f, ~OwnBodyMask, QueryTriggerInteraction.Ignore)
                    && TargetKey(wideHit) && (!direct || wideHit.distance <= hit.distance + .01f)) {
                    hit = wideHit; direct = true; hasTarget = true;
                }
            }
            // A remembered target can be destroyed (a served customer walks off); forget it.
            if (hasSticky && !stickyHit.collider) { hasSticky = false; stickyKey = null; }
            float dt = Time.deltaTime;
            if (!direct) {
                candidateKey = null; candidateTimer = 0;
                missTimer += dt;
                if (missTimer < StickyLose && hasSticky) { hit = stickyHit; return true; }
                hasSticky = false; return false;
            }
            missTimer = 0;
            var key = hasTarget ? TargetKey(hit) : null;
            if (key == null) { hasSticky = false; candidateKey = null; candidateTimer = 0; return true; }
            if (!hasSticky || key == stickyKey) { hasSticky = true; stickyKey = key; stickyHit = hit; candidateKey = null; candidateTimer = 0; return true; }
            if (key == candidateKey) candidateTimer += dt; else { candidateKey = key; candidateTimer = dt; }
            if (candidateTimer >= StickySwitch) { stickyKey = key; stickyHit = hit; candidateKey = null; candidateTimer = 0; hit = stickyHit; return true; }
            hit = stickyHit; return true;
        }
        // Teleporting jumps the player to an unrelated part of the world; the previous sticky target would
        // otherwise linger for up to StickyLose seconds and point at something no longer nearby.
        public void ResetInteractionTarget() { hasSticky = false; stickyKey = null; candidateKey = null; candidateTimer = 0; missTimer = 0; }
        public void ResolveAndInteract(bool pressed, bool held) {
            Target = null;
            if (Game.Restaurant) Game.Restaurant.ClearPlayerFocus(this);
            if (TryResolveInteractionHit(out var hit)) {
                bool usedRestaurant = Game.Restaurant && Game.Restaurant.InspectPlayerRay(this, hit, pressed, held);
                Target = hit.collider.GetComponentInParent<Interactable>();
                if (!usedRestaurant && Target && pressed) Game.InteractForPlayer(Target.Kind, this, Target.Site);
            }
        }
        public void ApplyLook(Vector2 degrees) {
            transform.Rotate(0, degrees.x, 0);
            pitch = Mathf.Clamp(pitch - degrees.y, -78, 78);
            if (!Elevated && View) View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void ApplyMovement(Vector2 input, float seconds, bool sprint = false) {
            if (!controller) controller = GetComponent<CharacterController>();
            Vector3 forward = Elevated ? Vector3.ProjectOnPlane(View.transform.forward, Vector3.up).normalized : transform.forward;
            Vector3 right = Elevated ? Vector3.ProjectOnPlane(View.transform.right, Vector3.up).normalized : transform.right;
            Vector3 move = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1);
            if (Elevated && move.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(move);
            gravity = controller.isGrounded ? -2 : gravity - 22 * seconds;
            controller.Move((move * (sprint ? 6.5f : 4) + Vector3.up * gravity) * seconds);
        }
        void LateUpdate() {
            if (!Game || !View || PlayerId == 0 && Game.Restaurant && Game.Restaurant.PlacementActive) return;
            if (Elevated) RefreshCameraPose();
        }
        public void RefreshCameraPose() {
            InitializeCamera();
            if (!View) return;
            View.orthographic = false;
            if (Elevated) {
                View.transform.position = transform.position + new Vector3(0, 9.5f, 7);
                View.transform.rotation = Quaternion.LookRotation(transform.position + Vector3.up * .6f - View.transform.position);
            } else {
                View.transform.localPosition = eyePosition;
                View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            if (Spatula) Spatula.gameObject.SetActive(!Elevated);
            View.cullingMask = (Elevated ? ~(1 << 27) : ~OwnBodyMask) & ~(1 << (PlayerId == 0 ? 26 : 25));
        }
        public bool Swing() {
            if (swingTimer > 0) return false;
            swingTimer = .55f;
            Ray ray = InteractionRay;
            if (Physics.SphereCast(ray.origin, .25f, ray.direction, out var strike, 3, ~OwnBodyMask, QueryTriggerInteraction.Ignore)) {
                var guard = strike.collider.GetComponentInParent<StreetGuard>();
                if (guard) { guard.Hit(); return true; }
            }
            return false;
        }
        public void Teleport(Vector3 position) {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false; transform.position = position; controller.enabled = true; gravity = 0;
            ResetInteractionTarget();
            if (Elevated) RefreshCameraPose();
        }
        public void LookAt(Vector3 point) {
            Vector3 direction = point - (Elevated ? transform.position + Vector3.up * 1.55f : View.transform.position);
            if (direction.sqrMagnitude < .0001f) return;
            transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(direction).eulerAngles.y, 0);
            pitch = Mathf.DeltaAngle(0, Quaternion.LookRotation(direction).eulerAngles.x);
            if (Elevated) RefreshCameraPose(); else View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        void OnApplicationFocus(bool focus) { if (PlayerId == 0 && !focus && Game && !Game.SmokeMode) Game.SetPaused(true); }
    }
}
