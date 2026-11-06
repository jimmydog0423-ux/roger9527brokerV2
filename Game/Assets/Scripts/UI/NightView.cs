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

        static readonly string[] Facade = { "#3a3f58", "#1f3a44", "#4a3a2c", "#3a3628", "#6b2a24", "#24304a", "#5a2420", "#4a3a24", "#2a2040" };
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
            // 夜空
            Px.Fill(rt, 0, 18, 640, 60, Pal.Hex("#151229"));
            Px.Fill(rt, 0, 78, 640, 40, Pal.Hex("#1b1733"));
            int[,] stars = { { 30, 28 }, { 92, 44 }, { 150, 24 }, { 210, 52 }, { 268, 30 }, { 330, 40 }, { 402, 26 }, { 470, 48 }, { 528, 32 }, { 590, 46 }, { 612, 22 }, { 360, 60 } };
            for (int i = 0; i < stars.GetLength(0); i++) Px.Fill(rt, stars[i, 0], stars[i, 1], 2, 2, Pal.Cream);
            Px.Fill(rt, 560, 26, 18, 18, Pal.Hex("#f1e6cc"));
            Px.Fill(rt, 566, 26, 12, 12, Pal.Hex("#151229"));
            // 遠方大樓
            int[,] b = { { 0, 70, 40 }, { 44, 56, 30 }, { 78, 80, 50 }, { 132, 62, 36 }, { 172, 74, 44 }, { 220, 50, 28 }, { 252, 66, 40 }, { 296, 84, 52 },
                         { 352, 58, 34 }, { 390, 72, 46 }, { 440, 54, 30 }, { 474, 78, 48 }, { 526, 60, 36 }, { 566, 70, 40 }, { 610, 64, 30 } };
            for (int i = 0; i < b.GetLength(0); i++)
            {
                int x = b[i, 0], y = b[i, 1], w = b[i, 2];
                Px.Fill(rt, x, y, w, 112 - y, Pal.Hex("#231f3a"));
                for (int wy = y + 6; wy < 106; wy += 10)
                    for (int wx = x + 4; wx < x + w - 4; wx += 8)
                        if (((wx * 7 + wy * 13) % 5) == 0) Px.Fill(rt, wx, wy, 3, 4, Pal.Hex("#e8c35a"));
            }
            // 馬路
            Px.Fill(rt, 0, FrontY + FrontH, 640, 4, Pal.Hex("#4a4858"));
            Px.Fill(rt, 0, FrontY + FrontH + 4, 640, 18, Pal.Hex("#2a2833"));
            for (int x = 8; x < 640; x += 40) Px.Fill(rt, x, FrontY + FrontH + 12, 20, 2, Pal.Hex("#8a8494"));

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
            Px.Button(top, "選單", 596, 2, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());
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
                RectTransform f = Px.Box(fronts, "Front_" + p.id, x, FrontY, FrontW, FrontH, Pal.Hex(Facade[i % Facade.Length]), Pal.Ink);
                string pid = p.id;
                Px.MakeClickable(f, () => { selected = pid; RefreshFronts(); RefreshPanel(); });
                // 招牌
                Color32 sign = locked ? Pal.PlumLine : Pal.Hex(SignBg[i % SignBg.Length]);
                Px.Fill(f, 4, 6, FrontW - 8, 18, Pal.Ink);
                Px.Fill(f, 5, 7, FrontW - 10, 16, sign);
                Px.Label(f, p.sign, 4, 8, FrontW - 8, 14, locked ? Pal.Lav : Pal.Ink, TextAnchor.UpperCenter, false);
                // 窗與門
                Color32 lit = locked || visited ? Pal.Hex("#3a3846") : Pal.Hex("#e8c35a");
                if (p.id == "junk")
                {
                    // 垃圾場：鐵絲網和廢棄物
                    for (int gx = 4; gx < FrontW - 4; gx += 5) Px.Fill(f, gx, 30, 1, 40, Pal.Hex("#6a6878"));
                    Px.Fill(f, 4, 30, FrontW - 8, 2, Pal.Hex("#8a8494"));
                    Px.Fill(f, 8, 84, 50, 34, Pal.Hex("#4a4a36"));
                    Px.Fill(f, 16, 74, 30, 12, Pal.Hex("#626a7e"));
                    Px.Fill(f, 22, 66, 12, 10, Pal.Hex("#c9a774"));
                }
                else
                {
                    Px.Fill(f, 6, 34, 22, 22, lit);
                    Px.Fill(f, FrontW - 28, 34, 22, 22, lit);
                    Px.Fill(f, 16, 34, 2, 22, Pal.Ink);
                    Px.Fill(f, FrontW - 18, 34, 2, 22, Pal.Ink);
                    float dx = Mathf.Floor((FrontW - 24) * 0.5f);
                    Px.Fill(f, dx, 70, 24, 48, Pal.Hex("#16131f"));
                    Px.Fill(f, dx + 2, 72, 20, 44, locked || visited ? Pal.Hex("#2a2833") : Pal.Hex("#57497a"));
                }
                // 今晚有活動的店，掛一張特價牌
                if (!locked && !string.IsNullOrEmpty(C.EventText(p.id)))
                {
                    Px.Fill(f, FrontW - 30, 24, 28, 12, Pal.Pink);
                    Px.Label(f, "活動", FrontW - 30, 23, 28, 14, Pal.Ink, TextAnchor.UpperCenter, false);
                }
                if (p.id == "roger")
                {
                    Px.Fill(f, 4, 86, 22, 30, Pal.Hex("#a07a52"));
                    Px.Fill(f, 2, 82, 26, 4, Pal.Hex("#a8352d"));
                }
                if (p.id == "temple")
                {
                    Px.Fill(f, 0, 0, FrontW, 5, Pal.Hex("#a8352d"));
                    Px.Fill(f, 8, 60, 6, 8, Pal.Hex("#e8893a"));
                    Px.Fill(f, FrontW - 14, 60, 6, 8, Pal.Hex("#e8893a"));
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
                if (!can && !locked && !visited) f.GetComponent<Image>().color = Pal.Alpha(Pal.Hex(Facade[i % Facade.Length]), 160);
            }
        }

        void RefreshPanel()
        {
            Px.Clear(panel);
            GameState s = game.State;
            RectTransform b = Px.Box(panel, "Info", 4, 258, 632, 98, Pal.Plum, Pal.PlumLight);
            NightPlace p = Career.Place(selected) ?? Career.Places[0];
            Px.Label(b, p.name, 8, 4, 140, 14, Pal.Orange);
            string ev = C.EventText(p.id);
            if (!string.IsNullOrEmpty(ev)) Px.Label(b, "今晚活動：" + ev, 150, 4, 284, 14, Pal.Yellow);
            Px.Label(b, p.desc, 8, 20, 420, 28, Pal.Cream, TextAnchor.UpperLeft, true);
            Px.Label(b, "花 1 段時間・疲勞 +" + (p.office ? 1 : 2) + (p.repeat ? "・可以重複來" : "・一晚一次"), 8, 50, 420, 14, Pal.Lav);
            string why;
            bool can = C.CanVisit(p, out why);
            Px.Button(b, can ? "前往" : why, 8, 72, 150, 20, can ? Pal.Orange : Pal.PlumLine, can ? Pal.Ink : Pal.Lav, Pal.Ink,
                () => { if (can) game.NightVisit(p.id); });

            Px.Fill(b, 440, 6, 1, 86, Pal.PlumLight);
            int pct = Mathf.RoundToInt((float)(C.OversleepChance * 100));
            bool none = s.nightVisited.Count == 0;
            Px.Label(b, none ? "現在回家：早點睡" : "明天睡過頭的機率", 448, 4, 180, 14, Pal.Lav);
            Px.Label(b, none ? "明天客人耐心 +1" : pct + "%", 448, 20, 180, 14, none ? Pal.TealLight : (pct >= 30 ? Pal.Pink : Pal.Cream));
            Px.Label(b, "睡過頭會錯過一通電話", 448, 36, 180, 14, Pal.PlumLight);
            Px.Button(b, "回家睡覺", 448, 56, 176, 36, Pal.TealDark, Pal.Cream, Pal.Ink, () => game.GoToBed());
        }

        // ------------------------------------------------------------------
        // 地點裡發生的事
        // ------------------------------------------------------------------

        public void ShowScene(NightScene sc, string result)
        {
            Px.Clear(modal);
            Image scrim = Px.Fill(modal, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 200));
            scrim.raycastTarget = true;
            int n = Mathf.Min(8, sc.options.Count);
            float h = 116 + n * 22 + 34;
            float y0 = Mathf.Floor((360 - h) * 0.5f);
            Px.Fill(modal, 93, y0 + 3, 460, h, Pal.Ink);
            RectTransform p = Px.Box(modal, "Scene", 90, y0, 460, h, Pal.Plum, Pal.Orange);
            Px.Fill(p, 1, 1, 458, 16, Pal.Ink);
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
                Px.Button(p, o.label, 10, y, 440, 20, o.enabled ? Pal.PlumLine : Pal.PlumDeep, o.enabled ? Pal.Cream : Pal.PlumLight, o.enabled ? Pal.PlumLight : Pal.PlumLine,
                    () => Choose(scene, opt));
            }
            Px.Button(p, "離開", 350, h - 28, 100, 20, Pal.Orange, Pal.Ink, Pal.Ink, () => { Px.Clear(modal); Refresh(); });
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
