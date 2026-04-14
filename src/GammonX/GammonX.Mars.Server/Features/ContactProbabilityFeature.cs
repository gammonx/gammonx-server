using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Combined evaluator for hit and escape probabilities.
    /// Generates each dice combinations legal move sequences once and extracts both features.
    /// </summary>
    public sealed class ContactProbabilityFeature : IFeature<ContactProbabilityResult>
    {
        private readonly IBoardService _boardService;

        public ContactProbabilityFeature(IBoardService boardService)
        {
            _boardService = boardService;
        }

        // <inheritdoc />
        public ContactProbabilityResult Eval(IBoardModel board, bool isWhite)
        {
            IBoardModel playersBoard = isWhite ? board.InvertBoard() : board;

            var exposedSet = GetExposedBlackCheckers(playersBoard);
            var oppHomeStart = playersBoard.HomeRangeWhite.Start.Value;
            var oppHomeEnd = playersBoard.HomeRangeWhite.End.Value;

            var hasExposed = exposedSet.Count > 0;
            var hasCheckersInOppHome = HasCheckersInOpponentHome(playersBoard, oppHomeStart, oppHomeEnd);

            int hitRolls1 = 0, hitRolls2 = 0;
            int escRolls1 = 0, escRolls2 = 0;

            for (int die1 = 1; die1 <= 6; die1++)
            {
                for (int die2 = die1; die2 <= 6; die2++)
                {
                    var weight = die1 == die2 ? 1 : 2;
                    int[] rolls = die1 == die2
                        ? [die1, die1, die1, die1]
                        : [die1, die2];

                    // opponents sequences — shared between hit detection and opponent escape
                    if (hasExposed)
                    {
                        var oppSequences = _boardService.GetLegalMoveSequences(playersBoard, true, rolls);
                        EvalHitFromSequences(oppSequences, exposedSet, weight, ref hitRolls1, ref hitRolls2);
                    }

                    // players sequences — for player escape
                    if (hasCheckersInOppHome)
                    {
                        var playerSequences = _boardService.GetLegalMoveSequences(playersBoard, false, rolls);
                        EvalEscapeFromSequences(playerSequences, oppHomeStart, oppHomeEnd, weight, ref escRolls1, ref escRolls2);
                    }
                }
            }

            return new ContactProbabilityResult(
                hitRolls1 / 36.0, hitRolls2 / 36.0,
                escRolls1 / 36.0, escRolls2 / 36.0);
        }

        private static void EvalHitFromSequences(
            MoveSequenceModel[] sequences, HashSet<int> exposedSet, int weight,
            ref int hitRolls1, ref int hitRolls2)
        {
            var canHitOne = false;
            var canHitTwo = false;

            foreach (var seq in sequences)
            {
                var hitExposed = new HashSet<int>();
                foreach (var move in seq.Moves)
                {
                    if (exposedSet.Contains(move.To))
                        hitExposed.Add(move.To);
                }

                if (hitExposed.Count >= 1) canHitOne = true;
                if (hitExposed.Count >= 2) canHitTwo = true;
                if (canHitOne && canHitTwo) break;
            }

            if (canHitOne) hitRolls1 += weight;
            if (canHitTwo) hitRolls2 += weight;
        }

        private static void EvalEscapeFromSequences(
            MoveSequenceModel[] sequences, int oppHomeStart, int oppHomeEnd, int weight,
            ref int escRolls1, ref int escRolls2)
        {
            var canEscapeOne = false;
            var canEscapeTwo = false;

            foreach (var seq in sequences)
            {
                var escaped = new HashSet<int>();
                foreach (var move in seq.Moves)
                {
                    if (move.From >= oppHomeStart && move.From <= oppHomeEnd &&
                        move.To < oppHomeStart)
                    {
                        escaped.Add(move.From);
                    }
                }

                if (escaped.Count >= 1) canEscapeOne = true;
                if (escaped.Count >= 2) canEscapeTwo = true;
                if (canEscapeOne && canEscapeTwo) break;
            }

            if (canEscapeOne) escRolls1 += weight;
            if (canEscapeTwo) escRolls2 += weight;
        }

        private static bool HasCheckersInOpponentHome(IBoardModel board, int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                if (board.Fields[i] > 0) return true;
            }
            return false;
        }

        private static HashSet<int> GetExposedBlackCheckers(IBoardModel board)
        {
            return board.Fields.Index()
                .Where(i => i.Item == 1)
                .Select(i => i.Index)
                .ToHashSet();
        }
    }

    public readonly record struct ContactProbabilityResult(
        double HitProbability1,
        double HitProbability2,
        double EscapeProbability1,
        double EscapeProbability2
    );
}