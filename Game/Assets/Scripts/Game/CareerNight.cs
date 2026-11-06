using System;
using System.Collections.Generic;

namespace SSD
{
    public class NightPlace
    {
        public string id;
        public string name;         // 完整名稱
        public string sign;         // 招牌（最多 4 個字）
        public string desc;
        public bool office;         // 留在辦公室：疲勞只 +1
        public bool repeat;         // 一晚可以去好幾次
        public int act = 1;
    }

    public class NightOption
    {
        public string label;
        public string note;         // 不能選的原因
        public bool enabled = true;
        public Func<string> act;
    }

    public class NightScene
    {
        public string id;
        public string title;
        public string text;
        public bool multi;          // 做完一件事還可以繼續（商店）
        public readonly List<NightOption> options = new List<NightOption>();
    }

    /// <summary>
    /// 晚上（參考深空當鋪的夜間行動）：結算後可以留在辦公室整理，或出門跑地點。
    /// 每做一件事會累積疲勞，疲勞越高，隔天越容易睡過頭、錯過客人的電話。
    /// </summary>
    public partial class Career
    {
        public const int NightActions = 3;
        static readonly double[] OversleepTable = { 0, 0.05, 0.15, 0.3, 0.45, 0.6, 0.75 };

        public static readonly NightPlace[] Places =
        {
            new NightPlace { id = "office", name = "辦公室", sign = "辦公室", office = true, repeat = true,
                desc = "留下來整理：翻新庫存的設備，或研究明天的行情。" },
            new NightPlace { id = "market", name = "光華電子街", sign = "電子街",
                desc = "正派商店：照標價買賣，不能議價。也賣好用的道具，有時候會打折。" },
            new NightPlace { id = "flea", name = "深夜跳蚤市場", sign = "跳蚤市場",
                desc = "東西便宜到不行，可以殺價。但沒有保固，也不知道是哪裡來的。" },
            new NightPlace { id = "junk", name = "垃圾場", sign = "垃圾場",
                desc = "堆滿被丟掉的電器和雜物。挖幾堆看看，運氣好能挖到還能用的寶貝。" },
            new NightPlace { id = "stirfry", name = "阿財熱炒", sign = "熱炒",
                desc = "請廠商吃飯喝酒。關係變好，明天可能直接介紹案子給你。" },
            new NightPlace { id = "netcafe", name = "星光網咖", sign = "網咖",
                desc = "新人實況主和想買設備的人都在這裡出沒。" },
            new NightPlace { id = "temple", name = "福德宮", sign = "宮廟",
                desc = "添香油錢、求支籤，保佑明天的直播平平安安。" },
            new NightPlace { id = "roger", name = "巷口宵夜攤", sign = "宵夜",
                desc = "羅傑每天晚上都在這裡。請他吃宵夜，心情會好一點。" },
            new NightPlace { id = "bar", name = "頂樓酒吧", sign = "酒吧", act = 3,
                desc = "大老闆出沒的地方。可能談到大案子，也可能遇到可疑的人。" }
        };

        public static NightPlace Place(string id)
        {
            foreach (NightPlace p in Places) if (p.id == id) return p;
            return null;
        }

        public double OversleepChance
        {
            get { return OversleepTable[Math.Max(0, Math.Min(OversleepTable.Length - 1, s.fatigue))]; }
        }

        public long NightCost(string what)
        {
            switch (what)
            {
                case "stirfry": return CaseGen.Nice(4000 * TierF);
                case "netcafe": return CaseGen.Nice(1500 * TierF);
                case "temple": return CaseGen.Nice(1000 * TierF);
                case "roger": return CaseGen.Nice(2000 * TierF);
                case "bar": return CaseGen.Nice(20000 * TierF);
                default: return 0;
            }
        }

        // ------------------------------------------------------------------
        // 開始與結束
        // ------------------------------------------------------------------

        public void BeginNight()
        {
            if (s.nightDay == s.day) return;
            s.nightDay = s.day;
            s.nightLeft = NightActions;
            s.fatigue = 0;
            s.nightVisited.Clear();
            Random r = new Random(s.seed ^ (s.day * 6151 + 7));
            s.shopStock.Clear();
            s.fleaStock.Clear();
            List<ItemDef> pool = new List<ItemDef>();
            foreach (ItemDef it in w.items) if (it.act <= s.act && it.act >= s.act - 1) pool.Add(it);
            if (pool.Count == 0) foreach (ItemDef it in w.items) if (it.act <= s.act) pool.Add(it);
            for (int i = 0; i < 4 && pool.Count > 0; i++)
            {
                ItemDef it = pool[r.Next(pool.Count)];
                int g = r.NextDouble() < 0.5 ? 0 : 1;
                s.shopStock.Add(new InvItem
                {
                    id = s.NewUid("sh"), name = it.name, category = it.cat, grade = g, condition = CaseGen.GradeText[g],
                    baseValue = it.price, cost = CaseGen.Nice(CaseGen.Market(this, it, g) * 1.05)
                });
            }
            StockFlea(r, pool);
            s.junkDug.Clear();
            s.junkDigs = 0;
        }

