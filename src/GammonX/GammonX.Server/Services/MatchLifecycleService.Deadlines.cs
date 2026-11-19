using GammonX.Diagnostics.Server;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Serilog;

using MatchSessionLease = GammonX.Server.Services.MatchSessionRepository.MatchSessionLease;

namespace GammonX.Server.Services
{
    internal sealed partial class MatchLifecycleService
    {
        private readonly Lock _taskGate = new();
        
        private readonly HashSet<Task> _pending = new();
        
        private readonly CancellationTokenSource _shutdown = CancellationTokenSource.CreateLinkedTokenSource(lifetime?.ApplicationStopping ?? CancellationToken.None);
        
        private bool _stopping;

        internal DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Task[] pending;
            lock (_taskGate)
            {
                _stopping = true;
                pending = _pending.ToArray();
            }

            await _shutdown.CancelAsync();
            await Task.WhenAll(pending).WaitAsync(cancellationToken);
        }

        public void Dispose() => _shutdown.Dispose();

        private Task RunOwnedAsync(CancellationTokenKey key, CancellationTokenSource generation, Func<CancellationToken, Task> run)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool accepted;
            lock (_taskGate)
            {
                accepted = !_stopping && !_shutdown.IsCancellationRequested;
                if (accepted)
                {
                    _pending.Add(completion.Task);
                }
            }

            if (!accepted)
            {
                _cancellationTokenService.TryCancel(key, generation);
                generation.Dispose();
                return Task.CompletedTask;
            }

            // Ambient request state must not flow into a long-lived deadline continuation.
            if (ExecutionContext.IsFlowSuppressed())
            {
                return ExecuteAsync();
            }
            using (ExecutionContext.SuppressFlow())
            {
                return ExecuteAsync();
            }

            async Task ExecuteAsync()
            {
                ServerMetrics.TimerTaskStarted();
                try
                {
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(generation.Token, _shutdown.Token);
                    try
                    {
                        await run(cancellation.Token);
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                    {
                        // The registration was superseded or the host is stopping.
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Deadline {Category} failed for match {MatchId}, player {PlayerId}.", key.Category, key.MatchId, key.PlayerId);
                }
                finally
                {
                    _cancellationTokenService.TryCancel(key, generation);
                    generation.Dispose();
                    ServerMetrics.TimerTaskFinished();
                    lock (_taskGate)
                    {
                        _pending.Remove(completion.Task);
                        completion.SetResult();
                    }
                }
            }
        }

        internal Task StartDisconnectGraceTimer(PlayerConnection playerConnection, Guid matchId)
        {
            var playerId = playerConnection.Id;
            var connectionId = playerConnection.ConnectionId!;
            var due = _timeProvider.GetUtcNow() + playerConnection.DisconnectGracePeriod;
            var key = ConstructTokenKey(playerId, matchId, CancellationTokenCategory.Disconnect);
            var generation = new CancellationTokenSource();
            _cancellationTokenService.Store(key, generation);

            return RunOwnedAsync(key, generation, async cancellation =>
            {
                await DelayUntilAsync(due, cancellation);
                var retainMarker = await HandlePermanentDisconnectAsync(playerId, matchId, connectionId, generation, cancellation);
                if (retainMarker)
                {
                    // Keep this generation until reconnect or the other player's expiry resolves the match.
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
                }
            });
        }

        internal async Task<bool> HandlePermanentDisconnectAsync(
            Guid playerId, 
            Guid matchId, 
            string connectionId,
            CancellationTokenSource timer, 
            CancellationToken cancellation = default)
        {
            using var callback = new ServerMetrics.CallbackScope();
            await using var matchLock = await _matchRepository.AcquireStateWithCancellationAsync(matchId, cancellation);
            cancellation.ThrowIfCancellationRequested();
            return await ResolveDisconnectExpiryAsync(matchLock, playerId, matchId, connectionId, timer);
        }

        internal async Task<bool> ResolveDisconnectExpiryAsync(
            MatchSessionLease matchLock, 
            Guid playerId, 
            Guid matchId, 
            string connectionId, 
            CancellationTokenSource timer)
        {
            var tokenKey = ConstructTokenKey(playerId, matchId, CancellationTokenCategory.Disconnect);
            var connection = _playerConnectionRepository.Get(playerId);
            // Expiry may have queued behind a reconnect or a new disconnect. Validate its generation
            // only after acquiring the lease, before removing connections or changing the match.
            var sameConnection = string.Equals(connection?.ConnectionId, connectionId, StringComparison.Ordinal);
            var sameTimer = _cancellationTokenService.TryGet(tokenKey, out var currentTimer) && ReferenceEquals(timer, currentTimer);
            if (timer.IsCancellationRequested || !sameTimer || connection == null || connection.IsBot || !sameConnection)
            {
                return false;
            }

            var matchToResignClone = matchLock.Get();
            var participant = matchToResignClone?.Player1.Id == playerId || matchToResignClone?.Player2.Id == playerId;
            if (matchToResignClone != null && !participant)
            {
                return false;
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
                    if (otherConnection != null && UtcNow - otherConnection.LastSeenUtc >= otherConnection.DisconnectGracePeriod)
                    {
                        // Both deadlines elapsed, no winner or rated result, regardless of callback order.
                        await CleanUpMatchAsync(matchLock, matchToResignClone, ConstructGroupName(matchId), _shutdown.Token);
                        return false;
                    }
                    // Retain this registration as the disconnected marker until the other player resolves.
                    return true;
                }
            }

            // We see that the player surpassed their grace period and the other player wins
            _cancellationTokenService.Cancel(tokenKey);
            if (matchToResignClone?.StartedAt == null && await _lobbyCleanup.TryCleanupUnstartedAsync(matchId, matchLock, _shutdown.Token))
            {
                return false;
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
                await SendToGroupAsync(ConstructGroupName(matchId), ServerEventTypes.ForceDisconnectEvent, emptyResponse, _shutdown.Token);
            }
            return false;
        }

