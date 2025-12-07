using GammonX.Models.Contracts;

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
            var gameRecords = new GameRecordContract[] { player1GameRecord, player2GameRecord };
            await workQueue.EnqueueBatchAsync(gameRecords, cancellationToken);
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
