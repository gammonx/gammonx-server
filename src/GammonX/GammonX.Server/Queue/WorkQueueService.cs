using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Extensions;

namespace GammonX.Server.Queue
{
    /// <summary>
    /// Provides the capabilities to trigger different work queues.
    /// </summary>
    public interface IWorkQueueService
    {
        /// <summary>
        /// Enqueues the <paramref name="records"/>.
        /// </summary>
        /// <param name="records">Game record contracts for player 1 and player 2.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueGameResultAsync(GameRecordContracts records, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="records"/> result.
        /// </summary>
        /// <param name="records">Match record contracts for player 1 and player 2.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueMatchResultAsync(MatchRecordContracts records, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="records"/> result for processing player stats.
        /// </summary>
        /// <param name="records">Match record contracts for player 1 and player 2.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueStatProcessingAsync(MatchRecordContracts records, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given <paramref name="records"/> result for processing player rating update.
        /// </summary>
        /// <param name="records">Match record contracts for player 1 and player 2.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueRatingProcessingAsync(MatchRecordContracts records, CancellationToken cancellationToken);
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
        public async Task EnqueueGameResultAsync(GameRecordContracts records, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.GameCompleted);
            // We enqueue the game results for both players at the same time and process them transactional
            var work = new GameCompletedWorkContract { Records = [records.Player1, records.Player2] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueMatchResultAsync(MatchRecordContracts records, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.MatchCompleted);
            // We enqueue the match results for both players at the same time and process them transactional
            var work = new MatchCompletedWorkContract { Records = [records.Player1, records.Player2] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueRatingProcessingAsync(MatchRecordContracts records, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.RatingUpdated);
            // We enqueue the rating updates for both players at the same time and process them transactional
            var work = new RatingUpdateWorkContract { Records = [records.Player1, records.Player2] };
            work.GetValidatedRecords();
            await workQueue.EnqueueAsync(work, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueStatProcessingAsync(MatchRecordContracts records, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.StatsUpdated);

            if (workQueue.IsFifo)
            {
                FifoWorkMessage<MatchRecordContract>[] messages =
               [
                    CreateStatsMessage(records.Player1),
                    CreateStatsMessage(records.Player2)
               ];
                await workQueue.EnqueueFifoBatchAsync(messages, cancellationToken);
            }
            else
            {
                await workQueue.EnqueueBatchAsync(
                    new[]
                    {
                        records.Player1,
                        records.Player2
                    },
                    cancellationToken);
            }
        }

        private static FifoWorkMessage<MatchRecordContract> CreateStatsMessage(MatchRecordContract record)
        {
            return new FifoWorkMessage<MatchRecordContract>(
                record,
                record.PlayerId.ToString("D"),
                $"{record.Id:D}:{record.PlayerId:D}");
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
