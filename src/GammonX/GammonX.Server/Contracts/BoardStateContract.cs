using GammonX.Engine.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class BoardStateContract
	{
		[DataMember(Name = "fields", IsRequired = true)]
		public int[] Fields { get; }

		[DataMember(Name = "bearOffCountWhite", IsRequired = true)]
		public int BearOffCountWhite { get; }

		[DataMember(Name = "bearOffCountBlack", IsRequired = true)]
		public int BearOffCountBlack { get; }

		[DataMember(Name = "pinnedFields", IsRequired = false, EmitDefaultValue = false)]
		public int[]? PinnedFields { get; }

		[DataMember(Name = "homeBarCountWhite", IsRequired = false, EmitDefaultValue = false)]
		public int HomeBarCountWhite { get; }

		[DataMember(Name = "homeBarCountBlack", IsRequired = false, EmitDefaultValue = false)]
		public int HomeBarCountBlack { get; }

		[DataMember(Name = "doublingCubeValue", IsRequired = false, EmitDefaultValue = false)]
		public int DoublingCubeValue { get; }

		[DataMember(Name = "doublingCubeOwner", IsRequired = false, EmitDefaultValue = false)]
		public bool DoublingCubeOwner { get; }

		public BoardStateContract(IBoardModel model, bool inverted)
		{
			if (inverted)
			{
				model = model.InvertBoard();
			}

			Fields = model.Fields;
			BearOffCountWhite = model.BearOffCountWhite;
			BearOffCountBlack = model.BearOffCountBlack;

			if (model is IPinModel pinModel)
			{
				PinnedFields = pinModel.PinnedFields;
			}
			else
			{
				PinnedFields = null;
			}

			if (model is IHomeBarModel homeBarModel)
			{
				HomeBarCountWhite = homeBarModel.HomeBarCountWhite;
				HomeBarCountBlack = homeBarModel.HomeBarCountBlack;
			}

			if (model is IDoublingCubeModel doublingCubeModel)
			{
				DoublingCubeValue = doublingCubeModel.DoublingCubeValue;
				DoublingCubeOwner = doublingCubeModel.DoublingCubeOwner;
			}

		}
	}
}
