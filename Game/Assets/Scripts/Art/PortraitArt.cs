using System.Collections.Generic;
using UnityEngine;

namespace SSD
{
    /// <summary>
    /// 優先使用 Resources/Portraits 下的角色立繪，沒有立繪時使用原本的像素頭像。
    /// </summary>
    public static class PortraitArt
    {
        public const int W = 24;
        public const int H = 28;
        const string Ink = "#1b1726";

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string who)
        {
            if (string.IsNullOrEmpty(who)) who = "lin";
            Sprite s;
            if (cache.TryGetValue(who, out s) && s != null) return s;
            s = Resources.Load<Sprite>("Portraits/" + who);
            if (s == null) s = Build(who);
            cache[who] = s;
            return s;
        }

        /// <summary>在 (x, y) 畫一個頭像，scale 是整數倍率（1 = 24x28）。</summary>
        public static UnityEngine.UI.Image Draw(Transform parent, string who, float x, float y, int scale)
        {
            RectTransform rt = Px.Rect(parent, "Portrait_" + who, x, y, W * scale, H * scale);
            UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = Get(who);
            img.raycastTarget = false;
            return img;
        }

        class Look
        {
            public string s1 = "#e2b78f", s2 = "#c4916c", brow = "#2a2026", mouth = "#8a4a3e";
            public readonly List<object[]> back = new List<object[]>();
            public readonly List<object[]> body = new List<object[]>();
            public List<object[]> face;
            public readonly List<object[]> front = new List<object[]>();
        }

        static object[] R(int x, int y, int w, int h, string c)
        {
            return new object[] { x, y, w, h, c };
        }

