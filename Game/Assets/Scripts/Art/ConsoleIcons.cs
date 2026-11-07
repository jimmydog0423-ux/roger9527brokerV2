using System.Collections.Generic;
using UnityEngine;

namespace SSD
{
    /// <summary>水蛇洞桌面的逐像素圖稿。大圖示是 12×12，小圖示是 7×7。</summary>
    static class ConsoleIcons
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite Get(string id)
        {
            if (!Sprites.TryGetValue(id, out Sprite sprite))
            {
                sprite = PixelSprites.FromRows(Rows.TryGetValue(id, out string[] art) ? art : Rows["report"], PixelColor);
                Sprites.Add(id, sprite);
            }
            return sprite;
        }

        static Color32 PixelColor(char key)
        {
            switch (key)
            {
                case 'k': return Pal.Ink;
                case 'p': return Pal.PlumLine;
                case 'l': return Pal.Lav;
                case 'h': return Pal.Hex("#cbbce8");
                case 'H': return Pal.PlumLight;
                case 'c': return Pal.Cream;
                case 's': return Pal.Hex("#b8a3bb");
                case 'S': return Pal.PlumLight;
                case 't': return Pal.Teal;
                case 'T': return Pal.TealLight;
                case 'a': return Pal.Orange;
                case 'o': return Pal.Yellow;
                case 'O': return Pal.Cream;
                case 'r': return Pal.Pink;
                default: return Pal.Clear;
            }
        }

