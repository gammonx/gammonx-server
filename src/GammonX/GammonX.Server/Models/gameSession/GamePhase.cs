namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public enum GamePhase
	{
		/// <summary>
		/// 
		/// </summary>
		WaitingForRoll = 0,
		/// <summary>
		/// 
		/// </summary>
		Rolling = 1,
		/// <summary>
		/// 
		/// </summary>
		Moving = 2,
		/// <summary>
		/// 
		/// </summary>
		WaitingForOpponent = 3,
		/// <summary>
		/// 
		/// </summary>
		Finished = 4,
		/// <summary>
		/// 
		/// </summary>
		Unknown = 99
	}
}
