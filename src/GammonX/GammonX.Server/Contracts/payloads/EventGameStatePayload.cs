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
        /// Gets or sets legal move sequences for the recipient. Empty for a waiting player.
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

        internal static EventGameStatePayload Create(
			IGameSessionModel model, 
			bool inverted, 
			bool activePlayer, 
			params string[] allowedCommands)
		{
			var boardState = model.BoardModel.ToContract(inverted);
            
			// Responses may outlive the repository read; never expose stored arrays.
            if (!inverted)
            {
                boardState.Fields = boardState.Fields.ToArray();
                boardState.PinnedFields = boardState.PinnedFields?.ToArray();
            }

            // A waiting player needs board state only; avoid cloning or inverting the move list.
            var moveSequences = Array.Empty<MoveSequenceModel>();
            if (activePlayer)
            {
                moveSequences = inverted
                    ? model.MoveSequences.ToContract(true, model.Modus)
                    : model.MoveSequences.Select(sequence => sequence.DeepClone()).ToArray();
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
				MoveSequences = moveSequences,
				BoardState = boardState,
			};
		}
	}
}
