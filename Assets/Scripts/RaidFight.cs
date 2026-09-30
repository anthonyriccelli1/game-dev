using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RestaurantCity {
    // One fighter in a raid: your crew, the rival's workers, or the boss. The player's spatula can hit any of them.
    public class RaidFighter : MonoBehaviour {
        public RaidBattle Battle; public bool Ours, Boss; public string Name; public WorkerState Worker;
        public float Hp, MaxHp, Damage, Interval, Speed, Armor = 1, Cool, Stagger, Windup;
        public CharacterMotion Motion; public TextMesh Caption; public bool Down => Hp <= 0;
        public void PlayerHit(FirstPersonPlayer player) { if (Battle) Battle.PlayerHit(this, player); }
    }

    // A live raid at the rival's place. The picked crew brawl the rival's workers on their own; the player fights the
    // boss (who only goes after players). Knock the boss down to win. Walk away, or get knocked out, and the raid is lost.
    public class RaidBattle : MonoBehaviour {
        public RestaurantController Owner; public RivalDef Rival; public bool Over, Won; public RaidLoot Loot;
        public bool BossPassive;   // tests: the boss stands still
        public readonly List<RaidFighter> Fighters = new List<RaidFighter>();
        public Vector3 Center; float endTimer = -1; Transform root;
        public const float PlayerDamage = 22, LeaveRadius = 26;

        public RaidFighter BossFighter => Fighters.FirstOrDefault(f => f.Boss);
        public void Begin(RestaurantController owner, RivalDef rival, List<WorkerState> crew) {
            Owner = owner; Rival = rival; Center = new Vector3(rival.X, 0, rival.Z - 4.3f);
            root = new GameObject("Raid / " + rival.Name).transform;
            RaidRules.Begin(owner.Game.State, rival);
            // A harsh work light over the lot so the brawl reads at night.
            var lamp = new GameObject("Raid work light").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.position = Center + Vector3.up * 7; lamp.type = LightType.Point; lamp.range = 20; lamp.intensity = 4.5f; lamp.color = new Color(1f, .86f, .62f); lamp.shadows = LightShadows.None;
            var boss = Spawn(rival.BossModel, rival.Boss, 1.95f, new Vector3(rival.X, 0, rival.Z - 3.4f), 180, false, true);
            boss.Hp = boss.MaxHp = rival.BossHits; boss.Damage = rival.BossDamage; boss.Interval = 1.4f; boss.Speed = 2.3f;
            for (int i = 0; i < rival.GoonModels.Length; i++) {
                var g = Spawn(rival.GoonModels[i], "Fry cook", 1.7f, new Vector3(rival.X + (i == 0 ? -3.2f : 3.2f), 0, rival.Z - 4.2f), 180, false, false);
                Stats(g, rival.GoonStats);
            }
            bool rally = crew.Any(w => StaffStats.For(w.Id).Perk == Perk.Rally);
            for (int i = 0; i < crew.Count; i++) {
                var w = crew[i]; var s = StaffStats.For(w.Id);
                var def = ResidentCast.ForWorker(w.Id);
                var f = Spawn(def.Id, RestaurantCatalog.Worker(w.Id)?.Name ?? def.Name, def.Height, new Vector3(rival.X - 3 + i * 3, 0, rival.Z - 10.5f), 0, true, false);
                f.Worker = w; Stats(f, s); if (rally) f.Damage *= 1.15f; if (s.Perk == Perk.Tough) f.Armor = .7f;
            }
            owner.Feedback("RAID! Your crew takes Gus's fry cooks. You take " + rival.Boss + ": " + rival.BossHits + " spatula hits (left click / RB). Walk away to give up.");
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
            var cap = go.AddComponent<CapsuleCollider>(); cap.radius = .38f; cap.height = height; cap.center = Vector3.up * height / 2;
            var f = go.AddComponent<RaidFighter>(); f.Battle = this; f.Ours = ours; f.Boss = boss; f.Name = name; f.Motion = go.GetComponent<CharacterMotion>();
            f.Caption = Owner.WorldCaption(go.transform, "", new Vector3(0, height + .45f, 0), .02f);
            Fighters.Add(f); return f;
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
            // Everyone walked away (or was knocked out and sent home): the raid is lost.
            if (!BossPassive && players.All(p => Flat(p.transform.position - Center).magnitude > LeaveRadius)) { Finish(false); return; }
            foreach (var f in Fighters) {
                if (f.Down) { Label(f); continue; }
                f.Cool -= dt; f.Stagger -= dt;
                if (f.Boss) { BossStep(f, players, dt); Label(f); continue; }
                // Crew fight goons; goons fight crew, and once your crew is down they come for you.
                var foe = Fighters.Where(o => !o.Down && !o.Boss && o.Ours != f.Ours).OrderBy(o => (o.transform.position - f.transform.position).sqrMagnitude).FirstOrDefault();
                Transform target = foe ? foe.transform : null; FirstPersonPlayer victim = null;
                if (!foe && !f.Ours && !BossPassive) { victim = players.OrderBy(p => (p.transform.position - f.transform.position).sqrMagnitude).FirstOrDefault(); if (victim && Flat(victim.transform.position - Center).magnitude < 16) target = victim.transform; }
                if (!target) { if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = false; } Label(f); continue; }
                Engage(f, target, dt, () => { if (foe) Hit(foe, f.Damage, f.Name); else if (victim) Owner.Game.HurtPlayer(victim, Mathf.RoundToInt(f.Damage * .5f)); });
                Label(f);
            }
            var bossF = BossFighter;
            if (bossF && bossF.Down) Finish(true);
        }
        void BossStep(RaidFighter b, List<FirstPersonPlayer> players, float dt) {
            if (BossPassive || b.Stagger > 0) { if (b.Motion) b.Motion.Walking = false; return; }
            var victim = players.Where(p => Flat(p.transform.position - Center).magnitude < 16).OrderBy(p => (p.transform.position - b.transform.position).sqrMagnitude).FirstOrDefault();
            if (!victim) { if (b.Motion) b.Motion.Walking = false; return; }
            var to = Flat(victim.transform.position - b.transform.position);
            if (b.Windup > 0) {
                b.Windup -= dt;
                if (b.Windup <= 0) { if (to.magnitude < 2.5f) Owner.Game.HurtPlayer(victim, Mathf.RoundToInt(b.Damage)); b.Cool = b.Interval; }
                return;
            }
            Engage(b, victim.transform, dt, () => { b.Windup = .85f; if (b.Motion) b.Motion.Headbutt(); Owner.Game.Notify(Rival.Boss + " is winding up! Step back or hit him first.", 1.1f); }, 1.8f);
        }
        void Engage(RaidFighter f, Transform target, float dt, System.Action strike, float reach = 1.35f) {
            var to = Flat(target.position - f.transform.position);
            if (to.sqrMagnitude > .01f) f.transform.rotation = Quaternion.LookRotation(to);
            if (f.Stagger > 0) { if (f.Motion) f.Motion.Walking = false; return; }
            if (to.magnitude > reach) { f.transform.position += to.normalized * Mathf.Min(to.magnitude - reach * .9f, f.Speed * dt); if (f.Motion) { f.Motion.Walking = true; f.Motion.Working = false; f.Motion.Fighting = false; } return; }
            if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = true; }
            if (f.Cool <= 0) { f.Cool = f.Interval; if (f.Motion && !f.Boss) f.Motion.Punch(); strike(); }
        }
        public void Hit(RaidFighter target, float damage, string from) {
            if (Over || target.Down) return;
            target.Hp = Mathf.Max(0, target.Hp - damage * target.Armor); target.Stagger = .25f;
            if (target.Down) KnockOut(target); else if (target.Motion) target.Motion.Flinch();
        }
        public void PlayerHit(RaidFighter target, FirstPersonPlayer player) {
            if (Over || target.Down) return;
            if (target.Ours) { Owner.Game.Notify("That's your crew!", 1.2f); return; }
            if (target.Boss) {
                target.Hp -= 1; target.Windup = 0; target.Stagger = .45f; target.Cool = .7f; if (target.Hp > 0 && target.Motion) target.Motion.Flinch();
                Owner.Game.Notify(target.Hp > 0 ? Rival.Boss + " staggered  /  " + (int)target.Hp + " hits left" : Rival.Boss + " is down!", 1.6f);
                if (target.Down) KnockOut(target);
            } else Hit(target, PlayerDamage, "you");
        }
        void KnockOut(RaidFighter f) {
            if (f.Motion) { f.Motion.Walking = false; f.Motion.Working = false; f.Motion.Fighting = false; }
            // With the knockout clip they fall on their own; without it, tip them over.
            if (!(f.Motion && f.Motion.KnockOut())) { f.transform.rotation = Quaternion.Euler(-90, f.transform.eulerAngles.y, 0); f.transform.position += Vector3.up * .25f; }
            foreach (var c in f.GetComponents<Collider>()) c.enabled = false;
        }
        void Label(RaidFighter f) {
            if (!f.Caption) return;
            if (f.Down) { f.Caption.text = f.Name + "\nKO"; f.Caption.color = new Color(.6f, .6f, .6f); return; }
            int n = f.Boss ? Mathf.CeilToInt(f.Hp) : Mathf.CeilToInt(f.Hp / f.MaxHp * 8);
            int max = f.Boss ? Mathf.RoundToInt(f.MaxHp) : 8;
            f.Caption.text = (f.Boss && f.Windup > 0 ? "WINDING UP!\n" : "") + f.Name + "\n" + new string('|', Mathf.Max(0, n)) + new string('.', Mathf.Max(0, max - n));
            f.Caption.color = f.Ours ? new Color(.45f, .95f, .75f) : f.Boss ? new Color(1f, .55f, .3f) : new Color(1f, .8f, .5f);
            if (f.Caption.transform.parent) f.Caption.transform.rotation = Quaternion.Euler(0, Camera.main ? Camera.main.transform.eulerAngles.y : 0, 0);
        }
        void Finish(bool won) {
            if (Over) return; Over = true; Won = won; endTimer = 4;
            var g = Owner.Game.State;
            foreach (var f in Fighters.Where(x => x.Ours)) RaidRules.CrewAfter(f.Worker, f.MaxHp > 0 ? f.Hp / f.MaxHp : 0, f.Down);
            if (won) {
                Loot = RaidRules.Win(g, Rival, Time.frameCount + g.Day * 31);
                foreach (var f in Fighters.Where(x => !x.Down && !x.Boss && !x.Ours)) f.gameObject.SetActive(false);   // the fry cooks run off
                foreach (var f in Fighters.Where(x => x.Ours && !x.Down)) if (f.Motion) f.Motion.Cheer();
                Owner.Feedback(Loot.Message);
                Owner.Game.Notify(Loot.Message, 10);
            } else {
                Owner.Game.Notify("Raid lost. " + Rival.Boss + " keeps his recipe tonight and your crew limps home tired. Try again tomorrow night.", 8);
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
        // The rival's place in the city: its truck/door opens the raid panel.
        void AttachRaidTargets() {
            foreach (var rival in Rivals.All) {
                var spot = GameObject.Find(rival.Name); if (!spot || spot.GetComponent<Interactable>()) continue;
                var box = spot.AddComponent<BoxCollider>(); box.center = new Vector3(rival.X, 1.2f, rival.Z) - spot.transform.position; box.size = new Vector3(2.6f, 2.4f, 5.4f);
                var it = spot.AddComponent<Interactable>(); it.Kind = InteractionKind.Raid; it.Site = rival.Id;
            }
        }
        bool InspectRaid(FirstPersonPlayer p, Interactable city, bool pressed) {
            if (city.Kind != InteractionKind.Raid) return false;
            var rival = Rivals.Get(city.Site); if (rival == null) return false;
            bool ok = RaidRules.CanRaid(Game.State, rival, ServiceInProgress, out var why);
            prompts[p.ActorId] = rival.Name + "  " + StarText.Of(rival.Stars) + "\n" + (ActiveRaid ? "Raid under way!" : ok ? "E / A  Plan a raid" : why);
            if (pressed && !ActiveRaid) OpenRaid(rival.Id);
            return true;
        }
    }
}
