using GammonX.Models.Contracts;

using GammonX.Engine.Models;
using GammonX.Engine.Extensions;

using GammonX.Models.Enums;

using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventGameStatePayload : EventPayloadBase
	{
		private const bool FullMoveSequences = false;

		[DataMember(Name = "modus")]
		public GameModus Modus { get; set; }

		[DataMember(Name = "matchId")]
		public Guid MatchId { get; set; }

		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "phase")]
		public GamePhase Phase { get; set; }

		[DataMember(Name = "activeTurn")]
		public Guid ActiveTurn { get; set; }

		[DataMember(Name = "turnNumber")]
		public int TurnNumber { get; set; }

        /// <summary>
        /// Gets or sets the dice rolls of the current turn.
        /// </summary>
        [DataMember(Name = "diceRolls")]
		public DiceRollContract[] DiceRolls { get; set; }

        /// <summary>
        /// Gets or sets the recipients move hints. Direct hints contain one legal command per sequence.
        /// A waiting player receives an empty array.
        /// </summary>
		[DataMember(Name = "moveSequences")]
		public MoveSequenceModel[] MoveSequences { get; set; }

		[DataMember(Name = "boardState")]
		public BoardModelContract BoardState { get; set; }

		public EventGameStatePayload(params string[] allowedCommands) : base(allowedCommands)
		{
			DiceRolls = Array.Empty<DiceRollContract>();
			MoveSequences = Array.Empty<MoveSequenceModel>();
			BoardState = new BoardModelContract();
		}

        /// <summary>
		/// Builds the game state in the recipient's orientation and turn phase.
		/// </summary>
        /// <param name="model">Round to project.</param>
        /// <param name="playerId">Player receiving the state.</param>
        /// <param name="inverted">Whether to project the board for the second seat.</param>
        /// <param name="requiresMoves">Whether the active recipient needs legal sequences when a move is due.</param>
        /// <param name="allowedCommands">Commands available to this recipient.</param>
        /// <param name="fullMoveSequences">Generate complete turn orderings for this projection.</param>
        /// <returns>A detached game state suitable for delivery to the player.</returns>
        internal static EventGameStatePayload ForPlayer(
            IGameSessionModel model, 
			Guid playerId, 
			bool inverted, 
			bool requiresMoves, 
			string[] allowedCommands,
            bool fullMoveSequences = FullMoveSequences)
        {
            var active = model.ActivePlayer == playerId;
			var phaseFits = model.Phase is GamePhase.Rolling or GamePhase.Moving;
			// we send the legal moves in the game state if player is active, it is required and the game phase is fitting
            MoveSequences? moves = null;
            if (active && requiresMoves && phaseFits)
            {
				if (fullMoveSequences)
				{
					// we send the full blown move seqeuences
					moves = model.GetLegalMoveSequences(!inverted);
				}
				else
				{
					// we send the already resolved distinct from/to moves
					moves = model.ResolveMoveSequences(!inverted);
				}
            }

            var payload = Create(model, moves, inverted, allowedCommands);
            
			if (!active)
            {
                payload.Phase = GamePhase.WaitingForOpponent;
            }
            
			return payload;
        }

        /// <summary>
		/// Projects a position with the move list prepared for the recipient.
		/// </summary>
        /// <param name="model">Round supplying the current board and dice.</param>
        /// <param name="moveSequences">Detached sequences to publish, or <see langword="null"/> for an empty list.</param>
        /// <param name="inverted">Whether to orient the board and moves for the second seat.</param>
        /// <param name="allowedCommands">Commands available to the recipient.</param>
        /// <returns>The projected board, dice, commands, and move sequences.</returns>
        internal static EventGameStatePayload Create(
			IGameSessionModel model,
			MoveSequences? moveSequences,
			bool inverted, 
			params string[] allowedCommands)
		{
			var boardState = model.BoardModel.ToContract(inverted);
            
			// Responses may outlive the repository read; never expose stored arrays.
            if (!inverted)
            {
                boardState.Fields = boardState.Fields.ToArray();
                boardState.PinnedFields = boardState.PinnedFields?.ToArray();
            }

			return new EventGameStatePayload(allowedCommands)
			{
				Modus = model.Modus,
				MatchId = model.MatchId,
				Id = model.Id,
				Phase = model.Phase,
				ActiveTurn = model.ActivePlayer,
				TurnNumber = model.TurnNumber,
				DiceRolls = ((DiceRollsModel)model.DiceRolls.Clone()).ToArray(),
				MoveSequences = moveSequences?.ToContract(inverted, model.Modus) ?? [],
				BoardState = boardState,
			};
		}
	}
}
