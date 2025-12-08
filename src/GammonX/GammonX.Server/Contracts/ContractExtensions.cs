using GammonX.Server.Models;

namespace GammonX.Server.Contracts
{
	public static class ContractExtensions
	{
		public static PlayerContract ToContract(this MatchPlayerModel model)
		{
			return new PlayerContract()
			{
				Id = model.Id,
				Points = model.Points,
				UserName = null
			};
		}
	}
}
