using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	// <inheritdoc />
	[DataContract]
	public abstract class EventPayloadBase : IEventPayload
	{
		// <inheritdoc />
		[DataMember(Name = "allowedCommands", IsRequired = true, EmitDefaultValue = true)]
		public string[] AllowedCommands { get; set; }

		public EventPayloadBase(params string[] allowedCommands)
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

	// <inheritdoc />
	public sealed class EmptyEventPayload : EventPayloadBase
	{
		public EmptyEventPayload() : base(Array.Empty<string>())
		{
		}
	}

	/// <summary>
	/// Marker interface for all event payloads.
	/// </summary>
	public interface IEventPayload
	{
		/// <summary>
		/// Gets or sets the list of allowed commands for the given player after receiving this event.
		/// </summary>
		string[] AllowedCommands { get; set; }
	}
}
