using GammonX.Engine.Extensions;

using GammonX.Models.Enums;
using GammonX.Models;

using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public sealed class TavliMatchSession : MatchSession
	{
		public TavliMatchSession(
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
        /// The first player to bear off all his checkers gets one point, or, 
        /// if the winner bears off all his checkers before the loser has borne off any, he gets two points. 
        /// There is no triple game.
        /// </remarks>
        /// <param name="playerId">Player id who won the game</param>
        /// <returns>Result of the concluded game.</returns>
        protected override GameResultModel ConcludeGame(Guid playerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

            var board = activeSession.BoardModel;
            var isWhite = IsWhite(playerId);
            var gameResult = board.ToGameResult(playerId, isWhite);
            return gameResult;
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
				// we play max 3 rounds in a cash game
				return [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga];
			}
			else if (matchType == MatchType.FivePointGame)
			{
				// we play max 9 rounds in a five point game
				return [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga];
			}
			else if (matchType == MatchType.SevenPointGame)
			{
				// we play max 13 rounds in a seven point game
				return [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Portes, GameModus.Plakoto];
			}
			else
			{
				throw new InvalidOperationException("the given match type is not supported for tavli match variant");
			}
		}
	}
}
