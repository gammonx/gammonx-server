using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Active-player measurements normalized to [0, 1] by <see cref="From"/>.
    /// Larger values retain the strategic direction of their raw measurements.
    /// </summary>
    /// <remarks>
    /// Use <see cref="From"/> for production inputs. Direct init assignments and
    /// the default struct do not validate values or evaluate a board position.
    /// </remarks>
    public readonly struct NormalizedEvalResultModel
    {
        public NormalizedEvalResultModel()
        {
            // pass
        }

        /// <summary>
        /// Gets boolean indicating if the position is a no contact (race) position
        /// </summary>
        public bool Race { get; init; }

        /// <summary>
        /// Gets the normalized probability of one player checker being hit on the next roll by opponent.
        /// Range: [0, 1], where 0 means no probability and 1 means certain hit.
        /// </summary>
        public double HitProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of two player checkers being hit on the next roll by opponent.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain hits.
        /// </summary>
        public double HitProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the player pinning at least one opponent checker on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain hit.
        /// </summary>
        public double HitOpponentProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the player pinning at least two opponent checkers on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain hits.
        /// </summary>
        public double HitOpponentProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the amount of blots for the player on the board.
        /// [Range: [0, 1], where 0 means no blots and 1 means all checkers are blots.
        /// </summary>
        public double BlotCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the opponent on the board.
        /// [Range: [0, 1], where 0 means no blots for opponent and 1 means all checkers are blots for opponent.
        /// </summary>
        public double BlotCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the player in the start range.
        /// [Range: [0, 1], where 0 means no blots in start range and 1 means 6 blots in start range.
        /// </summary>
        public double BlotInStartRangeCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the opponent in the start range.
        /// [Range: [0, 1], where 0 means no blots in start range and 1 means 6 blots in start range.
        /// </summary>
        public double BlotInStartRangeCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the player on the board.
        /// Range: [0, 1], where 0 means no made points and 1 means the mode's made-point bound.
        /// </summary>
        public double AnchorCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the opponent on the board.
        /// Range: [0, 1], where 0 means no made points and 1 means the mode's made-point bound.
        /// </summary>
        public double AnchorCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount difference of the player.
        /// Range: [0, 1], where 0 is maximum disadvantage, 0.5 is equal pips, and 1 is maximum advantage.
        /// </summary>
        /// <remarks>
        /// Values above 0.5 mean the player is ahead; values below 0.5 mean the player is behind.
        /// </remarks>
        public double PipDifference { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount to bearoff for the player.
        /// Range: [0, 1], where 0 means all player checkers are home or off.
        /// Includes pins and bar/reserve; normalized by the mode's maximum pips to enter home.
        /// </summary>
        public double PipToBearOff { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount to bearoff for opponent.
        /// Range: [0, 1], where 0 means all opponent checkers are home or off.
        /// Includes pins and bar/reserve; normalized by the mode's maximum pips to enter home.
        /// </summary>
        public double PipToBearOffOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of player checkers in front of the last pinned opponent checker in
        /// Range: [0, 1], where 0 means no checkers in front and 1 means all checkers in front.
        /// the players home board.
        /// </summary>
        public double NumChFrontLastPin { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of opponent checkers in front of the last pinned player checker in
        /// the opponents home board.
        /// Range: [0, 1], where 0 means no checkers in front and 1 means all checkers in front.
        /// </summary>
        public double NumChFrontLastPinOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized probability of at least one player checker escaping from the opponents home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain escape.
        /// </summary>
        public double EscapeProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least two player checkers escaping from the opponents home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain escapes.
        /// </summary>
        public double EscapeProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least one opponent checker escaping from the players home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain escape.
        /// </summary>
        public double EscapeProbability1Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least two opponent checkers escaping from the players home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain escapes.
        /// </summary>
        public double EscapeProbability2Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized number of opponent checkers currently pinned by the player.
        /// Range: [0, 1], where 0 means no pinned checkers and 1 means all opponent checkers are pinned.
        /// </summary>
        public double PinCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of player checkers currently pinned by the opponent.
        /// Range: [0, 1], where 0 means no pinned checkers and 1 means all player checkers are pinned.
        /// </summary>
        public double PinCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating whether the opponents mother checker is currently pinned by the player (0 or 1).
        /// Range: [0, 1], where 0 means the opponents mother checker is not pinned and 1 means it is pinned.
        /// </summary>
        public double OppMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating whether the players mother checker is currently pinned by the opponent (0 or 1).
        /// Range: [0, 1], where 0 means the players mother checker is not pinned and 1 means it is pinned.
        /// </summary>
        public double PlayerMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating the distance the players mother checker is away from home range.
        /// Range: [0, 1], where 0 means the players mother checker is in home range and 1 means it is far away.
        /// </summary>
        public double MotherDistancePlayer { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating the distance the opponents mother checker is away from home range.
        /// Range: [0, 1], where 0 means the opponents mother checker is in home range and 1 means it is far away.
        /// </summary>
        public double MotherDistanceOpp { get; init; } = 0;

        /// <summary>
        /// Gets the nonnegative turn number compressed as t / (t + 100).
        /// Range: [0, 1], 100 is a compression scale, not a maximum game length.
        /// </summary>
        public double TurnNumber { get; init; } = 0;

        #region Fevga Features

        /// <summary>
        /// Gets the normalized length of the longest prime of the player.
        /// Range: [0, 1], scaled by the mode's made-point bound; actual runs are not capped at six.
        /// </summary>
        public double MaxPrimeLengthPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the normalized length of the longest prime of the opponent.
        /// Range: [0, 1], scaled by the mode's made-point bound; actual runs are not capped at six.
        /// </summary>
        public double MaxPrimeLengthOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of checkers of the player on the homebar.
        /// Range: [0, 1], where 0 means no checkers and 1 means all checkers are on the homebar.
        /// </summary>
        public double HomebarCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of checkers of the opponent on the homebar.
        /// Range: [0, 1], where 0 means no checkers and 1 means all checkers are on the homebar.
        /// </summary>
        public double HomebarCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized probability of the player to form a prime in his next turn.
        /// Range: [0, 1], where 0 means no probability and 1 means certain prime formation.
        /// </summary>
        public double PrimeProbabilityPlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the opponent to form a prime in his next turn.
        /// Range: [0, 1], where 0 means no probability and 1 means certain prime formation.
        /// </summary>
        public double PrimeProbabilityOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the opponent.
        /// Range: [0, 1], where 0 means no made points in front; scaled by the mode's made-point bound.
        /// </summary>
        public double AnchorCountInFrontPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the player.
        /// Range: [0, 1], where 0 means no made points in front; scaled by the mode's made-point bound.
        /// </summary>
        public double AnchorCountInFrontOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the player.
        /// Range: [0, 1], where 0 means no visible checkers and 1 means a fifteen-checker stack.
        /// Singleton stacks map to 1/15. Pinned, bar/reserve, and borne-off checkers are excluded.
        /// </summary>
        public double AverageStackHeightPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the opponent.
        /// Range: [0, 1], where 0 means no visible checkers and 1 means a fifteen-checker stack.
        /// Singleton stacks map to 1/15. Pinned, bar/reserve, and borne-off checkers are excluded.
        /// </summary>
        public double AverageStackHeightOpp { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the player.
        /// Range: [0, 1], over remaining visible, pinned, and bar/reserve checkers.
        /// Excludes borne-off checkers from the mean; zero means none remain. Includes the final removal pip.
        /// </summary>
        public double AverageDistanceToBearOffPlayer { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the opponent.
        /// Range: [0, 1], over remaining visible, pinned, and bar/reserve checkers.
        /// Excludes borne-off checkers from the mean; zero means none remain. Includes the final removal pip.
        /// </summary>
        public double AverageDistanceToBearOffOpp { get; init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps associated with the player.
        /// Range: [0, 1], where 0 means player has no gaps and 1 means player has maximum gap size.
        /// </summary>
        public double AverageGapSizePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps for the opponent.
        /// Range: [0, 1], where 0 means opponent has no gaps and 1 means opponent has maximum gap size.
        /// </summary>
        public double AverageGapSizeOpp { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the player has in the prime zone (e.g. mid board).
        /// Range: [0, 1], where 0 means no player checkers in prime zone and 1 means all player checkers in prime zone.
        /// </summary>
        public double CheckersInPrimeZonePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the opponent has in the prime zone (e.g. mid board).
        /// Range: [0, 1], where 0 means no opponent checkers in prime zone and 1 means all opponent checkers in prime zone.
        /// </summary>
        public double CheckersInPrimeZoneOpp { get; init; } = 0.0;

        #endregion Fevga Features

        /// <summary>
        /// Validates raw feature domains and normalizes every numeric measurement to [0, 1].
        /// </summary>
        /// <param name="eval">Raw measurements from the active player's perspective.</param>
        /// <param name="modus">Game mode selecting geometric and made-point bounds.</param>
        /// <returns>Finite normalized measurements, preserving their strategic directions.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The mode or a raw feature value is invalid.</exception>
        /// <exception cref="ArgumentException">A two-checker probability exceeds its one-checker probability.</exception>
        public static NormalizedEvalResultModel From(EvalResultModel eval, GameModus modus)
        {
            var maxDistance = modus switch
            {
                GameModus.Backgammon or GameModus.Tavla or GameModus.Portes or GameModus.Fevga => 25d,
                // in plakoto we start at index 0 with all checkers
                GameModus.Plakoto => 24d,
                _ => throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unsupported feature game mode.")
            };

            // we expect a max of 7 anchors for all modes except plakoto and fevga
            var maxMadePoints = modus is GameModus.Plakoto or GameModus.Fevga ? 15d : 7d;
            var maxPips = 15 * maxDistance;
            var maxHomePips = 15 * (maxDistance - 6);

            var result = new NormalizedEvalResultModel
            {
                Race = eval.Race,
                HitProbability1 = Scale(eval.HitProbability1, 1, nameof(eval.HitProbability1)),
                HitProbability2 = Scale(eval.HitProbability2, 1, nameof(eval.HitProbability2)),
                HitOpponentProbability1 = Scale(eval.HitOpponentProbability1, 1, nameof(eval.HitOpponentProbability1)),
                HitOpponentProbability2 = Scale(eval.HitOpponentProbability2, 1, nameof(eval.HitOpponentProbability2)),
                BlotCount = Count(eval.BlotCount, 15, nameof(eval.BlotCount)),
                BlotCountOpp = Count(eval.BlotCountOpp, 15, nameof(eval.BlotCountOpp)),
                BlotInStartRangeCount = Count(eval.BlotInStartRangeCount, 6, nameof(eval.BlotInStartRangeCount)),
                BlotInStartRangeCountOpp = Count(eval.BlotInStartRangeCountOpp, 6, nameof(eval.BlotInStartRangeCountOpp)),
                AnchorCount = Count(eval.AnchorCount, maxMadePoints, nameof(eval.AnchorCount)),
                AnchorCountOpp = Count(eval.AnchorCountOpp, maxMadePoints, nameof(eval.AnchorCountOpp)),
                PipDifference = 0.5 + Count(eval.PipDifference, maxPips, nameof(eval.PipDifference), -maxPips) / 2,
                PipToBearOff = Count(eval.PipToBearOff, maxHomePips, nameof(eval.PipToBearOff)),
                PipToBearOffOpp = Count(eval.PipToBearOffOpp, maxHomePips, nameof(eval.PipToBearOffOpp)),
                NumChFrontLastPin = Count(eval.NumChFrontLastPin, 15, nameof(eval.NumChFrontLastPin)),
                NumChFrontLastPinOpp = Count(eval.NumChFrontLastPinOpp, 15, nameof(eval.NumChFrontLastPinOpp)),
                EscapeProbability1 = Scale(eval.EscapeProbability1, 1, nameof(eval.EscapeProbability1)),
                EscapeProbability2 = Scale(eval.EscapeProbability2, 1, nameof(eval.EscapeProbability2)),
                EscapeProbability1Opp = Scale(eval.EscapeProbability1Opp, 1, nameof(eval.EscapeProbability1Opp)),
                EscapeProbability2Opp = Scale(eval.EscapeProbability2Opp, 1, nameof(eval.EscapeProbability2Opp)),
                PinCountPlayer = Count(eval.PinCountPlayer, 15, nameof(eval.PinCountPlayer)),
                PinCountOpp = Count(eval.PinCountOpp, 15, nameof(eval.PinCountOpp)),
                OppMotherPinned = Count(eval.OppMotherPinned, 1, nameof(eval.OppMotherPinned)),
                PlayerMotherPinned = Count(eval.PlayerMotherPinned, 1, nameof(eval.PlayerMotherPinned)),
                MotherDistancePlayer = Count(eval.MotherDistancePlayer, 18, nameof(eval.MotherDistancePlayer)),
                MotherDistanceOpp = Count(eval.MotherDistanceOpp, 18, nameof(eval.MotherDistanceOpp)),
                TurnNumber = NormalizeTurn(eval.TurnNumber),
                MaxPrimeLengthPlayer = Count(eval.MaxPrimeLengthPlayer, maxMadePoints, nameof(eval.MaxPrimeLengthPlayer)),
                MaxPrimeLengthOpp = Count(eval.MaxPrimeLengthOpp, maxMadePoints, nameof(eval.MaxPrimeLengthOpp)),
                HomebarCountPlayer = Count(eval.HomebarCountPlayer, 15, nameof(eval.HomebarCountPlayer)),
                HomebarCountOpp = Count(eval.HomebarCountOpp, 15, nameof(eval.HomebarCountOpp)),
                PrimeProbabilityPlayer = Scale(eval.PrimeProbabilityPlayer, 1, nameof(eval.PrimeProbabilityPlayer)),
                PrimeProbabilityOpp = Scale(eval.PrimeProbabilityOpp, 1, nameof(eval.PrimeProbabilityOpp)),
                AnchorCountInFrontPlayer = Count(eval.AnchorCountInFrontPlayer, maxMadePoints, nameof(eval.AnchorCountInFrontPlayer)),
                AnchorCountInFrontOpp = Count(eval.AnchorCountInFrontOpp, maxMadePoints, nameof(eval.AnchorCountInFrontOpp)),
                AverageStackHeightPlayer = Mean(eval.AverageStackHeightPlayer, 15, nameof(eval.AverageStackHeightPlayer)),
                AverageStackHeightOpp = Mean(eval.AverageStackHeightOpp, 15, nameof(eval.AverageStackHeightOpp)),
                AverageDistanceToBearOffPlayer = Mean(eval.AverageDistanceToBearOffPlayer, maxDistance, nameof(eval.AverageDistanceToBearOffPlayer)),
                AverageDistanceToBearOffOpp = Mean(eval.AverageDistanceToBearOffOpp, maxDistance, nameof(eval.AverageDistanceToBearOffOpp)),
                AverageGapSizePlayer = Scale(eval.AverageGapSizePlayer, 22, nameof(eval.AverageGapSizePlayer)),
                AverageGapSizeOpp = Scale(eval.AverageGapSizeOpp, 22, nameof(eval.AverageGapSizeOpp)),
                CheckersInPrimeZonePlayer = Count(eval.CheckersInPrimeZonePlayer, 15, nameof(eval.CheckersInPrimeZonePlayer)),
                CheckersInPrimeZoneOpp = Count(eval.CheckersInPrimeZoneOpp, 15, nameof(eval.CheckersInPrimeZoneOpp))
            };

            ValidateProbabilityPair(eval.HitProbability1, eval.HitProbability2, nameof(eval.HitProbability1), nameof(eval.HitProbability2));
            ValidateProbabilityPair(eval.HitOpponentProbability1, eval.HitOpponentProbability2, nameof(eval.HitOpponentProbability1), nameof(eval.HitOpponentProbability2));
            ValidateProbabilityPair(eval.EscapeProbability1, eval.EscapeProbability2, nameof(eval.EscapeProbability1), nameof(eval.EscapeProbability2));
            ValidateProbabilityPair(eval.EscapeProbability1Opp, eval.EscapeProbability2Opp, nameof(eval.EscapeProbability1Opp), nameof(eval.EscapeProbability2Opp));

            return result;
        }

        private static double Scale(double value, double maximum, string feature, double minimum = 0)
        {
            // Validate the raw domain before scaling; invalid values are never clamped.
            if (!double.IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(feature, value, $"Expected a finite value in [{minimum}, {maximum}].");
            }

            return value / maximum;
        }

        private static double Count(double value, double maximum, string feature, double minimum = 0)
        {
            var normalized = Scale(value, maximum, feature, minimum);

            if (value != Math.Truncate(value))
            {
                throw new ArgumentOutOfRangeException(feature, value, "Expected an integer count, flag, or pip measurement.");
            }

            return normalized;
        }

        private static double Mean(double value, double maximum, string feature)
        {
            var normalized = Scale(value, maximum, feature);

            if (value > 0 && value < 1)
            {
                throw new ArgumentOutOfRangeException(feature, value, "Expected zero for an empty population, or a mean of at least one.");
            }

            return normalized;
        }

        private static double NormalizeTurn(int turn)
        {
            if (turn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(EvalResultModel.TurnNumber), turn, "Expected a nonnegative turn number.");
            }

            return turn / (turn + 100d);
        }

        private static void ValidateProbabilityPair(double one, double two, string oneName, string twoName)
        {
            if (two > one)
            {
                throw new ArgumentException($"{twoName} must not exceed {oneName}.", twoName);
            }
        }
    }
}
