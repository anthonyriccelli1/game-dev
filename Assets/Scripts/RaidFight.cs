using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
    // One fighter in a raid: your crew, the rival's fry cooks, or the boss. Players hit them with PlayerCombat.
    public class RaidFighter : MonoBehaviour {
        public RaidBattle Battle; public bool Ours, Boss; public string Name; public WorkerState Worker; public RaidFighter Opponent;
        public float Hp, MaxHp, Damage, Interval, Speed, Armor = 1, Cool, Stagger, Windup, PoiseReady, Dash, DashCool = 3; public bool Enraged, ComboNext;
        public Vector3 Knock; public CharacterMotion Motion; public TextMesh Caption; public Transform Bar, BarFill;
        public List<(Material m, Color c)> Tints = new List<(Material, Color)>(); public float Flash;
        public bool Down => Hp <= 0;
        public void TakePlayerHit(FirstPersonPlayer player, float damage, Vector3 knock, bool heavy) { if (Battle) Battle.PlayerHit(this, player, damage, knock, heavy); }
    }

    // A live raid at the rival's place. The rival brings one fry cook per crew member you bring and they pair off
    // one-on-one. You always fight the boss. If one of your crew goes down, their fry cook comes for you too.
    // If you go down, the raid is lost on the spot. Walking away forfeits it.
    public class RaidBattle : MonoBehaviour {
        public RestaurantController Owner; public RivalDef Rival; public bool Over, Won; public RaidLoot Loot; public int CashLost;
        public bool BossPassive;   // tests and screenshots: the boss and freed fry cooks leave the players alone
        public readonly List<RaidFighter> Fighters = new List<RaidFighter>();
        public Vector3 Center; float endTimer = -1; Transform root;
        public const float LeaveRadius = 26, GoonWindup = .45f, BossWindup = .7f;
        static readonly Color CrewColor = new Color(.45f, .95f, .7f), GoonColor = new Color(1f, .75f, .35f), BossColor = new Color(1f, .4f, .25f);

        public RaidFighter BossFighter => Fighters.FirstOrDefault(f => f.Boss);
        public void Begin(RestaurantController owner, RivalDef rival, List<WorkerState> crew) {
            Owner = owner; Rival = rival; Center = new Vector3(rival.X, 0, rival.Z - 5f);
            root = new GameObject("Raid / " + rival.Name).transform;
            RaidRules.Begin(owner.Game.State, rival);
            var lamp = new GameObject("Raid work light").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.position = Center + Vector3.up * 7; lamp.type = LightType.Point; lamp.range = 22; lamp.intensity = 5f; lamp.color = new Color(1f, .86f, .62f); lamp.shadows = LightShadows.None;
            // Gus steps out beside his serving hatch.
            var boss = Spawn(rival.BossModel, rival.Boss, ResidentCast.CustomResidentHeight, new Vector3(rival.X - 2.6f, 0, rival.Z - 1.5f), 180, false, true);
            boss.Hp = boss.MaxHp = rival.BossHealth; boss.Damage = rival.BossDamage; boss.Interval = 1.15f; boss.Speed = 2.9f;
            bool rally = crew.Any(w => StaffStats.For(w.Id).Perk == Perk.Rally);
            for (int i = 0; i < crew.Count; i++) {
                float x = rival.X + (i - (crew.Count - 1) / 2f) * 3f;
                var rf = rival.Roster[i % rival.Roster.Length];
                var g = Spawn(rf.Model, rf.Name, ResidentCast.CustomResidentHeight, new Vector3(x, 0, rival.Z - 5.5f), 180, false, false);
                Stats(g, rf.Stats);
                var w = crew[i]; var s = StaffStats.For(w.Id); var def = ResidentCast.ForWorker(w.Id);
                var f = Spawn(def.Model ?? def.Id, RestaurantCatalog.Worker(w.Id)?.Name ?? def.Name, def.Height, new Vector3(x, 0, rival.Z - 11f), 0, true, false);
                f.Worker = w; Stats(f, s); if (rally) f.Damage *= 1.15f; if (s.Perk == Perk.Tough) f.Armor = .7f;
                f.Opponent = g; g.Opponent = f;
            }
            owner.Feedback(crew.Count == 0 ? "RAID! Just you and " + rival.Boss + ". Hold click for a heavy hit, right click to block." :
                "RAID! " + crew.Count + " vs " + crew.Count + ": your crew take his imps, you take " + rival.Boss + ". Hold click to wind up, right click to block.");
        }
        static void Stats(RaidFighter f, ResidentStats s) {
            f.Hp = f.MaxHp = StaffStats.RaidHealth(s); f.Damage = StaffStats.RaidDamage(s); f.Interval = StaffStats.RaidInterval(s); f.Speed = 2.2f * (.7f + .1f * s.Speed);
        }
        RaidFighter Spawn(string model, string name, float height, Vector3 at, float yaw, bool ours, bool boss) {
            GameObject go;
            if (People.UseResidents) go = ResidentModels.Create(new ResidentDef(model, name, height, 0, StaffJob.Cook, ""), root);
            else go = RestaurantArt.CreateCharacter(ours ? 1 : boss ? 5 : 3, root);
            go.name = (ours ? "Raid crew / " : boss ? "Raid boss / " : "Raid goon / ") + name;
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var cap = go.AddComponent<CapsuleCollider>(); cap.radius = .42f; cap.height = height; cap.center = Vector3.up * height / 2;
            var f = go.AddComponent<RaidFighter>(); f.Battle = this; f.Ours = ours; f.Boss = boss; f.Name = name; f.Motion = go.GetComponent<CharacterMotion>();
            foreach (var r in go.GetComponentsInChildren<Renderer>()) foreach (var m in r.materials) if (m.HasProperty("_BaseColor")) f.Tints.Add((m, m.color));
            // Name and a health bar over their head.
            f.Caption = Owner.WorldCaption(go.transform, name, new Vector3(0, height + .62f, 0), boss ? .026f : .02f);
            f.Caption.color = ours ? CrewColor : boss ? BossColor : GoonColor;
            f.Bar = new GameObject("Health bar").transform; f.Bar.SetParent(go.transform, false); f.Bar.localPosition = new Vector3(0, height + .38f, 0);
            float w = boss ? 1.3f : .9f;
            Quad(f.Bar, "Back", new Vector3(w + .06f, .14f, 1), new Color(.08f, .08f, .1f), 0);
            f.BarFill = Quad(f.Bar, "Fill", new Vector3(w, .09f, 1), ours ? CrewColor : boss ? BossColor : GoonColor, -.01f).transform;
            Fighters.Add(f); return f;
        }
        static GameObject Quad(Transform parent, string name, Vector3 size, Color c, float z) {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = name; Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false); q.transform.localPosition = new Vector3(0, 0, z); q.transform.localScale = size;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = c; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * .9f);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return q;
        }
        // Gus's fry cooks all wear his colours: a paper hat and a mustard apron with a red stripe.
        static void FryCookUniform(RaidFighter f) {
            var anim = f.GetComponentInChildren<Animator>();
            Transform head = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null, chest = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine) : null;
            var paper = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.97f, .96f, .92f) };
            var apron = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.91f, .7f, .23f) };
            var stripe = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.78f, .25f, .18f) };
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(hat.GetComponent<Collider>()); hat.name = "Paper hat";
            hat.transform.SetParent(head ? head : f.transform, false); hat.transform.localPosition = head ? new Vector3(0, .2f, 0) : new Vector3(0, 1.85f, 0);
            hat.transform.localScale = head ? new Vector3(.24f, .1f, .3f) / Mathf.Max(.01f, head.lossyScale.x) : new Vector3(.24f, .1f, .3f); hat.GetComponent<Renderer>().sharedMaterial = paper;
            var bib = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(bib.GetComponent<Collider>()); bib.name = "Apron";
            bib.transform.SetParent(f.transform, false); bib.transform.localPosition = new Vector3(0, .95f, .2f); bib.transform.localScale = new Vector3(.42f, .7f, .03f); bib.GetComponent<Renderer>().sharedMaterial = apron;
            var band = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(band.GetComponent<Collider>()); band.name = "Apron stripe";
            band.transform.SetParent(bib.transform, false); band.transform.localPosition = new Vector3(0, .2f, -.6f); band.transform.localScale = new Vector3(1.01f, .12f, 1); band.GetComponent<Renderer>().sharedMaterial = stripe;
            if (chest) { bib.transform.SetParent(chest, true); }
        }

        IEnumerable<FirstPersonPlayer> Players() {
            var game = Owner.Game;
            if (game.CoOp != null && game.CoOp.PlayerCount > 0) { foreach (var p in game.CoOp.Players) if (p) yield return p; }
            else if (game.Player) yield return game.Player;
        }
        void Update() { if (!Owner || Owner.Game.Paused) return; Step(Time.deltaTime); }

        public void Step(float dt) {
            if (Over) { if (endTimer > 0 && (endTimer -= dt) <= 0) Cleanup(); return; }
            var players = Players().ToList();
            if (!BossPassive && players.All(p => Flat(p.transform.position - Center).magnitude > LeaveRadius)) { Finish(false, false); return; }
            foreach (var f in Fighters) {
                f.Flash = Mathf.MoveTowards(f.Flash, 0, dt * 6);
                foreach (var (m, c) in f.Tints) if (m) m.color = Color.Lerp(c, f.Windup > 0 ? new Color(1f, .9f, .3f) : Color.white, f.Windup > 0 ? .35f : f.Flash);
                if (f.Knock.sqrMagnitude > .0001f) { f.transform.position += f.Knock * Mathf.Min(1, dt * 9); f.Knock *= Mathf.Max(0, 1 - dt * 9); }
                if (f.Down) { Bars(f); continue; }
                f.Cool -= dt; f.Stagger -= dt;
                if (f.Boss) BossStep(f, players, dt); else FighterStep(f, players, dt);
                Bars(f);
            }
            var bossF = BossFighter;
            if (bossF && bossF.Down) Finish(true, false);
        }
        void FighterStep(RaidFighter f, List<FirstPersonPlayer> players, float dt) {
            // Paired fights first. A crew member whose fry cook is down helps a teammate; a fry cook whose
            // opponent is down goes after the nearest player.
            RaidFighter foe = f.Opponent && !f.Opponent.Down ? f.Opponent : Fighters.Where(o => !o.Down && !o.Boss && o.Ours != f.Ours && (f.Ours || !o.Ours)).OrderBy(o => (o.transform.position - f.transform.position).sqrMagnitude).FirstOrDefault();
            FirstPersonPlayer victim = null;
            if (!f.Ours && (!f.Opponent || f.Opponent.Down)) { foe = null; if (!BossPassive) victim = players.Where(p => Flat(p.transform.position - Center).magnitude < 18).OrderBy(p => (p.transform.position - f.transform.position).sqrMagnitude).FirstOrDefault(); }
            Transform target = foe ? foe.transform : victim ? victim.transform : null;
            if (!target) { Idle(f); return; }
            if (victim) { Telegraphed(f, victim, dt, GoonWindup, f.Damage * .6f, 1.5f); return; }
            Engage(f, target, dt, () => { if (f.Motion) f.Motion.Punch(); Hit(foe, f.Damage); });
        }
        // Gus: shrugs off light hits (only a heavy hit staggers him, and then not again for a few seconds), charges you
        // when you back off, sometimes follows a headbutt with a quick jab, and gets faster when badly hurt.
        void BossStep(RaidFighter b, List<FirstPersonPlayer> players, float dt) {
            if (BossPassive) { Idle(b); return; }
            var victim = players.Where(p => Flat(p.transform.position - Center).magnitude < 18).OrderBy(p => (p.transform.position - b.transform.position).sqrMagnitude).FirstOrDefault();
            if (!victim) { Idle(b); return; }
            if (!b.Enraged && b.Hp < b.MaxHp * .4f) { b.Enraged = true; b.Interval *= .7f; b.Speed *= 1.3f; Owner.Game.Notify(Rival.Boss + " is ENRAGED! Faster hits, keep your guard up.", 2.5f); }
            var to = Flat(victim.transform.position - b.transform.position); b.DashCool -= dt;
            if (b.Dash > 0) {
                b.Dash -= dt; if (to.sqrMagnitude > .01f) b.transform.rotation = Quaternion.LookRotation(to);
                b.transform.position += to.normalized * Mathf.Min(to.magnitude - 1.2f, 8f * dt); if (b.Motion) { b.Motion.Walking = true; b.Motion.Running = true; }
                if (to.magnitude < 1.7f) { b.Dash = 0; b.Cool = 0; b.ComboNext = true; }   // arrives swinging: a quick hit
                return;
            }
            if (b.Motion) b.Motion.Running = false;
            if (b.Windup <= 0 && b.Stagger <= 0 && b.DashCool <= 0 && to.magnitude > 4 && to.magnitude < 13) { b.Dash = .6f; b.DashCool = b.Enraged ? 3.5f : 5.5f; Owner.Game.Notify(Rival.Boss + " charges!", 1.2f); return; }
            bool quick = b.ComboNext;
            if (Telegraphed(b, victim, dt, quick ? .3f : b.Enraged ? .45f : BossWindup, quick ? b.Damage * .55f : b.Damage, 1.9f)) {
                if (quick) b.ComboNext = false;
                else if (Random.value < (b.Enraged ? .6f : .4f)) { b.ComboNext = true; b.Cool = .25f; }
            }
        }
        // Anyone attacking a player winds up first (they glow yellow): step back, block, or hit them to interrupt.
        // Returns true on the frame the blow resolves.
        bool Telegraphed(RaidFighter f, FirstPersonPlayer victim, float dt, float windup, float damage, float reach) {
            var to = Flat(victim.transform.position - f.transform.position);
            if (f.Windup > 0) {
                if (f.Stagger > 0) { f.Windup = 0; return false; }
                f.transform.rotation = Quaternion.Slerp(f.transform.rotation, Quaternion.LookRotation(to), dt * 4);
                f.Windup -= dt;
                if (f.Windup <= 0) {
                    f.Cool = f.Interval;
                    if (to.magnitude < reach + .7f) {
                        bool parried = Owner.Game.HurtPlayer(victim, damage, f.transform.position);
                        if (parried) { f.Stagger = 1.3f; f.ComboNext = false; if (f.Motion) f.Motion.Flinch(); }
                    }
                    return true;
                }
                return false;
            }
            Engage(f, victim.transform, dt, () => { f.Windup = windup; if (f.Motion) { if (f.Boss && windup > .35f) f.Motion.Headbutt(); else f.Motion.Punch(); } }, reach);
            return false;
        }
        void Idle(RaidFighter f) { if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = !Over; } }
        void Engage(RaidFighter f, Transform target, float dt, System.Action strike, float reach = 1.35f) {
            var to = Flat(target.position - f.transform.position);
            if (to.sqrMagnitude > .01f) f.transform.rotation = Quaternion.Slerp(f.transform.rotation, Quaternion.LookRotation(to), dt * 10);
            if (f.Stagger > 0) { if (f.Motion) f.Motion.Walking = false; return; }
            if (to.magnitude > reach) { f.transform.position += to.normalized * Mathf.Min(to.magnitude - reach * .9f, f.Speed * dt); if (f.Motion) { f.Motion.Walking = true; f.Motion.Working = false; f.Motion.Fighting = false; } return; }
            if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = true; }
            if (f.Cool <= 0) { f.Cool = f.Interval; strike(); }
        }
        public void Hit(RaidFighter target, float damage) {
            if (Over || target.Down) return;
            target.Hp = Mathf.Max(0, target.Hp - damage * target.Armor); target.Stagger = .25f; target.Flash = .6f;
            if (target.Down) KnockOut(target); else if (target.Motion) target.Motion.Flinch();
        }
        public void PlayerHit(RaidFighter target, FirstPersonPlayer player, float damage, Vector3 knock, bool heavy) {
            if (Over || target.Down || target.Ours) return;
            target.Hp = Mathf.Max(0, target.Hp - damage); target.Flash = 1; target.Knock += knock * (target.Boss ? .12f : .35f);
            if (target.Boss) {
                // Poise: light hits don't stop him. A heavy hit staggers and interrupts, then he's unstaggerable for a moment.
                if (heavy && Time.time >= target.PoiseReady) { target.Stagger = .55f; target.Windup = 0; target.Dash = 0; target.ComboNext = false; target.PoiseReady = Time.time + 2.5f; if (target.Motion && !target.Down) target.Motion.Flinch(); }
                if (target.Down) { KnockOut(target); Owner.Game.Notify(Rival.Boss + " is down!", 2); }
                return;
            }
            // Fry cooks: a jab only flinches them; a heavy hit knocks them off their swing.
            target.Stagger = Mathf.Max(target.Stagger, heavy ? .7f : .15f); if (target.Windup > 0 && heavy) target.Windup = 0;
            if (target.Down) KnockOut(target); else if (target.Motion) target.Motion.Flinch();
            if (target.Boss && target.Down) Owner.Game.Notify(Rival.Boss + " is down!", 2);
        }
        public void PlayerDowned(FirstPersonPlayer player) { Finish(false, true); }
        void KnockOut(RaidFighter f) {
            if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = false; }
            f.Windup = 0;
            if (!(f.Motion && f.Motion.KnockOut())) { f.transform.rotation = Quaternion.Euler(-90, f.transform.eulerAngles.y, 0); f.transform.position += Vector3.up * .25f; }
            foreach (var c in f.GetComponents<Collider>()) c.enabled = false;
        }
        void Bars(RaidFighter f) {
            if (!f.Bar) return;
            f.Bar.gameObject.SetActive(!f.Down); if (f.Caption) f.Caption.text = f.Down ? f.Name + "\nKO" : f.Windup > 0 ? "!! " + f.Name + " !!" : f.Name;
            float k = f.MaxHp > 0 ? Mathf.Clamp01(f.Hp / f.MaxHp) : 0;
            f.BarFill.localScale = new Vector3(Mathf.Max(.001f, k) * (f.Boss ? 1.3f : .9f), .09f, 1); f.BarFill.localPosition = new Vector3(-(1 - k) * (f.Boss ? 1.3f : .9f) / 2, 0, -.01f);
            var cam = Camera.main; if (cam) { f.Bar.rotation = Quaternion.LookRotation(f.Bar.position - cam.transform.position); if (f.Caption) f.Caption.transform.rotation = f.Bar.rotation; }
        }
        void Finish(bool won, bool downed) {
            if (Over) return; Over = true; Won = won; endTimer = won ? 4 : 1.5f;
            var g = Owner.Game.State; var crew = Fighters.Where(x => x.Ours).ToList();
            foreach (var f in Fighters) { f.Windup = 0; foreach (var (m, c) in f.Tints) if (m) m.color = c; }
            if (won) {
                foreach (var f in crew) RaidRules.CrewAfter(f.Worker, f.MaxHp > 0 ? f.Hp / f.MaxHp : 0, f.Down);
                Loot = RaidRules.Win(g, Rival, Time.frameCount + g.Day * 31);
                foreach (var f in Fighters.Where(x => !x.Down && !x.Boss && !x.Ours)) f.gameObject.SetActive(false);   // the fry cooks run off
                foreach (var f in crew.Where(x => !x.Down)) if (f.Motion) f.Motion.Cheer();
                Owner.Feedback(Loot.Message); Owner.Game.Notify(Loot.Message, 10);
            } else if (downed) {
                CashLost = RaidRules.Lose(g, crew.Select(x => x.Worker));
                // You wake up at your restaurant (or your stand) with half your health.
                var wake = g.Restaurant.Owned ? RestaurantController.W(-10, .15f, -10.7f) : Owner.Game.SpawnPoint;
                int n = 0; foreach (var p in Players()) { PlayerCombat.Of(p).Health = 50; p.Teleport(wake + Vector3.right * 1.4f * n++); }
                Owner.Game.Notify("KNOCKED OUT. You wake up back home. " + Rival.Boss + "'s crew took $" + CashLost + " and your crew is spent (0 energy).", 10);
                endTimer = .2f;
            } else {
                foreach (var f in crew) RaidRules.CrewAfter(f.Worker, f.MaxHp > 0 ? f.Hp / f.MaxHp : 0, f.Down);
                Owner.Game.Notify("You walked away. " + Rival.Boss + " keeps his recipe tonight and your crew limps home tired.", 8);
            }
            Owner.Game.Save();
        }
        public void Cleanup() { if (root) Destroy(root.gameObject); if (Owner && Owner.ActiveRaid == this) Owner.ActiveRaid = null; Destroy(this); }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }

    public partial class RestaurantController {
        public RaidBattle ActiveRaid;
        public string RaidRivalId = "gus";
        public readonly List<string> RaidCrew = new List<string>();
        public void OpenRaid(string rivalId) {
            RaidRivalId = rivalId;
            RaidCrew.RemoveAll(id => !RaidRules.CanFight(Data.Workers.Find(w => w.Id == id)));
            ShowPanel("Raid");
        }
        public void ToggleRaidCrew(string id) {
            if (RaidCrew.Remove(id)) { UI.Refresh(); return; }
            var w = Data.Workers.Find(x => x.Id == id);
            if (!RaidRules.CanFight(w)) { Feedback((RestaurantCatalog.Worker(id)?.Name ?? id) + " is too tired (or on the stand) to fight."); return; }
            if (RaidCrew.Count >= RaidRules.MaxCrew) { Feedback("A raid crew is at most " + RaidRules.MaxCrew + "."); return; }
            RaidCrew.Add(id); UI.Refresh();
        }
        public bool StartRaid(out string message) {
            var rival = Rivals.Get(RaidRivalId); message = "";
            if (ActiveRaid) { message = "A raid is already under way."; return false; }
            if (!RaidRules.CanRaid(Game.State, rival, ServiceInProgress, out message)) { Feedback(message); return false; }
            var crew = RaidCrew.Select(id => Data.Workers.Find(w => w.Id == id)).Where(RaidRules.CanFight).ToList();
            ClosePanel();
            ActiveRaid = new GameObject("Raid battle").AddComponent<RaidBattle>();
            ActiveRaid.Begin(this, rival, crew);
            RaidCrew.Clear(); return true;
        }
        // Hock-9's pawn counter: weapons for cash. Weapons go into the buyer's hotbar.
        public int PawnBuyer;
        bool InspectPawn(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (city.Kind != InteractionKind.Pawn) return false;
            prompts[p.ActorId] = "Hock-9's pawn counter\nE / A  Browse weapons";
            if (pressed) { PawnBuyer = p.PlayerId; ShowPanel("Pawn"); }
            return true;
        }
        public bool BuyWeapon(string id, out string message) {
            var w = System.Array.Find(Weapons.Shop, x => x.Id == id); var st = Game.State; message = "";
            if (w == null) return false;
            var inv = Hotbar.For(st, PawnBuyer);
            if (inv.Has(id)) { message = "You already carry a " + w.Name.ToLower() + "."; Feedback(message); return false; }
            if (st.Cash < w.Price) { message = "The " + w.Name.ToLower() + " is $" + w.Price + "."; Feedback(message); return false; }
            if (!Hotbar.Give(st, PawnBuyer, id, out message)) { Feedback(message); return false; }
            st.Cash -= w.Price; PlayChime(true); Game.Save();
            message = "Bought the " + w.Name.ToLower() + ". It's in your hotbar (" + (inv.Slots.FindIndex(x => x.Item == id) + 1) + ")."; Feedback(message); UI.Refresh(); return true;
        }
        // The rival's place in the city: its truck/door opens the raid panel. Also stands the pawnbroker at his counter.
        void AttachRaidTargets() {
            if (!GameObject.Find("Hock-9")) {
                var def = new ResidentDef("051_Polybot", "Hock-9", 1.8f, 0, StaffJob.Any, "Pawnbroker");
                var npc = People.UseResidents ? ResidentModels.Create(def, transform) : RestaurantArt.CreateCharacter(6, transform);
                npc.name = "Hock-9"; npc.transform.SetPositionAndRotation(new Vector3(30.3f, 0, -25), Quaternion.Euler(0, 90, 0));
                var cap = WorldCaption(npc.transform, "HOCK-9\nPawnbroker", new Vector3(0, 2.3f, 0), .018f); cap.transform.rotation = Quaternion.Euler(0, 90, 0);
            }
            foreach (var rival in Rivals.All) {
                var spot = GameObject.Find(rival.Name); if (!spot || spot.GetComponent<Interactable>()) continue;
                var box = spot.AddComponent<BoxCollider>(); box.center = new Vector3(rival.X, 1.2f, rival.Z) - spot.transform.position; box.size = new Vector3(2.6f, 2.4f, 5.4f);
                var it = spot.AddComponent<Interactable>(); it.Kind = InteractionKind.Raid; it.Site = rival.Id;
            }
        }
        bool InspectRaid(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (city.Kind != InteractionKind.Raid) return false;
            var rival = Rivals.Get(city.Site); if (rival == null) return false;
            // Raids are a restaurant-owner's game: on the truck, Gus is just the competition across the street.
            if (!Data.Owned) { prompts[p.ActorId] = rival.Name + "  " + StarText.Of(rival.Stars) + "\nThe competition. Out-serve him from Little Flame."; return true; }
            bool ok = RaidRules.CanRaid(Game.State, rival, ServiceInProgress, out var why);
            prompts[p.ActorId] = rival.Name + "  " + StarText.Of(rival.Stars) + "\n" + (ActiveRaid ? "Raid under way!" : ok ? "E / A  Plan a raid" : why);
            if (pressed && !ActiveRaid) OpenRaid(rival.Id);
            return true;
        }
    }
}
