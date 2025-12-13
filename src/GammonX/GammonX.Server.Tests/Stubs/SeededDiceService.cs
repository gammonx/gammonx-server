using GammonX.Engine.Services;

namespace GammonX.Server.Tests.Stubs
{
	internal class SeededDiceService : IDiceService
	{
		private readonly int _seed;

		public SeededDiceService(int seed)
		{
			_seed = seed;
		}

		public int[] Roll(int numberOfDice, int sidesPerDie)
		{
			var rng = new Random(_seed);

			var diceRolls = new int[numberOfDice];
			for (int i = 0; i < numberOfDice; i++)
			{
				diceRolls[i] = rng.Next(1, sidesPerDie + 1);
			}

			return diceRolls;
		}
	}

	internal class SeededDiceServiceFactory : IDiceServiceFactory
	{
		private readonly int _seed;

		public SeededDiceServiceFactory(int seed)
		{
			_seed = seed;
		}

		public IDiceService Create(DiceServiceType type)
		{
			return new SeededDiceService(_seed);
		}
	}
}
