using GammonX.Engine.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Represents an ordered list of evaluated move sequences.
    /// </summary>
    /// <seealso cref="List{T}"/>
    /// <seealso cref="FinalEvalResultModel"/>
    public class FinalEvalResultModels : List<FinalEvalResultModel>
    {
        public FinalEvalResultModels() : base() { }
            
        public FinalEvalResultModels(IEnumerable<FinalEvalResultModel> collection) : base(collection) { }

        /// <summary>
        /// Select a move sequence based on the given <paramref name="botLevel"/>.
        /// </summary>
        /// <param name="botLevel">Selection criteria.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If bot level is unknown.</exception>
        public MoveSequenceModel Select(BotLevel botLevel)
        {
            if (botLevel == BotLevel.Easy)
            {
                if (this.Count > 0)
                {
                    var halfCount = (int)Math.Round((double)this.Count / 2, 0, MidpointRounding.ToPositiveInfinity);
                    if (Count > 0)
                    {
                        // we want to return a random move from the worst half (third and fourth quartile)
                        var moveSeqIndex = Random.Shared.Next(halfCount - 1, this.Count - 1);
                        return this[moveSeqIndex]?.MoveSequence ?? new MoveSequenceModel();
                    }
                }
                return new MoveSequenceModel();
            }
            else if (botLevel == BotLevel.Medium)
            {
                var quarterCount = (int)Math.Round((double)this.Count / 4, 0, MidpointRounding.ToPositiveInfinity);
                if (quarterCount > 0)
                {
                    // we want to return a random move from the better half (first and second quartile)
                    var startIndex = 0;
                    var endIndex = Math.Min(3 * quarterCount, this.Count);
                    var moveSeqIndex = Random.Shared.Next(startIndex, endIndex - 1);
                    return this[moveSeqIndex]?.MoveSequence ?? new MoveSequenceModel();
                }
                return new MoveSequenceModel();
            }
            else if (botLevel == BotLevel.Hard)
            {
                // we always return the best move possible
                return this.FirstOrDefault()?.MoveSequence ?? new MoveSequenceModel();
            }

            throw new InvalidOperationException("The bot level is not supported or unknown.");
        }
    }

    public class FinalEvalResultModel
    {
        /// <summary>
        /// Gets the evaluation score of the move sequence.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the evaluated move sequence.
        /// </summary>
        public MoveSequenceModel MoveSequence { get; }

        /// <summary>
        /// Gets the eval result model which were the basis for calculating the evaluation score.
        /// </summary>
        public NormalizedEvalResultModel EvalResult { get; }

        public FinalEvalResultModel(double score, MoveSequenceModel moveSequence, NormalizedEvalResultModel evalResult)
        {
            Score = score;
            MoveSequence = moveSequence;
            EvalResult = evalResult;
        }    
    }
}
