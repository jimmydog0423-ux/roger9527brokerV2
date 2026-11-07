using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 晚上的街道：上面是夜景和一排店面，點店面看說明，按「前往」花一段時間過去。
    /// 右下角「回家睡覺」結束這一晚；跑越多地方，隔天越容易睡過頭。
    /// </summary>
    public class NightView : MonoBehaviour
    {
        const float FrontY = 112f;
        const float FrontW = 66f;
        const float FrontStep = 70f;
        const float FrontH = 120f;

        RectTransform rt;
        GameRoot game;
        CanvasGroup group;
        Transform top, fronts, panel, modal;
        ShopView shop;
        JunkView junk;
        string selected = "office";

        static readonly string[] SignBg = { "#57497a", "#3fa7a0", "#e8c35a", "#8a8494", "#a8352d", "#4a6b8a", "#e8893a", "#c9a442", "#9d50c4" };

        public bool Visible { get { return group.alpha > 0.5f; } }

        public static NightView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Night", 0, 0, 640, 360);
            NightView v = r.gameObject.AddComponent<NightView>();
            v.rt = r;
            v.game = game;
            v.group = r.gameObject.AddComponent<CanvasGroup>();
            v.BuildStatic();
            v.Show(false);
            return v;
        }

        public void Show(bool on)
        {
            group.alpha = on ? 1f : 0f;
            group.blocksRaycasts = on;
            group.interactable = on;
            if (!on)
            {
                Px.Clear(modal);
                if (shop != null) shop.Hide();
                if (junk != null) junk.Hide();
            }
            if (on)
            {
                selected = "office";
                Refresh();
            }
        }

        void BuildStatic()
        {
            Image bg = Px.Fill(rt, 0, 0, 640, 360, Pal.Hex("#0e0c18"));
            bg.raycastTarget = true;
            NightArt.Paint(bg, "street");
            Px.Fill(rt, 0, 0, 640, 18, Pal.Ink);
            Px.Fill(rt, 0, 18, 640, 1, Pal.PlumLine);
            top = Px.Rect(rt, "Top", 0, 0, 640, 18);
            fronts = Px.Rect(rt, "Fronts", 0, 0, 640, 360);
            panel = Px.Rect(rt, "Panel", 0, 0, 640, 360);
            modal = Px.Rect(rt, "Modal", 0, 0, 640, 360);
            shop = ShopView.Build(rt, game, RefreshTop);
            junk = JunkView.Build(rt, game, RefreshTop);
        }

        /// <summary>進電子街或跳蚤市場：貨架式的商店畫面。</summary>
        public void ShowShop(string id)
        {
            Px.Clear(modal);
            shop.Show(id, Refresh);
        }

        /// <summary>進垃圾場挖寶。</summary>
        public void ShowJunk()
        {
            Px.Clear(modal);
            junk.Show(Refresh);
        }

        Career C { get { return game.Career; } }

        public void Refresh()
        {
            if (C == null) return;
            RefreshTop();
            RefreshFronts();
            RefreshPanel();
        }

        void RefreshTop()
        {
            Px.Clear(top);
            GameState s = game.State;
            Px.Label(top, Calendar.Short(s.day - 1) + " 的晚上", 6, 2, 120, 14, Pal.Cream);
            Px.Label(top, "剩餘時間", 130, 2, 56, 14, Pal.Lav);
            for (int i = 0; i < Career.NightActions; i++) Px.Fill(top, 190 + i * 10, 6, 7, 7, i < s.nightLeft ? Pal.Orange : Pal.PlumLine);
            Px.Label(top, "疲勞", 236, 2, 30, 14, Pal.Lav);
            for (int i = 0; i < 6; i++) Px.Fill(top, 268 + i * 8, 6, 6, 7, i < s.fatigue ? (s.fatigue >= 4 ? Pal.Red : Pal.Yellow) : Pal.PlumLine);
            Px.Label(top, "現金 " + GameState.Money(s.cash), 330, 2, 170, 14, Pal.Cream);
            ConsoleArt.Key(top, "選單", 596, 2, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());
        }

        void RefreshFronts()
        {
            Px.Clear(fronts);
            for (int i = 0; i < Career.Places.Length; i++)
            {
                NightPlace p = Career.Places[i];
                float x = 4 + i * FrontStep;
                string why;
                bool can = C.CanVisit(p, out why);
                bool locked = p.act > game.State.act;
                bool visited = game.State.nightVisited.Contains(p.id) && !p.repeat;
                RectTransform f = Px.Box(fronts, "Front_" + p.id, x, FrontY, FrontW, FrontH, Pal.PlumDeep, Pal.Ink);
                string pid = p.id;
                Px.MakeClickable(f, () => { selected = pid; RefreshFronts(); RefreshPanel(); });
                Image facade = Px.Fill(f, 1, 1, FrontW - 2, FrontH - 2, Pal.Plum);
                NightArt.Paint(facade, "Fronts/" + p.id);
                if (locked || visited || !can) facade.color = Pal.Hex("#8c819d");
                Color32 sign = locked ? Pal.PlumLight : Pal.Hex(SignBg[i % SignBg.Length]);
                Px.Fill(f, 3, 5, FrontW - 6, 20, Pal.Alpha(Pal.Ink, 232));
                Px.Fill(f, 4, 5, FrontW - 8, 1, sign);
                Px.Fill(f, 4, 24, FrontW - 8, 1, Pal.Alpha(sign, 154));
                Px.Label(f, p.sign, 3, 8, FrontW - 6, 14, locked ? Pal.Lav : Pal.Cream, TextAnchor.UpperCenter, false);
                // 今晚有活動的店，掛一張特價牌
                if (!locked && !string.IsNullOrEmpty(C.EventText(p.id)))
                {
                    Px.Fill(f, FrontW - 30, 24, 28, 12, Pal.Pink);
                    Px.Label(f, "活動", FrontW - 30, 23, 28, 14, Pal.Ink, TextAnchor.UpperCenter, false);
                }
                if (locked)
                {
                    Px.Fill(f, 4, 58, FrontW - 8, 14, Pal.Alpha(Pal.Ink, 220));
                    Px.Label(f, why, 4, 58, FrontW - 8, 14, Pal.Lav, TextAnchor.UpperCenter, false);
                }
                else if (visited)
                {
                    Px.Fill(f, 4, 58, FrontW - 8, 14, Pal.Alpha(Pal.Ink, 220));
                    Px.Label(f, "去過了", 4, 58, FrontW - 8, 14, Pal.Lav, TextAnchor.UpperCenter, false);
                }
                if (selected == p.id) Px.Frame(fronts, x - 2, FrontY - 2, FrontW + 4, FrontH + 4, Pal.Orange, 2);
            }
        }

        void RefreshPanel()
        {
            Px.Clear(panel);
            GameState s = game.State;
            RectTransform b = ConsoleArt.Panel(panel, "Info", 4, 258, 632, 98, Pal.Plum, Pal.PlumLight);
            NightPlace p = Career.Place(selected) ?? Career.Places[0];
            ConsoleArt.Header(b, 632, 2, 16);
            Px.Label(b, p.name, 8, 4, 140, 14, Pal.Orange);
            string ev = C.EventText(p.id);
            if (!string.IsNullOrEmpty(ev)) Px.Label(b, "今晚活動：" + ev, 150, 4, 284, 14, Pal.Yellow);
            Px.Label(b, p.desc, 8, 20, 420, 28, Pal.Cream, TextAnchor.UpperLeft, true);
            Px.Label(b, "花 1 段時間・疲勞 +" + (p.office ? 1 : 2) + (p.repeat ? "・可以重複來" : "・一晚一次"), 8, 50, 420, 14, Pal.Lav);
            string why;
            bool can = C.CanVisit(p, out why);
            ConsoleArt.Key(b, can ? "前往" : why, 8, 72, 150, 20, can ? Pal.Orange : Pal.PlumLine, can ? Pal.Ink : Pal.Lav, Pal.Ink,
                () => { if (can) game.NightVisit(p.id); });

            Px.Fill(b, 440, 6, 1, 86, Pal.PlumLight);
            int pct = Mathf.RoundToInt((float)(C.OversleepChance * 100));
            bool none = s.nightVisited.Count == 0;
            Px.Label(b, none ? "現在回家：早點睡" : "明天睡過頭的機率", 448, 4, 180, 14, Pal.Lav);
            Px.Label(b, none ? "明天客人耐心 +1" : pct + "%", 448, 20, 180, 14, none ? Pal.TealLight : (pct >= 30 ? Pal.Pink : Pal.Cream));
            Px.Label(b, "睡過頭會錯過一通電話", 448, 36, 180, 14, Pal.PlumLight);
            ConsoleArt.Key(b, "回家睡覺", 448, 56, 176, 36, Pal.TealDark, Pal.Cream, Pal.Ink, () => game.GoToBed());
        }

        // ------------------------------------------------------------------
        // 地點裡發生的事
        // ------------------------------------------------------------------

        public void ShowScene(NightScene sc, string result)
        {
            Px.Clear(modal);
            Image backdrop = Px.Fill(modal, 0, 0, 640, 360, Pal.Night);
            bool illustrated = NightArt.Paint(backdrop, "Venues/" + sc.id);
            if (!illustrated) backdrop.gameObject.SetActive(false);
            Image scrim = Px.Fill(modal, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, illustrated ? (byte)86 : (byte)200));
            scrim.raycastTarget = true;
            int n = Mathf.Min(8, sc.options.Count);
            float h = 116 + n * 22 + 34;
            float y0 = Mathf.Floor((360 - h) * 0.5f);
            Px.Fill(modal, 93, y0 + 3, 460, h, Pal.Ink);
            RectTransform p = ConsoleArt.Panel(modal, "Scene", 90, y0, 460, h, Pal.Plum, Pal.Orange);
            ConsoleArt.Header(p, 460);
            Px.Label(p, sc.title, 8, 2, 300, 14, Pal.Orange);
            Px.Label(p, "現金 " + GameState.Money(game.State.cash), 250, 2, 202, 14, Pal.Cream, TextAnchor.UpperRight, false);
            Px.Label(p, sc.text, 10, 22, 440, 42, Pal.Cream, TextAnchor.UpperLeft, true);
            if (!string.IsNullOrEmpty(result))
            {
                Px.Fill(p, 8, 66, 444, 46, Pal.PlumDeep);
                Px.Label(p, result, 12, 68, 436, 42, Pal.Yellow, TextAnchor.UpperLeft, true);
            }
            for (int i = 0; i < n; i++)
            {
                NightOption o = sc.options[i];
                float y = 116 + i * 22;
                NightOption opt = o;
                NightScene scene = sc;
                ConsoleArt.Key(p, o.label, 10, y, 440, 20, o.enabled ? Pal.PlumLine : Pal.PlumDeep, o.enabled ? Pal.Cream : Pal.PlumLight, o.enabled ? Pal.PlumLight : Pal.PlumLine,
                    () => Choose(scene, opt));
            }
            ConsoleArt.Key(p, "離開", 350, h - 28, 100, 20, Pal.Orange, Pal.Ink, Pal.Ink, () => { Px.Clear(modal); Refresh(); });
        }

        void Choose(NightScene sc, NightOption o)
        {
            if (!o.enabled || o.act == null)
            {
                ShowScene(sc, o.note);
                return;
            }
            string res = o.act();
            game.SaveNow();
            RefreshTop();
            if (sc.multi) ShowScene(C.Scene(sc.id), res);
            else
            {
                NightScene done = new NightScene { id = sc.id, title = sc.title, text = sc.text };
                ShowScene(done, res);
            }
        }
    }
}
