using GammonX.Models.Enums;

using TorchSharp.Modules;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Nets
{
    /// <summary>
    /// Encapsulates a neural network model that can be used for evaluation.
    /// </summary>
    public interface INetModel
    {
        Tensor Forward(Tensor x);

        void Load(string location);

        void Save(string location);

        void Eval();

        void Train();

        IEnumerable<Parameter> GetParameters();
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
        public static INetModel Create(GameModus modus)
        {
            INetModel netModel = modus switch
            {
                GameModus.Plakoto => new PlakotoNet(),
                GameModus.Fevga => new FevgaNet(),
                _ => throw new NotSupportedException($"Modus {modus} has no net model.")
            };
            return netModel;
        }
    }
}
