using GammonX.Server.Services;

namespace GammonX.Server.Models;

/// <summary>
/// Describes the kind of obligation a given player owes to continue the active game/match.
/// </summary>
/// <param name="Action">The obligated action to perform.</param>
/// <param name="GameId">The related game id. May be null if the obligation is outside of the game context.</param>
/// <param name="TurnNumber">The turn number.</param>
public readonly record struct TurnObligation(
    TurnDeadlineAction Action,
    Guid? GameId = null,
    int? TurnNumber = null);

/// <summary>
/// Captures what a player owes and when it is due. The unique cancellation source is the registration's generation.
/// </summary>
/// <param name="Key">Key related to the given cancellation token.</param>
/// <param name="Token">Reference to the origin cancellation token.</param>
/// <param name="Obligation">Owed turn action.</param>
/// <param name="ConnectionId">Players connection id.</param>
/// <param name="WarnAt">DateTime when the player receives the first warning about expiring timer.</param>
/// <param name="ExpiresAt">Timer expiry.</param>
public sealed record TurnDeadline(
    CancellationTokenKey Key,
    CancellationTokenSource Token,
    TurnObligation Obligation,
    string ConnectionId,
    DateTimeOffset WarnAt,
    DateTimeOffset ExpiresAt);
