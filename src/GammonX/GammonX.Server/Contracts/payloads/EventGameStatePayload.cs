using GammonX.Engine.Models;

using GammonX.Server.Models;
using GammonX.Server.Services;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventGameStatePayload : EventPayload
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

		[DataMember(Name = "diceRolls")]
		public DiceRollContract[] DiceRolls { get; set; }

		// TODO doc
		[DataMember(Name = "moveSequences")]
		public MoveSequenceModel[] MoveSequences { get; set; }

		[DataMember(Name = "boardState")]
		public BoardStateContract BoardState { get; set; }

		public EventGameStatePayload(params string[] allowedCommands) : base(allowedCommands)
		{
			DiceRolls = Array.Empty<DiceRollContract>();
			MoveSequences = Array.Empty<MoveSequenceModel>();
			BoardState = new BoardStateContract();
		}

		public static EventGameStatePayload Create(IGameSessionModel model, bool inverted, params string[] allowedCommands)
		{
			return new EventGameStatePayload(allowedCommands)
			{
				Modus = model.Modus,
				MatchId = model.MatchId,
				Id = model.Id,
				Phase = model.Phase,
				ActiveTurn = model.ActivePlayer,
				TurnNumber = model.TurnNumber,
				DiceRolls = model.DiceRolls.ToArray(),
				MoveSequences = model.MoveSequences.ToArray(),
				BoardState = model.BoardModel.ToContract(inverted)
			};
		}
	}
}
