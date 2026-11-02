using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// Provides additional capabilities for a plakoto game session.
	/// </summary>
	public sealed class PlakotoGameSession : GameSessionImpl
	{
		public PlakotoGameSession(Guid matchId, GameModus modus, IBoardService boardService, IDiceService diceService)
			: base(matchId, modus, boardService, diceService)
		{
			// pass
		}

		// <inheritdoc />
		public override bool GameOver(bool isWhite)
		{
			if (BoardModel is IPinModel pinModel && pinModel.BothMothersArePinned)
			{
				return true;
			}

			return base.GameOver(isWhite);
		}

		// <inheritdoc />
        public override object Clone()
        {
            var moveSeqClone = this.MoveSequences.Select(ms => ms.DeepClone()).ToList();
            // we need to ensure the proper order of moves in the undo stack
            var undoStackClone = new Stack<MoveModel>(ActiveUndoStack.Select(m => (MoveModel)m.Clone()).Reverse());
			var rollsClone = new DiceRollsModel(this.DiceRolls.Select(dr => (DiceRollContract)dr.Clone()));

			var gameSession = new PlakotoGameSession(this.MatchId, this.Modus, this.BoardService, this.DiceService)
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
