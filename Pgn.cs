using ChessmasterBotsManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMPersonalityManager
{
    public class Pgn
    {
        public Dictionary<string, string> Info = new Dictionary<string, string>();
        public List<UInt16> Moves = new List<UInt16>();
        public int NbWhiteInCheck { get; set; }
        public int NbBlackInCheck { get; set; }

        public bool Parse(string text)
        {
            try
            {
                Info.Clear();
                Moves.Clear();
                List<Pgn> database = new List<Pgn>();

                StringBuilder maincontent = new StringBuilder();

                var sr = new StringReader(text);

                int count = 0;
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("["))
                    {
                        var p = (line.Replace("[", "").Replace("]", ""));
                        var splitpt = p.IndexOf(' ');

                        var key = p.Substring(0, splitpt);
                        var val = p.Substring(splitpt + 1).Replace("\"", "");
                        Info[key] = val;
                    }
                    else
                    {
                        maincontent.Append(line + " ");
                    }
                    count++;
                }

                string content = maincontent.ToString().Trim();
                int c = 1;
                var chessboard = new Chessboard();
                while (content.Length > 0)
                {
                    string curr;
                    string ply = c + ".";
                    string nxtply = (c + 1) + ".";

                    var p1 = content.IndexOf(ply);
                    var p2 = content.IndexOf(nxtply);
                    if (p2 != -1)
                    {
                        curr = content.Substring(p1 + ply.Length, p2 - (p1 + ply.Length)).Trim();
                        content = content.Substring(p2).Trim();
                    }
                    else
                    {
                        curr = content.Substring(p1 + ply.Length).Trim();
                        content = "";
                    }
                    var mvs = curr.Split(' ');

                    foreach (var strmove in mvs)
                    {
                        if (!string.IsNullOrWhiteSpace(strmove) && !strmove.StartsWith('0') && !strmove.StartsWith('1') && !strmove.StartsWith('*'))
                        {
                            if (strmove.Contains("+") || strmove.Contains("#"))
                            {
                                if (chessboard.currentColor == Tools.PlayerColor.White)
                                { NbBlackInCheck++; }
                                else
                                { NbWhiteInCheck++; }
                            }

                            var move = Tools.ParseMove(chessboard, strmove);
                            Moves.Add(move);
                            chessboard.Play(move);
                        }
                    }
                    c++;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
