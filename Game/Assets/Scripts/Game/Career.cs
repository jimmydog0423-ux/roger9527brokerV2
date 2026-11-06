using System;
using System.Collections.Generic;

namespace SSD
{
    public class DayReport
    {
        public int day;
        public long revenue;
        public long cost;
        public long profit;
        public long cash;
        public bool ending;
        public readonly List<string> lines = new List<string>();
    }

    /// <summary>
    /// 正式營業（企劃書第四章主要遊戲循環）：每日委託、通話時段、案件結果、
    /// 跨日後果（工商履約、羅傑出包、追償、查稅）、投資配息、出租、行情、公司升級與主線進度。
    /// 不碰畫面，所有數字都在這裡算。
    /// </summary>
    public partial class Career
    {
        public const long Goal = 30000000;
        static readonly double[] Tiers = { 1, 5, 14, 35 };
        static readonly long[] ActCash = { 0, 1000000, 5000000, 15000000 };
        static readonly long[] Overheads = { 3000, 12000, 40000, 90000 };
        static readonly long[] VSalary = { 0, 6000, 15000, 30000 };
        static readonly string[] ActNames = { "第一幕：一人工作室", "第二幕：水蛇洞開始賺錢", "第三幕：實況產業商人", "第四幕：三千萬計畫" };
        static readonly string[] ShadyCo = { "宏遠傳播", "金鑫娛樂", "天成行銷", "永盛國際", "大發媒體" };
        const string TsukiStudio = "月希工作室";
        static readonly KeyValuePair<string, string>[] LegacyRosterLabels =
        {
            new KeyValuePair<string, string>("阿凱的餐車", "街角餐車"),
            new KeyValuePair<string, string>("米亞音樂坊", "節拍音樂坊"),
            new KeyValuePair<string, string>("大熊戶外台", "山徑戶外台"),
            new KeyValuePair<string, string>("夜貓製作所", "深夜製作所"),
            new KeyValuePair<string, string>("小葵", "月希"),
            new KeyValuePair<string, string>("阿凱", "蛋頭"),
            new KeyValuePair<string, string>("米亞", "偷米"),
            new KeyValuePair<string, string>("大熊", "哈耿"),
            new KeyValuePair<string, string>("夜貓", "薛喜"),
            new KeyValuePair<string, string>("阿哲", "肯特"),
            new KeyValuePair<string, string>("莉莉", "Vivi 芝衣"),
            new KeyValuePair<string, string>("老K", "NL"),
            new KeyValuePair<string, string>("小雨", "阿森"),
            new KeyValuePair<string, string>("鐵頭", "虧皮"),
            new KeyValuePair<string, string>("布丁", "EASON"),
            new KeyValuePair<string, string>("阿傑", "MMD"),
            new KeyValuePair<string, string>("糖糖", "威傅"),
            new KeyValuePair<string, string>("蛋黃", "超負荷"),
            new KeyValuePair<string, string>("柚子", "花輪"),
            new KeyValuePair<string, string>("綠茶", "威傅"),
            new KeyValuePair<string, string>("小亮", "超負荷"),
            new KeyValuePair<string, string>("ZOD", "花輪")
        };

        public readonly GameState s;
        public readonly WorldData w;
        public readonly Dictionary<string, CaseGen.SponsorFacts> facts = new Dictionary<string, CaseGen.SponsorFacts>();

        public Career(GameState s, WorldData w)
        {
            this.s = s;
            this.w = w;
        }

        // ------------------------------------------------------------------
        // 數值
        // ------------------------------------------------------------------

        int ActIndex { get { return Math.Max(0, Math.Min(3, s.act - 1)); } }
        public long Tier { get { return (long)Math.Round(Tiers[ActIndex]); } }
        public double TierF { get { return Tiers[ActIndex]; } }
        public double NetBonus { get { return s.HasUpgrade("network2") ? 1.2 : 1.0; } }
        public int SlotsMax { get { return 3 + (s.Has("vjoined") ? 1 : 0) + (s.HasUpgrade("office") ? 1 : 0); } }
        public int DelegatesMax { get { return s.Has("vjoined") ? 2 + (s.HasUpgrade("vteam") ? 2 : 0) : 0; } }
        public int InboxSize { get { return 6; } }
        public int InventoryCap { get { return s.HasUpgrade("storage2") ? 10 : s.HasUpgrade("storage1") ? 6 : 3; } }
        public long Overhead { get { return Overheads[ActIndex] + (s.Has("vjoined") ? VSalary[ActIndex] : 0); } }
        public static string ActName(int act) { return ActNames[Math.Max(0, Math.Min(3, act - 1))]; }
        public long NextGoal { get { return s.act >= 4 ? Goal : ActCash[s.act]; } }

        public static long TalentFee(TalentState t)
        {
            return Math.Max(6000, (long)(t.fans * 0.7));
        }

        /// <summary>藝人類型的行情係數：電競和美妝比較貴，聊天台比較便宜。</summary>
        public static double GenreWeight(string genre)
        {
            switch (genre)
            {
                case "電競選手":
                case "電競賽評": return 1.3;
                case "美妝實況": return 1.2;
                case "歌唱實況":
                case "饒舌創作": return 1.1;
                case "料理實況": return 0.9;
                case "聊天實況": return 0.85;
                default: return 1.0;   // 遊戲實況、戶外實況
            }
        }

        /// <summary>
        /// 藝人接一場工商的標準報價（仲介案用）：
        /// （2 萬 + 粉絲數 × 1.5）× 類型係數 × 這一幕的行情倍率。粉絲越多、類型越熱門，報價越高。
        /// </summary>
        public long TalentQuote(TalentDef t)
        {
            return CaseGen.Nice((20000 + t.fans * 1.5) * GenreWeight(t.genre) * TierF);
        }

        public static string NpcNameFor(WorldData w, string npc, string key)
        {
            switch (npc)
            {
                case "lin": return "林經理";
                case "wang": return "王小姐";
                case "zhou": return "周老闆";
                case "zhang": return "張經理";
                case "chen": return "陳老闆";
                case "kui": return "月希";
                case "kai": return "蛋頭";
                default: return CaseGen.NpcName(w, key);
            }
        }

        public long ItemMarket(InvItem inv)
        {
            ItemDef def = w.Item(CaseGen.ItemIdOf(w, inv));
            int known = Math.Max(0, Array.IndexOf(CaseGen.GradeText, inv.condition));
            if (def == null) return CaseGen.Nice(inv.baseValue * CaseGen.GradeFactor[Math.Min(4, known)]);
            return CaseGen.Market(this, def, known);
        }

        // ------------------------------------------------------------------
        // 開始正式營業
        // ------------------------------------------------------------------

        public static void Init(GameState s, WorldData w, bool skipped)
        {
            s.career = true;
            s.tutorialDone = true;
            if (skipped)
            {
                s.cash = 500000;
                s.day = 9;
                s.Set("skipped");
            }
            if (s.seed == 12345 || s.seed == 0) s.seed = (Environment.TickCount & 0x7fffffff) | 1;
            if (s.Talent("roger") == null)
            {
                foreach (TalentDef t in w.talents)
                    if (t.id == "roger") s.talents.Add(FromDef(t, 70, 60));
            }
            for (int i = 0; i < s.investments.Count; i++)
            {
                Investment v = s.investments[i];
                if (string.IsNullOrEmpty(v.refId)) { v.refId = "pro" + i; v.quality = 1; v.periods = 6; v.startDay = s.day; v.status = "active"; }
                if (v.name != null && v.name.StartsWith(TsukiStudio, StringComparison.Ordinal))
                {
                    s.Set("mem_kui");
                    s.Set("kui_invested");
                }
            }
            foreach (InvItem it in s.inventory)
            {
                if (!string.IsNullOrEmpty(it.category)) continue;
                bool cap = it.name != null && it.name.Contains("擷取卡");
                it.category = cap ? "擷取卡" : "雜物";
                it.grade = cap ? 3 : 2;
                it.baseValue = cap ? 5200 : Math.Max(1000, it.cost);
                if (cap) it.name = "擷取卡";
                if (it.boughtDay == 0) it.boughtDay = Math.Max(1, s.day - 5);
            }
            Random r = new Random(s.seed);
            foreach (ItemDef it in w.items)
            {
                Trend t = s.TrendOf(it.cat);
                if (t.next == 100) t.next = 100 + r.Next(-8, 9);
            }
            s.act = 1;
            for (int a = 3; a >= 1; a--) if (s.cash >= ActCash[a]) { s.act = a + 1; break; }
            if (s.act >= 2) s.Set("vjoined");
            if (!s.pendingBeats.Contains("intro")) s.pendingBeats.Insert(0, "intro");
            s.startedDay = 0;
        }

