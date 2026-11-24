using GammonX.Diagnostics.Server;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Extensions;
using GammonX.Server.Models;
using GammonX.Server.Services;

using static GammonX.Server.Services.MatchLifecycleService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Serilog;

namespace GammonX.Server
{
    /// <summary>
    /// SignalR hub for managing match lobbies/sessions and handling the game flow.
    /// </summary>
    [Authorize(Policy = "AuthPolicy")]
    public class MatchLobbyHub : Hub
    {
        /// <summary>
        /// Stores the timeouts for players which are expected to do an action within a certain time frame
        /// - active player to end his turn
        /// - join lobby
        /// - start match
        /// - start next match
        /// - accept double offer
        /// - disconnect timeouts
        /// </summary>
        private readonly ICancellationTokenService _cancellationTokenService;

        private readonly IMatchmakingService _matchmakingService;

        private readonly MatchSessionRepository _matchRepository;

        private readonly PlayerConnectionRepository _playerConnectionRepository;

        private readonly IDiceService _diceService;

        internal MatchLifecycleService Lifecycle { get; }

        public MatchLobbyHub(IServiceProvider serviceProvider)
        {
            _matchmakingService = serviceProvider.GetRequiredService<IMatchmakingService>();
            _matchRepository = serviceProvider.GetRequiredService<MatchSessionRepository>();
            Lifecycle = serviceProvider.GetRequiredService<MatchLifecycleService>();
            _diceService = serviceProvider.GetRequiredService<IDiceServiceFactory>().Create(DiceServiceType.Crypto);
            _playerConnectionRepository = serviceProvider.GetRequiredService<PlayerConnectionRepository>();
            _cancellationTokenService = serviceProvider.GetRequiredService<ICancellationTokenService>();
        }

        #region Overrides

