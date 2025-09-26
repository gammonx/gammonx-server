using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class BoardStateContract
	{
		[DataMember(Name = "fields", IsRequired = true)]
		public int[] Fields { get; set; }

		[DataMember(Name = "bearOffCountWhite", IsRequired = true)]
		public int BearOffCountWhite { get; set; }

		[DataMember(Name = "bearOffCountBlack", IsRequired = true)]
		public int BearOffCountBlack { get; set; }

		[DataMember(Name = "pipCountWhite", IsRequired = true)]
		public int PipCountWhite { get; set; }

		[DataMember(Name = "pipCountBlack", IsRequired = true)]
		public int PipCountBlack { get; set; }

		[DataMember(Name = "pinnedFields", IsRequired = false, EmitDefaultValue = false)]
		public int[]? PinnedFields { get; set; }

		[DataMember(Name = "homeBarCountWhite", IsRequired = false, EmitDefaultValue = false)]
		public int HomeBarCountWhite { get; set; }

		[DataMember(Name = "homeBarCountBlack", IsRequired = false, EmitDefaultValue = false)]
		public int HomeBarCountBlack { get; set; }

		[DataMember(Name = "doublingCubeValue", IsRequired = false, EmitDefaultValue = false)]
		public int DoublingCubeValue { get; set; }

		[DataMember(Name = "doublingCubeOwner", IsRequired = false, EmitDefaultValue = false)]
		public bool DoublingCubeOwner { get; set; }

		public BoardStateContract()
		{
			Fields = Array.Empty<int>();
		}
	}
}
