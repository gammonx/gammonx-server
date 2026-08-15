using GammonX.Models.Enums;

namespace GammonX.Mars.Training;

/// <summary>
/// Calculates forward-view TD(lambda) targets for positions in a game trajectory.
///
/// Lambda controls how far the target looks ahead. A value of 0 uses the next
/// prediction only. A value of 1 uses the final game result, also known as the
/// Monte Carlo target. Values between 0 and 1 blend short and long returns.
/// For example, lambda=0.7 gives weights of 0.3, 0.21, 0.147, and so on to
/// successive returns, with the remaining weight on the final return.
///
/// Gamma controls how much future values are discounted. Gamma=1 keeps the
/// full game outcome value; lower values give less weight to distant outcomes.
/// </summary>
public static class ForwardViewTdCalculator
{
    public const int FullHeadCount = 5;

    /// <summary>
    /// Calculates the forward-view TD targets for a given game trajectory.
    /// </summary>
    /// <param name="trajectory">The game trajectory containing positions and results.</param>
    /// <param name="lambda">TD trace value in [0, 1]. 0 uses one-step returns; 1 uses the final game result.</param>
    /// <param name="gamma">Future-value discount in [0, 1]. Use 1 when the target is the eventual game outcome.</param>
    /// <param name="headCount">The number of output heads for the network.</param>
    /// <returns>A list of float arrays representing the forward-view TD targets for each position in the trajectory.</returns>
    public static IReadOnlyList<float[]> Calculate(
        GameTrajectory trajectory,
        float lambda,
        float gamma = 1.0f,
        int headCount = FullHeadCount)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        ValidateParameters(trajectory, lambda, gamma, headCount);

        var positions = trajectory.Positions;
        var labels = new float[positions.Count][];

        for (var positionIndex = 0; positionIndex < positions.Count; positionIndex++)
        {
            var position = positions[positionIndex];
            // the terminal result is expressed from the current players perspective
            var terminalReward = GetTerminalReward(trajectory, position.IsWhite, headCount);
            // this is the number of future positions available for the target
            var horizon = positions.Count - 1 - positionIndex;

            if (horizon == 0)
            {
                // we are at the end of the game, so the label is just the terminal reward
                labels[positionIndex] = terminalReward;
                continue;
            }

            var target = new float[headCount];
            // we apply the forward-view weights to each output head.
            var eligibilityWeight = 1.0f - lambda;

            // we build one return for every possible look-ahead distance.
            for (var step = 1; step <= horizon; step++)
            {
                // we let the lambda chooses between short returns and the full game return
                var weight = step == horizon
                    ? MathF.Pow(lambda, horizon - 1)
                    : eligibilityWeight * MathF.Pow(lambda, step - 1);

                if (weight == 0f)
                    continue;

                // we use gamma to discount the future prediction or terminal result
                var returnValue = GetNStepReturn(
                    trajectory,
                    positionIndex,
                    step,
                    terminalReward,
                    headCount,
                    gamma);

                // we add this weighted return to the target
                for (var head = 0; head < headCount; head++)
                    target[head] += weight * returnValue[head];
            }

            labels[positionIndex] = target;
        }

