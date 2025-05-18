using CMPersonalityManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public enum ProgramVersion
    {
        Unknown,
        CM10,
        CM11,
    };

    public class OpeningBook
    {
        public OpeningBook() {
            Version = ProgramVersion.CM11; // should be CM10 for better compatibility
        }

        public OpeningBookNode RootNode = new OpeningBookNode();
        public ProgramVersion Version { get; internal set; }
        public string BookName { get; set; }
    }
}
