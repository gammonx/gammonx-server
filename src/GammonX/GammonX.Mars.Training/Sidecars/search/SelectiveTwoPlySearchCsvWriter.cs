using System.Globalization;
using System.Text;

namespace GammonX.Mars.Training.Sidecars;

public static class SelectiveTwoPlySearchCsvWriter
{
    public const string Header = "gameId,modus,turnIndex,againstBot,candidateCount,evaluatedCandidateCount,selectiveCandidateLimit,onePlyBestScore,onePlySecondBestScore,onePlyScoreGap,reason,twoPlyBestScore,twoPlySecondBestScore,twoPlyScoreGap,bestMoveChanged,twoPlyBestOnePlyRank";

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static void WriteDecisions(string path, IEnumerable<SelectiveTwoPlySearchDecision> decisions)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine(Header);

        foreach (var decision in decisions)
        {
            writer.Write(decision.GameId.ToString("D"));
            writer.Write(',');
            writer.Write(decision.Modus.ToString());
            writer.Write(',');
            writer.Write(decision.TurnIndex.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(decision.AgainstBot ? "1" : "0");
            writer.Write(',');
            writer.Write(decision.CandidateCount.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(decision.EvaluatedCandidateCount.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(decision.SelectiveCandidateLimit?.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(decision.OnePlyBestScore.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.OnePlySecondBestScore?.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.OnePlyScoreGap?.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.Reason.ToString());
            writer.Write(',');
            writer.Write(decision.TwoPlyBestScore?.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.TwoPlySecondBestScore?.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.TwoPlyScoreGap?.ToString("G17", InvariantCulture));
            writer.Write(',');
            writer.Write(decision.BestMoveChanged switch
            {
                true => "1",
                false => "0",
                null => string.Empty
            });
            writer.Write(',');
            writer.Write(decision.TwoPlyBestOnePlyRank?.ToString(InvariantCulture));
            writer.WriteLine();
        }
    }
}