using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Engine.Tests;

public class BoardGameResultTests
{
    [Theory]
    [InlineData(GameModus.Backgammon, true, 0, 12)]
    [InlineData(GameModus.Backgammon, false, 0, 12)]
    [InlineData(GameModus.Backgammon, true, 1, 4)]
    [InlineData(GameModus.Backgammon, false, 1, 4)]
    [InlineData(GameModus.Tavla, true, 0, 2)]
    [InlineData(GameModus.Tavla, false, 1, 1)]
    [InlineData(GameModus.Portes, true, 0, 2)]
    [InlineData(GameModus.Portes, false, 1, 1)]
    [InlineData(GameModus.Plakoto, true, 0, 2)]
    [InlineData(GameModus.Plakoto, false, 1, 1)]
    [InlineData(GameModus.Fevga, true, 0, 2)]
    [InlineData(GameModus.Fevga, false, 1, 1)]
    public void ResignationScoresUnfinishedBoard(GameModus modus, bool winnerIsWhite, int loserBorneOff, int expected)
    {
        // Arrange.
        var fields = new int[24];
        fields[winnerIsWhite ? 23 : 0] = winnerIsWhite ? 15 - loserBorneOff : -(15 - loserBorneOff);
        var board = BoardServiceFactory.Create(modus).CreateBoard(new BoardModelContract
        {
            Fields = fields,
            BearOffCountWhite = winnerIsWhite ? 0 : loserBorneOff,
            BearOffCountBlack = winnerIsWhite ? loserBorneOff : 0,
            DoublingCubeValue = 4
        });
        var winnerId = Guid.NewGuid();

        // Act.
        var result = board.ToResignationResult(winnerId, winnerIsWhite);

        // Assert.
        Assert.Equal(new GameResultModel(winnerId, GameResult.Resign, GameResult.LostResign, expected), result);
    }

    [Theory]
    [InlineData(true, 0, 8)]
    [InlineData(false, 0, 8)]
    [InlineData(true, 1, 12)]
    [InlineData(false, 1, 12)]
    public void ResignationDistinguishesGammonFromBarBackgammon(bool winnerIsWhite, int loserBar, int expected)
    {
        // Arrange.
        var board = CreateWonBoard(winnerIsWhite, 12, cubeValue: 4, loserBarCount: loserBar);

        // Act.
        var result = board.ToResignationResult(Guid.NewGuid(), winnerIsWhite);

        // Assert.
        Assert.Equal(expected, result.Points);
    }

    [Theory]
    [InlineData(true, 18)]
    [InlineData(true, 19)]
    [InlineData(true, 20)]
    [InlineData(true, 21)]
    [InlineData(true, 22)]
    [InlineData(true, 23)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    public void LoserCheckerOnAnyWinnersHomePointAwardsBackgammon(bool winnerIsWhite, int loserPoint)
    {
        // Includes index 23 for white and index 5 for black, omitted by Take(Range).
        var board = CreateWonBoard(winnerIsWhite, loserPoint, cubeValue: 4);
        var winnerId = Guid.NewGuid();

        var result = board.ToGameResult(winnerId, winnerIsWhite);

        Assert.Equal(new GameResultModel(winnerId, GameResult.Backgammon, GameResult.LostBackgammon, 12), result);
    }

    [Theory]
    [InlineData(true, 17)]
    [InlineData(true, 12)]
    [InlineData(true, 0)]
    [InlineData(false, 6)]
    [InlineData(false, 12)]
    [InlineData(false, 23)]
    public void LoserCheckersOutsideWinnersHomeBoardAwardGammon(bool winnerIsWhite, int loserPoint)
    {
        var board = CreateWonBoard(winnerIsWhite, loserPoint);
        var winnerId = Guid.NewGuid();

        var result = board.ToGameResult(winnerId, winnerIsWhite);

        Assert.Equal(new GameResultModel(winnerId, GameResult.Gammon, GameResult.LostGammon, 2), result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoserCheckerOnBarAwardsBackgammonWithoutHomeBoardCheckers(bool winnerIsWhite)
    {
        var board = CreateWonBoard(winnerIsWhite, loserPoint: 12, cubeValue: 2, loserBarCount: 1);
        var winnerId = Guid.NewGuid();

        var result = board.ToGameResult(winnerId, winnerIsWhite);

        Assert.Equal(new GameResultModel(winnerId, GameResult.Backgammon, GameResult.LostBackgammon, 6), result);
    }

    [Theory]
    [InlineData(true, 23, 0)]
    [InlineData(false, 5, 0)]
    [InlineData(true, 23, 1)]
    [InlineData(false, 5, 1)]
    [InlineData(true, 12, 1)]
    [InlineData(false, 12, 1)]
    public void LoserWithBorneOffCheckerLosesSingleDespiteHomeBoardOrBarCheckers(
        bool winnerIsWhite, int loserPoint, int loserBarCount)
    {
        var board = CreateWonBoard(winnerIsWhite, loserPoint, cubeValue: 4,
            loserBearOffCount: 1, loserBarCount: loserBarCount);
        var winnerId = Guid.NewGuid();

        var result = board.ToGameResult(winnerId, winnerIsWhite);

        Assert.Equal(new GameResultModel(winnerId, GameResult.Single, GameResult.LostSingle, 4), result);
    }

    private static IBoardModel CreateWonBoard(bool winnerIsWhite, int loserPoint,
        int cubeValue = 1, int loserBearOffCount = 0, int loserBarCount = 0)
    {
        var loserSign = winnerIsWhite ? 1 : -1;
        var fields = new int[24];
        // Keep the remaining loser checkers outside both home boards.
        fields[12] = loserSign * (15 - loserBearOffCount - loserBarCount - 1);
        fields[loserPoint] += loserSign;
        var contract = new BoardModelContract
        {
            Fields = fields,
            BearOffCountWhite = winnerIsWhite ? 15 : loserBearOffCount,
            BearOffCountBlack = winnerIsWhite ? loserBearOffCount : 15,
            HomeBarCountWhite = winnerIsWhite ? 0 : loserBarCount,
            HomeBarCountBlack = winnerIsWhite ? loserBarCount : 0,
            DoublingCubeValue = cubeValue,
        };
        return BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard(contract);
    }
}
