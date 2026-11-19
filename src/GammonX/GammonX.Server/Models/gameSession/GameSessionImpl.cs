using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models;
using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class GameSessionImpl : IGameSessionModel, ICloneable
	{
		protected IDiceService DiceService { get; private set; }

		/// <summary>
		/// Stores each played step and its die index in reverse play order for undo.
		/// </summary>
		protected Stack<(int From, int To, int DieIndex)> ActiveUndoStack { get; init; } = new();

		/// <inheritdoc />
		public IBoardService BoardService { get; protected init; }

		// <inheritdoc />
		public GameModus Modus { get; }

		// <inheritdoc />
		public Guid MatchId { get; init; }

		// <inheritdoc />
		public Guid Id { get; init; }

		// <inheritdoc />
		public GamePhase Phase { get; set; }

		// <inheritdoc />
		public GameResultModel Result { get; protected set; } = GameResultModel.Empty();

		// <inheritdoc />
		public Guid ActivePlayer { get; protected set; }

		// <inheritdoc />
		public Guid OtherPlayer { get; protected set; }

		// <inheritdoc />
		public int TurnNumber { get; protected set; } = 1;

		// <inheritdoc />
		public DiceRollsModel DiceRolls { get; protected set; }

		// <inheritdoc />
		public IBoardModel BoardModel { get; init; }

		// <inheritdoc />
		public DateTime? StartedAt { get; protected set; }

		// <inheritdoc />
		public DateTime? EndedAt { get; protected set; }

		// <inheritdoc />
		public long Duration
		{
			get
			{
				if (!StartedAt.HasValue)
					return 0;

				var end = EndedAt ?? DateTime.UtcNow;
				var elapsedTicks = (end - StartedAt.Value).Ticks;
				return Math.Max(0L, elapsedTicks / TimeSpan.TicksPerMillisecond);
			}
		}

		public GameSessionImpl(Guid matchId, GameModus modus, IBoardService boardService, IDiceService diceService)
		{
			BoardService = boardService;
			DiceService = diceService;
			BoardModel = BoardService.CreateBoard();
			MatchId = matchId;
			Modus = modus;
			Id = Guid.NewGuid();
			DiceRolls = new DiceRollsModel();
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
		public void StopGame(GameResultModel result)
		{
			Result = result;
			Phase = GamePhase.GameOver;
			if (!StartedAt.HasValue)
			{
				StartedAt = DateTime.UtcNow;
			}
			EndedAt = DateTime.UtcNow;
		}

		// <inheritdoc />
		public void NextTurn(Guid playerId)
		{
			TurnNumber++;
			OtherPlayer = ActivePlayer;
			ActivePlayer = playerId;
			DiceRolls = new DiceRollsModel();
			ActiveUndoStack.Clear();
			Phase = GamePhase.WaitingForRoll;
		}

		// <inheritdoc />
		public void SetDiceRolls(int[] rolls, bool isWhite)
		{
			if (rolls[0].Equals(rolls[1]))
			{
				// pasch rolled
				var diceRoll1 = new DiceRollContract(rolls[0]);
				var diceRoll2 = new DiceRollContract(rolls[0]);
				var diceRoll3 = new DiceRollContract(rolls[0]);
				var diceRoll4 = new DiceRollContract(rolls[0]);
				DiceRolls = new DiceRollsModel() { diceRoll1, diceRoll2, diceRoll3, diceRoll4 };
			}
			else
			{
				var diceRoll1 = new DiceRollContract(rolls[0]);
				var diceRoll2 = new DiceRollContract(rolls[1]);
				DiceRolls = new DiceRollsModel() { diceRoll1, diceRoll2 };
			}

			if (CanMove(isWhite))
			{
				Phase = GamePhase.Rolling;
			}
			else
			{
				Phase = GamePhase.WaitingForEndTurn;
			}

			rolls = DiceRolls.Select(dr => dr.Roll).ToArray();
			BoardService.AddRollEventToHistory(BoardModel, isWhite, rolls);
		}

		// <inheritdoc />
		public int[] RollDices(int amount)
		{
			return DiceService.Roll(amount, 6);
		}

		// <inheritdoc />
		public void RollDices(Guid callingPlayerId, bool isWhite)
		{
			if (ActivePlayer != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to roll the dice.");
			}
			var rolls = RollDices(2);
			SetDiceRolls(rolls, isWhite);
		}

		// <inheritdoc />
		public void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite)
		{
			if (ActivePlayer != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to move the checkers.");
			}

			var remainingRolls = DiceRolls.GetRemainingRolls();
			var sequence = BoardService.ResolveMove(BoardModel, isWhite, from, to, remainingRolls);
			if (sequence == null)
			{
				throw new InvalidOperationException($"No legal move exists from '{from}' to '{to}' with the remaining dice.");
			}

			for (var i = 0; i < sequence.Moves.Count; i++)
			{
				var die = DiceRolls.First(d => !d.Used && d.Roll == sequence.UsedDices[i]);
				ApplyMove(sequence.Moves[i], die, isWhite);
			}

			Phase = CanMove(isWhite) ? GamePhase.Moving : GamePhase.WaitingForEndTurn;
		}

		// <inheritdoc />
		public void UndoLastMove(Guid callingPlayerId, bool isWhite)
		{
			if (ActivePlayer != callingPlayerId)
			{
				throw new InvalidOperationException("It's not your turn to undo your last move.");
			}

			if (!ActiveUndoStack.TryPeek(out var lastMove))
			{
				throw new InvalidOperationException("The active player has no move in his stack to undo.");
			}

			// We first peek if the last event in the history stack is viable
			if (!BoardModel.History.TryPeekLast(out var lastEvent)
				|| lastEvent == null
				|| (lastEvent.Type != HistoryEventType.Move && lastEvent.Type != HistoryEventType.Hit))
			{
				throw new InvalidOperationException("There is no last move to undo for the active player");
			}

			// we reverse the move direction in order to undo the last move
			BoardService.UndoMove(BoardModel, new MoveModel(lastMove.From, lastMove.To), isWhite);
			DiceRolls[lastMove.DieIndex].Revert();
			ActiveUndoStack.Pop();

			Phase = CanMove(isWhite) ? GamePhase.Moving : GamePhase.WaitingForEndTurn;
		}

		// <inheritdoc />
		public bool CanUndoLastMove(Guid callingPlayerId)
		{
			if (ActivePlayer != callingPlayerId)
			{
				return false;
			}
			return ActiveUndoStack.TryPeek(out var _);
		}

		/// <inheritdoc />
		public bool CanMove(bool isWhite)
		{
			var remainingRolls = DiceRolls.GetRemainingRolls();
			return BoardService.CanUseAnyRoll(BoardModel, isWhite, remainingRolls);
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
		public EventGameStatePayload ToPayload(Guid playerId, string[] allowedCommands, bool inverted, bool includeMoves = true)
		{
			return EventGameStatePayload.ForPlayer(this, playerId, inverted, includeMoves, allowedCommands);
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
				contract.Winner = Result.WinnerId;
				contract.Points = Result.Points;
			}
			return contract;
		}

		// <inheritdoc />
		public IGameHistory GetHistory(Guid player1, Guid player2)
		{
			return GameHistoryImpl.Create(this, player1, player2, Result.WinnerId, Result.Points);
		}

		/// <summary>
		/// Sets the internal used instance of <see cref="IDiceService"/> for unit test purposes."/>
		/// </summary>
		/// <param name="diceService">Dice service mock/stub to set.</param>
		/// <exception cref="ArgumentNullException">If instance is null.</exception>
		internal void SetDiceService(IDiceService diceService)
		{
			// unit test purposes only
			DiceService = diceService ?? throw new ArgumentNullException(nameof(diceService));
		}

		// <inheritdoc />
		public virtual object Clone()
		{
			// we need to ensure the proper order of moves in the undo stack
			var undoStackClone = new Stack<(int From, int To, int DieIndex)>(ActiveUndoStack.Reverse());
			var rollsClone = new DiceRollsModel(this.DiceRolls.Select(dr => (DiceRollContract)dr.Clone()));

			var gameSession = new GameSessionImpl(this.MatchId, this.Modus, this.BoardService, this.DiceService)
			{
				Id = this.Id,
				MatchId = this.MatchId,
				ActivePlayer = this.ActivePlayer,
				BoardModel = this.BoardModel.DeepClone(),
				DiceRolls = rollsClone,
				Result = this.Result,
				Phase = this.Phase,
				EndedAt = this.EndedAt,
				StartedAt = this.StartedAt,
				OtherPlayer = this.OtherPlayer,
				TurnNumber = this.TurnNumber,
				ActiveUndoStack = undoStackClone,
			};

			return gameSession;
		}

		/// <summary>Applies a resolved step and records the exact die for undo.</summary>
		/// <param name="move">Validated checker movement.</param>
		/// <param name="die">Unused die assigned to this step.</param>
		/// <param name="isWhite">Color of the moving checkers.</param>
		private void ApplyMove(MoveModel move, DiceRollContract die, bool isWhite)
		{
			BoardService.MoveCheckerTo(BoardModel, move.From, move.To, isWhite);
			die.Use();
			ActiveUndoStack.Push((move.From, move.To, DiceRolls.IndexOf(die)));
		}
	}
}
