using GammonX.Engine.Models;

using GammonX.Server.Contracts;
using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public sealed class BackgammonMatchSession : MatchSession, IDoubleCubeMatchSession
	{
		public BackgammonMatchSession(
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
		/// At the end of the game, if the losing player has borne off at least one checker, 
		/// he loses only the value showing on the doubling cube (one point, if there have been no doubles). 
		/// However, if the loser has not borne off any of his checkers, he is gammoned and loses twice the value of the doubling cube. 
		/// Or, worse, if the loser has not borne off any of his checkers and still has a checker on the bar or in the winner's home board, 
		/// he is backgammoned and loses three times the value of the doubling cube.
		/// </remarks>
		/// <param name="playerId">Player id who won the game</param>
		/// <returns>Score won with the game.</returns>
		protected override int CalculatePoints(Guid playerId)
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

		// <inheritdoc />
		public override EventGameStatePayload GetGameState(Guid playerId, params string[] allowedCommands)
		{
			var gameState = base.GetGameState(playerId, allowedCommands);

			var activeSession = GetGameSession(GameRound);

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (Player1.Id.Equals(playerId)
				&& activeSession.ActivePlayer == Player1.Id
				&& gameState.AllowedCommands.Contains(ServerCommands.RollCommand)
				&& gameState.Phase == GamePhase.WaitingForRoll)
			{
				// If no double was offered yet, every player can offer it at the start of his turn and before he made his roll
				if (doublingCubeModel.DoublingCubeValue == 1)
				{
					gameState.AppendAllowedCommands(ServerCommands.OfferDouble);
				}
				// Afterwards only the cube owner can offer one
				else if (doublingCubeModel.CanOfferDoublingCube())
				{
					gameState.AppendAllowedCommands(ServerCommands.OfferDouble);
				}
			}
			else if (Player2.Id.Equals(playerId)
					&& activeSession.ActivePlayer == Player2.Id
					&& gameState.AllowedCommands.Contains(ServerCommands.RollCommand)
					&& gameState.Phase == GamePhase.WaitingForRoll)
			{
				// If no double was offered yet, every player can offer it at the start of his turn and before he made his roll
				if (doublingCubeModel.DoublingCubeValue == 1)
				{
					gameState.AppendAllowedCommands(ServerCommands.OfferDouble);
				}
				// Afterwards only the cube owner can offer one
				else if (doublingCubeModel.CanOfferDoublingCube())
				{
					gameState.AppendAllowedCommands(ServerCommands.OfferDouble);
				}
			}

			return gameState;
		}

		// <inheritdoc />
		protected override int CalculateResignGamePoints()
		{
			// wins with a back-gammon
			return 3;
		}

		// <inheritdoc />
		protected override GameModus[] GetGameModusList(WellKnownMatchType matchType)
		{
			if (matchType == WellKnownMatchType.CashGame)
			{
				// we play max 1 round in a cash game
				return [GameModus.Backgammon];
			}
			else if (matchType == WellKnownMatchType.FivePointGame)
			{
				// we play max 9 rounds in a five point game
				return Enumerable.Repeat(GameModus.Backgammon, 9).ToArray();
			}
			else if (matchType == WellKnownMatchType.SevenPointGame)
			{
				// we play max 13 rounds in a seven point game
				return Enumerable.Repeat(GameModus.Backgammon, 13).ToArray();
			}
			else
			{
				throw new InvalidOperationException("the given match type is not supported for backgammon match variant");
			}
		}

		private Guid? _doubleCubeOfferPlayerId = null;

		// <inheritdoc />
		public bool IsDoubleOfferPending => _doubleCubeOfferPlayerId != null;

		// <inheritdoc />
		public int GetDoublingCubeValue()
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			return doublingCubeModel.DoublingCubeValue;
		}

		// <inheritdoc />
		public void OfferDouble(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (activeSession.ActivePlayer != callingPlayerId || activeSession.Phase != GamePhase.WaitingForRoll)
			{
				throw new InvalidOperationException("Doubles can only be offered at the start of the active players turn");
			}

			if (doublingCubeModel.DoublingCubeValue > 1)
			{
				if (Player1.Id.Equals(callingPlayerId) && !doublingCubeModel.DoublingCubeOwner)
				{
					// plays white checkers (non inverted board)
					throw new InvalidOperationException("The calling player is not owner of the doubling cube");
				}
				else if (Player2.Id.Equals(callingPlayerId) && doublingCubeModel.DoublingCubeOwner)
				{
					// plays black checkers (inverted board)
					throw new InvalidOperationException("The calling player is not owner of the doubling cube");
				}
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				_doubleCubeOfferPlayerId = callingPlayerId;
			}
			else
			{
				throw new InvalidOperationException($"The player with id '{_doubleCubeOfferPlayerId}' already offered a double and the response is pending");
			}
		}

		// <inheritdoc />
		public void AcceptDouble(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				throw new InvalidOperationException("There is not pending doubling cube offer to accept");
			}

			if (callingPlayerId == _doubleCubeOfferPlayerId.Value)
			{
				throw new InvalidOperationException("The calling player must not accept the double offer");
			}

			doublingCubeModel.AcceptDoublingCubeOffer();

			// we only track the board from the player1 white checker point of view
			// for player2 the board gets inverted. Therefore, we only need to set the
			// doubling cube owner with the persepctive of player1

			// player1 plays with white checkers
			if (Player1.Id.Equals(callingPlayerId))
			{
				doublingCubeModel.DoublingCubeOwner = true;
			}
			// player2 plays with black checkers
			else if (Player2.Id.Equals(callingPlayerId))
			{
				doublingCubeModel.DoublingCubeOwner = false;
			}
			else
			{
				throw new InvalidOperationException("calling player is not part of the match session");
			}

			// reset the pending doubling offer
			_doubleCubeOfferPlayerId = null;
		}

		// <inheritdoc />
		public void DeclineDouble(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				throw new InvalidOperationException("There is not pending doubling cube offer to accept");
			}

			if (callingPlayerId == _doubleCubeOfferPlayerId.Value)
			{
				throw new InvalidOperationException("The calling player must not accept the double offer");
			}

			// Other player gets the points for his score
			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			var otherPlayer = GetPlayer(otherPlayerId);
			var gameScore = 1 * doublingCubeModel.DoublingCubeValue;
			otherPlayer.Points += gameScore;
			activeSession.StopGame(otherPlayerId, gameScore);
			Player1.ActiveGameOver();
			Player2.ActiveGameOver();

			// reset the pending doubling offer
			_doubleCubeOfferPlayerId = null;
		}

		// <inheritdoc />
		public bool CanOfferDouble(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
			{
				return false;
			}

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				return false;
			}

			if (activeSession.ActivePlayer != callingPlayerId || activeSession.Phase != GamePhase.WaitingForRoll)
			{
				return false;
			}

			if (doublingCubeModel.DoublingCubeValue > 1)
			{
				if (Player1.Id.Equals(callingPlayerId) && !doublingCubeModel.DoublingCubeOwner)
				{
					// plays white checkers (non inverted board)
					return false;
				}
				else if (Player2.Id.Equals(callingPlayerId) && doublingCubeModel.DoublingCubeOwner)
				{
					// plays black checkers (inverted board)
					return false;
				}
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				return true;
			}
			else
			{
				return false;
			}
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
