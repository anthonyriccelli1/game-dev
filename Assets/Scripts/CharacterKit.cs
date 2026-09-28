using System;
using UnityEngine;
namespace RestaurantCity {
    // The resident kit: every character is assembled from shared parts on one rig (BodyRig / HeadRig / ArmL / ArmR /
    // LegL / LegR / Knee / Mouth), so all of them walk, sit, cheer and stomp with CharacterMotion. A new resident is a
    // CharacterSpec (a line of data), not new code. Look: chunky "toy figure" proportions, big heads, big eyes, short legs.
    public enum KitBody { Regular, Stocky, Lanky, Blob, Bot }
    public class CharacterSpec {
        public string Name = "Resident";
        public KitBody Body = KitBody.Regular;
        public string Head = "round";          // round, tall, wide, box
        public string Eyes = "big";            // big, sleepy, glow, three, mismatch, screen
        public string Skin = "E0B08A", Outfit = "467E93", Accent = "E8792E", Hair = "493F38";
        public string[] Parts = new string[0]; // see BuildPart
        public float Height = 1;
        public bool Has(string part) => Array.IndexOf(Parts, part) >= 0;
    }
    public static partial class RestaurantArt {
        public static GameObject BuildCharacter(CharacterSpec s, Transform parent) {
            var g = Group(s.Name, parent); var b = Group("BodyRig", g.transform).transform;
            Color skin = C(s.Skin), outfit = C(s.Outfit), accent = C(s.Accent), hair = C(s.Hair);
            float w = s.Body == KitBody.Stocky ? .8f : s.Body == KitBody.Lanky ? .46f : s.Body == KitBody.Bot ? .62f : .58f;
            float torso = s.Body == KitBody.Lanky ? .62f : s.Body == KitBody.Stocky ? .44f : .47f;
            float size = s.Body == KitBody.Stocky ? .76f : s.Body == KitBody.Lanky ? .64f : .72f;   // big heads are the look
            float top = .72f + torso, headY = top + size * .46f;
            if (s.Body == KitBody.Blob) {
                Shape("KitBlob", b, Profile("kitblob", new[] { 0f, .08f, .22f, .58f, .98f, 1.22f, 1.37f }, new[] { .31f, .49f, .5f, .45f, .39f, .24f, 0f }, 12), Vector3.zero, new Vector3(1, 1, .86f), Solid(skin));
                foreach (int side in new[] { -1, 1 }) Limb(b, side < 0 ? "ArmL" : "ArmR", new Vector3(side * .46f, .7f, 0), skin, .34f, .2f);
                headY = 1.0f; size = .62f;
            } else {
                if (s.Body == KitBody.Bot) Box("KitBotTorso", b, new Vector3(0, .72f + torso * .5f, 0), new Vector3(w, torso, w * .72f), outfit);
                else { var t = Lathe("KitTorso_" + s.Body, b, new Vector3(0, .72f, 0), new[] { 0f, .08f, torso * .8f, torso }, new[] { w * .46f, w * .55f, w * .5f, w * .3f }, Solid(outfit), 10); t.transform.localScale = new Vector3(1, 1, .8f); }
                float legW = s.Body == KitBody.Stocky ? .24f : .17f;
                foreach (int side in new[] { -1, 1 }) {
                    var leg = Group(side < 0 ? "LegL" : "LegR", b, new Vector3(side * w * .27f, .73f, 0)).transform;
                    Color pants = s.Body == KitBody.Bot ? outfit * .7f : Color.Lerp(outfit, Ink, .55f);
                    Box("KitThigh", leg, new Vector3(0, -.145f, 0), new Vector3(legW, .29f, .19f), pants);
                    var knee = Group("Knee", leg, new Vector3(0, -.29f, 0)).transform;
                    Box("KitShin", knee, new Vector3(0, -.145f, 0), new Vector3(legW * .94f, .29f, .18f), pants);
                    Box("KitShoe", knee, new Vector3(0, -.35f, .07f), new Vector3(legW + .07f, .14f, .33f), s.Body == KitBody.Bot ? Brass : Ink);
                    Limb(b, side < 0 ? "ArmL" : "ArmR", new Vector3(side * w * .58f, top - .08f, 0), s.Body == KitBody.Bot ? outfit : skin, s.Body == KitBody.Lanky ? .68f : .56f, s.Body == KitBody.Stocky ? .25f : .17f);
                }
            }
            var head = Group("HeadRig", b, new Vector3(0, headY, 0)).transform;
            Vector3 hs = s.Head == "tall" ? new Vector3(size * .8f, size * 1.18f, size * .84f) : s.Head == "wide" ? new Vector3(size * 1.22f, size * .82f, size * .9f) : new Vector3(size, size * .94f, size * .9f);
            if (s.Head == "box") Box("KitHead", head, Vector3.zero, new Vector3(size * .95f, size * .75f, size * .72f), skin);
            else Round("KitHead", head, Vector3.zero, hs, skin);
            float face = s.Head == "box" ? size * .37f : hs.z * .45f;   // how far forward the face sits
            BuildEyes(head, s, size, face, accent);
            Box("Mouth", head, new Vector3(0, -hs.y * .24f, face + .01f), new Vector3(size * .24f, .035f, .025f), s.Eyes == "screen" ? accent : Ink);
            foreach (var part in s.Parts) BuildPart(part, g.transform, b, head, s, hs, face, top, w, skin, outfit, accent, hair);
            g.transform.localScale = Vector3.one * s.Height;
            g.AddComponent<CharacterMotion>();
            return g;
        }

