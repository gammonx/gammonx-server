using GammonX.Engine.Models;

using GammonX.Models;
using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models;

/// <summary>
/// Shares a private completed round until a caller requests mutable state.
/// Each match owns its wrapper. The shared backing round is never exposed.
/// </summary>
internal sealed class GameSessionSnapshot : IGameSessionModel, ICloneable
{
    private readonly IGameSessionModel _snapshot;

    private IGameSessionModel? _working;

    /// <summary>
    /// Provides read only access to the underlying game session snapshot.
    /// </summary>
    private IGameSessionModel Read => _working ?? _snapshot;

    /// <summary>
    /// Provides write access to an on-demand copy of the given game session.
    /// </summary>
    private IGameSessionModel Write => _working ??= (IGameSessionModel)((ICloneable)_snapshot).Clone();

    private GameSessionSnapshot(IGameSessionModel snapshot)
    {
        _snapshot = snapshot;
    }

    /// <summary>
    /// Creates the game session deep clone for writing access if necessary.
    /// </summary>
    /// <returns>Writable game session clone.</returns>
    internal IGameSessionModel Detach() => Write;

    internal static IGameSessionModel? CloneSession(IGameSessionModel? session)
    {
        if (session == null)
        {
            return null;
        }
        if (session is GameSessionSnapshot shared)
        {
            return (IGameSessionModel)shared.Clone();
        }

        var copy = (IGameSessionModel)((ICloneable)session).Clone();
        return copy.Phase == GamePhase.GameOver ? new GameSessionSnapshot(copy) : copy;
    }

    public object Clone()
    {
        // Once mutable state has escaped, it must be copied again before sharing.
        return _working == null ? new GameSessionSnapshot(_snapshot) : CloneSession(_working)!;
    }


    #region Immutable Properties

    // <inheritdoc />
    public GameModus Modus => Read.Modus;

    // <inheritdoc />
    public Guid MatchId => Read.MatchId;

    // <inheritdoc />
    public Guid Id => Read.Id;

    // <inheritdoc />
    public GamePhase Phase => Read.Phase;

    // <inheritdoc />
    public GameResultModel Result => Read.Result;

    // <inheritdoc />
    public Guid ActivePlayer => Read.ActivePlayer;

    // <inheritdoc />
    public Guid OtherPlayer => Read.OtherPlayer;

    // <inheritdoc />
    public int TurnNumber => Read.TurnNumber;

    // <inheritdoc />
    public DateTime? StartedAt => Read.StartedAt;

    // <inheritdoc />
    public DateTime? EndedAt => Read.EndedAt;

    // <inheritdoc />
    public long Duration => Read.Duration;

    #endregion Immutable Properties

    #region Mutable Properties

    // <inheritdoc />
    public DiceRollsModel DiceRolls => Write.DiceRolls;

    // <inheritdoc />
    public MoveSequences MoveSequences => Write.MoveSequences;

    // <inheritdoc />
    public IBoardModel BoardModel => Write.BoardModel;

    #endregion Mutable Properties

    #region Immutable Access

    // <inheritdoc />
    public bool CanUndoLastMove(Guid callingPlayerId) => Read.CanUndoLastMove(callingPlayerId);

    // <inheritdoc />
    public bool GameOver(bool isWhite) => Read.GameOver(isWhite);

    // <inheritdoc />
    public GameRoundContract ToContract(int gameRoundIndex) => Read.ToContract(gameRoundIndex);

    // <inheritdoc />
    public EventGameStatePayload ToPayload(Guid playerId, string[] allowedCommands, bool inverted) => Read.ToPayload(playerId, allowedCommands, inverted);

    #endregion Immutable Access

    #region Mutable Access

    // <inheritdoc />
    public void StartGame(Guid activePlayer, Guid otherPlayer) => Write.StartGame(activePlayer, otherPlayer);

    // <inheritdoc />
    public void StopGame(GameResultModel result) => Write.StopGame(result);

    // <inheritdoc />
    public void NextTurn(Guid playerId) => Write.NextTurn(playerId);

    // <inheritdoc />
    public void SetDiceRolls(int[] rolls, bool isWhite) => Write.SetDiceRolls(rolls, isWhite);

    // <inheritdoc />
    public int[] RollDices(int amount) => Write.RollDices(amount);

    // <inheritdoc />
    public void RollDices(Guid playerId, bool isWhite) => Write.RollDices(playerId, isWhite);

    // <inheritdoc />
    public void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite) => Write.MoveCheckers(callingPlayerId, from, to, isWhite);

    // <inheritdoc />
    public void UndoLastMove(Guid callingPlayerId, bool isWhite) => Write.UndoLastMove(callingPlayerId, isWhite);

    // <inheritdoc />
    public IGameHistory GetHistory(Guid player1, Guid player2) => Write.GetHistory(player1, player2);

    #endregion Mutable Access
}
