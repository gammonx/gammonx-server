using System.Reflection;

using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using GammonX.Models.Enums;

using Serilog;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public sealed class NeuralEvalService : INeuralEvalService
    {
        // TorchSharps internal symbolic shape system (SymNodeImpl) uses global C++ state
        // that is not thread-safe. All forward passes must be serialized across all instances.
        private static readonly SemaphoreSlim InferLock = new(1, 1);

        private readonly INetModel _netModel;
        private readonly IFeatureVectorExtractor _extractor;
        private readonly Device _device;

        // <inheritdoc />
        public int MaxBatchSize => 32;

        internal NeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor, Device device)
        {
            _netModel = netModel;
            _extractor = extractor;
            _device = device;
        }

        /// <summary>
        /// Loads the model from the given path for serialized inference.
        /// </summary>
        /// <param name="modus">The game modus.</param>
        /// <param name="modelPath">The path to the model file.</param>
        /// <param name="device">The device to run the model on.</param>
        /// <returns>The loaded <see cref="NeuralEvalService"/>.</returns>
        public static INeuralEvalService Load(GameModus modus, string modelPath, Device device)
        {
            InferLock.Wait();
            try
            {
                // we only support CPU based models for now in production
                var net = NetModelFactory.CreateForModel(modus, modelPath, device);
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.Eval();
                return new NeuralEvalService(net, extractor, device);
            }
            finally
            {
                InferLock.Release();
            }
        }

        /// <summary>
        /// Loads the model from an embedded resource at
        /// <c>NeuralNets/{modus}/training_net.dat</c> in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly to load the embedded resource from.</param>
        /// <param name="modus">The game modus.</param>
        /// <param name="device">The device to run the model on.</param>
        /// <returns><c>null</c> if no embedded resource exists for the given modus.</returns>
        public static INeuralEvalService LoadEmbedded(Assembly assembly, GameModus modus, Device device)
        {
            var resourceName = $"GammonX.Mars.Server.NeuralNets.{modus}.training_net.dat";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Warning("A neural net model is missing for {modus}. Falling back to linear model.", modus);
                return null!;
            }

            InferLock.Wait();
            try
            {
                // we only support CPU based models for now in production
                var metadataResourceName = resourceName + NetModelMetadata.MetadataSuffix;
                using var metadataStream = assembly.GetManifestResourceStream(metadataResourceName);
                var net = NetModelFactory.CreateForModel(modus, stream, metadataStream, device, "training_net.dat");
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.Eval();
                return new NeuralEvalService(net, extractor, device);
            }
            finally
            {
                InferLock.Release();
            }
        }

        // <inheritdoc />
        public async Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var predictions = await PredictBatchAsync([ExtractFeatures(model, board, isWhite)], cancellationToken).ConfigureAwait(false);
            return predictions[0];
        }

        // <inheritdoc />
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination)
        {
            if (destination == null)
            {
                // we fall back to the version without a pre-allocated destination array
                return ExtractFeatures(model, board, isWhite);
            }

            _extractor.Extract(model, board, isWhite, destination);
            return destination;
        }

        // <inheritdoc />
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var features = _extractor.Extract(model, board, isWhite);

            if (features.Length != _extractor.FeatureCount)
            {
                throw new InvalidDataException($"The feature extractor must return {_extractor.FeatureCount} values.");
            }

            return features;
        }

        // <inheritdoc />
        public async Task<float[][]> PredictBatchAsync(float[][] features, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ArgumentNullException.ThrowIfNull(features);
            if (features.Length == 0 || features.Length > MaxBatchSize)
                throw new ArgumentException($"A batch must contain between 1 and {MaxBatchSize} rows.", nameof(features));
            var flat = new float[features.Length * _extractor.FeatureCount];
            for (var index = 0; index < features.Length; index++)
            {
                var row = features[index];
                ArgumentNullException.ThrowIfNull(row);
                if (row.Length != _extractor.FeatureCount)
                    throw new ArgumentException($"Each row must have {_extractor.FeatureCount} features.", nameof(features));
                row.CopyTo(flat, index * _extractor.FeatureCount);
            }
            await InferLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var input = tensor(flat, [features.Length, _extractor.FeatureCount], device: _device);
                using var _ = no_grad();
                using var output = _netModel.Forward(input);
                using var cpuOutput = output.to(CPU);
                var values = cpuOutput.data<float>().ToArray();
                var shape = output.shape;
                if (shape.Length != 1 && shape.Length != 2)
                    throw new InvalidDataException("Inference output must have one or five values for every input row.");
                var width = shape.Length == 1 ? 1 : checked((int)shape[^1]);
                if (shape[0] != features.Length || (width != 1 && width != 5) || values.Length != features.Length * width)
                    throw new InvalidDataException("Inference output must have one or five values for every input row.");
                var results = new float[features.Length][];
                for (var index = 0; index < results.Length; index++)
                {
                    results[index] = new float[5];
                    Array.Copy(values, index * width, results[index], 0, width);
                }

                cancellationToken.ThrowIfCancellationRequested();

                return results;
            }
            finally
            {
                InferLock.Release();
            }
        }
    }
}
