using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Contracts;
using GammonX.Server.Models.gameSession;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class GameSessionImpl : IGameSessionModel
	{
		private readonly IBoardService _boardService;
		private IDiceService _diceService;

		private int _winnerPoints;
		private Guid _winnerPlayerId;

		// <inheritdoc />
		public GameModus Modus { get; private set; }

		// <inheritdoc />
		public Guid MatchId { get; }

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public GamePhase Phase { get; set; }

		// <inheritdoc />
		public Guid ActivePlayer { get; private set; }

		// <inheritdoc />
		public Guid OtherPlayer { get; private set; }

		// <inheritdoc />
		public int TurnNumber { get; private set; } = 1;

		// <inheritdoc />
		public DiceRolls DiceRolls { get; private set; }

		// <inheritdoc />
		public MoveSequences MoveSequences { get; private set; }

		// <inheritdoc />
		public IBoardModel BoardModel { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public DateTime EndedAt { get; private set; }

		// <inheritdoc />
		public long Duration => (StartedAt - DateTime.UtcNow).Duration().Milliseconds;

		public GameSessionImpl(Guid matchId, GameModus modus, IBoardService boardService, IDiceService diceService)
		{
			_boardService = boardService;
			_diceService = diceService;
			BoardModel = _boardService.CreateBoard();
			MatchId = matchId;
			Modus = modus;
			Id = Guid.NewGuid();
			DiceRolls = new DiceRolls();
			MoveSequences = new MoveSequences();
			Phase = GamePhase.NotStarted;
		}

		// <inheritdoc />
		public void StartGame(Guid activePlayer, Guid otherPLayer)
		{
			StartedAt = DateTime.UtcNow;
			Phase = GamePhase.WaitingForRoll;
			// otherPlayer waiting for the opponent to roll and move afterwards
			OtherPlayer = otherPLayer;
			// activePlayer starts rolling the dice
			ActivePlayer = activePlayer;
		}

		// <inheritdoc />
		public void StopGame(Guid winnerPlayerId, int points)
		{
			_winnerPlayerId = winnerPlayerId;
			_winnerPoints = points;
			Phase = GamePhase.GameOver;
			EndedAt = DateTime.UtcNow;
		}

		// <inheritdoc />
		public void NextTurn(Guid playerId)
		{
			TurnNumber++;
			OtherPlayer = ActivePlayer;
			ActivePlayer = playerId;
			DiceRolls = new DiceRolls();
			MoveSequences = new MoveSequences();
			Phase = GamePhase.WaitingForRoll;
		}

		// <inheritdoc />
		public void RollDices(Guid callingPlayerId, bool isWhite)
		{
			if (ActivePlayer != callingPlayerId)
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
				DiceRolls = new DiceRolls() { diceRoll1, diceRoll2, diceRoll3, diceRoll4 };
			}
			else
			{
				var diceRoll1 = new DiceRollContract(rolls[0]);
				var diceRoll2 = new DiceRollContract(rolls[1]);
				DiceRolls = new DiceRolls() { diceRoll1, diceRoll2 };
			}

			rolls = DiceRolls.Select(dr => dr.Roll).ToArray();
			var legalMoves = _boardService.GetLegalMoveSequences(BoardModel, isWhite, rolls);
			MoveSequences = new MoveSequences();
			MoveSequences.AddRange(legalMoves);
			Phase = GamePhase.Rolling;
			AddEventToHistory(isWhite, rolls);
		}

		// <inheritdoc />
		public void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite)
		{
			if (ActivePlayer != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to move the checkers.");
			}

			if (!DiceRolls.TryUseDice(BoardModel, from, to))
			{
				throw new InvalidOperationException($"No unused dice roll for the move '{from}'/'{to}' left");
			}

			if (MoveSequences.TryUseMove(from, to, out var playedMoves))
			{
				if (playedMoves == null || playedMoves.Count == 0)
				{
					throw new InvalidOperationException($"No legal move exists for from '{from}' to '{to}'.");
				}				

				foreach (var move in playedMoves)
				{
					_boardService.MoveCheckerTo(BoardModel, move.From, move.To, isWhite);
					AddEventToHistory(isWhite, move);
				}

				// we recalculate the remaining move for the given unused dices
				var remainingRolls = DiceRolls.GetRemainingRolls();
				var legalMoves = _boardService.GetLegalMoveSequences(BoardModel, isWhite, remainingRolls);
				MoveSequences = new MoveSequences();
				MoveSequences.AddRange(legalMoves);

				// if dices left
				if (MoveSequences.CanMove)
				{
					// still dices left, so we stay in the moving phase
					Phase = GamePhase.Moving;
				}
				else
				{
					// no dices left, so we should switch to the next turn
					MoveSequences = new MoveSequences();
					Phase = GamePhase.WaitingForEndTurn;
				}
			}
			else
			{
				throw new InvalidOperationException($"No legal move exists for from '{from}' to '{to}'.");
			}
		}

		// <inheritdoc />
		public virtual bool GameOver(bool isWhite)
		{
			if (isWhite)
			{
				if (BoardModel.BearOffCountWhite == BoardModel.WinConditionCount)
				{
					return true;
				}
				return false;
			}
			else
			{
				if (BoardModel.BearOffCountBlack == BoardModel.WinConditionCount)
				{
					return true;
				}
				return false;
			}
		}

		// <inheritdoc />
		public EventGameStatePayload ToPayload(Guid playerId, bool inverted, params string[] allowedCommands)
		{
			var payload = EventGameStatePayload.Create(this, inverted, allowedCommands);

			if (ActivePlayer != playerId)
			{
				// we need to set the phase on the payload in order to
				// no disturb the active players game phase
				payload.Phase = GamePhase.WaitingForOpponent;
			}

			return payload;
		}

		// <inheritdoc />
		public GameRoundContract ToContract(int gameRoundIndex)
		{
			var contract = new GameRoundContract()
			{
				GameRoundIndex = gameRoundIndex,
				Modus = Modus,
				Phase = Phase,
			};

			if (Phase == GamePhase.GameOver)
			{
				contract.Winner = _winnerPlayerId;
				contract.Score = _winnerPoints;
			}
			return contract;
		}

		// <inheritdoc />
		public IGameHistory GetHistory()
		{
			return GameHistoryImpl.Create(this, _winnerPlayerId, _winnerPoints);
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

		private void AddEventToHistory(bool isWhite, object eventValues)
		{
			var boardHistory = BoardModel.History;

			if (eventValues is int[] rolls)
			{
				var rollEvent = HistoryEventFactory.CreateRollEvent(isWhite, rolls);
				boardHistory.Add(rollEvent);
			}
			else if (eventValues is MoveModel move)
			{
				var moveEvent = HistoryEventFactory.CreateMoveEvent(isWhite, move);
				boardHistory.Add(moveEvent);
			}
		}
	}
}
