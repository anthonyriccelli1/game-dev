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
            // Transform.Find treats "/" as a path separator, so match the child by name instead.
            rivalRoot = null; if (world) foreach (Transform child in world) if (child.name == "Rival restaurant / The Gilded Orbit") { rivalRoot = child; break; }
            if (!rivalRoot || rivalRoot.Find("Interior")) return;
            var inside = new GameObject("Interior").transform; inside.SetParent(rivalRoot, false);

            // Kitchen line along the back wall, facing the dining room.
            Place("oven", 15.4f, 22.6f, 180, inside); Place("grill", 17.5f, 22.6f, 180, inside); Place("grill", 19.6f, 22.6f, 180, inside);
            Place("stove", 21.2f, 22.6f, 180, inside); Place("fridge", 22.2f, 22.6f, 180, inside); Place("sink", 23.2f, 22.6f, 180, inside);
            Place("assembly", 18.2f, 21.0f, 180, inside); Place("assembly", 20.3f, 21.0f, 180, inside);
            for (int i = 0; i < 4; i++) {
                var plate = KitchenArt.CreateItem("Plate", new List<string> { "bun", "cooked_patty", i % 2 == 0 ? "midnight_sauce" : "chopped_greens" }, inside);
                plate.transform.position = new Vector3(17.6f + i * .95f, 1.1f, 21.0f);
            }
            Place("neon_moon", 19.6f, 23.15f, 180, inside); Place("art_orbit", 15.4f, 23.2f, 180, inside); Place("art_orbit", 22.6f, 23.2f, 180, inside);
            // Side walls: a backlit wine wall and a glowing aquarium.
            Luxury("wine_wall", new Vector3(14.45f, 0, 21.2f), 90, inside, true);
            Luxury("aquarium", new Vector3(23.55f, 0, 21.2f), 270, inside, true);
            Place("jukebox", 16.3f, 21.0f, 180, inside);
            // Dining room.
            Place("rug_sunset", 19f, 18.6f, 0, inside); var table = Place("communal_table", 19f, 18.6f, 0, inside);
            var boothL = Place("booth_coral", 15.3f, 18.6f, 90, inside); var boothR = Place("booth_teal", 22.7f, 18.6f, 270, inside);
            Luxury("chandelier", new Vector3(19, .08f, 18.6f), 0, inside, false);
            Place("pendant_amber", 15.3f, 18.6f, 0, inside); Place("pendant_amber", 22.7f, 18.6f, 0, inside);
            Luxury("gold_column", new Vector3(16.6f, .08f, 17.0f), 0, inside, true); Luxury("gold_column", new Vector3(21.4f, .08f, 17.0f), 0, inside, true);
            Place("fern", 17.6f, 17.0f, 0, inside); Place("fern", 20.4f, 17.0f, 0, inside);
            Place("globe_lamp", 14.6f, 16.95f, 0, inside); Place("globe_lamp", 23.4f, 16.95f, 0, inside);
            Luxury("velvet_curtain", new Vector3(14.35f, .08f, 20.2f), 90, inside, false); Luxury("velvet_curtain", new Vector3(23.65f, .08f, 20.2f), 270, inside, false);
            // Outside: gold orbit statues flanking the red carpet.
            Luxury("statue", new Vector3(16.3f, 0, 15.5f), 0, inside, true); Luxury("statue", new Vector3(21.7f, 0, 15.5f), 0, inside, true);

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
            // Elite staff. They never leave this building.
            Elite(0, new Vector3(18.5f, 0, 21.8f), 0, true, inside, "MAESTRO VEY\nHead chef");
            Elite(2, new Vector3(17.5f, 0, 21.9f), 0, true, inside, "K-9\nLine cook");
            Elite(2, new Vector3(19.9f, 0, 21.8f), 0, true, inside, null);
            Elite(4, new Vector3(21.7f, 0, 21.8f), 0, true, inside, "SERAPHINE\nPastry chef");
            Elite(6, new Vector3(15.2f, 0, 21.2f), 270, true, inside, "LUMEN\nMixologist");
            sommelier = Elite(1, new Vector3(17, 0, 20.1f), 90, false, inside, "NYX\nSommelier");
            Elite(3, new Vector3(17.7f, 0, 15.3f), 180, false, inside, "AURORA\nMaitre d'");
            Elite(5, new Vector3(20.4f, 0, 15.3f), 180, false, inside, "OBSIDIAN TITAN\nDoorman");

            // Warm, expensive light that glows through the glass at night.
            foreach (var x in new[] { 16f, 22f }) AddRivalLight(new Vector3(x, 3.9f, 19.8f), new Color(1f, .78f, .5f), 3.2f, 7.5f, inside);
            AddRivalLight(new Vector3(19, 3.2f, 18.6f), new Color(1f, .86f, .6f), 4f, 6f, inside);
            AddRivalLight(new Vector3(23.2f, 1.6f, 21.2f), new Color(.3f, .85f, 1f), 2.5f, 4f, inside);
            AddRivalLight(new Vector3(14.8f, 1.6f, 21.2f), new Color(1f, .65f, .3f), 2.5f, 4f, inside);
            AddRivalLight(new Vector3(19, 5.2f, 15.4f), new Color(1f, .75f, .35f), 4f, 6f, inside);
        }

        void Luxury(string id, Vector3 position, float rotation, Transform parent, bool solid) {
            var obj = RestaurantArt.CreateLuxury(id, parent);
            obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
            if (solid) { var c = obj.AddComponent<BoxCollider>(); c.center = new Vector3(0, 1, 0); c.size = id == "gold_column" ? new Vector3(.6f, 2, .6f) : new Vector3(1.8f, 2, .6f); }
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
