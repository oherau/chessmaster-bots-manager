using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public class OpeningBookNode
    {
        public UInt16 Move { get; set; }
        public byte Weight { get; set; }

        public SortedDictionary<UInt16,OpeningBookNode> Children = new SortedDictionary<UInt16, OpeningBookNode>();
    }
}
