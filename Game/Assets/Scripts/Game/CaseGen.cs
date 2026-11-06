using System;
using System.Collections.Generic;

namespace SSD
{
    /// <summary>
    /// 正式營業的案件產生器（企劃書第十四章）：用手工設計的模板，
    /// 依照公司現況替換品牌、金額、人物、商品、日期與錯誤內容。
    /// 每一種錯誤都保證有兩份文件可以互相對照，不會產生無解的矛盾。
    /// </summary>
    public static class CaseGen
    {
        // 紙張顏色
        const string PContract = "#ecdcc8", HContract = "#d9bfa8";
        const string PQuote = "#d6e0e6", HQuote = "#b8c8d4";
        const string PTalent = "#e6d3a6", HTalent = "#cdb27a";
        const string PBook = "#e9dcbc", HBook = "#cdbd96";
        const string PCard = "#f1e6cc", HCard = "#d8cba8";
        const string POffer = "#f1e6cc", HOffer = "#e8c35a";
        const string PItem = "#d9e6d8", HItem = "#b6cdb3";
        const string PClaim = "#efd6cf", HClaim = "#d9aaa0";
        const string PRec = "#e3dcef", HRec = "#c6bbdc";

        public static readonly string[] GradeText = { "全新", "近全新", "有刮傷", "修過的", "故障品" };
        public static readonly float[] GradeFactor = { 1f, 0.85f, 0.65f, 0.5f, 0.2f };
        static readonly string[] Appearance = { "完好如新", "輕微使用痕", "外殼刮傷", "外殼刮傷", "外殼破損" };
        static readonly string[] Conclusion = { "全新品", "可用，近新", "可用，有瑕疵", "可用，修過", "故障品" };

        // ------------------------------------------------------------------
        // 小工具
        // ------------------------------------------------------------------

        static FieldData F(string id, string label, string value)
        {
            return new FieldData { id = id, label = label, value = value };
        }

        static DocumentData Doc(string id, string title, string tray, string icon, string paper, string head, int lw, params FieldData[] fields)
        {
            DocumentData d = new DocumentData
            {
                id = id, title = title, tray = tray, icon = icon, paper = paper, head = head, labelWidth = lw,
                fields = fields, info = new FieldData[0]
            };
            CaseData.NormalizeDoc(d);
            return d;
        }

        static ContradictionData C(string key, string a, string b, string name, string fixField, string fixValue, long shift, string reply, string risk)
        {
            return new ContradictionData
            {
                key = key, a = a, b = b, name = name, fixField = fixField, fixValue = fixValue,
                shift = (int)shift, reply = reply, risk = risk
            };
        }

        public static long Nice(double v)
        {
            if (v <= 0) return 0;
            double unit = v < 10000 ? 100 : v < 100000 ? 1000 : v < 1000000 ? 10000 : v < 10000000 ? 100000 : 1000000;
            return (long)(Math.Round(v / unit) * unit);
        }

        public static long CeilNice(double v)
        {
            if (v <= 0) return 0;
            double unit = v < 10000 ? 100 : v < 100000 ? 1000 : v < 1000000 ? 10000 : v < 10000000 ? 100000 : 1000000;
            return (long)(Math.Ceiling(v / unit) * unit);
        }

        public static int Step(long v)
        {
            int[] steps = { 100, 500, 1000, 5000, 10000, 50000, 100000 };
            double target = v / 25.0;
            int best = steps[0];
            foreach (int s in steps) if (s <= target) best = s;
            return best;
        }

        static double Rand(Random r, double a, double b)
        {
            return a + r.NextDouble() * (b - a);
        }

        static T Pick<T>(Random r, IList<T> list)
        {
            return list[r.Next(list.Count)];
        }

        static void Shuffle<T>(Random r, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s ?? "") h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        static readonly Dictionary<char, char> Typo = new Dictionary<char, char>
        {
            { '傑', '潔' }, { '葵', '癸' }, { '凱', '愷' }, { '亞', '雅' }, { '熊', '雄' }, { '貓', '苗' }, { '哲', '喆' },
            { '莉', '俐' }, { '雨', '宇' }, { '頭', '投' }, { '丁', '釘' }, { '糖', '唐' }, { '黃', '皇' }, { '子', '仔' },
            { '星', '心' }, { '雷', '蕾' }, { '喵', '妙' }, { '周', '舟' }, { '極', '吉' }, { '龍', '隆' }, { '鯨', '京' },
            { '花', '華' }, { '飛', '菲' }, { '潮', '巢' }, { '卡', '咖' }, { '阿', '啊' }, { '蜜', '密' }, { '雪', '靴' },
            { '光', '廣' }, { '方', '芳' }, { '晨', '辰' }, { '跑', '泡' }, { '汪', '王' }, { '老', '佬' }, { '金', '今' },
            { '街', '階' }, { '海', '嗨' }, { '鋼', '剛' }, { '黑', '嘿' }, { '宏', '弘' }, { '天', '添' }
        };

        public static string Misspell(string s, int which)
        {
            if (string.IsNullOrEmpty(s)) return s;
            char[] cs = s.ToCharArray();
            int idx = which == 0 ? cs.Length - 1 : 0;
            for (int k = 0; k < cs.Length; k++)
            {
                int i = which == 0 ? cs.Length - 1 - k : k;
                if (Typo.ContainsKey(cs[i])) { idx = i; break; }
            }
            char repl;
            cs[idx] = Typo.TryGetValue(cs[idx], out repl) ? repl : (cs[idx] == '仔' ? '子' : '仔');
            return new string(cs);
        }

