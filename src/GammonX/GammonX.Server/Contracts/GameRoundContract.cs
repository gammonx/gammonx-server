using GammonX.Engine.Models;

using GammonX.Server.Models;

using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class GameRoundContract
	{
		[DataMember(Name = "gameRoundIndex", IsRequired = true, EmitDefaultValue = true)]
		public int GameRoundIndex { get; set; }

		[DataMember(Name = "modus", IsRequired = true, EmitDefaultValue = true)]
		public GameModus Modus { get; set; }

		[DataMember(Name = "phase", IsRequired = true, EmitDefaultValue = true)]
		public GamePhase Phase { get; set; }

		[DataMember(Name = "winner", IsRequired = true, EmitDefaultValue = true)]
		public Guid? Winner { get; set; }

		[DataMember(Name = "points", IsRequired = true, EmitDefaultValue = true)]
		public int? Points { get; set; }
	}
}
