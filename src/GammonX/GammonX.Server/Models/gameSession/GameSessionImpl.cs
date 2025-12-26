using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public class GameSessionImpl : IGameSessionModel
	{
		private readonly IBoardService _boardService;
		private IDiceService _diceService;

		private readonly Stack<MoveModel> _activeUndoStack = new();

		// <inheritdoc />
		public GameModus Modus { get; private set; }

		// <inheritdoc />
		public Guid MatchId { get; }

		// <inheritdoc />
		public Guid Id { get; }

		// <inheritdoc />
		public GamePhase Phase { get; set; }

        // <inheritdoc />
        public GameResultModel Result { get; protected set; } = GameResultModel.Empty();

		// <inheritdoc />
		public Guid ActivePlayer { get; protected set; }

		// <inheritdoc />
		public Guid OtherPlayer { get; protected set; }

		// <inheritdoc />
		public int TurnNumber { get; private set; } = 1;

		// <inheritdoc />
		public DiceRollsModel DiceRolls { get; private set; }

		// <inheritdoc />
		public MoveSequences MoveSequences { get; private set; }

		// <inheritdoc />
		public IBoardModel BoardModel { get; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; } = DateTime.MinValue;

        // <inheritdoc />
        public DateTime EndedAt { get; private set; } = DateTime.MaxValue;

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
			_activeUndoStack.Clear();
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

            _boardService.AddEventToHistory(BoardModel, isWhite, rolls);
        }

        // <inheritdoc />
        public int[] RollDices(int amount)
		{
			return _diceService.Roll(amount, 6);
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
                    _activeUndoStack.Push(move);
                    _boardService.AddEventToHistory(BoardModel, isWhite, move);
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

			if (_activeUndoStack.TryPop(out var lastMove))
			{
				// we reverse the move direction in order to undo the last move
				_boardService.UndoMove(BoardModel, lastMove, isWhite);
				if (BoardModel.History.TryPeekLast(out var lastEvent))
				{
					if (lastEvent != null && lastEvent.Type == HistoryEventType.Move && BoardModel.History.TryRemoveLast())
					{
						var roll = DiceRollsModel.GetMoveDistance(BoardModel, lastMove.From, lastMove.To, out var _);
						DiceRolls.UndoDiceRoll(roll);
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

						return;
					}
				}

				throw new InvalidOperationException("There is no last move to undo for the active player");
			}
			else
			{
				throw new InvalidOperationException("The active player has no move in his stack to undo.");
			}
		}

		// <inheritdoc />
		public bool CanUndoLastMove(Guid callingPlayerId)
		{
			if (ActivePlayer != callingPlayerId)
			{
				return false;
			}
			return _activeUndoStack.TryPeek(out var _);
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
			_diceService = diceService ?? throw new ArgumentNullException(nameof(diceService));
		}

		private void CalculateLegalMoveSequences(bool isWhite, int[] rolls)
		{
			var legalMoves = _boardService.GetLegalMoveSequences(BoardModel, isWhite, rolls);
			MoveSequences = new MoveSequences();
			MoveSequences.AddRange(legalMoves);
		}
	}
}
