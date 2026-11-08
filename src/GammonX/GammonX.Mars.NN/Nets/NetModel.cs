using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.NN.Nets
{
    /// <summary>
    /// Keeps network configuration and persistence together with its parameters.
    /// </summary>
    public abstract class NetModel : Module<Tensor, Tensor>, INetModel
    {
        public NetModelMetadata Metadata { get; private set; }

        protected NetModel(string name, NetModelMetadata metadata) : base(name)
        {
            metadata.Validate(metadata.Modus, name);
            Metadata = metadata;
        }

        // <inheritdoc />
        public void ConfigureTraining(NetTrainingMetadata training)
        {
            training.Validate();
            Metadata = Metadata with { Name = Path.GetFileName(training.OutputModelPath), Training = training };
        }

        // <inheritdoc />
        public Tensor Forward(Tensor x)
        {
            return forward(x);
        }

        // <inheritdoc />
        public void Load(string location)
        {
            var metadata = NetModelMetadata.ReadOrLegacy(location, Metadata.Modus);
            if (metadata.OutputMode != Metadata.OutputMode || metadata.Architecture != Metadata.Architecture)
            {
                throw new InvalidDataException($"Model '{location}' is incompatible with this network's architecture or output mode.");
            }

            load(location);
            Metadata = metadata;
        }

        // <inheritdoc />
        public void LoadFromStream(Stream stream)
        {
            var tmp = Path.GetTempFileName();
            try
            {
                using (var fs = File.OpenWrite(tmp))
                    stream.CopyTo(fs);
                load(tmp);
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        // <inheritdoc />
        public void Save(string location)
        {
            var metadata = Metadata with
            {
                FormatVersion = NetModelMetadata.CurrentFormatVersion,
                Name = Path.GetFileName(location)
            };

            var tmp = location + $".{Guid.NewGuid():N}.tmp";

            try
            {
                save(tmp);
                metadata.Write(tmp);
                File.Move(tmp, location, overwrite: true);
                File.Move(NetModelMetadata.GetPath(tmp), NetModelMetadata.GetPath(location), overwrite: true);
                Metadata = metadata;
            }
            finally
            {
                File.Delete(tmp);
                File.Delete(NetModelMetadata.GetPath(tmp));
            }
        }

        // <inheritdoc />
        public void Eval()
        {
            eval();
        }

        // <inheritdoc />
        public void Train()
        {
            train();
        }

        // <inheritdoc />
        public IEnumerable<Parameter> GetParameters()
        {
            return parameters();
        }

        // <inheritdoc />
        public void MoveTo(Device device)
        {
            foreach (var (_, param) in named_parameters())
            {
                param.to(device);
            }
            foreach (var (_, buf) in named_buffers())
            {
                buf.to(device);
            }
        }
    }
}
