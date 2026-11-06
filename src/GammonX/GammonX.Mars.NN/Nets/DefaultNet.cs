using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Nets
{
    /// <summary>
    /// Encapsulates the neural network architecture for <see cref="GameModus.Backgammon"/>, <see cref="GameModus.Tavla"/> and <see cref="GameModus.Portes"/>.
    /// </summary>
    /// <remarks>
    /// The five output columns are ordered as P(win), P(gammon win), P(backgammon win),
    /// P(gammon loss), and P(backgammon loss). In <see cref="GameOutcomeOutputMode.MonotonicCumulative"/>
    /// mode, the latter four values are cumulative probabilities and therefore obey the outcome hierarchy.
    /// In legacy mode, all five values are returned as independent sigmoid outputs.
    /// </remarks>
    /// <seealso cref="INetModel"/>
    public sealed class DefaultNet : Module<Tensor, Tensor>, INetModel
    {
        private Linear _fc1 = null!;
        private Linear _fc2 = null!;
        private Linear _fc3 = null!;
        private Linear _fc4 = null!;

        private readonly Dropout _drop;

        /// <summary>
        /// Gets the output transformation used to convert logits into outcome probabilities.
        /// </summary>
        public GameOutcomeOutputMode OutputMode { get; }

        public DefaultNet(Device device, GameOutcomeOutputMode outputMode, NetArchitecture architecture) : base(nameof(DefaultNet))
        {
            if (!Enum.IsDefined(outputMode))
            {
                throw new ArgumentOutOfRangeException(nameof(outputMode), outputMode, "Unknown output mode.");
            }

            InitializeModelArchitecture(architecture, device);
            OutputMode = outputMode;
            // we increase p if model is over fitting
            _drop = Dropout(p: 0.0);
            RegisterComponents();
        }

        private void InitializeModelArchitecture(NetArchitecture architecture, Device device)
        {
            if (architecture == NetArchitecture.A)
            {
                _fc1 = Linear(216, 256, true, device);
                _fc2 = Linear(256, 128, true, device);
                _fc3 = Linear(128, 64, true, device);
                _fc4 = Linear(64, 5, true, device);
            }
            else if (architecture == NetArchitecture.B)
            {
                _fc1 = Linear(216, 384, true, device);
                _fc2 = Linear(384, 192, true, device);
                _fc3 = Linear(192, 96, true, device);
                _fc4 = Linear(96, 5, true, device);
            }
        }

        // <inheritdoc />
        public Tensor Forward(Tensor x)
        {
            return forward(x);
        }

        // <inheritdoc />
        public void Load(string location)
        {
            load(location);
        }

        // <inheritdoc />
        public void Save(string location)
        {
            save(location);
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
            using var logits = _fc4.forward(r3);
            using var gates = sigmoid(logits);

            if (OutputMode == GameOutcomeOutputMode.LegacyIndependentSigmoid)
                return gates.squeeze(-1);

            // Treat the sigmoid values as conditional probabilities along each outcome hierarchy.
            using var pWin = gates.select(1, 0);
            using var pGammonWinGate = gates.select(1, 1);
            using var pBackgammonWinGate = gates.select(1, 2);
            using var pGammonLossGate = gates.select(1, 3);
            using var pBackgammonLossGate = gates.select(1, 4);
            // A gammon win is a subset of wins, a backgammon win is a subset of gammons.
            using var pGammonWin = pWin * pGammonWinGate;
            using var pBackgammonWin = pGammonWin * pBackgammonWinGate;
            using var pLoss = ones_like(pWin) - pWin;
            // Loss outcomes use the complementary loss probability as their hierarchy root.
            using var pGammonLoss = pLoss * pGammonLossGate;
            using var pBackgammonLoss = pGammonLoss * pBackgammonLossGate;

            // Return cumulative heads in the same order expected by the outcome validators and consumers.
            return stack([pWin, pGammonWin, pBackgammonWin, pGammonLoss, pBackgammonLoss], dim: 1);
        }
    }
}
