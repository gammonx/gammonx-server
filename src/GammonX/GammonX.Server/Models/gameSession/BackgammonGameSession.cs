using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Server.Models.gameSession
{
	public interface IDoublingCubeGameSession
	{
		/// <summary>
		/// Changes the active player without any other effect (e.g. turnnumber)
		/// </summary>
		/// <param name="callingPlayerId">Player who offered the double.</param>
		void DoubleOffered(Guid callingPlayerId);

		/// <summary>
		/// Changes the active player without any other effect (e.g. turnnumber)
		/// </summary>
		/// <param name="callingPlayerId">Player who acceppted the double.</param>
		void DoubleAccepted(Guid callingPlayerId);

		/// <summary>
		/// Changes the active player without any other effect (e.g. turnnumber)
		/// </summary>
		/// <param name="callingPlayerId">Player who declined the double.</param>
		void DoubleDeclined(Guid callingPlayerId);
	}

	// <inheritdoc />
	public sealed class BackgammonGameSession : GameSessionImpl, IDoublingCubeGameSession
	{
		public BackgammonGameSession(Guid matchId, GameModus modus, IBoardService boardService, IDiceService diceService)
			: base(matchId, modus, boardService, diceService)
		{
			// pass
		}

		// <inheritdoc />
		public void DoubleAccepted(Guid callingPlayerId)
		{
			ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}

		// <inheritdoc />
		public void DoubleDeclined(Guid callingPlayerId)
		{
			ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}

		// <inheritdoc />
		public void DoubleOffered(Guid callingPlayerId)
		{
			ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}
	}
}
