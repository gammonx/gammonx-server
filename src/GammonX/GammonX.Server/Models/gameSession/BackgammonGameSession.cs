using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Server.Models.gameSession
{
    /// <summary>
    /// Provides the capabilities for a backgammon game session, including handling of the doubling cube actions.
    /// </summary>
    public interface IDoublingCubeGameSession
	{
		/// <summary>
		/// Changes the active player without any other effect (e.g. turnnumber)
		/// </summary>
		/// <param name="callingPlayerId">Player who offered the double.</param>
		/// <param name="isWhite">Indicates if the player is white.</param>
		void DoubleOffered(Guid callingPlayerId, bool isWhite);

        /// <summary>
        /// Changes the active player without any other effect (e.g. turnnumber)
        /// </summary>
        /// <param name="callingPlayerId">Player who acceppted the double.</param>
        /// <param name="isWhite">Indicates if the player is white.</param>
        void DoubleAccepted(Guid callingPlayerId, bool isWhite);

        /// <summary>
        /// Changes the active player without any other effect (e.g. turnnumber)
        /// </summary>
        /// <param name="callingPlayerId">Player who declined the double.</param>
        /// <param name="isWhite">Indicates if the player is white.</param>
        void DoubleDeclined(Guid callingPlayerId, bool isWhite);
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
		public void DoubleAccepted(Guid callingPlayerId, bool isWhite)
		{
            BoardService.AddCubeEventToHistory(BoardModel, isWhite, CubeAction.Take);
            ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}

		// <inheritdoc />
		public void DoubleDeclined(Guid callingPlayerId, bool isWhite)
		{
            BoardService.AddCubeEventToHistory(BoardModel, isWhite, CubeAction.Pass);
            ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}

		// <inheritdoc />
		public void DoubleOffered(Guid callingPlayerId, bool isWhite)
		{
			BoardService.AddCubeEventToHistory(BoardModel, isWhite, CubeAction.Offer);
			ActivePlayer = OtherPlayer;
			OtherPlayer = callingPlayerId;
		}
	}
}
