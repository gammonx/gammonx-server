using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Tests.Testdata
{
	public static class RandomStandardBoardTestData
	{
		public static IEnumerable<object[]> GetRandomBoards()
		{
			var variantModeTuple = new[] { 
				new Tuple<WellKnownMatchVariant, GameModus>(WellKnownMatchVariant.Backgammon, GameModus.Backgammon),
				new Tuple<WellKnownMatchVariant, GameModus>(WellKnownMatchVariant.Backgammon, GameModus.Backgammon),
				new Tuple<WellKnownMatchVariant, GameModus>(WellKnownMatchVariant.Tavla, GameModus.Tavla) };

			foreach (var mode in variantModeTuple)
			{
				for (int i = 0; i < 10; i++)
				{
					var fields = RandomBoardGenerator.GenerateRandomFields(
						24, 15, 15,
						out int bearOffBlack,
						out int bearOffWhite,
						out int barBlack,
						out int barWhite
					);

					yield return new object[]
					{
						mode, fields, bearOffBlack, bearOffWhite, barBlack, barWhite
					};
				}
			}
		}
	}
}
