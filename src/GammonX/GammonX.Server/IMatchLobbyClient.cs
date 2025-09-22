using GammonX.Server.Contracts;

using Microsoft.AspNetCore.SignalR;

namespace GammonX.Server
{
	/// <summary>
	/// Provides the capability to strongly type signalR hub methods
	/// </summary>
	public interface IMatchLobbyClient : IClientProxy
	{
		/// <summary>
		/// Mock method to verify server event calls.
		/// </summary>
		/// <param name="eventType">Server event type broadcasted.</param>
		/// <param name="response">Payload to send.</param>
		/// <param name="allowedCommands">Next comands allowed.</param>
		/// <returns>A task to be awaited.</returns>
		Task ReceiveMessage<T>(string eventType, EventResponseContract<T> response, params string[] allowedCommands) where T : EventPayloadBase;
	}
}
