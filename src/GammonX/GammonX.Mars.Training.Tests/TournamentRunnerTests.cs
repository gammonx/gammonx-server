using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class TournamentRunnerTests
{
    [Fact]
    public async Task ParallelColorAssignmentsRemainIterationLocal()
    {
        var modelA = new TournamentEntry("model-a.dat", BotLevel.Hard, null, null);
        var modelB = new TournamentEntry("model-b.dat", BotLevel.Hard, null, null);
        var assignments = new (bool ModelAIsWhite, bool ModelBIsWhite)[1_000];

        await Parallel.ForAsync(0, assignments.Length, (index, _) =>
        {
            var assigned = TournamentRunner.AssignColors(modelA, modelB, index % 2 == 0);
            assignments[index] = (
                assigned.ModelA.IsWhite!.Value,
                assigned.ModelB!.IsWhite!.Value);
            return ValueTask.CompletedTask;
        });

        Assert.Null(modelA.IsWhite);
        Assert.Null(modelB.IsWhite);
        for (var index = 0; index < assignments.Length; index++)
        {
            Assert.Equal(index % 2 == 0, assignments[index].ModelAIsWhite);
            Assert.Equal(index % 2 != 0, assignments[index].ModelBIsWhite);
        }
    }

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