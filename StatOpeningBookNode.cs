using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public class StatOpeningBookNode
    {
        public int TimesPlayed { get; set; }
        public Dictionary<UInt16, StatOpeningBookNode> Children = new Dictionary<UInt16, StatOpeningBookNode>();
    }
}
