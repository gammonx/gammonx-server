using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
	/// <summary>
	/// Contract providing a completed game to the simple queue service.
	/// </summary>
	[DataContract]
	public class GameRecordContract
	{
		[DataMember(Name = "Id")]
		public Guid Id { get; set; } = Guid.Empty;

		[DataMember(Name = "PlayerId")]
		public Guid PlayerId { get; set; } = Guid.Empty;

		[DataMember(Name = "PipesLeft")]
		public int PipesLeft { get; set; } = 0;

		[DataMember(Name = "Result")]
		public GameResult Result { get; set; } = GameResult.Unknown;

		[DataMember(Name = "DoublingCubeValue")]
		public int? DoublingCubeValue { get; set; } = null;

		[DataMember(Name = "GameHistory")]
		public string GameHistory { get; set; } = string.Empty;

		[DataMember(Name = "Format")]
		public HistoryFormat Format { get; set; } = HistoryFormat.Unknown;
	}
}
