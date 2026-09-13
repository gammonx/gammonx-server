using Newtonsoft.Json;

namespace GammonX.Server.Queue
{
    // <inheritdoc />
    public class LogWorkQueue : IWorkQueue
    {
        // <inheritdoc />
        public Task EnqueueAsync<T>(T message, CancellationToken cancellationToken)
        {
            var body = JsonConvert.SerializeObject(message);
            Serilog.Log.Debug("LogWorkQueue Enqueue: {Body}", body);
            return Task.CompletedTask;
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

        // <inheritdoc />
        public Task EnqueueFifoBatchAsync<T>(IEnumerable<FifoWorkMessage<T>> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                var body = JsonConvert.SerializeObject(message.Message);
                Serilog.Log.Debug(
                    "LogWorkQueue EnqueueFifoBatch: {Body}, GroupId: {GroupId}, DeduplicationId: {DeduplicationId}",
                    body,
                    message.GroupId,
                    message.DeduplicationId);
            }
            return Task.CompletedTask;
        }
    }
}
