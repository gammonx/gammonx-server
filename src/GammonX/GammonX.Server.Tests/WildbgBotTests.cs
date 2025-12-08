using GammonX.Engine.Services;

using GammonX.Models.Enums;

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
		private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public async Task BotCanPlayStandardStartBoards(MatchVariant variant, GameModus modus)
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
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public async Task TwoBotsCanPlayStandalone(MatchVariant variant, GameModus modus)
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
			var botPlayer1Id = matchSession.Player1.Id;
			var botPlayer2Id = matchSession.Player2.Id;
			var activePlayerId = botPlayer1Id;
			Assert.Equal(modus, matchSession.GetGameModus());
			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();
			do
			{
				if (matchSession.CanStartNextGame())
				{
					matchSession.StartNextGame(activePlayerId);
				}

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
				else
				{
					matchSession.Player1.AcceptNextGame();
					matchSession.Player2.AcceptNextGame();
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
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		public async Task TwoBotsCanPlayStandaloneWithCubeDecisions(MatchVariant variant, GameModus modus)
		{
			var diceFactory = new DiceServiceFactory();
			var gameSessionFactory = new GameSessionFactory(diceFactory);
			var matchFactory = new MatchSessionFactory(gameSessionFactory);
			var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(variant, matchFactory);
			var cubeSession = matchSession as IDoubleCubeMatchSession;
			Assert.NotNull(cubeSession);
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
			var otherPlayerId = botPlayer2Id;
			var doubleAccepted = false;

			Assert.Equal(modus, matchSession.GetGameModus());
			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();
			matchSession.StartNextGame(activePlayerId);

			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);
			do
			{
				if (cubeSession.CanOfferDouble(activePlayerId))
				{
					var shouldOffer = await botService.ShouldOfferDouble(matchSession, activePlayerId);
					if (shouldOffer)
					{
						gameSession = matchSession.GetGameSession(matchSession.GameRound);
						Assert.NotNull(gameSession);
						Assert.Equal(activePlayerId, gameSession.ActivePlayer);
						cubeSession.OfferDouble(activePlayerId);
						Assert.Equal(otherPlayerId, gameSession.ActivePlayer);
						var shouldAccept = await botService.ShouldAcceptDouble(matchSession, otherPlayerId);
						if (shouldAccept)
						{
							cubeSession.AcceptDouble(otherPlayerId);
							Assert.Equal(activePlayerId, gameSession.ActivePlayer);
							doubleAccepted = true;
						}
						else
						{
							cubeSession.DeclineDouble(otherPlayerId);
							Assert.Equal(activePlayerId, gameSession.ActivePlayer);
							break;
						}
					}
				}

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
					activePlayerId = otherPlayerId;
					otherPlayerId = activePlayerId == botPlayer1Id ? botPlayer2Id : botPlayer1Id;
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.True(matchSession.Player1.Points > 0 || matchSession.Player2.Points > 0);
			if (doubleAccepted)
			{
				Assert.True(matchSession.Player1.Points > 1 || matchSession.Player2.Points > 1);
			}
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 42)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 753)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 289)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 67643)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 33456)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 32)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 9)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 446)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 999)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 321356)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 101)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 202)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 303)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 404)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 505)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 606)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 707)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 808)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 909)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1111)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1212)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1313)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1414)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1515)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1616)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1717)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1818)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 1919)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 2020)]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon, 2121)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 34257)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2341)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 546)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 35)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 5)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 9896)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 745)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 346345)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 346)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 7574)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2201)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2302)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2403)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2504)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2605)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2706)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2807)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 2908)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3009)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3110)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3211)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3312)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3413)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3514)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3615)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3716)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3817)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 3918)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 4019)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes, 4120)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 2024)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 3453)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 23445)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 789)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 632)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 2513)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 123)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 256)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 774)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 413)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4201)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4302)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4403)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4504)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4605)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4706)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4807)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 4908)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5009)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5110)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5211)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5312)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5413)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5514)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5615)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5716)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5817)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 5918)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 6019)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla, 6120)]
		public async Task BotCanPlayRandomStandardBoards(MatchVariant variant, GameModus modus, int seed)
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
