using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Server.NN
{
    // <inheritdoc />
    public sealed class PlakotoNet : Module<Tensor, Tensor>, INetModel
    {
        private readonly Linear _fc1;
        private readonly Linear _fc2;
        private readonly Linear _fc3;

        public PlakotoNet() : base(nameof(PlakotoNet))
        {
            _fc1 = Linear(21, 64);
            _fc2 = Linear(64, 32);
            _fc3 = Linear(32, 1);
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
            x = functional.relu(_fc1.forward(x));
            x = functional.relu(_fc2.forward(x));
            x = sigmoid(_fc3.forward(x));
            return x.squeeze(-1);
        }
    }
}