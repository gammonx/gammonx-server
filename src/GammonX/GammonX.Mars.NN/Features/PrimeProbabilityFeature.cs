using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Features
{
    public readonly record struct PrimeProbabilityResult(
        double PrimeProbabilityPlayer,
        double PrimeProbabilityOpp
    );

    /// <summary>
    /// Calculates the probability of forming a prime (6 consecutive occupied points)
    /// in the next turn for both the player and the opponent.
    /// </summary>
    public class PrimeProbabilityFeature : IFeature<PrimeProbabilityResult>
    {
        private const int PrimeLength = 6;

        private readonly IBoardService _boardService;

        public PrimeProbabilityFeature(IBoardService boardService)
        {
            _boardService = boardService;
        }

        // <inheritdoc />
        public PrimeProbabilityResult Eval(IBoardModel board, bool isWhite)
        {
            var playerCounts = GetCheckerCounts(board, isWhite);
            var oppCounts = GetCheckerCounts(board, !isWhite);

            // TODO: only explore for a prime probability if checkers needed for a prime == dice count (2 or 4)

            var playerPrimeRolls = 0;
            var oppPrimeRolls = 0;

            for (var die1 = 1; die1 <= 6; die1++)
            {
                for (var die2 = die1; die2 <= 6; die2++)
                {
                    var weight = die1 == die2 ? 1 : 2;
                    int[] rolls = die1 == die2
                        ? [die1, die1, die1, die1]
                        : [die1, die2];

                    // players chance to form a prime
                    var playerCanFormPrime = false;
                    _boardService.ExploreLegalMoveSequences(board, isWhite, rolls, moves =>
                    {
                        if (FormsPrime(playerCounts, moves))
                        {
                            playerCanFormPrime = true;
                            return true;
                        }
                        return false;
                    });
                    if (playerCanFormPrime)
                        playerPrimeRolls += weight;

                    // opponents chance to form a prime
                    var oppCanFormPrime = false;
                    _boardService.ExploreLegalMoveSequences(board, !isWhite, rolls, moves =>
                    {
                        if (FormsPrime(oppCounts, moves))
                        {
                            oppCanFormPrime = true;
                            return true;
                        }
                        return false;
                    });
                    if (oppCanFormPrime)
                        oppPrimeRolls += weight;
                }
            }

            return new PrimeProbabilityResult(
                playerPrimeRolls / 36.0,
                oppPrimeRolls / 36.0);
        }

        /// <summary>
        /// Checks whether applying the given move sequence results in 6 or more
        /// consecutive occupied points for the player.
        /// </summary>
        private static bool FormsPrime(Dictionary<int, int> currentCounts, IReadOnlyList<MoveModel> moves)
        {
            // we compute net change per board point
            var deltas = new Dictionary<int, int>();

            foreach (var move in moves)
            {
                var from = move.From;
                var to = move.To;

                if (from != BoardPositions.HomeBarWhite && from != BoardPositions.HomeBarBlack)
                {
                    deltas[from] = deltas.GetValueOrDefault(from) - 1;
                }

                if (to != BoardPositions.BearOffWhite && to != BoardPositions.BearOffBlack)
                {
                    deltas[to] = deltas.GetValueOrDefault(to) + 1;
                }
            }

            // we build resulting count per point and check for a 6-run
            // only touched points need re-evaluation, untouched points keep their original count.
            // we scan the full board range (0-23) for consecutive runs.
            var consecutive = 0;
            for (var i = 0; i < 24; i++)
            {
                var count = currentCounts.GetValueOrDefault(i) + deltas.GetValueOrDefault(i);
                if (count > 0)
                {
                    consecutive++;
                    if (consecutive >= PrimeLength)
                        return true;
                }
                else
                {
                    consecutive = 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the checker count per board index for the given player.
        /// Counts are always positive (absolute value).
        /// </summary>
        private static Dictionary<int, int> GetCheckerCounts(IBoardModel board, bool isWhite)
        {
            var counts = new Dictionary<int, int>();
            var fields = board.Fields;
            for (var index = 0; index < fields.Length; index++)
            {
                if (isWhite && fields[index] < 0)
                {
                    counts[index] = Math.Abs(fields[index]);
                }
                else if (!isWhite && fields[index] > 0)
                {
                    counts[index] = Math.Abs(fields[index]);
                }
            }
            return counts;
        }

    }
}
