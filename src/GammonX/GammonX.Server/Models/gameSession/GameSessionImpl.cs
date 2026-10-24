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

		protected Stack<MoveModel> ActiveUndoStack { get; init; } = new();

        protected IBoardService BoardService { get; init; }

        // <inheritdoc />
        public GameModus Modus { get; private set; }

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
		public MoveSequences MoveSequences { get; protected set; }

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
		public void StopGame(GameResultModel result)
		{
			Result = result;
			Phase = GamePhase.GameOver;
			EndedAt = DateTime.UtcNow;
		}

		// <inheritdoc />
		public void NextTurn(Guid playerId)
		{
			TurnNumber++;
			OtherPlayer = ActivePlayer;
			ActivePlayer = playerId;
			DiceRolls = new DiceRollsModel();
			MoveSequences = new MoveSequences();
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

            rolls = DiceRolls.Select(dr => dr.Roll).ToArray();
            CalculateLegalMoveSequences(isWhite, rolls);

            if (MoveSequences.CanMove)
            {
                Phase = GamePhase.Rolling;
            }
            else
            {
                Phase = GamePhase.WaitingForEndTurn;
            }

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

            if (MoveSequences.TryUseMove(from, to, out var playedMoves))
            {
                foreach (var move in playedMoves)
                {
                    if (!DiceRolls.TryUseDice(BoardModel, move.From, move.To))
                    {
                        throw new InvalidOperationException($"No unused dice roll for the submove '{move.From}'/'{move.To}' of client move '{from}'/'{to}' left");
                    }
                }

                if (playedMoves == null || playedMoves.Count == 0)
                {
                    throw new InvalidOperationException($"No legal move exists for from '{from}' to '{to}'.");
                }

                foreach (var move in playedMoves)
                {
                    BoardService.MoveCheckerTo(BoardModel, move.From, move.To, isWhite);
                    ActiveUndoStack.Push(move);
                }

                // we recalculate the remaining move for the given unused dices
                var remainingRolls = DiceRolls.GetRemainingRolls();
                CalculateLegalMoveSequences(isWhite, remainingRolls);

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
			BoardService.UndoMove(BoardModel, lastMove, isWhite);
			var roll = DiceRollsModel.GetMoveDistance(BoardModel, lastMove.From, lastMove.To, out var bearOffMove);
			DiceRolls.UndoDiceRoll(roll, bearOffMove);
			ActiveUndoStack.Pop();
			var remainingRolls = DiceRolls.GetRemainingRolls();
			CalculateLegalMoveSequences(isWhite, remainingRolls);

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

		// <inheritdoc />
		public bool CanUndoLastMove(Guid callingPlayerId)
		{
			if (ActivePlayer != callingPlayerId)
			{
				return false;
			}
			return ActiveUndoStack.TryPeek(out var _);
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
		public EventGameStatePayload ToPayload(Guid playerId, string[] allowedCommands, bool inverted)
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

		private void CalculateLegalMoveSequences(bool isWhite, int[] rolls)
		{
			var legalMoves = BoardService.GetLegalMoveSequences(BoardModel, isWhite, rolls);
			MoveSequences = new MoveSequences();
			MoveSequences.AddRange(legalMoves);
		}

		// <inheritdoc />
        public virtual object Clone()
        {
			var moveSeqClone = this.MoveSequences.Select(ms => ms.DeepClone()).ToList();
			// we need to ensure the proper order of moves in the undo stack
            var undoStackClone = new Stack<MoveModel>(ActiveUndoStack.Select(m => (MoveModel)m.Clone()).Reverse());
			var rollsClone = new DiceRollsModel(this.DiceRolls.Select(dr => (DiceRollContract)dr.Clone()));

			var gameSession = new GameSessionImpl(this.MatchId, this.Modus, this.BoardService, this.DiceService)
			{
				Id = this.Id,
				MatchId = this.MatchId,
				ActivePlayer = this.ActivePlayer,
				BoardModel = this.BoardModel.DeepClone(),
				DiceRolls = rollsClone,
				MoveSequences = new MoveSequences(moveSeqClone),
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
    }
}
