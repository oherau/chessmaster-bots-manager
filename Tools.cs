using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessmasterBotsManager;
using CMPersonalityManager;

namespace CMPersonalityManager
{
    
    public class Tools
    {
        
        public enum PlayerColor
        {
            White,
            Black
        }

        public static AnalysisDiff DiffStats(AnalysisReport single, AnalysisReport average)
        {
            return new AnalysisDiff()
            {
                NbGames = single.NbGames,
                KingMovesPct = single.KingMovesPct - average.KingMovesPct,
                QueenMovesPct = single.QueenMovesPct - average.QueenMovesPct,
                RookMovesPct = single.RookMovesPct - average.RookMovesPct,
                BishopMovesPct = single.BishopMovesPct - average.BishopMovesPct,
                KnightMovesPct = single.KnightMovesPct - average.KnightMovesPct,
                PawnMovesPct = single.PawnMovesPct - average.PawnMovesPct,
                DrawsPct = single.DrawsPct - average.PawnMovesPct,
                CastleTimePct = single.CastleTimePct - average.CastleTimePct,
                CenterScorePct = single.CenterScorePct - average.CenterScorePct,
                MaxElo = single.MaxElo,
                Agressiveness = single.Agressiveness - average.Agressiveness,
                InCheckByOpponentPct = single.InCheckByOpponentPct - average.InCheckByOpponentPct,
                InCheckOpponentPct = single.InCheckOpponentPct - average.InCheckOpponentPct,
            };
        }

        internal static byte GetWeightFromPct(float realweight)
        {
            // 0=0%  1=25%  2=50%  3=100%
            if (realweight > 50)        return 3;  // 100
            else if (realweight > 25)   return 2;  // 50
            else if (realweight > 12)   return 1;  // 25
            return 0;                              // 0
        }

        internal static string FindPath(string rootpath, string filter)
        {
            string[] files = Directory.GetFiles(rootpath, filter, SearchOption.AllDirectories);

            var ponderpaths = new Dictionary<string, int>();
            foreach (var p in files)
            {
                var rp = Path.GetDirectoryName(p);
                if (ponderpaths.ContainsKey(rp))
                {
                    ponderpaths[rp]++;
                }
                else
                {
                    ponderpaths[rp] = 1;
                }
            }
            ponderpaths = ponderpaths.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            if (ponderpaths.Count > 0) return ponderpaths.Keys.ToArray()[0];
            return rootpath;
        }

        internal static void AutomaticBackup(string personalities_path)
        {
            var files = Directory.GetFiles(personalities_path, "*.cmp", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                BackupFile(file);
            }
        }

        public static void BackupFile(string file)
        {
            if (File.Exists(file))
            {
                var dir = Path.GetDirectoryName(file);
                var targetfile = Path.Combine(dir, Path.GetFileNameWithoutExtension(file)) + "_bkp";
                if (!File.Exists(targetfile))
                {
                    File.Copy(file, targetfile);
                }
            }
        }

        public static List<Pgn> ParsePgn(string fileName)
        {
            List<Pgn> database = new List<Pgn>();


            using (StreamReader reader = new StreamReader(fileName))
            {
                StringBuilder buffer = new StringBuilder();
                bool readingContent = false;
                string line;
                Pgn pgn;
                while ((line = reader.ReadLine()) != null)
                {
                    if (readingContent && line.StartsWith("["))
                    {
                        readingContent = false;
                        pgn = new Pgn();
                        if (pgn.Parse(Tools.RemoveComments(buffer.ToString())))
                        {
                            database.Add(pgn);
                        }else
                        {
                            var a = 1;
                        }
                        buffer.Clear();
                    }
                    else if (line.StartsWith("1."))
                    {
                        readingContent = true;
                    }
                    buffer.AppendLine(line);
                }
                pgn = new Pgn();
                if(pgn.Parse(Tools.RemoveComments(buffer.ToString()))) database.Add(pgn);
                buffer.Clear();
            }

            return database;
        }

        private static string RemoveComments(string s)
        {
            var start = s.IndexOf('{');
            if (start == -1) return s;

            var sb = new StringBuilder();
            var comm = 0;
            foreach(var c in s)
            {
                if(c=='{')
                { comm++; }
                else if (c == '}')
                { comm--; }
                else if(comm == 0)
                {
                    sb.Append(c);
                }
            }
 
            return sb.ToString();
        }

