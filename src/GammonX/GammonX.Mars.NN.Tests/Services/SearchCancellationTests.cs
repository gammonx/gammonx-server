using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Mars.NN.Tests.Services;

public class SearchCancellationTests
{
    [Theory]
    [InlineData(GameModus.Backgammon, true, BotLevel.Expert)]
    [InlineData(GameModus.Backgammon, false, BotLevel.Expert)]
    [InlineData(GameModus.Tavla, true, BotLevel.Expert)]
    [InlineData(GameModus.Portes, true, BotLevel.Expert)]
    [InlineData(GameModus.Plakoto, true, BotLevel.Expert)]
    [InlineData(GameModus.Fevga, true, BotLevel.Expert)]
    [InlineData(GameModus.Backgammon, true, BotLevel.Hard)]
    public async Task CancellingInferenceRestoresRootBoardAndHistory(GameModus modus, bool reuseReplyBoard, BotLevel level)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var boards = BoardServiceFactory.Create(modus);
        var board = boards.CreateBoard();
        boards.AddRollEventToHistory(board, true, [1, 2]);
        var original = board.DeepClone();
        var history = board.History.Events.ToArray();
        var moves = boards.GetUniqueLegalMoveSequences(board, true, [1, 2], cancellation.Token).Take(2).ToArray();
        Assert.NotEmpty(moves);
        var neural = new Mock<INeuralEvalService>();
        neural.SetupGet(value => value.MaxBatchSize).Returns(4);
        neural.Setup(value => value.ExtractFeatures(It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>()))
            .Returns([1f]);
        neural.Setup(value => value.ExtractFeatures(It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<float[]?>()))
            .Returns((NormalizedEvalResultModel _, IBoardModel _, bool _, float[]? destination) => destination ?? [1f]);
        neural.Setup(value => value.PredictBatchAsync(It.IsAny<float[][]>(), It.IsAny<CancellationToken>()))
            .Returns((float[][] _, CancellationToken token) =>
            {
                Assert.Equal(cancellation.Token, token);
                cancellation.Cancel();
                return Task.FromCanceled<float[][]>(token);
            });
        neural.Setup(value => value.PredictAsync(It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((NormalizedEvalResultModel _, IBoardModel _, bool _, CancellationToken token) =>
            {
                Assert.Equal(cancellation.Token, token);
                cancellation.Cancel();
                return Task.FromCanceled<float[]>(token);
            });
        var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(modus, neural.Object));
        service.ReuseReplyBoard = reuseReplyBoard;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetCandidatesByEvalAsync(
            board, moves, true, EvalWeights.GetContactWeights(modus), moves.Length, level, cancellation.Token));

        Assert.True(board.Equals(original));
        Assert.Equal(history, board.History.Events.ToArray());
    }
}
