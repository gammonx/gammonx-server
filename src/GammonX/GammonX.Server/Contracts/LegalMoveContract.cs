using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class LegalMoveContract
	{
		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public int From { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		[DataMember] 
		public int To { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		[IgnoreDataMember]
		public bool Used { get; set; }

		public LegalMoveContract(int from, int to)
		{
			From = from;
			To = to;
			Used = false;
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
