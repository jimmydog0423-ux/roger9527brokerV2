using System;
using System.Collections.Generic;

namespace SSD
{
    [Serializable]
    public class InvItem
    {
        public string id;
        public string name;
        public string condition;    // 玩家知道的狀況
        public long cost;
        public string category;     // 商品種類（對應 world.json 的 items.cat）
        public int grade;           // 真正的狀況：0 全新 1 近全新 2 有刮傷 3 修過 4 故障
        public long baseValue;      // 全新時的行情
        public string flags;        // stolen 等
        public int rentDays;        // 出租剩餘天數
        public long rentPerDay;
        public int boughtDay;
    }

    [Serializable]
    public class Investment
    {
        public string name;
        public long amount;
        public string refId;
        public int quality;         // 0 最好 ~ 3 最差（玩家看不到）
        public int periods = 6;     // 每 3 天配息一次，共幾次
        public int paid;
        public int startDay;
        public string status = "active";   // active / done / failed / exited
        public long received;
    }

    [Serializable]
    public class TalentState
    {
        public string id;
        public string name;
        public string portrait;
        public string genre;
        public int fans;
        public int mood = 70;       // 0~100
        public int trust = 50;      // 0~100
        public string[] likes = new string[0];
        public string[] dislikes = new string[0];
        public string style;
        public string interests;
        public int raiseDay = -99;
        public int jobs;
        public int incidents;
        public int cut = 100;       // 公司實際拿到的比例（%）
    }

    [Serializable]
    public class Booking
    {
        public string uid;
        public string talent;
        public int day;
        public string brand;
        public string company;
        public string category;
        public string npc;
        public string npcName;
        public long fee;
        public int penaltyX = 1;
        public int hours = 2;
        public string flags = "";   // 沒抓到的問題：date,name,corp,hours,excl,gray,pay
        public string exclBrand;
        public bool done;
        public int fitLevel;        // 對方沒指定藝人時派的人合不合適：0 沒有評分，1~4 = 不合適～最合適
    }

    [Serializable]
    public class Busy
    {
        public string talent;
        public int day;
        public string text;
    }

    [Serializable]
    public class Opportunity
    {
        public string uid;
        public string type;         // sponsor / buy / sell / broker / plan / invest / dispute / gray / talent / audit
        public int seed;
        public string npc;
        public string npcName;
        public string company;
        public string role;
        public string title;
        public string summary;
        public long est;
        public int expires;         // 最後一天（含）
        public int showDay;         // 這天才會出現在信箱
        public bool urgent;
        public bool held;           // 暫緩過
        public bool lead;           // 晚上跑人脈得來的委託
        // 參數
        public string talent;
        public string brand;
        public string category;
        public string item;
        public int day;
        public long amount;
        public long amount2;
        public int level;
        public string kind;
        public string refUid;
        public string text;
    }

    [Serializable]
    public class Relation
    {
        public string npc;
        public string name;
        public int value;
    }

    [Serializable]
    public class Trend
    {
        public string cat;
        public int pct = 100;
        public int next = 100;      // 明天的行情（市場情報看得到）
    }

    /// <summary>跳蚤市場的一筆交易：老闆心裡的底線、被殺價幾次。</summary>
    [Serializable]
    public class FleaDeal
    {
        public string id;           // 對應 fleaStock 的商品
        public long floor;          // 老闆一定肯賣的價錢
        public long walk;           // 低於這個價錢老闆會翻臉
        public int misses;          // 殺價失敗幾次
        public bool looked;         // 仔細檢查過了
        public bool refused;        // 老闆不賣了
    }

    [Serializable]
    public class Stat
    {
        public string key;
        public long value;
    }

    /// <summary>
    /// 公司整體狀態（企劃書 10.4 財務規則）。整個物件會存進存檔。
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int day = 1;
        public long cash = 100000;
        public long receivable;
        public long debt;
        public long revenueToday;
        public long costToday;
        public long revenueTotal;
        public long costTotal;
        public long lastProfit;
        public long lastSettle;
        public int lesson;
        public bool tutorialDone;
        public List<InvItem> inventory = new List<InvItem>();
        public List<Investment> investments = new List<Investment>();
        public List<string> flags = new List<string>();

