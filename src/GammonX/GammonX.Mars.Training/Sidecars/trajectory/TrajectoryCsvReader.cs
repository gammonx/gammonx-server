using System.Globalization;

using GammonX.Mars.Training.Generator;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Sidecars;

public static class TrajectoryCsvReader
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static IReadOnlyList<GameMetadata> ReadGames(string path)
    {
        using var reader = new StreamReader(path);
        var header = reader.ReadLine();
        if (header != WellKnownCsvHeaders.GamesSidecarHeader)
        {
            throw new InvalidDataException($"Unexpected game metadata header in '{path}'.");
        }

        var games = new List<GameMetadata>();
        var gameIds = new HashSet<Guid>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 5)
                throw new InvalidDataException($"Game metadata row {lineNumber} in '{path}' has {columns.Length} columns; expected 5.");

            var gameId = ParseGuid(columns[0], path, lineNumber);
            if (!gameIds.Add(gameId))
                throw new InvalidDataException($"Game metadata contains duplicate game ID '{gameId}'.");

            if (!int.TryParse(columns[1], NumberStyles.Integer, InvariantCulture, out var totalTurns) || totalTurns <= 0)
                throw new InvalidDataException($"Invalid total turn count on game metadata row {lineNumber} in '{path}'.");

            var whiteWon = ParseBoolean(columns[2], path, lineNumber);
            if (!Enum.TryParse<GameResult>(columns[3], ignoreCase: false, out var winnerResult)
                || !Enum.TryParse<GameResult>(columns[4], ignoreCase: false, out var loserResult))
            {
                throw new InvalidDataException($"Invalid game result on game metadata row {lineNumber} in '{path}'.");
            }

            games.Add(new GameMetadata(gameId, totalTurns, whiteWon, winnerResult, loserResult));
        }

        return games;
    }

    public static IReadOnlyList<Guid> ReadGameIds(string path)
    {
        using var reader = new StreamReader(path);
        var header = reader.ReadLine();
        if (header != WellKnownCsvHeaders.TrajectorySidecarHeader)
            throw new InvalidDataException($"Unexpected trajectory header in '{path}'.");

        var gameIds = new List<Guid>();
        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 9)
                throw new InvalidDataException($"Trajectory row {lineNumber} has {columns.Length} columns; expected 9.");

            gameIds.Add(ParseGuid(columns[0], path, lineNumber));
        }

        return gameIds;
    }

    public static IEnumerable<float[]> ReadPredictions(string path)
    {
        using var reader = new StreamReader(path);
        var header = reader.ReadLine();
        if (header != WellKnownCsvHeaders.TrajectorySidecarHeader)
            throw new InvalidDataException($"Unexpected trajectory header in '{path}'.");

        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 9)
                throw new InvalidDataException($"Trajectory row {lineNumber} has {columns.Length} columns; expected 9.");

            ParseGuid(columns[0], path, lineNumber);
            var prediction = new float[ForwardViewTdCalculator.FullHeadCount];
            for (var head = 0; head < prediction.Length; head++)
                prediction[head] = ParseFloat(columns[4 + head], path, lineNumber);

            yield return prediction;
        }
    }

    public static IReadOnlyList<TrainingDataRow> ReadRows(
        string trainingCsvPath,
        string trajectoryCsvPath,
        int labelCount)
        => ReadRowsStreaming(trainingCsvPath, trajectoryCsvPath, labelCount).ToList();

    public static IEnumerable<TrainingDataRow> ReadRowsStreaming(
        string trainingCsvPath,
        string trajectoryCsvPath,
        int labelCount)
    {
        if (labelCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(labelCount));

        using var trainingReader = new StreamReader(trainingCsvPath);
        using var trajectoryReader = new StreamReader(trajectoryCsvPath);

        var trainingHeader = trainingReader.ReadLine();
        var trajectoryHeader = trajectoryReader.ReadLine();
        if (string.IsNullOrWhiteSpace(trainingHeader))
            throw new InvalidDataException($"Training CSV '{trainingCsvPath}' has no header.");
        if (trajectoryHeader != WellKnownCsvHeaders.TrajectorySidecarHeader)
            throw new InvalidDataException($"Unexpected trajectory header in '{trajectoryCsvPath}'.");

        var trainingColumnCount = trainingHeader.Split(',').Length;
        if (trainingColumnCount <= labelCount)
            throw new InvalidDataException($"Training CSV '{trainingCsvPath}' does not contain features and labels.");

        var featureCount = trainingColumnCount - labelCount;
        var lineNumber = 1;
        while (true)
        {
            var trainingLine = trainingReader.ReadLine();
            var trajectoryLine = trajectoryReader.ReadLine();
            if (trainingLine is null || trajectoryLine is null)
            {
                if (trainingLine is not null || trajectoryLine is not null)
                    throw new InvalidDataException("Training CSV and trajectory sidecar have different row counts.");
                break;
            }

            lineNumber++;
            if (string.IsNullOrWhiteSpace(trainingLine) || string.IsNullOrWhiteSpace(trajectoryLine))
                throw new InvalidDataException($"Blank row at line {lineNumber} is not supported.");

            var trainingColumns = trainingLine.Split(',');
            if (trainingColumns.Length != trainingColumnCount)
                throw new InvalidDataException($"Training row {lineNumber} has {trainingColumns.Length} columns; expected {trainingColumnCount}.");

            var trajectoryColumns = trajectoryLine.Split(',');
            if (trajectoryColumns.Length != 9)
                throw new InvalidDataException($"Trajectory row {lineNumber} has {trajectoryColumns.Length} columns; expected 9.");

            var gameId = ParseGuid(trajectoryColumns[0], trajectoryCsvPath, lineNumber);
            if (!int.TryParse(trajectoryColumns[1], NumberStyles.Integer, InvariantCulture, out var turnIndex) || turnIndex < 0)
                throw new InvalidDataException($"Invalid turn index on trajectory row {lineNumber}.");

            var isWhite = ParseBoolean(trajectoryColumns[2], trajectoryCsvPath, lineNumber);
            var isTerminal = ParseBoolean(trajectoryColumns[3], trajectoryCsvPath, lineNumber);
            var prediction = new float[ForwardViewTdCalculator.FullHeadCount];
            for (var head = 0; head < prediction.Length; head++)
                prediction[head] = ParseFloat(trajectoryColumns[4 + head], trajectoryCsvPath, lineNumber);

            var features = new float[featureCount];
            for (var feature = 0; feature < featureCount; feature++)
                features[feature] = ParseFloat(trainingColumns[feature], trainingCsvPath, lineNumber);

            var labels = new float[labelCount];
            for (var label = 0; label < labelCount; label++)
                labels[label] = ParseFloat(trainingColumns[featureCount + label], trainingCsvPath, lineNumber);

            yield return new TrainingDataRow(
                gameId,
                new TrajectoryPosition(turnIndex, isWhite, features, prediction, isTerminal),
                labels);
        }
    }

    private static Guid ParseGuid(string value, string path, int lineNumber)
        => Guid.TryParse(value, out var gameId)
            ? gameId
            : throw new InvalidDataException($"Invalid game ID on row {lineNumber} in '{path}'.");

    private static bool ParseBoolean(string value, string path, int lineNumber)
    {
        return value switch
        {
            "0" => false,
            "1" => true,
            _ => throw new InvalidDataException($"Expected 0 or 1 on row {lineNumber} in '{path}'.")
        };
    }

    private static float ParseFloat(string value, string path, int lineNumber)
    {
        if (!float.TryParse(value, NumberStyles.Float, InvariantCulture, out var result) || !float.IsFinite(result))
            throw new InvalidDataException($"Invalid numeric value on row {lineNumber} in '{path}'.");
        return result;
    }
}
