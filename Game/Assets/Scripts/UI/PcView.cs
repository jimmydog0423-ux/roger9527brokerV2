using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 電腦畫面（C2 版面）。
    /// 上方：聯絡人 / 視訊 / 日期資金 / 目前任務
    /// 中間：四格檢視區，直接在文件上點選欄位、提出質疑、議價、蓋章
    /// 下方：資料倉庫，拖到檢視區打開
    /// 所有內容都由 CaseSession 的狀態決定，狀態一變就重畫。
    /// </summary>
    public class PcView : MonoBehaviour
    {
        public static readonly float[] SlotX = { 4f, 163f, 322f, 481f };
        public const float SlotY = 136f;
        public const float SlotW = 155f;
        public const float SlotH = 164f;

        RectTransform rt;
        GameRoot game;
        CaseSession s;
        CanvasGroup group;

        Transform contacts, replyLayer, toolbar, tasks, slotsLayer, linkLayer, hotbarLayer, toastLayer, planLayer, rosterLayer, detailLayer, overlayLayer;
        string detailDoc;   // 正在看「詳細」的文件
        Text callTitle, callStatus, bubble, nameTag, dayText, recvText, cashText, capText;
        Image callDot;
        readonly Image[] bars = new Image[5];
        RectTransform dropHi, ghost;
        Image portrait;
        readonly Dictionary<string, RectTransform> fieldRects = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, RectTransform> docRects = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, RectTransform> itemRects = new Dictionary<string, RectTransform>();
        RectTransform stampRect, replyRect;
        string dragId;
        Toast lastToast;
        float toastTime;

        public CanvasGroup Group { get { return group; } }

        public static PcView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Computer", 0, 0, 640, 360);
            PcView v = r.gameObject.AddComponent<PcView>();
            v.rt = r;
            v.game = game;
            v.group = r.gameObject.AddComponent<CanvasGroup>();
            v.BuildStatic();
            return v;
        }

        public void Bind(CaseSession session)
        {
            s = session;
            if (s == null) return;
            CaseData d = s.data;
            portrait.sprite = PortraitArt.Get(d.client.id);
            nameTag.text = Px.Fit(d.client.name + "・" + d.client.company, 186);
            lastToast = null;
            Refresh();
        }

        void Update()
        {
            if (s == null || s.toast == null) return;
            if (s.toast != lastToast)
            {
                lastToast = s.toast;
                toastTime = 0f;
                return;
            }
            toastTime += Time.unscaledDeltaTime;
            if (toastTime > 5f) s.CloseToast();
        }

        // ------------------------------------------------------------------
        // 固定不變的部分
        // ------------------------------------------------------------------

        void BuildStatic()
        {
            Image bg = Px.Fill(rt, 0, 0, 640, 360, Pal.Night);
            bg.raycastTarget = true;
            Px.Fill(rt, 0, 132, 640, 172, Pal.PlumDeep);
            Px.Tiled(rt, 0, 133, 640, 171, PixelSprites.Grid16, Pal.DeskGrid);
            Px.Fill(rt, 0, 132, 640, 1, Pal.PlumLine);
            Px.Fill(rt, 0, 304, 640, 56, Pal.Ink);
            Px.Fill(rt, 0, 304, 640, 1, Pal.PlumLine);

            // 1 聯絡人
            RectTransform cp = Px.Box(rt, "Contacts", 4, 4, 104, 124, Pal.PlumDeep, Pal.PlumLine);
            Px.Fill(cp, 1, 1, 102, 14, Pal.Ink);
            Px.Fill(cp, 1, 1, 2, 14, Pal.TealDark);
            Px.Label(cp, "聯絡人", 6, 1, 90, 14, Pal.Cream);
            contacts = Px.Rect(cp, "List", 0, 0, 104, 124);

            // 2 視訊
            RectTransform vw = Px.Box(rt, "Video", 112, 4, 316, 124, Pal.Ink, Pal.PlumLight);
            Px.Fill(vw, 1, 1, 314, 15, Pal.Plum);
            callDot = Px.Fill(vw, 5, 6, 5, 5, Pal.TealLight);
            callTitle = Px.Label(vw, "", 14, 1, 232, 14, Pal.Cream);
            Px.Fill(vw, 252, 2, 58, 13, Pal.Ink);
            callStatus = Px.Label(vw, "", 252, 1, 58, 14, Pal.TealLight, TextAnchor.UpperCenter, false);

            RectTransform feed = Px.Rect(vw, "Feed", 1, 16, 314, 86);
            Image fbg = feed.gameObject.AddComponent<Image>();
            fbg.color = Pal.TrayBg;
            fbg.raycastTarget = false;
            feed.gameObject.AddComponent<RectMask2D>();
            Px.Fill(feed, 4, 4, 100, 80, Pal.VideoWall);
            Px.Tiled(feed, 5, 5, 98, 78, PixelSprites.Blinds, Pal.White);
            Px.Frame(feed, 4, 4, 100, 80, Pal.PlumLight);
            portrait = PortraitArt.Draw(feed, "lin", 21, 2, 3);

            RectTransform bb = Px.Box(feed, "Bubble", 108, 4, 202, 62, Pal.Cream, Pal.Ink);
            Px.Fill(bb, 1, 1, 200, 15, Pal.PlumDeep);
            Px.Fill(bb, 1, 1, 3, 15, Pal.TealDark);
            nameTag = Px.Label(bb, "", 8, 1, 186, 14, Pal.Cream);
            bubble = Px.Label(bb, "", 4, 17, 194, 42, Pal.PaperInk, TextAnchor.UpperLeft, true);
            replyRect = Px.Rect(feed, "Replies", 4, 68, 306, 16);
            replyLayer = replyRect;

            Px.Fill(vw, 1, 102, 314, 21, Pal.Plum);
            toolbar = Px.Rect(vw, "Toolbar", 0, 102, 316, 21);
            Px.Button(vw, "掛斷", 89, 105, 32, 15, Pal.Red, Pal.Cream, Pal.Ink, () => { if (s != null) s.Finish("hold"); });
            Px.Label(vw, "客戶耐心", 214, 105, 56, 14, Pal.Lav);
            for (int i = 0; i < bars.Length; i++)
                bars[i] = Px.Fill(vw, 272 + i * 7, 110, 5, 7, Pal.Orange);

            // 3 日期與資金
            RectTransform dp = Px.Box(rt, "Day", 432, 4, 204, 56, Pal.Plum, Pal.PlumLight);
            dayText = Px.Label(dp, "", 6, 2, 120, 14, Pal.Cream);
            recvText = Px.Label(dp, "", 80, 2, 118, 14, Pal.TealLight, TextAnchor.UpperRight, false);
            cashText = Px.Label(dp, "", 6, 16, 190, 14, Pal.Cream);
            Px.Button(dp, "選單", 6, 36, 40, 16, Pal.PlumLine, Pal.Cream, Pal.Ink, () => game.OpenMenu());
            Px.Button(dp, "結束營業", 138, 36, 60, 16, Pal.Red, Pal.Cream, Pal.Ink, () => game.RequestEndDay());

            // 4 目前任務
            RectTransform tp = Px.Box(rt, "Tasks", 432, 64, 204, 67, Pal.Plum, Pal.PlumLight);
            Px.Fill(tp, 1, 1, 202, 14, Pal.Ink);
            Px.Fill(tp, 1, 1, 2, 14, Pal.Orange);
            Px.Label(tp, "目前任務", 6, 1, 120, 14, Pal.Cream);
            tasks = Px.Rect(tp, "List", 0, 0, 204, 67);

            // 5 檢視區、連線
            slotsLayer = Px.Rect(rt, "Slots", 0, 0, 640, 360);
            linkLayer = Px.Rect(rt, "Link", 0, 0, 640, 360);

            // 6 資料倉庫
            RectTransform si = Px.Box(rt, "StorageInfo", 4, 308, 96, 48, Pal.Plum, Pal.PlumLight);
            Px.Label(si, "資料倉庫", 5, 1, 88, 14, Pal.Cream);
            capText = Px.Label(si, "", 5, 15, 88, 14, Pal.Lav);
            Px.Button(si, "擴充倉庫", 5, 31, 60, 14, Pal.PlumLine, Pal.Cream, Pal.Ink,
                () => { if (s != null) s.Notify("提示", "這裡放的是這通電話收到的資料。公司的商品庫存在電腦桌面的「庫存」", "info"); });
            hotbarLayer = Px.Rect(rt, "Hotbar", 0, 0, 640, 360);

            toastLayer = Px.Rect(rt, "Toast", 0, 0, 640, 360);

            dropHi = Px.Rect(rt, "DropHint", 0, SlotY, SlotW, SlotH);
            Px.Fill(dropHi, 0, 0, SlotW, SlotH, Pal.Alpha(Pal.Orange, 36));
            Px.Frame(dropHi, 0, 0, SlotW, SlotH, Pal.Orange, 2);
            Px.Fill(dropHi, 44, SlotH - 22, 66, 14, Pal.Ink);
            Px.Label(dropHi, "放到這裡", 44, SlotH - 22, 66, 14, Pal.Orange, TextAnchor.UpperCenter, false);
            dropHi.gameObject.SetActive(false);

            ghost = Px.Rect(rt, "Ghost", 0, 0, 48, 48);
            ghost.gameObject.SetActive(false);

            planLayer = Px.Rect(rt, "Plan", 0, 0, 640, 360);
            rosterLayer = Px.Rect(rt, "Roster", 0, 0, 640, 360);
            detailLayer = Px.Rect(rt, "Detail", 0, 0, 640, 360);
            overlayLayer = Px.Rect(rt, "Overlay", 0, 0, 640, 360);
        }

        // ------------------------------------------------------------------
        // 依狀態重畫
        // ------------------------------------------------------------------

        public void Refresh()
        {
            if (s == null) return;
            RefreshContacts();
            RefreshCall();
            RefreshDay();
            RefreshTasks();
            RefreshSlots();
            RefreshLink();
            RefreshHotbar();
            RefreshToast();
            RefreshPlan();
            RefreshRoster();
            RefreshDetail();
            RefreshOverlay();
        }

        void RefreshContacts()
        {
            Px.Clear(contacts);
            ContactData[] list = s.data.contacts;
            for (int i = 0; i < list.Length && i < 3; i++)
            {
                ContactData c = list[i];
                string kind;
                string status = s.ContactStatus(c, out kind);
                Color32 sc = kind == "talk" ? Pal.TealLight : (kind == "unread" ? Pal.Yellow : Pal.Lav);
                RectTransform card = Px.Box(contacts, "Card_" + c.id, 4, 18 + i * 35, 96, 32, Pal.Plum, Pal.PlumLine);
                if (c.caller) Px.Fill(card, 0, 0, 2, 32, Pal.TealLight);
                string id = c.id;
                Px.MakeClickable(card, () => s.ContactClicked(id));
                Px.Fill(card, 3, 2, 24, 28, Pal.VideoWall);
                PortraitArt.Draw(card, c.id, 3, 2, 1);
                Px.Label(card, c.name, 31, 2, 62, 14, Pal.Cream);
                Px.Label(card, status, 31, 16, 62, 14, sc);
            }
        }

        void RefreshCall()
        {
            ClientData cl = s.data.client;
            bool live = s.result == null;
            string role = string.IsNullOrEmpty(cl.role) ? "" : "（" + cl.role + "）";
            callTitle.text = Px.Fit(cl.company + role, 232);
            callDot.color = live ? Pal.TealLight : Pal.Lav;
            callStatus.text = live ? "通話中" : "已結束";
            callStatus.color = callDot.color;
            bubble.text = s.bubble;

            Px.Clear(replyLayer);
            float x = 0;
            foreach (ReplyData r in s.AvailableReplies())
            {
                float w = Px.TextWidth(r.text) + 12;
                if (x + w > 306) break;
                string id = r.id;
                Px.Button(replyLayer, r.text, x, 0, w, 16, Pal.Orange, Pal.Ink, Pal.Ink, () => s.Reply(id));
                x += w + 4;
            }

            Px.Clear(toolbar);
            Px.Button(toolbar, "麥克風", 5, 3, 44, 15, s.mic ? Pal.TealDark : Pal.PlumDeep, Pal.Cream, s.mic ? Pal.Teal : Pal.Pink, () => s.ToggleMic());
            Px.Button(toolbar, "鏡頭", 53, 3, 32, 15, s.cam ? Pal.TealDark : Pal.PlumDeep, Pal.Cream, s.cam ? Pal.Teal : Pal.Pink, () => s.ToggleCam());

            int maxP = Mathf.Max(1, s.data.patience);
            int lit = Mathf.CeilToInt(5f * s.patience / maxP);
            Color32 patienceColor = lit <= 2 ? Pal.Pink : Pal.Orange;
            for (int i = 0; i < bars.Length; i++) bars[i].color = i < lit ? patienceColor : Pal.PlumLine;
        }

        void RefreshDay()
        {
            GameState g = s.state;
            dayText.text = "第 " + g.day + " 天・" + g.Weekday;
            cashText.text = "現金 " + GameState.Money(g.cash);
            recvText.text = g.receivable > 0 ? "應收 " + GameState.Money(g.receivable) : "";
        }

        void RefreshTasks()
        {
            Px.Clear(tasks);
            string obj = game.Objective;
            if (!string.IsNullOrEmpty(obj))
            {
                Px.Fill(tasks, 6, 21, 6, 6, Pal.Orange);
                Px.Label(tasks, obj, 16, 17, 182, 44, Pal.Orange, TextAnchor.UpperLeft, true);
                return;
            }
            TaskData[] list = s.data.tasks;
            int rows = Mathf.Min(list.Length, 4);
            float gap = rows > 3 ? 12 : 15;
            for (int i = 0; i < rows; i++)
            {
                float y = 17 + i * gap;
                bool done = s.Check(list[i].doneWhen);
                Px.Frame(tasks, 6, y + 3, 8, 8, Pal.Lav);
                if (done) Px.Fill(tasks, 7, y + 4, 6, 6, Pal.TealLight);
                Px.Label(tasks, done ? list[i].text : TaskText(list[i]), 19, y, 180, 14, done ? Pal.Lav : Pal.Cream);
                if (done) Px.Fill(tasks, 19, y + 7, Px.TextWidth(list[i].text), 1, Pal.Lav);
            }
        }

        /// <summary>
        /// 一個任務要找好幾個問題時，在後面加上進度，例如「檢查日期與條款（1/3）」，
        /// 玩家才知道還差幾個，不會以為已經找完了。
        /// </summary>
        string TaskText(TaskData t)
        {
            string w = t.doneWhen;
            if (string.IsNullOrEmpty(w)) return t.text;
            string core = null;
            foreach (string alt in w.Split('|'))
            {
                string a = alt.Trim();
                if (a == "result" || a.Length == 0) continue;
                if (core != null) return t.text;   // 有兩種以上的完成方式，不顯示進度
                core = a;
            }
            if (core == null) return t.text;
            int have, need;
            if (core == "found==all")
            {
                have = s.FoundGood;
                need = s.TotalGood;
            }
            else if (core.StartsWith("found>="))
            {
                if (!int.TryParse(core.Substring(7), out need)) return t.text;
                have = Mathf.Min(s.FoundGood, need);
            }
            else if (core.IndexOf('&') >= 0)
            {
                string[] parts = core.Split('&');
                have = 0;
                need = parts.Length;
                foreach (string part in parts)
                {
                    string q = part.Trim();
                    if (!q.StartsWith("found:")) return t.text;
                    if (s.Check(q)) have++;
                }
            }
            else return t.text;
            if (need <= 1) return t.text;
            return t.text + "（" + have + "/" + need + "）";
        }

        // ---------- 檢視區 ----------

        void RefreshSlots()
        {
            Px.Clear(slotsLayer);
            fieldRects.Clear();
            docRects.Clear();
            stampRect = null;
            for (int i = 0; i < CaseSession.SlotCount; i++)
            {
                string id = s.slots[i];
                if (id == null)
                {
                    Px.Fill(slotsLayer, SlotX[i], SlotY, SlotW, SlotH, Pal.Alpha(Pal.Night, 216));
                    Px.Frame(slotsLayer, SlotX[i], SlotY, SlotW, SlotH, Pal.PlumLine);
                    Px.Fill(slotsLayer, SlotX[i] + 1, SlotY + 1, SlotW - 2, 16, Pal.TrayBg);
                    Px.Label(slotsLayer, "檢視區 " + (i + 1), SlotX[i] + 7, SlotY + 2, SlotW - 14, 14, Pal.Lav);
                    Px.Label(slotsLayer, "拖曳或點選", SlotX[i], SlotY + 68, SlotW, 14, Pal.Lav, TextAnchor.UpperCenter, false);
                    Px.Label(slotsLayer, "下方資料", SlotX[i], SlotY + 82, SlotW, 14, Pal.Lav, TextAnchor.UpperCenter, false);
                    continue;
                }
                DocumentData d = s.Doc(id);
                if (d != null) BuildDoc(i, d);
            }
        }

        RectTransform DocFrame(int slot, DocumentData d, float h)
        {
            Color32 paper = Pal.Hex(d.paper);
            Color32 head = Pal.Hex(d.head);
            string docId = d.id;
            RectTransform doc = Px.Rect(slotsLayer, "Doc_" + d.id, SlotX[slot], SlotY, SlotW, h);
            Px.Fill(doc, 2, 2, SlotW, h, Pal.Ink);
            Px.Fill(doc, 0, 0, SlotW, h, paper);
            Px.Frame(doc, 0, 0, SlotW, h, Pal.Ink);
            Px.Fill(doc, 1, 1, SlotW - 2, 14, head);
            Px.Fill(doc, 1, 15, SlotW - 2, 1, Pal.Ink);
            // 有字放不下的文件，標題旁邊多一個「詳細」，打開可以看完整內容
            bool detail = NeedsDetail(d);
            Px.Label(doc, d.title, 5, 1, detail ? 82 : 112, 14, Pal.PaperInk);
            if (detail) Px.Button(doc, "詳細", 88, 1, 31, 14, Pal.Orange, Pal.Ink, Pal.Ink, () => { detailDoc = docId; RefreshDetail(); });
            Px.Button(doc, "收回", 122, 1, 31, 14, paper, Pal.PaperInk, Pal.Ink, () => s.Close(docId));
            docRects[d.id] = doc;
            return doc;
        }

        void BuildDoc(int slot, DocumentData d)
        {
            switch (d.kind)
            {
                case "offer": BuildOffer(slot, d); return;
                case "finance": BuildFinance(slot, d); return;
                case "plan": BuildPlanDoc(slot, d); return;
                case "roster": BuildRosterDoc(slot, d); return;
            }

            bool talent = d.kind == "talent";
            bool isLog = d.kind == "log";
            bool rec = d.recommend && s.data.recommend != null;
            float rowsY = talent ? 94f : 16f;
            float rowsEnd = isLog ? 158f : rowsY + d.fields.Length * 15f;
            float footer = (d.stamp || rec) ? 24f : 3f;
            float h = rowsEnd + footer + 1f;
            Color32 paper = Pal.Hex(d.paper);
            RectTransform doc = DocFrame(slot, d, h);

            if (talent)
            {
                Px.Box(doc, "PortraitFrame", 3, 18, 50, 58, Pal.VideoWall, Pal.Ink);
                PortraitArt.Draw(doc, string.IsNullOrEmpty(d.portrait) ? "roger" : d.portrait, 4, 19, 2);
                for (int j = 0; j < d.info.Length && j < 4; j++) Row(doc, d.info[j], 55, 18 + j * 15, 98, InfoLabelW, true);
                Px.Label(doc, d.subTitle, 6, 79, 146, 14, Pal.PaperLabel);
            }

            if (isLog)
            {
                RectTransform area = Px.Rect(doc, "Log", 4, 18, 147, 138);
                area.gameObject.AddComponent<RectMask2D>();
                Px.Label(area, LogText(), 0, 0, 147, 138, Pal.PaperInk, TextAnchor.LowerLeft, true);
            }
            else
            {
                for (int j = 0; j < d.fields.Length; j++) Row(doc, d.fields[j], 2, rowsY + j * 15, 151, d.labelWidth, true);
            }

            float y = rowsEnd + 3f;
            if (d.stamp)
            {
                if (s.CanAct)
                {
                    bool can = s.CanSign;
                    Px.Button(doc, s.data.stampOk, 46, y, 62, 18, can ? paper : Pal.Hex("#cfc6b4"), can ? Pal.TealDark : Pal.PaperLabel, can ? Pal.TealDark : Pal.PaperLabel, () => s.Finish("signed"));
                    Px.Button(doc, s.data.stampNo, 112, y, 38, 18, paper, Pal.Red, Pal.Red, () => s.Finish("reject"));
                }
                else
                {
                    string t = "未處理";
                    if (s.result == "signed") t = "已簽約";
                    else if (s.result == "reject") t = "已退回";
                    else if (s.result == "hold") t = "暫緩";
                    else if (s.result == "leave") t = "客戶取消";
                    else if (s.result == "deal") t = "已成交";
                    Stamp(doc, t, s.result == "signed" || s.result == "deal" ? Pal.TealDark : Pal.Red, y, paper);
                }
                stampRect = Px.Rect(doc, "StampArea", 2, y - 1, 151, 20);
            }
            else if (rec)
            {
                string docId = d.id;
                if (s.recommendDone)
                {
                    if (s.data.recommend.correct == d.id) Stamp(doc, "已推薦", Pal.TealDark, y, paper);
                }
                else if (s.CanAct)
                {
                    Px.Button(doc, "推薦給客戶", 4, y, 147, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => s.Recommend(docId));
                }
                stampRect = Px.Rect(doc, "StampArea", 2, y - 1, 151, 20);
            }
        }

        void Stamp(RectTransform doc, string t, Color32 c, float y, Color32 paper)
        {
            float w = Px.TextWidth(t) + 12;
            Px.Box(doc, "Stamp", 150 - w, y, w, 18, paper, c);
            Px.Label(doc, t, 150 - w, y + 2, w, 14, c, TextAnchor.UpperCenter, false);
        }

        void Row(RectTransform doc, FieldData f, float x, float y, float w, float lw, bool clickable)
        {
            string fid = f.id;
            int si = s.sel.IndexOf(fid);
            bool isFixed = s.IsFixed(fid);
            RectTransform row = Px.Rect(doc, "Row_" + fid, x, y, w, 15);
            Image bg = row.gameObject.AddComponent<Image>();
            bg.color = si >= 0 ? (Color32)Pal.Orange : Pal.Clear;
            bg.raycastTarget = clickable;
            if (clickable)
            {
                Clickable c = row.gameObject.AddComponent<Clickable>();
                c.onClick = () => s.Pick(fid);
            }
            Px.Tiled(row, 0, 14, w, 1, PixelSprites.DotH, Pal.PaperDot);
            Px.Label(row, f.label, 2, 0, lw, 14, si >= 0 ? Pal.Ink : Pal.PaperLabel);
            Color32 vc = si >= 0 ? Pal.Ink : (isFixed ? Pal.TealDark : Pal.PaperInk);
            Px.Label(row, s.ValueOf(f), 2 + lw, 0, w - 4 - lw, 14, vc);
            if (isFixed && si < 0) Px.Fill(row, w - 8, 5, 5, 5, Pal.TealDark);
            if (si >= 0)
            {
                Px.Fill(row, w - 12, 1, 11, 13, Pal.Ink);
                Px.Label(row, si == 0 ? "A" : "B", w - 12, 0, 11, 14, Pal.Orange, TextAnchor.UpperCenter, false);
            }
            fieldRects[fid] = row;
        }

        void InfoRow(RectTransform doc, string id, string label, string value, float y, float lw)
        {
            RectTransform row = Px.Rect(doc, "Row_" + id, 2, y, 151, 15);
            Px.Tiled(row, 0, 14, 151, 1, PixelSprites.DotH, Pal.PaperDot);
            Px.Label(row, label, 2, 0, lw, 14, Pal.PaperLabel);
            Px.Label(row, value, 2 + lw, 0, 147 - lw, 14, Pal.PaperInk);
            fieldRects[id] = row;
        }

        // 議價單：從幾個固定的出價挑一個，按下去就送出
        void BuildOffer(int slot, DocumentData d)
        {
            OfferData o = s.data.offer;
            Color32 paper = Pal.Hex(d.paper);
            RectTransform doc = DocFrame(slot, d, 160);
            if (o == null) return;
            InfoRow(doc, "offer_item", "項目", o.label, 16, 56);
            InfoRow(doc, "offer_ask", "對方開價", GameState.Money(s.offerAsk), 31, 56);

            RectTransform vbox = Px.Box(doc, "Value", 4, 49, 147, 18, Pal.Cream, Pal.Ink);
            Text vtext = Px.Label(vbox, s.offerDone ? "成交 " + GameState.Money(s.offerFinal) : "選一個出價", 0, 2, 147, 14,
                s.offerDone ? Pal.TealDark : Pal.PaperLabel, TextAnchor.UpperCenter, false);
            fieldRects["offer_value"] = vbox;

            if (!s.offerDone && s.CanAct)
            {
                // 成功率平常不顯示：滑鼠移到出價按鈕上（手機是按住）才看得到
                bool mobile = Px.Mobile;
                string idle = mobile ? "按住看成功率，滑開取消" : "滑鼠移到按鈕上看成功率";
                Text rateText = Px.Label(doc, idle, 5, 70, 145, 14, Pal.PaperLabel);
                Image barBg = Px.Fill(doc, 46, 74, 66, 6, Pal.Hex("#cfc6b4"));
                Image bar = Px.Fill(doc, 46, 74, 0, 6, Pal.TealDark);
                barBg.gameObject.SetActive(false);
                bar.gameObject.SetActive(false);
                Text pctText = Px.Label(doc, "", 112, 70, 38, 14, Pal.TealDark, TextAnchor.UpperRight, false);

                for (int i = 0; i < CaseSession.OfferOptionCount; i++)
                {
                    int k = i;
                    RectTransform b = Px.Button(doc, s.OfferOptionLabel(i), 4 + i * 37, 88, 35, 24,
                        i == 0 ? Pal.PlumLight : Pal.PlumLine, Pal.Cream, Pal.Ink, () => s.OfferPick(k));
                    Hover h = b.gameObject.AddComponent<Hover>();
                    h.onHover = on =>
                    {
                        if (on)
                        {
                            long v = s.OfferOption(k);
                            int pct = s.OfferChance(v);
                            Color32 pc = pct >= 70 ? Pal.TealDark : (pct >= 30 ? Pal.Hex("#b0601e") : Pal.Red);
                            vtext.text = Px.Fit(s.OfferOptionLabel(k) + "：" + GameState.Money(v), 147);
                            vtext.color = Pal.PaperInk;
                            rateText.text = "成功率";
                            rateText.color = Pal.PaperLabel;
                            barBg.gameObject.SetActive(true);
                            bar.gameObject.SetActive(true);
                            bar.rectTransform.sizeDelta = new Vector2(Mathf.Round(66 * pct / 100f), 6);
                            bar.color = pc;
                            pctText.text = pct + "%";
                            pctText.color = pc;
                        }
                        else
                        {
                            vtext.text = "選一個出價";
                            vtext.color = Pal.PaperLabel;
                            rateText.text = idle;
                            barBg.gameObject.SetActive(false);
                            bar.gameObject.SetActive(false);
                            pctText.text = "";
                        }
                    };
                }
                stampRect = Px.Rect(doc, "OfferArea", 2, 68, 151, 46);
                Px.Label(doc, "按下去就出價。沒談成會被還價，耐心 -1。", 5, 115, 145, 28, Pal.PaperLabel, TextAnchor.UpperLeft, true);

                string chip;
                if (s.FoundGood > 0) chip = "籌碼：已找到 " + s.FoundGood + " 個問題";
                else if (s.TotalGood > 0) chip = "找出問題能提高成功率";
                else chip = "出價越貪心，成功率越低";
                Px.Label(doc, chip, 5, 143, 145, 14, s.FoundGood > 0 ? Pal.TealDark : Pal.PaperLabel);
            }
            else
            {
                Stamp(doc, s.offerDone ? "已成交" : "未成交", s.offerDone ? Pal.TealDark : Pal.Red, 91, paper);
            }
        }

        // 財務報表：由公司狀態即時產生
        void BuildFinance(int slot, DocumentData d)
        {
            GameState g = s.state;
            RectTransform doc = DocFrame(slot, d, 140);
            long profit = g.revenueToday - g.costToday;
            InfoRow(doc, "fin_cash", "現金", GameState.Money(g.cash), 16, 56);
            InfoRow(doc, "fin_recv", "應收款", GameState.Money(g.receivable), 31, 56);
            InfoRow(doc, "fin_rev", "今日收入", GameState.Money(g.revenueToday), 46, 56);
            InfoRow(doc, "fin_cost", "今日成本", GameState.Money(g.costToday), 61, 56);
            InfoRow(doc, "fin_profit", "今日利潤", GameState.Money(profit), 76, 56);
            InfoRow(doc, "fin_inv", "投資資產", GameState.Money(g.InvestmentTotal), 91, 56);
            InfoRow(doc, "fin_stock", "庫存", GameState.Money(g.InventoryValue), 106, 56);
            InfoRow(doc, "fin_debt", "負債", GameState.Money(g.debt), 121, 56);
        }

        // 企劃單：列出需求，按鈕打開組裝畫面
        void BuildPlanDoc(int slot, DocumentData d)
        {
            PlanData p = s.data.plan;
            Color32 paper = Pal.Hex(d.paper);
            int n = p != null ? p.reqs.Length : 0;
            float rowsEnd = 16 + n * 15;
            RectTransform doc = DocFrame(slot, d, rowsEnd + 25);
            for (int i = 0; i < n; i++) InfoRow(doc, "plan_req" + i, "需求" + (i + 1), p.reqs[i].text, 16 + i * 15, 40);
            float y = rowsEnd + 3;
            if (p == null) return;
            if (s.planDone) Stamp(doc, "企劃通過", Pal.TealDark, y, paper);
            else if (s.CanAct) Px.Button(doc, "開始組裝企劃", 4, y, 147, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => s.PlanOpen());
            stampRect = Px.Rect(doc, "PlanArea", 2, y - 1, 151, 20);
        }

        string LogText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int start = Mathf.Max(0, s.log.Count - 8);
            for (int i = start; i < s.log.Count; i++)
            {
                LogLine l = s.log[i];
                string hex = l.who == "你" ? "1f6f66" : (l.who == s.data.client.name ? "8a3f12" : "7a5a00");
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<color=#").Append(hex).Append('>').Append(l.who).Append("：</color>").Append(l.text);
            }
            return sb.ToString();
        }

        // ---------- 兩個欄位之間的連線與「提出質疑」 ----------

        void RefreshLink()
        {
            Px.Clear(linkLayer);
            if (s.sel.Count != 2 || !s.CanAct) return;
            RectTransform ra, rb;
            if (!fieldRects.TryGetValue(s.sel[0], out ra) || !fieldRects.TryGetValue(s.sel[1], out rb)) return;
            int ia = s.SlotOf(s.DocOf(s.sel[0]));
            int ib = s.SlotOf(s.DocOf(s.sel[1]));
            if (ia < 0 || ib < 0) return;

            Rect a = Px.ToRoot(rt, ra);
            Rect b = Px.ToRoot(rt, rb);
            float ya = Mathf.Round(a.y + a.height * 0.5f);
            float yb = Mathf.Round(b.y + b.height * 0.5f);
            bool right = ia < ib;
            // 連線只走文件之間的縫和文件下方的空白，不會壓在中間那份文件的字上
            float gapA = right ? SlotX[ia] + SlotW + 2 : SlotX[ia] - 2;
            float gapB = right ? SlotX[ib] - 2 : SlotX[ib] + SlotW + 2;
            float edgeA = right ? a.x + a.width : a.x;
            float edgeB = right ? b.x : b.x + b.width;
            float mx;
            if (ia == ib)
            {
                // 同一份文件：從左邊的縫繞過去
                gapB = gapA;
                edgeB = b.x;
            }

            LinkH(edgeA, gapA, ya);
            if (Mathf.Abs(ia - ib) <= 1)
            {
                LinkV(gapA, ya, yb);
                mx = gapA;
            }
            else
            {
                float yLow = SlotY + SlotH + 4;
                LinkV(gapA, ya, yLow);
                LinkH(gapA, gapB, yLow);
                LinkV(gapB, yLow, yb);
                mx = Mathf.Round((gapA + gapB) * 0.5f);
            }
            LinkH(gapB, edgeB, yb);
            LinkDot(edgeA, ya);
            LinkDot(edgeB, yb);

            float py;
            if (Mathf.Abs(ia - ib) <= 1)
            {
                py = Mathf.Max(ya, yb) + 10f;
                if (py + 24f > 300f) py = Mathf.Min(ya, yb) - 34f;
                py = Mathf.Max(SlotY, py);
            }
            else py = SlotY + SlotH - 8f;
            float px = Mathf.Clamp(mx - 53f, 4f, 636f - 106f);
            RectTransform pill = Px.Box(linkLayer, "Ask", px, py, 106, 24, Pal.Ink, Pal.Orange);
            pill.GetComponent<Image>().raycastTarget = true;
            Px.Button(pill, "提出質疑", 3, 4, 62, 16, Pal.Orange, Pal.Ink, Pal.Orange, () => s.Challenge());
            Px.Button(pill, "取消", 68, 4, 35, 16, Pal.PlumLine, Pal.Cream, Pal.PlumLine, () => s.ClearSel());
            fieldRects["link_pill"] = pill;
        }

        // 連線：深色底再疊一條粗的橘色虛線，淺色紙上和深色背景上都看得清楚
        void LinkH(float x0, float x1, float y)
        {
            float a = Mathf.Min(x0, x1), w = Mathf.Abs(x1 - x0);
            if (w < 1f) return;
            Px.Fill(linkLayer, a, y - 2, w, 4, Pal.Ink);
            Px.Tiled(linkLayer, a, y - 1, w, 2, PixelSprites.DashH, Pal.Orange);
        }

        void LinkV(float x, float y0, float y1)
        {
            float a = Mathf.Min(y0, y1), h = Mathf.Abs(y1 - y0) + 1;
            Px.Fill(linkLayer, x - 2, a - 2, 4, h + 4, Pal.Ink);
            Px.Tiled(linkLayer, x - 1, a, 2, h, PixelSprites.DashV, Pal.Orange);
        }

        void LinkDot(float x, float y)
        {
            Px.Fill(linkLayer, x - 4, y - 4, 8, 8, Pal.Ink);
            Px.Fill(linkLayer, x - 3, y - 3, 6, 6, Pal.Orange);
        }

        // ---------- 資料倉庫 ----------

        void RefreshHotbar()
        {
            Px.Clear(hotbarLayer);
            itemRects.Clear();
            CaseData d = s.data;
            capText.text = "容量 " + s.storage.Count + " / " + d.storageUnlocked;
            for (int i = 0; i < d.storageTotal; i++)
            {
                float x = 104 + i * 52;
                if (i < s.storage.Count)
                {
                    string id = s.storage[i];
                    DocumentData doc = s.Doc(id);
                    if (doc == null) continue;
                    bool inUse = s.SlotOf(id) >= 0;
                    RectTransform slot = Px.Rect(hotbarLayer, "Item_" + id, x, 308, 48, 48);
                    Image bg = slot.gameObject.AddComponent<Image>();
                    bg.color = Pal.Night;
                    bg.raycastTarget = true;
                    if (inUse) Px.DashedRect(slot, 0, 0, 48, 48, Pal.Orange);
                    else Px.Frame(slot, 0, 0, 48, 48, Pal.PlumLine);
                    IconArt.Draw(slot, doc.icon, inUse ? 0.35f : 1f);
                    Px.Label(slot, doc.tray, 0, 33, 48, 14, inUse ? Pal.Orange : Pal.Cream, TextAnchor.UpperCenter, false);
                    DragSource ds = slot.gameObject.AddComponent<DragSource>();
                    // 點一下放進檢視區；已經在檢視區的，再點一下就收回
                    ds.onClick = () => { if (s.SlotOf(id) >= 0) s.Close(id); else s.Open(id); };
                    ds.onBegin = e => BeginDrag(id, e);
                    ds.onDrag = e => MoveDrag(e);
                    ds.onEnd = e => EndDrag(e);
                    itemRects[id] = slot;
                }
                else if (i < d.storageUnlocked)
                {
                    Px.DashedRect(hotbarLayer, x, 308, 48, 48, Pal.PlumLight);
                    Px.Fill(hotbarLayer, x + 17, 308 + 23, 14, 2, Pal.PlumLight);
                    Px.Fill(hotbarLayer, x + 23, 308 + 17, 2, 14, Pal.PlumLight);
                }
                else
                {
                    Px.Box(hotbarLayer, "Locked", x, 308, 48, 48, Pal.TrayBg, Pal.Plum);
                    Px.Fill(hotbarLayer, x + 18, 308 + 12, 12, 2, Pal.PlumLight);
                    Px.Fill(hotbarLayer, x + 18, 308 + 12, 2, 10, Pal.PlumLight);
                    Px.Fill(hotbarLayer, x + 28, 308 + 12, 2, 10, Pal.PlumLight);
                    Px.Fill(hotbarLayer, x + 15, 308 + 21, 18, 14, Pal.PlumLight);
                }
            }
        }

        int SlotAt(Vector2 p)
        {
            if (p.y < SlotY || p.y > SlotY + SlotH) return -1;
            for (int i = 0; i < SlotX.Length; i++)
                if (p.x >= SlotX[i] && p.x <= SlotX[i] + SlotW) return i;
            return -1;
        }

        void BeginDrag(string id, PointerEventData e)
        {
            DocumentData doc = s.Doc(id);
            if (doc == null) return;
            dragId = id;
            Px.Clear(ghost);
            Px.Fill(ghost, 0, 0, 48, 48, Pal.Night);
            Px.Frame(ghost, 0, 0, 48, 48, Pal.Orange);
            IconArt.Draw(ghost, doc.icon, 1f);
            Px.Label(ghost, doc.tray, 0, 33, 48, 14, Pal.Cream, TextAnchor.UpperCenter, false);
            ghost.gameObject.SetActive(true);
            dropHi.SetAsLastSibling();
            ghost.SetAsLastSibling();
            planLayer.SetAsLastSibling();
            overlayLayer.SetAsLastSibling();
            MoveDrag(e);
        }

        void MoveDrag(PointerEventData e)
        {
            if (dragId == null) return;
            Vector2 p = Px.ScreenToRoot(rt, e.position);
            Px.Move(ghost, Mathf.Round(p.x - 24f), Mathf.Round(p.y - 24f));
            int i = SlotAt(p);
            dropHi.gameObject.SetActive(i >= 0);
            if (i >= 0) Px.Move(dropHi, SlotX[i], SlotY);
        }

        void EndDrag(PointerEventData e)
        {
            string id = dragId;
            dragId = null;
            ghost.gameObject.SetActive(false);
            dropHi.gameObject.SetActive(false);
            if (id == null) return;
            int i = SlotAt(Px.ScreenToRoot(rt, e.position));
            if (i >= 0) s.Place(id, i);
        }

        // ---------- 通知 ----------

        void RefreshToast()
        {
            Px.Clear(toastLayer);
            if (s.toast == null) return;
            string k = s.toast.kind;
            Color32 c = k == "bad" ? Pal.Pink : (k == "good" ? Pal.TealLight : (k == "warn" || k == "msg" ? Pal.Yellow : Pal.Lav));
            string text = s.toast.who + "：" + s.toast.text;
            float w = Mathf.Min(624, Px.TextWidth(text) + 16);
            RectTransform box = Px.Box(toastLayer, "Toast", Mathf.Round((640 - w) * 0.5f), 284, w, 16, Pal.Ink, c);
            Px.MakeClickable(box, () => s.CloseToast());
            string hex = ColorUtility.ToHtmlStringRGB(c);
            Px.Label(box, "<color=#" + hex + ">" + s.toast.who + "：</color>" + s.toast.text, 8, 1, w - 12, 14, Pal.Cream);
        }

        // ---------- 企劃組裝 ----------

        void RefreshPlan()
        {
            Px.Clear(planLayer);
            PlanData p = s.data.plan;
            if (p == null || !s.planOpen || s.planDone || !s.CanAct) return;
            Image scrim = Px.Fill(planLayer, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 170));
            scrim.raycastTarget = true;
            Px.Fill(planLayer, 63, 33, 520, 300, Pal.Ink);
            RectTransform panel = Px.Box(planLayer, "PlanPanel", 60, 30, 520, 300, Pal.Plum, Pal.Orange);
            Px.Fill(panel, 1, 1, 518, 16, Pal.Ink);
            DocumentData pd = s.Doc(p.docId);
            Px.Label(panel, "企劃組裝：" + (pd != null ? pd.title : ""), 8, 2, 300, 14, Pal.Orange);
            Px.Button(panel, "關閉", 474, 2, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => s.PlanClose());

            float y = 24;
            for (int g = 0; g < p.groups.Length; g++)
            {
                PlanGroup grp = p.groups[g];
                Px.Label(panel, grp.name, 10, y, 200, 14, Pal.Lav);
                for (int j = 0; j < grp.options.Length && j < 3; j++)
                {
                    PlanOption opt = grp.options[j];
                    bool on = s.planPick[g] == j;
                    int gg = g, jj = j;
                    RectTransform b = Px.Box(panel, "Opt", 10 + j * 112, y + 15, 108, 30, on ? Pal.Orange : Pal.PlumDeep, on ? Pal.Ink : Pal.PlumLight);
                    Px.MakeClickable(b, () => s.PlanPick(gg, jj));
                    Px.Label(b, opt.text, 4, 1, 100, 14, on ? Pal.Ink : Pal.Cream);
                    Px.Label(b, opt.cost > 0 ? GameState.Money(opt.cost) : "免費", 4, 15, 100, 14, on ? Pal.Ink : Pal.Lav);
                }
                y += 52;
            }

            Px.Fill(panel, 354, 22, 1, 270, Pal.PlumLight);
            Px.Label(panel, "客戶需求", 362, 24, 150, 14, Pal.Lav);
            float ry = 42;
            foreach (PlanReq r in p.reqs)
            {
                bool ok = s.PlanReqOk(r) && s.PlanComplete();
                Px.Fill(panel, 362, ry + 3, 8, 8, ok ? Pal.TealLight : Pal.PlumLine);
                Px.Label(panel, r.text, 374, ry, 140, 28, ok ? Pal.Cream : Pal.Lav, TextAnchor.UpperLeft, true);
                ry += 30;
            }
            Px.Label(panel, "總成本", 362, 228, 50, 14, Pal.Lav);
            Px.Label(panel, GameState.Money(s.PlanCost()), 410, 228, 104, 14, Pal.Cream);
            if (p.payDiff)
            {
                Px.Label(panel, "企劃費", 362, 244, 50, 14, Pal.Lav);
                Px.Label(panel, GameState.Money(s.PlanRewardNow()), 410, 244, 104, 14, Pal.TealLight);
            }
            Px.Button(panel, "送出企劃", 362, 266, 150, 22, Pal.Orange, Pal.Ink, Pal.Ink, () => s.PlanSubmit());
            fieldRects["plan_panel"] = panel;
        }

        // ---------- 文件詳細 ----------

        const float InfoLabelW = 26f;
        const float InfoValueW = 98f - 4f - InfoLabelW;

        /// <summary>這份文件有沒有字被截掉（標題、頭像旁的欄位、一般欄位）。</summary>
        bool NeedsDetail(DocumentData d)
        {
            switch (d.kind)
            {
                case "offer": case "finance": case "plan": case "roster": case "log": return false;
            }
            if (Px.TextWidth(d.title) > 113) return true;
            if (d.kind == "talent")
            {
                foreach (FieldData f in d.info) if (Px.TextWidth(s.ValueOf(f)) > InfoValueW + 1) return true;
                if (!string.IsNullOrEmpty(d.subTitle) && Px.TextWidth(d.subTitle) > 147) return true;
            }
            foreach (FieldData f in d.fields) if (Px.TextWidth(s.ValueOf(f)) > 148 - d.labelWidth) return true;
            return false;
        }

        /// <summary>文件詳細：每個欄位完整顯示，太長會自動換行。</summary>
        void RefreshDetail()
        {
            Px.Clear(detailLayer);
            if (detailDoc == null) return;
            DocumentData d = s.Doc(detailDoc);
            if (d == null)
            {
                detailDoc = null;
                return;
            }
            List<string[]> rows = new List<string[]>();
            foreach (FieldData f in d.info) rows.Add(new[] { f.label, s.ValueOf(f) });
            if (!string.IsNullOrEmpty(d.subTitle)) rows.Add(new[] { "", d.subTitle });
            foreach (FieldData f in d.fields) rows.Add(new[] { f.label, s.ValueOf(f) });

            const float W = 440f, LabelW = 84f;
            float valueW = W - 16 - LabelW;
            float[] hs = new float[rows.Count];
            float total = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                int lines = Mathf.Max(1, Mathf.CeilToInt(Px.TextWidth(rows[i][1]) / valueW));
                hs[i] = lines * 14 + 3;
                total += hs[i];
            }
            float h = Mathf.Min(330, 22 + total + 30);
            float x0 = Mathf.Floor((640 - W) * 0.5f);
            float y0 = Mathf.Floor((360 - h) * 0.5f);
            Image scrim = Px.Fill(detailLayer, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 170));
            scrim.raycastTarget = true;
            Px.MakeClickable((RectTransform)scrim.transform, () => { detailDoc = null; RefreshDetail(); });
            Color32 paper = Pal.Hex(d.paper);
            Px.Fill(detailLayer, x0 + 3, y0 + 3, W, h, Pal.Ink);
            RectTransform p = Px.Box(detailLayer, "DetailPanel", x0, y0, W, h, paper, Pal.Ink);
            p.GetComponent<Image>().raycastTarget = true;
            Px.Fill(p, 1, 1, W - 2, 16, Pal.Hex(d.head));
            Px.Label(p, d.title, 8, 2, W - 70, 14, Pal.PaperInk);
            float y = 22;
            for (int i = 0; i < rows.Count && y + hs[i] <= h - 28; i++)
            {
                bool section = rows[i][0] == "";
                if (!section) Px.Label(p, rows[i][0], 8, y, LabelW - 4, 14, Pal.PaperLabel);
                Px.Label(p, rows[i][1], section ? 8 : 8 + LabelW, y, section ? W - 16 : valueW, hs[i], section ? Pal.TealDark : Pal.PaperInk, TextAnchor.UpperLeft, true);
                Px.Tiled(p, 8, y + hs[i] - 2, W - 16, 1, PixelSprites.DotH, Pal.PaperDot);
                y += hs[i];
            }
            Px.Button(p, "關閉", W - 68, h - 24, 60, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => { detailDoc = null; RefreshDetail(); });
        }

        // ---------- 工商派人 ----------

        /// <summary>檢視區裡的「旗下藝人」：列出每個人那天有沒有空，按鈕打開選人面板。</summary>
        void BuildRosterDoc(int slot, DocumentData d)
        {
            AssignData a = s.data.assign;
            int n = a != null ? a.roster.Length : 0;
            float rowsEnd = 16 + 15 + n * 15;
            RectTransform doc = DocFrame(slot, d, rowsEnd + 25);
            if (a == null) return;
            string who = s.AssignedName;
            Px.Label(doc, a.open ? (who == null ? "對方沒指定，自己挑" : "派去：" + who) : (s.assigned == null ? "對方指定：" + a.wantedName : "換成：" + who),
                5, 16, 145, 14, a.open && who == null ? Pal.Red : Pal.TealDark);
            for (int i = 0; i < n; i++)
            {
                RosterEntry e = a.roster[i];
                float y = 31 + i * 15;
                Px.Tiled(doc, 2, y + 14, 151, 1, PixelSprites.DotH, Pal.PaperDot);
                bool on = e.id == s.assigned || (!a.open && s.assigned == null && e.id == a.wanted);
                if (on) Px.Fill(doc, 2, y + 1, 2, 12, Pal.TealDark);
                Px.Label(doc, e.name, 6, y, 68, 14, on ? Pal.TealDark : Pal.PaperInk);
                string tag = a.open ? e.fitText : (e.id == a.wanted ? "指定" : (e.swapFee > 0 ? "可換" : "不可"));
                Px.Label(doc, e.busy ? "有約" : tag, 74, y, 76, 14, e.busy ? Pal.Red : (a.open ? FitColor(e.fit) : Pal.PaperLabel), TextAnchor.UpperRight, false);
            }
            float by = rowsEnd + 3;
            if (s.CanAct)
                Px.Button(doc, a.open ? (who == null ? "選擇藝人" : "改派別人") : "提議換人", 4, by, 147, 18, Pal.Orange, Pal.Ink, Pal.Ink, () => s.RosterOpen());
            fieldRects["roster_doc"] = doc;
        }

        static Color32 FitColor(int fit)
        {
            return fit >= 3 ? Pal.TealDark : (fit == 2 ? Pal.PaperInk : (fit == 1 ? Pal.Hex("#b0601e") : Pal.Red));
        }

        /// <summary>選人面板：每位藝人的類型、粉絲、喜好、那天有沒有空，以及派他去的後果。</summary>
        void RefreshRoster()
        {
            Px.Clear(rosterLayer);
            AssignData a = s.data.assign;
            if (a == null || !s.rosterOpen || !s.CanAct) return;
            Image scrim = Px.Fill(rosterLayer, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 170));
            scrim.raycastTarget = true;
            Px.Fill(rosterLayer, 43, 23, 560, 318, Pal.Ink);
            RectTransform panel = Px.Box(rosterLayer, "RosterPanel", 40, 20, 560, 318, Pal.Plum, Pal.Orange);
            Px.Fill(panel, 1, 1, 558, 16, Pal.Ink);
            Px.Label(panel, a.open ? "選擇要派去的藝人" : "提議換人（對方指定" + a.wantedName + "）", 8, 2, 400, 14, Pal.Orange);
            Px.Button(panel, "關閉", 514, 2, 40, 14, Pal.PlumLine, Pal.Cream, Pal.Ink, () => s.RosterClose());
            string need = a.open
                ? "產品類別 " + a.category + "・希望類型 " + a.genre + "・粉絲至少 " + CaseGen.FansText(a.minFans) + "・直播 " + Calendar.Short(a.day)
                : "產品類別 " + a.category + "・直播 " + Calendar.Short(a.day) + "・粉絲比原本少，費用會照比例變少";
            Px.Label(panel, need, 8, 20, 544, 14, Pal.Lav);
            for (int i = 0; i < a.roster.Length && i < 5; i++)
            {
                RosterEntry e = a.roster[i];
                float y = 38 + i * 55;
                bool on = e.id == s.assigned || (!a.open && s.assigned == null && e.id == a.wanted);
                RectTransform card = Px.Box(panel, "R_" + e.id, 6, y, 548, 52, on ? Pal.PlumDeep : Pal.PlumLine, on ? Pal.TealLight : Pal.PlumLight);
                Px.Fill(card, 4, 4, 24, 28, Pal.VideoWall);
                PortraitArt.Draw(card, e.portrait, 4, 4, 1);
                Px.Label(card, e.name + "・" + e.genre + "・粉絲 " + CaseGen.FansText(e.fans), 34, 2, 300, 14, Pal.Cream);
                Px.Label(card, "喜歡 " + e.likes + "　討厭 " + e.dislikes, 34, 18, 300, 14, Pal.Lav);
                Px.Label(card, e.status, 34, 34, 300, 14, e.busy ? Pal.Pink : Pal.TealLight);
                Color32 fc = a.open ? (e.fit >= 3 ? Pal.TealLight : e.fit == 2 ? Pal.Cream : e.fit == 1 ? Pal.Yellow : Pal.Pink)
                    : (e.id == a.wanted ? Pal.Cream : e.swapFee > 0 ? Pal.Yellow : Pal.Pink);
                Px.Label(card, e.fitText, 340, 2, 200, 14, fc, TextAnchor.UpperRight, false);
                Px.Label(card, e.outcome, 340, 18, 200, 14, fc, TextAnchor.UpperRight, false);
                string btn = on ? "目前人選" : (a.open ? "派他去" : (e.id == a.wanted ? "換回原本的" : "提議換成他"));
                string id = e.id;
                if (on) Px.Label(card, btn, 440, 34, 100, 14, Pal.TealLight, TextAnchor.UpperRight, false);
                else Px.Button(card, btn, 440, 33, 100, 16, Pal.Orange, Pal.Ink, Pal.Ink, () => s.Assign(id));
            }
            fieldRects["roster_panel"] = panel;
        }

        /// <summary>簽約結果：派去的人、合不合適、有沒有撞期。</summary>
        void AddAssignLines(List<string> lines)
        {
            AssignData a = s.data.assign;
            RosterEntry e = s.RosterOf(s.assigned) ?? s.RosterOf(a.wanted);
            if (e == null) return;
            if (a.open) lines.Add("派去的人：" + e.name + "（" + e.fitText + "，" + e.outcome + "）");
            else if (s.assigned != null) lines.Add("換成" + e.name + "去直播");
            if (e.dislikes.Contains(a.category)) lines.Add("注意：" + e.name + "討厭" + a.category + "類工商");
            if (e.busy) lines.Add("注意：" + e.name + " " + e.status + "，可能會開天窗");
        }

        // ---------- 結果與結算 ----------

        void RefreshOverlay()
        {
            Px.Clear(overlayLayer);
            bool showDay = s.dayEnded;
            bool showResult = s.result != null && !s.overlayClosed;
            if (!showDay && !showResult) return;
            CaseData d = s.data;
            GameState g = s.state;
            string title;
            string button;
            System.Action act;
            List<string> lines = new List<string>();

            if (showDay)
            {
                title = "第 " + g.day + " 天結算";
                long profit = g.revenueToday - g.costToday;
                lines.Add("今日收入 " + GameState.Money(g.revenueToday));
                lines.Add("今日成本 " + GameState.Money(g.costToday));
                lines.Add("今日利潤 " + GameState.Money(profit));
                lines.Add("明天入帳的應收款 " + GameState.Money(g.receivable));
                lines.Add("下班時現金 " + GameState.Money(g.cash));
                button = "下班";
                act = () => game.CloseDay();
            }
            else
            {
                List<string> risks = s.Risks();
                switch (s.result)
                {
                    case "signed":
                        title = "已簽約：" + d.title;
                        lines.Add("合作金額 " + GameState.Money(s.Fee) + "，" + (string.IsNullOrEmpty(d.payNote) ? "記入應收款" : d.payNote));
                        if (s.TotalGood > 0) lines.Add("找到的問題 " + s.FoundGood + " / " + s.TotalGood);
                        foreach (string r in risks) lines.Add("未處理：" + r);
                        if (d.assign != null) AddAssignLines(lines);
                        else if (!string.IsNullOrEmpty(d.signedWarning)) lines.Add(d.signedWarning);
                        break;
                    case "deal":
                        title = string.IsNullOrEmpty(d.dealTitle) ? "成交：" + d.title : d.dealTitle;
                        AddDealLines(lines);
                        break;
                    case "reject":
                        title = "已退回：" + d.title;
                        lines.Add(d.client.name + "：" + d.replyReject);
                        break;
                    case "leave":
                        title = "客戶結束通話";
                        lines.Add(d.client.name + "的耐心用完了。");
                        lines.Add("提出質疑或出價前，先想清楚。");
                        break;
                    default:
                        title = "案件暫緩：" + d.title;
                        if (!string.IsNullOrEmpty(d.replyHold)) lines.Add(d.client.name + "：" + d.replyHold);
                        break;
                }
                button = "繼續";
                act = () => s.CloseOverlay();
            }

            Image scrim = Px.Fill(overlayLayer, 0, 0, 640, 360, Pal.Alpha(Pal.Ink, 184));
            scrim.raycastTarget = true;
            float h = 8 + 14 + 6 + lines.Count * 14 + 10 + 20 + 10;
            float y0 = Mathf.Floor((360 - h) * 0.5f);
            Px.Fill(overlayLayer, 143, y0 + 3, 360, h, Pal.Ink);
            RectTransform panel = Px.Box(overlayLayer, "Result", 140, y0, 360, h, Pal.Plum, Pal.Orange);
            Px.Label(panel, title, 12, 8, 336, 14, Pal.Orange);
            for (int i = 0; i < lines.Count; i++) Px.Label(panel, lines[i], 12, 28 + i * 14, 336, 14, Pal.Cream);
            Px.Button(panel, button, 12, h - 30, 72, 20, Pal.Orange, Pal.Ink, Pal.Ink, act);
            fieldRects["overlay_button"] = Px.Rect(panel, "BtnArea", 12, h - 30, 72, 20);
        }

        void AddDealLines(List<string> lines)
        {
            OfferData o = s.data.offer;
            GameState g = s.state;
            if (s.offerDone && o != null)
            {
                switch (o.apply)
                {
                    case "buy":
                        lines.Add("買進價 " + GameState.Money(s.offerFinal) + "，已放進庫存");
                        if (s.inspectCost > 0) lines.Add("檢測費 " + GameState.Money(s.inspectCost) + " 算進成本");
                        break;
                    case "sell":
                        lines.Add("賣出 " + GameState.Money(s.offerFinal));
                        lines.Add("這筆利潤 " + GameState.Money(g.lastProfit));
                        break;
                    case "settle":
                        lines.Add("和解金 " + GameState.Money(s.offerFinal) + "（原本求償 " + GameState.Money(o.ask) + "）");
                        break;
                    case "invest":
                        lines.Add("投資 " + GameState.Money(s.offerFinal) + "，記入投資資產");
                        break;
                    default:
                        lines.Add(o.label + " " + GameState.Money(s.offerFinal));
                        break;
                }
            }
            if (s.recommendDone && s.data.recommend != null) lines.Add("仲介佣金 " + GameState.Money(s.recommendReward) + "（應收款）");
            if (s.planDone && s.data.plan != null) lines.Add("企劃費 " + GameState.Money(s.planReward) + "（應收款）");
            if (lines.Count == 0) lines.Add("交易完成");
        }

        // ---------- 教學提示用的位置 ----------

        public bool TryFocus(string key, out Rect r)
        {
            r = new Rect();
            if (s == null || string.IsNullOrEmpty(key)) return false;
            switch (key)
            {
                case "contacts": r = new Rect(4, 4, 104, 124); return true;
                case "video": r = new Rect(112, 4, 316, 124); return true;
                case "bubble": r = new Rect(221, 24, 202, 62); return true;
                case "replies": r = new Rect(116, 87, 308, 18); return true;
                case "patience": r = new Rect(324, 107, 102, 18); return true;
                case "day": r = new Rect(432, 4, 204, 56); return true;
                case "tasks": r = new Rect(432, 64, 204, 67); return true;
                case "slots": r = new Rect(4, 136, 632, 164); return true;
                case "hotbar": r = new Rect(104, 308, 52 * s.data.storageTotal - 4, 48); return true;
                case "storage": r = new Rect(4, 308, 96, 48); return true;
                case "menu": r = new Rect(438, 40, 40, 16); return true;
                case "endday": r = new Rect(570, 40, 60, 16); return true;
            }
            RectTransform t = null;
            if (key.StartsWith("slot") && key.Length == 5)
            {
                int i = key[4] - '0';
                if (i >= 0 && i < 4) { r = new Rect(SlotX[i], SlotY, SlotW, SlotH); return true; }
            }
            if (key == "stamp") t = stampRect;
            else if (key.StartsWith("field:")) fieldRects.TryGetValue(key.Substring(6), out t);
            else if (key.StartsWith("item:")) itemRects.TryGetValue(key.Substring(5), out t);
            else if (key.StartsWith("doc:"))
            {
                string id = key.Substring(4);
                if (!docRects.TryGetValue(id, out t)) itemRects.TryGetValue(id, out t);
            }
            else fieldRects.TryGetValue(key, out t);
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            r = Px.ToRoot(rt, t);
            return true;
        }
    }
}
