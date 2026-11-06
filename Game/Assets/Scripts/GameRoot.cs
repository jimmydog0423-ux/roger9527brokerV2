using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 遊戲入口。按下 Play（或打開 exe）時自動產生，不需要在場景裡放任何東西。
    /// 負責：畫面切換（標題、辦公室、電腦）、放大動畫、案件流程、序章教學、存檔。
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public const string ChapterPath = "Story/prologue";
        public const string CareerPath = "Story/career";

        PixelScaler scaler;
        RectTransform root;
        OfficeView office;
        PcView pc;
        DialogueView dialogue;
        TitleView title;
        MenuViews menus;
        DesktopView desktop;
        NightView night;

        WorldData world;
        ChapterData careerStory;
        Career career;
        Opportunity currentOp;

        GameState state = new GameState();
        CaseSession session;
        ChapterData chapter;
        TutorialDirector director;
        readonly Queue<string> events = new Queue<string>();
        string objective;
        string objectiveFocus;
        string lessonTitle;
        bool busy;
        bool inPc;
        bool answered;

        public GameState State { get { return state; } }
        public CaseSession Session { get { return session; } }
        public bool InPc { get { return inPc; } }
        /// <summary>目前目標。提示裡的 {found}/{total} 這類代號每次讀取時才換成數字，找到問題時會跟著更新。</summary>
        public string Objective
        {
            get
            {
                if (objective != null && objective.IndexOf('{') >= 0 && director != null) return director.Fill(objective);
                return objective;
            }
        }
        public Career Career { get { return career; } }
        public WorldData World { get { return world; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameRoot>() != null) return;
            new GameObject("GameRoot").AddComponent<GameRoot>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            LoadFont();
            chapter = ChapterData.Load(ChapterPath);
            world = WorldData.Load();
            careerStory = ChapterData.Load(CareerPath);
            BuildCamera();
            BuildEventSystem();
            BuildCanvas();
            office = OfficeView.Build(root, this);
            pc = PcView.Build(root, this);
            desktop = DesktopView.Build(root, this);
            night = NightView.Build(root, this);
            dialogue = DialogueView.Build(root, this);
            title = TitleView.Build(root, this);
            menus = MenuViews.Build(root, this);
            office.Bind(state);
            SetPcVisible(0f);
            Settings.ApplyDisplay();
            title.Show();
            if (LoadReport.Problems.Count > 0)
                menus.Message("資料讀取失敗", string.Join("\n", LoadReport.Problems.ToArray()) + "\n請重新執行「輸出遊戲.bat」。", null);
        }

        void OnDestroy()
        {
            Font.textureRebuilt -= OnFontRebuilt;
        }

        void Update()
        {
            while (events.Count > 0)
            {
                string ev = events.Dequeue();
                if (director != null) director.OnEvent(ev);
                if (career != null && ev == "case_closed") OnCareerCaseClosed();
            }
            if (director != null && !dialogue.Busy) director.Poll();
            if (EscapePressed()) ToggleMenu();
        }

        static bool EscapePressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        // ---------- 建立基本物件 ----------

        void LoadFont()
        {
            Px.font = Resources.Load<Font>("Fonts/Cubic11");
            if (Px.font == null)
            {
                Debug.LogWarning("[水蛇洞] 找不到 Resources/Fonts/Cubic11.ttf，暫時改用系統字型。");
                Px.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei", "PingFang TC", "Arial" }, 12);
            }
            Font.textureRebuilt += OnFontRebuilt;
        }

        static void OnFontRebuilt(Font f)
        {
            if (f != Px.font || f.material == null || f.material.mainTexture == null) return;
            f.material.mainTexture.filterMode = FilterMode.Point;
        }

        void BuildCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Ink;
            cam.orthographic = true;
            cam.cullingMask = 0;
        }

        void BuildEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            Type inputSystemModule = null;
#if ENABLE_INPUT_SYSTEM
            inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
