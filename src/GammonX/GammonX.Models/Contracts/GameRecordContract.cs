using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracats
{
	[DataContract]
	internal class GameRecordContract
	{
		[DataMember(Name = "Id")]
		public Guid Id { get; set; } = Guid.Empty;

		[DataMember(Name = "PlayerId")]
		public Guid PlayerId { get; set; } = Guid.Empty;

		[DataMember(Name = "Points")]
		public int Points { get; set; } = 0;

		[DataMember(Name = "Won")]
		public bool Won { get; set; } = false;

		[DataMember(Name = "Mouds")]
		public GameModus Mouds { get; set; }

		[DataMember(Name = "StartedAt")]
		public DateTime StartedAt { get; set; }

		[DataMember(Name = "EndedAt")]
		public DateTime EndedAt { get; set; }
	}
}
