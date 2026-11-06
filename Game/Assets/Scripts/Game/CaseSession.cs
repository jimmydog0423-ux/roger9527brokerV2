using System;
using System.Collections.Generic;

namespace SSD
{
    public class LogLine
    {
        public string who;
        public string text;

        public LogLine(string who, string text)
        {
            this.who = who;
            this.text = text;
        }
    }

    public class Toast
    {
        public string who;
        public string text;
        public string kind;   // info / bad / warn / msg / good
    }

    /// <summary>
    /// 一通電話（一個案件）進行中的所有狀態與規則。不碰畫面。
    /// 狀態改變時先觸發 Changed（畫面重畫），再依序送出事件給教學導演。
    /// </summary>
    public class CaseSession
    {
        public const int SlotCount = 4;

        public readonly CaseData data;
        public readonly GameState state;
        public readonly string[] slots = new string[SlotCount];
        public readonly List<string> storage = new List<string>();
        public readonly List<string> sel = new List<string>();
        public readonly HashSet<string> fixedKeys = new HashSet<string>();
        public readonly List<string> found = new List<string>();
        public readonly HashSet<string> readContacts = new HashSet<string>();
        public readonly HashSet<string> usedReplies = new HashSet<string>();
        public readonly List<LogLine> log = new List<LogLine>();

        public bool tried;
        public int wrong;
        public int patience;
        public string result;          // null / signed / hold / reject / leave / deal
        public bool overlayClosed;
        public bool dayEnded;
        public Toast toast;
        public string bubble;
        public bool mic = true;
        public bool cam = true;
        public long feeOverride;
        public long inspectCost;

        // 議價
        public long offerValue;
        public long offerAsk;
        public int offerShift;
        public bool offerDone;
        public long offerFinal;
        public long offerHarden;     // 每次沒談成，對方的底線就收緊一點
        public int offerMisses;
        public Random rng;           // 議價擲骰；測試時可以換掉

        // 工商派人
        public string assigned;          // 派去的藝人 id（null = 照原本指定的，或還沒選）
        public int assignFit = -1;       // 對方沒指定時，派的人合不合適
        public long swapFee;             // 換人後的費用
        public bool rosterOpen;
        readonly Dictionary<string, string> fieldOverride = new Dictionary<string, string>();

        // 仲介與企劃
        public bool recommendDone;
        public long recommendReward;
        public int[] planPick = new int[0];
        public bool planOpen;
        public bool planDone;
        public long planReward;

        public event Action Changed;
        public event Action<string> OnEvent;

        readonly Dictionary<string, DocumentData> docs = new Dictionary<string, DocumentData>();
        readonly Dictionary<string, string> fieldDoc = new Dictionary<string, string>();
        readonly Dictionary<string, string> fieldLabel = new Dictionary<string, string>();
        readonly Dictionary<string, FieldData> fieldData = new Dictionary<string, FieldData>();
        readonly List<string> pending = new List<string>();

        public CaseSession(CaseData data, GameState state)
        {
            this.data = data;
            this.state = state;
            patience = data.patience;
            foreach (DocumentData d in data.documents) AddDoc(d);
            foreach (string id in data.storage) storage.Add(id);
            if (data.useInventory)
            {
                int at = 0;
                foreach (InvItem item in state.inventory)
                {
                    DocumentData d = InventoryDoc(item);
                    AddDoc(d);
                    if (!storage.Contains(d.id)) storage.Insert(at++, d.id);
                }
            }
            for (int i = 0; i < SlotCount; i++)
            {
                string id = i < data.startSlots.Length ? data.startSlots[i] : null;
                slots[i] = string.IsNullOrEmpty(id) || !docs.ContainsKey(id) ? null : id;
            }
            if (data.offer != null)
            {
                offerValue = data.offer.start;
                offerAsk = data.offer.ask;
            }
            int seed = state.day * 7919 + state.uidSeq * 31;
            foreach (char ch in data.id ?? "") seed = seed * 31 + ch;
            rng = new Random(seed);
            if (data.plan != null)
            {
                planPick = new int[data.plan.groups.Length];
                for (int i = 0; i < planPick.Length; i++) planPick[i] = -1;
            }
            bubble = data.greeting;
            log.Add(new LogLine(data.client.name, data.greeting));
        }

        static DocumentData InventoryDoc(InvItem item)
        {
            DocumentData d = new DocumentData
            {
                id = item.id,
                title = "庫存・" + item.name,
                tray = "庫存",
                kind = "form",
                icon = "box",
                paper = "#d9e6d8",
                head = "#b6cdb3",
                labelWidth = 32,
                fields = new[]
                {
                    new FieldData { id = item.id + "_n", label = "品名", value = item.name },
                    new FieldData { id = item.id + "_s", label = "狀況", value = item.condition },
                    new FieldData { id = item.id + "_c", label = "成本", value = GameState.Money(item.cost) }
                }
            };
            CaseData.NormalizeDoc(d);
            return d;
        }

        void AddDoc(DocumentData d)
        {
            docs[d.id] = d;
            foreach (FieldData f in d.info) { fieldDoc[f.id] = d.id; fieldLabel[f.id] = f.label; fieldData[f.id] = f; }
            foreach (FieldData f in d.fields) { fieldDoc[f.id] = d.id; fieldLabel[f.id] = f.label; fieldData[f.id] = f; }
        }

