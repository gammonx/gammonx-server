using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Models;
using GammonX.Server.Services;
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
		}

		[Theory]
		[InlineData(GameModus.Backgammon, true)]
		[InlineData(GameModus.Tavla, true)]
		[InlineData(GameModus.Portes, true)]
		[InlineData(GameModus.Plakoto, true)]
		[InlineData(GameModus.Fevga, true, Skip = "fevga is played from index 24 (bar) to 12 (start)")]
		[InlineData(GameModus.Fevga, false, Skip = "fevga is played from index -24 (bar) to 0 (start)")]
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
			var legalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault(ms => Math.Abs(ms.From - ms.To) == 2);
			Assert.NotNull(legalMove);
			gameSession.MoveCheckers(player1Id, legalMove.From, legalMove.To, isWhite);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.True(gameSession.DiceRolls[0].Used);
			Assert.False(gameSession.DiceRolls[1].Used);
			Assert.NotEmpty(gameSession.MoveSequences);
			Assert.True(gameSession.MoveSequences.Select(ms => ms.Moves).All(ms => Math.Abs(ms[0].From - ms[0].To) == 3));
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
		[InlineData(GameModus.Fevga, true, Skip = "fevga is played from index 24 (bar) to 12 (start)")]
		[InlineData(GameModus.Fevga, false, Skip = "fevga is played from index -24 (bar) to 0 (start)")]
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
			var movSeq = gameSession.MoveSequences.Select(ms => ms.Moves).FirstOrDefault(ms => Math.Abs(ms[0].From - ms[1].To) == 5);
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
		[InlineData(GameModus.Fevga, true, Skip = "fevga is played from index 24 (bar) to 12 (start)")]
		[InlineData(GameModus.Fevga, false, Skip = "fevga is played from index -24 (bar) to 0 (start)")]
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
			var legalMoves = gameSession.MoveSequences.Select(ms => ms.Moves).FirstOrDefault(ms => Math.Abs(ms[0].From - ms[1].To) == 2);
			Assert.NotNull(legalMoves);
			gameSession.MoveCheckers(player1Id, legalMoves[0].From, legalMoves[1].To, isWhite);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.True(gameSession.DiceRolls[0].Used);
			Assert.True(gameSession.DiceRolls[1].Used);
			Assert.False(gameSession.DiceRolls[2].Used);
			Assert.False(gameSession.DiceRolls[3].Used);
			Assert.NotEmpty(gameSession.MoveSequences);
			// two dices with 1 roll value left
			Assert.True(gameSession.MoveSequences.Select(ms => ms.Moves).All(ms => Math.Abs(ms[0].From - ms[1].To) == 2 || Math.Abs(ms[0].From - ms[0].To) == 1));
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
	}
}
