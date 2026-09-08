using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides capabilities to evaluate board states.
    /// </summary>
    public interface IFeatureEvalService
    {
        /// <summary>
        /// Evaluates the cube decision for the match state in <paramref name="contract"/>.
        /// </summary>
        /// <param name="contract">Match state to evaluate</param>
        /// <returns>Cube actions to take based on the evaluation.</returns>
        Task<(CubeAction ShouldOffer, CubeAction ShouldTake)> EvalCubeAsync(EvalCubeRequestContract contract);

        /// <summary>
        /// Calculates a rating for the board state in <paramref name="contract"/> based on weights in <paramref name="contactWeights"/>.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="contract">Contains board state .</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <returns>Score rating of the given board for a given player.</returns>
        Task<double> EvalBoardStateAsync(
            EvalBoardRequestContract contract,
            ContactWeightModel contactWeights);

        /// <summary>
        /// Calculates the best rated move sequence for the board and roll in <paramref name="contract"/> based on
        /// weights in <paramref name="contactWeights"/>.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="contract">Contains board state and rolls.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="maxCandidates">Maximum number of candidates to fully evaluate. If null, full sample is evaluated.</param>
        /// <returns>Best rated move sequence.</returns>
        Task<MoveSequenceModel> EvalMoveSequencesAsync(
            EvalMoveRequestContract contract,
            ContactWeightModel contactWeights,
            int? maxCandidates = null);

        /// <summary>
        /// Evaluates all legal move sequences without the cheap pre-filter.
        /// Intended for self-play training data collection only, slower but unbiased.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="contract">Contains board state and rolls.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="maxCandidates">Maximum number of candidates to fully evaluate. If null, full sample is evaluated.</param>
        /// <returns>All rated moves sorted descending by their eval score.</returns>
        Task<FinalEvalResultModels> EvalMoveSequencesForTrainingAsync(
            EvalMoveRequestContract contract,
            ContactWeightModel contactWeights,
            int? maxCandidates = null);

        /// <summary>
        /// Evaluates an explicit set of move candidates at the requested search level.
        /// </summary>
        /// <param name="contract">The board state before any candidate move is applied.</param>
        /// <param name="isWhite">Indicates whether the player making the moves is white.</param>
        /// <param name="candidates">The move candidates to evaluate.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="searchLevel">The search level to use for every candidate.</param>
        /// <returns>The evaluated candidates sorted descending by score.</returns>
        Task<FinalEvalResultModels> EvalMoveSequenceCandidatesAsync(
            BoardModelContract contract,
            bool isWhite,
            IReadOnlyList<MoveSequenceModel> candidates,
            ContactWeightModel contactWeights,
            BotLevel searchLevel);

        /// <summary>
        /// Calculates the normalized position values for a turn without requiring a legal move.
        /// Used to preserve pass turns in training trajectories.
        /// </summary>
        NormalizedEvalResultModel EvalPositionForTraining(BoardModelContract board, bool isWhite);

        /// <summary>
        /// Evaluates the given <param name="contract"></param> with a predefined <param name="moveSequence"></param> and
        /// calculates the eval result for the final board state after applying the move sequence.
        /// </summary>
        /// <remarks>
        /// Linear contact weights are ignored if a neural net service is injected.
        /// </remarks>
        /// <param name="contract">Contains the board state.</param>
        /// <param name="isWhite">Indicates if the player is white.</param>
        /// <param name="moveSequence">The sequence of moves to evaluate.</param>
        /// <param name="botLevel">The search level to use for the evaluation.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <returns>The final eval result for the given <paramref name="moveSequence"/></returns>
        Task<FinalEvalResultModel> EvalMoveSequenceAsync(
            BoardModelContract contract,
            bool isWhite,
            MoveSequenceModel moveSequence,
            BotLevel botLevel,
            ContactWeightModel contactWeights);
    }
}