        public static string FansText(int fans)
        {
            if (fans >= 10000) return (fans / 10000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "萬";
            return fans.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string NpcName(WorldData w, string seedKey)
        {
            int h = Hash(seedKey);
            return w.surnames[h % w.surnames.Length] + w.titles[(h / 7) % w.titles.Length];
        }

        static DocumentData Handbook(string title, params string[] rules)
        {
            FieldData[] f = new FieldData[rules.Length];
            for (int i = 0; i < rules.Length; i++) f[i] = F("h_" + i, "第" + (i + 1) + "條", rules[i]);
            return Doc("hb", "手冊・" + title, "手冊", "book", PBook, HBook, 44, f);
        }

        static DocumentData OfferDoc(string title)
        {
            DocumentData d = Doc("offer", title, title, "receipt", POffer, HOffer, 56);
            d.kind = "offer";
            return d;
        }

        /// <summary>藝人討厭的工商類別，全部列出來（例如「飲料、美妝」）。</summary>
        static string Dislikes(string[] d)
        {
            return d == null || d.Length == 0 ? "沒有" : string.Join("、", d);
        }

        static string Likes(string[] l)
        {
            return l == null || l.Length == 0 ? "喜歡：沒特別" : "喜歡：" + string.Join("、", l);
        }

        /// <summary>文字寬度（跟畫面上的 Px.TextWidth 一樣的字寬表）。</summary>
        public static int TextW(string t)
        {
            if (string.IsNullOrEmpty(t)) return 0;
            int w = 0;
            foreach (char ch in t)
            {
                if (ch >= 128) { w += ch >= '\uFF61' && ch <= '\uFF9F' ? 6 : 13; continue; }
                if (" !',.:;I`ijl".IndexOf(ch) >= 0) w += 4;
                else if ("rt".IndexOf(ch) >= 0) w += 5;
                else if ("\"()*1[]^abcdefghknopqsuvxyz|".IndexOf(ch) >= 0) w += 6;
                else if ("#$&-023456789=?BCEFJKLPRSXZ_~".IndexOf(ch) >= 0) w += 7;
                else if ("%+/<>@ADGHNOQTUVY\\m{}".IndexOf(ch) >= 0) w += 8;
                else if ("MWw".IndexOf(ch) >= 0) w += 10;
                else w += 8;
            }
            return w;
        }

        static DocumentData TalentDoc(string id, string title, string tray, string portrait, FieldData[] info, FieldData[] fields, bool recommend, string sub)
        {
            // 名字比較長（例如「Vivi 芝衣」）時，標題和倉庫標籤縮短，免得超出框框
            if (TextW(title) > 112 && title.StartsWith("藝人檔案・")) title = "藝人・" + title.Substring(5);
            if (TextW(tray) > 46 && tray.Contains(" ")) tray = tray.Substring(tray.LastIndexOf(' ') + 1);
            // 沒有日期當標籤的檔案，標籤欄可以窄一點，讓內容多放幾個字
            int lw = 32;
            bool dateLabels = false;
            foreach (FieldData fd in fields) if (fd.label.Contains("/")) dateLabels = true;
            if (!dateLabels) lw = 26;
            DocumentData d = Doc(id, title, tray, "folder", PTalent, HTalent, lw, fields);
            d.kind = "talent";
            d.portrait = portrait;
            d.info = info;
            d.recommend = recommend;
            if (!string.IsNullOrEmpty(sub)) d.subTitle = sub;
            return d;
        }

        static ContactData Caller(Opportunity op)
        {
            return new ContactData { id = op.npc, name = op.npcName, caller = true };
        }

        static ContactData Vboss(GameState s)
        {
            return s.Has("vjoined") ? new ContactData { id = "vboss", name = "V老大", status = "企劃營運部" } : null;
        }

        static ContactData[] Contacts(params ContactData[] list)
        {
            List<ContactData> l = new List<ContactData>();
            foreach (ContactData c in list) if (c != null) l.Add(c);
            return l.ToArray();
        }

        static CaseData Base(Opportunity op, string title, string greeting, int patience)
        {
            return new CaseData
            {
                id = op.uid,
                title = title,
                client = new ClientData { id = op.npc, name = op.npcName, company = op.company, role = op.role },
                greeting = greeting,
                patience = patience,
                hintWho = "提示",
                startSlots = new string[0],
                storage = new string[0],
                replies = new ReplyData[0],
                contradictions = new ContradictionData[0],
                tasks = new TaskData[0]
            };
        }

        static TaskData Task(string text, string when)
        {
            return new TaskData { text = text, doneWhen = when };
        }

        public static long Market(Career c, ItemDef item, int grade)
        {
            int pct = c.s.TrendOf(item.cat).pct;
            return Nice(item.price * GradeFactor[Math.Max(0, Math.Min(4, grade))] * pct / 100.0);
        }

        static DocumentData MarketDoc(Career c, ItemDef item)
        {
            List<FieldData> f = new List<FieldData>();
            for (int g = 0; g < 5; g++) f.Add(F("m_" + g, GradeText[g], GameState.Money(Market(c, item, g))));
            if (c.s.HasUpgrade("intel"))
            {
                Trend t = c.s.TrendOf(item.cat);
                int diff = t.next - t.pct;
                f.Add(F("m_t", "明日走勢", diff > 1 ? "上漲 " + diff + "%" : diff < -1 ? "下跌 " + (-diff) + "%" : "持平"));
            }
            return Doc("market", "市場行情・" + item.cat, "行情", "chart", PCard, HCard, 56, f.ToArray());
        }

        // ------------------------------------------------------------------
        // 入口
        // ------------------------------------------------------------------

        public static CaseData Build(Career c, Opportunity op)
        {
            Random r = new Random(op.seed);
            CaseData d;
            switch (op.type)
            {
                case "sponsor": d = op.kind == "open" ? SponsorOpen(c, op, r) : Sponsor(c, op, r, false); break;
                case "gray": d = Sponsor(c, op, r, true); break;
                case "buy": d = Buy(c, op, r); break;
                case "sell": d = Sell(c, op, r); break;
                case "broker": d = Broker(c, op, r); break;
                case "plan": d = Plan(c, op, r); break;
                case "invest": d = Invest(c, op, r); break;
                case "dispute": d = Dispute(c, op, r); break;
                case "audit": d = Audit(c, op, r); break;
                case "talent": d = SignTalent(c, op, r); break;
                default: return null;
            }
            if (d == null) return null;
            d.Normalize();
            return d;
        }

        // ------------------------------------------------------------------
        // 工商（含灰色案件）
        // ------------------------------------------------------------------

        public class SponsorFacts
        {
            public int writtenDay;      // 合約上寫的日期
            public int realDay;         // 抓到撞期後會改成的日期
            public int penalty = 1;
            public int hours;
            public long quote;
            public string exclBrand;
            public List<string> errors = new List<string>();
        }

        static readonly string[] BusyTexts = { "家族旅行", "看牙醫", "休假", "參加婚禮", "健康檢查", "回老家" };

        /// <summary>這一天藝人有沒有事（已簽的工商或私人行程）。</summary>
        public static string BusyOn(GameState s, string talent, int day, string ignoreUid)
        {
            foreach (Booking b in s.bookings)
                if (!b.done && b.talent == talent && b.day == day && b.uid != ignoreUid) return b.brand + "（" + b.category + "）";
            foreach (Busy b in s.busy)
                if (b.talent == talent && b.day == day) return b.text;
            return null;
        }

        public static int FreeDayFrom(GameState s, string talent, int day)
        {
            int d = Math.Max(day, s.day + 1);
            while (BusyOn(s, talent, d, null) != null) d++;
            return d;
        }

        static CaseData Sponsor(Career c, Opportunity op, Random r, bool gray)
        {
            GameState s = c.s;
            WorldData w = c.w;
            TalentState t = s.Talent(op.talent) ?? (s.talents.Count > 0 ? s.talents[0] : null);
            if (t == null) return null;
            string cat = op.category;
            CategoryDef cd = w.Category(cat);
            GrayDef gd = gray ? w.gray[Math.Max(0, op.level) % Math.Max(1, w.gray.Length)] : null;
            string product = gray ? gd.product : (cd != null ? cd.product : "工商直播");

            SponsorFacts f = new SponsorFacts();
            f.hours = 1 + r.Next(3);
            f.quote = op.amount;
            int day = FreeDayFrom(s, t.id, Math.Max(op.day, s.day + 2));
            f.writtenDay = day;
            f.realDay = day;

            // 決定這份合約藏了哪些錯
            List<string> pool = gray ? new List<string> { "name", "date" } : new List<string> { "fee", "name", "pen", "hours", "corp", "date" };
            Booking excl = null;
            if (!gray)
            {
                foreach (Booking b in s.bookings)
                    if (!b.done && b.talent == t.id && b.category == cat && b.brand != op.brand && Math.Abs(b.day - day) <= 20) { excl = b; break; }
                if (excl != null) pool.Add("excl");
            }
            int[] weights = s.act <= 1 ? new[] { 15, 40, 35, 10 } : new[] { 8, 30, 40, 22 };
            if (gray) weights = new[] { 40, 45, 15, 0 };
            int k = WeightedIndex(r, weights);
            Shuffle(r, pool);
            for (int i = 0; i < pool.Count && f.errors.Count < k; i++) f.errors.Add(pool[i]);
            if (excl == null) f.errors.Remove("excl");

            int conflictDay = 0;
            if (f.errors.Contains("date"))
            {
                // 找一天藝人有事：優先用真的行程，沒有就安排一個私人行程
                for (int dd = s.day + 2; dd <= s.day + 7 && conflictDay == 0; dd++)
                    if (BusyOn(s, t.id, dd, null) != null) conflictDay = dd;
                if (conflictDay == 0)
                {
                    conflictDay = s.day + 2 + r.Next(4);
                    s.busy.Add(new Busy { talent = t.id, day = conflictDay, text = BusyTexts[r.Next(BusyTexts.Length)] });
                }
                f.writtenDay = conflictDay;
                f.realDay = FreeDayFrom(s, t.id, conflictDay + 1);
            }
            if (f.errors.Contains("pen")) f.penalty = 2 + r.Next(2);
            if (f.errors.Contains("excl")) f.exclBrand = excl.brand;

            long contractFee = f.quote;
            if (f.errors.Contains("fee"))
            {
                contractFee = Nice(f.quote * (0.8 + 0.05 * r.Next(3)));
                if (contractFee >= f.quote) contractFee = f.quote - Math.Max(100, Nice(f.quote * 0.1));
            }
            string company = op.company;
            long shift = Nice(f.quote * 0.03);

            // ---- 文件 ----
            List<DocumentData> docs = new List<DocumentData>();
            List<ContradictionData> cons = new List<ContradictionData>();
            int found = f.errors.Count;
            string ctitle = s.HasUpgrade("scanner") ? "合約・疑點 " + found : "合作合約";

            List<FieldData> cf = new List<FieldData>
            {
                F("c_a", "甲方", f.errors.Contains("corp") ? Misspell(company, 1) : company),
                F("c_t", "實況主", f.errors.Contains("name") ? Misspell(t.name, 0) : t.name),
                F("c_k", "合作形式", product),
                F("c_d", "直播日期", Calendar.Long(f.writtenDay)),
                F("c_h", "直播時數", (f.errors.Contains("hours") ? f.hours + 1 : f.hours) + " 小時"),
                F("c_f", "合作費用", GameState.Money(contractFee)),
                F("c_p", "違約賠償", "費用 " + f.penalty + " 倍")
            };
            if (!gray) cf.Add(F("c_x", "排他條款", f.errors.Contains("excl") ? "30日不接" + cat : "無"));
            DocumentData cdoc = Doc("c", ctitle, "合約", "contract", PContract, HContract, 56, cf.ToArray());
            cdoc.stamp = true;
            docs.Add(cdoc);

            if (!gray)
            {
                docs.Add(Doc("q", "報價單", "報價單", "quote", PQuote, HQuote, 56,
                    F("q_a", "客戶", company),
                    F("q_t", "實況主", t.name),
                    F("q_i", "項目", product + f.hours + "小時"),
                    F("q_f", "報價金額", GameState.Money(f.quote)),
                    F("q_v", "有效期限", "14 天")));
            }

            // 藝人行程：一定包含合約日期、撞期那天、排他對象那天
            List<int> days = new List<int> { f.writtenDay };
            if (excl != null && f.errors.Contains("excl") && !days.Contains(excl.day)) days.Add(excl.day);
            for (int dd = f.writtenDay - 1; days.Count < 4 && dd <= f.writtenDay + 6; dd++)
            {
                int pickDay = dd;
                if (pickDay <= s.day || days.Contains(pickDay)) continue;
                days.Add(pickDay);
            }
            days.Sort();
            List<FieldData> sched = new List<FieldData>();
            foreach (int dd in days)
            {
                string busy = BusyOn(s, t.id, dd, null);
                sched.Add(F("k_" + dd, Calendar.Short(dd), busy ?? "空檔"));
            }
            docs.Add(TalentDoc("r", "藝人檔案・" + t.name, "藝人檔", t.portrait,
                new[] { F("r_n", "藝名", t.name), F("r_t", "類型", t.genre), F("r_f", "粉絲", FansText(t.fans)), F("r_p", "討厭", Dislikes(t.dislikes)) },
                sched.ToArray(), false, Likes(t.likes)));

            if (!gray)
            {
                docs.Add(Handbook("合約規則", "金額同報價單", "藝名要寫對", "日期不可撞期", "賠償上限1倍", "時數同報價單", "排他不含既有約", "公司名對名片"));
                docs.Add(Doc("bc", "名片", "名片", "card", PCard, HCard, 32,
                    F("b_c", "公司", company), F("b_n", "姓名", op.npcName), F("b_j", "職稱", "行銷部" + (op.npcName.Length > 1 ? op.npcName.Substring(1) : "")), F("b_e", "分機", (200 + Hash(company) % 700).ToString())));
                docs.Add(OfferDoc("議價單"));
            }

            // ---- 矛盾 ----
            if (f.errors.Contains("fee"))
            {
                ContradictionData x = C("fee", "c_f", "q_f", "金額不一致", "c_f", GameState.Money(f.quote), shift, "啊，那是舊版合約，以報價單為準。", "合約少寫 " + GameState.Money(f.quote - contractFee));
                x.fee = (int)f.quote;
                cons.Add(x);
            }
            if (f.errors.Contains("name")) cons.Add(C("name", "c_t", "r_n", "名字寫錯", "c_t", t.name, shift, "抱歉抱歉，名字打錯了，馬上改。", "藝名寫錯，對方可能拒付部分款項"));
            if (f.errors.Contains("corp")) cons.Add(C("corp", "c_a", "b_c", "公司名不符", "c_a", company, shift, "呃，公司名稱打錯了，我更正。", "甲方名稱錯誤，合約效力有疑慮"));
            if (f.errors.Contains("pen")) cons.Add(C("pen", "c_p", "h_3", "賠償過高", "c_p", "費用 1 倍", shift, "這是制式條款……好吧，改成 1 倍。", "違約賠償 " + f.penalty + " 倍，出事時損失加倍"));
            if (f.errors.Contains("hours")) cons.Add(C("hours", "c_h", "q_i", "時數不一致", "c_h", f.hours + " 小時", shift, "報價是 " + f.hours + " 小時沒錯，合約改回來。", "多播 1 小時沒有多收錢，" + t.name + "會累"));
            if (f.errors.Contains("date")) cons.Add(C("date", "c_d", "k_" + f.writtenDay, "日期撞期", "c_d", Calendar.Long(f.realDay), shift, "那天他有事？那改 " + Calendar.Short(f.realDay) + " 好了。", Calendar.Short(f.writtenDay) + " 撞期，可能會開天窗"));
            if (f.errors.Contains("excl")) cons.Add(C("excl", "c_x", "k_" + excl.day, "排他衝突", "c_x", "直播後才起算", shift, "他已經接了" + excl.brand + "？那排他期改成直播後才起算。", "排他條款跟" + excl.brand + "衝突，對方可能求償"));

            bool likes = Array.IndexOf(t.likes, cat) >= 0;
            bool hates = Array.IndexOf(t.dislikes, cat) >= 0;
            string attitude = gray ? "這個錢很多欸！老闆你決定就好。"
                : hates ? "蛤？" + cat + "類的工商喔……我不太喜歡欸。"
                : likes ? op.brand + "？這個我喜歡！"
                : op.brand + "喔？好啊，我沒差。";

            CaseData d = Base(op, op.title, gray ? "我們有一個轉播的案子，報酬很高，就是要快點簽。" : SponsorGreeting(op, t, r), gray ? 4 : 5);
            d.baseFee = (int)contractFee;
            d.deferFee = true;
            d.payNote = "直播（" + Calendar.Short(f.writtenDay) + "）完成後入帳";
            if (hates) d.signedWarning = "注意：" + t.name + "討厭" + cat + "類工商";
            d.contacts = Contacts(Caller(op), new ContactData { id = t.portrait, name = t.name, message = attitude }, Vboss(s));
            d.replySigned = gray ? "爽快！錢明天就匯。" : "合作愉快！";
            d.replyReject = gray ? "嘖，不做就算了。" : "了解，有機會再合作。";
            d.replyHold = "好，等你消息。";
            d.replyWrong = "嗯？這兩個有什麼問題嗎？";
            d.replyLeave = "我看我們找別家好了。";

            if (gray)
            {
                Gray(c, op, r, d, docs, cons, f, gd, company);
            }
            else
            {
                d.offer = new OfferData
                {
                    docId = "offer", mode = "raise", label = "合作費用", apply = "fee", feeField = "c_f",
                    ask = (int)f.quote, start = (int)f.quote, step = Step(f.quote),
                    min = (int)Nice(f.quote * 0.7), max = (int)Nice(f.quote * 1.8),
                    accept = (int)Nice(f.quote * (1.04 + 0.01 * s.RelValue(op.npc) + (likes ? 0.02 : 0))),
                    walk = (int)Nice(f.quote * 1.22),
                    acceptText = "好，{v}，就這麼說定。",
                    counterText = "{v} 是我們的上限了。",
                    refuseText = "這價錢太誇張了吧……"
                };
                d.startSlots = new[] { "c", "q", null, null };
                d.storage = new[] { "c", "q", "r", "hb", "bc", "offer" };
                d.tasks = new[] { Task("檢查合約", "tried|result"), Task("談合作費用（可選）", "offer"), Task("簽約或退回", "result") };
                if (s.talents.Count >= 2)
                {
                    d.assign = BuildAssign(c, op, cat, f.writtenDay, false, t, f.quote, null, 0);
                    docs.Add(RosterDoc());
                    d.storage = new[] { "c", "q", "r", "hb", "bc", "offer", "roster" };
                }
            }
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            c.facts[op.uid] = f;
            return d;
        }

        // ------------------------------------------------------------------
        // 工商派人
        // ------------------------------------------------------------------

        static DocumentData RosterDoc()
        {
            DocumentData d = Doc("roster", "旗下藝人", "藝人", "folder", PTalent, HTalent, 32);
            d.kind = "roster";
            return d;
        }

        /// <summary>
        /// 旗下每位藝人適不適合這個工商。
        /// 對方沒指定：看類型、喜不喜歡這類產品、粉絲夠不夠，決定直播後加碼或被解約的機會。
        /// 對方有指定：換成粉絲比較少的人，費用照比例變少；討厭這類產品、粉絲不到一半，對方不接受。
        /// </summary>
        static AssignData BuildAssign(Career c, Opportunity op, string cat, int day, bool open, TalentState wanted, long quote, string genre, int minFans)
        {
            GameState s = c.s;
            AssignData a = new AssignData
            {
                open = open, wanted = wanted != null ? wanted.id : null, wantedName = wanted != null ? wanted.name : null,
                category = cat, genre = genre, minFans = minFans, day = day, quote = quote
            };
            List<RosterEntry> list = new List<RosterEntry>();
            foreach (TalentState t in s.talents)
            {
                if (list.Count >= 5) break;
                bool hates = Array.IndexOf(t.dislikes, cat) >= 0;
                bool likes = Array.IndexOf(t.likes, cat) >= 0;
                string busy = BusyOn(s, t.id, day, null);
                RosterEntry e = new RosterEntry
                {
                    id = t.id, name = t.name, portrait = t.portrait, genre = t.genre, fans = t.fans,
                    likes = t.likes.Length > 0 ? string.Join("、", t.likes) : "沒特別",
                    dislikes = Dislikes(t.dislikes),
                    busy = busy != null,
                    status = Calendar.Short(day) + (busy != null ? " 有約：" + busy : " 有空")
                };
                if (open)
                {
                    bool genreOk = t.genre == genre;
                    bool fansOk = t.fans >= minFans;
                    int fit;
                    if (hates) fit = 0;
                    else if (fansOk && genreOk && likes) fit = 3;
                    else if (fansOk && (genreOk || likes)) fit = 2;
                    else if (fansOk || genreOk || likes) fit = 1;
                    else fit = 0;
                    e.fit = fit;
                    e.fitText = FitText[fit];
                    e.outcome = FitOutcome[fit];
                    e.reply = fit == 3 ? "太好了！就是要這種人！"
                        : fit == 2 ? "可以，就" + t.name + "吧。"
                        : fit == 1 ? "嗯……跟我們想的不太一樣，先這樣吧。"
                        : hates ? t.name + "好像不太喜歡" + cat + "類的東西……你確定？"
                        : "粉絲好像少了點，類型也不太對……你確定？";
                }
                else if (wanted != null && t.id == wanted.id)
                {
                    e.swapFee = quote;
                    e.fitText = "對方指定";
                    e.outcome = "費用照原本 " + GameState.Short(quote);
                    e.reply = "好，那就照原本的，" + t.name + "。";
                }
                else
                {
                    double ratio = wanted != null && wanted.fans > 0 ? t.fans / (double)wanted.fans : 1.0;
                    if (hates)
                    {
                        e.swapFee = 0;
                        e.fitText = "對方不會答應";
                        e.outcome = t.name + "討厭" + cat + "類";
                        e.reply = t.name + "不是討厭" + cat + "類嗎？這樣不行。";
                    }
                    else if (ratio < 0.5)
                    {
                        e.swapFee = 0;
                        e.fitText = "對方不會答應";
                        e.outcome = "粉絲不到" + (wanted != null ? wanted.name : "原本") + "的一半";
                        e.reply = "粉絲差太多了，我們就是要" + (wanted != null ? wanted.name : "原本那位") + "。";
                    }
                    else
                    {
                        long fee = Math.Min(quote, Nice(quote * Math.Min(1.0, ratio) * (likes ? 1.0 : 0.95)));
                        e.swapFee = fee;
                        e.fitText = fee >= quote ? "可以換" : "可以換，但費用變少";
                        e.outcome = "費用變成 " + GameState.Short(fee);
                        e.reply = fee >= quote ? "換成" + t.name + "也行，費用照舊。" : "換成" + t.name + "？可以，但費用只能給 " + GameState.Short(fee) + "。";
                    }
                }
                list.Add(e);
            }
            a.roster = list.ToArray();
            return a;
        }

        public static readonly string[] FitText = { "不合適", "勉強", "合適", "最合適" };
        public static readonly string[] FitOutcome = { "一半機會被解約", "兩成機會被解約", "正常入帳", "做得好會加碼 2 成" };

        /// <summary>對方沒指定藝人的工商：寫明需求，自己挑人。</summary>
        static CaseData SponsorOpen(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            WorldData w = c.w;
            string cat = op.category;
            CategoryDef cd = w.Category(cat);
            string product = cd != null ? cd.product : "工商直播";
            string genre = string.IsNullOrEmpty(op.text) ? "遊戲實況" : op.text;
            int minFans = Math.Max(1000, op.level);

            SponsorFacts f = new SponsorFacts();
            f.hours = 1 + r.Next(3);
            f.quote = op.amount;
            int day = Math.Max(op.day, s.day + 2);
            f.writtenDay = day;
            f.realDay = day;

            // 藝人還沒決定，所以只會有金額、賠償、時數、公司名的錯
            List<string> pool = new List<string> { "fee", "pen", "hours", "corp" };
            int k = WeightedIndex(r, s.act <= 1 ? new[] { 20, 45, 30, 5 } : new[] { 10, 35, 40, 15 });
            Shuffle(r, pool);
            for (int i = 0; i < pool.Count && f.errors.Count < k; i++) f.errors.Add(pool[i]);
            if (f.errors.Contains("pen")) f.penalty = 2 + r.Next(2);
            long contractFee = f.quote;
            if (f.errors.Contains("fee"))
            {
                contractFee = Nice(f.quote * (0.8 + 0.05 * r.Next(3)));
                if (contractFee >= f.quote) contractFee = f.quote - Math.Max(100, Nice(f.quote * 0.1));
            }
            string company = op.company;
            long shift = Nice(f.quote * 0.03);

            List<DocumentData> docs = new List<DocumentData>();
            List<ContradictionData> cons = new List<ContradictionData>();
            string ctitle = s.HasUpgrade("scanner") ? "合約・疑點 " + f.errors.Count : "合作合約";
            DocumentData cdoc = Doc("c", ctitle, "合約", "contract", PContract, HContract, 56,
                F("c_a", "甲方", f.errors.Contains("corp") ? Misspell(company, 1) : company),
                F("c_t", "實況主", "（待定）"),
                F("c_k", "合作形式", product),
                F("c_d", "直播日期", Calendar.Long(day)),
                F("c_h", "直播時數", (f.errors.Contains("hours") ? f.hours + 1 : f.hours) + " 小時"),
                F("c_f", "合作費用", GameState.Money(contractFee)),
                F("c_p", "違約賠償", "費用 " + f.penalty + " 倍"),
                F("c_x", "排他條款", "無"));
            cdoc.stamp = true;
            docs.Add(cdoc);
            docs.Add(Doc("q", "報價單", "報價單", "quote", PQuote, HQuote, 56,
                F("q_a", "客戶", company),
                F("q_t", "實況主", "不指定"),
                F("q_g", "希望類型", genre),
                F("q_m", "粉絲至少", FansText(minFans)),
                F("q_i", "項目", product + f.hours + "小時"),
                F("q_f", "報價金額", GameState.Money(f.quote)),
                F("q_v", "有效期限", "14 天")));
            docs.Add(RosterDoc());
            docs.Add(Handbook("合約規則", "金額同報價單", "賠償上限1倍", "時數同報價單", "公司名對名片", "派最合適的人"));
            docs.Add(Doc("bc", "名片", "名片", "card", PCard, HCard, 32,
                F("b_c", "公司", company), F("b_n", "姓名", op.npcName), F("b_j", "職稱", "行銷部" + (op.npcName.Length > 1 ? op.npcName.Substring(1) : "")), F("b_e", "分機", (200 + Hash(company) % 700).ToString())));
            docs.Add(OfferDoc("議價單"));

            if (f.errors.Contains("fee"))
            {
                ContradictionData x = C("fee", "c_f", "q_f", "金額不一致", "c_f", GameState.Money(f.quote), shift, "啊，那是舊版合約，以報價單為準。", "合約少寫 " + GameState.Money(f.quote - contractFee));
                x.fee = (int)f.quote;
                cons.Add(x);
            }
            if (f.errors.Contains("corp")) cons.Add(C("corp", "c_a", "b_c", "公司名不符", "c_a", company, shift, "呃，公司名稱打錯了，我更正。", "甲方名稱錯誤，合約效力有疑慮"));
            if (f.errors.Contains("pen")) cons.Add(C("pen", "c_p", "h_1", "賠償過高", "c_p", "費用 1 倍", shift, "這是制式條款……好吧，改成 1 倍。", "違約賠償 " + f.penalty + " 倍，出事時損失加倍"));
            if (f.errors.Contains("hours")) cons.Add(C("hours", "c_h", "q_i", "時數不一致", "c_h", f.hours + " 小時", shift, "報價是 " + f.hours + " 小時沒錯，合約改回來。", "多播 1 小時沒有多收錢，藝人會累"));

            CaseData d = Base(op, op.title, "你好，我是" + op.company + "的" + op.npcName + "。想找你們的實況主開工商，人選你們決定，需求寫在報價單上。", 5);
            d.baseFee = (int)contractFee;
            d.deferFee = true;
            d.payNote = "直播（" + Calendar.Short(day) + "）完成後入帳";
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.replySigned = "合作愉快！期待你們的人選。";
            d.replyReject = "了解，有機會再合作。";
            d.replyHold = "好，等你消息。";
            d.replyWrong = "嗯？這兩個有什麼問題嗎？";
            d.replyLeave = "我看我們找別家好了。";
            d.offer = new OfferData
            {
                docId = "offer", mode = "raise", label = "合作費用", apply = "fee", feeField = "c_f",
                ask = (int)f.quote, start = (int)f.quote, step = Step(f.quote),
                min = (int)Nice(f.quote * 0.7), max = (int)Nice(f.quote * 1.8),
                accept = (int)Nice(f.quote * (1.04 + 0.01 * s.RelValue(op.npc))),
                walk = (int)Nice(f.quote * 1.22),
                acceptText = "好，{v}，就這麼說定。",
                counterText = "{v} 是我們的上限了。",
                refuseText = "這價錢太誇張了吧……"
            };
            d.assign = BuildAssign(c, op, cat, day, true, null, f.quote, genre, minFans);
            d.startSlots = new[] { "c", "q", "roster", null };
            d.storage = new[] { "c", "q", "roster", "hb", "bc", "offer" };
            d.tasks = new[] { Task("選擇藝人", "assigned"), Task("檢查合約", "tried|result"), Task("談合作費用（可選）", "offer"), Task("簽約或退回", "result") };
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            c.facts[op.uid] = f;
            return d;
        }

        static string SponsorGreeting(Opportunity op, TalentState t, Random r)
        {
            string[] g =
            {
                "你好，我是" + op.company + "的" + op.npcName + "。想請" + t.name + "幫我們開工商，合約傳過去了。",
                op.npcName + "這邊！" + op.brand + "想找" + t.name + "合作，文件都在這裡。",
                "您好～" + op.brand + "想跟" + t.name + "合作，麻煩看一下合約。"
            };
            return g[r.Next(g.Length)];
        }

        static readonly string[] ShadyCo = { "宏遠傳播", "金鑫娛樂", "天成行銷", "永盛國際", "大發媒體" };

        static void Gray(Career c, Opportunity op, Random r, CaseData d, List<DocumentData> docs, List<ContradictionData> cons, SponsorFacts f, GrayDef gd, string company)
        {
            GameState s = c.s;
            List<string> clues = new List<string> { "scope", "expire", "holder", "dead" };
            Shuffle(r, clues);
            int n = 1 + (r.NextDouble() < 0.4 ? 1 : 0);
            clues = clues.GetRange(0, n);
            int expire = clues.Contains("expire") ? f.writtenDay - 1 - r.Next(5) : f.writtenDay + 20 + r.Next(30);
            string holder = clues.Contains("holder") ? Misspell(company, 0) : company;

            // 合約要用的欄位：內容來源
            docs[0].fields = Append(docs[0].fields, F("c_s", "內容來源", gd.licensor + "授權"));
            if (s.HasUpgrade("scanner")) docs[0].title = "合約・疑點 " + clues.Count;

            docs.Add(Doc("lic", "授權書", "授權書", "contract", PQuote, HQuote, 56,
                F("l_a", "授權方", gd.licensor),
                F("l_b", "被授權方", holder),
                F("l_c", "授權內容", gd.content),
                F("l_r", "授權範圍", clues.Contains("scope") ? gd.realScope : gd.scope),
                F("l_e", "授權期限", "至" + Calendar.Short(Math.Max(s.day, expire)))));
            docs.Add(Doc("reg", "公司登記資料", "登記", "report", PBook, HBook, 56,
                F("g_n", "公司名稱", company),
                F("g_o", "負責人", NpcName(c.w, company + "boss")),
                F("g_s", "營業狀態", clues.Contains("dead") ? "已解散" : "營業中"),
                F("g_c", "資本額", "NT$100,000")));
            docs.Add(Handbook("灰色案件", "授權範圍要符合", "授權期限要有效", "被授權方要一致", "公司要在營業"));
            docs.Add(OfferDoc("議價單"));
            // 抓到的疑點越多，對方越心虛，加價的成功率越高
            int riskShift = (int)Nice(f.quote * 0.07);

            foreach (string k in clues)
            {
                switch (k)
                {
                    case "scope": cons.Add(C("risk_scope", "c_k", "l_r", "授權範圍不符", null, null, riskShift, "範圍？那個不重要啦，大家都這樣播。", "授權範圍不符，可能被追償")); break;
                    case "expire": cons.Add(C("risk_expire", "c_d", "l_e", "授權已過期", null, null, riskShift, "過期？呃……續約手續還在跑。", "授權過期，可能被追償")); break;
                    case "holder": cons.Add(Alt(C("risk_holder", "c_a", "l_b", "被授權方不符", null, null, riskShift, "那是我們的關係企業啦，一樣的。", "授權給別家公司，可能被追償"), "g_n~l_b")); break;
                    default: cons.Add(C("risk_dead", "c_a", "g_s", "公司已解散", null, null, riskShift, "登記資料還沒更新而已……", "簽約對象已解散，錢可能收不到")); break;
                }
            }
            d.replies = new[]
            {
                new ReplyData { id = "take", text = "加錢就不問", say = "這授權怪怪的。要我們接，得多付三成。", response = "好好好，多三成，別問了。", requires = "found>=1", flag = "", gives = new string[0], shift = (int)Nice(f.quote * 0.2) },
                new ReplyData { id = "refuse", text = "這案子不接", say = "授權有問題，這個案子我們不接。", response = "嘖，膽子真小。", requires = "found>=1", result = "reject", gives = new string[0] }
            };
            d.offer = new OfferData
            {
                docId = "offer", mode = "raise", label = "合作費用", apply = "fee", feeField = "c_f",
                ask = (int)f.quote, start = (int)f.quote, step = Step(f.quote),
                min = (int)Nice(f.quote * 0.7), max = (int)Nice(f.quote * 2.2),
                accept = (int)Nice(f.quote * 1.05), walk = (int)Nice(f.quote * 1.45),
                acceptText = "好啦，{v}，快點簽。",
                counterText = "{v}，再多就不划算了。",
                refuseText = "你們當我們是提款機喔？"
            };
            d.startSlots = new[] { "c", "lic", null, null };
            d.storage = new[] { "c", "lic", "reg", "r", "hb", "offer" };
            d.tasks = new[] { Task("檢查授權", "tried|result"), Task("談合作費用（可選）", "offer"), Task("決定接不接", "result") };
            d.signedWarning = "這個案子的授權來源要多留意";
        }

        /// <summary>加上其他也能證明這個問題的欄位（格式見 ContradictionData.alt）。</summary>
        static ContradictionData Alt(ContradictionData c, string alt)
        {
            c.alt = string.IsNullOrEmpty(c.alt) ? alt : c.alt + "," + alt;
            return c;
        }

        static FieldData[] Append(FieldData[] a, FieldData x)
        {
            FieldData[] b = new FieldData[a.Length + 1];
            Array.Copy(a, b, a.Length);
            b[a.Length] = x;
            return b;
        }

        static int WeightedIndex(Random r, int[] weights)
        {
            int total = 0;
            foreach (int w in weights) total += w;
            int x = r.Next(Math.Max(1, total));
            for (int i = 0; i < weights.Length; i++)
            {
                if (x < weights[i]) return i;
                x -= weights[i];
            }
            return 0;
        }

        // ------------------------------------------------------------------
        // 收購
        // ------------------------------------------------------------------

        static CaseData Buy(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            ItemDef item = c.w.Item(op.item);
            if (item == null) return null;
            int claimed = op.level;
            int real = (int)op.amount2;
            bool stolen = op.kind == "stolen";
            long claimedVal = Market(c, item, claimed);
            long realVal = Market(c, item, real);
            long ask = Nice(claimedVal * 1.05);
            long inspectCost = s.HasUpgrade("inspect") ? 0 : Nice(Math.Max(300, claimedVal * 0.04));

            List<FieldData> tf = new List<FieldData>
            {
                F("t_a", "外觀", Appearance[real]),
                F("t_p", "零件", real == 3 ? item.part + "修過" : real == 4 ? item.part + "燒毀" : "原廠零件"),
                F("t_f", "功能", real == 4 ? "無法開機" : "正常")
            };
            if (s.HasUpgrade("inspect")) tf.Add(F("t_s", "序號", stolen ? "已登記失竊" : "查無異常"));
            tf.Add(F("t_c", "結論", Conclusion[real]));

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("item", "商品資料", "商品", item.icon, PItem, HItem, 56,
                    F("i_n", "品名", item.name),
                    F("i_s", "狀況", GradeText[claimed]),
                    F("i_r", "維修紀錄", "無"),
                    F("i_a", "配件", "齊全"),
                    F("i_o", "來源", stolen ? "朋友轉賣" : "原廠發票"),
                    F("i_p", "賣家開價", GameState.Money(ask))),
                MarketDoc(c, item),
                Doc("inspect", "檢測報告", "檢測", "report", PBook, HBook, 56, tf.ToArray()),
                OfferDoc("議價單"),
                Handbook("交易", "先看行情再出價", "賣家說的要查證", "檢測費算成本", "買進價要留利潤", "來源不明不要收")
            };

            List<ContradictionData> cons = new List<ContradictionData>();
            long drop = (long)((claimedVal - realVal) * 0.95);
            List<string> defects = new List<string>();
            if (real == 4) defects.Add("broken");
            if (Appearance[real] != Appearance[claimed]) defects.Add("cond");
            if (real >= 3) defects.Add("repair");
            float[] share = { 0.4f, 0.35f, 0.25f };
            float total = 0;
            for (int i = 0; i < defects.Count; i++) total += share[Array.IndexOf(new[] { "cond", "repair", "broken" }, defects[i])];
            foreach (string def in defects)
            {
                float part = share[Array.IndexOf(new[] { "cond", "repair", "broken" }, def)] / Math.Max(0.01f, total);
                long sh = (long)(drop * part);
                switch (def)
                {
                    case "cond": cons.Add(Alt(C("cond", "i_s", "t_a", "狀況不實", "i_s", real >= 2 ? "有刮傷" : "近全新", sh, "呃……好啦，是有點痕跡，算你便宜一點。", null), "t_c")); break;
                    case "repair": cons.Add(Alt(C("repair", "i_r", "t_p", "隱瞞維修", "i_r", real == 4 ? item.part + "燒毀" : item.part + "修過", sh, "唉唷，修過一次而已啦……好啦，再便宜一點。", null), real == 3 ? "t_c" : "")); break;
                    default: cons.Add(Alt(C("broken", "i_s", "t_f", "其實故障", "i_s", "故障品", sh, "壞、壞掉了？我寄出前還好好的啊……", null), "t_c")); break;
                }
            }
            List<ReplyData> reps = new List<ReplyData>
            {
                new ReplyData { id = "inspect", text = inspectCost > 0 ? "先送檢測" : "用設備檢測", say = inspectCost > 0 ? "可以先送檢測嗎？檢測費我們出。" : "我先用公司的設備檢查一下。",
                    response = inspectCost > 0 ? "可以啊，檢測費 " + GameState.Money(inspectCost) + " 你出。報告傳過去了。" : "好啊，你檢查，我等你。", gives = new[] { "inspect" }, cost = (int)inspectCost }
            };
            if (stolen && s.HasUpgrade("inspect"))
            {
                cons.Add(C("stolen", "i_o", "t_s", "疑似贓物", null, null, 0, "失、失竊？那是朋友轉賣給我的，我不知道啦！", "這個商品可能是贓物"));
                reps.Add(new ReplyData { id = "nostolen", text = "贓物不收", say = "序號登記失竊，這東西我們不收。", response = "……算了，當我沒打來。", requires = "found:stolen", result = "reject", gives = new string[0] });
            }

            CaseData d = Base(op, "買進" + item.name, BuyGreeting(op, item, claimed, ask, r), 5);
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            d.replies = reps.ToArray();
            d.offer = new OfferData
            {
                docId = "offer", mode = "lower", label = "買進價", apply = "buy",
                itemId = op.uid, itemName = item.name, itemCondition = GradeText[claimed], itemCategory = item.cat,
                itemGrade = real, itemBase = item.price, itemFlags = stolen ? "stolen" : "", refId = op.uid,
                ask = (int)ask, start = (int)ask, step = Step(ask), min = (int)Nice(ask * 0.1), max = (int)Nice(ask * 1.3),
                accept = (int)Nice(claimedVal * 0.9), walk = (int)Nice(claimedVal * 0.74),
                acceptText = "好啦，{v} 成交。", counterText = "這價太低了啦，最少 {v}。", refuseText = "你在開玩笑嗎？這價錢我自己留著用！",
                endsCall = true
            };
            d.startSlots = new[] { "item", "market", null, null };
            d.storage = new[] { "item", "market", "offer", "hb" };
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.tasks = new[] { Task("送檢測", "reply:inspect"), Task("找出商品問題", "found>=1"), Task("談好買進價", "offer|result") };
            d.replyWrong = "蛤？哪裡有問題？";
            d.replyLeave = "不賣了不賣了，你這人太難搞。";
            d.replyHold = "好吧，想清楚再打給我。";
            d.replyReject = "好啦，不收就算了。";
            d.dealTitle = "買進成交：" + item.name;
            return d;
        }

        static string BuyGreeting(Opportunity op, ItemDef item, int grade, long ask, Random r)
        {
            string[] g =
            {
                "我這邊有一台" + item.name + "，" + GradeText[grade] + "的，" + GameState.Short(ask) + "讓你。",
                item.name + "要不要？" + GradeText[grade] + "，" + GameState.Short(ask) + "就好，很划算啦！",
                "聽說你們在收設備？我有" + item.name + "，狀況" + GradeText[grade] + "。"
            };
            return g[r.Next(g.Length)];
        }

        // ------------------------------------------------------------------
        // 出售
        // ------------------------------------------------------------------

        static readonly string[] BuyerUse = { "開台直播用", "社團比賽用", "網咖升級", "工作室使用", "節目製作", "婚禮攝影" };

        static CaseData Sell(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            InvItem inv = s.inventory.Find(i => i.id == op.item);
            if (inv == null) return null;
            ItemDef item = c.w.Item(ItemIdOf(c.w, inv)) ?? new ItemDef { name = inv.name, cat = inv.category, price = (int)Math.Max(1000, inv.baseValue), icon = "box", part = "零件" };
            long budget = op.amount;
            int known = Math.Max(0, Array.IndexOf(GradeText, inv.condition));
            long knownVal = Market(c, item, known);
            bool flawed = inv.grade >= 2;

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("need", "買家需求", "需求", "request", PCard, HCard, 56,
                    F("n_u", "用途", BuyerUse[Hash(op.npc) % BuyerUse.Length]),
                    F("n_w", "想買", item.name),
                    F("n_b", "預算", r.NextDouble() < 0.4 ? "約 " + GameState.Short(Nice(budget * 0.95)) : "沒有說"),
                    F("n_n", "備註", flawed ? "希望東西耐用" : "想買狀況好的")),
                Doc("inv", inv.name.Length <= 5 ? "庫存・" + inv.name : inv.name, "庫存", item.icon, PItem, HItem, 56,
                    F("v_n", "品名", inv.name),
                    F("v_s", "狀況", inv.condition),
                    F("v_c", "成本", GameState.Money(inv.cost)),
                    F("v_d", "買進日", Calendar.Long(Math.Max(1, inv.boughtDay)))),
                MarketDoc(c, item),
                OfferDoc("議價單")
            };
            List<ReplyData> reps = new List<ReplyData>();
            if (flawed)
                reps.Add(new ReplyData { id = "honest", text = "坦白說有瑕疵", say = "先跟你說，這台有瑕疵，但功能正常。", response = "謝謝你老實說！價錢我可以多出一點。", shift = (int)Nice(budget * 0.04), gives = new string[0] });

            CaseData d = Base(op, "賣出" + inv.name, "你好！聽說你們有" + item.name + "？我想買，價錢好談。", 5);
            d.documents = docs.ToArray();
            d.replies = reps.ToArray();
            d.offer = new OfferData
            {
                docId = "offer", mode = "raise", label = "賣出價", apply = "sell", itemId = inv.id, refId = op.uid,
                ask = (int)Nice(budget * 0.85), start = (int)Nice(Math.Min(budget * 1.6, Math.Max(knownVal * 1.15, budget * 0.9))), step = Step(budget),
                min = (int)Nice(budget * 0.3), max = (int)Nice(budget * 2.5), accept = (int)budget, walk = (int)Nice(budget * 1.12),
                acceptText = "好！{v}，我買了！", counterText = "{v} 是我的極限了。", refuseText = "太、太貴了吧……", endsCall = true
            };
            d.startSlots = new[] { "need", "inv", "market", null };
            d.storage = new[] { "need", "inv", "market", "offer" };
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.tasks = new[] { Task("看成本和行情", "open:inv|open:market"), Task("賣出商品", "offer|result") };
            d.replyLeave = "我再看看別家好了……";
            d.replyHold = "好，我等你消息。";
            d.dealTitle = "賣出成交：" + inv.name;
            return d;
        }