        public bool CanVisit(NightPlace p, out string why)
        {
            why = null;
            if (p.act > s.act) { why = "第" + "一二三四"[Math.Max(0, Math.Min(3, p.act - 1))] + "幕開放"; return false; }
            if (s.nightLeft <= 0) { why = "時間不早了，該回家了"; return false; }
            if (!p.repeat && s.nightVisited.Contains(p.id)) { why = "今晚去過了"; return false; }
            if (p.id == "roger" && s.Talent("roger") == null) { why = "羅傑不在"; return false; }
            return true;
        }

        public bool Enter(NightPlace p)
        {
            string why;
            if (!CanVisit(p, out why)) return false;
            s.nightLeft--;
            s.fatigue += p.office ? 1 : 2;
            s.nightVisited.Add(p.id);
            s.AddStat("nights", 1);
            if (p.id == "junk") JunkStart();
            return true;
        }

        /// <summary>回家睡覺。決定明天會不會睡過頭。</summary>
        public void EndNight()
        {
            if (s.nightVisited.Count == 0)
            {
                s.restedDay = s.day;
                s.morning.Add("昨晚早早就睡了，今天精神很好。（今天的客人耐心 +1）");
                return;
            }
            Random r = new Random(s.seed ^ (s.day * 7177 + 3));
            if (r.NextDouble() < OversleepChance) s.oversleep = s.fatigue >= 5 ? 2 : 1;
        }

        void ApplyOversleep()
        {
            if (s.oversleep <= 0) return;
            Opportunity miss = null;
            foreach (Opportunity o in s.inbox)
            {
                if (o.showDay != s.day || o.urgent || o.lead) continue;
                if (miss == null || o.est > miss.est) miss = o;
            }
            if (miss != null)
            {
                s.inbox.Remove(miss);
                s.morning.Add("睡過頭了！手機裡有一通未接來電：" + miss.npcName + "（" + miss.company + "）。對方已經找別家了。");
            }
            else s.morning.Add("睡過頭了！還好今天早上沒有人打來。");
            if (s.oversleep >= 2)
            {
                s.slots = Math.Max(1, s.slots - 1);
                s.morning.Add("一覺睡到中午，今天少了一個通話時段。");
            }
            s.AddStat("overslept", 1);
            s.oversleep = 0;
        }

        // ------------------------------------------------------------------
        // 各地點
        // ------------------------------------------------------------------

        public NightScene Scene(string id)
        {
            NightScene sc = new NightScene { id = id };
            NightPlace p = Place(id);
            sc.title = p != null ? p.name : id;
            switch (id)
            {
                case "office": OfficeScene(sc); break;
                case "stirfry": StirfryScene(sc); break;
                case "netcafe": NetcafeScene(sc); break;
                case "temple": TempleScene(sc); break;
                case "roger": RogerScene(sc); break;
                case "bar": BarScene(sc); break;
            }
            return sc;
        }

        NightOption Opt(string label, Func<string> act)
        {
            return new NightOption { label = label, act = act };
        }

        NightOption Off(string label, string why)
        {
            return new NightOption { label = label, enabled = false, note = why };
        }

        Random NightRand(string key)
        {
            int h = 17;
            foreach (char c in key) h = unchecked(h * 31 + c);
            return new Random(s.seed ^ (s.day * 9973) ^ h ^ s.uidSeq);
        }

        void OfficeScene(NightScene sc)
        {
            sc.text = "辦公室只剩你一個人。窗外的招牌一閃一閃。要做點什麼？";
            int n = 0;
            foreach (InvItem it in s.inventory)
            {
                if (it.rentDays > 0 || n >= 4) continue;
                if (it.grade != 2 && it.grade != 4) continue;
                InvItem item = it;
                int to = it.grade == 4 ? 3 : 1;
                long cost = CaseGen.Nice(Math.Max(300, it.baseValue * 0.1));
                string label = "翻新" + it.name + "（變成" + CaseGen.GradeText[to] + "）" + GameState.Short(cost);
                if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
                else sc.options.Add(Opt(label, () => Refurbish(item, to, cost)));
                n++;
            }
            if (n == 0) sc.options.Add(Off("沒有可以翻新的設備", "只有「有刮傷」和「故障品」可以翻新"));
            sc.options.Add(Opt("研究明天的行情", StudyMarket));
        }