        // ---------- 正式營業 ----------
        public bool career;
        public bool ended;
        public int act = 1;
        public int seed = 12345;
        public int uidSeq;
        public int slots = 3;
        public int delegates;
        public int reputation = 50;
        public List<string> upgrades = new List<string>();
        public List<TalentState> talents = new List<TalentState>();
        public List<Booking> bookings = new List<Booking>();
        public List<Busy> busy = new List<Busy>();
        public List<Opportunity> inbox = new List<Opportunity>();
        public List<Relation> relations = new List<Relation>();
        public List<Trend> market = new List<Trend>();
        public List<Stat> stats = new List<Stat>();
        public List<string> report = new List<string>();
        public List<string> news = new List<string>();
        public List<long> history = new List<long>();
        public List<Opportunity> events = new List<Opportunity>();   // 之後才會發生的事件
        public List<string> pendingBeats = new List<string>();       // 等著播的劇情
        public int startedDay;

        // ---------- 晚上 ----------
        public int nightDay;                                      // 這個晚上是哪一天的前一晚
        public int nightLeft;                                     // 剩下幾段時間
        public int fatigue;                                       // 疲勞，越高越容易睡過頭
        public List<string> nightVisited = new List<string>();
        public List<InvItem> shopStock = new List<InvItem>();
        public List<InvItem> fleaStock = new List<InvItem>();
        public int oversleep;                                     // 0 沒事 / 1 睡過頭 / 2 睡到中午
        public int restedDay;                                     // 這天精神飽滿（客人耐心 +1）
        public int luckDay;                                       // 這天拜過拜（出包機率減半）
        public List<string> morning = new List<string>();         // 早上要說的話
        public List<FleaDeal> fleaDeals = new List<FleaDeal>();
        public List<int> junkDug = new List<int>();               // 垃圾場這一晚挖過的堆
        public int junkDigs;                                      // 垃圾場還能挖幾次

        static readonly string[] Weekdays = { "週一", "週二", "週三", "週四", "週五", "週六", "週日" };

        public string Weekday
        {
            get { return Weekdays[(Math.Max(1, day) - 1) % 7]; }
        }

        public bool Has(string flag)
        {
            return flags.Contains(flag);
        }

