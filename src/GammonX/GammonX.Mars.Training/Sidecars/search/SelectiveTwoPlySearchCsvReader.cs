using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

using System.Globalization;

namespace GammonX.Mars.Training.Sidecars;

public static class SelectiveTwoPlySearchCsvReader
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static IEnumerable<SelectiveTwoPlySearchDecision> ReadDecisions(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path);
        var header = reader.ReadLine()?.TrimStart('\uFEFF');
        var validFormat = string.Equals(header, WellKnownCsvHeaders.SelectiveTwoPlySearchSidecarHeader, StringComparison.Ordinal);
        if (!validFormat)
        {
            throw new InvalidOperationException($"Incompatible CSV header: {header}");
        }

        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            var expectedColumnCount = 16;
            if (columns.Length != expectedColumnCount)
            {
                throw new InvalidDataException($"Selective two-ply search sidecar line {lineNumber} has {columns.Length} columns instead of {expectedColumnCount}.");
            }

            if (!Guid.TryParse(columns[0], out var gameId)
                || !Enum.TryParse<GameModus>(columns[1], out var modus)
                || !int.TryParse(columns[2], NumberStyles.Integer, InvariantCulture, out var turnIndex)
                || !TryParseBoolean(columns[3], out var againstBot)
                || !int.TryParse(columns[4], NumberStyles.Integer, InvariantCulture, out var candidateCount)
                || !int.TryParse(columns[5], NumberStyles.Integer, InvariantCulture, out var evaluatedCandidateCount)
                || !int.TryParse(columns[6], NumberStyles.Integer, InvariantCulture, out var selectiveCandidateLimit)
                || !double.TryParse(columns[7], NumberStyles.Float, InvariantCulture, out var onePlyBestScore)
                || !TryParseOptionalDouble(columns[8], out var onePlySecondBestScore)
                || !TryParseOptionalDouble(columns[9], out var onePlyScoreGap)
                || !Enum.TryParse<SelectiveTwoPlyReason>(columns[10], out var reason)
                || !TryParseOptionalDouble(columns[11], out var twoPlyBestScore)
                || !TryParseOptionalDouble(columns[12], out var twoPlySecondBestScore)
                || !TryParseOptionalDouble(columns[13], out var twoPlyScoreGap)
                || !TryParseOptionalBoolean(columns[14], out var bestMoveChanged)
                || !TryParseOptionalInteger(columns[15], out var twoPlyBestOnePlyRank))
            {
                throw new InvalidDataException($"Selective two-ply search sidecar line {lineNumber} contains an invalid value.");
            }

            yield return new SelectiveTwoPlySearchDecision(
                gameId,
                modus,
                turnIndex,
                againstBot,
                candidateCount,
                evaluatedCandidateCount,
                selectiveCandidateLimit,
                onePlyBestScore,
                onePlySecondBestScore,
                onePlyScoreGap,
                reason,
                twoPlyBestScore,
                twoPlySecondBestScore,
                twoPlyScoreGap,
                bestMoveChanged,
                twoPlyBestOnePlyRank);
        }
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        result = value == "1";
        return value is "0" or "1";
    }

    private static bool TryParseOptionalBoolean(string value, out bool? result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = null;
            return true;
        }

        if (TryParseBoolean(value, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

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

    private static bool TryParseOptionalInteger(string value, out int? result)
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