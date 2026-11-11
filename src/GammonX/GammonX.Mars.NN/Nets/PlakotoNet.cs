using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Nets
{
    // <inheritdoc />
    public sealed class PlakotoNet : NetModel
    {
        private readonly Linear _fc1;
        private readonly Linear _fc2;
        private readonly Linear _fc3;
        private readonly Linear _fc4;
        private readonly Dropout _drop;

        public PlakotoNet(Device device)
            : this(device, new NetModelMetadata(NetModelMetadata.CurrentFormatVersion, GameModus.Plakoto, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A))
        {
            // pass
        }

        public PlakotoNet(Device device, NetModelMetadata metadata) : base(nameof(PlakotoNet), metadata)
        {
            metadata.Validate(GameModus.Plakoto, nameof(PlakotoNet));
            _fc1 = Linear(266, 256, true, device);
            _fc2 = Linear(256, 128, true, device);
            _fc3 = Linear(128, 64, true, device);
            _fc4 = Linear(64, 1, true, device);
            // increase p if model is over fitting
            _drop = Dropout(p: 0.0);
            RegisterComponents();
        }

        // <inheritdoc />
        public override Tensor forward(Tensor x)
        {
            using var h1 = _fc1.forward(x);
            using var r1 = functional.relu(h1);
            using var d1 = _drop.forward(r1);
            using var h2 = _fc2.forward(d1);
            using var r2 = functional.relu(h2);
            using var d2 = _drop.forward(r2);
            using var h3 = _fc3.forward(d2);
            using var r3 = functional.relu(h3);
            using var h4 = _fc4.forward(r3);
            using var s  = sigmoid(h4);
            return s.squeeze(-1);
        }
    }
}
