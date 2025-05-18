using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CMPersonalityManager.Tools;

namespace ChessmasterBotsManager
{
    public class Chessboard
    {
        public Piece[] board;// = new Piece[64];

        public PlayerColor currentColor { get; private set; }

        //int WK_pos = 4;
        //int BK_pos = 60;
        //List<int> WQ_pos = new List<int>();
        //List<int> BQ_pos = new List<int>();
        //List<int> WR_pos = new List<int>();
        //List<int> BR_pos = new List<int>();
        //List<int> WN_pos = new List<int>();
        //List<int> BN_pos = new List<int>();
        //List<int> WB_pos = new List<int>();
        //List<int> BB_pos = new List<int>();
        //List<int> WP_pos = new List<int>();
        //List<int> BP_pos = new List<int>();

        public Chessboard()
        {
            board = new Piece[] { Piece.WR, Piece.WN,Piece.WB,Piece.WQ,Piece.WK,Piece.WB,Piece.WN,Piece.WR,
                                Piece.WP,Piece.WP,Piece.WP,Piece.WP,Piece.WP,Piece.WP,Piece.WP,Piece.WP,
                                Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,
                                Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,
                                Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,
                                Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,Piece.Empty,
                                Piece.BP,Piece.BP,Piece.BP,Piece.BP,Piece.BP,Piece.BP,Piece.BP,Piece.BP,
                                Piece.BR, Piece.BN,Piece.BB,Piece.BQ,Piece.BK,Piece.BB,Piece.BN,Piece.BR };
            currentColor = PlayerColor.White;

            //WK_pos = 4;
            //BK_pos = 60;

            //WQ_pos.Add(3);
            //BQ_pos.Add(59);

            //WR_pos.Add(0);
            //WR_pos.Add(7);
            //BR_pos.Add(56);
            //BR_pos.Add(63);

            //WN_pos.Add(1);
            //WN_pos.Add(6);
            //BN_pos.Add(57);
            //BN_pos.Add(62);

            //WB_pos.Add(2);
            //BB_pos.Add(5);
            //WP_pos.Add(58);
            //BP_pos.Add(61);

            //for (int i = 8; i < 16; i++) WP_pos.Add(i);
            //for (int i = 48; i < 56; i++) BP_pos.Add(i);
        }

        internal void Play(UInt16 move)
        {
            
            var endsq = move & 0x3f;
            var startsq = move >> 8;

            // standard move
            var p = board[startsq];

            // NOTE: Chessmaster opening book does not handle sub-promotions
            //       The queen is always selected by default

            // promotion
            if (p == Piece.WP && endsq >= 56) p = Piece.WQ;
            if (p == Piece.BP && endsq <= 7) p = Piece.BQ;

            // en-passant
            if (p == Piece.WP && board[startsq] == Piece.Empty) board[endsq - 8] = Piece.Empty;
            if (p == Piece.BP && board[startsq] == Piece.Empty) board[endsq + 8] = Piece.Empty;

            // castling
            if (p == Piece.WK && startsq == 4 && endsq == 6) { board[7] = Piece.Empty; board[5] = Piece.WR; }
            if (p == Piece.WK && startsq == 4 && endsq == 2) { board[0] = Piece.Empty; board[3] = Piece.WR; }
            if (p == Piece.BK && startsq == 60 && endsq == 62) { board[63] = Piece.Empty; board[61] = Piece.WR; }
            if (p == Piece.BK && startsq == 60 && endsq == 58) { board[56] = Piece.Empty; board[59] = Piece.WR; }

            board[endsq] = p;
            board[startsq] = Piece.Empty;

            currentColor = (currentColor == PlayerColor.White) ? PlayerColor.Black : PlayerColor.White;
            //DebugDisplayMove(move);
            //DebugDisplayBoard(board);
        }

        private void DebugDisplayMove(UInt16 move)
        {
            var startsq = move & 0x3f;
            var endsq = move >> 8;
            string strmove = GetMoveString(startsq, endsq);
            Debug.WriteLine(strmove);
        }

        private string GetMoveString(int startsq, int endsq)
        {
            return GetSquareString(startsq) + "-" + GetSquareString(endsq);
        }

        private string GetSquareString(int sq)
        {
            var col = sq & 0x7;
            var row = sq >> 3;
            return ""+ "abcdefgh"[col] + "12345678"[row];
        }

        private void DebugDisplayBoard(Piece[] b)
        {
            for(int r = 0; r < 8; r++)
            {
                Debug.Write("\n  +---+---+---+---+---+---+---+---+\n");
                Debug.Write(r + " | ");
                for (int c = 0; c < 8; c++)
                {
                    Debug.Write((char)b[c+8*(7-r)]+ " | ");
                }
            }
            Debug.WriteLine("\n  +---+---+---+---+---+---+---+---+");
            Debug.WriteLine("    A   B   C   D   E   F   G   H\n");
        }

        internal bool IsCastle(UInt16 move)
        {
            var startsq = move & 0x3f;
            var endsq = move >> 8;
            var p = board[startsq];
            return (p == Piece.WK || p==Piece.BK) && (Math.Abs(startsq-endsq)==2);
        }

        internal bool IsCapture(UInt16 move)
        {
            var endsq = move >> 8;
            return board[endsq] != Piece.Empty;
        }

        internal bool IsPromotion(UInt16 move)
        {
            var startsq = move & 0x3f;
            var p = board[startsq];
            return (p == Piece.WP && startsq > 55) || (p == Piece.BP && startsq < 16);
        }

        internal bool IsCheck(UInt16 move)
        {
            // TODO: implement this function later (only usefull for king protection param)
            //throw new NotImplementedException();
            return false;
        }

        internal Piece GetMovedPiece(UInt16 move)
        {
            var startsq = move & 0x3f;
            return board[startsq];
        }

        internal bool IsEmpty(int sq)
        {
            return board[sq] == Piece.Empty;
        }

        internal static int StringToSquare(string move)
        {
            int c = move[move.Length - 2] - 'a';
            int r = move[move.Length - 1] - '1';
            return c + 8 * r;
        }
    }
}
