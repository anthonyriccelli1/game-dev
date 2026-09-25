using UnityEngine;

namespace RestaurantCity {
    public class StreetGuard : MonoBehaviour {
        public CityGame Game;
        public Transform Body;
        public Renderer Coat;
        public bool Defeated { get; private set; }
        public bool WindingUp => windup > 0;
        public int HitsRemaining { get; private set; } = 3;
        Vector3 home;
        float windup, cooldown, stagger;
        bool lastNight;
        void Awake() { home = transform.position; }
        void Update() {
            bool night = Game.State.IsNight;
            if (night && !lastNight && Game.State.LastStashDay != Game.State.Day) { Defeated = false; HitsRemaining = 3; transform.position = home; }
            lastNight = night;
            bool active = night && !Defeated && Game.State.LastStashDay != Game.State.Day;
            Body.gameObject.SetActive(active);
            if (!active || Game.Paused) return;
            cooldown -= Time.deltaTime; stagger -= Time.deltaTime;
            var target = Game.Player;
            if(Game.CoOp)foreach(var candidate in Game.CoOp.Players)if(Vector3.Distance(candidate.transform.position,transform.position)<Vector3.Distance(target.transform.position,transform.position))target=candidate;
            Vector3 player = target.transform.position;
            bool inAlley = EncounterRules.InTerritory(player.x, player.z);
            Vector3 destination = inAlley ? player : home; destination.y = transform.position.y;
            float distance = Vector3.Distance(transform.position, destination);
            if (windup > 0) {
                windup -= Time.deltaTime;
                if (windup <= 0) {
                    Vector3 rayStart = transform.position + Vector3.up;
                    Vector3 rayEnd = target.transform.position + Vector3.up;
                    bool clear = Physics.Linecast(rayStart, rayEnd, out var obstruction, ~0, QueryTriggerInteraction.Ignore)
                        && obstruction.collider.GetComponent<FirstPersonPlayer>() == target;
                    if (inAlley && distance < 2.5f && clear) Game.HurtPlayer(target,25);
                    cooldown = 1.3f;
                }
            } else if (stagger <= 0) {
                if (distance > 1.8f) transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * 2.2f);
                else if (inAlley && cooldown <= 0) { windup = .9f; Game.Notify("Rival winding up! Step back or swing.", 1.2f); }
            }
            if (distance > .1f) transform.rotation = Quaternion.LookRotation(destination - transform.position) * Quaternion.Euler(0, 180, 0);
            if (Coat) Coat.material.color = WindingUp ? new Color(1, .28f, .15f) : new Color(.28f, .18f, .36f);
        }
        public void Hit() {
            if (!Game.State.IsNight || Defeated || Game.State.LastStashDay == Game.State.Day) return;
            HitsRemaining--; windup = 0; cooldown = .8f; stagger = .35f;
            Game.Notify(HitsRemaining > 0 ? "Rival staggered  /  " + HitsRemaining + " hits left" : "Rival defeated. The recipe stash is yours.");
            if (HitsRemaining <= 0) { Defeated = true; Body.gameObject.SetActive(false); Game.State.GainReputation(Game.State.BeatAlleyRival ? Reputation.RematchRival : Reputation.BeatRival); Game.State.BeatAlleyRival = true; }
        }
        public void ResetGuard() { Defeated = false; HitsRemaining = 3; windup = 0; cooldown = 2; transform.position = home; }
    }
}
