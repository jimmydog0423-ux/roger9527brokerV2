using System.IO;
using UnityEngine;

namespace SSD
{
    /// <summary>
    /// 存檔放在 Application.persistentDataPath（Windows：C:\Users\你\AppData\LocalLow\公司\水蛇洞經紀人）。
    /// 序章在每一課開頭自動存檔。
    /// </summary>
    public enum SaveStatus { Ok, Missing, Corrupt }

    /// <summary>啟動時讀取資料檔的問題，標題畫面會一次告訴玩家。</summary>
    public static class LoadReport
    {
        public static readonly System.Collections.Generic.List<string> Problems = new System.Collections.Generic.List<string>();
        public static string Last { get { return Problems.Count > 0 ? Problems[Problems.Count - 1] : ""; } }

        /// <summary>錯誤訊息太長時只留前面一段。</summary>
        public static string Brief(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return "原因不明";
            msg = msg.Replace("\r", " ").Replace("\n", " ").Trim();
            return msg.Length > 48 ? msg.Substring(0, 48) + "…" : msg;
        }

        public static void Add(string text)
        {
            Debug.LogError("[水蛇洞] " + text);
            if (!Problems.Contains(text)) Problems.Add(text);
        }
    }

    public static class SaveSystem
    {
        public static string LastError = "";
        public static SaveStatus LastStatus = SaveStatus.Missing;
        static string PathOf
        {
            get { return Path.Combine(Application.persistentDataPath, "save1.json"); }
        }

        public static bool Exists()
        {
            return File.Exists(PathOf);
        }

        /// <summary>先寫到暫存檔再換掉正式存檔，寫到一半當機也不會把存檔弄壞。</summary>
        public static bool Save(GameState s)
        {
            try
            {
                string tmp = PathOf + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(s, true));
                if (File.Exists(PathOf)) File.Delete(PathOf);
                File.Move(tmp, PathOf);
                LastError = "";
                return true;
            }
            catch (System.Exception e)
            {
                LastError = LoadReport.Brief(e.Message);
                Debug.LogWarning("[水蛇洞] 存檔失敗：" + e.Message);
                return false;
            }
        }

        /// <summary>讀檔。失敗時 LastStatus 會說明是「沒有存檔」還是「存檔壞掉」。</summary>
        public static GameState Load()
        {
            if (!Exists())
            {
                LastStatus = SaveStatus.Missing;
                LastError = "";
                return null;
            }
            try
            {
                string text = File.ReadAllText(PathOf);
                GameState s = string.IsNullOrEmpty(text.Trim()) ? null : JsonUtility.FromJson<GameState>(text);
                if (s == null) throw new System.Exception("存檔是空的");
                if (s.day < 1) throw new System.Exception("存檔內容不完整");
                LastStatus = SaveStatus.Ok;
                LastError = "";
                return s;
            }
            catch (System.Exception e)
            {
                LastStatus = SaveStatus.Corrupt;
                LastError = LoadReport.Brief(e.Message);
                Debug.LogWarning("[水蛇洞] 讀檔失敗：" + e.Message);
                return null;
            }
        }

        /// <summary>把壞掉的存檔改名保留，回傳新的檔名。</summary>
        public static string BackupBroken()
        {
            try
            {
                string bak = Path.Combine(Application.persistentDataPath, "save1.broken.json");
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(PathOf, bak);
                return "save1.broken.json";
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[水蛇洞] 備份壞掉的存檔失敗：" + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (Exists()) File.Delete(PathOf);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[水蛇洞] 刪除存檔失敗：" + e.Message);
            }
        }
    }

    /// <summary>玩家設定，存在 PlayerPrefs。</summary>
    public static class Settings
    {
        public static bool Fullscreen
        {
            get { return PlayerPrefs.GetInt("ssd_fullscreen", 0) == 1; }
            set { PlayerPrefs.SetInt("ssd_fullscreen", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool IntegerScaling
        {
            get { return PlayerPrefs.GetInt("ssd_integer", 1) == 1; }
            set { PlayerPrefs.SetInt("ssd_integer", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>0 慢、1 中、2 快、3 立即。</summary>
        public static int TextSpeed
        {
            get { return PlayerPrefs.GetInt("ssd_textspeed", 1); }
            set { PlayerPrefs.SetInt("ssd_textspeed", value); PlayerPrefs.Save(); }
        }

        public static float CharsPerSecond
        {
            get
            {
                switch (TextSpeed)
                {
                    case 0: return 22f;
                    case 2: return 90f;
                    case 3: return 100000f;
                    default: return 45f;
                }
            }
        }

        public static void ApplyDisplay()
        {
            if (Application.isEditor) return;
#if UNITY_WEBGL
            Screen.fullScreen = Fullscreen;
#else
            Resolution r = Screen.currentResolution;
            if (Fullscreen)
            {
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
                return;
            }
            // 視窗大小依螢幕決定：盡量用 640x360 的整數倍，螢幕太小時改用剛好放得下的 16:9
            int k = Mathf.FloorToInt(Mathf.Min(r.width * 0.9f / 640f, r.height * 0.85f / 360f));
            int w, h;
            if (k >= 2)
            {
                w = 640 * k;
                h = 360 * k;
            }
            else
            {
                h = Mathf.Max(360, Mathf.RoundToInt(r.height * 0.85f));
                w = Mathf.RoundToInt(h * 16f / 9f);
                if (w > r.width * 0.95f)
                {
                    w = Mathf.RoundToInt(r.width * 0.95f);
                    h = Mathf.RoundToInt(w * 9f / 16f);
                }
            }
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
#endif
        }
    }
}
