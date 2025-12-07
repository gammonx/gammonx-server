using Newtonsoft.Json;

namespace GammonX.Server.Queue
{
    // <inheritdoc />
    public class LogWorkQueue : IWorkQueue
    {
        public LogWorkQueue()
        {
            // pass
        }

        // <inheritdoc />
        public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken)
        {
            var body = JsonConvert.SerializeObject(message);
            Serilog.Log.Debug("LogWorkQueue Enqueue: {Body}", body);
        }

        // <inheritdoc />
        public Task EnqueueBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                var body = JsonConvert.SerializeObject(message);
                Serilog.Log.Debug("LogWorkQueue EnqueueBatch: {Body}", body);
            }
            return Task.CompletedTask;
        }
    }
}