        public static AnalysisReport Mean(Dictionary<string, AnalysisReport> database)
        {
            var res = new AnalysisReport();
            foreach (var item in database)
            {
                res += item.Value;
            }
            return res;
        }

        public static Dictionary<string, AnalysisReport> Analyze(List<Pgn> pgnlist, ProgressBar pb, TextBox sst)
        {
            var report = new Dictionary<string, AnalysisReport>();
            pb.Minimum = 0;
            pb.Maximum = pgnlist.Count();
            pb.Value = 0;
            foreach (var pgn in pgnlist)
            {
                var wp = pgn.Info["White"];
                var bp = pgn.Info["Black"];

                if (report.ContainsKey(wp)) report[wp] += Analyse(pgn, PlayerColor.White);
                else report[wp] = Analyse(pgn, PlayerColor.White);

                if (report.ContainsKey(bp)) report[bp] += Analyse(pgn, PlayerColor.Black);
                else report[bp] = Analyse(pgn, PlayerColor.Black);
                pb.Value++;
            }

            sst.Text = pgnlist.Count() + " games analyzed.";
            return report;
        }

        public static AnalysisReport Analyse(Pgn pgn, PlayerColor color)
        {
            if (pgn.Info["Black"].Contains("Dickenson, Neil F"))
            {
                var a = 1;
            }
            var result = new AnalysisReport();
            if (pgn.Info.ContainsKey("ECO"))
            {
                result.Openings[pgn.Info["ECO"]] = 1;
            }
            result.NbGames = 1;

            if (pgn.Info.ContainsKey(color == PlayerColor.White ? "WhiteElo":"BlackElo"))
            {
                var elostr = "";
                elostr = (color == PlayerColor.White) ? pgn.Info["WhiteElo"] : pgn.Info["BlackElo"];
                int elo = 0;
                if (int.TryParse(elostr, out elo))
                {
                    result.MaxElo = elo;
                }
            }

            result.NbInCheckByOpponent = (color == PlayerColor.White) ? pgn.NbWhiteInCheck : pgn.NbBlackInCheck;
            result.NbInCheckOpponent = (color == PlayerColor.Black) ? pgn.NbWhiteInCheck : pgn.NbBlackInCheck;

            PlayerColor currColor = PlayerColor.White;
            var plyCount = 0;
            bool castle = false;
            var chessboard = new Chessboard();
            foreach (var move in pgn.Moves)
            {
                // square A1=0 => H8=3f
                var startsq = move & 0x3f;
                var endsq = move >> 8;

                // Make stats only for the selected color
                if (currColor == color)
                {
                    plyCount++;
                    //result.TotalMoves++;

                    bool takes = false;
                    bool prom = false;
                    bool checks = false;

                    if (chessboard.IsCastle(move))
                    {
                        result.Castles += 1;
                        result.CastleTime += plyCount;
                        result.KingMoves++;
                        result.RookMoves++;
                        castle = true;
                    }
                    else
                    {
                        Piece movedp = chessboard.GetMovedPiece(move);
                        if (chessboard.IsCapture(move)) takes = true;
                        if (chessboard.IsPromotion(move)) prom = true;

                        switch (movedp)
                        {
                            case Piece.WK:
                            case Piece.BK:
                                result.KingMoves++;
                                if (takes) result.KingTakes++;
                                break;
                            case Piece.WQ:
                            case Piece.BQ:
                                result.QueenMoves++;
                                if (takes) result.QueenTakes++;
                                break;
                            case Piece.WR:
                            case Piece.BR:
                                result.RookMoves++;
                                if (takes) result.RookTakes++;
                                break;
                            case Piece.WB:
                            case Piece.BB:
                                result.BishopMoves++;
                                if (takes) result.BishopTakes++;
                                break;
                            case Piece.WN:
                            case Piece.BN:
                                result.KnightMoves++;
                                if (takes) result.KnightTakes++;
                                break;
                            case Piece.WP:
                            case Piece.BP:
                                result.PawnMoves++;
                                if (takes) result.PawnTakes++;
                                if (prom)
                                {
                                    result.Promotions++;
                                    result.PromotionTime += plyCount;
                                }
                                break;
                        }
                        // positional analysis (VERY basic)
                        int tc = move;
                        int tr = move;
                        if (tc > 0 && tc < 7 && tr > 0 && tr < 7) result.CenterScore += 0.2f;
                        if (tc > 1 && tc < 6 && tr > 1 && tr < 6) result.CenterScore += 0.3f;
                        if (tc > 2 && tc < 5 && tr > 2 && tr < 5) result.CenterScore += 0.5f;

                        //// useless
                        //if (checks)
                        //{
                        //    result.Checks++;
                        //    result.CheckTime += plyCount;
                        //}
                    }
                }

                currColor = currColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                chessboard.Play(move);
            }

            if (pgn.Info["Result"] == "1/2-1/2")
            {
                result.Draws = 1;
                result.DrawTime = plyCount;
            }

            if (castle)
            {
                result.TotalMovesWhenCastle = plyCount;
            }

            return result;
        }

