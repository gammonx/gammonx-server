using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
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
        /// <param name="model">Computed model values.</param>
        /// <param name="board">Target board state.</param>
        /// <param name="isWhite">Player indicator.</param>
        /// <returns>A value between 0 (lost) and 1 (won).</returns>
        float Predict(NormalizedEvalResultModel model, IBoardModel board, bool isWhite);
    }
}
