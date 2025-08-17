using GammonX.Engine.Models;

using GammonX.Server.Contracts;

namespace GammonX.Server.Services
{
	/// <summary>
	/// 
	/// </summary>
	public static class BoardModelBroker
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="inverted"></param>
		/// <returns></returns>
		public static BoardStateContract ToContract(this IBoardModel model, bool inverted)
		{
			return new BoardStateContract(model, inverted);
		}
	}
}
