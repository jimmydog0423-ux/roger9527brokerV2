using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 資料倉庫格子裡的小圖示（48x48 格子內的座標）。雛形先用色塊拼。
    /// </summary>
    public static class IconArt
    {
        struct Part
        {
            public int x, y, w, h;
            public string c;
            public bool border;

            public Part(int x, int y, int w, int h, string c, bool border)
            {
                this.x = x; this.y = y; this.w = w; this.h = h; this.c = c; this.border = border;
            }
        }

        static Part P(int x, int y, int w, int h, string c) { return new Part(x, y, w, h, c, false); }
        static Part B(int x, int y, int w, int h, string c) { return new Part(x, y, w, h, c, true); }

        static Part[] Parts(string icon)
        {
            switch (icon)
            {
                case "book":
                    return new[] { B(13, 3, 20, 26, "#2c3a5a"), P(14, 4, 4, 24, "#1f2a44"), P(20, 9, 10, 5, "#e9dcbc") };
                case "folder":
                    return new[] { B(10, 3, 12, 6, "#cdb27a"), B(10, 7, 27, 22, "#cdb27a"), P(14, 12, 9, 10, "#4a5560") };
                case "contract":
                    return new[] { B(14, 2, 19, 27, "#ecdcc8"), P(16, 7, 15, 1, "#b3a27a"), P(16, 11, 15, 1, "#b3a27a"),
                                   P(16, 15, 15, 1, "#b3a27a"), P(16, 19, 8, 1, "#b3a27a"), P(25, 21, 6, 6, "#a8352d") };
                case "quote":
                    return new[] { B(14, 2, 19, 27, "#d6e0e6"), P(16, 7, 15, 1, "#9fb0bf"), P(16, 11, 15, 1, "#9fb0bf"),
                                   P(16, 15, 15, 1, "#9fb0bf"), P(17, 21, 13, 3, "#e8893a") };
                case "card":
                    return new[] { B(9, 8, 29, 18, "#f1e6cc"), P(13, 13, 15, 2, "#b3a27a"), P(13, 17, 15, 2, "#b3a27a") };
                case "pcb":
                    return new[] { B(8, 7, 31, 20, "#2f6f6a"), P(13, 11, 9, 9, "#16131f"), P(26, 14, 6, 4, "#e8c35a"), P(9, 24, 29, 2, "#e8c35a") };
                case "recorder":
                    return new[] { B(18, 2, 12, 27, "#4a4858"), P(21, 6, 6, 5, "#a8352d"), P(20, 14, 8, 1, "#16131f"), P(20, 18, 8, 1, "#16131f") };
                case "request":
                    return new[] { B(12, 3, 24, 28, "#a07a52"), P(15, 7, 18, 21, "#f1e6cc"), P(19, 2, 10, 4, "#4a4858"),
                                   P(17, 12, 14, 1, "#b3a27a"), P(17, 16, 14, 1, "#b3a27a"), P(17, 20, 9, 1, "#b3a27a") };
                case "receipt":
                    return new[] { B(14, 2, 19, 27, "#f1e6cc"), P(17, 6, 13, 3, "#e8893a"), P(17, 12, 13, 1, "#b3a27a"),
                                   P(17, 16, 13, 1, "#b3a27a"), P(17, 22, 13, 2, "#2a2236") };
                case "report":
                    return new[] { B(14, 2, 19, 27, "#e9dcbc"), P(16, 6, 15, 1, "#b3a27a"), B(18, 11, 9, 9, "#9d90c4"), P(26, 19, 3, 6, "#4a4858") };
                case "chart":
                    return new[] { B(11, 3, 26, 26, "#f1e6cc"), P(15, 18, 4, 8, "#3fa7a0"), P(21, 12, 4, 14, "#e8893a"), P(27, 15, 4, 11, "#57497a") };
                case "plan":
                    return new[] { B(12, 3, 24, 28, "#57497a"), P(15, 7, 18, 21, "#e9dcbc"), P(17, 10, 3, 3, "#3fa7a0"), P(22, 11, 9, 1, "#9d90c4"),
                                   P(17, 16, 3, 3, "#3fa7a0"), P(22, 17, 9, 1, "#9d90c4"), P(17, 22, 3, 3, "#e8893a"), P(22, 23, 9, 1, "#9d90c4") };
                case "claim":
                    return new[] { B(14, 2, 19, 27, "#f0d6cc"), P(15, 3, 17, 5, "#a8352d"), P(17, 12, 13, 1, "#b3a27a"),
                                   P(17, 16, 13, 1, "#b3a27a"), P(17, 22, 13, 2, "#a8352d") };
                case "record":
                    return new[] { B(8, 6, 32, 21, "#16131f"), P(11, 22, 26, 2, "#e8893a"), P(12, 9, 4, 4, "#a8352d"), P(19, 10, 10, 8, "#3e3459") };
                case "proposal":
                    return new[] { B(10, 3, 12, 6, "#3fa7a0"), B(10, 7, 27, 22, "#3fa7a0"), P(16, 13, 15, 6, "#f1e6cc"), P(18, 15, 11, 2, "#e8893a") };
                case "box":
                    return new[] { B(9, 9, 30, 20, "#a07a52"), P(22, 10, 4, 18, "#c9a774"), P(10, 10, 28, 3, "#8a6844") };
                case "mic":
                    return new[] { B(19, 3, 10, 15, "#4a4858"), P(20, 5, 8, 2, "#8f8f9c"), P(20, 9, 8, 2, "#8f8f9c"), P(23, 18, 2, 7, "#2a2833"), B(16, 25, 16, 3, "#2a2833") };
                case "lamp":
                    return new[] { B(12, 4, 24, 16, "#e8c35a"), P(14, 6, 20, 12, "#f1e6cc"), P(23, 20, 2, 7, "#4a4858"), B(17, 26, 14, 3, "#4a4858") };
                case "camera":
                    return new[] { B(9, 10, 26, 17, "#2a2833"), B(35, 13, 6, 11, "#2a2833"), P(15, 13, 10, 10, "#57497a"), P(17, 15, 6, 6, "#9d90c4"), P(11, 7, 8, 3, "#4a4858") };
                case "chair":
                    return new[] { B(15, 3, 18, 15, "#a8352d"), P(18, 6, 12, 2, "#16131f"), B(13, 18, 22, 4, "#a8352d"), P(23, 22, 2, 5, "#4a4858"), P(16, 27, 16, 2, "#4a4858") };
                case "keyboard":
                    return new[] { B(6, 12, 36, 14, "#2a2833"), P(9, 15, 30, 2, "#8f8f9c"), P(9, 19, 30, 2, "#8f8f9c"), P(14, 23, 20, 2, "#8f8f9c") };
                case "headset":
                    return new[] { P(14, 5, 20, 3, "#2a2833"), P(12, 7, 3, 10, "#2a2833"), P(33, 7, 3, 10, "#2a2833"), B(9, 15, 8, 12, "#e8893a"), B(31, 15, 8, 12, "#e8893a") };
                case "pc":
                    return new[] { B(8, 4, 32, 20, "#2a2833"), P(10, 6, 28, 16, "#3fa7a0"), P(22, 24, 4, 3, "#4a4858"), B(15, 27, 18, 2, "#4a4858") };
                case "drink":
                    return new[] { B(17, 6, 14, 23, "#3fa7a0"), P(17, 6, 14, 3, "#8f8f9c"), P(19, 13, 10, 8, "#f1e6cc"), P(21, 15, 6, 2, "#e8893a") };
                case "gift":
                    return new[] { B(10, 11, 28, 18, "#a8352d"), P(22, 11, 4, 18, "#e8c35a"), P(10, 18, 28, 3, "#e8c35a"), P(17, 6, 6, 5, "#e8c35a"), P(25, 6, 6, 5, "#e8c35a") };
                case "detector":
                    return new[] { P(30, 3, 3, 18, "#8f8f9c"), B(26, 3, 10, 6, "#2a2833"), P(14, 19, 18, 3, "#8f8f9c"), B(6, 21, 16, 8, "#e8c35a"), P(10, 23, 8, 4, "#a07a52") };
                case "headlamp":
                    return new[] { P(9, 12, 30, 4, "#2a2833"), B(18, 9, 12, 10, "#4a4858"), P(20, 11, 8, 6, "#f1e6cc"), P(30, 13, 9, 2, "#e8c35a"), P(30, 17, 7, 2, "#e8c35a") };
                default:
                    return new[] { B(14, 2, 19, 27, "#e9dcbc") };
            }
        }

        /// <summary>在格子裡畫圖示。alpha 小於 1 時表示「已放到檢視區」。</summary>
        public static void Draw(Transform slot, string icon, float alpha)
        {
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
            foreach (Part p in Parts(icon))
            {
                Image f = Px.Fill(slot, p.x, p.y, p.w, p.h, Pal.Alpha(Pal.Hex(p.c), a));
                f.name = "Icon";
                if (p.border) Px.Frame(slot, p.x, p.y, p.w, p.h, Pal.Alpha(Pal.Ink, a));
            }
        }
    }
}
