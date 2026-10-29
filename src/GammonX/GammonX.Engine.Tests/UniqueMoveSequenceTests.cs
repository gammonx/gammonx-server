using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Engine.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Engine.Tests;

public class UniqueMoveSequenceTests
{
    [Theory]
    [InlineData(GameModus.Backgammon, true)]
    [InlineData(GameModus.Backgammon, false)]
    [InlineData(GameModus.Portes, true)]
    [InlineData(GameModus.Portes, false)]
    [InlineData(GameModus.Tavla, true)]
    [InlineData(GameModus.Tavla, false)]
    [InlineData(GameModus.Plakoto, true)]
    [InlineData(GameModus.Plakoto, false)]
    [InlineData(GameModus.Fevga, true)]
    [InlineData(GameModus.Fevga, false)]
    public void MatchesPostTraversalDeduplicationForEveryRoll(GameModus modus, bool isWhite)
    {
        var service = BoardServiceFactory.Create(modus);
        var board = service.CreateBoard();

        CompareEveryRoll(service, board, isWhite);

        var random = new Random(2710);
        for (var turn = 0; turn < 10; turn++)
        {
            var playerIsWhite = turn % 2 == 0;
            var die1 = random.Next(1, 7);
            var die2 = random.Next(1, 7);
            var rolls = die1 == die2 ? new[] { die1, die1, die1, die1 } : [die1, die2];
            service.AddRollEventToHistory(board, playerIsWhite, rolls);
            var sequences = service.GetLegalMoveSequences(board, playerIsWhite, rolls);
            if (sequences.Length == 0)
                continue;

            var sequence = sequences[random.Next(sequences.Length)];
            foreach (var move in sequence.Moves)
                service.MoveCheckerTo(board, move.From, move.To, playerIsWhite);
        }

        CompareEveryRoll(service, board, isWhite);
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void PreservesFullDiceAndHigherDieFiltering(GameModus modus)
    {
        var service = BoardServiceFactory.Create(modus);
        var bothDice = service.CreateBoard(new BoardModelContract
        {
            Fields = BoardMocks.BothDicesMustBeUsedBoardStandardWhite,
            PinnedFields = new int[24]
        });
        AssertEquivalent(service, bothDice, true, [4, 5]);
        Assert.All(service.GetUniqueLegalMoveSequences(bothDice, true, 4, 5),
            sequence => Assert.Equal(2, sequence.UsedDices.Count));

        var higherDie = service.CreateBoard(new BoardModelContract
        {
            Fields = BoardMocks.HigherRollMustBeUsedBoardStandardWhite,
            PinnedFields = new int[24]
        });
        AssertEquivalent(service, higherDie, true, [3, 5]);
        var sequence = Assert.Single(service.GetUniqueLegalMoveSequences(higherDie, true, 3, 5));
        Assert.Equal([5], sequence.UsedDices);
    }

    [Fact]
    public void PreservesHitHomeBarBlockedAndBearOffPositions()
    {
        var service = BoardServiceFactory.Create(GameModus.Backgammon);
        var contact = new int[24];
        contact[6] = -2;
        contact[8] = 1;
        contact[12] = 2;
        CompareEveryRoll(service, service.CreateBoard(new BoardModelContract
        {
            Fields = contact,
            BearOffCountWhite = 12,
            BearOffCountBlack = 12,
            HomeBarCountWhite = 1
        }), true);

        var blocked = new int[24];
        Array.Fill(blocked, 2, 0, 6);
        AssertEquivalent(service, service.CreateBoard(new BoardModelContract
        {
            Fields = blocked,
            HomeBarCountWhite = 1
        }), true, [1, 2]);

        var bearOff = new int[24];
        bearOff[22] = -1;
        bearOff[23] = -1;
        CompareEveryRoll(service, service.CreateBoard(new BoardModelContract
        {
            Fields = bearOff,
            BearOffCountWhite = 13,
            BearOffCountBlack = 15
        }), true);
    }

    [Fact]
    public void PreservesPlakotoPinsAndRestoresHistory()
    {
        var service = BoardServiceFactory.Create(GameModus.Plakoto);
        var fields = new int[24];
        fields[6] = -2;
        fields[8] = 1;
        fields[12] = 2;
        var pinned = new int[24];
        pinned[12] = -1;
        var board = service.CreateBoard(new BoardModelContract
        {
            Fields = fields,
            PinnedFields = pinned,
            BearOffCountWhite = 12,
            BearOffCountBlack = 12
        });
        service.AddRollEventToHistory(board, true, [2, 3]);

        CompareEveryRoll(service, board, true);
        CompareEveryRoll(service, board, false);
    }

    private static void CompareEveryRoll(IBoardService service, IBoardModel board, bool isWhite)
    {
        for (var die1 = 1; die1 <= 6; die1++)
        {
            for (var die2 = die1; die2 <= 6; die2++)
            {
                int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];
                AssertEquivalent(service, board, isWhite, rolls);
            }
        }
    }