        void Raise(string ev)
        {
            pending.Add(ev);
        }

        void Flush()
        {
            if (Changed != null) Changed();
            if (pending.Count == 0) return;
            string[] evs = pending.ToArray();
            pending.Clear();
            foreach (string ev in evs)
            {
                Action<string> h = OnEvent;
                if (h != null) h(ev);
            }
        }

        // ---------- 查詢 ----------

        public DocumentData Doc(string id)
        {
            DocumentData d;
            return id != null && docs.TryGetValue(id, out d) ? d : null;
        }

        public bool CanAct
        {
            get { return result == null && !dayEnded; }
        }

        public int TotalGood
        {
            get
            {
                int n = 0;
                foreach (ContradictionData c in data.contradictions) if (!c.bad) n++;
                return n;
            }
        }

        public int FoundGood
        {
            get
            {
                int n = 0;
                foreach (ContradictionData c in data.contradictions)
                    if (!c.bad && fixedKeys.Contains(c.key)) n++;
                return n;
            }
        }

        /// <summary>還沒找到的問題名稱，例如「日期撞期、賠償過高」。</summary>
        public string MissedNames()
        {
            List<string> n = new List<string>();
            foreach (ContradictionData c in data.contradictions)
                if (!c.bad && !fixedKeys.Contains(c.key)) n.Add(string.IsNullOrEmpty(c.name) ? c.key : c.name);
            return string.Join("、", n.ToArray());
        }

        public bool CanSign
        {
            get { return (!data.requireAll || FoundGood >= TotalGood) && !NeedsAssign; }
        }

        /// <summary>對方沒指定藝人，而且還沒選人。</summary>
        public bool NeedsAssign
        {
            get { return data.assign != null && data.assign.open && assigned == null; }
        }

        /// <summary>現在要去直播的藝人名字。</summary>
        public string AssignedName
        {
            get
            {
                AssignData a = data.assign;
                if (a == null) return null;
                RosterEntry e = RosterOf(assigned);
                if (e != null) return e.name;
                return a.open ? null : a.wantedName;
            }
        }

        public RosterEntry RosterOf(string id)
        {
            if (data.assign == null || string.IsNullOrEmpty(id)) return null;
            foreach (RosterEntry e in data.assign.roster) if (e.id == id) return e;
            return null;
        }

        public void RosterOpen()
        {
            if (data.assign == null || !CanAct) return;
            rosterOpen = true;
            Raise("roster:open");
            Flush();
        }

        public void RosterClose()
        {
            rosterOpen = false;
            Flush();
        }

        /// <summary>
        /// 派人或提議換人。對方沒指定：直接派，合不合適會影響直播後加碼或被解約。
        /// 對方有指定：換成粉絲比較少的人，費用會變少；討厭這類產品或粉絲差太多，對方不接受。
        /// </summary>
        public void Assign(string talentId)
        {
            AssignData a = data.assign;
            RosterEntry e = RosterOf(talentId);
            if (a == null || e == null || !CanAct) return;
            if (offerDone)
            {
                toast = new Toast { who = "提示", text = "價錢已經談好了，不能再換人", kind = "warn" };
                Flush();
                return;
            }
            if (talentId == assigned || (!a.open && talentId == a.wanted && assigned == null))
            {
                toast = new Toast { who = "提示", text = "現在就是派" + e.name, kind = "info" };
                Flush();
                return;
            }
            log.Add(new LogLine("你", a.open ? "這次派" + e.name + "去。" : "可以換成" + e.name + "嗎？"));
            if (!a.open && talentId != a.wanted && e.swapFee <= 0)
            {
                // 對方不接受換人
                patience = Math.Max(0, patience - 1);
                bubble = e.reply;
                log.Add(new LogLine(data.client.name, e.reply));
                toast = new Toast { who = "提示", text = "對方不接受換人，耐心下降", kind = "bad" };
                rosterOpen = false;
                Raise("assign:refused");
                if (patience == 0) EndAs("leave", data.replyLeave);
                Flush();
                return;
            }

            bool back = !a.open && talentId == a.wanted;
            assigned = back ? null : talentId;
            assignFit = a.open ? e.fit : -1;
            if (back)
            {
                fieldOverride.Remove("c_t");
                fieldOverride.Remove("q_t");
            }
            else
            {
                fieldOverride["c_t"] = e.name;
                fieldOverride["q_t"] = e.name;
            }
            if (!a.open)
            {
                // 換人：照新的費用重算合約、報價單和議價單
                long newFee = back ? a.quote : e.swapFee;
                double k = a.quote > 0 ? newFee / (double)a.quote : 1.0;
                swapFee = back ? 0 : newFee;
                ScaleOffer(k);
                if (back)
                {
                    fieldOverride.Remove("c_f");
                    fieldOverride.Remove("q_f");
                }
                else
                {
                    fieldOverride["c_f"] = GameState.Money(newFee);
                    fieldOverride["q_f"] = GameState.Money(newFee);
                }
                DropTalentIssues();
            }
            bubble = e.reply;
            log.Add(new LogLine(data.client.name, e.reply));
            toast = new Toast { who = a.open ? "已派人" : "換人", text = a.open ? e.name + "：" + e.fitText + "，" + e.outcome : e.name + "，費用 " + GameState.Money(Fee), kind = a.open && e.fit <= 1 ? "warn" : "good" };
            rosterOpen = false;
            Raise("assigned");
            Flush();
        }