        static void BuildEyes(Transform head, CharacterSpec s, float size, float z, Color accent) {
            float ew = size * .2f, sep = s.Head == "wide" ? size * .3f : size * .2f, y = size * .05f;   // y: eye height for glow/screen eyes
            void Eye(float x, float scale, bool sleepy, float lift = 0) {
                float y = size * .05f + lift;
                Round("EyeWhite", head, new Vector3(x, y, z), new Vector3(ew * scale, ew * 1.25f * scale, .07f), White);
                Round("Pupil", head, new Vector3(x, y - ew * .08f, z + .035f), new Vector3(ew * .52f * scale, ew * .74f * scale, .035f), Ink);
                Round("EyeGlint", head, new Vector3(x - ew * .12f, y + ew * .16f * scale, z + .055f), new Vector3(ew * .2f, ew * .2f, .02f), Color.white);
                if (sleepy) Box("Eyelid", head, new Vector3(x, y + ew * .38f, z + .04f), new Vector3(ew * 1.15f, ew * .62f, .06f), C(s.Skin) * .9f);
            }
            switch (s.Eyes) {
                case "glow": foreach (float x in new[] { -sep, sep }) Shape("GlowEye", head, Profile("gem", new[] { -.5f, -.35f, 0, .35f, .5f }, new[] { 0f, .38f, .5f, .38f, 0f }, 10), new Vector3(x, y, z), new Vector3(ew * .9f, ew * .55f, .05f), Mat("EyeGlow" + s.Accent, accent, 0, true)); break;
                case "three": Eye(-sep * 1.1f, .8f, false); Eye(sep * 1.1f, .8f, false); Eye(0, 1.05f, false, size * .2f); break;   // the middle one sits higher
                case "mismatch": Eye(-sep, 1.15f, false); Eye(sep, .62f, false); break;
                case "screen":
                    Box("Screen", head, new Vector3(0, 0, z + .01f), new Vector3(size * .72f, size * .46f, .04f), Ink);
                    foreach (float x in new[] { -sep * .9f, sep * .9f }) Shape("LEDEye", head, Profile("gem", new[] { -.5f, -.35f, 0, .35f, .5f }, new[] { 0f, .38f, .5f, .38f, 0f }, 10), new Vector3(x, y, z + .04f), new Vector3(ew * .75f, ew, .04f), Mat("LED" + s.Accent, accent, 0, true));
                    break;
                default: foreach (float x in new[] { -sep, sep }) Eye(x, 1, s.Eyes == "sleepy"); break;
            }
        }

