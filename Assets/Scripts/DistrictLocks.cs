using System.Collections.Generic;
using UnityEngine;
namespace RestaurantCity {
    // Keeps players out of districts their reputation rank hasn't opened, hides the roadblocks once it has, and
    // announces rank-ups. Locks are position based, so sidewalks and lots are covered as well as roads.
    public class DistrictLocks : MonoBehaviour {
        public static bool Suspended;   // snapshot tours and tests may move the camera anywhere
        public CityGame Game;
        readonly Dictionary<FirstPersonPlayer, Vector3> safe = new Dictionary<FirstPersonPlayer, Vector3>();
        float nextNotice; int shownRank = -1;
        void Start() { Refresh(); }
        void Refresh() {
            if (Game == null || Game.State == null) return;
            int rank = Reputation.Rank(Game.State.Xp); shownRank = rank;
            var root = GameObject.Find("District locks"); if (!root) return;
            foreach (var d in CityDistricts.All) { var t = root.transform.Find("Lock_" + d.Id); if (t) t.gameObject.SetActive(rank < d.Rank); }
        }
        IEnumerable<FirstPersonPlayer> Players() {
            if (LocalCoop.Instance != null && LocalCoop.Instance.PlayerCount > 0) { foreach (var p in LocalCoop.Instance.Players) if (p) yield return p; }
            else if (Game.Player) yield return Game.Player;
        }
        void Update() {
            if (Game == null || Game.State == null) return;
            if (Game.State.RankUpTo >= 0) {
                int r = Game.State.RankUpTo; Game.State.RankUpTo = -1; var opened = CityDistricts.OpenedAt(r);
                Game.Notify("RANK UP!  You're a " + Reputation.Titles[r].ToUpper() + ".  " + (opened != null ? opened.Name + " is now open. Check the Map (M)." : ""), 10);
                Refresh();
            } else if (Reputation.Rank(Game.State.Xp) != shownRank) Refresh();
            if (Suspended) return;
            foreach (var player in Players()) {
                var p = player.transform.position; var d = CityDistricts.At(p.x, p.z);
                if (d != null && !CityDistricts.Unlocked(d, Game.State.Xp)) {
                    if (safe.TryGetValue(player, out var back)) player.Teleport(back);
                    if (Time.unscaledTime > nextNotice) {
                        nextNotice = Time.unscaledTime + 3;
                        Game.Notify(d.Name.ToUpper() + " is closed to you.  Reach " + Reputation.Titles[d.Rank].ToUpper() + " (" + Game.State.Xp + "/" + Reputation.Thresholds[d.Rank] + " reputation). Earn it by serving customers.", 3);
                    }
                } else safe[player] = p;
            }
        }
    }
}
