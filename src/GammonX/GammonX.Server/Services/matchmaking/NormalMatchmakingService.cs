using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Simple matchmaking service that allows players to join a queue and find matches.
    /// </summary>
    internal class NormalMatchmakingService : MatchmakingServiceBaseImpl
    {
        public NormalMatchmakingService(PlayerConnectionRepository playerConnectionRepository)
            : base(playerConnectionRepository)
        {
            // pass
        }

        // <inheritdoc />
        public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
        {
            if (queueKey.MatchModus != MatchModus.Normal)
            {
                throw new InvalidOperationException("match modus must be of type normal in order to join this queue");
            }

            if (_queue.Any(ml => ml.Value.PlayerId == playerId))
            {
                throw new InvalidOperationException($"Player '{playerId}' is already part of a match lobby queue");
            }

            // add player to queue
            var entry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 0);
            Enqueue(entry);
            return Task.FromResult(entry);
        }

        // <inheritdoc />
        public override Task MatchQueuedPlayersAsync()
        {
            foreach (var (queueKey, queue) in _modeQueues)
            {
                while (queue.TryDequeue(out var idA))
                {
                    // we dequeue entry ids from the mode queue and do not readd them if no related entry is found
                    if (!_queue.TryGetValue(idA, out var entryA))
                        continue;

                    if (!queue.TryDequeue(out var idB))
                    {
                        // we requeue a and cancel the active search iteration if no other player is queued up
                        queue.Enqueue(idA);
                        break;
                    }

                    if (!_queue.TryGetValue(idB, out var entryB))
                    {
                        // we requeue entry a if no entry b is found
                        queue.Enqueue(idA);
                        continue;
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

                    if (claimedA && claimedB)
                    {
                        // create lobby
                        var playerConnectionA = _playerConnectionRepository.GetOrCreate(entryA.PlayerId);
                        var playerConnectionB = _playerConnectionRepository.GetOrCreate(entryB.PlayerId);
                        var lobby = new MatchLobby(Guid.NewGuid(), queueKey, playerConnectionA);
                        lobby.Join(playerConnectionB);
                        _matchLobbies[entryA] = lobby;
                        _matchLobbies[entryB] = lobby;
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
