namespace GammonX.Engine.Services
{
    /// <summary>
    /// Interface for a service that simulates rolling dice.
    /// </summary>
    public interface IDiceService
    {
        /// <summary>
        /// Simulates rolling a specified number of dice, each with a given number of sides.
        /// </summary>
        /// <remarks>
        /// Each element in the returned array corresponds to the result of rolling one die. The
        /// values in the array will range from 1 to <paramref name="sidesPerDie"/>.
        /// </remarks>
        /// <param name="numberOfDice">The number of dice to roll. Must be greater than 0.</param>
        /// <param name="sidesPerDie">The number of sides on each die. Must be greater than 1.</param>
        /// <returns>An array of integers representing the results of each die roll.</returns>
        int[] Roll(int numberOfDice, int sidesPerDie);
    }
}
