using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace RestaurantCity {
    // Plays shared Mixamo clips (Resources/Mixamo) on a humanoid resident. No AnimatorController asset: a small
    // playable graph crossfades between looping states and layers one-shot reactions on top.
    // Missing clips fall back gracefully (sitting falls back to a procedural leg pose), so a checkout without
    // the Mixamo files still runs.
    public class ResidentAnimator : MonoBehaviour {
        public enum State { Idle, Walk, Sit, Eat, Work }
        public State Current;
        public Gait Gait;
        Animator animator; PlayableGraph graph; AnimationMixerPlayable mixer;
        readonly float[] weights = new float[6];
        AnimationClip[] loops; bool hasSit;
        float oneShotUntil, oneShotLength;
        const int OneShotInput = 5;

        static readonly Dictionary<string, AnimationClip> cache = new Dictionary<string, AnimationClip>();
        public static AnimationClip Clip(string name) {
            if (cache.TryGetValue(name, out var c)) return c;
            foreach (var clip in Resources.LoadAll<AnimationClip>("Mixamo/" + name)) if (!clip.name.StartsWith("__preview__")) { c = clip; break; }
            cache[name] = c; return c;
        }
        public static bool Available => Clip("Idle") != null;

        public void Init(Animator a, Gait gait) {
            animator = a; Gait = gait; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            string idle = gait == Gait.Zombie ? "ZombieIdle" : gait == Gait.Light ? "IdleB" : "Idle";
            string walk = gait == Gait.Zombie ? "ZombieWalk" : gait == Gait.Light ? "WalkB" : "Walk";
            var idleClip = Clip(idle) ?? Clip("Idle"); var sit = Clip("Sit"); hasSit = sit;
            loops = new[] { idleClip, Clip(walk) ?? Clip("Walk") ?? idleClip, sit ?? idleClip, Clip("Eat") ?? sit ?? idleClip, Clip("Work") ?? idleClip };
            graph = PlayableGraph.Create(name + " anim"); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 6);
            float offset = Random.value * 3;   // so a crowd doesn't move in lockstep
            for (int i = 0; i < loops.Length; i++) {
                if (!loops[i]) continue;
                var p = AnimationClipPlayable.Create(graph, loops[i]); p.SetTime(offset); graph.Connect(p, 0, mixer, i);
            }
            AnimationPlayableOutput.Create(graph, "Resident", animator).SetSourcePlayable(mixer);
            weights[0] = 1; Apply(); graph.Play();
        }
        // Poses the model immediately (for portraits taken outside the normal frame loop).
        public void Pose(float seconds) { if (graph.IsValid()) graph.Evaluate(seconds); }
        public void React(bool happy) {
            var clip = happy ? (Clip("Cheer") ?? Clip("Happy")) : Clip("Angry");
            if (!clip || !graph.IsValid()) return;
            var old = mixer.GetInput(OneShotInput); if (old.IsValid()) { graph.Disconnect(mixer, OneShotInput); old.Destroy(); }
            var p = AnimationClipPlayable.Create(graph, clip); graph.Connect(p, 0, mixer, OneShotInput);
            oneShotLength = Mathf.Min(clip.length, 2.2f); oneShotUntil = Time.time + oneShotLength;
        }
        void Update() {
            if (!graph.IsValid()) return;
            float dt = Time.deltaTime * 6;
            for (int i = 0; i < 5; i++) weights[i] = Mathf.MoveTowards(weights[i], i == (int)Current ? 1 : 0, dt);
            bool shot = Time.time < oneShotUntil && Current != State.Sit && Current != State.Eat;
            weights[OneShotInput] = Mathf.MoveTowards(weights[OneShotInput], shot ? 1 : 0, dt);
            Apply();
        }
        void Apply() {
            float loopTotal = 0; for (int i = 0; i < 5; i++) loopTotal += weights[i];
            float scale = (1 - weights[OneShotInput]) / Mathf.Max(.0001f, loopTotal);
            for (int i = 0; i < 5; i++) mixer.SetInputWeight(i, weights[i] * scale);
            mixer.SetInputWeight(OneShotInput, weights[OneShotInput]);
        }
        // Without a sitting clip, fold the legs into a chair pose after the animation has posed the body.
        void LateUpdate() {
            if (hasSit || !animator || (Current != State.Sit && Current != State.Eat)) return;
            var fwd = transform.forward; fwd.y = 0; fwd.Normalize();
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips); var thigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var kneeL = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            if (hips && thigh && kneeL) hips.position += Vector3.down * Vector3.Distance(thigh.position, kneeL.position) * .95f;
            foreach (var side in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot), (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot) }) {
                var hip = animator.GetBoneTransform(side.Item1); var knee = animator.GetBoneTransform(side.Item2); var foot = animator.GetBoneTransform(side.Item3);
                if (!hip || !knee || !foot) continue;
                hip.rotation = Quaternion.FromToRotation(knee.position - hip.position, fwd) * hip.rotation;
                knee.rotation = Quaternion.FromToRotation(foot.position - knee.position, Vector3.down) * knee.rotation;
            }
        }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
