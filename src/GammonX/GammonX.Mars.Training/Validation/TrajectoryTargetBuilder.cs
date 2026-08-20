using GammonX.Mars.Training.Generator;

using GammonX.Mars.Training.Sidecars;

namespace GammonX.Mars.Training.Validation;

/// <summary>
/// Rebuilds training labels from recorded game trajectories using forward-view temporal-difference targets.
/// </summary>
/// <remarks>
/// Rows are grouped by game, ordered by turn, and matched with game metadata before labels are calculated.
/// The returned rows preserve the original input order; only their <see cref="TrainingDataRow.Label"/>
/// values are replaced.
/// </remarks>
public static class TrajectoryTargetBuilder
{
    /// <summary>
    /// Rebuilds the target labels for all supplied trajectory rows using the specified TD parameters.
    /// </summary>
    /// <param name="rows">The training data rows.</param>
    /// <param name="games">The game metadata.</param>
    /// <param name="lambda">The trace-decay parameter used by the forward-view target calculator.</param>
    /// <param name="gamma">The discount factor used by the forward-view target calculator.</param>
    /// <param name="headCount">The number of output heads to calculate, typically one or five.</param>
    /// <returns>The input rows in their original order with recalculated labels.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> or <paramref name="games"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when metadata is missing, row counts differ from game metadata, or trajectory keys are duplicated or missing.</exception>
    /// <remarks>
    /// Each game must have exactly <c>TotalTurns</c> rows. Labels are indexed by game ID and turn index,
    /// allowing the final projection back to preserve the input ordering even though calculation requires
    /// rows to be sorted chronologically. For a single-head result, only the first calculated value is retained.
    /// </remarks>
    public static IReadOnlyList<TrainingDataRow> Recalculate(
        IEnumerable<TrainingDataRow> rows,
        IEnumerable<GameMetadata> games,
        float lambda,
        float gamma = 1.0f,
        int headCount = ForwardViewTdCalculator.FullHeadCount)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(games);

        // Materialize once because the input is grouped for calculation and enumerated again for reconstruction.
        var sourceRows = rows.ToList();
        var gameMap = games.ToDictionary(game => game.GameId);
        var labels = new Dictionary<(Guid GameId, int TurnIndex), float[]>();

        foreach (var gameRows in sourceRows.GroupBy(row => row.GameId))
        {
            if (!gameMap.TryGetValue(gameRows.Key, out var game))
            {
                throw new InvalidDataException($"No game metadata exists for '{gameRows.Key}'.");
            }

            // Forward-view targets depend on chronological order within each game.
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

        // Restore the caller's original row order while replacing only the labels.
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