        // <inheritdoc />
        public override async Task OnConnectedAsync()
        {
            try
            {
                var playerId = Context.GetPlayerId();
                var matchId = Context.GetMatchId();

                if (playerId.HasValue && matchId.HasValue)
                {
                    await using var matchLock = await _matchRepository.AcquireStateAsync(matchId.Value, Context.ConnectionAborted);
                    var tokenKey = ConstructTokenKey(playerId.Value, matchId.Value, CancellationTokenCategory.Disconnect);
                    // We check if the connecting player has a disconnect timer running
                    if (_cancellationTokenService.TryGet(tokenKey, out var cts) && cts != null)
                    {
                        // Charge the completed absence before AddOrUpdate changes LastSeenUtc.
                        // The canceled timer must not mutate connection state later.
                        var disconnectedPlayer = _playerConnectionRepository.Get(playerId.Value);
                        if (disconnectedPlayer != null && !cts.IsCancellationRequested)
                        {
                            var elapsed = Lifecycle.UtcNow - disconnectedPlayer.LastSeenUtc;
                            if (elapsed >= disconnectedPlayer.DisconnectGracePeriod)
                            {
                                // An expired absence cannot be rescued while we wait for the other deadline.
                                await Lifecycle.ResolveDisconnectExpiryAsync(matchLock, playerId.Value, matchId.Value, disconnectedPlayer.ConnectionId!, cts);
                                var payload = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                                await Lifecycle.SendToClientAsync(Context.ConnectionId, ServerEventTypes.ForceDisconnectEvent, payload);
                                return;
                            }
                            disconnectedPlayer.DisconnectGracePeriod = (disconnectedPlayer.DisconnectGracePeriod - elapsed).Max(TimeSpan.Zero);
                        }
                        _cancellationTokenService.Cancel(tokenKey);
                    }

                    var connectionId = Context.ConnectionId;
                    _playerConnectionRepository.AddOrUpdate(playerId.Value, connectionId);

                    var matchSession = matchLock.Get();
                    // The opponent may have created the session before this lobby player joined it.
                    if (matchSession != null && (matchSession.Player1.Id == playerId.Value || matchSession.Player2.Id == playerId.Value))
                    {
                        await Groups.AddToGroupAsync(connectionId, ConstructGroupName(matchSession.Id));
                        var otherPlayerId = matchLock.GetOtherPlayerId(playerId.Value);
                        var otherKey = ConstructTokenKey(otherPlayerId, matchSession.Id, CancellationTokenCategory.Disconnect);
                        // We check if the other player has a disconnect timer running
                        if (_cancellationTokenService.TryGet(otherKey, out var otherTimer) && otherTimer != null)
                        {
                            var otherConnection = _playerConnectionRepository.Get(otherPlayerId);
                            // We check if the other player surpassed their grace period
                            if (otherConnection != null && Lifecycle.UtcNow - otherConnection.LastSeenUtc >= otherConnection.DisconnectGracePeriod)
                            {
                                await Lifecycle.ResolveDisconnectExpiryAsync(matchLock, otherPlayerId, matchSession.Id, otherConnection.ConnectionId!, otherTimer);
                            }
                            else if (otherConnection != null)
                            {
                                var stateCommand = matchSession.GetGameSession(matchSession.GameRound) == null ? ServerCommands.MatchStateCommand : ServerCommands.GameStateCommand;
                                var connectedPayload = new EventMatchLobbyPayload(matchSession.Id, matchSession.Player1.Id, matchSession.Player2.Id, stateCommand);
                                var connectedResponse = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, connectedPayload);
                                // We send to group that the player reconnected
                                await Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, connectedResponse);
                                // We send to player that the other player is still disconnected
                                var otherDisconnectPayload = EventDisconnectedPayload.From(otherConnection);
                                var otherDisconnectResponse = new EventResponseContract<EventDisconnectedPayload>(ServerEventTypes.PlayerDisconnectedEvent, otherDisconnectPayload);
                                await Lifecycle.SendToClientAsync(connectionId, ServerEventTypes.PlayerDisconnectedEvent, otherDisconnectResponse);
                            }
                            // We keep turn deadlines paused until the other human returns or forfeits.
                            return;
                        }

                        // we try to reconnect to an existing match and resend the state
                        var activeGameSession = matchSession.GetGameSession(matchSession.GameRound);
                        if (activeGameSession != null)
                        {
                            var payload = new EventMatchLobbyPayload(matchSession.Id, matchSession.Player1.Id, matchSession.Player2.Id, ServerCommands.GameStateCommand);
                            var contract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, payload);
                            // we try to resume the game flow after the player disconnected and the bot is at its turn
                            if (activeGameSession.Phase != GamePhase.GameOver && matchLock.IsBotPlayer(activeGameSession.ActivePlayer))
                            {
                                // The player is reconnecting. Bot requests use the players ConnectionAborted token,
                                // so their disconnect may have canceled a bot action after its preceding state was committed.
                                // Resume the bots interrupted turn from the committed game state.
                                ServerMetrics.BotRecoveries.Add(1);
                                await Lifecycle.PerformBotTurnAsync(matchLock, matchSession, activeGameSession.ActivePlayer, Context.ConnectionAborted,
                                    () => Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract));
                                return;
                            }

                            await Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract);
                            if (activeGameSession.Phase != GamePhase.GameOver)
                            {
                                // A player owes the next move or cube response, restore their response deadline.
                                Lifecycle.StartTurnTimer(matchSession, activeGameSession.ActivePlayer);
                            }
                            else if (!matchSession.IsMatchOver())
                            {
                                // Restore deadlines only for outstanding acceptances. The bot accepts automatically.
                                if (!matchSession.Player1.NextGameAccepted)
                                {
                                    Lifecycle.StartTurnTimer(matchSession, matchSession.Player1.Id);
                                }

                                if (!matchSession.Player2.IsBot && !matchSession.Player2.NextGameAccepted)
                                {
                                    Lifecycle.StartTurnTimer(matchSession, matchSession.Player2.Id);
                                }
                            }
                        }
                        else
                        {
                            // we handle a first time connect on before match has started
                            var payload = new EventMatchLobbyPayload(matchSession.Id, matchSession.Player1.Id, matchSession.Player2.Id, ServerCommands.MatchStateCommand);
                            var contract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, payload);
                            await Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract);

                            if (matchSession.Player1.Claimed && matchSession.Player2.Claimed && !matchSession.IsMatchOver())
                            {
                                // Both seats are assigned, but the first game has not started. Restore player start deadlines;
                                // the bots opening roll and acceptance are supplied when the player starts the match.
                                Lifecycle.StartTurnTimer(matchSession, matchSession.Player1.Id);
                                if (!matchLock.IsBotPlayer(matchSession.Player2.Id))
                                {
                                    Lifecycle.StartTurnTimer(matchSession, matchSession.Player2.Id);
                                }
                            }
                            else if (matchSession.IsMatchOver())
                            {
                                // if no game is active and no next game can be started, we expect the match to be finished
                            }
                            else
                            {
                                throw new InvalidOperationException("A player reconnected to a match but no active game session or possibility to start a next game was found. This match state was not expected.");
                            }
                        }

                        matchLock.Commit();
                    }
                    else
                    {
                        if (_matchmakingService.TryFindMatchLobby(matchId.Value, out var matchLobby) && matchLobby != null)
                        {
                            // Lobby exists — player disconnected before joining. Give them a deadline.
                            var payload = new EventMatchLobbyPayload(matchId.Value, playerId.Value, null, ServerCommands.JoinMatchCommand);
                            var contract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, payload);
                            await Groups.AddToGroupAsync(connectionId, ConstructGroupName(matchId.Value));
                            await Lifecycle.SendToClientAsync(connectionId, ServerEventTypes.PlayerConnectedEvent, contract);
                            Lifecycle.StartJoinTimer(matchLobby, matchSession, playerId.Value, connectionId);
                        }
                        else
                        {
                            // Match is gone (forfeited, cleaned up). Nothing to join.
                            var emptyResponse = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                            await Lifecycle.SendToClientAsync(connectionId, ServerEventTypes.ForceDisconnectEvent, emptyResponse);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Failed to obtain the required claims from the authorization token");
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while handling player connect.");
            }
        }

        // <inheritdoc />
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var playerId = Context.GetPlayerId();
                var matchId = Context.GetMatchId();

                if (playerId.HasValue && matchId.HasValue)
                {
                    await using var matchLock = await _matchRepository.AcquireStateAsync(matchId.Value);
                    var playerConnection = _playerConnectionRepository.Get(playerId.Value);

                    // SignalR can deliver the old sockets callback after its replacement connected.
                    var sameConnection = string.Equals(playerConnection?.ConnectionId, Context.ConnectionId, StringComparison.Ordinal);
                    if (playerConnection != null && !playerConnection.IsBot && sameConnection)
                    {
                        var disconnectKey = ConstructTokenKey(playerId.Value, matchId.Value, CancellationTokenCategory.Disconnect);
                        // We check if a disconnected timer already runs for the given player and match
                        if (_cancellationTokenService.TryGet(disconnectKey, out _))
                        {
                            return;
                        }

                        var matchSession = matchLock.Get();
                        var participant = matchSession?.Player1.Id == playerId.Value || matchSession?.Player2.Id == playerId.Value;
                        if (matchSession != null && !participant)
                        {
                            return;
                        }

                        Lifecycle.CancelTimersFor(matchId.Value, CancellationTokenCategory.Turn);
                        Lifecycle.CancelTimersFor(matchId.Value, CancellationTokenCategory.BotRecovery);
                        playerConnection.LastSeenUtc = Lifecycle.UtcNow;

                        _ = Lifecycle.StartDisconnectGraceTimer(playerConnection, matchId.Value);
                        var payload = EventDisconnectedPayload.From(playerConnection);
                        var contract = new EventResponseContract<EventDisconnectedPayload>(ServerEventTypes.PlayerDisconnectedEvent, payload);
                        await Lifecycle.SendToGroupAsync(ConstructGroupName(matchId.Value), ServerEventTypes.PlayerDisconnectedEvent, contract);
                    }
                    else
                    {
                        Log.Debug("Ignoring disconnect for player {playerId} in match {matchId}; the connection is no longer current.", playerId.Value, matchId.Value);
                        return;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Failed to obtain the required claims from the authorization token");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while handling player disconnect.");
            }
        }

        #endregion Overrides

        #region Commands

        /// <summary>
        /// The given player with ID <paramref name="playerId"/> joins the match <paramref name="matchId"/>.
        /// </summary>
        /// <remarks>
        /// First player to join the match receives the <see cref="ServerEventTypes.MatchLobbyWaitingEvent"/>.
        /// If both players joined the match, the <see cref="ServerEventTypes.MatchLobbyFoundEvent"/> is sent to both players.
        /// Afterward, the clients also receive the <see cref="ServerEventTypes.MatchWaitingForStartEvent"/> with the initial match state.
        /// </remarks>
        /// <param name="matchId">Match to join.</param>
        /// <param name="playerId">Player who wants to join the match.</param>
        /// <returns>Task to be awaited.</returns>
        [HubMethodName(ServerCommands.JoinMatchCommand)]
        public async Task JoinMatchAsync(string matchId, string playerId)
        {
            try
            {
                // TODO: we should compare the player id with the auth caller player id
                if (!Guid.TryParse(matchId, out var matchGuid))
                    await SendErrorEventAsync("LOBBY_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);

                if (!Guid.TryParse(playerId, out var playerIdGuid))
                    await SendErrorEventAsync("LOBBY_ERROR", $"The given playerId '{playerIdGuid}'  is not a valid GUID.", Context.ConnectionId);

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                if (_matchmakingService.TryFindMatchLobby(matchGuid, out var matchLobby) && matchLobby != null)
                {
                    if (matchLobby.Status != QueueEntryStatus.OpponentFound)
                    {
                        throw new InvalidOperationException($"The match with given id '{matchId}' cannot be started. No opponent found yet.");
                    }

                    // we need to preserve this for compatibility reason when no bearer token with claims exist
                    var matchIdClaim = Context.GetMatchId();
                    if (!matchIdClaim.HasValue)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, ConstructGroupName(matchLobby.MatchId));
                    }

                    var matchSession = matchLock.GetOrCreate(matchLobby.QueueKey);
                    if (matchLobby.Player1.Id == playerIdGuid)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, matchLobby.Player1.Id, CancellationTokenCategory.Turn);
                        matchLobby.Player1.SetConnectionId(Context.ConnectionId);
                        ArgumentNullException.ThrowIfNull(matchLobby.Player1.ConnectionId, nameof(matchLobby.Player1.ConnectionId));
                        matchSession.JoinSession(matchLobby.Player1);

                        if (matchLobby.Player2 is { IsBot: true } botOpponent)
                        {
                            matchSession.JoinSession(botOpponent);
                        }
                        else if (matchLobby.QueueKey.MatchModus == MatchModus.Bot)
                        {
                            // in a bot game, the player 2 is always the "bot"
                            // we assign a fixed GUID to the bot player in order to track his stats and rating in the db
                            // we recycle the same "bot connection" for all bot matches
                            var botId = WellKnownBotPlayers.GetBotPlayerId(matchLobby.QueueKey.BotLevel);
                            // All bot levels share a connection marker; their player IDs remain distinct for ratings.
                            var botPlayer = _playerConnectionRepository.GetOrCreate(botId);
                            botPlayer.SetConnectionId(WellKnownBotPlayers.ConnectionId);
                            matchSession.JoinSession(botPlayer);
                        }
                    }

                    if (matchLobby.Player2?.Id == playerIdGuid)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, matchLobby.Player2.Id, CancellationTokenCategory.Turn);
                        matchLobby.Player2.SetConnectionId(Context.ConnectionId);
                        ArgumentNullException.ThrowIfNull(matchLobby.Player2.ConnectionId, nameof(matchLobby.Player2.ConnectionId));
                        matchSession.JoinSession(matchLobby.Player2);
                    }

                    if (matchSession.Player1.Id != Guid.Empty && matchSession.Player2.Id != Guid.Empty)
                    {
                        var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchSession.Player1.Id, matchSession.Player2.Id);
                        var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyPayload);
                        matchLock.Commit();
                        await Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.MatchLobbyFoundEvent, matchLobbyContract);
                        await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchWaitingForStartEvent, matchSession);
                        // we set a timeout for the players to start the match
                        Lifecycle.StartTurnTimer(matchSession, matchSession.Player1.Id);
                        if (!matchLock.IsBotPlayer(matchSession.Player2.Id))
                        {
                            Lifecycle.StartTurnTimer(matchSession, matchSession.Player2.Id);
                        }
                    }
                    else
                    {
                        var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.Id, null);
                        var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyPayload);
                        matchLock.Commit();
                        await Lifecycle.SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyContract);
                    }
                }
                else
                {
                    await SendErrorEventAsync("LOBBY_ERROR", "No match lobby was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("LOBBY_ERROR", $"An error occurred while trying to join the match lobby: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// Starts the match with the given <paramref name="matchId"/> and implicitly the first game.
        /// Both players roll their starting dice. If both players roll the same value, the call to start the match has to be repeated.
        /// First player to start the match receives the <see cref="ServerEventTypes.MatchWaitingEvent"/> with the match state.
        /// The game phase advances to <see cref="GamePhase.Rolling"/>.
        /// If both players accepted the match, the <see cref="ServerEventTypes.MatchStartedEvent"/> is sent to both players
        /// containing the initial game state including the first pair of dices for the player who won the opening roll.
        /// </summary>
        /// <param name="matchId">Match containing the game to start.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.StartMatchCommand)]
        public async Task StartMatchAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("START_MATCH_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var player1 = matchSession.Player1;
                    var player2 = matchSession.Player2;
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);

                    if (!matchSession.IsAllowedCommand(callingPlayerId, ServerCommands.StartMatchCommand))
                    {
                        await SendErrorEventAsync("START_MATCH_ERROR", "You cannot start the match at this moment.", Context.ConnectionId);
                        return;
                    }

                    // both players roll their starting dice
                    var startingRolls = _diceService.Roll(1, 6);
                    SetStartRollForPlayer(matchSession, startingRolls[0]);

                    if (player1.ConnectionId == Context.ConnectionId)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, player1.Id, CancellationTokenCategory.Turn);
                        player1.AcceptNextGame();

                        if (matchSession.Player2.IsBot)
                        {
                            // in a bot game, the player 2 is always the "bot"
                            var botStartingRoll = _diceService.Roll(1, 6);
                            player2.SetStartDiceRoll(botStartingRoll[0]);
                            player2.AcceptNextGame();
                        }
                    }

                    if (player2.ConnectionId == Context.ConnectionId)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, player2.Id, CancellationTokenCategory.Turn);
                        player2.AcceptNextGame();
                    }

                    if (player1.StartDiceRoll != null && player2.StartDiceRoll != null)
                    {
                        // we check if both players roll the same starting dice
                        if (player1.StartDiceRoll == player2.StartDiceRoll)
                        {
                            // re-roll is required in this case
                            matchLock.Commit();
                            // Both human players need a new deadline after their opening timers were canceled.
                            Lifecycle.StartTurnTimer(matchSession, player1.Id);
                            if (!player2.IsBot)
                            {
                                Lifecycle.StartTurnTimer(matchSession, player2.Id);
                            }

                            await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchWaitingForStartEvent, matchSession);
                        }
                        else
                        {
                            if (matchSession.CanStartNextGame())
                            {
                                var startingPlayerId = GetStartingPlayerId(matchSession);
                                var gameSession = matchSession.StartMatch(startingPlayerId);

                                // A retry can resolve the starting roll tie while the opponent still has a reroll timer.
                                Lifecycle.CancelTimersFor(matchSession.Id, CancellationTokenCategory.Turn);

                                var isWhite = matchLock.IsWhite(startingPlayerId);
                                // we set the opening dice rolls for the starting player
                                gameSession.SetDiceRolls([player1.StartDiceRoll.Value, player2.StartDiceRoll.Value], isWhite);

                                if (!matchLock.Commit())
                                {
                                    throw new InvalidOperationException("Could not commit the match start.");
                                }

                                _matchmakingService.TryRemoveMatchLobby(matchSession.Id);

                                if (matchLock.IsBotPlayer(startingPlayerId))
                                {
                                    await Lifecycle.PerformBotTurnAsync(matchLock, matchSession, startingPlayerId, Context.ConnectionAborted, async () =>
                                    {
                                        await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchStartedEvent, matchSession);
                                        await Lifecycle.SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
                                    });
                                }
                                else
                                {
                                    Lifecycle.StartTurnTimer(matchSession, startingPlayerId);
                                    await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchStartedEvent, matchSession);
                                    await Lifecycle.SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
                                }
                            }
                        }
                    }
                    else
                    {
                        // we wait for other player to roll his starting dice
                        matchLock.Commit();
                        var matchPayload = matchSession.ToPayload(callingPlayerId);
                        var matchContract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.MatchWaitingEvent, matchPayload);
                        await SendToCallerAsync(ServerEventTypes.MatchWaitingEvent, matchContract);
                    }
                }
                else
                {
                    await SendErrorEventAsync("START_MATCH_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (BotServiceException bse)
            {
                await SendErrorEventAsync("BOT_SERVICE_ERROR", $"An error occurred while trying to start the match in the bot service '{bse.BotName}': '{bse.Message}'", Context.ConnectionId, bse);
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("START_MATCH_ERROR", $"An error occurred while trying to start the match: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// Starts the next game round of the match with the given <paramref name="matchId"/>.
        /// First player to start the game receives the <see cref="ServerEventTypes.GameWaitingEvent"/> with the match state.
        /// The game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
        /// If both players accepted the next game, the <see cref="ServerEventTypes.GameStartedEvent"/> is sent to both players
        /// containing the initial game state and the first player to roll his dices.
        /// </summary>
        /// <param name="matchId">Match containing the game to start.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.StartGameCommand)]
        public async Task StartGameAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("START_GAME_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    if (!matchSession.IsAllowedCommand(callingPlayerId, ServerCommands.StartGameCommand))
                    {
                        await SendErrorEventAsync("START_GAME_ERROR", "You cannot start the next game at this moment.", Context.ConnectionId);
                        return;
                    }

                    if (matchSession.Player1.ConnectionId == Context.ConnectionId)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, matchSession.Player1.Id, CancellationTokenCategory.Turn);
                        matchSession.Player1.AcceptNextGame();

                        if (matchSession.Player2.IsBot)
                        {
                            // in a bot game, the player 2 is always the "bot"
                            matchSession.Player2.AcceptNextGame();
                        }
                    }

                    if (matchSession.Player2.ConnectionId == Context.ConnectionId)
                    {
                        Lifecycle.CancelTimersFor(matchSession.Id, matchSession.Player2.Id, CancellationTokenCategory.Turn);
                        matchSession.Player2.AcceptNextGame();
                    }

                    if (matchSession.CanStartNextGame())
                    {
                        var startingPlayerId = GetStartingPlayerId(matchSession);
                        matchSession.StartNextGame(startingPlayerId);
                        matchLock.Commit();
                        if (matchLock.IsBotPlayer(startingPlayerId))
                        {
                            // we must send a fresh game state here because bot may offer double at game start
                            await Lifecycle.PerformBotTurnAsync(matchLock, matchSession, startingPlayerId, Context.ConnectionAborted,
                                () => Lifecycle.SendGameState(ServerEventTypes.GameStartedEvent, matchSession));
                        }
                        else
                        {
                            Lifecycle.StartTurnTimer(matchSession, startingPlayerId);
                            await Lifecycle.SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
                        }
                    }
                    else
                    {
                        matchLock.Commit();
                        callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                        var matchPayload = matchSession.ToPayload(callingPlayerId);
                        var matchContract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.GameWaitingEvent, matchPayload);
                        await SendToCallerAsync(ServerEventTypes.GameWaitingEvent, matchContract);
                    }
                }
                else
                {
                    await SendErrorEventAsync("START_GAME_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (BotServiceException bse)
            {
                await SendErrorEventAsync("BOT_SERVICE_ERROR", $"An error occurred while trying to start the game in the bot service '{bse.BotName}': '{bse.Message}'", Context.ConnectionId, bse);
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("START_GAME_ERROR", $"An error occurred while trying to start the game: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The active turn player rolls the dices for the current game round. The game phase advances to <see cref="GamePhase.Rolling"/>.
        /// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 (black checkers) 
        /// an inverted board state and inverted move sequences.
        /// </summary>
        /// <remarks>
        /// The player which is not active receives the <see cref="GamePhase.WaitingForOpponent"/>.
        /// If legal moves are found, the calling player can execute a move command. Otherwise, he has to end his turn.
        /// </remarks>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.RollCommand)]
        public async Task RollAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("ROLL_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    await Lifecycle.PerformRollAsync(matchLock, matchSession, callingPlayerId);
                }
                else
                {
                    await SendErrorEventAsync("ROLL_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("ROLL_ERROR", $"An error occurred while trying to roll the dices: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The active turn player moves his checkers from <paramref name="from"/> to <paramref name="to"/>.
        /// The game phase advances to <see cref="GamePhase.Moving"/>.
        /// If some dices are not used up, the active player can continue to move his checkers.
        /// If the given from to move uses up all rolled dices, The game phase advances to <see cref="GamePhase.WaitingForEndTurn"/>.
        /// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 an inverted board state.
        /// </summary>
        /// <remarks>
        /// An internal validation happens if the given from to move is based on the rolled dices and is actually a legal move.
        /// The player which is not active receives the <see cref="GamePhase.WaitingForOpponent"/> and an inverted board state.
        /// </remarks>
        /// <param name="matchId">ID of the match.</param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.MoveCommand)]
        public async Task MoveAsync(string matchId, int from, int to)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("MOVE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    // calling and active player must be the same
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    var isWhite = matchLock.IsWhite(callingPlayerId);
                    if (!isWhite)
                    {
                        // we need to invert the from/to move back for black checkers
                        // because the client always plays and acts with white checkers
                        var invertedMove = InvertFromToMove(matchSession, from, to);
                        from = invertedMove.Item1;
                        to = invertedMove.Item2;

                    }

                    await Lifecycle.PerformMoveAsync(matchLock, matchSession, callingPlayerId, from, to);
                }
                else
                {
                    await SendErrorEventAsync("MOVE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("MOVE_ERROR", $"An error occurred while trying to move checkers: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The active player undoes, if possible, his last move in his move stack.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.UndoMoveCommand)]
        public async Task UndoMoveAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("UNDO_MOVE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);

                    if (!matchSession.CanUndoLastMove(callingPlayerId))
                    {
                        await SendErrorEventAsync("UNDO_MOVE_ERROR", "You cannot undo a move at this moment.", Context.ConnectionId);
                        return;
                    }

                    matchSession.UndoLastMove(callingPlayerId);
                    matchLock.Commit();
                    await Lifecycle.SendGameState(ServerEventTypes.GameStateEvent, matchSession);
                }
                else
                {
                    await SendErrorEventAsync("UNDO_MOVE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("UNDO_MOVE_ERROR", $"An error occurred while trying to undo the last move: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The active player ends his turn. The active player is switched to the other player.
        /// The former active players game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
        /// The new active players game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
        /// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 an inverted board state.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.EndTurnCommand)]
        public async Task EndTurnAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("END_TURN_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    var otherPlayerId = matchLock.GetOtherPlayerId(callingPlayerId);

                    if (!matchSession.CanEndTurn(callingPlayerId))
                    {
                        await SendErrorEventAsync("END_TURN_ERROR", "You cannot end your turn at this moment.", Context.ConnectionId);
                        return;
                    }

                    Lifecycle.CancelTimersFor(matchSession.Id, callingPlayerId, CancellationTokenCategory.Turn);
                    matchSession.EndTurn(callingPlayerId);
                    matchLock.Commit();

                    if (matchLock.IsBotPlayer(otherPlayerId))
                    {
                        await Lifecycle.PerformBotTurnAsync(matchLock, matchSession, otherPlayerId, Context.ConnectionAborted);
                    }
                    else
                    {
                        Lifecycle.StartTurnTimer(matchSession, otherPlayerId);
                        await Lifecycle.SendGameState(ServerEventTypes.GameStateEvent, matchSession);
                    }
                }
                else
                {
                    await SendErrorEventAsync("END_TURN_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (BotServiceException bse)
            {
                await SendErrorEventAsync("BOT_SERVICE_ERROR", $"An error occurred while trying to end the turn in the bot service '{bse.BotName}': '{bse.Message}'", Context.ConnectionId, bse);
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("END_TURN_ERROR", $"An error occurred while trying to end the turn: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The calling player resigns the match with given <paramref name="matchId"/>.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>Task to be awaited.</returns>
        [HubMethodName(ServerCommands.ResignMatchCommand)]
        public async Task ResignMatchAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("RESIGN_MATCH_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    matchSession.ResignMatch(callingPlayerId);
                    matchLock.Commit();
                    await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
                }
                else
                {
                    await SendErrorEventAsync("RESIGN_MATCH_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("RESIGN_MATCH_ERROR", $"An error occurred while resigning the game: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The calling player resigns the active game round of the match with given <paramref name="matchId"/>.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>Task to be awaited.</returns>
        [HubMethodName(ServerCommands.ResignGameCommand)]
        public async Task ResignGameAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("RESIGN_GAME_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    matchSession.ResignGame(callingPlayerId);
                    matchLock.Commit();
                    // check if this was the last game round of the match
                    if (matchSession.IsMatchOver())
                    {
                        await Lifecycle.SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
                    }
                    else
                    {
                        await Lifecycle.SendMatchState(matchLock, ServerEventTypes.GameEndedEvent, matchSession);
                    }
                }
                else
                {
                    await SendErrorEventAsync("RESIGN_GAME_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("RESIGN_GAME_ERROR", $"An error occurred while resigning the game: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        [HubMethodName(ServerCommands.GameStateCommand)]
        public async Task GameStateAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("GET_GAME_STATE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                var payload = matchLock.ReadGameState(callingPlayerId);
                if (payload != null)
                {
                    var contract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.GameStateEvent, payload);
                    await Lifecycle.SendToClientAsync(Context.ConnectionId, ServerEventTypes.GameStateEvent, contract, Context.ConnectionAborted);
                }
                else
                {
                    await SendErrorEventAsync("GET_GAME_STATE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("GET_GAME_STATE_ERROR", $"An error occurred while retrieving the game state: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        [HubMethodName(ServerCommands.MatchStateCommand)]
        public async Task MatchStateAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("GET_MATCH_STATE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                var payload = matchLock.ReadMatchState(callingPlayerId);
                if (payload != null)
                {
                    payload.AppendRatingUrl(callingPlayerId, ServerEventTypes.MatchStateEvent);
                    var contract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.MatchStateEvent, payload);
                    await Lifecycle.SendToClientAsync(Context.ConnectionId, ServerEventTypes.MatchStateEvent, contract, Context.ConnectionAborted);
                }
                else
                {
                    await SendErrorEventAsync("GET_MATCH_STATE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("GET_MATCH_STATE_ERROR", $"An error occurred while retrieving the match state: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        #region Doubling Cube Commands

        /// <summary>
        /// The active doubling cube owner offers a double to his opponent who can accept or decline it.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>Task to be awaited.</returns>
        [HubMethodName(ServerCommands.OfferDoubleCommand)]
        public async Task OfferDoubleAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null && matchSession is IDoubleCubeMatchSession doubleCubeSession)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);

                    if (!doubleCubeSession.CanOfferDouble(callingPlayerId))
                    {
                        await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The player with the id '{callingPlayerId}' cannot offer a double at the moment.", Context.ConnectionId);
                        return;
                    }

                    await Lifecycle.PerformOfferDoubleAsync(matchLock, matchSession, doubleCubeSession, callingPlayerId, Context.ConnectionAborted);
                }
                else
                {
                    await SendErrorEventAsync("OFFER_DOUBLE_ERROR", "No double cube match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (BotServiceException bse)
            {
                await SendErrorEventAsync("BOT_SERVICE_ERROR", $"An error occurred while a double was offered in the bot service '{bse.BotName}': '{bse.Message}'", Context.ConnectionId, bse);
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"An error occurred while a double was offered: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The player who got the offer for a doubling cube increase accepted it.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.AcceptDoubleCommand)]
        public async Task AcceptDoubleAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
                    {
                        await Lifecycle.PerformAcceptDoubleAsync(matchLock, matchSession, doubleCubeSession, callingPlayerId, Context.ConnectionAborted);
                    }
                    else
                    {
                        await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
                    }
                }
                else
                {
                    await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (BotServiceException bse)
            {
                await SendErrorEventAsync("BOT_SERVICE_ERROR", $"An error occurred while trying to accepting a double offer in the bot service '{bse.BotName}': '{bse.Message}'", Context.ConnectionId, bse);
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"An error occurred while accepting a double offer: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        /// <summary>
        /// The player who got the offer for a doubling cube declines it and loses the active game round.
        /// </summary>
        /// <param name="matchId">ID of the match.</param>
        /// <returns>A task to be awaited.</returns>
        [HubMethodName(ServerCommands.DeclineDoubleCommand)]
        public async Task DeclineDoubleAsync(string matchId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
                }

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid, Context.ConnectionAborted);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
                    {
                        await Lifecycle.PerformDeclineDoubleAsync(matchLock, matchSession, doubleCubeSession, callingPlayerId);
                    }
                    else
                    {
                        await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
                    }
                }
                else
                {
                    await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
            {
                // The caller disconnected while the command was pending.
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"An error occurred while declining a double offer: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        #endregion Doubling Cube Commands

        #endregion Commands

        private static Guid GetStartingPlayerId(IMatchSessionModel matchSession)
        {
            var activeSession = matchSession.GetGameSession(matchSession.GameRound);
            // if not null, the first game session was already played
            // the winner of the previous game will start the next one
            if (activeSession?.Phase == GamePhase.GameOver)
            {
                var contract = activeSession.ToContract(matchSession.GameRound);
                return contract.Winner ?? matchSession.Player1.Id;
            }
            else if (matchSession.GameRound == 1)
            {
                if (matchSession.Player1.StartDiceRoll == null || matchSession.Player2.StartDiceRoll == null)
                {
                    throw new InvalidOperationException("An error occurred while determining the starting player. One of the players has not rolled the starting dice yet.");
                }

                // we decide which player starts with a single dice roll
                var player1Roll = matchSession.Player1.StartDiceRoll;
                var player2Roll = matchSession.Player2.StartDiceRoll;

                if (player1Roll > player2Roll)
                {
                    return matchSession.Player1.Id;
                }
                else
                {
                    return matchSession.Player2.Id;
                }
            }
            else
            {
                throw new InvalidOperationException("An error occurred while determining the last game round winner");
            }
        }

        private async Task SendErrorEventAsync(string errorCode, string message, string? connectionId = null, Exception? exception = null)
        {
            var unWrappedException = UnWrapAggregateException(exception);
            var payload = new EventErrorPayload(errorCode, message, unWrappedException, ServerCommands.GameStateCommand, ServerCommands.MatchStateCommand);
            var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);

            if (!string.IsNullOrEmpty(connectionId))
            {
                await Lifecycle.SendToClientAsync(connectionId, ServerEventTypes.ErrorEvent, contract);
            }
            else
            {
                await SendToCallerAsync(ServerEventTypes.ErrorEvent, contract);
            }

            Log.Logger.Error(unWrappedException, "ConnectionId {connId} :: Code {errorCode} :: Message {errorMessage}", connectionId, errorCode, message);
        }

        private void SetStartRollForPlayer(IMatchSessionModel matchSession, int startDiceRoll)
        {
            if (matchSession.Player1.ConnectionId == Context.ConnectionId)
            {
                matchSession.Player1.SetStartDiceRoll(startDiceRoll);
            }
            else if (matchSession.Player2.ConnectionId == Context.ConnectionId)
            {
                matchSession.Player2.SetStartDiceRoll(startDiceRoll);
            }
            else
            {
                throw new InvalidOperationException("The calling player is not part of the match session.");
            }
        }

        private static (int, int) InvertFromToMove(IMatchSessionModel matchSession, int from, int to)
        {
            var gameModus = matchSession.GetGameModus();
            return BoardBroker.InvertFromToMove(gameModus, from, to);
        }

        private async Task SendToCallerAsync<T>(string serverEventName, EventResponseContract<T> response) where T : EventPayloadBase
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.ExternalDuration, "signalr.send");
            await Clients.Caller.SendAsync(serverEventName, response);
        }
    }
}
