namespace GammonX.Mars.Training;

/// <summary>
/// Provides the capability to rebuild the target labels for a given lambda and gamma value.
/// </summary>
public static class TrajectoryTargetBuilder
{
    /// <summary>
    /// Rebuilds the target labels for a given trajectory, lambda, and gamma value.
    /// </summary>
    /// <param name="rows">The training data rows.</param>
    /// <param name="games">The game metadata.</param>
    /// <param name="lambda">The lambda value.</param>
    /// <param name="gamma">The gamma value.</param>
    /// <param name="headCount">The head count.</param>
    /// <returns>The recalculated training data rows.</returns>
    /// <exception cref="InvalidDataException">Throws when the game metadata is missing or the trajectory rows are inconsistent.</exception>
    public static IReadOnlyList<TrainingDataRow> Recalculate(
        IEnumerable<TrainingDataRow> rows,
        IEnumerable<GameMetadata> games,
        float lambda,
        float gamma = 1.0f,
        int headCount = ForwardViewTdCalculator.FullHeadCount)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(games);

        var sourceRows = rows.ToList();
        var gameMap = games.ToDictionary(game => game.GameId);
        var labels = new Dictionary<(Guid GameId, int TurnIndex), float[]>();

        foreach (var gameRows in sourceRows.GroupBy(row => row.GameId))
        {
            if (!gameMap.TryGetValue(gameRows.Key, out var game))
                throw new InvalidDataException($"No game metadata exists for '{gameRows.Key}'.");

            var orderedRows = gameRows
                .OrderBy(row => row.Position.TurnIndex)
                .ToArray();
            if (orderedRows.Length != game.TotalTurns)
            {
                throw new InvalidDataException(
                    $"Game '{game.GameId}' contains {orderedRows.Length} trajectory rows; expected {game.TotalTurns}.");
            }

            var trajectory = new GameTrajectory(
                game.GameId,
                orderedRows.Select(row => row.Position).ToArray(),
                game.WinnerResult,
                game.LoserResult,
                game.WhiteWon);
            var gameLabels = ForwardViewTdCalculator.Calculate(trajectory, lambda, gamma, headCount);

            for (var index = 0; index < orderedRows.Length; index++)
            {
                var key = (game.GameId, orderedRows[index].Position.TurnIndex);
                if (!labels.TryAdd(key, gameLabels[index]))
                    throw new InvalidDataException($"Duplicate trajectory row '{game.GameId}/{key.TurnIndex}'.");
            }
        }

        return sourceRows
            .Select(row =>
            {
                var key = (row.GameId, row.Position.TurnIndex);
                return labels.TryGetValue(key, out var label)
                    ? row with { Label = headCount == 1 ? [label[0]] : label }
                    : throw new InvalidDataException($"No calculated label exists for '{row.GameId}/{row.Position.TurnIndex}'.");
            })
            .ToList();
    }
}