        public static string ItemIdOf(WorldData w, InvItem inv)
        {
            foreach (ItemDef i in w.items) if (i.name == inv.name) return i.id;
            return null;
        }

        // ------------------------------------------------------------------
        // 仲介
        // ------------------------------------------------------------------

        static CaseData Broker(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            WorldData w = c.w;
            string cat = op.category;
            long budget = op.amount;
            List<TalentDef> pool = new List<TalentDef>();
            foreach (TalentDef t in w.talents) if (s.Talent(t.id) == null) pool.Add(t);
            if (pool.Count < 3) return null;

            // 最適合的人：類型對、不排斥這類工商、報價在預算內
            TalentDef right = pool.Find(t => t.id == op.talent && Array.IndexOf(t.dislikes, cat) < 0 && c.TalentQuote(t) <= budget);
            if (right == null)
            {
                List<TalentDef> okList = pool.FindAll(t => Array.IndexOf(t.dislikes, cat) < 0 && c.TalentQuote(t) <= budget);
                if (okList.Count == 0) return null;
                right = Pick(r, okList);
            }
            string genre = right.genre;
            int day = op.day > s.day ? op.day : s.day + 4;

            // 其他兩位：同類型的要靠報價、偏好或檔期才刷得掉；不同類型的看類型就知道
            List<TalentDef> others = new List<TalentDef>(pool);
            others.Remove(right);
            Shuffle(r, others);
            List<TalentDef> same = others.FindAll(t => t.genre == genre);
            List<TalentDef> diff = others.FindAll(t => t.genre != genre);
            List<TalentDef> wrong = new List<TalentDef>();
            if (same.Count > 0) wrong.Add(same[0]);
            if (same.Count > 1 && r.NextDouble() < 0.5) wrong.Add(same[1]);
            foreach (TalentDef t in diff) { if (wrong.Count >= 2) break; wrong.Add(t); }
            foreach (TalentDef t in same) { if (wrong.Count >= 2) break; if (!wrong.Contains(t)) wrong.Add(t); }
            if (wrong.Count < 2) return null;

            List<KeyValuePair<TalentDef, string>> cands = new List<KeyValuePair<TalentDef, string>> { new KeyValuePair<TalentDef, string>(right, "ok") };
            foreach (TalentDef t in wrong)
            {
                bool hate = Array.IndexOf(t.dislikes, cat) >= 0;
                bool pricey = c.TalentQuote(t) > budget;
                string why = t.genre != genre ? "genre" : pricey ? "fee" : hate ? "hate" : "busy";
                cands.Add(new KeyValuePair<TalentDef, string>(t, why));
            }
            Shuffle(r, cands);

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("req", "合作需求單", "需求單", "request", PCard, HCard, 56,
                    F("n_k", "合作內容", cat + "工商直播"),
                    F("n_g", "需要類型", genre),
                    F("n_b", "預算上限", GameState.Money(budget)),
                    F("n_d", "檔期", Calendar.Long(day)),
                    F("n_x", "條件", "不能排斥" + cat))
            };
            List<string> ids = new List<string>();
            int idx = 0;
            string correctId = null;
            foreach (KeyValuePair<TalentDef, string> kv in cands)
            {
                TalentDef t = kv.Key;
                string fl = kv.Value;
                string id = "t" + idx++;
                ids.Add(id);
                if (fl == "ok") correctId = id;
                // 報價一律用標準公式：粉絲越多、類型越熱門越貴
                long fee = c.TalentQuote(t);
                string avail = fl == "busy" ? Calendar.Short(day) + " 有約" : Calendar.Short(day) + " 有空";
                docs.Add(TalentDoc(id, "藝人檔案・" + t.name, t.name, t.portrait,
                    new[] { F(id + "_n", "藝名", t.name), F(id + "_t", "類型", t.genre), F(id + "_f", "粉絲", FansText(t.fans)), F(id + "_p", "討厭", Dislikes(t.dislikes)) },
                    new[] { F(id + "_1", "檔期", avail), F(id + "_2", "報價", GameState.Money(fee)),
                        string.IsNullOrEmpty(t.interests) ? F(id + "_3", "風格", t.style) : F(id + "_3", "興趣", t.interests) },
                    true, "合作資訊"));
            }
            docs.Add(Handbook("仲介", "類型要符合", "報價不能超預算", "檔期要有空", "偏好不能衝突", "報價依粉絲計算"));
            docs.Add(OfferDoc("議價單"));

