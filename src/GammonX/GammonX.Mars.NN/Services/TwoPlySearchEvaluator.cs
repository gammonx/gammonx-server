using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;

using GammonX.Engine.Services;

namespace GammonX.Mars.NN.Services
{
    internal sealed partial class TwoPlySearchEvaluator
    {
        private readonly IBoardService _boardService;
        private readonly Func<IBoardModel, IBoardModel> _cloneBoard;
        private readonly Func<IReadOnlyList<IBoardModel>, bool, Task<EvalResult[]>> _evalPositionsAsync;
        private readonly int _maxBatchSize;
        // if set to true, each inference batch is filled and may contain replies from multiple rolls; if false, each batch is flushed after each roll.
        private readonly bool _batchAcrossRolls;

        public TwoPlySearchEvaluator(
            IBoardService boardService,
            Func<IReadOnlyList<IBoardModel>, bool, Task<EvalResult[]>> evalPositions,
            Func<IBoardModel, IBoardModel>? cloneBoard = null,
            int maxBatchSize = 32,
            bool batchAcrossRolls = false)
        {
            _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            _evalPositionsAsync = evalPositions ?? throw new ArgumentNullException(nameof(evalPositions));
            _cloneBoard = cloneBoard ?? (board => board.DeepClone());
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBatchSize);
            _maxBatchSize = maxBatchSize;
            _batchAcrossRolls = batchAcrossRolls;
        }

        public async Task<EvalResult> EvaluateAsync(IBoardModel board, bool isWhite)
        {
            var opponentIsWhite = !isWhite;
            var bestByRoll = new EvalResult[21];

            Array.Fill(bestByRoll, new EvalResult(double.NegativeInfinity, null));
            var boards = new List<IBoardModel>(_maxBatchSize);
            var pending = new PendingReply[_maxBatchSize];
            var rollIndex = 0;

            try
            {
                // we fill up the batch (pending scoring) and as soon as it is full
                // we restore the pending queue and prepare the next batch
                for (var die1 = 1; die1 <= 6; die1++)
                {
                    for (var die2 = die1; die2 <= 6; die2++, rollIndex++)
                    {
                        // we account for pasch rolls
                        int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];

                        // we get all legal move sequences for the current roll, and apply them to clones of the original board
                        var sequences = _boardService.GetUniqueLegalMoveSequences(board, opponentIsWhite, rolls);
                        if (sequences.Length == 0)
                        {
                            // A pass contributes one row for this roll, even when adjacent rolls also pass.
                            // Generation restores the root before scoring, and never runs during an await.
                            pending[boards.Count] = new PendingReply { RollIndex = rollIndex };
                            boards.Add(board);

                            if (boards.Count == _maxBatchSize)
                            {
                                await ScorePendingAsync();
                            }
                        }
                        else
                        {
                            foreach (var sequence in sequences)
                            {
                                IBoardModel reply;
                                using (EvaluationMetrics.Measure(EvaluationMetrics.ReplyClone))
                                {
                                    reply = _cloneBoard(board);
                                }

                                var index = boards.Count;
                                pending[index] = new PendingReply { RollIndex = rollIndex, Sequence = sequence };
                                boards.Add(reply);
                                EvaluationMetrics.RecordReplyBoards(1);

                                foreach (var move in sequence.Moves)
                                {
                                    _boardService.MoveCheckerTo(reply, move.From, move.To, opponentIsWhite);
                                    pending[index].AppliedMoves++;
                                }

                                // One bounded group is awaited before admitting any more replies.
                                if (boards.Count == _maxBatchSize)
                                {
                                    await ScorePendingAsync();
                                }
                            }
                        }

                        // if disabled we score the pending board now
                        // if enable we will fill the boards array until the max batch size is reached
                        if (!_batchAcrossRolls)
                        {
                            await ScorePendingAsync();
                        }
                    }
                }

                await ScorePendingAsync();
            }
            finally
            {
                // Also restores a partial group if generation, cloning, moving or scoring fails.
                RestorePending();
            }

            return AverageBestReplies(bestByRoll);

            async Task ScorePendingAsync()
            {
                if (boards.Count == 0)
                {
                    return;
                }

                var scores = await _evalPositionsAsync(boards, opponentIsWhite);

                if (scores.Length != boards.Count)
                {
                    throw new InvalidDataException("Batch scoring must return one score per reply or pass.");
                }

                for (var index = 0; index < scores.Length; index++)
                {
                    var roll = pending[index].RollIndex;
                    if (scores[index].Equity > bestByRoll[roll].Equity)
                    {
                        bestByRoll[roll] = scores[index];
                    }
                }

                RestorePending();
            }

            void RestorePending()
            {
                var retained = 0;
                for (var index = 0; index < boards.Count; index++)
                {
                    if (pending[index].Sequence != null)
                    {
                        retained++;
                    }
                }

                try
                {
                    for (var index = 0; index < boards.Count; index++)
                    {
                        for (var moveIndex = pending[index].AppliedMoves - 1; moveIndex >= 0; moveIndex--)
                        {
                            _boardService.UndoMove(boards[index], pending[index].Sequence!.Moves[moveIndex], opponentIsWhite);
                        }
                    }
                }
                finally
                {
                    Array.Clear(pending, 0, boards.Count);
                    boards.Clear();
                    EvaluationMetrics.RecordReplyBoards(-retained);
                }
            }
        }

        private static EvalResult AverageBestReplies(EvalResult[] bestByRoll)
        {
            // we keep equity arithmetic unchanged and average only the selected replys distribution.
            var expectedEquity = 0d;
            // TODO: Plakoto and Fevga 5 headoutput
            var headCount = bestByRoll.Any(reply => reply.Prediction is { Length: 1 }) ? 1 : 5;
            
            var expectedPrediction = bestByRoll.All(reply => reply.Prediction != null)
                ? new double[headCount]
                : null;
            
            var rollIndex = 0;
            for (var die1 = 1; die1 <= 6; die1++)
            {
                for (var die2 = die1; die2 <= 6; die2++)
                {
                    var reply = bestByRoll[rollIndex++];
                    var weight = die1 == die2 ? 1d : 2d;
                    expectedEquity -= weight * reply.Equity / 36d;
                    
                    if (expectedPrediction == null)
                        continue;

                    var prediction = reply.Prediction!;
                    expectedPrediction[0] += weight * (1d - prediction[0]) / 36d;
                    if (headCount == 5)
                    {
                        // we invert the prediction for the players perspective
                        expectedPrediction[1] += weight * prediction[3] / 36d;
                        expectedPrediction[2] += weight * prediction[4] / 36d;
                        expectedPrediction[3] += weight * prediction[1] / 36d;
                        expectedPrediction[4] += weight * prediction[2] / 36d;
                    }
                }
            }

            return new EvalResult(
                expectedEquity,
                expectedPrediction == null 
                    ? null 
                    : Array.ConvertAll(expectedPrediction, value => (float)value));
        }

        private struct PendingReply
        {
            public MoveSequenceModel? Sequence;
            public int RollIndex;
            public int AppliedMoves;
        }
    }
}