        static TalentState FromDef(TalentDef t, int mood, int trust)
        {
            var state = new TalentState { id = t.id, fans = t.fans, mood = mood, trust = trust };
            ApplyTalentProfile(state, t);
            return state;
        }

        static void ApplyTalentProfile(TalentState state, TalentDef profile)
        {
            state.name = profile.name;
            state.portrait = profile.portrait;
            state.genre = profile.genre;
            state.style = profile.style;
            state.interests = profile.interests;
            state.likes = (string[])profile.likes.Clone();
            state.dislikes = (string[])profile.dislikes.Clone();
        }

        public static void RefreshTalentRoster(GameState s, WorldData w)
        {
            foreach (TalentDef profile in w.talents)
            {
                if (profile.id == "roger") continue;
                TalentState member = s.Talent(profile.id);
                if (member != null) ApplyTalentProfile(member, profile);
            }

            foreach (Opportunity op in s.inbox)
                if (op.type == "talent" && op.talent == "lily" && op.npc == "vboss") op.npc = "vivi";

            if (s.Has("xd_roster_v2")) return;
            foreach (Investment investment in s.investments)
                investment.name = RefreshRosterLabel(investment.name);
            foreach (Relation relation in s.relations)
                relation.name = RefreshRosterLabel(relation.name);
            foreach (Opportunity op in s.inbox)
            {
                RefreshOpportunityLabels(op);
                if (op.type == "talent")
                {
                    TalentDef profile = Array.Find(w.talents, t => t.id == op.talent);
                    if (profile == null) continue;
                    op.npcName = profile.name;
                    op.company = profile.genre;
                    op.summary = profile.genre + "・粉絲 " + CaseGen.FansText(profile.fans);
                }
                if (op.type == "sell" && op.npc == "kui")
                {
                    op.company = "遊戲實況主";
                    if (op.summary != null) op.summary = op.summary.Replace("新人實況主", op.company);
                }
            }
            foreach (Opportunity op in s.events) RefreshOpportunityLabels(op);
            foreach (Busy booking in s.busy) booking.text = RefreshRosterLabel(booking.text);
            for (int i = 0; i < s.morning.Count; i++) s.morning[i] = RefreshRosterLabel(s.morning[i]);
            for (int i = 0; i < s.report.Count; i++) s.report[i] = RefreshRosterLabel(s.report[i]);
            s.Set("xd_roster_v2");
        }

        static void RefreshOpportunityLabels(Opportunity op)
        {
            op.npcName = RefreshRosterLabel(op.npcName);
            op.company = RefreshRosterLabel(op.company);
            op.title = RefreshRosterLabel(op.title);
            op.summary = RefreshRosterLabel(op.summary);
            op.text = RefreshRosterLabel(op.text);
        }

        static string RefreshRosterLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return label;
            foreach (var replacement in LegacyRosterLabels)
                label = label.Replace(replacement.Key, replacement.Value);
            return label;
        }

        // ------------------------------------------------------------------
        // 每天開始
        // ------------------------------------------------------------------

        /// <summary>開始新的一天。回傳要先播的劇情（可能是空的）。同一天重複呼叫不會重置。</summary>
        public List<string> StartDay()
        {
            List<string> beats = new List<string>(s.pendingBeats);
            s.pendingBeats.Clear();
            if (s.startedDay != s.day)
            {
                s.startedDay = s.day;
                s.slots = SlotsMax;
                s.delegates = DelegatesMax;
                FillInbox(new Random(s.seed ^ (s.day * 104729)));
                ApplyOversleep();
            }
            return beats;
        }

        public List<Opportunity> VisibleInbox()
        {
            List<Opportunity> list = s.inbox.FindAll(o => o.showDay <= s.day);
            list.Sort((a, b) =>
            {
                if (a.urgent != b.urgent) return a.urgent ? -1 : 1;
                return string.CompareOrdinal(a.uid.PadLeft(8, '0'), b.uid.PadLeft(8, '0'));
            });
            return list;
        }

        public int NewPerDay { get { return 3 + (s.HasUpgrade("network1") ? 1 : 0) + (s.HasUpgrade("network2") ? 1 : 0) + (s.reputation >= 75 ? 1 : 0) - (s.reputation < 25 ? 1 : 0); } }
        /// <summary>聲譽影響工商報價：0 分打七五折，100 分多兩成五。</summary>
        public double RepBonus { get { return 1 + (s.reputation - 50) / 200.0; } }

        void FillInbox(Random r)
        {
            int want = NewPerDay;
            int guard = 0;
            while (want > 0 && guard++ < 30)
            {
                Opportunity op = MakeOpp(PickType(r), r);
                if (op == null) continue;
                s.inbox.Add(op);
                want--;
            }
            // 新人想加盟
            if (s.day >= 10 && s.talents.Count < 5 && r.NextDouble() < (s.Has("kui_invested") && s.Talent("kui") == null ? 0.3 : 0.12))
            {
                if (!s.inbox.Exists(o => o.type == "talent"))
                {
                    Opportunity t = MakeOpp("talent", r);
                    if (t != null) s.inbox.Add(t);
                }
            }
            // 信箱最多 6 件：先丟掉舊的普通委託，人脈介紹的留著
            List<Opportunity> vis = s.inbox.FindAll(o => o.showDay <= s.day);
            while (vis.Count > 6)
            {
                Opportunity old = vis.Find(o => !o.urgent && !o.held && !o.lead) ?? vis.Find(o => !o.urgent && !o.lead) ?? vis.Find(o => !o.urgent);
                if (old == null) break;
                s.inbox.Remove(old);
                vis.Remove(old);
            }
        }

        string PickType(Random r)
        {
            List<KeyValuePair<string, int>> w8 = new List<KeyValuePair<string, int>>();
            bool canSell = s.inventory.Exists(i => i.rentDays <= 0 && !s.inbox.Exists(o => o.type == "sell" && o.item == i.id));
            if (s.act <= 1)
            {
                w8.Add(new KeyValuePair<string, int>("sponsor", 46));
                w8.Add(new KeyValuePair<string, int>("buy", 20));
                w8.Add(new KeyValuePair<string, int>("broker", 15));
                w8.Add(new KeyValuePair<string, int>("invest", 8));
                if (canSell) w8.Add(new KeyValuePair<string, int>("sell", 25));
            }
            else
            {
                w8.Add(new KeyValuePair<string, int>("sponsor", 30));
                w8.Add(new KeyValuePair<string, int>("buy", 12));
                w8.Add(new KeyValuePair<string, int>("broker", 12));
                w8.Add(new KeyValuePair<string, int>("plan", 15));
                w8.Add(new KeyValuePair<string, int>("invest", 13));
                w8.Add(new KeyValuePair<string, int>("gray", 8));
                if (canSell) w8.Add(new KeyValuePair<string, int>("sell", 18));
            }
            int total = 0;
            foreach (var kv in w8) total += kv.Value;
            int x = r.Next(total);
            foreach (var kv in w8)
            {
                if (x < kv.Value) return kv.Key;
                x -= kv.Value;
            }
            return "sponsor";
        }

