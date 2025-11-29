using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
	[DataContract]
	public class GameRecordContract
	{
		[DataMember(Name = "Id")]
		public Guid Id { get; set; } = Guid.Empty;

		[DataMember(Name = "PlayerId")]
		public Guid PlayerId { get; set; } = Guid.Empty;

		[DataMember(Name = "Length")]
		public int Length { get; set; } = 0;

		[DataMember(Name = "Points")]
		public int Points { get; set; } = 0;

		[DataMember(Name = "PipesLeft")]
		public int PipesLeft { get; set; } = 0;

		[DataMember(Name = "Result")]
		public GameResult Result { get; set; } = GameResult.Unknown;

		[DataMember(Name = "DoublingCubeValue")]
		public int? DoublingCubeValue { get; set; } = null;

		[DataMember(Name = "Modus")]
		public GameModus Modus { get; set; } = GameModus.Unknown;

		[DataMember(Name = "StartedAt")]
		public DateTime StartedAt { get; set; }

		[DataMember(Name = "EndedAt")]
		public DateTime EndedAt { get; set; }

		[DataMember(Name = "GameHistory")]
		public string GameHistory { get; set; } = string.Empty;

		[DataMember(Name = "Format")]
		public HistoryFormat Format { get; set; } = HistoryFormat.Unknown;
	}
}
