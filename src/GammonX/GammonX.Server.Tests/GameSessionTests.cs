using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models;
using GammonX.Models.Enums;
using GammonX.Models.Helpers;

using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Testdata;
using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests
{
    public class GameSessionTests
    {
        private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();
        private static readonly GameSessionFactory _gameSessionFactory = new(_diceServiceFactory);

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void DirectHintsMatchPlayablePrefixesAcrossRollsAndUndo(GameModus modus)
        {
            foreach (var white in new[] { true, false })
            {
                foreach (var dice in new[] { new[] { 1, 2 }, new[] { 1, 1 } })
                {
                    // Arrange
                    var game = _gameSessionFactory.Create(Guid.NewGuid(), modus);
                    game.StartGame(Guid.NewGuid(), Guid.NewGuid());
                    game.SetDiceRolls(dice, white);
                    var first = game.GetLegalMoveSequences(white).First().Moves[0];

                    // Act and assert at each state visible to the client.
                    CheckHints(game, white);
                    game.MoveCheckers(game.ActivePlayer, first.From, first.To, white);
                    CheckHints(game, white);
                    game.UndoLastMove(game.ActivePlayer, white);
                    CheckHints(game, white);
                }
            }
        }

        [Fact]
        public void DirectHintsIncludeBarEntryAndOversizedBearOff()
        {
            // Arrange
            var game = _gameSessionFactory.Create(Guid.NewGuid(), GameModus.Backgammon);
            game.StartGame(Guid.NewGuid(), Guid.NewGuid());
            Array.Clear(game.BoardModel.Fields);
            game.BoardModel.Fields[23] = -14;
            game.BoardModel.Fields[2] = 2;
            game.BoardModel.Fields[20] = 13;
            var bar = (IHomeBarModel)game.BoardModel;
            bar.AddToHomeBar(true, 1);
            game.SetDiceRolls([1, 2], true);

            // Act
            var entries = game.ResolveMoveSequences(true).Select(sequence => Assert.Single(sequence.Moves)).ToArray();
            bar.RemoveFromHomeBar(true, 1);
            game.BoardModel.Fields[23] = -15;
            var bearOff = Assert.Single(Assert.Single(game.ResolveMoveSequences(true)).Moves);

            // Assert
            Assert.NotEmpty(entries);
            Assert.All(entries, move => Assert.Equal(BoardPositions.HomeBarWhite, move.From));
            Assert.DoesNotContain(entries, move => move.To == 2);
            Assert.Equal((23, BoardPositions.BearOffWhite), (bearOff.From, bearOff.To));
        }

        private static void CheckHints(IGameSessionModel game, bool white)
        {
            var expected = new HashSet<(int From, int To)>();
            foreach (var sequence in game.GetLegalMoveSequences(white).Where(sequence => sequence.Moves.Count > 0))
            {
                var from = sequence.Moves[0].From;
                var position = from;
                foreach (var step in sequence.Moves)
                {
                    if (step.From != position)
                    {
                        break;
                    }
                    expected.Add((from, step.To));
                    position = step.To;
                }
            }
            var actual = game.ResolveMoveSequences(white).Select(sequence => Assert.Single(sequence.Moves))
                .Select(move => (move.From, move.To)).ToArray();
            Assert.Equal(expected.Count, actual.Length);
            Assert.True(expected.SetEquals(actual));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CombinedMoveRequiresLegalIntermediateStepsAndUndoesEachHit(bool blocked)
        {
            // Arrange
            var game = _gameSessionFactory.Create(Guid.NewGuid(), GameModus.Backgammon);
            var player = Guid.NewGuid();
            game.StartGame(player, Guid.NewGuid());
            var fields = new int[24];
            fields[0] = -1;
            fields[1] = blocked ? 2 : 1;
            fields[2] = 2;
            game.BoardModel.SetFields(fields);
            game.SetDiceRolls([1, 2], true);
            var historyCount = game.BoardModel.History.Events.Count;

            // Act and assert
            if (blocked)
            {
                Assert.Throws<InvalidOperationException>(() => game.MoveCheckers(player, 0, 3, true));
                Assert.False(game.CanMove(true));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => game.MoveCheckers(player, 1, 3, true));
                game.MoveCheckers(player, 0, 3, true);
                Assert.All(game.DiceRolls, die => Assert.True(die.Used));
                Assert.Equal(1, ((IHomeBarModel)game.BoardModel).HomeBarCountBlack);

                game.UndoLastMove(player, true);
                Assert.Equal(-1, game.BoardModel.Fields[1]);
                Assert.Equal(new[] { 2 }, game.DiceRolls.GetRemainingRolls());
                game.UndoLastMove(player, true);
            }

            Assert.Equal(fields, game.BoardModel.Fields);
            Assert.Equal(historyCount, game.BoardModel.History.Events.Count);
            Assert.Equal(new[] { 1, 2 }, game.DiceRolls.GetRemainingRolls());
            Assert.False(game.CanUndoLastMove(player));
        }

        [Fact]
        public void HumanMustUseHigherDieWhenOnlyOneCanBePlayed()
        {
            // Arrange
            var game = _gameSessionFactory.Create(Guid.NewGuid(), GameModus.Backgammon);
            var player = Guid.NewGuid();
            game.StartGame(player, Guid.NewGuid());
            var fields = new int[24];
            fields[0] = -1;
            fields[3] = 2;
            game.BoardModel.SetFields(fields);
            game.SetDiceRolls([1, 2], true);

            // Act
            var hint = Assert.Single(Assert.Single(game.ResolveMoveSequences(true)).Moves);
            Assert.Equal((0, 2), (hint.From, hint.To));
            Assert.Throws<InvalidOperationException>(() => game.MoveCheckers(player, 0, 1, true));
            game.MoveCheckers(player, 0, 2, true);

            // Assert
            Assert.Equal(-1, game.BoardModel.Fields[2]);
            Assert.Equal(new[] { 1 }, game.DiceRolls.GetRemainingRolls());
            Assert.Equal(GamePhase.WaitingForEndTurn, game.Phase);
        }

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
        public void PayloadIncludesLegalMovesOnlyForActivePlayerAndFollowsTurnChanges(GameModus modus, bool isWhite)
        {
            // Arrange
            var game = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var active = Guid.NewGuid();
            var waiting = Guid.NewGuid();
            game.StartGame(active, waiting);
            game.SetDiceRolls([3, 3], isWhite);
            var sequenceCount = game.GetLegalMoveSequences(isWhite).Count;

            // Act
            var activePayload = game.ToPayload(active, [], !isWhite);
            var waitingPayload = game.ToPayload(waiting, [], isWhite);

            // Assert
            Assert.True(sequenceCount > 0);
            Assert.Equal(game.ResolveMoveSequences(isWhite).Count, activePayload.MoveSequences.Length);
            Assert.All(activePayload.MoveSequences, sequence => Assert.Single(sequence.Moves));
            Assert.Empty(waitingPayload.MoveSequences);
            Assert.Equal(GamePhase.WaitingForOpponent, waitingPayload.Phase);
            Assert.Equal(game.Phase, activePayload.Phase);
            Assert.Equal(active, waitingPayload.ActiveTurn);
            Assert.Equal(activePayload.TurnNumber, waitingPayload.TurnNumber);
            Assert.Equal(activePayload.DiceRolls.Select(dice => dice.Roll), waitingPayload.DiceRolls.Select(dice => dice.Roll));
            Assert.NotEmpty(waitingPayload.BoardState.Fields);
            var full = GammonX.Server.Contracts.EventGameStatePayload.ForPlayer(game, active, !isWhite, true, [], fullMoveSequences: true);
            Assert.Equal(sequenceCount, full.MoveSequences.Length);
            Assert.Contains(full.MoveSequences, sequence => sequence.Moves.Count > 1);

            game.NextTurn(waiting);
            game.SetDiceRolls([2, 3], !isWhite);
            Assert.NotEmpty(game.GetLegalMoveSequences(!isWhite));
            Assert.Equal(game.ResolveMoveSequences(!isWhite).Count, game.ToPayload(waiting, [], isWhite).MoveSequences.Length);
            Assert.Empty(game.ToPayload(active, [], !isWhite).MoveSequences);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionClonePreservesUnstartedState(GameModus modus)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);

            var clone = Assert.IsAssignableFrom<IGameSessionModel>(Assert.IsAssignableFrom<ICloneable>(gameSession).Clone());

            Assert.NotSame(gameSession, clone);
            Assert.Equal(gameSession.GetType(), clone.GetType());
            Assert.Equal(gameSession.Id, clone.Id);
            Assert.Equal(gameSession.MatchId, clone.MatchId);
            Assert.Equal(modus, clone.Modus);
            Assert.Equal(gameSession.Phase, clone.Phase);
            Assert.Equal(gameSession.ActivePlayer, clone.ActivePlayer);
            Assert.Equal(gameSession.OtherPlayer, clone.OtherPlayer);
            Assert.Equal(gameSession.TurnNumber, clone.TurnNumber);
            Assert.Equal(gameSession.Result, clone.Result);
            Assert.Null(clone.StartedAt);
            Assert.Null(clone.EndedAt);
            Assert.Equal(0, clone.Duration);
            Assert.Empty(clone.DiceRolls);
            Assert.Empty(clone.GetLegalMoveSequences(true));
            Assert.NotSame(gameSession.BoardModel, clone.BoardModel);
            Assert.Equal(gameSession.BoardModel.Fields, clone.BoardModel.Fields);
            Assert.NotSame(gameSession.BoardModel.Fields, clone.BoardModel.Fields);
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        [InlineData(GameModus.Fevga, false)]
        public void GameSessionCloneCopiesDiceAndLegalMovesIndependently(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var playerId = Guid.NewGuid();
            gameSession.StartGame(playerId, Guid.NewGuid());
            gameSession.SetDiceRolls([2, 3], isWhite);

            var clone = Assert.IsAssignableFrom<IGameSessionModel>(Assert.IsAssignableFrom<ICloneable>(gameSession).Clone());

            Assert.Equal(gameSession.Phase, clone.Phase);
            Assert.Equal(gameSession.StartedAt, clone.StartedAt);
            Assert.Equal(gameSession.ActivePlayer, clone.ActivePlayer);
            Assert.Equal(gameSession.OtherPlayer, clone.OtherPlayer);
            Assert.NotSame(gameSession.DiceRolls, clone.DiceRolls);
            Assert.Equal(gameSession.DiceRolls.Count, clone.DiceRolls.Count);
            for (var diceIndex = 0; diceIndex < gameSession.DiceRolls.Count; diceIndex++)
            {
                Assert.NotSame(gameSession.DiceRolls[diceIndex], clone.DiceRolls[diceIndex]);
                Assert.Equal(gameSession.DiceRolls[diceIndex].Roll, clone.DiceRolls[diceIndex].Roll);
                Assert.Equal(gameSession.DiceRolls[diceIndex].Used, clone.DiceRolls[diceIndex].Used);
            }

            var clonedMoves = clone.GetLegalMoveSequences(isWhite);
            var originalMoves = gameSession.GetLegalMoveSequences(isWhite);
            Assert.NotEmpty(clonedMoves);
            Assert.NotSame(originalMoves, clonedMoves);
            Assert.Equal(originalMoves.Count, clonedMoves.Count);
            for (var sequenceIndex = 0; sequenceIndex < originalMoves.Count; sequenceIndex++)
            {
                var originalSequence = originalMoves[sequenceIndex];
                var clonedSequence = clonedMoves[sequenceIndex];
                Assert.NotSame(originalSequence, clonedSequence);
                Assert.NotSame(originalSequence.Moves, clonedSequence.Moves);
                Assert.NotSame(originalSequence.UsedDices, clonedSequence.UsedDices);
                Assert.Equal(originalSequence.Moves, clonedSequence.Moves);
                Assert.Equal(originalSequence.UsedDices, clonedSequence.UsedDices);
                for (var moveIndex = 0; moveIndex < originalSequence.Moves.Count; moveIndex++)
                {
                    Assert.NotSame(originalSequence.Moves[moveIndex], clonedSequence.Moves[moveIndex]);
                }
            }

            clone.DiceRolls[0].Use();
            clonedMoves[0].Moves[0].From = 99;
            clonedMoves[0].UsedDices.Clear();
            Assert.All(gameSession.DiceRolls, dice => Assert.False(dice.Used));
            Assert.NotEqual(99, originalMoves[0].Moves[0].From);
            Assert.NotEmpty(originalMoves[0].UsedDices);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionCloneCanUndoMovesWithoutChangingOriginal(GameModus modus)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var playerId = Guid.NewGuid();
            gameSession.StartGame(playerId, Guid.NewGuid());
            gameSession.SetDiceRolls([2, 3], true);
            var initialFields = gameSession.BoardModel.Fields.ToArray();
            var moves = gameSession.GetLegalMoveSequences(true).First().Moves.ToArray();
            Assert.Equal(2, moves.Length);
            foreach (var move in moves)
            {
                gameSession.MoveCheckers(playerId, move.From, move.To, true);
            }
            var movedFields = gameSession.BoardModel.Fields.ToArray();
            var originalHistory = gameSession.BoardModel.History.ToString();

            var clone = Assert.IsAssignableFrom<IGameSessionModel>(Assert.IsAssignableFrom<ICloneable>(gameSession).Clone());

            Assert.True(clone.CanUndoLastMove(playerId));
            clone.UndoLastMove(playerId, true);
            Assert.True(clone.CanUndoLastMove(playerId));
            clone.UndoLastMove(playerId, true);
            Assert.False(clone.CanUndoLastMove(playerId));
            Assert.Equal(initialFields, clone.BoardModel.Fields);
            Assert.All(clone.DiceRolls, dice => Assert.False(dice.Used));
            Assert.Single(clone.BoardModel.History.Events);
            Assert.Equal(movedFields, gameSession.BoardModel.Fields);
            Assert.All(gameSession.DiceRolls, dice => Assert.True(dice.Used));
            Assert.Equal(originalHistory, gameSession.BoardModel.History.ToString());
            Assert.True(gameSession.CanUndoLastMove(playerId));
            gameSession.UndoLastMove(playerId, true);
            Assert.True(gameSession.CanUndoLastMove(playerId));
            gameSession.UndoLastMove(playerId, true);
            Assert.Equal(initialFields, gameSession.BoardModel.Fields);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionClonePreservesCompletedStateAndHistory(GameModus modus)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.SetDiceRolls([2, 3], true);
            gameSession.NextTurn(player2Id);
            var result = new GameResultModel(player2Id, GameResult.Resign, GameResult.LostResign, 2);
            gameSession.StopGame(result);

            var clone = Assert.IsAssignableFrom<IGameSessionModel>(Assert.IsAssignableFrom<ICloneable>(gameSession).Clone());

            Assert.Equal(gameSession.Id, clone.Id);
            Assert.Equal(GamePhase.GameOver, clone.Phase);
            Assert.Equal(gameSession.TurnNumber, clone.TurnNumber);
            Assert.Equal(player2Id, clone.ActivePlayer);
            Assert.Equal(player1Id, clone.OtherPlayer);
            Assert.Equal(result, clone.Result);
            Assert.Equal(gameSession.StartedAt, clone.StartedAt);
            Assert.Equal(gameSession.EndedAt, clone.EndedAt);
            Assert.Equal(gameSession.Duration, clone.Duration);
            Assert.NotSame(gameSession.BoardModel.History, clone.BoardModel.History);
            Assert.Equal(gameSession.BoardModel.History.Events.Count, clone.BoardModel.History.Events.Count);
            Assert.Equal(gameSession.BoardModel.History.ToString(), clone.BoardModel.History.ToString());
            clone.BoardModel.Fields[0] = 99;
            Assert.NotEqual(99, gameSession.BoardModel.Fields[0]);
            clone.StopGame(GameResultModel.Draw());
            Assert.Equal(result, gameSession.Result);
        }

        [Fact]
        public void PlakotoGameSessionClonePreservesPinnedMotherGameOver()
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), GameModus.Plakoto);
            var pinModel = Assert.IsAssignableFrom<IPinModel>(gameSession.BoardModel);
            pinModel.PinnedFields[0] = -1;
            pinModel.PinnedFields[23] = 1;
            Assert.True(gameSession.GameOver(true));
            Assert.True(gameSession.GameOver(false));

            var clone = Assert.IsAssignableFrom<IGameSessionModel>(Assert.IsAssignableFrom<ICloneable>(gameSession).Clone());

            Assert.True(clone.GameOver(true));
            Assert.True(clone.GameOver(false));
            var clonedPinModel = Assert.IsAssignableFrom<IPinModel>(clone.BoardModel);
            Assert.NotSame(pinModel.PinnedFields, clonedPinModel.PinnedFields);
            clonedPinModel.PinnedFields[0] = 0;
            Assert.Equal(-1, pinModel.PinnedFields[0]);
            Assert.False(clone.GameOver(true));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionCreateTest(GameModus modus)
        {
            var matchId = Guid.NewGuid();
            var gameSession = _gameSessionFactory.Create(matchId, modus);
            Assert.NotNull(gameSession);
            Assert.Equal(matchId, gameSession.MatchId);
            Assert.NotEqual(matchId, gameSession.Id);
            Assert.Equal(1, gameSession.TurnNumber);
            Assert.Equal(GamePhase.NotStarted, gameSession.Phase);
            Assert.Equal(modus, gameSession.Modus);
            Assert.NotNull(gameSession.BoardModel);
            Assert.NotEmpty(gameSession.BoardModel.Fields);
            Assert.Empty(gameSession.DiceRolls);
            Assert.Empty(gameSession.GetLegalMoveSequences(true));
            Assert.Null(gameSession.StartedAt);
            Assert.Null(gameSession.EndedAt);
            Assert.Equal(0, gameSession.Duration);
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        public void GameSessionActivePlayerCanMoveCheckersSingleDice(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            Assert.Equal(GamePhase.Rolling, gameSession.Phase);
            Assert.NotEmpty(gameSession.DiceRolls);
            Assert.Equal(2, gameSession.DiceRolls.Count);
            Assert.Equal(2, gameSession.DiceRolls[0].Roll);
            Assert.Equal(3, gameSession.DiceRolls[1].Roll);
            Assert.False(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(isWhite));
            var legalMove = gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves[0]).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms.From, ms.To, out var _) == 2);
            Assert.NotNull(legalMove);
            gameSession.MoveCheckers(player1Id, legalMove.From, legalMove.To, isWhite);
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(isWhite));
            Assert.True(gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves).All(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[0].To, out var _) == 3));
            legalMove = gameSession.GetLegalMoveSequences(isWhite).SelectMany(ms => ms.Moves).FirstOrDefault();
            Assert.NotNull(legalMove);
            gameSession.MoveCheckers(player1Id, legalMove.From, legalMove.To, isWhite);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        public void GameSessionActivePlayerCanMoveCheckersTwoDices(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            Assert.Equal(GamePhase.Rolling, gameSession.Phase);
            Assert.NotEmpty(gameSession.DiceRolls);
            Assert.Equal(2, gameSession.DiceRolls.Count);
            Assert.Equal(2, gameSession.DiceRolls[0].Roll);
            Assert.Equal(3, gameSession.DiceRolls[1].Roll);
            Assert.False(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(isWhite));
            var movSeq = gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 5);
            Assert.NotNull(movSeq);
            gameSession.MoveCheckers(player1Id, movSeq[0].From, movSeq[1].To, isWhite);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.True(gameSession.DiceRolls[1].Used);
            Assert.Empty(gameSession.GetLegalMoveSequences(isWhite));
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        public void GameSessionActivePlayerCanMoveCheckersPaschDices(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([1, 1]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            Assert.Equal(GamePhase.Rolling, gameSession.Phase);
            Assert.NotEmpty(gameSession.DiceRolls);
            Assert.Equal(4, gameSession.DiceRolls.Count);
            Assert.Equal(1, gameSession.DiceRolls[0].Roll);
            Assert.Equal(1, gameSession.DiceRolls[1].Roll);
            Assert.Equal(1, gameSession.DiceRolls[2].Roll);
            Assert.Equal(1, gameSession.DiceRolls[3].Roll);
            Assert.False(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.False(gameSession.DiceRolls[2].Used);
            Assert.False(gameSession.DiceRolls[3].Used);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(isWhite));
            // use up 2 dices
            // so we search a move sequence were move 1 and 2 are using two 1 roll dices
            var legalMoves = gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 2);
            Assert.NotNull(legalMoves);
            gameSession.MoveCheckers(player1Id, legalMoves[0].From, legalMoves[1].To, isWhite);
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.True(gameSession.DiceRolls[1].Used);
            Assert.False(gameSession.DiceRolls[2].Used);
            Assert.False(gameSession.DiceRolls[3].Used);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(isWhite));
            // two dices with 1 roll value left
            Assert.True(gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves).All(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 2 || DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[0].To, out var _) == 1));
            legalMoves = gameSession.GetLegalMoveSequences(isWhite).Select(ms => ms.Moves).FirstOrDefault(lm => Math.Abs(lm[0].From - lm[1].To) == 2);
            Assert.NotNull(legalMoves);
            gameSession.MoveCheckers(player1Id, legalMoves[0].From, legalMoves[1].To, isWhite);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        [InlineData(GameModus.Fevga, false)]
        public void GameSessionCannotMoveNotExistingLegalMove(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, true);
            Assert.Throws<InvalidOperationException>(() => gameSession.MoveCheckers(player1Id, 0, 0, isWhite));
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        public void GameSessionActivePlayerCanUndoHistLastMoves(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            var movSeq = gameSession.GetLegalMoveSequences(isWhite).FirstOrDefault();
            Assert.NotNull(movSeq);
            var firstMove = movSeq.Moves.First();
            var lastMove = movSeq.Moves.Last();

            var firstFromCheckAmount = gameSession.BoardModel.Fields[firstMove.From];
            var firstToCheckAmount = gameSession.BoardModel.Fields[firstMove.To];
            var lastFromCheckAmount = gameSession.BoardModel.Fields[lastMove.From];
            var lastToCheckAmount = gameSession.BoardModel.Fields[lastMove.To];

            gameSession.MoveCheckers(player1Id, firstMove.From, firstMove.To, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            gameSession.MoveCheckers(player1Id, lastMove.From, lastMove.To, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);

            // 1x roll + 2x move
            Assert.Equal(3, gameSession.BoardModel.History.Events.Count);

            gameSession.UndoLastMove(player1Id, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            gameSession.UndoLastMove(player1Id, isWhite);
            Assert.False(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);

            // 1x roll + 0x move
            Assert.Single(gameSession.BoardModel.History.Events);

            Assert.Equal(firstFromCheckAmount, gameSession.BoardModel.Fields[firstMove.From]);
            Assert.Equal(firstToCheckAmount, gameSession.BoardModel.Fields[firstMove.To]);
            Assert.Equal(lastFromCheckAmount, gameSession.BoardModel.Fields[lastMove.From]);
            Assert.Equal(lastToCheckAmount, gameSession.BoardModel.Fields[lastMove.To]);
        }

        [Fact]
        public void GameSessionCanUndoElementaryMovesAfterHit()
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), GameModus.Backgammon);
            gameSession.BoardModel.SetFields([
                -1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            ]);
            var homeBarModel = Assert.IsAssignableFrom<IHomeBarModel>(gameSession.BoardModel);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 1]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();

            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, true);
            gameSession.MoveCheckers(player1Id, 0, 2, true);
            gameSession.MoveCheckers(player1Id, 2, 3, true);

            Assert.Equal(0, gameSession.BoardModel.Fields[0]);
            Assert.Equal(0, gameSession.BoardModel.Fields[2]);
            Assert.Equal(-1, gameSession.BoardModel.Fields[3]);
            Assert.Equal(1, homeBarModel.HomeBarCountBlack);

            gameSession.UndoLastMove(player1Id, true);

            Assert.Equal(0, gameSession.BoardModel.Fields[0]);
            Assert.Equal(-1, gameSession.BoardModel.Fields[2]);
            Assert.Equal(0, gameSession.BoardModel.Fields[3]);
            Assert.Equal(1, homeBarModel.HomeBarCountBlack);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.True(gameSession.CanUndoLastMove(player1Id));

            gameSession.UndoLastMove(player1Id, true);

            Assert.Equal(-1, gameSession.BoardModel.Fields[0]);
            Assert.Equal(1, gameSession.BoardModel.Fields[2]);
            Assert.Equal(0, gameSession.BoardModel.Fields[3]);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.All(gameSession.DiceRolls, dice => Assert.False(dice.Used));
            Assert.False(gameSession.CanUndoLastMove(player1Id));
            Assert.Single(gameSession.BoardModel.History.Events);
            Assert.Equal(HistoryEventType.Roll, gameSession.BoardModel.History.Events.Peek().Type);
        }

        [Theory]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        public void FevgaGameSessionActivePlayerCanUndoHistLastMoves(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            var movSeq = gameSession.GetLegalMoveSequences(isWhite).FirstOrDefault();
            Assert.NotNull(movSeq);
            var firstMove = movSeq.Moves.First();
            var lastMove = movSeq.Moves.Last();
            var fevgaModel = gameSession.BoardModel as IHomeBarModel;
            Assert.NotNull(fevgaModel);

            var firstFromCheckAmount = isWhite ? fevgaModel.HomeBarCountWhite : fevgaModel.HomeBarCountBlack;
            var firstToCheckAmount = gameSession.BoardModel.Fields[firstMove.To];
            var lastFromCheckAmount = gameSession.BoardModel.Fields[lastMove.From];
            var lastToCheckAmount = gameSession.BoardModel.Fields[lastMove.To];

            gameSession.MoveCheckers(player1Id, firstMove.From, firstMove.To, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            gameSession.MoveCheckers(player1Id, lastMove.From, lastMove.To, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);

            Assert.NotEqual(firstFromCheckAmount, isWhite ? fevgaModel.HomeBarCountWhite : fevgaModel.HomeBarCountBlack);
            Assert.Equal(firstToCheckAmount, gameSession.BoardModel.Fields[firstMove.To]);
            Assert.Equal(lastFromCheckAmount, gameSession.BoardModel.Fields[lastMove.From]);
            Assert.NotEqual(lastToCheckAmount, gameSession.BoardModel.Fields[lastMove.To]);

            // 1x roll + 2x move
            Assert.Equal(3, gameSession.BoardModel.History.Events.Count);

            gameSession.UndoLastMove(player1Id, isWhite);
            Assert.True(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            gameSession.UndoLastMove(player1Id, isWhite);
            Assert.False(gameSession.CanUndoLastMove(player1Id));
            Assert.False(gameSession.CanUndoLastMove(player2Id));
            Assert.Equal(GamePhase.Moving, gameSession.Phase);

            // 1x roll + 0x move
            Assert.Single(gameSession.BoardModel.History.Events);

            Assert.Equal(firstFromCheckAmount, isWhite ? fevgaModel.HomeBarCountWhite : fevgaModel.HomeBarCountBlack);
            Assert.Equal(firstToCheckAmount, gameSession.BoardModel.Fields[firstMove.To]);
            Assert.Equal(lastFromCheckAmount, gameSession.BoardModel.Fields[lastMove.From]);
            Assert.Equal(lastToCheckAmount, gameSession.BoardModel.Fields[lastMove.To]);
        }

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Plakoto, false)]
        [InlineData(GameModus.Fevga, false)]
        public void GameSessionLegalMovesAreInverted(GameModus modus, bool isWhite)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, isWhite);
            // moves are inverted for black player
            var gameState = gameSession.ToPayload(player1Id, Array.Empty<string>(), !isWhite);
            Assert.NotNull(gameState);
            var legalMoves = gameSession.ResolveMoveSequences(isWhite).SelectMany(ms => ms.Moves).ToHashSet();
            foreach (var moveSeq in gameState.MoveSequences)
            {
                foreach (var move in moveSeq.Moves)
                {
                    // inversion only applies for black player and checkers
                    if (!isWhite)
                    {
                        if (modus == GameModus.Fevga)
                        {
                            // we invert the inverted move again to find the original move in the game session
                            var invertedFrom = BoardBroker.InvertBoardMoveDiagonalHorizontally(move.From, move.To);
                            Assert.Contains(legalMoves, m => m.From == invertedFrom.Item1 && m.To == invertedFrom.Item2);
                        }
                        else
                        {
                            // we invert the inverted move again to find the original move in the game session
                            var invertedFrom = BoardBroker.InvertBoardMoveHorizontally(move.From, move.To);
                            Assert.Contains(legalMoves, m => m.From == invertedFrom.Item1 && m.To == invertedFrom.Item2);
                        }
                    }
                    else
                    {
                        Assert.Contains(legalMoves, m => m.From == move.From && m.To == move.To);
                    }
                }
            }
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionBlackInvertsToWhiteStart(GameModus modus)
        {
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            // prepare black game session
            var blackGameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var blackMock = new Mock<IDiceService>();
            blackMock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            blackGameSession.InjectDiceServiceMock(blackMock.Object);
            blackGameSession.StartGame(player1Id, player2Id);
            blackGameSession.RollDices(player1Id, false);
            // moves are inverted for black player
            var blackGameState = blackGameSession.ToPayload(player1Id, Array.Empty<string>(), true);
            Assert.NotNull(blackGameState);
            // prepare white game session
            var whiteGameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var whiteMock = new Mock<IDiceService>();
            whiteMock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            whiteGameSession.InjectDiceServiceMock(whiteMock.Object);
            whiteGameSession.StartGame(player1Id, player2Id);
            whiteGameSession.RollDices(player1Id, true);
            // moves are inverted for black player
            var whiteGameState = whiteGameSession.ToPayload(player1Id, Array.Empty<string>(), false);
            Assert.NotNull(whiteGameState);

            // both game states should contain the exact same moves now
            foreach (var blackMoveSeq in blackGameState.MoveSequences)
            {
                foreach (var blackMove in blackMoveSeq.Moves)
                {
                    Assert.Contains(whiteGameState.MoveSequences.SelectMany(ms => ms.Moves), m => m.From == blackMove.From && m.To == blackMove.To);
                }
            }
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        public void GameSessionHasEndTurnPhaseIfRollReturnsNoLegalMoves(GameModus modus)
        {
            // white checkers
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            gameSession.BoardModel.SetFields(BoardMocks.WhiteCannotMoveWithRoll);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, true);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
            // black checkers
            gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            gameSession.BoardModel.SetFields(BoardMocks.BlackCannotMoveWithRoll);
            player1Id = Guid.NewGuid();
            player2Id = Guid.NewGuid();
            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, false);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        public void GameSessionReturnsProperPipesLeftForEachPlayer(GameModus modus)
        {
            // white checkers
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var mock = new Mock<IDiceService>();
            mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
            gameSession.InjectDiceServiceMock(mock.Object);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();

            var player1PipCount = gameSession.BoardModel.PipCountWhite;
            var player2PipCount = gameSession.BoardModel.PipCountBlack;

            gameSession.StartGame(player1Id, player2Id);
            gameSession.RollDices(player1Id, true);
            var moveSeq = gameSession.GetLegalMoveSequences(true).First();
            foreach (var move in moveSeq.Moves)
            {
                gameSession.MoveCheckers(player1Id, move.From, move.To, true);
            }

            // we moved 5 fields
            Assert.Equal(player1PipCount - 5, gameSession.BoardModel.PipCountWhite);
            Assert.Equal(player2PipCount, gameSession.BoardModel.PipCountBlack);

            var gameState = gameSession.ToPayload(player1Id, Array.Empty<string>(), false);
            Assert.NotNull(gameState);
            Assert.Equal(player1PipCount - 5, gameState.BoardState.PipCountWhite);
            Assert.Equal(player2PipCount, gameState.BoardState.PipCountBlack);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void GameSessionStartStopIntegrationTest(GameModus modus)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();

            gameSession.StartGame(player1Id, player2Id);
            Assert.NotNull(gameSession.StartedAt);
            Assert.Null(gameSession.EndedAt);
            Assert.Equal(player1Id, gameSession.ActivePlayer);
            Assert.Equal(player2Id, gameSession.OtherPlayer);
            Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
            Assert.Empty(gameSession.DiceRolls);
            Assert.Empty(gameSession.GetLegalMoveSequences(true));
            Assert.False(gameSession.GameOver(true));
            Assert.False(gameSession.GameOver(false));
            Assert.Equal(1, gameSession.TurnNumber);
            Assert.Equal(GameResultModel.Empty(), gameSession.Result);

            // we manually roll and set the dices
            var rolls = gameSession.RollDices(2);
            Assert.Equal(2, rolls.Length);
            gameSession.SetDiceRolls(rolls, true);
            Assert.NotEmpty(gameSession.DiceRolls);
            Assert.NotEmpty(gameSession.GetLegalMoveSequences(true));
            Assert.True(gameSession.GetLegalMoveSequences(true).CanMove);
            Assert.Equal(GamePhase.Rolling, gameSession.Phase);

            // we go to the next turn without actually moving anything (validation happens im match session)
            gameSession.NextTurn(player2Id);
            Assert.Equal(player2Id, gameSession.ActivePlayer);
            Assert.Equal(player1Id, gameSession.OtherPlayer);
            Assert.Empty(gameSession.DiceRolls);
            Assert.Empty(gameSession.GetLegalMoveSequences(true));
            Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);

            var gameResult = new GameResultModel(player2Id, GameResult.Backgammon, GameResult.LostBackgammon, 3);
            Assert.Equal(player2Id, gameResult.WinnerId);
            Assert.Equal(GameResult.Backgammon, gameResult.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameResult.LoserResult);
            Assert.Equal(3, gameResult.Points);
            gameSession.StopGame(gameResult);
            Assert.NotNull(gameSession.EndedAt);
            Assert.Equal(gameResult, gameSession.Result);
            Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.True(gameSession.Duration >= 0);

            var contract = gameSession.ToContract(1);
            Assert.NotNull(contract);
            Assert.Equal(player2Id, contract.Winner);
            Assert.Equal(1, contract.GameRoundIndex);
            Assert.Equal(3, contract.Points);
            Assert.Equal(modus, contract.Modus);
            Assert.Equal(GamePhase.GameOver, contract.Phase);

            var history = gameSession.GetHistory(player1Id, player2Id);
            Assert.NotNull(history);
            Assert.Equal(player2Id, history.WinnerPlayerId);
            Assert.Equal(player1Id, history.Player1);
            Assert.Equal(player2Id, history.Player2);
            Assert.Equal(gameSession.StartedAt, history.StartedAt);
            Assert.Equal(gameSession.EndedAt, history.EndedAt);
            Assert.Equal(gameSession.Id, history.Id);
            Assert.Equal(3, history.Points);
            Assert.Equal(modus, history.Modus);
            var historyText = history.ToString();
            Assert.Contains($";[Started At '{DateTimeHelper.FormatUtc(history.StartedAt)}']", historyText);
            Assert.Contains($";[Ended At '{DateTimeHelper.FormatUtc(history.EndedAt)}']", historyText);
            Assert.NotNull(history.BoardHistory);
            // we expect a single roll event
            Assert.Single(history.BoardHistory.Events);
            var peek = history.BoardHistory.TryPeekLast(out var lastEvent);
            Assert.True(peek);
            Assert.IsAssignableFrom<HistoryEventImpl>(lastEvent);
            Assert.NotNull(lastEvent);
            Assert.Equal(HistoryEventType.Roll, lastEvent.Type);
            Assert.True(lastEvent.IsWhite);
        }
    }
}
