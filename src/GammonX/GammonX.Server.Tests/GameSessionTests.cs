using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

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
            Assert.Empty(gameSession.MoveSequences);
            Assert.Equal(DateTime.MinValue, gameSession.StartedAt);
            Assert.Equal(DateTime.MaxValue, gameSession.EndedAt);
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
            Assert.NotEmpty(gameSession.MoveSequences);
            var legalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms.From, ms.To, out var _) == 2);
            Assert.NotNull(legalMove);
            gameSession.MoveCheckers(player1Id, legalMove.From, legalMove.To, isWhite);
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.False(gameSession.DiceRolls[1].Used);
            Assert.NotEmpty(gameSession.MoveSequences);
            Assert.True(gameSession.MoveSequences.Select(ms => ms.Moves).All(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[0].To, out var _) == 3));
            legalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
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
            Assert.NotEmpty(gameSession.MoveSequences);
            var movSeq = gameSession.MoveSequences.Select(ms => ms.Moves).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 5);
            Assert.NotNull(movSeq);
            gameSession.MoveCheckers(player1Id, movSeq[0].From, movSeq[1].To, isWhite);
            Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.True(gameSession.DiceRolls[1].Used);
            Assert.Empty(gameSession.MoveSequences);
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
            Assert.NotEmpty(gameSession.MoveSequences);
            // use up 2 dices
            // so we search a move sequence were move 1 and 2 are using two 1 roll dices
            var legalMoves = gameSession.MoveSequences.Select(ms => ms.Moves).FirstOrDefault(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 2);
            Assert.NotNull(legalMoves);
            gameSession.MoveCheckers(player1Id, legalMoves[0].From, legalMoves[1].To, isWhite);
            Assert.Equal(GamePhase.Moving, gameSession.Phase);
            Assert.True(gameSession.DiceRolls[0].Used);
            Assert.True(gameSession.DiceRolls[1].Used);
            Assert.False(gameSession.DiceRolls[2].Used);
            Assert.False(gameSession.DiceRolls[3].Used);
            Assert.NotEmpty(gameSession.MoveSequences);
            // two dices with 1 roll value left
            Assert.True(gameSession.MoveSequences.Select(ms => ms.Moves).All(ms => DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[1].To, out var _) == 2 || DiceRollsModel.GetMoveDistance(gameSession.BoardModel, ms[0].From, ms[0].To, out var _) == 1));
            legalMoves = gameSession.MoveSequences.Select(ms => ms.Moves).FirstOrDefault(lm => Math.Abs(lm[0].From - lm[1].To) == 2);
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
            var movSeq = gameSession.MoveSequences.FirstOrDefault();
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
            var movSeq = gameSession.MoveSequences.FirstOrDefault();
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
                            Assert.Contains(gameSession.MoveSequences.SelectMany(ms => ms.Moves), m => m.From == invertedFrom.Item1 && m.To == invertedFrom.Item2);
                        }
                        else
                        {
                            // we invert the inverted move again to find the original move in the game session
                            var invertedFrom = BoardBroker.InvertBoardMoveHorizontally(move.From, move.To);
                            Assert.Contains(gameSession.MoveSequences.SelectMany(ms => ms.Moves), m => m.From == invertedFrom.Item1 && m.To == invertedFrom.Item2);
                        }
                    }
                    else
                    {
                        Assert.Contains(gameSession.MoveSequences.SelectMany(ms => ms.Moves), m => m.From == move.From && m.To == move.To);
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
        [InlineData(GameModus.Fevga)]
        public void GameSessionStartStopIntegrationTest(GameModus modus)
        {
            var gameSession = _gameSessionFactory.Create(Guid.NewGuid(), modus);
            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();

            gameSession.StartGame(player1Id, player2Id);
            Assert.NotEqual(DateTime.MinValue, gameSession.StartedAt);
            Assert.Equal(DateTime.MaxValue, gameSession.EndedAt);
            Assert.Equal(player1Id, gameSession.ActivePlayer);
            Assert.Equal(player2Id, gameSession.OtherPlayer);
            Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
            Assert.Empty(gameSession.DiceRolls);
            Assert.Empty(gameSession.MoveSequences);
            Assert.False(gameSession.GameOver(true));
            Assert.False(gameSession.GameOver(false));
            Assert.Equal(1, gameSession.TurnNumber);
            Assert.Equal(GameResultModel.Empty(), gameSession.Result);

            // we manually roll and set the dices
            var rolls = gameSession.RollDices(2);
            Assert.Equal(2, rolls.Length);
            gameSession.SetDiceRolls(rolls, true);
            Assert.NotEmpty(gameSession.DiceRolls);
            Assert.NotEmpty(gameSession.MoveSequences);
            Assert.True(gameSession.MoveSequences.CanMove);
            Assert.Equal(GamePhase.Rolling, gameSession.Phase);

            // we go to the next turn without actually moving anything (validation happens im match session)
            gameSession.NextTurn(player2Id);
            Assert.Equal(player2Id, gameSession.ActivePlayer);
            Assert.Equal(player1Id, gameSession.OtherPlayer);
            Assert.Empty(gameSession.DiceRolls);
            Assert.Empty(gameSession.MoveSequences);
            Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);

            var gameResult = new GameResultModel(player2Id, GameResult.Backgammon, GameResult.LostBackgammon, 3);
            Assert.Equal(player2Id, gameResult.WinnerId);
            Assert.Equal(GameResult.Backgammon, gameResult.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameResult.LoserResult);
            Assert.Equal(3, gameResult.Points);
            gameSession.StopGame(gameResult);
            Assert.NotEqual(DateTime.MaxValue, gameSession.EndedAt);
            Assert.Equal(gameResult, gameSession.Result);
            Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal((gameSession.EndedAt - gameSession.StartedAt).Milliseconds, gameSession.Duration);

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
