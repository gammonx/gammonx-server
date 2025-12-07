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

        /// <summary>
        /// Enqueues the given <paramref name="match"/> result.
        /// </summary>
        /// <param name="match">Match to process..</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueMatchResultAsync(IMatchSessionModel match, CancellationToken cancellationToken);
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

        // <inheritdoc />
        public async Task EnqueueMatchResultAsync(IMatchSessionModel match, CancellationToken cancellationToken)
        {
            var workQueue = GetWorkQueue(WorkQueueType.MatchCompleted);
            var player1MatchRecord = match.ToRecord(match.Player1.Id);
            var player2MatchRecord = match.ToRecord(match.Player2.Id);
            var matchRecords = new MatchRecordContract[] { player1MatchRecord, player2MatchRecord };
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