        static void BuildPart(string part, Transform root, Transform b, Transform head, CharacterSpec s, Vector3 hs, float face, float top, float w, Color skin, Color outfit, Color accent, Color hair) {
            switch (part) {
                case "hair": Round("KitHair", head, new Vector3(0, hs.y * .3f, -.03f), new Vector3(hs.x * 1.02f, hs.y * .5f, hs.z * .95f), hair); break;
                case "slick_hair":   // vampire: swept back with a widow's peak
                    Round("SlickHair", head, new Vector3(0, hs.y * .28f, -.06f), new Vector3(hs.x * 1.03f, hs.y * .52f, hs.z * .96f), hair);
                    var peak = Box("WidowsPeak", head, new Vector3(0, hs.y * .3f, face - .04f), new Vector3(hs.x * .2f, hs.x * .2f, .06f), hair); peak.transform.localRotation = Quaternion.Euler(0, 0, 45); break;
                case "cap":   // backwards cap
                    Lathe("CapDome", head, new Vector3(0, hs.y * .22f, 0), new[] { 0f, .12f, .2f }, new[] { hs.x * .53f, hs.x * .45f, 0f }, Solid(accent), 12);
                    Box("CapBrim", head, new Vector3(0, hs.y * .25f, -hs.z * .52f), new Vector3(hs.x * .6f, .03f, hs.z * .4f), accent); break;
                case "fangs": foreach (float x in new[] { -.045f, .045f }) Box("Fang", head, new Vector3(x, -hs.y * .3f, face + .02f), new Vector3(.03f, .07f, .03f), White); break;
                case "cape":   // high vampire collar and a cape down the back
                    foreach (int side in new[] { -1, 1 }) { var c = Box("CapeCollar", b, new Vector3(side * w * .42f, top + .12f, -.08f), new Vector3(.05f, .42f, .34f), outfit); c.transform.localRotation = Quaternion.Euler(0, side * 25, side * 14); Box("CollarLining", b, new Vector3(side * w * .4f, top + .12f, -.06f), new Vector3(.02f, .36f, .28f), accent); }
                    Box("Cape", b, new Vector3(0, .82f, -w * .42f), new Vector3(w * 1.25f, .95f, .04f), outfit); break;
                case "stitches":
                    for (int i = 0; i < 4; i++) Box("Stitch", head, new Vector3(-hs.x * .15f + i * hs.x * .1f, hs.y * .28f, face - .02f), new Vector3(.015f, .08f, .02f), Ink);
                    Box("StitchLine", head, new Vector3(0, hs.y * .28f, face - .025f), new Vector3(hs.x * .38f, .015f, .02f), Ink);
                    Box("Patch", b, new Vector3(w * .15f, top - .2f, w * .43f), new Vector3(.14f, .12f, .02f), accent); break;
                case "dark_eyes": foreach (float x in new[] { -hs.x * .2f, hs.x * .2f }) Round("EyeBag", head, new Vector3(x, hs.y * .02f, face - .02f), new Vector3(hs.x * .3f, hs.x * .32f, .04f), C(s.Skin) * .62f); break;
                case "antennae":
                    foreach (int side in new[] { -1, 1 }) { Rod("Antenna", head, new Vector3(side * hs.x * .18f, hs.y * .4f, 0), new Vector3(side * hs.x * .38f, hs.y * .95f, -.04f), .025f, skin * .8f); Shape("AntennaBulb", head, Profile("gem", new[] { -.5f, -.35f, 0, .35f, .5f }, new[] { 0f, .38f, .5f, .38f, 0f }, 10), new Vector3(side * hs.x * .38f, hs.y * .98f, -.04f), Vector3.one * .1f, Mat("Bulb" + s.Accent, accent, 0, true)); }
                    break;
                case "long_ears": foreach (int side in new[] { -1, 1 }) { var ear = Round("LongEar", head, new Vector3(side * hs.x * .42f, hs.y * .55f, 0), new Vector3(.14f, .68f, .14f), skin); ear.transform.localRotation = Quaternion.Euler(0, 0, -side * 16); } break;
                case "horns": foreach (int side in new[] { -1, 1 }) { var horn = Lathe("Horn", head, new Vector3(side * hs.x * .35f, hs.y * .36f, 0), new[] { 0f, .18f, .32f }, new[] { .075f, .05f, 0f }, Solid(Cream), 8); horn.transform.localRotation = Quaternion.Euler(0, 0, -side * 28); } break;
                case "mushroom_cap": Lathe("MushroomCap", head, new Vector3(0, hs.y * .22f, 0), new[] { 0f, .05f, .14f, .31f, .45f }, new[] { .58f, .61f, .52f, .36f, 0f }, Solid(accent), 12); for (int i = 0; i < 4; i++) Round("CapSpot", head, new Vector3(Mathf.Cos(i * 1.6f) * .3f, hs.y * .22f + .25f, Mathf.Sin(i * 1.6f) * .3f), new Vector3(.1f, .05f, .1f), White); break;
                case "tentacles": for (int i = 0; i < 4; i++) { var t = Round("Tentacle", head, new Vector3(-.15f + i * .1f, -hs.y * .5f, face * .7f), new Vector3(.07f, .3f, .07f), skin * .9f); t.transform.localRotation = Quaternion.Euler(15, 0, (i - 1.5f) * 12); } break;
                case "chain": for (int i = 0; i < 9; i++) { float a = Mathf.PI * (.15f + i * .0875f); Round("ChainLink", b, new Vector3(Mathf.Cos(a) * w * .36f, top - .06f - Mathf.Sin(a) * .16f, w * .38f), Vector3.one * .06f, Brass); } Round("ChainPendant", b, new Vector3(0, top - .25f, w * .4f), new Vector3(.1f, .12f, .04f), Brass); break;
                case "shades_up": foreach (float x in new[] { -hs.x * .17f, hs.x * .17f }) Box("ShadeLens", head, new Vector3(x, hs.y * .36f, face - .06f), new Vector3(hs.x * .26f, hs.x * .13f, .03f), Ink); Box("ShadeBridge", head, new Vector3(0, hs.y * .37f, face - .06f), new Vector3(hs.x * .1f, .02f, .03f), Brass); break;
                case "tie": Box("Tie", b, new Vector3(0, top - .2f, w * .44f), new Vector3(.06f, .26f, .025f), accent); break;
                case "apron": Box("Apron", b, new Vector3(0, top - .24f, w * .44f), new Vector3(w * .78f, .52f, .03f), White); break;
                case "chef_hat": Lathe("ChefBrim", head, new Vector3(0, hs.y * .38f, 0), new[] { 0f, .14f }, new[] { .26f, .26f }, Solid(White), 12); for (int i = 0; i < 3; i++) Round("ChefPuff", head, new Vector3((i - 1) * .13f, hs.y * .38f + .2f, 0), new Vector3(.24f, .22f, .26f), White); break;
                case "bolts": foreach (int side in new[] { -1, 1 }) { Round("EarBolt", head, new Vector3(side * hs.x * .52f, 0, 0), Vector3.one * .12f, Brass); Round("ShoulderBolt", b, new Vector3(side * w * .55f, top - .06f, 0), Vector3.one * .18f, Brass); } break;
            }
        }

