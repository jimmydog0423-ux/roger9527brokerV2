using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 讓 640x360 的畫面以整數倍放大，並對齊螢幕像素，避免像素被拉歪。
    /// 空間可容納兩倍畫面時優先用整數倍率。較小的視窗維持可讀的畫面大小。
    /// 視窗模式下玩家拉成別的比例，放開後會自動調回 16:9。
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class PixelScaler : MonoBehaviour
    {
        public const int Width = 640;
        public const int Height = 360;

        public RectTransform root;
        public bool integerScaling = true;

        CanvasScaler scaler;
        int lastW, lastH;
        bool lastInteger;
        float changedAt;
        bool pendingSnap;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100;
        }

        void LateUpdate()
        {
            if (Screen.width != lastW || Screen.height != lastH || integerScaling != lastInteger)
            {
                bool resized = lastW != 0 && (Screen.width != lastW || Screen.height != lastH);
                Apply();
                if (resized)
                {
                    changedAt = Time.unscaledTime;
                    pendingSnap = true;
                }
            }
            if (pendingSnap && Time.unscaledTime - changedAt > 0.6f)
            {
                pendingSnap = false;
                SnapWindow();
            }
        }

        /// <summary>視窗模式：把視窗調回 16:9，畫面兩側或上下就不會出現黑邊。</summary>
        static void SnapWindow()
        {
            if (Application.isEditor || Screen.fullScreenMode != FullScreenMode.Windowed) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer) return;
            int w = Screen.width, h = Screen.height;
            if (h <= 0 || Mathf.Abs(w / (float)h - 16f / 9f) < 0.02f) return;
            // 視窗按了最大化就不去改它，只在兩側或上下留黑邊
            Resolution d = Screen.currentResolution;
            if (w >= d.width * 0.95f || h >= d.height * 0.88f) return;
            int nw = w, nh = Mathf.RoundToInt(w * 9f / 16f);
            int maxH = Mathf.RoundToInt(d.height * 0.85f);
            if (nh > maxH)
            {
                nh = maxH;
                nw = Mathf.RoundToInt(nh * 16f / 9f);
            }
            Screen.SetResolution(Mathf.Max(640, nw), Mathf.Max(360, nh), FullScreenMode.Windowed);
        }

        public void Apply()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastInteger = integerScaling;
            float fit = Mathf.Min(lastW / (float)Width, lastH / (float)Height);
            float s = integerScaling && fit >= 2f ? Mathf.Floor(fit) : fit;
            if (s <= 0f) s = 1f;
            scaler.scaleFactor = s;
            // 手指點下去稍微晃一下，不要被當成拖曳（大約 6 個遊戲像素）
            if (EventSystem.current != null) EventSystem.current.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(s * 6));
            if (root == null) return;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.sizeDelta = new Vector2(Width, Height);
            float left = Mathf.Floor((lastW - Width * s) * 0.5f);
            float bottom = Mathf.Floor((lastH - Height * s) * 0.5f);
            root.anchoredPosition = new Vector2(left / s, bottom / s);
        }
    }
}
