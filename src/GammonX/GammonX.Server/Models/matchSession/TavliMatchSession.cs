using GammonX.Engine.Models;

using GammonX.Server.Contracts;
using GammonX.Server.Services;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class TavliMatchSession : IMatchSessionModel
	{
		private static readonly GameModus[] _rounds =
		[
			GameModus.Portes,
			GameModus.Plakoto,
			GameModus.Fevga
		];

		private readonly IGameSessionModel[] _gameSession = new IGameSessionModel[_rounds.Length];

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

		public TavliMatchSession(Guid id)
		{
			Id = id;
			GameRound = 1;
			Variant = WellKnownMatchVariant.Tavli;
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
			return gameSession;
		}

		// <inheritdoc />
		public IGameSessionModel NextGameRound()
		{
			var oldSession = GetOrCreateGameSession(GameRound);
			oldSession.StopGame();
			GameRound++;
			var newSession = GetOrCreateGameSession(GameRound);
			return newSession;
		}

		// <inheritdoc />
		public IGameSessionModel GetGameSession()
		{
			var activeSession = GetOrCreateGameSession(GameRound);
			return activeSession;
		}

		// <inheritdoc />
		public GameModus GetGameModus()
		{
			return _rounds[GameRound - 1];
		}

		// <inheritdoc />
		public MatchStatePayload ToPayload()
		{
			return new MatchStatePayload(Id, GameRound, Variant);
		}

		private IGameSessionModel GetOrCreateGameSession(int round)
		{
			var existingSession = _gameSession[round - 1];
			if (existingSession == null)
			{
				var activeModus = GetGameModus();
				var newSession = GameSessionFactory.Create(Id, activeModus);
				return newSession;
			}
			return existingSession;
		}
	}
}
