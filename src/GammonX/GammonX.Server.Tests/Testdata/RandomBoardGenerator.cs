namespace GammonX.Server.Tests.Testdata
{
	public static class RandomBoardGenerator
	{
		public static int[] GenerateRandomFields(
			int boardSize,
			int maxCheckersBlack,
			int maxCheckersWhite,
			out int bearOffBlack,
			out int bearOffWhite,
			out int barBlack,
			out int barWhite,
			int? seed = null)
		{
			var rng = seed.HasValue ? new Random(seed.Value) : new Random();

			int[] fields = new int[boardSize];

			int remainingBlack = maxCheckersBlack;
			int remainingWhite = maxCheckersWhite;

			barBlack = rng.Next(0, 2);
			barWhite = rng.Next(0, 2);
			remainingBlack -= barBlack;
			remainingWhite -= barWhite;

			bearOffBlack = rng.Next(0, 3);
			bearOffWhite = rng.Next(0, 3);
			remainingBlack -= bearOffBlack;
			remainingWhite -= bearOffWhite;

			while (remainingBlack > 0)
			{
				int pos = rng.Next(boardSize);
				int add = rng.Next(1, remainingBlack);
				if (fields[pos] < 0)
					continue;
				fields[pos] += add;
				remainingBlack -= add;
			}

			while (remainingWhite > 0)
			{
				int pos = rng.Next(boardSize);
				int add = rng.Next(1, remainingWhite);
				if (fields[pos] > 0)
					continue;
				fields[pos] -= add;
				remainingWhite -= add;
			}

			return fields;
		}
	}
}
