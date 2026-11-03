using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Extracts board features and evaluates neural models without exposing tensor operations to callers.
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
        /// Extracts features into <paramref name="destination"/>, allocating a new buffer when it is null.
        /// </summary>
        /// <param name="model">The normalized evaluation result model to extract the features from.</param>
        /// <param name="board">The board state to extract the features from.</param>
        /// <param name="isWhite">Indicates whether the player is white.</param>
        /// <param name="destination">The buffer to fill. If null, a new array is allocated.</param>
        /// <returns>The supplied or newly allocated feature buffer, including history-derived inputs.</returns>
        float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination);

        /// <summary>
        /// Predicts the output for a batch of feature vectors.
        /// </summary>
        /// <remarks>
        /// A batch must contain between one and <see cref="MaxBatchSize"/> rows of
        /// the extractor's feature width. Inputs must not be mutated until completion,
        /// including cancellation completion. Queued work is cancelled by the worker;
        /// an executing native forward pass finishes before cancellation is acknowledged.
        /// Each result uses the same five-value format as <see cref="PredictAsync"/>.
        /// Scalar models are normalized to [prediction, 0, 0, 0, 0].
        /// </remarks>
        /// <param name="features">The batch of feature vectors to predict.</param>
        /// <param name="cancellationToken">Cancels evaluation without returning a partial result.</param>
        /// <returns>The predicted outputs for the batch, in the same order as the input rows.</returns>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
        Task<float[][]> PredictBatchAsync(float[][] features, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the neural prediction for the active player given the board features.
        /// </summary>
        /// <remarks>
        /// Outcome models predict five probabilities:
        /// [0] P(win)
        /// [1] P(gammon win)
        /// [2] P(backgammon win)
        /// [3] P(gammon loss)
        /// [4] P(backgammon loss)
        /// Scalar models return their evaluation in the first slot and zero in the other four slots.
        /// </remarks>
        /// <param name="model">Computed model values.</param>
        /// <param name="board">Target board state.</param>
        /// <param name="isWhite">Player indicator.</param>
        /// <param name="cancellationToken">Cancels evaluation without returning a partial result.</param>
        /// <returns>Five prediction values in the documented order.</returns>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
        Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, CancellationToken cancellationToken);
    }
}
