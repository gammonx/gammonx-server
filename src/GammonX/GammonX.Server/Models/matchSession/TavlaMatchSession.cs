using GammonX.Models.Enums;

using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Models
{
	public sealed class TavlaMatchSession : MatchSession
	{
		public TavlaMatchSession(
			Guid id,
			QueueKey queueKey,
			IGameSessionFactory gameSessionFactory
		) : base(id, queueKey, gameSessionFactory)
		{
			// pass
		}

		/// <summary>
		/// Calculates the score for the player who won the game.
		/// </summary>
		/// <remarks>
		/// The first player to bear off all fifteen checkers wins the game. 
		/// If the losing player has borne off at least one checker, he loses only one point. 
		/// If the losing player has not borne off any checkers, he loses two points.
		/// There is no triple game.
		/// </remarks>
		/// <param name="playerId">Player id who won the game</param>
		/// <returns>Result of the concluded game.</returns>
		protected override GameResultModel ConcludeGame(Guid playerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			if (Player1.Id.Equals(playerId))
			{
				if (activeSession.BoardModel.BearOffCountWhite != activeSession.BoardModel.WinConditionCount)
					throw new InvalidOperationException("Player 1 cannot win the game, because not all checkers are borne off.");

				// white checker player
				if (activeSession.BoardModel.BearOffCountBlack == 0)
				{
					return new GameResultModel(playerId, GameResult.Gammon, GameResult.LostGammon, 2);
				}
				else
				{
                    return new GameResultModel(playerId, GameResult.Single, GameResult.LostSingle, 1);
                }
			}
			else if (Player2.Id.Equals(playerId))
			{
				// black checker player
				if (activeSession.BoardModel.BearOffCountBlack != activeSession.BoardModel.WinConditionCount)
					throw new InvalidOperationException("Player 1 cannot win the game, because not all checkers are borne off.");

				// white checker player
				if (activeSession.BoardModel.BearOffCountWhite == 0)
				{
                    return new GameResultModel(playerId, GameResult.Gammon, GameResult.LostGammon, 2);
                }
				else
				{
                    return new GameResultModel(playerId, GameResult.Single, GameResult.LostSingle, 1);
                }
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		// <inheritdoc />
		protected override int CalculateResignGamePoints()
		{
			// wins with a gammon
			return 2;
		}

		// <inheritdoc />
		protected override GameModus[] GetGameModusList(MatchType matchType)
		{
			if (matchType == MatchType.CashGame)
			{
				// we play max 1 round in a cash game
				return [GameModus.Tavla];
			}
			else if (matchType == MatchType.FivePointGame)
			{
				// we play max 9 rounds in a five point game
				return Enumerable.Repeat(GameModus.Tavla, 9).ToArray();
			}
			else if (matchType == MatchType.SevenPointGame)
			{
				// we play max 13 rounds in a seven point game
				return Enumerable.Repeat(GameModus.Tavla, 13).ToArray();
			}
			else
			{
				throw new InvalidOperationException("the given match type is not supported for tavla match variant");
			}
		}
	}
}
