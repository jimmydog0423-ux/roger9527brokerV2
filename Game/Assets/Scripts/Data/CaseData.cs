using System;
using UnityEngine;

namespace SSD
{
    // 案件資料結構（企劃書第十四章）。內容放在 Resources/Cases/*.json，
    // 新增案件只要多寫一個 JSON，不需要改程式。

    [Serializable]
    public class FieldData
    {
        public string id;
        public string label;
        public string value;
    }

    [Serializable]
    public class DocumentData
    {
        public string id;
        public string title;        // 文件標題
        public string tray;         // 倉庫格子上的短名稱（最多 3 個字）
        public string kind;         // form / talent / log / offer / finance / plan
        public string icon;         // 倉庫圖示
        public string paper;        // 紙張底色
        public string head;         // 標題列底色
        public string portrait;     // talent 文件的頭像角色 id
        public string subTitle;     // talent 文件下半部的小標題（預設「近期行程」）
        public int labelWidth;      // 欄位名稱寬度（邏輯像素）
        public bool stamp;          // 是否有簽約蓋章區
        public bool recommend;      // 仲介案件：可以「推薦給客戶」
        public FieldData[] info;    // talent 文件頭像旁的欄位
        public FieldData[] fields;  // 一般欄位
    }

    [Serializable]
    public class ContradictionData
    {
        public string key;          // 矛盾代號
        public string a;            // 欄位 A
        public string b;            // 欄位 B
        public string name;         // 問題名稱
        public string fixField;     // 質疑成功後要改的欄位
        public string fixValue;     // 改成什麼
        public int fee;             // 質疑成功後的新合作金額（0 = 不變）
        public int shift;           // 對議價底線的影響（正數 = 對玩家有利）
        public bool bad;            // true = 這其實是我方理虧的地方，拿出來會吃虧
        public string reply;        // 客戶回應
        public string risk;         // 沒抓到時的風險說明
        public string alt;          // 其他也能證明這個問題的欄位，用逗號分隔：「欄位」= 跟寫錯的欄位配對；「欄位~欄位」= 指定兩個欄位
    }

    [Serializable]
    public class ReplyData
    {
        public string id;
        public string text;         // 按鈕上的字（最多 7 個字）
        public string say;          // 你說出口的完整句子
        public string response;     // 客戶回應
        public string requires;     // 出現條件（例如 found:fee、reply:ask）
        public string result;       // 選了之後直接結束通話（hold / reject）
        public string flag;         // 設定旗標
        public string[] gives;      // 客戶傳來的文件
        public int patience;        // 耐心變化
        public int shift;           // 議價底線變化
        public int cost;            // 要花的錢（例如檢測費）
        public bool repeat;
    }

    [Serializable]
    public class OfferData
    {
        public string docId;        // 議價單文件 id
        public string mode;         // raise：我方希望高（賣、收費） / lower：我方希望低（買、賠償、投資）
        public string label;        // 談的是什麼
        public string apply;        // fee / buy / sell / settle / invest
        public string itemId;       // 買賣的商品 id
        public string itemName;
        public string itemCondition;
        public string itemCategory;  // 正式營業：商品種類
        public int itemGrade;        // 正式營業：真正的狀況
        public int itemBase;         // 正式營業：全新行情
        public string itemFlags;     // 正式營業：stolen 等
        public string refId;         // 正式營業：對應的委託
        public string feeField;     // 談好之後要更新的合約欄位
        public int ask;             // 對方一開始開的價
        public int start;           // 我方出價起點
        public int step;
        public int min;
        public int max;
        public int accept;          // 對方能接受的界線
        public int walk;            // 超過這條線對方會生氣
        public string acceptText;
        public string counterText;
        public string refuseText;
        public bool endsCall;
    }

    [Serializable]
    public class PlanOption
    {
        public string id;
        public string text;
        public int cost;
        public string[] tags;
    }

    [Serializable]
    public class PlanGroup
    {
        public string name;
        public PlanOption[] options;
    }

    [Serializable]
    public class PlanReq
    {
        public string text;
        public string tag;          // 必須包含
        public string notag;        // 不能包含
        public int budget;          // 總成本上限
    }

    [Serializable]
    public class PlanData
    {
        public string docId;
        public int reward;
        public PlanGroup[] groups;
        public PlanReq[] reqs;
        public string okText;
        public bool payDiff;         // true：企劃費 = reward + (預算 - 成本)，越省錢賺越多
    }

    /// <summary>工商派人：旗下每位藝人適不適合這個案子。</summary>
    [Serializable]
    public class RosterEntry
    {
        public string id;
        public string name;
        public string portrait;
        public string genre;
        public int fans;
        public string likes;
        public string dislikes;
        public string status;       // 直播那天有沒有空
        public bool busy;
        public int fit = -1;        // 對方沒指定時：0 不合適 / 1 勉強 / 2 合適 / 3 最合適
        public string fitText;
        public string outcome;      // 派他去的後果
        public long swapFee;        // 對方有指定時：換成他之後的費用（0 = 對方不接受）
        public string reply;        // 對方的反應
    }

    /// <summary>工商要派哪位藝人：對方沒指定就自己挑，有指定也可以提議換人。</summary>
    [Serializable]
    public class AssignData
    {
        public bool open;           // 對方沒指定藝人
        public string wanted;       // 對方指定的藝人 id
        public string wantedName;
        public string category;
        public string genre;        // 希望的類型（沒指定時）
        public int minFans;
        public int day;
        public long quote;
        public RosterEntry[] roster = new RosterEntry[0];
    }

