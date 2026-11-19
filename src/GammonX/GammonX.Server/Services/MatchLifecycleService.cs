using GammonX.Diagnostics.Server;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Extensions;
using GammonX.Server.Models;
using GammonX.Server.Bot;
using GammonX.Server.Queue;

using Microsoft.AspNetCore.SignalR;

using Serilog;

using MatchSessionLease = GammonX.Server.Services.MatchSessionRepository.MatchSessionLease;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Runs match transitions and publishes their results for hub commands and host-owned deadlines.
    /// </summary>
    internal sealed partial class MatchLifecycleService(
        ICancellationTokenService _cancellationTokenService,
        IMatchmakingService _matchmakingService,
        MatchSessionRepository _matchRepository,
        MatchLobbyCleanupService _lobbyCleanup,
        PlayerConnectionRepository _playerConnectionRepository,
        IWorkQueueService _workQueue,
        IHubContext<MatchLobbyHub> _hubContext,
        TimeProvider _timeProvider,
        BotTurnRecoveryService _botRecovery,
        [FromKeyedServices(WellKnownBotServices.Mars)] IBotService? botService = null,
        IHostApplicationLifetime? lifetime = null) : IHostedService, IDisposable
    {
        internal async Task SendGameState(string serverEventName, IMatchSessionModel matchSession, CancellationToken cancellationToken = default)
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

        internal async Task SendMatchState(MatchSessionLease matchLock, string serverEventName, IMatchSessionModel match)
        {
            try
            {
                // we put the results in the work queue if applicable
                // We await the call in order to ensure that the events are properly published
                await EnqueueResultsForProcessingAsync(match, serverEventName);

                var payloadPlayer1 = match.ToPayload(match.Player1.Id);
                payloadPlayer1.AppendRatingUrl(match.Player1.Id, serverEventName);
                var contractPlayer1 = new EventResponseContract<EventMatchStatePayload>(serverEventName, payloadPlayer1);
                await SendToClientAsync(match.Player1.ConnectionId, serverEventName, contractPlayer1, _shutdown.Token);

                if (!match.Player2.IsBot)
                {
                    var payloadPlayer2 = match.ToPayload(match.Player2.Id);
                    payloadPlayer2.AppendRatingUrl(match.Player2.Id, serverEventName);
                    var contractPlayer2 = new EventResponseContract<EventMatchStatePayload>(serverEventName, payloadPlayer2);
                    await SendToClientAsync(match.Player2.ConnectionId, serverEventName, contractPlayer2, _shutdown.Token);
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
                    await CleanUpMatchAsync(matchLock, match, ConstructGroupName(match.Id), _shutdown.Token);
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

            if (await _lobbyCleanup.TryCleanupUnstartedAsync(match.Id, matchLock, cancellationToken))
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
                        await _workQueue.EnqueueGameResultAsync(gameRecords, _shutdown.Token);
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
                        await _workQueue.EnqueueGameResultAsync(gameRecords, _shutdown.Token);
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
                        await _workQueue.EnqueueMatchResultAsync(matchRecords, _shutdown.Token);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Code {errorCode} :: Message {errorMessage}", "ENQUEUE_MATCH_RESULT_ERROR", $"An error occurred while enqueuing the match result for match {match.Id}.");
                        enqueueExceptions.Add(e);
                    }
                    try
                    {
                        // we update the player stats based on the match result
                        await _workQueue.EnqueueStatProcessingAsync(matchRecords, _shutdown.Token);
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
                            await _workQueue.EnqueueRatingProcessingAsync(matchRecords, _shutdown.Token);
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
                await SendErrorEventToGroupAsync("PROCESS_MATCH_RESULT_ERROR", "An error occurred while processing the match result. Some match data may be lost.", groupName, e, _shutdown.Token);
            }
        }

        private static int GetLastConcludedGameRoundIndex(IMatchSessionModel match)
        {
            var gameSessions = match.GetGameSessions().ToList();
            gameSessions.Reverse();
            var lastConcludedGame = gameSessions.First(gr => gr != null && gr.Result.IsConcluded);
            return match.GetGameSessions().ToList().IndexOf(lastConcludedGame) + 1;
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

        internal static Exception? UnWrapAggregateException(Exception? ex)
        {
            if (ex is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions.FirstOrDefault() ?? aggregateException;
            }
            return ex;
        }

        internal async Task SendToClientAsync<T>(
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
            await _hubContext.Clients.Client(connectionId).SendAsync(serverEventName, response, cancellation);
        }

        internal async Task SendToGroupAsync<T>(
            string groupName,
            string serverEventName,
            EventResponseContract<T> response,
            CancellationToken cancellationToken = default) where T : EventPayloadBase
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.ExternalDuration, "signalr.send");
            await _hubContext.Clients.Group(groupName).SendAsync(serverEventName, response, cancellationToken);
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

            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
        }

        internal static string ConstructGroupName(Guid matchId)
        {
            return $"match_{matchId}";
        }

        internal static CancellationTokenKey ConstructTokenKey(Guid playerId, Guid matchId, CancellationTokenCategory category)
        {
            return new CancellationTokenKey
            {
                PlayerId = playerId,
                MatchId = matchId,
                Category = category
            };
        }
    }
}
