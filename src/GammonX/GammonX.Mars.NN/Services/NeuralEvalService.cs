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

        private NeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor)
        {
            _netModel = netModel;
            _extractor = extractor;
        }

        public static INeuralEvalService Load(GameModus modus, string modelPath)
        {
            lock (InferLock)
            {
                var net = NetModelFactory.Create(modus);
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.Load(modelPath);
                net.Eval();
                return new NeuralEvalService(net, extractor);
            }
        }

        /// <summary>
        /// Loads the model from an embedded resource at
        /// <c>NeuralNets/{modus}/training_net.dat</c> in the calling assembly.
        /// Returns <c>null</c> if no embedded resource exists for the given modus.
        /// </summary>
        public static INeuralEvalService LoadEmbedded(GameModus modus)
        {
            var assembly = typeof(NeuralEvalService).Assembly;
            var resourceName = $"GammonX.Mars.Server.NeuralNets.{modus}.training_net.dat";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Warning("A neural net model is missing for {modus}. Falling back to linear model.", modus);
                return null!;
            }

            lock (InferLock)
            {
                var net = NetModelFactory.Create(modus);
                var extractor = FeatureVectorExtractorFactory.Create(modus);
                net.LoadFromStream(stream);
                net.Eval();
                return new NeuralEvalService(net, extractor);
            }
        }

        // <inheritdoc />
        public float Predict(NormalizedEvalResultModel features)
        {
            var vec = _extractor.Extract(features);
            lock (InferLock)
            {
                using var raw   = tensor(vec);
                using var input = raw.unsqueeze(0);
                using var _     = no_grad();
                using var output = _netModel.Forward(input);
                return output.item<float>();
            }
        }
    }
}
