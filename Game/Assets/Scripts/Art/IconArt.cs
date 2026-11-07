using System.Collections.Generic;
using UnityEngine;

namespace SSD
{
    /// <summary>遊戲道具和設備的 16×16 像素圖稿。</summary>
    public static class IconArt
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static readonly Color32 Highlight = Pal.Hex("#cbbce8");
        static readonly Color32 Casing = Pal.Hex("#574968");
        static readonly Color32 Glass = Pal.Hex("#252d47");

        public static void Draw(Transform parent, string id, float alpha, float x = 8, float y = 1, int size = 32)
        {
            if (!TryDraw(parent, id, alpha, x, y, size))
                TryDraw(parent, "box", alpha, x, y, size);
        }

        public static bool TryDraw(Transform parent, string id, float alpha, float x, float y, int size)
        {
            if (string.IsNullOrEmpty(id)) return false;
            // 讀取使用早期分類圖示的案件資料。
            switch (id)
            {
                case "pcb": id = "capcard"; break;
                case "lamp": id = "light"; break;
                case "camera": id = "webcam"; break;
            }

            if (!Sprites.TryGetValue(id, out Sprite sprite))
            {
                if (!Rows.TryGetValue(id, out string[] rows)) return false;
                sprite = PixelSprites.FromRows(rows, PixelColor);
                Sprites.Add(id, sprite);
            }

            var image = Px.Fill(parent, x, y, size, size, Pal.Alpha(Pal.White, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f)));
            image.name = "ItemIcon_" + id;
            image.sprite = sprite;
            return true;
        }

        static Color32 PixelColor(char key)
        {
            switch (key)
            {
                case 'k': return Pal.Ink;
                case 'p': return Pal.PlumLine;
                case 'l': return Pal.Lav;
                case 'h': return Highlight;
                case 'c': return Pal.Cream;
                case 'b': return Casing;
                case 'd': return Glass;
                case 't': return Pal.Teal;
                case 'T': return Pal.TealLight;
                case 'a': return Pal.Orange;
                case 'o': return Pal.Yellow;
                case 'r': return Pal.Pink;
                case 'm': return Pal.Red;
                default: return Pal.Clear;
            }
        }

        static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
        {
            // 商品目錄，各品項沿用 world.json 的代號。
            { "capcard", new[] {
                "................", "................", "................", ".......kkk......",
                "...kkkklllkk....", ".kkkllllhhlllk..", ".kkllhhhlllllkk.", ".kphhlllllkkkpk.",
                ".kppkllkkkppbbk.", ".kpkkkkppbbbTpk.", ".kpkllkbdddppkk.", "..kkkkkpppkkk...",
                "...kkppkkk......", ".....kk.........", "................", "................"
            } },
            { "mic", new[] {
                "................", ".....kkkkkk.....", ".....kllllk.....", ".....kbbbbk.....",
                ".....kllllk.....", "...l.kppppk.p...", "...l.kpTppk.p...", "...l.kppppk.p...",
                "...l.kkkkkk.p...", "...l........p...", "....l......p....", ".....ppllpp.....",
                ".......ll.......", "....kkkkkkkk....", "....kllllllk....", ".......klk......"
            } },
            { "light", new[] {
                "................", ".......k........", "....kccccck...c.", "...kkoooookk....",
                "...koookoook.a..", "...cookpkook....", "..kookpppkook...", "...kookpkook....",
                "...koookoook....", "...kkoooooak....", "....kkkokkk.....", ".......ll.......",
                ".......pp.......", ".....ppllpp.....", "..klk..ll..klk..", "................"
            } },
            { "webcam", new[] {
                "................", "................", "................", "................",
                ".kkkkkkkkkkkkkk.", ".kllllkklkklllk.", ".kppppkTtlkppTk.", ".kpppkldddlkppk.",
                ".kppppkldlkpppk.", ".kbbbpkklkkbbbk.", ".kkkkkppppkkkkk.", "......pppp......",
                "....kkkkkkkk....", "....kllllllk....", "....kkkkkkkk....", ".......klk......"
            } },
            { "chair", new[] {
                "................", "....kkkkkkk.....", "....krrrrrkk....", "....kormmmrkk...",
                "...kkormmmrkk...", "...kkormmmrkk...", "...kkormmmkk....", "...kkormmmkk....",
                "...kkrrmrrkk.l..", "..kkkkkkkkkkkk..", "..kkrrrrrrrrrkk.", "...kkkkkkkkkkk..",
                ".....kkllkkkk...", ".....kkll.......", ".....kpppp......", "...klk....klk..."
            } },
            { "mixer", new[] {
                "................", "................", "................", ".kkkkkkkkkkkkkk.",
                ".kllllllllllllk.", ".kpdTTTdppaprpk.", ".kpdddddppppppk.", ".kppppppppppppk.",
                ".kppkpppkpppkpk.", ".kplllppkpppkpk.", ".kppkpplllppkpk.", ".kppkpppkppaaak.",
                ".kppkpppkpppkpk.", ".kkkkkkkkkkkkkk.", "................", "................"
            } },
            { "keyboard", new[] {
                "................", "................", "................", "................",
                "................", "kkkkkkkkkkkkkkkk", "kllllllllllllllk", "kbhbbhbbhbbhbbhk",
                "khbbhbbhbbhbbhbk", "kbbhbbhbbhbbhbbk", "kpplllllllllpTpk", "kpddddddddddTrpk",
                "kkkkkkkkkkkkkkkk", "................", "................", "................"
            } },
            { "headset", new[] {
                ".......k........", "....kkklkkk.....", "...klhlllllk....", "..kllll.llllk...",
                ".kll.......llk..", ".kl.........lk..", ".kp.........pk..", "klp.........plk.",
                ".kkkk......kkkk.", ".koob......book.", ".kaab......baak.", ".kaab......baak.",
                ".kaab......baak.", ".kkkk......klkk.", ".......TTTll....", ".......TTT......"
            } },
            { "cam4k", new[] {
                "................", "................", "....kkkkkk......", "....kllTlk......",
                "....kppppk......", ".kkkkkkkkkkk..k.", ".klllllllllkkkkk", ".kbbbbbbbbkklkkk",
                ".kdddddbbbkTtlkk", ".kdrdddbbkldddlk", ".kdddddbbbkldlkk", ".kbbbbbbbbkklkkk",
                ".kppppppkkkkk...", "................", "................", "................"
            } },
            { "pc", new[] {
                "................", "...kkkkkkkkkk...", "...kllllllllk...", "...kpbbbbblpk...",
                "...kpbbdbblak...", "...kpbdddblpk...", "...kpddTddlpk...", "...kpbdddblpk...",
                "...kpbbdbblpk...", "...kpbdddblpk...", "...kpddTddlpk...", "...kpbdddblpk...",
                "...kpbbdbblpk...", "...kbbbbbbbpk...", "...kklkkkkklk...", "................"
            } },
            { "gpu", new[] {
                "................", "................", "................", "................",
                ".kkkkkkkkkkkkkk.", ".klllllllllllrk.", ".kcbbbdbbbbdbbk.", ".kcbbdtdbbdtdbk.",
                ".klbdTdtddTdtdk.", ".kcbbdtdbbdtdbk.", ".kcbbbdbbbbdbbk.", ".kkkkkkkkkkkkkk.",
                ".....oooooooo...", "................", "................", "................"
            } },
            { "wmic", new[] {
                "................", "......b.........", ".....hbb........", "....bbbbb.......",
                ".....bbb........", ".....lb......l..", ".....lp......l..", "....lpa......l..",
                "....lp.kkkkkklk.", "...lp..kllllllk.", "...lp..kpdTddpk.", "..lp...kpddddpk.",
                "..lp...kppppppk.", ".......kkkkkkkk.", "................", "................"
            } },
            { "switcher", new[] {
                "................", "................", "................", "................",
                ".kkkkkkkkkkkkkk.", ".kllllllllllllk.", ".kpTTTTdddddapk.", ".kpddddddddddpk.",
                ".kppppppppppppk.", ".kprrpttpttptlh.", ".kppppppppppplk.", ".kpoopooprrpolk.",
                ".kppppppppppplk.", ".kkkkkkkkkkkkkk.", "................", "................"
            } },
            { "greenscreen", new[] {
                "................", "................", ".kkkkkkkkkkkkkk.", ".kbbbbbbbbbbbak.",
                ".kdtttttTTtttdk.", ".kdttttTTttttdk.", ".kdtttTTtttttdk.", ".kdttTTttttttdk.",
                ".kdtTTtttttttdk.", ".kdTTttttttttdk.", ".kddddddddddddk.", ".kklkkkkkkkklkk.",
                "...l........l...", "...l........l...", "..klk......klk..", "................"
            } },
            { "lens", new[] {
                "................", "................", "................", "....kkkkkkkk....",
                "...kkkkkkkkkk...", "..kkllallkklkk..", "..khbbabklldllk.", "..kkbbabklTtdlk.",
                "..kkbbakldddddlk", "..kkbbabkldddlk.", "..kkbbabklldllk.", "..kbbbabppplkk..",
                "...kkkkkkkkkk...", "................", "................", "................"
            } },
            { "director", new[] {
                "................", "..kkkkkkkkkkkk..", "..kllllllllllk..", "..kbTddddddtbk..",
                "..kbdddddttdbk..", "..kbdddttdddbk..", "..kbdttdddddbk..", "..kbtdddddddbk..",
                "..kkkkkkkkkkkk..", "..kkppppppppkk..", "..kppppppppppk..", "..kppppppppppk..",
                ".kphphphphphppk.", ".kktktktkrkrkk..", "..kllllllllkka..", "................"
            } },
            { "camkit", new[] {
                "................", "................", ".....llll.......", "....kkkkkkkkkk..",
                "....klllkklkkk..", "....kbbbkTtlkk..", "....lbbkldddlk..", "...lkbbbkldlkk..",
                "..p.kbbbkklkkk..", "..p.kkkkkkkkkk..", ".kpkkkkkkkkkkkk.", ".kplllllllllllk.",
                ".kpbobbbbbbbopk.", ".kpbbbbbbbbbbpk.", ".kkkkkkkkkkkkkk.", "................"
            } },
            { "ledwall", new[] {
                "................", "................", ".kkkkkkkkkkkkkk.", ".klllllllllllak.",
                ".kTTdTlldllldrk.", ".kTdTlldllldrrk.", ".kdTlldllldrrrk.", ".kTlldllldrrrdk.",
                ".klldllldrrrdrk.", ".kldllldrrrdrrk.", ".kdllldrrrdrrrk.", ".kppppppppppppk.",
                ".kkkkkkkkkkkkkk.", "...klk....klk...", "................", "................"
            } },
            { "mocap", new[] {
                "................", ".......l........", "......lll.......", "...a.llTll......",
                "......lll...r...", ".......d........", "....lpdTdpl.....", "...l...d...lld..",
                "..dl...l....dTd.", ".dTd...d.....d..", "..d...dTd.......", "......ldl.......",
                ".....l...l......", "....dl...ld.....", "...dTd...dTd....", "....d.....d....."
            } },
            { "obvan", new[] {
                "................", ".....h..........", ".....lh.........", "......llh.......",
                ".......llh......", "......l.........", "kkkkkkkkkkk.....", "khhhhhhhhhkkk...",
                "kpppppppppkppk..", "kpttttttttkpTTk.", "kpttttttttkpTTpk", "rrppkpppppkpkppo",
                "kkkkkkkkkkkkkkkk", "..kklkk...kklkk.", "...kkk.....kkk..", "....k.......k..."
            } },
            { "studiokit", new[] {
                "................", "........kk......", ".....kkkokk.....", "..kkkooccokk....",
                ".kkooccoook.....", ".koccooookk.....", "..oooooookk.....", "..kooooook......",
                "..koookkkkkkkkkk", "...kkk.l..kllllk", ".......l..kbaabk", ".......l..kbaabk",
                ".......p..kbbbTk", "......plp.kkkkkk", "....pp.l.p......", "...p......p....."
            } },
            { "virtual", new[] {
                "................", "................", "................", ".kkkkkkkkkkkkkk.",
                ".kallllllllllak.", ".kbTdddtddddtbk.", ".kbdddltldttdbk.", ".kbddldttllddbk.",
                ".kbdTdttdddTdbk.", ".kbdtTdtddTddbk.", ".kbtddTtTTdddbk.", ".kabbbbtbbbbbak.",
                ".kkkkkkkkkkkkkk.", ".......ll.......", ".....bbbbbb.....", "................"
            } },
            { "stage", new[] {
                "................", "................", "kkkkkkkkkkkkkkkk", "kllhllllhllllhlk",
                "kppppppppppppppk", "kkbbbkkbbbkkbbbk", "..bcb..bcb..bcb.", "..aa...tt...rr..",
                ".aaa...tt..rrr..", ".aaaa.tttt.rrrr.", ".aaaa.tttt.rrrr.", "aaaaa.ttttrrrrr.",
                "aaaaatttTtrrrrrr", ".....tttttt.....", "pppppppppppppppp", "................"
            } },

            // 電子街道具和未分類包裹。
            { "drink", new[] {
                "................", "....kkkkkkkk....", "....kcccccck....", "....kddddddk....",
                "....ktTtttdk....", "....kttottdk....", "....kttottdk....", "....ktoooodk....",
                "....ktttotdk....", "....ktttttdk....", "....kttottdk....", "....ktttttdk....",
                "....kddddddk....", "....kllllllk....", "....kkkkkkkk....", "................"
            } },
            { "gift", new[] {
                ".....a....a.....", "....aaa..aaa....", "...aakaaaakaa...", "....aao..ooa....",
                ".kkkkakookakkkk.", ".krrrrroarrrrrk.", ".kmmmmmoammmmmk.", ".kkkkkkoakkkkkk.",
                "..kmmmmoammmmk..", "..kmmmmoammmmk..", "..kmrmmoammmmk..", "..kmmmmoammmmk..",
                "..kmmmmoammmrk..", "..kmmmmaammmmk..", "..kkkkkkkkkkkk..", "................"
            } },
            { "detector", new[] {
                "................", ".............pa.", "..........kkkkk.", "..........klllk.",
                "..........kbTbk.", "..........kkkkk.", ".........kp.....", ".........pl.....",
                "........pl......", "....k..pl.......", "..ckokkp........", "..koookl........",
                ".kookook........", "..koook.........", "..kkokk.........", "....k..........."
            } },
            { "headlamp", new[] {
                "................", "................", "................", "..............a.",
                "...kkkkkkkk.aaa.", "...kllllllkaaoo.", ".ppkboooobaooaa.", ".llkboccoboaaaa.",
                ".kkkboccobcccccc", "...kbooooboaaaa.", "...kTbbbbbkooaa.", "...kkkkkkkk.aoo.",
                "..............a.", "................", "................", "................"
            } },
            { "box", new[] {
                "................", "................", ".......kk.......", ".....kkookk.....",
                "....kcoooook....", "..koooccoooook..", ".kkoooooccoookk.", ".kakkooooocckbk.",
                ".kaakkkookklbbk.", ".kaarrkkkbblbbk.", ".kkaaaakbbblbkk.", "..kkaaakbbbkkk..",
                "....kkakbkkk....", ".....kkkkkk.....", ".......kk.......", "................"
            } },

            // 垃圾場可互動的廢棄物堆。
            { "junk-electronic", new[] {
                "....l....l......", "....l...l.......", ".kkkklklkkkk....", ".klllllllllk....",
                ".kbTdddddtbk....", ".kbddddttdbk....", ".kbdddtdddbk....", ".kbdttddddbk....",
                ".kbtddddddak....", ".kbbbbbbkkkkkkk.", ".kkkkkkkklllllk.", "..ppppppkbdddbk.",
                ".plpptppkkkkkkk.", "ppppppttppppppp.", "ppppppppttttttpp", "................"
            } },
            { "junk-carton", new[] {
                "................", "................", "................", "..kkkkkkkk......",
                "..kooccook......", "..kaaccaak......", "..kaaccaak......", "..kaaccakkkkkkk.",
                "..kbbbaakaaoaak.", "..kbbbaakmmommk.", "..kaaaaakmmommk.", "..kkkkkkkmmmmmk.",
                ".ppppllpkmmmmmk.", "pppllpppkkkkkkk.", "pppppppppprppppp", "................"
            } },
            { "junk-mixed", new[] {
                "................", "................", ".............o..", "............o.ab",
                ".....k.....o.ab.", "..kklbkkk..oab..", ".kklbbbbkko.ab..", ".kbbbkbbbo.ab...",
                ".kbbkkkbokab....", "kbbkkkkkoabpp...", ".kbbkkkobabppp..", ".kbbbkobabppppp.",
                "pkkbbbbabkbbTbp.", "ppkrkbkkkpbbbbpp", "pppppkpppppppppp", "................"
            } },
            { "junk-dug", new[] {
                "................", "................", "................", "................",
                "................", "................", "................", "................",
                "................", "................", "...ppkkkpppp....", "..pkbkkkkkbkp...",
                ".pbbkkkkkkkbbpp.", "pbpkkbkkkkpkpbb.", ".bbppppppppppp..", "................"
            } },
        };
    }
}
