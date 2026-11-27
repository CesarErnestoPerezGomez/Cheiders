#if UNITY_EDITOR
using UnityEngine;

namespace ACG
{
    // Numbers are surviving bikes. The clock stops on contact with the final wall.
    internal sealed class RaceScoreboard
    {
        static readonly Color Blue = new Color(.025f, .43f, .75f);
        static readonly Color Orange = new Color(.82f, .26f, .045f);
        readonly Font font;
        GUIStyle label;
        int orange = 5, lastSecond = -1;
        float lastLoss = float.NegativeInfinity;
        string clock, orangeCount = "5";
        public float Seconds { private get; set; }

        public RaceScoreboard(Font font) => this.font = font;

        public void LoseOrange()
        {
            orangeCount = (--orange).ToString();
            lastLoss = Time.time;
        }

        public void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;
            int second = Mathf.FloorToInt(Seconds);
            if (second != lastSecond)
            {
                lastSecond = second;
                clock = string.Format("{0}:{1:00}", second / 60, second % 60);
            }
            if (label == null) label = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter };
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            Matrix4x4 previous = GUI.matrix;
            Color tint = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * .5f, 12 * scale), Quaternion.identity,
                new Vector3(scale, scale, 1));
            float pulse = Mathf.Clamp01(1 - (Time.time - lastLoss) / .65f);
            var ink = new Color(.035f, .055f, .09f, .97f);
            Box(-144, 3, 288, 28, ink);
            Box(-147, 6, 3, 22, Blue);
            Box(144, 6, 3, 22, Orange);
            Box(-144, 3, 67, 28, new Color(.035f, .19f, .32f));
            Box(77, 3, 67, 28, new Color(.32f, .115f, .055f));
            Box(-77, 1, 36, 32, Blue);
            Box(41, 1, 36, 32, Color.Lerp(Orange, new Color(1, .68f, .30f), pulse));
            Box(-41, 0, 82, 38, ink);
            Box(-144, 29, 67, 2, new Color(.12f, .78f, 1));
            Box(77, 29, 67, 2, new Color(1, .46f, .13f));
            Text(-144, 3, 67, 27, "CIAN", 8);
            Text(77, 3, 67, 27, "NARANJA", 8);
            Text(-77, 1, 36, 32, "5", 16);
            Text(41, 1, 36, 32, orangeCount, 16);
            Text(-41, 2, 82, 20, clock, 13);
            Text(-41, 22, 82, 13, "EN PISTA", 6);
            GUI.matrix = previous;
            GUI.color = tint;
        }

        static void Box(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }

        void Text(float x, float y, float w, float h, string value, int size)
        {
            label.fontSize = size;
            label.normal.textColor = Color.white;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, h), value, label);
        }
    }
}
#endif