        internal static float GetEngineSettingValue(float min, float middle, float max, float factor, float value)
        {
            if(float.IsNaN(value)) {
                return middle;
            }
            var val = middle + value * factor;

            if(val<min) { return min; }
            if(val>max) { return max; }

            return val;
        }

        internal static UInt16 ParseMove(Chessboard board, string strmove)
        {
            strmove = strmove.Replace("+", "").Replace("#", "");
            if (strmove == "O-O")
            {
                var startsq = board.currentColor == PlayerColor.White ? 4 : 60;
                var endsq = board.currentColor == PlayerColor.White ? 6 : 62;
                return (UInt16)(endsq + (startsq << 8));
            }
            else if (strmove == "O-O-O")
            {
                var startsq = board.currentColor == PlayerColor.White ? 4 : 60;
                var endsq = board.currentColor == PlayerColor.White ? 2 : 58;
                return (UInt16)(endsq + (startsq << 8));
            }
            else
            {
                char p = strmove[0];
                var capt_index = strmove.IndexOf('x');
                var prom_index = strmove.IndexOf('=');
                var check_index = strmove.IndexOf('+');
                var mate_index = strmove.IndexOf('#');

                var desambig_index = (p<'Z') ? 1 : 0;               

                var endsq_index = strmove.Length - 2;
                if (capt_index != -1) { endsq_index = capt_index + 1;}
                else if (check_index != -1) { endsq_index = check_index - 2;}
                else if (mate_index != -1) { endsq_index = mate_index - 2; }
                else if (prom_index != -1) { endsq_index = prom_index - 2; }

                var desambig_end = endsq_index-1;
                if(capt_index != -1) { desambig_end--; }
                var desambig_length = desambig_end - desambig_index+1;
                var desamb_c = -1;
                var desamb_r = -1;
                if (desambig_length > 0) {
                    var strdesambig = strmove.Substring(desambig_index, desambig_length);
                    desamb_c = GetDesambigCol(strdesambig);
                    desamb_r = GetDesambigRow(strdesambig);
                }

                // end sq
                int endsq_c = strmove[endsq_index] - 'a';
                int endsq_r = strmove[endsq_index+1] - '1';
                //endsq = endsq_c + 8 * endsq_r;
                //int end_row = endsq >> 3;
                //int end_col = endsq & 0x7;

                if (p == 'K')
                {
                    return ParseMoveK(board, endsq_c, endsq_r);
                }
                if (p == 'Q') {
                    return ParseMoveQ(board, endsq_c, endsq_r, desamb_c, desamb_r);
                }
                if (p == 'R') {
                    return ParseMoveR(board, endsq_c, endsq_r, desamb_c, desamb_r);
                }
                if (p == 'B') {
                    return ParseMoveB(board, endsq_c, endsq_r, desamb_c, desamb_r);
                }
                if (p == 'N') {
                    return ParseMoveN(board, endsq_c, endsq_r, desamb_c, desamb_r);
                }
                return ParseMoveP(board, endsq_c, endsq_r, desamb_c);
            }
        }

        private static int GetDesambigRow(string strdesambig)
        {
            foreach (var r in strdesambig)
            {
                if (r >= '1' && r <= '8') return (int)(r - '1');
            }
            return -1;
        }

        private static int GetDesambigCol(string strdesambig)
        {
            foreach (var c in strdesambig)
            {
                if (c >= 'a' && c <= 'h') return (int)(c - 'a');
            }
            return -1;
        }

