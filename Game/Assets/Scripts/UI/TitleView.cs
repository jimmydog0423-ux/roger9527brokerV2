using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>標題畫面：開新遊戲、繼續遊戲、設定、離開遊戲。</summary>
    public class TitleView : MonoBehaviour
    {
        const int MenuX = 38;
        const int MenuWidth = 252;
        const int RowX = 10;
        const int RowWidth = MenuWidth - RowX * 2;
        const int UtilityGap = 8;

        RectTransform rt;
        GameRoot game;
        RectTransform menu;

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

            RectTransform badge = Px.Box(rt, "Version", MenuX, 30, 28, 17, Pal.PlumDeep, Pal.Orange);
            Px.Label(badge, "V2", 0, 1, 28, 14, Pal.Orange, TextAnchor.UpperCenter, false);
            Px.Label(rt, "經紀模擬・正式營業", MenuX + 38, 31, 216, 14, Pal.Lav);
            Px.Label(rt, "水蛇洞經紀人", MenuX + 2, 63, 286, 42, Pal.Ink).fontSize = 36;
            Px.Label(rt, "水蛇洞經紀人", MenuX, 61, 286, 42, Pal.Cream).fontSize = 36;
            Px.Fill(rt, MenuX, 108, 26, 2, Pal.Orange);
            Px.Label(rt, "傳奇經紀人的最後一位學徒", MenuX, 119, 265, 14, Pal.Cream);

            RectTransform cast = Px.Box(rt, "Cast", 350, 152, 238, 19, Pal.Alpha(Pal.Ink, 232), Pal.PlumLight);
            Px.Fill(cast, 0, 0, 2, 19, Pal.Teal);
            Px.Label(cast, "人物檔案", 9, 2, 96, 14, Pal.Cream);
            DrawPortrait("guidong", "鬼東", "傳奇經紀人", 350, 176, Pal.Teal);
            DrawPortrait("roger", "羅傑", "實況主", 474, 176, Pal.Orange);

            Px.Fill(rt, 0, 337, 640, 23, Pal.Alpha(Pal.Ink, 230));
            Px.Fill(rt, MenuX, 336, 550, 1, Pal.PlumLight);
            Px.Label(rt, "遊戲中可點選畫面上的「選單」", 327, 342, 261, 14, Pal.Lav, TextAnchor.UpperRight, false);
            RectTransform panel = Px.Box(rt, "Menu", MenuX, 150, MenuWidth, 170, Pal.Alpha(Pal.Ink, 244), Pal.PlumLight);
            Px.Fill(panel, 1, 1, MenuWidth - 2, 22, Pal.PlumDeep);
            Px.Label(panel, "主選單", RowX, 4, 90, 14, Pal.Cream);
            Px.Fill(panel, RowX, 124, RowWidth, 1, Pal.PlumLine);
            menu = Px.Rect(panel, "MenuActions", 0, 0, MenuWidth, 170);
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
            Px.Label(menu, hasSave ? "找到本機存檔" : "尚無存檔", 112, 4, 128, 14,
                hasSave ? Pal.TealLight : Pal.Lav, TextAnchor.UpperRight, false);
            if (hasSave)
            {
                Entry("繼續遊戲", "接續上次自動存檔", 29, true, game.ContinueGame);
                Entry("開新遊戲", "開始前會確認是否覆蓋存檔", 76, false, game.NewGame);
            }
            else
            {
                Entry("開新遊戲", "開始你的經紀人生涯", 29, true, game.NewGame);
                Entry("繼續遊戲", "建立進度後即可繼續", 76, false, null);
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            const int utilityWidth = (RowWidth - UtilityGap) / 2;
            Utility("遊戲設定", RowX, utilityWidth, game.OpenSettings);
            Utility("離開遊戲", RowX + utilityWidth + UtilityGap, utilityWidth, game.Quit);
#else
            Utility("遊戲設定", RowX, RowWidth, game.OpenSettings);
#endif
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Entry(string title, string description, int y, bool primary, Action action)
        {
            bool enabled = action != null;
            Color32 text = primary ? Pal.Ink : enabled ? Pal.Cream : Pal.Lav;
            RectTransform row = MenuButton(title, RowX, y, RowWidth, 41, primary, action);
            Px.Label(row, title, 12, 5, RowWidth - 34, 14, text);
            Px.Label(row, description, 12, 22, RowWidth - 34, 14, primary ? Pal.PaperInk : Pal.Lav);
            if (enabled)
                Px.Label(row, ">", RowWidth - 21, 13, 12, 14, text, TextAnchor.UpperCenter, false);
        }

        void Utility(string title, int x, int width, Action action)
        {
            RectTransform row = MenuButton(title, x, 132, width, 26, false, action);
            Px.Label(row, title, 0, 5, width, 14, Pal.Cream, TextAnchor.UpperCenter, false);
        }

        RectTransform MenuButton(string title, int x, int y, int width, int height, bool primary, Action action)
        {
            bool enabled = action != null;
            Color32 fill = primary ? Pal.Orange : enabled ? Pal.Plum : Pal.Alpha(Pal.PlumDeep, 178);
            RectTransform row = Px.Box(menu, "Btn_" + title, x, y, width, height,
                enabled ? Pal.White : fill, primary ? Pal.Yellow : Pal.PlumLight);
            if (!enabled) return row;

            Image surface = row.GetComponent<Image>();
            surface.raycastTarget = true;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = primary ? Pal.Yellow : Pal.PlumLine;
            colors.pressedColor = primary ? Pal.Hex("#d87731") : Pal.PlumDeep;
            colors.fadeDuration = 0;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return row;
        }
    }
}
