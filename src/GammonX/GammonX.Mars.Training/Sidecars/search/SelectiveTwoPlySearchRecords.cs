using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Sidecars;

public sealed record SelectiveTwoPlySearchDecision(
    Guid GameId,
    GameModus Modus,
    int TurnIndex,
    bool AgainstBot,
    int CandidateCount,
    int EvaluatedCandidateCount,
    int? SelectiveCandidateLimit,
    double OnePlyBestScore,
    double? OnePlySecondBestScore,
    double? OnePlyScoreGap,
    SelectiveTwoPlyReason Reason,
    double? TwoPlyBestScore,
    double? TwoPlySecondBestScore,
    double? TwoPlyScoreGap,
    bool? BestMoveChanged,
    int? TwoPlyBestOnePlyRank);

public sealed record SelectiveTwoPlySearchDiagnosticsKey(GameModus Modus, bool AgainstBot);

public sealed record SelectiveTwoPlySearchSummary(
    long DecisionCount,
    long EvaluatedDecisionCount,
    long BestMoveChangedCount,
    double BestMoveChangeRate,
    double AverageEvaluatedCandidateCount,
    double? MeanOnePlyScoreGap,
    double? MeanTwoPlyScoreGap,
    long AuditDecisionCount,
    long AuditBestMoveChangedCount,
    double AuditBestMoveChangeRate,
    long RankedAuditDecisionCount,
    long AuditWinnerOutsideCandidateLimitCount,
    double AuditWinnerOutsideCandidateLimitRate,
    IReadOnlyDictionary<int, long> AuditWinnerOnePlyRankCounts,
    IReadOnlyDictionary<SelectiveTwoPlyReason, long> ReasonCounts);

public sealed record SelectiveTwoPlySearchDiagnosticsReport(
    SelectiveTwoPlySearchSummary Overall,
    IReadOnlyDictionary<SelectiveTwoPlySearchDiagnosticsKey, SelectiveTwoPlySearchSummary> Groups);