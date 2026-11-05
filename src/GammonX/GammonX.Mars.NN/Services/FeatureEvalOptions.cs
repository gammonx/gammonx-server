using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    public class FeatureEvalOptions
    {
        public EvalCubeRequestContract? EvalCubeRequest { get; set; }

        public EvalBoardRequestContract? EvalBoardRequest { get; set; }

        public EvalMoveRequestContract? EvalMoveRequest { get; set; }

        public BoardModelContract? BoardContract { get; set; }

        public bool? IsWhite { get; set; }

        public int? TurnNumber { get; set; }

        /// <summary>
        /// Gets or sets the bot level which influences the evaluation depth.
        /// </summary>
        public BotLevel? BotLevel { get; set; }

        /// <summary>
        /// Gets or sets the candidate move sequence to evaluate.
        /// </summary>
        public MoveSequenceModel? Candidate { get; set; }

        /// <summary>
        /// Gets or sets the list of candidate moves to evaluate.
        /// </summary>
        public IReadOnlyList<MoveSequenceModel>? Candidates { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of candidates to fully evaluate. If null, full sample is evaluated.
        /// </summary>
        public int? MaxCandidates { get; set; }

        /// <summary>
        /// Gets or sets the contact weights models for linear evaluation.
        /// </summary>
        public ContactWeightModel? ContactWeights { get; set; }
    }
}
