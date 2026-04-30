using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;
using GammonX.Mars.Server.Services.NN;

using GammonX.Models.Enums;

using Serilog;

using static TorchSharp.torch;

namespace GammonX.Mars.Server.NN
{
    // <inheritdoc />
    public sealed class NeuralEvalService : INeuralEvalService
    {
        private readonly INetModel _netModel;
        private readonly IFeatureVectorExtractor _extractor;

        private NeuralEvalService(INetModel netModel, IFeatureVectorExtractor extractor)
        {
            _netModel = netModel;
            _extractor = extractor;
        }

        public static INeuralEvalService Load(GameModus modus, string modelPath)
        {
            var net = NetModelFactory.Create(modus);
            var extractor = FeatureVectorExtractorFactory.Create(modus);
            net.Load(modelPath);
            net.Eval();
            return new NeuralEvalService(net, extractor);
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
                return null;
            }

            var net = NetModelFactory.Create(modus);
            var extractor = FeatureVectorExtractorFactory.Create(modus);
            net.LoadFromStream(stream);
            net.Eval();
            return new NeuralEvalService(net, extractor);
        }

        // <inheritdoc />
        public float Predict(NormalizedEvalResultModel features)
        {
            // extract game modus dependent features and predict
            var vec = _extractor.Extract(features);
            using var input = tensor(vec).unsqueeze(0);
            using var _ = no_grad();
            using var output = _netModel.Forward(input);
            return output.item<float>();
        }
    }
}
