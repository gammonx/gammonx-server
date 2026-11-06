using GammonX.Engine.Extensions;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public class FeatureEvalOptionsTests
{
    public static IEnumerable<object[]> Variants() =>
        new[] { GameModus.Backgammon, GameModus.Portes, GameModus.Tavla, GameModus.Fevga, GameModus.Plakoto }
            .SelectMany(modus => new[] { true, false }.Select(isWhite => new object[] { modus, isWhite }));

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task MoveRequestGeneratesCandidatesWithoutAdditionalOptions(GameModus modus, bool isWhite)
    {
        var boards = BoardServiceFactory.Create(modus);
        var board = boards.CreateBoard();
        var legalMoves = boards.GetUniqueLegalMoveSequences(board, isWhite, [2, 3]);
        Assert.NotEmpty(legalMoves);
        var options = new FeatureEvalOptions
        {
            EvalMoveRequest = new EvalMoveRequestContract
            {
                TurnNumber = 37,
                Board = board.ToContract(false),
                Modus = modus,
                IsWhite = isWhite,
                Rolls = [2, 3],
                BotLevel = BotLevel.Hard,
            },
            TurnNumber = 37,
            ContactWeights = EvalWeights.GetContactWeights(modus),
            MaxCandidates = 1,
        };
        var service = FeatureEvalServiceFactory.Create(modus, null!);

        var results = await service.EvalMoveSequencesForTrainingAsync(options);
        var selected = await service.EvalMoveSequencesAsync(options);

        Assert.Single(results);
        Assert.All(results, result =>
        {
            Assert.Equal(legalMoves[0], result.MoveSequence);
            Assert.Equal(37d / 137d, result.EvalResult.TurnNumber, 10);
        });
        Assert.Equal(results[0].MoveSequence, selected);
        Assert.Null(options.Candidates);
        Assert.Null(options.BotLevel);
        Assert.Null(options.IsWhite);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task SingleCandidateIsEvaluatedWithoutCandidateList(GameModus modus, bool isWhite)
    {
        var boards = BoardServiceFactory.Create(modus);
        var board = boards.CreateBoard();
        var candidate = boards.GetUniqueLegalMoveSequences(board, isWhite, [2, 3])[0];
        var options = new FeatureEvalOptions
        {
            BoardContract = board.ToContract(false),
            IsWhite = isWhite,
            Candidate = candidate,
            BotLevel = BotLevel.Hard,
            TurnNumber = 42,
            ContactWeights = EvalWeights.GetContactWeights(modus),
        };
        var service = FeatureEvalServiceFactory.Create(modus, null!);

        var result = await service.EvalMoveSequenceAsync(options);

        Assert.Same(candidate, result.MoveSequence);
        Assert.Equal(42d / 142d, result.EvalResult.TurnNumber, 10);
        Assert.Null(options.Candidates);
        Assert.Equal(42d / 142d, service.EvalPositionForTraining(options).TurnNumber, 10);
    }
}