        private static bool IsOkWithDesambig(int sq, int desamb_c, int desamb_r)
        {
            if (desamb_c != -1)
            {
                int col = sq & 0x7;
                if (col != desamb_c) return false;
            }
            if (desamb_c != -1)
            {
                int row = sq >> 3;
                if(row!=desamb_r) return false;
            }
            return true;
        }

        internal static UInt16 GetTaggedPonderMove(UInt16 nodeType, UInt16 weight, UInt16 move)
        {
            //// 0=firstchild 1=last child  3=leaf
            //var nodetype_ = (result >> 8) >> 6;
            //// square A1=0 => H8=3f
            //var startsq_ = (result >> 8) & 0x3f;
            //// 0=0%  1=25%  2=50%  3=100%
            //var weight_ = (result & 0xff) >> 6;
            //// square A1=0 => H8=3f
            //var endsq_ = (result & 0xff) & 0x3f;
            UInt16 res = (UInt16)(move + (nodeType << 14) + (weight << 6));

            return res;
        }

        private static UInt16 ParseMoveP(Chessboard board, int end_col, int end_row, int desamb_c)
        {
            int startsq = 0;
            var endsq = end_col + 8 * end_row;
            if (desamb_c != -1)
            {
                var diff_c = desamb_c - end_col;
                startsq = endsq + diff_c + (board.currentColor == PlayerColor.White ? -8 : +8);
            }
            else
            {
                // standard move
                startsq = endsq + (board.currentColor == PlayerColor.White ? -8 : +8);
                if (board.IsEmpty(startsq)) startsq += (board.currentColor == PlayerColor.White ? -8 : +8);
            }

            return (UInt16)(endsq + (startsq << 8));
        }

        private static UInt16 ParseMoveN(Chessboard board, int end_col, int end_row, int desamb_c, int desamb_r)
        {
            var endsq = end_col + 8 * end_row;
            // standard move
            //int endsq = Chessboard.StringToSquare(strmove);
            //int end_row = endsq >> 3;
            //int end_col = endsq & 0x7;
            int startsq = 0;

            // TODO: manage multiple knights 
            Piece p = board.currentColor == PlayerColor.White ? Piece.WN : Piece.BN;

            if (end_col > 1 && end_row > 0 && board.board[endsq - 10] == p && IsOkWithDesambig(endsq - 10, desamb_c, desamb_r)) startsq = endsq - 10;
            else if (end_col > 0 && end_row > 1 && board.board[endsq - 17] == p && IsOkWithDesambig(endsq - 17, desamb_c, desamb_r)) startsq = endsq - 17;
            else if (end_col > 1 && end_row < 7 && board.board[endsq + 6] == p && IsOkWithDesambig(endsq + 6, desamb_c, desamb_r)) startsq = endsq + 6;
            else if (end_col > 0 && end_row < 6 && board.board[endsq + 15] == p && IsOkWithDesambig(endsq + 15, desamb_c, desamb_r)) startsq = endsq + 15;
            else if (end_col < 6 && end_row < 7 && board.board[endsq + 10] == p && IsOkWithDesambig(endsq + 10, desamb_c, desamb_r)) startsq = endsq + 10;
            else if (end_col < 7 && end_row < 6 && board.board[endsq + 17] == p && IsOkWithDesambig(endsq + 17, desamb_c, desamb_r)) startsq = endsq + 17;
            else if (end_col < 7 && end_row > 1 && board.board[endsq - 15] == p && IsOkWithDesambig(endsq - 15, desamb_c, desamb_r)) startsq = endsq - 15;
            else if (end_col < 6 && end_row > 0 && board.board[endsq - 6] == p && IsOkWithDesambig(endsq - 6, desamb_c, desamb_r)) startsq = endsq - 6;

            return (UInt16)(endsq + (startsq << 8));
        }

