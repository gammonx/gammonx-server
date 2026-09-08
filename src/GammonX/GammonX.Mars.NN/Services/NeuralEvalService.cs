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
        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var vec = _extractor.Extract(model, board, isWhite);
            lock (InferLock)
            {
                using var raw   = tensor(vec, device: _device);
                using var input = raw.unsqueeze(0);
                using var _ = no_grad();
                using var output = _netModel.Forward(input);
                var result = output.data<float>().ToArray();
                // we normalize single-output nets
                if (result.Length == 1)
                {
                    // TODO: enable full GAME equity predictions for plakoto/fevga
                    return Task.FromResult(new float[] { result[0], 0f, 0f, 0f, 0f });
                }
                return Task.FromResult(result);
            }
        }
    }
}
