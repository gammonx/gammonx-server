using System.Net.Http.Json;

using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.Server.Controllers;
using GammonX.Mars.Server.Tests.Data;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Controller;

public class EvalControllerCancellationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EvalControllerCancellationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("move")]
    [InlineData("board")]
    [InlineData("cube")]
    public async Task RequestCancellationIsNotConvertedToBadRequest(string endpoint)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IFeatureEvalService>(GameModus.Backgammon,
            new DefaultFeatureEvalService(new CancellationProbe(), GameModus.Backgammon));
        using var provider = services.BuildServiceProvider();
        var controller = new EvalController(provider);
        var request = CreateRequest();

        Task<IActionResult> Execute() => endpoint switch
        {
            "move" => controller.Move(request, cancellation.Token),
            "board" => controller.Board(new EvalBoardRequestContract
            {
                Board = request.Board, Modus = request.Modus, BotLevel = request.BotLevel
            }, cancellation.Token),
            "cube" => controller.Cube(new EvalCubeRequestContract
            {
                Board = request.Board, Modus = request.Modus, BotLevel = request.BotLevel
            }, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(Execute);
    }

    [Fact]
    public async Task AbortingHttpRequestCancelsInferenceAndAllowsAnotherRequest()
    {
        var probe = new CancellationProbe();
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddKeyedSingleton<INeuralEvalService>(GameModus.Backgammon, probe)));
        using var client = factory.CreateClient();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var request = CreateRequest();
        request.BotLevel = BotLevel.Expert;
        var pending = client.PostAsJsonAsync("/bot/mars/api/eval/move", request, cancellation.Token);
        try
        {
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await probe.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        request.BotLevel = BotLevel.Hard;
        using var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static EvalMoveRequestContract CreateRequest() => new()
    {
        Board = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1)!,
        Modus = GameModus.Backgammon,
        BotLevel = BotLevel.Hard,
        IsWhite = true,
        Rolls = [1, 2]
    };

    private sealed class CancellationProbe : INeuralEvalService
    {
        private readonly IFeatureVectorExtractor _extractor = FeatureVectorExtractorFactory.Create(GameModus.Backgammon);
        private int _calls;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MaxBatchSize => 4;

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
            _extractor.Extract(model, board, isWhite);

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination)
        {
            destination ??= new float[_extractor.FeatureCount];
            _extractor.Extract(model, board, isWhite, destination);
            return destination;
        }

        public async Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, CancellationToken cancellationToken) =>
            (await PredictBatchAsync([ExtractFeatures(model, board, isWhite)], cancellationToken))[0];

        public async Task<float[][]> PredictBatchAsync(float[][] features, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                Started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Cancelled.TrySetResult();
                    throw;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return features.Select(_ => new[] { 0.5f, 0f, 0f, 0f, 0f }).ToArray();
        }
    }
}
