using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    public static class EvalScoreCalculator
    {
        /// <summary>
        /// Calculates a weighted score based of mixed-direction features.
        /// </summary>
        /// <remarks>
        /// The score acts as a relative ranking metric, not an absolute position quality. It is only meaningful
        /// when comparing two positions against each other.
        /// </remarks>
        /// <param name="eval">Absolute feature eval scores.</param>
        /// <param name="contactWeights">Contact position weights.</param>
        /// <param name="modus">Game mode selecting normalization bounds.</param>
        /// <returns>A weighted score representing the relative quality of the position.</returns>
        public static double CalculateScore(EvalResultModel eval, ContactWeightModel contactWeights, GameModus modus)
        {
            var normalizedResult = NormalizedEvalResultModel.From(eval, modus);
            var score = ScoreBlots(normalizedResult, contactWeights, modus);

            // good for the player
            score += normalizedResult.HitOpponentProbability1 * contactWeights.HitOpponentProbability1Weight;
            score += normalizedResult.HitOpponentProbability2 * contactWeights.HitOpponentProbability2Weight;
            score += SignedPipAdvantage(normalizedResult) * contactWeights.PipDifferenceWeight;
            score += normalizedResult.PipToBearOffOpp * contactWeights.PipToBearOffOppWeight;
            score += normalizedResult.NumChFrontLastPin * contactWeights.NumChFrontLastPinWeight;
            score += normalizedResult.EscapeProbability1 * contactWeights.EscapeProbability1Weight;
            score += normalizedResult.EscapeProbability2 * contactWeights.EscapeProbability2Weight;
            score += normalizedResult.PinCountOpp * contactWeights.PinCountOppWeight;
            score += normalizedResult.OppMotherPinned * contactWeights.OppMotherPinnedWeight;
            score += normalizedResult.AnchorCount * contactWeights.AnchorCountWeight;
            score += normalizedResult.MaxPrimeLengthPlayer * contactWeights.MaxPrimeLengthPlayerWeight;
            score += normalizedResult.PrimeProbabilityPlayer * contactWeights.PrimeProbabilityPlayerWeight;
            score += normalizedResult.AnchorCountInFrontPlayer * contactWeights.AnchorCountInFrontPlayerWeight;
            score += normalizedResult.AverageStackHeightOpp * contactWeights.AverageStackHeightOppWeight;
            score += normalizedResult.AverageDistanceToBearOffOpp * contactWeights.AverageDistanceToBearOffOppWeight;
            score += normalizedResult.AverageGapSizeOpp * contactWeights.AverageGapSizeOppWeight;
            score += normalizedResult.CheckersInPrimeZonePlayer * contactWeights.CheckersInPrimeZonePlayerWeight;
            score += normalizedResult.MotherDistanceOpp * contactWeights.MotherDistanceOppWeight;
            score += normalizedResult.BlotInStartRangeCountOpp * contactWeights.BlotInStartRangeCountOppWeight;
            score += normalizedResult.HomebarCountOpp * contactWeights.HomebarCountOppWeight;
            // bad for the player
            score -= normalizedResult.AnchorCountOpp * contactWeights.AnchorCountOppWeight;
            score -= normalizedResult.BlotInStartRangeCount * contactWeights.BlotInStartRangeCountWeight;
            score -= normalizedResult.HitProbability1 * contactWeights.HitProbability1Weight;
            score -= normalizedResult.HitProbability2 * contactWeights.HitProbability2Weight;
            score -= normalizedResult.PipToBearOff * contactWeights.PipToBearOffWeight;
            score -= normalizedResult.NumChFrontLastPinOpp * contactWeights.NumChFrontLastPinOppWeight;
            score -= normalizedResult.EscapeProbability1Opp * contactWeights.EscapeProbability1OppWeight;
            score -= normalizedResult.EscapeProbability2Opp * contactWeights.EscapeProbability2OppWeight;
            score -= normalizedResult.PinCountPlayer * contactWeights.PinCountPlayerWeight;
            score -= normalizedResult.PlayerMotherPinned * contactWeights.PlayerMotherPinnedWeight;
            score -= normalizedResult.MaxPrimeLengthOpp * contactWeights.MaxPrimeLengthOppWeight;
            score -= normalizedResult.HomebarCountPlayer * contactWeights.HomebarCountPlayerWeight;
            score -= normalizedResult.PrimeProbabilityOpp * contactWeights.PrimeProbabilityOppWeight;
            score -= normalizedResult.AnchorCountInFrontOpp * contactWeights.AnchorCountInFrontOppWeight;
            score -= normalizedResult.AverageStackHeightPlayer * contactWeights.AverageStackHeightPlayerWeight;
            score -= normalizedResult.AverageDistanceToBearOffPlayer * contactWeights.AverageDistanceToBearOffPlayerWeight;
            score -= normalizedResult.AverageGapSizePlayer * contactWeights.AverageGapSizePlayerWeight;
            score -= normalizedResult.CheckersInPrimeZoneOpp * contactWeights.CheckersInPrimeZoneOppWeight;
            score -= normalizedResult.MotherDistancePlayer * contactWeights.MotherDistancePlayerWeight;

            return score;
        }

        /// <summary>
        /// Calculates a (cheap) weighted score based of mixed-direction features. The cheap score only includes
        /// a subset of features which are less expensive to calculate.
        /// </summary>
        /// <remarks>
        /// The score acts as a relative ranking metric, not an absolute position quality. It is only meaningful
        /// when comparing two positions against each other.
        /// </remarks>
        /// <param name="eval">Absolute feature eval scores.</param>
        /// <param name="contactWeights">(Cheap) Contact position weights.</param>
        /// <param name="raceWeights">Race position weights.</param>
        /// <param name="modus">Game mode selecting normalization bounds.</param>
        /// <returns>A (cheap) weighted score representing the relative quality of the position.</returns>
        public static (NormalizedEvalResultModel, double) CalculateCheapScore(
            EvalResultModel eval,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            GameModus modus)
        {
            var normalizedResult = NormalizedEvalResultModel.From(eval, modus);
            var score = 0.0;

            // we do not account for the turnnumber because it is kind of neutral

            // we exclude contact based features in race positions, as they are not relevant and can be misleading
            if (eval.Race)
            {
                // good for the player
                score += SignedPipAdvantage(normalizedResult) * raceWeights.PipDifferenceWeight;
                score += normalizedResult.PipToBearOffOpp * raceWeights.PipToBearOffOppWeight;
                // bad for the player
                score -= normalizedResult.PipToBearOff * raceWeights.PipToBearOffWeight;
            }
            else
            {
                score += ScoreBlots(normalizedResult, contactWeights, modus);
                // good for the player
                score += SignedPipAdvantage(normalizedResult) * contactWeights.PipDifferenceWeight;
                score += normalizedResult.PipToBearOffOpp * contactWeights.PipToBearOffOppWeight;
                score += normalizedResult.PinCountOpp * contactWeights.PinCountOppWeight;
                score += normalizedResult.AnchorCount * contactWeights.AnchorCountWeight;
                score += normalizedResult.MaxPrimeLengthPlayer * contactWeights.MaxPrimeLengthPlayerWeight;
                score += normalizedResult.AnchorCountInFrontPlayer * contactWeights.AnchorCountInFrontPlayerWeight;
                score += normalizedResult.AverageStackHeightOpp * contactWeights.AverageStackHeightOppWeight;
                score += normalizedResult.AverageDistanceToBearOffOpp * contactWeights.AverageDistanceToBearOffOppWeight;
                score += normalizedResult.AverageGapSizeOpp * contactWeights.AverageGapSizeOppWeight;
                score += normalizedResult.CheckersInPrimeZonePlayer * contactWeights.CheckersInPrimeZonePlayerWeight;
                score += normalizedResult.BlotInStartRangeCountOpp * contactWeights.BlotInStartRangeCountOppWeight;
                score += normalizedResult.HomebarCountOpp * contactWeights.HomebarCountOppWeight;
                // bad for the player
                score -= normalizedResult.AnchorCountOpp * contactWeights.AnchorCountOppWeight;
                score -= normalizedResult.BlotInStartRangeCount * contactWeights.BlotInStartRangeCountWeight;
                score -= normalizedResult.PipToBearOff * contactWeights.PipToBearOffWeight;
                score -= normalizedResult.PinCountPlayer * contactWeights.PinCountPlayerWeight;
                score -= normalizedResult.MaxPrimeLengthOpp * contactWeights.MaxPrimeLengthOppWeight;
                score -= normalizedResult.HomebarCountPlayer * contactWeights.HomebarCountPlayerWeight;
                score -= normalizedResult.AnchorCountInFrontOpp * contactWeights.AnchorCountInFrontOppWeight;
                score -= normalizedResult.AverageStackHeightPlayer * contactWeights.AverageStackHeightPlayerWeight;
                score -= normalizedResult.AverageDistanceToBearOffPlayer * contactWeights.AverageDistanceToBearOffPlayerWeight;
                score -= normalizedResult.AverageGapSizePlayer * contactWeights.AverageGapSizePlayerWeight;
                score -= normalizedResult.CheckersInPrimeZoneOpp * contactWeights.CheckersInPrimeZoneOppWeight;
            }

            return new ValueTuple<NormalizedEvalResultModel, double>(normalizedResult, score);
        }

        // Neural inputs use [0, 1] with equality at 0.5. Heuristic weights multiply
        // signed advantage in [-1, 1], so equal pips contribute zero.
        private static double SignedPipAdvantage(NormalizedEvalResultModel model) =>
            2 * model.PipDifference - 1;

        private static double ScoreBlots(NormalizedEvalResultModel model, ContactWeightModel weights, GameModus modus)
        {
            var ownMinusOpponent = model.BlotCount * weights.BlotCountWeight
                - model.BlotCountOpp * weights.BlotCountOppWeight;
            // Fevga singletons block points; in hitting/pinning modes they are exposed checkers.
            return modus == GameModus.Fevga ? ownMinusOpponent : -ownMinusOpponent;
        }
    }
}
