using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventMatchLobbyPayload : EventPayloadBase
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "groupName")]
		public string GroupName => $"match_{Id}";

		[DataMember(Name = "player1")]
		public Guid Player1 { get; set; }

		[DataMember(Name = "player2")]
		public Guid? Player2 { get; set; }

		[DataMember(Name = "matchFound")]
		public bool MatchFound => Player2.HasValue;

		public EventMatchLobbyPayload(Guid id, Guid player1, Guid? player2, params string[] allowedCommands)
			: base(allowedCommands)
		{
			Id = id;
			Player1 = player1;
			Player2 = player2;
		}
	}
}
