using System.Diagnostics.Metrics;

using GammonX.Engine.Diagnostics;
using GammonX.Engine.Services;
using GammonX.Models.Enums;

namespace GammonX.Engine.Tests;

public class MoveGenerationCancellationTests
{
    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void CancellationDuringDoublesTraversalRestoresBoardAndHistory(GameModus modus)
    {
        var service = BoardServiceFactory.Create(modus);
        var board = service.CreateBoard();
        int[] rolls = [2, 2, 2, 2];
        service.AddRollEventToHistory(board, true, rolls);
        var original = board.DeepClone();
        var history = board.History.Events.ToArray();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var cancelledDuringTraversal = false;
        var traversalThread = Environment.CurrentManagedThreadId;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == EvaluationMetrics.MeterName && instrument.Name == EvaluationMetrics.DurationName)
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            if (Environment.CurrentManagedThreadId != traversalThread || board.History.Events.Count() <= history.Length)
                return;
            foreach (var tag in tags)
            {
                if (tag.Key == "stage" && Equals(tag.Value, EvaluationMetrics.TraversalSnapshot))
                {
                    cancelledDuringTraversal = true;
                    cancellation.Cancel();
                }
            }
        });
        listener.Start();

        Assert.ThrowsAny<OperationCanceledException>(() => service.GetUniqueLegalMoveSequences(board, true, rolls, cancellation.Token));

        Assert.True(cancelledDuringTraversal);
        Assert.True(board.Equals(original));
        Assert.Equal(history, board.History.Events.ToArray());
        Assert.Equal(new[] { 2, 2, 2, 2 }, rolls);
        listener.Dispose();
        Assert.NotEmpty(service.GetUniqueLegalMoveSequences(board, true, rolls, TestContext.Current.CancellationToken));
    }
}