        public void Set(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        public bool HasUpgrade(string id)
        {
            return upgrades.Contains(id);
        }

        public string NewUid(string prefix)
        {
            uidSeq++;
            return prefix + uidSeq;
        }

        public long GetStat(string key)
        {
            foreach (Stat s in stats) if (s.key == key) return s.value;
            return 0;
        }

        public void AddStat(string key, long n)
        {
            foreach (Stat s in stats)
            {
                if (s.key != key) continue;
                s.value += n;
                return;
            }
            stats.Add(new Stat { key = key, value = n });
        }

        public TalentState Talent(string id)
        {
            foreach (TalentState t in talents) if (t.id == id) return t;
            return null;
        }

        public Relation Rel(string npc)
        {
            foreach (Relation r in relations) if (r.npc == npc) return r;
            return null;
        }

        public void AddRel(string npc, string name, int n)
        {
            if (string.IsNullOrEmpty(npc)) return;
            Relation r = Rel(npc);
            if (r == null)
            {
                r = new Relation { npc = npc, name = name };
                relations.Add(r);
            }
            r.value = Math.Max(-5, Math.Min(5, r.value + n));
        }

        public int RelValue(string npc)
        {
            Relation r = Rel(npc);
            return r == null ? 0 : r.value;
        }

        public void AddRep(int n)
        {
            reputation = Math.Max(0, Math.Min(100, reputation + n));
        }

        public Trend TrendOf(string cat)
        {
            foreach (Trend t in market) if (t.cat == cat) return t;
            Trend nt = new Trend { cat = cat };
            market.Add(nt);
            return nt;
        }

        /// <summary>收到現金（買賣成交）。</summary>
        public void Earn(long n)
        {
            cash += n;
            revenueToday += n;
            revenueTotal += n;
        }

        /// <summary>簽約或佣金：先記成應收款，下班結算時入帳。</summary>
        public void Accrue(long n)
        {
            receivable += n;
            revenueToday += n;
            revenueTotal += n;
        }

        /// <summary>花錢（收購、檢測、賠償、投資）。</summary>
        public void Spend(long n)
        {
            cash -= n;
            costToday += n;
            costTotal += n;
        }

        public long InvestmentTotal
        {
            get
            {
                long t = 0;
                foreach (Investment i in investments) if (i.status == "active") t += i.amount;
                return t;
            }
        }

        public long InventoryValue
        {
            get
            {
                long t = 0;
                foreach (InvItem i in inventory) t += i.cost;
                return t;
            }
        }

        /// <summary>已簽約、直播完才會入帳的工商費用。</summary>
        public long PendingFees
        {
            get
            {
                long t = 0;
                foreach (Booking b in bookings) if (!b.done) t += b.fee;
                return t;
            }
        }

        public static string Money(long n)
        {
            string s = Math.Abs(n).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            return (n < 0 ? "-NT$" : "NT$") + s;
        }

        /// <summary>短一點的金額：NT$12.5萬、NT$1,234萬。</summary>
        public static string Short(long n)
        {
            long a = Math.Abs(n);
            string sign = n < 0 ? "-" : "";
            if (a >= 100000000) return sign + "NT$" + (a / 1000000 / 100.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "億";
            if (a >= 100000) return sign + "NT$" + (a / 10000.0).ToString(a >= 10000000 ? "0" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + "萬";
            return sign + "NT$" + a.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>很短的金額，給按鈕用：3萬、1.5萬、8千、500。</summary>
        public static string Compact(long n)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            long a = Math.Abs(n);
            string sign = n < 0 ? "-" : "";
            if (a >= 100000000) return sign + (a / 100000000.0).ToString(a >= 1000000000 ? "0" : "0.#", inv) + "億";
            if (a >= 10000) return sign + (a / 10000.0).ToString(a >= 100000 ? "0" : "0.#", inv) + "萬";
            if (a >= 1000) return sign + (a / 1000.0).ToString("0.#", inv) + "千";
            return sign + a.ToString(inv);
        }

        public GameState Clone()
        {
            return UnityEngine.JsonUtility.FromJson<GameState>(UnityEngine.JsonUtility.ToJson(this));
        }
    }

    /// <summary>遊戲裡的日期：第 1 天是 8 月 1 日（週一）。</summary>
    public static class Calendar
    {
        static readonly int[] MonthDays = { 31, 30, 31, 30, 31, 31, 28, 31, 30, 31, 30, 31 };
        static readonly int[] MonthNo = { 8, 9, 10, 11, 12, 1, 2, 3, 4, 5, 6, 7 };

        public static void Date(int day, out int month, out int dom)
        {
            int d = Math.Max(1, day) - 1;
            int i = 0;
            while (d >= MonthDays[i % 12])
            {
                d -= MonthDays[i % 12];
                i++;
            }
            month = MonthNo[i % 12];
            dom = d + 1;
        }

        /// <summary>8/14</summary>
        public static string Short(int day)
        {
            int m, d;
            Date(day, out m, out d);
            return m + "/" + d;
        }

        /// <summary>8月14日</summary>
        public static string Long(int day)
        {
            int m, d;
            Date(day, out m, out d);
            return m + "月" + d + "日";
        }

        public static string Weekday(int day)
        {
            string[] w = { "一", "二", "三", "四", "五", "六", "日" };
            return "週" + w[(Math.Max(1, day) - 1) % 7];
        }
    }
}
