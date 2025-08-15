using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models.gameSession
{
	// <inheritdoc />
	public sealed class PortesGameSession : IGameSessionModel
	{
		private readonly IBoardService _boardService;

		// <inheritdoc />
		public GameModus Modus => GameModus.Portes;

		// <inheritdoc />
		public Guid MatchId { get; }

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public GamePhase Phase { get; private set; }

		// <inheritdoc />
		public Guid ActiveTurn { get; private set; }

		// <inheritdoc />
		public int TurnNumber { get; private set; }

		// <inheritdoc />
		public DiceRollsModel DiceRolls { get; private set; }

		// <inheritdoc />
		public LegalMovesModel LegalMoves { get; private set; }

		// <inheritdoc />
		public IBoardModel BoardModel { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - DateTime.UtcNow).Duration().Milliseconds;

		public PortesGameSession(Guid matchId)
		{
			_boardService = BoardServiceFactory.Create(Modus);
			BoardModel = _boardService.CreateBoard();
			MatchId = matchId;
			Id = Guid.NewGuid();
			DiceRolls = new DiceRollsModel(Array.Empty<DiceRollContract>());
			LegalMoves = new LegalMovesModel(Array.Empty<LegalMoveContract>());
		}

		// <inheritdoc />
		public void StartGame(Guid playerId)
		{
			StartedAt = DateTime.UtcNow;
			TurnNumber = 1;
			Phase = GamePhase.WaitingForRoll;
			ActiveTurn = playerId;
		}

		// <inheritdoc />
		public void StopGame()
		{
			throw new NotImplementedException();
		}

		public void NextTurn(Guid playerId)
		{
			TurnNumber++;
			ActiveTurn = playerId;
			DiceRolls = new DiceRollsModel(Array.Empty<DiceRollContract>());
			LegalMoves = new LegalMovesModel(Array.Empty<LegalMoveContract>());
		}

		// <inheritdoc />
		public GameStatePayload ToPayload(bool inverted)
		{
			if (inverted)
			{
				// for player 2
				return null;
			}
			else
			{
				// for player 1
				return null;
			}
		}		
	}
}
