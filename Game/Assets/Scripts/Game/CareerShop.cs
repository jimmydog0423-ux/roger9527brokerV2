using System;
using System.Collections.Generic;

namespace SSD
{
    /// <summary>電子街賣的道具（不是庫存商品）：買了馬上用，或一直有效的工具。</summary>
    public class Supply
    {
        public string id;
        public string name;
        public string icon;
        public string use;
        public long basePrice;
        public bool tool;           // 工具：買一次一直有效
    }

    /// <summary>垃圾場挖一堆的結果。</summary>
    public class JunkFind
    {
        public string kind;         // nothing / scrap / coins / hurt / item
        public InvItem item;
        public long cash;
        public string text;
        public bool lost;           // 庫存滿了帶不走
    }

    /// <summary>
    /// 晚上的商店、跳蚤市場和垃圾場。
    /// 電子街是正派商店：照標價買賣，不能議價，但星期幾會有活動打折。
    /// 跳蚤市場可以殺價，先仔細檢查找出問題，老闆會心虛、比較好談。
    /// 垃圾場可以挖寶：一晚挖幾堆，可能挖到故障品、刮傷品，運氣好挖到近全新的好貨。
    /// </summary>
    public partial class Career
    {
        // ------------------------------------------------------------------
        // 星期活動
        // ------------------------------------------------------------------

        /// <summary>今晚是星期幾（0 是週一）。晚上的日期是剛結算完的那一天。</summary>
        public int NightWeekday
        {
            get { return (Math.Max(2, s.day) - 2) % 7; }
        }

        /// <summary>這個地點今晚的活動，沒有活動回傳 null。</summary>
        public string EventText(string place)
        {
            int d = NightWeekday;
            switch (place)
            {
                case "market":
                    if (d == 0) return "週一新品日：全新、近全新 9 折";
                    if (d == 4) return "週五收購祭：賣給店家多 1 成";
                    if (d == 5) return "週六特賣：全館 85 折";
                    return null;
                case "flea":
                    if (d == 2) return "週三清倉夜：老闆開價 8 折";
                    if (d == 3) return "週四新貨到：多兩個攤位";
                    return null;
                case "junk":
                    if (d == 6) return "週日回收日：多四堆，好貨變多";
                    return null;
            }
            return null;
        }

        /// <summary>電子街的折扣（1 是原價）。</summary>
        public double ShopDiscount(InvItem it)
        {
            if (NightWeekday == 5) return 0.85;
            if (NightWeekday == 0 && it != null && it.grade <= 1) return 0.9;
            return 1.0;
        }

        public long ShopPriceNow(InvItem it)
        {
            return CaseGen.Nice(it.cost * ShopDiscount(it));
        }

        /// <summary>店家收購價（週五多一成）。</summary>
        public long ShopSellPrice(InvItem it)
        {
            return CaseGen.Nice(ShopPrice(it) * (NightWeekday == 4 ? 1.1 : 1.0));
        }

        /// <summary>這件商品現在的行情（照它真正的狀況）。</summary>
        public long MarketOf(InvItem it)
        {
            return ShopPrice(it) * 100 / 85;
        }

        public string ItemUse(InvItem it)
        {
            ItemDef def = w.Item(CaseGen.ItemIdOf(w, it));
            return def != null && !string.IsNullOrEmpty(def.use) ? def.use : "直播用的器材";
        }

        public string ItemIcon(InvItem it)
        {
            if (it.id == "inv_cx4k" || it.name == "擷取卡 CX-4K60") return "capcard";
            ItemDef def = w.Item(CaseGen.ItemIdOf(w, it));
            return def != null ? def.icon : "box";
        }

        // ------------------------------------------------------------------
        // 電子街：道具
        // ------------------------------------------------------------------

        public static readonly Supply[] Supplies =
        {
            new Supply { id = "drink", name = "提神飲料", icon = "drink", use = "喝了精神好，疲勞 -1", basePrice = 300 },
            new Supply { id = "gift", name = "宵夜禮盒", icon = "gift", use = "旗下藝人心情全部 +8", basePrice = 2500 },
            new Supply { id = "detector", name = "金屬探測器", icon = "detector", use = "垃圾場每次多挖 2 堆", basePrice = 15000, tool = true },
            new Supply { id = "headlamp", name = "頭燈", icon = "headlamp", use = "看得出垃圾場哪堆有貨", basePrice = 8000, tool = true }
        };

        public bool HasTool(string id)
        {
            return s.Has("tool_" + id);
        }

