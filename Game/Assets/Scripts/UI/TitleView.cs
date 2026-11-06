using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>標題畫面：開新遊戲、繼續遊戲、設定、離開遊戲。</summary>
    public class TitleView : MonoBehaviour
    {
        const int MenuX = 42;
        const int MenuWidth = 196;
        const int ItemHeight = 29;

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
            Image bg = Px.Fill(rt, 0, 0, PixelScaler.Width, PixelScaler.Height, Pal.Ink);
            bg.raycastTarget = true;
            Sprite office = Resources.Load<Sprite>("Title/broker-office-v2");
            if (office != null)
            {
                bg.sprite = office;
                bg.color = Color.white;
            }

            RectTransform badge = Px.Box(rt, "Version", MenuX, 37, 26, 17, Pal.Orange, Pal.Clear);
            Px.Label(badge, "V2", 0, 1, 26, 14, Pal.Ink, TextAnchor.UpperCenter, false);
            Px.Label(rt, "經紀模擬・正式營業", MenuX + 35, 38, 216, 14, Pal.Lav);
            Px.Label(rt, "水蛇洞經紀人", MenuX + 2, 70, 286, 42, Pal.Ink).fontSize = 36;
            Px.Label(rt, "水蛇洞經紀人", MenuX, 68, 286, 42, Pal.Cream).fontSize = 36;
            Px.Fill(rt, MenuX, 117, 26, 2, Pal.Orange);
            Px.Label(rt, "傳奇經紀人的最後一位學徒", MenuX, 130, 265, 14, Pal.Cream);

            DrawPortrait("guidong", "鬼東", "傳奇經紀人", 350, 181, Pal.Teal);
            DrawPortrait("roger", "羅傑", "實況主", 474, 181, Pal.Orange);

            Px.Fill(rt, 0, 337, 640, 23, Pal.Alpha(Pal.Ink, 230));
            Px.Fill(rt, MenuX, 336, 556, 1, Pal.PlumLight);
            Px.Label(rt, "遊戲中可點選畫面上的「選單」", 337, 342, 261, 14, Pal.Lav, TextAnchor.UpperRight, false);
            menu = Px.Rect(rt, "Menu", 0, 0, 640, 360);
        }

        void DrawPortrait(string id, string characterName, string role, int x, int y, Color32 accent)
        {
            RectTransform card = Px.Box(rt, "Title_" + id, x, y, 114, 144, Pal.Alpha(Pal.Ink, 224), Pal.PlumLight);
            Px.Fill(card, 1, 1, 112, 112, Pal.Alpha(accent, 42));
            PortraitArt.Draw(card, id, 9, 1, 4);
            Px.Fill(card, 1, 113, 112, 1, accent);
            Px.Label(card, characterName, 8, 121, 31, 14, Pal.Cream);
            Px.Label(card, role, 40, 121, 66, 14, Pal.Lav, TextAnchor.UpperRight, false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            Px.Clear(menu);
            bool hasSave = SaveSystem.Exists();
            Item("開新遊戲", 165, !hasSave, game.NewGame);
            Item("繼續遊戲", 201, hasSave, hasSave ? game.ContinueGame : null);
            Item("設定", 237, false, game.OpenSettings);
#if !UNITY_WEBGL || UNITY_EDITOR
            Item("離開遊戲", 273, false, game.Quit);
#endif
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Item(string label, int y, bool primary, System.Action act)
        {
            bool enabled = act != null;
            Color32 bg = primary ? Pal.Orange : enabled ? Pal.Plum : Pal.PlumDeep;
            Color32 fg = primary ? Pal.Ink : enabled ? Pal.Cream : Pal.Lav;
            RectTransform button = Px.Button(menu, label, MenuX, y, MenuWidth, ItemHeight, bg, fg,
                primary ? Pal.Yellow : Pal.PlumLight, act);
            Text caption = button.GetComponentInChildren<Text>();
            caption.alignment = TextAnchor.UpperLeft;
            Px.Place(caption.rectTransform, 12, 7, 96, 14);
            Px.Label(button, enabled ? ">" : "尚無存檔", 110, 7, 74, 14, fg, TextAnchor.UpperRight, false);
            Image surface = button.GetComponent<Image>();
            surface.raycastTarget = enabled;
            if (enabled)
                button.gameObject.AddComponent<Hover>().onHover = over => surface.color = over
                    ? (primary ? Pal.Yellow : Pal.PlumLine)
                    : bg;
        }
    }
}
