using System.Globalization;
using System.Text;

namespace GammonX.Mars.Training.Sidecars
{
    /// <summary>
    /// Writes exploration decisions to the fixed twelve-column CSV sidecar format.
    /// </summary>
    public static class ExplorationCsvWriter
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Writes a header and all supplied exploration decisions to a UTF-8 file.
        /// </summary>
        /// <param name="path">The destination file path.</param>
        /// <param name="decisions">The decisions to serialize.</param>
        /// <remarks>
        /// Numeric values use invariant culture and high-precision round-trip formatting.
        /// Boolean values are written as <c>1</c> or <c>0</c>; nullable values are empty fields.
        /// </remarks>
        public static void WriteDecisions(string path, IEnumerable<ExplorationDecision> decisions)
        {
            // The reader expects this exact header and column order.
            using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
            writer.WriteLine(Constants.ExplorationCsvHeader);

            foreach (var decision in decisions)
            {
                writer.Write(decision.GameId.ToString("D"));
                writer.Write(',');
                writer.Write(decision.Modus.ToString());
                writer.Write(',');
                writer.Write(decision.TurnIndex.ToString(InvariantCulture));
                writer.Write(',');
                writer.Write(decision.EarlyPhase ? "1" : "0");
                writer.Write(',');
                writer.Write(decision.AgainstBot ? "1" : "0");
                writer.Write(',');
                writer.Write(decision.CandidateCount.ToString(InvariantCulture));
                writer.Write(',');
                writer.Write(decision.BestScore.ToString("G17", InvariantCulture));
                writer.Write(',');
                writer.Write(decision.SecondBestScore?.ToString("G17", InvariantCulture));
                writer.Write(',');
                writer.Write(decision.ScoreGap?.ToString("G17", InvariantCulture));
                writer.Write(',');
                writer.Write(decision.Choice.ToString());
                writer.Write(',');
                writer.Write(decision.SelectedRank?.ToString(InvariantCulture));
                writer.Write(',');
                writer.Write(decision.SelectedScore.ToString("G17", InvariantCulture));
                writer.WriteLine();
            }
        }
    }
}
