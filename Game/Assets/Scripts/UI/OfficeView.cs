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

        const int ScreenWidth = 240;
        const int ScreenHeight = 135;
        const int DeskTagY = 208;
        public const float ZoomMax = PixelScaler.Width / (float)ScreenWidth;
        static readonly Rect MonitorBounds = new Rect(192, 44, 256, 151);
        static readonly Rect ScreenBounds = new Rect(200, 52, ScreenWidth, ScreenHeight);
        static readonly Rect PopupBounds = new Rect(45, 24, 150, 86);
        static readonly Rect HandbookBounds = new Rect(50, 244, 101, 56);
        static readonly Vector2 ZoomCenter = ScreenBounds.position * (ZoomMax / (ZoomMax - 1f));

        RectTransform rt;
        GameRoot game;
        Text hudDay, hudCash, hudObjective;
        Image popupFrame, led;
        RectTransform popup, handbookTag, trayTag;
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
            Image room = Px.Fill(rt, 0, 0, PixelScaler.Width, PixelScaler.Height, Pal.Night);
            Sprite sprite = Resources.Load<Sprite>("Office/broker-room-v2");
            if (sprite != null)
            {
                room.sprite = sprite;
                room.color = Color.white;
                OfficeRain.Build(rt);
            }

            Px.Fill(rt, 0, 0, PixelScaler.Width, 14, Pal.Ink);
            hudDay = Px.Label(rt, "", 8, 0, 90, 14, Pal.Cream);
            hudCash = Px.Label(rt, "", 100, 0, 160, 14, Pal.Cream);
            hudObjective = Px.Label(rt, "", 250, 0, 340, 14, Pal.Orange, TextAnchor.UpperRight, false);
            Px.Button(rt, "選單", 598, 0, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());

            RectTransform sign = Px.Box(rt, "Plate", 267, 20, 106, 19, Pal.Hex("#261e2d"), Pal.Hex("#735049"));
            Px.Label(sign, "水蛇洞經紀人", 0, 2, 106, 14, Pal.Cream, TextAnchor.UpperCenter, false);

            if (sprite != null)
            {
                handbookTag = BuildDeskTag("工作手冊", 101, Pal.Yellow);
                trayTag = BuildDeskTag("待辦", 531, Pal.TealLight);
            }

            BuildMonitor();
            SetCall(Call.None, null);
        }

        RectTransform BuildDeskTag(string text, int centerX, Color32 accent)
        {
            int width = Px.TextWidth(text) + 18;
            RectTransform tag = Px.Rect(rt, "DeskTag_" + text, centerX - width / 2, DeskTagY, width, 22);
            Px.Fill(tag, 2, 2, width, 18, Pal.Alpha(Pal.Ink, 170));
            Px.Box(tag, "Plate", 0, 0, width, 18, Pal.PlumDeep, accent);
            Px.Label(tag, text, 0, 2, width, 14, Pal.Cream, TextAnchor.MiddleCenter, false);
            Px.Fill(tag, width / 2 - 3, 18, 7, 1, accent);
            Px.Fill(tag, width / 2 - 2, 19, 5, 1, accent);
            Px.Fill(tag, width / 2 - 1, 20, 3, 1, accent);
            Px.Fill(tag, width / 2, 21, 1, 1, accent);
            return tag;
        }

        static void FloatDeskTag(RectTransform tag, float phase)
        {
            if (tag == null) return;
            // 以整個邏輯像素移動，讓標牌和文字保持銳利。
            float y = -DeskTagY + Mathf.Round(Mathf.Sin(phase) * 3f);
            Vector2 position = tag.anchoredPosition;
            if (position.y != y) tag.anchoredPosition = new Vector2(position.x, y);
        }

        void BuildMonitor()
        {
            // 支架和桌面的接觸陰影。
            Px.Fill(rt, 275, 238, 93, 5, Pal.Alpha(Pal.Ink, 150));
            Px.Fill(rt, 305, 192, 30, 45, Pal.Hex("#251e28"));
            Px.Fill(rt, 305, 194, 3, 40, Pal.Hex("#42303b"));
            Px.Box(rt, "Stand", 278, 232, 84, 9, Pal.Hex("#2f252f"), Pal.Ink);
            Px.Fill(rt, 281, 233, 78, 1, Pal.Hex("#59404a"));

            Px.Fill(rt, 195, 48, 256, 151, Pal.Alpha(Pal.Ink, 112));
            RectTransform bezel = Px.Box(rt, "Bezel", MonitorBounds.x, MonitorBounds.y,
                MonitorBounds.width, MonitorBounds.height, Pal.Hex("#2f2633"), Pal.Ink);
            Px.Fill(bezel, 1, 1, 254, 2, Pal.Hex("#58404f"));
            Px.Fill(bezel, 1, 3, 2, 146, Pal.Hex("#463443"));
            Px.Fill(bezel, 7, 7, 242, 137, Pal.Ink);
            Px.Fill(bezel, 8, 145, 240, 1, Pal.Hex("#4b3745"));
            led = Px.Fill(rt, 316, 190, 8, 2, Pal.Hex("#2a2833"));

            Image screen = Px.Fill(rt, ScreenBounds.x, ScreenBounds.y, ScreenBounds.width, ScreenBounds.height, Pal.Night);
            screen.name = "Screen";
            RectTransform contents = screen.rectTransform;
            Px.Fill(contents, 0, 0, ScreenWidth, 6, Pal.Ink);
            Px.Fill(contents, 2, 2, 2, 2, Pal.Orange);
            for (int i = 0; i < 3; i++) Px.Box(contents, "Icon", 8, 14 + i * 24, 16, 16, Pal.PlumLine, Pal.PlumLight);
            idleText = Px.Label(contents, "目前沒有來電", 30, 60, 210, 14, Pal.Lav, TextAnchor.UpperCenter, false);

            popup = Px.Rect(contents, "Popup", PopupBounds.x, PopupBounds.y, PopupBounds.width, PopupBounds.height);
            Image pbg = popup.gameObject.AddComponent<Image>();
            pbg.color = Pal.Plum;
            pbg.raycastTarget = false;
            popupFrame = Px.Frame(popup, 0, 0, PopupBounds.width, PopupBounds.height, Pal.PlumLight, 2);
            popupBody = Px.Rect(popup, "Body", 0, 0, PopupBounds.width, PopupBounds.height);
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
                case "office_monitor": r = MonitorBounds; return true;
                case "office_popup":
                    r = PopupBounds;
                    r.position += ScreenBounds.position;
                    return true;
                case "office_handbook": r = HandbookBounds; return true;
                case "office_menu": r = new Rect(598, 0, 40, 14); return true;
            }
            return false;
        }

        void Update()
        {
            float phase = Time.unscaledTime * (2f * Mathf.PI / 2.8f);
            FloatDeskTag(handbookTag, phase);
            FloatDeskTag(trayTag, phase + 0.85f);

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
