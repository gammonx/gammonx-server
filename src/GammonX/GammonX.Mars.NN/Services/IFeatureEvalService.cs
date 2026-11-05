using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides capabilities to evaluate board states.
    /// </summary>
    public interface IFeatureEvalService
    {
        /// <summary>
        /// Evaluates the cube decision for the match state in <see cref="FeatureEvalOptions.EvalCubeRequest"/>.
        /// </summary>
        /// <param name="options">Evaluation options.</param>
        /// <returns>Cube actions to take based on the evaluation.</returns>
        Task<(CubeAction ShouldOffer, CubeAction ShouldTake)> EvalCubeAsync(FeatureEvalOptions options);

        /// <summary>
        /// Calculates a rating for the board state in <see cref="FeatureEvalOptions.EvalBoardRequest"/> based on weights in <see cref="FeatureEvalOptions.ContactWeights"/>.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="options">Evaluation options.</param>
        /// <returns>Equity and available outcome predictions for the given player.</returns>
        Task<EvalResult> EvalBoardStateAsync(FeatureEvalOptions options);

        /// <summary>
        /// Calculates the best rated move sequence for the board and roll in <see cref="FeatureEvalOptions.EvalMoveRequest"/> based on
        /// weights in <see cref="FeatureEvalOptions.ContactWeights"/>.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="options">Evaluation options.</param>
        /// <returns>Best rated move sequence.</returns>
        Task<MoveSequenceModel> EvalMoveSequencesAsync(FeatureEvalOptions options);

        /// <summary>
        /// Evaluates all legal move sequences without the cheap pre-filter.
        /// Intended for self-play training data collection only, slower but unbiased.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="options">Evaluation options.</param>
        /// <returns>All rated moves sorted descending by their eval score.</returns>
        Task<FinalEvalResultModels> EvalMoveSequencesForTrainingAsync(FeatureEvalOptions options);

        /// <summary>
        /// Evaluates an explicit set of move candidates at the requested search level.
        /// </summary>
        /// <param name="options">Evaluation options.</param>
        /// <returns>The evaluated candidates sorted descending by score.</returns>
        Task<FinalEvalResultModels> EvalMoveSequenceCandidatesAsync(FeatureEvalOptions options);

        /// <summary>
        /// Calculates the normalized position values for a turn without requiring a legal move.
        /// Used to preserve pass turns in training trajectories.
        /// </summary>
        /// <param name="options">Evaluation options.</param>
        /// <returns>The normalized position values for the given board state.</returns>
        NormalizedEvalResultModel EvalPositionForTraining(FeatureEvalOptions options);

        /// <summary>
        /// Evaluates <see cref="FeatureEvalOptions.BoardContract"/> with the predefined <see cref="FeatureEvalOptions.Candidate"/> and
        /// calculates the eval result for the final board state after applying the move sequence.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="options">Evaluation options.</param>
        /// <returns>The final eval result for the given <see cref="FeatureEvalOptions.Candidate"/>.</returns>
        Task<FinalEvalResultModel> EvalMoveSequenceAsync(FeatureEvalOptions options);
    }
}
