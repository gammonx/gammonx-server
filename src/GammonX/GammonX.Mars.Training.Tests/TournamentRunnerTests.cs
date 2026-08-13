using GammonX.Mars.Training;

namespace GammonX.Mars.Training.Tests;

public sealed class TournamentRunnerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AssignBotPlayersMapsModelAndWildBgToRequestedColor(bool modelIsWhite)
    {
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var assignment = TournamentRunner.AssignBotPlayers(player1Id, player2Id, modelIsWhite);

        Assert.Equal(modelIsWhite ? player1Id : player2Id, assignment.ModelPlayerId);
        Assert.Equal(modelIsWhite ? player2Id : player1Id, assignment.WildBgPlayerId);
    }
}