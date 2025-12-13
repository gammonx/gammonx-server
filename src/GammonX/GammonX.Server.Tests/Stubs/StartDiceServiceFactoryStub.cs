using GammonX.Engine.Services;

namespace GammonX.Server.Tests.Stubs
{
	internal class StartDiceServiceFactoryStub : IDiceServiceFactory
	{
		public IDiceService Create(DiceServiceType type)
		{
			return new StartDiceServiceStub();
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
