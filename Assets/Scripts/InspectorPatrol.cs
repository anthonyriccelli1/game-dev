using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Night health inspectors: Observers with flashlights patrol Old Market's streets after dark.
    // See you carrying Zeeb's sauce -> suspicion -> "STOP!" -> stand still to be searched, or run.
    // Sprinting outpaces them; break line of sight long enough and they give up. Rules live in Inspections.
    public partial class RestaurantController {
        public static readonly ResidentDef InspectorLook = new ResidentDef("007_Observer", "Night Inspector", 1.9f, 0, StaffJob.Any, "");
        public const float PatrolSpeed = 1.6f, ApproachSpeed = 3.2f, ChaseSpeed = 5.6f, SightRange = 14f, SightAngle = 35f,
                           SuspicionSeconds = 1.2f, EscapeSeconds = 5f, SearchRange = 1.8f, CatchRange = 1.3f, FleeRange = 9f, Cooldown = 20f;
        // Walking routes along the roads (ping-pong or loops).
        static readonly Vector3[][] Routes = {
            new[] { new Vector3(-75, 0, -1.5f), new Vector3(-6, 0, -1.5f) },
            new[] { new Vector3(6, 0, 1.5f), new Vector3(75, 0, 1.5f) },
            new[] { new Vector3(39, 0, -1), new Vector3(39, 0, -50), new Vector3(-39, 0, -50), new Vector3(-39, 0, -1) },
        };
        public enum InspectorMode { Patrol, Suspicious, Stop, Chase, Cooldown }
        public class Inspector {
            public GameObject Root; public CharacterMotion Motion; public TextMesh Bubble; public Light Torch;
            public int Route, Leg; public float Suspicion, Lost, CooldownLeft, StopTimer, StopDistance; public InspectorMode Mode; public FirstPersonPlayer Target;
        }
        public readonly List<Inspector> Inspectors = new List<Inspector>();
        bool inspectorsOut;

        IEnumerable<FirstPersonPlayer> InspectablePlayers() {
            if (LocalCoop.Instance != null && LocalCoop.Instance.PlayerCount > 0) { foreach (var p in LocalCoop.Instance.Players) if (p) yield return p; }
            else if (Game.Player) yield return Game.Player;
        }

        void TickInspectors(float seconds) {
            bool night = Game.State.IsNight && People.UseResidents;
            if (night != inspectorsOut) {
                inspectorsOut = night;
                foreach (var i in Inspectors) if (i.Root) Destroy(i.Root);
                Inspectors.Clear();
                if (night) { for (int r = 0; r < Routes.Length; r++) SpawnInspector(r); Game.Notify("Night falls. Health inspectors are out on patrol. Don't get caught carrying Zeeb's sauce.", 7); }
            }
            foreach (var i in Inspectors) if (i.Root) TickInspector(i, seconds);
        }

        void SpawnInspector(int route) {
            var root = ResidentModels.Create(InspectorLook, null);
            root.name = "Night inspector " + route; root.transform.position = Routes[route][0];
            var torch = new GameObject("Flashlight").AddComponent<Light>(); torch.transform.SetParent(root.transform, false);
            torch.transform.localPosition = new Vector3(.25f, 1.45f, .3f); torch.transform.localRotation = Quaternion.Euler(14, 0, 0);
            torch.type = LightType.Spot; torch.range = SightRange + 3; torch.spotAngle = SightAngle * 2; torch.intensity = 4f; torch.color = new Color(.85f, .92f, 1f);
            var i = new Inspector { Root = root, Motion = root.GetComponent<CharacterMotion>(), Torch = torch, Route = route, Leg = 1,
                                    Bubble = WorldCaption(root.transform, "", new Vector3(0, 2.45f, 0), .03f) };
            Inspectors.Add(i);
        }

        bool Sees(Inspector i, FirstPersonPlayer p) {
            var eye = i.Root.transform.position + Vector3.up * 1.6f; var head = p.transform.position + Vector3.up * 1.4f;
            var to = head - eye; float d = to.magnitude; if (d > SightRange) return false;
            var flat = new Vector3(to.x, 0, to.z);
            if (d > 3.5f && Vector3.Angle(i.Root.transform.forward, flat) > SightAngle) return false;
            if (Physics.Linecast(eye, head, out var hit, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(p.transform)) return false;
            return true;
        }

        void Walk(Inspector i, Vector3 target, float speed, float seconds) {
            var pos = i.Root.transform.position; target.y = pos.y;
            var next = Vector3.MoveTowards(pos, target, speed * seconds); var step = next - pos;
            i.Root.transform.position = next;
            if (step.sqrMagnitude > 1e-6f) i.Root.transform.rotation = Quaternion.Slerp(i.Root.transform.rotation, Quaternion.LookRotation(new Vector3(step.x, 0, step.z)), seconds * 8);
            if (i.Motion) i.Motion.Walking = step.sqrMagnitude > 1e-6f;
        }

        void TickInspector(Inspector i, float seconds) {
            var s = Game.State; if (i.Motion) i.Motion.Running = i.Mode == InspectorMode.Chase;
            switch (i.Mode) {
                case InspectorMode.Patrol: case InspectorMode.Cooldown: case InspectorMode.Suspicious: {
                    if (i.Mode == InspectorMode.Cooldown && (i.CooldownLeft -= seconds) <= 0) i.Mode = InspectorMode.Patrol;
                    FirstPersonPlayer seen = null;
                    if (i.Mode != InspectorMode.Cooldown) foreach (var p in InspectablePlayers()) if (Inspections.Carried(s, p.ActorId) > 0 && Sees(i, p)) { seen = p; break; }
                    if (seen) {
                        i.Target = seen; i.Mode = InspectorMode.Suspicious; i.Suspicion += seconds / SuspicionSeconds;
                        var look = seen.transform.position - i.Root.transform.position; look.y = 0;
                        if (look.sqrMagnitude > .01f) i.Root.transform.rotation = Quaternion.Slerp(i.Root.transform.rotation, Quaternion.LookRotation(look), seconds * 5);
                        if (i.Motion) i.Motion.Walking = false;
                        SetBubble(i.Bubble, "<color=#F2C94C>?</color>");
                        if (i.Suspicion >= 1) {
                            i.Mode = InspectorMode.Stop; i.StopTimer = 0; i.StopDistance = Vector3.Distance(i.Root.transform.position, seen.transform.position); Fx(SoundFx.Warning, 1f);
                            SetBubble(i.Bubble, "<color=#E1543B>STOP!</color>\nHealth inspection!");
                            Game.Notify("INSPECTOR: \"Stop right there! Health inspection!\"  Stand still to be searched, or run for it.", 5);
                        }
                        break;
                    }
                    i.Suspicion = Mathf.Max(0, i.Suspicion - seconds * .5f);
                    if (i.Mode == InspectorMode.Suspicious && i.Suspicion <= 0) i.Mode = InspectorMode.Patrol;
                    SetBubble(i.Bubble, "");
                    var route = Routes[i.Route]; var goal = route[i.Leg];
                    Walk(i, goal, PatrolSpeed, seconds);
                    if ((i.Root.transform.position - new Vector3(goal.x, i.Root.transform.position.y, goal.z)).sqrMagnitude < .05f) i.Leg = (i.Leg + 1) % route.Length;
                    break;
                }
                case InspectorMode.Stop: {
                    if (!i.Target) { i.Mode = InspectorMode.Patrol; break; }
                    float d = Vector3.Distance(i.Root.transform.position, i.Target.transform.position);
                    Walk(i, i.Target.transform.position, ApproachSpeed, seconds);
                    if (d < SearchRange) { FinishInspection(i, false); break; }
                    i.StopDistance = Mathf.Min(i.StopDistance, d);   // closest they've been since the call
                    if (d > i.StopDistance + FleeRange * .5f && d > 4) {
                        i.Mode = InspectorMode.Chase; i.Lost = 0; Fx(SoundFx.Horn, .9f);
                        SetBubble(i.Bubble, "<color=#E1543B>HEY! Get back here!</color>");
                        Game.Notify("You're running from a health inspector! Sprint (Shift) and break line of sight to lose them.", 5);
                    }
                    break;
                }
                case InspectorMode.Chase: {
                    if (!i.Target) { i.Mode = InspectorMode.Patrol; break; }
                    Walk(i, i.Target.transform.position, ChaseSpeed, seconds);
                    float d = Vector3.Distance(i.Root.transform.position, i.Target.transform.position);
                    if (d < CatchRange) { FinishInspection(i, true); break; }
                    i.Lost = Sees(i, i.Target) && d < SightRange * 1.6f ? 0 : i.Lost + seconds;
                    if (i.Lost >= EscapeSeconds || d > 40) {
                        i.Mode = InspectorMode.Cooldown; i.CooldownLeft = Cooldown; i.Suspicion = 0; i.Target = null;
                        SetBubble(i.Bubble, "<color=#9FB3C8>...lost them.</color>");
                        Fx(SoundFx.Huff, .8f); Game.Notify("You lost the inspector. Get that sauce into a pantry.", 5);
                    }
                    break;
                }
            }
        }

        void FinishInspection(Inspector i, bool caught) {
            var p = i.Target; if (!p) return;
            Inspections.Search(Game.State, p.ActorId, caught, out var message);
            Feedback(message);
            if (caught) { p.Teleport(StandFront + new Vector3(0, 0, -3)); Fx(SoundFx.Huff, 1f); }
            else Fx(message.StartsWith("Inspector") ? SoundFx.Clink : SoundFx.Register, .8f);
            Pop(i.Root.transform.position + Vector3.up * 2.8f, message.StartsWith("Inspector") ? "All clear." : "Confiscated!", message.StartsWith("Inspector") ? "4FCB7A" : "E1543B", .03f);
            i.Mode = InspectorMode.Cooldown; i.CooldownLeft = Cooldown; i.Suspicion = 0; i.Target = null;
            Game.Save();
        }
    }
}
