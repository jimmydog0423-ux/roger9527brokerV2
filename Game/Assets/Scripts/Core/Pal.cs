using UnityEngine;

namespace SSD
{
    /// <summary>
    /// 全遊戲共用的色盤。跟網頁雛形使用同一組顏色。
    /// </summary>
    public static class Pal
    {
        public static Color32 Hex(string hex)
        {
            Color c;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return new Color32(255, 0, 255, 255);
        }

        public static Color32 Alpha(Color32 c, byte a)
        {
            return new Color32(c.r, c.g, c.b, a);
        }

        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        public static readonly Color32 White = new Color32(255, 255, 255, 255);

        // 介面
        public static readonly Color32 Ink = Hex("#16131f");
        public static readonly Color32 Night = Hex("#221d33");
        public static readonly Color32 Plum = Hex("#2f2745");
        public static readonly Color32 PlumDeep = Hex("#2a2340");
        public static readonly Color32 PlumLine = Hex("#3e3459");
        public static readonly Color32 PlumLight = Hex("#57497a");
        public static readonly Color32 Lav = Hex("#9d90c4");
        public static readonly Color32 Cream = Hex("#f1e6cc");
        public static readonly Color32 Orange = Hex("#e8893a");
        public static readonly Color32 Teal = Hex("#3fa7a0");
        public static readonly Color32 TealLight = Hex("#5fc9b8");
        public static readonly Color32 TealDark = Hex("#1f6f66");
        public static readonly Color32 Red = Hex("#a8352d");
        public static readonly Color32 Pink = Hex("#f08a7a");
        public static readonly Color32 Yellow = Hex("#e8c35a");
        public static readonly Color32 DeskGrid = Hex("#30284a");
        public static readonly Color32 TrayBg = Hex("#1d1a2c");

        // 紙張
        public static readonly Color32 PaperInk = Hex("#2a2236");
        public static readonly Color32 PaperLabel = Hex("#5e5140");
        public static readonly Color32 PaperDot = Hex("#8a7a58");

        // 視訊背景
        public static readonly Color32 VideoWall = Hex("#4a5560");
    }
}
