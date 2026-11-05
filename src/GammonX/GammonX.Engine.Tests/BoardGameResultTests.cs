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