        Opportunity NewOp(string type, Random r)
        {
            return new Opportunity { uid = s.NewUid("o"), type = type, seed = r.Next(), expires = s.day + 1, showDay = s.day };
        }

        BrandDef PickBrand(Random r)
        {
            List<BrandDef> list = new List<BrandDef>();
            foreach (BrandDef b in w.brands) if (b.act <= s.act) list.Add(b);
            return list[r.Next(list.Count)];
        }

        void SetClient(Opportunity op, BrandDef b)
        {
            op.brand = b.name;
            op.company = b.company;
            op.category = b.cat;
            op.npc = string.IsNullOrEmpty(b.npc) ? "g_" + b.name : b.npc;
            op.npcName = NpcNameFor(w, op.npc, b.name);
            op.role = "廠商";
        }

        TalentState PickTalent(Random r)
        {
            List<TalentState> list = s.talents.FindAll(t => s.bookings.FindAll(b => !b.done && b.talent == t.id).Count < 5);
            if (list.Count == 0) return null;
            long total = 0;
            foreach (TalentState t in list) total += Math.Max(1000, t.fans);
            long x = (long)(r.NextDouble() * total);
            foreach (TalentState t in list)
            {
                x -= Math.Max(1000, t.fans);
                if (x < 0) return t;
            }
            return list[0];
        }

