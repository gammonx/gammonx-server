using GammonX.Engine.Models;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models.gameSession;
using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

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
        /// <returns>Result of the concluded game.</returns>
        protected override GameResultModel ConcludeGame(Guid playerId)
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
					var points = 3 * cubeValue;
					var result = new GameResultModel(playerId, GameResult.Backgammon, GameResult.LostBackgammon, points);
					return result;
				}
				if (board.BearOffCountBlack == 0)
				{
                    var points = 2 * cubeValue;
                    var result = new GameResultModel(playerId, GameResult.Gammon, GameResult.LostGammon, points);
					return result;
				}
				else
				{
                    var points = 1 * cubeValue;
                    var result = new GameResultModel(playerId, GameResult.Single, GameResult.LostSingle, points);
					return result;

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
                    var points = 3 * cubeValue;
                    var result = new GameResultModel(playerId, GameResult.Backgammon, GameResult.LostBackgammon, points);
                    return result;
                }
				else if (board.BearOffCountWhite == 0)
				{
                    var points = 2 * cubeValue;
                    var result = new GameResultModel(playerId, GameResult.Gammon, GameResult.LostGammon, points);
                    return result;
                }
				else
				{
                    var points = 1 * cubeValue;
                    var result = new GameResultModel(playerId, GameResult.Single, GameResult.LostSingle, points);
                    return result;
                }
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		// <inheritdoc />
		public override EventGameStatePayload GetGameState(Guid callingPlayerId)
		{
			var gameState = base.GetGameState(callingPlayerId);
			if (CanExecuteOfferDoubleCommand(callingPlayerId))
			{
				gameState.AppendAllowedCommands(ServerCommands.OfferDoubleCommand);
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
		protected override GameModus[] GetGameModusList(MatchType matchType)
		{
			if (matchType == MatchType.CashGame)
			{
				// we play max 1 round in a cash game
				return [GameModus.Backgammon];
			}
			else if (matchType == MatchType.FivePointGame)
			{
				// we play max 9 rounds in a five point game
				return Enumerable.Repeat(GameModus.Backgammon, 9).ToArray();
			}
			else if (matchType == MatchType.SevenPointGame)
			{
				// we play max 13 rounds in a seven point game
				return Enumerable.Repeat(GameModus.Backgammon, 13).ToArray();
			}
			else
			{
				throw new InvalidOperationException("the given match type is not supported for backgammon match variant");
			}
		}

		protected override bool IsCommandCallValid(Guid callingPlayerId, string calledCommand)
		{
			var availableCommands = ServerCommands.GetAllowedCommands(this, callingPlayerId, _lastExecutedCommand).ToList();
			if (CanExecuteOfferDoubleCommand(callingPlayerId))
			{
				availableCommands.Add(ServerCommands.OfferDoubleCommand);
			}
			if (availableCommands.Contains(calledCommand))
			{
				return true;
			}
			return false;
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
		[ServerCommand(ServerCommands.OfferDoubleCommand)]
		public void OfferDouble(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.OfferDoubleCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.OfferDoubleCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (activeSession is not IDoublingCubeGameSession doublingCubeGameSession)
			{
				throw new InvalidOperationException("The game does not support doubling cubes.");
			}

			if (activeSession.ActivePlayer != callingPlayerId || activeSession.Phase != GamePhase.WaitingForRoll)
			{
				throw new InvalidOperationException("Doubles can only be offered at the start of the active players turn");
			}

			if (doublingCubeModel.DoublingCubeValue > 1)
			{
				var isWhite = IsWhite(callingPlayerId);
				if (!doublingCubeModel.CanOfferDoublingCube(isWhite))
				{
					throw new InvalidOperationException("A double can only be offered by the owner of the cube");
				}
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				_doubleCubeOfferPlayerId = callingPlayerId;
				doublingCubeGameSession.DoubleOffered(callingPlayerId);
				_lastExecutedCommand = ServerCommands.OfferDoubleCommand;
			}
			else
			{
				throw new InvalidOperationException($"The player with id '{_doubleCubeOfferPlayerId}' already offered a double and the response is pending");
			}
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.AcceptDoubleCommand)]
		public void AcceptDouble(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.AcceptDoubleCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.AcceptDoubleCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (activeSession is not IDoublingCubeGameSession doublingCubeGameSession)
			{
				throw new InvalidOperationException("The game does not support doubling cubes.");
			}

			if (_doubleCubeOfferPlayerId == null)
			{
				throw new InvalidOperationException("There is not pending doubling cube offer to accept");
			}

			if (callingPlayerId == _doubleCubeOfferPlayerId.Value)
			{
				throw new InvalidOperationException("The calling player must not accept the double offer");
			}

			var isWhite = IsWhite(callingPlayerId);
			doublingCubeModel.AcceptDoublingCubeOffer(isWhite);

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
			_lastExecutedCommand = ServerCommands.AcceptDoubleCommand;
			doublingCubeGameSession.DoubleAccepted(callingPlayerId);
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.DeclineDoubleCommand)]
		public void DeclineDouble(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.DeclineDoubleCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.DeclineDoubleCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				throw new InvalidOperationException("The match does not support doubling cubes.");
			}

			if (activeSession is not IDoublingCubeGameSession doublingCubeGameSession)
			{
				throw new InvalidOperationException("The game does not support doubling cubes.");
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
			var gamePoints = 1 * doublingCubeModel.DoublingCubeValue;
			var gameResult = new GameResultModel(otherPlayerId, GameResult.DoubleDeclined, GameResult.LostDoubleDeclined, gamePoints);
			otherPlayer.Points += gamePoints;
			activeSession.StopGame(gameResult);
			Player1.ActiveGameOver();
			Player2.ActiveGameOver();

			// reset the pending doubling offer
			_doubleCubeOfferPlayerId = null;
			_lastExecutedCommand = ServerCommands.DeclineDoubleCommand;
			doublingCubeGameSession.DoubleDeclined(callingPlayerId);
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
				var isWhite = IsWhite(callingPlayerId);
				if (!doublingCubeModel.CanOfferDoublingCube(isWhite))
				{
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

		private bool CanExecuteOfferDoubleCommand(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession?.BoardModel is not IDoublingCubeModel doublingCubeModel)
			{
				return false;
			}

			var allowedCommands = ServerCommands.GetAllowedCommands(this, callingPlayerId, _lastExecutedCommand);

			if (Player1.Id.Equals(callingPlayerId)
				&& activeSession.ActivePlayer == Player1.Id
				&& allowedCommands.Contains(ServerCommands.RollCommand)
				&& activeSession.Phase == GamePhase.WaitingForRoll)
			{
				// If no double was offered yet, every player can offer it at the start of his turn and before he made his roll
				if (doublingCubeModel.DoublingCubeValue == 1)
				{
					return true;
				}
				// Afterwards only the cube owner can offer one
				else if (doublingCubeModel.DoublingCubeOwner)
				{
					return true;
				}
			}
			else if (Player2.Id.Equals(callingPlayerId)
					&& activeSession.ActivePlayer == Player2.Id
					&& allowedCommands.Contains(ServerCommands.RollCommand)
					&& activeSession.Phase == GamePhase.WaitingForRoll)
			{
				// If no double was offered yet, every player can offer it at the start of his turn and before he made his roll
				if (doublingCubeModel.DoublingCubeValue == 1)
				{
					return true;
				}
				// Afterwards only the cube owner can offer one
				// We need to invert the model for player 2 (black)
				else if (!doublingCubeModel.DoublingCubeOwner)
				{
					return true;
				}
			}
			return false;
		}
	}
}
