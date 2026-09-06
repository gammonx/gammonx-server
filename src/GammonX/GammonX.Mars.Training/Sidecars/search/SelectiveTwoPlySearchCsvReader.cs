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
        var validFormat = string.Equals(header, SelectiveTwoPlySearchCsvWriter.Header, StringComparison.Ordinal);
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
                throw new InvalidDataException($"Selective two-ply search sidecar line {lineNumber} has {columns.Length} columns instead of {expectedColumnCount}.");

            int? selectiveCandidateLimit = null;
            int? twoPlyBestOnePlyRank = null;

            const int scoreOffset = 1;

            if (!Guid.TryParse(columns[0], out var gameId)
                || !Enum.TryParse<GameModus>(columns[1], out var modus)
                || !int.TryParse(columns[2], NumberStyles.Integer, InvariantCulture, out var turnIndex)
                || !TryParseBoolean(columns[3], out var againstBot)
                || !int.TryParse(columns[4], NumberStyles.Integer, InvariantCulture, out var candidateCount)
                || !int.TryParse(columns[5], NumberStyles.Integer, InvariantCulture, out var evaluatedCandidateCount)
                || !double.TryParse(columns[6 + scoreOffset], NumberStyles.Float, InvariantCulture, out var onePlyBestScore)
                || !TryParseOptionalDouble(columns[7 + scoreOffset], out var onePlySecondBestScore)
                || !TryParseOptionalDouble(columns[8 + scoreOffset], out var onePlyScoreGap)
                || !Enum.TryParse<SelectiveTwoPlyReason>(columns[9 + scoreOffset], out var reason)
                || !TryParseOptionalDouble(columns[10 + scoreOffset], out var twoPlyBestScore)
                || !TryParseOptionalDouble(columns[11 + scoreOffset], out var twoPlySecondBestScore)
                || !TryParseOptionalDouble(columns[12 + scoreOffset], out var twoPlyScoreGap)
                || !TryParseOptionalBoolean(columns[13 + scoreOffset], out var bestMoveChanged))
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
}