        return labels;
    }

    /// <summary>
    /// Returns terminal rewards for the given game <paramref name="result"/>.
    /// The win heads are cumulative: a gammon sets pWin and pGammonWin, while
    /// a backgammon sets pWin, pGammonWin, and pBackgammonWin. The loss heads
    /// follow the same pattern for gammon and backgammon losses.
    /// </summary>
    /// <param name="result">Game result.</param>
    /// <param name="headCount">Reward label count.</param>
    /// <returns>An array in size of <paramref name="headCount"/> containing the terminal rewards.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If headcount is not 1 or <see cref="FullHeadCount"/>.</exception>
    /// <exception cref="InvalidDataException">Unknown game result enum value.</exception>
    public static float[] GetTerminalReward(GameResult result, int headCount = FullHeadCount)
    {
        if (headCount is not (1 or FullHeadCount))
            throw new ArgumentOutOfRangeException(nameof(headCount), headCount, "Head count must be 1 or 5.");

        var reward = result switch
        {
            GameResult.Single or GameResult.DoubleDeclined or GameResult.Resign
                => new[] { 1f, 0f, 0f, 0f, 0f },
            GameResult.Gammon
                => new[] { 1f, 1f, 0f, 0f, 0f },
            GameResult.Backgammon
                => new[] { 1f, 1f, 1f, 0f, 0f },
            GameResult.LostSingle or GameResult.LostDoubleDeclined or GameResult.LostResign
                => new[] { 0f, 0f, 0f, 0f, 0f },
            GameResult.LostGammon
                => new[] { 0f, 0f, 0f, 1f, 0f },
            GameResult.LostBackgammon
                => new[] { 0f, 0f, 0f, 1f, 1f },
            GameResult.Draw
                => new[] { 0.5f, 0f, 0f, 0f, 0f },
            _ => throw new InvalidDataException($"Unsupported terminal game result: {result}.")
        };

        return headCount == 1 ? [reward[0]] : reward;
    }

    /// <summary>
    /// Converts a prediction to the other player's perspective.
    /// </summary>
    /// <param name="prediction">Prediction to invert.</param>
    /// <param name="headCount">Reward label count.</param>
    /// <returns>Inverted terminal reward predictions.</returns>
    /// <exception cref="ArgumentException">If prediction count is less than <paramref name="headCount"/>.</exception>
    public static float[] InvertPrediction(IReadOnlyList<float> prediction, int headCount = FullHeadCount)
    {
        ArgumentNullException.ThrowIfNull(prediction);
        if (prediction.Count < headCount)
            throw new ArgumentException("Prediction does not contain enough output heads.", nameof(prediction));

        if (headCount == 1)
            return [1f - prediction[0]];

        return
        [
            1f - prediction[0],
            prediction[3],
            prediction[4],
            prediction[1],
            prediction[2]
        ];
    }

    private static float[] GetNStepReturn(
        GameTrajectory trajectory,
        int positionIndex,
        int step,
        IReadOnlyList<float> terminalReward,
        int headCount,
        float gamma)
    {
        var finalPositionIndex = trajectory.Positions.Count - 1;
        var futurePositionIndex = positionIndex + step;
        var discount = MathF.Pow(gamma, step);

        if (futurePositionIndex == finalPositionIndex)
        {
            return Scale(terminalReward, discount);
        }

        var currentPosition = trajectory.Positions[positionIndex];
        var futurePosition = trajectory.Positions[futurePositionIndex];
        // we use the future prediction and convert its perspective when needed.
        var prediction = futurePosition.IsWhite == currentPosition.IsWhite
            ? futurePosition.Prediction.Take(headCount).ToArray()
            : InvertPrediction(futurePosition.Prediction, headCount);

        return Scale(prediction, discount);
    }

    private static float[] GetTerminalReward(GameTrajectory trajectory, bool isWhite, int headCount)
    {
        var activePlayerWon = isWhite == trajectory.WhiteWon;
        var result = activePlayerWon ? trajectory.WinnerResult : trajectory.LoserResult;
        return GetTerminalReward(result, headCount);
    }

    private static float[] Scale(IReadOnlyList<float> values, float factor)
    {
        var result = new float[values.Count];
        for (var i = 0; i < values.Count; i++)
            result[i] = values[i] * factor;
        return result;
    }

    private static void ValidateParameters(GameTrajectory trajectory, float lambda, float gamma, int headCount)
    {
        if (!float.IsFinite(lambda) || lambda is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(lambda), lambda, "Lambda must be finite and in [0, 1].");

        if (!float.IsFinite(gamma) || gamma is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(gamma), gamma, "Gamma must be finite and in [0, 1].");

        if (headCount is not (1 or FullHeadCount))
            throw new ArgumentOutOfRangeException(nameof(headCount), headCount, "Head count must be 1 or 5.");

        if (trajectory.Positions.Count == 0)
            throw new InvalidDataException("A trajectory must contain at least one position.");

        for (var index = 0; index < trajectory.Positions.Count; index++)
        {
            var position = trajectory.Positions[index];
            if (position.TurnIndex != index)
                throw new InvalidDataException($"Trajectory position {index} has turn index {position.TurnIndex}.");

            if (position.Prediction.Length < headCount)
                throw new InvalidDataException($"Trajectory position {index} has fewer than {headCount} predictions.");

            for (var head = 0; head < headCount; head++)
            {
                var prediction = position.Prediction[head];
                if (!float.IsFinite(prediction) || prediction is < 0f or > 1f)
                    throw new InvalidDataException($"Trajectory prediction {index}/{head} must be finite and in [0, 1].");
            }

            if (index > 0 && position.IsWhite == trajectory.Positions[index - 1].IsWhite)
                throw new InvalidDataException($"Trajectory positions {index - 1} and {index} have the same active player.");
        }

        if (!trajectory.Positions[^1].IsTerminal)
            throw new InvalidDataException("The final trajectory position must be terminal.");

        if (trajectory.Positions.Count(position => position.IsTerminal) != 1)
            throw new InvalidDataException("A trajectory must contain exactly one terminal position.");
    }
}
