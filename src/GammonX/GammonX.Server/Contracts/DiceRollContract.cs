using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class DiceRollContract : ICloneable
	{
		[DataMember(Name = "roll")]
		public int Roll { get; set; }

		[DataMember(Name = "used")]
		public bool Used { get; set; }

		public DiceRollContract(int roll)
		{
			Roll = roll;
			Used = false;
		}

		/// <summary>
		/// Marks the dice as used.
		/// </summary>
		public void Use()
		{
			Used = true;
		}

		/// <summary>
		/// Unmarks the dice as used, allowing it to be reused.
		/// </summary>
		public void Revert()
		{
			Used = false;
		}

		// <inheritdoc />
		public object Clone()
		{
			return new DiceRollContract(this.Roll)
			{
				Used = this.Used
			};
		}
	}
}
