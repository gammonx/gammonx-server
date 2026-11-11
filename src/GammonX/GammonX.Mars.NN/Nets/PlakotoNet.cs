using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Nets
{
    // <inheritdoc />
    public sealed class PlakotoNet : NetModel
    {
        private Linear _fc1 = null!;
        private Linear _fc2 = null!;
        private Linear _fc3 = null!;
        private Linear _fc4 = null!;
        private readonly Dropout _drop;

        public PlakotoNet(Device device)
            : this(device, new NetModelMetadata(NetModelMetadata.CurrentFormatVersion, GameModus.Plakoto, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A))
        {
            // pass
        }

        public PlakotoNet(Device device, NetModelMetadata metadata) : base(nameof(PlakotoNet), metadata)
        {
            metadata.Validate(GameModus.Plakoto, nameof(PlakotoNet));

            InitializeModelArchitecture(metadata.Architecture, device);

            // dropout temporarily switches off randomly selected hidden activations during training. 
            // With p = 0.1, each activation has a 10% chance of becoming zero on a training pass. 
            // This discourages the network from relying too heavily on particular neurons and can reduce over fitting
            // we currently disabled dropout because no over fitting is spotted
            _drop = Dropout(p: 0.0);
            
            RegisterComponents();
        }

        public GameOutcomeOutputMode OutputMode => Metadata.OutputMode;

        private void InitializeModelArchitecture(NetArchitecture architecture, Device device)
        {
            switch (architecture)
            {
                case NetArchitecture.A:
                    _fc1 = Linear(266, 256, true, device);
                    _fc2 = Linear(256, 128, true, device);
                    _fc3 = Linear(128, 64, true, device);
                    _fc4 = Linear(64, 1, true, device);
                    break;
                case NetArchitecture.B:
                    _fc1 = Linear(266, 440, true, device);
                    _fc2 = Linear(440, 220, true, device);
                    _fc3 = Linear(220, 110, true, device);
                    _fc4 = Linear(110, 3, true, device);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(architecture));
            }
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
            if (OutputMode == GameOutcomeOutputMode.LegacyIndependentSigmoid)
                return s.squeeze(-1);

            // Gammon wins and losses are subsets of their respective outcomes.
            using var pWin = s.select(1, 0);
            using var winGate = s.select(1, 1);
            using var lossGate = s.select(1, 2);
            using var pLoss = ones_like(pWin) - pWin;
            using var pGammonWin = pWin * winGate;
            using var pGammonLoss = pLoss * lossGate;
            return stack([pWin, pGammonWin, pGammonLoss], dim: 1);
        }
    }
}
