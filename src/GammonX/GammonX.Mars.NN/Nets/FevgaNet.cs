using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.NN.Nets
{
    // <inheritdoc />
    public sealed class FevgaNet : Module<Tensor, Tensor>, INetModel
    {
        private readonly Linear _fc1;
        private readonly Linear _fc2;
        private readonly Linear _fc3;
        private readonly Linear _fc4;
        private readonly Dropout _drop;

        public FevgaNet() : base(nameof(FevgaNet))
        {
            _fc1 = Linear(216, 256);
            _fc2 = Linear(256, 128);
            _fc3 = Linear(128, 64);
            _fc4 = Linear(64, 1);
            // we randomly zero 10% of the neurons during training process to prevent overfitting
            // we do not apply it in eval process
            _drop = Dropout(p: 0.1);
            RegisterComponents();
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