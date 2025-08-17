using GammonX.Engine.Models;

using GammonX.Server.Models;
using GammonX.Server.Services;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class EventGameStatePayload : EventPayload
	{
		[DataMember(Name = "modus")]
		public GameModus Modus { get; }

		[DataMember(Name = "matchId")]
		public Guid MatchId { get; }

		[DataMember(Name = "id")]
		public Guid Id { get; }

		[DataMember(Name = "phase")]
		public GamePhase Phase { get; private set; }

		[DataMember(Name = "activeTurn")]
		public Guid ActiveTurn { get; private set; }

		[DataMember(Name = "turnNumber")]
		public int TurnNumber { get; private set; }

		[DataMember(Name = "diceRolls")]
		public DiceRollContract[] DiceRolls { get; private set; }

		[DataMember(Name = "legalMoves")]
		public LegalMoveContract[] LegalMoves { get; private set; }

		[DataMember(Name = "boardState")]
		public BoardStateContract BoardModel { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="inverted"></param>
		public EventGameStatePayload(IGameSessionModel model, bool inverted)
		{
			Modus = model.Modus;
			MatchId = model.MatchId;
			Id = model.Id;
			Phase = model.Phase;
			ActiveTurn = model.ActiveTurn;
			TurnNumber = model.TurnNumber;
			DiceRolls = model.DiceRollsModel.DiceRolls;
			LegalMoves = model.LegalMovesModel.LegalMoves;
			BoardModel = model.BoardModel.ToContract(inverted);
		}
	}
}
