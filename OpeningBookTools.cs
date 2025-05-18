using CMPersonalityManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CMPersonalityManager.Tools;

namespace ChessmasterBotsManager
{
    public class OpeningBookTools
    {
        public static Encoding MainEncoding = Encoding.GetEncoding("iso-8859-1");

        public static OpeningBook ConvertToOpeningBook(StatOpeningBook statOpeningBook)
        {
            var book = new OpeningBook();
            book.RootNode.Children = ConvertChildren(statOpeningBook.RootNode.Children);

            return book;
        }

        private static SortedDictionary<UInt16, OpeningBookNode> ConvertChildren(Dictionary<UInt16, StatOpeningBookNode> statChildren)
        {
            var children = new SortedDictionary<UInt16, OpeningBookNode>();
            var nbchilds = statChildren.Count();
            if (nbchilds > 0)
            {
                var maxply = statChildren.Max(c => c.Value.TimesPlayed);
                float wfactor = 100f / maxply;

                foreach (var statChild in statChildren)
                {
                    var node = new OpeningBookNode();
                    // TODO: find the best type to store weight info (ushort, byte, enum)
                    // quantized value to [0,25,50,100]
                    var realweight = wfactor * statChild.Value.TimesPlayed;

                    node.Weight = Tools.GetWeightFromPct(realweight);
                    node.Move = statChild.Key;

                    children[statChild.Key] = node;
                    // launch recursion on node
                    node.Children = ConvertChildren(statChild.Value.Children);
                }
            }
            return children;
        }

        internal static StatOpeningBook? AnalysePlayerOpenings(string playerName, List<Pgn> database, ProgressBar pb, TextBox sbx)
        {
            StatOpeningBook result = new StatOpeningBook();
            pb.Minimum = 0;
            pb.Maximum = database.Count();
            pb.Value = 0;

            foreach (var pgn in database)
            {
                if (pgn.Info["White"] == playerName) { result.AppendOpeningMoves(result,PlayerColor.White, pgn.Moves);}
                else if(pgn.Info["Black"] == playerName) { result.AppendOpeningMoves(result, PlayerColor.Black, pgn.Moves); }
                pb.Value++;
            }

            return result;
        }

        internal static List<KeyValuePair<OpeningBook,float>> GetClosestOpeningBooks(OpeningBook player_book, Dictionary<string, OpeningBook> openingBooks)
        {
            var scores = new Dictionary<OpeningBook, float>();
            var scorespct = new Dictionary<OpeningBook, float>();
            float maxscore = 0;
            foreach(var item in openingBooks)
            {
                float score = GetDistance(player_book, item.Value);
                scores[item.Value] = score;
                if (maxscore < score) { maxscore = score; }
            }

            // convert score to pct: score 0 => 100%   maxscore => 0%
            
            foreach(var item in scores)
            {
                scorespct[item.Key] = 100f - item.Value * (100f / maxscore);
            }

            var sortedScores = scorespct.OrderByDescending(x => x.Value).ToList();
            // TODO might also return the score
            return sortedScores;
        }

        private static float GetDistance(OpeningBook b1, OpeningBook b2)
        {
            // no fancy calculation here (ex Robinson)
            // we only alternatively go through one book and take the other for comparison

            float score = 0;

            score += GetDistanceScore(b1.RootNode, b2.RootNode);
            score += GetDistanceScore(b2.RootNode, b1.RootNode);

            return score;
        }

        private static float GetDistanceScore(OpeningBookNode n1, OpeningBookNode n2)
        {
            var stack1 = new Stack<OpeningBookNode>();
            var stack2 = new Stack<Tuple<OpeningBookNode, OpeningBookNode>>();

            float score = 0;
            stack2.Push(new Tuple<OpeningBookNode, OpeningBookNode>(n1,n2));

            while (stack2.Count() > 0)
            {
                var elem = stack2.Pop();
                score += Math.Abs(elem.Item1.Weight - elem.Item2.Weight);
                foreach (var c1 in elem.Item1.Children)
                {
                    if (elem.Item2.Children.ContainsKey(c1.Key))
                    {
                        stack2.Push(new Tuple<OpeningBookNode, OpeningBookNode>(c1.Value, elem.Item2.Children[c1.Key])); 
                    }
                    else
                    {
                        stack1.Push(c1.Value);
                    }
                }
            }

            while (stack1.Count() > 0)
            {
                var child = stack1.Pop();
                score += child.Weight;
                foreach (var cc in child.Children.Values)
                {
                    stack1.Push(cc);
                }
            }

            return score;
        }

