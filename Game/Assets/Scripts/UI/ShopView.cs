using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 晚上的商店畫面（光華電子街、深夜跳蚤市場）。
    /// 貨架上擺著商品，每張卡片寫著用途和價錢；點一下或拖到下面的桌上就能結帳。
    /// 電子街是正派商店，照標價買賣、不能議價；跳蚤市場要在桌上跟老闆殺價，
    /// 可以先「仔細檢查」找出問題，老闆會心虛，成功率跟著變高。
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        const float ShelfY = 42f;
        const float CardW = 152f;
        const float CardH = 92f;
        const float TableY = 236f;
        const float TableW = 418f;
        const float DealX = 426f;
        const int PerPage = 8;

        /// <summary>貨架上的一格：商品或道具。</summary>
        class Entry
        {
            public InvItem item;
            public Supply supply;
            public bool mine;       // 自己庫存裡的（賣給店家）
        }

        RectTransform rt;
        GameRoot game;
        System.Action onChanged;
        System.Action onLeave;
        Transform bg, header, shelf, table, deal, ghostLayer;
        RectTransform ghost;

        string place = "market";
        bool sellTab;
        int page;
        Entry onTable;
        Entry dragging;
        string message;

        Career C { get { return game.Career; } }
        GameState S { get { return game.State; } }
        bool Flea { get { return place == "flea"; } }

        public static ShopView Build(RectTransform parent, GameRoot game, System.Action onChanged)
        {
            RectTransform r = Px.Rect(parent, "Shop", 0, 0, 640, 360);
            ShopView v = r.gameObject.AddComponent<ShopView>();
            v.rt = r;
            v.game = game;
            v.onChanged = onChanged;
            v.bg = Px.Rect(r, "Bg", 0, 0, 640, 360);
            v.header = Px.Rect(r, "Header", 0, 0, 640, 360);
            v.shelf = Px.Rect(r, "Shelf", 0, 0, 640, 360);
            v.table = Px.Rect(r, "Table", 0, 0, 640, 360);
            v.deal = Px.Rect(r, "Deal", 0, 0, 640, 360);
            v.ghostLayer = Px.Rect(r, "GhostLayer", 0, 0, 640, 360);
            v.ghost = Px.Rect(v.ghostLayer, "Ghost", 0, 0, 48, 48);
            v.ghost.gameObject.SetActive(false);
            r.gameObject.SetActive(false);
            return v;
        }

        public bool Visible { get { return gameObject.activeSelf; } }

        public void Show(string placeId, System.Action leave)
        {
            place = placeId;
            onLeave = leave;
            sellTab = false;
            page = 0;
            onTable = null;
            message = Flea
                ? "老闆：「都好的啦！便宜賣！」看中哪個就放到桌上，價錢可以談。"
                : "店員：「歡迎光臨！」看中哪個就放到桌上結帳。這裡照標價賣，不議價。";
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Refresh()
        {
            DrawBackground();
            DrawHeader();
            DrawShelf();
            DrawTable();
            DrawDeal();
        }

        void Changed()
        {
            game.SaveNow();
            if (onChanged != null) onChanged();
        }

        // ------------------------------------------------------------------
        // 背景與上方標題
        // ------------------------------------------------------------------

        void DrawBackground()
        {
            Px.Clear(bg);
            Image wall = Px.Fill(bg, 0, 19, 640, 341, Pal.Hex(Flea ? "#1d1626" : "#262c44"));
            wall.raycastTarget = true;
            if (Flea)
            {
                // 夜市的帆布棚和燈泡
                for (int x = 0; x < 640; x += 40) Px.Fill(bg, x, 38, 20, 4, Pal.Hex("#a8352d"));
                for (int x = 12; x < 640; x += 46) Px.Fill(bg, x, 44, 3, 3, Pal.Yellow);
            }
            // 兩層貨架的木板
            for (int r = 0; r < 2; r++) Px.Fill(bg, 0, ShelfY + r * 96 + CardH + 1, 640, 3, Pal.Hex(Flea ? "#5a4632" : "#8a6844"));
            // 桌子
            Px.Fill(bg, 0, TableY, 640, 124, Pal.Hex("#1a1626"));
            Px.Fill(bg, 4, TableY, TableW, 8, Pal.Hex(Flea ? "#7a5a3a" : "#a07a52"));
            Px.Fill(bg, 4, TableY + 8, TableW, 112, Pal.Hex(Flea ? "#4a3626" : "#5e4630"));
            Px.Fill(bg, 4, TableY + 8, TableW, 2, Pal.Hex("#2a1e14"));
        }

        void DrawHeader()
        {
            Px.Clear(header);
            RectTransform h = Px.Box(header, "Head", 4, 21, 632, 18, Pal.Plum, Pal.PlumLight);
            NightPlace p = Career.Place(place);
            Px.Label(h, p != null ? p.name : place, 6, 2, 100, 14, Pal.Orange);
            string ev = C.EventText(place);
            Px.Label(h, string.IsNullOrEmpty(ev) ? (Flea ? "可以殺價・不能檢測・沒有保固" : "照標價買賣・不能議價") : ev, 104, 2, 250, 14,
                string.IsNullOrEmpty(ev) ? Pal.Lav : Pal.Yellow);
            Px.Label(h, "庫存 " + S.inventory.Count + "/" + C.InventoryCap, 356, 2, 70, 14, Pal.Lav);
            float x = 430;
            int total = Entries().Count;
            if (total > PerPage)
            {
                int pages = (total + PerPage - 1) / PerPage;
                Px.Button(h, "下一頁 " + (page + 1) + "/" + pages, x - 76, 2, 72, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () =>
                {
                    page = (page + 1) % pages;
                    Refresh();
                });
            }
            if (!Flea)
            {
                Px.Button(h, "買東西", x, 2, 60, 14, sellTab ? Pal.PlumLine : Pal.Orange, sellTab ? Pal.Cream : Pal.Ink, Pal.Ink, () => SetTab(false));
                Px.Button(h, "賣東西", x + 64, 2, 60, 14, sellTab ? Pal.Orange : Pal.PlumLine, sellTab ? Pal.Ink : Pal.Cream, Pal.Ink, () => SetTab(true));
            }
            Px.Button(h, "離開", 580, 2, 48, 14, Pal.Red, Pal.Cream, Pal.Ink, Leave);
        }

        void SetTab(bool sell)
        {
            sellTab = sell;
            page = 0;
            onTable = null;
            message = sell ? "店員：「要賣的東西放到桌上，我幫你驗貨估價。」" : "店員：「看中哪個就放到桌上結帳。」";
            Refresh();
        }

        void Leave()
        {
            Hide();
            if (onLeave != null) onLeave();
        }

        // ------------------------------------------------------------------
        // 貨架
        // ------------------------------------------------------------------

        List<Entry> Entries()
        {
            List<Entry> list = new List<Entry>();
            if (Flea)
            {
                foreach (InvItem it in S.fleaStock) list.Add(new Entry { item = it });
            }
            else if (sellTab)
            {
                foreach (InvItem it in S.inventory) list.Add(new Entry { item = it, mine = true });
            }
            else
            {
                foreach (InvItem it in S.shopStock) list.Add(new Entry { item = it });
                foreach (Supply sp in C.ShopSupplies()) list.Add(new Entry { supply = sp });
            }
            return list;
        }

        void DrawShelf()
        {
            Px.Clear(shelf);
            List<Entry> list = Entries();
            int start = page * PerPage;
            if (start >= list.Count) start = page = 0;
            for (int i = 0; i < PerPage && start + i < list.Count; i++)
            {
                Entry e = list[start + i];
                float x = 4 + (i % 4) * 158;
                float y = ShelfY + (i / 4) * 96;
                bool here = onTable != null && Same(onTable, e);
                RectTransform card = Card(shelf, x, y, e, here);
                DragSource ds = card.gameObject.AddComponent<DragSource>();
                Entry entry = e;
                ds.onClick = () => PutOnTable(entry);
                ds.onBegin = ev => BeginDrag(entry, ev);
                ds.onDrag = ev => MoveDrag(ev);
                ds.onEnd = ev => EndDrag(ev);
            }
            if (list.Count == 0)
            {
                string empty = Flea ? "攤位都收了，今晚的東西都被買走了。"
                    : sellTab ? "庫存是空的，沒有東西可以賣。" : "貨架都空了。";
                Px.Label(shelf, empty, 4, 120, 632, 14, Pal.Lav, TextAnchor.UpperCenter, false);
            }
        }

        static bool Same(Entry a, Entry b)
        {
            return a.item != null ? a.item == b.item : a.supply != null && a.supply == b.supply;
        }

        string Icon(Entry e)
        {
            return e.supply != null ? e.supply.icon : C.ItemIcon(e.item);
        }

        /// <summary>商品卡片：圖示、名稱、狀況、價錢、用途。</summary>
        RectTransform Card(Transform parent, float x, float y, Entry e, bool dim)
        {
            RectTransform card = Px.Box(parent, "Card", x, y, CardW, CardH, dim ? Pal.PlumDeep : Pal.Plum, dim ? Pal.PlumLine : Pal.PlumLight);
            card.GetComponent<Image>().raycastTarget = true;
            float a = dim ? 0.35f : 1f;
            RectTransform icon = Px.Rect(card, "Icon", 1, 1, 48, 48);
            IconArt.Draw(icon, Icon(e), a);
            Color32 cream = dim ? Pal.PlumLight : Pal.Cream;

            if (e.supply != null)
            {
                Supply sp = e.supply;
                Px.Label(card, sp.name, 50, 3, 98, 14, cream);
                Px.Label(card, sp.tool ? "工具" : "道具", 50, 17, 98, 14, dim ? Pal.PlumLight : Pal.TealLight);
                Px.Label(card, GameState.Money(C.SupplyPrice(sp)), 50, 31, 98, 14, dim ? Pal.PlumLight : Pal.Yellow);
                Px.Label(card, sp.use, 4, 49, 144, 28, cream, TextAnchor.UpperLeft, true);
                Px.Label(card, sp.tool ? "買一次，一直有效" : "買了馬上用", 4, 76, 144, 14, Pal.Lav);
                if (C.NightWeekday == 5) Tag(card, "85折");
                return card;
            }

            InvItem it = e.item;
            Px.Label(card, it.name, 50, 3, 98, 14, cream);
            if (e.mine)
            {
                Px.Label(card, it.condition, 50, 17, 98, 14, dim ? Pal.PlumLight : GradeColor(it.grade));
                Px.Label(card, it.rentDays > 0 ? "出租中" : "店家收 " + GameState.Short(C.ShopSellPrice(it)), 50, 31, 98, 14,
                    dim ? Pal.PlumLight : (it.rentDays > 0 ? Pal.Lav : Pal.Yellow));
                Px.Label(card, C.ItemUse(it), 4, 49, 144, 28, cream, TextAnchor.UpperLeft, true);
                Px.Label(card, "成本 " + GameState.Short(it.cost), 4, 76, 144, 14, Pal.Lav);
                if (C.NightWeekday == 4) Tag(card, "加1成");
                return card;
            }
            if (Flea)
            {
                FleaDeal d = C.DealOf(it);
                bool refused = d != null && d.refused;
                bool looked = d != null && d.looked;
                Px.Label(card, looked ? it.condition : "說是" + it.condition, 50, 17, 98, 14, dim ? Pal.PlumLight : (looked ? GradeColor(it.grade) : Pal.Lav));
                Px.Label(card, "開價 " + GameState.Short(it.cost), 50, 31, 98, 14, dim ? Pal.PlumLight : Pal.Yellow);
                Px.Label(card, C.ItemUse(it), 4, 49, 144, 28, cream, TextAnchor.UpperLeft, true);
                Px.Label(card, refused ? "老闆不賣你了" : "不能檢測・沒有保固", 4, 76, 144, 14, refused ? Pal.Pink : Pal.PlumLight);
                if (C.NightWeekday == 2) Tag(card, "8折");
                return card;
            }
            long price = C.ShopPriceNow(it);
            Px.Label(card, it.condition, 50, 17, 98, 14, dim ? Pal.PlumLight : GradeColor(it.grade));
            Px.Label(card, GameState.Money(price), 50, 31, 98, 14, dim ? Pal.PlumLight : Pal.Yellow);
            Px.Label(card, C.ItemUse(it), 4, 49, 144, 28, cream, TextAnchor.UpperLeft, true);
            Px.Label(card, "轉賣行情 " + GameState.Short(C.MarketOf(it)), 4, 76, 144, 14, Pal.Lav);
            double k = C.ShopDiscount(it);
            if (k < 1) Tag(card, k <= 0.85 ? "85折" : "9折");
            return card;
        }

        /// <summary>折扣牌：貼在圖示下方，不會壓到商品名稱。</summary>
        static void Tag(RectTransform card, string text)
        {
            float w = Px.TextWidth(text) + 6;
            Px.Fill(card, 3, 33, w, 14, Pal.Pink);
            Px.Label(card, text, 3, 33, w, 14, Pal.Ink, TextAnchor.UpperCenter, false);
        }

        static Color32 GradeColor(int g)
        {
            return g <= 1 ? Pal.TealLight : (g <= 3 ? Pal.Yellow : Pal.Pink);
        }

        // ------------------------------------------------------------------
        // 拖曳
        // ------------------------------------------------------------------

        void BeginDrag(Entry e, PointerEventData ev)
        {
            dragging = e;
            Px.Clear(ghost);
            Px.Fill(ghost, 0, 0, 48, 48, Pal.Night);
            Px.Frame(ghost, 0, 0, 48, 48, Pal.Orange);
            IconArt.Draw(ghost, Icon(e), 1f);
            ghost.gameObject.SetActive(true);
            ghostLayer.SetAsLastSibling();
            MoveDrag(ev);
        }

        void MoveDrag(PointerEventData ev)
        {
            if (dragging == null) return;
            Vector2 p = Px.ScreenToRoot(rt, ev.position);
            Px.Move(ghost, Mathf.Round(p.x - 24f), Mathf.Round(p.y - 24f));
        }

        void EndDrag(PointerEventData ev)
        {
            Entry e = dragging;
            dragging = null;
            ghost.gameObject.SetActive(false);
            if (e == null) return;
            Vector2 p = Px.ScreenToRoot(rt, ev.position);
            if (p.y >= TableY - 8 && p.x <= DealX) PutOnTable(e);
        }

        void PutOnTable(Entry e)
        {
            if (Flea)
            {
                FleaDeal d = C.DealOf(e.item);
                if (d != null && d.refused)
                {
                    message = "老闆把東西收了回去：「不賣你了啦。」";
                    onTable = null;
                    Refresh();
                    return;
                }
            }
            onTable = e;
            if (Flea) message = "老闆：「" + e.item.name + " 喔？" + e.item.condition + "的啦，" + GameState.Short(e.item.cost) + " 帶走。」";
            else if (e.mine) message = "店員看了看" + e.item.name + "：「這個我們收 " + GameState.Short(C.ShopSellPrice(e.item)) + "。」";
            else message = "店員：「" + (e.supply != null ? e.supply.name : e.item.name) + "，要結帳嗎？」";
            Refresh();
        }

        void ClearTable(string msg)
        {
            onTable = null;
            if (msg != null) message = msg;
            Refresh();
        }

        // ------------------------------------------------------------------
        // 桌子
        // ------------------------------------------------------------------

        void DrawTable()
        {
            Px.Clear(table);
            if (onTable != null)
            {
                Card(table, 12, TableY + 16, onTable, false);
            }
            else
            {
                Px.DashedRect(table, 12, TableY + 16, CardW, CardH, Pal.Hex("#c9a774"));
                Px.Label(table, "把商品拖到桌上", 12, TableY + 46, CardW, 14, Pal.Hex("#c9a774"), TextAnchor.UpperCenter, false);
                Px.Label(table, "（或點一下）", 12, TableY + 62, CardW, 14, Pal.Hex("#c9a774"), TextAnchor.UpperCenter, false);
            }
            if (!string.IsNullOrEmpty(message))
            {
                Px.Fill(table, 172, TableY + 16, 242, 92, Pal.Alpha(Pal.Ink, 200));
                Px.Label(table, message, 178, TableY + 20, 232, 84, Pal.Yellow, TextAnchor.UpperLeft, true);
            }
        }

        // ------------------------------------------------------------------
        // 右下角的結帳／議價
        // ------------------------------------------------------------------

        void DrawDeal()
        {
            Px.Clear(deal);
            RectTransform p = Px.Box(deal, "Deal", DealX, TableY, 210, 120, Pal.Plum, Pal.PlumLight);
            if (onTable == null)
            {
                Px.Label(p, Flea ? "殺價小技巧" : "怎麼買", 6, 4, 196, 14, Pal.Orange);
                string tip = Flea
                    ? "先按「仔細檢查」。找到瑕疵或贓物的跡象，老闆會心虛，成功率變高。殺價失敗太多次，老闆就不賣了。"
                    : sellTab ? "店家會驗貨，照真正的狀況估價。有問題的東西瞞不過他們。"
                    : "把商品放到桌上就能結帳。道具買了馬上有效，商品會放進庫存，可以出租或轉賣。";
                Px.Label(p, tip, 6, 22, 198, 94, Pal.Cream, TextAnchor.UpperLeft, true);
                return;
            }
            if (onTable.supply != null) DealSupply(p, onTable.supply);
            else if (onTable.mine) DealSell(p, onTable.item);
            else if (Flea) DealFlea(p, onTable.item);
            else DealBuy(p, onTable.item);
        }

        void DealSupply(RectTransform p, Supply sp)
        {
            Px.Label(p, sp.name, 6, 4, 196, 14, Pal.Orange);
            Px.Label(p, sp.use, 6, 20, 198, 28, Pal.Cream, TextAnchor.UpperLeft, true);
            Px.Label(p, "價格 " + GameState.Money(C.SupplyPrice(sp)), 6, 52, 196, 14, Pal.Yellow);
            Px.Label(p, "電子街不議價", 6, 68, 196, 14, Pal.PlumLight);
            Px.Button(p, "購買", 6, 96, 120, 18, Pal.Orange, Pal.Ink, Pal.Ink, () =>
            {
                string res = C.BuySupply(sp);
                Changed();
                ClearTable(res);
            });
            Px.Button(p, "放回", 130, 96, 74, 18, Pal.PlumLine, Pal.Cream, Pal.Ink, () => ClearTable("放回貨架了。"));
        }

        void DealBuy(RectTransform p, InvItem it)
        {
            long price = C.ShopPriceNow(it);
            Px.Label(p, it.name, 6, 4, 196, 14, Pal.Orange);
            Px.Label(p, it.condition, 6, 20, 196, 14, GradeColor(it.grade));
            Px.Label(p, "價格 " + GameState.Money(price), 6, 36, 196, 14, Pal.Yellow);
            if (price < it.cost) Px.Label(p, "原價 " + GameState.Money(it.cost), 6, 52, 196, 14, Pal.Lav);
            else Px.Label(p, "轉賣行情 " + GameState.Money(C.MarketOf(it)), 6, 52, 196, 14, Pal.Lav);
            Px.Label(p, "電子街不議價", 6, 68, 196, 14, Pal.PlumLight);
            Px.Button(p, "結帳", 6, 96, 120, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => Buy(it));
            Px.Button(p, "放回", 130, 96, 74, 18, Pal.PlumLine, Pal.Cream, Pal.Ink, () => ClearTable("放回貨架了。"));
        }

        void Buy(InvItem it)
        {
            string res = C.ShopBuyNow(it);
            if (!S.shopStock.Contains(it))
            {
                Changed();
                ClearTable(res);
            }
            else
            {
                message = res;
                Refresh();
            }
        }

        void DealSell(RectTransform p, InvItem it)
        {
            long price = C.ShopSellPrice(it);
            Px.Label(p, it.name, 6, 4, 196, 14, Pal.Orange);
            Px.Label(p, it.condition, 6, 20, 196, 14, GradeColor(it.grade));
            Px.Label(p, "店家收購 " + GameState.Money(price), 6, 36, 196, 14, Pal.Yellow);
            long profit = price - it.cost;
            Px.Label(p, "成本 " + GameState.Short(it.cost) + "・利潤 " + GameState.Short(profit), 6, 52, 196, 14, profit >= 0 ? Pal.TealLight : Pal.Pink);
            Px.Label(p, C.NightWeekday == 4 ? "週五收購祭，多給 1 成" : "店家會照真正的狀況估價", 6, 68, 196, 14, C.NightWeekday == 4 ? Pal.Yellow : Pal.PlumLight);
            Px.Button(p, "賣出", 6, 96, 120, 18, Pal.Orange, Pal.Ink, Pal.Ink, () =>
            {
                string res = C.ShopSellNow(it);
                Changed();
                ClearTable(res);
            });
            Px.Button(p, "放回", 130, 96, 74, 18, Pal.PlumLine, Pal.Cream, Pal.Ink, () => ClearTable("先不賣了。"));
        }

        void DealFlea(RectTransform p, InvItem it)
        {
            FleaDeal d = C.DealOf(it);
            Px.Label(p, it.name, 6, 2, 140, 14, Pal.Orange);
            // 老闆的耐心
            int pat = C.FleaPatience(it);
            for (int i = 0; i < 3; i++) Px.Fill(p, 176 + i * 10, 6, 7, 7, i < pat ? Pal.Orange : Pal.PlumLine);
            Px.Label(p, (d != null && d.looked ? "檢查結果：" : "老闆說：") + it.condition, 6, 16, 198, 14, Pal.Lav);
            Px.Label(p, "開價 " + GameState.Money(it.cost), 6, 30, 198, 14, Pal.Cream);

            // 出價只能從固定的幅度挑，按下去就出價；滑鼠移上去（手機按住）才看得到成功率
            bool mobile = Px.Mobile;
            string idle = mobile ? "按住出價按鈕看成功率，滑開取消" : "滑鼠移到出價按鈕上看成功率";
            Text line = Px.Label(p, idle, 6, 46, 198, 14, Pal.Lav);
            Image barBg = Px.Fill(p, 6, 64, 150, 6, Pal.PlumLine);
            Image bar = Px.Fill(p, 6, 64, 0, 6, Pal.TealLight);
            Text pctText = Px.Label(p, "", 160, 60, 44, 14, Pal.TealLight, TextAnchor.UpperRight, false);
            barBg.gameObject.SetActive(false);
            bar.gameObject.SetActive(false);
            double[] steps = { 0, -0.1, -0.2, -0.3 };
            for (int i = 0; i < steps.Length; i++)
            {
                long v = System.Math.Max(CaseGen.Step(it.cost), CaseGen.Nice(it.cost * (1 + steps[i])));
                string label = i == 0 ? "照價" : "-" + System.Math.Round(-steps[i] * 100) + "%";
                RectTransform b = Px.Button(p, label, 6 + i * 50, 76, 48, 20, i == 0 ? Pal.PlumLight : Pal.PlumLine, Pal.Cream, Pal.Ink, () => Offer(it, v));
                Hover h = b.gameObject.AddComponent<Hover>();
                h.onHover = on =>
                {
                    if (on)
                    {
                        int pct = C.FleaChance(it, v);
                        Color32 pc = pct >= 70 ? Pal.TealLight : (pct >= 30 ? Pal.Yellow : Pal.Pink);
                        line.text = Px.Fit(label + "：出 " + GameState.Money(v), 198);
                        line.color = Pal.Cream;
                        barBg.gameObject.SetActive(true);
                        bar.gameObject.SetActive(true);
                        bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(150 * pct / 100f), 6);
                        bar.color = pc;
                        pctText.text = pct + "%";
                        pctText.color = pc;
                    }
                    else
                    {
                        line.text = idle;
                        line.color = Pal.Lav;
                        barBg.gameObject.SetActive(false);
                        bar.gameObject.SetActive(false);
                        pctText.text = "";
                    }
                };
            }

            bool looked = d != null && d.looked;
            Px.Button(p, looked ? "檢查過了" : "仔細檢查", 6, 98, 120, 18, looked ? Pal.PlumDeep : Pal.TealDark, looked ? Pal.PlumLight : Pal.Cream, Pal.Ink, () =>
            {
                message = C.FleaLook(it);
                Changed();
                Refresh();
            });
            Px.Button(p, "放回", 130, 98, 74, 18, Pal.PlumLine, Pal.Cream, Pal.Ink, () => ClearTable("把東西放回攤位上。"));
        }

        void Offer(InvItem it, long v)
        {
            bool bought;
            string res = C.FleaOffer(it, v, out bought);
            Changed();
            FleaDeal d = C.DealOf(it);
            if (bought || (d != null && d.refused)) ClearTable(res);
            else
            {
                message = res;
                Refresh();
            }
        }
    }
}
