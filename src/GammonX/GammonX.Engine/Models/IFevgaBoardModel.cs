namespace GammonX.Engine.Models
{
	/// <summary>
	/// Provides additional capabilities specific to the fevga board.
	/// </summary>
	public interface IFevgaBoardModel
	{
		/// <summary>
		/// Inverts the board vertically and returns it.
		/// </summary>
		/// <remarks>
		/// Only inverts the board fields vertically. Black stays black and white stays white.
		/// Inverts index 12 (black start) to index 23.
		/// Inverts index 0 (white start) to index 11.
		/// </remarks>
		/// <returns>Vertically inverted board model.</returns>
		public IBoardModel InvertBoardVertically();
	}
}
