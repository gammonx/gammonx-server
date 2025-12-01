using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public abstract class MatchSession : IMatchSessionModel
	{
		private readonly Func<IMatchSessionModel, bool> _isMatchOver;
		private readonly GameModus[] _rounds;
		private readonly IGameSessionModel[] _gameSessions;
		private readonly IGameSessionFactory _gameSessionFactory;

		protected string _lastExecutedCommand = string.Empty;

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public int GameRound { get; private set; }

		// <inheritdoc />
		public MatchVariant Variant { get; }

		// <inheritdoc />
		public MatchModus Modus { get; }

		// <inheritdoc />
		public GammonX.Models.Enums.MatchType Type { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public DateTime? EndedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - (DateTime)(EndedAt == null ? DateTime.UtcNow : EndedAt)).Duration().Milliseconds;

		// <inheritdoc />
		public MatchPlayerModel Player1 { get; private set; }

		// <inheritdoc />
		public MatchPlayerModel Player2 { get; private set; }

		public MatchSession(
			Guid id, 
			QueueKey queueKey, 
			IGameSessionFactory gameSessionFactory)
		{
			Id = id;
			GameRound = 1;
			Variant = queueKey.MatchVariant;
			Modus = queueKey.MatchModus;
			Type = queueKey.MatchType;
			_rounds = GetGameModusList(queueKey.MatchType);
			_gameSessions = new IGameSessionModel[_rounds.Length];
			_gameSessionFactory = gameSessionFactory;
			Player1 = new MatchPlayerModel(Guid.Empty, string.Empty);
			Player2 = new MatchPlayerModel(Guid.Empty, string.Empty);
			_isMatchOver = Type.GetMatchOverFunc();
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.JoinMatchCommand)]
		public void JoinSession(LobbyEntry player)
		{
			ArgumentNullException.ThrowIfNull(player.ConnectionId, nameof(player.ConnectionId));

			var valid = IsCommandCallValid(player.PlayerId, ServerCommands.JoinMatchCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.JoinMatchCommand}' is not in the list of allowed commands.");
			}

			if (Player1.Id == Guid.Empty)
			{
				Player1 = new MatchPlayerModel(player.PlayerId, player.ConnectionId);
			}
			else if (Player2.Id == Guid.Empty)
			{
				if (player.PlayerId == Player1.Id)
				{
					throw new InvalidOperationException("Player 1 cannot join as Player 2.");
				}

				Player2 = new MatchPlayerModel(player.PlayerId, player.ConnectionId);
			}
			else
			{
				throw new InvalidOperationException("Both players are already assigned to this match session.");
			}

			_lastExecutedCommand = ServerCommands.JoinMatchCommand;
		}

		// <inheritdoc />
		public bool CanStartNextGame()
		{
			var activeGameSession = GetGameSession(GameRound);

			return
				!IsMatchOver() &&
				Player1 != null &&
				Player1.NextGameAccepted &&
				Player2 != null &&
				Player2.NextGameAccepted &&
				// game 1 starts next or max the last game round is available
				(GameRound == 1 || GameRound + 1 <= _rounds.Length) &&
				(activeGameSession == null || activeGameSession.Phase == GamePhase.GameOver);
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.StartGameCommand)]
		public IGameSessionModel StartNextGame(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.StartGameCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.StartGameCommand}' is not in the list of allowed commands.");
			}

			var gameSession = GetOrCreateGameSession(GameRound);
			var otherPlayerId = GetOtherPlayerId(callingPlayerId);

			if (gameSession.Phase != GamePhase.GameOver)
			{
				if (GameRound == 1)
				{
					StartedAt = DateTime.UtcNow;
				}
				gameSession.StartGame(callingPlayerId, otherPlayerId);
				_lastExecutedCommand = ServerCommands.StartGameCommand;
				return gameSession;
			}
			else if (GameRound <= _rounds.Length)
			{
				GameRound++;
				var newSession = GetOrCreateGameSession(GameRound);
				newSession.StartGame(callingPlayerId, otherPlayerId);
				_lastExecutedCommand = ServerCommands.StartGameCommand;
				return newSession;
			}
			else
			{
				throw new InvalidOperationException($"Cannot start game for round {GameRound}, no more rounds available.");
			}
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.RollCommand)]
		public void RollDices(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.RollCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.RollCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to roll the dices.");
			}

			var isWhite = IsWhite(callingPlayerId);
			activeSession.RollDices(callingPlayerId, isWhite);
			_lastExecutedCommand = ServerCommands.RollCommand;
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.MoveCommand)]
		public bool MoveCheckers(Guid callingPlayerId, int from, int to)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.MoveCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.MoveCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to move the checkers.");
			}

			var isWhite = IsWhite(callingPlayerId);
			activeSession.MoveCheckers(callingPlayerId, from, to, isWhite);
			_lastExecutedCommand = ServerCommands.MoveCommand;

			if (GameOver(callingPlayerId, out var points))
			{
				var activePlayer = GetPlayer(callingPlayerId);
				activePlayer.Points += points;
				Player1.ActiveGameOver();
				Player2.ActiveGameOver();
				return true;
			}

			return false;
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.UndoMoveCommand)]
		public void UndoLastMove(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.UndoMoveCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.UndoMoveCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to undo the last move.");
			}

			var isWhite = IsWhite(callingPlayerId);
			activeSession.UndoLastMove(callingPlayerId, isWhite);
			_lastExecutedCommand = ServerCommands.UndoMoveCommand;
		}

		// <inheritdoc />
		public bool CanUndoLastMove(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId == activePlayerId)
			{
				return activeSession.CanUndoLastMove(callingPlayerId);
			}

			return false;
		}

		// <inheritdoc />
		public bool CanEndTurn(Guid callingPlayerId)
		{
			if (IsMatchOver())
				return false;

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			if (activeSession.Phase == GamePhase.GameOver)
				return false;

			if (activeSession.ActivePlayer == callingPlayerId)
			{
				if (activeSession.DiceRolls.HasUnused)
				{
					return !activeSession.MoveSequences.CanMove;
				}
				else
				{
					return true;
				}
			}
			return false;
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.EndTurnCommand)]
		public void EndTurn(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.EndTurnCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.EndTurnCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to end the turn.");
			}

			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			activeSession.NextTurn(otherPlayerId);
			_lastExecutedCommand = ServerCommands.EndTurnCommand;
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.ResignGameCommand)]
		public void ResignGame(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.ResignGameCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.ResignGameCommand}' is not in the list of allowed commands.");
			}

			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			// Other player gets the points for his score
			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			var otherPlayer = GetPlayer(otherPlayerId);
			var gamePoints = CalculateResignGamePoints();
			otherPlayer.Points += gamePoints;
			activeSession.StopGame(otherPlayerId, gamePoints);
			Player1.ActiveGameOver();
			Player2.ActiveGameOver();
			_lastExecutedCommand = ServerCommands.ResignGameCommand;
		}

		// <inheritdoc />
		[ServerCommand(ServerCommands.ResignMatchCommand)]
		public void ResignMatch(Guid callingPlayerId)
		{
			var valid = IsCommandCallValid(callingPlayerId, ServerCommands.ResignMatchCommand);
			if (!valid)
			{
				throw new InvalidOperationException($"The given command '{ServerCommands.ResignMatchCommand}' is not in the list of allowed commands.");
			}

			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			var otherPlayer = GetPlayer(otherPlayerId);

			// get the score for all unplayed/unfinished game rounds
			var scoreToAdd = 0;
			for (int i = 0; i < _gameSessions.Length; i++)
			{
				var gameSession = _gameSessions[i];
				if (gameSession == null || gameSession.Phase != GamePhase.GameOver)
				{
					var gameScore = CalculateResignGamePoints();
					gameSession ??= GetOrCreateGameSession(i + 1);
					gameSession.StopGame(otherPlayerId, gameScore);
					scoreToAdd += gameScore;
				}
			}

			Player1.ActiveGameOver();
			Player2.ActiveGameOver();
			GameRound = _gameSessions.Length;
			// Other player gets the points for his score
			otherPlayer.Points += scoreToAdd;
			_lastExecutedCommand = ServerCommands.ResignMatchCommand;
		}

		// <inheritdoc />
		public bool IsMatchOver()
		{
			var matchOver = _isMatchOver(this);
			if (matchOver)
			{
				EndedAt = DateTime.UtcNow;
			}
			return matchOver;
		}

		// <inheritdoc />
		public int PointsAway(Guid callingPlayerId)
		{
			var maxPoints = Type.GetMaxPoints();
			var player = GetPlayer(callingPlayerId);
			return maxPoints - player.Points;
		}

		// <inheritdoc />
		public GameModus GetGameModus()
		{
			return _rounds[GameRound - 1];
		}

		// <inheritdoc />
		public virtual EventGameStatePayload GetGameState(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var allowedCommands = ServerCommands.GetAllowedCommands(this, callingPlayerId, _lastExecutedCommand);
			if (Player1.Id.Equals(callingPlayerId))
			{
				return activeSession.ToPayload(callingPlayerId, allowedCommands, false);
			}
			else if (Player2.Id.Equals(callingPlayerId))
			{
				// invert for player 2
				return activeSession.ToPayload(callingPlayerId, allowedCommands, true);
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		// <inheritdoc />
		public IMatchHistory GetHistory()
		{
			return MatchHistoryImpl.Create(this);
		}

		// <inheritdoc />
		public EventMatchStatePayload ToPayload(Guid callingPlayerId)
		{
			var allowedCommands = ServerCommands.GetAllowedCommands(this, callingPlayerId, _lastExecutedCommand);
			var payload = new EventMatchStatePayload()
			{
				Id = Id,
				Player1 = Player1.ToContract(),
				Player2 = Player2.ToContract(),
				GameRound = GameRound,
				GameRounds = GetGameRoundContracts(),
				Variant = Variant,
				Modus = Modus,
				Type = Type,
				AllowedCommands = allowedCommands,
				Winner = null,
				WinnerPoints = null,
				Loser = null,
				LoserPoints = null
			};

			if (_isMatchOver.Invoke(this))
			{
				payload.Winner = Player1.Points > Player2.Points ? Player1.Id : Player2.Id;
				payload.WinnerPoints = Player1.Points > Player2.Points ? Player1.Points : Player2.Points;
				payload.Loser = Player1.Points < Player2.Points ? Player1.Id : Player2.Id;
				payload.LoserPoints = Player1.Points < Player2.Points ? Player1.Points : Player2.Points;
			}

			return payload;
		}

		// <inheritdoc />
		public IGameSessionModel? GetGameSession(int gameRound)
		{
			var targetIndex = gameRound - 1;
            if (targetIndex < _gameSessions.Length)
            {
                var existingSession = _gameSessions[targetIndex];
                return existingSession;
            }
			return null;
		}

		// <inheritdoc />
		public IGameSessionModel[] GetGameSessions()
		{
			return _gameSessions;
		}

		#region Abstract Methods

		/// <summary>
		/// Calculates the game score for the winning player given by <paramref name="playerId"/>.
		/// </summary>
		/// <param name="playerId">Player who won the game round.</param>
		/// <returns>Amount of points/score.</returns>
		protected abstract int CalculatePoints(Guid playerId);

		/// <summary>
		/// Calculates the amout of points for the score of the non-resigning player.
		/// </summary>
		/// <returns>Amount of points/score.</returns>
		protected abstract int CalculateResignGamePoints();

		/// <summary>
		/// Gets the amount and order of the max playable game modus
		/// </summary>
		/// <param name="matchType">Match type which determines the game modus played.</param>
		/// <returns>A ordered list of game modus played in this match.</returns>
		protected abstract GameModus[] GetGameModusList(MatchType matchType);

		#endregion Abstract Methods

		#region Protected Methods

		protected virtual bool IsCommandCallValid(Guid callingPlayerId, string calledCommand)
		{
			var availableCommands = ServerCommands.GetAllowedCommands(this, callingPlayerId, _lastExecutedCommand);
			if (availableCommands.Contains(calledCommand))
			{
				return true;
			}
			return false;
		}

		protected bool GameOver(Guid playerId, out int points)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			points = 0;
			var isWhite = IsWhite(playerId);
			if (!activeSession.GameOver(isWhite))
				return false;
			
			if (Player1.Id.Equals(playerId))
			{
				points = CalculatePoints(playerId);
				activeSession.StopGame(playerId, points);
				return true;
			}
			else if (Player2.Id.Equals(playerId))
			{
				points = CalculatePoints(playerId);
				activeSession.StopGame(playerId, points);
				return true;
			}
			else
			{
				throw new InvalidOperationException("Player is not part of this match session.");
			}
		}

		protected Guid GetOtherPlayerId(Guid playerId)
		{
			if (Player1.Id.Equals(playerId))
			{
				return Player2.Id;
			}
			else if (Player2.Id.Equals(playerId))
			{
				return Player1.Id;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}

		protected MatchPlayerModel GetPlayer(Guid playerId)
		{
			if (Player1.Id.Equals(playerId))
			{
				return Player1;
			}
			else if (Player2.Id.Equals(playerId))
			{
				return Player2;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}

		protected bool IsWhite(Guid playerId)
		{
			// player 1 plays always with white checkers
			if (Player1.Id.Equals(playerId))
			{
				return true;
			}
			// player 2 plays always with black checkers
			else if (Player2.Id.Equals(playerId))
			{
				return false;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}

		#endregion

		#region Private Methods

		private IGameSessionModel GetOrCreateGameSession(int round)
		{
			var existingSession = GetGameSession(round);
			if (existingSession == null)
			{
				var activeModus = GetGameModus();
				var newSession = _gameSessionFactory.Create(Id, activeModus);
				_gameSessions[round - 1] = newSession;
				return newSession;
			}
			return existingSession;
		}

		private GameRoundContract[] GetGameRoundContracts()
		{
			var gameRoundContracts = new List<GameRoundContract>();
			for (int gameRoundIndex = 0; gameRoundIndex < _gameSessions.Length; gameRoundIndex++)
			{
				var session = _gameSessions[gameRoundIndex];
				// if game round is already started or played
				if (session != null)
				{
					var contract = session.ToContract(gameRoundIndex);
					gameRoundContracts.Add(contract);
				}
			}
			return gameRoundContracts.ToArray();
		}

		#endregion Private Methods
	}
}
