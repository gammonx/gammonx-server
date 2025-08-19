using GammonX.Engine.Models;

using GammonX.Server.Contracts;
using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class MatchSession : IMatchSessionModel
	{
		private readonly GameModus[] _rounds;
		private readonly IGameSessionModel[] _gameSessions;

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public int GameRound { get; private set; }

		// <inheritdoc />
		public WellKnownMatchVariant Variant { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - DateTime.UtcNow).Duration().Milliseconds;

		// <inheritdoc />
		public PlayerModel Player1 { get; private set; }

		// <inheritdoc />
		public PlayerModel Player2 { get; private set; }

		public MatchSession(Guid id, WellKnownMatchVariant variant, GameModus[] rounds)
		{
			Id = id;
			GameRound = 1;
			Variant = variant;
			_rounds = rounds;
			_gameSessions = new IGameSessionModel[_rounds.Length];
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
		public IGameSessionModel StartMatch()
		{
			if (GameRound > 1)
			{
				throw new InvalidOperationException("Cannot start game session for round 1, use NextRound() to get the next game session.");
			}

			StartedAt = DateTime.UtcNow;
			var gameSession = GetOrCreateGameSession(GameRound);
			gameSession.StartGame(Player1.Id);
			return gameSession;
		}

		// <inheritdoc />
		public IGameSessionModel NextGameRound()
		{
			// TODO what about single round matches? (tavla?)
			var oldSession = GetGameSession(GameRound);

			if (oldSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			oldSession.StopGame();
			GameRound++;
			var newSession = GetOrCreateGameSession(GameRound);
			// TODO Game need to be started again?
			return newSession;
		}

		// <inheritdoc />
		public void RollDices(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			var activePlayerId = activeSession.ActiveTurn;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to roll the dices.");
			}

			var isWhite = IsWhite(callingPlayerId);
			activeSession.RollDices(callingPlayerId, isWhite);
		}

		// <inheritdoc />
		public void MoveCheckers(Guid callingPlayerId, int from, int to)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			var activePlayerId = activeSession.ActiveTurn;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to move the checkers.");
			}

			var isWhite = IsWhite(callingPlayerId);
			activeSession.MoveCheckers(callingPlayerId, from, to, isWhite);

			// TODO :: check if the player has won the game
			// TODO :: assign the scores
			// TODO :: advances to the next game round if available
			// TODO :: or conclude the match session
		}

		// <inheritdoc />
		public void EndTurn(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			var activePlayerId = activeSession.ActiveTurn;

			if (callingPlayerId != activePlayerId)
			{
				throw new InvalidOperationException("It is not your turn to move the checkers.");
			}

			var otherPlayerId = GetOtherPlayerId(callingPlayerId);
			activeSession.NextTurn(otherPlayerId);
		}

		// <inheritdoc />
		public EventGameStatePayload GetGameState(Guid playerId, params string[] allowedCommands)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (Player1.Id.Equals(playerId))
			{
				if (activeSession.ActiveTurn == Player1.Id)
				{
					// only active player can send commands
					return activeSession.ToPayload(playerId, false, allowedCommands);
				}
				return activeSession.ToPayload(playerId, false, Array.Empty<string>());
			}
			else if (Player2.Id.Equals(playerId))
			{
				// invert for player 2
				if (activeSession.ActiveTurn == Player2.Id)
				{
					// only active player can send commands
					return activeSession.ToPayload(playerId, true, allowedCommands);
				}
				return activeSession.ToPayload(playerId, true, Array.Empty<string>());
			}

			throw new InvalidOperationException("Player is not part of this match session.");
		}

		// <inheritdoc />
		public GameModus GetGameModus()
		{
			return _rounds[GameRound - 1];
		}

		// <inheritdoc />
		public bool CanStartGame()
		{
			return Player1 != null && Player1.MatchAccepted && Player2 != null && Player2.MatchAccepted;
		}		

		// <inheritdoc />
		public bool CanEndTurn(Guid callingPlayerId)
		{
			var activeSession = GetGameSession(GameRound);

			if (activeSession == null)
			{
				throw new InvalidOperationException($"No game session exists for round {GameRound}.");
			}

			if (activeSession.ActiveTurn == callingPlayerId)
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
		public EventMatchStatePayload ToPayload(params string[] allowedCommands)
		{
			return new EventMatchStatePayload(Id, Player1, Player2, GameRound, Variant, allowedCommands);
		}

		private IGameSessionModel GetOrCreateGameSession(int round)
		{
			var existingSession = GetGameSession(round);
			if (existingSession == null)
			{
				var activeModus = GetGameModus();
				var newSession = GameSessionFactory.Create(Id, activeModus);
				_gameSessions[round - 1] = newSession;
				return newSession;
			}
			return existingSession;
		}

		private IGameSessionModel? GetGameSession(int round)
		{
			var existingSession = _gameSessions[round - 1];
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

		private Guid GetOtherPlayerId(Guid playerId)
		{
			if (Player1.Id.Equals(playerId))
			{
				return Player2.Id;
			}
			else
			{
				return Player1.Id;
			}
		}
	}
}
