using GammonX.Models.Enums;

using TorchSharp.Modules;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Nets
{
    /// <summary>
    /// Encapsulates a neural network model that can be used for evaluation.
    /// </summary>
    /// <remarks>
    /// Implementations accept and return TorchSharp tensors. For the five-head outcome models,
    /// the output columns are ordered as win, win gammon, win backgammon, lose gammon, and lose backgammon.
    /// </remarks>
    public interface INetModel
    {
        /// <summary>
        /// Evaluates a batch of input feature rows.
        /// </summary>
        /// <param name="x">The input tensor, typically shaped as <c>[batch, featureCount]</c>.</param>
        /// <returns>The model predictions for each input row.</returns>
        Tensor Forward(Tensor x);

        /// <summary>
        /// Loads model parameters from the specified file location.
        /// </summary>
        /// <param name="location">The model file path.</param>
        void Load(string location);

        /// <summary>
        /// Saves model parameters to the specified file location.
        /// </summary>
        /// <param name="location">The destination model file path.</param>
        void Save(string location);

        /// <summary>
        /// Switches the model to evaluation mode.
        /// </summary>
        void Eval();

        /// <summary>
        /// Switches the model to training mode.
        /// </summary>
        void Train();

        /// <summary>
        /// Gets the trainable parameters used by an optimizer.
        /// </summary>
        /// <returns>An enumerable sequence of model parameters.</returns>
        IEnumerable<Parameter> GetParameters();

        /// <summary>
        /// Moves model parameters and buffers to the specified TorchSharp device.
        /// </summary>
        /// <param name="device">The target CPU or accelerator device.</param>
        void MoveTo(Device device);
    }

    public static class NetModelExtensions
    {
        /// <summary>
        /// Loads model weights from a stream by writing to a temp file, since TorchSharp
        /// only supports loading from a file path.
        /// </summary>
        public static void LoadFromStream(this INetModel model, Stream stream)
        {
            var tmp = Path.GetTempFileName();
            try
            {
                using (var fs = File.OpenWrite(tmp))
                    stream.CopyTo(fs);
                model.Load(tmp);
            }
            finally
            {
                File.Delete(tmp);
            }
        }
    }

    public static class NetModelFactory
    {
        public static INetModel Create(GameModus modus, Device device, GameOutcomeOutputMode outputMode)
        {
            INetModel netModel = modus switch
            {
                GameModus.Plakoto => new PlakotoNet(device),
                GameModus.Fevga => new FevgaNet(device),
                GameModus.Backgammon => new DefaultNet(device, outputMode),
                GameModus.Tavla => new DefaultNet(device, outputMode),
                GameModus.Portes => new DefaultNet(device, outputMode),
                _ => throw new NotSupportedException($"Modus {modus} has no net model.")
            };

            if (modus is GameModus.Plakoto or GameModus.Fevga
                && outputMode != GameOutcomeOutputMode.LegacyIndependentSigmoid)
            {
                throw new ArgumentException(
                    $"Output mode {outputMode} is only supported by five-head game modes.",
                    nameof(outputMode));
            }

            return netModel;
        }

        public static INetModel CreateForModel(GameModus modus, string modelPath, Device device)
        {
            var metadata = NetModelMetadata.ReadOrLegacy(modelPath, modus);
            return Create(modus, device, metadata.OutputMode);
        }
    }
}
