using GammonX.Mars.Training.Generator;

namespace GammonX.Mars.Training.Sidecars;

public static class SelectiveTwoPlySearchBroker
{
    public static SelectiveTwoPlySearchDiagnosticsReport Analyze(IEnumerable<SelectiveTwoPlySearchDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var decisionList = decisions.ToArray();
        var groups = decisionList
            .GroupBy(decision => new SelectiveTwoPlySearchDiagnosticsKey(decision.Modus, decision.AgainstBot))
            .ToDictionary(group => group.Key, Summarize);

        return new SelectiveTwoPlySearchDiagnosticsReport(Summarize(decisionList), groups);
    }

    private static SelectiveTwoPlySearchSummary Summarize(IEnumerable<SelectiveTwoPlySearchDecision> decisions)
    {
        var decisionList = decisions.ToArray();
        var evaluated = decisionList.Where(decision => decision.EvaluatedCandidateCount > 0).ToArray();
        var changedCount = evaluated.LongCount(decision => decision.BestMoveChanged == true);
        var onePlyGaps = decisionList.Where(decision => decision.OnePlyScoreGap.HasValue).Select(decision => decision.OnePlyScoreGap!.Value).ToArray();
        var twoPlyGaps = evaluated.Where(decision => decision.TwoPlyScoreGap.HasValue).Select(decision => decision.TwoPlyScoreGap!.Value).ToArray();
        var audits = decisionList.Where(decision => decision.Reason == SelectiveTwoPlyReason.Audit).ToArray();
        var auditChangedCount = audits.LongCount(decision => decision.BestMoveChanged == true);
        var rankedAudits = audits
            .Where(decision => decision.SelectiveCandidateLimit.HasValue && decision.TwoPlyBestOnePlyRank.HasValue)
            .ToArray();
        var auditWinnerOutsideCandidateLimitCount = rankedAudits.LongCount(decision =>
            decision.TwoPlyBestOnePlyRank!.Value > decision.SelectiveCandidateLimit!.Value);

        return new SelectiveTwoPlySearchSummary(
            decisionList.LongLength,
            evaluated.LongLength,
            changedCount,
            evaluated.Length == 0 ? 0d : (double)changedCount / evaluated.Length,
            evaluated.Length == 0 ? 0d : evaluated.Average(decision => decision.EvaluatedCandidateCount),
            onePlyGaps.Length == 0 ? null : onePlyGaps.Average(),
            twoPlyGaps.Length == 0 ? null : twoPlyGaps.Average(),
            audits.LongLength,
            auditChangedCount,
            audits.Length == 0 ? 0d : (double)auditChangedCount / audits.Length,
            rankedAudits.LongLength,
            auditWinnerOutsideCandidateLimitCount,
            rankedAudits.Length == 0 ? 0d : (double)auditWinnerOutsideCandidateLimitCount / rankedAudits.Length,
            rankedAudits
                .GroupBy(decision => decision.TwoPlyBestOnePlyRank!.Value)
                .ToDictionary(group => group.Key, group => group.LongCount()),
            decisionList
                .GroupBy(decision => decision.Reason)
                .ToDictionary(group => group.Key, group => group.LongCount()));
    }
}