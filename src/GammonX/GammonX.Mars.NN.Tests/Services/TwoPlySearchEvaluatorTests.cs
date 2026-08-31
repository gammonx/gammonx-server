using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Mars.NN.Tests.Services
{
    public class TwoPlySearchEvaluatorTests
    {
        [Fact]
        public async Task UsesBackgammonDiceProbabilities()
        {
            var harness = CreateHarness(
                rolls =>
                {
                    var marker = rolls.Length == 4 ? 100 : 0;
                    return [CreateSequence(marker)];
                });
            var evaluator = CreateEvaluator(harness, (board, _) => Task.FromResult((double)board.Fields[0]));

            var score = await evaluator.EvaluateAsync(harness.Board.Object, true);

            Assert.Equal(-600d / 36d, score, 10);
            Assert.Equal(21, harness.RequestedRolls.Count);
            Assert.Equal(36, harness.RequestedRolls.Sum(rolls => rolls.Length == 4 ? 1 : 2));
        }

        [Fact]
        public async Task SelectsTheBestOpponentResponseForEachRollBeforeAveraging()
        {
            var harness = CreateHarness(
                rolls =>
                {
                    var rollKey = rolls.Length == 4
                        ? rolls[0] * 10 + rolls[0]
                        : rolls[0] * 10 + rolls[1];
                    return [CreateSequence(rollKey), CreateSequence(rollKey + 100)];
                });
            var evaluator = CreateEvaluator(harness, (board, _) => Task.FromResult((double)board.Fields[0]));

            var score = await evaluator.EvaluateAsync(harness.Board.Object, true);

            var expected = 0d;
            for (var die1 = 1; die1 <= 6; die1++)
            {
                for (var die2 = die1; die2 <= 6; die2++)
                {
                    var rollKey = die1 * 10 + die2;
                    var rollWeight = die1 == die2 ? 1d : 2d;
                    expected -= rollWeight * (rollKey + 100) / 36d;
                }
            }

            Assert.Equal(expected, score, 10);
        }

        [Fact]
        public async Task StartsOpponentLeafEvaluationsBeforeAwaitingThem()
        {
            var harness = CreateHarness(_ => [CreateSequence(1), CreateSequence(2)]);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var scoreCallCount = 0;

            async Task<double> ScoreAsync()
            {
                var callIndex = Interlocked.Increment(ref scoreCallCount);
                if (callIndex <= 2)
                {
                    if (callIndex == 2)
                        release.TrySetResult(true);

                    await release.Task.WaitAsync(TimeSpan.FromSeconds(1));
                }

                return 0d;
            }

            var evaluator = CreateEvaluator(harness, (_, _) => ScoreAsync());

            var score = await evaluator.EvaluateAsync(harness.Board.Object, true);

            Assert.Equal(0d, score);
            Assert.Equal(42, scoreCallCount);
            Assert.Equal(0, harness.Fields[0]);
        }

        [Fact]
        public async Task MaximizesNegativeOpponentScoreBeforeNegatingIt()
        {
            var harness = CreateHarness(_ => [CreateSequence(-8), CreateSequence(-2)]);
            var evaluator = CreateEvaluator(harness, (board, _) => Task.FromResult((double)board.Fields[0]));

            var score = await evaluator.EvaluateAsync(harness.Board.Object, true);

            Assert.Equal(2d, score, 10);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task PassesWhenOpponentHasNoLegalResponseAndRestoresTheBoard(
            bool rootIsWhite,
            bool expectedOpponentIsWhite)
        {
            var harness = CreateHarness(_ => [] , initialMarker: 7);
            var perspectives = new List<bool>();
            var evaluator = CreateEvaluator(
                harness,
                (board, isWhite) =>
                {
                    perspectives.Add(isWhite);
                    return Task.FromResult((double)board.Fields[0]);
                });

            var score = await evaluator.EvaluateAsync(harness.Board.Object, rootIsWhite);

            Assert.Equal(-7d, score, 10);
            Assert.Equal(21, perspectives.Count);
            Assert.All(perspectives, perspective => Assert.Equal(expectedOpponentIsWhite, perspective));
            Assert.Equal(7, harness.Fields[0]);
            Assert.Empty(harness.UndoneMoves);
        }

        [Fact]
        public async Task UndoesOpponentMovesInReverseOrder()
        {
            var harness = CreateHarness(_ => [CreateSequence(1, 2)]);
            var evaluator = CreateEvaluator(harness, (board, _) => Task.FromResult((double)board.Fields[0]));

            await evaluator.EvaluateAsync(harness.Board.Object, true);

            Assert.Equal(0, harness.Fields[0]);
            Assert.Equal(42, harness.UndoneMoves.Count);
            Assert.Equal((1, 2), (harness.UndoneMoves[0].From, harness.UndoneMoves[0].To));
            Assert.Equal((0, 1), (harness.UndoneMoves[1].From, harness.UndoneMoves[1].To));
        }

        [Fact]
        public async Task RestoresAHitDuringRealBackgammonTraversal()
        {
            var fields = new int[24];
            fields[0] = -14;
            fields[22] = -1;
            fields[23] = 15;

            await AssertRealBackgammonTraversalRestoresBoard(fields);
        }

        [Fact]
        public async Task RestoresAHomeBarEntryDuringRealBackgammonTraversal()
        {
            var fields = new int[24];
            fields[0] = -4;
            fields[18] = -2;
            fields[19] = -2;
            fields[20] = -2;
            fields[21] = -2;
            fields[22] = -2;
            fields[23] = -1;
            fields[10] = 14;

            await AssertRealBackgammonTraversalRestoresBoard(fields, homeBarCountBlack: 1);
        }

        [Fact]
        public async Task RestoresABearOffDuringRealBackgammonTraversal()
        {
            var fields = new int[24];
            fields[5] = 15;
            fields[23] = -15;

            await AssertRealBackgammonTraversalRestoresBoard(fields);
        }

        private static TwoPlySearchEvaluator CreateEvaluator(
            Harness harness,
            Func<IBoardModel, bool, Task<double>> scorePosition)
        {
            return new TwoPlySearchEvaluator(
                harness.BoardService.Object,
                scorePosition,
                _ => harness.Board.Object);
        }

        private static Harness CreateHarness(
            Func<int[], MoveSequenceModel[]> getResponses,
            int initialMarker = 0)
        {
            var fields = new int[24];
            fields[0] = initialMarker;
            var board = new Mock<IBoardModel>();
            board.SetupGet(model => model.Fields).Returns(fields);

            var boardService = new Mock<IBoardService>();
            var requestedRolls = new List<int[]>();
            var undoStack = new Stack<int>();
            var undoneMoves = new List<MoveModel>();

            boardService
                .Setup(service => service.GetUniqueLegalMoveSequences(
                    board.Object,
                    It.IsAny<bool>(),
                    It.IsAny<int[]>()))
                .Returns((IBoardModel _, bool _, int[] rolls) =>
                {
                    requestedRolls.Add(rolls.ToArray());
                    return getResponses(rolls);
                });

            boardService
                .Setup(service => service.MoveCheckerTo(
                    board.Object,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>()))
                .Callback<IBoardModel, int, int, bool>((_, _, to, _) =>
                {
                    undoStack.Push(fields[0]);
                    fields[0] = to;
                });

            boardService
                .Setup(service => service.UndoMove(
                    board.Object,
                    It.IsAny<MoveModel>(),
                    It.IsAny<bool>()))
                .Callback<IBoardModel, MoveModel, bool>((_, move, _) =>
                {
                    undoneMoves.Add(move);
                    fields[0] = undoStack.Pop();
                });


            return new Harness(board, boardService, fields, requestedRolls, undoneMoves);
        }

        private static MoveSequenceModel CreateSequence(params int[] markers)
        {
            var sequence = new MoveSequenceModel();
            for (var index = 0; index < markers.Length; index++)
            {
                var from = index == 0 ? 0 : markers[index - 1];
                sequence.Moves.Add(new MoveModel(from, markers[index]));
            }

            return sequence;
        }

        private static async Task AssertRealBackgammonTraversalRestoresBoard(
            int[] fields,
            int homeBarCountWhite = 0,
            int homeBarCountBlack = 0)
        {
            var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
            var board = boardService.CreateBoard(new BoardModelContract
            {
                Fields = fields,
                HomeBarCountWhite = homeBarCountWhite,
                HomeBarCountBlack = homeBarCountBlack
            });
            boardService.AddRollEventToHistory(board, true, [6, 6]);

            var expectedFields = board.Fields.ToArray();
            var expectedHistory = board.History.Events.ToArray();
            var expectedBearOffWhite = board.BearOffCountWhite;
            var expectedBearOffBlack = board.BearOffCountBlack;

            var evaluator = new TwoPlySearchEvaluator(boardService, (_, _) => Task.FromResult(0d));
            await evaluator.EvaluateAsync(board, true);

            Assert.Equal(expectedFields, board.Fields);
            Assert.Equal(expectedBearOffWhite, board.BearOffCountWhite);
            Assert.Equal(expectedBearOffBlack, board.BearOffCountBlack);
            Assert.Equal(homeBarCountWhite, ((IHomeBarModel)board).HomeBarCountWhite);
            Assert.Equal(homeBarCountBlack, ((IHomeBarModel)board).HomeBarCountBlack);
            Assert.Equal(expectedHistory, board.History.Events.ToArray());
        }

        private sealed record Harness(
            Mock<IBoardModel> Board,
            Mock<IBoardService> BoardService,
            int[] Fields,
            List<int[]> RequestedRolls,
            List<MoveModel> UndoneMoves);
    }
}