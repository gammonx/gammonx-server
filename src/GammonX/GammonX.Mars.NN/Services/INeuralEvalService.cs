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
        /// Gets the positive maximum number of rows that can be submitted as one batch.
        /// </summary>
        int MaxBatchSize { get; }

        /// <summary>
        /// Extracts the feature vector for the given model and board state.
        /// </summary>
        /// <param name="model">The normalized evaluation result model to extract the features from.</param>
        /// <param name="board">The board state to extract the features from.</param>
        /// <param name="isWhite">Indicates whether the player is white.</param>
        /// <returns>An independent feature array, including history-derived inputs.</returns>
        float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite);

        /// <summary>
        /// Predicts the output for a batch of feature vectors.
        /// </summary>
        /// <remarks>
        /// A batch must contain between one and <see cref="MaxBatchSize"/> rows of
        /// the extractor's feature width. Inputs must not be mutated until completion.
        /// Each result uses the same five-value format as <see cref="PredictAsync"/>.
        /// Scalar models are normalized to [prediction, 0, 0, 0, 0].
        /// </remarks>
        /// <param name="features">The batch of feature vectors to predict.</param>
        /// <returns>The predicted outputs for the batch, in the same order as the input rows.</returns>
        Task<float[][]> PredictBatchAsync(float[][] features);

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
        Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite);
    }
}
