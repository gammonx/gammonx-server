using GammonX.Models.Enums;
using GammonX.Models.History.MAT;

namespace GammonX.Models.History
{
	public static class HistoryParserFactory
	{
		public static IGameHistoryParser Create(HistoryFormat format)
		{
			switch (format)
			{
				case HistoryFormat.MAT:
					return new MatParser();
				case HistoryFormat.Unknown:
				default:
					throw new InvalidOperationException($"The given format '{format}' is unknown");
			}
		}
	}
}
