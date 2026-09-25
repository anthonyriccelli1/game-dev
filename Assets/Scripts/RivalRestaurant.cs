using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // "The Gilded Orbit": the rival four-star restaurant across the street. Pure showcase: a glimpse of
    // what the player's place could become. Its elite staff never leave the building and can't be hired.
    public partial class RestaurantController {
        Transform rivalRoot;
        CharacterMotion sommelier;
        float sommelierT;

        void BuildRivalInterior() {
            var world = Game.Stand ? Game.Stand.transform.parent : null;
            rivalRoot = world ? world.Find("Rival restaurant / The Gilded Orbit") : null;
            if (!rivalRoot || rivalRoot.Find("Interior")) return;
            var inside = new GameObject("Interior").transform; inside.SetParent(rivalRoot, false);

            // Kitchen line along the back wall, facing the dining room.
            Place("oven", 15.4f, 22.6f, 180, inside); Place("grill", 17.5f, 22.6f, 180, inside); Place("grill", 19.6f, 22.6f, 180, inside);
            Place("stove", 21.2f, 22.6f, 180, inside); Place("fridge", 22.2f, 22.6f, 180, inside); Place("sink", 23.2f, 22.6f, 180, inside);
            var pass = Place("assembly", 18.5f, 21.0f, 180, inside); Place("assembly", 20.6f, 21.0f, 180, inside);
            // Plated burgers waiting on the pass.
            for (int i = 0; i < 4; i++) {
                var plate = KitchenArt.CreateItem("Plate", new List<string> { "bun", "cooked_patty", i % 2 == 0 ? "midnight_sauce" : "bun" }, inside);
                plate.transform.position = new Vector3(17.9f + i * .95f, 1.1f, 21.0f);
            }
            // Dining room.
            Place("rug_sunset", 19f, 18.6f, 0, inside); var table = Place("communal_table", 19f, 18.6f, 0, inside);
            var boothL = Place("booth_coral", 15.3f, 18.9f, 90, inside); var boothR = Place("booth_teal", 22.7f, 18.9f, 270, inside);
            Place("fern", 14.7f, 17.1f, 0, inside); Place("fern", 23.3f, 17.1f, 0, inside);
            Place("globe_lamp", 14.7f, 20.3f, 0, inside); Place("globe_lamp", 23.3f, 20.3f, 0, inside);
            Place("jukebox", 23.3f, 21.2f, 270, inside);

            // Happy regulars at every seat.
            int[] diners = { 1, 3, 5, 7, 4, 6, 0, 2, 9, 8 }; int d = 0;
            foreach (var furniture in new[] { table, boothL, boothR }) {
                if (!furniture) continue;
                for (int s = 0; s < 6; s++) {
                    var seat = furniture.transform.Find("Seat_" + s); if (!seat) continue;
                    var guest = RestaurantArt.CreateCharacter(diners[d++ % diners.Length], inside);
                    guest.transform.SetPositionAndRotation(seat.position, seat.rotation);
                    var m = guest.GetComponent<CharacterMotion>(); if (m) { m.Seated = true; m.SetMood(.95f); }
                }
            }
            // Elite staff.
            Elite(0, new Vector3(18.5f, 0, 21.8f), 0, true, inside, "MAESTRO VEY\nHead chef");
            Elite(2, new Vector3(15.4f, 0, 21.7f), 0, true, inside, "K-9\nLine cook");
            Elite(2, new Vector3(19.9f, 0, 21.7f), 0, true, inside, null);
            sommelier = Elite(1, new Vector3(17, 0, 20.1f), 90, false, inside, "NYX\nSommelier");
            Elite(3, new Vector3(19, 0, 17.3f), 180, false, inside, "AURORA\nMaitre d'");

            // Warm, expensive light that glows through the glass at night.
            foreach (var x in new[] { 16f, 19f, 22f }) AddRivalLight(new Vector3(x, 3.9f, 19.8f), new Color(1f, .78f, .5f), 3.2f, 7.5f, inside);
            AddRivalLight(new Vector3(19, 5.2f, 15.4f), new Color(1f, .75f, .35f), 4f, 6f, inside);
        }

        GameObject Place(string id, float x, float z, float rotation, Transform parent) {
            var item = RestaurantCatalog.Find(id); if (item == null) return null;
            var obj = CreateFurnishing(id, parent);
            obj.transform.SetPositionAndRotation(new Vector3(x, .08f, z), Quaternion.Euler(0, rotation, 0));
            foreach (var target in obj.GetComponentsInChildren<RestaurantTarget>()) Destroy(target);
            if (item.OccupiesFloor && obj.GetComponentsInChildren<Collider>().Length == 0) {
                var c = obj.AddComponent<BoxCollider>(); c.center = new Vector3(0, .65f, 0); c.size = new Vector3(item.Width * .9f, 1.3f, item.Depth * .9f);
            }
            return obj;
        }

        CharacterMotion Elite(int variant, Vector3 position, float rotation, bool working, Transform parent, string caption) {
            var npc = RestaurantArt.CreateEliteCharacter(variant, parent);
            npc.transform.SetPositionAndRotation(position + Vector3.up * .08f, Quaternion.Euler(0, rotation, 0));
            var capsule = npc.AddComponent<CapsuleCollider>(); capsule.radius = .3f; capsule.height = 1.7f; capsule.center = Vector3.up * .85f;
            var motion = npc.GetComponent<CharacterMotion>(); if (motion) { motion.Working = working; motion.SetMood(.9f); }
            if (caption != null) { var t = WorldCaption(npc.transform, caption, new Vector3(0, 2.55f, 0), .014f); t.color = new Color(1f, .82f, .45f); t.transform.rotation = Quaternion.identity; }
            return motion;
        }

        static void AddRivalLight(Vector3 position, Color color, float intensity, float range, Transform parent) {
            var go = new GameObject("Rival light"); go.transform.SetParent(parent, false); go.transform.position = position;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        }

        // The sommelier glides between tables; everyone else stays at their post.
        void AnimateRival(float seconds) {
            if (!sommelier) return;
            sommelierT += seconds * .35f;
            float x = 19 + Mathf.Sin(sommelierT) * 2.4f, vx = Mathf.Cos(sommelierT);
            sommelier.transform.position = new Vector3(x, .08f, 20.1f);
            sommelier.transform.rotation = Quaternion.Euler(0, vx >= 0 ? 90 : 270, 0);
            sommelier.Walking = Mathf.Abs(vx) > .15f;
        }
    }
}
