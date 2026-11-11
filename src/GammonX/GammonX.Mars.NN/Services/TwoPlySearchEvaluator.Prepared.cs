using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Services;

internal readonly record struct PreparedPosition(float[]? Features, EvalResult? Terminal = null);

internal sealed partial class TwoPlySearchEvaluator
{
    /// <summary>
    /// Evaluates the prepared positions asynchronously.
    /// </summary>
    /// <param name="board">The board to evaluate.</param>
    /// <param name="isWhite">Indicates if the player is white.</param>
    /// <param name="prepare">The function to prepare the position.</param>
    /// <param name="scoreAsync">The function to score the positions asynchronously.</param>
    /// <param name="cancellationToken">Cancels traversal and scoring without returning an incomplete expected score.</param>
    /// <returns>The evaluated score.</returns>
    /// <exception cref="InvalidDataException">Thrown when the input data is invalid.</exception>
    internal async Task<EvalResult> EvalPreparedAsync(
        IBoardModel board, bool isWhite,
        Func<IBoardModel, bool, PreparedPosition> prepare,
        Func<IReadOnlyList<PreparedPosition>, Task<EvalResult[]>> scoreAsync,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var opponentIsWhite = !isWhite;
        var bestByRoll = new EvalResult[21];
        Array.Fill(bestByRoll, new EvalResult(double.NegativeInfinity, null));
        var positions = new List<PreparedPosition>(_maxBatchSize);
        var rollIndices = new int[_maxBatchSize];
        IBoardModel? working = null;
        var retainedFeatures = 0;
        var rollIndex = 0;

        try
        {
            // we iterate over all 21 unique rolls
            for (var die1 = 1; die1 <= 6; die1++)
            {
                for (var die2 = die1; die2 <= 6; die2++, rollIndex++)
                {
                    int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];
                    var sequences = _boardService.GetUniqueLegalMoveSequences(board, opponentIsWhite, rolls, cancellationToken);

                    // we extract the features directly from the board if no legal moves are available, which is the case for a pass
                    if (sequences.Length == 0)
                    {
                        Add(prepare(board, opponentIsWhite));
                        if (positions.Count == _maxBatchSize)
                        {
                            await FlushAsync();
                        }
                    }
                    else
                    {
                        if (working == null)
                        {
                            using (EvaluationMetrics.Measure(EvaluationMetrics.ReplyClone))
                            {
                                working = _cloneBoard(board);
                            }
                            EvaluationMetrics.RecordReplyBoards(1);
                        }

                        foreach (var sequence in sequences)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var applied = 0;
                            try
                            {
                                // we first execute the given move sequence
                                foreach (var move in sequence.Moves)
                                {
                                    _boardService.MoveCheckerTo(working, move.From, move.To, opponentIsWhite);
                                    applied++;
                                }
                                // we prepare the feature vectors
                                Add(prepare(working, opponentIsWhite));
                            }
                            finally
                            {
                                // we undo the moves on the working board and restore it to the original state
                                for (var index = applied - 1; index >= 0; index--)
                                {
                                    _boardService.UndoMove(working, sequence.Moves[index], opponentIsWhite);
                                }
                            }

                            // we restore before awaiting; only independent feature rows reach inference.
                            if (positions.Count == _maxBatchSize)
                            {
                                await FlushAsync();
                            }
                        }
                    }

                    if (!_batchAcrossRolls)
                    {
                        await FlushAsync();
                    }
                }
            }

            await FlushAsync();
        }
        finally
        {
            ClearPending();
            if (working != null)
            {
                EvaluationMetrics.RecordReplyBoards(-1);
            }
        }

        return AverageBestReplies(bestByRoll);

        void Add(PreparedPosition position)
        {
            rollIndices[positions.Count] = rollIndex;
            positions.Add(position);
            if (position.Features != null)
            {
                retainedFeatures++;
                EvaluationMetrics.RecordFeatureRows(1);
            }
        }

        async Task FlushAsync()
        {
            if (positions.Count == 0)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var scores = await scoreAsync(positions);

            cancellationToken.ThrowIfCancellationRequested();

            if (scores.Length != positions.Count)
            {
                throw new InvalidDataException("Batch scoring must return one score per reply or pass.");
            }

            for (var index = 0; index < scores.Length; index++)
            {
                var roll = rollIndices[index];
                if (scores[index].Equity > bestByRoll[roll].Equity)
                {
                    bestByRoll[roll] = scores[index];
                }
            }

            ClearPending();
        }

        void ClearPending()
        {
            positions.Clear();
            EvaluationMetrics.RecordFeatureRows(-retainedFeatures);
            retainedFeatures = 0;
        }
    }
}
