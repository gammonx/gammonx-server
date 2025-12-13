using System.Security.Cryptography;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Cryptographically secure dice service.
    /// <para>
    /// RandomNumberGenerator pulls randomness backed by the systems CSPRNG:
    /// On Linux equals to /dev/urandom(ChaCha20 or similar)
    /// On Windows equals to CNG(CryptGenRandom equivalent)
    /// These are cryptographically secure pseudorandom number generators(CSPRNGs).
    /// Designed to:
    /// <list type="bullet">
    /// <c>produce non-deterministic output. No patterns can be detected in the output</c>
    /// <c>be resilient against reverse engineering attacks</c>
    /// <c>high quality entropy from OS kernel sources (timing jitter, hardware RNG, interrupts, etc.)</c>
    /// <c>the implementation is secure against timing attacks</c>
    /// </list>
    /// </para>
    /// </summary
    internal class CryptoDiceService : IDiceService
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        // <inheritdoc />
        public int[] Roll(int numberOfDice, int sidesPerDie)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numberOfDice, 1, nameof(numberOfDice));
            // only 6-sided dice are supported in this implementation
            ArgumentOutOfRangeException.ThrowIfNotEqual(sidesPerDie, 6, nameof(sidesPerDie));
            var result = new int[numberOfDice];
            for (var i = 0; i < numberOfDice; i++)
            {
                result[i] = RollSingleDie();
            }
            return result;
        }

        private static int RollSingleDie()
        {
            Span<byte> buffer = stackalloc byte[1];

            while (true)
            {
                Rng.GetBytes(buffer);
                byte randByte = buffer[0];

                if (randByte < 252)
                    return (randByte % 6) + 1;
            }
        }
    }
}
