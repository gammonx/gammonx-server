namespace GammonX.Mars.Training.Data;

public sealed record GameGroupedSplit<T>(
    IReadOnlyList<T> Training,
    IReadOnlyList<T> Validation);

/// <summary>
/// Helpers to shuffle games grouped by their game id.
/// </summary>
public static class GameGroupedShuffler
{
    public static GameGroupedSplit<T> SplitByGame<T>(
        IReadOnlyList<T> rows,
        Func<T, Guid> gameIdSelector,
        double trainFraction,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(gameIdSelector);

        if (!double.IsFinite(trainFraction) || trainFraction <= 0d || trainFraction >= 1d)
            throw new ArgumentOutOfRangeException(nameof(trainFraction), trainFraction, "The train fraction must be greater than 0 and less than 1.");

        if (rows.Count == 0)
            return new GameGroupedSplit<T>([], []);

        var groupsById = new Dictionary<Guid, List<T>>();
        var gameIds = new List<Guid>();
        foreach (var row in rows)
        {
            var gameId = gameIdSelector(row);
            if (!groupsById.TryGetValue(gameId, out var group))
            {
                group = [];
                groupsById.Add(gameId, group);
                gameIds.Add(gameId);
            }

            group.Add(row);
        }

        random ??= Random.Shared;
        Shuffle(gameIds, random);

        var trainGroupCount = FindBestTrainGroupCount(gameIds, groupsById, rows.Count, trainFraction);
        var training = new List<T>(rows.Count);
        var validation = new List<T>(rows.Count);

        for (var groupIndex = 0; groupIndex < gameIds.Count; groupIndex++)
        {
            var destination = groupIndex < trainGroupCount ? training : validation;
            destination.AddRange(groupsById[gameIds[groupIndex]]);
        }

        return new GameGroupedSplit<T>(training, validation);
    }

    public static IReadOnlyList<Guid> SelectGames(
        IReadOnlyList<Guid> gameIds,
        int gameCount,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(gameIds);

        if (gameCount < 0)
            throw new ArgumentOutOfRangeException(nameof(gameCount), gameCount, "The game count cannot be negative.");

        var uniqueGameIds = gameIds.Distinct().ToList();
        if (gameCount > uniqueGameIds.Count)
            throw new ArgumentOutOfRangeException(nameof(gameCount), gameCount, "The game count cannot exceed the number of available games.");

        random ??= Random.Shared;
        Shuffle(uniqueGameIds, random);
        return uniqueGameIds.Take(gameCount).ToArray();
    }

    private static int FindBestTrainGroupCount<T>(
        IReadOnlyList<Guid> gameIds,
        IReadOnlyDictionary<Guid, List<T>> groupsById,
        int rowCount,
        double trainFraction)
    {
        var targetTrainRows = rowCount * trainFraction;
        var firstCandidate = gameIds.Count > 1 ? 1 : 0;
        var lastCandidate = gameIds.Count > 1 ? gameIds.Count - 1 : gameIds.Count;
        var bestGroupCount = firstCandidate;
        var bestDifference = double.PositiveInfinity;
        var candidateRows = 0;

        for (var groupCount = 0; groupCount <= gameIds.Count; groupCount++)
        {
            if (groupCount > 0)
                candidateRows += groupsById[gameIds[groupCount - 1]].Count;

            if (groupCount < firstCandidate || groupCount > lastCandidate)
                continue;

            var difference = Math.Abs(candidateRows - targetTrainRows);
            if (difference < bestDifference)
            {
                bestDifference = difference;
                bestGroupCount = groupCount;
            }
        }

        return bestGroupCount;
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        // We shuffle the index array in-place (Fisher-Yates)
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }
}