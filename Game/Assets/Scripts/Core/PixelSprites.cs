using UnityEngine;

namespace SSD
{
    /// <summary>
    /// 執行時產生的小貼圖：邊框、虛線、點線、格線等。
    /// 全部用 Point 取樣，1 張貼圖像素 = 1 個邏輯像素。
    /// </summary>
    public static class PixelSprites
    {
        static Sprite frame1, frame2, dashH, dashV, dotH, grid16, blinds, keys;

        static Texture2D Tex(int w, int h, TextureWrapMode wrap)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = wrap;
            t.hideFlags = HideFlags.DontSave;
            return t;
        }

        static Sprite Make(Texture2D t, Vector4 border)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        internal static Sprite FromRows(string[] rows, System.Func<char, Color32> color, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            int w = rows[0].Length;
            int h = rows.Length;
            Texture2D tex = Tex(w, h, wrap);
            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[(h - 1 - y) * w + x] = color(rows[y][x]);
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Make(tex, Vector4.zero);
        }

        /// <summary>白色邊框（中間透明），用 Sliced 模式拉伸，靠 Image.color 上色。</summary>
        public static Sprite Frame(int thick)
        {
            if (thick >= 2)
            {
                if (frame2 == null) frame2 = BuildFrame(2);
                return frame2;
            }
            if (frame1 == null) frame1 = BuildFrame(1);
            return frame1;
        }

        static Sprite BuildFrame(int t)
        {
            int n = t * 2 + 1;
            Texture2D tex = Tex(n, n, TextureWrapMode.Clamp);
            Color32[] px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool edge = x < t || y < t || x >= n - t || y >= n - t;
                    px[y * n + x] = edge ? Pal.White : Pal.Clear;
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Make(tex, new Vector4(t, t, t, t));
        }

        /// <summary>橫向虛線：2 實 2 空。</summary>
        public static Sprite DashH
        {
            get
            {
                if (dashH == null)
                {
                    Texture2D t = Tex(4, 1, TextureWrapMode.Repeat);
                    t.SetPixels32(new[] { Pal.White, Pal.White, Pal.Clear, Pal.Clear });
                    t.Apply();
                    dashH = Make(t, Vector4.zero);
                }
                return dashH;
            }
        }

        /// <summary>直向虛線：2 實 2 空。</summary>
        public static Sprite DashV
        {
            get
            {
                if (dashV == null)
                {
                    Texture2D t = Tex(1, 4, TextureWrapMode.Repeat);
                    t.SetPixels32(new[] { Pal.White, Pal.White, Pal.Clear, Pal.Clear });
                    t.Apply();
                    dashV = Make(t, Vector4.zero);
                }
                return dashV;
            }
        }

        /// <summary>點線：1 實 1 空，用在文件欄位底線。</summary>
        public static Sprite DotH
        {
            get
            {
                if (dotH == null)
                {
                    Texture2D t = Tex(2, 1, TextureWrapMode.Repeat);
                    t.SetPixels32(new[] { Pal.White, Pal.Clear });
                    t.Apply();
                    dotH = Make(t, Vector4.zero);
                }
                return dotH;
            }
        }

        /// <summary>16x16 格線，左邊和上面各一條線。</summary>
        public static Sprite Grid16
        {
            get
            {
                if (grid16 == null)
                {
                    Texture2D t = Tex(16, 16, TextureWrapMode.Repeat);
                    Color32[] px = new Color32[256];
                    for (int y = 0; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                            px[y * 16 + x] = (x == 0 || y == 15) ? Pal.White : Pal.Clear;
                    t.SetPixels32(px);
                    t.Apply();
                    grid16 = Make(t, Vector4.zero);
                }
                return grid16;
            }
        }

        /// <summary>百葉窗紋理（已含顏色）。</summary>
        public static Sprite Blinds
        {
            get
            {
                if (blinds == null)
                {
                    Texture2D t = Tex(1, 4, TextureWrapMode.Repeat);
                    Color32 a = Pal.Hex("#6f8fa8");
                    Color32 b = Pal.Hex("#5d7a92");
                    t.SetPixels32(new[] { b, a, a, a });
                    t.Apply();
                    blinds = Make(t, Vector4.zero);
                }
                return blinds;
            }
        }

        /// <summary>鍵盤按鍵紋理（已含顏色）：10 鍵 2 縫。</summary>
        public static Sprite Keys
        {
            get
            {
                if (keys == null)
                {
                    Texture2D t = Tex(12, 1, TextureWrapMode.Repeat);
                    Color32 k = Pal.Hex("#4a4858");
                    Color32 g = Pal.Hex("#2a2833");
                    Color32[] px = new Color32[12];
                    for (int i = 0; i < 12; i++) px[i] = i < 10 ? k : g;
                    t.SetPixels32(px);
                    t.Apply();
                    keys = Make(t, Vector4.zero);
                }
                return keys;
            }
        }
    }
}
