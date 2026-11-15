using GammonX.Engine.Services;

namespace GammonX.Server.Tests.Stubs
{
	internal class StartDiceServiceFactoryStub : IDiceServiceFactory
	{
		// Hub instances are transient, but opening rolls belong to the same test host.
		private readonly IDiceService _diceService = new StartDiceServiceStub();

		public IDiceService Create(DiceServiceType type)
		{
			return _diceService;
		}
	}

	internal class StartDiceServiceStub : IDiceService
	{
		private int _rollCount = 0;

		public int[] Roll(int numberOfDice, int sidesPerDie)
		{
			// we make sure that always the player1 starts in order to be predictable for the unit/integration tests.
			if (_rollCount == 0) 
			{
				_rollCount++;
				return [6];
			}
			else
			{
				_rollCount = 0;
				return [1];
			}		
		}
	}
}
