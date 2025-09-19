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

				var isWhite = IsWhite(matchSession, playerId);
				var requestParameters = CreateParameters(matchSession, isWhite);

				var client = new WildbgClient(_httpClient);
				var result = await client.GetMoveAsync(requestParameters);

				// get best move by equity
				var bestMove = result.LegalMoves.FirstOrDefault();
				if (bestMove != null && bestMove.LegalPlays.Count > 0)
				{
					var legalMoves = new List<MoveModel>();
					foreach (var play in bestMove.LegalPlays)
					{
						var legalMove = Convert(gameSession, play, isWhite);
						legalMoves.Add(legalMove);
					}

					var legalMoveSeq = new MoveSequenceModel();
					legalMoveSeq.Moves.AddRange(legalMoves);
					legalMoveSeq.UsedDices.AddRange(gameSession.DiceRolls.Select(dr => dr.Roll));
					return legalMoveSeq;
				}

				return new MoveSequenceModel();
			}
			catch (Exception)
			{
				// debugging purposes only
				throw;
			}
		}

		private static GetMoveParameter CreateParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			if (gameSession.Modus == GameModus.Portes ||
				gameSession.Modus == GameModus.Backgammon ||
				gameSession.Modus == GameModus.Tavla)
			{
				return CreateStandardParameters(matchSession, isWhite);
			}
			else if (gameSession.Modus == GameModus.Plakoto)
			{
				throw new InvalidOperationException("The plakoto game modus is not yet supported for bot games.");
			}
			else if (gameSession.Modus == GameModus.Fevga)
			{
				throw new InvalidOperationException("The fevga game modus is not yet supported for bot games.");
			}
			else
			{
				throw new InvalidOperationException("The given game modus is not yet supported for bot games.");
			}
		}

		private static GetMoveParameter CreateStandardParameters(IMatchSessionModel matchSession, bool isWhite)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			IBoardModel boardModel;
			if (isWhite)
			{
				// we need to create a board from the black perspective
				boardModel = gameSession.BoardModel.InvertBoard();
			}
			else
			{
				boardModel = gameSession.BoardModel;
			}

			var diceRolls = gameSession.DiceRolls;
			var xPointsAway = matchSession.PointsAway(gameSession.ActivePlayer);
			var oPointsAway = matchSession.PointsAway(gameSession.OtherPlayer);

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
			
			var requestParameters = new GetMoveParameter
			{
				DiceRoll1 = diceRolls[0].Roll,
				DiceRoll2 = diceRolls[1].Roll,
				XPointsAway = xPointsAway,
				OPointsAway = oPointsAway,
				Points = paramBoard
			};
			return requestParameters;
		}

		private static MoveModel Convert(IGameSessionModel gameSession, Play play, bool isWhite)
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
