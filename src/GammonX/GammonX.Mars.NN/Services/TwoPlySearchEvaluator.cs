using GammonX.Engine.Models;

using GammonX.Engine.Services;

namespace GammonX.Mars.NN.Services
{
    internal sealed class TwoPlySearchEvaluator
    {
        private readonly IBoardService _boardService;
        private readonly Func<IBoardModel, bool, Task<double>> _evalPositionAsync;
        private readonly Func<IBoardModel, IBoardModel> _cloneBoard;

        public TwoPlySearchEvaluator(
            IBoardService boardService,
            Func<IBoardModel, bool, Task<double>> scorePosition,
            Func<IBoardModel, IBoardModel>? cloneBoard = null)
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _evalPositionAsync = scorePosition ?? throw new ArgumentNullException(nameof(scorePosition));
            _cloneBoard = cloneBoard ?? (board => board.DeepClone());
        }

        public async Task<double> EvaluateAsync(IBoardModel board, bool isWhite)
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
                    var opponentBestScore = await CalculateBestOpponentResponseAsync(board, isWhite, rolls);
                    // opponentBestScore is from the opponent's perspective; convert it to the active player's perspective
                    activePlayerExpectedScore -= rollWeight * opponentBestScore / 36d;
                }
            }

            // higher values are better for the active player and are sorted first by the caller
            return activePlayerExpectedScore;
        }

        private async Task<double> CalculateBestOpponentResponseAsync(IBoardModel board, bool isWhite, int[] rolls)
        {
            // score each reply from the opponents perspective
            var opponentIsWhite = !isWhite;
            var opponentMoveSequences = _boardService.GetUniqueLegalMoveSequences(board, opponentIsWhite, rolls);
            if (opponentMoveSequences.Length == 0)
            {
                return await _evalPositionAsync(board, opponentIsWhite);
            }

            var scoreTasks = new List<Task<double>>(opponentMoveSequences.Length);
            foreach (var opponentMoveSequence in opponentMoveSequences)
            {
                var boardCopy = _cloneBoard(board);
                var appliedMoves = 0;
                try
                {
                    foreach (var move in opponentMoveSequence.Moves)
                    {
                        _boardService.MoveCheckerTo(boardCopy, move.From, move.To, opponentIsWhite);
                        appliedMoves++;
                    }

                    scoreTasks.Add(_evalPositionAsync(boardCopy, opponentIsWhite));
                }
                finally
                {
                    for (var moveIndex = appliedMoves - 1; moveIndex >= 0; moveIndex--)
                    {
                        _boardService.UndoMove(boardCopy, opponentMoveSequence.Moves[moveIndex], opponentIsWhite);
                    }
                }
            }

            var opponentScores = await Task.WhenAll(scoreTasks);
            // scores are from the opponents perspective, so the opponent maximizes them
            return opponentScores.Max();
        }
    }
}