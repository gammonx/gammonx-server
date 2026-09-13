using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Extensions;
using GammonX.Server.Models;

namespace GammonX.Server.Queue
{
    /// <summary>
    /// Provides the capabilities to trigger different work queues.
    /// </summary>
    public interface IWorkQueueService
    {
        /// <summary>
        /// Enqueues the <paramref name="gameRound"/> of the given <paramref name="match"/>.
        /// </summary>
        /// <param name="match">Match containing the game.</param>
        /// <param name="gameRound">Game round index.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueGameResultAsync(IMatchSessionModel match, int gameRound, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="match"/> result.
        /// </summary>
        /// <param name="match">Match to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueMatchResultAsync(IMatchSessionModel match, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="match"/> result for processing player stats.
        /// </summary>
        /// <param name="match">Match to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueStatProcessingAsync(IMatchSessionModel match, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="match"/> result for processing player rating update.
        /// </summary>
        /// <param name="match">Match to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueRatingProcessingAsync(IMatchSessionModel match, CancellationToken cancellationToken);
    }

    // <inheritdoc />
    public class WorkQueueService : IWorkQueueService
    {
        private readonly IServiceProvider _services;

        public WorkQueueService(IServiceProvider services)
        {
            _services = services;
        }

        // <inheritdoc />
        public async Task EnqueueGameResultAsync(IMatchSessionModel match, int gameRound, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.GameCompleted);
            var player1GameRecord = match.ToRecord(gameRound, match.Player1.Id);
            var player2GameRecord = match.ToRecord(gameRound, match.Player2.Id);
            // We enqueue the game results for both players at the same time and process them transactional
            var work = new GameCompletedWorkContract { Records = [player1GameRecord, player2GameRecord] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueMatchResultAsync(IMatchSessionModel match, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.MatchCompleted);
            var player1MatchRecord = match.ToRecord(match.Player1.Id);
            var player2MatchRecord = match.ToRecord(match.Player2.Id);
            // We enqueue the match results for both players at the same time and process them transactional
            var work = new MatchCompletedWorkContract { Records = [player1MatchRecord, player2MatchRecord] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueRatingProcessingAsync(IMatchSessionModel match, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.RatingUpdated);
            var player1MatchRecord = match.ToRecord(match.Player1.Id);
            var player2MatchRecord = match.ToRecord(match.Player2.Id);
            // We enqueue the rating updates for both players at the same time and process them transactional
            var work = new RatingUpdateWorkContract { Records = [player1MatchRecord, player2MatchRecord] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueStatProcessingAsync(IMatchSessionModel match, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.StatsUpdated);
            var player1MatchRecord = match.ToRecord(match.Player1.Id);
            var player2MatchRecord = match.ToRecord(match.Player2.Id);
            var matchRecords = new[] { player1MatchRecord, player2MatchRecord };
            await workQueue.EnqueueBatchAsync(matchRecords, cancellationToken);
        }

        private IWorkQueue GetWorkQueue(WorkQueueType queueType)
        {
            var workQueue = _services.GetKeyedService<IWorkQueue>(queueType);
            if (workQueue == null)
            {
                // return default queue logger
                return _services.GetRequiredService<IWorkQueue>();
            }
            return workQueue;
        }
    }
}
