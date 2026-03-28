using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Repository;

namespace GammonX.Server.Services
{
    // <inheritdoc />
    internal class RankedMatchmakingService : MatchmakingServiceBaseImpl
    {
        private readonly IRepositoryClient _repoClient;

        public RankedMatchmakingService(PlayerConnectionRepository playerConnectionRepository, IRepositoryClient client)
            : base(playerConnectionRepository)
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

                    var claimedA = false;
                    var claimedB = false;
                    // atomic claims
                    if (_queue.TryRemove(entryA.Id, out var _))
                    {
                        claimedA = true;
                    }
                    if (_queue.TryRemove(entryB.Id, out var _))
                    {
                        claimedB = true;
                    }

                    // rollback if one couldn't be claimed
                    if (!claimedA && !claimedB)
                    {
                        queue.Enqueue(idA);
                    }
                    else if (!claimedA)
                    {
                        // a could not be claimed > requeue b
                        _queue.TryAdd(entryB.Id, entryB);
                        queue.Enqueue(idA);
                    }
                    else if (!claimedB)
                    {
                        //b could not be claimed > requeue a
                        _queue.TryAdd(entryA.Id, entryA);
                        queue.Enqueue(idA);
                    }

                    // create lobby
                    var playerConnectionA = _playerConnectionRepository.Create(entryA.PlayerId);
                    var playerConnectionB = _playerConnectionRepository.Create(entryB.PlayerId);
                    var lobby = new MatchLobby(Guid.NewGuid(), queueKey, playerConnectionA);
                    lobby.Join(playerConnectionB);
                    _matchLobbies[entryA] = lobby;
                    _matchLobbies[entryB] = lobby;
                }
            }

            return Task.CompletedTask;
        }

        private bool TryFindRankedPartner(QueueKey key, QueueEntry entryA, out QueueEntry entryB)
        {
            entryB = null!;

            if (!_modeQueues.TryGetValue(key, out var queue))
                return false;

            while (queue.TryDequeue(out var entryId))
            {
                if (!_queue.TryGetValue(entryId, out var candidate))
                    continue;

                var diff = Math.Abs(entryA.CurrentRating - candidate.CurrentRating);
                var allowed = Math.Min(GetAllowedRatingDifference(entryA), GetAllowedRatingDifference(candidate));

                if (diff <= allowed)
                {
                    entryB = candidate;
                    return true;
                }
                else
                {
                    // we put the entry back into the queue if the difference is to high
                    queue.Enqueue(entryId);
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
