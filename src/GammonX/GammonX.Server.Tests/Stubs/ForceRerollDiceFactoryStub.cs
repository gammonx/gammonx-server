using GammonX.Engine.Services;

namespace GammonX.Server.Tests.Stubs
{
    internal class ForceRerollDiceFactoryStub : IDiceServiceFactory
    {
        public IDiceService Create(DiceServiceType type)
        {
            return new ForceRerollDiceServiceStub();
        }
    }

    internal class ForceRerollDiceServiceStub : IDiceService
    {
        private static int _rollCount = 0;

        public int[] Roll(int numberOfDice, int sidesPerDie)
        {
            // we make sure that the first opening dice rolls are equal for both players in order to force a reroll.
            if (_rollCount == 0)
            {
                _rollCount++;
                return [5];
            }
            else if (_rollCount == 1)
            {
                _rollCount++;
                return [5];
            }
            // we make sure that always the player1 starts in order to be predictable for the unit/integration tests.
            else if (_rollCount == 2)
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