        string Refurbish(InvItem it, int to, long cost)
        {
            s.Spend(cost);
            it.cost += cost;
            bool surprise = Array.IndexOf(CaseGen.GradeText, it.condition) < it.grade;
            it.grade = to;
            it.condition = CaseGen.GradeText[to];
            return "忙到半夜，" + it.name + "整理成「" + it.condition + "」了。" + (surprise ? "拆開才發現它原本比賣家說的還糟。" : "");
        }

        string StudyMarket()
        {
            List<string> up = new List<string>();
            List<string> down = new List<string>();
            foreach (Trend t in s.market)
            {
                int d = t.next - t.pct;
                if (d >= 5) up.Add(t.cat + " +" + d + "%");
                else if (d <= -5) down.Add(t.cat + " " + d + "%");
            }
            if (up.Count == 0 && down.Count == 0) return "研究到很晚。明天的行情大致持平。";
            string text = "研究到很晚。明天";
            if (up.Count > 0) text += "會漲：" + string.Join("、", up.ToArray());
            if (down.Count > 0) text += (up.Count > 0 ? "；" : "") + "會跌：" + string.Join("、", down.ToArray());
            return text + "。";
        }

        long ShopPrice(InvItem it)
        {
            ItemDef def = w.Item(CaseGen.ItemIdOf(w, it));
            long m = def != null ? CaseGen.Market(this, def, it.grade) : CaseGen.Nice(it.baseValue * CaseGen.GradeFactor[Math.Min(4, it.grade)]);
            return CaseGen.Nice(m * 0.85);
        }

        string ShopBuy(InvItem it)
        {
            s.Spend(it.cost);
            s.shopStock.Remove(it);
            it.id = s.NewUid("inv");
            it.boughtDay = s.day;
            s.inventory.Add(it);
            return "買了" + it.name + "，花了 " + GameState.Short(it.cost) + "。";
        }

        string ShopSell(InvItem it, long price)
        {
            string truth = Array.IndexOf(CaseGen.GradeText, it.condition) < it.grade ? "老闆驗完貨說：「這台其實是" + CaseGen.GradeText[it.grade] + "喔。」" : "";
            s.Earn(price);
            s.lastProfit = price - it.cost;
            s.inventory.Remove(it);
            s.inbox.RemoveAll(o => o.type == "sell" && o.item == it.id);
            s.AddStat("tradeProfit", s.lastProfit);
            return truth + "賣掉" + it.name + "，拿到 " + GameState.Short(price) + "（利潤 " + GameState.Money(s.lastProfit) + "）。";
        }

        string ShopRefuse(InvItem it)
        {
            s.AddRep(-1);
            return "老闆查了序號，臉色一變：「這是失竊登記的東西，我們不收。」";
        }

        string FleaBuy(InvItem it)
        {
            s.Spend(it.cost);
            s.fleaStock.Remove(it);
            it.id = s.NewUid("inv");
            it.boughtDay = s.day;
            s.inventory.Add(it);
            if (it.flags == "stolen")
                s.events.Add(new Opportunity { uid = s.NewUid("e"), type = "police", item = it.id, showDay = s.day + 1 + new Random(s.seed ^ s.uidSeq).Next(4), amount = it.cost });
            return "用 " + GameState.Short(it.cost) + " 買下" + it.name + "。老闆收錢收得很快。";
        }

        List<BrandDef> NightBrands(Random r, int n)
        {
            List<BrandDef> list = new List<BrandDef>();
            foreach (BrandDef b in w.brands) if (b.act <= s.act) list.Add(b);
            // 先給每個品牌一個固定的亂數，再依關係排序（比較函式不能每次回傳不同結果）
            Dictionary<string, int> key = new Dictionary<string, int>();
            foreach (BrandDef b in list) key[b.name] = r.Next(1000);
            list.Sort((a, b) =>
            {
                int ra = s.RelValue(string.IsNullOrEmpty(a.npc) ? "g_" + a.name : a.npc);
                int rb = s.RelValue(string.IsNullOrEmpty(b.npc) ? "g_" + b.name : b.npc);
                if (ra != rb) return rb - ra;
                return key[a.name] - key[b.name];
            });
            List<BrandDef> pick = new List<BrandDef>();
            foreach (BrandDef b in list)
            {
                if (pick.Count >= n) break;
                if (r.NextDouble() < 0.6 || list.Count - list.IndexOf(b) <= n - pick.Count) pick.Add(b);
            }
            return pick;
        }

