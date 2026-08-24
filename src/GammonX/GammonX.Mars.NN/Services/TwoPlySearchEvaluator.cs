using GammonX.Engine.Models;

using GammonX.Engine.Services;

namespace GammonX.Mars.NN.Services
{
    internal sealed class TwoPlySearchEvaluator
    {
        private readonly IBoardService _boardService;
        private readonly Func<IBoardModel, bool, double> _scorePosition;

        public TwoPlySearchEvaluator(
            IBoardService boardService,
            Func<IBoardModel, bool, double> scorePosition)
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _scorePosition = scorePosition ?? throw new ArgumentNullException(nameof(scorePosition));
        }

        public double Evaluate(IBoardModel board, bool isWhite)
        {
            var activePlayerExpectedScore = 0d;
            // we iterate over all dice rolls (36 combinations)
            for (var die1 = 1; die1 <= 6; die1++)
            {
                for (var die2 = die1; die2 <= 6; die2++)
                {
                    // we consider possible double rolls
                    int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];
                    // we apply the proper probability weight to the roll
                    var rollWeight = die1 == die2 ? 1d : 2d;
                    var opponentBestScore = CalculateBestOpponentResponse(board, isWhite, rolls);
                    // opponentBestScore is from the opponent's perspective; convert it to the active player's perspective
                    activePlayerExpectedScore -= rollWeight * opponentBestScore / 36d;
                }
            }

            // higher values are better for the active player and are sorted first by the caller
            return activePlayerExpectedScore;
        }

        private double CalculateBestOpponentResponse(IBoardModel board, bool isWhite, int[] rolls)
        {
            // score each reply from the opponent's perspective
            var opponentIsWhite = !isWhite;
            var opponentMoveSequences = _boardService.GetUniqueLegalMoveSequences(board, opponentIsWhite, rolls);
            if (opponentMoveSequences.Length == 0)
            {
                return _scorePosition(board, opponentIsWhite);
            }

            var bestOpponentScore = double.NegativeInfinity;
            // scores are from the opponent's perspective, so the opponent maximizes them
            foreach (var opponentMoveSequence in opponentMoveSequences)
            {
                var appliedMoves = 0;
                try
                {
                    foreach (var move in opponentMoveSequence.Moves)
                    {
                        _boardService.MoveCheckerTo(board, move.From, move.To, opponentIsWhite);
                        appliedMoves++;
                    }

                    var opponentScore = _scorePosition(board, opponentIsWhite);
                    bestOpponentScore = Math.Max(bestOpponentScore, opponentScore);
                }
                finally
                {
                    for (var moveIndex = appliedMoves - 1; moveIndex >= 0; moveIndex--)
                    {
                        _boardService.UndoMove(board, opponentMoveSequence.Moves[moveIndex], opponentIsWhite);
                    }
                }
            }

            return bestOpponentScore;
        }
    }
}