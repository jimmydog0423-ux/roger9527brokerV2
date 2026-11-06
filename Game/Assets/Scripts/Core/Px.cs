using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 像素 UI 小工具。所有座標都是「邏輯像素、以父物件左上角為原點、y 往下」，
    /// 跟網頁雛形的寫法一樣，方便直接對照數字。
    /// </summary>
    public static class Px
    {
        public static Font font;
        /// <summary>手機或平板（網頁版在手機上打開時也是）。</summary>
        public static readonly bool Mobile = Application.isMobilePlatform;
        public const int FontSize = 12;
        public const int LineHeight = 14;

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Place(rt, x, y, w, h);
            return rt;
        }

        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Move(RectTransform rt, float x, float y)
        {
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Fill(Transform parent, float x, float y, float w, float h, Color32 c)
        {
            RectTransform rt = Rect(parent, "Fill", x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Image Frame(Transform parent, float x, float y, float w, float h, Color32 c, int thick)
        {
            RectTransform rt = Rect(parent, "Frame", x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = PixelSprites.Frame(thick);
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Image Frame(Transform parent, float x, float y, float w, float h, Color32 c)
        {
            return Frame(parent, x, y, w, h, c, 1);
        }

        /// <summary>有底色和邊框的方塊。回傳的 RectTransform 可以再放子物件。</summary>
        public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color32 fill, Color32 border, int thick)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h);
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = fill;
            bg.raycastTarget = false;
            if (border.a > 0)
            {
                Image fr = Frame(rt, 0, 0, w, h, border, thick);
                fr.name = "Border";
                // 邊框跟著方塊大小走，之後用 Place 改尺寸也不會錯位
                RectTransform frt = fr.rectTransform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;
            }
            return rt;
        }

        public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color32 fill, Color32 border)
        {
            return Box(parent, name, x, y, w, h, fill, border, 1);
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, Color32 c, TextAnchor anchor, bool wrap)
        {
            RectTransform rt = Rect(parent, "Text", x, y, w, h);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = FontSize;
            t.lineSpacing = 1f;
            t.supportRichText = true;
            t.alignment = anchor;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = c;
            t.raycastTarget = false;
            // 單行文字超出框框時自動截斷，不會跑到框外
            t.text = wrap ? text : Fit(text, w + 1);
            return t;
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, Color32 c)
        {
            return Label(parent, text, x, y, w, h, c, TextAnchor.UpperLeft, false);
        }

        public static Image Tiled(Transform parent, float x, float y, float w, float h, Sprite s, Color32 c)
        {
            RectTransform rt = Rect(parent, "Tiled", x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = Image.Type.Tiled;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>1 像素虛線外框。</summary>
        public static void DashedRect(Transform parent, float x, float y, float w, float h, Color32 c)
        {
            Tiled(parent, x, y, w, 1, PixelSprites.DashH, c);
            Tiled(parent, x, y + h - 1, w, 1, PixelSprites.DashH, c);
            Tiled(parent, x, y, 1, h, PixelSprites.DashV, c);
            Tiled(parent, x + w - 1, y, 1, h, PixelSprites.DashV, c);
        }

        /// <summary>可點擊的按鈕：方塊 + 置中文字。</summary>
        public static RectTransform Button(Transform parent, string label, float x, float y, float w, float h, Color32 bg, Color32 fg, Color32 border, Action onClick)
        {
            RectTransform rt = Box(parent, "Btn_" + label, x, y, w, h, bg, border);
            Image img = rt.GetComponent<Image>();
            img.raycastTarget = true;
            // 手機上按鈕很小，可點的範圍往外多 2 個遊戲像素
            if (Mobile) img.raycastPadding = new Vector4(-2, -2, -2, -2);
            Label(rt, Fit(label, w - 4), 0, Mathf.Floor((h - LineHeight) * 0.5f), w, LineHeight, fg, TextAnchor.UpperCenter, false);
            Clickable c = rt.gameObject.AddComponent<Clickable>();
            c.onClick = onClick;
            return rt;
        }

        /// <summary>讓一個方塊變成可點擊。</summary>
        public static Clickable MakeClickable(RectTransform rt, Action onClick)
        {
            Image img = rt.GetComponent<Image>();
            if (img == null)
            {
                img = rt.gameObject.AddComponent<Image>();
                img.color = Pal.Clear;
            }
            img.raycastTarget = true;
            Clickable c = rt.gameObject.AddComponent<Clickable>();
            c.onClick = onClick;
            return c;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject go = t.GetChild(i).gameObject;
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        // 俐方體11號在 12 號字的實際字寬（從字型檔量出來的）
        const string W4 = " !',.:;I`ijl";
        const string W5 = "rt";
        const string W6 = "\"()*1[]^abcdefghknopqsuvxyz|";
        const string W7 = "#$&-023456789=?BCEFJKLPRSXZ_~";
        const string W8 = "%+/<>@ADGHNOQTUVY\\m{}";

        public static int CharWidth(char ch)
        {
            if (ch >= 128) return ch >= '\uFF61' && ch <= '\uFF9F' ? 6 : 13;
            if (W4.IndexOf(ch) >= 0) return 4;
            if (W5.IndexOf(ch) >= 0) return 5;
            if (W6.IndexOf(ch) >= 0) return 6;
            if (W7.IndexOf(ch) >= 0) return 7;
            if (W8.IndexOf(ch) >= 0) return 8;
            if (ch == 'M' || ch == 'W' || ch == 'w') return 10;
            return 8;
        }

        /// <summary>一行文字的寬度（像素）。&lt;color&gt; 這類標籤不算寬度。</summary>
        public static int TextWidth(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int w = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int skip = TagLength(s, i);
                if (skip > 0)
                {
                    i += skip - 1;
                    continue;
                }
                w += CharWidth(s[i]);
            }
            return w;
        }

        /// <summary>s[i] 開始如果是 &lt;color=...&gt;、&lt;/color&gt;、&lt;b&gt; 之類的標籤，回傳標籤長度。</summary>
        static int TagLength(string s, int i)
        {
            if (s[i] != '<') return 0;
            int end = s.IndexOf('>', i);
            if (end < 0) return 0;
            string tag = s.Substring(i + 1, end - i - 1).TrimStart('/');
            if (tag == "b" || tag == "i" || tag.StartsWith("color") || tag.StartsWith("size")) return end - i + 1;
            return 0;
        }

        /// <summary>太長就從後面截掉、補上「…」，保證不超過 width。</summary>
        public static string Fit(string text, float width)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.IndexOf('\n') >= 0)
            {
                string[] lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = Fit(lines[i], width);
                return string.Join("\n", lines);
            }
            if (TextWidth(text) <= width) return text;
            if (text.IndexOf('<') >= 0) return text;   // 有標籤的不截，避免把標籤切壞
            int w = CharWidth('…');
            int n = 0;
            while (n < text.Length && w + CharWidth(text[n]) <= width)
            {
                w += CharWidth(text[n]);
                n++;
            }
            return text.Substring(0, n) + "…";
        }

        /// <summary>把子物件的範圍換算成 root（640x360）左上角座標系。</summary>
        public static Rect ToRoot(RectTransform root, RectTransform child)
        {
            Vector3[] wc = new Vector3[4];
            child.GetWorldCorners(wc);
            Vector3 bl = root.InverseTransformPoint(wc[0]);
            Vector3 tr = root.InverseTransformPoint(wc[2]);
            Rect r = root.rect;
            float x = bl.x - r.xMin;
            float y = r.yMax - tr.y;
            return new Rect(x, y, tr.x - bl.x, tr.y - bl.y);
        }

        /// <summary>把螢幕座標換算成 root 左上角座標系。</summary>
        public static Vector2 ScreenToRoot(RectTransform root, Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out local);
            Rect r = root.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }
    }
}
