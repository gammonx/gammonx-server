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
        /// <returns>Cube action to take based on the evaluation.</returns>
        CubeAction EvalCube(EvalCubeRequestContract contract);

        /// <summary>
        /// Calculates a rating for the board state in <paramref name="contract"/> based on weights in
        /// <paramref name="contactWeights"/> and <paramref name="raceWeights"/>.
        /// </summary>
        /// <param name="contract">Contains board state .</param>
        /// <param name="cheapContactWeight">Cheap contact position weights to prefilter.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="raceWeights">Race position weights.</param>
        /// <returns>Score rating of the given board for a given player.</returns>
        double EvalBoardState(
            EvalBoardRequestContract contract,
            ContactWeightModel cheapContactWeight,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights);

        /// <summary>
        /// Calculates the best rated move sequence for the board and roll in <paramref name="contract"/> based on
        /// weights in <paramref name="contactWeights"/> and <paramref name="raceWeights"/>.
        /// </summary>
        /// <param name="contract">Contains board state and rolls.</param>
        /// <param name="cheapContactWeight">Cheap contact position weights to prefilter.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="raceWeights">Race position weights.</param>
        /// <param name="maxCandidates">Maximum number of candidates to fully evaluate.</param>
        /// <returns>Best rated move sequence.</returns>
        MoveSequenceModel EvalMoveSequence(
            EvalMoveRequestContract contract,
            ContactWeightModel cheapContactWeight,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            int maxCandidates);

        /// <summary>
        /// Evaluates all legal move sequences without the cheap pre-filter.
        /// Intended for self-play training data collection only, slower but unbiased.
        /// </summary>
        /// <param name="contract">Contains board state and rolls.</param>
        /// <param name="cheapContactWeight">Cheap contact position weights to prefilter.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="raceWeights">Race position weights.</param>
        /// <param name="maxCandidates">Maximum number of candidates to fully evaluate.</param>
        /// <returns>All rated moves sorted descending by their eval score.</returns>
        FinalEvalResult[] EvalMoveSequenceForTraining(
            EvalMoveRequestContract contract,
            ContactWeightModel cheapContactWeight,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            int maxCandidates);
    }

    /// <summary>
    /// Provides the final evaluation results for all move explored.
    /// </summary>
    /// <param name="Score">Evaluated score.</param>
    /// <param name="Move">Evaluated move.</param>
    /// <param name="EvalResult">Normalized evaluation result.</param>
    public record FinalEvalResult(
        double Score,
        MoveSequenceModel Move,
        NormalizedEvalResultModel EvalResult);

    /// <summary>
    /// Provides the cheap evaluation results for a list of legal move sequences.
    /// </summary>
    /// <param name="CheapScore">Calculated cheap score.</param>
    /// <param name="Index">Index of the move sequence.</param>
    /// <param name="IsRace">Indicates if the move is a race move.</param>
    /// <param name="EvalResult">Normalized evaluation result.</param>
    public record CheapEvalResult(
        double CheapScore,
        int Index,
        bool IsRace,
        NormalizedEvalResultModel EvalResult)
    {
        /// <summary>
        /// Sorts by <see cref="CheapScore"/> descending — highest score first.
        /// </summary>
        public sealed class DescendingComparer : IComparer<CheapEvalResult>
        {
            public static readonly DescendingComparer Instance = new();

            private DescendingComparer() 
            {
                // pass
            }

            // <inheritdoc />
            public int Compare(CheapEvalResult? x, CheapEvalResult? y) => y!.CheapScore.CompareTo(x!.CheapScore);
        }
    }
}
