using System;
using System.Collections.Generic;
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
        static readonly Dictionary<string, Sprite> Documents = new Dictionary<string, Sprite>();

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
            // 需求單和報價單都用帶夾板的表單，名稱仍由案件資料顯示。
            string spriteId = id == "request" ? "quote" : id;
            if (!string.IsNullOrEmpty(spriteId))
            {
                if (!Documents.TryGetValue(spriteId, out Sprite sprite))
                {
                    sprite = Resources.Load<Sprite>("Computer/Documents/" + spriteId);
                    Documents.Add(spriteId, sprite);
                }
                if (sprite != null)
                {
                    Px.Fill(parent, 4, 2, 40, 31, Pal.Alpha(Pal.White, (byte)Mathf.RoundToInt(alpha * 255f))).sprite = sprite;
                    return;
                }
            }
            IconArt.Draw(parent, id, alpha);
        }

        static void ApplySlice(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.color = Pal.White;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
        }

        public static RectTransform Key(Transform parent, string text, float x, float y, float w, float h,
            Color32 fill, Color32 ink, Color32 border, Action action)
        {
            RectTransform key = Px.Button(parent, text, x, y, w, h, fill, ink, border, action);
            Px.Fill(key, 1, 1, w - 2, 1, Pal.Alpha(Pal.Cream, 78));
            Px.Fill(key, 1, h - 2, w - 2, 1, Pal.Alpha(Pal.Ink, 134));
            return key;
        }
    }
}
