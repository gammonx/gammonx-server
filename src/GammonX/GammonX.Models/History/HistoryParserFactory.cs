using GammonX.Models.Enums;
using GammonX.Models.History.MAT;

namespace GammonX.Models.History
{
	public static class HistoryParserFactory
	{
		public static T Create<T>(HistoryFormat format) where T : IHistoryParser
		{
			switch (format)
			{
				case HistoryFormat.MAT:
					return (T)(IHistoryParser)new MATParser();
				case HistoryFormat.Unknown:
				default:
					throw new InvalidOperationException($"The given format '{format}' is unknown");
			}
		}
	}
}