        void StirfryScene(NightScene sc)
        {
            sc.text = "熱炒店裡吵吵鬧鬧，啤酒一手一手地上。幾位認識的廠商剛好也在。";
            Random r = NightRand("stirfry");
            long cost = NightCost("stirfry");
            foreach (BrandDef b in NightBrands(r, 3))
            {
                BrandDef brand = b;
                string npc = string.IsNullOrEmpty(b.npc) ? "g_" + b.name : b.npc;
                string name = NpcNameFor(w, npc, b.name);
                string label = "請" + name + "（" + b.name + "）喝一攤 " + GameState.Short(cost);
                if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
                else sc.options.Add(Opt(label, () => Treat(brand, npc, name, cost)));
            }
            sc.options.Add(Opt("自己吃一盤炒飯就走", () => "炒飯很香。你一個人吃完，默默回家。"));
        }

        string Treat(BrandDef b, string npc, string name, long cost)
        {
            s.Spend(cost);
            s.AddRel(npc, name, 2);
            Random r = NightRand("treat" + b.name);
            string rumor = Rumor(r);
            if (r.NextDouble() < 0.65)
            {
                Opportunity op = MakeOpp("sponsor", r);
                if (op != null)
                {
                    SetClient(op, b);
                    op.amount = CaseGen.Nice(op.amount * 1.3);
                    op.est = op.amount;
                    op.title = b.name + "工商";
                    op.lead = true;
                    s.inbox.Add(op);
                    return name + "喝得很開心：「明天有個案子，我直接找你！」（明天信箱會有" + b.name + "的工商）" + rumor;
                }
            }
            return name + "聊得很開心，但今天沒有談到案子。關係變好了。" + rumor;
        }

        string Rumor(Random r)
        {
            List<Trend> big = s.market.FindAll(x => Math.Abs(x.next - x.pct) >= 6);
            if (big.Count == 0) return "";
            Trend t = big[r.Next(big.Count)];
            return "\n席間聽說：明天" + t.cat + (t.next > t.pct ? "會漲。" : "會跌。");
        }

        void NetcafeScene(NightScene sc)
        {
            sc.text = "網咖裡滿滿的人。有人在開台，有人在討論新設備。";
            sc.options.Add(Opt("到處看看新人開台（免費）", ScoutTalent));
            long cost = NightCost("netcafe");
            string label = "請網咖老闆喝飲料，打聽誰想買設備 " + GameState.Short(cost);
            bool any = s.inventory.Exists(i => i.rentDays <= 0 && !s.inbox.Exists(o => o.type == "sell" && o.item == i.id));
            if (!any) sc.options.Add(Off(label, "庫存裡沒有可以賣的東西"));
            else if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
            else sc.options.Add(Opt(label, () => FindBuyer(cost)));
        }

        string ScoutTalent()
        {
            Random r = NightRand("scout");
            if (s.talents.Count >= 5) return "看了一整晚。你旗下的藝人已經夠多了，先專心帶好他們吧。";
            if (s.inbox.Exists(o => o.type == "talent")) return "看了一整晚。信箱裡已經有一位想加盟的新人了。";
            if (r.NextDouble() < 0.55)
            {
                Opportunity op = MakeOpp("talent", r);
                if (op != null)
                {
                    op.lead = true;
                    op.expires = s.day + 1;
                    s.inbox.Add(op);
                    return "你注意到一個很有趣的台：" + op.npcName + "（" + op.company + "）。聊了幾句，對方說明天會打給你。";
                }
            }
            return "看了一整晚，今天沒看到特別的新人。";
        }

        string FindBuyer(long cost)
        {
            s.Spend(cost);
            Random r = NightRand("buyer");
            Opportunity op = MakeOpp("sell", r);
            if (op == null) return "老闆想了想：「最近好像沒有人在找耶。」";
            op.amount = CaseGen.Nice(op.amount * 1.15);
            op.est = op.amount;
            op.lead = true;
            s.inbox.Add(op);
            return "老闆說：「有喔，" + op.company + "的" + op.npcName + "在找" + op.title.Replace("想買", "") + "，出價不錯。」（明天信箱會有這位買家）";
        }

        static readonly string[] GoodLots =
        {
            "上上籤：「財源廣進，口舌無災。」廟公說：明天什麼事都會很順。",
            "上上籤：「貴人相助，事事如意。」"
        };
        static readonly string[] MidLots =
        {
            "中籤：「守成為上，勿貪近利。」",
            "中籤：「謀事在人，成事在天。」",
            "中籤：「凡事三思，自然無憂。」"
        };
        static readonly string[] BadLots =
        {
            "下籤：「小心口舌之爭。」你想到了羅傑。",
            "下籤：「錢財易散，宜守不宜攻。」"
        };