        public static OpeningBook? LoadOpeningBook(string fileName)
        {
            OpeningBook? book = null;
            if (File.Exists(fileName))
            {
                using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    book = new OpeningBook();
                    book.BookName = Path.GetFileNameWithoutExtension(fileName);
                    var stack = new Stack<Tuple<byte, OpeningBookNode>>();
                    using (var reader = new BinaryReader(stream, MainEncoding, false))
                    {
                        var version = string.Join(null, reader.ReadChars(4));
                        if (version == "BOO!")
                        {
                            book.Version = ProgramVersion.CM11;
                            var nbitems = reader.ReadInt32();
                            var sizeinbytes = reader.ReadInt32();// ? size of comments ???

                            var currNode = book.RootNode;
                            //var temp = reader.ReadBytes(nbitems*2);
                            for (int i = 0; i < nbitems; i++)
                            {
                                byte[] v = reader.ReadBytes(2);

                                // 0=firstchild 1=last child  3=leaf
                                byte nodetype = (byte)(v[0] >> 6);
                                // square A1=0 => H8=3f
                                byte startsq = (byte)(v[0] & 0x3f);
                                // 0=0%  1=25%  2=50%  3=100%
                                byte weight = (byte)(v[1] >> 6);
                                // square A1=0 => H8=3f
                                byte endsq = (byte)(v[1] & 0x3f);

                                UInt16 move = (UInt16)(startsq + (endsq << 8));

                                var newNode = new OpeningBookNode()
                                {
                                    Move = move,
                                    Weight = weight,
                                };

                                currNode.Children[move] = newNode;
                                switch (nodetype)
                                {
                                    case 0:
                                        stack.Push(new Tuple<byte, OpeningBookNode>(nodetype, currNode));
                                        currNode = newNode;
                                        break;
                                    case 1:
                                        stack.Push(new Tuple<byte, OpeningBookNode>(nodetype, currNode));
                                        currNode = newNode;
                                        break;
                                    case 2:
                                        // same level no need to go deeper
                                        break;
                                    case 3:
                                        // last node
                                        while (stack.Count() > 0 && nodetype != 1)
                                        {
                                            Tuple<byte, OpeningBookNode> elem = stack.Pop();
                                            nodetype = elem.Item1;
                                            currNode = elem.Item2;
                                        }
                                        break;
                                }
                            }


                            // TODO: comments part - not in use for now
                            //var col1 = new List<string>();
                            //var col2 = new List<string>();
                            //var col3 = new List<string>();
                            //var col4 = new List<string>();
                            //while (reader.BaseStream.Position != reader.BaseStream.Length)
                            //{
                            //    // seems 00 00 00 marks a new section
                            //    // 00 00 00 04 82 => ECO
                            //    // 00 00 00 04 81 => variation
                            //    // 00 00 00 3e 80 => comment
                            //    //var id = reader.ReadInt32();
                            //    var id = reader.ReadBytes(4);

                            //    var length = reader.ReadByte();
                            //    var type = reader.ReadByte();
                            //    switch (type)
                            //    {
                            //        case 130: // 82 = ECO code
                            //            var eco = MainEncoding.GetString(reader.ReadBytes(length));
                            //            //var eco = string.Join(null, reader.ReadChars(length));
                            //            col1.Add(eco);
                            //            break;
                            //        //case 132: // 84 = Comment
                            //        //    var comment = MainEncoding.GetString(reader.ReadBytes(length));
                            //        //    col2.Add(comment);
                            //        //    break;
                            //        case 129: // 81 = comment
                            //            var comment1 = MainEncoding.GetString(reader.ReadBytes(length));
                            //            col3.Add(comment1);
                            //            break;
                            //        case 128: // 80 = comment
                            //            var comment2 = MainEncoding.GetString(reader.ReadBytes(length));
                            //            col4.Add(comment2);
                            //            break;
                            //    }
                            //}
                        }
                        else if (version == "UGWS")
                        {
                            book.Version = ProgramVersion.CM10;
                        }
                        else { book.Version = ProgramVersion.Unknown; }

                        ////var d2 = reader.ReadByte();
                        //var dum = reader.ReadBytes(6);
                        //var d3 = reader.ReadBytes(100);

                    }
                }
            }

            return book;
        }

        internal static void SaveOpeningBook(OpeningBook book, string fileName)
        {
            if (File.Exists(fileName))
            {
                // save a .obk backup
            }

            using (var stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var stack = new Stack<Tuple<byte,OpeningBookNode>>();
                using (var writer = new BinaryWriter(stream, Encoding.Default, false))
                {
                    if (book.Version == ProgramVersion.CM11)
                    {
                        writer.Write(MainEncoding.GetBytes("BOO!"));

                        // read the book tree and prepare pondered moves
                        var ponderedmoves = new List<UInt16>();
                        stack.Push(new Tuple<byte, OpeningBookNode>(1,book.RootNode));
                        Tuple<byte, OpeningBookNode> currentNode;
                        while(stack.Count() > 0)
                        {
                            currentNode = stack.Pop();
                            int c = 1;
                            foreach(var child in currentNode.Item2.Children)
                            {
                                var nbcchildren = child.Value.Children.Count();
                                bool isfirst = c == 1;
                                bool islast = c == currentNode.Item2.Children.Count();

                                byte nodeType = 0;
                                if(islast && nbcchildren == 0)
                                {
                                    nodeType = 3;
                                }
                                else if(islast && nbcchildren > 0)
                                {
                                    nodeType = 1;
                                }
                                else if (!islast && nbcchildren == 0)
                                {
                                    nodeType = 2;
                                }
                                var weight = child.Value.Weight;
                                var move = child.Value.Move;
                                ponderedmoves.Add(Tools.GetTaggedPonderMove(nodeType, weight, move));

                                if (nodeType == 0 || nodeType == 1) { stack.Push(new Tuple<byte, OpeningBookNode>(nodeType, child.Value)); }
                                c++;
                            }
                        }

                        // save the tree in file
                        writer.Write(ponderedmoves.Count());
                        writer.Write(0);

                        foreach(var ponderedmove in ponderedmoves)
                        {
                            byte b0 = (byte)(ponderedmove >> 8);
                            byte b1 = (byte)(ponderedmove & 0xff);
                            writer.Write(b0);
                            writer.Write(b1);
                        }
                    }
                    else if (book.Version == ProgramVersion.CM10)
                    {
                        writer.Write(MainEncoding.GetBytes("UGWS"));

                    }
                }
            }            
        }
    }
}
