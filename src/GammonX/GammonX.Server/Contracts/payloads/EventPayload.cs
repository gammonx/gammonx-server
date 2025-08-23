using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// Marker base class for all event payloads.
	/// </summary>
	[DataContract]
	public abstract class EventPayload
	{
		/// <summary>
		/// Gets or sets the list of allowed commands for the given player after receiving this event.
		/// </summary>
		[DataMember(Name = "allowedCommands", IsRequired = true, EmitDefaultValue = true)]
		public string[] AllowedCommands { get; set; }

		public EventPayload(params string[] allowedCommands)
		{
			AllowedCommands = allowedCommands;
		}

		public void AppendAllowedCommands(params string[] allowedCommands)
		{
			var newAllowedCommands = AllowedCommands.ToList();
			newAllowedCommands.AddRange(allowedCommands);
			AllowedCommands = newAllowedCommands.ToArray();
		}
	}
}
