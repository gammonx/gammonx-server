using GammonX.Models.Enums;

namespace GammonX.Models.Contracts
{
    internal static class WorkContractValidation
    {
        /// <summary>
        /// Validates the provided game records, ensuring there is a winner and a loser.
        /// </summary>
        /// <param name="records">The array of game records to validate.</param>
        /// <returns>A tuple containing the winner and loser game records.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the game records are invalid or do not represent a completed game.</exception>
        public static (GameRecordContract Winner, GameRecordContract Loser) ValidateGameRecords(GameRecordContract[]? records)
        {
            if (records == null || records.Length != 2 || records.Any(record => record == null))
                throw new InvalidOperationException("A completed game requires exactly two player records.");

            var first = records[0];
            var second = records[1];
            ValidateCommonIdentity(
                first.Id,
                second.Id,
                first.PlayerId,
                second.PlayerId,
                first.Format,
                second.Format,
                first.GameHistory,
                second.GameHistory,
                "game");

            if (first.MatchId == Guid.Empty || first.MatchId != second.MatchId)
                throw new InvalidOperationException("Completed game records must reference the same match.");

            return SelectDecisivePair(first, second, record => record.Result.HasWon(), "game");
        }

        /// <summary>
        /// Validates the provided match records, ensuring there is a winner and a loser.
        /// </summary>
        /// <param name="records">The array of match records to validate.</param>
        /// <param name="requireRanked">Indicates whether the match must be ranked.</param>
        /// <returns>A tuple containing the winner and loser match records.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the match records are invalid or do not represent a completed match.</exception>
        public static (MatchRecordContract Winner, MatchRecordContract Loser) ValidateMatchRecords(
            MatchRecordContract[]? records,
            bool requireRanked)
        {
            if (records == null || records.Length != 2 || records.Any(record => record == null))
                throw new InvalidOperationException("A completed match requires exactly two player records.");

            var first = records[0];
            var second = records[1];
            ValidateCommonIdentity(
                first.Id,
                second.Id,
                first.PlayerId,
                second.PlayerId,
                first.Format,
                second.Format,
                first.MatchHistory,
                second.MatchHistory,
                "match");

            if (first.Variant != second.Variant || first.Type != second.Type || first.Modus != second.Modus || first.BotLevel != second.BotLevel)
                throw new InvalidOperationException("Completed match records must have matching match settings.");
            if (requireRanked && first.Modus != MatchModus.Ranked)
                throw new InvalidOperationException("Rating updates require a ranked match.");

            return SelectDecisivePair(first, second, record => record.Result.HasWon(), "match");
        }

        private static void ValidateCommonIdentity(
            Guid firstId,
            Guid secondId,
            Guid firstPlayerId,
            Guid secondPlayerId,
            HistoryFormat firstFormat,
            HistoryFormat secondFormat,
            string firstHistory,
            string secondHistory,
            string itemName)
        {
            if (firstId == Guid.Empty || firstId != secondId)
                throw new InvalidOperationException($"Completed {itemName} records must have the same non-empty ID.");
            if (firstPlayerId == Guid.Empty || secondPlayerId == Guid.Empty || firstPlayerId == secondPlayerId)
                throw new InvalidOperationException($"Completed {itemName} records must belong to two distinct players.");
            if (firstFormat == HistoryFormat.Unknown || firstFormat != secondFormat)
                throw new InvalidOperationException($"Completed {itemName} records must have the same known history format.");
            if (string.IsNullOrWhiteSpace(firstHistory) || !string.Equals(firstHistory, secondHistory, StringComparison.Ordinal))
                throw new InvalidOperationException($"Completed {itemName} records must have the same non-empty history.");
        }

        private static (T Winner, T Loser) SelectDecisivePair<T>(
            T first,
            T second,
            Func<T, bool?> resultSelector,
            string itemName)
        {
            var firstResult = resultSelector(first);
            var secondResult = resultSelector(second);
            if (firstResult == true && secondResult == false)
                return (first, second);
            if (firstResult == false && secondResult == true)
                return (second, first);

            throw new InvalidOperationException($"Completed {itemName} records require one winner and one loser.");
        }
    }
}