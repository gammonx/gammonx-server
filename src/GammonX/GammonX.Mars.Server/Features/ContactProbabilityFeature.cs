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

                    // opponents sequences — early termination once both hit-1 and hit-2 are confirmed
                    if (hasExposed)
                    {
                        bool canHitOne = false, canHitTwo = false;
                        _boardService.ExploreSequencesUntil(playersBoard, true, rolls, moves =>
                        {
                            int hitCount = 0;
                            foreach (var move in moves)
                            {
                                if (exposedSet.Contains(move.To))
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
                        bool canEscapeOne = false, canEscapeTwo = false;
                        _boardService.ExploreSequencesUntil(playersBoard, false, rolls, moves =>
                        {
                            int escCount = 0;
                            foreach (var move in moves)
                            {
                                if (move.From >= oppHomeStart && move.From <= oppHomeEnd &&
                                    move.To < oppHomeStart)
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