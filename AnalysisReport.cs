using System.Collections.Generic;

namespace CMPersonalityManager
{
    public class AnalysisReport
    {
        public AnalysisReport()
        {
            Openings = new Dictionary<string, int>();
        }

        public static AnalysisReport operator+(AnalysisReport a, AnalysisReport b)
        {

            var result = new AnalysisReport
            {
                NbGames = a.NbGames + b.NbGames,
                KingMoves = a.KingMoves + b.KingMoves,
                QueenMoves = a.QueenMoves + b.QueenMoves,
                RookMoves = a.RookMoves + b.RookMoves,
                BishopMoves = a.BishopMoves + b.BishopMoves,
                KnightMoves = a.KnightMoves + b.KnightMoves,
                PawnMoves = a.PawnMoves + b.PawnMoves,
                KingTakes = a.KingTakes + b.KingTakes,
                QueenTakes = a.QueenTakes + b.QueenTakes,
                RookTakes = a.RookTakes + b.RookTakes,
                BishopTakes = a.BishopTakes + b.BishopTakes,
                KnightTakes = a.KnightTakes + b.KnightTakes,
                PawnTakes = a.PawnTakes + b.PawnTakes,
                Promotions = a.Promotions + b.Promotions,
                PromotionTime = a.PromotionTime + b.PromotionTime,
                Castles = a.Castles + b.Castles,
                CastleTime = a.CastleTime + b.CastleTime,
                Draws = a.Draws + b.Draws,
                DrawTime = a.DrawTime + b.DrawTime,
                Checks = a.Checks + b.Checks,
                CheckTime = a.CheckTime + b.CheckTime,
                TotalMovesWhenCastle = a.TotalMovesWhenCastle + b.TotalMovesWhenCastle,
                CenterScore = a.CenterScore + b.CenterScore,
                MaxElo = Math.Max(a.MaxElo.HasValue ? a.MaxElo.Value : 0, b.MaxElo.HasValue ? b.MaxElo.Value : 0),
                NbInCheckByOpponent = a.NbInCheckByOpponent + b.NbInCheckByOpponent,
                NbInCheckOpponent = a.NbInCheckOpponent + b.NbInCheckOpponent,
            };

            result.Openings = a.Openings;
            foreach(var o in b.Openings)
            {
                if(result.Openings.ContainsKey(o.Key))
                {
                    result.Openings[o.Key] += o.Value; 
                }
                else
                {
                    result.Openings[o.Key] = o.Value;
                }
            }
            return result;
        }

        //public string PlayerName { get; set; }
        public int NbGames { get; set; }
        public int KingMoves { get; set; }
        public int QueenMoves { get; set; }
        public int RookMoves { get; set; }
        public int BishopMoves { get; set; }
        public int KnightMoves { get; set; }
        public int PawnMoves { get; set; }
        public int KingTakes { get; set; }
        public int QueenTakes { get; set; }
        public int RookTakes { get; set; }
        public int BishopTakes { get; set; }
        public int KnightTakes { get; set; }
        public int PawnTakes { get; set; }
        public int Promotions { get; set; }
        public int PromotionTime { get; set; }
        public int Castles { get; set; }
        public int CastleTime { get; set; }
        public int Draws { get; set; }
        public int DrawTime { get; set; }
        public int Checks { get; set; }
        public int CheckTime { get; set; }
        public float CenterScore { get; set; }
        public int NbInCheckOpponent { get; set; }
        public int NbInCheckByOpponent { get; set; }
        public Dictionary<string, int> Openings;

        public int TotalMovesWhenCastle { get; set; }
        public int TotalMoves { get { return KingMoves + QueenMoves + RookMoves + BishopMoves + KnightMoves + PawnMoves; } }
        //public int TotalMoves { get; set; }
        public int TotalTakes { get { return KingTakes + QueenTakes + RookTakes + BishopTakes + KnightTakes + PawnTakes; } }
        public float KingMovesPct { get { return (float)KingMoves / (float)TotalMoves; } }
        public float QueenMovesPct { get { return (float)QueenMoves / (float)TotalMoves; } }
        public float RookMovesPct { get { return (float)RookMoves / (float)TotalMoves; } }
        public float BishopMovesPct { get { return (float)BishopMoves / (float)TotalMoves; } }
        public float KnightMovesPct { get { return (float)KnightMoves / (float)TotalMoves; } }
        public float PawnMovesPct { get { return (float)PawnMoves / (float)TotalMoves; } }
        public float DrawsPct { get { return (float)Draws / NbGames; } }
        // not significant - use nb of checks the opponent is giving instead
        public float CastleTimePct { get { return (float)CastleTime / (float)TotalMovesWhenCastle; } }

        
        public float CenterScorePct { get { return (float)CastleTime / (float)TotalMoves; } }
        public float Agressiveness { get { return (TotalTakes+Checks) / (2f*TotalMoves); } }
        public float InCheckByOpponentPct { get { return NbInCheckByOpponent / (2f * TotalMoves); } }

        public float InCheckOpponentPct { get { return NbInCheckOpponent / (2f * TotalMoves); } }
        public int? MaxElo { get; set; }
        
    }
}