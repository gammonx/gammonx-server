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

            if (_queueEntries.Any(ml => ml.Value.PlayerId == playerId))
            {
                throw new InvalidOperationException($"Player '{playerId}' is already part of a match lobby queue");
            }

            var ratingResponse = await _repoClient.GetRatingAsync(playerId, queueKey.MatchVariant, queueKey.MatchType, CancellationToken.None);
            // use default glicko2 rating if player has no rating yet
            var relevantRating = ratingResponse?.Rating ?? 1200;

            // we normalize the bot level here for the ranked queue
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
                        if (!_queueEntries.TryGetValue(idA, out var entryA))
                        {
                            continue;
                        }

                        // We first try to match to a human player
                        if (TryFindRankedPartner(queueKey, entryA, out var entryB))
                        {
                            MatchQueuedEntries(queueKey with { BotLevel = BotLevel.Unknown }, entryA, entryB);

                            continue;
                        }

                        var ratingKey = queueKey with { BotLevel = BotLevel.Unknown };

                        if (!ratingsByKey.TryGetValue(ratingKey, out var ratings))
                        {
                            try
                            {
                                ratings = await GetBotRatingsAsync(ratingKey);
                                ratingsByKey[ratingKey] = ratings;
                            }
                            catch
                            {
                                // Retry the dequeued player on the next pass if the rating service failed.
                                queue.Enqueue(entryA.Id);
                                throw;
                            }
                        }

                        
                        var level = ratings.Length == 0
                            ? GetFallbackBotLevel(entryA.CurrentRating)
                            : ratings.OrderBy(rating => Math.Abs(rating.Rating - entryA.CurrentRating)).First().Level;
                        
                        // We try to match the searching player with an equivalent bot player
                        MatchQueuedEntries(ratingKey with { BotLevel = level }, entryA);
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
                if (!_queueEntries.TryGetValue(entryId, out var candidate))
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
