using System.Security.Cryptography;

public static class Dice
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public static int RollSingleDie()
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

RandomNumberGenerator pulls randomness backed by the system’s CSPRNG:
On Linux → /dev/urandom (ChaCha20 or similar)
On Windows → CNG (CryptGenRandom equivalent)
These are cryptographically secure pseudorandom number generators (CSPRNGs).
Designed to:
- Output is unpredictable
- Suitable for security, cryptography, games, tokens, etc.
- High quality entropy from OS kernel sources (timing jitter, hardware RNG, interrupts, etc.)
  - Random numbers are truly unpredictable
  - No patterns can be detected in the output
  - The randomness is suitable for cryptographic purposes
  - The implementation is secure against timing attacks

  ## Backgammon Match Start Use Case

  When `exclude_doubles: true`, this function is specifically designed for Backgammon
  match initialization where:
  - First dice value determines the host player's opening move
  - Second dice value determines the guest player's opening move
  - Both players must have different opening moves (no doubles allowed)
  - Ensures fair and varied opening moves for both players


Backgammon/Portes/Tavla:
- Each player rolls one die.
- Higher die goes first.
- The two dice (yours + opponent’s) become the first move.
- If tied, reroll.

Plakoto/Fevga
- Each player rolls two dice and only the player who rolls higher total plays.
- The player who starts uses their own dice only (not both players’ dice combined).
- No reroll unless totals are identical.