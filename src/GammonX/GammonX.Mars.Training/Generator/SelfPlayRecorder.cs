using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.Training.Sidecars;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Generator
{
    /// <summary>
    /// Records (features, outcome) pairs during self-play for NN training.
    /// All positions use TD(lambda) with the recorded one-ply or searched predictions.
    /// </summary>
    public sealed class SelfPlayRecorder
    {
        public const float DefaultLambda = 1.0f;

        private readonly IFeatureVectorExtractor _extractor;

        private readonly INeuralEvalService? _neuralEvalService; // null = linear training weights

        private readonly float _lambda;

        private readonly List<TrajectoryPosition> _positions = [];

        private ConstraintMetricsAccumulator? _constraintMetrics;

        public Guid GameId { get; } = Guid.NewGuid();

        /// <summary>
        /// Returns the recorded one-ply or searched outcome predictions collected during the game.
        /// </summary>
        public IReadOnlyList<float[]> Predictions => [.. _positions.Select(p => p.Prediction)];

        public ConstraintMetricsResult? ConstraintMetrics => _constraintMetrics?.Complete();

        public SelfPlayRecorder(
            IFeatureVectorExtractor extractor,
            INeuralEvalService? neuralEvalService = null,
            float lambda = DefaultLambda)
        {
            _extractor = extractor;
            _lambda = lambda;
            _neuralEvalService = neuralEvalService;
        }

        /// <summary>
        /// Records the resulting board features after a move was played.
        /// Features are already in active-player perspective so white and black
        /// positions are directly comparable — no separate recorder per color needed.
        /// </summary>
        /// <param name="model">Normalized features of the resulting board state.</param>
        /// <param name="board">Target board.</param>
        /// <param name="isWhite"><c>true</c> if the active player this turn was white.</param>
        /// <param name="prediction">The played candidate evaluation, or null to evaluate the teacher directly.</param>
        public async Task RecordPositionAsync(
            NormalizedEvalResultModel model,
            IBoardModel board,
            bool isWhite,
            float[]? prediction = null)
        {
            var features = _extractor.Extract(model, board, isWhite);

            if (prediction == null && _neuralEvalService != null)
            {
                prediction = await _neuralEvalService.PredictAsync(model, board, isWhite);
            }

            if (prediction?.Length == GameOutcomeConstraintValidator.FullHeadCount)
            {
                (_constraintMetrics ??= new ConstraintMetricsAccumulator()).AddRow(prediction);
            }

            prediction ??= [0.5f, 0.0f, 0.0f, 0.0f, 0.0f];

            _positions.Add(new TrajectoryPosition(
                _positions.Count,
                isWhite,
                features,
                prediction.ToArray()));
        }

        /// <summary>
        /// Records the played candidate's one-ply or searched evaluation for TD bootstrapping.
        /// </summary>
        public Task RecordPositionAsync(FinalEvalResultModel result, IBoardModel board, bool isWhite)
        {
            return RecordPositionAsync(result.EvalResult, board, isWhite, result.Prediction);
        }

        /// <summary>
        /// Finalizes the recording and returns all (features, label) training samples.
        /// All labels are TD(lambda) returns; terminal positions use exact outcomes.
        /// </summary>
        /// <param name="winnerResult">The result from the winner's perspective.</param>
        /// <param name="loserResult">The result from the loser's perspective.</param>
        /// <param name="whiteWon">Whether white won the game.</param>
        public (GameTrajectory Trajectory, IReadOnlyList<(float[] Features, float[] Label)> Samples) Finalize(GameResult winnerResult, GameResult loserResult, bool whiteWon)
        {
            if (_positions.Count == 0)
            {
                return (CreateTrajectory(winnerResult, loserResult, whiteWon), []);
            }

            var trajectory = CreateTrajectory(winnerResult, loserResult, whiteWon);
            var labels = ForwardViewTdCalculator.Calculate(trajectory, _lambda);

            var samples = trajectory.Positions
                .Select((position, index) => (position.Features, labels[index]))
                .ToList();

            return (trajectory, samples);
        }

        private GameTrajectory CreateTrajectory(GameResult winnerResult, GameResult loserResult, bool whiteWon)
        {
            var positions = _positions
                .Select((position, index) => position with
                {
                    TurnIndex = index,
                    IsTerminal = index == _positions.Count - 1
                })
                .ToArray();

            return new GameTrajectory(GameId, positions, winnerResult, loserResult, whiteWon);
        }
    }
}
