using CMPersonalityManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public class StatOpeningBook
    {
        public StatOpeningBookNode RootNode = new StatOpeningBookNode();
        public ProgramVersion Version { get; internal set; }
        public string BookName { get; set; }

        internal void AppendOpeningMoves(StatOpeningBook result, Tools.PlayerColor color, List<UInt16> moves)
        {
            StatOpeningBookNode currNode = RootNode;
            var currColor = Tools.PlayerColor.White;
            foreach (var move in moves)
            {
                if (currNode.Children.ContainsKey(move))
                {
                    currNode = currNode.Children[move];
                }
                else
                {
                    currNode = currNode.Children[move] = new StatOpeningBookNode();
                }
                if(currColor == color)
                {
                    currNode.TimesPlayed++;
                }
                currColor = (currColor == Tools.PlayerColor.White) ? Tools.PlayerColor.Black : Tools.PlayerColor.White;
            }
        }
    }
}
