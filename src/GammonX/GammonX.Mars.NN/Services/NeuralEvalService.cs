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
        private static readonly Lock InferLock = new();

        private readonly INetModel _netModel;
        private readonly IFeatureVectorExtractor _extractor;
        private readonly Device _device;

        private NeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor, Device device)
        {
            _netModel = netModel;
            _extractor = extractor;
            _device = device;
        }

        /// <summary>
        /// Loads the model from the given path and starts the background worker.
        /// </summary>
        /// <param name="modus">The game modus.</param>
        /// <param name="modelPath">The path to the model file.</param>
        /// <param name="device">The device to run the model on.</param>
        /// <returns>The loaded <see cref="NeuralEvalService"/>.</returns>
        public static INeuralEvalService Load(GameModus modus, string modelPath, Device device)
        {
            lock (InferLock)
            {
                // we only support CPU based models for now in production
                var net = NetModelFactory.CreateForModel(modus, modelPath, device);
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.Eval();
                return new NeuralEvalService(net, extractor, device);
            }
        }

        /// <summary>
        /// Loads the model from an embedded resource at
        /// <c>NeuralNets/{modus}/training_net.dat</c> in the given assembly.
        /// <param name="assembly">The assembly to load the embedded resource from.</param>
        /// <param name="modus">The game modus.</param>
        /// <param name="device">The device to run the model on.</param>
        /// <returns><c>null</c> if no embedded resource exists for the given modus.</returns>
        /// </summary>
        public static INeuralEvalService LoadEmbedded(Assembly assembly, GameModus modus, Device device)
        {
            var resourceName = $"GammonX.Mars.Server.NeuralNets.{modus}.training_net.dat";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Warning("A neural net model is missing for {modus}. Falling back to linear model.", modus);
                return null!;
            }

            lock (InferLock)
            {
                // we only support CPU based models for now in production
                var metadataResourceName = resourceName + NetModelMetadata.MetadataSuffix;
                using var metadataStream = assembly.GetManifestResourceStream(metadataResourceName);
                var metadata = NetModelMetadata.ReadOrLegacy(metadataStream, modus, metadataResourceName);
                var net = NetModelFactory.CreateNew(modus, device, metadata.OutputMode, metadata.Architecture);
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.LoadFromStream(stream);
                net.Eval();
                return new NeuralEvalService(net, extractor, device);
            }
        }

        // <inheritdoc />
        public async Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var predictions = await PredictBatchAsync([ExtractFeatures(model, board, isWhite)]).ConfigureAwait(false);
            return predictions[0];
        }

        // <inheritdoc />
        public int MaxBatchSize => 32;

        // <inheritdoc />
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var features = _extractor.Extract(model, board, isWhite);
            if (features.Length != _extractor.FeatureCount)
                throw new InvalidDataException($"The feature extractor must return {_extractor.FeatureCount} values.");
            return features;
        }

        // <inheritdoc />
        public Task<float[][]> PredictBatchAsync(float[][] features)
        {
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
            lock (InferLock)
            {
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
                return Task.FromResult(results);
            }
        }
    }
}
