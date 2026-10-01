using UnityEngine;
using UnityEngine.InputSystem;
namespace RestaurantCity {
    // First-person fighting for each player: the hotbar weapon (or bare fists) drawn in view, quick jabs,
    // held-and-released heavy hits, a block (a well-timed block parries and staggers), and hits that land with
    // a hit-pause, camera kick, knockback, sound and a damage number. Health regenerates out of combat.
    public class PlayerCombat : MonoBehaviour {
        public FirstPersonPlayer Player;
        public bool Blocking { get; private set; }
        public float HurtFlash { get; private set; }     // 0-1, drawn as a red edge by the HUD
        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStart) / HeavyTime) : 0;
        public const float HeavyTime = .75f, TapTime = .22f, RegenDelay = 5, RegenRate = 9, ParryWindow = .25f;

        Transform rig, fistL, fistR, weaponHolder; GameObject weaponModel; string shownWeapon = "?";
        float cooldown, chargeStart, blockStart = -9, lastHurt = -99, shake, bob, swingT = -1; bool charging, heavySwing; int side;
        AudioSource sound;
        Vector3 restL = new Vector3(-.27f, -.21f, .58f), restR = new Vector3(.28f, -.2f, .6f);
        static readonly Quaternion RestRotL = Quaternion.Euler(-18, 14, 8), RestRotR = Quaternion.Euler(-18, -14, -8);

        GameState State => Player.Game.State;
        public static PlayerCombat Of(FirstPersonPlayer p) { if (!p) return null; var c = p.GetComponent<PlayerCombat>(); if (!c) { c = p.gameObject.AddComponent<PlayerCombat>(); c.Player = p; } return c; }
        public float Health { get => Player.Health; set { Player.Health = Mathf.Clamp(value, 0, 100); if (Player.PlayerId == 0) State.Health = Mathf.RoundToInt(Player.Health); } }

        void Build() {
            if (rig || !Player || !Player.View) return;
            Player.View.nearClipPlane = .05f;
            rig = new GameObject("Viewmodel").transform; rig.SetParent(Player.View.transform, false);
            var skin = Mat(new Color(.85f, .64f, .5f)); var sleeve = Mat(Player.PlayerId == 0 ? new Color(.78f, .3f, .22f) : new Color(.2f, .45f, .78f));
            fistL = Arm("Left", skin, sleeve); fistR = Arm("Right", skin, sleeve);
            weaponHolder = new GameObject("Weapon").transform; weaponHolder.SetParent(fistR, false);
            int layer = 25 + Player.PlayerId; foreach (var t in rig.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 0; sound.playOnAwake = false;
        }
        Transform Arm(string name, Material skin, Material sleeve) {
            var fist = new GameObject("Fist " + name).transform; fist.SetParent(rig, false);
            // A clenched fist: palm block, four knuckle ridges, a thumb across the front, then the wrist and a sleeve
            // running back and down toward the screen corner.
            float s = name == "Left" ? 1 : -1;
            Cube("Palm", fist, Vector3.zero, new Vector3(.082f, .072f, .075f), skin);
            for (int i = 0; i < 4; i++) Cube("Knuckle", fist, new Vector3(-.03f + i * .02f, .022f, .04f), new Vector3(.019f, .03f, .03f), skin);
            Cube("Fingers", fist, new Vector3(0, -.008f, .044f), new Vector3(.08f, .036f, .026f), skin);
            Cube("Thumb", fist, new Vector3(s * .012f, -.03f, .038f), new Vector3(.05f, .02f, .024f), skin);
            Cube("Wrist", fist, new Vector3(0, -.004f, -.06f), new Vector3(.06f, .058f, .06f), skin);
            Cube("Cuff", fist, new Vector3(0, -.004f, -.1f), new Vector3(.078f, .078f, .035f), Mat(new Color(.95f, .92f, .85f)));
            Cube("Sleeve", fist, new Vector3(0, -.004f, -.27f), new Vector3(.088f, .088f, .32f), sleeve);
            return fist;
        }
        static GameObject Cube(string n, Transform p, Vector3 at, Vector3 size, Material m) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = n; Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(p, false); go.transform.localPosition = at; go.transform.localScale = size; var r = go.GetComponent<Renderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return go;
        }
        static Material Mat(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = c; m.SetFloat("_Smoothness", .25f); return m; }

        // Weapon models: the frying pan comes from the shop pack when available; the bat and knuckles are built here.
        void ShowWeapon(string id) {
            if (id == shownWeapon) return; shownWeapon = id;
            if (weaponModel) Destroy(weaponModel); weaponModel = null;
            if (id == "fists") return;
            weaponModel = new GameObject("Weapon " + id); var t = weaponModel.transform; t.SetParent(weaponHolder, false);
            if (id == "bat") {
                var wood = Mat(new Color(.78f, .55f, .3f)); var tape = Mat(new Color(.12f, .12f, .14f));
                for (int i = 0; i < 6; i++) { float k = i / 5f; var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(seg.GetComponent<Collider>()); seg.transform.SetParent(t, false);
                    seg.transform.localPosition = new Vector3(0, .04f + k * .62f, 0); float r = Mathf.Lerp(.035f, .075f, k * k); seg.transform.localScale = new Vector3(r, .065f, r); seg.GetComponent<Renderer>().sharedMaterial = i == 0 ? tape : wood; }
                t.localRotation = Quaternion.Euler(18, 0, -22); t.localScale = Vector3.one * .85f;   // up and back over the right shoulder
            } else if (id == "pan") {
                var art = Resources.Load<GameObject>("ArtOverrides/Weapons/pan");
                if (art) { var go = Instantiate(art, t, false); go.transform.localPosition = new Vector3(0, .08f, .05f); go.transform.localRotation = Quaternion.Euler(-80, 0, 0); foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c); }
                else { var iron = Mat(new Color(.16f, .16f, .18f)); Cube("Handle", t, new Vector3(0, .12f, 0), new Vector3(.035f, .22f, .03f), iron);
                    var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(disc.GetComponent<Collider>()); disc.transform.SetParent(t, false); disc.transform.localPosition = new Vector3(0, .36f, 0); disc.transform.localRotation = Quaternion.Euler(90, 0, 0); disc.transform.localScale = new Vector3(.28f, .02f, .28f); disc.GetComponent<Renderer>().sharedMaterial = iron; }
                t.localRotation = Quaternion.Euler(10, 0, -18); t.localScale = Vector3.one * .85f;
            } else if (id == "knuckles") {
                var brass = Mat(new Color(.9f, .72f, .3f)); brass.SetFloat("_Metallic", .8f);
                foreach (var f in new[] { fistR, fistL }) { var bar = Cube("Brass", f == fistR ? t : f, new Vector3(0, .02f, .058f), new Vector3(.09f, .03f, .018f), brass); if (f == fistL) bar.name = "Brass L"; }
            }
            int layer = 25 + Player.PlayerId; foreach (var tr in rig.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = layer;
            if (id != "knuckles") { var extra = fistL.Find("Brass L"); if (extra) Destroy(extra.gameObject); }
        }

        // Called from FirstPersonPlayer.Update with the same input gating as movement.
        public void Tick(Keyboard keys, Mouse mouse, Gamepad pad) {
            Build(); if (!rig) return;
            var inv = Hotbar.For(State, Player.PlayerId);
            // Hotbar: number keys, scroll wheel, D-pad left/right.
            int pick = -1;
            if (keys != null) for (int i = 0; i < PlayerInventory.Size; i++) if (keys[Key.Digit1 + i].wasPressedThisFrame) pick = i;
            if (mouse != null) { float s = mouse.scroll.ReadValue().y; if (s > .1f) pick = inv.Selected - 1; else if (s < -.1f) pick = inv.Selected + 1; }
            if (pad != null) { if (pad.dpad.right.wasPressedThisFrame) pick = inv.Selected + 1; if (pad.dpad.left.wasPressedThisFrame) pick = inv.Selected - 1; }
            if (pick != -1 && !Hotbar.Select(State, Player.PlayerId, pick, out var why)) Player.Game.Notify(why, 2);
            Hotbar.Sync(State, Player.PlayerId);

            bool handsBusy = State.Kitchen != null && State.Kitchen.Hold(Player.ActorId) != null;   // food, a plate, or the selected bag
            bool blockHeld = !handsBusy && (mouse != null && mouse.rightButton.isPressed || pad != null && pad.leftTrigger.ReadValue() > .5f);
            if (blockHeld && !Blocking) blockStart = Time.time; Blocking = blockHeld;
            bool press = mouse != null && mouse.leftButton.wasPressedThisFrame || pad != null && pad.rightShoulder.wasPressedThisFrame;
            bool held = mouse != null && mouse.leftButton.isPressed || pad != null && pad.rightShoulder.isPressed;
            if (press) {
                if (Player.Game.Restaurant && Player.Game.Restaurant.TryFlipStation(Player)) { }   // left click still flips patties at the grill
                else if (!handsBusy && !Blocking && cooldown <= 0) { charging = true; chargeStart = Time.time; }
            }
            else if (mouse != null && mouse.delta.ReadValue().y > 28 && Player.Game.Restaurant) Player.Game.Restaurant.TryFlickFlip(Player);
            if (charging && (!held || Time.time - chargeStart > HeavyTime + .25f)) {
                float t = Time.time - chargeStart; charging = false;
                Attack(t >= TapTime, Mathf.Clamp01((t - TapTime) / (HeavyTime - TapTime)));
            }
        }

        // A light jab (for tests and taps): true when it hit something.
        public bool LightAttack() { Build(); return cooldown <= 0 && Attack(false, 0); }

        bool Attack(bool heavy, float power) {
            var w = Hotbar.For(State, Player.PlayerId).Weapon;
            cooldown = w.Cooldown * (heavy ? 1.35f : 1); swingT = 0; heavySwing = heavy; side = w.Id == "fists" || w.Id == "knuckles" ? 1 - side : 1;
            float damage = w.Damage * (heavy ? Mathf.Lerp(1.4f, w.HeavyMultiplier, power) : 1), knock = w.Knockback * (heavy ? 2.2f : 1);
            Play(Whoosh, .5f);
            var ray = Player.InteractionRay;
            var hits = Physics.SphereCastAll(ray.origin, heavy ? .42f : .32f, ray.direction, w.Reach + (heavy ? .25f : 0), ~Player.OwnBodyMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits) {
                var fighter = h.collider.GetComponentInParent<RaidFighter>();
                if (fighter && !fighter.Down && !fighter.Ours) {
                    fighter.TakePlayerHit(Player, damage, Flat(fighter.transform.position - Player.transform.position).normalized * knock, heavy);
                    Impact(h.point, damage, heavy, w); return true;
                }
                var guard = h.collider.GetComponentInParent<StreetGuard>();
                if (guard) { guard.Hit(); Impact(h.point, damage, heavy, w); return true; }
                if (!h.collider.isTrigger && h.distance > .1f && !h.collider.GetComponentInParent<FirstPersonPlayer>()) break;   // a wall in the way
            }
            return false;
        }
        void Impact(Vector3 at, float damage, bool heavy, WeaponDef w) {
            shake = Mathf.Max(shake, heavy ? .09f : .045f);
            Play(w.Id == "pan" ? Clang : w.Id == "bat" ? Crack : Punch, heavy ? 1 : .8f);
            StartCoroutine(HitStop(heavy ? .085f : .045f));
            DamageNumber(at, damage, heavy);
        }
        System.Collections.IEnumerator HitStop(float seconds) {
            if (Time.timeScale < .5f) yield break;
            float before = Time.timeScale; Time.timeScale = .06f;
            yield return new WaitForSecondsRealtime(seconds);
            if (Time.timeScale < .5f) Time.timeScale = before;
        }
        void DamageNumber(Vector3 at, float damage, bool heavy) {
            var go = new GameObject("Damage number"); go.transform.position = at + Vector3.up * .3f;
            var tm = go.AddComponent<TextMesh>(); tm.text = Mathf.RoundToInt(damage) + (heavy ? "!" : ""); tm.characterSize = heavy ? .05f : .036f; tm.fontSize = 64; tm.anchor = TextAnchor.MiddleCenter;
            tm.color = heavy ? new Color(1f, .45f, .2f) : new Color(1f, .92f, .6f); go.layer = 0;
            go.AddComponent<FloatAway>();
        }

        // Incoming damage passes through here: blocking in front soaks most of it; a quick block parries.
        public float Incoming(float amount, Vector3? from, out bool parried) {
            parried = false;
            if (Blocking && from.HasValue && Vector3.Dot(Player.transform.forward, Flat(from.Value - Player.transform.position).normalized) > .2f) {
                if (Time.time - blockStart < ParryWindow) { parried = true; Play(Clang, .8f); shake = .03f; Player.Game.Notify("PARRY!", .8f); return 0; }
                amount *= .3f; Play(Block, .8f); shake = Mathf.Max(shake, .03f);
            }
            if (amount > 0) { lastHurt = Time.time; HurtFlash = Mathf.Clamp01(.35f + amount / 40f); shake = Mathf.Max(shake, .06f); Play(Grunt, .7f); }
            return amount;
        }

        void Update() {
            if (!Player || !Player.Game) return;
            Build(); if (!rig) return;
            cooldown -= Time.deltaTime; HurtFlash = Mathf.MoveTowards(HurtFlash, 0, Time.deltaTime * 1.6f);
            if (Time.time - lastHurt > RegenDelay && Health > 0 && Health < 100) Health += RegenRate * Time.deltaTime;
            var inv = Hotbar.For(State, Player.PlayerId);
            bool handsBusy = State.Kitchen != null && State.Kitchen.Hold(Player.ActorId) != null;
            var r = Player.Game.Restaurant;
            var scrub = Player.GetComponent<PlateScrub>();
            bool visible = !Player.Elevated && !handsBusy && !(scrub && scrub.HandsBusy) && !(r && (r.PanelOpen || r.PlacementActive)) && Player.Game.Started;
            if (rig.gameObject.activeSelf != visible) rig.gameObject.SetActive(visible);
            ShowWeapon(inv.Weapon.Id);
            // Pose: idle bob, block guard, wind-up while charging, then the strike.
            bob += Time.deltaTime * (Player.Sprinting ? 11 : 6); float b = Mathf.Sin(bob) * .006f;
            Vector3 l = restL + Vector3.up * b, rr = restR + Vector3.up * -b; Quaternion rotL = RestRotL, rotR = RestRotR;
            bool swinging = inv.Weapon.Id == "bat" || inv.Weapon.Id == "pan";
            if (Blocking) { l = new Vector3(-.1f, -.07f, .42f); rr = new Vector3(.1f, -.06f, .43f); rotL = Quaternion.Euler(-30, 20, 10); rotR = Quaternion.Euler(-30, -20, -10); }
            else if (charging) { float c = Charge01; if (swinging) { rr = Vector3.Lerp(restR, new Vector3(.34f, -.02f, .28f), c); rotR = Quaternion.Euler(-40 * c, 0, -50 * c); } else { rr = Vector3.Lerp(restR, new Vector3(.25f, -.18f, .3f), c); rotR = Quaternion.Euler(0, 0, -25 * c); } rr += Random.insideUnitSphere * .004f * c; }
            if (swingT >= 0) {
                swingT += Time.deltaTime / (heavySwing ? .3f : .2f); float k = swingT < .35f ? swingT / .35f : 1 - (swingT - .35f) / .65f; k = Mathf.Clamp01(k);
                if (swinging) { rr = Vector3.Lerp(new Vector3(.34f, -.02f, .3f), new Vector3(-.2f, -.3f, .55f), Mathf.Clamp01(swingT * 1.5f)); rotR = Quaternion.Euler(Mathf.Lerp(-40, 60, Mathf.Clamp01(swingT * 1.5f)), 0, Mathf.Lerp(-50, 60, Mathf.Clamp01(swingT * 1.5f))); }
                else if (side == 1) { rr = Vector3.Lerp(rr, new Vector3(.04f, -.1f, heavySwing ? .95f : .88f), k); rotR = Quaternion.Slerp(rotR, Quaternion.identity, k); } else { l = Vector3.Lerp(l, new Vector3(-.04f, -.1f, heavySwing ? .95f : .88f), k); rotL = Quaternion.Slerp(rotL, Quaternion.identity, k); }
                if (swingT >= 1) swingT = -1;
            }
            fistL.localPosition = Vector3.Lerp(fistL.localPosition, l, Time.deltaTime * 22); fistR.localPosition = Vector3.Lerp(fistR.localPosition, rr, Time.deltaTime * 22);
            fistL.localRotation = Quaternion.Slerp(fistL.localRotation, rotL, Time.deltaTime * 18); fistR.localRotation = Quaternion.Slerp(fistR.localRotation, rotR, Time.deltaTime * 18);
            // Camera kick.
            if (shake > 0) { rig.localPosition = Random.insideUnitSphere * shake * .6f; shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * .5f); } else rig.localPosition = Vector3.zero;
        }
        void LateUpdate() { if (shake > .001f && Player && Player.View && !Player.Elevated) Player.View.transform.localRotation *= Quaternion.Euler(Random.Range(-1f, 1f) * shake * 30, Random.Range(-1f, 1f) * shake * 30, 0); }

        void Play(AudioClip clip, float v) { if (sound && clip) sound.PlayOneShot(clip, v); }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        static AudioClip Punch => SoundFx.Get("punch", .16f, t => (Random.value * 2 - 1) * Mathf.Exp(-t * 45) * .5f + Mathf.Sin(2 * Mathf.PI * (110 - t * 200) * t) * Mathf.Exp(-t * 22) * .7f);
        static AudioClip Clang => SoundFx.Get("clang", .5f, t => (Mathf.Sin(2 * Mathf.PI * 820 * t) + .7f * Mathf.Sin(2 * Mathf.PI * 1310 * t) + .4f * Mathf.Sin(2 * Mathf.PI * 2150 * t)) * Mathf.Exp(-t * 9) * .22f);
        static AudioClip Crack => SoundFx.Get("crack", .18f, t => (Random.value * 2 - 1) * Mathf.Exp(-t * 60) * .7f + Mathf.Sin(2 * Mathf.PI * 180 * t) * Mathf.Exp(-t * 30) * .5f);
        static AudioClip Whoosh => SoundFx.Get("whoosh", .2f, t => (Random.value * 2 - 1) * Mathf.Sin(Mathf.PI * t / .2f) * .14f);
        static AudioClip Block => SoundFx.Get("block", .14f, t => Mathf.Sin(2 * Mathf.PI * 160 * t) * Mathf.Exp(-t * 35) * .6f);
        static AudioClip Grunt => SoundFx.Get("grunt", .28f, t => (Mathf.Sin(2 * Mathf.PI * (150 - t * 120) * t) * .5f + (Random.value * 2 - 1) * .15f) * Mathf.Exp(-t * 10));
    }
    // Floating damage numbers rise, face the camera and fade.
    public class FloatAway : MonoBehaviour {
        float t; TextMesh tm;
        void Start() { tm = GetComponent<TextMesh>(); }
        void Update() {
            t += Time.unscaledDeltaTime; transform.position += Vector3.up * Time.unscaledDeltaTime * .9f;
            if (Camera.main) transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            if (tm) { var c = tm.color; c.a = 1 - t / .8f; tm.color = c; }
            if (t > .8f) Destroy(gameObject);
        }
    }
}
