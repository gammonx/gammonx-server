namespace GammonX.Engine.History
{
	/// <summary>
	/// Provides the capabilities to handle different kind of game history event values.
	/// </summary>
	/// <remarks>
	/// The history event value should be immutable.
	/// </remarks>
	public interface IHistoryEventValue
	{
		/// <summary>
		/// Gets the value of the history event.
		/// </summary>
		/// <returns>Returns the value.</returns>
		object GetValue();

		/// <summary>
		/// Creates a deep copy of the history event value.
		/// </summary>
		/// <returns>Returns a deep copy of the history event value.</returns>
		object Clone();
	}
}
