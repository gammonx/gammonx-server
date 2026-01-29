using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Repository;

namespace GammonX.Server.Services
{
    // <inheritdoc />
    internal class RankedMatchmakingService : MatchmakingServiceBaseImpl
    {
        private readonly IRepositoryClient _repoClient;

        public RankedMatchmakingService(IRepositoryClient client)
        {
            _repoClient = client;
        }

        // <inheritdoc />
        public override async Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
        {
            if (queueKey.MatchModus != MatchModus.Ranked)
            {
                throw new InvalidOperationException("Match modus must be of type normal in order to join this queue");
            }

            if (_queue.Any(ml => ml.Value.PlayerId == playerId || ml.Value.PlayerId == playerId))
            {
                throw new InvalidOperationException($"Player '{playerId}' is already part of a match lobby queue");
            }

            var ratingResponse = await _repoClient.GetRatingAsync(playerId, queueKey.MatchVariant, CancellationToken.None);
            // use default glicko2 rating if player has no rating yet
            var relevantRating = ratingResponse?.Rating ?? 1200;

            var entry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, relevantRating);
            Enqueue(entry);
            return entry;
        }

        // <inheritdoc />
        public override Task MatchQueuedPlayersAsync()
        {
            foreach (var (queueKey, queue) in _modeQueues)
            {
                while (queue.TryDequeue(out var idA))
                {
                    if (!_queue.TryGetValue(idA, out var entryA))
                        continue;

                    // try to find a partner
                    if (!TryFindRankedPartner(queueKey, entryA, out var entryB))
                    {
                        // couldn't match now > requeue
                        queue.Enqueue(idA);
                        break;
                    }

                    // atomic claim
                    if (!_queue.TryRemove(entryA.Id, out _) || !_queue.TryRemove(entryB.Id, out _))
                    {
                        // rollback if needed
                        _queue.TryAdd(entryA.Id, entryA);
                        _queue.TryAdd(entryB.Id, entryB);
                        continue;
                    }

                    // create lobby
                    var lobbyEntryA = new LobbyEntry(entryA.PlayerId);
                    var lobbyEntryB = new LobbyEntry(entryB.PlayerId);
                    var lobby = new MatchLobby(Guid.NewGuid(), queueKey, lobbyEntryA);
                    lobby.Join(lobbyEntryB);
                    _matchLobbies[entryA] = lobby;
                    _matchLobbies[entryB] = lobby;
                }
            }

            return Task.CompletedTask;
        }

        private bool TryFindRankedPartner(QueueKey key, QueueEntry entryA, out QueueEntry entryB)
        {
            // TODO Two-pointer scan (O(n))
            entryB = null!;

            if (!_modeQueues.TryGetValue(key, out var queue))
                return false;

            var snapshot = queue
                .Select(id => _queue.TryGetValue(id, out var e) ? e : null)
                .Where(e => e != null)
                .OrderBy(e => e?.CurrentRating ?? 0)
                .ToList();

            foreach (var entry in snapshot)
            {
                if (entry == null || !_queue.TryGetValue(entry.Id, out var candidate))
                    continue;

                var diff = Math.Abs(entryA.CurrentRating - candidate.CurrentRating);
                var allowed = Math.Min(GetAllowedRatingDifference(entryA), GetAllowedRatingDifference(candidate));

                if (diff <= allowed)
                {
                    entryB = candidate;
                    return true;
                }
            }

            return false;
        }

        private static double GetAllowedRatingDifference(QueueEntry entry)
        {
            var secondsWaiting = (DateTime.UtcNow - entry.EnqueuedAtUtc).TotalSeconds;
            return secondsWaiting switch
            {
                < 5 => 50,
                < 15 => 100,
                < 30 => 200,
                _ => 400
            };
        }
    }
}
