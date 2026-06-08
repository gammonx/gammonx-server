using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Services;

using GammonX.Models.Enums;

using Serilog;

using System.Threading.Channels;

using static TorchSharp.torch;

namespace GammonX.Mars.Server.Services
{
    /// <summary>
    /// Neural eval service that batches concurrent Predict calls into a single forward pass.
    /// All pending predictions are collected in a <see cref="Channel{T}"/>, flushed by a
    /// single background worker, and results are fanned back out to each caller individually.
    /// Implements <see cref="IHostedService"/> so ASP.NET Core manages the worker lifetime.
    /// </summary>
    public sealed class BatchedNeuralEvalService : INeuralEvalService, IHostedService, IDisposable
    {
        private readonly record struct InferenceRequest(float[] Vec, TaskCompletionSource<float[]> Result);

        private readonly INetModel _netModel;
        private readonly IFeatureVectorExtractor _extractor;
        private readonly Channel<InferenceRequest> _channel;
        private readonly int _maxBatchSize;
        private Task? _workerTask;
        private CancellationTokenSource _cts = new();

        private BatchedNeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor, int maxBatchSize)
        {
            _netModel = netModel;
            _extractor = extractor;
            _maxBatchSize = maxBatchSize;
            _channel = Channel.CreateBounded<InferenceRequest>(
                new BoundedChannelOptions(maxBatchSize * 4)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true
                });
        }

        /// <summary>
        /// Loads the model from an embedded resource at
        /// <c>NeuralNets/{modus}/training_net.dat</c> in the calling assembly.
        /// Returns <c>null</c> if no embedded resource exists for the given modus.
        /// </summary>
        public static BatchedNeuralEvalService? LoadEmbedded(GameModus modus, int maxBatchSize = 32)
        {
            var assembly = typeof(BatchedNeuralEvalService).Assembly;
            var resourceName = $"GammonX.Mars.Server.NeuralNets.{modus}.training_net.dat";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                Log.Warning("Neural net model missing for {Modus}. Falling back to linear model.", modus);
                return null;
            }

            var net = NetModelFactory.Create(modus);
            var extractor = FeatureVectorExtractorFactory.Create(modus);
            net.LoadFromStream(stream);
            net.Eval();
            return new BatchedNeuralEvalService(net, extractor, maxBatchSize);
        }

        // <inheritdoc />
        public float[] Predict(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var vec = _extractor.Extract(model, board, isWhite);
            var tcs = new TaskCompletionSource<float[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            // TryWrite will not block here: the channel is bounded but large relative to batch size;
            // under sustained overload the channel's Wait mode applies back-pressure.
            _channel.Writer.TryWrite(new InferenceRequest(vec, tcs));
            return tcs.Task.GetAwaiter().GetResult();
        }

        // <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _workerTask = Task.Run(() => RunWorkerAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        // <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _cts.CancelAsync();
                _channel.Writer.Complete();
                if (_workerTask != null)
                {
                    await _workerTask.ConfigureAwait(false);
                }
            }
            catch (ChannelClosedException)
            {
                // pass
            }
        }

        private async Task RunWorkerAsync(CancellationToken ct)
        {
            var batch = new List<InferenceRequest>(_maxBatchSize);

            while (!ct.IsCancellationRequested)
            {
                batch.Clear();

                // we block until at least one request arrives
                if (!await _channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
                    break;

                // we drain all currently queued requests up to maxBatchSize
                while (batch.Count < _maxBatchSize && _channel.Reader.TryRead(out var req))
                {
                    batch.Add(req);
                }

                if (batch.Count == 0)
                    continue;

                try
                {
                    RunBatch(batch);
                }
                catch (Exception ex)
                {
                    foreach (var r in batch)
                        r.Result.TrySetException(ex);
                }
            }
        }

        private void RunBatch(List<InferenceRequest> batch)
        {
            var featureCount = batch[0].Vec.Length;
            var flat = new float[batch.Count * featureCount];
            for (var i = 0; i < batch.Count; i++)
            {
                batch[i].Vec.CopyTo(flat, i * featureCount);
            }

            // one forward pass for all positions in the batch: [N, featureCount] > [N, outputCount]
            using var input = tensor(flat, [batch.Count, featureCount]);
            using var _ = no_grad();
            using var output = _netModel.Forward(input);

            // we fan results back out, row i is independent of all other rows
            for (var i = 0; i < batch.Count; i++)
            {
                using var row = output[i];
                var result = row.data<float>().ToArray();
                // we normalize single-output nets
                if (result.Length == 1)
                {
                    // TODO: can be removed once plakoto/fevga supports 5 outputs
                    result = [result[0], 0f, 0f, 0f, 0f];
                }
                batch[i].Result.TrySetResult(result);
            }
        }

        public void Dispose() => _cts.Dispose();
    }
}
