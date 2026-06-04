using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Evaluates a normalized board position and returns win probabilities in [0, 1].
    /// Abstracts the neural network so GammonX.Mars.Server has no TorchSharp dependency.
    /// </summary>
    public interface INeuralEvalService
    {
        /// <summary>
        /// Returns the predicted win probabilities for the active player given the board features.
        /// </summary>
        /// <remarks>
        /// We predict 5 output probabilities:
        /// [0] P(win)
        /// [1] P(gammon win)
        /// [2] P(backgammon win)
        /// [3] P(gammon loss)
        /// [4] P(backgammon loss)
        /// </remarks>
        /// <param name="model">Computed model values.</param>
        /// <param name="board">Target board state.</param>
        /// <param name="isWhite">Player indicator.</param>
        /// <returns>An array of values between 0 (lost) and 1 (won) for each output.</returns>
        float[] Predict(NormalizedEvalResultModel model, IBoardModel board, bool isWhite);
    }
}
