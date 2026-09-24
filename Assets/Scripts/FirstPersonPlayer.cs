using UnityEngine;
using UnityEngine.InputSystem;

namespace RestaurantCity {
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour {
        public Camera View;
        public CityGame Game;
        public float Sensitivity = .11f;
        public Interactable Target { get; private set; }
        CharacterController controller;
        float pitch, gravity, swingTimer;
        public Transform Spatula;
        Quaternion toolRotation;

        void Awake() { controller = GetComponent<CharacterController>(); if (Spatula) toolRotation = Spatula.localRotation; }
        void Update() {
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) Game.SetPaused(!Game.Paused);
            Target = null;
            if (!Game.Paused) swingTimer = Mathf.Max(0, swingTimer - Time.deltaTime);
            if (Game.Paused || Game.SmokeMode) return;
            if (mouse != null) {
                Vector2 delta = mouse.delta.ReadValue() * Sensitivity;
                transform.Rotate(0, delta.x, 0);
                pitch = Mathf.Clamp(pitch - delta.y, -78, 78);
                View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            if (keys != null) {
                float x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                float z = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
                Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1);
                gravity = controller.isGrounded ? -2 : gravity - 22 * Time.deltaTime;
                controller.Move((move * (keys.leftShiftKey.isPressed ? 6.5f : 4) + Vector3.up * gravity) * Time.deltaTime);
            }
            if (Physics.Raycast(View.transform.position, View.transform.forward, out var hit, 3.6f, ~0, QueryTriggerInteraction.Ignore)) {
                Target = hit.collider.GetComponentInParent<Interactable>();
                if (Target && keys != null && keys.eKey.wasPressedThisFrame) Game.Interact(Target.Kind);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) Swing();
            if (Spatula) Spatula.localRotation = toolRotation * Quaternion.Euler(Mathf.Sin(swingTimer / .55f * Mathf.PI) * -65, 0, 0);
            if (transform.position.y < -5) Teleport(Game.SpawnPoint);
        }
        public bool Swing() {
            if (swingTimer > 0) return false;
            swingTimer = .55f;
            if (Physics.SphereCast(View.transform.position, .25f, View.transform.forward, out var strike, 3, ~0, QueryTriggerInteraction.Ignore)) {
                var guard = strike.collider.GetComponentInParent<StreetGuard>();
                if (guard) { guard.Hit(); return true; }
            }
            return false;
        }
        public void Teleport(Vector3 position) {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false; transform.position = position; controller.enabled = true; gravity = 0;
        }
        public void LookAt(Vector3 point) {
            Vector3 direction = point - View.transform.position;
            transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(direction).eulerAngles.y, 0);
            pitch = Mathf.DeltaAngle(0, Quaternion.LookRotation(direction).eulerAngles.x);
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        void OnApplicationFocus(bool focus) { if (!focus && Game && !Game.SmokeMode) Game.SetPaused(true); }
    }
}
