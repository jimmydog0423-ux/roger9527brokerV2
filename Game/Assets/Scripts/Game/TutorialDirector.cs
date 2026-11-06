using System;

namespace SSD
{
    /// <summary>
    /// 依照 Resources/Story/prologue.json 一步一步執行序章：劇情、鬼東的教學、等待玩家操作。
    /// 只透過 GameRoot 的公開方法操作畫面。
    /// </summary>
    public class TutorialDirector
    {
        readonly GameRoot game;
        readonly ChapterData chapter;
        int ip = -1;
        string waitEv;
        string waitCond;
        bool running;
        string coach;
        Action onReturn;
        Action pendingAfter;

        public TutorialDirector(GameRoot game, ChapterData chapter)
        {
            this.game = game;
            this.chapter = chapter;
        }

        public bool Waiting
        {
            get { return waitEv != null || waitCond != null; }
        }

        public void StartLesson(int lesson)
        {
            ip = 0;
            for (int i = 0; i < chapter.steps.Length; i++)
            {
                StepData st = chapter.steps[i];
                if (st.type == "lesson" && st.lesson == lesson) { ip = i; break; }
            }
            Run();
        }

        /// <summary>從某個標記開始播一段劇情，播到 return 步驟時呼叫 done。找不到標記就直接呼叫 done。</summary>
        public void Play(string label, Action done)
        {
            int at = -1;
            for (int i = 0; i < chapter.steps.Length; i++)
                if (chapter.steps[i].type == "label" && chapter.steps[i].id == label) { at = i; break; }
            if (at < 0)
            {
                if (done != null) done();
                return;
            }
            waitEv = null;
            waitCond = null;
            coach = null;
            onReturn = done;
            ip = at + 1;
            Run();
        }

        /// <summary>現在是否在等某個事件（用來決定能不能結束營業等）。</summary>
        public bool WaitingFor(string ev)
        {
            return waitEv != null && Match(waitEv, ev);
        }

        public void OnEvent(string ev)
        {
            if (ev == "wrong" && !string.IsNullOrEmpty(coach) && game.Session != null)
                game.Session.Notify("鬼東", coach, "warn");
            if (!Waiting) return;
            if (waitEv != null && Match(waitEv, ev)) { Proceed(); return; }
            if (waitCond != null && CondOk(waitCond)) Proceed();
        }

        /// <summary>每一格畫面檢查一次狀態條件。</summary>
        public void Poll()
        {
            if (waitCond != null && !running && CondOk(waitCond)) Proceed();
        }

        void Proceed()
        {
            waitEv = null;
            waitCond = null;
            game.SetObjective(null, null);
            ip++;
            Run();
        }

        static bool Match(string pattern, string ev)
        {
            foreach (string p in pattern.Split('|'))
            {
                string q = p.Trim();
                if (q.Length == 0) continue;
                if (q.EndsWith(":") ? ev.StartsWith(q) : ev == q) return true;
            }
            return false;
        }

        bool CondOk(string cond)
        {
            if (cond == "pc") return game.InPc;
            if (cond == "office") return !game.InPc;
            if (game.Session != null) return game.Session.Check(cond);
            GameState s = game.State;
            foreach (string any in cond.Split('|'))
            {
                bool all = true;
                foreach (string part in any.Split('&'))
                {
                    string c = part.Trim();
                    bool neg = c.StartsWith("!");
                    if (neg) c = c.Substring(1);
                    bool v = c.StartsWith("flag:") && s.Has(c.Substring(5));
                    if (neg) v = !v;
                    if (!v) { all = false; break; }
                }
                if (all) return true;
            }
            return false;
        }

        int LabelIndex(string label)
        {
            for (int i = 0; i < chapter.steps.Length; i++)
                if (chapter.steps[i].type == "label" && chapter.steps[i].id == label) return i;
            UnityEngine.Debug.LogWarning("[水蛇洞] 找不到劇情標記：" + label);
            return ip + 1;
        }

        public string Fill(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            GameState s = game.State;
            CaseSession c = game.Session;
            text = text.Replace("{cash}", GameState.Money(s.cash));
            text = text.Replace("{day}", s.day.ToString());
            text = text.Replace("{profit}", GameState.Money(s.lastProfit));
            text = text.Replace("{settle}", GameState.Money(s.lastSettle));
            if (c != null)
            {
                text = text.Replace("{found}", c.FoundGood.ToString());
                text = text.Replace("{total}", c.TotalGood.ToString());
                text = text.Replace("{fee}", GameState.Money(c.Fee));
                text = text.Replace("{wrong}", c.wrong.ToString());
                text = text.Replace("{offer}", GameState.Money(c.offerFinal));
                text = text.Replace("{missed}", c.MissedNames());
            }
            return text;
        }

        void Run()
        {
            if (running) return;
            running = true;
            Action after = null;
            try
            {
                while (ip >= 0 && ip < chapter.steps.Length)
                {
                    if (!Exec(chapter.steps[ip])) break;
                }
                after = pendingAfter;
                pendingAfter = null;
            }
            finally
            {
                running = false;
            }
            if (after != null) after();
        }

        void Next()
        {
            ip++;
            Run();
        }

        /// <summary>執行一個步驟。回傳 true 表示可以直接繼續下一步。</summary>
        bool Exec(StepData st)
        {
            switch (st.type)
            {
                case "lesson":
                    game.BeginLesson(st.lesson, st.text);
                    ip++;
                    return true;
                case "scene":
                    game.SetStory(st.bg);
                    ip++;
                    return true;
                case "say":
                    game.Say(st.who, st.name, Fill(st.text), st.focus, Next);
                    return false;
                case "card":
                    game.Card(Fill(st.text), Fill(st.hint), Next);
                    return false;
                case "case":
                    coach = null;
                    game.StartCase(st.id);
                    ip++;
                    return true;
                case "wait":
                    waitEv = string.IsNullOrEmpty(st.ev) ? null : st.ev;
                    waitCond = string.IsNullOrEmpty(st.cond) ? null : st.cond;
                    if (waitEv == null && waitCond == null) { ip++; return true; }
                    if (waitCond != null && CondOk(waitCond))
                    {
                        waitEv = null;
                        waitCond = null;
                        ip++;
                        return true;
                    }
                    game.SetObjective(st.hint, st.focus);   // 代號由 GameRoot.Objective 即時替換
                    return false;
                case "coach":
                    coach = st.text;
                    ip++;
                    return true;
                case "branch":
                    ip = CondOk(st.cond) ? LabelIndex(st.go) : ip + 1;
                    return true;
                case "goto":
                    ip = LabelIndex(st.go);
                    return true;
                case "flag":
                    game.State.Set(st.id);
                    ip++;
                    return true;
                case "office":
                    game.ForceOffice();
                    ip++;
                    return true;
                case "cash":
                    game.State.Spend(st.amount);
                    ip++;
                    return true;
                case "earn":
                    game.State.Accrue(st.amount);
                    ip++;
                    return true;
                case "item":
                    game.State.inventory.Add(new InvItem { id = st.id, name = st.text, condition = st.hint, cost = st.amount });
                    ip++;
                    return true;
                case "end":
                    ip = chapter.steps.Length;
                    game.EndChapter();
                    return false;
                case "return":
                    ip = chapter.steps.Length;
                    pendingAfter = onReturn;
                    onReturn = null;
                    return false;
                default:
                    ip++;
                    return true;
            }
        }
    }
}
