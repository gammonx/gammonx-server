using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.Training.Generator;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Mars.Training.Tests;

public sealed class SelfPlayRunnerTests
{
    [Fact]
    public async Task SelectiveTwoPlyReranksOnlyAmbiguousTopCandidates()
    {
        var onePlyResults = CreateRankedResults(0.50d, 0.49d, 0.20d, 0.10d);
        var twoPlyResults = new FinalEvalResultModels
        {
            new(0.60d, onePlyResults[1].MoveSequence, onePlyResults[1].EvalResult),
            new(0.40d, onePlyResults[0].MoveSequence, onePlyResults[0].EvalResult),
            new(0.30d, onePlyResults[2].MoveSequence, onePlyResults[2].EvalResult)
        };
        var evalService = new Mock<IFeatureEvalService>();
        evalService
            .Setup(service => service.EvalMoveSequenceCandidatesAsync(
                It.IsAny<GammonX.Models.Contracts.BoardModelContract>(),
                true,
                It.Is<IReadOnlyList<MoveSequenceModel>>(moves => moves.Count == 3),
                It.IsAny<ContactWeightModel>(),
                BotLevel.TwoPly))
            .ReturnsAsync(twoPlyResults);

        var result = await SelfPlayRunner.ApplySelectiveTwoPlyAsync(
            evalService.Object,
            new GammonX.Models.Contracts.BoardModelContract { Fields = new int[24] },
            true,
            onePlyResults,
            EvalWeights.GetContactWeights(GameModus.Backgammon),
            new SelectiveTwoPlyOptions
            {
                Enabled = true,
                MaximumOnePlyGap = 0.02d,
                MaximumCandidates = 3,
                FullSearchAuditProbability = 0f
            },
            0.5f,
            Guid.NewGuid(),
            GameModus.Backgammon,
            4,
            false);

        Assert.Equal(4, result.Results.Count);
        Assert.Equal(twoPlyResults, result.Results.Take(3));
        Assert.Same(onePlyResults[3], result.Results[3]);
        Assert.Equal(SelectiveTwoPlyReason.Ambiguous, result.Decision.Reason);
        Assert.Equal(3, result.Decision.EvaluatedCandidateCount);
        Assert.Equal(3, result.Decision.SelectiveCandidateLimit);
        Assert.Equal(0.01d, result.Decision.OnePlyScoreGap!.Value, 10);
        Assert.Equal(0.20d, result.Decision.TwoPlyScoreGap!.Value, 10);
        Assert.True(result.Decision.BestMoveChanged);
        Assert.Equal(2, result.Decision.TwoPlyBestOnePlyRank);
        evalService.VerifyAll();
    }

