using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class GameSessionImpl : IGameSessionModel
	{
		private readonly IBoardService _boardService;
		private IDiceService _diceService;

		// <inheritdoc />
		public GameModus Modus { get; private set; }

		// <inheritdoc />
		public Guid MatchId { get; }

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public GamePhase Phase { get; set; }

		// <inheritdoc />
		public Guid ActiveTurn { get; private set; }

		// <inheritdoc />
		public int TurnNumber { get; private set; }

		// <inheritdoc />
		public DiceRollsModel DiceRollsModel { get; private set; }

		// <inheritdoc />
		public LegalMovesModel LegalMovesModel { get; private set; }

		// <inheritdoc />
		public IBoardModel BoardModel { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - DateTime.UtcNow).Duration().Milliseconds;

		public GameSessionImpl(Guid matchId, GameModus modus)
		{
			_boardService = BoardServiceFactory.Create(Modus);
			_diceService = DiceServiceFactory.Create();
			BoardModel = _boardService.CreateBoard();
			MatchId = matchId;
			Modus = modus;
			Id = Guid.NewGuid();
			DiceRollsModel = new DiceRollsModel(Array.Empty<DiceRollContract>());
			LegalMovesModel = new LegalMovesModel(Array.Empty<LegalMoveContract>());
			Phase = GamePhase.NotStarted;
		}

		// <inheritdoc />
		public void StartGame(Guid playerId)
		{
			StartedAt = DateTime.UtcNow;
			TurnNumber = 1;
			// player 1 starts rolling the dice
			// player 2 waiting for the opponent to roll and move afterwards
			Phase = GamePhase.WaitingForRoll;
			ActiveTurn = playerId;
		}

		// <inheritdoc />
		public void StopGame()
		{
			// TODO :: what happens here?
		}

		// <inheritdoc />
		public void NextTurn(Guid playerId)
		{
			TurnNumber++;
			ActiveTurn = playerId;
			DiceRollsModel = new DiceRollsModel(Array.Empty<DiceRollContract>());
			LegalMovesModel = new LegalMovesModel(Array.Empty<LegalMoveContract>());
			Phase = GamePhase.WaitingForRoll;
		}

		// <inheritdoc />
		public void RollDices(Guid callingPlayerId, bool isWhite)
		{
			if (ActiveTurn != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to roll the dice.");
			}

			var rolls = _diceService.Roll(2, 6);

			if (rolls[0].Equals(rolls[1]))
			{
				// pasch rolled
				var diceRoll1 = new DiceRollContract(rolls[0]);
				var diceRoll2 = new DiceRollContract(rolls[0]);
				var diceRoll3 = new DiceRollContract(rolls[0]);
				var diceRoll4 = new DiceRollContract(rolls[0]);
				DiceRollsModel = new DiceRollsModel(diceRoll1, diceRoll2, diceRoll3, diceRoll4);
			}
			else
			{
				var diceRoll1 = new DiceRollContract(rolls[0]);
				var diceRoll2 = new DiceRollContract(rolls[1]);
				DiceRollsModel = new DiceRollsModel(diceRoll1, diceRoll2);
			}

			var legalMoves = _boardService.GetLegalMoves(BoardModel, isWhite, rolls);
			var legalMoveContracts = legalMoves.Select(lm => new LegalMoveContract(lm.Item1, lm.Item2)).ToArray();
			LegalMovesModel = new LegalMovesModel(legalMoveContracts);

			Phase = GamePhase.Rolling;
		}

		// <inheritdoc />
		public void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite)
		{
			if (ActiveTurn != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to move the checkers.");
			}

			var legalMove = LegalMovesModel.LegalMoves.FirstOrDefault(lm => lm.From == from && lm.To == to);
			if (legalMove != null && !legalMove.Used)
			{
				var distance = Math.Abs(from - to);
				if (TryFindDiceUsage(DiceRollsModel.GetUnusedDiceRolls(), distance, out var usedDices))
				{
					_boardService.MoveCheckerTo(BoardModel, from, to, isWhite);
					legalMove.Use();
					usedDices.ForEach(ud => DiceRollsModel.UseDice(ud.Roll));

					// if dices left
					if (DiceRollsModel.HasUnused)
					{
						// still dices left, so we stay in the moving phase
						var rolls = DiceRollsModel.GetUnusedDiceRollValues();
						var legalMoves = _boardService.GetLegalMoves(BoardModel, isWhite, rolls);
						var legalMoveContracts = legalMoves.Select(lm => new LegalMoveContract(lm.Item1, lm.Item2)).ToArray();
						LegalMovesModel = new LegalMovesModel(legalMoveContracts);
						Phase = GamePhase.Moving;
					}
					else
					{
						// no dices left, so we should switch to the next turn
						LegalMovesModel = new LegalMovesModel(Array.Empty<LegalMoveContract>());
						Phase = GamePhase.WaitingForEndTurn;
					}
				}
				else
				{
					throw new InvalidOperationException($"The given move from '{from}' to '{to}' is not possible with the given dice rolls");
				}
			}
			else
			{
				throw new InvalidOperationException($"No legal move exists or was already used from '{from}' to '{to}'.");
			}
		}

		// <inheritdoc />
		public EventGameStatePayload ToPayload(Guid playerId, bool inverted, params string[] allowedCommands)
		{
			var payload = EventGameStatePayload.Create(this, inverted, allowedCommands);

			if (ActiveTurn != playerId)
			{
				// we need to set the phase on the payload in order to
				// no disturb the active players game phase
				payload.Phase = GamePhase.WaitingForOpponent;
			}

			return payload;
		}

		/// <summary>
		/// Sets the internal used instance of <see cref="IDiceService"/> for unit test purposes."/>
		/// </summary>
		/// <param name="diceService">Dice service mock/stub to set.</param>
		/// <exception cref="ArgumentNullException">If instance is null.</exception>
		internal void SetDiceService(IDiceService diceService)
		{
			// unit test purposes only
			_diceService = diceService ?? throw new ArgumentNullException(nameof(diceService));
		}

		private bool TryFindDiceUsage(DiceRollContract[] unusedDices, int distance, out List<DiceRollContract> usedDices)
		{
			// TODO :: highest roll has to be used first
			usedDices = new List<DiceRollContract>();
			return Backtrack(unusedDices, distance, 0, new List<DiceRollContract>(), out usedDices);
		}

		private bool Backtrack(
			DiceRollContract[] dices,
			int distance,
			int start,
			List<DiceRollContract> current,
			out List<DiceRollContract> result)
		{
			// Algorithmus (Subset-Sum auf Dices)
			if (distance == 0)
			{
				result = new List<DiceRollContract>(current);
				return true;
			}

			for (int i = start; i < dices.Length; i++)
			{
				if (dices[i].Roll <= distance)
				{
					current.Add(dices[i]);
					if (Backtrack(dices.Where((_, idx) => idx != i).ToArray(), distance - dices[i].Roll, 0, current, out result))
						return true;
					current.RemoveAt(current.Count - 1);
				}
			}

			result = null!;
			return false;
		}
	}
}
