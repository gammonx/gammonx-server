using GammonX.Models.Enums;

using TorchSharp.Modules;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Nets
{
    /// <summary>
    /// Provides different neural network architecture sizes for different network models.
    /// </summary>
    /// <remarks>
    /// Each model implementation interprets the architecture sizes differently, so the sizes are not necessarily comparable across models.
    /// </remarks>
    public enum NetArchitecture
    {
        A = 0,
        B = 1
    }

    /// <summary>
    /// Encapsulates a neural network model that can be used for evaluation.
    /// </summary>
    /// <remarks>
    /// Implementations accept and return TorchSharp tensors. For the five-head outcome models,
    /// the output columns are ordered as win, win gammon, win backgammon, lose gammon, and lose backgammon.
    /// </remarks>
    public interface INetModel : IDisposable
    {
        /// <summary>
        /// Gets the model configuration and any recorded training settings.
        /// </summary>
        NetModelMetadata Metadata { get; }

        /// <summary>
        /// Configures a new training run without changing the network architecture or output mode.
        /// </summary>
        void ConfigureTraining(NetTrainingMetadata training);

        /// <summary>
        /// Evaluates a batch of input feature rows.
        /// </summary>
        /// <param name="x">The input tensor, typically shaped as <c>[batch, featureCount]</c>.</param>
        /// <returns>The model predictions for each input row.</returns>
        Tensor Forward(Tensor x);

        /// <summary>
        /// Loads model parameters and compatible metadata from the specified file location.
        /// </summary>
        /// <param name="location">The model file path.</param>
        void Load(string location);

        /// <summary>
        /// Loads model weights from a stream, preserving the metadata supplied at construction.
        /// </summary>
        void LoadFromStream(Stream stream);

        /// <summary>
        /// Saves model parameters and metadata to the specified file location and its sidecar.
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

    public static class NetModelFactory
    {
        public static INetModel CreateNew(GameModus modus, Device device, GameOutcomeOutputMode outputMode, NetArchitecture architecture)
        {
            return CreateNew(new NetModelMetadata(NetModelMetadata.CurrentFormatVersion, modus, outputMode, architecture), device);
        }

        public static INetModel CreateNew(NetModelMetadata metadata, Device device)
        {
            return metadata.Modus switch
            {
                GameModus.Plakoto => new PlakotoNet(device, metadata),
                GameModus.Fevga => new FevgaNet(device, metadata),
                GameModus.Backgammon => new DefaultNet(device, metadata),
                GameModus.Tavla => new DefaultNet(device, metadata),
                GameModus.Portes => new DefaultNet(device, metadata),
                _ => throw new NotSupportedException($"Modus {metadata.Modus} has no net model.")
            };
        }

        /// <summary>
        /// Create a neural net model for the specified <paramref name="modelPath"/>.
        /// </summary>
        /// <remarks>
        /// Reads the metadata sidecar file to determine the model type, architecture, and output mode.
        /// </remarks>
        /// <param name="modus">The game mode.</param>
        /// <param name="modelPath">The path to the model file.</param>
        /// <param name="device">The target device for the model.</param>
        /// <returns>The created neural net model.</returns>
        public static INetModel CreateForModel(GameModus modus, string modelPath, Device device)
        {
            var metadata = NetModelMetadata.ReadOrLegacy(modelPath, modus);
            var netModel = CreateNew(metadata, device);
            try
            {
                netModel.Load(modelPath);
                return netModel;
            }
            catch
            {
                netModel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates a model from weight and metadata streams, including embedded resources.
        /// </summary>
        public static INetModel CreateForModel(GameModus modus, Stream modelStream, Stream? metadataStream, Device device, string modelName)
        {
            var metadata = NetModelMetadata.ReadOrLegacy(metadataStream, modus, modelName + NetModelMetadata.MetadataSuffix)
                with { Name = Path.GetFileName(modelName) };
            var netModel = CreateNew(metadata, device);
            try
            {
                netModel.LoadFromStream(modelStream);
                return netModel;
            }
            catch
            {
                netModel.Dispose();
                throw;
            }
        }
    }
}