    [Fact]
    public async Task SelectiveTwoPlySkipsClearPosition()
    {
        var onePlyResults = CreateRankedResults(0.50d, 0.40d);
        var evalService = new Mock<IFeatureEvalService>();

        var result = await SelfPlayRunner.ApplySelectiveTwoPlyAsync(
            evalService.Object,
            new GammonX.Models.Contracts.BoardModelContract { Fields = new int[24] },
            true,
            onePlyResults,
            EvalWeights.GetContactWeights(GameModus.Backgammon),
            new SelectiveTwoPlyOptions
            {
                Enabled = true,
                MaximumOnePlyGap = 0.02d,
                MaximumCandidates = 3,
                FullSearchAuditProbability = 0f
            },
            0.5f,
            Guid.NewGuid(),
            GameModus.Backgammon,
            4,
            false);

        Assert.Same(onePlyResults, result.Results);
        Assert.Equal(SelectiveTwoPlyReason.GapTooLarge, result.Decision.Reason);
        Assert.Equal(0, result.Decision.EvaluatedCandidateCount);
        Assert.Null(result.Decision.TwoPlyBestScore);
        evalService.Verify(
            service => service.EvalMoveSequenceCandidatesAsync(
                It.IsAny<GammonX.Models.Contracts.BoardModelContract>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyList<MoveSequenceModel>>(),
                It.IsAny<ContactWeightModel>(),
                It.IsAny<BotLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectiveTwoPlyAuditRecordsWinnerOutsideConfiguredCandidateLimit()
    {
        var onePlyResults = CreateRankedResults(0.50d, 0.40d, 0.30d, 0.20d, 0.10d);
        var twoPlyResults = new FinalEvalResultModels
        {
            new(0.80d, onePlyResults[3].MoveSequence, onePlyResults[3].EvalResult),
            new(0.70d, onePlyResults[0].MoveSequence, onePlyResults[0].EvalResult),
            new(0.60d, onePlyResults[1].MoveSequence, onePlyResults[1].EvalResult),
            new(0.50d, onePlyResults[2].MoveSequence, onePlyResults[2].EvalResult),
            new(0.40d, onePlyResults[4].MoveSequence, onePlyResults[4].EvalResult)
        };
        var evalService = new Mock<IFeatureEvalService>();
        evalService
            .Setup(service => service.EvalMoveSequenceCandidatesAsync(
                It.IsAny<GammonX.Models.Contracts.BoardModelContract>(),
                true,
                It.Is<IReadOnlyList<MoveSequenceModel>>(moves => moves.Count == 5),
                It.IsAny<ContactWeightModel>(),
                BotLevel.TwoPly))
            .ReturnsAsync(twoPlyResults);

        var result = await SelfPlayRunner.ApplySelectiveTwoPlyAsync(
            evalService.Object,
            new GammonX.Models.Contracts.BoardModelContract { Fields = new int[24] },
            true,
            onePlyResults,
            EvalWeights.GetContactWeights(GameModus.Backgammon),
            new SelectiveTwoPlyOptions
            {
                Enabled = true,
                MaximumOnePlyGap = 0.02d,
                MaximumCandidates = 3,
                FullSearchAuditProbability = 1f
            },
            0.5f,
            Guid.NewGuid(),
            GameModus.Backgammon,
            4,
            false);

        Assert.Same(twoPlyResults, result.Results);
        Assert.Equal(SelectiveTwoPlyReason.Audit, result.Decision.Reason);
        Assert.Equal(3, result.Decision.SelectiveCandidateLimit);
        Assert.Equal(4, result.Decision.TwoPlyBestOnePlyRank);
        Assert.True(result.Decision.BestMoveChanged);
        evalService.VerifyAll();
    }

    [Fact]
    public void AllLegalExplorationUsesUniqueResultingBoards()
    {
        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard();
        var rolls = new[] { 1, 2 };

        var rawMoves = boardService.GetLegalMoveSequences(board, true, rolls);
        var explorationMoves = SelfPlayRunner.GetAllLegalExplorationMoves(
            boardService,
            board,
            true,
            rolls);
        var uniqueMoves = boardService.GetUniqueLegalMoveSequences(board, true, rolls);

        Assert.True(rawMoves.Length > uniqueMoves.Length);
        Assert.Equal(uniqueMoves, explorationMoves);
    }

    [Fact]
    public void ConstructorRejectsModelBWithoutEvaluationService()
    {
        const GameModus modus = GameModus.Backgammon;
        var modelA = new ConstantNeuralEvalService(0.25f);
        var recorder = new SelfPlayRecorder(
            FeatureVectorExtractorFactory.Create(modus),
            modelA);

        var exception = Assert.Throws<ArgumentException>(() => new SelfPlayRunner(
            recorder,
            modus,
            new SelfPlayEntry(modelA, BotLevel.Hard),
            new SelfPlayEntry(null, BotLevel.Hard)));

        Assert.Equal("entryB", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsSelectiveSearchWithoutNeuralModel()
    {
        const GameModus modus = GameModus.Backgammon;
        var recorder = new SelfPlayRecorder(
            FeatureVectorExtractorFactory.Create(modus),
            null);

        var exception = Assert.Throws<ArgumentException>(() => new SelfPlayRunner(
            recorder,
            modus,
            new SelfPlayEntry(null, BotLevel.Hard),
            selectiveTwoPlyOptions: new SelectiveTwoPlyOptions { Enabled = true }));

        Assert.Equal("selectiveTwoPlyOptions", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsSelectiveSearchAfterFullTwoPlyRanking()
    {
        const GameModus modus = GameModus.Backgammon;
        var model = new ConstantNeuralEvalService(0.25f);
        var recorder = new SelfPlayRecorder(
            FeatureVectorExtractorFactory.Create(modus),
            model);

        var exception = Assert.Throws<ArgumentException>(() => new SelfPlayRunner(
            recorder,
            modus,
            new SelfPlayEntry(model, BotLevel.TwoPly),
            selectiveTwoPlyOptions: new SelectiveTwoPlyOptions { Enabled = true }));

        Assert.Equal("selectiveTwoPlyOptions", exception.ParamName);
    }

    [Fact]
    public async Task SingleModelRunCollectsExplorationDecisionsForBothPlayers()
    {
        const GameModus modus = GameModus.Backgammon;
        var model = new ConstantNeuralEvalService(0.25f);
        var recorder = new SelfPlayRecorder(
            FeatureVectorExtractorFactory.Create(modus),
            model);
        var options = new ExplorationOptions
        {
            CollectScoreGapDiagnostics = true,
            EarlyRankedExplorationProbability = 0f,
            LateRankedExplorationProbability = 0f,
            AllLegalExplorationProbability = 0f
        };
        var runner = new SelfPlayRunner(
            recorder,
            modus,
            new SelfPlayEntry(model, BotLevel.Hard),
            explorationOptions: options);

        var result = await runner.RunAsync(EvalWeights.GetContactWeights(modus), modelAIsWhite: true);

        Assert.NotNull(result.Trajectory);
        Assert.Contains(result.ExplorationDecisions!, decision =>
            result.Trajectory!.Positions[decision.TurnIndex - 1].IsWhite);
        Assert.Contains(result.ExplorationDecisions!, decision =>
            !result.Trajectory!.Positions[decision.TurnIndex - 1].IsWhite);
    }

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

        var result = await runner.RunAsync(EvalWeights.GetContactWeights(modus), modelAIsWhite);

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

    private static FinalEvalResultModels CreateRankedResults(params double[] scores)
    {
        return new FinalEvalResultModels(scores.Select((score, index) =>
        {
            var moveSequence = new MoveSequenceModel();
            moveSequence.Moves.Add(new MoveModel(index, index + 1));
            return new FinalEvalResultModel(score, moveSequence, new NormalizedEvalResultModel());
        }));
    }
}
