using GammonX.Engine.Models;
using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server.Services
{
    /// <summary>
    /// Provides capabilities to evaluate board states.
    /// </summary>
    public interface IFeatureEvalService
    {
        /// <summary>
        /// Calculates a rating for the board state in <paramref name="contract"/> based on weights in
        /// <paramref name="contactWeights"/> and <paramref name="raceWeights"/>.
        /// </summary>
        /// <param name="contract">Contains board state .</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="raceWeights">Raace position weights.</param>
        /// <returns>Score rating of the given board for a given player.</returns>
        double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights);

        /// <summary>
        /// Calculates the best rated move sequence for the board and roll in <paramref name="contract"/> based on
        /// weights in <paramref name="contactWeights"/> and <paramref name="raceWeights"/>.
        /// </summary>
        /// <param name="contract">Contains board state and rolls.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="raceWeights">Raace position weights.</param>
        /// <returns>Best rated move sequence.</returns>
        MoveSequenceModel EvalMoveSequence(EvalMoveRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights);
    }
}
