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
    /// Each position is labelled with the terminal game outcome from the active
    /// player's perspective: 1.0 = that player won, 0.0 = that player lost.
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
        /// Returns the raw network predictions collected during the game.
        /// Only meaningful when a neural eval service is present; otherwise all values are 0.5.
        /// </summary>
        public IReadOnlyList<float[]> NetPredictions => [.. _positions.Select(p => p.Prediction)];

        public ConstraintMetricsResult? ConstraintMetrics => _constraintMetrics?.Complete();

        public SelfPlayRecorder(IFeatureVectorExtractor extractor, INeuralEvalService? neuralEvalService = null, float lambda = DefaultLambda)
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
        public async Task RecordPositionAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var features = _extractor.Extract(model, board, isWhite);
            // we store the networks current prediction for this state (0.5 if no net yet)
            float[] netPred = [0.5f, 0.0f, 0.0f, 0.0f, 0.0f];
            if (_neuralEvalService != null)
            {
                netPred = await _neuralEvalService.PredictAsync(model, board, isWhite);
                if (netPred.Length == GameOutcomeConstraintValidator.FullHeadCount)
                {
                    (_constraintMetrics ??= new ConstraintMetricsAccumulator()).AddRow(netPred);
                }
            }
            _positions.Add(new TrajectoryPosition(_positions.Count, isWhite, features, netPred));
        }

        /// <summary>
        /// Finalizes the recording and returns all (features, label) training samples.
        /// Each position receives the terminal game outcome from the active player's perspective.
        /// </summary>
        /// <param name="winnerResult">The result from the winner's perspective.</param>
        /// <param name="loserResult">The result from the loser's perspective.</param>
        /// <param name="whiteWon">Whether white won the game.</param>
        public (GameTrajectory Trajectory, IReadOnlyList<(float[] Features, float[] Label)> Samples) Finalize(GameResult winnerResult, GameResult loserResult, bool whiteWon)
        {
            if (_positions.Count == 0)
                return (CreateTrajectory(winnerResult, loserResult, whiteWon), []);

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
