using GammonX.Engine.Models;
using GammonX.Mars.NN;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class SelfPlayRunnerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DualModelRunUsesModelAForRecordingAndDiagnostics(bool modelAIsWhite)
    {
        const GameModus modus = GameModus.Backgammon;
        var modelA = new ConstantNeuralEvalService(0.25f);
        var modelB = new ConstantNeuralEvalService(0.75f);
        var recorder = new SelfPlayRecorder(
            FeatureVectorExtractorFactory.Create(modus),
            modelA);
        var options = new ExplorationOptions
        {
            CollectScoreGapDiagnostics = true,
            EarlyRankedExplorationProbability = 0f,
            LateRankedExplorationProbability = 0f,
            AllLegalExplorationProbability = 0f
        };

        var entryA = new SelfPlayEntry(modelA, BotLevel.Hard);
        var entryB = new SelfPlayEntry(modelB, BotLevel.Hard);

        var runner = new SelfPlayRunner(recorder, modus, entryA, entryB, options);

        var result = await runner.RunAsync(
            EvalWeights.GetContactWeights(modus),
            EvalWeights.GetCheapContactWeights(modus),
            EvalWeights.GetRaceWeights(modus),
            modelAIsWhite);

        Assert.NotNull(result.Trajectory);
        Assert.NotEmpty(result.Samples);
        Assert.Equal(result.Samples.Count, result.Trajectory!.Positions.Count);
        Assert.True(modelA.PredictionCount > 0);
        Assert.True(modelB.PredictionCount > 0);
        Assert.NotNull(result.ConstraintMetrics);
        Assert.Equal(result.Trajectory.Positions.Count, result.ConstraintMetrics!.RowCount);
        Assert.NotNull(result.PredictionVariance);
        Assert.NotEmpty(result.ExplorationDecisions!);

        Assert.All(
            result.Trajectory.Positions,
            position => Assert.Equal(0.25f, position.Prediction[0]));

        Assert.All(
            result.ExplorationDecisions!,
            decision => Assert.Equal(
                modelAIsWhite,
                result.Trajectory.Positions[decision.TurnIndex - 1].IsWhite));
    }

    private sealed class ConstantNeuralEvalService : INeuralEvalService
    {
        private readonly float _pWin;

        public ConstantNeuralEvalService(float pWin)
        {
            _pWin = pWin;
        }

        public int PredictionCount { get; private set; }

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            PredictionCount++;
            return Task.FromResult(new [] { _pWin, 0.1f, 0.05f, 0.1f, 0.05f });
        }
    }
}
