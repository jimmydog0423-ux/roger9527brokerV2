using System;
using UnityEngine;

namespace SSD
{
    // 正式營業用的世界資料（品牌、商品、藝人、企劃選項……），放在 Resources/Main/world.json。
    // 想加品牌或商品，只要改 JSON。

    [Serializable]
    public class BrandDef
    {
        public string name;         // 品牌（最多 4 個字，會出現在行程表）
        public string company;      // 公司全名（最多 6 個字）
        public string cat;          // 類別（2 個字）
        public string npc;          // 固定聯絡人 id（空白 = 自動產生）
        public int act = 1;         // 第幾幕開始出現
    }

    [Serializable]
    public class CategoryDef
    {
        public string id;           // 2 個字
        public string product;      // 合作形式（最多 4 個字）
    }

    [Serializable]
    public class ItemDef
    {
        public string id;
        public string name;         // 最多 6 個字
        public string cat;
        public string icon;
        public string part;         // 會被修過的零件（最多 4 個字）
        public int act = 1;
        public int price;           // 全新行情
        public string use;          // 商店貨架上寫的用途
    }

    [Serializable]
    public class TalentDef
    {
        public string id;
        public string name;         // 最多 3 個字
        public string genre;        // 最多 4 個字
        public string portrait;
        public string style;        // 最多 4 個字
        public string interests;
        public string affiliation;
        public int fans;
        public string[] likes;
        public string[] dislikes;
    }

    [Serializable]
    public class PlanOptDef
    {
        public string id;
        public string text;
        public int cost;
        public string[] tags;
    }

    [Serializable]
    public class PlanGroupDef
    {
        public string name;
        public PlanOptDef[] options;
    }

    [Serializable]
    public class TagText
    {
        public string tag;
        public string need;         // 「要有……」
        public string avoid;        // 「不要……」（空白 = 不拿來當禁止條件）
    }

    [Serializable]
    public class InvestDef
    {
        public string name;         // 最多 7 個字
        public string kind;         // 最多 5 個字
        public string portrait;
    }

    [Serializable]
    public class GrayDef
    {
        public string title;
        public string product;      // 合作形式（最多 4 個字）
        public string licensor;     // 授權方（最多 6 個字）
        public string content;      // 授權內容（最多 6 個字）
        public string scope;        // 合約要的範圍
        public string realScope;    // 授權書上真正的範圍
    }

    [Serializable]
    public class NewsDef
    {
        public string cat;
        public string up;
        public string down;
    }

    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public string name;
        public string desc;
        public int act = 1;
        public long cost;
        public string requires;
    }

    [Serializable]
    public class NpcDef
    {
        public string id;
        public string name;
        public string company;
        public string role;
    }

    [Serializable]
    public class WorldData
    {
        public CategoryDef[] categories;
        public BrandDef[] brands;
        public ItemDef[] items;
        public TalentDef[] talents;
        public PlanGroupDef[] planGroups;
        public TagText[] tagTexts;
        public InvestDef[] invest;
        public GrayDef[] gray;
        public NewsDef[] news;
        public UpgradeDef[] upgrades;
        public NpcDef[] sellers;
        public NpcDef[] buyers;
        public string[] surnames;
        public string[] titles;

        public static WorldData Load()
        {
            TextAsset ta = Resources.Load<TextAsset>("Main/world");
            if (ta == null)
            {
                LoadReport.Add("找不到正式營業的資料 Resources/Main/world.json");
                return null;
            }
            try
            {
                WorldData w = JsonUtility.FromJson<WorldData>(ta.text);
                if (w == null) throw new Exception("內容是空的");
                w.Normalize();
                if (w.brands.Length == 0 || w.items.Length == 0 || w.talents.Length == 0) throw new Exception("品牌、商品或藝人清單是空的");
                return w;
            }
            catch (Exception e)
            {
                LoadReport.Add("正式營業資料格式錯誤 Resources/Main/world.json：" + LoadReport.Brief(e.Message));
                return null;
            }
        }

        public void Normalize()
        {
            if (categories == null) categories = new CategoryDef[0];
            if (brands == null) brands = new BrandDef[0];
            if (items == null) items = new ItemDef[0];
            if (talents == null) talents = new TalentDef[0];
            if (planGroups == null) planGroups = new PlanGroupDef[0];
            if (tagTexts == null) tagTexts = new TagText[0];
            if (invest == null) invest = new InvestDef[0];
            if (gray == null) gray = new GrayDef[0];
            if (news == null) news = new NewsDef[0];
            if (upgrades == null) upgrades = new UpgradeDef[0];
            if (sellers == null) sellers = new NpcDef[0];
            if (buyers == null) buyers = new NpcDef[0];
            if (surnames == null || surnames.Length == 0) surnames = new[] { "李" };
            if (titles == null || titles.Length == 0) titles = new[] { "經理" };
            foreach (TalentDef t in talents)
            {
                if (t.likes == null) t.likes = new string[0];
                if (t.dislikes == null) t.dislikes = new string[0];
            }
            foreach (PlanGroupDef g in planGroups)
            {
                if (g.options == null) g.options = new PlanOptDef[0];
                foreach (PlanOptDef o in g.options) if (o.tags == null) o.tags = new string[0];
            }
        }

        public CategoryDef Category(string id)
        {
            foreach (CategoryDef c in categories) if (c.id == id) return c;
            return null;
        }

        public ItemDef Item(string id)
        {
            foreach (ItemDef i in items) if (i.id == id) return i;
            return null;
        }

        public BrandDef Brand(string name)
        {
            foreach (BrandDef b in brands) if (b.name == name) return b;
            return null;
        }

        public TagText Tag(string tag)
        {
            foreach (TagText t in tagTexts) if (t.tag == tag) return t;
            return null;
        }

        public UpgradeDef Upgrade(string id)
        {
            foreach (UpgradeDef u in upgrades) if (u.id == id) return u;
            return null;
        }
    }
}
