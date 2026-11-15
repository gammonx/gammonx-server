using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Repository;

namespace GammonX.Server.Services
{
    // <inheritdoc />
    internal class RankedMatchmakingService : MatchmakingServiceBaseImpl
    {
        private readonly IRepositoryClient _repoClient;

        private readonly SemaphoreSlim _matchingLock = new(1, 1);

        private static readonly BotLevel[] BotLevels = { BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.Expert };

        // <inheritdoc />
        public override MatchModus Modus => MatchModus.Ranked;

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
                throw new InvalidOperationException("Match modus must be of type ranked in order to join this queue");
            }

            if (_queue.Any(ml => ml.Value.PlayerId == playerId))
            {
                throw new InvalidOperationException($"Player '{playerId}' is already part of a match lobby queue");
            }

            var ratingResponse = await _repoClient.GetRatingAsync(playerId, queueKey.MatchVariant, queueKey.MatchType, CancellationToken.None);
            // use default glicko2 rating if player has no rating yet
            var relevantRating = ratingResponse?.Rating ?? 1200;

            var entry = new QueueEntry(Guid.NewGuid(), playerId, queueKey with { BotLevel = BotLevel.Unknown }, DateTime.UtcNow, relevantRating);

            Enqueue(entry);

            return entry;
        }

        // <inheritdoc />
        public override async Task MatchQueuedPlayersAsync()
        {
            // Rating reads yield to other callers. Only one pass may claim opponents at a time.
            await _matchingLock.WaitAsync();

            try
            {
                var ratingsByKey = new Dictionary<QueueKey, (BotLevel Level, double Rating)[]>();

                foreach (var (queueKey, queue) in _modeQueues.Where(mq => mq.Key.MatchModus == Modus))
                {
                    var count = queue.Count;

                    for (var i = 0; i < count && queue.TryDequeue(out var idA); i++)
                    {
                        if (!_queue.TryGetValue(idA, out var entryA))
                        {
                            continue;
                        }

                        // We first try to match to a human player
                        if (TryFindRankedPartner(queueKey, entryA, out var entryB))
                        {
                            var claimedA = _queue.TryRemove(entryA.Id, out _);
                            var claimedB = _queue.TryRemove(entryB.Id, out _);
                            if (!claimedA || !claimedB)
                            {
                                if (claimedA)
                                {
                                    Enqueue(entryA);
                                }
                                if (claimedB)
                                {
                                    Enqueue(entryB);
                                }
                                continue;
                            }

                            var tmpQueueKey = queueKey with { BotLevel = BotLevel.Unknown };
                            var player1Conn = _playerConnectionRepository.GetOrCreate(entryA.PlayerId);
                            var player2Conn = _playerConnectionRepository.GetOrCreate(entryB.PlayerId);
                            var matchId = Guid.NewGuid();
                            var lobby = new MatchLobby(matchId, tmpQueueKey, player1Conn);

                            lobby.Join(player2Conn);
                            _matchLobbies[entryA] = lobby;
                            _matchLobbies[entryB] = lobby;

                            continue;
                        }

                        var ratingKey = queueKey with { BotLevel = BotLevel.Unknown };

                        // We try then to match the current searching player against a fitting bot opponent
                        try
                        {
                            if (!ratingsByKey.TryGetValue(ratingKey, out var ratings))
                            {
                                // We update the cache bot level ranked ratings
                                ratings = await GetBotRatingsAsync(ratingKey);
                                ratingsByKey[ratingKey] = ratings;
                            }

                            var level = ratings.Length == 0
                                ? GetFallbackBotLevel(entryA.CurrentRating)
                                : ratings.OrderBy(rating => Math.Abs(rating.Rating - entryA.CurrentRating)).First().Level;

                            // Cancellation and expiry can remove the entry while rating reads are in flight.
                            if (!_queue.TryRemove(entryA.Id, out _))
                            {
                                continue;
                            }

                            var bot = _playerConnectionRepository.GetOrCreate(WellKnownBotPlayers.GetBotPlayerId(level));
                            // We use an empty guid for the bot connection id as well known value
                            bot.SetConnectionId(WellKnownBotPlayers.ConnectionId);
                            var matchId = Guid.NewGuid();
                            // We set the bot level for this ranked
                            var tmpRatingKey = ratingKey with { BotLevel = level };
                            var player1Conn =  _playerConnectionRepository.GetOrCreate(entryA.PlayerId);
                            var lobby = new MatchLobby(matchId, tmpRatingKey, player1Conn);

                            lobby.Join(bot);
                            _matchLobbies[entryA] = lobby;
                        }
                        catch
                        {
                            if (_queue.ContainsKey(entryA.Id))
                            {
                                queue.Enqueue(entryA.Id);
                            }

                            throw;
                        }
                    }
                }
            }
            finally
            {
                _matchingLock.Release();
            }
        }

        private async Task<(BotLevel Level, double Rating)[]> GetBotRatingsAsync(QueueKey key)
        {
            var responses = await Task.WhenAll(BotLevels.Select(level =>
            {
                var botPlayerId = WellKnownBotPlayers.GetBotPlayerId(level);
                return _repoClient.GetRatingAsync(botPlayerId, key.MatchVariant, key.MatchType, CancellationToken.None);
            }));

            // Preserve level order for equal-distance ties. Missing metadata from older APIs is not a recorded rating.
            return responses.Select((response, index) => (Response: response, Level: BotLevels[index]))
                .Where(value => value.Response?.HasRecordedRating == true && double.IsFinite(value.Response.Rating))
                .Select(value => (value.Level, value.Response!.Rating))
                .ToArray();
        }

        private static BotLevel GetFallbackBotLevel(double rating) => rating switch
        {
            < 1000 => BotLevel.Easy,
            < 1200 => BotLevel.Medium,
            < 1500 => BotLevel.Hard,
            _ => BotLevel.Expert
        };

        private bool TryFindRankedPartner(QueueKey key, QueueEntry entryA, out QueueEntry entryB)
        {
            entryB = null!;

            if (!_modeQueues.TryGetValue(key, out var queue))
            {
                return false;
            }

            var count = queue.Count;
            for (var i = 0; i < count && queue.TryDequeue(out var entryId); i++)
            {
                if (!_queue.TryGetValue(entryId, out var candidate))
                {
                    continue;
                }

                var diff = Math.Abs(entryA.CurrentRating - candidate.CurrentRating);
                var allowedDiffA = GetAllowedRatingDifference(entryA);
                var allowedDiffCanidate = GetAllowedRatingDifference(candidate);
                var allowed = Math.Min(allowedDiffA, allowedDiffCanidate);

                if (candidate.PlayerId != entryA.PlayerId && diff <= allowed)
                {
                    entryB = candidate;
                    return true;
                }

                queue.Enqueue(entryId);
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
