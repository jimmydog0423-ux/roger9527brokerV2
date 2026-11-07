using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>彈出視窗：遊戲選單、設定、確認對話框。永遠在最上層。</summary>
    public class MenuViews : MonoBehaviour
    {
        RectTransform rt;
        GameRoot game;
        Transform layer;
        Action onBack;

        public bool IsOpen { get { return layer.childCount > 0 && gameObject.activeSelf && open; } }
        bool open;

        public static MenuViews Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Menus", 0, 0, 640, 360);
            MenuViews v = r.gameObject.AddComponent<MenuViews>();
            v.rt = r;
            v.game = game;
            v.layer = Px.Rect(r, "Layer", 0, 0, 640, 360);
            return v;
        }

        public void Close()
        {
            Px.Clear(layer);
            open = false;
        }

        RectTransform Panel(string title, float w, float h)
        {
            Px.Clear(layer);
            open = true;
            rt.SetAsLastSibling();
            Image scrim = Px.Fill(layer, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 200));
            scrim.raycastTarget = true;
            float x = Mathf.Floor((640 - w) * 0.5f);
            float y = Mathf.Floor((360 - h) * 0.5f);
            Px.Fill(layer, x + 3, y + 3, w, h, Pal.Ink);
            RectTransform p = ConsoleArt.Panel(layer, "Panel", x, y, w, h, Pal.Plum, Pal.Orange);
            ConsoleArt.Header(p, w);
            Px.Label(p, title, 8, 2, w - 16, 14, Pal.Orange);
            return p;
        }

        public void ShowPause(string lessonTitle)
        {
            bool career = game.Career != null;
            RectTransform p = Panel("選單", 220, 176);
            Px.Label(p, string.IsNullOrEmpty(lessonTitle) ? "" : "目前：" + lessonTitle, 10, 22, 200, 14, Pal.Lav);
            ConsoleArt.Key(p, "繼續遊戲", 30, 44, 160, 20, Pal.PlumLine, Pal.Cream, Pal.PlumLight, Close);
            ConsoleArt.Key(p, "設定", 30, 70, 160, 20, Pal.PlumLine, Pal.Cream, Pal.PlumLight, () => ShowSettings(() => ShowPause(lessonTitle)));
            ConsoleArt.Key(p, "回到標題", 30, 96, 160, 20, Pal.PlumLine, Pal.Cream, Pal.PlumLight,
                () => Confirm(career ? "回到標題？進度會回到最後一次自動存檔。" : "回到標題？這一課的進度會回到本課開頭。", () => { Close(); game.BackToTitle(); }, () => ShowPause(lessonTitle)));
#if !UNITY_WEBGL || UNITY_EDITOR
            ConsoleArt.Key(p, "離開遊戲", 30, 122, 160, 20, Pal.Red, Pal.Cream, Pal.Ink,
                () => Confirm(career ? "離開遊戲？進度會回到最後一次自動存檔。" : "離開遊戲？這一課的進度會回到本課開頭。", () => game.Quit(), () => ShowPause(lessonTitle)));
#endif
            Px.Label(p, career ? "每天開始、每通電話後自動存檔" : "存檔會在每一課開頭自動保存", 10, 152, 200, 14, Pal.PlumLight);
        }

        public void ShowSettings(Action back)
        {
            onBack = back;
            RectTransform p = Panel("設定", 300, 160);
            Px.Label(p, "顯示模式", 12, 26, 80, 14, Pal.Cream);
            Choice(p, 100, 24, "視窗", !Settings.Fullscreen, () => { Settings.Fullscreen = false; Settings.ApplyDisplay(); ShowSettings(back); });
            Choice(p, 172, 24, "全螢幕", Settings.Fullscreen, () => { Settings.Fullscreen = true; Settings.ApplyDisplay(); ShowSettings(back); });

            Px.Label(p, "像素縮放", 12, 52, 80, 14, Pal.Cream);
            Choice(p, 100, 50, "整數倍", Settings.IntegerScaling, () => { Settings.IntegerScaling = true; game.ApplyScaling(); ShowSettings(back); });
            Choice(p, 172, 50, "盡量放大", !Settings.IntegerScaling, () => { Settings.IntegerScaling = false; game.ApplyScaling(); ShowSettings(back); });

            Px.Label(p, "對話速度", 12, 78, 80, 14, Pal.Cream);
            string[] names = { "慢", "中", "快", "立即" };
            for (int i = 0; i < 4; i++)
            {
                int v = i;
                Choice(p, 100 + i * 48, 76, names[i], Settings.TextSpeed == i, () => { Settings.TextSpeed = v; ShowSettings(back); }, 44);
            }
            Px.Label(p, "整數倍優先保留方正像素，太小時會放大。\n盡量放大會用滿可用畫面。", 12, 104, 280, 28, Pal.Lav, TextAnchor.UpperLeft, true);
            ConsoleArt.Key(p, "返回", 110, 136, 80, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => { if (onBack != null) onBack(); else Close(); });
        }

        void Choice(RectTransform p, float x, float y, string label, bool on, Action act, float w = 68)
        {
            ConsoleArt.Key(p, label, x, y, w, 18, on ? Pal.Orange : Pal.PlumDeep, on ? Pal.Ink : Pal.Cream, on ? Pal.Ink : Pal.PlumLight, act);
        }

        public void Confirm(string text, Action yes, Action no)
        {
            RectTransform p = Panel("確認", 280, 100);
            Px.Label(p, text, 12, 24, 256, 42, Pal.Cream, TextAnchor.UpperLeft, true);
            ConsoleArt.Key(p, "確定", 50, 72, 80, 18, Pal.Orange, Pal.Ink, Pal.Ink, yes);
            ConsoleArt.Key(p, "取消", 150, 72, 80, 18, Pal.PlumLine, Pal.Cream, Pal.PlumLight, () => { if (no != null) no(); else Close(); });
        }

        /// <summary>兩個選項的對話框。</summary>
        public void Choose(string title, string text, string a, Action actA, string b, Action actB)
        {
            RectTransform p = Panel(title, 300, 130);
            Px.Label(p, text, 12, 24, 276, 42, Pal.Cream, TextAnchor.UpperLeft, true);
            ConsoleArt.Key(p, a, 20, 70, 260, 20, Pal.Orange, Pal.Ink, Pal.Ink, () => { Close(); if (actA != null) actA(); });
            ConsoleArt.Key(p, b, 20, 96, 260, 20, Pal.PlumLine, Pal.Cream, Pal.PlumLight, () => { Close(); if (actB != null) actB(); });
        }

        public void Message(string title, string text, Action ok)
        {
            // 依文字行數決定視窗高度，錯誤訊息比較長也不會被切掉
            float th = Mathf.Clamp(CountLines(text, 276) * 14, 56, 252);
            RectTransform p = Panel(title, 300, 54 + th);
            Px.Label(p, text, 12, 24, 276, th, Pal.Cream, TextAnchor.UpperLeft, true);
            ConsoleArt.Key(p, "好", 110, 28 + th, 80, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => { Close(); if (ok != null) ok(); });
        }

        /// <summary>估算自動換行後有幾行。</summary>
        static int CountLines(string text, float width)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            int lines = 0;
            foreach (string para in text.Split('\n'))
            {
                lines++;
                int w = 0;
                for (int i = 0; i < para.Length; i++)
                {
                    int cw = Px.TextWidth(para[i].ToString());
                    if (w + cw > width)
                    {
                        lines++;
                        w = 0;
                    }
                    w += cw;
                }
            }
            return lines;
        }
    }
}
