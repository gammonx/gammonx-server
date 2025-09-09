using System.Runtime.Serialization;

namespace GammonX.Server.Bot
{
	/// <summary>
	/// Contains the list of all legal moves.
	/// </summary>
	[DataContract]
	public class GetMoveResponse
	{
		/// <summary>
		/// The array is ordered by match equity. First move is the best one. 
		/// If moving checkers is not possible, the array contains exactly one move and the play array is empty.
		/// </summary>
		[DataMember(Name = "moves")]
		public List<Move> LegalMoves { get; set; } = new();
	}

	[DataContract]
	public class Move
	{
		/// <summary>
		/// Contains 0 to 4 elements for moving a single checker. 
		/// If no move is possible because everything is blocked, the array is empty. 
		/// If the dice are different, the array contains up to 2 elements. If the dice are identical (double roll), 
		/// the array contains up to 4 elements.
		/// </summary>
		[DataMember(Name ="play")]
		public List<Play> LegalPlays { get; set; } = new();

		/// <summary>
		/// Gets the win/lose probabilities for the move given in <see cref="LegalPlays"/>
		/// </summary>
		[DataMember(Name = "probabilities")]
		public Probabilities PlayProbabilities { get; set; } = new();
	}

	[DataContract]
	public class Play
	{
		/// <summary>
		/// Gets the from position index. Values 1 - 25.
		/// </summary>
		/// <remarks>
		/// The bar is represented by 25.
		/// </remarks>
		[DataMember(Name = "from")]
		public int From { get; set; }

		/// <summary>
		/// Gets the to position index. Values 0 - 24.
		/// </summary>
		/// <remarks>
		/// bear off is represented by 0
		/// </remarks>
		[DataMember(Name = "to")]
		public int To { get; set; }
	}

	[DataContract]
	public class Probabilities
	{
		/// <summary>
		/// Probability to lose backgammon
		/// </summary>
		[DataMember(Name = "loseBg")]
		public double LoseBackGammon { get; set; }

		/// <summary>
		/// Probability to lose gammon or backgammon
		/// </summary>
		[DataMember(Name = "loseG")]
		public double LoseGammon { get; set; }

		/// <summary>
		/// Probability to win normal, gammon or backgammon.
		/// </summary>
		[DataMember(Name = "win")]
		public double Win { get; set; }

		/// <summary>
		/// Probability to lose normal, gammon or backgammon.
		/// </summary>
		[IgnoreDataMember]
		public double Lose => 1 - Win;

		/// <summary>
		/// Probability to win backgammon
		/// </summary>
		[DataMember(Name = "winBg")]
		public double WinBackGammon { get; set; }

		/// <summary>
		/// Probability to win gammon or backgammon
		/// </summary>
		[DataMember(Name = "winG")]
		public double WinGammon { get; set; }
	}
}
