using GammonX.Engine.Models;

using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public sealed class BackgammonMatchSession : MatchSession
	{
		public BackgammonMatchSession(
			Guid id, 
			WellKnownMatchVariant variant, 
			GameModus[] rounds, 
			IGameSessionFactory gameSessionFactory
		) : base(id, variant, rounds, gameSessionFactory)
		{
			// pass
		}

		/// <summary>
		/// Calculates the score for the player who won the game.
		/// </summary>
		/// <remarks>
		/// At the end of the game, if the losing player has borne off at least one checker, 
		/// he loses only the value showing on the doubling cube (one point, if there have been no doubles). 
		/// However, if the loser has not borne off any of his checkers, he is gammoned and loses twice the value of the doubling cube. 
		/// Or, worse, if the loser has not borne off any of his checkers and still has a checker on the bar or in the winner's home board, 
		/// he is backgammoned and loses three times the value of the doubling cube.
		/// </remarks>
		/// <param name="playerId">Player id who won the game</param>
		/// <returns>Score won with the game.</returns>
		protected override int CalculateScore(Guid playerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			if (activeSession.BoardModel is not IDoublingCubeModel doublingCubeModel)
				throw new InvalidOperationException("The board model does not support a doubling cube, so no score can be calculated.");

			if (activeSession.BoardModel is not IHomeBarModel homeBarModel)
				throw new InvalidOperationException("The board model does not support a home bar, so no score can be calculated.");

			var board = activeSession.BoardModel;
			var cubeValue = doublingCubeModel.DoublingCubeValue;

			if (Player1.Id.Equals(playerId))
			{
				if (board.BearOffCountWhite != board.WinConditionCount)
					throw new InvalidOperationException("Player 1 cannot win the game, because not all checkers are borne off.");

				// white checker player
				if (board.BearOffCountBlack == 0 && (homeBarModel.HomeBarCountBlack > 0 || LoserHasCheckersInWinnersHomeBoard(board, true)))
				{
					return 3 * cubeValue; // backgammon
				}
				if (board.BearOffCountBlack == 0)
				{
					// player won with a double game
					return 2 * cubeValue; // gammon
				}
				else
				{
					// player won with a single game
					return 1 * cubeValue; // single game
				}
			}
			else if (Player2.Id.Equals(playerId))
			{
				// black checker player
				if (board.BearOffCountBlack != board.WinConditionCount)
					throw new InvalidOperationException("Player 1 cannot win the game, because not all checkers are borne off.");

				// white checker player
				if (board.BearOffCountWhite == 0 && (homeBarModel.HomeBarCountWhite > 0 || LoserHasCheckersInWinnersHomeBoard(board, false)))
				{
					return 3 * cubeValue; // backgammon
				}
				else if (board.BearOffCountWhite == 0)
				{
					// player won with a double game
					return 2 * cubeValue; // gammon
				}
				else
				{
					// player won with a single game
					return 1 * cubeValue; // single game
				}
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		private static bool LoserHasCheckersInWinnersHomeBoard(IBoardModel model, bool isWhite)
		{
			if (isWhite)
			{
				var blackHasCheckersThere = model.Fields
					.Take(model.HomeRangeWhite)
					.Any(v => v > 0);
				return blackHasCheckersThere;
			}
			else
			{
				if (model.HomeRangeBlack.Start.Value > model.HomeRangeBlack.End.Value)
				{
					var whiteHasCheckersThere = model.Fields
						.Take(new Range(model.HomeRangeBlack.End.Value, model.HomeRangeBlack.Start.Value))
						.Any(v => v < 0);
					return whiteHasCheckersThere;
				}
				else
				{
					var whiteHasCheckersThere = model.Fields
						.Take(model.HomeRangeBlack)
						.Any(v => v < 0);
					return whiteHasCheckersThere;
				}				
			}
		}
	}
}
