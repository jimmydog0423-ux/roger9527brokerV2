using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>標題畫面：開新遊戲、繼續遊戲、設定、離開遊戲。</summary>
    public class TitleView : MonoBehaviour
    {
        RectTransform rt;
        GameRoot game;
        Transform menu;

        public static TitleView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Title", 0, 0, 640, 360);
            TitleView v = r.gameObject.AddComponent<TitleView>();
            v.rt = r;
            v.game = game;
            v.BuildArt();
            return v;
        }

        void BuildArt()
        {
            Image bg = Px.Fill(rt, 0, 0, 640, 360, Pal.Hex("#14111e"));
            bg.raycastTarget = true;

            // 夜晚的城市剪影
            Px.Fill(rt, 0, 0, 640, 200, Pal.Hex("#1a1630"));
            int[,] stars = { { 40, 30 }, { 120, 18 }, { 210, 44 }, { 330, 22 }, { 420, 38 }, { 520, 16 }, { 600, 50 }, { 80, 70 }, { 470, 70 } };
            for (int i = 0; i < stars.GetLength(0); i++) Px.Fill(rt, stars[i, 0], stars[i, 1], 2, 2, Pal.PlumLight);
            int[,] city = { { 0, 150, 60, 210 }, { 56, 120, 50, 240 }, { 104, 160, 70, 200 }, { 170, 110, 44, 250 }, { 212, 140, 70, 220 },
                            { 380, 130, 60, 230 }, { 436, 100, 46, 260 }, { 480, 150, 70, 210 }, { 546, 120, 50, 240 }, { 592, 160, 48, 200 } };
            for (int i = 0; i < city.GetLength(0); i++)
                Px.Fill(rt, city[i, 0], city[i, 1], city[i, 2], city[i, 3], i % 2 == 0 ? Pal.Hex("#231d3a") : Pal.Hex("#2a2346"));
            int[,] wins = { { 70, 140 }, { 82, 170 }, { 120, 180 }, { 184, 130 }, { 196, 160 }, { 230, 170 }, { 396, 150 }, { 450, 120 },
                            { 462, 150 }, { 496, 170 }, { 560, 140 }, { 606, 180 }, { 70, 200 }, { 450, 190 } };
            for (int i = 0; i < wins.GetLength(0); i++) Px.Fill(rt, wins[i, 0], wins[i, 1], 4, 4, i % 3 == 0 ? Pal.Orange : Pal.Yellow);
            Px.Fill(rt, 0, 300, 640, 60, Pal.Hex("#0d0b14"));

            // 鬼東與羅傑
            Px.Box(rt, "GuiDong", 40, 176, 100, 116, Pal.VideoWall, Pal.Ink);
            PortraitArt.Draw(rt, "guidong", 42, 178, 4);
            Px.Box(rt, "Roger", 500, 176, 100, 116, Pal.VideoWall, Pal.Ink);
            PortraitArt.Draw(rt, "roger", 502, 178, 4);
            Px.Label(rt, "傳奇經紀人・鬼東", 30, 296, 120, 14, Pal.Lav, TextAnchor.UpperCenter, false);
            Px.Label(rt, "實況主・羅傑", 490, 296, 120, 14, Pal.Lav, TextAnchor.UpperCenter, false);

            // 標題
            Text shadow = Px.Label(rt, "水蛇洞經紀人", 3, 47, 640, 40, Pal.Ink, TextAnchor.UpperCenter, false);
            shadow.fontSize = 36;
            Text title = Px.Label(rt, "水蛇洞經紀人", 0, 44, 640, 40, Pal.Orange, TextAnchor.UpperCenter, false);
            title.fontSize = 36;
            Px.Label(rt, "傳奇經紀人的最後一位學徒", 0, 92, 640, 14, Pal.Cream, TextAnchor.UpperCenter, false);

            menu = Px.Rect(rt, "Menu", 0, 0, 640, 360);
            Px.Label(rt, "雛形版 v0.4　正式營業", 8, 342, 200, 14, Pal.PlumLight);
            Px.Label(rt, Px.Mobile ? "遊戲中點畫面上的「選單」打開選單" : "按 Esc 可以在遊戲中打開選單", 400, 342, 232, 14, Pal.PlumLight, TextAnchor.UpperRight, false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            Px.Clear(menu);
            bool hasSave = SaveSystem.Exists();
            float y = 168;
            Item("開新遊戲", y, true, () => game.NewGame()); y += 26;
            Item("繼續遊戲", y, hasSave, () => game.ContinueGame()); y += 26;
            Item("設定", y, true, () => game.OpenSettings()); y += 26;
#if !UNITY_WEBGL || UNITY_EDITOR
            Item("離開遊戲", y, true, () => game.Quit());
#endif
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Item(string label, float y, bool enabled, System.Action act)
        {
            Color32 bg = enabled ? Pal.Plum : Pal.PlumDeep;
            Color32 fg = enabled ? Pal.Cream : Pal.PlumLight;
            RectTransform b = Px.Button(menu, label, 250, y, 140, 20, bg, fg, enabled ? Pal.PlumLight : Pal.PlumLine, enabled ? act : null);
            if (!enabled) b.GetComponent<Image>().raycastTarget = false;
        }
    }
}
