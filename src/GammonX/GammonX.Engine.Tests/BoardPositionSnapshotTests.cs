using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Engine.Tests;

public class BoardPositionSnapshotTests
{
    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void SnapshotPreservesVariantStateWithoutHistoryOrSharedMutableArrays(GameModus modus)
    {
        var service = BoardServiceFactory.Create(modus);
        var board = CreateBoard(service);
        service.AddRollEventToHistory(board, true, [2, 3]);
        service.AddRollEventToHistory(board, false, [4, 5]);

        var snapshot = board.CreatePositionSnapshot();

        Assert.NotSame(board, snapshot);
        Assert.Equal(board.GetType(), snapshot.GetType());
        Assert.True(board.Equals(snapshot));
        Assert.True(snapshot.Equals(board));
        Assert.Equal(board.GetHashCode(), snapshot.GetHashCode());
        Assert.Equal(board.PipCountWhite, snapshot.PipCountWhite);
        Assert.Equal(board.PipCountBlack, snapshot.PipCountBlack);
        Assert.NotSame(board.Fields, snapshot.Fields);
        Assert.NotSame(board.History, snapshot.History);
        Assert.Empty(snapshot.History.Events);
        Assert.Equal(2, board.History.Events.Count);
        var positions = new HashSet<IBoardModel> { snapshot };
        Assert.Contains(board, positions);

        if (board is IPinModel pins)
        {
            var snapshotPins = Assert.IsAssignableFrom<IPinModel>(snapshot);
            Assert.NotSame(pins.PinnedFields, snapshotPins.PinnedFields);
            snapshotPins.PinnedFields[6] = 0;
            Assert.Equal(-1, pins.PinnedFields[6]);
        }
        if (board is IHomeBarModel homeBar)
        {
            var snapshotHomeBar = Assert.IsAssignableFrom<IHomeBarModel>(snapshot);
            snapshotHomeBar.AddToHomeBar(true, 1);
            Assert.Equal(4, homeBar.HomeBarCountWhite);
        }
        if (board is IDoublingCubeModel cube)
        {
            var snapshotCube = Assert.IsAssignableFrom<IDoublingCubeModel>(snapshot);
            Assert.Equal(cube.DoublingCubeValue, snapshotCube.DoublingCubeValue);
            snapshotCube.DoublingCubeOwner = false;
            Assert.True(cube.DoublingCubeOwner);
        }
        snapshot.Fields[0] = 99;
        Assert.Equal(-4, board.Fields[0]);
        board.Fields[23] = 98;
        Assert.Equal(3, snapshot.Fields[23]);
        snapshot.BearOffChecker(true, 1);
        Assert.Equal(2, board.BearOffCountWhite);
        service.AddRollEventToHistory(snapshot, true, [6, 6, 6, 6]);
        Assert.Single(snapshot.History.Events);
        Assert.Equal(2, board.History.Events.Count);
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void UniqueGenerationDoesNotCloneHistoryEventsButDeepCloneStillDoes(GameModus modus)
    {
        var service = BoardServiceFactory.Create(modus);
        var board = Assert.IsAssignableFrom<BoardBaseImpl>(service.CreateBoard());
        var historyEvent = new Mock<IHistoryEvent>();
        var cloneableEvent = historyEvent.As<ICloneable>();
        cloneableEvent.Setup(value => value.Clone())
            .Throws(new InvalidOperationException("History cloning must not occur in a position snapshot."));
        Assert.IsAssignableFrom<IEditableBoardHistory>(board.History).Add(historyEvent.Object);

        var snapshot = board.CreatePositionSnapshot();
        var sequences = service.GetUniqueLegalMoveSequences(board, false, 2, 2, 2, 2);

        Assert.NotEmpty(sequences);
        Assert.Empty(snapshot.History.Events);
        Assert.Same(historyEvent.Object, Assert.Single(board.History.Events));
        cloneableEvent.Verify(value => value.Clone(), Times.Never);
        Assert.Throws<InvalidOperationException>(() => board.Clone());
        cloneableEvent.Verify(value => value.Clone(), Times.Once);
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void SnapshotAllocationDoesNotGrowWithHistoryLength(GameModus modus)
    {
        var service = BoardServiceFactory.Create(modus);
        var board = CreateBoard(service);
        var withoutHistory = AllocatedForSnapshots(board);
        for (var turn = 0; turn < 100; turn++)
            service.AddRollEventToHistory(board, turn % 2 == 0, [2, 3]);

        var withHistory = AllocatedForSnapshots(board);

        Assert.Equal(withoutHistory, withHistory);
    }

    private static long AllocatedForSnapshots(BoardBaseImpl board)
    {
        GC.KeepAlive(board.CreatePositionSnapshot());
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++)
            GC.KeepAlive(board.CreatePositionSnapshot());
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static BoardBaseImpl CreateBoard(IBoardService service)
    {
        var fields = new int[24];
        fields[0] = -4;
        fields[23] = 3;
        var pins = new int[24];
        pins[6] = -1;
        return Assert.IsAssignableFrom<BoardBaseImpl>(service.CreateBoard(new BoardModelContract
        {
            Fields = fields,
            PinnedFields = pins,
            BearOffCountWhite = 2,
            BearOffCountBlack = 3,
            HomeBarCountWhite = 4,
            HomeBarCountBlack = 5,
            DoublingCubeValue = 4,
            DoublingCubeOwner = true
        }));
    }
}