        public long SupplyPrice(Supply sp)
        {
            double k = sp.tool ? 1.0 : TierF;
            if (NightWeekday == 5) k *= 0.85;
            return CaseGen.Nice(sp.basePrice * k);
        }

        /// <summary>電子街貨架上的道具（工具已經有了就不擺）。</summary>
        public List<Supply> ShopSupplies()
        {
            List<Supply> list = new List<Supply>();
            foreach (Supply sp in Supplies) if (!sp.tool || !HasTool(sp.id)) list.Add(sp);
            return list;
        }

        public string BuySupply(Supply sp)
        {
            long price = SupplyPrice(sp);
            if (sp.tool && HasTool(sp.id)) return "你已經有" + sp.name + "了。";
            if (s.cash < price) return "現金不夠。";
            if (sp.id == "drink" && s.fatigue <= 0) return "你現在精神很好，不用喝。";
            if (sp.id == "gift" && s.talents.Count == 0) return "旗下沒有藝人可以送。";
            s.Spend(price);
            switch (sp.id)
            {
                case "drink":
                    s.fatigue = Math.Max(0, s.fatigue - 1);
                    return "一口氣喝完，精神來了。（疲勞 -1）";
                case "gift":
                    foreach (TalentState t in s.talents) t.mood = Math.Min(100, t.mood + 8);
                    return "請店家把禮盒送到大家家裡。（旗下藝人心情 +8）";
                case "detector":
                    s.Set("tool_detector");
                    return "買了金屬探測器。之後去垃圾場，每次可以多挖 2 堆。";
                default:
                    s.Set("tool_" + sp.id);
                    return "買了頭燈。垃圾場裡哪幾堆有東西，一照就知道。";
            }
        }

        // ------------------------------------------------------------------
        // 電子街：買賣（照標價，不能議價）
        // ------------------------------------------------------------------

        public string ShopBuyNow(InvItem it)
        {
            if (!s.shopStock.Contains(it)) return "這件已經賣掉了。";
            if (s.inventory.Count >= InventoryCap) return "庫存滿了，先賣掉一些再來。";
            long price = ShopPriceNow(it);
            if (s.cash < price) return "現金不夠。";
            it.cost = price;
            return ShopBuy(it);
        }

        public string ShopSellNow(InvItem it)
        {
            if (!s.inventory.Contains(it)) return "這件已經不在庫存了。";
            if (it.rentDays > 0) return "出租中的東西不能賣。";
            if (it.flags == "stolen") return ShopRefuse(it);
            return ShopSell(it, ShopSellPrice(it));
        }

        // ------------------------------------------------------------------
        // 跳蚤市場：可以殺價
        // ------------------------------------------------------------------

        public FleaDeal DealOf(InvItem it)
        {
            if (it == null) return null;
            return s.fleaDeals.Find(d => d.id == it.id);
        }

        /// <summary>出這個價錢，老闆答應的機率（0~100）。</summary>
        public int FleaChance(InvItem it, long v)
        {
            FleaDeal d = DealOf(it);
            if (d == null || d.refused) return 0;
            if (v >= d.floor) return 100;
            if (v <= d.walk) return 0;
            double t = (d.floor - v) / (double)Math.Max(1, d.floor - d.walk);
            return Math.Max(5, Math.Min(95, (int)Math.Round(95 * (1 - t))));
        }

        /// <summary>老闆還剩幾次耐心（殺價失敗一次扣一格，亂喊扣兩格）。</summary>
        public int FleaPatience(InvItem it)
        {
            FleaDeal d = DealOf(it);
            return d == null || d.refused ? 0 : Math.Max(0, 3 - d.misses);
        }

        public string FleaOffer(InvItem it, long v, out bool bought)
        {
            bought = false;
            FleaDeal d = DealOf(it);
            if (d == null || !s.fleaStock.Contains(it)) return "這攤已經收了。";
            if (d.refused) return "老闆揮揮手：「不賣你了啦。」";
            if (s.inventory.Count >= InventoryCap) return "庫存滿了，帶不回去。";
            if (s.cash < v) return "現金不夠。";
            int chance = FleaChance(it, v);
            Random r = NightRand("flea" + it.id + "_" + d.misses);
            if (chance >= 100 || (chance > 0 && r.Next(100) < chance))
            {
                long ask = it.cost;
                it.cost = v;
                bought = true;
                string head = v < ask ? "老闆嘆了口氣：「好啦好啦，" + GameState.Short(v) + " 給你。」" : "";
                return head + FleaBuy(it);
            }
            d.misses += chance <= 0 ? 2 : 1;
            if (d.misses >= 3)
            {
                d.refused = true;
                return chance <= 0 ? "老闆臉一沉：「你在開玩笑嗎？不賣了！」" : "老闆不耐煩了：「殺成這樣，不賣你了！」";
            }
            if (chance <= 0) return "老闆瞪了你一眼：「這價錢你也說得出口？」（成功率 0%，老闆很不爽）";
            // 沒談成：老闆的底線收緊一點，同一個價錢一直喊會越來越難
            d.walk += (d.floor - d.walk) / 3;
            return "老闆搖頭：「" + GameState.Short(d.floor) + " 啦，不能再少了。」（剛剛的成功率 " + chance + "%）";
        }

