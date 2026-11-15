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
        /// <summary>
        /// Expires old lobbies in the supplied matchmaking mode.
        /// </summary>
        public async Task CleanupExpiredAsync(IMatchmakingService mode, DateTime utcNow)
        {
            foreach (var candidate in mode.GetMatchLobbies().DistinctBy(lobby => lobby.MatchId))
            {
                if (utcNow - candidate.CreatedAtUtc < MatchLobby.StartTimeout)
                {
                    continue;
                }

                await using var lease = await matches.AcquireStateAsync(candidate.MatchId);
                
                // Joining/start and expiry share this lease. Recheck after waiting for a start.
                if (matchmaking.TryFindMatchLobby(candidate.MatchId, out var current) &&
                    current != null && utcNow - current.CreatedAtUtc >= MatchLobby.StartTimeout)
                {
                    await TryCleanupUnstartedAsync(candidate.MatchId, lease);
                }
            }
        }

        /// <summary>
        /// Removes pre-start state while the caller holds the specified matchs lease.
        /// </summary>
        /// <returns>
        /// True if an unstarted lobby or session was removed; false if absent or started.
        /// </returns>
        public async Task<bool> TryCleanupUnstartedAsync(Guid matchId, MatchSessionLease lease)
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
                await hub.Clients.Group(groupName).SendAsync(ServerEventTypes.ForceDisconnectEvent, response);
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
                        await hub.Groups.RemoveFromGroupAsync(player.ConnectionId, groupName);
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
