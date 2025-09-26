using System.Runtime.Serialization;

namespace GammonX.Server.Bot
{
	/// <summary>
	/// The whole body of the /eval HTTP response. Contains the probabilities for this position and cube decisions.
	/// </summary>
	[DataContract]
	public class GetEvalResponse
	{
		/// <summary>
		/// Gets information about proper cube decisions.
		/// </summary>
		[DataMember(Name = "cube")]
		public CubeDecision CubeDecision { get; set; } = new();

		/// <summary>
		/// Gets the win/lose probabilities for the move given in <see cref="LegalPlays"/>
		/// </summary>
		[DataMember(Name = "probabilities")]
		public Probabilities PlayProbabilities { get; set; } = new();
	}

	/// <summary>
	/// Information about proper cube decisions.
	/// </summary>
	[DataContract]
	public class CubeDecision
	{
		/// <summary>
		/// Gets or sets a boolean if a double should be accepted or not.
		/// <c>true</c> if the opponent should take the cube, <c>false</c> if they should reject.
		/// </summary>
		[DataMember(Name = "accept")]
		public bool Accept { get; set; } = false;

		/// <summary>
		/// Gets or sets a boolean.
		/// <c>true</c> if the player active player should double, <c>false</c> if no double yet or too good.
		/// </summary>
		[DataMember(Name = "double")]
		public bool Double { get; set; } = false;
	}
}
