
namespace GammonX.Engine
{
    // <inheritdoc />
    internal class SimpleDiceService : IDiceService
    {
        // <inheritdoc />
        public int[] Roll(int numberOfDice, int sidesPerDie)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numberOfDice, 1, nameof(numberOfDice));
            ArgumentOutOfRangeException.ThrowIfLessThan(sidesPerDie, 2, nameof(sidesPerDie));

            var diceRolls = new int[numberOfDice];
            for (int i = 0; i < numberOfDice; i++)
            {
                diceRolls[i] = Random.Shared.Next(1, sidesPerDie + 1);
            }

            return diceRolls;
        }
    }
}
