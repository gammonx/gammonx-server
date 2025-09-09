using GammonX.Engine.Models;

using GammonX.Server.Contracts;
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
		public async Task<LegalMoveContract[]> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
		{
			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			if (gameSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			var isWhite = IsWhite(matchSession, playerId);
			var requestParameters = CreateParameters(gameSession, isWhite);

			var client = new WildbgClient(_httpClient);
			var result = await client.GetMoveAsync(requestParameters);

			// get best move by equity
			var bestMove = result.LegalMoves.FirstOrDefault();
			if (bestMove != null && bestMove.LegalPlays.Any())
			{
				var legalMoves = new List<LegalMoveContract>();
				foreach (var play in bestMove.LegalPlays)
				{
					var legalMove = Convert(gameSession, play, isWhite);
					legalMoves.Add(legalMove);
				}
				return legalMoves.ToArray();
			}

			return Array.Empty<LegalMoveContract>();
		}

		private static GetMoveParameter CreateParameters(IGameSessionModel gameSession, bool isWhite)
		{
			// TODO :: add support for fevga and plakoto
			if (gameSession.Modus == GameModus.Portes ||
				gameSession.Modus == GameModus.Backgammon ||
				gameSession.Modus == GameModus.Tavla)
			{
				return CreateStandardParameters(gameSession, isWhite);
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

		private static GetMoveParameter CreateStandardParameters(IGameSessionModel gameSession, bool isWhite)
		{
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

			var paramBoard = new Dictionary<int, int>();
			// we start at black checkers starting field and iterate to its home field
			for (int boardIndex = 23; boardIndex > 0; boardIndex--)
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

			var diceRolls = gameSession.DiceRollsModel.DiceRolls;
			var requestParameters = new GetMoveParameter
			{
				DiceRoll1 = diceRolls[0].Roll,
				DiceRoll2 = diceRolls[1].Roll,
				// TODO multiple point games
				XPointsAway = 0,
				OPointsAway = 0,
				Points = paramBoard
			};
			return requestParameters;
		}

		private static LegalMoveContract Convert(IGameSessionModel gameSession, Play play, bool isWhite)
		{
			if (play.From == 0)
			{
				// convert well known homebar position
				play.From = isWhite ? WellKnownBoardPositions.HomeBarBlack : WellKnownBoardPositions.HomeBarWhite;
			}
			else if (play.From == 25)
			{
				// convert well know homebar position
				play.From = isWhite ? WellKnownBoardPositions.HomeBarWhite : WellKnownBoardPositions.HomeBarBlack;
			}
			else
			{
				// convert back to zero based array
				play.From = play.From - 1;
			}

			if (play.To == 0)
			{
				// convert well known homebar position
				play.To = isWhite ? WellKnownBoardPositions.HomeBarWhite : WellKnownBoardPositions.HomeBarBlack;
			}
			else if (play.To == 25)
			{
				// convert well know homebar position
				play.To = isWhite ? WellKnownBoardPositions.HomeBarBlack : WellKnownBoardPositions.HomeBarWhite;
			}
			else
			{
				// convert back to zero based array
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
				if (play.To != WellKnownBoardPositions.HomeBarBlack && play.To != WellKnownBoardPositions.HomeBarWhite)
				{
					// we need to invert the play for the white checkers perspective
					convertedTo = (maxFieldIndex) - play.To;
				}
				return new LegalMoveContract(convertedFrom, convertedTo);
				
			}
			else
			{
				// the wildbg service already returns it from the black checker perspective
				return new LegalMoveContract(play.From, play.To);
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
