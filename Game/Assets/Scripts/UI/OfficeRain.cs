using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>將窗外的雨畫成同一張 UI 網格，並沿窗框和盆栽剪裁。</summary>
    public sealed class OfficeRain : MaskableGraphic
    {
        const int WindowTop = 25;
        const int WindowBottom = 199;
        const int WindowLeft = 18;
        const int WindowRight = 175;
        const int FramesPerSecond = 24;

        struct Drop
        {
            public int x, phase, length, speed;
            public byte alpha;
        }

        readonly Drop[] drops = new Drop[62];
        int frame;

        public static void Build(Transform parent)
        {
            RectTransform layer = Px.Rect(parent, "WindowRain", WindowLeft, WindowTop,
                WindowRight - WindowLeft, WindowBottom - WindowTop);
            layer.gameObject.AddComponent<OfficeRain>().raycastTarget = false;
        }

        protected override void Awake()
        {
            base.Awake();
            var random = new System.Random(371);
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = new Drop
                {
                    x = random.Next(WindowLeft, WindowRight + 23),
                    phase = random.Next(WindowBottom - WindowTop + 12),
                    length = random.Next(4, 11),
                    speed = random.Next(105, 225),
                    alpha = (byte)random.Next(70, 152)
                };
            }
        }

        void Update()
        {
            int nextFrame = Mathf.FloorToInt(Time.unscaledTime * FramesPerSecond);
            if (canvasRenderer.cull || nextFrame == frame) return;
            frame = nextFrame;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float time = frame / (float)FramesPerSecond;
            foreach (Drop drop in drops)
            {
                float travel = time * drop.speed + drop.phase;
                int y = WindowTop + (int)Mathf.Repeat(travel, WindowBottom - WindowTop + drop.length);
                for (int offset = 0; offset < drop.length; offset++)
                {
                    int pixelY = y - offset;
                    int pixelX = drop.x - (pixelY - WindowTop) / 9;
                    if (!OnGlass(pixelX, pixelY)) continue;
                    byte alpha = (byte)(drop.alpha * (drop.length - offset) / drop.length);
                    AddPixel(mesh, pixelX - WindowLeft, WindowTop - pixelY, new Color32(181, 202, 242, alpha));
                }
            }
        }

        static bool OnGlass(int x, int y)
        {
            bool topPane = y >= WindowTop && y < 40;
            bool bottomPane = y >= 50 && y < WindowBottom;
            bool leftPane = x >= WindowLeft && x < 55;
            bool rightPane = x >= 67 && x < WindowRight;
            // 左下的盆栽會遮住靠近窗柱的雨絲。
            return (leftPane && (topPane || (bottomPane && y < 148)))
                || (rightPane && (topPane || (bottomPane && (x >= 76 || y < 154))));
        }

        static void AddPixel(VertexHelper mesh, int x, int y, Color32 color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), color, Vector2.zero);
            mesh.AddVert(new Vector3(x + 1, y), color, Vector2.zero);
            mesh.AddVert(new Vector3(x + 1, y - 1), color, Vector2.zero);
            mesh.AddVert(new Vector3(x, y - 1), color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
