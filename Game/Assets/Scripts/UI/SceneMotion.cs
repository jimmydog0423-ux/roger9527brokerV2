using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>點陣雨絲、光暈與反光。每個場景共用一張網格及貼圖。</summary>
    public sealed class SceneMotion : MaskableGraphic
    {
        const int Width = 640, Height = 360, FramesPerSecond = 24, GlowSize = 32;
        const string LayerName = "SceneMotion";
        static readonly Vector2 SolidUV = new Vector2((GlowSize + 1f) / (GlowSize + 2f), 0.5f);
        static readonly Vector2 GlowUV = new Vector2(GlowSize / (GlowSize + 2f), 1f);
        static Texture2D atlas;

        enum Motion { Breath, Flicker, Sweep }

        readonly struct Light
        {
            public readonly RectInt area;
            public readonly Color32 tint;
            public readonly float period, phase;
            public readonly Motion motion;

            public Light(int x, int y, int w, int h, Color32 tint, float period, float phase, Motion motion)
            {
                area = new RectInt(x, y, w, h);
                this.tint = tint;
                this.period = period;
                this.phase = phase;
                this.motion = motion;
            }
        }

        sealed class Rain
        {
            public readonly RectInt area;
            public readonly RectInt[] panes;
            public readonly int count, seed;

            public Rain(int x, int y, int w, int h, int count, int seed, params RectInt[] panes)
            {
                area = new RectInt(x, y, w, h);
                this.panes = panes;
                this.count = count;
                this.seed = seed;
            }

            public bool Contains(int x, int y)
            {
                if (panes.Length == 0) return area.Contains(new Vector2Int(x, y));
                foreach (RectInt pane in panes)
                    if (pane.Contains(new Vector2Int(x, y))) return true;
                return false;
            }
        }

        sealed class Design
        {
            public readonly Rain rain;
            public readonly Light[] lights;

            public Design(Rain rain, params Light[] lights)
            {
                this.rain = rain;
                this.lights = lights;
            }
        }

        struct Drop
        {
            public int x, phase, length, speed;
            public byte alpha;
        }

        Design design;
        Drop[] drops;
        int frame = -1;

        public override Texture mainTexture
        {
            get
            {
                if (atlas == null) atlas = BuildAtlas();
                return atlas;
            }
        }

        public static SceneMotion Attach(Image backdrop, string id)
        {
            Transform existing = backdrop.transform.Find(LayerName);
            if (id == null || !Designs.TryGetValue(id, out Design preset))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return null;
            }

            SceneMotion layer;
            if (existing != null) layer = existing.GetComponent<SceneMotion>();
            else
            {
                RectTransform rect = Px.Stretch(backdrop.transform, LayerName);
                layer = rect.gameObject.AddComponent<SceneMotion>();
                layer.raycastTarget = false;
            }
            if (layer.design != preset) layer.Configure(preset);
            layer.gameObject.SetActive(true);
            return layer;
        }

        void Configure(Design preset)
        {
            design = preset;
            Rain rain = preset.rain;
            if (rain != null)
            {
                drops = new Drop[rain.count];
                var random = new System.Random(rain.seed);
                for (int i = 0; i < drops.Length; i++)
                {
                    drops[i] = new Drop
                    {
                        x = random.Next(rain.area.xMin, rain.area.xMax + rain.area.height / 9 + 1),
                        phase = random.Next(rain.area.height + 12),
                        length = random.Next(4, 11),
                        speed = random.Next(105, 225),
                        alpha = (byte)random.Next(80, 164)
                    };
                }
            }
            frame = CurrentFrame();
            SetVerticesDirty();
        }

        static int CurrentFrame() => Mathf.FloorToInt(Time.unscaledTime * FramesPerSecond);

        void Update()
        {
            int next = CurrentFrame();
            if (next == frame || canvasRenderer.cull || canvasRenderer.GetInheritedAlpha() <= 0.01f) return;
            frame = next;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (design == null) return;
            float time = frame / (float)FramesPerSecond;
            Rect rect = rectTransform.rect;
            foreach (Light light in design.lights) DrawLight(mesh, rect, light, time);
            if (design.rain != null) DrawRain(mesh, rect, design.rain, time);
        }

        static void DrawLight(VertexHelper mesh, Rect rect, Light light, float time)
        {
            float cycle = Mathf.Repeat(time / light.period + light.phase, 1f);
            float wave = Mathf.Sin(cycle * 2f * Mathf.PI);
            RectInt spot = light.area;
            float strength = 0.68f + 0.26f * wave;
            if (light.motion == Motion.Flicker)
            {
                // 兩次短暫接觸不良，各光源的週期和起始相位不同。
                bool dip = (cycle > 0.22f && cycle < 0.25f) || (cycle > 0.272f && cycle < 0.292f);
                if (dip)
                {
                    Color32 dim = Pal.Alpha(Pal.Ink, (byte)(light.tint.a * 0.82f));
                    Quad(mesh, rect, spot.x, spot.y, spot.width, spot.height, dim, Vector2.zero, GlowUV);
                    strength = 0.12f;
                }
            }
            if (light.motion == Motion.Sweep)
            {
                spot.x += Mathf.RoundToInt(wave * spot.width * 0.18f);
                spot.y += Mathf.RoundToInt(Mathf.Cos(cycle * 2f * Mathf.PI) * spot.height * 0.20f);
            }
            Color32 tint = Pal.Alpha(light.tint, (byte)Mathf.RoundToInt(light.tint.a * strength));
            Quad(mesh, rect, spot.x, spot.y, spot.width, spot.height, tint, Vector2.zero, GlowUV);
        }

        void DrawRain(VertexHelper mesh, Rect rect, Rain rain, float time)
        {
            foreach (Drop drop in drops)
            {
                float travel = time * drop.speed + drop.phase;
                int y = rain.area.yMin + (int)Mathf.Repeat(travel, rain.area.height + drop.length);
                for (int offset = 0; offset < drop.length; offset++)
                {
                    int py = y - offset;
                    int px = drop.x - (py - rain.area.yMin) / 9;
                    if (!rain.Contains(px, py)) continue;
                    byte alpha = (byte)(drop.alpha * (drop.length - offset) / drop.length);
                    Quad(mesh, rect, px, py, 1, 1, new Color32(188, 204, 242, alpha), SolidUV, SolidUV);
                }
            }
        }

        static void Quad(VertexHelper mesh, Rect rect, int x, int y, int w, int h, Color32 tint, Vector2 uvMin, Vector2 uvMax)
        {
            float sx = rect.width / Width, sy = rect.height / Height;
            float l = rect.xMin + x * sx, r = l + w * sx;
            float t = rect.yMax - y * sy, b = t - h * sy;
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(l, t), tint, new Vector2(uvMin.x, uvMax.y));
            mesh.AddVert(new Vector3(r, t), tint, uvMax);
            mesh.AddVert(new Vector3(r, b), tint, new Vector2(uvMax.x, uvMin.y));
            mesh.AddVert(new Vector3(l, b), tint, uvMin);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        static Texture2D BuildAtlas()
        {
            const int stride = GlowSize + 2;
            var texture = new Texture2D(stride, GlowSize, TextureFormat.RGBA32, false)
            {
                name = "Scene glow and rain",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[stride * GlowSize];
            for (int y = 0; y < GlowSize; y++)
                for (int x = 0; x < stride; x++)
                {
                    float dx = (x + 0.5f) * 2f / GlowSize - 1f;
                    float dy = (y + 0.5f) * 2f / GlowSize - 1f;
                    float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    byte alpha = x >= GlowSize ? (byte)255 : (byte)Mathf.RoundToInt(255f * falloff * falloff);
                    pixels[y * stride + x] = Pal.Alpha(Pal.White, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Light Warm(int x, int y, int w, int h, byte a, float period, float phase, Motion motion = Motion.Breath)
            => new Light(x, y, w, h, new Color32(255, 173, 89, a), period, phase, motion);
        static Light Cool(int x, int y, int w, int h, byte a, float period, float phase, Motion motion = Motion.Breath)
            => new Light(x, y, w, h, new Color32(99, 239, 211, a), period, phase, motion);

        static readonly Dictionary<string, Design> Designs = new Dictionary<string, Design>
        {
            // 辦公室窗柱和盆栽遮住雨絲，桌燈及桌面反光另行變化。
            { "office", new Design(new Rain(18, 25, 157, 174, 62, 371,
                new RectInt(18, 25, 37, 15), new RectInt(67, 25, 108, 15),
                new RectInt(18, 50, 37, 98), new RectInt(67, 50, 9, 104), new RectInt(76, 50, 99, 149)),
                Warm(533, 163, 112, 41, 134, 6.4f, 0.08f), Warm(499, 239, 140, 60, 119, 8.9f, 0.4f, Motion.Sweep),
                new Light(476, 250, 109, 53, Pal.Alpha(Pal.Ink, 115), 9.4f, 0.12f, Motion.Sweep),
                Cool(129, 105, 70, 46, 88, 7.4f, 0.7f, Motion.Flicker)) },
            { "title", new Design(new Rain(282, 2, 301, 185, 93, 883,
                new RectInt(285, 10, 115, 168), new RectInt(411, 4, 163, 173)),
                Warm(512, 138, 79, 37, 152, 6.3f, 0.1f), Warm(481, 190, 142, 70, 124, 8.6f, 0.48f, Motion.Sweep),
                Cool(485, 36, 101, 102, 115, 7.9f, 0.8f, Motion.Flicker)) },
            { "street", new Design(new Rain(0, 19, 640, 240, 170, 627),
                Warm(13, 63, 96, 42, 123, 5.1f, 0.45f, Motion.Flicker),
                Cool(407, 21, 65, 96, 98, 9.2f, 0.67f, Motion.Flicker),
                Warm(32, 240, 151, 16, 176, 4.1f, 0.2f, Motion.Sweep),
                Cool(451, 241, 123, 16, 172, 5.4f, 0.1f, Motion.Sweep)) },
            { "market", new Design(new Rain(0, 25, 62, 207, 24, 335),
                Warm(69, 27, 154, 28, 119, 5.8f, 0.03f), Cool(0, 38, 83, 171, 135, 7.5f, 0.6f, Motion.Flicker),
                Warm(7, 225, 258, 22, 176, 5.3f, 0.25f, Motion.Sweep), Warm(444, 225, 169, 23, 116, 7.6f, 0.6f, Motion.Sweep)) },
            { "flea", new Design(new Rain(0, 35, 640, 240, 125, 437,
                new RectInt(0, 35, 80, 236), new RectInt(554, 35, 86, 236)),
                Warm(30, 31, 118, 47, 122, 5.8f, 0.78f, Motion.Flicker),
                Warm(503, 31, 122, 47, 129, 7.1f, 0.14f),
                Warm(77, 225, 234, 29, 165, 6.8f, 0.1f, Motion.Sweep)) },
            { "junk", new Design(new Rain(0, 19, 640, 217, 143, 619),
                Warm(72, 43, 77, 58, 155, 7.8f, 0.04f, Motion.Flicker), Cool(521, 29, 116, 78, 127, 9.2f, 0.64f, Motion.Flicker),
                Warm(36, 109, 156, 30, 143, 6.1f, 0.17f, Motion.Sweep),
                Cool(465, 113, 139, 37, 157, 7.9f, 0.39f, Motion.Sweep)) },
            { "story", new Design(new Rain(146, 6, 494, 238, 133, 951,
                new RectInt(151, 18, 107, 213), new RectInt(272, 12, 195, 224), new RectInt(481, 6, 159, 228)),
                Warm(0, 151, 105, 156, 120, 8.5f, 0.27f),
                Cool(488, 74, 141, 124, 110, 7.1f, 0.78f, Motion.Flicker),
                Warm(123, 242, 248, 42, 148, 6.1f, 0.32f, Motion.Sweep)) },
            { "desktop", new Design(new Rain(0, 18, 640, 278, 131, 961),
                Cool(491, 150, 148, 108, 129, 8.1f, 0.12f, Motion.Flicker),
                Warm(51, 280, 221, 57, 155, 6.7f, 0.81f, Motion.Sweep),
                Cool(403, 289, 185, 58, 148, 9.4f, 0.36f, Motion.Sweep)) },
            { "video", new Design(new Rain(0, 112, 104, 151, 28, 97),
                Warm(439, 135, 190, 117, 121, 5.9f, 0.39f), Cool(7, 148, 107, 101, 79, 7.1f, 0.83f, Motion.Flicker)) },

            { "Fronts/office", new Design(null, Warm(244, 90, 379, 161, 136, 7.3f, 0.16f)) },
            { "Fronts/market", new Design(null, Cool(9, 121, 495, 150, 180, 6.4f, 0.63f, Motion.Flicker)) },
            { "Fronts/flea", new Design(null, Warm(59, 117, 483, 160, 137, 5.4f, 0.9f)) },
            { "Fronts/junk", new Design(null, Warm(222, 77, 369, 129, 153, 8.2f, 0.38f, Motion.Flicker)) },
            { "Fronts/stirfry", new Design(null, Warm(28, 89, 423, 166, 164, 5.9f, 0.16f)) },
            { "Fronts/netcafe", new Design(null, Cool(39, 115, 508, 133, 154, 7.2f, 0.71f, Motion.Flicker)) },
            { "Fronts/temple", new Design(null, Warm(135, 105, 420, 208, 152, 6.9f, 0.72f, Motion.Sweep)) },
            { "Fronts/roger", new Design(null, Warm(201, 107, 424, 178, 135, 6.3f, 0.42f)) },
            { "Fronts/bar", new Design(null,
                new Light(23, 113, 566, 194, new Color32(202, 130, 255, 150), 7.7f, 0.34f, Motion.Flicker)) },

            { "Venues/office", new Design(new Rain(226, 8, 414, 184, 96, 427,
                new RectInt(271, 18, 108, 141), new RectInt(402, 17, 221, 158)),
                Warm(91, 123, 170, 120, 142, 6.7f, 0.4f), Warm(5, 230, 224, 57, 126, 8.8f, 0.18f, Motion.Sweep)) },
            { "Venues/stirfry", new Design(null, Warm(35, 25, 206, 118, 159, 5.7f, 0.12f),
                Warm(484, 104, 156, 151, 164, 4.1f, 0.44f, Motion.Sweep)) },
            { "Venues/netcafe", new Design(null, Cool(0, 118, 181, 119, 158, 5.5f, 0.26f, Motion.Flicker),
                Cool(480, 99, 160, 136, 158, 8.6f, 0.58f, Motion.Flicker)) },
            { "Venues/temple", new Design(null, Warm(7, 52, 134, 160, 150, 6.5f, 0.32f),
                Warm(453, 187, 183, 106, 136, 8.3f, 0.7f, Motion.Sweep)) },
            { "Venues/roger", new Design(new Rain(382, 35, 258, 220, 75, 732),
                Warm(22, 34, 196, 132, 152, 5.8f, 0.45f), Warm(489, 249, 115, 47, 109, 7.8f, 0.65f, Motion.Sweep)) },
            { "Venues/bar", new Design(new Rain(299, 34, 341, 199, 90, 731),
                Warm(10, 37, 165, 96, 122, 7.8f, 0.27f),
                Cool(482, 64, 145, 134, 116, 8.9f, 0.18f, Motion.Flicker),
                Warm(422, 250, 186, 40, 103, 6.6f, 0.65f, Motion.Sweep)) }
        };
    }
}
