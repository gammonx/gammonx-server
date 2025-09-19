using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Stubs;
using GammonX.Server.Tests.Testdata;
using GammonX.Server.Tests.Utils;

namespace GammonX.Server.Tests
{
	public class WildbgBotTests
	{
		private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082") };

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public async Task BotCanPlayStandardStartBoards(WellKnownMatchVariant variant, GameModus modus)
		{
			var diceFactory = new DiceServiceFactory();
			var gameSessionFactory = new GameSessionFactory(diceFactory);
			var matchFactory = new MatchSessionFactory(gameSessionFactory);
			var matchSession = SessionUtils.CreateMatchSessionWithBot(variant, matchFactory);
			Assert.Equal(Guid.Empty.ToString(), matchSession.Player2.ConnectionId);

			var botService = new WildbgBotService(_wildBgClient);

			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();

			var botPlayer1Id = matchSession.Player1.Id;
			var botPlayer2Id = matchSession.Player2.Id;

			Assert.Equal(modus, matchSession.GetGameModus());

			// black checker bot moves first
			matchSession.StartNextGame(botPlayer2Id);
			matchSession.RollDices(botPlayer2Id);
			var nextMoves = await botService.GetNextMovesAsync(matchSession, botPlayer2Id);
			var canMove = true;
			foreach (var nextMove in nextMoves.Moves)
			{
				canMove = matchSession.MoveCheckers(botPlayer2Id, nextMove.From, nextMove.To);
			}
			Assert.False(canMove);
			matchSession.EndTurn(botPlayer2Id);

			// white checker bot moves second
			matchSession.StartNextGame(botPlayer1Id);
			matchSession.RollDices(botPlayer1Id);
			nextMoves = await botService.GetNextMovesAsync(matchSession, botPlayer1Id);
			canMove = true;
			foreach (var nextMove in nextMoves.Moves)
			{
				canMove = matchSession.MoveCheckers(botPlayer1Id, nextMove.From, nextMove.To);
			}
			Assert.False(canMove);
			matchSession.EndTurn(botPlayer1Id);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, Skip = "Plakoto and Fevga not yet supported")]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public async Task TwoBotsCanPlayStandalone(WellKnownMatchVariant variant, GameModus modus)
		{
			var diceFactory = new DiceServiceFactory();
			var gameSessionFactory = new GameSessionFactory(diceFactory);
			var matchFactory = new MatchSessionFactory(gameSessionFactory);
			var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(variant, matchFactory);
			Assert.Equal(Guid.Empty.ToString(), matchSession.Player1.ConnectionId);
			Assert.Equal(Guid.Empty.ToString(), matchSession.Player2.ConnectionId);
			Assert.True(matchSession.Player1.IsBot);
			Assert.True(matchSession.Player2.IsBot);

			var botService = new WildbgBotService(_wildBgClient);

			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();

			var botPlayer1Id = matchSession.Player1.Id;
			var botPlayer2Id = matchSession.Player2.Id;
			var activePlayerId = botPlayer1Id;

			Assert.Equal(modus, matchSession.GetGameModus());
			do
			{
				matchSession.Player1.AcceptNextGame();
				matchSession.Player2.AcceptNextGame();

				matchSession.StartNextGame(activePlayerId);
				matchSession.RollDices(activePlayerId);
				var nextMoves = await botService.GetNextMovesAsync(matchSession, activePlayerId);
				var hasWon = false;
				foreach (var nextMove in nextMoves.Moves)
				{
					hasWon = matchSession.MoveCheckers(activePlayerId, nextMove.From, nextMove.To);
					if (hasWon)
						break;
				}

				if (!hasWon)
				{
					matchSession.EndTurn(activePlayerId);
					activePlayerId = activePlayerId == botPlayer1Id ? botPlayer2Id : botPlayer1Id;
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.True(matchSession.Player1.Points > 0 || matchSession.Player2.Points > 0);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 42)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 753)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 289)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 67643)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 33456)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 32)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 9)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 446)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 999)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 321356)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 101)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 202)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 303)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 404)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 505)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 606)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 707)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 808)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 909)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1111)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1212)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1313)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1414)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1515)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1616)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1717)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1818)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 1919)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 2020)]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon, 2121)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 34257)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2341)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 546)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 35)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 5)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 9896)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 745)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 346345)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 346)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 7574)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2201)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2302)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2403)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2504)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2605)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2706)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2807)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 2908)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3009)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3110)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3211)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3312)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3413)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3514)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3615)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3716)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3817)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 3918)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 4019)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes, 4120)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 2024)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 3453)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 23445)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 789)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 632)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 2513)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 123)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 256)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 774)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 413)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4201)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4302)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4403)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4504)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4605)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4706)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4807)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 4908)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5009)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5110)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5211)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5312)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5413)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5514)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5615)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5716)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5817)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 5918)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 6019)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla, 6120)]
		public async Task BotCanPlayRandomStandardBoards(WellKnownMatchVariant variant, GameModus modus, int seed)
		{
			var fields = RandomBoardGenerator.GenerateRandomFields(
				24, 15, 15,
				out int bearOffBlack,
				out int bearOffWhite,
				out int barBlack,
				out int barWhite,
				seed
			);

			var diceFactory = new SeededDiceServiceFactory(seed);
			var gameSessionFactory = new RandomGameSessionBoardFactory(diceFactory, modus, fields, bearOffBlack, bearOffWhite, barBlack, barWhite);
			var matchFactory = new MatchSessionFactory(gameSessionFactory);
			var matchSession = SessionUtils.CreateMatchSessionWithBot(variant, matchFactory);
			Assert.Equal(Guid.Empty.ToString(), matchSession.Player2.ConnectionId);

			var botService = new WildbgBotService(_wildBgClient);

			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();

			var botPlayer1Id = matchSession.Player1.Id;
			var botPlayer2Id = matchSession.Player2.Id;

			Assert.Equal(modus, matchSession.GetGameModus());

			// black checker bot moves first
			matchSession.StartNextGame(botPlayer2Id);
			matchSession.RollDices(botPlayer2Id);
			var nextMoveSeq = await botService.GetNextMovesAsync(matchSession, botPlayer2Id);
			var canMove = true;

			if (nextMoveSeq.Moves.Count == 0)
			{
				canMove = false;
			}

			foreach (var nextMove in nextMoveSeq.Moves)
			{
				canMove = matchSession.MoveCheckers(botPlayer2Id, nextMove.From, nextMove.To);
			}
			Assert.False(canMove);
			matchSession.EndTurn(botPlayer2Id);

			// white checker bot moves second
			matchSession.StartNextGame(botPlayer1Id);
			matchSession.RollDices(botPlayer1Id);
			nextMoveSeq = await botService.GetNextMovesAsync(matchSession, botPlayer1Id);
			canMove = true;

			if (nextMoveSeq.Moves.Count == 0)
			{
				canMove = false;
			}

			foreach (var nextMove in nextMoveSeq.Moves)
			{
				canMove = matchSession.MoveCheckers(botPlayer1Id, nextMove.From, nextMove.To);
			}
			Assert.False(canMove);
			matchSession.EndTurn(botPlayer1Id);
		}
	}
}
