using GammonX.Server.Contracts;
using GammonX.Server.Models;

namespace GammonX.Server.Extensions
{
	public static class ContractExtensions
	{
		public static PlayerContract ToContract(this MatchPlayerModel model)
		{
			return new PlayerContract()
			{
				Id = model.Id,
				Points = model.Points,
				UserName = null,
				StartDiceRoll = model.StartDiceRoll
            };
		}
	}
}