        private static UInt16 ParseMoveB(Chessboard board, int end_col, int end_row, int desamb_c, int desamb_r)
        {
            var endsq = end_col + 8 * end_row;
            Piece p = board.currentColor == PlayerColor.White ? Piece.WB : Piece.BB;

            int[][] shifts = { new int[]{ 1, 1 }, new int[]{ -1, 1 }, new int[]{ 1, -1 }, new int[]{ -1, -1 } };

            foreach(var shift in shifts)
            {
                int start_col = end_col;
                int start_row = end_row;
                while (start_col > 0 && start_col < 7 && start_row > 0 && start_row < 7)
                {
                    start_col += shift[0];
                    start_row += shift[1];
                    int startsq = start_col + 8 * start_row;
                    if (board.board[startsq] == p && IsOkWithDesambig(startsq, desamb_c, desamb_r)) return (UInt16)(endsq + (startsq << 8));
                    if (board.board[startsq] != Piece.Empty) break;
                }
            }

            return 0;
        }

        private static UInt16 ParseMoveR(Chessboard board, int end_col, int end_row, int desamb_c, int desamb_r)
        {
            var endsq = end_col + 8 * end_row;
            Piece p = board.currentColor == PlayerColor.White ? Piece.WR : Piece.BR;
            int[][] shifts = { new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 } };

            foreach (var shift in shifts)
            {
                int start_col = end_col;
                int start_row = end_row;
                while (start_col > 0 && start_col < 7 && start_row > 0 && start_row < 7)
                {
                    start_col += shift[0];
                    start_row += shift[1];
                    int startsq = start_col + 8 * start_row;
                    if (board.board[startsq] == p && IsOkWithDesambig(startsq, desamb_c, desamb_r)) return (UInt16)(endsq + (startsq << 8));
                    if (board.board[startsq] != Piece.Empty) break;
                }
            }

            return 0;
        }

        private static UInt16 ParseMoveQ(Chessboard board, int end_col, int end_row, int desamb_c, int desamb_r)
        {
            var endsq = end_col + 8 * end_row;
            Piece p = board.currentColor == PlayerColor.White ? Piece.WQ : Piece.BQ;
            int[][] shifts = { new int[] { 1, 1 }, new int[] { -1, 1 }, new int[] { 1, -1 }, new int[] { -1, -1 } ,
                               new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 } };

            foreach (var shift in shifts)
            {
                int start_col = end_col;
                int start_row = end_row;
                while (start_col > 0 && start_col < 7 && start_row > 0 && start_row < 7)
                {
                    start_col += shift[0];
                    start_row += shift[1];
                    int startsq = start_col + 8 * start_row;
                    if (board.board[startsq] == p && IsOkWithDesambig(startsq, desamb_c, desamb_r)) return (UInt16)(endsq + (startsq << 8));
                    if (board.board[startsq] != Piece.Empty) break;
                }
            }

            return 0;
        }

        private static UInt16 ParseMoveK(Chessboard board, int end_col, int end_row)
        {
            // standard move
            //int endsq = Chessboard.StringToSquare(strmove);
            //int end_row = endsq >> 3;
            //int end_col = endsq & 0x7;
            int startsq = 0;
            var endsq = end_col + 8 * end_row;

            Piece p = board.currentColor == PlayerColor.White ? Piece.WK : Piece.BK;

            if (end_col > 0 && end_row > 0 && board.board[endsq - 9] == p) startsq = endsq - 9;
            else if (end_col > 0 && board.board[endsq - 1] == p) startsq = endsq - 1;
            else if (end_col > 1 && end_row < 7 && board.board[endsq + 7] == p) startsq = endsq + 7;
            else if (end_row < 7 && board.board[endsq + 8] == p) startsq = endsq + 8;
            else if (end_col < 7 && end_row < 7 && board.board[endsq + 9] == p) startsq = endsq + 9;
            else if (end_col < 7 && board.board[endsq + 1] == p) startsq = endsq + 1;
            else if (end_col < 7 && end_row > 1 && board.board[endsq - 7] == p) startsq = endsq - 7;
            else if (end_row > 0 && board.board[endsq - 8] == p) startsq = endsq - 8;

            return (UInt16)(endsq + (startsq << 8));
        }

        internal static float GetRandomness(float value)
        {
            // TODO: use a log regression
            // 1 elo     => 100%
            // 1000 elo  => ~25%
            // 1800 elo  => ~1%
            // 2200+ elo => 0%
            return Math.Max(0f, 100f*(1f - (value - 1f) / (2200f - 1f)));
        }

        internal static float GetGameLevel(float value)
        {
            return 100f / 2700f * value;
        }
    }
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            return new ArraySegment<T>(array, offset, length)
                        .ToArray();
        }
    }
}
