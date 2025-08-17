using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class EventMatchLobbyPayload : EventPayload
	{
		[DataMember(Name = "matchId")]
		public Guid Id { get; private set; }

		[DataMember(Name = "groupName")]
		public string GroupName => $"match_{Id}";

		[DataMember(Name = "player1")]
		public Guid Player1 { get; private set; }

		[DataMember(Name = "player2")]
		public Guid? Player2 { get; private set; }

		[DataMember(Name = "matchFound")]
		public bool MatchFound => Player2.HasValue;

		public EventMatchLobbyPayload(Guid id, Guid player1, Guid? player2)
		{
			Id = id;
			Player1 = player1;
			Player2 = player2;
		}
	}
}