        /// <summary>仔細檢查：找到問題，老闆會心虛，底線跟著降。</summary>
        public string FleaLook(InvItem it)
        {
            FleaDeal d = DealOf(it);
            if (d == null) return "這攤已經收了。";
            if (d.looked) return "已經仔細看過了。";
            d.looked = true;
            Random r = NightRand("look" + it.id);
            int claimed = Math.Max(0, Array.IndexOf(CaseGen.GradeText, it.condition));
            List<string> found = new List<string>();
            double drop = 1;
            if (it.grade > claimed && r.NextDouble() < 0.75)
            {
                found.Add("它其實是「" + CaseGen.GradeText[it.grade] + "」，不是「" + it.condition + "」");
                drop *= CaseGen.GradeFactor[Math.Min(4, it.grade)] / CaseGen.GradeFactor[claimed];
                it.condition = CaseGen.GradeText[it.grade];
            }
            if (it.flags == "stolen" && r.NextDouble() < 0.6)
            {
                found.Add("序號被磨掉了，很可能是贓物");
                drop *= 0.8;
            }
            if (found.Count == 0) return "翻來覆去看了一遍，看起來沒什麼問題。";
            d.floor = CaseGen.Nice(d.floor * drop);
            d.walk = CaseGen.Nice(d.walk * drop);
            s.AddStat("fleaFound", 1);
            return "你發現" + string.Join("，而且", found.ToArray()) + "。老闆有點心虛，價錢好談多了。";
        }

        /// <summary>跳蚤市場今晚的攤位（BeginNight 時呼叫）。</summary>
        void StockFlea(Random r, List<ItemDef> pool)
        {
            s.fleaStock.Clear();
            s.fleaDeals.Clear();
            int n = 3 + (NightWeekday == 3 ? 2 : 0);
            double k = NightWeekday == 2 ? 0.8 : 1.0;
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                ItemDef it = pool[r.Next(pool.Count)];
                int claimed = r.Next(3);
                int real = Math.Min(4, claimed + new[] { 0, 0, 1, 2 }[r.Next(4)]);
                bool stolen = r.NextDouble() < (s.act >= 2 ? 0.2 : 0.12);
                long cost = CaseGen.Nice(CaseGen.Market(this, it, claimed) * (0.45 + r.NextDouble() * 0.25) * k);
                InvItem item = new InvItem
                {
                    id = s.NewUid("fl"), name = it.name, category = it.cat, grade = real, condition = CaseGen.GradeText[claimed],
                    baseValue = it.price, flags = stolen ? "stolen" : "", cost = cost
                };
                s.fleaStock.Add(item);
                s.fleaDeals.Add(new FleaDeal
                {
                    id = item.id,
                    floor = CaseGen.Nice(cost * (0.78 + r.NextDouble() * 0.1)),
                    walk = CaseGen.Nice(cost * (0.5 + r.NextDouble() * 0.1))
                });
            }
        }

        // ------------------------------------------------------------------
        // 垃圾場：挖寶
        // ------------------------------------------------------------------

        public static readonly string[] JunkKindName = { "電器堆", "紙箱堆", "雜物堆" };

        public int JunkCount
        {
            get { return 12 + (NightWeekday == 6 ? 4 : 0); }
        }

        public int JunkDigsMax
        {
            get { return 3 + (HasTool("detector") ? 2 : 0); }
        }

        /// <summary>進垃圾場時呼叫：重設這一趟能挖的次數。</summary>
        public void JunkStart()
        {
            s.junkDug.Clear();
            s.junkDigs = JunkDigsMax;
        }

        Random JunkRand(int i)
        {
            return new Random(s.seed ^ (s.nightDay * 7919) ^ (i * 104729 + 13));
        }

