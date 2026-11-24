using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Microsoft.AspNetCore.SignalR;

using Serilog;

using MatchSessionLease = GammonX.Server.Services.MatchSessionRepository.MatchSessionLease;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Removes abandoned pre-start lobbies without producing competitive results.
    /// </summary>
    public sealed class MatchLobbyCleanupService(
        IMatchmakingService matchmaking,
        MatchSessionRepository matches,
        PlayerConnectionRepository connections,
        ICancellationTokenService timers,
        IHubContext<MatchLobbyHub> hub)
    {
        private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Expires old lobbies within one time budget per pass. Busy lobbies are retried on the next pass.
        /// </summary>
        /// <param name="mode">Matchmaking service whose lobbies will be checked.</param>
        /// <param name="utcNow">UTC time used to determine whether a lobby has expired.</param>
        /// <param name="cancellationToken">Cancels the pass when the worker stops.</param>
        public async Task CleanupExpiredAsync(IMatchmakingService mode, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(CleanupTimeout);
            
            try
            {
                // A busy match must not prevent other expired lobbies from releasing their players.
                var expired = mode.GetMatchLobbies()
                    .TakeWhile(_ => !deadline.IsCancellationRequested)
                    .Where(lobby => utcNow - lobby.CreatedAtUtc >= MatchLobby.StartTimeout);
                
                await Task.WhenAll(expired.Select(lobby => CleanupLobbyAsync(lobby.MatchId, utcNow, deadline.Token)));
                
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                Log.Debug("Expired lobby cleanup reached its time budget of {CleanupTimeout}.", CleanupTimeout);
            }
        }

        private async Task CleanupLobbyAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken)
        {
            await using var lease = await matches.AcquireStateAsync(matchId, cancellationToken);
            // Joining/start and expiry share this lease. Recheck after waiting for a start.
            if (!matchmaking.TryFindMatchLobby(matchId, out var current) ||
                current == null || utcNow - current.CreatedAtUtc < MatchLobby.StartTimeout)
            {
                return;
            }

            if (lease.HasStarted())
            {
                matchmaking.TryRemoveMatchLobby(matchId);
                return;
            }

            await TryCleanupUnstartedAsync(matchId, lease, cancellationToken);
        }

        /// <summary>
        /// Removes pre-start state while the caller holds the specified matchs lease.
        /// </summary>
        /// <returns>
        /// True if an unstarted lobby or session was removed; false if absent or started.
        /// </returns>
        public Task<bool> TryCleanupUnstartedAsync(Guid matchId, MatchSessionLease lease)
        {
            return TryCleanupUnstartedAsync(matchId, lease, CancellationToken.None);
        }

        internal async Task<bool> TryCleanupUnstartedAsync(Guid matchId, MatchSessionLease lease, CancellationToken cancellationToken)
        {
            var match = lease.Get();
            if (match?.StartedAt != null)
            {
                return false;
            }

            matchmaking.TryFindMatchLobby(matchId, out var lobby);
            if (match == null && lobby == null)
            {
                return false;
            }

            // Include lobby participants who never joined the session. Bots are shared profiles.
            var playerIds = new[] { lobby?.Player1.Id, lobby?.Player2?.Id, match?.Player1.Id, match?.Player2.Id }
                .Where(id => id.HasValue && id.Value != Guid.Empty).Select(id => id!.Value).Distinct().ToArray();
            var players = playerIds.Select(connections.Get).Where(player => player != null && !player.IsBot).ToArray();
            var groupName = $"match_{matchId}";

            matchmaking.TryRemoveMatchLobby(matchId);
            lease.TryRemove();
            timers.CancelForMatch(matchId, CancellationTokenCategory.Turn);
            timers.CancelForMatch(matchId, CancellationTokenCategory.Disconnect);

            foreach (var player in players)
            {
                // Another participation is possible today, dont remove its shared connection.
                if (matches.GetPlayersMatch(player!.Id) == null &&
                    !matchmaking.GetQueueEntries().Any(entry => entry.PlayerId == player.Id) &&
                    !matchmaking.GetMatchLobbies().Any(other => other.Player1.Id == player.Id || other.Player2?.Id == player.Id))
                {
                    connections.TryRemove(player.Id);
                }
            }

            // Cleanup must complete even if a client cannot receive its final notification.
            try
            {
                var response = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnectEvent, new EmptyEventPayload());
                await hub.Clients.Group(groupName).SendAsync(ServerEventTypes.ForceDisconnectEvent, response, cancellationToken).WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to notify expired lobby {matchId}.", matchId);
            }

            foreach (var player in players)
            {
                if (!string.IsNullOrEmpty(player!.ConnectionId))
                {
                    try
                    {
                        await hub.Groups.RemoveFromGroupAsync(player.ConnectionId, groupName, cancellationToken).WaitAsync(cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Unable to remove a connection from expired lobby {matchId}.", matchId);
                    }
                }
            }
            return true;
        }
    }
}
