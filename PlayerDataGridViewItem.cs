using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessmasterBotsManager
{
    public class PlayerDataGridViewItem
    {
        public string Name { get; set; }
        public float KingMovesPct { get; set; }
        public float QueenMovesPct { get; set; }
        public float RookMovesPct { get; set; }
        public float BishopMovesPct { get; set; }
        public float KnightMovesPct { get; set; }
        public float PawnMovesPct { get; set; }
        public float DrawsPct { get; set; }
        public float CenterScorePct { get; set; }
        public float CastleTimePct { get; set; }
        public int? MaxElo { get; set; }
        public float Agressiveness { get; set; }
    }
}
