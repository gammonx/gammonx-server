using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Sidecars;

/// <summary>
/// Represents a single position in a trajectory.
/// </summary>
/// <param name="TurnIndex">The index of the turn in the game.</param>
/// <param name="IsWhite">Indicates if the position is for the white player.</param>
/// <param name="Features">The feature vector representing the position.</param>
/// <param name="Prediction">The predicted values for the position.</param>
/// <param name="IsTerminal">Indicates if the position is terminal.</param>
public sealed record TrajectoryPosition(
    int TurnIndex,
    bool IsWhite,
    float[] Features,
    float[] Prediction,
    bool IsTerminal = false);

/// <summary>
/// Represents the metadata for a game trajectory.
/// </summary>
/// <param name="GameId">The unique identifier of the game.</param>
/// <param name="Positions">The list of positions in the trajectory.</param>
/// <param name="WinnerResult">The result for the winning player.</param>
/// <param name="LoserResult">The result for the losing player.</param>
/// <param name="WhiteWon">Indicates if the white player won the game.</param>
public sealed record GameTrajectory(
    Guid GameId,
    IReadOnlyList<TrajectoryPosition> Positions,
    GameResult WinnerResult,
    GameResult LoserResult,
    bool WhiteWon)
{
    /// <summary>
    /// Creates a new instance of <see cref="GameTrajectory"/> with the specified parameters.
    /// </summary>
    public GameMetadata Metadata => new(GameId, Positions.Count, WhiteWon, WinnerResult, LoserResult);
}

/// <summary>
/// Represents the metadata for a game.
/// </summary>
/// <param name="GameId">The unique identifier of the game.</param>
/// <param name="TotalTurns">The total number of turns in the game.</param>
/// <param name="WhiteWon">Indicates if the white player won the game.</param>
/// <param name="WinnerResult">The result for the winning player.</param>
/// <param name="LoserResult">The result for the losing player.</param>
public sealed record GameMetadata(
    Guid GameId,
    int TotalTurns,
    bool WhiteWon,
    GameResult WinnerResult,
    GameResult LoserResult);

/// <summary>
/// Represents a single position in a trajectory with additional metadata.
/// </summary>
/// <param name="GameId">The unique identifier of the game.</param>
/// <param name="Position">The position in the trajectory.</param>
/// <param name="Label">The label for the position.</param>
public sealed record TrainingDataRow(
    Guid GameId,
    TrajectoryPosition Position,
    float[] Label);
