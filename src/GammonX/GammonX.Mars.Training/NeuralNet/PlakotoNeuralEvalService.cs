using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;
using GammonX.Mars.Server.Services.NN;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.NeuralNet;

public sealed class PlakotoNeuralEvalService : INeuralEvalService
{
    private readonly PlakotoNet _net;
    private readonly PlakotoFeatureVectorExtractor _extractor = new();

    private PlakotoNeuralEvalService(PlakotoNet net)
    {
        _net = net;
    }

    public static PlakotoNeuralEvalService Load(string modelPath)
    {
        var net = new PlakotoNet();
        net.load(modelPath);
        net.eval();
        return new PlakotoNeuralEvalService(net);
    }

    public float Predict(NormalizedEvalResultModel features)
    {
        var vec = _extractor.Extract(features);
        using var input = tensor(vec).unsqueeze(0); // [1, 21] > TODO plakoto specific
        using var _ = no_grad();
        using var output = _net.forward(input);
        return output.item<float>();
    }
}