    [Serializable]
    public class RecommendData
    {
        public string correct;
        public int reward;
        public string okText;
        public string badText;
        public string[] gives;
    }

    [Serializable]
    public class ContactData
    {
        public string id;
        public string name;
        public string status;
        public string message;
        public bool caller;
    }

    [Serializable]
    public class TaskData
    {
        public string text;
        public string doneWhen;     // 條件，寫法同教學腳本
    }

    [Serializable]
    public class ClientData
    {
        public string id;
        public string name;
        public string company;
        public string role;
    }

    [Serializable]
    public class CaseData
    {
        public string id;
        public string title;
        public ClientData client;
        public string greeting;
        public int patience;
        public int baseFee;
        public string[] startSlots;
        public string[] storage;
        public int storageUnlocked;
        public int storageTotal;
        public bool requireAll;      // 問題沒抓完不能簽約
        public bool useInventory;    // 把公司庫存放進資料倉庫
        public string stampOk;
        public string stampNo;
        public DocumentData[] documents;
        public ContradictionData[] contradictions;
        public ReplyData[] replies;
        public OfferData offer;
        public PlanData plan;
        public RecommendData recommend;
        public AssignData assign;   // 工商派人（正式營業）
        public ContactData[] contacts;
        public TaskData[] tasks;

        public string replyWrong;
        public string replyLeave;
        public string replySigned;
        public string replyHold;
        public string replyReject;
        public string replyDeal;
        public string dealTitle;
        public string signedWarning;
        public bool deferFee;        // 正式營業：簽約後不直接記應收，直播完成才入帳
        public string payNote;       // 簽約結果單上的入帳說明
        public string hintWho;       // 提示訊息的署名（預設「鬼東」）

        public static CaseData Load(string resourcePath)
        {
            TextAsset ta = Resources.Load<TextAsset>(resourcePath);
            if (ta == null)
            {
                LoadReport.Add("找不到案件資料 Resources/" + resourcePath + ".json");
                return null;
            }
            try
            {
                CaseData d = JsonUtility.FromJson<CaseData>(ta.text);
                if (d == null) throw new Exception("內容是空的");
                d.Normalize();
                return d;
            }
            catch (Exception e)
            {
                LoadReport.Add("案件資料格式錯誤 Resources/" + resourcePath + ".json：" + LoadReport.Brief(e.Message));
                return null;
            }
        }

        public void Normalize()
        {
            if (startSlots == null) startSlots = new string[0];
            if (storage == null) storage = new string[0];
            if (documents == null) documents = new DocumentData[0];
            if (contradictions == null) contradictions = new ContradictionData[0];
            if (replies == null) replies = new ReplyData[0];
            if (contacts == null) contacts = new ContactData[0];
            if (tasks == null) tasks = new TaskData[0];
            if (patience <= 0) patience = 5;
            if (storageUnlocked <= 0) storageUnlocked = 8;
            if (storageTotal < storageUnlocked) storageTotal = storageUnlocked;
            if (string.IsNullOrEmpty(stampOk)) stampOk = "核准簽約";
            if (string.IsNullOrEmpty(stampNo)) stampNo = "退回";
            if (string.IsNullOrEmpty(replyWrong)) replyWrong = "呃……這兩個有什麼問題嗎？";
            if (string.IsNullOrEmpty(replyLeave)) replyLeave = "我看我們再考慮一下好了。";
            if (string.IsNullOrEmpty(hintWho)) hintWho = "鬼東";
            if (offer != null && string.IsNullOrEmpty(offer.docId)) offer = null;
            if (plan != null && string.IsNullOrEmpty(plan.docId)) plan = null;
            if (recommend != null && string.IsNullOrEmpty(recommend.correct)) recommend = null;
            if (assign != null && (assign.roster == null || assign.roster.Length == 0)) assign = null;
            foreach (DocumentData doc in documents) NormalizeDoc(doc);
            foreach (ReplyData r in replies) if (r.gives == null) r.gives = new string[0];
            if (plan != null)
            {
                if (plan.groups == null) plan.groups = new PlanGroup[0];
                if (plan.reqs == null) plan.reqs = new PlanReq[0];
                foreach (PlanGroup g in plan.groups)
                {
                    if (g.options == null) g.options = new PlanOption[0];
                    foreach (PlanOption o in g.options) if (o.tags == null) o.tags = new string[0];
                }
            }
            if (recommend != null && recommend.gives == null) recommend.gives = new string[0];
        }

        public static void NormalizeDoc(DocumentData doc)
        {
            if (doc.info == null) doc.info = new FieldData[0];
            if (doc.fields == null) doc.fields = new FieldData[0];
            if (doc.labelWidth <= 0) doc.labelWidth = 56;
            if (string.IsNullOrEmpty(doc.kind)) doc.kind = "form";
            if (string.IsNullOrEmpty(doc.paper)) doc.paper = "#e9dcbc";
            if (string.IsNullOrEmpty(doc.head)) doc.head = "#cdbd96";
            if (string.IsNullOrEmpty(doc.subTitle)) doc.subTitle = "近期行程";
        }

        public DocumentData Doc(string id)
        {
            for (int i = 0; i < documents.Length; i++)
                if (documents[i].id == id) return documents[i];
            return null;
        }
    }
}
