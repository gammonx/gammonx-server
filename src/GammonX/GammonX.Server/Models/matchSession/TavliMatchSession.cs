using GammonX.Engine.Models;

using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public sealed class TavliMatchSession : MatchSession
	{
		public TavliMatchSession(
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
		/// The first player to bear off all his checkers gets one point, or, 
		/// if the winner bears off all his checkers before the loser has borne off any, he gets two points. 
		/// There is no triple game.
		/// </remarks>
		/// <param name="playerId">Player id who won the game</param>
		/// <returns>Score won with the game.</returns>
		protected override int CalculateScore(Guid playerId)
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
					// player won with a double game
					return 2;
				}
				else
				{
					// player won with a single game
					return 1;
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
					// player won with a double game
					return 2;
				}
				else
				{
					// player won with a single game
					return 1;
				}
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}
	}
}
