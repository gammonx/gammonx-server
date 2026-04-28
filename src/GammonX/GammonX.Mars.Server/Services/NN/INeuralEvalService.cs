using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server.Services.NN
{
    /// <summary>
    /// Evaluates a normalized board position and returns a win probability in [0, 1].
    /// Abstracts the neural network so GammonX.Mars.Server has no TorchSharp dependency.
    /// </summary>
    public interface INeuralEvalService
    {
        /// <summary>
        /// Returns the predicted win probability for the active player given the board features.
        /// </summary>
        float Predict(NormalizedEvalResultModel normalizedEvalResult);
    }
}
