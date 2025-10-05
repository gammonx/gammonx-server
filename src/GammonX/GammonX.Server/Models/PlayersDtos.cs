namespace GammonX.Server.Models
{
	/// <summary>
	/// REST request for creating a player in the server backend.
	/// </summary>
	/// <param name="Id">Id of the given external user management (e.g. cognito).</param>
	/// <param name="UserName">Username of the player to create.</param>
	public record CreateRequest(Guid Id, string UserName);
}