        double offerScale = 1.0;

        /// <summary>換人後議價單的金額一起照比例調整。</summary>
        void ScaleOffer(double k)
        {
            OfferData o = data.offer;
            if (o == null || offerDone) return;
            double r = k / offerScale;
            offerScale = k;
            o.ask = (int)CaseGen.Nice(o.ask * r);
            o.start = (int)CaseGen.Nice(o.start * r);
            o.min = (int)CaseGen.Nice(o.min * r);
            o.max = (int)CaseGen.Nice(o.max * r);
            o.accept = (int)CaseGen.Nice(o.accept * r);
            o.walk = (int)CaseGen.Nice(o.walk * r);
            offerAsk = CaseGen.Nice(offerAsk * r);
            offerValue = CaseGen.Nice(offerValue * r);
        }

        /// <summary>換人之後，原本那位藝人的名字、檔期、排他問題就不算了（還沒抓到的拿掉）。</summary>
        void DropTalentIssues()
        {
            List<ContradictionData> keep = new List<ContradictionData>();
            foreach (ContradictionData c in data.contradictions)
            {
                bool talentIssue = c.key == "name" || c.key == "date" || c.key == "excl" || c.key == "fee";
                if (talentIssue && !fixedKeys.Contains(c.key)) continue;
                keep.Add(c);
            }
            data.contradictions = keep.ToArray();
            sel.Clear();
        }

        public long Fee
        {
            get
            {
                if (feeOverride > 0) return feeOverride;
                if (swapFee > 0) return swapFee;
                long fee = data.baseFee;
                foreach (ContradictionData c in data.contradictions)
                    if (c.fee > 0 && fixedKeys.Contains(c.key)) fee = c.fee;
                return fee;
            }
        }

        public string DocOf(string fieldId)
        {
            string d;
            return fieldId != null && fieldDoc.TryGetValue(fieldId, out d) ? d : null;
        }

        /// <summary>每種問題對應的手冊規則：拿寫錯的欄位跟這條規則比，也算抓到。</summary>
        static readonly Dictionary<string, string[]> RuleOf = new Dictionary<string, string[]>
        {
            { "fee", new[] { "金額同報價單" } },
            { "name", new[] { "藝名要寫對", "名字要寫對" } },
            { "date", new[] { "日期不可撞期" } },
            { "pen", new[] { "賠償上限1倍" } },
            { "hours", new[] { "時數同報價單" } },
            { "excl", new[] { "排他不含既有約" } },
            { "exc", new[] { "排他不含既有約" } },
            { "corp", new[] { "公司名對名片" } },
            { "share", new[] { "分潤照意向書" } },
            { "bonus", new[] { "簽約金照意向書" } },
            { "risk_scope", new[] { "授權範圍要符合" } },
            { "risk_expire", new[] { "授權期限要有效" } },
            { "risk_holder", new[] { "被授權方要一致" } },
            { "risk_dead", new[] { "公司要在營業" } },
            { "debt", new[] { "負債要揭露" } },
            { "proj", new[] { "數字要有根據" } },
            { "cap", new[] { "賠償看合約" } },
            { "stolen", new[] { "來源不明不要收" } }
        };

        /// <summary>
        /// 這兩個欄位能不能證明這個問題。為了不讓「明明有錯，卻說沒問題」，以下都算：
        /// 1. 原本設定的那一組。
        /// 2. 另外列出的其他證據（例如檢測報告的「結論」也能證明狀況不實）。
        /// 3. 寫錯的欄位，對上任何一份文件裡寫著正確內容的欄位（例如甲方對名片或報價單都可以）。
        /// 4. 寫錯的欄位，對上手冊裡管這件事的規則。
        /// </summary>
        bool Proves(ContradictionData c, string a, string b)
        {
            if ((c.a == a && c.b == b) || (c.a == b && c.b == a)) return true;
            if (c.bad) return false;
            string wrong = !string.IsNullOrEmpty(c.fixField) ? c.fixField : null;

            if (!string.IsNullOrEmpty(c.alt))
            {
                foreach (string raw in c.alt.Split(','))
                {
                    string e = raw.Trim();
                    if (e.Length == 0) continue;
                    int k = e.IndexOf('~');
                    if (k > 0)
                    {
                        string x = e.Substring(0, k), y = e.Substring(k + 1);
                        if ((x == a && y == b) || (x == b && y == a)) return true;
                    }
                    else
                    {
                        string side = wrong ?? c.a;
                        if ((side == a && e == b) || (side == b && e == a)) return true;
                    }
                }
            }

            // 寫錯的欄位 + 別份文件上寫著正確內容的欄位
            if (wrong != null && !string.IsNullOrEmpty(c.fixValue) && (a == wrong || b == wrong))
            {
                string other = a == wrong ? b : a;
                FieldData of;
                if (DocOf(other) != DocOf(wrong) && fieldData.TryGetValue(other, out of) && ValueOf(of).Trim() == c.fixValue.Trim()) return true;
            }

            // 有問題的欄位 + 手冊裡的對應規則（沒有「寫錯的欄位」時，兩邊都算有問題的欄位）
            string[] rules;
            if (RuleOf.TryGetValue(c.key, out rules))
            {
                foreach (string side in wrong != null ? new[] { wrong } : new[] { c.a, c.b })
                {
                    string other = a == side ? b : (b == side ? a : null);
                    FieldData of;
                    if (other == null || DocOf(other) == DocOf(side) || !fieldData.TryGetValue(other, out of)) continue;
                    if (Array.IndexOf(rules, of.value) >= 0) return true;
                }
            }
            return false;
        }

