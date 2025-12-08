namespace GammonX.Models.History
{
	/// <summary>
	/// Marker interface for a history parser for a given format.
	/// </summary>
	public interface IMatchHistoryParser : IHistoryParser
	{
		/// <summary>
		/// Parses the given <paramref name="content"/>.
		/// </summary>
		/// <param name="content">String content to parse from.</param>
		/// <returns>A parsed <see cref="IParsedMatchHistory"/> instance.</returns>
		IParsedMatchHistory ParseMatch(string content);
	}
}
