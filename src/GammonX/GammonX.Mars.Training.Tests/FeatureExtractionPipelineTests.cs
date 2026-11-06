using GammonX.Engine.Extensions;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Services;
using GammonX.Mars.Training.Generator;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using Microsoft.Extensions.Hosting;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class FeatureExtractionPipelineTests
{
    [Theory]
    [MemberData(nameof(InferencePaths))]
    public async Task RecorderAndBothInferenceOverloadsUseIdenticalCurrentFeatures(GameModus mode, bool batched)
    {
        // we test the whole training pipeline
        var modelPath = Path.Combine(Path.GetTempPath(), $"gammonx-features-{Guid.NewGuid():N}.dat");
        INeuralEvalService? neural = null;
        try
        {
            // fresh parameters exercise current services without depending on any existing model.
            var outputMode = mode is GameModus.Fevga or GameModus.Plakoto
                ? GameOutcomeOutputMode.LegacyIndependentSigmoid : GameOutcomeOutputMode.MonotonicCumulative;

            var net = NetModelFactory.CreateNew(mode, CPU, outputMode, NetArchitecture.A);
            try
            {
                net.Save(modelPath);
            }
            finally
            {
                (net as IDisposable)?.Dispose();
            }

            NetModelMetadata.Write(modelPath, mode, outputMode, NetArchitecture.A);
            neural = batched ? BatchedNeuralEvalService.Load(mode, modelPath, CPU) : NeuralEvalService.Load(mode, modelPath, CPU);

            if (neural is IHostedService worker)
            {
                await worker.StartAsync(TestContext.Current.CancellationToken);
            }

            // we construct some test boards
            var fields = new int[24];
            fields[0] = -4; fields[7] = -1; fields[13] = -6;
            fields[3] = 1; fields[11] = 3; fields[21] = mode == GameModus.Plakoto ? 5 : 4;
            var pins = new int[24];
            pins[3] = -1; pins[7] = 1;

            var board = BoardServiceFactory.Create(mode).CreateBoard(new BoardModelContract
            {
                Fields = fields, PinnedFields = mode == GameModus.Plakoto ? pins : null,
                BearOffCountWhite = 3, BearOffCountBlack = 5,
                HomeBarCountWhite = mode == GameModus.Plakoto ? 0 : 1,
                HomeBarCountBlack = mode == GameModus.Plakoto ? 0 : 2
            });

            var extractor = FeatureVectorExtractorFactory.Create(mode);
            var recorder = new SelfPlayRecorder(extractor, neural);
            var expectedRows = new List<float[]>();
            var expectedPredictions = new List<float[]>();
            var original = board.DeepClone();

            foreach (var white in new[] { true, false })
            {
                var model = FeatureEvalServiceFactory.Create(mode, null!).EvalPositionForTraining(new FeatureEvalOptions
                {
                    BoardContract = board.ToContract(false), IsWhite = white, TurnNumber = 137
                });

                var expected = extractor.Extract(model, board, white);
                var allocated = neural.ExtractFeatures(model, board, white);
                var destination = Enumerable.Repeat(float.NaN, extractor.FeatureCount).ToArray();

                Assert.Same(destination, neural.ExtractFeatures(model, board, white, destination));
                Assert.Equal(expected, allocated);
                Assert.Equal(expected, destination);
                Assert.All(expected, value => Assert.True(float.IsFinite(value) && value >= 0 && value <= 1));

                expectedRows.Add(expected);
                expectedPredictions.Add((await neural.PredictBatchAsync([expected]))[0]);
                await recorder.RecordPositionAsync(model, board, white);
            }

            Assert.Equal(original, board);
            // recorded arrays must remain snapshots when the live board subsequently changes.
            Array.Clear(board.Fields);
            var (trajectory, samples) = recorder.Finalize(GameResult.Single, GameResult.LostSingle, true);

            Assert.Equal(2, samples.Count);

            for (var row = 0; row < samples.Count; row++)
            {
                Assert.Equal(expectedRows[row], samples[row].Features);
                Assert.Equal(expectedRows[row], trajectory.Positions[row].Features);
                Assert.Equal(expectedPredictions[row], trajectory.Positions[row].Prediction);
                Assert.Equal(row == 0, trajectory.Positions[row].IsWhite);
            }
        }
        finally
        {
            if (neural is IHostedService worker)
            {
                await worker.StopAsync(CancellationToken.None);
            }
            (neural as IDisposable)?.Dispose();
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }

    public static IEnumerable<object[]> InferencePaths() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Fevga, GameModus.Plakoto }
            .SelectMany(mode => new[] { false, true }.Select(batched => new object[] { mode, batched }));
}
