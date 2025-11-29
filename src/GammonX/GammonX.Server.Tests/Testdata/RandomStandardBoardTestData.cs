using GammonX.Models.Enums;

namespace GammonX.Server.Tests.Testdata
{
	public static class RandomStandardBoardTestData
	{
		public static IEnumerable<object[]> GetRandomBoards()
		{
			var variantModeTuple = new[] { 
				new Tuple<MatchVariant, GameModus>(MatchVariant.Backgammon, GameModus.Backgammon),
				new Tuple<MatchVariant, GameModus>(MatchVariant.Backgammon, GameModus.Backgammon),
				new Tuple<MatchVariant, GameModus>(MatchVariant.Tavla, GameModus.Tavla) };

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
