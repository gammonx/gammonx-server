using GammonX.Diagnostics.Server;

using GammonX.Engine.Services;
using GammonX.Engine.Models;

using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Extensions;
using GammonX.Server.Models;
using GammonX.Server.Queue;
using GammonX.Server.Services;

using MatchSessionLease = GammonX.Server.Services.MatchSessionRepository.MatchSessionLease;

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

        private readonly MatchLobbyCleanupService _lobbyCleanup;

        private readonly PlayerConnectionRepository _playerConnectionRepository;

        private readonly IDiceService _diceService;

        private readonly IServiceProvider _serviceProvider;

        private readonly IWorkQueueService _workQueue;

        private readonly IHubContext<MatchLobbyHub> _hubContext;

        private readonly TimeProvider _timeProvider;

        private readonly BotTurnRecoveryService _botRecovery;

        public MatchLobbyHub(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _workQueue = serviceProvider.GetRequiredService<IWorkQueueService>();
            _matchmakingService = serviceProvider.GetRequiredService<IMatchmakingService>();
            _matchRepository = serviceProvider.GetRequiredService<MatchSessionRepository>();
            _lobbyCleanup = serviceProvider.GetRequiredService<MatchLobbyCleanupService>();
            _diceService = serviceProvider.GetRequiredService<IDiceServiceFactory>().Create(DiceServiceType.Crypto);
            _playerConnectionRepository = serviceProvider.GetRequiredService<PlayerConnectionRepository>();
            _cancellationTokenService = serviceProvider.GetRequiredService<ICancellationTokenService>();
            _hubContext = serviceProvider.GetRequiredService<IHubContext<MatchLobbyHub>>();
            _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
            _botRecovery = serviceProvider.GetRequiredService<BotTurnRecoveryService>();
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
                    await using var matchLock = await _matchRepository.AcquireStateAsync(matchId.Value);
                    var tokenKey = ConstructTokenKey(playerId.Value, matchId.Value, CancellationTokenCategory.Disconnect);
                    // We check if the connecting player has a disconnect timer running
                    if (_cancellationTokenService.TryGet(tokenKey, out var cts) && cts != null)
                    {
                        // Charge the completed absence before AddOrUpdate changes LastSeenUtc.
                        // The canceled timer must not mutate connection state later.
                        var disconnectedPlayer = _playerConnectionRepository.Get(playerId.Value);
                        if (disconnectedPlayer != null && !cts.IsCancellationRequested)
                        {
                            var elapsed = DateTime.UtcNow - disconnectedPlayer.LastSeenUtc;
                            if (elapsed >= disconnectedPlayer.DisconnectGracePeriod)
                            {
                                // An expired absence cannot be rescued while we wait for the other deadline.
                                await ResolveDisconnectExpiryAsync(matchLock, playerId.Value, matchId.Value, disconnectedPlayer.ConnectionId!, cts);
                                var payload = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                                await SendToClientAsync(Context.ConnectionId, ServerEventTypes.ForceDisconnectEvent, payload);
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
                            if (otherConnection != null && DateTime.UtcNow - otherConnection.LastSeenUtc >= otherConnection.DisconnectGracePeriod)
                            {
                                await ResolveDisconnectExpiryAsync(matchLock, otherPlayerId, matchSession.Id, otherConnection.ConnectionId!, otherTimer);
                            }
                            else if (otherConnection != null)
                            {
                                var stateCommand = matchSession.GetGameSession(matchSession.GameRound) == null ? ServerCommands.MatchStateCommand : ServerCommands.GameStateCommand;
                                var connectedPayload = new EventMatchLobbyPayload(matchSession.Id, matchSession.Player1.Id, matchSession.Player2.Id, stateCommand);
                                var connectedResponse = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, connectedPayload);
                                // We send to group that the player reconnected
                                await SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, connectedResponse);
                                // We send to player that the other player is still disconnected
                                var otherDisconnectPayload = EventDisconnectedPayload.From(otherConnection);
                                var otherDisconnectResponse = new EventResponseContract<EventDisconnectedPayload>(ServerEventTypes.PlayerDisconnectedEvent, otherDisconnectPayload);
                                await SendToClientAsync(connectionId, ServerEventTypes.PlayerDisconnectedEvent, otherDisconnectResponse);
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
                                await PerformBotTurnAsync(matchLock, matchSession, activeGameSession.ActivePlayer,
                                    () => SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract));
                                return;
                            }

                            await SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract);
                            if (activeGameSession.Phase != GamePhase.GameOver)
                            {
                                // A player owes the next move or cube response, restore their response deadline.
                                StartTurnTimer(matchSession, activeGameSession.ActivePlayer);
                            }
                            else if (!matchSession.IsMatchOver())
                            {
                                // Restore deadlines only for outstanding acceptances. The bot accepts automatically.
                                if (!matchSession.Player1.NextGameAccepted)
                                {
                                    StartTurnTimer(matchSession, matchSession.Player1.Id);
                                }

                                if (!matchSession.Player2.IsBot && !matchSession.Player2.NextGameAccepted)
                                {
                                    StartTurnTimer(matchSession, matchSession.Player2.Id);
                                }
                            }
                        }
                        else
                        {
                            // we handle a first time connect on before match has started
                            var payload = new EventMatchLobbyPayload(matchSession.Id, matchSession.Player1.Id, matchSession.Player2.Id, ServerCommands.MatchStateCommand);
                            var contract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.PlayerConnectedEvent, payload);
                            await SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.PlayerConnectedEvent, contract);

                            if (matchSession.Player1.Claimed && matchSession.Player2.Claimed && !matchSession.IsMatchOver())
                            {
                                // Both seats are assigned, but the first game has not started. Restore player start deadlines;
                                // the bots opening roll and acceptance are supplied when the player starts the match.
                                StartTurnTimer(matchSession, matchSession.Player1.Id);
                                if (!matchLock.IsBotPlayer(matchSession.Player2.Id))
                                {
                                    StartTurnTimer(matchSession, matchSession.Player2.Id);
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
                            await SendToClientAsync(connectionId, ServerEventTypes.PlayerConnectedEvent, contract);
                            StartJoinTimer(matchLobby, matchSession, playerId.Value, connectionId);
                        }
                        else
                        {
                            // Match is gone (forfeited, cleaned up). Nothing to join.
                            var emptyResponse = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                            await SendToClientAsync(connectionId, ServerEventTypes.ForceDisconnectEvent, emptyResponse);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Failed to obtain the required claims from the authorization token");
                }
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

                        CancelTimersFor(matchId.Value, CancellationTokenCategory.Turn);
                        CancelTimersFor(matchId.Value, CancellationTokenCategory.BotRecovery);
                        playerConnection.LastSeenUtc = DateTime.UtcNow;

                        StartDisconnectGraceTimer(playerConnection, matchId.Value);
                        var payload = EventDisconnectedPayload.From(playerConnection);
                        var contract = new EventResponseContract<EventDisconnectedPayload>(ServerEventTypes.PlayerDisconnectedEvent, payload);
                        await SendToGroupAsync(ConstructGroupName(matchId.Value), ServerEventTypes.PlayerDisconnectedEvent, contract);
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

        private void StartDisconnectGraceTimer(PlayerConnection playerConnection, Guid matchId)
        {
            var tokenKey = ConstructTokenKey(playerConnection.Id, matchId, CancellationTokenCategory.Disconnect);
            _cancellationTokenService.Cancel(tokenKey);

            var newCts = new CancellationTokenSource();
            _cancellationTokenService.Store(tokenKey, newCts);
            var connectionId = playerConnection.ConnectionId!;
            var gracePeriod = playerConnection.DisconnectGracePeriod;

            _ = Task.Run(async () =>
            {
                ServerMetrics.TimerTaskStarted();
                try
                {
                    await Task.Delay(gracePeriod, newCts.Token);
                    await HandlePermanentDisconnectAsync(playerConnection.Id, matchId, connectionId, newCts);
                }
                catch (OperationCanceledException)
                {
                    // Reconnection accounts for elapsed grace under the match lease. In particular,
                    // an old task must not cancel a replacement timer registered under the same key.
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while handling disconnect expiry for match {matchId}.", matchId);
                }
                finally
                {
                    ServerMetrics.TimerTaskFinished();
                }
            });
        }

        private async Task HandlePermanentDisconnectAsync(Guid playerId, Guid matchId, string connectionId, CancellationTokenSource timer)
        {
            using var callback = new ServerMetrics.CallbackScope();
            await using var matchLock = await _matchRepository.AcquireStateAsync(matchId);
            await ResolveDisconnectExpiryAsync(matchLock, playerId, matchId, connectionId, timer);
        }

        private async Task ResolveDisconnectExpiryAsync(MatchSessionLease matchLock, Guid playerId, Guid matchId, string connectionId, CancellationTokenSource timer)
        {
            var tokenKey = ConstructTokenKey(playerId, matchId, CancellationTokenCategory.Disconnect);
            var connection = _playerConnectionRepository.Get(playerId);
            // Expiry may have queued behind a reconnect or a new disconnect. Validate its generation
            // only after acquiring the lease, before removing connections or changing the match.
            var sameConnection = string.Equals(connection?.ConnectionId, connectionId, StringComparison.Ordinal);
            var sameTimer = _cancellationTokenService.TryGet(tokenKey, out var currentTimer) && ReferenceEquals(timer, currentTimer);
            if (timer.IsCancellationRequested || !sameTimer || connection == null || connection.IsBot || !sameConnection)
            {
                return;
            }

            var matchToResignClone = matchLock.Get();
            var participant = matchToResignClone?.Player1.Id == playerId || matchToResignClone?.Player2.Id == playerId;
            if (matchToResignClone != null && !participant)
            {
                return;
            }

            connection.DisconnectGracePeriod = TimeSpan.Zero;

            if (matchToResignClone != null)
            {
                var otherPlayerId = matchLock.GetOtherPlayerId(playerId);
                var otherKey = ConstructTokenKey(otherPlayerId, matchId, CancellationTokenCategory.Disconnect);
                // We check if the other player is also disconnected
                if (_cancellationTokenService.TryGet(otherKey, out _))
                {
                    var otherConnection = _playerConnectionRepository.Get(otherPlayerId);
                    if (otherConnection != null && DateTime.UtcNow - otherConnection.LastSeenUtc >= otherConnection.DisconnectGracePeriod)
                    {
                        // Both deadlines elapsed, no winner or rated result, regardless of callback order.
                        await CleanUpMatchAsync(matchLock, matchToResignClone, ConstructGroupName(matchId));
                    }
                    // Retain this registration as the disconnected marker until the other player resolves.
                    return;
                }
            }

            // We see that the player surpassed their grace period and the other player wins
            _cancellationTokenService.Cancel(tokenKey);
            if (matchToResignClone?.StartedAt == null && await _lobbyCleanup.TryCleanupUnstartedAsync(matchId, matchLock))
            {
                return;
            }
            _playerConnectionRepository.TryRemove(playerId);

            if (matchToResignClone != null && !matchToResignClone.IsMatchOver())
            {
                matchToResignClone.ResignMatch(playerId);
                matchLock.Commit();
                matchLock.TryRemove();
                await SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchToResignClone);
            }
            else
            {
                // no match was started, we simply close the connections in this case
                var emptyResponse = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                await SendToGroupAsync(ConstructGroupName(matchId), ServerEventTypes.ForceDisconnectEvent, emptyResponse);
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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
                        CancelTimersFor(matchSession.Id, matchLobby.Player1.Id, CancellationTokenCategory.Turn);
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
                        CancelTimersFor(matchSession.Id, matchLobby.Player2.Id, CancellationTokenCategory.Turn);
                        matchLobby.Player2.SetConnectionId(Context.ConnectionId);
                        ArgumentNullException.ThrowIfNull(matchLobby.Player2.ConnectionId, nameof(matchLobby.Player2.ConnectionId));
                        matchSession.JoinSession(matchLobby.Player2);
                    }

                    if (matchSession.Player1.Id != Guid.Empty && matchSession.Player2.Id != Guid.Empty)
                    {
                        var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchSession.Player1.Id, matchSession.Player2.Id);
                        var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyPayload);
                        matchLock.Commit();
                        await SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.MatchLobbyFoundEvent, matchLobbyContract);
                        await SendMatchState(matchLock, ServerEventTypes.MatchWaitingForStartEvent, matchSession);
                        // we set a timeout for the players to start the match
                        StartTurnTimer(matchSession, matchSession.Player1.Id);
                        if (!matchLock.IsBotPlayer(matchSession.Player2.Id))
                        {
                            StartTurnTimer(matchSession, matchSession.Player2.Id);
                        }
                    }
                    else
                    {
                        var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.Id, null);
                        var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyPayload);
                        matchLock.Commit();
                        await SendToGroupAsync(ConstructGroupName(matchSession.Id), ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyContract);
                    }
                }
                else
                {
                    await SendErrorEventAsync("LOBBY_ERROR", "No match lobby was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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
                        CancelTimersFor(matchSession.Id, player1.Id, CancellationTokenCategory.Turn);
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
                        CancelTimersFor(matchSession.Id, player2.Id, CancellationTokenCategory.Turn);
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
                            StartTurnTimer(matchSession, player1.Id);
                            if (!player2.IsBot)
                            {
                                StartTurnTimer(matchSession, player2.Id);
                            }

                            await SendMatchState(matchLock, ServerEventTypes.MatchWaitingForStartEvent, matchSession);
                        }
                        else
                        {
                            if (matchSession.CanStartNextGame())
                            {
                                var startingPlayerId = GetStartingPlayerId(matchSession);
                                var gameSession = matchSession.StartMatch(startingPlayerId);

                                // A retry can resolve the starting roll tie while the opponent still has a reroll timer.
                                CancelTimersFor(matchSession.Id, CancellationTokenCategory.Turn);

                                var isWhite = matchLock.IsWhite(startingPlayerId);
                                // we set the opening dice rolls for the starting player
                                gameSession.SetDiceRolls([player1.StartDiceRoll.Value, player2.StartDiceRoll.Value], isWhite);

                                matchLock.Commit();

                                if (matchLock.IsBotPlayer(startingPlayerId))
                                {
                                    await PerformBotTurnAsync(matchLock, matchSession, startingPlayerId, async () =>
                                    {
                                        await SendMatchState(matchLock, ServerEventTypes.MatchStartedEvent, matchSession);
                                        await SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
                                    });
                                }
                                else
                                {
                                    StartTurnTimer(matchSession, startingPlayerId);
                                    await SendMatchState(matchLock, ServerEventTypes.MatchStartedEvent, matchSession);
                                    await SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
                                }

                                // we clean up the match lobby as soon as the first player rolled his starting dice
                                _matchmakingService.TryRemoveMatchLobby(matchSession.Id);
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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
                        CancelTimersFor(matchSession.Id, matchSession.Player1.Id, CancellationTokenCategory.Turn);
                        matchSession.Player1.AcceptNextGame();

                        if (matchSession.Player2.IsBot)
                        {
                            // in a bot game, the player 2 is always the "bot"
                            matchSession.Player2.AcceptNextGame();
                        }
                    }

                    if (matchSession.Player2.ConnectionId == Context.ConnectionId)
                    {
                        CancelTimersFor(matchSession.Id, matchSession.Player2.Id, CancellationTokenCategory.Turn);
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
                            await PerformBotTurnAsync(matchLock, matchSession, startingPlayerId,
                                () => SendGameState(ServerEventTypes.GameStartedEvent, matchSession));
                        }
                        else
                        {
                            StartTurnTimer(matchSession, startingPlayerId);
                            await SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    await PerformRollAsync(matchLock, matchSession, callingPlayerId);
                }
                else
                {
                    await SendErrorEventAsync("ROLL_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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

                    await PerformMoveAsync(matchLock, matchSession, callingPlayerId, from, to);
                }
                else
                {
                    await SendErrorEventAsync("MOVE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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
                    await SendGameState(ServerEventTypes.GameStateEvent, matchSession);
                }
                else
                {
                    await SendErrorEventAsync("UNDO_MOVE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
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

                    CancelTimersFor(matchSession.Id, callingPlayerId, CancellationTokenCategory.Turn);
                    matchSession.EndTurn(callingPlayerId);
                    matchLock.Commit();

                    if (matchLock.IsBotPlayer(otherPlayerId))
                    {
                        await PerformBotTurnAsync(matchLock, matchSession, otherPlayerId);
                    }
                    else
                    {
                        StartTurnTimer(matchSession, otherPlayerId);
                        await SendGameState(ServerEventTypes.GameStateEvent, matchSession);
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    matchSession.ResignMatch(callingPlayerId);
                    matchLock.Commit();
                    await SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
                }
                else
                {
                    await SendErrorEventAsync("RESIGN_MATCH_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    matchSession.ResignGame(callingPlayerId);
                    matchLock.Commit();
                    // check if this was the last game round of the match
                    if (matchSession.IsMatchOver())
                    {
                        await SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
                    }
                    else
                    {
                        await SendMatchState(matchLock, ServerEventTypes.GameEndedEvent, matchSession);
                    }
                }
                else
                {
                    await SendErrorEventAsync("RESIGN_GAME_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                var payload = matchLock.ReadGameState(callingPlayerId);
                if (payload != null)
                {
                    var contract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.GameStateEvent, payload);
                    await SendToClientAsync(Context.ConnectionId, ServerEventTypes.GameStateEvent, contract);
                }
                else
                {
                    await SendErrorEventAsync("GET_GAME_STATE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                var payload = matchLock.ReadMatchState(callingPlayerId);
                if (payload != null)
                {
                    payload.AppendRatingUrl(callingPlayerId, ServerEventTypes.MatchStateEvent);
                    var contract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.MatchStateEvent, payload);
                    await SendToClientAsync(Context.ConnectionId, ServerEventTypes.MatchStateEvent, contract);
                }
                else
                {
                    await SendErrorEventAsync("GET_MATCH_STATE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null && matchSession is IDoubleCubeMatchSession doubleCubeSession)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);

                    if (!doubleCubeSession.CanOfferDouble(callingPlayerId))
                    {
                        await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The player with the id '{callingPlayerId}' cannot offer a double at the moment.", Context.ConnectionId);
                        return;
                    }

                    await PerformOfferDoubleAsync(matchLock, matchSession, callingPlayerId);
                    var otherPlayerId = matchLock.GetOtherPlayerId(callingPlayerId);
                    if (!matchLock.IsBotPlayer(otherPlayerId))
                    {
                        StartTurnTimer(matchSession, otherPlayerId);
                    }
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    await PerformAcceptDoubleAsync(matchLock, matchSession, callingPlayerId);

                    var otherPlayerId = matchLock.GetOtherPlayerId(callingPlayerId);
                    if (!matchLock.IsBotPlayer(otherPlayerId))
                    {
                        StartTurnTimer(matchSession, otherPlayerId);
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

                await using var matchLock = await _matchRepository.AcquireStateAsync(matchGuid);
                var matchSession = matchLock.Get();
                if (matchSession != null)
                {
                    var callingPlayerId = matchLock.GetCallingPlayerId(Context.ConnectionId);
                    await PerformDeclineDoubleAsync(matchLock, matchSession, callingPlayerId);
                }
                else
                {
                    await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", "No match session was found with the given matchId.", Context.ConnectionId);
                }
            }
            catch (Exception e)
            {
                await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"An error occurred while declining a double offer: '{e.Message}'", Context.ConnectionId, e);
            }
        }

        private async Task PerformOfferDoubleAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid offeringPlayerId,
            CancellationToken cancellationToken = default)
        {
            if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
            {
                // active players changes here
                doubleCubeSession.OfferDouble(offeringPlayerId);
                CancelTimersFor(matchSession.Id, offeringPlayerId, CancellationTokenCategory.Turn);

                matchLock.Commit();
                var offeredPlayerId = matchLock.GetOtherPlayerId(offeringPlayerId);

                if (matchLock.IsBotPlayer(offeredPlayerId))
                {
                    await PerformBotTurnAsync(matchLock, matchSession, offeredPlayerId);
                }
                else
                {
                    if (matchLock.IsBotPlayer(offeringPlayerId))
                    {
                        StartTurnTimer(matchSession, offeredPlayerId);
                    }

                    // the player who is offering the double is waiting for a response
                    var callingPlayerGameSession = matchSession.GetGameState(offeringPlayerId);
                    var callingPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.DoubleOfferedEvent, callingPlayerGameSession);
                    var callingConnectionId = matchLock.GetPlayerConnectionId(offeringPlayerId);
                    await SendToClientAsync(callingConnectionId, ServerEventTypes.DoubleOfferedEvent, callingPlayerContract, cancellationToken);

                    // the player who got the double offered has to accept or decline it
                    var otherPlayerGameSession = matchSession.GetGameState(offeredPlayerId);
                    var otherPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.DoubleOfferedEvent, otherPlayerGameSession);
                    var otherPlayerConnectionId = matchLock.GetPlayerConnectionId(offeredPlayerId);
                    await SendToClientAsync(otherPlayerConnectionId, ServerEventTypes.DoubleOfferedEvent, otherPlayerContract, cancellationToken);
                }
            }
            else
            {
                await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
            }
        }

        private async Task PerformAcceptDoubleAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid offeredPlayerId,
            CancellationToken cancellationToken = default)
        {
            if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
            {
                doubleCubeSession.AcceptDouble(offeredPlayerId);
                CancelTimersFor(matchSession.Id, offeredPlayerId, CancellationTokenCategory.Turn);

                var offeringPlayerId = matchLock.GetOtherPlayerId(offeredPlayerId);
                matchLock.Commit();

                if (matchLock.IsBotPlayer(offeringPlayerId))
                {
                    // the bot offered the double and the player player accepted it
                    // bot executes the next turn
                    await PerformBotTurnAsync(matchLock, matchSession, offeringPlayerId);
                }
                else
                {
                    // doubles can only be offered/accepted/declined at the start of a turn and before the dices got rolled
                    // Therefore, the next command must be the roll command
                    if (matchLock.IsBotPlayer(offeredPlayerId))
                    {
                        StartTurnTimer(matchSession, offeringPlayerId);
                    }

                    await SendGameState(ServerEventTypes.DoubleAcceptedEvent, matchSession, cancellationToken);
                }
            }
            else
            {
                await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
            }
        }

        private async Task PerformDeclineDoubleAsync(MatchSessionLease matchLock, IMatchSessionModel matchSession, Guid offeredPlayerId)
        {
            if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
            {
                // declining a double offer automatically loses the current game round
                doubleCubeSession.DeclineDouble(offeredPlayerId);
                CancelTimersFor(matchSession.Id, offeredPlayerId, CancellationTokenCategory.Turn);

                matchLock.Commit();
                // check if this was the last game round of the match
                if (matchSession.IsMatchOver())
                {
                    // match ended bot does not have another turn
                    await SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
                }
                else
                {
                    // game ended, a potential bot turn is handled in the start game command
                    await SendMatchState(matchLock, ServerEventTypes.GameEndedEvent, matchSession);
                }
            }
            else
            {
                await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
            }
        }

        #endregion Doubling Cube Commands

        private async Task PerformRollAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid callingPlayerId,
            CancellationToken cancellationToken = default)
        {
            matchSession.RollDices(callingPlayerId);
            matchLock.Commit();
            await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
        }

        private async Task PerformMoveAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid callingPlayerId,
            int from,
            int to,
            CancellationToken cancellationToken = default)
        {
            var gameOver = matchSession.MoveCheckers(callingPlayerId, from, to);
            matchLock.Commit();

            try
            {
                await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
            }
            finally
            {
                if (gameOver)
                {
                    // The result was already committed. Attempt publication even if the board send fails.
                    var resultEvent = matchSession.IsMatchOver() ? ServerEventTypes.MatchEndedEvent : ServerEventTypes.GameEndedEvent;
                    await SendMatchState(matchLock, resultEvent, matchSession);
                }
            }
        }

        private async Task PerformBotTurnAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid botPlayerId,
            Func<Task>? notifyStart = null)
        {
            var game = matchSession.GetGameSession(matchSession.GameRound);
            if (game == null)
            {
                throw new InvalidOperationException("A bot turn requires an active game.");
            }

            var turn = new BotTurnIdentity(matchSession.Id, botPlayerId, game.Id, game.TurnNumber);
            CancelTimersFor(turn.MatchId, turn.BotId, CancellationTokenCategory.BotRecovery);

            var disconnected = Context.ConnectionAborted;
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(disconnected, _botRecovery.Stopping);

            try
            {
                operation.Token.ThrowIfCancellationRequested();

                if (notifyStart != null)
                {
                    await notifyStart();
                }

                await RunBotTurnAsync(matchLock, matchSession, turn, operation.Token);
            }
            catch (Exception) when (operation.IsCancellationRequested)
            {
                // A reconnect can resume from the last committed action.
            }
            catch (Exception ex)
            {
                if (GetActiveBotGame(_matchRepository.Get(turn.MatchId), turn) != null)
                {
                    // we schedule a bot retry once, if this fails to we close the match as inconclusive (no rating change)
                    _botRecovery.Schedule(turn, disconnected, RecoverBotTurnAsync);
                }

                Log.Error(ex, "Bot turn failed for match {MatchId}, game {GameId}, turn {TurnNumber}.", turn.MatchId, turn.GameId, turn.TurnNumber);
                throw new BotServiceException(WellKnownBotServices.Mars, "An error occurred while executing the bot turn", ex);
            }
        }

        private async Task RunBotTurnAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            BotTurnIdentity turn,
            CancellationToken cancellationToken,
            bool fallbackEval = false)
        {
            var game = RequireActiveBotGame(matchSession, turn, cancellationToken);

            if (matchSession is IDoubleCubeMatchSession cubeSession)
            {
                if (cubeSession.IsDoubleOfferPending)
                {
                    var shouldTake = fallbackEval || await EvaluateBotCubeAsync(matchSession, turn.BotId, true, cancellationToken);
                    RequireActiveBotGame(matchSession, turn, cancellationToken);

                    if (shouldTake)
                    {
                        await PerformAcceptDoubleAsync(matchLock, matchSession, turn.BotId, cancellationToken);
                    }
                    else
                    {
                        await PerformDeclineDoubleAsync(matchLock, matchSession, turn.BotId);
                    }

                    return;
                }

                if (!fallbackEval && cubeSession.CanOfferDouble(turn.BotId))
                {
                    var shouldOffer = await EvaluateBotCubeAsync(matchSession, turn.BotId, false, cancellationToken);
                    RequireActiveBotGame(matchSession, turn, cancellationToken);

                    if (shouldOffer)
                    {
                        await PerformOfferDoubleAsync(matchLock, matchSession, turn.BotId, cancellationToken);
                        return;
                    }
                }
            }

            if (game.Phase == GamePhase.WaitingForRoll)
            {
                await PerformRollAsync(matchLock, matchSession, turn.BotId, cancellationToken);
                game = RequireActiveBotGame(matchSession, turn, cancellationToken);
            }

            var diceRemaining = game.DiceRolls.Count(roll => !roll.Used);
            IReadOnlyList<MoveModel>? moveSeq = null;
            var nextMove = 0;

            while (diceRemaining > 0 && game.CanMove(matchLock.IsWhite(turn.BotId)))
            {
                RequireActiveBotGame(matchSession, turn, cancellationToken);

                if (!fallbackEval && (moveSeq == null || nextMove >= moveSeq.Count))
                {
                    try
                    {
                        moveSeq = (await GetBotService().GetNextMovesAsync(matchSession, turn.BotId, cancellationToken))?.Moves;
                    }
                    catch (Exception ex)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Log.Warning(ex, "Bot move evaluation failed for match {MatchId}.", turn.MatchId);
                        moveSeq = null;
                    }

                    nextMove = 0;
                    game = RequireActiveBotGame(matchSession, turn, cancellationToken);
                }

                var move = moveSeq != null && nextMove < moveSeq.Count ? moveSeq[nextMove++] : null;
                if (move == null)
                {
                    if (!fallbackEval)
                    {
                        ServerMetrics.BotFallbacks.Add(1, new KeyValuePair<string, object?>("operation", "move"));
                        fallbackEval = true;
                    }

                    // the bot service failed to deliver a move, we just fall back on the first one
                    moveSeq = game.GetLegalMoveSequences(matchLock.IsWhite(turn.BotId)).First().Moves;
                    nextMove = 1;
                    move = moveSeq[0];
                }

                RequireActiveBotGame(matchSession, turn, cancellationToken);

                await PerformMoveAsync(matchLock, matchSession, turn.BotId, move.From, move.To, cancellationToken);

                if (game.Phase == GamePhase.GameOver)
                {
                    return;
                }

                game = RequireActiveBotGame(matchSession, turn, cancellationToken);

                var unused = game.DiceRolls.Count(roll => !roll.Used);
                if (unused >= diceRemaining)
                {
                    throw new InvalidOperationException("A committed bot move must spend at least one die.");
                }

                diceRemaining = unused;
            }

            RequireActiveBotGame(matchSession, turn, cancellationToken);

            if (!matchSession.CanEndTurn(turn.BotId))
            {
                throw new InvalidOperationException("The bot could not finish its turn within the remaining dice.");
            }

            matchSession.EndTurn(turn.BotId);
            matchLock.Commit();

            StartTurnTimer(matchSession, matchLock.GetOtherPlayerId(turn.BotId));
            await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
        }

        private async Task<bool> EvaluateBotCubeAsync(
            IMatchSessionModel match,
            Guid botId,
            bool take,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var bot = GetBotService();

                return take
                    ? await bot.ShouldTakeDouble(match, botId, cancellationToken)
                    : await bot.ShouldOfferDouble(match, botId, cancellationToken);
            }
            catch (Exception ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Log.Warning(ex, "Bot cube evaluation failed for match {MatchId}.", match.Id);

                ServerMetrics.BotFallbacks.Add(1, new KeyValuePair<string, object?>("operation", take ? "cube.take" : "cube.offer"));

                return take;
            }
        }

        private static IGameSessionModel? GetActiveBotGame(IMatchSessionModel? match, BotTurnIdentity turn)
        {
            var game = match?.GetGameSession(match.GameRound);
            var sameMatch = match?.Id == turn.MatchId;
            var sameGame = game?.Id == turn.GameId;
            var sameTurn = game?.TurnNumber == turn.TurnNumber;
            var isBotTurn = game?.ActivePlayer == turn.BotId;
            var isGameOver = game?.Phase != GamePhase.GameOver;
            return sameMatch && sameGame && sameTurn && isBotTurn && isGameOver ? game : null;
        }

        private static IGameSessionModel RequireActiveBotGame(IMatchSessionModel match, BotTurnIdentity turn, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var game = GetActiveBotGame(match, turn);
            if (game == null)
            {
                throw new InvalidOperationException("The bot no longer owns this game turn.");
            }

            return game;
        }

        private async Task RecoverBotTurnAsync(BotTurnRecoveryService.Deadline deadline, CancellationToken cancellationToken)
        {
            var turn = deadline.Turn;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Generation.Token);
            await using var matchLock = await _matchRepository.AcquireStateWithCancellationAsync(turn.MatchId, wait.Token);

            cancellationToken.ThrowIfCancellationRequested();

            var match = matchLock.Get();
            if (match == null || GetActiveBotGame(match, turn) == null ||
                IsDisconnectPending(turn.MatchId, matchLock.GetOtherPlayerId(turn.BotId)) ||
                !_cancellationTokenService.TryCancel(deadline.Key, deadline.Generation))
            {
                return;
            }

            ServerMetrics.BotRecoveries.Add(1);

            try
            {
                await RunBotTurnAsync(matchLock, match, turn, cancellationToken, fallbackEval: true);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Disconnect/reconnect or host shutdown now owns the committed state.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Automatic bot turn recovery failed for match {MatchId}, game {GameId}, turn {TurnNumber}.", turn.MatchId, turn.GameId, turn.TurnNumber);

                var committed = _matchRepository.Get(turn.MatchId);
                if (committed == null || GetActiveBotGame(committed, turn) == null)
                {
                    return;
                }

                var groupName = ConstructGroupName(turn.MatchId);

                try
                {
                    await SendErrorEventToGroupAsync("BOT_RECOVERY_FAILED", "The match was closed because the bot turn could not be completed.", groupName, cancellationToken: cancellationToken);
                }
                finally
                {
                    await CleanUpMatchAsync(matchLock, committed, groupName, cancellationToken);
                }
            }
        }

        #endregion Commands

        #region Turn Timers

        private void StartTurnTimer(IMatchSessionModel match, Guid playerId)
        {
            if (RegisterTurnDeadline(match, playerId) is { } deadline)
            {
                _ = RunTurnTimerAsync(deadline);
            }
        }

        private void StartJoinTimer(MatchLobby lobby, IMatchSessionModel? match, Guid playerId, string connectionId)
        {
            if (RegisterJoinDeadline(lobby, match, playerId, connectionId) is { } deadline)
            {
                _ = RunTurnTimerAsync(deadline);
            }
        }

        internal async Task RunTurnTimerAsync(TurnDeadline deadline)
        {
            ServerMetrics.TimerTaskStarted();
            var cancellation = deadline.Token.Token;
            var warning = SendTurnWarningAsync(deadline, cancellation);

            try
            {
                await DelayUntilAsync(deadline.ExpiresAt, cancellation);
                await HandleTurnTimeoutAsync(deadline);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // A command, disconnect, or reconnect superseded the deadline.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Turn expiry failed for match {matchId}, player {playerId}", deadline.Key.MatchId, deadline.Key.PlayerId);
            }
            finally
            {
                _cancellationTokenService.TryCancel(deadline.Key, deadline.Token);
                // Warning delivery and expiry run independently. A slow send cannot grant extra time.
                await warning;
                deadline.Token.Dispose();
                ServerMetrics.TimerTaskFinished();
            }
        }

        internal TurnDeadline? RegisterTurnDeadline(IMatchSessionModel match, Guid playerId)
        {
            var player = GetDeadlinePlayer(match, playerId);
            var obligation = GetTurnObligation(match, player);
            var connectionId = player?.ConnectionId;
            if (obligation != null && connectionId != null && connectionId.Length > 0)
            {
                return RegisterTurnDeadline(match.Id, playerId, connectionId, obligation.Value, match.Modus);
            }

            return null;
        }

        private TurnDeadline RegisterTurnDeadline(
            Guid matchId,
            Guid playerId,
            string connectionId,
            TurnObligation obligation,
            MatchModus? modus)
        {
            var key = ConstructTokenKey(playerId, matchId, CancellationTokenCategory.Turn);
            var now = _timeProvider.GetUtcNow();
            var limit = GetTurnTimeLimit(modus);
            var warnAt = now + limit.Divide(2);
            var expireAt = now + limit;
            var token = new CancellationTokenSource();

            var deadline = new TurnDeadline(key, token, obligation, connectionId, warnAt, expireAt);
            _cancellationTokenService.Store(key, deadline.Token);

            return deadline;
        }

        internal TurnDeadline? RegisterJoinDeadline(MatchLobby lobby, IMatchSessionModel? match, Guid playerId, string connectionId)
        {
            if (OwesJoin(lobby, match, playerId))
            {
                var obligation = new TurnObligation(TurnDeadlineAction.Join);
                return RegisterTurnDeadline(lobby.MatchId, playerId, connectionId, obligation, null);
            }
            return null;
        }

        private async Task SendTurnWarningAsync(TurnDeadline deadline, CancellationToken cancellation)
        {
            try
            {
                await DelayUntilAsync(deadline.WarnAt, cancellation);
                if (_timeProvider.GetUtcNow() >= deadline.ExpiresAt || !IsActiveDeadline(deadline))
                {
                    return;
                }

                var connection = _playerConnectionRepository.Get(deadline.Key.PlayerId);
                if (connection != null && connection.ConnectionId != deadline.ConnectionId)
                {
                    return;
                }

                var payload = new EventTurnTimerPayload
                {
                    ActivePlayer = deadline.Key.PlayerId,
                    TurnExpiration = deadline.ExpiresAt.UtcDateTime
                };

                var response = new EventResponseContract<EventTurnTimerPayload>(ServerEventTypes.TurnTimerEvent, payload);
                await SendToClientAsync(deadline.ConnectionId, ServerEventTypes.TurnTimerEvent, response, cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // The deadline was superseded before the warning could be sent.
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Turn warning failed for match {matchId}, player {playerId}", deadline.Key.MatchId, deadline.Key.PlayerId);
            }
        }

        private async Task DelayUntilAsync(DateTimeOffset due, CancellationToken cancellation)
        {
            var remaining = due - _timeProvider.GetUtcNow();
            while (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, _timeProvider, cancellation);
                remaining = due - _timeProvider.GetUtcNow();
            }
            cancellation.ThrowIfCancellationRequested();
        }

        private bool IsActiveDeadline(TurnDeadline deadline)
        {
            var isNotCancelled = !deadline.Token.IsCancellationRequested;
            if (isNotCancelled && _cancellationTokenService.TryGet(deadline.Key, out var registered))
            {
                return ReferenceEquals(registered, deadline.Token);
            }
            return false;
        }

        private static MatchPlayerModel? GetDeadlinePlayer(IMatchSessionModel? match, Guid playerId)
        {
            if (match?.Player1.Id == playerId)
            {
                return match.Player1;
            }
            return match?.Player2.Id == playerId ? match.Player2 : null;
        }

        private static bool OwesJoin(MatchLobby? lobby, IMatchSessionModel? match, Guid playerId)
        {
            if (lobby == null)
            {
                return false;
            }

            var matchStarted = match?.StartedAt == null;
            var player = GetDeadlinePlayer(match, playerId) == null;
            var notBot = lobby.Player1.Id == playerId && !lobby.Player1.IsBot || lobby.Player2?.Id == playerId && !lobby.Player2.IsBot;
            return matchStarted && player && notBot;
        }

        private static TurnObligation? GetTurnObligation(IMatchSessionModel? match, MatchPlayerModel? player)
        {
            if (match == null || player == null || !player.Claimed || player.IsBot || match.EndedAt != null)
            {
                return null;
            }

            return match.PendingObligation(player.Id);
        }

        private static TimeSpan GetTurnTimeLimit(MatchModus? modus)
        {
            if (modus == null)
            {
                // we return default time limit if modus is not specified
                return TimeSpan.FromSeconds(60);
            }

            return modus switch
            {
                // we allow a generous turn timer for bot matches
                MatchModus.Bot => TimeSpan.FromSeconds(300),
                MatchModus.Ranked => TimeSpan.FromSeconds(90),
                MatchModus.Normal => TimeSpan.FromSeconds(60),
                _ => throw new ArgumentOutOfRangeException(nameof(modus), modus, null)
            };
        }

        internal async Task HandleTurnTimeoutAsync(TurnDeadline deadline)
        {
            using var callback = new ServerMetrics.CallbackScope();
            var (matchId, playerId) = (deadline.Key.MatchId, deadline.Key.PlayerId);
            await using var matchLock = await _matchRepository.AcquireStateAsync(matchId);
            if (_timeProvider.GetUtcNow() < deadline.ExpiresAt || !IsActiveDeadline(deadline))
            {
                return;
            }

            var match = matchLock.Get();
            var stillOwed = IsTurnObligationCurrent(deadline, match);
            // Consume only this generation, even if it no longer applies. Duplicate callbacks
            // and callbacks from a replaced timer must not change the match or its new timer.
            if (!_cancellationTokenService.TryCancel(deadline.Key, deadline.Token) || !stillOwed)
            {
                return;
            }

            switch (deadline.Obligation.Action)
            {
                case TurnDeadlineAction.Join:
                case TurnDeadlineAction.StartMatch:
                    await _lobbyCleanup.TryCleanupUnstartedAsync(matchId, matchLock);
                    return;
                case TurnDeadlineAction.AcceptNextGame:
                    match!.ResignMatch(playerId);
                    break;
                case TurnDeadlineAction.RespondToCube:
                    // A missed cube response passes at the already accepted cube value.
                    ((IDoubleCubeMatchSession)match!).DeclineDouble(playerId);
                    break;
                case TurnDeadlineAction.PlayTurn:
                    match!.ResignGame(playerId);
                    break;
            }

            matchLock.Commit();
            var serverEvent = match!.IsMatchOver() ? ServerEventTypes.MatchEndedEvent : ServerEventTypes.GameEndedEvent;
            await SendMatchState(matchLock, serverEvent, match);
        }

        private bool IsTurnObligationCurrent(TurnDeadline deadline, IMatchSessionModel? match)
        {
            var matchId = deadline.Key.MatchId;
            var playerId = deadline.Key.PlayerId;
            var currentConnection = _playerConnectionRepository.Get(playerId);

            // we check if the given player has a disconnect timer running
            if ((currentConnection != null && currentConnection.ConnectionId != deadline.ConnectionId) || IsDisconnectPending(matchId, playerId))
            {
                return false;
            }

            if (deadline.Obligation.Action == TurnDeadlineAction.Join)
            {
                var hasLobby = _matchmakingService.TryFindMatchLobby(matchId, out var lobby);
                var owesJoin = OwesJoin(lobby, match, playerId);
                if (currentConnection == null || !hasLobby || lobby == null || !owesJoin)
                {
                    return false;
                }

                var otherLobbyPlayer = lobby.Player1.Id == playerId ? lobby.Player2 : lobby.Player1;
                return otherLobbyPlayer == null || otherLobbyPlayer.IsBot || !IsDisconnectPending(matchId, otherLobbyPlayer.Id);
            }

            var player = GetDeadlinePlayer(match, playerId);
            var other = match?.Player1.Id == playerId ? match.Player2 : match?.Player1;
            var sameConnId = player?.ConnectionId == deadline.ConnectionId;
            var sameObligation = GetTurnObligation(match, player) == deadline.Obligation;
            var validTarget = other == null || other.IsBot || !IsDisconnectPending(matchId, other.Id);
            return sameConnId && sameObligation && validTarget;
        }

        private bool IsDisconnectPending(Guid matchId, Guid playerId)
        {
            var tokenKey = ConstructTokenKey(playerId, matchId, CancellationTokenCategory.Disconnect);
            return _cancellationTokenService.TryGet(tokenKey, out _);
        }

        private void CancelTimersFor(Guid matchId, Guid playerId, CancellationTokenCategory category)
        {
            var tokenKey = ConstructTokenKey(playerId, matchId, category);
            _cancellationTokenService.Cancel(tokenKey);
        }

        private void CancelTimersFor(Guid matchId, CancellationTokenCategory category)
        {
            _cancellationTokenService.CancelForMatch(matchId, category);
        }

        #endregion Turn Timers

        #region Private Members

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

        private async Task SendGameState(
            string serverEventName,
            IMatchSessionModel matchSession,
            CancellationToken cancellationToken = default)
        {
            var gameSessionPlayer1 = matchSession.GetGameState(matchSession.Player1.Id);
            var player1Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer1);
            await SendToClientAsync(matchSession.Player1.ConnectionId, serverEventName, player1Contract, cancellationToken);

            if (!matchSession.Player2.IsBot)
            {
                var gameSessionPlayer2 = matchSession.GetGameState(matchSession.Player2.Id);
                var player2Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer2);
                await SendToClientAsync(matchSession.Player2.ConnectionId, serverEventName, player2Contract, cancellationToken);
            }
        }

        private async Task SendMatchState(MatchSessionLease matchLock, string serverEventName, IMatchSessionModel match)
        {
            try
            {
                // we put the results in the work queue if applicable
                // We await the call in order to ensure that the events are properly published
                await EnqueueResultsForProcessingAsync(match, serverEventName);

                var payloadPlayer1 = match.ToPayload(match.Player1.Id);
                payloadPlayer1.AppendRatingUrl(match.Player1.Id, serverEventName);
                var contractPlayer1 = new EventResponseContract<EventMatchStatePayload>(serverEventName, payloadPlayer1);
                await SendToClientAsync(match.Player1.ConnectionId, serverEventName, contractPlayer1);

                if (!match.Player2.IsBot)
                {
                    var payloadPlayer2 = match.ToPayload(match.Player2.Id);
                    payloadPlayer2.AppendRatingUrl(match.Player2.Id, serverEventName);
                    var contractPlayer2 = new EventResponseContract<EventMatchStatePayload>(serverEventName, payloadPlayer2);
                    await SendToClientAsync(match.Player2.ConnectionId, serverEventName, contractPlayer2);
                }
            }
            finally
            {
                if (serverEventName.Equals(ServerEventTypes.GameEndedEvent))
                {
                    // we do not know for sure which turn it was, so we cancel all active turn timers
                    CancelTimersFor(match.Id, CancellationTokenCategory.Turn);
                    // we set a timeout for the players to start the next game
                    StartTurnTimer(match, match.Player1.Id);
                    if (!matchLock.IsBotPlayer(match.Player2.Id))
                    {
                        StartTurnTimer(match, match.Player2.Id);
                    }
                }
                // Completed matches must not remain stored when publication or notification fails.
                else if (serverEventName.Equals(ServerEventTypes.MatchEndedEvent))
                {
                    await CleanUpMatchAsync(matchLock, match, ConstructGroupName(match.Id));
                }
            }
        }

        private async Task CleanUpMatchAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel match,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            CancelTimersFor(match.Id, CancellationTokenCategory.BotRecovery);

            if (await _lobbyCleanup.TryCleanupUnstartedAsync(match.Id, matchLock))
            {
                return;
            }

            try
            {
                try
                {
                    var response = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                    await SendToGroupAsync(groupName, ServerEventTypes.ForceDisconnectEvent, response, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to notify completed match {matchId}.", match.Id);
                }

                foreach (var player in new[] { match.Player1, match.Player2 })
                {
                    if (!player.IsBot && !string.IsNullOrEmpty(player.ConnectionId))
                    {
                        try
                        {
                            await RemoveFromGroupAsync(player.ConnectionId, groupName, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Unable to remove a connection from completed match {matchId}.", match.Id);
                        }
                    }
                }
            }
            finally
            {
                _matchmakingService.TryRemoveMatchLobby(match.Id);
                matchLock.TryRemove();

                if (!match.Player1.IsBot)
                {
                    _playerConnectionRepository.TryRemove(match.Player1.Id);
                }

                if (!match.Player2.IsBot)
                {
                    _playerConnectionRepository.TryRemove(match.Player2.Id);
                }

                CancelTimersFor(match.Id, CancellationTokenCategory.Turn);
                CancelTimersFor(match.Id, CancellationTokenCategory.Disconnect);
            }
        }

        private async Task EnqueueResultsForProcessingAsync(IMatchSessionModel match, string serverEventName)
        {
            try
            {
                var enqueueExceptions = new List<Exception>();
                if (serverEventName.Equals(ServerEventTypes.GameEndedEvent))
                {
                    try
                    {
                        var gameRound = GetLastConcludedGameRoundIndex(match);
                        var gameRecords = match.ToRecords(gameRound);
                        await _workQueue.EnqueueGameResultAsync(gameRecords, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_GAME_RESULT_ERROR", $"An error occurred while enqueuing the game result for match {match.Id}.");
                        enqueueExceptions.Add(e);
                    }
                }
                else if (serverEventName.Equals(ServerEventTypes.MatchEndedEvent))
                {
                    try
                    {
                        // we have to enqueue the last game of the match as well
                        var gameRound = GetLastConcludedGameRoundIndex(match);
                        var gameRecords = match.ToRecords(gameRound);
                        await _workQueue.EnqueueGameResultAsync(gameRecords, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_GAME_RESULT_ERROR", $"An error occurred while enqueuing the game result for match {match.Id}.");
                        enqueueExceptions.Add(e);
                    }

                    // we fetch the match records and their related games and histories once
                    var matchRecords = match.ToRecords();

                    try
                    {
                        // we process the match result
                        await _workQueue.EnqueueMatchResultAsync(matchRecords, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_MATCH_RESULT_ERROR", $"An error occurred while enqueuing the match result for match {match.Id}.");
                        enqueueExceptions.Add(e);
                    }
                    try
                    {
                        // we update the player stats based on the match result
                        await _workQueue.EnqueueStatProcessingAsync(matchRecords, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_STAT_PROCESSING_ERROR", $"An error occurred while enqueuing the stat processing for match {match.Id}.");
                        enqueueExceptions.Add(e);
                    }
                    // we update the player rating based on the match result if a ranked was played
                    if (match.Modus == MatchModus.Ranked)
                    {
                        try
                        {
                            await _workQueue.EnqueueRatingProcessingAsync(matchRecords, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_RATING_PROCESSING_ERROR", $"An error occurred while enqueuing the rating processing for match {match.Id}.");
                            enqueueExceptions.Add(e);
                        }
                    }
                }

                if (enqueueExceptions.Count != 0)
                {
                    throw new AggregateException("One or more errors occurred while enqueuing work tasks.", enqueueExceptions);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Logger.Error("Code {errorCode} :: Message {errorMessage}", "PROCESS_MATCH_RESULT_ERROR", $"The processing of match {match.Id} was cancelled.");
            }
            catch (Exception e)
            {
                // TODO: Failed publication can permanently lose a rating update.
                // TODO: Implement a peristent outbox pattern for guaranteed delivery
                var groupName = ConstructGroupName(match.Id);
                await SendErrorEventToGroupAsync("PROCESS_MATCH_RESULT_ERROR", "An error occurred while processing the match result. Some match data may be lost.", groupName, e);
            }
        }

        private static int GetLastConcludedGameRoundIndex(IMatchSessionModel match)
        {
            var gameSessions = match.GetGameSessions().ToList();
            gameSessions.Reverse();
            var lastConcludedGame = gameSessions.First(gr => gr != null && gr.Result.IsConcluded);
            return match.GetGameSessions().ToList().IndexOf(lastConcludedGame) + 1;
        }

        private async Task SendErrorEventAsync(string errorCode, string message, string? connectionId = null, Exception? exception = null)
        {
            var unWrappedException = UnWrapAggregateException(exception);
            var payload = new EventErrorPayload(errorCode, message, unWrappedException, ServerCommands.GameStateCommand, ServerCommands.MatchStateCommand);
            var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);

            if (!string.IsNullOrEmpty(connectionId))
            {
                await SendToClientAsync(connectionId, ServerEventTypes.ErrorEvent, contract);
            }
            else
            {
                await SendToCallerAsync(ServerEventTypes.ErrorEvent, contract);
            }

            Log.Logger.Error(unWrappedException, "ConnectionId {connId} :: Code {errorCode} :: Message {errorMessage}", connectionId, errorCode, message);
        }

        private async Task SendErrorEventToGroupAsync(
            string errorCode,
            string message,
            string groupName,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            var unWrappedException = UnWrapAggregateException(exception);
            var payload = new EventErrorPayload(errorCode, message, unWrappedException, ServerCommands.GameStateCommand, ServerCommands.MatchStateCommand);
            var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);
            await SendToGroupAsync(groupName, ServerEventTypes.ErrorEvent, contract, cancellationToken);
            Log.Logger.Error(unWrappedException, "Code {errorCode} :: Message {errorMessage}", errorCode, message);
        }

        private static Exception? UnWrapAggregateException(Exception? ex)
        {
            if (ex is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions.FirstOrDefault() ?? aggregateException;
            }
            return ex;
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

        private IBotService GetBotService()
        {
            return _serviceProvider.GetRequiredKeyedService<IBotService>(WellKnownBotServices.Mars);
        }

        private async Task SendToCallerAsync<T>(string serverEventName, EventResponseContract<T> response) where T : EventPayloadBase
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.ExternalDuration, "signalr.send");
            await Clients.Caller.SendAsync(serverEventName, response);
        }

        private async Task SendToClientAsync<T>(
            string connectionId,
            string serverEventName,
            EventResponseContract<T> response,
            CancellationToken cancellation = default) where T : EventPayloadBase
        {
            if (string.Equals(connectionId, WellKnownBotPlayers.ConnectionId, StringComparison.Ordinal))
            {
                return;
            }

            using var measurement = ServerMetrics.Measure(ServerMetrics.ExternalDuration, "signalr.send");
            try
            {
                await Clients.Client(connectionId).SendAsync(serverEventName, response, cancellation);
            }
            catch (ObjectDisposedException)
            {
                // if the hub instance already got disposed, we must use the hub context
                await _hubContext.Clients.Client(connectionId).SendAsync(serverEventName, response, cancellation);
            }
        }

        private async Task SendToGroupAsync<T>(
            string groupName,
            string serverEventName,
            EventResponseContract<T> response,
            CancellationToken cancellationToken = default) where T : EventPayloadBase
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.ExternalDuration, "signalr.send");
            try
            {
                await Clients.Group(groupName).SendAsync(serverEventName, response, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // if the hub instance already got disposed, we must use the hub context
                await _hubContext.Clients.Group(groupName).SendAsync(serverEventName, response, cancellationToken);
            }
        }

        private async Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(connectionId, WellKnownBotPlayers.ConnectionId, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                await Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
            }
        }

        private static string ConstructGroupName(Guid matchId)
        {
            return $"match_{matchId}";
        }

        private static CancellationTokenKey ConstructTokenKey(Guid playerId, Guid matchId, CancellationTokenCategory category)
        {
            return new CancellationTokenKey
            {
                PlayerId = playerId,
                MatchId = matchId,
                Category = category
            };
        }

        #endregion Private Members
    }
}
