using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using GammonX.Models.Enums;

using Microsoft.Extensions.Hosting;

using Serilog;

using System.Reflection;
using System.Threading.Channels;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Neural eval service that batches concurrent Predict calls into a single forward pass.
    /// Single predictions and bounded explicit groups share a FIFO <see cref="Channel{T}"/>.
    /// A single worker combines whole queue items up to the configured row limit.
    /// Implements <see cref="IHostedService"/> so ASP.NET Core manages the worker lifetime.
    /// </summary>
    public sealed class BatchedNeuralEvalService : INeuralEvalService, IHostedService, IDisposable
    {
        private readonly record struct InferenceRequest(
            float[][] Vectors,
            TaskCompletionSource<float[][]> Result,
            long Queued)
        {
            public int Count => Vectors.Length;

            public void Fail(Exception error)
            {
                Result.TrySetException(error);
            }

            public void Cancel(CancellationToken token)
            {
                Result.TrySetCanceled(token);
            }
        }

        private readonly INetModel _netModel;
        private readonly IFeatureVectorExtractor _extractor;
        private readonly Device _device;
        private readonly Channel<InferenceRequest> _channel;
        private Task? _workerTask;
        private CancellationTokenSource _cts = new();
        private int _state;

        internal BatchedNeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor, int maxBatchSize, Device device)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBatchSize);
            _netModel = netModel;
            _extractor = extractor;
            _device = device;
            MaxBatchSize = maxBatchSize;
            _channel = Channel.CreateBounded<InferenceRequest>(
                new BoundedChannelOptions(maxBatchSize * 4)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
        }

        /// <summary>
        /// Loads the model from the given path. Call <see cref="StartAsync"/> before submitting predictions.
        /// </summary>
        /// <param name="modus">The game modus.</param>
        /// <param name="modelPath">The path to the model file.</param>
        /// <param name="device">The device to run the model on.</param>
        /// <param name="maxBatchSize">The maximum batch size for inference.</param>
        /// <returns>The loaded <see cref="BatchedNeuralEvalService"/>.</returns>
        public static INeuralEvalService Load(GameModus modus, string modelPath, Device device, int maxBatchSize = 32)
        {
            // we only support CPU based models for now in production
            var net = NetModelFactory.CreateForModel(modus, modelPath, device);
            var extractor = FeatureVectorExtractorFactory.Create(modus);
            net.Eval();
            return new BatchedNeuralEvalService(net, extractor, maxBatchSize, device);
        }

        /// <summary>
        /// Loads the model from an embedded resource at
        /// <c>NeuralNets/{modus}/training_net.dat</c> in the calling assembly.
        /// Returns <c>null</c> if no embedded resource exists for the given modus.
        /// </summary>
        /// <param name="assembly">The assembly to load the embedded resource from.</param>
        /// <param name="modus">The game modus.</param>
        /// <param name="maxBatchSize">The maximum batch size for inference.</param>
        /// <returns>The loaded <see cref="BatchedNeuralEvalService"/> or <c>null</c> if no embedded resource exists for the given modus.</returns>
        public static INeuralEvalService? LoadEmbedded(Assembly assembly, GameModus modus, int maxBatchSize = 32)
        {
            var resourceName = $"GammonX.Mars.Server.NeuralNets.{modus}.training_net.dat";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                Log.Warning("Neural net model missing for {Modus}. Falling back to linear model.", modus);
                return null;
            }

            // we only support CPU based models for now in production
            var device = cuda.is_available() ? CUDA : CPU;
            var metadataResourceName = resourceName + NetModelMetadata.MetadataSuffix;
            using var metadataStream = assembly.GetManifestResourceStream(metadataResourceName);
            var metadata = NetModelMetadata.ReadOrLegacy(metadataStream, modus, metadataResourceName);
            var net = NetModelFactory.CreateNew(modus, device, metadata.OutputMode, metadata.Architecture);
            var extractor = FeatureVectorExtractorFactory.Create(modus);
            net.LoadFromStream(stream);
            net.Eval();
            return new BatchedNeuralEvalService(net, extractor, maxBatchSize, device);
        }

        #region INeuralEvalService Impl

        // <inheritdoc />
        public async Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            EnsureRunning();

            var vec = ExtractFeatures(model, board, isWhite);
            var results = await PredictBatchAsync([vec]).ConfigureAwait(false);
            return results[0];
        }

        #endregion INeuralEvalService Impl

        #region Batch Inference

        // <inheritdoc />
        public int MaxBatchSize { get; }

        // <inheritdoc />
        public async Task<float[][]> PredictBatchAsync(float[][] features)
        {
            ArgumentNullException.ThrowIfNull(features);
            if (features.Length == 0 || features.Length > MaxBatchSize)
            {
                throw new ArgumentException($"A batch must contain between 1 and {MaxBatchSize} rows.", nameof(features));
            }

            foreach (var row in features)
            {
                ArgumentNullException.ThrowIfNull(row);
                if (row.Length != _extractor.FeatureCount)
                {
                    throw new ArgumentException($"Each row must have {_extractor.FeatureCount} features.", nameof(features));
                }
            }

            EnsureRunning();
            var tcs = new TaskCompletionSource<float[][]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _channel.Writer.WriteAsync(
                new InferenceRequest(
                    features,
                    tcs,
                    EvaluationMetrics.GetQueueTimestamp()),
                _cts.Token)
                .ConfigureAwait(false);

            EvaluationMetrics.RecordInferenceRequest();

            return await tcs.Task.ConfigureAwait(false);
        }

        // <inheritdoc />
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            using (EvaluationMetrics.Measure(EvaluationMetrics.FeatureVector))
            {
                var features = _extractor.Extract(model, board, isWhite);
                if (features.Length != _extractor.FeatureCount)
                {
                    throw new InvalidDataException($"The feature extractor must return {_extractor.FeatureCount} values.");
                }
                return features;
            }
        }

        #endregion Batch Inference

        // <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            {
                throw new InvalidOperationException("The inference worker can only be started once.");
            }

            _cts.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Always enter the worker so its finally block drains requests on cancellation.
            _workerTask = Task.Run(() => RunWorkerAsync(_cts.Token));

            return Task.CompletedTask;
        }

        // <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _state, 2);
            _channel.Writer.TryComplete();
            try
            {
                await _cts.CancelAsync();
                if (_workerTask != null)
                {
                    await _workerTask.ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // pass
            }
            catch (ChannelClosedException)
            {
                // pass
            }
            catch (OperationCanceledException)
            {
                // cancellation is expected when the worker is stopped
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // cancellation is expected when the worker is stopped
            }
        }

        // <inheritdoc />
        public void Dispose() => _cts.Dispose();

        private async Task RunWorkerAsync(CancellationToken ct)
        {
            var batch = new List<InferenceRequest>(MaxBatchSize);
            try
            {
                while (await _channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    batch.Clear();
                    var rowCount = 0;
                    while (_channel.Reader.TryPeek(out var next) && rowCount + next.Count <= MaxBatchSize)
                    {
                        if (!_channel.Reader.TryRead(out var request))
                            break;
                        batch.Add(request);
                        rowCount += request.Count;
                    }

                    if (batch.Count == 0)
                        continue;

                    try
                    {
                        RunBatch(batch, rowCount);
                    }
                    catch (Exception ex)
                    {
                        foreach (var request in batch)
                            request.Fail(ex);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _state, 2);
                _channel.Writer.TryComplete();
                while (_channel.Reader.TryRead(out var request))
                    request.Cancel(ct);
            }
        }

        private void RunBatch(List<InferenceRequest> batch, int rowCount)
        {
            var batchStarted = EvaluationMetrics.GetQueueTimestamp();

            if (batchStarted != 0)
            {
                foreach (var request in batch)
                {
                    for (var row = 0; row < request.Count; row++)
                        EvaluationMetrics.RecordQueueWait(request.Queued, batchStarted);
                }
            }

            EvaluationMetrics.RecordBatchSize(rowCount);
            var preparation = EvaluationMetrics.Measure(EvaluationMetrics.BatchPreparation);

            var featureCount = _extractor.FeatureCount;
            var flat = new float[rowCount * featureCount];
            var offset = 0;

            foreach (var request in batch)
            {
                foreach (var row in request.Vectors)
                {
                    row.CopyTo(flat, offset);
                    offset += featureCount;
                }
            }

            // one forward pass for all positions in the batch: [N, featureCount] > [N, outputCount]
            using var input = tensor(flat, [rowCount, featureCount], device: _device);

            preparation.Dispose();
            var forward = EvaluationMetrics.Measure(EvaluationMetrics.Forward);

            using var _ = no_grad();
            using var output = _netModel.Forward(input);

            // we perform one GPU-to-CPU transfer and one synchronization for the whole batch.
            using var cpuOutput = output.to(CPU);
            var values = cpuOutput.data<float>().ToArray();

            var shape = output.shape;
            if (shape.Length != 1 && shape.Length != 2)
                throw new InvalidDataException("Inference output must have one or five values for every input row.");
            var outputWidth = shape.Length == 1 ? 1 : checked((int)shape[^1]);
            if (shape[0] != rowCount ||
                (outputWidth != 1 && outputWidth != 5) || values.Length != rowCount * outputWidth)
                throw new InvalidDataException("Inference output must have one or five values for every input row.");

            // Publish worker timings before completing requests so awaiting callers see all stage measurements.
            forward.Dispose();

            // we fan results back out, row i is independent of all other rows
            offset = 0;
            foreach (var request in batch)
            {
                var results = new float[request.Count][];
                for (var row = 0; row < results.Length; row++)
                    results[row] = ReadRow(offset++);
                request.Result.TrySetResult(results);
            }

            return;

            float[] ReadRow(int index)
            {
                if (outputWidth == 1)
                    return [values[index], 0f, 0f, 0f, 0f];
                var result = new float[outputWidth];
                Array.Copy(values, index * outputWidth, result, 0, outputWidth);
                return result;
            }
        }

        private void EnsureRunning()
        {
            if (Volatile.Read(ref _state) != 1)
            {
                throw new InvalidOperationException("The inference worker must be started and not stopped.");
            }
        }
    }
}
