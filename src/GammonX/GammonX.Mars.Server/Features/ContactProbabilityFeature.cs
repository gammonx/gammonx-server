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
            var exposedCheckers = GetExposedCheckers(board, isWhite);

            var hasCheckersInOppHome = HasCheckersInOpponentHome(board, !isWhite);

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

                    // opponents sequences — early termination once both hit-1 and hit-2 are confirmed
                    if (exposedCheckers.Count != 0)
                    {
                        bool canHitOne = false;
                        bool canHitTwo = false;
                        _boardService.ExploreLegalMoveSequences(board, !isWhite, rolls, moves =>
                        {
                            int hitCount = 0;
                            foreach (var move in moves)
                            {
                                if (exposedCheckers.Contains(move.To))
                                    hitCount++;
                            }
                            if (hitCount >= 1) canHitOne = true;
                            if (hitCount >= 2) canHitTwo = true;
                            return canHitOne && canHitTwo;
                        });
                        if (canHitOne) hitRolls1 += weight;
                        if (canHitTwo) hitRolls2 += weight;
                    }

                    // players sequences — early termination once both esc-1 and esc-2 are confirmed
                    if (hasCheckersInOppHome)
                    {
                        bool canEscapeOne = false;
                        bool canEscapeTwo = false;
                        _boardService.ExploreLegalMoveSequences(board, isWhite, rolls, moves =>
                        {
                            int escCount = 0;
                            foreach (var move in moves)
                            {
                                // we need to check if white is in black home range
                                // and if black is in white home range (escape from opponents home)
                                var fromInOppHomeRange = board.IsInHomeOperator(!isWhite, move.From);
                                var toInOppHomeRange = board.IsInHomeOperator(!isWhite, move.To);
                                if (fromInOppHomeRange && !toInOppHomeRange)
                                {
                                    escCount++;
                                }
                            }
                            if (escCount >= 1) canEscapeOne = true;
                            if (escCount >= 2) canEscapeTwo = true;
                            return canEscapeOne && canEscapeTwo;
                        });
                        if (canEscapeOne) escRolls1 += weight;
                        if (canEscapeTwo) escRolls2 += weight;
                    }
                }
            }

            return new ContactProbabilityResult(
                hitRolls1 / 36.0, hitRolls2 / 36.0,
                escRolls1 / 36.0, escRolls2 / 36.0);
        }

        private static bool HasCheckersInOpponentHome(IBoardModel board, bool isWhite)
        {
            var fieldTuples = board.Fields.Index();

            if (!isWhite)
            {
                // we want to check if white checkers are in black home
                var whiteIndices = fieldTuples.Where(i => i.Item < 0).Select(i => i.Index);
                return whiteIndices.Any(wi => board.IsInHomeOperator(isWhite, wi));
            }
            else
            {
                // we want to check if black checkers are in white home
                var blackIndices = fieldTuples.Where(i => i.Item > 0).Select(i => i.Index);
                return blackIndices.Any(wi => board.IsInHomeOperator(isWhite, wi));
            }
        }

        private static HashSet<int> GetExposedCheckers(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                return board.Fields.Index()
                .Where(i => i.Item == -1)
                .Select(i => i.Index)
                .ToHashSet();
            }
            else
            {
                return board.Fields.Index()
                .Where(i => i.Item == 1)
                .Select(i => i.Index)
                .ToHashSet();
            }
        }
    }

    public readonly record struct ContactProbabilityResult(
        double HitProbability1,
        double HitProbability2,
        double EscapeProbability1,
        double EscapeProbability2
    );
}