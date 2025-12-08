using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
	/// <summary>
	/// Contract providing a completed match to the simple queue service.
	/// </summary>
	[DataContract]
	public class MatchRecordContract
	{
		[DataMember(Name = "Id")]
		public Guid Id { get; set; } = Guid.Empty;

		[DataMember(Name = "PlayerId")]
		public Guid PlayerId { get; set; } = Guid.Empty;

		[DataMember(Name = "Result")]
		public MatchResult Result { get; set; } = MatchResult.Unknown;

		[DataMember(Name = "Variant")]
		public MatchVariant Variant { get; set; } = MatchVariant.Unknown;

		[DataMember(Name = "Type")]
		public Enums.MatchType Type { get; set; } = Enums.MatchType.Unknown;

		[DataMember(Name = "Modus")]
		public MatchModus Modus { get; set; } = MatchModus.Unknown;

		[DataMember(Name = "MatchHistory")]
		public string MatchHistory { get; set; } = string.Empty;

		[DataMember(Name = "Format")]
		public HistoryFormat Format { get; set; } = HistoryFormat.Unknown;

		[DataMember(Name = "Games")]
		public IEnumerable<GameRecordContract> Games { get; set; } = Array.Empty<GameRecordContract>(); 
	}
}
