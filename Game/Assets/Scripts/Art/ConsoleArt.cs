using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>經紀工作台的像素素材。互動與座標由各畫面管理。</summary>
    public static class ConsoleArt
    {
        static readonly Sprite Chassis = Resources.Load<Sprite>("Computer/broker-console-v2");
        static readonly Sprite Bezel = Resources.Load<Sprite>("Computer/console-panel-v2");
        static readonly Sprite Well = Resources.Load<Sprite>("Computer/console-slot-v2");

        public static bool ApplyChassis(Image image)
        {
            if (Chassis == null) return false;
            image.sprite = Chassis;
            image.color = Pal.White;
            return true;
        }

        public static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h,
            Color32 fill, Color32 border)
        {
            RectTransform panel = Px.Box(parent, name, x, y, w, h, fill, Bezel == null ? border : Pal.Clear);
            if (Bezel != null) ApplySlice(panel.GetComponent<Image>(), Bezel);
            return panel;
        }

        public static bool ApplyWell(Image image)
        {
            if (Well == null) return false;
            ApplySlice(image, Well);
            return true;
        }

        public static bool DrawWell(Transform parent, float x, float y, float w, float h)
        {
            if (Well == null) return false;
            ApplySlice(Px.Fill(parent, x, y, w, h, Pal.White), Well);
            return true;
        }

        public static void FrameCamera(Transform parent, float x, float y, float w, float h)
        {
            if (Bezel == null) return;
            Image rim = Px.Fill(parent, x, y, w, h, Pal.White);
            ApplySlice(rim, Bezel);
            rim.fillCenter = false;
        }

        public static void DrawDocumentIcon(Transform parent, string id, float alpha)
        {
            if (!IconArt.TryDraw(parent, id, alpha, 8, 1, 32))
                Icon(parent, string.IsNullOrEmpty(id) ? "report" : id, 12, 6, 24, alpha);
        }

        public static Image Icon(Transform parent, string id, float x, float y, int size = 7, float alpha = 1f)
        {
            Image image = Px.Fill(parent, x, y, size, size, Pal.Alpha(Pal.White, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f)));
            image.name = "Icon_" + id;
            image.sprite = ConsoleIcons.Get(id);
            return image;
        }

        static void ApplySlice(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.color = Pal.White;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
        }

        public static RectTransform Key(Transform parent, string text, float x, float y, float w, float h,
            Color32 fill, Color32 ink, Color32 border, Action action, string icon = null)
        {
            RectTransform key = Px.Button(parent, text, x, y, w, h, fill, ink, border, action);
            Px.Fill(key, 1, 1, w - 2, 1, Pal.Alpha(Pal.Cream, 78));
            Px.Fill(key, 1, h - 2, w - 2, 1, Pal.Alpha(Pal.Ink, 134));
            if (icon != null)
            {
                int textWidth = Px.TextWidth(text);
                float left = Mathf.Max(2, Mathf.Floor((w - textWidth - 9) * 0.5f));
                Text label = key.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.UpperLeft;
                Px.Move(label.rectTransform, left + 9, Mathf.Floor((h - Px.LineHeight) * 0.5f));
                label.rectTransform.sizeDelta = new Vector2(w - left - 10, Px.LineHeight);
                float iconY = Mathf.Floor((h - 7) * 0.5f);
                Px.Fill(key, left - 1, iconY - 1, 9, 9, Pal.Night);
                Icon(key, icon, left, iconY);
            }
            return key;
        }
    }
}
