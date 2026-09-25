using UnityEngine;

namespace RestaurantCity {
    public class CityHud : MonoBehaviour {
        public CityGame Game;
        GUIStyle small, body, title, heading, button;
        readonly Color ink = new Color(.075f, .13f, .16f);
        readonly Color cream = new Color(.97f, .95f, .87f);
        readonly Color teal = new Color(.25f, .80f, .66f);
        readonly Color coral = new Color(1, .43f, .31f);
        bool confirmReset;
        void Init() {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            small = new GUIStyle { font = font, fontSize = 13, normal = { textColor = cream }, wordWrap = true };
            body = new GUIStyle(small) { fontSize = 19 };
            heading = new GUIStyle(body) { fontSize = 26, fontStyle = FontStyle.Bold };
            title = new GUIStyle(heading) { fontSize = 58 };
            button = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = ink } };
        }
        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Text(float x, float y, float w, float h, string text, GUIStyle style = null) { GUI.Label(new Rect(x, y, w, h), text, style ?? body); }
        bool Button(Rect r, string label, Color color) {
            bool hover = r.Contains(Event.current.mousePosition);
            Box(r, hover ? Color.Lerp(color, Color.white, .15f) : color);
            return GUI.Button(r, label, button);
        }
        void Panel(Rect r) { Box(r, new Color(ink.r, ink.g, ink.b, .94f)); }
        void Meter(Rect r, float amount, Color color) { Box(r, new Color(1, 1, 1, .13f)); Box(new Rect(r.x, r.y, r.width * Mathf.Clamp01(amount), r.height), color); }

        void OnGUI() {
            if (!Game) return;
            if (Game.CoOp && !Game.Paused) return;
            if (Game.Restaurant && (Game.Restaurant.PanelOpen || Game.Restaurant.PlacementActive || Game.Restaurant.Data.Owned && Game.Restaurant.Inside && !Game.Paused)) return;
            if (body == null) Init();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1440f, Screen.height / 900f, 1));
            var s = Game.State;
            Box(new Rect(0, 0, 1440, 5), s.IsNight ? coral : teal);
            Panel(new Rect(28, 26, 318, 79));
            Text(46, 38, 290, 18, "RESTAURANT CITY   /   FIRST PLAYABLE", small);
            Text(46, 60, 290, 35, "01  /  Market Row", heading);
            Panel(new Rect(940, 26, 472, 79));
            Text(960, 38, 180, 24, "DAY " + s.Day + (s.IsNight ? "  /  NIGHT" : "  /  AFTERNOON"), small);
            Text(960, 62, 180, 25, s.IsNight ? "Orders pay 50% more" : "Nightfall brings opportunity", small);
            Text(1170, 44, 110, 42, "$" + s.Cash, heading);
            Text(1280, 42, 160, 24, "PANTRY  " + s.Restaurant.Protein + "/" + s.Restaurant.Produce, small);
            Text(1280, 67, 120, 24, "HEALTH  " + s.Health, small);
            Meter(new Rect(960, 91, 430, 3), s.Clock / 240, s.IsNight ? coral : teal);

            if (Game.Paused) {
                Box(new Rect(0, 105, 1440, 795), new Color(.03f, .07f, .09f, .35f));
                Panel(new Rect(70, 174, 565, 590));
                Box(new Rect(70, 174, 5, 590), teal);
                Text(106, 207, 480, 22, "A SMALL STAND. A BIG AMBITION.", small);
                Text(103, 246, 510, 143, Game.Started ? "Take a\nbreather." : "Your city.\nYour kitchen.", title);
                Text(106, 407, 480, 86, "Buy ingredients. Fire up the grill. Earn your first customers, then chase a rare recipe after dark.");
                Text(106, 515, 490, 68, "WASD  Move     MOUSE  Look     SHIFT  Sprint\nE  Interact     LEFT CLICK  Swing spatula\nESC  Pause and release the mouse", small);
                if (Button(new Rect(106, 603, 490, 56), Game.Started ? "BACK TO THE STREET   >" : "START YOUR FIRST SHIFT   >", teal)) { confirmReset = false; Game.SetPaused(false); }
                if (Game.Started) {
                    if (Button(new Rect(106, 674, 235, 39), confirmReset ? "CONFIRM NEW GAME" : "NEW GAME", confirmReset ? coral : cream)) {
                        if (confirmReset) { Game.NewGame(); confirmReset = false; } else confirmReset = true;
                    }
                    if (Button(new Rect(354, 674, 242, 39), "SAVE & QUIT", cream)) {
                        Game.Save();
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                }
                Text(106, 735, 480, 21, Game.SaveStatus, small);
                return;
            }

            Panel(new Rect(28, 680, 420, 155));
            Text(48, 698, 370, 18, "YOUR NEXT MOVE", small);
            Text(48, 730, 375, 95, Game.Objective);
            Panel(new Rect(1080, 130, 332, 132));
            Text(1100, 147, 290, 20, s.HasOrder ? "ORDER  /  ONE BURGER" : "SERVICE", small);
            Text(1100, 179, 290, 34, s.HasOrder ? "Customer waiting" : s.StandBuilt ? "Next guest on the way" : "Set up to open", body);
            if (s.HasOrder) {
                Meter(new Rect(1100, 224, 290, 7), s.Patience / 65, s.Patience > 20 ? teal : coral);
                Text(1100, 237, 290, 20, Mathf.CeilToInt(s.Patience) + "s patience remaining", small);
            }
            if (s.Food != FoodStage.Empty) {
                Panel(new Rect(1080, 278, 332, 125));
                Text(1100, 295, 290, 20, "IN YOUR KITCHEN", small);
                string stage = s.Food == FoodStage.Prepared ? "Prepared / take to grill" : s.Food == FoodStage.Plated ? s.IsBurnt ? "Burned / use the bin" : "Plated / ready to serve" : s.IsBurnt ? "Burned / discard this one" : s.CookSeconds >= 4 ? "READY / press E at grill" : "Grilling / " + s.CookSeconds.ToString("0.0") + "s";
                Text(1100, 326, 290, 46, stage);
                if (s.Food == FoodStage.Cooking) {
                    Meter(new Rect(1100, 379, 290, 8), s.CookSeconds / 10, s.IsBurnt ? coral : s.CookSeconds >= 4 ? teal : cream);
                    Box(new Rect(1216, 376, 2, 14), cream);
                }
            }
            if (s.RecipeUnlocked) {
                Panel(new Rect(1080, 420, 332, 75));
                Text(1100, 435, 290, 20, "RECIPE DISCOVERED", small);
                Text(1100, 459, 290, 27, "Midnight burger  /  $18+", body);
            }
            Box(new Rect(717, 447, 6, 6), cream);
            if (Game.Player.Target) {
                Panel(new Rect(440, 567, 560, 54));
                Box(new Rect(450, 577, 34, 34), teal);
                GUI.Label(new Rect(450, 577, 34, 34), "E", button);
                Text(499, 581, 487, 36, Game.Player.Target.Prompt(Game), small);
            }
            if (!string.IsNullOrEmpty(Game.Notice)) {
                Panel(new Rect(460, 736, 565, 87));
                Box(new Rect(460, 736, 4, 87), teal);
                Text(480, 752, 525, 65, Game.Notice, body);
            }
            Panel(new Rect(0, 858, 1440, 42));
            Text(28, 871, 960, 22, "WASD  MOVE     SHIFT  SPRINT     E  INTERACT     LEFT CLICK  SPATULA     ESC  PAUSE", small);
            Text(1090, 871, 322, 22, "SERVED " + s.Served + "    /    " + Game.SaveStatus, small);
        }
    }
}
