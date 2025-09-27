namespace GammonX.Engine.History
{
	/// <summary>
	/// Provides the capabilities to handle different kind of game history event values.
	/// </summary>
	public interface IHistoryEventValue
	{
		/// <summary>
		/// Gets the value of the history event.
		/// </summary>
		/// <returns>Returns the value.</returns>
		object GetValue();
	}
}
