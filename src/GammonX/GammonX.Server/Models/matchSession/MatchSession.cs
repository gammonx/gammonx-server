using GammonX.Engine.Models;

using GammonX.Server.Contracts;
using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public abstract class MatchSession : IMatchSessionModel
	{
		private readonly GameModus[] _rounds;
		private readonly IGameSessionModel[] _gameSessions;
		private readonly IGameSessionFactory _gameSessionFactory;
		private static string[] _alwaysAvailableCommands =
			[
				ServerCommands.ResignGame,
				ServerCommands.ResignMatch,
			];

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public int GameRound { get; private set; }

		// <inheritdoc />
		public WellKnownMatchVariant Variant { get; }

		// <inheritdoc />
		public WellKnownMatchType Type { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - DateTime.UtcNow).Duration().Milliseconds;

		// <inheritdoc />
		public PlayerModel Player1 { get; private set; }

		// <inheritdoc />
		public PlayerModel Player2 { get; private set; }

		public MatchSession(
			Guid id, 
			WellKnownMatchVariant variant,
			WellKnownMatchType type,
			GameModus[] rounds, 
			IGameSessionFactory gameSessionFactory)
		{
			Id = id;
			GameRound = 1;
			Variant = variant;
			Type = type;
			_rounds = rounds;
			_gameSessions = new IGameSessionModel[_rounds.Length];
			_gameSessionFactory = gameSessionFactory;
			Player1 = new PlayerModel(Guid.Empty, string.Empty);
			Player2 = new PlayerModel(Guid.Empty, string.Empty);
		}

		// <inheritdoc />
		public void JoinSession(LobbyEntry player)
		{
			ArgumentNullException.ThrowIfNull(player.ConnectionId, nameof(player.ConnectionId));
			if (Player1.Id == Guid.Empty)
			{
				Player1 = new PlayerModel(player.PlayerId, player.ConnectionId);
			}
			else if (Player2.Id == Guid.Empty)
			{
				if (player.PlayerId == Player1.Id)
				{
					throw new InvalidOperationException("Player 1 cannot join as Player 2.");
				}

				Player2 = new PlayerModel(player.PlayerId, player.ConnectionId);
			}
			else
			{
				throw new InvalidOperationException("Both players are already assigned to this match session.");
			}
		}

		// <inheritdoc />
		public bool CanStartNextGame()
		{
			return
				Player1 != null &&
				Player1.NextGameAccepted &&
				Player2 != null &&
				Player2.NextGameAccepted &&
				// game 1 starts next or max the last game round is available
				(GameRound == 1 || GameRound + 1 <= _rounds.Length);
		}

		// <inheritdoc />
		public IGameSessionModel StartNextGame(Guid playerId)
		{
			var gameSession = GetOrCreateGameSession(GameRound);

			if (gameSession.Phase != GamePhase.GameOver)
			{
				if (GameRound == 1)
				{
					StartedAt = DateTime.UtcNow;
				}
				gameSession.StartGame(playerId);
				return gameSession;
			}
			else if (GameRound <= _rounds.Length)
			{
				GameRound++;
				var newSession = GetOrCreateGameSession(GameRound);
				newSession.StartGame(playerId);
				return newSession;
			}
			else
			{
				throw new InvalidOperationException($"Cannot start game for round {GameRound}, no more rounds available.");
			}
		}

		// <inheritdoc />
		public void RollDices(Guid callingPlayerId)
		{
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
		}

		// <inheritdoc />
		public bool MoveCheckers(Guid callingPlayerId, int from, int to)
		{
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

			if (GameOver(callingPlayerId, out var score))
			{
				var activePlayer = GetPlayer(callingPlayerId);
				activePlayer.Score += score;
				Player1.ActiveGameOver();
				Player2.ActiveGameOver();
				return true;
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
				if (activeSession.DiceRollsModel.HasUnused)
				{
					return !activeSession.LegalMovesModel.HasLegalMoves();
				}
				else
				{
					return true;
				}
			}
			return false;
		}

		// <inheritdoc />
		public void EndTurn(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			var activePlayerId = activeSession.ActivePlayer;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to move the checkers.");
			}

			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			activeSession.NextTurn(otherPlayerId);
		}

		// <inheritdoc />
		public void ResignGame(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			// Other player gets the points for his score
			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			var otherPlayer = GetPlayer(otherPlayerId);
			var gameScore = CalculateResignGameScore();
			otherPlayer.Score += gameScore;
			activeSession.StopGame(otherPlayerId, gameScore);
			Player1.ActiveGameOver();
			Player2.ActiveGameOver();
		}

		// <inheritdoc />
		public void ResignMatch(Guid callingPlayerId)
		{
			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			var otherPlayer = GetPlayer(otherPlayerId);

			// get the score for all unplayed/unfinished game rounds
			var scoreToAdd = 0;
			for (int i = 0; i < _gameSessions.Length; i++)
			{
				var gameSession = _gameSessions[i];
				if (gameSession == null || gameSession.Phase != GamePhase.GameOver)
				{
					var gameScore = CalculateResignGameScore();
					if (gameSession == null)
					{
						gameSession = GetOrCreateGameSession(i + 1);
					}
					gameSession.StopGame(otherPlayerId, gameScore);
					scoreToAdd += gameScore;
				}
			}

			Player1.ActiveGameOver();
			Player2.ActiveGameOver();
			GameRound = _gameSessions.Length;
			// Other player gets the points for his score
			otherPlayer.Score += scoreToAdd;
		}

		// <inheritdoc />
		public bool IsMatchOver()
		{
			// TODO :: support point games
			return _gameSessions.All(gs => gs?.Phase == GamePhase.GameOver);
		}

		// <inheritdoc />
		public GameModus GetGameModus()
		{
			return _rounds[GameRound - 1];
		}

		// <inheritdoc />
		public virtual EventGameStatePayload GetGameState(Guid playerId, params string[] allowedCommands)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			if (Player1.Id.Equals(playerId))
			{
				if (activeSession.ActivePlayer == Player1.Id)
				{
					// only active player can send allowed commands
					return activeSession.ToPayload(playerId, false, allowedCommands.Union(_alwaysAvailableCommands).ToArray());
				}
				return activeSession.ToPayload(playerId, false, _alwaysAvailableCommands);
			}
			else if (Player2.Id.Equals(playerId))
			{
				// invert for player 2
				if (activeSession.ActivePlayer == Player2.Id)
				{
					// only active player can send allowed commands
					return activeSession.ToPayload(playerId, true, allowedCommands.Union(_alwaysAvailableCommands).ToArray());
				}
				return activeSession.ToPayload(playerId, true, _alwaysAvailableCommands);
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		// <inheritdoc />
		public EventMatchStatePayload ToPayload(params string[] allowedCommands)
		{
			return new EventMatchStatePayload()
			{
				Id = Id,
				Player1 = Player1.ToContract(),
				Player2 = Player2.ToContract(),
				GameRound = GameRound,
				GameRounds = GetGameRoundContracts(),
				Variant = Variant,
				AllowedCommands = allowedCommands
			};
		}

		// <inheritdoc />
		public IGameSessionModel? GetGameSession(int gameRound)
		{
			var existingSession = _gameSessions[gameRound - 1];
			return existingSession;
		}

		#region Abstract Methods

		/// <summary>
		/// Calculates the game score for the winning player given by <paramref name="playerId"/>.
		/// </summary>
		/// <param name="playerId">Player who won the game round.</param>
		/// <returns>Amount of points/score.</returns>
		protected abstract int CalculateScore(Guid playerId);

		/// <summary>
		/// Calculates the amout of points for the score of the non-resigning player.
		/// </summary>
		/// <returns>Amount of points/score.</returns>
		protected abstract int CalculateResignGameScore();

		#endregion Abstract Methods

		#region Protected Methods

		protected bool GameOver(Guid playerId, out int score)
		{
			var activeSession = GetGameSession(GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");

			score = 0;
			if (Player1.Id.Equals(playerId))
			{
				if (activeSession.BoardModel.BearOffCountWhite == activeSession.BoardModel.WinConditionCount)
				{
					score = CalculateScore(playerId);
					activeSession.StopGame(playerId, score);
					return true;
				}
				return false;
			}
			else if (Player2.Id.Equals(playerId))
			{
				if (activeSession.BoardModel.BearOffCountBlack == activeSession.BoardModel.WinConditionCount)
				{
					score = CalculateScore(playerId);
					activeSession.StopGame(playerId, score);
					return true;
				}
				return false;
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

		protected PlayerModel GetPlayer(Guid playerId)
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

		private bool IsWhite(Guid playerId)
		{
			if (Player1.Id.Equals(playerId))
			{
				return true;
			}
			else if (Player2.Id.Equals(playerId))
			{
				return false;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
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