        internal void StartTurnTimer(IMatchSessionModel match, Guid playerId)
        {
            if (RegisterTurnDeadline(match, playerId) is { } deadline)
            {
                _ = RunTurnTimerAsync(deadline);
            }
        }

        internal void StartJoinTimer(MatchLobby lobby, IMatchSessionModel? match, Guid playerId, string connectionId)
        {
            if (RegisterJoinDeadline(lobby, match, playerId, connectionId) is { } deadline)
            {
                _ = RunTurnTimerAsync(deadline);
            }
        }

        internal Task RunTurnTimerAsync(TurnDeadline deadline)
        {
            return RunOwnedAsync(deadline.Key, deadline.Token, async cancellation =>
            {
                var warning = SendTurnWarningAsync(deadline, cancellation);
                try
                {
                    await DelayUntilAsync(deadline.ExpiresAt, cancellation);
                    await HandleTurnTimeoutAsync(deadline, cancellation);
                }
                finally
                {
                    _cancellationTokenService.TryCancel(deadline.Key, deadline.Token);
                    await warning;
                }
            });
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

        internal async Task HandleTurnTimeoutAsync(TurnDeadline deadline, CancellationToken cancellation = default)
        {
            using var callback = new ServerMetrics.CallbackScope();
            var (matchId, playerId) = (deadline.Key.MatchId, deadline.Key.PlayerId);
            await using var matchLock = await _matchRepository.AcquireStateWithCancellationAsync(matchId, cancellation);
            cancellation.ThrowIfCancellationRequested();
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

        internal void CancelTimersFor(Guid matchId, Guid playerId, CancellationTokenCategory category)
        {
            var tokenKey = ConstructTokenKey(playerId, matchId, category);
            _cancellationTokenService.Cancel(tokenKey);
        }

        internal void CancelTimersFor(Guid matchId, CancellationTokenCategory category)
        {
            _cancellationTokenService.CancelForMatch(matchId, category);
        }
    }
}
