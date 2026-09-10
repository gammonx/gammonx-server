using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Sidecars
{
    /// <summary>
    /// Provides diagnostic methods for exploration policies.
    /// </summary>
    /// <param name="GameId">Unique game identifier.</param>
    /// <param name="Modus">The game modus.</param>
    /// <param name="TurnIndex">The index of the turn.</param>
    /// <param name="EarlyPhase">Indicates if the decision was made in the early phase of the game.</param>
    /// <param name="AgainstBot">Indicates if the decision was made against a bot.</param>
    /// <param name="CandidateCount">The number of candidate moves considered.</param>
    /// <param name="BestScore">The score of the best candidate move.</param>
    /// <param name="SecondBestScore">The score of the second best candidate move, if available.</param>
    /// <param name="ScoreGap">The difference between the best and second best scores, if available.</param>
    /// <param name="Choice">The exploration choice made.</param>
    /// <param name="SelectedRank">The rank of the selected move, if available.</param>
    /// <param name="SelectedScore">The score of the selected move.</param>
    public sealed record ExplorationDecision(
        Guid GameId,
        GameModus Modus,
        int TurnIndex,
        bool EarlyPhase,
        bool AgainstBot,
        int CandidateCount,
        double BestScore,
        double? SecondBestScore,
        double? ScoreGap,
        ExplorationChoice Choice,
        int? SelectedRank,
        double SelectedScore);

    /// <summary>
    /// Identifies a diagnostics group by game mode, phase, opponent, and candidate-count bucket.
    /// </summary>
    /// <param name="Modus">The game mode.</param>
    /// <param name="EarlyPhase">Whether the decision occurred during the early game phase.</param>
    /// <param name="AgainstBot">Whether the decision was made against a bot.</param>
    /// <param name="CandidateBucket">The normalized candidate-count range.</param>
    public sealed record ExplorationDiagnosticsKey(
        GameModus Modus,
        bool EarlyPhase,
        bool AgainstBot,
        string CandidateBucket);

    /// <summary>
    /// Summarizes score gaps and exploration choices for a set of decisions.
    /// </summary>
    /// <param name="DecisionCount">The number of decisions analyzed.</param>
    /// <param name="GapCount">The number of decisions with a score gap.</param>
    /// <param name="NoGapCount">The number of decisions without a score gap.</param>
    /// <param name="MinGap">The minimum observed score gap, or <see langword="null"/> when no gaps exist.</param>
    /// <param name="MeanGap">The mean observed score gap, or <see langword="null"/> when no gaps exist.</param>
    /// <param name="MaxGap">The maximum observed score gap, or <see langword="null"/> when no gaps exist.</param>
    /// <param name="P10Gap">The tenth percentile score gap.</param>
    /// <param name="P25Gap">The twenty-fifth percentile score gap.</param>
    /// <param name="P50Gap">The fiftieth percentile score gap.</param>
    /// <param name="P75Gap">The seventy-fifth percentile score gap.</param>
    /// <param name="P90Gap">The ninetieth percentile score gap.</param>
    /// <param name="P95Gap">The ninety-fifth percentile score gap.</param>
    /// <param name="ChoiceCounts">The number of decisions for each exploration choice.</param>
    /// <param name="SelectedRankCounts">The number of selections for each candidate rank.</param>
    public sealed record ExplorationGapSummary(
        long DecisionCount,
        long GapCount,
        long NoGapCount,
        double? MinGap,
        double? MeanGap,
        double? MaxGap,
        double? P10Gap,
        double? P25Gap,
        double? P50Gap,
        double? P75Gap,
        double? P90Gap,
        double? P95Gap,
        IReadOnlyDictionary<ExplorationChoice, long> ChoiceCounts,
        IReadOnlyDictionary<int, long> SelectedRankCounts);

    /// <summary>
    /// Contains overall exploration diagnostics and diagnostics split by group.
    /// </summary>
    /// <param name="Overall">The summary across all decisions.</param>
    /// <param name="Groups">Summaries keyed by mode, phase, opponent, and candidate bucket.</param>
    public sealed record ExplorationDiagnosticsReport(
        ExplorationGapSummary Overall,
        IReadOnlyDictionary<ExplorationDiagnosticsKey, ExplorationGapSummary> Groups);

}