        public string LabelOf(string fieldId)
        {
            string l;
            return fieldLabel.TryGetValue(fieldId, out l) ? l : fieldId;
        }

        public bool IsFixed(string fieldId)
        {
            if (offerDone && data.offer != null && data.offer.feeField == fieldId) return true;
            if (fieldOverride.ContainsKey(fieldId)) return true;
            foreach (ContradictionData c in data.contradictions)
                if (c.fixField == fieldId && fixedKeys.Contains(c.key)) return true;
            return false;
        }

        public string ValueOf(FieldData f)
        {
            if (offerDone && data.offer != null && data.offer.feeField == f.id) return GameState.Money(offerFinal);
            string ov;
            if (fieldOverride.TryGetValue(f.id, out ov)) return ov;
            foreach (ContradictionData c in data.contradictions)
                if (c.fixField == f.id && fixedKeys.Contains(c.key) && !string.IsNullOrEmpty(c.fixValue)) return c.fixValue;
            return f.value;
        }

        public int SlotOf(string docId)
        {
            if (docId == null) return -1;
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] == docId) return i;
            return -1;
        }

        public List<string> Risks()
        {
            List<string> r = new List<string>();
            foreach (ContradictionData c in data.contradictions)
                if (!c.bad && !fixedKeys.Contains(c.key) && !string.IsNullOrEmpty(c.risk)) r.Add(c.risk);
            return r;
        }

        public string IssueName(string key)
        {
            foreach (ContradictionData c in data.contradictions)
                if (c.key == key) return string.IsNullOrEmpty(c.name) ? key : c.name;
            return key;
        }

        public string ContactStatus(ContactData c, out string kind)
        {
            if (c.caller)
            {
                kind = result == null ? "talk" : "normal";
                return result == null ? "通話中" : "通話結束";
            }
            if (!string.IsNullOrEmpty(c.message))
            {
                bool read = readContacts.Contains(c.id);
                kind = read ? "normal" : "unread";
                return read ? "已讀" : "未讀 1";
            }
            kind = "normal";
            return c.status;
        }

        public List<ReplyData> AvailableReplies()
        {
            List<ReplyData> list = new List<ReplyData>();
            if (!CanAct) return list;
            foreach (ReplyData r in data.replies)
            {
                if (!r.repeat && usedReplies.Contains(r.id)) continue;
                if (!string.IsNullOrEmpty(r.requires) && !Check(r.requires)) continue;
                list.Add(r);
            }
            return list;
        }

        // ---------- 條件 ----------

        /// <summary>
        /// 條件語法：用 | 分隔「或」，用 &amp; 分隔「且」，前面加 ! 表示「不」。
        /// 例：found:fee、found==all、reply:ask、open:hb、offer、result==signed、wrong>0、flag:xxx。
        /// </summary>
        public bool Check(string cond)
        {
            if (string.IsNullOrEmpty(cond)) return true;
            foreach (string any in cond.Split('|'))
            {
                bool all = true;
                foreach (string part in any.Split('&'))
                {
                    if (!CheckOne(part.Trim())) { all = false; break; }
                }
                if (all) return true;
            }
            return false;
        }

        bool CheckOne(string c)
        {
            if (c.Length == 0) return true;
            bool neg = c[0] == '!';
            if (neg) c = c.Substring(1);
            bool v = Eval(c);
            return neg ? !v : v;
        }

        bool Eval(string c)
        {
            if (c == "tried") return tried;
            if (c == "result") return result != null;
            if (c == "offer") return offerDone;
            if (c == "recommend") return recommendDone;
            if (c == "plan") return planDone;
            if (c == "assigned") return assigned != null;
            if (c == "closed") return overlayClosed;
            if (c == "found==all") return FoundGood >= TotalGood;
            if (c.StartsWith("found:")) return fixedKeys.Contains(c.Substring(6));
            if (c.StartsWith("fixed:")) return fixedKeys.Contains(c.Substring(6));
            if (c.StartsWith("reply:")) return usedReplies.Contains(c.Substring(6));
            if (c.StartsWith("open:")) return SlotOf(c.Substring(5)) >= 0;
            if (c.StartsWith("result==")) return result == c.Substring(8);
            if (c.StartsWith("flag:")) return state.Has(c.Substring(5));
            return Compare(c);
        }

        bool Compare(string c)
        {
            string[] ops = { ">=", "<=", "==", ">", "<" };
            foreach (string op in ops)
            {
                int i = c.IndexOf(op, StringComparison.Ordinal);
                if (i <= 0) continue;
                string name = c.Substring(0, i).Trim();
                long rhs;
                if (!long.TryParse(c.Substring(i + op.Length).Trim(), out rhs)) return false;
                long lhs = Value(name);
                switch (op)
                {
                    case ">=": return lhs >= rhs;
                    case "<=": return lhs <= rhs;
                    case "==": return lhs == rhs;
                    case ">": return lhs > rhs;
                    default: return lhs < rhs;
                }
            }
            return false;
        }

        public long Value(string name)
        {
            switch (name)
            {
                case "found": return FoundGood;
                case "total": return TotalGood;
                case "wrong": return wrong;
                case "patience": return patience;
                case "fee": return Fee;
                case "cash": return state.cash;
                case "profit": return state.lastProfit;
                case "settle": return state.lastSettle;
                case "offer": return offerFinal;
                default: return 0;
            }
        }

        // ---------- 一般操作 ----------

        public void Notify(string who, string text, string kind)
        {
            toast = new Toast { who = who, text = text, kind = kind };
            Flush();
        }

        public void CloseToast()
        {
            toast = null;
            Flush();
        }

        public void ToggleMic()
        {
            mic = !mic;
            Flush();
        }

        public void ToggleCam()
        {
            cam = !cam;
            Flush();
        }

        public void Pick(string fieldId)
        {
            if (!CanAct) return;
            int at = sel.IndexOf(fieldId);
            if (at >= 0)
            {
                sel.RemoveAt(at);
                Flush();
                return;
            }
            string doc = DocOf(fieldId);
            int same = sel.FindIndex(f => DocOf(f) == doc);
            if (same >= 0) sel[same] = fieldId;
            else if (sel.Count < 2) sel.Add(fieldId);
            else sel[1] = fieldId;
            if (sel.Count == 1) toast = new Toast { who = "提示", text = "再點另一份文件裡對不上的欄位", kind = "info" };
            else if (toast != null && toast.who == "提示") toast = null;
            Raise("picked");
            Flush();
        }

        public void ClearSel()
        {
            sel.Clear();
            if (toast != null && toast.who == "提示") toast = null;
            Flush();
        }

        public void Challenge()
        {
            if (!CanAct || sel.Count < 2) return;
            string a = sel[0];
            string b = sel[1];
            string names = "「" + LabelOf(a) + "」和「" + LabelOf(b) + "」";
            ContradictionData hit = null;
            // 所有能用這兩個欄位證明的問題裡，先挑還沒提過的
            foreach (ContradictionData c in data.contradictions)
            {
                if (!Proves(c, a, b)) continue;
                if (!fixedKeys.Contains(c.key)) { hit = c; break; }
                if (hit == null) hit = c;
            }
            sel.Clear();
            tried = true;

            if (hit != null && fixedKeys.Contains(hit.key))
            {
                toast = new Toast { who = "提示", text = "這個問題已經提過了", kind = "info" };
                Flush();
                return;
            }
            if (hit != null)
            {
                fixedKeys.Add(hit.key);
                offerShift += hit.shift;
                toast = null;
                bubble = hit.reply;
                log.Add(new LogLine("你", names + "對不上。"));
                log.Add(new LogLine(data.client.name, hit.reply));
                if (hit.bad)
                {
                    toast = new Toast { who = "提示", text = "這是我方理虧的地方，對方的態度更硬了", kind = "bad" };
                    Raise("found_bad:" + hit.key);
                }
                else
                {
                    found.Add(hit.key);
                    Raise("found:" + hit.key);
                }
                Flush();
                return;
            }

            wrong++;
            patience = Math.Max(0, patience - 1);
            string reply = patience > 0 ? data.replyWrong : data.replyLeave;
            bubble = reply;
            log.Add(new LogLine("你", names + "好像有問題？"));
            log.Add(new LogLine(data.client.name, reply));
            toast = new Toast { who = "提示", text = "沒有矛盾，客戶耐心下降", kind = "bad" };
            Raise("wrong");
            if (patience == 0) EndAs("leave", reply);
            Flush();
        }

        public void Place(string docId, int i)
        {
            if (i < 0 || i >= SlotCount || Doc(docId) == null) return;
            int from = SlotOf(docId);
            if (from == i) return;
            string old = slots[i];
            if (from >= 0) slots[from] = old;
            slots[i] = docId;
            sel.RemoveAll(f => SlotOf(DocOf(f)) < 0);
            if (from < 0) Raise("opened:" + docId);
            Flush();
        }

        public void Open(string docId)
        {
            if (SlotOf(docId) >= 0)
            {
                Notify("提示", "這份已經在檢視區了", "info");
                return;
            }
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null)
                {
                    Place(docId, i);
                    return;
                }
            }
            Notify("提示", "檢視區滿了，先收回一份或拖去替換", "warn");
        }

        public void Close(string docId)
        {
            int i = SlotOf(docId);
            if (i < 0) return;
            slots[i] = null;
            sel.RemoveAll(f => DocOf(f) == docId);
            Flush();
        }

        public void ContactClicked(string id)
        {
            ContactData c = null;
            foreach (ContactData x in data.contacts)
                if (x.id == id) c = x;
            if (c == null) return;
            if (c.caller)
            {
                Notify("提示", "正在跟" + c.name + "通話", "info");
                return;
            }
            if (!string.IsNullOrEmpty(c.message))
            {
                if (!readContacts.Contains(c.id))
                {
                    readContacts.Add(c.id);
                    log.Add(new LogLine(c.name, c.message));
                }
                toast = new Toast { who = c.name, text = c.message, kind = "msg" };
                Raise("message:" + c.id);
                Flush();
                return;
            }
            Notify("提示", "這位聯絡人目前沒有事情", "info");
        }

        public void Reply(string id)
        {
            if (!CanAct) return;
            ReplyData r = null;
            foreach (ReplyData x in data.replies)
                if (x.id == id) r = x;
            if (r == null) return;
            usedReplies.Add(id);
            log.Add(new LogLine("你", string.IsNullOrEmpty(r.say) ? r.text : r.say));
            if (r.cost > 0)
            {
                state.Spend(r.cost);
                inspectCost += r.cost;
            }
            patience = Math.Max(0, Math.Min(data.patience, patience + r.patience));
            offerShift += r.shift;
            state.Set(r.flag);
            List<string> got = new List<string>();
            foreach (string g in r.gives)
            {
                if (Doc(g) == null || storage.Contains(g)) continue;
                storage.Add(g);
                got.Add(Doc(g).title);
            }
            toast = got.Count > 0 ? new Toast { who = "收到資料", text = string.Join("、", got.ToArray()) + "（在下方資料倉庫）", kind = "good" } : null;
            if (!string.IsNullOrEmpty(r.response))
            {
                bubble = r.response;
                log.Add(new LogLine(data.client.name, r.response));
            }
            Raise("reply:" + id);
            if (!string.IsNullOrEmpty(r.result)) EndAs(r.result, string.IsNullOrEmpty(r.response) ? null : r.response);
            Flush();
        }

        // ---------- 議價 ----------

        public long OfferLimit
        {
            get
            {
                OfferData o = data.offer;
                if (o == null) return 0;
                return o.mode == "lower" ? Math.Max(0, o.accept - offerShift) : o.accept + offerShift;
            }
        }

        long OfferWalk
        {
            get
            {
                OfferData o = data.offer;
                long lim = OfferLimit;
                long gap = Math.Max(o.step, 1);
                if (o.mode == "lower") return Math.Min(Math.Max(0, o.walk - offerShift + offerHarden), Math.Max(0, lim - gap));
                return Math.Max(o.walk + offerShift - offerHarden, lim + gap);
            }
        }

        /// <summary>
        /// 這個價錢對方答應的機率（0~100）。在對方底線以內一定成交；
        /// 越接近對方的極限機率越低，超過極限就是 0（對方會生氣）。
        /// 找到的問題、用對的回應都會讓底線往我們這邊移，機率跟著變高。
        /// </summary>
        public int OfferChance(long v)
        {
            OfferData o = data.offer;
            if (o == null) return 0;
            bool raise = o.mode != "lower";
            long lim = OfferLimit;
            long walk = OfferWalk;
            if (raise ? v <= lim : v >= lim) return 100;
            if (raise ? v >= walk : v <= walk) return 0;
            double t = Math.Abs(v - lim) / (double)Math.Max(1, Math.Abs(walk - lim));
            int p = (int)Math.Round(95 * (1 - t));
            return Math.Max(5, Math.Min(95, p));
        }

        public int OfferChanceNow { get { return OfferChance(offerValue); } }

        // ---------- 固定的出價選項 ----------
        // 不能自己微調金額，只能從幾個固定的幅度挑一個，按下去就送出。
        static readonly double[] RaiseSteps = { 0, 0.1, 0.2, 0.3 };
        static readonly double[] LowerSteps = { 0, -0.1, -0.2, -0.3 };
        public const int OfferOptionCount = 4;

        double OfferStepAt(int i)
        {
            OfferData o = data.offer;
            double[] steps = o != null && o.mode == "lower" ? LowerSteps : RaiseSteps;
            return steps[Math.Max(0, Math.Min(steps.Length - 1, i))];
        }

        /// <summary>第 i 個選項的金額（以對方現在的開價為準）。</summary>
        public long OfferOption(int i)
        {
            OfferData o = data.offer;
            if (o == null) return 0;
            long v = CaseGen.Nice(offerAsk * (1 + OfferStepAt(i)));
            if (o.max > 0) v = Math.Min(v, o.max);
            return Math.Max(o.min, v);
        }

        /// <summary>按鈕上的字：照價、+10%、-20%……</summary>
        public string OfferOptionLabel(int i)
        {
            double p = OfferStepAt(i);
            if (p == 0) return "照價";
            return (p > 0 ? "+" : "-") + Math.Round(Math.Abs(p) * 100) + "%";
        }

        /// <summary>選一個出價，馬上送出。</summary>
        public void OfferPick(int i)
        {
            if (data.offer == null || offerDone || !CanAct) return;
            offerValue = OfferOption(i);
            OfferSubmit();
        }

        public void OfferStep(int steps)
        {
            OfferData o = data.offer;
            if (o == null || offerDone || !CanAct) return;
            long v = offerValue + (long)steps * o.step;
            if (o.max > 0) v = Math.Min(v, o.max);
            v = Math.Max(o.min, v);
            offerValue = v;
            Flush();
        }

        public void OfferSubmit()
        {
            OfferData o = data.offer;
            if (o == null || offerDone || !CanAct) return;
            long lim = OfferLimit;
            long walk = OfferWalk;
            int chance = OfferChance(offerValue);
            bool ok = chance >= 100 || (chance > 0 && rng.Next(100) < chance);
            bool near = chance > 0;
            log.Add(new LogLine("你", o.label + "，我們提 " + GameState.Money(offerValue) + "。"));
            string text;
            if (ok)
            {
                offerDone = true;
                offerFinal = offerValue;
                text = Fill(o.acceptText, offerValue);
                ApplyOffer(o);
                bubble = text;
                log.Add(new LogLine(data.client.name, text));
                toast = new Toast { who = "成交", text = o.label + " " + GameState.Money(offerFinal), kind = "good" };
                Raise("offer:accepted");
                if (o.endsCall) EndAs("deal", text);
            }
            else if (near)
            {
                patience = Math.Max(0, patience - 1);
                offerAsk = lim;
                offerMisses++;
                // 對方的極限收緊三分之一，同一個價錢一直重試，機率會越來越低
                offerHarden += Math.Max(o.step, Math.Abs(walk - lim) / 3);
                text = Fill(o.counterText, lim);
                bubble = text;
                log.Add(new LogLine(data.client.name, text));
                toast = new Toast { who = "提示", text = "沒談成（成功率 " + chance + "%），對方還價，耐心下降", kind = "warn" };
                Raise("offer:countered");
            }
            else
            {
                patience = Math.Max(0, patience - 2);
                text = Fill(o.refuseText, lim);
                bubble = text;
                log.Add(new LogLine(data.client.name, text));
                toast = new Toast { who = "提示", text = "出價太離譜，耐心大幅下降", kind = "bad" };
                Raise("offer:refused");
            }
            if (result == null && patience == 0) EndAs("leave", data.replyLeave);
            Flush();
        }

        static string Fill(string text, long v)
        {
            if (string.IsNullOrEmpty(text)) return GameState.Money(v);
            return text.Replace("{v}", GameState.Money(v));
        }

        /// <summary>議價談成的金額；沒談或談的不是這一項，就用原本的金額。</summary>
        long NegotiatedOr(string apply, long original)
        {
            return data.offer != null && offerDone && data.offer.apply == apply ? offerFinal : original;
        }

        void ApplyOffer(OfferData o)
        {
            switch (o.apply)
            {
                case "commission":   // 佣金：推薦成功時照談好的金額算
                case "planfee":      // 企劃費：企劃通過時照談好的金額算
                    break;
                case "fee":
                    feeOverride = offerFinal;
                    break;
                case "buy":
                    state.Spend(offerFinal);
                    state.inventory.Add(new InvItem
                    {
                        id = string.IsNullOrEmpty(o.itemId) ? "inv_item" : o.itemId,
                        name = o.itemName,
                        condition = KnownCondition(o),
                        cost = offerFinal + inspectCost,
                        category = o.itemCategory,
                        grade = o.itemGrade,
                        baseValue = o.itemBase,
                        flags = o.itemFlags,
                        boughtDay = state.day
                    });
                    state.lastProfit = 0;
                    break;
                case "sell":
                    InvItem item = state.inventory.Find(i => i.id == o.itemId);
                    if (item == null && state.inventory.Count > 0) item = state.inventory[0];
                    state.Earn(offerFinal);
                    state.lastProfit = offerFinal - (item != null ? item.cost : 0);
                    if (item != null) state.inventory.Remove(item);
                    break;
                case "settle":
                    state.Spend(offerFinal);
                    state.lastSettle = offerFinal;
                    break;
                case "invest":
                    state.Spend(offerFinal);
                    state.investments.Add(new Investment { name = o.itemName, amount = offerFinal, refId = o.refId, startDay = state.day });
                    break;
            }
        }

        /// <summary>收購時玩家知道的狀況：對方說的，或質疑後改正的。</summary>
        string KnownCondition(OfferData o)
        {
            foreach (ContradictionData c in data.contradictions)
                if (fixedKeys.Contains(c.key) && c.key == "broken" && !string.IsNullOrEmpty(c.fixValue)) return c.fixValue;
            foreach (ContradictionData c in data.contradictions)
                if (fixedKeys.Contains(c.key) && c.key == "cond" && !string.IsNullOrEmpty(c.fixValue)) return c.fixValue;
            return o.itemCondition;
        }

        // ---------- 仲介推薦 ----------

        public void Recommend(string docId)
        {
            RecommendData r = data.recommend;
            if (r == null || recommendDone || !CanAct) return;
            log.Add(new LogLine("你", "我推薦" + (Doc(docId) != null ? Doc(docId).title.Replace("藝人檔案・", "") : docId) + "。"));
            if (docId == r.correct)
            {
                recommendDone = true;
                recommendReward = NegotiatedOr("commission", r.reward);
                if (recommendReward > 0) state.Accrue(recommendReward);
                bubble = r.okText;
                log.Add(new LogLine(data.client.name, r.okText));
                List<string> got = new List<string>();
                foreach (string g in r.gives)
                {
                    if (Doc(g) == null || storage.Contains(g)) continue;
                    storage.Add(g);
                    got.Add(Doc(g).title);
                }
                string extra = got.Count > 0 ? "，收到" + string.Join("、", got.ToArray()) : "";
                toast = new Toast { who = "仲介成功", text = "佣金 " + GameState.Money(recommendReward) + " 記入應收款" + extra, kind = "good" };
                Raise("recommend:ok");
                if (data.plan == null) EndAs("deal", r.okText);
            }
            else
            {
                wrong++;
                patience = Math.Max(0, patience - 1);
                bubble = r.badText;
                log.Add(new LogLine(data.client.name, r.badText));
                toast = new Toast { who = "提示", text = "推薦的人不符合需求，客戶耐心下降", kind = "bad" };
                Raise("recommend:bad");
                if (patience == 0) EndAs("leave", data.replyLeave);
            }
            Flush();
        }

        // ---------- 企劃組裝 ----------

        public void PlanOpen()
        {
            if (data.plan == null || planDone || !CanAct) return;
            planOpen = true;
            Raise("plan_open");
            Flush();
        }

        public void PlanClose()
        {
            planOpen = false;
            Flush();
        }

        public void PlanPick(int g, int o)
        {
            if (g < 0 || g >= planPick.Length) return;
            planPick[g] = planPick[g] == o ? -1 : o;
            Flush();
        }

        public long PlanCost()
        {
            long t = 0;
            if (data.plan == null) return 0;
            for (int g = 0; g < planPick.Length; g++)
                if (planPick[g] >= 0) t += data.plan.groups[g].options[planPick[g]].cost;
            return t;
        }

        bool PlanHas(string tag)
        {
            for (int g = 0; g < planPick.Length; g++)
            {
                if (planPick[g] < 0) continue;
                foreach (string t in data.plan.groups[g].options[planPick[g]].tags)
                    if (t == tag) return true;
            }
            return false;
        }

        public bool PlanReqOk(PlanReq r)
        {
            if (r.budget > 0 && PlanCost() > r.budget) return false;
            if (!string.IsNullOrEmpty(r.tag) && !PlanHas(r.tag)) return false;
            if (!string.IsNullOrEmpty(r.notag) && PlanHas(r.notag)) return false;
            return true;
        }

        public int PlanBudget()
        {
            int b = 0;
            if (data.plan == null) return 0;
            foreach (PlanReq r in data.plan.reqs) if (r.budget > 0 && (b == 0 || r.budget < b)) b = r.budget;
            return b;
        }

        public long PlanRewardNow()
        {
            PlanData p = data.plan;
            if (p == null) return 0;
            long r = NegotiatedOr("planfee", p.reward);
            if (p.payDiff) r += Math.Max(0, PlanBudget() - PlanCost());
            return r;
        }

        public bool PlanComplete()
        {
            foreach (int p in planPick) if (p < 0) return false;
            return true;
        }

        public void PlanSubmit()
        {
            PlanData p = data.plan;
            if (p == null || planDone || !CanAct) return;
            if (!PlanComplete())
            {
                toast = new Toast { who = "提示", text = "每一項都要選一個", kind = "warn" };
                Flush();
                return;
            }
            PlanReq fail = null;
            foreach (PlanReq r in p.reqs)
                if (!PlanReqOk(r)) { fail = r; break; }
            if (fail != null)
            {
                patience = Math.Max(0, patience - 1);
                toast = new Toast { who = "企劃被退回", text = "不符合：" + fail.text, kind = "bad" };
                bubble = "這份企劃……「" + fail.text + "」這點沒做到喔。";
                log.Add(new LogLine(data.client.name, bubble));
                Raise("plan:fail");
                if (patience == 0) EndAs("leave", data.replyLeave);
                Flush();
                return;
            }
            planDone = true;
            planOpen = false;
            planReward = PlanRewardNow();
            if (planReward > 0) state.Accrue(planReward);
            toast = new Toast { who = "企劃通過", text = "企劃費 " + GameState.Money(planReward) + " 記入應收款", kind = "good" };
            Raise("plan:ok");
            EndAs("deal", p.okText);
            Flush();
        }

        // ---------- 結束 ----------

        public void Finish(string kind)
        {
            if (!CanAct) return;
            if (kind == "signed" && NeedsAssign)
            {
                toast = new Toast { who = "提示", text = "對方沒指定藝人，先打開「旗下藝人」選一位", kind = "warn" };
                Flush();
                return;
            }
            if (kind == "signed" && !CanSign)
            {
                toast = new Toast { who = data.hintWho, text = "合約裡還有問題沒抓到，先別蓋章。", kind = "warn" };
                Flush();
                return;
            }
            EndAs(kind, null);
            Flush();
        }

        void EndAs(string kind, string line)
        {
            if (result != null) return;
            string reply = line;
            if (reply == null)
            {
                switch (kind)
                {
                    case "signed": reply = data.replySigned; break;
                    case "reject": reply = data.replyReject; break;
                    case "deal": reply = data.replyDeal; break;
                    case "leave": reply = data.replyLeave; break;
                    default: reply = data.replyHold; break;
                }
                if (!string.IsNullOrEmpty(reply)) log.Add(new LogLine(data.client.name, reply));
            }
            result = kind;
            sel.Clear();
            planOpen = false;
            if (!string.IsNullOrEmpty(reply)) bubble = reply;
            if (kind == "signed" && !data.deferFee) state.Accrue(Fee);
            Raise("result:" + kind);
        }

        public void CloseOverlay()
        {
            if (overlayClosed) return;
            overlayClosed = true;
            Raise("case_closed");
            Flush();
        }

        public void SetDayEnded(bool v)
        {
            dayEnded = v;
            sel.Clear();
            Flush();
        }
    }
}
