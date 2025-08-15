using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class DiceRollContract
	{
		/// <summary>
		/// 
		/// </summary>
		[DataMember(Name = "roll")]
		public int Roll { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		[DataMember(Name = "used")]
		public bool Used { get; private set; }

		public DiceRollContract(int roll)
		{
			Roll = roll;
			Used = true;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Use()
		{
			Used = true;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Revert()
		{
			Used = false;
		}
	}
}
