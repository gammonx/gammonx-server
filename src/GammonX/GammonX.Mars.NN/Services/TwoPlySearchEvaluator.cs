using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;

using GammonX.Engine.Services;

namespace GammonX.Mars.NN.Services
{
    internal sealed class TwoPlySearchEvaluator
    {
        private readonly IBoardService _boardService;
        private readonly Func<IBoardModel, IBoardModel> _cloneBoard;
        private readonly Func<IReadOnlyList<IBoardModel>, bool, Task<double[]>> _evalPositionsAsync;
        private readonly int _maxBatchSize;

        public TwoPlySearchEvaluator(
            IBoardService boardService,
            Func<IReadOnlyList<IBoardModel>, bool, Task<double[]>> scorePositions,
            Func<IBoardModel, IBoardModel>? cloneBoard = null,
            int maxBatchSize = 32)
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _evalPositionsAsync = scorePositions ?? throw new ArgumentNullException(nameof(scorePositions));
            _cloneBoard = cloneBoard ?? (board => board.DeepClone());
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBatchSize);
            _maxBatchSize = maxBatchSize;
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
                var scores = await _evalPositionsAsync([board], opponentIsWhite);
                if (scores.Length != 1)
                    throw new InvalidDataException("Batch scoring must return one score for a pass.");
                return scores[0];
            }
            return await CalculateBatchedResponsesAsync(board, opponentIsWhite, opponentMoveSequences);
        }

        private async Task<double> CalculateBatchedResponsesAsync(IBoardModel board, bool isWhite, MoveSequenceModel[] sequences)
        {
            var best = double.NegativeInfinity;
            for (var start = 0; start < sequences.Length; start += _maxBatchSize)
            {
                var count = Math.Min(_maxBatchSize, sequences.Length - start);
                var boards = new IBoardModel[count];
                var applied = new int[count];
                try
                {
                    // we iterate through all collected board and execute the moves
                    for (var index = 0; index < count; index++)
                    {
                        using (EvaluationMetrics.Measure(EvaluationMetrics.ReplyClone))
                        {
                            boards[index] = _cloneBoard(board);
                        }

                        foreach (var move in sequences[start + index].Moves)
                        {
                            _boardService.MoveCheckerTo(boards[index], move.From, move.To, isWhite);
                            applied[index]++;
                        }
                    }
                    // we then execute the inference in batch
                    var scores = await _evalPositionsAsync(boards, isWhite);
                    if (scores.Length != count)
                    {
                        throw new InvalidDataException("Batch scoring must return one score per reply.");
                    }
                    // scores are from the opponents perspective, so the opponent maximizes them
                    best = Math.Max(best, scores.Max());
                }
                finally
                {
                    for (var index = 0; index < count; index++)
                    {
                        for (var moveIndex = applied[index] - 1; moveIndex >= 0; moveIndex--)
                        {
                            _boardService.UndoMove(boards[index], sequences[start + index].Moves[moveIndex], isWhite);
                        }
                    }
                }
            }
            return best;
        }
    }
}