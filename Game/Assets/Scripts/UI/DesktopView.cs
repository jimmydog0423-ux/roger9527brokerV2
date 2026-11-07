using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 正式營業的電腦桌面：左邊委託信箱，右邊是行程、藝人、庫存、投資、公司、帳本、手冊。
    /// 沒有通話時就停在這個畫面；按「撥打」會切到原本的視訊與文件畫面。
    /// </summary>
    public class DesktopView : MonoBehaviour
    {
        static readonly string[] Tabs = { "行程", "藝人", "庫存", "投資", "公司", "帳本", "手冊" };

        RectTransform rt;
        GameRoot game;
        CanvasGroup group;
        Transform top, inboxLayer, tabLayer, content, bottom, modal;
        int tab;
        string toast;
        float toastTime;

        public CanvasGroup Group { get { return group; } }
        public bool Visible { get { return group.alpha > 0.5f; } }

        public static DesktopView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Desktop", 0, 0, 640, 360);
            DesktopView v = r.gameObject.AddComponent<DesktopView>();
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
            if (on) Refresh();
        }

        public void SetAlpha(float a)
        {
            group.alpha = a;
            group.blocksRaycasts = a >= 1f;
            group.interactable = a >= 1f;
        }

        void Update()
        {
            if (toast == null) return;
            toastTime += Time.unscaledDeltaTime;
            if (toastTime > 7f)
            {
                toast = null;
                RefreshBottom();
            }
        }

        public void Toast(string text)
        {
            toast = text;
            toastTime = 0f;
            RefreshBottom();
        }

        void BuildStatic()
        {
            Image bg = Px.Fill(rt, 0, 0, 640, 360, Pal.Night);
            bg.raycastTarget = true;
            Sprite wallpaper = Resources.Load<Sprite>("Computer/desktop-wallpaper-v2");
            if (wallpaper != null)
            {
                bg.sprite = wallpaper;
                bg.color = Pal.White;
            }
            else Px.Tiled(rt, 0, 18, 640, 342, PixelSprites.Grid16, Pal.DeskGrid);
            Color32 panel = wallpaper != null ? Pal.Alpha(Pal.PlumDeep, 218) : Pal.PlumDeep;
            Px.Fill(rt, 0, 0, 640, 18, Pal.Ink);
            Px.Fill(rt, 0, 18, 640, 1, Pal.PlumLine);
            top = Px.Rect(rt, "Top", 0, 0, 640, 18);

            RectTransform ib = ConsoleArt.Panel(rt, "Inbox", 4, 22, 316, 334, panel, Pal.PlumLine);
            Px.Fill(ib, 4, 2, 308, 15, Pal.Alpha(Pal.Ink, 215));
            inboxLayer = Px.Rect(rt, "InboxList", 0, 0, 640, 360);

            tabLayer = Px.Rect(rt, "Tabs", 0, 0, 640, 360);
            RectTransform cb = ConsoleArt.Panel(rt, "Content", 324, 40, 312, 276, panel, Pal.PlumLine);
            content = Px.Rect(cb, "Inner", 0, 0, 312, 276);
            bottom = Px.Rect(rt, "Bottom", 0, 0, 640, 360);
            modal = Px.Rect(rt, "Modal", 0, 0, 640, 360);
        }

        Career C { get { return game.Career; } }
        GameState S { get { return game.State; } }

        public void Refresh()
        {
            if (C == null) return;
            RefreshTop();
            RefreshInbox();
            RefreshTabs();
            RefreshContent();
            RefreshBottom();
        }

        // ------------------------------------------------------------------
        // 上方狀態列
        // ------------------------------------------------------------------

        void RefreshTop()
        {
            Px.Clear(top);
            GameState s = S;
            Px.Label(top, "第 " + s.day + " 天・" + Calendar.Weekday(s.day) + " " + Calendar.Short(s.day), 6, 2, 150, 14, Pal.Cream);
            Px.Label(top, "現金 " + GameState.Money(s.cash), 150, 2, 170, 14, s.cash < 0 ? Pal.Pink : Pal.Cream);
            long pending = s.PendingFees + s.receivable;
            if (s.debt > 0) Px.Label(top, "欠銀行 " + GameState.Short(s.debt), 318, 2, 124, 14, Pal.Pink);
            else if (pending > 0) Px.Label(top, "待入帳 " + GameState.Short(pending), 318, 2, 124, 14, Pal.TealLight);
            float pct = Mathf.Clamp01(s.cash / (float)Career.Goal);
            Px.Label(top, "三千萬", 446, 2, 40, 14, Pal.Lav);
            Px.Fill(top, 488, 6, 70, 6, Pal.PlumLine);
            Px.Fill(top, 488, 6, Mathf.Round(70 * pct), 6, Pal.Orange);
            Px.Label(top, Mathf.FloorToInt(pct * 100) + "%", 561, 2, 32, 14, Pal.Orange);
            Px.Button(top, "選單", 596, 2, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());
        }

        // ------------------------------------------------------------------
        // 委託信箱
        // ------------------------------------------------------------------

        static string TypeTag(string t)
        {
            switch (t)
            {
                case "sponsor": case "gray": return "工商";
                case "buy": return "出售";    // 對方要賣東西給我們
                case "sell": return "收購";   // 對方要跟我們買東西
                case "broker": return "仲介";
                case "plan": return "企劃";
                case "invest": return "投資";
                case "dispute": return "糾紛";
                case "audit": return "查稅";
                case "talent": return "簽約";
                default: return "委託";
            }
        }

        static Color32 TypeColor(string t)
        {
            switch (t)
            {
                case "sponsor": case "gray": return Pal.Orange;
                case "buy": return Pal.Teal;
                case "sell": return Pal.TealLight;
                case "broker": return Pal.Lav;
                case "plan": return Pal.Yellow;
                case "invest": return Pal.Pink;
                case "dispute": case "audit": return Pal.Red;
                default: return Pal.Yellow;
            }
        }

        string ValueText(Opportunity op)
        {
            switch (op.type)
            {
                case "buy": return "開價 " + GameState.Short(op.est);
                case "sell":
                    if (S.HasUpgrade("intel"))
                    {
                        InvItem it = S.inventory.Find(i => i.id == op.item);
                        if (it != null) return "賺約 " + GameState.Short(op.est - it.cost);
                    }
                    return "約 " + GameState.Short(op.est);
                case "broker": return "佣金約 " + GameState.Short(op.est);
                case "invest": return "要 " + GameState.Short(op.est);
                case "dispute": case "audit": return "求償 " + GameState.Short(-op.est);
                case "talent": return "簽約金 " + GameState.Short(-op.est);
                default: return GameState.Short(op.est);
            }
        }

        void RefreshInbox()
        {
            Px.Clear(inboxLayer);
            GameState s = S;
            Career c = C;
            Px.Label(inboxLayer, "委託信箱", 10, 23, 80, 14, Pal.Lav);
            string slots = "通話時段";
            Px.Label(inboxLayer, slots, 150, 23, 56, 14, Pal.Lav);
            for (int i = 0; i < c.SlotsMax; i++)
            {
                bool on = i < s.slots;
                Px.Fill(inboxLayer, 208 + i * 9, 27, 7, 7, on ? Pal.Orange : Pal.PlumLine);
            }
            if (c.DelegatesMax > 0) Px.Label(inboxLayer, "委派 " + s.delegates + "/" + c.DelegatesMax, 256, 23, 60, 14, Pal.TealLight);

            List<Opportunity> list = c.VisibleInbox();
            if (list.Count == 0)
            {
                Px.Label(inboxLayer, "今天沒有新的委託了。", 4, 160, 316, 14, Pal.Lav, TextAnchor.UpperCenter, false);
                Px.Label(inboxLayer, "可以整理庫存、升級公司，或是結束營業。", 4, 176, 316, 14, Pal.PlumLight, TextAnchor.UpperCenter, false);
                return;
            }
            for (int i = 0; i < list.Count && i < 6; i++) InboxRow(list[i], 6, 42 + i * 49);
        }

        void InboxRow(Opportunity op, float x, float y)
        {
            GameState s = S;
            Career c = C;
            Color32 border = op.urgent ? Pal.Red : (op.lead ? Pal.TealLight : (op.held ? Pal.Yellow : Pal.PlumLine));
            RectTransform card = Px.Box(inboxLayer, "Op_" + op.uid, x, y, 312, 47, Pal.Plum, border);
            Px.Fill(card, 4, 4, 24, 28, Pal.VideoWall);
            PortraitArt.Draw(card, op.npc, 4, 4, 1);

            string tag = TypeTag(op.type);
            float tw = Px.TextWidth(tag) + 6;
            Px.Fill(card, 32, 3, tw, 14, TypeColor(op.type));
            Px.Label(card, tag, 32, 3, tw, 14, Pal.Ink, TextAnchor.UpperCenter, false);
            Px.Label(card, op.title, 36 + tw, 3, 150, 14, Pal.Cream);
            bool cost = op.est < 0 || op.type == "buy" || op.type == "invest";
            Px.Label(card, ValueText(op), 180, 3, 126, 14, op.urgent ? Pal.Pink : (cost ? Pal.Lav : Pal.Orange), TextAnchor.UpperRight, false);
            Px.Label(card, op.summary, 32, 17, 176, 14, Pal.Lav);
            int left = op.expires - s.day;
            string due = op.urgent ? (left <= 0 ? "今天截止" : "剩 " + (left + 1) + " 天") : (left <= 0 ? "今天到期" : "明天到期");
            if (op.lead && !op.urgent) due = "人脈・" + due;
            Px.Label(card, due, 210, 17, 96, 14, op.urgent ? Pal.Pink : Pal.PlumLight, TextAnchor.UpperRight, false);
            Px.Label(card, op.npcName + "・" + op.company, 32, 31, 170, 14, Pal.PlumLight);

            string why;
            bool canCall = c.CanCall(op, out why);
            string whyCall = why;
            ConsoleArt.Key(card, "撥打", 262, 30, 44, 14, canCall ? Pal.Orange : Pal.PlumLine, canCall ? Pal.Ink : Pal.Lav, Pal.Ink,
                () => { if (canCall) game.CallOpportunity(op); else Toast(whyCall); });
            if (s.Has("vjoined"))
            {
                bool canDel = c.CanDelegate(op, out why);
                string whyDel = why;
                ConsoleArt.Key(card, "委派", 214, 30, 44, 14, canDel ? Pal.TealDark : Pal.PlumLine, Pal.Cream, Pal.Ink,
                    () => { if (canDel) game.DelegateOpportunity(op); else Toast(whyDel); });
            }
        }

        // ------------------------------------------------------------------
        // 分頁
        // ------------------------------------------------------------------

        void RefreshTabs()
        {
            Px.Clear(tabLayer);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int k = i;
                bool on = tab == i;
                ConsoleArt.Key(tabLayer, Tabs[i], 324 + i * 44.6f, 22, 43, 16, on ? Pal.Orange : Pal.Plum, on ? Pal.Ink : Pal.Cream, on ? Pal.Ink : Pal.PlumLine,
                    () => { tab = k; RefreshTabs(); RefreshContent(); });
            }
        }

        void RefreshContent()
        {
            Px.Clear(content);
            switch (tab)
            {
                case 0: Schedule(); break;
                case 1: Talents(); break;
                case 2: Inventory(); break;
                case 3: Investments(); break;
                case 4: Company(); break;
                case 5: Ledger(); break;
                default: Handbook(); break;
            }
        }

        void Header(string text, float y)
        {
            Px.Fill(content, 1, y, 310, 15, Pal.Ink);
            Px.Label(content, text, 6, y, 300, 14, Pal.Lav);
        }

        void Schedule()
        {
            GameState s = S;
            Header("接下來 7 天的行程", 1);
            for (int i = 0; i < 7; i++)
            {
                int d = s.day + i;
                float y = 19 + i * 20;
                if (i == 0) Px.Fill(content, 2, y - 1, 308, 19, Pal.Plum);
                Px.Label(content, Calendar.Short(d) + " " + Calendar.Weekday(d), 6, y, 70, 14, i == 0 ? Pal.Orange : Pal.Lav);
                List<string> items = new List<string>();
                foreach (Booking b in s.bookings)
                {
                    if (b.done || b.day != d) continue;
                    TalentState t = s.Talent(b.talent);
                    items.Add((t != null ? t.name : "") + "：" + b.brand);
                }
                foreach (Busy b in s.busy)
                {
                    if (b.day != d) continue;
                    TalentState t = s.Talent(b.talent);
                    items.Add((t != null ? t.name : "") + "：" + b.text);
                }
                foreach (Opportunity o in s.inbox)
                    if (o.urgent && o.expires == d) items.Add("求償截止：" + o.company);
                string text = items.Count == 0 ? "—" : string.Join("、", items.ToArray());
                Px.Label(content, Clip(text, 228), 78, y, 230, 14, items.Count == 0 ? Pal.PlumLight : Pal.Cream);
            }
            Header("昨天的帳與新聞", 162);
            float yy = 180;
            List<string> lines = new List<string>();
            if (s.history.Count > 0) lines.Add("昨天利潤 " + GameState.Money(s.history[s.history.Count - 1]));
            foreach (string n in s.news) if (lines.Count < 6) lines.Add(n);
            foreach (string l in s.report) if (lines.Count < 6 && !lines.Contains(l) && !l.StartsWith("收入") && !l.StartsWith("房租")) lines.Add(l);
            foreach (string l in lines)
            {
                Px.Label(content, Clip(l, 300), 6, yy, 302, 14, l.StartsWith("新聞") ? Pal.Yellow : Pal.Cream);
                yy += 15;
            }
        }

        static string Clip(string text, float width)
        {
            if (Px.TextWidth(text) <= width) return text;
            string t = text;
            while (t.Length > 1 && Px.TextWidth(t + "…") > width) t = t.Substring(0, t.Length - 1);
            return t + "…";
        }

        void Bar(float x, float y, float w, int value, Color32 c)
        {
            Px.Fill(content, x, y + 4, w, 6, Pal.PlumLine);
            Px.Fill(content, x, y + 4, Mathf.Round(w * Mathf.Clamp01(value / 100f)), 6, c);
        }

        void Talents()
        {
            GameState s = S;
            Career c = C;
            Header("旗下藝人 " + s.talents.Count + " 位", 1);
            for (int i = 0; i < s.talents.Count && i < 5; i++)
            {
                TalentState t = s.talents[i];
                float y = 19 + i * 51;
                RectTransform card = Px.Box(content, "T_" + t.id, 3, y, 306, 48, Pal.Plum, Pal.PlumLine);
                Px.Fill(card, 4, 4, 24, 28, Pal.VideoWall);
                PortraitArt.Draw(card, t.portrait, 4, 4, 1);
                Px.Label(card, t.name + "・" + t.genre, 32, 2, 106, 14, Pal.Cream);
                Px.Label(card, "粉絲 " + CaseGen.FansText(t.fans), 32, 16, 100, 14, Pal.Lav);
                Px.Label(card, "心情", 140, 2, 30, 14, Pal.Lav);
                Px.Fill(card, 172, 6, 50, 6, Pal.PlumLine);
                Px.Fill(card, 172, 6, Mathf.Round(50 * t.mood / 100f), 6, t.mood < 40 ? Pal.Red : (t.mood < 60 ? Pal.Yellow : Pal.TealLight));
                Px.Label(card, "信任", 140, 16, 30, 14, Pal.Lav);
                Px.Fill(card, 172, 20, 50, 6, Pal.PlumLine);
                Px.Fill(card, 172, 20, Mathf.Round(50 * t.trust / 100f), 6, Pal.Lav);
                // 討厭的放前面：那是會讓藝人出包的資訊，不能被截掉
                string pref = (t.dislikes.Length > 0 ? "討厭" + string.Join("、", t.dislikes) : "")
                    + (t.likes.Length > 0 ? (t.dislikes.Length > 0 ? "　" : "") + "愛" + string.Join("、", t.likes) : "");
                if (!string.IsNullOrEmpty(t.interests)) pref = "興趣 " + t.interests;
                Px.Label(card, Clip(pref, 194), 32, 31, 196, 14, Pal.PlumLight);
                TalentState tt = t;
                Px.Button(card, "放假", 230, 4, 70, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => Act(c.Rest(tt)));
                bool can = s.day - t.raiseDay >= 7;
                Px.Button(card, can ? "加薪 " + GameState.Compact(c.RaiseCost(t)) : "加薪冷卻", 230, 20, 70, 14, can ? Pal.TealDark : Pal.PlumLine, Pal.Cream, Pal.Ink, () => Act(c.GiveRaise(tt)));
                if (t.cut < 100) Px.Label(card, "分潤 " + t.cut + "%", 232, 33, 70, 14, Pal.Pink);
            }
            if (s.talents.Count == 0) Px.Label(content, "目前沒有藝人", 6, 30, 300, 14, Pal.Lav);
        }

        void Inventory()
        {
            GameState s = S;
            Career c = C;
            Header("商品庫存 " + s.inventory.Count + " / " + c.InventoryCap, 1);
            if (s.inventory.Count == 0)
            {
                Px.Label(content, "庫存是空的。跟賣家買二手設備，再賣給需要的買家。", 6, 24, 300, 28, Pal.Lav, TextAnchor.UpperLeft, true);
                return;
            }
            for (int i = 0; i < s.inventory.Count && i < 8; i++)
            {
                InvItem it = s.inventory[i];
                float y = 19 + i * 32;
                RectTransform card = Px.Box(content, "I_" + it.id, 3, y, 306, 30, Pal.Plum, Pal.PlumLine);
                Px.Label(card, it.name + "・" + it.condition, 5, 1, 170, 14, Pal.Cream);
                long m = c.ItemMarket(it);
                Px.Label(card, "成本 " + GameState.Short(it.cost) + " → 行情 " + GameState.Short(m), 5, 15, 200, 14, m > it.cost ? Pal.TealLight : Pal.Pink);
                InvItem ii = it;
                if (it.rentDays > 0)
                {
                    Px.Label(card, "出租中 " + it.rentDays + " 天", 206, 8, 96, 14, Pal.Yellow, TextAnchor.UpperRight, false);
                }
                else
                {
                    Px.Button(card, "出租", 210, 3, 44, 12, Pal.TealDark, Pal.Cream, Pal.Ink, () => Act(c.RentOut(ii)));
                    Px.Button(card, "回收", 258, 3, 44, 12, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.Confirm("賣給回收商只拿行情的六成，確定嗎？", () => Act(c.Scrap(ii))));
                    Px.Label(card, "", 210, 15, 92, 14, Pal.Lav);
                }
            }
        }

        void Investments()
        {
            GameState s = S;
            Career c = C;
            Header("投資組合（配息每 3 天一次）", 1);
            List<Investment> list = new List<Investment>(s.investments);
            list.Sort((a, b) => (a.status == "active" ? 0 : 1) - (b.status == "active" ? 0 : 1));
            if (list.Count == 0)
            {
                Px.Label(content, "還沒有投資。投資前先比對企劃書和財務試算的數字。", 6, 24, 300, 28, Pal.Lav, TextAnchor.UpperLeft, true);
                return;
            }
            for (int i = 0; i < list.Count && i < 7; i++)
            {
                Investment v = list[i];
                float y = 19 + i * 36;
                RectTransform card = Px.Box(content, "V_" + i, 3, y, 306, 34, Pal.Plum, Pal.PlumLine);
                string st = v.status == "active" ? "配息中 " + v.paid + "/" + v.periods + " 期" : v.status == "done" ? "已到期" : v.status == "failed" ? "失敗" : "已出場";
                Px.Label(card, v.name, 5, 2, 150, 14, Pal.Cream);
                Px.Label(card, st, 150, 2, 90, 14, v.status == "failed" ? Pal.Pink : (v.status == "active" ? Pal.TealLight : Pal.Lav));
                Px.Label(card, "投入 " + GameState.Short(v.amount) + "　已收回 " + GameState.Short(v.received), 5, 17, 230, 14, Pal.Lav);
                if (v.status == "active")
                {
                    Investment vv = v;
                    Px.Button(card, "出場", 250, 9, 50, 14, Pal.PlumLine, Pal.Cream, Pal.Ink,
                        () => game.Confirm("把" + vv.name + "的股份賣掉，可以拿回 " + GameState.Short(c.ExitValue(vv)) + "。確定嗎？", () => Act(c.ExitInvestment(vv))));
                }
            }
        }

        void Company()
        {
            GameState s = S;
            Career c = C;
            Header(Career.ActName(s.act), 1);
            Px.Label(content, "聲譽", 6, 18, 30, 14, Pal.Lav);
            Bar(38, 18, 70, s.reputation, s.reputation >= 60 ? Pal.TealLight : (s.reputation >= 35 ? Pal.Yellow : Pal.Red));
            Px.Label(content, s.reputation + "", 112, 18, 30, 14, Pal.Cream);
            Px.Label(content, "每日開銷 " + GameState.Short(c.Overhead), 150, 18, 156, 14, Pal.Lav);
            Px.Label(content, "下個目標 " + GameState.Short(c.NextGoal), 6, 33, 150, 14, Pal.Orange);
            Px.Label(content, "時段 " + c.SlotsMax + "　庫存 " + c.InventoryCap + "　委託 " + c.InboxSize, 150, 33, 156, 14, Pal.Lav);
            Header("公司升級（點名稱看說明）", 48);
            float y = 64;
            foreach (UpgradeDef u in game.World.upgrades)
            {
                string why;
                bool can = c.UpgradeAvailable(u, out why);
                bool own = s.HasUpgrade(u.id);
                UpgradeDef uu = u;
                RectTransform row = Px.Rect(content, "U_" + u.id, 3, y, 110, 15);
                Px.MakeClickable(row, () => Toast(uu.name + "：" + uu.desc));
                Px.Label(content, u.name, 6, y, 110, 14, own ? Pal.TealLight : Pal.Cream);
                Px.Label(content, GameState.Short(u.cost), 116, y, 90, 14, Pal.Lav);
                if (own) Px.Label(content, "已擁有", 220, y, 86, 14, Pal.TealLight, TextAnchor.UpperRight, false);
                else if (can) Px.Button(content, "購買", 256, y, 50, 14, Pal.Orange, Pal.Ink, Pal.Ink,
                    () => game.Confirm("花 " + GameState.Short(uu.cost) + " 買「" + uu.name + "」？\n" + uu.desc, () => Act(c.BuyUpgrade(uu))));
                else Px.Label(content, why, 180, y, 126, 14, Pal.PlumLight, TextAnchor.UpperRight, false);
                y += 15;
                if (y > 262) break;
            }
        }

        void Ledger()
        {
            GameState s = S;
            Header("公司帳本", 1);
            long assets = s.cash + s.receivable + s.PendingFees + s.InvestmentTotal + s.InventoryValue;
            string[,] rows =
            {
                { "現金（算進三千萬）", GameState.Money(s.cash) },
                { "待履約的工商", GameState.Money(s.PendingFees) },
                { "應收款（今晚入帳）", GameState.Money(s.receivable) },
                { "投資中的本金", GameState.Money(s.InvestmentTotal) },
                { "庫存成本", GameState.Money(s.InventoryValue) },
                { s.debt > 0 ? "總資產（已扣欠款）" : "總資產", GameState.Money(assets - s.debt) },
                { "今日收入", GameState.Money(s.revenueToday) },
                { "今日支出", GameState.Money(s.costToday) }
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                float y = 18 + i * 15;
                Px.Label(content, rows[i, 0], 6, y, 150, 14, i == 5 ? Pal.Orange : Pal.Lav);
                Px.Label(content, rows[i, 1], 150, y, 156, 14, Pal.Cream, TextAnchor.UpperRight, false);
            }
            Header("最近幾天的利潤", 140);
            int n = Mathf.Min(10, s.history.Count);
            long max = 1;
            for (int i = s.history.Count - n; i < s.history.Count; i++) max = Math.Max(max, Math.Abs(s.history[i]));
            for (int i = 0; i < n; i++)
            {
                long v = s.history[s.history.Count - n + i];
                float h = Mathf.Max(1, Mathf.Round(40f * Math.Abs(v) / max));
                float x = 10 + i * 30;
                if (v >= 0) Px.Fill(content, x, 200 - h, 20, h, Pal.TealLight);
                else Px.Fill(content, x, 200, 20, h, Pal.Pink);
            }
            Px.Fill(content, 6, 200, 300, 1, Pal.PlumLight);
            Px.Label(content, "成交 " + (s.GetStat("deals") + s.GetStat("trades") + s.GetStat("brokers") + s.GetStat("plans")) + " 件　糾紛 " + s.GetStat("disputes") + " 件　羅傑出包 " + s.GetStat("incidents") + " 次", 6, 246, 300, 14, Pal.Lav);
            Px.Label(content, "三千萬只算現金，不算待收款和資產。", 6, 260, 300, 14, Pal.PlumLight);
        }

        void Handbook()
        {
            Header("鬼東的經紀人工作手冊", 1);
            string[] lines =
            {
                "合約：金額對報價單、名字對檔案、日期別撞期。",
                "　　　賠償上限 1 倍，排他不能壓到已簽的約。",
                "工商：看藝人偏好。討厭的牌子，直播當天容易出包。",
                "　　　簽好的工商，直播完成才入帳。",
                "交易：先送檢測再出價。賣家說的都要查證。",
                "　　　有瑕疵就老實講，客訴比少賺更貴。",
                "仲介：類型、預算、檔期、偏好，四個都要對。",
                "企劃：需求全部做到，成本越低賺越多。",
                "投資：企劃書的數字要跟財務試算對得上。",
                "糾紛：先道歉、不否認事實、逐條核對主張。",
                "　　　求償放著不管，期限到了照全額賠。",
                "灰色案件：報酬高的，先看授權書和公司登記。",
                "財務：三千萬只算現金。每天有固定開銷。",
                "委派：V老大可以代辦簡單的案子，但不會全部抓到。",
                "晚上：出門跑越多地方，隔天越容易睡過頭。"
            };
            for (int i = 0; i < lines.Length; i++)
                Px.Label(content, lines[i], 6, 18 + i * 17, 304, 14, lines[i].StartsWith("　") ? Pal.Lav : Pal.Cream);
        }

        void Act(string msg)
        {
            game.SaveNow();
            Toast(msg);
            Refresh();
        }

        // ------------------------------------------------------------------
        // 下方：提示與結束營業
        // ------------------------------------------------------------------

        void RefreshBottom()
        {
            Px.Clear(bottom);
            if (!string.IsNullOrEmpty(toast))
            {
                RectTransform box = Px.Box(bottom, "Toast", 324, 320, 196, 36, Pal.Ink, Pal.Yellow);
                Px.MakeClickable(box, () => { toast = null; RefreshBottom(); });
                Px.Label(box, toast, 4, 2, 188, 32, Pal.Cream, TextAnchor.UpperLeft, true);
            }
            else if (C != null)
            {
                GameState s = S;
                string hint = s.slots > 0 ? "從左邊的委託挑一件撥打" : "今天的時段用完了，結束營業吧";
                Px.Label(bottom, hint, 326, 322, 194, 14, Pal.Lav);
                Px.Label(bottom, "目標：現金三千萬", 326, 338, 194, 14, Pal.PlumLight);
            }
            Px.Button(bottom, "結束營業", 524, 320, 112, 36, Pal.Red, Pal.Cream, Pal.Ink, () => game.RequestEndDay());
        }

        // ------------------------------------------------------------------
        // 日結報告
        // ------------------------------------------------------------------

        public void ShowReport(DayReport rep, Action next)
        {
            Px.Clear(modal);
            Image scrim = Px.Fill(modal, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 200));
            scrim.raycastTarget = true;
            List<string> lines = new List<string>(rep.lines);
            int maxLines = 15;
            if (lines.Count > maxLines) { lines = lines.GetRange(0, maxLines - 1); lines.Add("……還有其他消息，記在行程頁"); }
            float h = 30 + lines.Count * 15 + 44;
            float y0 = Mathf.Floor((360 - h) * 0.5f);
            Px.Fill(modal, 103, y0 + 3, 440, h, Pal.Ink);
            RectTransform p = Px.Box(modal, "Report", 100, y0, 440, h, Pal.Plum, Pal.Orange);
            Px.Fill(p, 1, 1, 438, 16, Pal.Ink);
            Px.Label(p, "第 " + rep.day + " 天結算・" + Calendar.Short(rep.day), 8, 2, 300, 14, Pal.Orange);
            Px.Label(p, "今日利潤 " + GameState.Money(rep.profit), 230, 2, 202, 14, rep.profit >= 0 ? Pal.TealLight : Pal.Pink, TextAnchor.UpperRight, false);
            for (int i = 0; i < lines.Count; i++)
            {
                string l = lines[i];
                Color32 col = l.Contains("！") ? Pal.Pink : (l.StartsWith("新聞") ? Pal.Yellow : Pal.Cream);
                Px.Label(p, Clip(l, 420), 10, 22 + i * 15, 424, 14, col);
            }
            Px.Label(p, "下班時現金 " + GameState.Money(rep.cash), 10, h - 38, 300, 14, Pal.Orange);
            Px.Button(p, rep.ending ? "……" : "下一天", 330, h - 30, 100, 22, Pal.Orange, Pal.Ink, Pal.Ink, () => { Px.Clear(modal); next(); });
        }
    }
}
