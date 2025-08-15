using GammonX.Engine.Services;
using GammonX.Server.Contracts;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;

namespace GammonX.Server
{
	/// <summary>
	/// 
	/// </summary>
	internal class MatchLobbyHub : Hub
	{
		private readonly MatchmakingService _service;
		private readonly MatchSessionRepository _repository;

		public MatchLobbyHub(MatchmakingService service, MatchSessionRepository repository)
		{
			_service = service;
			_repository = repository;
		}

		#region Overrides

		// <inheritdoc />
		public override Task OnConnectedAsync()
		{
			return base.OnConnectedAsync();
		}

		// <inheritdoc />
		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			// Optional: Spieler aus Warteschlange entfernen
			await base.OnDisconnectedAsync(exception);
		}

		#endregion Overrides

		#region Commands


		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task JoinMatch(string matchId, string clientId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");

				if (!Guid.TryParse(clientId, out var clientGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given clientId '{clientId}'  is not a valid GUID.");

				if (_service.TryFindMatchLobby(matchGuid, out var matchLobby) && matchLobby != null)
				{
					var groupName = matchLobby.GroupName;
					if (matchLobby.Player1.Id == clientGuid)
					{
						matchLobby.Player1.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player1.ConnectionId, nameof(matchLobby.Player1.ConnectionId));
						await Groups.AddToGroupAsync(matchLobby.Player1.ConnectionId, groupName);
					}

					if (matchLobby.Player2?.Id == clientGuid)
					{
						matchLobby.Player2.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player2.ConnectionId, nameof(matchLobby.Player2.ConnectionId));
						await Groups.AddToGroupAsync(matchLobby.Player2.ConnectionId, groupName);
					}

					if (matchLobby.Player1.ConnectionId != null && matchLobby.Player2?.ConnectionId != null)
					{
						var matchLobbyPayload = new MatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.Id, matchLobby.Player2.Id);
						var matchLobbyContract = new EventResponseContract(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyPayload);
						await Clients.Group(groupName).SendAsync(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyContract);
						// create a match session repository by its guid as identifier
						var matchSession = _repository.Create(matchLobby.MatchId, matchLobby.Variant);
						var matchStatePayload = matchSession.ToPayload();
						var matchStateContract = new EventResponseContract(ServerEventTypes.MatchStartedEvent, matchStatePayload);
						await Clients.Client(matchLobby.Player1.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, matchStateContract);
						await Clients.Client(matchLobby.Player2.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, matchStateContract);
					}
					else
					{
						var matchLobbyPayload = new MatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.Id, null);
						var matchLobbyContract = new EventResponseContract(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyPayload);
						await Clients.Group(groupName).SendAsync(ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyContract);
					}
				}
				else
				{
					await SendErrorEventAsync("LOBBY_ERROR", "No match lobby was found with the given matchId.");
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("LOBBY_ERROR", $"An error occurred while trying to join the match lobby: '{e.Message}'");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <returns></returns>
		public async Task StartGame(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("LOBBY_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");
				}

				// TODO move player ids into match session
				if (_service.TryFindMatchLobby(matchGuid, out var matchLobby) && matchLobby != null)
				{
					// TODO encapsulate start game in match session
					var matchSession = _repository.Get(matchGuid);
					var gameSession = matchSession.StartMatch();
					gameSession.StartGame(matchLobby.Player1.Id);
					var gameSessionPlayer1 = gameSession.ToPayload(false);
					var player1Contract = new EventResponseContract(ServerEventTypes.MatchStartedEvent, gameSessionPlayer1);
					var gameSessionPlayer2 = gameSession.ToPayload(true);
					var player2Contract = new EventResponseContract(ServerEventTypes.MatchStartedEvent, gameSessionPlayer2);
					await Clients.Client(matchLobby.Player1.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, player1Contract);
					await Clients.Client(matchLobby.Player2.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, player2Contract);
				}
				else
				{
					await SendErrorEventAsync("LOBBY_ERROR", "No match lobby was found with the given matchId.");
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("LOBBY_ERROR", $"An error occurred while trying to start the game: '{e.Message}'");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <returns></returns>
		public async Task Roll(string matchId)
		{
			var diceService = DiceServiceFactory.Create();
			var rolls = diceService.Roll(2, 6);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <returns></returns>
		public async Task Move(string matchId)
		{

		}

		#endregion Commands

		#region Private Members

		private async Task SendErrorEventAsync(string errorCode, string message, string connectionId = null)
		{
			var payload = new EventErrorPayload(errorCode, message);
			var contract = new EventResponseContract(ServerEventTypes.ErrorEvent, payload);
			if (string.IsNullOrEmpty(connectionId))
			{
				await Clients.Client(connectionId).SendAsync(ServerEventTypes.ErrorEvent, contract);
			}
			else
			{
				await Clients.Caller.SendAsync(ServerEventTypes.ErrorEvent, contract);
			}
		}

		private async Task SendErrorEventToGroupAsync(string errorCode, string message, string groupName)
		{
			var payload = new EventErrorPayload(errorCode, message);
			var contract = new EventResponseContract(ServerEventTypes.ErrorEvent, payload);
			await Clients.Group(groupName).SendAsync(ServerEventTypes.MatchLobbyWaitingEvent, contract);
		}

		#endregion Private Members
	}
}
