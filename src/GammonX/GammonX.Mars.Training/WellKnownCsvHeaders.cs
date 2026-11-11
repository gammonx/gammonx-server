using GammonX.Models.Enums;

namespace GammonX.Mars.Training;

public static class WellKnownCsvHeaders
{
    /// <summary>
    /// Gets the three-head label columns.
    /// </summary>
    public const string TrainingDataHeaderGammon = "pWin,pGammonWin,pGammonLoss";

    /// <summary>
    /// Gets the five-head label columns.
    /// </summary>
    public const string TrainingDataHeader = "pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss";

    public const string ExplorationSidecarHeader =
        "gameId,modus,turnIndex,earlyPhase,againstBot,candidateCount,bestScore,secondBestScore,scoreGap,choice,selectedRank,selectedScore";

    public const string SelectiveTwoPlySearchSidecarHeader =
        "gameId,modus,turnIndex,againstBot,candidateCount,evaluatedCandidateCount,selectiveCandidateLimit,onePlyBestScore,onePlySecondBestScore,onePlyScoreGap,reason,twoPlyBestScore,twoPlySecondBestScore,twoPlyScoreGap,bestMoveChanged,twoPlyBestOnePlyRank";

    public const string TrajectorySidecarHeader = "gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss";

    public const string GamesSidecarHeader = "gameId,totalTurns,whiteWon,winnerResult,loserResult";

    public static int LabelCount(GameModus modus) => modus is GameModus.Fevga or GameModus.Plakoto ? 3 : 5;

    public static string TrainingHeader(int labelCount) => labelCount switch
    {
        3 => TrainingDataHeaderGammon,
        5 => TrainingDataHeader,
        _ => throw new ArgumentOutOfRangeException(nameof(labelCount), "Training requires three or five labels.")
    };

    /// <summary>
    /// Reject mismatched datasets before label columns can be mistaken for features.
    /// </summary>
    public static void ValidateTrainingHeader(string header, int labelCount, int? expectedFeatureCount = null)
    {
        var columns = header.Split(',');
        var featureCount = columns.Length - labelCount;
        
        if (featureCount <= 0
            || (expectedFeatureCount.HasValue && featureCount != expectedFeatureCount.Value)
            || string.Join(',', columns.Skip(Math.Max(0, featureCount))) != TrainingHeader(labelCount)
            || !columns.Take(Math.Max(0, featureCount)).SequenceEqual(Enumerable.Range(0, Math.Max(0, featureCount)).Select(i => $"f{i}")))
        {
            throw new InvalidDataException($"Expected feature columns followed by {TrainingHeader(labelCount)}. Regenerate incompatible training data.");
        }
    }
}