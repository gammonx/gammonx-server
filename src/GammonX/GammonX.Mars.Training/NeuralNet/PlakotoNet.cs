using TorchSharp;
using TorchSharp.Modules;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training.NeuralNet;

public sealed class PlakotoNet : Module<Tensor, Tensor>
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

    public override Tensor forward(Tensor x)
    {
        x = functional.relu(_fc1.forward(x));
        x = functional.relu(_fc2.forward(x));
        x = torch.sigmoid(_fc3.forward(x));
        return x.squeeze(-1);
    }
}