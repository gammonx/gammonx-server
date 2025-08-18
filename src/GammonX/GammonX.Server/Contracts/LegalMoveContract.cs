using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class LegalMoveContract
	{
		[DataMember]
		public int From { get; private set; }

		[DataMember] 
		public int To { get; private set; }

		[IgnoreDataMember]
		public bool Used { get; set; }

		public LegalMoveContract(int from, int to)
		{
			From = from;
			To = to;
			Used = false;
		}

		/// <summary>
		/// Marks this legal move as used, so it cannot be used again.
		/// </summary>
		public void Use()
		{
			Used = true;
		}

		/// <summary>
		/// Unmarks this legal move, so it can be used again.
		/// </summary>
		public void Revert()
		{
			Used = false;
		}
	}
}