#endif
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
        }

        void BuildCanvas()
        {
            GameObject cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            GameObject rgo = new GameObject("Root640x360", typeof(RectTransform));
            root = (RectTransform)rgo.transform;
            root.SetParent(cgo.transform, false);
            rgo.AddComponent<RectMask2D>();

            scaler = cgo.AddComponent<PixelScaler>();
            scaler.root = root;
            scaler.integerScaling = Settings.IntegerScaling;
            scaler.Apply();
        }

        public void ApplyScaling()
        {
            scaler.integerScaling = Settings.IntegerScaling;
            scaler.Apply();
        }

        // ---------- 標題與存檔 ----------

        public void NewGame()
        {
            Action choose = () => menus.Choose("開新遊戲", "要先上鬼東的七堂課嗎？第一次玩建議從序章開始。",
                "從序章開始（新手教學）", () => StartFrom(new GameState(), 0),
                "跳過序章，直接開始營業", () => StartCareer(NewCareerState(), true));
            if (SaveSystem.Exists())
                menus.Confirm("開新遊戲會覆蓋現在的存檔，確定嗎？", () => choose(), null);
            else
                choose();
        }

        GameState NewCareerState()
        {
            GameState s = new GameState();
            if (world != null) Career.Init(s, world, true);
            return s;
        }

        public void ContinueGame()
        {
            GameState s = SaveSystem.Load();
            if (s == null)
            {
                if (SaveSystem.LastStatus == SaveStatus.Corrupt)
                {
                    string why = SaveSystem.LastError;
                    string bak = SaveSystem.BackupBroken();
                    title.Show();   // 先刷新標題（繼續遊戲會變灰），訊息才會疊在上面
                    menus.Message("存檔壞掉了", "存檔讀不出來（" + why + "）。" + (bak != null ? "\n壞掉的檔案已另存成 " + bak + "。" : "") + "\n請選「開新遊戲」重新開始。", null);
                }
                else menus.Message("繼續遊戲", "還沒有存檔，請選「開新遊戲」。", null);
                return;
            }
            if (world != null) Career.RefreshTalentRoster(s, world);
            if (s.tutorialDone)
            {
                if (!s.career && world != null) Career.Init(s, world, false);
                StartCareer(s, false);
                return;
            }
            StartFrom(s, s.lesson);
        }

        void StartFrom(GameState s, int lesson)
        {
            if (chapter == null)
            {
                menus.Message("錯誤", "序章劇情讀取失敗：" + LoadReport.Last + "\n請重新執行「輸出遊戲.bat」。", null);
                return;
            }
            state = s;
            career = null;
            currentOp = null;
            ClearCase();
            ResetView();
            title.Hide();
            dialogue.HideAll();
            events.Clear();
            director = new TutorialDirector(this, chapter);
            director.StartLesson(lesson);
        }

        public void BackToTitle()
        {
            director = null;
            career = null;
            currentOp = null;
            events.Clear();
            dialogue.HideAll();
            SetObjective(null, null);
            ClearCase();
            ResetView();
            title.Show();
        }

        public void OpenSettings()
        {
            menus.ShowSettings(null);
        }

        public void OpenMenu()
        {
            if (title.gameObject.activeSelf) return;
            if (career != null) lessonTitle = "第 " + state.day + " 天・" + Career.ActName(state.act);
            menus.ShowPause(lessonTitle);
        }

        public void Confirm(string text, Action yes)
        {
            menus.Confirm(text, () => { menus.Close(); if (yes != null) yes(); }, null);
        }

        float saveWarnAt = -999f;

        /// <summary>存檔；寫不進去時在畫面上方提醒，不會默默失敗。
        /// 用不擋操作的提示條，避免蓋掉正在等玩家按的對話框。</summary>
        public void SaveNow()
        {
            if (SaveSystem.Save(state)) return;
            if (Time.unscaledTime - saveWarnAt < 60f) return;
            saveWarnAt = Time.unscaledTime;
            Notice("存檔失敗：進度可能沒存到，請檢查硬碟空間");
        }

        /// <summary>畫面上方的提示條，8 秒後消失，不擋滑鼠。</summary>
        void Notice(string text)
        {
            RectTransform b = Px.Rect(root, "Notice", 150, 4, 340, 20);
            b.SetAsLastSibling();
            Px.Fill(b, 0, 0, 340, 20, Pal.Orange).raycastTarget = false;
            Px.Fill(b, 1, 1, 338, 18, Pal.Ink).raycastTarget = false;
            Px.Label(b, text, 6, 3, 328, 14, Pal.Orange, TextAnchor.MiddleCenter, false).raycastTarget = false;
            Destroy(b.gameObject, 8f);
        }

        void ToggleMenu()
        {
            if (menus.IsOpen) menus.Close();
            else OpenMenu();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void BeginLesson(int lesson, string name)
        {
            state.lesson = lesson;
            lessonTitle = name;
            SaveNow();
        }

        public void EndChapter()
        {
            state.tutorialDone = true;
            if (world == null || careerStory == null)
            {
                SaveNow();
                BackToTitle();
                menus.Message("序章完成", "恭喜完成鬼東的最後一堂課。", null);
                return;
            }
            Career.Init(state, world, false);
            SaveNow();
            StartCareer(state, false);
        }

        // ---------- 劇情與教學 ----------

        public void Say(string who, string name, string text, string focus, Action done)
        {
            dialogue.Say(who, name, text, focus, done);
        }

        public void Card(string text, string sub, Action done)
        {
            dialogue.Card(text, sub, done);
        }

        public void SetStory(string bg)
        {
            dialogue.SetStory(bg);
        }

        public void SetObjective(string text, string focus)
        {
            objective = string.IsNullOrEmpty(text) ? null : text;
            objectiveFocus = focus;
            office.SetObjective(Objective);
            dialogue.SetWaitFocus(focus);
            if (session != null) pc.Refresh();
        }

        public bool TryFocus(string key, out Rect r)
        {
            r = new Rect();
            if (string.IsNullOrEmpty(key)) return false;
            if (key.StartsWith("office_")) return !inPc && !busy && office.TryFocus(key, out r);
            if (!inPc) return false;
            return pc.TryFocus(key, out r);
        }

        // ---------- 案件 ----------

        public void StartCase(string id)
        {
            CaseData d = CaseData.Load("Cases/" + id);
            if (d == null)
            {
                menus.Message("錯誤", "案件資料讀取失敗：" + LoadReport.Last + "\n請重新執行「輸出遊戲.bat」。", null);
                return;
            }
            bool fromPc = inPc && !busy;
            ClearCase();
            session = new CaseSession(d, state);
            session.Changed += OnSessionChanged;
            session.OnEvent += OnCaseEvent;
            answered = false;
            if (!fromPc) ResetView();
            pc.Bind(session);
            office.SetCall(OfficeView.Call.Ringing, d);
            // 人在電腦前時，新的來電會讓畫面拉回桌前，看到螢幕上的來電視窗
            if (fromPc) StartCoroutine(ZoomOut(false));
        }

        void ClearCase()
        {
            if (session != null)
            {
                session.Changed -= OnSessionChanged;
                session.OnEvent -= OnCaseEvent;
            }
            session = null;
            office.SetCall(OfficeView.Call.None, null);
        }

        void OnSessionChanged()
        {
            pc.Refresh();
            office.Bind(state);
        }

        void OnCaseEvent(string ev)
        {
            events.Enqueue(ev);
        }

        void Emit(string ev)
        {
            events.Enqueue(ev);
        }

        public void ForceOffice()
        {
            if (inPc && !busy)
            {
                StartCoroutine(ZoomOut(true));
                return;
            }
            StopAllCoroutines();
            busy = false;
            inPc = false;
            SetPcVisible(0f);
            office.SetZoom(0f);
            UpdateOfficeCall();
        }

        void ResetView()
        {
            StopAllCoroutines();
            busy = false;
            inPc = false;
            SetPcVisible(0f);
            desktop.Show(false);
            night.Show(false);
            office.SetZoom(0f);
            office.Bind(state);
        }

        void UpdateOfficeCall()
        {
            if (session == null) { office.SetCall(OfficeView.Call.None, null); return; }
            office.SetCall(session.result == null ? OfficeView.Call.Active : OfficeView.Call.Ended, session.data);
        }

        void SetPcVisible(float alpha)
        {
            CanvasGroup g = pc.Group;
            g.alpha = alpha;
            g.blocksRaycasts = alpha >= 1f;
            g.interactable = alpha >= 1f;
        }

        public void Answer()
        {
            if (busy || inPc || session == null) return;
            StartCoroutine(ZoomIn());
        }

        public void Decline()
        {
            if (busy || inPc || session == null) return;
            office.SetCall(OfficeView.Call.Missed, session.data);
        }

        public void ExitPc()
        {
            if (busy || !inPc) return;
            StartCoroutine(ZoomOut(true));
        }

        public void RequestEndDay()
        {
            if (career != null)
            {
                if (session != null && currentOp != null)
                {
                    session.Notify("提示", "先把這通電話講完，再結束營業", "warn");
                    return;
                }
                if (busy || dialogue.Busy) return;
                int left = state.slots;
                if (left > 0 && career.VisibleInbox().Count > 0)
                    Confirm("還有 " + left + " 個通話時段沒用，確定要下班嗎？", DoEndDay);
                else
                    DoEndDay();
                return;
            }
            if (session == null) return;
            if (director != null && !director.WaitingFor("day_closed"))
            {
                session.Notify("鬼東", "還沒下班，先把手上的事做完。", "warn");
                return;
            }
            session.SetDayEnded(true);
        }

        public void CloseDay()
        {
            long collected = state.receivable;
            state.cash += collected;
            state.receivable = 0;
            state.revenueToday = 0;
            state.costToday = 0;
            state.day++;
            if (session != null) session.SetDayEnded(false);
            office.Bind(state);
            pc.Refresh();
            Emit("day_closed");
        }


        // ---------- 正式營業 ----------

        void StartCareer(GameState s, bool fresh)
        {
            if (world == null || careerStory == null)
            {
                menus.Message("錯誤", "正式營業的資料讀取失敗：" + LoadReport.Last + "\n請重新執行「輸出遊戲.bat」。", null);
                return;
            }
            state = s;
            career = new Career(state, world);
            director = new TutorialDirector(this, careerStory);
            currentOp = null;
            ClearCase();
            ResetView();
            title.Hide();
            dialogue.HideAll();
            events.Clear();
            SetObjective(null, null);
            BeginCareerDay();
        }

        void ShowOfficeScene()
        {
            StopAllCoroutines();
            busy = false;
            inPc = false;
            SetPcVisible(0f);
            desktop.Show(false);
            night.Show(false);
            office.SetZoom(0f);
            office.SetCall(OfficeView.Call.None, null);
            office.Bind(state);
        }

        void BeginCareerDay()
        {
            List<string> beats = career.StartDay();
            List<string> morning = new List<string>(state.morning);
            state.morning.Clear();
            SaveNow();
            ShowOfficeScene();
            PlayBeats(beats, 0, () =>
            {
                dialogue.SetStory("none");
                Card("第 " + state.day + " 天", Calendar.Long(state.day) + "　" + Calendar.Weekday(state.day), () =>
                    PlayLines(morning, 0, () => StartCoroutine(ZoomToDesktop())));
            });
        }

        void PlayLines(List<string> lines, int i, Action done)
        {
            if (career == null) return;
            if (i >= lines.Count) { done(); return; }
            Say("", "", lines[i], null, () => PlayLines(lines, i + 1, done));
        }

        // ---------- 晚上 ----------

        void ShowNight()
        {
            if (career == null) return;
            career.BeginNight();
            SaveNow();
            StopAllCoroutines();
            busy = false;
            SetPcVisible(0f);
            desktop.Show(false);
            night.Show(true);
            inPc = false;
        }

        public void NightVisit(string id)
        {
            if (career == null) return;
            NightPlace p = Career.Place(id);
            if (p == null || !career.Enter(p)) { night.Refresh(); return; }
            SaveNow();
            night.Refresh();
            if (id == "market" || id == "flea") night.ShowShop(id);
            else if (id == "junk") night.ShowJunk();
            else night.ShowScene(career.Scene(id), null);
        }

        public void GoToBed()
        {
            if (career == null) return;
            career.EndNight();
            SaveNow();
            night.Show(false);
            BeginCareerDay();
        }

        void PlayBeats(List<string> beats, int i, Action done)
        {
            if (career == null) return;
            if (i >= beats.Count) { done(); return; }
            director.Play(beats[i], () => PlayBeats(beats, i + 1, done));
        }

        IEnumerator ZoomToDesktop()
        {
            busy = true;
            office.Bind(state);
            const float dur = 0.6f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                office.SetZoom(Smooth(t / dur));
                yield return null;
            }
            office.SetZoom(1f);
            desktop.Refresh();
            for (int i = 1; i <= 4; i++)
            {
                desktop.SetAlpha(i / 4f);
                yield return new WaitForSecondsRealtime(0.04f);
            }
            desktop.Show(true);
            inPc = true;
            busy = false;
        }

        public void CallOpportunity(Opportunity op)
        {
            if (career == null || busy || session != null) return;
            string why;
            if (!career.CanCall(op, out why)) { desktop.Toast(why); return; }
            CaseData d = career.BuildCase(op);
            if (d == null)
            {
                state.inbox.Remove(op);
                desktop.Toast("這件委託已經失效了。");
                desktop.Refresh();
                return;
            }
            currentOp = op;
            session = new CaseSession(d, state);
            session.Changed += OnSessionChanged;
            session.OnEvent += OnCaseEvent;
            answered = true;
            pc.Bind(session);
            desktop.Show(false);
            SetPcVisible(1f);
            inPc = true;
        }

        public void DelegateOpportunity(Opportunity op)
        {
            if (career == null || busy || session != null) return;
            string why;
            if (!career.CanDelegate(op, out why)) { desktop.Toast(why); return; }
            string msg = career.Delegate(op);
            SaveNow();
            desktop.Toast(msg);
            desktop.Refresh();
        }

        void OnCareerCaseClosed()
        {
            if (career == null || session == null || currentOp == null) return;
            List<string> notes = career.ApplyOutcome(currentOp, session, false);
            currentOp = null;
            ClearCase();
            SetPcVisible(0f);
            SaveNow();
            desktop.Show(true);
            if (notes.Count > 0) desktop.Toast(string.Join("；", notes.ToArray()));
        }

        void DoEndDay()
        {
            if (career == null) return;
            DayReport rep = career.EndDay();
            SaveNow();
            desktop.Refresh();
            desktop.ShowReport(rep, () =>
            {
                if (rep.ending) PlayEnding();
                else ShowNight();
            });
        }

        void PlayEnding()
        {
            PrepareEndingFlags();
            career.ApplyEnding();
            SaveNow();
            ShowOfficeScene();
            director.Play("ending", () =>
            {
                dialogue.SetStory("black");
                string stats = "第 " + state.day + " 天達成・成交 " + (state.GetStat("deals") + state.GetStat("trades") + state.GetStat("brokers") + state.GetStat("plans"))
                    + " 件・糾紛 " + state.GetStat("disputes") + " 件・羅傑出包 " + state.GetStat("incidents") + " 次";
                Card("完", stats, () =>
                {
                    dialogue.HideAll();
                    SaveNow();
                    BackToTitle();
                    menus.Message("感謝遊玩", "羅傑和 V老大度完蜜月，回來上班了（錢沒還）。\n選「繼續遊戲」可以接著經營水蛇洞。", null);
                });
            });
        }

        void PrepareEndingFlags()
        {
            if (state.RelValue("wang") >= 2) state.Set("mem_wang");
            if (state.RelValue("zhou") >= 2) state.Set("mem_zhou");
            if (state.Talent("kui") != null) state.Set("mem_kui");
            if (state.talents.Count >= 3) state.Set("mem_talents");
            if (state.GetStat("incidents") >= 5) state.Set("mem_chaos");
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        IEnumerator ZoomIn()
        {
            busy = true;
            UpdateOfficeCall();
            const float dur = 0.7f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                office.SetZoom(Smooth(t / dur));
                yield return null;
            }
            office.SetZoom(1f);
            pc.Refresh();
            for (int i = 1; i <= 4; i++)
            {
                SetPcVisible(i / 4f);
                yield return new WaitForSecondsRealtime(0.04f);
            }
            inPc = true;
            busy = false;
            if (!answered)
            {
                answered = true;
                Emit("answered");
            }
            Emit("enter_pc");
        }

        IEnumerator ZoomOut(bool showCallState)
        {
            busy = true;
            SetPcVisible(0f);
            if (showCallState) UpdateOfficeCall();
            const float dur = 0.6f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                office.SetZoom(1f - Smooth(t / dur));
                yield return null;
            }
            office.SetZoom(0f);
            inPc = false;
            busy = false;
            Emit("exit_pc");
        }
    }
}
