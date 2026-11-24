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
		private PlakotoGameSession(PlakotoGameSession source) : base(source)
		{
			// pass
		}

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
        public override object Clone() => new PlakotoGameSession(this);
	}
}