            long reward = Nice(budget * 0.22 * c.NetBonus);
            CaseData d = Base(op, "仲介：" + op.brand, "我們想找一位" + genre + "的實況主合作，你們有人選嗎？", 5);
            d.documents = docs.ToArray();
            d.recommend = new RecommendData
            {
                correct = correctId, reward = (int)reward, gives = new string[0],
                okText = "就是他了！條件完全符合，謝謝你們！", badText = "嗯……這位好像不符合我們的需求耶？"
            };
            d.offer = new OfferData
            {
                docId = "offer", mode = "raise", label = "佣金", apply = "commission",
                ask = (int)reward, start = (int)reward, step = Step(reward),
                min = (int)Nice(reward * 0.6), max = (int)Nice(reward * 2.2),
                accept = (int)Nice(reward * (1.05 + 0.01 * s.RelValue(op.npc))), walk = (int)Nice(reward * 1.45),
                acceptText = "好，佣金 {v}，人選對就照這個算。",
                counterText = "佣金最多 {v}，不能再多了。",
                refuseText = "這佣金也太敢開了吧……"
            };
            List<string> storage = new List<string> { "req" };
            storage.AddRange(ids);
            storage.Add("hb");
            storage.Add("offer");
            d.storage = storage.ToArray();
            d.startSlots = new[] { "req", null, null, null };
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.tasks = new[] { Task("看需求單", "open:req"), Task("談佣金（可選）", "offer"), Task("推薦合適的人", "recommend|result") };
            d.replyLeave = "我們再找別家問問看好了。";
            d.replyHold = "好，那等你們消息。";
            d.dealTitle = "仲介成功：" + op.brand;
            return d;
        }

