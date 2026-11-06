using System;
using UnityEngine;

namespace SSD
{
    /// <summary>
    /// 教學與劇情腳本的一個步驟。所有欄位都可以省略，依 type 使用需要的欄位。
    ///
    /// type 一覽：
    ///   lesson  記錄目前第幾課並自動存檔（lesson、text = 課名）
    ///   scene   切換背景：black（全黑）、office（辦公室）、none（取消劇情背景）
    ///   say     角色說話（who = 頭像、name = 名字、text、focus = 要指的地方）
    ///   card    畫面中央的標題卡（text、hint = 副標）
    ///   case    開始一個案件，辦公室電腦會響（id = 案件檔名）
    ///   wait    等玩家做到某件事（ev = 事件、cond = 狀態條件、hint = 目前任務、focus）
    ///   coach   設定玩家質疑錯誤時鬼東的提醒（text）
    ///   branch  條件成立就跳到 label（cond、go）
    ///   goto    直接跳到 label（go）
    ///   label   標記位置（id）
    ///   flag    設定旗標（id）
    ///   office  強制回到辦公室畫面
    ///   cash    公司付出一筆錢（amount）
    ///   earn    記一筆應收款，下班入帳（amount）
    ///   item    放一件商品進庫存（id、text = 品名、hint = 狀況、amount = 成本）
    ///   end     序章結束，回到標題
    /// </summary>
    [Serializable]
    public class StepData
    {
        public string type;
        public string who;
        public string name;
        public string text;
        public string hint;
        public string focus;
        public string ev;
        public string cond;
        public string bg;
        public string id;
        public string go;
        public int lesson;
        public long amount;
    }

    [Serializable]
    public class ChapterData
    {
        public string title;
        public StepData[] steps;

        public static ChapterData Load(string resourcePath)
        {
            TextAsset ta = Resources.Load<TextAsset>(resourcePath);
            if (ta == null)
            {
                LoadReport.Add("找不到劇情腳本 Resources/" + resourcePath + ".json");
                return null;
            }
            try
            {
                ChapterData c = JsonUtility.FromJson<ChapterData>(ta.text);
                if (c == null) throw new System.Exception("內容是空的");
                if (c.steps == null || c.steps.Length == 0) throw new System.Exception("沒有任何步驟");
                return c;
            }
            catch (System.Exception e)
            {
                LoadReport.Add("劇情腳本格式錯誤 Resources/" + resourcePath + ".json：" + LoadReport.Brief(e.Message));
                return null;
            }
        }
    }
}
