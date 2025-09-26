using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
	/// <summary>
	/// Provides additional services for the Fevga board game variant.
	/// </summary>
	public interface IFevgaBoardService
	{
		/// <summary>
		/// Checks if the given player passed the opponents start position.
		/// </summary>
		/// <param name="model">The board model.</param>
		/// <param name="isWhite"><c>True</c>if white checkers. Otherwise <c>false</c>.</param>
		/// <returns>Boolean indicating if the given player passed the opponents start.</returns>
		public bool HasPassedOpponentsStart(IBoardModel model, bool isWhite);
	}
}
