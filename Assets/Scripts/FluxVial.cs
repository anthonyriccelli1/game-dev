using UnityEngine;

namespace RestaurantCity {
    // A Flux vial: the Tripo canister (Resources/Flux/FluxVial.prefab, built by the SetupFlux editor command) with a
    // glowing green liquid column in a glass tube. This adds the life: a soft green light, a slow pulse, rising bubbles,
    // and a gentle bob and turn so it catches the eye at night.
    public sealed class FluxVial : MonoBehaviour {
        public const float ModelHeight = .977f;
        static readonly Color Green = new Color(.12f, .95f, .3f);
        Light glow; Material liquid; Transform[] bubbles; float[] bubbleSpeed; Vector3 rest; float phase;
        public bool Bob = true;

        public static GameObject Create(Transform parent, Vector3 position, float height = .32f) {
            var prefab = Resources.Load<GameObject>("Flux/FluxVial"); if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false); go.name = "Flux vial";
            go.transform.localPosition = position; go.transform.localScale = Vector3.one * (height / ModelHeight);
            go.AddComponent<FluxVial>(); return go;
        }

        void Start() {
            rest = transform.localPosition; phase = Random.value * 6.28f;
            var l = transform.Find("Flux liquid");
            if (l) liquid = l.GetComponent<Renderer>().material;   // own instance so each vial pulses on its own
            glow = new GameObject("Flux glow").AddComponent<Light>(); glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0, 1.7f, 0); glow.type = LightType.Point; glow.color = Green;   // above the cap: inside, it blew the glass out to white
            glow.range = 3f; glow.intensity = 1.1f; glow.shadows = LightShadows.None;
            var bubbleMat = FoodLooks.Mat(new Color(.75f, 1f, .8f), 2.2f);
            bubbles = new Transform[7]; bubbleSpeed = new float[bubbles.Length];
            for (int i = 0; i < bubbles.Length; i++) {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.name = "Flux bubble";
                b.transform.SetParent(transform, false); float a = i * 2.4f, r = .03f + (i % 3) * .03f;
                b.transform.localPosition = new Vector3(Mathf.Cos(a) * r, .29f + i * .06f, Mathf.Sin(a) * r);
                b.transform.localScale = Vector3.one * (.03f + (i % 3) * .012f);
                b.GetComponent<Renderer>().sharedMaterial = bubbleMat; b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bubbles[i] = b.transform; bubbleSpeed[i] = .07f + (i % 4) * .025f;
            }
        }

        void Update() {
            float t = Time.time + phase, k = 1 + .28f * Mathf.Sin(t * 2.1f);
            if (liquid) liquid.SetColor("_EmissionColor", Green * 1.35f * k);
            if (glow) glow.intensity = 1.1f * k;
            if (Bob) {
                transform.localPosition = rest + Vector3.up * Mathf.Sin(t * 1.3f) * .02f;
                transform.Rotate(0, 25 * Time.deltaTime, 0, Space.Self);
            }
            if (bubbles != null)
                for (int i = 0; i < bubbles.Length; i++) {
                    var p = bubbles[i].localPosition; p.y += bubbleSpeed[i] * Time.deltaTime;
                    if (p.y > .69f) p.y = .29f;
                    bubbles[i].localPosition = p;
                }
        }
    }
}