        // The style-test cast: one resident per species, built entirely from the kit.
        public static readonly CharacterSpec[] StyleTestCast = {
            new CharacterSpec { Name = "Rush-hour Rae (human)", Body = KitBody.Regular, Head = "round", Eyes = "big", Skin = "C68B5E", Outfit = "467E93", Accent = "D96555", Hair = "3B2A22", Parts = new[] { "hair", "tie" } },
            new CharacterSpec { Name = "Count Vex (vampire)", Body = KitBody.Lanky, Head = "tall", Eyes = "glow", Skin = "DCD6F0", Outfit = "2A1F3D", Accent = "E1243B", Hair = "1A1426", Parts = new[] { "slick_hair", "fangs", "cape" }, Height = 1.05f },
            new CharacterSpec { Name = "Grub (zombie)", Body = KitBody.Stocky, Head = "round", Eyes = "mismatch", Skin = "93B27A", Outfit = "6B6E5A", Accent = "B55A3C", Hair = "3C4A33", Parts = new[] { "dark_eyes", "stitches", "hair" } },
            new CharacterSpec { Name = "Zeeb (alien dealer)", Body = KitBody.Regular, Head = "wide", Eyes = "three", Skin = "7B5CD6", Outfit = "F5F1E8", Accent = "7FF0D0", Parts = new[] { "antennae", "shades_up", "chain" } },
            new CharacterSpec { Name = "Unit P-04 (robot)", Body = KitBody.Bot, Head = "box", Eyes = "screen", Skin = "A4C3BE", Outfit = "317E79", Accent = "73E08A", Parts = new[] { "bolts" } },
            new CharacterSpec { Name = "Gloop (jelly)", Body = KitBody.Blob, Head = "round", Eyes = "big", Skin = "77C5A6", Outfit = "77C5A6", Accent = "D96555" },
            new CharacterSpec { Name = "Morel (mushroom)", Body = KitBody.Regular, Head = "round", Eyes = "sleepy", Skin = "E6D4A3", Outfit = "866747", Accent = "D96555", Parts = new[] { "mushroom_cap" }, Height = .88f },
            new CharacterSpec { Name = "Bront (horned)", Body = KitBody.Stocky, Head = "wide", Eyes = "big", Skin = "D9826B", Outfit = "586354", Accent = "CA9B53", Parts = new[] { "horns" }, Height = 1.08f },
        };
    }
}