        static Look Define(string who)
        {
            Look k = new Look();
            switch (who)
            {
                case "guidong":
                {
                    // 鬼東：油頭灰髮、墨鏡、山羊鬍、深色西裝金領帶
                    k.s1 = "#d9a77f"; k.s2 = "#b98260"; k.mouth = "#6e3a30";
                    string hr = "#8f8f9c";
                    k.body.Add(R(2, 21, 20, 7, "#2a2236")); k.body.Add(R(9, 20, 6, 4, "#e9dcbc"));
                    k.body.Add(R(11, 21, 2, 7, "#e8c35a")); k.body.Add(R(7, 21, 2, 6, "#3e3459")); k.body.Add(R(15, 21, 2, 6, "#3e3459"));
                    k.face = new List<object[]> {
                        R(6, 11, 12, 2, Ink), R(7, 13, 4, 1, Ink), R(13, 13, 4, 1, Ink), R(7, 11, 2, 1, "#57497a"), R(13, 11, 2, 1, "#57497a"),
                        R(11, 14, 2, 1, k.s2), R(9, 15, 6, 1, hr), R(10, 16, 4, 1, k.mouth), R(10, 17, 4, 2, hr)
                    };
                    k.front.Add(R(6, 3, 12, 3, hr)); k.front.Add(R(6, 6, 12, 1, "#a5a5b2")); k.front.Add(R(5, 5, 2, 5, hr)); k.front.Add(R(17, 5, 2, 4, hr));
                    k.front.Add(R(8, 3, 6, 1, "#a5a5b2"));
                    break;
                }
                case "roger":
                {
                    k.s1 = "#d9a77f"; k.s2 = "#b98260"; k.brow = "#3a2a24"; k.mouth = "#7a3e34";
                    string hr = "#3a2a24", hp = "#2b2b38";
                    k.body.Add(R(2, 21, 20, 7, "#e8893a")); k.body.Add(R(16, 21, 6, 7, "#b8612a")); k.body.Add(R(7, 19, 10, 2, "#b8612a"));
                    k.body.Add(R(10, 22, 1, 3, "#f1e6cc")); k.body.Add(R(13, 22, 1, 3, "#f1e6cc"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 1, k.s2), R(14, 12, 2, 1, k.s2), R(8, 13, 2, 1, Ink), R(14, 13, 2, 1, Ink),
                        R(8, 10, 3, 1, k.brow), R(13, 11, 3, 1, k.brow), R(11, 14, 2, 1, k.s2), R(10, 16, 3, 1, k.mouth), R(13, 15, 1, 1, k.mouth)
                    };
                    k.front.Add(R(6, 3, 12, 4, hr)); k.front.Add(R(7, 2, 2, 1, hr)); k.front.Add(R(11, 1, 2, 2, hr)); k.front.Add(R(15, 2, 2, 1, hr));
                    k.front.Add(R(5, 5, 1, 5, hr)); k.front.Add(R(18, 5, 1, 5, hr)); k.front.Add(R(6, 7, 3, 1, hr)); k.front.Add(R(10, 7, 2, 1, hr)); k.front.Add(R(14, 7, 3, 1, hr));
                    k.front.Add(R(6, 0, 12, 1, hp)); k.front.Add(R(5, 1, 1, 2, hp)); k.front.Add(R(18, 1, 1, 2, hp)); k.front.Add(R(4, 3, 1, 7, hp)); k.front.Add(R(19, 3, 1, 7, hp));
                    k.front.Add(R(3, 10, 3, 5, hp)); k.front.Add(R(18, 10, 3, 5, hp)); k.front.Add(R(4, 11, 1, 3, "#a8352d")); k.front.Add(R(19, 11, 1, 3, "#a8352d"));
                    break;
                }
                case "chen":
                {
                    k.s1 = "#c99270"; k.s2 = "#a8714f"; k.brow = "#8a8494"; k.mouth = "#6e3a30";
                    k.body.Add(R(2, 21, 20, 7, "#5f7a52")); k.body.Add(R(9, 20, 6, 2, "#4a5f40")); k.body.Add(R(2, 24, 20, 1, "#4a5f40"));
                    k.body.Add(R(11, 22, 1, 1, "#e9dcbc")); k.body.Add(R(11, 25, 1, 1, "#e9dcbc"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow),
                        R(11, 14, 2, 1, k.s2), R(9, 15, 6, 1, "#8a8494"), R(10, 16, 4, 1, k.mouth)
                    };
                    k.front.Add(R(6, 3, 12, 4, "#4a6b8a")); k.front.Add(R(7, 2, 10, 1, "#4a6b8a")); k.front.Add(R(5, 7, 14, 1, "#36506b")); k.front.Add(R(11, 4, 2, 1, "#e8c35a"));
                    k.front.Add(R(5, 8, 1, 4, "#8a8494")); k.front.Add(R(18, 8, 1, 4, "#8a8494"));
                    break;
                }
                case "kui":
                {
                    // 月希：深色長髮、月亮髮飾和藍紫色上衣。
                    k.s1 = "#f0c9a4"; k.s2 = "#d6a580"; k.brow = "#6b3d2e"; k.mouth = "#b0524a";
                    string hr = "#30263f";
                    k.back.Add(R(4, 5, 16, 17, hr));
                    k.body.Add(R(2, 21, 20, 7, "#9d90c4")); k.body.Add(R(16, 21, 6, 7, "#7d70a4")); k.body.Add(R(9, 20, 6, 1, "#7d70a4"));
                    k.body.Add(R(4, 20, 3, 6, hr)); k.body.Add(R(17, 20, 3, 6, hr));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(7, 12, 1, 1, Ink), R(16, 12, 1, 1, Ink),
                        R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow), R(11, 14, 2, 1, k.s2), R(11, 16, 2, 1, k.mouth),
                        R(7, 15, 1, 1, "#e89a8a"), R(16, 15, 1, 1, "#e89a8a")
                    };
                    k.front.Add(R(6, 4, 12, 3, hr)); k.front.Add(R(5, 5, 1, 4, hr)); k.front.Add(R(18, 5, 1, 4, hr));
                    k.front.Add(R(6, 7, 4, 2, hr)); k.front.Add(R(11, 7, 2, 1, hr)); k.front.Add(R(14, 7, 4, 2, hr));
                    k.front.Add(R(16, 4, 1, 3, "#e8c35a")); k.front.Add(R(17, 6, 2, 1, "#e8c35a"));
                    break;
                }
                case "wang":
                {
                    // 王小姐（喵喵罐頭）：鮑伯頭、橘色貓耳髮箍
                    k.s1 = "#f0c9a4"; k.s2 = "#d6a580"; k.brow = "#3b2a24"; k.mouth = "#b0524a";
                    string hr = "#3b2a24";
                    k.back.Add(R(5, 5, 14, 13, hr));
                    k.body.Add(R(2, 21, 20, 7, "#e8c35a")); k.body.Add(R(16, 21, 6, 7, "#c9a442")); k.body.Add(R(9, 20, 6, 2, "#f1e6cc"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(7, 12, 1, 1, Ink), R(16, 12, 1, 1, Ink),
                        R(11, 14, 2, 1, k.s2), R(10, 16, 4, 1, k.mouth), R(7, 15, 1, 1, "#e89a8a"), R(16, 15, 1, 1, "#e89a8a")
                    };
                    k.front.Add(R(6, 4, 12, 3, hr)); k.front.Add(R(6, 7, 12, 2, hr)); k.front.Add(R(5, 6, 2, 11, hr)); k.front.Add(R(17, 6, 2, 11, hr));
                    k.front.Add(R(6, 3, 12, 1, "#e8893a")); k.front.Add(R(6, 1, 3, 2, "#e8893a")); k.front.Add(R(15, 1, 3, 2, "#e8893a"));
                    k.front.Add(R(7, 2, 1, 1, "#f08a7a")); k.front.Add(R(16, 2, 1, 1, "#f08a7a"));
                    break;
                }
                case "zhou":
                {
                    // 周老闆（周記滷味）：光頭、白毛巾頭帶、紅衣白圍裙
                    k.s1 = "#c99270"; k.s2 = "#a8714f"; k.brow = "#3a2a24"; k.mouth = "#6e3a30";
                    k.body.Add(R(2, 21, 20, 7, "#a8352d")); k.body.Add(R(7, 22, 10, 6, "#e9dcbc")); k.body.Add(R(8, 21, 1, 2, "#e9dcbc")); k.body.Add(R(15, 21, 1, 2, "#e9dcbc"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow),
                        R(11, 14, 2, 1, k.s2), R(9, 15, 6, 1, "#3a2a24"), R(10, 16, 4, 1, k.mouth)
                    };
                    k.front.Add(R(6, 8, 12, 2, "#e9dcbc")); k.front.Add(R(17, 9, 3, 2, "#e9dcbc")); k.front.Add(R(9, 6, 3, 1, "#e0b48c"));
                    k.front.Add(R(5, 10, 1, 4, "#3a2a24")); k.front.Add(R(18, 10, 1, 4, "#3a2a24"));
                    break;
                }
                case "kai":
                {
                    // 蛋頭：棒球帽、深色上衣和手持麥克風。
                    k.s1 = "#e2b78f"; k.s2 = "#c4916c"; k.brow = "#2a2026"; k.mouth = "#8a4a3e";
                    k.body.Add(R(2, 21, 20, 7, "#30283e")); k.body.Add(R(9, 21, 6, 1, "#b8a68e"));
                    k.body.Add(R(17, 21, 2, 7, "#292532")); k.body.Add(R(16, 20, 4, 3, "#8f8f9c"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow),
                        R(11, 14, 2, 1, k.s2), R(10, 16, 4, 1, k.mouth), R(9, 15, 1, 1, k.mouth), R(14, 15, 1, 1, k.mouth)
                    };
                    k.front.Add(R(6, 3, 12, 4, "#2a2026")); k.front.Add(R(5, 5, 16, 2, "#2a2026"));
                    k.front.Add(R(10, 4, 3, 1, "#e8c35a"));
                    break;
                }
                case "zhang":
                {
                    // 張總監（極光電競）：漸層藍綠頭髮、黑色夾克、霓虹線條
                    k.s1 = "#e2b78f"; k.s2 = "#c4916c"; k.brow = "#2a2026"; k.mouth = "#8a4a3e";
                    k.body.Add(R(2, 21, 20, 7, "#1f2a44")); k.body.Add(R(2, 24, 20, 1, "#5fc9b8")); k.body.Add(R(9, 20, 6, 3, "#16131f"));
                    k.face = new List<object[]> {
                        R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow),
                        R(11, 14, 2, 1, k.s2), R(10, 16, 4, 1, k.mouth)
                    };
                    k.front.Add(R(6, 2, 12, 5, "#3fa7a0")); k.front.Add(R(6, 2, 12, 1, "#5fc9b8")); k.front.Add(R(14, 7, 4, 1, "#3fa7a0"));
                    k.front.Add(R(5, 6, 1, 5, "#2a2026")); k.front.Add(R(18, 6, 1, 5, "#2a2026"));
                    k.front.Add(R(4, 12, 2, 4, "#16131f")); k.front.Add(R(5, 16, 5, 1, "#16131f"));
                    break;
                }
                case "vboss":
                {
                    // V老大：深紫色高馬尾、銳利眼線、紅唇、黑西裝白襯衫、金耳環
                    k.s1 = "#f0c9a4"; k.s2 = "#d6a580"; k.brow = "#2a1f33"; k.mouth = "#a8352d";
                    string hr = "#3e2a5c", hl = "#57497a";
                    k.back.Add(R(16, 1, 5, 4, hr)); k.back.Add(R(18, 4, 4, 12, hr)); k.back.Add(R(19, 15, 3, 5, hr));
                    k.body.Add(R(2, 21, 20, 7, "#16131f")); k.body.Add(R(9, 20, 6, 8, "#f1e6cc")); k.body.Add(R(11, 22, 2, 6, "#16131f"));
                    k.body.Add(R(7, 21, 2, 7, "#2a2236")); k.body.Add(R(15, 21, 2, 7, "#2a2236"));
                    k.face = new List<object[]> {
                        R(7, 12, 3, 1, Ink), R(14, 12, 3, 1, Ink), R(8, 13, 2, 1, Ink), R(14, 13, 2, 1, Ink), R(10, 12, 1, 1, Ink), R(17, 12, 1, 1, Ink),
                        R(7, 10, 4, 1, k.brow), R(13, 10, 4, 1, k.brow), R(11, 14, 2, 1, k.s2), R(10, 16, 4, 1, k.mouth)
                    };
                    k.front.Add(R(6, 3, 12, 4, hr)); k.front.Add(R(6, 3, 12, 1, hl)); k.front.Add(R(5, 5, 2, 7, hr)); k.front.Add(R(17, 5, 2, 5, hr));
                    k.front.Add(R(7, 7, 5, 1, hr)); k.front.Add(R(14, 2, 4, 2, hr));
                    k.front.Add(R(5, 13, 1, 2, "#e8c35a")); k.front.Add(R(18, 13, 1, 2, "#e8c35a"));
                    break;
                }
                case "tax":
                {
                    // 稅務員：灰西裝、方框眼鏡、嚴肅
                    k.s1 = "#e2b78f"; k.s2 = "#c4916c"; k.mouth = "#6e3a30";
                    string hr = "#2a2026";
                    k.body.Add(R(2, 21, 20, 7, "#6b6b78")); k.body.Add(R(9, 20, 6, 8, "#e9dcbc")); k.body.Add(R(11, 21, 2, 7, "#2c3a5a"));
                    k.face = new List<object[]> {
                        R(7, 11, 4, 4, Ink), R(13, 11, 4, 4, Ink), R(8, 12, 2, 2, "#c4d4e0"), R(14, 12, 2, 2, "#c4d4e0"), R(11, 12, 2, 1, Ink),
                        R(11, 15, 2, 1, k.s2), R(9, 17, 6, 1, k.mouth)
                    };
                    k.front.Add(R(6, 3, 12, 3, hr)); k.front.Add(R(6, 6, 12, 1, hr)); k.front.Add(R(5, 5, 1, 5, hr)); k.front.Add(R(18, 5, 1, 5, hr));
                    break;
                }
                case "lin":
                case null:
                case "":
                {
                    LinLook(k);
                    break;
                }
                default:
                {
                    GenLook(k, who);
                    break;
                }
            }
            if (k.face == null)
            {
                k.face = new List<object[]> {
                    R(8, 12, 2, 2, Ink), R(14, 12, 2, 2, Ink), R(8, 10, 3, 1, k.brow), R(13, 10, 3, 1, k.brow),
                    R(11, 14, 2, 1, k.s2), R(10, 16, 4, 1, k.mouth)
                };
            }
            return k;
        }

        static void LinLook(Look k)
        {
            {
                {
                    // lin：林經理
                    string hr = "#2a2026";
                    k.body.Add(R(2, 21, 20, 7, "#3a3f58")); k.body.Add(R(9, 20, 6, 8, "#8fb3d9")); k.body.Add(R(9, 20, 2, 2, "#dbe6f0"));
                    k.body.Add(R(13, 20, 2, 2, "#dbe6f0")); k.body.Add(R(11, 22, 2, 6, "#a8352d"));
                    k.front.Add(R(6, 4, 12, 3, hr)); k.front.Add(R(6, 7, 5, 1, hr)); k.front.Add(R(5, 5, 1, 6, hr)); k.front.Add(R(18, 5, 1, 4, hr));
                    k.front.Add(R(7, 11, 4, 1, Ink)); k.front.Add(R(7, 14, 4, 1, Ink)); k.front.Add(R(7, 11, 1, 4, Ink)); k.front.Add(R(10, 11, 1, 4, Ink));
                    k.front.Add(R(13, 11, 4, 1, Ink)); k.front.Add(R(13, 14, 4, 1, Ink)); k.front.Add(R(13, 11, 1, 4, Ink)); k.front.Add(R(16, 11, 1, 4, Ink));
                    k.front.Add(R(11, 12, 2, 1, Ink));
                }
            }
        }

        static readonly string[] Skins = { "#f0c9a4", "#e2b78f", "#c99270", "#d9a77f" };
        static readonly string[] Hairs = { "#2a2026", "#3b2a24", "#6b3d2e", "#8f8f9c", "#c9a442", "#3fa7a0", "#a8352d", "#57497a" };
        static readonly string[] Shirts = { "#3a3f58", "#5f7a52", "#a8352d", "#e8c35a", "#57497a", "#2f6f6a", "#e8893a", "#f1e6cc", "#9d90c4", "#4a6b8a" };

        /// <summary>自動產生的客戶頭像：用 id 決定髮型、髮色、膚色、衣服和配件，同一個人每次都長一樣。</summary>
        static void GenLook(Look k, string who)
        {
            int h = 17;
            foreach (char c in who) h = unchecked(h * 31 + c);
            h &= 0x7fffffff;
            k.s1 = Skins[h % Skins.Length];
            k.s2 = k.s1 == "#c99270" ? "#a8714f" : (k.s1 == "#f0c9a4" ? "#d6a580" : "#b98260");
            string hr = Hairs[(h / 5) % Hairs.Length];
            string sh = Shirts[(h / 41) % Shirts.Length];
            int style = (h / 7) % 5;
            k.brow = hr == "#8f8f9c" || hr == "#c9a442" ? "#5e5140" : hr;
            k.body.Add(R(2, 21, 20, 7, sh));
            k.body.Add(R(9, 20, 6, 2, "#e9dcbc"));
            if ((h / 3) % 3 == 0) k.body.Add(R(11, 21, 2, 7, "#a8352d"));
            switch (style)
            {
                case 0: // 短髮
                    k.front.Add(R(6, 3, 12, 4, hr)); k.front.Add(R(5, 5, 1, 5, hr)); k.front.Add(R(18, 5, 1, 5, hr)); k.front.Add(R(7, 7, 4, 1, hr));
                    break;
                case 1: // 長髮
                    k.back.Add(R(4, 5, 16, 17, hr));
                    k.front.Add(R(6, 4, 12, 3, hr)); k.front.Add(R(5, 5, 1, 4, hr)); k.front.Add(R(18, 5, 1, 4, hr)); k.front.Add(R(6, 7, 4, 2, hr)); k.front.Add(R(14, 7, 4, 2, hr));
                    break;
                case 2: // 鮑伯頭
                    k.back.Add(R(5, 5, 14, 13, hr));
                    k.front.Add(R(6, 4, 12, 3, hr)); k.front.Add(R(6, 7, 12, 2, hr)); k.front.Add(R(5, 6, 2, 11, hr)); k.front.Add(R(17, 6, 2, 11, hr));
                    break;
                case 3: // 平頭
                    k.front.Add(R(6, 4, 12, 2, hr)); k.front.Add(R(5, 6, 1, 3, hr)); k.front.Add(R(18, 6, 1, 3, hr));
                    break;
                default: // 刺蝟頭
                    k.front.Add(R(6, 3, 12, 4, hr)); k.front.Add(R(7, 1, 2, 2, hr)); k.front.Add(R(11, 0, 2, 3, hr)); k.front.Add(R(15, 1, 2, 2, hr));
                    k.front.Add(R(5, 5, 1, 5, hr)); k.front.Add(R(18, 5, 1, 5, hr));
                    break;
            }
            if ((h / 11) % 4 == 0)
            {
                k.front.Add(R(7, 11, 4, 1, Ink)); k.front.Add(R(7, 14, 4, 1, Ink)); k.front.Add(R(7, 11, 1, 4, Ink)); k.front.Add(R(10, 11, 1, 4, Ink));
                k.front.Add(R(13, 11, 4, 1, Ink)); k.front.Add(R(13, 14, 4, 1, Ink)); k.front.Add(R(13, 11, 1, 4, Ink)); k.front.Add(R(16, 11, 1, 4, Ink));
                k.front.Add(R(11, 12, 2, 1, Ink));
            }
            else if ((h / 13) % 6 == 0)
            {
                k.front.Add(R(5, 2, 14, 3, sh)); k.front.Add(R(5, 5, 16, 1, sh));
            }
        }

        static Sprite Build(string who)
        {
            Look k = Define(who);
            List<object[]> all = new List<object[]>();
            all.AddRange(k.back);
            all.Add(R(10, 18, 4, 3, k.s2));
            all.AddRange(k.body);
            all.Add(R(7, 6, 10, 1, k.s1));
            all.Add(R(6, 7, 12, 11, k.s1));
            all.Add(R(7, 18, 10, 1, k.s2));
            all.Add(R(17, 8, 1, 9, k.s2));
            all.Add(R(5, 11, 1, 3, k.s1));
            all.Add(R(18, 11, 1, 3, k.s2));
            all.AddRange(k.face);
            all.AddRange(k.front);

            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontSave;
            Color32[] px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Pal.Clear;
            foreach (object[] r in all)
            {
                int rx = (int)r[0], ry = (int)r[1], rw = (int)r[2], rh = (int)r[3];
                Color32 c = Pal.Hex((string)r[4]);
                for (int yy = ry; yy < ry + rh; yy++)
                    for (int xx = rx; xx < rx + rw; xx++)
                    {
                        if (xx < 0 || xx >= W || yy < 0 || yy >= H) continue;
                        px[(H - 1 - yy) * W + xx] = c;
                    }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