        public int JunkKind(int i)
        {
            return JunkRand(i).Next(3);
        }

        /// <summary>這一堆裡面有沒有商品（頭燈看得到）。</summary>
        public bool JunkHasItem(int i)
        {
            return JunkPeek(i).kind == "item";
        }

        /// <summary>這一堆裡面是什麼（不會改變任何狀態）。</summary>
        JunkFind JunkPeek(int i)
        {
            Random r = JunkRand(i);
            int kind = r.Next(3);
            bool sunday = NightWeekday == 6;
            double good = (kind == 1 ? 0.05 : kind == 0 ? 0.03 : 0.02) * (sunday ? 2 : 1);
            double scratch = kind == 1 ? 0.15 : kind == 0 ? 0.12 : 0.08;
            double broken = (kind == 0 ? 0.40 : kind == 1 ? 0.15 : 0.20) * (sunday ? 1.2 : 1);
            double scrap = kind == 0 ? 0.25 : kind == 1 ? 0.12 : 0.38;
            double coins = kind == 1 ? 0.08 : 0.02;
            double hurt = kind == 2 ? 0.10 : 0.03;
            double x = r.NextDouble();
            JunkFind f = new JunkFind();
            int grade = -1;
            if ((x -= good) < 0) grade = 1;
            else if ((x -= scratch) < 0) grade = 2;
            else if ((x -= broken) < 0) grade = 4;
            if (grade >= 0)
            {
                int maxAct = grade == 1 ? Math.Min(4, s.act + 1) : s.act;
                List<ItemDef> pool = new List<ItemDef>();
                foreach (ItemDef it in w.items) if (it.act <= maxAct && it.act >= maxAct - 1) pool.Add(it);
                if (pool.Count == 0) foreach (ItemDef it in w.items) if (it.act <= maxAct) pool.Add(it);
                ItemDef def = pool[r.Next(pool.Count)];
                f.kind = "item";
                f.item = new InvItem
                {
                    name = def.name, category = def.cat, grade = grade, condition = CaseGen.GradeText[grade],
                    baseValue = def.price, cost = 0, flags = ""
                };
                f.text = grade == 1 ? "挖到寶了！一台幾乎全新的" + def.name + "！"
                    : grade == 2 ? "挖出一台" + def.name + "（有刮傷）。擦一擦還能用！"
                    : "挖出一台" + def.name + "（故障品）。帶回辦公室翻新，就能賣錢。";
                return f;
            }
            if ((x -= scrap) < 0)
            {
                f.kind = "scrap";
                f.cash = CaseGen.Nice((200 + r.Next(600)) * TierF);
                f.text = "挖到一堆廢鐵，拿去回收換了 " + GameState.Money(f.cash) + "。";
                return f;
            }
            if ((x -= coins) < 0)
            {
                f.kind = "coins";
                f.cash = CaseGen.Nice((300 + r.Next(1200)) * TierF);
                f.text = "紙箱底下有一包被丟掉的零錢，" + GameState.Money(f.cash) + "。";
                return f;
            }
            if ((x -= hurt) < 0)
            {
                f.kind = "hurt";
                f.text = "被躲在裡面的野貓抓了一下！（疲勞 +1）";
                return f;
            }
            f.kind = "nothing";
            string[] none = { "只翻到發臭的便當盒和舊報紙。", "一堆壞掉的延長線，沒有用。", "裡面只有一隻嚇壞的蟑螂。", "空空的紙箱，什麼都沒有。" };
            f.text = none[r.Next(none.Length)];
            return f;
        }

        /// <summary>挖一堆。不能挖時回傳 null。</summary>
        public JunkFind Dig(int i)
        {
            if (i < 0 || i >= JunkCount || s.junkDigs <= 0 || s.junkDug.Contains(i)) return null;
            s.junkDigs--;
            s.junkDug.Add(i);
            s.AddStat("junkDigs", 1);
            JunkFind f = JunkPeek(i);
            switch (f.kind)
            {
                case "scrap":
                case "coins":
                    s.Earn(f.cash);
                    break;
                case "hurt":
                    s.fatigue++;
                    break;
                case "item":
                    if (s.inventory.Count >= InventoryCap)
                    {
                        f.lost = true;
                        f.text += "可惜庫存滿了，帶不走。";
                    }
                    else
                    {
                        f.item.id = s.NewUid("inv");
                        f.item.boughtDay = s.day;
                        s.inventory.Add(f.item);
                        s.AddStat("junkFinds", 1);
                    }
                    break;
            }
            return f;
        }
    }
}
