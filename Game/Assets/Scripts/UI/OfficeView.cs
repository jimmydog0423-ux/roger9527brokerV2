using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 辦公室畫面：正前方是電腦，來電時螢幕上的視窗會閃。
    /// 接聽後整個辦公室放大，讓電腦螢幕剛好填滿畫面。
    /// </summary>
    public class OfficeView : MonoBehaviour
    {
        public enum Call { None, Ringing, Active, Missed, Ended }

        // 螢幕在辦公室裡的位置：(200, 52) 大小 240x135，放大 640/240 倍剛好填滿。
        public const float ZoomMax = 640f / 240f;
        static readonly Vector2 ZoomCenter = new Vector2(320f, 83.2f);

        RectTransform rt;
        GameRoot game;
        Text hudDay, hudCash, hudObjective;
        Image popupFrame, led;
        RectTransform popup;
        Transform popupBody;
        Text idleText;
        Call call = Call.None;
        float blinkTimer;
        bool blinkOn;

        public static OfficeView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Office", 0, 0, 640, 360);
            OfficeView v = r.gameObject.AddComponent<OfficeView>();
            v.rt = r;
            v.game = game;
            v.BuildArt();
            return v;
        }

        public void Bind(GameState state)
        {
            hudDay.text = "第 " + state.day + " 天・" + state.Weekday;
            hudCash.text = "現金 " + GameState.Money(state.cash);
        }

        public void SetObjective(string text)
        {
            hudObjective.text = string.IsNullOrEmpty(text) ? "" : "目標：" + text;
        }

        void BuildArt()
        {
            // 上方狀態列
            Px.Fill(rt, 0, 0, 640, 14, Pal.Ink);
            hudDay = Px.Label(rt, "", 8, 0, 90, 14, Pal.Cream);
            hudCash = Px.Label(rt, "", 100, 0, 160, 14, Pal.Cream);
            hudObjective = Px.Label(rt, "", 250, 0, 340, 14, Pal.Orange, TextAnchor.UpperRight, false);
            Px.Button(rt, "選單", 598, 0, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());

            // 牆
            Px.Fill(rt, 0, 14, 640, 200, Pal.Hex("#48585a"));
            Px.Fill(rt, 0, 200, 640, 14, Pal.Hex("#3f4d4f"));

            // 窗戶
            Px.Fill(rt, 24, 34, 124, 106, Pal.Hex("#2b3436"));
            Px.Fill(rt, 28, 38, 116, 98, Pal.Hex("#1e2a44"));
            Px.Fill(rt, 28, 90, 26, 46, Pal.Hex("#2c3a5a"));
            Px.Fill(rt, 56, 74, 22, 62, Pal.Hex("#34436a"));
            Px.Fill(rt, 80, 98, 30, 38, Pal.Hex("#2c3a5a"));
            Px.Fill(rt, 112, 82, 32, 54, Pal.Hex("#34436a"));
            int[,] lights = { { 34, 98 }, { 42, 110 }, { 62, 82 }, { 62, 114 }, { 100, 118 }, { 120, 90 }, { 120, 122 } };
            for (int i = 0; i < lights.GetLength(0); i++) Px.Fill(rt, lights[i, 0], lights[i, 1], 4, 4, Pal.Yellow);
            int[,] off = { { 70, 98 }, { 90, 106 }, { 130, 106 } };
            for (int i = 0; i < off.GetLength(0); i++) Px.Fill(rt, off[i, 0], off[i, 1], 4, 4, Pal.Hex("#4a5a80"));
            Px.Fill(rt, 84, 38, 4, 98, Pal.Hex("#2b3436"));
            Px.Fill(rt, 28, 84, 116, 4, Pal.Hex("#2b3436"));
            Px.Fill(rt, 20, 140, 132, 6, Pal.Hex("#3b4849"));

            // 公司名牌
            Px.Box(rt, "Plate", 268, 18, 104, 20, Pal.Plum, Pal.Ink);
            Px.Label(rt, "水蛇洞經紀人", 268, 21, 104, 14, Pal.Cream, TextAnchor.UpperCenter, false);

            // 書架
            Px.Fill(rt, 484, 34, 132, 116, Pal.Hex("#573b2f"));
            Px.Fill(rt, 488, 38, 124, 108, Pal.Hex("#3b2a24"));
            Px.Fill(rt, 488, 76, 124, 4, Pal.Hex("#6b4a3a"));
            Px.Fill(rt, 488, 114, 124, 4, Pal.Hex("#6b4a3a"));
            Px.Fill(rt, 494, 46, 10, 30, Pal.Red);
            Px.Fill(rt, 505, 44, 10, 32, Pal.Teal);
            Px.Fill(rt, 516, 48, 10, 28, Pal.Yellow);
            Px.Fill(rt, 527, 45, 10, 31, Pal.PlumLight);
            Px.Fill(rt, 550, 56, 32, 20, Pal.Hex("#a07a52"));
            Px.Fill(rt, 564, 56, 4, 20, Pal.Hex("#c9a774"));
            Px.Fill(rt, 586, 62, 20, 14, Pal.Hex("#8a6844"));
            Px.Fill(rt, 496, 92, 28, 22, Pal.Hex("#2a2833"));
            Px.Fill(rt, 504, 96, 12, 12, Pal.PlumLight);
            Px.Fill(rt, 508, 100, 4, 4, Pal.Lav);
            Px.Fill(rt, 540, 98, 4, 16, Pal.Hex("#2a2833"));
            Px.Fill(rt, 536, 86, 12, 14, Pal.Hex("#4a4858"));
            Px.Fill(rt, 560, 98, 24, 16, Pal.Hex("#a07a52"));
            Px.Fill(rt, 588, 102, 18, 12, Pal.Hex("#8a6844"));
            Px.Fill(rt, 494, 132, 60, 14, Pal.Hex("#cdbd96"));
            Px.Fill(rt, 498, 126, 56, 6, Pal.Hex("#e9dcbc"));
            Px.Fill(rt, 570, 124, 36, 22, Pal.Hex("#8a6844"));

            // 桌子
            Px.Fill(rt, 0, 214, 640, 122, Pal.Hex("#6b4a3a"));
            Px.Fill(rt, 0, 214, 640, 3, Pal.Hex("#573b2f"));
            Px.Fill(rt, 0, 336, 640, 24, Pal.Hex("#4d3329"));
            Px.Fill(rt, 0, 336, 640, 2, Pal.Hex("#7d5843"));

            // 螢幕支架與外框
            Px.Fill(rt, 304, 195, 32, 19, Pal.Hex("#2a2833"));
            Px.Fill(rt, 272, 212, 96, 8, Pal.Hex("#2a2833"));
            Px.Fill(rt, 272, 212, 96, 2, Pal.Hex("#3a3846"));
            Px.Box(rt, "Bezel", 192, 44, 256, 151, Pal.Hex("#1b1a24"), Pal.Hex("#0f0d15"));
            led = Px.Fill(rt, 316, 190, 8, 2, Pal.Hex("#2a2833"));

            // 螢幕內容
            RectTransform screen = Px.Rect(rt, "Screen", 200, 52, 240, 135);
            Image sbg = screen.gameObject.AddComponent<Image>();
            sbg.color = Pal.Night;
            sbg.raycastTarget = false;
            Px.Fill(screen, 0, 0, 240, 6, Pal.Ink);
            Px.Fill(screen, 2, 2, 2, 2, Pal.Orange);
            for (int i = 0; i < 3; i++) Px.Box(screen, "Icon", 8, 14 + i * 24, 16, 16, Pal.PlumLine, Pal.PlumLight);
            idleText = Px.Label(screen, "目前沒有來電", 30, 60, 210, 14, Pal.Lav, TextAnchor.UpperCenter, false);

            popup = Px.Rect(screen, "Popup", 45, 24, 150, 86);
            Image pbg = popup.gameObject.AddComponent<Image>();
            pbg.color = Pal.Plum;
            pbg.raycastTarget = false;
            popupFrame = Px.Frame(popup, 0, 0, 150, 86, Pal.PlumLight, 2);
            popupBody = Px.Rect(popup, "Body", 0, 0, 150, 86);

            // 鍵盤、滑鼠
            Px.Box(rt, "Keyboard", 236, 234, 168, 28, Pal.Hex("#2a2833"), Pal.Ink);
            Px.Tiled(rt, 240, 238, 160, 6, PixelSprites.Keys, Pal.White);
            Px.Tiled(rt, 240, 246, 160, 6, PixelSprites.Keys, Pal.White);
            Px.Fill(rt, 264, 254, 112, 4, Pal.Hex("#4a4858"));
            Px.Box(rt, "Mouse", 418, 237, 14, 22, Pal.Hex("#2a2833"), Pal.Ink);

            // 鬼東工作手冊
            Px.Fill(rt, 57, 299, 91, 3, Pal.Hex("#e9dcbc"));
            Px.Box(rt, "Handbook", 56, 234, 92, 66, Pal.Hex("#2c3a5a"), Pal.Ink);
            Px.Fill(rt, 57, 235, 7, 64, Pal.Hex("#1f2a44"));
            Px.Fill(rt, 148, 242, 4, 8, Pal.Orange);
            Px.Fill(rt, 148, 254, 4, 8, Pal.Teal);
            Px.Fill(rt, 148, 266, 4, 8, Pal.Yellow);
            Px.Box(rt, "Label", 62, 250, 82, 18, Pal.Hex("#e9dcbc"), Pal.Ink);
            Px.Label(rt, "鬼東工作手冊", 62, 252, 82, 14, Pal.PaperInk, TextAnchor.UpperCenter, false);

            // 杯子
            Px.Box(rt, "Mug", 168, 236, 20, 26, Pal.Hex("#e9dcbc"), Pal.Ink);
            Px.Fill(rt, 170, 238, 16, 4, Pal.Hex("#3b2a24"));
            Px.Frame(rt, 187, 242, 8, 12, Pal.Hex("#e9dcbc"), 2);

            // 待辦文件盤
            Px.Box(rt, "Tray", 472, 238, 108, 54, Pal.PlumLight, Pal.Ink);
            Px.Box(rt, "Papers", 480, 232, 88, 40, Pal.Cream, Pal.Hex("#b3a27a"));
            Px.Box(rt, "TrayLabel", 488, 270, 60, 16, Pal.Plum, Pal.Ink);
            Px.Label(rt, "待辦", 488, 271, 60, 14, Pal.Cream, TextAnchor.UpperCenter, false);

            SetCall(Call.None, null);
        }

        public void SetCall(Call c, CaseData d)
        {
            call = c;
            Px.Clear(popupBody);
            bool show = c != Call.None && d != null;
            popup.gameObject.SetActive(show);
            idleText.gameObject.SetActive(!show);
            popupFrame.color = Pal.PlumLight;
            led.color = Pal.Hex("#2a2833");
            if (!show) return;

            Color32 headColor = Pal.Orange;
            string head = "視訊來電";
            if (c == Call.Active) { head = "通話中"; headColor = Pal.TealLight; }
            else if (c == Call.Ended) { head = "通話結束"; headColor = Pal.Lav; }
            else if (c == Call.Missed) { head = "未接來電"; headColor = Pal.Lav; }
            Px.Label(popupBody, head, 6, 3, 100, 14, headColor);
            Px.Fill(popupBody, 3, 19, 26, 30, Pal.VideoWall);
            PortraitArt.Draw(popupBody, d.client.id, 4, 21, 1);
            Px.Label(popupBody, d.client.name, 35, 20, 110, 14, Pal.Cream);
            Px.Label(popupBody, d.client.company, 35, 34, 110, 14, Pal.Lav);
            if (c == Call.Ringing)
            {
                Px.Button(popupBody, "接聽", 4, 58, 66, 20, Pal.Teal, Pal.Ink, Pal.Ink, () => game.Answer());
                Px.Button(popupBody, "拒接", 76, 58, 66, 20, Pal.Red, Pal.Cream, Pal.Ink, () => game.Decline());
            }
            else
            {
                string label = c == Call.Missed ? "回撥" : "回到電腦";
                Px.Button(popupBody, label, 4, 58, 138, 20, Pal.Teal, Pal.Ink, Pal.Ink, () => game.Answer());
            }
        }

        /// <summary>0 = 一般辦公室，1 = 螢幕填滿畫面。</summary>
        public void SetZoom(float e)
        {
            float s = 1f + (ZoomMax - 1f) * e;
            rt.localScale = new Vector3(s, s, 1f);
            Vector2 p = ZoomCenter * (1f - s);
            rt.anchoredPosition = new Vector2(p.x, -p.y);
        }

        public bool TryFocus(string key, out Rect r)
        {
            r = new Rect();
            switch (key)
            {
                case "office_monitor": r = new Rect(192, 44, 256, 151); return true;
                case "office_popup": r = new Rect(245, 76, 150, 86); return true;
                case "office_handbook": r = new Rect(56, 234, 92, 66); return true;
                case "office_menu": r = new Rect(598, 0, 40, 14); return true;
            }
            return false;
        }

        void Update()
        {
            if (call != Call.Ringing) return;
            blinkTimer += Time.unscaledDeltaTime;
            if (blinkTimer < 0.4f) return;
            blinkTimer = 0f;
            blinkOn = !blinkOn;
            popupFrame.color = blinkOn ? Pal.Orange : Pal.PlumLight;
            led.color = blinkOn ? Pal.Orange : Pal.Hex("#2a2833");
        }
    }
}
