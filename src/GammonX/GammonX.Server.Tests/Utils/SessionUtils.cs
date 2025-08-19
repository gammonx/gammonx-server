using GammonX.Engine.Services;

using GammonX.Server.Models;

namespace GammonX.Server.Tests.Utils
{
	internal static class SessionUtils
	{
		public static void InjectDiceServiceMock(this IGameSessionModel model, IDiceService diceService)
		{
			if (model is GameSessionImpl impl)
			{
				impl.SetDiceService(diceService);
			}
		}
	}
}