        public Opportunity MakeOpp(string type, Random r)
        {
            Opportunity op = NewOp(type, r);
            switch (type)
            {
                case "sponsor":
                {
                    TalentState t = PickTalent(r);
                    if (t == null) return null;
                    BrandDef b = PickBrand(r);
                    SetClient(op, b);
                    op.talent = t.id;
                    op.day = s.day + 1 + r.Next(5);
                    op.amount = CaseGen.Nice(TalentFee(t) * TierF * (0.85 + r.NextDouble() * 0.45) * NetBonus * RepBonus * (1 + 0.04 * s.RelValue(op.npc)));
                    CategoryDef cd = w.Category(b.cat);
                    op.title = b.name + "工商";
                    op.summary = t.name + "・" + (cd != null ? cd.product : "工商") + "・" + Calendar.Short(op.day);
                    // 旗下有兩位以上藝人時，有些廠商不指定人選，只寫需求，讓你自己挑
                    if (s.talents.Count >= 2 && r.NextDouble() < 0.4)
                    {
                        op.kind = "open";
                        op.talent = "";
                        op.text = t.genre;
                        op.level = (int)Math.Max(1000, Math.Round(t.fans * (0.5 + r.NextDouble() * 0.3) / 1000.0) * 1000);
                        op.summary = "不指定藝人・" + (cd != null ? cd.product : "工商") + "・" + Calendar.Short(op.day);
                    }
                    op.est = op.amount;
                    return op;
                }
                case "gray":
                {
                    TalentState t = s.Talent("roger") ?? PickTalent(r);
                    if (t == null || w.gray.Length == 0) return null;
                    op.level = r.Next(w.gray.Length);
                    GrayDef g = w.gray[op.level];
                    string co = ShadyCo[r.Next(ShadyCo.Length)];
                    op.brand = co;
                    op.company = co;
                    op.category = "轉播";
                    op.npc = "g_" + co;
                    op.npcName = CaseGen.NpcName(w, co + "x");
                    op.role = "中間人";
                    op.talent = t.id;
                    op.day = s.day + 2 + r.Next(4);
                    op.amount = CaseGen.Nice(TalentFee(t) * TierF * 2.5 * (0.9 + r.NextDouble() * 0.3));
                    op.title = g.title;
                    op.summary = t.name + "・報酬優渥・" + Calendar.Short(op.day);
                    op.est = op.amount;
                    return op;
                }
                case "buy":
                {
                    List<ItemDef> items = new List<ItemDef>();
                    foreach (ItemDef it in w.items)
                    {
                        if (it.act > s.act) continue;
                        int weight = it.act == s.act ? 3 : (it.act == s.act - 1 ? 2 : 1);
                        for (int k = 0; k < weight; k++) items.Add(it);
                    }
                    ItemDef item = items[r.Next(items.Count)];
                    int claimed = new[] { 0, 1, 1, 1, 2, 2 }[r.Next(6)];
                    int extra = new[] { 0, 0, 1, 1, 2 }[r.Next(5)];
                    int real = Math.Min(4, claimed + extra);
                    bool stolen = s.act >= 2 && r.NextDouble() < 0.12;
                    if (stolen) claimed = Math.Min(claimed, 1);
                    NpcDef seller = w.sellers[r.Next(w.sellers.Length)];
                    op.npc = seller.id;
                    op.npcName = seller.name;
                    op.company = seller.company;
                    op.role = "賣家";
                    op.item = item.id;
                    op.level = claimed;
                    op.amount2 = real;
                    op.kind = stolen ? "stolen" : "";
                    long ask = CaseGen.Nice(CaseGen.Market(this, item, claimed) * 1.05);
                    op.amount = ask;
                    op.title = "想賣" + item.name;
                    op.summary = CaseGen.GradeText[claimed] + "・開價 " + GameState.Short(ask);
                    op.est = ask;
                    return op;
                }
                case "sell":
                {
                    List<InvItem> list = s.inventory.FindAll(i => i.rentDays <= 0 && !s.inbox.Exists(o => o.type == "sell" && o.item == i.id));
                    if (list.Count == 0) return null;
                    InvItem inv = list[r.Next(list.Count)];
                    NpcDef buyer = w.buyers[r.Next(w.buyers.Length)];
                    op.npc = buyer.id;
                    op.npcName = buyer.name;
                    op.company = buyer.company;
                    op.role = "買家";
                    op.item = inv.id;
                    ItemDef def = w.Item(CaseGen.ItemIdOf(w, inv));
                    long real = def != null ? CaseGen.Market(this, def, inv.grade) : CaseGen.Nice(inv.baseValue * CaseGen.GradeFactor[Math.Min(4, inv.grade)]);
                    op.amount = CaseGen.Nice(real * (0.98 + r.NextDouble() * 0.2) * (s.HasUpgrade("network2") ? 1.1 : 1));
                    op.title = "想買" + inv.name;
                    op.summary = op.company + "・" + inv.condition;
                    op.est = op.amount;
                    return op;
                }
                case "broker":
                {
                    BrandDef b = PickBrand(r);
                    SetClient(op, b);
                    // 先決定最適合的人，預算照他的標準報價再多一點；其他候選人的報價也用同一套公式算
                    List<TalentDef> fit = new List<TalentDef>();
                    foreach (TalentDef t in w.talents)
                        if (s.Talent(t.id) == null && Array.IndexOf(t.dislikes, b.cat) < 0) fit.Add(t);
                    if (fit.Count == 0) return null;
                    TalentDef right = fit[r.Next(fit.Count)];
                    op.talent = right.id;
                    op.amount = CaseGen.CeilNice(TalentQuote(right) * (1.08 + r.NextDouble() * 0.35));
                    op.day = s.day + 3 + r.Next(5);
                    op.title = b.name + "找人";
                    op.summary = "仲介・" + b.cat + "工商";
                    op.est = CaseGen.Nice(op.amount * 0.22 * NetBonus);
                    return op;
                }
                case "plan":
                {
                    BrandDef b = PickBrand(r);
                    SetClient(op, b);
                    op.amount = CaseGen.Nice((40000 + r.NextDouble() * 40000) * TierF * NetBonus);
                    op.title = b.name + "企劃";
                    op.summary = "直播企劃・越省越賺";
                    op.est = CaseGen.Nice(op.amount * 0.6);
                    return op;
                }
                case "invest":
                {
                    List<InvestDef> list = new List<InvestDef>();
                    foreach (InvestDef d in w.invest)
                        if (!s.investments.Exists(v => v.name == d.name && v.status == "active") && !s.inbox.Exists(o => o.type == "invest" && o.title == d.name)) list.Add(d);
                    if (list.Count == 0) return null;
                    InvestDef inv = list[r.Next(list.Count)];
                    op.level = new[] { 0, 0, 1, 1, 1, 2, 2, 3 }[r.Next(8)];
                    op.amount = CaseGen.Nice((80000 + r.NextDouble() * 120000) * TierF);
                    op.npc = string.IsNullOrEmpty(inv.portrait) ? "g_" + inv.name : inv.portrait;
                    op.npcName = NpcNameFor(w, op.npc, inv.name);
                    op.company = inv.name;
                    op.role = "創業者";
                    op.title = inv.name;
                    op.summary = inv.kind + "・找投資";
                    op.est = op.amount;
                    op.kind = inv.kind;
                    return op;
                }
                case "talent":
                {
                    List<TalentDef> list = new List<TalentDef>();
                    foreach (TalentDef t in w.talents) if (t.id != "roger" && s.Talent(t.id) == null) list.Add(t);
                    if (list.Count == 0) return null;
                    TalentDef pick = list.Find(t => t.id == "kui" && s.Has("kui_invested")) ?? list[r.Next(list.Count)];
                    long[] k = { 2, 4, 8, 16 };
                    op.talent = pick.id;
                    op.npc = pick.portrait;
                    op.npcName = pick.name;
                    op.company = pick.genre;
                    op.role = "新人";
                    op.amount = CaseGen.Nice(pick.fans * k[ActIndex]);
                    op.title = pick.name + "想加盟";
                    op.summary = pick.genre + "・粉絲 " + CaseGen.FansText(pick.fans);
                    op.est = -op.amount;
                    op.expires = s.day + 2;
                    return op;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 打電話、委派
        // ------------------------------------------------------------------

        public bool CanCall(Opportunity op, out string why)
        {
            why = null;
            if (s.slots <= 0) { why = "今天的通話時段用完了"; return false; }
            return CanHandle(op, out why);
        }

        bool CanHandle(Opportunity op, out string why)
        {
            why = null;
            if (op.type == "buy" && s.inventory.Count >= InventoryCap) { why = "庫存滿了"; return false; }
            if (op.type == "sell")
            {
                InvItem it = s.inventory.Find(i => i.id == op.item);
                if (it == null) { why = "商品已經不在了"; return false; }
                if (it.rentDays > 0) { why = "商品出租中"; return false; }
            }
            if (op.type == "talent" && s.cash < op.amount) { why = "現金不夠付簽約金"; return false; }
            if ((op.type == "invest" || op.type == "buy") && s.cash < op.amount * 0.3) { why = "現金不夠"; return false; }
            return true;
        }

        public bool CanDelegate(Opportunity op, out string why)
        {
            why = null;
            if (!s.Has("vjoined")) { why = "還沒有人可以委派"; return false; }
            if (s.delegates <= 0) { why = "V老大今天沒空了"; return false; }
            if (op.type != "sponsor" && op.type != "buy" && op.type != "sell" && op.type != "broker" && op.type != "plan") { why = "這種案子要你親自處理"; return false; }
            return CanHandle(op, out why);
        }

        public CaseData BuildCase(Opportunity op)
        {
            CaseData d = CaseGen.Build(this, op);
            if (d != null && s.restedDay == s.day) d.patience += 1;
            return d;
        }

        public string Delegate(Opportunity op)
        {
            CaseData d = BuildCase(op);
            if (d == null) return "這件委託已經失效了。";
            CaseSession cs = new CaseSession(d, s);
            Random r = new Random(op.seed ^ 0x2545F491);
            if (op.type == "buy") cs.Reply("inspect");
            foreach (ContradictionData c in d.contradictions)
            {
                if (c.bad || c.key.StartsWith("risk") || c.key == "stolen") continue;
                if (r.NextDouble() < 0.6)
                {
                    cs.Pick(c.a);
                    cs.Pick(c.b);
                    cs.Challenge();
                }
            }
            switch (op.type)
            {
                case "sponsor":
                    if (d.assign != null && d.assign.open)
                    {
                        // V老大挑最合適、那天有空的人
                        RosterEntry pickE = null;
                        foreach (RosterEntry e in d.assign.roster)
                            if (!e.busy && (pickE == null || e.fit > pickE.fit)) pickE = e;
                        if (pickE == null) pickE = d.assign.roster[0];
                        cs.Assign(pickE.id);
                    }
                    cs.Finish("signed");
                    break;
                case "buy":
                case "sell":
                    cs.offerValue = op.type == "buy" ? cs.OfferLimit : (long)(cs.OfferLimit * 0.95);
                    cs.OfferSubmit();
                    break;
                case "broker":
                    if (r.NextDouble() < 0.3)
                        foreach (DocumentData doc in d.documents)
                            if (doc.recommend && doc.id != d.recommend.correct) { cs.Recommend(doc.id); break; }
                    cs.Recommend(d.recommend.correct);
                    break;
                case "plan":
                    int[] best = CaseGen.BestPlan(cs);
                    if (best != null)
                    {
                        for (int g = 0; g < best.Length; g++) cs.planPick[g] = best[g];
                        cs.PlanSubmit();
                    }
                    break;
            }
            if (cs.result == null) cs.Finish("hold");
            List<string> notes = ApplyOutcome(op, cs, true);
            string head = "V老大：" + d.title;
            switch (cs.result)
            {
                case "signed": head += "簽好了，" + GameState.Short(cs.Fee); break;
                case "deal": head += "辦好了"; break;
                default: head += "沒談成"; break;
            }
            if (notes.Count > 0) head += "。" + notes[0];
            return head;
        }

        // ------------------------------------------------------------------
        // 案件結果
        // ------------------------------------------------------------------

        /// <summary>一通電話結束後套用結果，回傳要告訴玩家的提醒。</summary>
        public List<string> ApplyOutcome(Opportunity op, CaseSession cs, bool delegated)
        {
            List<string> notes = new List<string>();
            string res = cs.result ?? "hold";
            if (delegated) s.delegates = Math.Max(0, s.delegates - 1);
            else s.slots = Math.Max(0, s.slots - 1);
            s.AddStat("calls", 1);
            bool remove = res != "hold";
            Random r = new Random(op.seed ^ 0x1E3779B9);

            switch (op.type)
            {
                case "sponsor":
                case "gray":
                    if (res == "signed") notes.AddRange(SignBooking(op, cs, r));
                    else if (res == "reject" && op.type == "gray" && cs.found.Exists(k => k.StartsWith("risk")))
                    {
                        s.AddRep(4);
                        s.Set("mem_gray_refuse");
                        s.AddStat("grayRefused", 1);
                        notes.Add("拒絕了可疑的案子，聲譽上升");
                    }
                    break;
                case "buy":
                    if (res == "deal")
                    {
                        s.AddStat("trades", 1);
                        InvItem it = s.inventory.Find(i => i.id == op.uid);
                        if (it != null && it.flags == "stolen")
                        {
                            s.events.Add(new Opportunity { uid = s.NewUid("e"), type = "police", item = it.id, showDay = s.day + 2 + r.Next(4), amount = it.cost });
                            if (cs.found.Contains("stolen")) notes.Add("明知道可能是贓物還收……希望不會出事");
                        }
                    }
                    else if (res == "reject" && cs.found.Contains("stolen"))
                    {
                        s.AddRep(2);
                        notes.Add("沒有收贓物，聲譽上升");
                    }
                    break;
                case "sell":
                    if (res == "deal")
                    {
                        s.AddStat("trades", 1);
                        s.AddStat("tradeProfit", s.lastProfit);
                        bool honest = cs.usedReplies.Contains("honest");
                        s.AddRel(op.npc, op.npcName, honest ? 2 : 1);
                        if (!honest && cs.data.replies.Length > 0 && r.NextDouble() < 0.45)
                        {
                            s.events.Add(new Opportunity { uid = s.NewUid("e"), type = "complaint", showDay = s.day + 1, amount = CaseGen.Nice(cs.offerFinal * 0.2), text = op.npcName });
                        }
                        notes.Add("這筆利潤 " + GameState.Money(s.lastProfit));
                    }
                    break;
                case "broker":
                    if (res == "deal") { s.AddStat("brokers", 1); s.AddStat("incBroker", cs.recommendReward); s.AddRel(op.npc, op.npcName, 1); }
                    break;
                case "plan":
                    if (res == "deal") { s.AddStat("plans", 1); s.AddStat("incPlan", cs.planReward); s.AddRel(op.npc, op.npcName, 1); }
                    break;
                case "invest":
                    if (res == "deal")
                    {
                        Investment v = s.investments.Find(x => x.refId == op.uid);
                        if (v != null)
                        {
                            v.quality = op.level;
                            v.periods = 6;
                            v.startDay = s.day;
                            v.status = "active";
                        }
                        s.AddStat("invests", 1);
                        if (op.title == TsukiStudio) s.Set("mem_kui");
                        notes.Add("每 3 天配息一次，共 6 次，最後拿回本金");
                    }
                    break;
                case "dispute":
                case "audit":
                    if (res == "deal")
                    {
                        s.AddStat("disputes", 1);
                        s.AddStat("costDispute", cs.offerFinal);
                        bool good = cs.offerFinal <= op.amount / 2;
                        if (good) { s.AddRep(3); s.Set("mem_dispute_good"); notes.Add("漂亮的和解，聲譽上升"); }
                        if (op.npc == "lin") s.Set("mem_lin");
                        s.AddRel(op.npc, op.npcName, 1);
                        if (cs.usedReplies.Contains("redo")) RedoShow(op, notes);
                        if (op.type == "audit") s.Set("audited");
                    }
                    else remove = false;
                    if (res == "leave") notes.Add("對方掛電話了。期限前還能再打一次");
                    break;
                case "talent":
                    if (res == "signed") notes.AddRange(SignTalent(op, cs));
                    break;
            }
            if (res == "leave") s.AddRel(op.npc, op.npcName, -1);
            if (res == "hold") op.held = true;
            if (remove) s.inbox.Remove(op);
            return notes;
        }

        List<string> SignBooking(Opportunity op, CaseSession cs, Random r)
        {
            List<string> notes = new List<string>();
            CaseGen.SponsorFacts f;
            if (!facts.TryGetValue(op.uid, out f)) f = new CaseGen.SponsorFacts { writtenDay = op.day, realDay = op.day, hours = 2, quote = op.amount };
            Func<string, bool> Fixed = k => cs.fixedKeys.Contains(k);
            int day = f.errors.Contains("date") && Fixed("date") ? f.realDay : f.writtenDay;
            List<string> flags = new List<string>();
            foreach (string e in f.errors)
            {
                if (Fixed(e)) continue;
                if (e == "name" || e == "corp" || e == "hours" || e == "date" || e == "excl") flags.Add(e);
            }
            long fee = cs.Fee;
            bool gray = op.type == "gray";
            // 換了人或對方沒指定：原本那位的名字、檔期、排他問題不算，改看實際去的人
            string talentId = !string.IsNullOrEmpty(cs.assigned) ? cs.assigned : op.talent;
            if (!string.IsNullOrEmpty(cs.assigned) || op.kind == "open")
            {
                flags.RemoveAll(x => x == "name" || x == "date" || x == "excl");
                if (CaseGen.BusyOn(s, talentId, day, null) != null) flags.Add("date");
                if (f.errors.Contains("excl") && !Fixed("excl"))
                {
                    foreach (Booking ob in s.bookings)
                        if (!ob.done && ob.talent == talentId && ob.category == op.category && ob.brand != op.brand && Math.Abs(ob.day - day) <= 20)
                        {
                            flags.Add("excl");
                            f.exclBrand = ob.brand;
                            break;
                        }
                }
            }
            if (gray)
            {
                flags.Add("gray");
                if (cs.usedReplies.Contains("take"))
                {
                    flags.Add("take");
                    // 沒另外議價才直接加三成；有議價的話，「加錢就不問」已經算進議價的籌碼了
                    if (!cs.offerDone) fee = CaseGen.Nice(fee * 1.3);
                }
            }
            TalentState t = s.Talent(talentId);
            Booking b = new Booking
            {
                uid = s.NewUid("b"), talent = talentId, day = day, brand = op.brand, company = op.company, category = op.category,
                fitLevel = op.kind == "open" && cs.assignFit >= 0 ? cs.assignFit + 1 : 0,
                npc = op.npc, npcName = op.npcName, fee = fee, penaltyX = f.errors.Contains("pen") && !Fixed("pen") ? f.penalty : 1,
                hours = f.hours, flags = string.Join(",", flags.ToArray()), exclBrand = f.exclBrand
            };
            s.bookings.Add(b);
            s.AddStat("deals", 1);
            s.AddRel(op.npc, op.npcName, 1);
            if (t != null && Array.IndexOf(t.dislikes, op.category) >= 0) notes.Add(t.name + "討厭" + op.category + "類，直播當天要小心");
            if (flags.Contains("date")) notes.Add(Calendar.Short(day) + " 撞期了，可能會開天窗");
            if (gray)
            {
                s.AddStat("gray", 1);
                s.AddStat("grayRevenue", fee);
                s.Set("mem_gray_take");
                s.AddRep(-3);
                double chance = flags.Contains("take") ? 0.8 : 0.65;
                if (r.NextDouble() < chance)
                {
                    GrayDef gd = w.gray.Length > 0 ? w.gray[Math.Max(0, op.level) % w.gray.Length] : null;
                    s.events.Add(new Opportunity
                    {
                        uid = s.NewUid("e"), type = "graycl", showDay = day + 1 + r.Next(4), amount = CaseGen.Nice(fee * 1.2),
                        company = gd != null ? gd.licensor : "版權公司", talent = t != null ? t.name : "羅傑", day = day, refUid = b.uid
                    });
                }
                if (s.GetStat("gray") >= 2 && !s.Has("audit_scheduled"))
                {
                    s.Set("audit_scheduled");
                    s.events.Add(new Opportunity { uid = s.NewUid("e"), type = "audit", showDay = s.day + 5 + r.Next(5) });
                }
            }
            return notes;
        }

        void RedoShow(Opportunity op, List<string> notes)
        {
            TalentState t = s.talents.Find(x => x.name == op.talent);
            if (t == null) return;
            int d = CaseGen.FreeDayFrom(s, t.id, s.day + 1);
            s.busy.Add(new Busy { talent = t.id, day = d, text = "免費補播" });
            t.mood = Math.Max(0, t.mood - 8);
            notes.Add(t.name + "要在 " + Calendar.Short(d) + " 免費補一場");
        }

        List<string> SignTalent(Opportunity op, CaseSession cs)
        {
            List<string> notes = new List<string>();
            TalentDef def = null;
            foreach (TalentDef t in w.talents) if (t.id == op.talent) def = t;
            if (def == null || s.Talent(def.id) != null) return notes;
            s.Spend(cs.Fee);
            TalentState ts = FromDef(def, 75, 55);
            bool shareBad = false;
            foreach (ContradictionData c in cs.data.contradictions) if (c.key == "share" && !cs.fixedKeys.Contains("share")) shareBad = true;
            if (shareBad) { ts.cut = 67; notes.Add("分潤寫錯沒改，以後他的工商只拿三分之二"); }
            s.talents.Add(ts);
            s.AddStat("talents", 1);
            if (def.id == "kui") s.Set("mem_kui");
            notes.Add(def.name + "加入水蛇洞了！之後會有他的工商委託");
            return notes;
        }

        // ------------------------------------------------------------------
        // 公司操作（藝人、庫存、投資、升級）
        // ------------------------------------------------------------------

        public string Rest(TalentState t)
        {
            int d = s.day + 1;
            if (CaseGen.BusyOn(s, t.id, d, null) != null) return t.name + "明天已經有事了";
            s.busy.Add(new Busy { talent = t.id, day = d, text = "放假" });
            t.mood = Math.Min(100, t.mood + 20);
            return t.name + "明天放假，心情變好了";
        }

        public long RaiseCost(TalentState t)
        {
            return CaseGen.Nice(TalentFee(t) * TierF * 0.5);
        }

        public string GiveRaise(TalentState t)
        {
            if (s.day - t.raiseDay < 7) return "最近才加過薪";
            long cost = RaiseCost(t);
            if (s.cash < cost) return "現金不夠";
            s.Spend(cost);
            t.mood = Math.Min(100, t.mood + 15);
            t.trust = Math.Min(100, t.trust + 12);
            t.raiseDay = s.day;
            return t.name + "：老闆最好了！（花了 " + GameState.Short(cost) + "）";
        }

        public string RentOut(InvItem it)
        {
            if (it.rentDays > 0) return "已經在出租了";
            if (it.grade >= 4) return "故障品租不出去";
            it.rentDays = 5;
            it.rentPerDay = CaseGen.Nice(ItemMarket(it) * 0.035);
            s.inbox.RemoveAll(o => o.type == "sell" && o.item == it.id);
            return it.name + "出租 5 天，每天 " + GameState.Money(it.rentPerDay);
        }

        public string Scrap(InvItem it)
        {
            if (it.rentDays > 0) return "出租中不能賣";
            long v = CaseGen.Nice(ItemMarket(it) * 0.6);
            s.Earn(v);
            s.lastProfit = v - it.cost;
            s.inventory.Remove(it);
            s.inbox.RemoveAll(o => o.type == "sell" && o.item == it.id);
            return "賣給回收商 " + GameState.Money(v) + "（利潤 " + GameState.Money(s.lastProfit) + "）";
        }

        public long ExitValue(Investment v)
        {
            double[] k = { 1.05, 0.9, 0.6, 0.2 };
            return CaseGen.Nice(v.amount * k[Math.Max(0, Math.Min(3, v.quality))]);
        }

        public string ExitInvestment(Investment v)
        {
            if (v.status != "active") return "這筆投資已經結束了";
            long val = ExitValue(v);
            s.Earn(val);
            v.received += val;
            v.status = "exited";
            return "賣掉" + v.name + "的股份，拿回 " + GameState.Money(val);
        }

        public bool UpgradeAvailable(UpgradeDef u, out string why)
        {
            why = null;
            if (s.HasUpgrade(u.id)) { why = "已擁有"; return false; }
            if (u.act > s.act) { why = "第" + "一二三四"[Math.Max(0, Math.Min(3, u.act - 1))] + "幕開放"; return false; }
            if (!string.IsNullOrEmpty(u.requires))
            {
                bool ok = u.requires == "vjoined" ? s.Has("vjoined") : s.HasUpgrade(u.requires);
                if (!ok) { why = "需要先" + (u.requires == "vjoined" ? "有 V老大" : "買" + (w.Upgrade(u.requires) != null ? w.Upgrade(u.requires).name : u.requires)); return false; }
            }
            if (s.cash < u.cost) { why = "現金不夠"; return false; }
            return true;
        }

        public string BuyUpgrade(UpgradeDef u)
        {
            string why;
            if (!UpgradeAvailable(u, out why)) return why;
            s.Spend(u.cost);
            s.upgrades.Add(u.id);
            if (u.id == "office") s.slots++;
            if (u.id == "vteam") s.delegates += 2;
            return "買下了「" + u.name + "」";
        }

        // ------------------------------------------------------------------
        // 結束營業
        // ------------------------------------------------------------------

        public DayReport EndDay()
        {
            DayReport rep = new DayReport { day = s.day };
            List<string> L = rep.lines;
            Random r = new Random(s.seed ^ (s.day * 7919 + 13));

            // 1. 今天的工商直播
            foreach (Booking b in s.bookings.ToArray())
                if (!b.done && b.day <= s.day) ProcessBooking(b, r, L);
            s.bookings.RemoveAll(b => b.done && b.day < s.day - 30);

            // 2. 投資
            foreach (Investment v in s.investments)
            {
                if (v.status != "active") continue;
                int el = s.day - v.startDay;
                if (el <= 0 || el % 3 != 0) continue;
                int q = Math.Max(0, Math.Min(3, v.quality));
                double fail = q == 3 ? 0.25 : q == 2 ? 0.04 : 0;
                if (r.NextDouble() < fail)
                {
                    v.status = "failed";
                    L.Add(v.name + "經營失敗，投資血本無歸");
                    continue;
                }
                long pay = CaseGen.Nice(v.amount * CaseGen.InvestRate[q]);
                if (pay > 0) { s.Earn(pay); v.received += pay; s.AddStat("incInvest", pay); }
                v.paid++;
                if (v.paid >= v.periods)
                {
                    long back = CaseGen.Nice(v.amount * CaseGen.InvestReturn[q]);
                    s.Earn(back);
                    s.AddStat("incInvest", back);
                    v.received += back;
                    v.status = "done";
                    L.Add(v.name + "投資到期，拿回 " + GameState.Short(back) + "（總共 " + GameState.Short(v.received) + "）");
                }
                else if (pay > 0) L.Add(v.name + "配息 " + GameState.Short(pay));
                else L.Add(v.name + "這期沒有配息");
            }

            // 3. 出租
            foreach (InvItem it in s.inventory)
            {
                if (it.rentDays <= 0) continue;
                s.Earn(it.rentPerDay);
                it.rentDays--;
                if (it.rentDays == 0)
                {
                    if (r.NextDouble() < 0.12 && it.grade < 4)
                    {
                        it.grade++;
                        int known = Math.Max(0, Array.IndexOf(CaseGen.GradeText, it.condition));
                        it.condition = CaseGen.GradeText[Math.Min(4, Math.Max(known + 1, it.grade))];
                        L.Add(it.name + "租完回來，被用壞了一點");
                    }
                    else L.Add(it.name + "出租結束，收回庫存");
                }
            }

            // 4. 攝影棚
            if (s.HasUpgrade("studio"))
            {
                long st = 96000;
                s.Earn(st);
                L.Add("攝影棚出租收入 " + GameState.Short(st));
            }

            // 5. 固定開銷
            s.Spend(Overhead);
            s.AddStat("costOverhead", Overhead);

            // 6. 之前埋下的事件
            foreach (Opportunity e in s.events.ToArray())
            {
                if (e.showDay > s.day) continue;
                s.events.Remove(e);
                HandleEvent(e, r, L);
            }

            // 7. 過期的委託
            foreach (Opportunity op in s.inbox.ToArray())
            {
                if (op.expires > s.day) continue;
                if (op.type == "dispute" || op.type == "audit")
                {
                    long pay = op.type == "dispute" && op.amount2 > 0 ? Math.Min(op.amount, op.amount2) : op.amount;
                    s.Spend(pay);
                    s.AddStat("costDispute", pay);
                    s.AddRep(s.HasUpgrade("pr") ? -3 : -6);
                    s.AddRel(op.npc, op.npcName, -2);
                    s.AddStat("ignored", 1);
                    L.Add("沒處理" + op.company + "的求償，被判賠 " + GameState.Short(pay));
                }
                s.inbox.Remove(op);
            }

            // 8. 應收款入帳
            if (s.receivable > 0) L.Add("佣金、企劃費入帳 " + GameState.Short(s.receivable));
            s.cash += s.receivable;
            s.receivable = 0;

            // 8b. 現金不夠就向銀行借，有錢時自動還
            if (s.debt > 0)
            {
                long interest = CaseGen.Nice(Math.Max(100, s.debt * 0.005));
                s.cash -= interest;
                if (s.cash > s.debt * 3)
                {
                    s.cash -= s.debt;
                    L.Add("還清銀行借款 " + GameState.Short(s.debt));
                    s.debt = 0;
                }
                else L.Add("銀行借款利息 " + GameState.Short(interest) + "（欠款 " + GameState.Short(s.debt) + "）");
            }
            if (s.cash < 0)
            {
                long loan = -s.cash + 50000 * Tier;
                s.debt += loan;
                s.cash += loan;
                s.AddRep(-2);
                L.Add("現金不夠付帳，向銀行借了 " + GameState.Short(loan) + "！");
            }

            // 9. 藝人心情
            foreach (TalentState t in s.talents)
            {
                t.mood += (65 - t.mood) / 8;
                t.mood = Math.Max(0, Math.Min(100, t.mood));
            }
            if (r.NextDouble() < 0.28) RogerEvent(r, L);
            s.busy.RemoveAll(b => b.day < s.day - 3);

            // 10. 行情
            MarketMove(r, L);

            rep.revenue = s.revenueToday;
            rep.cost = s.costToday;
            rep.profit = s.revenueToday - s.costToday;
            L.Insert(0, "收入 " + GameState.Money(rep.revenue) + "　支出 " + GameState.Money(rep.cost));
            L.Insert(1, "房租水電與薪水 " + GameState.Money(Overhead));

            // 11. 主線進度
            int newAct = 1;
            for (int a = 3; a >= 1; a--) if (s.cash - s.debt >= ActCash[a]) { newAct = a + 1; break; }
            if (newAct > s.act)
            {
                s.act = newAct;
                s.pendingBeats.Add("act" + newAct);
                if (newAct >= 2 && !s.Has("vjoined")) s.Set("vjoined");
            }
            long[] marks = { 3000000, 10000000, 22000000, 28000000 };
            string[] beats = { "beat3m", "beat10m", "beat22m", "beat28m" };
            for (int i = 0; i < marks.Length; i++)
            {
                if (s.cash - s.debt >= marks[i] && !s.Has("b_" + beats[i]) && s.Has("vjoined"))
                {
                    s.Set("b_" + beats[i]);
                    s.pendingBeats.Add(beats[i]);
                }
            }
            rep.ending = !s.ended && s.cash - s.debt >= Goal;

            s.history.Add(rep.profit);
            if (s.history.Count > 30) s.history.RemoveAt(0);
            s.revenueToday = 0;
            s.costToday = 0;
            s.day++;
            rep.cash = s.cash;
            s.report = new List<string>(L);
            return rep;
        }

        double IncidentChance(TalentState t, Booking b)
        {
            double p = 0.05;
            if (t.id == "roger") p += 0.03;
            if (Array.IndexOf(t.dislikes, b.category) >= 0) p += 0.3;
            if (Array.IndexOf(t.likes, b.category) >= 0) p -= 0.03;
            if (t.mood < 40) p += 0.15;
            else if (t.mood < 60) p += 0.05;
            int near = s.bookings.FindAll(x => x.talent == t.id && Math.Abs(x.day - b.day) <= 2).Count;
            if (near >= 3) p += 0.1;
            p -= t.trust / 1000.0;
            if (s.HasUpgrade("pr")) p -= 0.05;
            if (s.luckDay == s.day) p *= 0.5;
            return Math.Max(0.01, Math.Min(0.75, p));
        }

        void ProcessBooking(Booking b, Random r, List<string> L)
        {
            b.done = true;
            TalentState t = s.Talent(b.talent);
            string tn = t != null ? t.name : "藝人";
            double cut = t != null ? t.cut / 100.0 : 1.0;
            Func<string, bool> has = f => ("," + b.flags + ",").Contains("," + f + ",");

            // 對方沒指定藝人、你派的人不太合適：廠商可能臨時解約
            if (b.fitLevel == 1 || b.fitLevel == 2)
            {
                double breach = b.fitLevel == 1 ? 0.5 : 0.2;
                if (r.NextDouble() < breach)
                {
                    s.AddRep(-2);
                    s.AddRel(b.npc, b.npcName, -1);
                    if (t != null) t.mood = Math.Max(0, t.mood - 5);
                    s.AddStat("breached", 1);
                    L.Add(b.brand + "覺得" + tn + "不適合，臨時解約了，這場沒有入帳");
                    return;
                }
            }
            if (has("date"))
            {
                MakeDispute("noshow", b, tn, CaseGen.Nice(b.fee * b.penaltyX * 1.3), b.company, b.npc, b.npcName, null);
                if (t != null) { t.trust = Math.Max(0, t.trust - 5); t.incidents++; }
                s.AddRep(s.HasUpgrade("pr") ? -2 : -4);
                L.Add(Calendar.Short(b.day) + " 撞期，" + b.brand + "的直播開天窗！明天要處理求償");
                return;
            }
            if (t != null && r.NextDouble() < IncidentChance(t, b))
            {
                bool hate = Array.IndexOf(t.dislikes, b.category) >= 0;
                string[] kinds = hate ? new[] { "bad", "bad", "refuse", "refuse", "early" } : new[] { "bad", "refuse", "early", "late" };
                string kind = kinds[r.Next(kinds.Length)];
                long paid = CaseGen.Nice(b.fee * 0.5 * cut);
                s.Earn(paid);
                MakeDispute(kind, b, tn, CaseGen.Nice(b.fee * b.penaltyX * 1.3), b.company, b.npc, b.npcName, null);
                t.mood = Math.Max(0, t.mood - 10);
                t.trust = Math.Max(0, t.trust - 5);
                t.incidents++;
                t.fans = kind == "bad" ? (int)(t.fans * 1.03) : (int)(t.fans * 0.98);
                s.AddRep(s.HasUpgrade("pr") ? -1 : -3);
                s.AddStat("incidents", 1);
                L.Add(tn + "在" + b.brand + "的直播" + CaseGen.IncidentText(kind) + "！只收到一半 " + GameState.Short(paid) + "，明天要處理求償");
                return;
            }
            double k = cut * (has("name") ? 0.7 : 1) * (has("corp") ? 0.85 : 1);
            long got = CaseGen.Nice(b.fee * k);
            s.Earn(got);
            s.AddStat("incSponsor", got);
            string extra = has("name") || has("corp") ? "（合約瑕疵被扣款）" : "";
            L.Add(b.brand + "工商完成，入帳 " + GameState.Short(got) + extra);
            if (b.fitLevel == 4)
            {
                // 派了最合適的人，廠商很滿意，加碼兩成
                long bonus = CaseGen.Nice(got * 0.2);
                s.Earn(bonus);
                s.AddStat("incSponsor", bonus);
                s.AddRel(b.npc, b.npcName, 1);
                s.AddStat("fitBonus", 1);
                L.Add(b.brand + "很滿意" + tn + "的表現，加碼 " + GameState.Short(bonus));
            }
            if (t != null)
            {
                t.jobs++;
                bool like = Array.IndexOf(t.likes, b.category) >= 0;
                bool hate = Array.IndexOf(t.dislikes, b.category) >= 0;
                t.fans = (int)(t.fans * (like ? 1.03 : 1.015));
                t.mood = Math.Max(0, Math.Min(100, t.mood - 3 + (like ? 8 : 0) - (hate ? 9 : 0) - (has("hours") ? 10 : 0)));
            }
            if (has("excl"))
            {
                BrandDef other = w.Brand(b.exclBrand);
                string co = other != null ? other.company : b.exclBrand;
                string npc = other != null && !string.IsNullOrEmpty(other.npc) ? other.npc : "g_" + b.exclBrand;
                MakeDispute("excl", b, tn, CaseGen.Nice(b.fee * 1.3), co, npc, NpcNameFor(w, npc, b.exclBrand), b.brand);
                L.Add(b.exclBrand + "發現" + tn + "接了競品，說違反排他條款");
            }
        }

        void MakeDispute(string kind, Booking b, string talentName, long claim, string company, string npc, string npcName, string competitor)
        {
            long cap = kind == "excl" ? b.fee : b.fee * b.penaltyX;
            s.inbox.Add(new Opportunity
            {
                uid = s.NewUid("o"), type = "dispute", seed = (int)(s.seed ^ (s.uidSeq * 31)), urgent = true,
                showDay = s.day + 1, expires = s.day + 3, kind = kind, talent = talentName, day = b.day, level = b.hours,
                amount = claim, amount2 = cap, text = "費用 " + (kind == "excl" ? 1 : b.penaltyX) + " 倍",
                brand = b.brand, company = company, category = b.category, npc = npc, npcName = npcName, role = "求償方",
                item = competitor, refUid = b.uid, title = "糾紛：" + company, summary = CaseGen.IncidentText(kind), est = -claim
            });
        }

        void HandleEvent(Opportunity e, Random r, List<string> L)
        {
            switch (e.type)
            {
                case "complaint":
                    s.Spend(e.amount);
                    s.AddRep(-2);
                    L.Add("買家" + e.text + "發現瑕疵，要求退一部分錢 " + GameState.Short(e.amount));
                    break;
                case "police":
                {
                    InvItem it = s.inventory.Find(i => i.id == e.item);
                    long fine = CaseGen.Nice(Math.Max(1000, e.amount * 0.3));
                    if (it != null) s.inventory.Remove(it);
                    s.Spend(fine);
                    s.AddRep(-5);
                    L.Add("警察上門：之前收的商品是贓物，" + (it != null ? "被沒收，" : "") + "還罰了 " + GameState.Short(fine));
                    break;
                }
                case "graycl":
                    s.inbox.Add(new Opportunity
                    {
                        uid = s.NewUid("o"), type = "dispute", seed = (int)(s.seed ^ (s.uidSeq * 37)), urgent = true,
                        showDay = s.day + 1, expires = s.day + 3, kind = "gray", talent = e.talent, day = e.day, level = 2,
                        amount = e.amount, amount2 = 0, text = "", company = e.company, npc = "g_" + e.company,
                        npcName = CaseGen.NpcName(w, e.company + "law"), role = "版權方", refUid = e.refUid,
                        title = "糾紛：" + e.company, summary = "侵權轉播求償", est = -e.amount
                    });
                    s.AddRep(s.HasUpgrade("pr") ? -3 : -6);
                    L.Add(e.company + "發現侵權，寄來求償書！");
                    break;
                case "audit":
                {
                    long gray = s.GetStat("grayRevenue");
                    long total = CaseGen.Nice(Math.Max(100000 * Tier, gray * 0.5 + s.revenueTotal * 0.01));
                    s.inbox.Add(new Opportunity
                    {
                        uid = s.NewUid("o"), type = "audit", seed = (int)(s.seed ^ (s.uidSeq * 41)), urgent = true,
                        showDay = s.day + 1, expires = s.day + 3, amount = total, amount2 = gray,
                        company = "國稅局", npc = "tax", npcName = "稅務員", role = "查核",
                        title = "稅務查核", summary = "帳務有疑點", est = -total
                    });
                    L.Add("國稅局寄來查核通知，公司的帳要被查了");
                    break;
                }
            }
        }

        void RogerEvent(Random r, List<string> L)
        {
            TalentState t = s.Talent("roger");
            if (t == null) return;
            int k = r.Next(6);
            switch (k)
            {
                case 0:
                    t.fans = (int)(t.fans * 1.08);
                    L.Add("羅傑的精華影片爆紅，粉絲漲到 " + CaseGen.FansText(t.fans));
                    break;
                case 1:
                    t.mood = Math.Max(0, t.mood - 15);
                    L.Add("羅傑：老闆……我最近好累，可以加薪嗎？（心情下降）");
                    break;
                case 2:
                    if (s.inventory.Count > 0)
                    {
                        InvItem it = s.inventory[r.Next(s.inventory.Count)];
                        if (it.grade < 4)
                        {
                            it.grade++;
                            int known = Math.Max(0, Array.IndexOf(CaseGen.GradeText, it.condition));
                            it.condition = CaseGen.GradeText[Math.Min(4, Math.Max(known + 1, it.grade))];
                        }
                        L.Add("羅傑在倉庫打電動，把" + it.name + "撞壞了一點");
                    }
                    else
                    {
                        long fix = CaseGen.Nice(3000 * Tier);
                        s.Spend(fix);
                        L.Add("羅傑把公司的椅子坐壞了，修理費 " + GameState.Short(fix));
                    }
                    break;
                case 3:
                    t.mood = Math.Max(0, t.mood - 10);
                    L.Add("羅傑熬夜打排位，今天精神很差");
                    break;
                case 4:
                    t.mood = Math.Min(100, t.mood + 10);
                    L.Add("粉絲寄了一大箱零食給羅傑，他心情很好");
                    break;
                default:
                    if (s.Has("vjoined"))
                    {
                        t.mood = Math.Min(100, t.mood + 8);
                        L.Add("V老大幫羅傑排好了下週的行程，羅傑偷偷在笑");
                    }
                    else
                    {
                        s.AddRep(-1);
                        L.Add("羅傑在直播上嘴了別的實況主，聊天室吵起來了");
                    }
                    break;
            }
        }

        void MarketMove(Random r, List<string> L)
        {
            string bestCat = null;
            int bestDelta = 0;
            foreach (ItemDef it in w.items) s.TrendOf(it.cat);
            foreach (Trend t in s.market)
            {
                int before = t.pct;
                t.pct = t.next;
                int delta = t.pct - before;
                if (Math.Abs(delta) > Math.Abs(bestDelta)) { bestDelta = delta; bestCat = t.cat; }
                int n = (int)Math.Round(t.pct + (100 - t.pct) * 0.25 + r.Next(-12, 13));
                t.next = Math.Max(65, Math.Min(145, n));
            }
            if (bestCat != null && Math.Abs(bestDelta) >= 8)
            {
                NewsDef nd = null;
                foreach (NewsDef x in w.news) if (x.cat == bestCat) nd = x;
                string text = nd != null ? (bestDelta > 0 ? nd.up : nd.down) : bestCat + (bestDelta > 0 ? "行情上漲" : "行情下跌");
                string line = "新聞：" + text + "（" + bestCat + (bestDelta > 0 ? " +" : " ") + bestDelta + "%）";
                L.Add(line);
                s.news.Insert(0, line);
                if (s.news.Count > 5) s.news.RemoveAt(5);
            }
        }

        /// <summary>結局：羅傑捲走三千萬。</summary>
        public void ApplyEnding()
        {
            s.ended = true;
            s.cash -= Goal;
            s.AddStat("endDay", s.day);
        }
    }
}
