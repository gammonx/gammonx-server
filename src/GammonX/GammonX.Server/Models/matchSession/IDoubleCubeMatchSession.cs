namespace GammonX.Server.Models
{
	/// <summary>
	/// Provides the capabilities for a match variant to play with a doubling cube
	/// </summary>
	/// <remarks>
	/// Backgammon is played for an agreed stake per point. Each game starts at one point. 
	/// During the course of the game, a player who feels he has a sufficient advantage may propose doubling the stakes. 
	/// He may do this only at the start of his own turn and before he has rolled the dice.
	/// 
	/// A player who is offered a double may refuse, in which case he concedes the game and pays one point.
	/// Otherwise, he must accept the double and play on for the new higher stakes.
	/// A player who accepts a double becomes the owner of the cube and only he may make the next double.
	/// 
	/// Subsequent doubles in the same game are called redoubles. If a player refuses a redouble, 
	/// he must pay the number of points that were at stake prior to the redouble.Otherwise, 
	/// he becomes the new owner of the cube and the game continues at twice the previous stakes.
	/// There is no limit to the number of redoubles in a game.
	/// </remarks>
	/// <see cref="Engine.Models.IDoublingCubeModel"/>
	public interface IDoubleCubeMatchSession
	{
		/// <summary>
		/// Gets a boolean indicating if a double offer is pending
		/// </summary>
		bool IsDoubleOfferPending { get; }

		/// <summary>
		/// Gets the doubling cube value.
		/// </summary>
		int GetDoublingCubeValue();

		/// <summary>
		/// The player with id <paramref name="callingPlayerId"/> offers a double to his opponent.
		/// </summary>
		/// <param name="callingPlayerId">Calling player id.</param>
		void OfferDouble(Guid callingPlayerId);

		/// <summary>
		/// The player with id <paramref name="callingPlayerId"/> was offered a double and accepted it.
		/// </summary>
		/// <remarks>
		/// The doubling cube owner switches. The player who accpeted the double can now offer it.
		/// </remarks>
		/// <param name="callingPlayerId">Calling player id.</param>
		void AcceptDouble(Guid callingPlayerId);

		/// <summary>
		/// The player with id <paramref name="callingPlayerId"/> was offered a double and declined it.
		/// The game ends and the player who offered wins and the player who declined it loses.
		/// </summary>
		/// <param name="callingPlayerId">Calling player id.</param>
		void DeclineDouble(Guid callingPlayerId);

		/// <summary>
		/// Checks if the player with id <paramref name="callingPlayerId"/> can offer a double.
		/// </summary>
		/// <param name="callingPlayerId">Calling player id.</param>
		/// <returns>True if double can be offered. Otherwise, false.</returns>
		bool CanOfferDouble(Guid callingPlayerId);
	}
}
