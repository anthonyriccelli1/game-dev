using UnityEngine;

namespace RestaurantCity {
    /// <summary>Presentation only: movement/state are supplied by the restaurant simulation.</summary>
    public class CharacterMotion : MonoBehaviour {
        public bool Walking, Seated, Working;
        Transform body, head, leftArm, rightArm, leftLeg, rightLeg, leftKnee, rightKnee, mouth;
        Vector3 bodyOrigin, headOrigin;
        float mood = .7f, phase;
        static int nextPhase;
        void Awake() {
            body = transform.Find("BodyRig");
            if (!body) return;
            head = body.Find("HeadRig"); leftArm = body.Find("ArmL"); rightArm = body.Find("ArmR");
            leftLeg = body.Find("LegL"); rightLeg = body.Find("LegR");
            if (leftLeg) leftKnee = leftLeg.Find("Knee");
            if (rightLeg) rightKnee = rightLeg.Find("Knee");
            bodyOrigin = body.localPosition;
            if (head) { headOrigin = head.localPosition; mouth = head.Find("Mouth"); }
            phase = (nextPhase++ % 31) * .7f;
        }
        public void SetMood(float score) { mood = Mathf.Clamp01(score > 1 ? score / 100f : score); }
        void Update() {
            if (!body) return;
            float t = Time.time * (Walking ? 8f : Working ? 5f : 1.9f) + phase;
            float swing = Walking ? Mathf.Sin(t) * 26 : Working ? Mathf.Sin(t) * 18 : Mathf.Sin(t) * 3;
            body.localPosition = bodyOrigin + Vector3.up * (Seated ? -.19f : Mathf.Abs(Mathf.Sin(t)) * (Walking ? .045f : .012f));
            body.localRotation = Quaternion.Euler(0, 0, Walking ? Mathf.Sin(t) * 2 : 0);
            if (leftLeg) leftLeg.localRotation = Quaternion.Euler(Seated ? -82 : swing, 0, 0);
            if (rightLeg) rightLeg.localRotation = Quaternion.Euler(Seated ? -82 : -swing, 0, 0);
            if (leftKnee) leftKnee.localRotation = Quaternion.Euler(Seated ? 82 : Walking ? Mathf.Max(0, -swing) : 0, 0, 0);
            if (rightKnee) rightKnee.localRotation = Quaternion.Euler(Seated ? 82 : Walking ? Mathf.Max(0, swing) : 0, 0, 0);
            if (leftArm) leftArm.localRotation = Quaternion.Euler(Working ? -50 + swing : Seated ? -35 : -swing, 0, mood < .35f ? -12 : 5);
            if (rightArm) rightArm.localRotation = Quaternion.Euler(Working ? -65 - swing : Seated ? -35 : swing, 0, mood < .35f ? 12 : -5);
            if (head) {
                head.localPosition = headOrigin;
                head.localRotation = Quaternion.Euler(mood < .35f ? 9 : Mathf.Sin(t * .4f) * 2, Mathf.Sin(t * .23f) * 7, mood > .8f ? -4 : 0);
            }
            if (mouth) mouth.localScale = new Vector3(mood < .35f ? .65f : 1, mood > .7f ? 1.35f : .35f, 1);
        }
    }
}
