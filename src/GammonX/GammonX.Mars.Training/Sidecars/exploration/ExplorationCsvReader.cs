using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

using System.Globalization;

namespace GammonX.Mars.Training.Sidecars
{
    /// <summary>
    /// Reads exploration decisions from the fixed twelve-column CSV sidecar format.
    /// </summary>
    public static class ExplorationCsvReader
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Lazily reads and validates exploration decisions from a UTF-8 CSV file.
        /// </summary>
        /// <param name="path">The source file path.</param>
        /// <returns>An enumerable sequence of parsed exploration decisions.</returns>
        /// <exception cref="InvalidDataException">Thrown when the header, column count, or a field is invalid.</exception>
        public static IEnumerable<ExplorationDecision> ReadDecisions(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            using var reader = new StreamReader(path);
            var header = reader.ReadLine()?.TrimStart('\uFEFF');
            if (!string.Equals(header, Constants.ExplorationCsvHeader, StringComparison.Ordinal))
                throw new InvalidDataException("The exploration sidecar header is invalid.");

            string? line;
            var lineNumber = 1;
            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // The sidecar format has no quoted fields, so each row is split into fixed columns.
                var columns = line.Split(',');
                if (columns.Length != 12)
                    throw new InvalidDataException($"Exploration sidecar line {lineNumber} has {columns.Length} columns instead of 12.");

                if (!Guid.TryParse(columns[0], out var gameId)
                    || !Enum.TryParse<GameModus>(columns[1], out var modus)
                    || !int.TryParse(columns[2], NumberStyles.Integer, InvariantCulture, out var turnIndex)
                    || !TryParseBoolean(columns[3], out var earlyPhase)
                    || !TryParseBoolean(columns[4], out var againstBot)
                    || !int.TryParse(columns[5], NumberStyles.Integer, InvariantCulture, out var candidateCount)
                    || !double.TryParse(columns[6], NumberStyles.Float, InvariantCulture, out var bestScore)
                    || !TryParseOptionalDouble(columns[7], out var secondBestScore)
                    || !TryParseOptionalDouble(columns[8], out var scoreGap)
                    || !Enum.TryParse<ExplorationChoice>(columns[9], out var choice)
                    || !TryParseOptionalInt(columns[10], out var selectedRank)
                    || !double.TryParse(columns[11], NumberStyles.Float, InvariantCulture, out var selectedScore))
                {
                    throw new InvalidDataException($"Exploration sidecar line {lineNumber} contains an invalid value.");
                }

                yield return new ExplorationDecision(
                    gameId,
                    modus,
                    turnIndex,
                    earlyPhase,
                    againstBot,
                    candidateCount,
                    bestScore,
                    secondBestScore,
                    scoreGap,
                    choice,
                    selectedRank,
                    selectedScore);
            }
        }

        /// <summary>
        /// Parses the sidecar's numeric boolean representation.
        /// </summary>
        private static bool TryParseBoolean(string value, out bool result)
        {
            if (value == "1")
            {
                result = true;
                return true;
            }

            if (value == "0")
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        /// <summary>
        /// Parses an optional invariant-culture double from an empty or populated field.
        /// </summary>
        private static bool TryParseOptionalDouble(string value, out double? result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = null;
                return true;
            }

            if (double.TryParse(value, NumberStyles.Float, InvariantCulture, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Parses an optional invariant-culture integer from an empty or populated field.
        /// </summary>
        private static bool TryParseOptionalInt(string value, out int? result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = null;
                return true;
            }

            if (int.TryParse(value, NumberStyles.Integer, InvariantCulture, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = null;
            return false;
        }
    }
}