        static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
        {
            // 文件與案件紀錄。
            { "contract", new[] {
                "..kkkkkkk...", ".kcccccckk..", ".kcoocccck..", ".kcppppcck..",
                ".kccccccck..", ".kcppppcck..", ".kcccccrrk..", ".kcpppcrak..",
                ".kcccccrrk..", ".kscccccck..", "..kkkkkkk...", "............"
            } },
            { "quote", new[] {
                "..kkkkkkkk..", ".kttttttttk.", ".ktkkkkkttk.", ".ktkcccktkk.",
                ".ktkcppktkk.", ".ktkcccktkk.", ".ktkcppktkk.", ".ktkcacktkk.",
                ".ktkcccktkk.", ".ktkkkkkttk.", "..kkkkkkkk..", "............"
            } },
            { "request", new[] {
                "..kkkkk.....", ".klllllk....", ".kllllllkk..", ".kltlllhhhk.",
                ".klllllllhk.", ".kltlllhlhk.", ".klllllllhk.", ".klolllhlhk.",
                ".klllllllhk.", ".kppppppppk.", "..kkkkkkkk..", "............"
            } },
            { "folder", new[] {
                "............", "..kkkkk.....", ".klpppkkkkk.", ".kllllllllk.",
                ".kppppppppk.", ".kptttkccpk.", ".kptTtkccpk.", ".kptttkcakk.",
                ".kppppppppk.", ".kppppppppk.", "..kkkkkkkk..", "............"
            } },
            { "book", new[] {
                "...kkkkkkk..", "..klppppppk.", ".ktlppppppk.", ".ktlpaaappk.",
                ".kTlpacappk.", ".ktlpaaappk.", ".ktlppppppk.", ".kTlppppppk.",
                ".ktlppppppk.", ".kccccccck..", "..kkkkokkk..", "......ok...."
            } },
            { "card", new[] {
                "............", ".kkkkkkkkkk.", "kcccccccccck", "kctttccppcck",
                "kctTtcccccck", "kctttcppccck", "kcccccccaack", "kcscccccockk",
                ".kkkkkkkkkk.", "............", "............", "............"
            } },
            { "receipt", new[] {
                "..kkkkkkkk..", "..kaaaaaak..", "..kcccccck..", "..kcpppcck..",
                "..kcccccck..", "..kcpppcck..", "..kcccccck..", "..kcooccsk..",
                "..kcccccck..", "..kcpcpcck..", "...k.kk.k...", "............"
            } },
            { "report", new[] {
                "..kkkkkk....", ".kcccccckk..", ".kccccccclk.", ".kcppppccck.",
                ".kcccccccsk.", ".kccctccosk.", ".kccttcoosk.", ".kccttoooSk.",
                ".kcccccccsk.", ".ksccccccsk.", "..kkkkkkkk..", "............"
            } },
            { "chart", new[] {
                "............", ".kkkkkkkkkk.", "kllllppppppk", "kppppppppppk",
                "kphhhhhhhhpk", "kphccctcchpk", "kphcactochpk", "kphcactochpk",
                "kphhhhhhhhpk", "kpppttpppppk", ".kkkkkkkkkk.", "............"
            } },
            { "plan", new[] {
                "...kkkkkk...", "..kppplppk..", ".klpaaapppk.", ".kppppppppk.",
                ".kphThhhppk.", ".kphhhhhppk.", ".kphThhhppk.", ".kphhhhhppk.",
                ".kphoHhhppk.", ".kppppppppk.", "..kkkkkkkk..", "............"
            } },
            { "claim", new[] {
                "...kkkkkk...", "..krrrrrrk..", "..krooorok..", "..kcccccck..",
                "..kcokkcck..", "..kcokkcck..", "..kcckkcck..", "..kcooccck..",
                "..kcccccck..", "..kcppppck..", "..kkkkkkkk..", "............"
            } },
            { "record", new[] {
                "............", ".kkkkkkkkkk.", "kppppppppppk", "kpttttttttpk",
                "kptTtTtTttpk", "kptTtTtTttpk", "kpttttttttpk", "kppppppppppk",
                "kprpoooooopk", ".kkkkkkkkkk.", "............", "............"
            } },
            { "recorder", new[] {
                "....kkkk....", "...kllllk...", "...klpplk...", "...klpplk...",
                "..kkppppkk..", "..kptTTtpk..", "..kpttttpk..", "..kppppppk..",
                "..kprroppk..", "..kpllllpk..", "...kkkkkk...", "............"
            } },
            { "proposal", new[] {
                "............", "..kkkkkkkk..", ".kttttttttk.", ".ktltttltkk.",
                ".kttttttttk.", ".kttccccttk.", ".kttcTacttk.", ".kttccccttk.",
                ".kttttttttk.", ".kpttoottpk.", "..kkkkkkkk..", "............"
            } },

            // 終端機控制鍵、狀態和桌面捷徑。
            { "live", new[] { "..ttt..", ".tTTTt.", "tTTTTTt", "tTTcTTt", "tTTTTTt", ".tTTTt.", "..ttt.." } },
            { "idle", new[] { "..lll..", ".lpppl.", "lpppppl", "lpphppl", "lpppppl", ".lpppl.", "..lll.." } },
            { "signal", new[] { "......l", "....l.l", "..l.l.l", "l.l.l.l", "l.l.l.l", "l.l.l.l", "l.l.l.l" } },
            { "window", new[] { "lllllll", "lhhhhhl", "lpppppl", "lpppppl", "lpppppl", "lpppppl", "lllllll" } },
            { "close", new[] { ".......", ".r...r.", "..r.r..", "...r...", "..r.r..", ".r...r.", "......." } },
            { "target", new[] { "...a...", "..aaa..", ".aaoaa.", "aaocOaa", ".aaoaa.", "..aaa..", "...a..." } },
            { "pending", new[] { "...l...", "..lpl..", ".lpppl.", "lpphppl", ".lpppl.", "..lpl..", "...l..." } },
            { "check", new[] { ".....t.", ".....tT", "....tT.", "t..tT..", "tTtT...", ".tT....", "......." } },
            { "add", new[] { "..lll..", "..lhl..", "lllhlll", "lhhchhl", "lllhlll", "..lhl..", "..lll.." } },
            { "lock", new[] { "..lll..", ".lhphl.", ".lhphl.", "lllllll", "lppcppl", "lppcppl", "lllllll" } },
            { "micOn", new[] { "..lll..", "..lhl..", "..lhl..", "l.lhl.l", "l.lllll", ".lllll.", "..lll.." } },
            { "micOff", new[] { "r.lll..", ".rlhl..", "..rhl..", "l.lrl.l", "l.llrll", ".llllr.", "..lll.r" } },
            { "cameraOn", new[] { ".......", ".llll..", "lhthl.l", "lTtTlhl", "lhtTlhl", "lllll.l", "......." } },
            { "cameraOff", new[] { "r......", ".rlll..", "lhrhl.l", "lllrlhl", "lhhhrhl", "lllllrl", "......r" } },
            { "hang", new[] { ".......", "..ccc..", ".crrrc.", "crrrrcc", "crcccrc", "cc...cc", "......." } },
            { "answer", new[] { "l......", "ll.....", "Tlt....", ".tTt...", "..tTt..", "...tTll", "....lll" } },
            { "decline", new[] { ".......", "..rrr..", ".rcccr.", "rccrccr", "rcrrrcr", "rr...rr", "......." } },
            { "inbox", new[] { ".......", "lllllll", "lhpplhl", "lplllpl", "lpppppl", "lllllll", "......." } },
            { "ledger", new[] { ".lllll.", "lhhppll", "lpphppl", "lhhppll", "lpphppl", "lhhppll", ".lllll." } },
            { "workbook", new[] { ".lllll.", "lTppppl", "ltpccpl", "lTppppl", "ltpccpl", "lTppppl", ".lllll." } },
            { "day", new[] { ".t...t.", "lllllll", "laaaaal", "lllllll", "lpchppl", "lpphcpl", "lllllll" } },
            { "talent", new[] { "..lll..", ".lcccl.", ".lcccl.", "..lll..", ".ttttt.", "ttTTTtt", "lllllll" } },
            { "inventory", new[] { "...a...", "..aoa..", ".aocaa.", "aaoaaaa", "ahoohaa", "ahhohaa", ".aaaaa." } },
            { "invest", new[] { "......T", ".....Tt", "....Tt.", ".T.Tt..", "TtTt...", "tTt....", "ttttttt" } },
            { "company", new[] { "..lll..", "..lhl..", ".llhll.", ".lhphl.", "llhphll", "lhppphl", "lllllll" } },
            { "delegate", new[] { "..ll...", ".lhhl..", "..ll.T.", "....TTT", ".lll.T.", "lpppl..", "lllll.." } },
            { "money", new[] { "..aaa..", ".aocoa.", "aococoa", "acococa", "aococoa", ".aocoa.", "..aaa.." } },
            { "store", new[] { ".lllll.", "ltttttl", "laaaaal", ".lpppl.", ".lptpl.", ".lpppl.", "lllllll" } },
            { "sync", new[] { "..ttt..", ".t...t.", "t..T.tt", "t.TTT.t", "tt.T..t", ".t...t.", "..ttt.." } },
        };
    }
}