        void TempleScene(NightScene sc)
        {
            sc.text = "香煙裊裊。廟公坐在門口：「少年仔，來拜一下保平安啦。」";
            long cost = NightCost("temple");
            string label = "添香油錢 " + GameState.Short(cost) + "（明天出包機率減半）";
            if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
            else sc.options.Add(Opt(label, () =>
            {
                s.Spend(cost);
                s.luckDay = s.day;
                return "你虔誠地拜了拜，把今天的事都跟土地公說了。明天的直播應該會平安一點。";
            }));
            sc.options.Add(Opt("抽一支籤（免費）", () =>
            {
                Random r = NightRand("lot");
                double x = r.NextDouble();
                if (x < 0.25)
                {
                    s.luckDay = s.day;
                    return GoodLots[r.Next(GoodLots.Length)] + "（明天出包機率減半）";
                }
                if (x < 0.75) return MidLots[r.Next(MidLots.Length)];
                return BadLots[r.Next(BadLots.Length)];
            }));
        }

        static readonly string[] RogerLines =
        {
            "「老闆，我跟你說，甜的飲料真的不行，喝了會想吐。」",
            "「今天聊天室有人說我胖……我只是剛吃飽而已啦。」",
            "「鬼東哥以前也常帶我來這攤，他都點滷大腸。」",
            "「老闆，你覺得我有一天會變成百萬實況主嗎？」",
            "「我最近在練新遊戲，下次工商我會好好表現的！」"
        };
        static readonly string[] RogerVLines =
        {
            "「V老大今天又罵我了……可是她罵人的樣子好帥喔。」",
            "「老闆，你覺得 V老大喜歡什麼樣的人啊？我只是問問。」",
            "「欸老闆，婚紗……我是說，攝影棚，租一天要多少錢？」"
        };

        void RogerScene(NightScene sc)
        {
            sc.text = "羅傑蹲在宵夜攤旁邊吃滷味：「老闆！你也來啦？」";
            long cost = NightCost("roger");
            TalentState t = s.Talent("roger");
            string label = "請羅傑吃宵夜 " + GameState.Short(cost) + "（心情 +15、信任 +5）";
            if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
            else sc.options.Add(Opt(label, () =>
            {
                s.Spend(cost);
                if (t != null)
                {
                    t.mood = Math.Min(100, t.mood + 15);
                    t.trust = Math.Min(100, t.trust + 5);
                }
                Random r = NightRand("roger");
                bool v = s.Has("vjoined") && r.NextDouble() < 0.6;
                string line = v ? RogerVLines[r.Next(RogerVLines.Length)] : RogerLines[r.Next(RogerLines.Length)];
                return "羅傑吃得很開心。" + line;
            }));
            sc.options.Add(Opt("打個招呼就走", () => "「老闆晚安！明天見！」羅傑揮了揮竹籤。"));
        }

        void BarScene(NightScene sc)
        {
            sc.text = "頂樓的風很大，酒很貴。角落坐著幾個看起來很有錢的人。";
            long cost = NightCost("bar");
            string label = "找大老闆聊聊 " + GameState.Short(cost);
            if (s.cash < cost) sc.options.Add(Off(label, "現金不夠"));
            else sc.options.Add(Opt(label, () =>
            {
                s.Spend(cost);
                Random r = NightRand("bar");
                double x = r.NextDouble();
                if (x < 0.5)
                {
                    Opportunity op = MakeOpp("sponsor", r);
                    if (op != null)
                    {
                        op.amount = CaseGen.Nice(op.amount * 1.8);
                        op.est = op.amount;
                        op.lead = true;
                        s.inbox.Add(op);
                        return "一位穿西裝的大老闆對你很有興趣：「" + op.brand + "正在找人，明天我叫人聯絡你。」";
                    }
                }
                else if (x < 0.75)
                {
                    Opportunity op = MakeOpp("gray", r);
                    if (op != null)
                    {
                        op.lead = true;
                        s.inbox.Add(op);
                        return "一個戴墨鏡的男人塞給你一張名片：「" + op.company + "。報酬很好，就是要快。」";
                    }
                }
                return "大老闆們聊的都是高爾夫。你喝了一杯很貴的酒，什麼都沒談到。";
            }));
            sc.options.Add(Opt("看看夜景就好", () => "城市的燈很漂亮。你想起還有三千萬要賺。"));
        }
    }
}
