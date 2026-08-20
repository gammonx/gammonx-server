using GammonX.Mars.Training.Generator;

using System.Globalization;
using System.Text;

namespace GammonX.Mars.Training.Sidecars;

public static class TrajectoryCsvWriter
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static void WritePositions(string path, IEnumerable<TrainingDataRow> rows)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine("gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss");

        foreach (var row in rows)
        {
            var prediction = row.Position.Prediction;
            if (prediction.Length < ForwardViewTdCalculator.FullHeadCount)
                throw new InvalidDataException("Trajectory predictions must contain five output heads.");

            writer.Write(row.GameId.ToString("D"));
            writer.Write(',');
            writer.Write(row.Position.TurnIndex.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(row.Position.IsWhite ? "1" : "0");
            writer.Write(',');
            writer.Write(row.Position.IsTerminal ? "1" : "0");

            for (var head = 0; head < ForwardViewTdCalculator.FullHeadCount; head++)
            {
                writer.Write(',');
                writer.Write(prediction[head].ToString("G9", InvariantCulture));
            }

            writer.WriteLine();
        }
    }

    public static void WriteGames(string path, IEnumerable<GameMetadata> games)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine("gameId,totalTurns,whiteWon,winnerResult,loserResult");

        foreach (var game in games)
        {
            writer.Write(game.GameId.ToString("D"));
            writer.Write(',');
            writer.Write(game.TotalTurns.ToString(InvariantCulture));
            writer.Write(',');
            writer.Write(game.WhiteWon ? "1" : "0");
            writer.Write(',');
            writer.Write(game.WinnerResult.ToString());
            writer.Write(',');
            writer.Write(game.LoserResult.ToString());
            writer.WriteLine();
        }
    }
}
