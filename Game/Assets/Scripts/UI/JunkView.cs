using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 垃圾場：地上有好幾堆廢棄物，點一堆就挖。一趟能挖的次數有限（金屬探測器可以多挖），
    /// 可能挖到故障品、刮傷品，運氣好挖到近全新的好貨；也可能只有廢鐵，或被野貓抓一下。
    /// 有頭燈的話，看得出哪幾堆裡面有東西。
    /// </summary>
    public class JunkView : MonoBehaviour
    {
        const float ResultY = 236f;

        RectTransform rt;
        GameRoot game;
        System.Action onChanged;
        System.Action onLeave;
        Transform bg, header, piles, result;
        JunkFind last;
        string message;

        Career C { get { return game.Career; } }
        GameState S { get { return game.State; } }

        static readonly string[] MoundIcons = { "junk-electronic", "junk-carton", "junk-mixed" };

        public static JunkView Build(RectTransform parent, GameRoot game, System.Action onChanged)
        {
            RectTransform r = Px.Rect(parent, "Junk", 0, 0, 640, 360);
            JunkView v = r.gameObject.AddComponent<JunkView>();
            v.rt = r;
            v.game = game;
            v.onChanged = onChanged;
            v.bg = Px.Rect(r, "Bg", 0, 0, 640, 360);
            v.header = Px.Rect(r, "Header", 0, 0, 640, 360);
            v.piles = Px.Rect(r, "Piles", 0, 0, 640, 360);
            v.result = Px.Rect(r, "Result", 0, 0, 640, 360);
            r.gameObject.SetActive(false);
            return v;
        }

        public bool Visible { get { return gameObject.activeSelf; } }

        public void Show(System.Action leave)
        {
            onLeave = leave;
            last = null;
            message = "月光下，廢棄物堆得跟小山一樣。挑一堆開始挖吧。";
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            DrawBackground();
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Refresh()
        {
            DrawHeader();
            DrawPiles();
            DrawResult();
        }

        void DrawBackground()
        {
            Px.Clear(bg);
            Image sky = Px.Fill(bg, 0, 19, 640, 341, Pal.Hex("#14121f"));
            sky.raycastTarget = true;
            Px.Fill(bg, 540, 44, 14, 14, Pal.Cream);
            Px.Fill(bg, 545, 44, 9, 9, Pal.Hex("#14121f"));
            // 鐵絲網圍籬
            Px.Fill(bg, 0, 54, 640, 2, Pal.Hex("#5a5868"));
            for (int x = 0; x < 640; x += 6) Px.Fill(bg, x, 56, 1, 16, Pal.Hex("#3a3846"));
            for (int x = 0; x < 640; x += 64) Px.Fill(bg, x, 50, 3, 24, Pal.Hex("#6a6878"));
            // 地面
            Px.Fill(bg, 0, 72, 640, 164, Pal.Hex("#2a241e"));
            for (int i = 0; i < 40; i++) Px.Fill(bg, (i * 53) % 636, 80 + (i * 37) % 150, 3, 2, Pal.Hex("#3a3228"));
            Px.Fill(bg, 0, ResultY - 2, 640, 126, Pal.Hex("#14121f"));
        }

        void DrawHeader()
        {
            Px.Clear(header);
            RectTransform h = Px.Box(header, "Head", 4, 21, 632, 18, Pal.Plum, Pal.PlumLight);
            Px.Label(h, "垃圾場", 6, 2, 60, 14, Pal.Orange);
            string ev = C.EventText("junk");
            Px.Label(h, string.IsNullOrEmpty(ev) ? "點一堆就開始挖" : ev, 64, 2, 250, 14, string.IsNullOrEmpty(ev) ? Pal.Lav : Pal.Yellow);
            Px.Label(h, "還能挖", 320, 2, 44, 14, Pal.Lav);
            int max = C.JunkDigsMax;
            for (int i = 0; i < max; i++) Px.Fill(h, 366 + i * 10, 5, 7, 8, i < S.junkDigs ? Pal.Orange : Pal.PlumLine);
            Px.Label(h, "庫存 " + S.inventory.Count + "/" + C.InventoryCap, 430, 2, 80, 14, Pal.Lav);
            Px.Button(h, "離開", 580, 2, 48, 14, Pal.Red, Pal.Cream, Pal.Ink, () =>
            {
                Hide();
                if (onLeave != null) onLeave();
            });
        }

        void DrawPiles()
        {
            Px.Clear(piles);
            int n = C.JunkCount;
            int perRow = n <= 12 ? 6 : 8;
            float w = perRow == 6 ? 92 : 72;
            float gap = perRow == 6 ? 104 : 78;
            float x0 = Mathf.Floor((640 - (perRow - 1) * gap - w) * 0.5f);
            bool lamp = C.HasTool("headlamp");
            for (int i = 0; i < n; i++)
            {
                float x = x0 + (i % perRow) * gap;
                float y = 78 + (i / perRow) * 76;
                bool dug = S.junkDug.Contains(i);
                int kind = C.JunkKind(i);
                RectTransform pile = Px.Rect(piles, "Pile" + i, x, y, w, 72);
                Image hit = pile.gameObject.AddComponent<Image>();
                hit.color = Pal.Clear;
                hit.raycastTarget = true;
                IconArt.Draw(pile, dug ? "junk-dug" : MoundIcons[kind], 1f, Mathf.Floor((w - 48) * 0.5f), 8, 48);
                if (lamp && !dug && C.JunkHasItem(i))
                {
                    // 頭燈照到的反光
                    Px.Fill(pile, w * 0.5f - 6, 20, 2, 2, Pal.Yellow);
                    Px.Fill(pile, w * 0.5f + 5, 28, 2, 2, Pal.Yellow);
                    Px.Fill(pile, w * 0.5f - 1, 14, 2, 2, Pal.Cream);
                }
                Px.Label(pile, dug ? "挖過了" : Career.JunkKindName[kind], 0, 58, w, 14, dug ? Pal.PlumLight : Pal.Lav, TextAnchor.UpperCenter, false);
                int idx = i;
                Px.MakeClickable(pile, () => Dig(idx));
            }
        }

        void Dig(int i)
        {
            if (S.junkDug.Contains(i))
            {
                message = "這堆已經挖過了。";
                last = null;
                Refresh();
                return;
            }
            if (S.junkDigs <= 0)
            {
                message = "手痠得抬不起來了。今晚就挖到這裡吧。";
                last = null;
                Refresh();
                return;
            }
            last = C.Dig(i);
            message = last != null ? last.text : "挖不動。";
            game.SaveNow();
            if (onChanged != null) onChanged();
            Refresh();
        }

        void DrawResult()
        {
            Px.Clear(result);
            RectTransform p = Px.Box(result, "Result", 4, ResultY, 632, 120, Pal.Plum, Pal.PlumLight);
            if (last != null && last.kind == "item" && last.item != null)
            {
                RectTransform icon = Px.Box(p, "Icon", 8, 8, 48, 48, Pal.Night, last.lost ? Pal.PlumLine : Pal.Orange);
                IconArt.Draw(icon, C.ItemIcon(last.item), last.lost ? 0.35f : 1f, 8, 8);
                Px.Label(p, last.item.name + "・" + last.item.condition, 64, 8, 300, 14, Pal.Orange);
                Px.Label(p, message, 64, 26, 560, 42, Pal.Yellow, TextAnchor.UpperLeft, true);
            }
            else
            {
                Px.Label(p, message, 8, 8, 616, 56, Pal.Yellow, TextAnchor.UpperLeft, true);
            }
            string tip;
            if (S.junkDigs <= 0) tip = "今晚挖完了，按右上角「離開」回到街上。";
            else if (!C.HasTool("detector")) tip = "電子街賣的金屬探測器，可以讓每次多挖 2 堆。";
            else if (!C.HasTool("headlamp")) tip = "電子街賣的頭燈，看得出哪幾堆裡面有東西。";
            else tip = "會發光的那幾堆，裡面有東西。";
            Px.Label(p, "故障品和刮傷品可以在晚上回辦公室翻新，再拿去賣。", 8, 78, 616, 14, Pal.Lav);
            Px.Label(p, tip, 8, 96, 616, 14, Pal.PlumLight);
        }
    }
}
