using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Services;

internal readonly record struct PreparedPosition(float[]? Features, double TerminalScore = 0);

internal sealed partial class TwoPlySearchEvaluator
{
    /// <summary>
    /// Evaluates the prepared positions asynchronously.
    /// </summary>
    /// <param name="board">The board to evaluate.</param>
    /// <param name="isWhite">Indicates if the player is white.</param>
    /// <param name="prepare">The function to prepare the position.</param>
    /// <param name="scoreAsync">The function to score the positions asynchronously.</param>
    /// <returns>The evaluated score.</returns>
    /// <exception cref="InvalidDataException">Thrown when the input data is invalid.</exception>
    internal async Task<double> EvaluatePreparedAsync(
        IBoardModel board, bool isWhite,
        Func<IBoardModel, bool, PreparedPosition> prepare,
        Func<IReadOnlyList<PreparedPosition>, Task<double[]>> scoreAsync)
    {
        var opponentIsWhite = !isWhite;
        var bestByRoll = new double[21];
        Array.Fill(bestByRoll, double.NegativeInfinity);
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
                    var sequences = _boardService.GetUniqueLegalMoveSequences(board, opponentIsWhite, rolls);
                    
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

        var expected = 0d;
        rollIndex = 0;
        for (var die1 = 1; die1 <= 6; die1++)
        {
            for (var die2 = die1; die2 <= 6; die2++)
            {
                expected -= (die1 == die2 ? 1d : 2d) * bestByRoll[rollIndex++] / 36d;
            }
        }

        return expected;

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

            var scores = await scoreAsync(positions);
            if (scores.Length != positions.Count)
            {
                throw new InvalidDataException("Batch scoring must return one score per reply or pass.");
            }

            for (var index = 0; index < scores.Length; index++)
            {
                var roll = rollIndices[index];
                bestByRoll[roll] = Math.Max(bestByRoll[roll], scores[index]);
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
