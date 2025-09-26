using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	/// <summary>
	/// Made uses of the open source wildbg bot in order to calculate the match equity.
	/// </summary>
	/// <seealso cref="https://github.com/carsten-wenderdel/wildbg"/>
	public class WildbgBotService : IBotService
	{
		private readonly HttpClient _httpClient;
		private readonly SimplePlakotoBotService _simplePlakotoBot = new();
		private readonly SimpleFevgaBotService _simpleFevgaBot = new();

		public WildbgBotService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		// <inheritdoc />
		public async Task<MoveSequenceModel> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
		{
			try
			{
				var gameSession = matchSession.GetGameSession(matchSession.GameRound);
				if (gameSession == null)
					throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

				if (gameSession.Modus == GameModus.Plakoto)
				{
					// plakoto is not supported by wildbg bot
					return await _simplePlakotoBot.GetNextMovesAsync(matchSession, playerId);
				}
				else if (gameSession.Modus == GameModus.Fevga)
				{
					// fevga is not supported by wildbg bot
					return await _simpleFevgaBot.GetNextMovesAsync(matchSession, playerId);
				}

				var isWhite = IsWhite(matchSession, playerId);
				var requestParameters = GetMoveParameters(matchSession, isWhite);

				// wildbg bot only supports cash and single point games
				// we also use cash games for all match variants
				// a five/seven point game equals in this context to multiple cash games
				if (matchSession.Type == WellKnownMatchType.CashGame)
				{
					requestParameters.XPointsAway = 0;
					requestParameters.OPointsAway = 0;
				}
				else
				{
					requestParameters.XPointsAway = 0;
					requestParameters.OPointsAway = 0;
				}

				var client = new WildbgClient(_httpClient);
				var result = await client.GetMoveAsync(requestParameters);
				var legalMoveSequences = result.LegalMoves.Select(lm => ConvertMoveToSequence(gameSession, lm, isWhite)).ToList();

				// get first move which has the highest match equity
				var bestMoveSequence = legalMoveSequences.FirstOrDefault();
				if (bestMoveSequence != null && bestMoveSequence.Moves.Count > 0)
				{					
					return bestMoveSequence;
				}

				return new MoveSequenceModel();
			}
			catch (Exception)
			{
				// debugging purposes only
				throw;
			}
		}

		// <inheritdoc />
		public async Task<bool> ShouldAcceptDouble(IMatchSessionModel matchSession, Guid playerId)
		{
			try
			{
				var gameSession = matchSession.GetGameSession(matchSession.GameRound);
				if (gameSession == null)
					throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

				var isWhite = IsWhite(matchSession, playerId);
				var requestParameters = GetEvalParameters(matchSession, isWhite);

				var client = new WildbgClient(_httpClient);
				var result = await client.GetEvalAsync(requestParameters);

				return result.CubeDecision.Accept;
			}
			catch (Exception)
			{
				// debugging purposes only
				throw;
			}
		}

		// <inheritdoc />
		public async Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
		{
			try
			{
				var gameSession = matchSession.GetGameSession(matchSession.GameRound);
				if (gameSession == null)
					throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

				var isWhite = IsWhite(matchSession, playerId);
				var requestParameters = GetEvalParameters(matchSession, isWhite);

				var client = new WildbgClient(_httpClient);
				var result = await client.GetEvalAsync(requestParameters);

				return result.CubeDecision.Double;
			}
			catch (Exception)
			{
				// debugging purposes only
				throw;
			}
		}

		private static GetMoveParameter GetMoveParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			if (gameSession.Modus == GameModus.Portes ||
				gameSession.Modus == GameModus.Backgammon ||
				gameSession.Modus == GameModus.Tavla)
			{
				return CreateMoveParameters(matchSession, isWhite);
			}
			else if (gameSession.Modus == GameModus.Plakoto)
			{
				throw new InvalidOperationException("Plakoto is not supported by the wildbg bot.");
			}
			else if (gameSession.Modus == GameModus.Fevga)
			{
				throw new InvalidOperationException("Plakoto is not supported by the wildbg bot.");
			}
			else
			{
				throw new InvalidOperationException("The given game modus is not yet supported for bot games.");
			}
		}

		private static GetEvalParameter GetEvalParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			if (gameSession.Modus == GameModus.Backgammon)
			{
				return CreateEvalParameters(matchSession, isWhite);
			}
			else
			{
				throw new InvalidOperationException("The given game modus does not support cube decisions.");
			}
		}

		private static GetMoveParameter CreateMoveParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");
			
			var diceRolls = gameSession.DiceRolls;
			var xPointsAway = matchSession.PointsAway(gameSession.ActivePlayer);
			var oPointsAway = matchSession.PointsAway(gameSession.OtherPlayer);
			var pointsParam = GetBoardStateAsPointsParam(gameSession, isWhite);

			var requestParameters = new GetMoveParameter
			{
				DiceRoll1 = diceRolls[0].Roll,
				DiceRoll2 = diceRolls[1].Roll,
				XPointsAway = xPointsAway,
				OPointsAway = oPointsAway,
				Points = pointsParam
			};
			return requestParameters;
		}

		private static GetEvalParameter CreateEvalParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			var pointsParam = GetBoardStateAsPointsParam(gameSession, isWhite);

			var requestParameters = new GetEvalParameter
			{
				Points = pointsParam
			};
			return requestParameters;
		}

		private static Dictionary<int, int> GetBoardStateAsPointsParam(IGameSessionModel gameSession, bool isWhite)
		{
			IBoardModel boardModel;
			if (isWhite)
			{
				// we need to create a board from the black perspective
				// also applies for fevga board, white plays from 0 to 23
				boardModel = gameSession.BoardModel.InvertBoard();
			}
			else
			{
				// black plays from 23 to 0
				boardModel = gameSession.BoardModel;
			}

			var paramBoard = new Dictionary<int, int>();
			// we start at black checkers starting field and iterate to its home field
			for (int boardIndex = 23; boardIndex >= 0; boardIndex--)
			{
				var checkerCount = boardModel.Fields[boardIndex];
				// negative values are for opponent (white checkers)
				// positive values are for player (black checkers)
				if (checkerCount != 0)
				{
					// web api expects a non null based array
					paramBoard[boardIndex + 1] = checkerCount;
				}
			}
			if (boardModel is IHomeBarModel homeBar)
			{
				paramBoard[25] = homeBar.HomeBarCountBlack;
				paramBoard[0] = -homeBar.HomeBarCountWhite;
			}

			return paramBoard;
		}

		private static MoveSequenceModel ConvertMoveToSequence(IGameSessionModel gameSession, Move move, bool isWhite)
		{
			var moveModels = move.LegalPlays.Select(p => ConvertPlayToMove(gameSession, p, isWhite)).ToList();
			var sequenceModel = new MoveSequenceModel();
			sequenceModel.Moves.AddRange(moveModels);
			sequenceModel.UsedDices.AddRange(gameSession.DiceRolls.GetRemainingRolls());
			return sequenceModel;
		}

		private static MoveModel ConvertPlayToMove(IGameSessionModel gameSession, Play play, bool isWhite)
		{
			// 25 is homebar index
			if (play.From == 25)
			{
				if (isWhite)
				{
					play.From = WellKnownBoardPositions.HomeBarWhite;
				}
				else
				{
					play.From = WellKnownBoardPositions.HomeBarBlack;
				}
			}
			else
			{
				play.From = play.From - 1;

			}
			// 0 is bear off index
			if (play.To == 0)
			{
				if (isWhite)
				{
					play.To = WellKnownBoardPositions.BearOffWhite;
				}
				else
				{
					play.To = WellKnownBoardPositions.BearOffBlack;
				}
			}
			else
			{
				play.To = play.To - 1;
			}

			var maxFieldIndex = gameSession.BoardModel.Fields.Length - 1;
			if (isWhite)
			{
				int convertedFrom = play.From;
				int convertedTo = play.To;
				if (play.From != WellKnownBoardPositions.HomeBarBlack && play.From != WellKnownBoardPositions.HomeBarWhite)
				{
					// we need to invert the play for the white checkers perspective
					convertedFrom = (maxFieldIndex) - play.From;
				}
				if (play.To != WellKnownBoardPositions.BearOffBlack && play.To != WellKnownBoardPositions.BearOffWhite)
				{
					// we need to invert the play for the white checkers perspective
					convertedTo = (maxFieldIndex) - play.To;
				}
				return new MoveModel(convertedFrom, convertedTo);
			}
			else
			{
				// the wildbg service already returns it from the black checker perspective
				return new MoveModel(play.From, play.To);
			}
		}

		private static bool IsWhite(IMatchSessionModel matchSession, Guid playerId)
		{
			if (matchSession.Player1.Id.Equals(playerId))
			{
				return true;
			}
			else if (matchSession.Player2.Id.Equals(playerId))
			{
				return false;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}
	}
}