    private static void AssertEquivalent(IBoardService service, IBoardModel board, bool isWhite, int[] rolls)
    {
        var original = board.DeepClone();
        var history = board.History.Events.ToArray();
        var expected = ReferenceSequences(service, board, isWhite, rolls);

        var actual = service.GetUniqueLegalMoveSequences(board, isWhite, rolls);

        Assert.True(board.Equals(original), "Unique generation changed the input position.");
        Assert.Equal(history, board.History.Events.ToArray());
        Assert.Equal(expected.Length, actual.Length);
        var endStates = new HashSet<IBoardModel>();
        for (var index = 0; index < actual.Length; index++)
        {
            Assert.True(expected[index].Equals(actual[index]), "The first representative or its order changed.");
            Assert.Equal(expected[index].UsedDices, actual[index].UsedDices);
            var endBoard = board.DeepClone();
            foreach (var move in actual[index].Moves)
                service.MoveCheckerTo(endBoard, move.From, move.To, isWhite);
            Assert.True(endStates.Add(endBoard), "Duplicate end position returned.");
        }
    }

    private static MoveSequenceModel[] ReferenceSequences(
        IBoardService service, IBoardModel board, bool isWhite, int[] rolls)
    {
        // Independent copy-per-branch traversal: retain every terminal position, then deduplicate and filter.
        var terminal = new List<(IBoardModel Board, MoveSequenceModel Sequence)>();
        Explore(board.DeepClone(), rolls.Order().ToArray(), new MoveSequenceModel());
        var seen = new HashSet<IBoardModel>();
        var unique = terminal.Where(result => seen.Add(result.Board)).Select(result => result.Sequence).ToArray();
        var full = unique.Where(sequence => sequence.UsedDices.Count == rolls.Length).ToArray();
        if (full.Length > 0)
            return full;
        var higher = unique.Where(sequence => sequence.UsedDices.Contains(rolls.Max())).ToArray();
        return higher.Length > 0 ? higher : unique;

        void Explore(IBoardModel position, int[] remaining, MoveSequenceModel current)
        {
            var canMove = false;
            for (var dieIndex = 0; dieIndex < remaining.Length; dieIndex++)
            {
                var die = remaining[dieIndex];
                if (dieIndex > 0 && die == remaining[dieIndex - 1])
                    continue;

                var fromFields = new List<int>();
                if (position is IHomeBarModel homeBar &&
                    (isWhite ? homeBar.HomeBarCountWhite : homeBar.HomeBarCountBlack) > 0)
                {
                    fromFields.Add(isWhite ? homeBar.StartIndexWhite : homeBar.StartIndexBlack);
                    if (homeBar.MustEnterFromHomebar)
                        AddMoves();
                    else
                    {
                        AddBoardFields();
                        AddMoves();
                    }
                }
                else
                {
                    AddBoardFields();
                    AddMoves();
                }

                void AddBoardFields()
                {
                    for (var field = 0; field < position.Fields.Length; field++)
                    {
                        if (isWhite ? position.Fields[field] < 0 : position.Fields[field] > 0)
                            fromFields.Add(field);
                    }
                }

                void AddMoves()
                {
                    foreach (var from in fromFields)
                    {
                        var bearOff = service.CanBearOffChecker(position, from, die, isWhite);
                        if (!bearOff && !service.CanMoveChecker(position, from, die, isWhite))
                            continue;

                        canMove = true;
                        var to = bearOff
                            ? (isWhite ? BoardPositions.BearOffWhite : BoardPositions.BearOffBlack)
                            : position.MoveOperator(isWhite, from, die);
                        var child = position.DeepClone();
                        service.MoveCheckerTo(child, from, to, isWhite);
                        var sequence = current.DeepClone();
                        sequence.Moves.Add(new MoveModel(from, to));
                        sequence.UsedDices.Add(die);
                        Explore(child, remaining.Where((_, index) => index != dieIndex).ToArray(), sequence);
                    }
                }
            }

            if (!canMove && current.Moves.Count > 0)
                terminal.Add((position, current));
        }
    }
}