        // ------------------------------------------------------------------
        // 企劃
        // ------------------------------------------------------------------

        static CaseData Plan(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            WorldData w = c.w;
            long tier = c.Tier;
            if (w.planGroups.Length < 4) return null;

            List<int> gi = new List<int>();
            for (int i = 0; i < w.planGroups.Length; i++) gi.Add(i);
            Shuffle(r, gi);
            gi = gi.GetRange(0, 4);
            gi.Sort();

            List<PlanGroup> groups = new List<PlanGroup>();
            int[] target = new int[4];
            foreach (int g in gi)
            {
                PlanGroupDef def = w.planGroups[g];
                List<PlanOptDef> opts = new List<PlanOptDef>(def.options);
                Shuffle(r, opts);
                List<PlanOption> chosen = new List<PlanOption>();
                for (int j = 0; j < 3 && j < opts.Count; j++)
                {
                    PlanOptDef o = opts[j];
                    long cost = o.cost * tier;
                    if (o.id == "studio" && s.HasUpgrade("studio")) cost = 0;
                    chosen.Add(new PlanOption { id = o.id, text = o.text, cost = (int)cost, tags = o.tags });
                }
                groups.Add(new PlanGroup { name = def.name, options = chosen.ToArray() });
            }
            for (int g = 0; g < 4; g++) target[g] = r.Next(groups[g].options.Length);

            // 需求：從目標組合挑兩個必要標籤、一個禁止標籤，預算比目標成本多一點
            List<string> have = new List<string>();
            long targetCost = 0;
            for (int g = 0; g < 4; g++)
            {
                PlanOption o = groups[g].options[target[g]];
                targetCost += o.cost;
                foreach (string t in o.tags) if (!have.Contains(t) && w.Tag(t) != null) have.Add(t);
            }
            Shuffle(r, have);
            List<PlanReq> reqs = new List<PlanReq>();
            for (int i = 0; i < have.Count && reqs.Count < 2; i++) reqs.Add(new PlanReq { text = w.Tag(have[i]).need, tag = have[i] });
            List<string> avoid = new List<string>();
            for (int g = 0; g < 4; g++)
                for (int j = 0; j < groups[g].options.Length; j++)
                {
                    if (j == target[g]) continue;
                    foreach (string t in groups[g].options[j].tags)
                    {
                        TagText tt = w.Tag(t);
                        if (tt != null && !string.IsNullOrEmpty(tt.avoid) && !have.Contains(t) && !avoid.Contains(t)) avoid.Add(t);
                    }
                }
            if (avoid.Count > 0) { string a = Pick(r, avoid); reqs.Add(new PlanReq { text = w.Tag(a).avoid, notag = a }); }
            long budget = CeilNice(Math.Max(targetCost + 1000 * tier, targetCost + op.amount * Rand(r, 0.15, 0.35)));
            reqs.Add(new PlanReq { text = "預算 " + GameState.Short(budget), budget = (int)budget });

            List<FieldData> pf = new List<FieldData>();
            for (int i = 0; i < reqs.Count; i++) pf.Add(F("p_" + i, "需求" + (i + 1), reqs[i].text));
            DocumentData pdoc = Doc("plan", "企劃單・" + op.brand, "企劃單", "plan", POffer, HOffer, 40, pf.ToArray());
            pdoc.kind = "plan";

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("brief", "企劃需求", "需求", "request", PCard, HCard, 56,
                    F("b_b", "品牌", op.brand),
                    F("b_k", "活動", op.category + "工商直播"),
                    F("b_m", "預算", GameState.Money(budget)),
                    F("b_n", "備註", "省下來的歸你們")),
                pdoc,
                Handbook("企劃", "需求全部做到", "預算是上限", "越省錢賺越多"),
                OfferDoc("議價單")
            };
            long reward = Nice(budget * 0.35);
            CaseData d = Base(op, "企劃：" + op.brand, "我們想辦一場直播活動，需求都寫在企劃單上了，麻煩幫我們組一下！", 5);
            d.documents = docs.ToArray();
            d.plan = new PlanData { docId = "plan", reward = (int)reward, groups = groups.ToArray(), reqs = reqs.ToArray(), okText = "這企劃好棒！就照這樣辦！", payDiff = true };
            d.offer = new OfferData
            {
                docId = "offer", mode = "raise", label = "企劃費", apply = "planfee",
                ask = (int)reward, start = (int)reward, step = Step(reward),
                min = (int)Nice(reward * 0.6), max = (int)Nice(reward * 2.2),
                accept = (int)Nice(reward * (1.05 + 0.01 * s.RelValue(op.npc))), walk = (int)Nice(reward * 1.4),
                acceptText = "好，企劃費 {v}，企劃要讓我們滿意喔。",
                counterText = "企劃費 {v}，這是我們能給的了。",
                refuseText = "企劃費要這麼多？那我們自己來就好。"
            };
            d.startSlots = new[] { "brief", "plan", null, null };
            d.storage = new[] { "brief", "plan", "hb", "offer" };
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.tasks = new[] { Task("打開企劃單", "open:plan"), Task("談企劃費（可選）", "offer"), Task("組裝企劃", "plan|result") };
            d.replyLeave = "我們再想想好了。";
            d.replyHold = "好，等你們的企劃。";
            d.dealTitle = "企劃通過：" + op.brand;
            return d;
        }

        /// <summary>委派用：找出成本最低、符合全部需求的組合。</summary>
        public static int[] BestPlan(CaseSession cs)
        {
            PlanData p = cs.data.plan;
            if (p == null) return null;
            int n = p.groups.Length;
            int[] cur = new int[n];
            int[] best = null;
            long bestCost = long.MaxValue;
            int combos = 1;
            foreach (PlanGroup g in p.groups) combos *= Math.Max(1, g.options.Length);
            for (int k = 0; k < combos; k++)
            {
                int x = k;
                for (int g = 0; g < n; g++)
                {
                    int m = Math.Max(1, p.groups[g].options.Length);
                    cur[g] = x % m;
                    x /= m;
                }
                for (int g = 0; g < n; g++) cs.planPick[g] = cur[g];
                bool ok = true;
                foreach (PlanReq rq in p.reqs) if (!cs.PlanReqOk(rq)) { ok = false; break; }
                if (!ok) continue;
                long cost = cs.PlanCost();
                if (cost < bestCost) { bestCost = cost; best = (int[])cur.Clone(); }
            }
            for (int g = 0; g < n; g++) cs.planPick[g] = -1;
            return best;
        }

        // ------------------------------------------------------------------
        // 投資
        // ------------------------------------------------------------------

        public static readonly double[] InvestRate = { 0.08, 0.05, 0.02, 0.0 };
        public static readonly double[] InvestReturn = { 1.25, 1.0, 0.7, 0.15 };
        static readonly string[] RiskText = { "風險低", "風險中等", "風險偏高", "風險極高" };

        static CaseData Invest(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            int q = Math.Max(0, Math.Min(3, op.level));
            long ask = op.amount;
            double[] realK = { 0.45, 0.32, 0.18, 0.08 };
            long real = Nice(ask * realK[q] * Rand(r, 0.9, 1.1));
            long claim = q >= 2 ? Nice(real * Rand(r, 2.2, 3.5)) : (q == 1 && r.NextDouble() < 0.5 ? Nice(real * 1.6) : real);
            long debt = q >= 2 ? Nice(ask * Rand(r, 0.3, 0.8)) : 0;
            int months = (int)Math.Max(2, Math.Round(ask / Math.Max(1.0, claim * 0.2)));

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("prop", "投資企劃書", "企劃書", "proposal", PItem, HItem, 54,
                    F("p_i", "投資項目", op.title),
                    F("p_m", "需要資金", GameState.Money(ask)),
                    F("p_s", "我方分潤", "收入 2 成"),
                    F("p_r", "預估月收", GameState.Money(claim)),
                    F("p_d", "財務狀況", "無負債"),
                    F("p_t", "團隊", "經驗豐富"),
                    F("p_b", "預計回本", months + " 個月")),
                Doc("fin", "財務試算", "試算", "chart", PQuote, HQuote, 56,
                    F("f_1", "上月收入", GameState.Money(Nice(real * 0.95))),
                    F("f_2", "上月成本", GameState.Money(Nice(real * 0.5))),
                    F("f_3", "負債", debt > 0 ? GameState.Money(debt) : "無"),
                    F("f_4", "合理月收", GameState.Money(real)))
            };
            bool dd = s.HasUpgrade("research");
            if (dd)
            {
                docs.Add(Doc("dd", "盡職調查報告", "盡調", "report", PBook, HBook, 56,
                    F("d_1", "創辦人", q == 3 ? "曾倒閉兩次" : "經歷單純"),
                    F("d_2", "市場", q <= 1 ? "需求成長中" : "競爭激烈"),
                    F("d_3", "評估", RiskText[q])));
            }
            DocumentData rep = Doc("report", "公司財務報表", "財報", "report", PCard, HCard, 56);
            rep.kind = "finance";
            docs.Add(rep);
            docs.Add(Handbook("投資", "數字要有根據", "負債要揭露", "別用光現金", "可以說不"));
            docs.Add(OfferDoc("投資單"));

            List<ContradictionData> cons = new List<ContradictionData>();
            if (claim != real) cons.Add(C("proj", "p_r", "f_4", "預估太樂觀", "p_r", GameState.Money(real), Nice(ask * 0.15), "那是最理想的狀況啦……照試算的話確實沒那麼多。", null));
            if (debt > 0) cons.Add(C("debt", "p_d", "f_3", "隱瞞負債", "p_d", "負債 " + GameState.Short(debt), Nice(ask * 0.12), "負債……那個很快就會還清的！", null));
            if (dd && q == 3) cons.Add(C("founder", "p_t", "d_1", "團隊經歷不實", "p_t", "曾經倒閉", Nice(ask * 0.1), "以前是以前啦，這次一定成功！", null));

            CaseData d = Base(op, "投資：" + op.title, "我們在找投資！" + op.title + "的企劃書傳過去了，請看看。", 5);
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            d.replies = new[] { new ReplyData { id = "pass", text = "這次先不投", say = "企劃很有心，但這次我們先不投資。", response = "好……謝謝你願意看完。", result = "reject", gives = new string[0] } };
            d.offer = new OfferData
            {
                docId = "offer", mode = "lower", label = "投資金額", apply = "invest", itemName = op.title, refId = op.uid,
                ask = (int)ask, start = (int)ask, step = Step(ask), min = (int)Nice(ask * 0.2), max = (int)Nice(ask * 1.3),
                accept = (int)Nice(ask * 0.92), walk = (int)Nice(ask * 0.7),
                acceptText = "好！{v}，我們一定會讓你回本！", counterText = "{v}……再少就做不起來了。", refuseText = "這樣根本不夠啦……", endsCall = true
            };
            List<string> storage = new List<string> { "prop", "fin" };
            if (dd) storage.Add("dd");
            storage.AddRange(new[] { "report", "hb", "offer" });
            d.storage = storage.ToArray();
            d.startSlots = new[] { "prop", "fin", null, null };
            d.contacts = Contacts(Caller(op), Vboss(s));
            d.tasks = new[] { Task("核對企劃書數字", "tried|result"), Task("決定投不投、投多少", "result") };
            d.replyLeave = "我、我再找別人好了……";
            d.replyHold = "好，等你們考慮。";
            d.dealTitle = "投資成立：" + op.title;
            return d;
        }

        // ------------------------------------------------------------------
        // 糾紛（羅傑出包、撞期、排他、侵權）
        // ------------------------------------------------------------------

        class ClaimDef
        {
            public string key, claim, field;
            public ClaimDef(string k, string c, string f) { key = k; claim = c; field = f; }
        }

        static readonly ClaimDef[] Claims =
        {
            new ClaimDef("bad", "負面發言", "r_s"),
            new ClaimDef("refuse", "拒用產品", "r_p"),
            new ClaimDef("early", "提早下播", "r_l"),
            new ClaimDef("late", "開播遲到", "r_o"),
            new ClaimDef("report", "觀眾檢舉", "r_c")
        };

        public static string IncidentText(string kind)
        {
            switch (kind)
            {
                case "bad": return "在直播上說品牌壞話";
                case "refuse": return "拒絕使用廠商產品";
                case "early": return "直播提早下播";
                case "late": return "開播嚴重遲到";
                case "noshow": return "撞期，直播開天窗";
                case "excl": return "違反排他條款";
                case "gray": return "被版權方追償";
                default: return "出包";
            }
        }

        static CaseData Dispute(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            string kind = op.kind;
            long claim = op.amount;
            long cap = op.amount2;
            int hours = Math.Max(1, op.level);
            string talentName = string.IsNullOrEmpty(op.talent) ? "羅傑" : op.talent;
            int pen = PenFromText(op.text);
            long fee = cap / Math.Max(1, pen);
            bool noshow = kind == "noshow";
            bool gray = kind == "gray";

            // 紀錄：先全部正常，再改出包的那一項
            Dictionary<string, string> rec = new Dictionary<string, string>
            {
                { "r_l", hours + "小時0" + (2 + r.Next(7)) + "分" },
                { "r_p", "有使用" },
                { "r_s", "正常" },
                { "r_o", "準時" },
                { "r_c", "沒有檢舉" }
            };
            string trueClaim, trueField;
            switch (kind)
            {
                case "bad": rec["r_s"] = "「不喜歡欸」"; trueClaim = "負面發言"; trueField = "r_s"; break;
                case "refuse": rec["r_p"] = "拒絕試用"; trueClaim = "拒用產品"; trueField = "r_p"; break;
                case "early": rec["r_l"] = Math.Max(0, hours - 1) + "小時05分"; trueClaim = "提早下播"; trueField = "r_l"; break;
                case "late": rec["r_o"] = "晚了 40 分"; trueClaim = "開播遲到"; trueField = "r_o"; break;
                case "noshow":
                    rec["r_l"] = "未開播"; rec["r_p"] = "沒有進行"; rec["r_s"] = "沒有發言"; rec["r_o"] = "沒有開播";
                    trueClaim = "未依約開播"; trueField = "r_l"; break;
                case "excl": trueClaim = "違反排他"; trueField = "r_j"; break;
                default: trueClaim = "未經授權"; trueField = "r_k"; break;
            }
            List<ClaimDef> falsePool = new List<ClaimDef>();
            foreach (ClaimDef cd in Claims)
            {
                if (cd.key == kind) continue;
                if (noshow && cd.key != "bad" && cd.key != "report") continue;
                falsePool.Add(cd);
            }
            Shuffle(r, falsePool);
            List<string[]> claims = new List<string[]> { new[] { trueClaim, trueField, "true" } };
            for (int i = 0; i < 2 && i < falsePool.Count; i++) claims.Add(new[] { falsePool[i].claim, falsePool[i].field, "false" });
            Shuffle(r, claims);

            string reason = kind == "bad" ? "實況主負評" : kind == "refuse" ? "拒用產品" : kind == "early" ? "提早下播" : kind == "late" ? "嚴重遲到"
                : noshow ? "直播開天窗" : kind == "excl" ? "違反排他" : "侵權轉播";
            List<FieldData> cl = new List<FieldData> { F("cl_a", "求償方", op.company), F("cl_r", "事由", reason) };
            for (int i = 0; i < claims.Count; i++) cl.Add(F("cl_" + (i + 1), "主張" + "一二三"[i], claims[i][0]));
            cl.Add(F("cl_m", "求償金額", GameState.Money(claim)));

            List<FieldData> rf = new List<FieldData>
            {
                F("r_l", "直播時長", rec["r_l"]), F("r_p", "產品環節", rec["r_p"]), F("r_s", "發言", rec["r_s"]),
                F("r_o", "開播時間", rec["r_o"]), F("r_c", "觀眾反應", rec["r_c"])
            };
            if (kind == "excl") rf.Add(F("r_j", "接案紀錄", Calendar.Short(op.day) + " " + (string.IsNullOrEmpty(op.item) ? "競品" : op.item)));
            if (gray) rf.Add(F("r_k", "授權狀態", "沒有授權"));

            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("claim", gray ? "版權求償書" : "求償單", "求償單", "claim", PClaim, HClaim, 56, cl.ToArray()),
                Doc("rec", "直播紀錄", "紀錄", "record", PRec, HRec, 56, rf.ToArray())
            };
            if (!gray)
            {
                List<FieldData> cf = new List<FieldData>
                {
                    F("c_a", "甲方", op.company), F("c_t", "實況主", talentName), F("c_d", "直播日期", Calendar.Long(op.day)),
                    F("c_h", "直播時數", hours + " 小時"), F("c_f", "合作費用", GameState.Money(fee)), F("c_p", "違約賠償", "費用 " + pen + " 倍")
                };
                if (kind == "excl") cf.Add(F("c_x", "排他條款", "30日不接" + op.category));
                docs.Add(Doc("c", "合作合約（已簽）", "合約", "contract", PContract, HContract, 56, cf.ToArray()));
            }
            docs.Add(Handbook("糾紛", "先道歉再談", "不否認事實", "逐條核對主張", "賠償看合約"));
            DocumentData log = Doc("log", "通話錄音", "錄音筆", "recorder", PRec, HRec, 56);
            log.kind = "log";
            docs.Add(log);
            docs.Add(OfferDoc("和解單"));

            List<ContradictionData> cons = new List<ContradictionData>();
            int fi = 0;
            for (int i = 0; i < claims.Count; i++)
            {
                string fid = "cl_" + (i + 1);
                if (claims[i][2] == "true")
                {
                    ContradictionData bad = C("deny", fid, claims[i][1], "否認事實", null, null, -Nice(claim * 0.06), "你現在是要否認？大家都看到了好嗎！", null);
                    bad.bad = true;
                    cons.Add(bad);
                }
                else
                {
                    cons.Add(C("f" + fi++, fid, claims[i][1], "主張不成立", fid, "（已撤回）", Nice(claim * 0.12), "……好，這條我收回。", null));
                }
            }
            if (!gray && claim > cap) cons.Add(C("cap", "cl_m", "c_p", "超過賠償上限", "cl_m", GameState.Money(cap), Nice((claim - cap) * 0.8), "合約寫的上限……好，最多就照合約。", null));

            List<ReplyData> reps = new List<ReplyData>
            {
                new ReplyData { id = "sorry", text = "先道歉", say = "這件事是我們的錯，先跟您道歉。", response = "……至少你還有誠意。好，我聽你說。", patience = 2, shift = (int)Nice(claim * 0.03), gives = new string[0] },
            };
            if (!gray) reps.Add(new ReplyData { id = "redo", text = "免費補一場", say = "我們讓" + talentName + "免費補一場直播。", response = "補一場？……如果他願意好好做，可以談。", requires = "tried", shift = (int)Nice(claim * 0.1), gives = new string[0] });
            if (s.HasUpgrade("legal")) reps.Add(new ReplyData { id = "legal", text = "請法務出面", say = "這件事我請公司法務跟您談。", response = "法務……好吧，我們理性一點。", shift = (int)Nice(claim * 0.15), gives = new string[0] });

            string greet;
            switch (kind)
            {
                case "bad": greet = talentName + "在直播上說我們的壞話！求償單傳過去了，給個交代！"; break;
                case "refuse": greet = talentName + "直播時拒絕用我們的產品！求償單傳過去了！"; break;
                case "early": greet = talentName + "直播到一半就下播了！求償單傳過去了！"; break;
                case "late": greet = talentName + "開播遲到快一小時！求償單傳過去了！"; break;
                case "noshow": greet = "說好的直播呢？" + talentName + "根本沒開台！求償單傳過去了！"; break;
                case "excl": greet = talentName + "在排他期間接了競品！我們要求賠償！"; break;
                default: greet = "你們未經授權轉播我們的內容！求償書傳過去了，請給個交代。"; break;
            }

            CaseData d = Base(op, "糾紛：" + op.company, greet, 6);
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            d.replies = reps.ToArray();
            d.offer = new OfferData
            {
                docId = "offer", mode = "lower", label = "和解金", apply = "settle", refId = op.uid,
                ask = (int)claim, start = (int)claim, step = Step(claim), min = 0, max = (int)claim,
                accept = (int)Nice(claim * 0.85), walk = (int)Nice(claim * 0.6),
                acceptText = "好，{v}，這件事到此為止。", counterText = "{v}，不能再少了。", refuseText = "你在開玩笑嗎？那我們法院見！", endsCall = true
            };
            List<string> storage = new List<string> { "claim", "rec" };
            if (!gray) storage.Add("c");
            storage.AddRange(new[] { "hb", "log", "offer" });
            d.storage = storage.ToArray();
            d.startSlots = gray ? new[] { "claim", "rec", null, null } : new[] { "claim", "c", null, null };
            d.contacts = Contacts(Caller(op), new ContactData { id = TalentPortrait(s, op.talent), name = talentName, message = "我、我真的不是故意的啦……" }, Vboss(s));
            d.tasks = new[] { Task("先穩住對方", "reply:sorry|result"), Task("逐條核對求償", "found>=2|result"), Task("談和解金", "offer|result") };
            d.replyWrong = "你到底在找什麼？我很忙！";
            d.replyLeave = "不用談了，我們直接寄存證信函。";
            d.replyHold = "給你們時間考慮，但別拖太久。";
            d.dealTitle = "和解成立：" + op.company;
            return d;
        }

        static string TalentPortrait(GameState s, string name)
        {
            foreach (TalentState t in s.talents) if (t.name == name) return t.portrait;
            return "roger";
        }

        public static int PenFromText(string t)
        {
            if (string.IsNullOrEmpty(t)) return 1;
            foreach (char ch in t) if (ch >= '1' && ch <= '9') return ch - '0';
            return 1;
        }

        static CaseData Audit(Career c, Opportunity op, Random r)
        {
            long gray = op.amount2;
            long total = op.amount;
            long a = gray > 0 ? Nice(total * 0.5) : 0;
            long b = Nice((total - a) * 0.55);
            long cc = Math.Max(0, total - a - b);
            List<DocumentData> docs = new List<DocumentData>
            {
                Doc("notice", "補稅通知", "通知", "claim", PClaim, HClaim, 56,
                    F("n_1", "漏報收入", GameState.Money(a)),
                    F("n_2", "收據不全", GameState.Money(b)),
                    F("n_3", "虛報成本", GameState.Money(cc)),
                    F("n_m", "補稅加罰", GameState.Money(total))),
                Doc("ledger", "公司帳本", "帳本", "report", PBook, HBook, 56,
                    F("l_1", "灰色收入", gray > 0 ? GameState.Money(gray) : "無"),
                    F("l_2", "收據", "全部齊全"),
                    F("l_3", "成本發票", "都有開立")),
                Handbook("稅務", "帳要一筆一筆對", "有憑證就爭取", "不要否認事實"),
                OfferDoc("和解單")
            };
            List<ContradictionData> cons = new List<ContradictionData>
            {
                C("receipt", "n_2", "l_2", "收據其實齊全", "n_2", "（已撤銷）", Nice(b * 0.9), "收據齊全……好，這項撤銷。", null),
                C("cost", "n_3", "l_3", "成本有發票", "n_3", "（已撤銷）", Nice(cc * 0.9), "有發票的話，這項不算。", null)
            };
            if (gray > 0)
            {
                ContradictionData bad = C("deny", "n_1", "l_1", "否認事實", null, null, -Nice(total * 0.05), "帳本上明明就有，你還想否認？", null);
                bad.bad = true;
                cons.Add(bad);
            }
            CaseData d = Base(op, "稅務查核", "國稅局查核。你們公司的帳有問題，補稅通知傳過去了。", 6);
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            d.replies = new[] { new ReplyData { id = "sorry", text = "配合調查", say = "我們會全力配合調查。", response = "很好，那我們一項一項來。", patience = 2, shift = (int)Nice(total * 0.03), gives = new string[0] } };
            d.offer = new OfferData
            {
                docId = "offer", mode = "lower", label = "補繳金額", apply = "settle", refId = op.uid,
                ask = (int)total, start = (int)total, step = Step(total), min = 0, max = (int)total,
                accept = (int)Nice(total * 0.92), walk = (int)Nice(total * 0.6),
                acceptText = "{v}，補繳後結案。", counterText = "最少要補 {v}。", refuseText = "這不是菜市場，請認真看待。", endsCall = true
            };
            d.storage = new[] { "notice", "ledger", "hb", "offer" };
            d.startSlots = new[] { "notice", "ledger", null, null };
            d.contacts = Contacts(Caller(op), Vboss(c.s));
            d.tasks = new[] { Task("核對補稅項目", "found>=1|result"), Task("談補繳金額", "offer|result") };
            d.replyWrong = "請針對帳目說明。";
            d.replyLeave = "既然不配合，就照原本的金額開罰。";
            d.replyHold = "限期內要處理。";
            d.dealTitle = "稅務查核結案";
            return d;
        }

        // ------------------------------------------------------------------
        // 簽新人
        // ------------------------------------------------------------------

        static CaseData SignTalent(Career c, Opportunity op, Random r)
        {
            GameState s = c.s;
            TalentDef t = null;
            foreach (TalentDef x in c.w.talents) if (x.id == op.talent) t = x;
            if (t == null || s.Talent(t.id) != null) return null;
            long bonus = op.amount;
            List<string> errs = new List<string> { "share", "bonus", "name" };
            Shuffle(r, errs);
            errs = errs.GetRange(0, 1 + (r.NextDouble() < 0.5 ? 1 : 0));
            long wrongBonus = Nice(bonus * Rand(r, 1.3, 1.6));

            List<DocumentData> docs = new List<DocumentData>();
            DocumentData cdoc = Doc("c", "經紀合約", "合約", "contract", PContract, HContract, 56,
                F("c_t", "藝人", errs.Contains("name") ? Misspell(t.name, 0) : t.name),
                F("c_p", "合約期間", "2 年"),
                F("c_s", "公司分潤", errs.Contains("share") ? "收入 2 成" : "收入 3 成"),
                F("c_b", "簽約金", GameState.Money(errs.Contains("bonus") ? wrongBonus : bonus)),
                F("c_x", "違約金", "簽約金 3 倍"));
            cdoc.stamp = true;
            docs.Add(cdoc);
            docs.Add(Doc("letter", "合作意向書", "意向書", "quote", PQuote, HQuote, 56,
                F("l_t", "藝人", t.name), F("l_p", "合約期間", "2 年"), F("l_s", "公司分潤", "收入 3 成"), F("l_b", "簽約金", GameState.Money(bonus))));
            docs.Add(TalentDoc("prof", "藝人檔案・" + t.name, "藝人檔", t.portrait,
                new[] { F("p_n", "藝名", t.name), F("p_t", "類型", t.genre), F("p_f", "粉絲", FansText(t.fans)), F("p_p", "討厭", Dislikes(t.dislikes)) },
                new[] { F("p_1", "風格", t.style), F("p_2", "興趣", t.interests),
                    string.IsNullOrEmpty(t.affiliation) ? F("p_3", "身分", "實況創作者") : F("p_3", "團隊", t.affiliation) }, false, "個人資料"));
            docs.Add(Handbook("經紀", "分潤照意向書", "簽約金照意向書", "名字要寫對", "期間要一致"));
            docs.Add(OfferDoc("議價單"));

            List<ContradictionData> cons = new List<ContradictionData>();
            long shift = Nice(bonus * 0.05);
            if (errs.Contains("share")) cons.Add(C("share", "c_s", "l_s", "分潤寫錯", "c_s", "收入 3 成", shift, "啊，是 3 成沒錯，我改。", "公司分潤少一成，以後他的工商收入打折"));
            if (errs.Contains("bonus"))
            {
                ContradictionData x = C("bonus", "c_b", "l_b", "簽約金不符", "c_b", GameState.Money(bonus), shift, "簽約金……是我寫錯了，照意向書。", "簽約金多付 " + GameState.Money(wrongBonus - bonus));
                x.fee = (int)bonus;
                cons.Add(x);
            }
            if (errs.Contains("name")) cons.Add(C("name", "c_t", "p_n", "名字寫錯", "c_t", t.name, shift, "哈哈，我的名字被打錯了！", "藝名寫錯，合約可能無效"));

            CaseData d = Base(op, "簽約：" + t.name, "你好，我是" + t.name + "！想加入水蛇洞，合約已經擬好了。", 5);
            d.client = new ClientData { id = t.portrait, name = t.name, company = t.genre, role = "新人" };
            d.documents = docs.ToArray();
            d.contradictions = cons.ToArray();
            d.baseFee = (int)(errs.Contains("bonus") ? wrongBonus : bonus);
            d.deferFee = true;
            d.payNote = "簽約金今天付清";
            d.stampOk = "簽下藝人";
            d.offer = new OfferData
            {
                docId = "offer", mode = "lower", label = "簽約金", apply = "fee", feeField = "c_b",
                ask = (int)bonus, start = (int)bonus, step = Step(bonus),
                min = (int)Nice(bonus * 0.4), max = (int)Nice(bonus * 1.3),
                accept = (int)Nice(bonus * 0.95), walk = (int)Nice(bonus * 0.7),
                acceptText = "好吧，簽約金 {v}，我接受！",
                counterText = "簽約金至少要 {v}……",
                refuseText = "這也砍太多了吧！"
            };
            d.startSlots = new[] { "c", "letter", null, null };
            d.storage = new[] { "c", "letter", "prof", "hb", "offer" };
            d.contacts = Contacts(new ContactData { id = t.portrait, name = t.name, caller = true }, Vboss(s));
            d.tasks = new[] { Task("核對經紀合約", "tried|result"), Task("談簽約金（可選）", "offer"), Task("決定簽不簽", "result") };
            d.replySigned = "太好了！以後請多指教！";
            d.replyReject = "好吧……那我再找找別家。";
            d.replyHold = "好，我等你們消息！";
            return d;
        }
    }
}
