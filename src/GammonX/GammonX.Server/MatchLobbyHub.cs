using GammonX.Server.Contracts;
using GammonX.Server.Models;
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
		/// <param name="playerId"></param>
		/// <returns></returns>
		public async Task JoinMatch(string matchId, string playerId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");

				if (!Guid.TryParse(playerId, out var playerIdGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given playerId '{playerIdGuid}'  is not a valid GUID.");

				if (_service.TryFindMatchLobby(matchGuid, out var matchLobby) && matchLobby != null)
				{
					var matchSession = _repository.GetOrCreate(matchLobby.MatchId, matchLobby.Variant);
					var groupName = matchLobby.GroupName;
					if (matchLobby.Player1.Id == playerIdGuid)
					{
						matchLobby.Player1.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player1.ConnectionId, nameof(matchLobby.Player1.ConnectionId));
						matchSession.JoinSession(matchLobby.Player1);
						await Groups.AddToGroupAsync(matchLobby.Player1.ConnectionId, groupName);
					}

					if (matchLobby.Player2?.Id == playerIdGuid)
					{
						matchLobby.Player2.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player2.ConnectionId, nameof(matchLobby.Player2.ConnectionId));
						matchSession.JoinSession(matchLobby.Player2);
						await Groups.AddToGroupAsync(matchLobby.Player2.ConnectionId, groupName);
					}

					if (matchSession.Player1.Id != Guid.Empty && matchSession.Player2.Id != Guid.Empty)
					{
						var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchSession.Player1.Id, matchSession.Player2.Id);
						var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyPayload);
						await Clients.Group(groupName).SendAsync(ServerEventTypes.MatchLobbyFoundEvent, matchLobbyContract);
						
						var matchStatePayload = matchSession.ToPayload();
						var matchStateContract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.MatchStartedEvent, matchStatePayload);
						ArgumentNullException.ThrowIfNull(matchSession.Player1.ConnectionId, nameof(matchSession.Player1.ConnectionId));
						ArgumentNullException.ThrowIfNull(matchSession.Player2.ConnectionId, nameof(matchSession.Player2.ConnectionId));
						
						await Clients.Client(matchSession.Player1.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, matchStateContract);
						await Clients.Client(matchSession.Player2.ConnectionId).SendAsync(ServerEventTypes.MatchStartedEvent, matchStateContract);
					}
					else
					{
						var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.Id, null);
						var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyPayload);
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
					await SendErrorEventAsync("MATCH_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					if (matchSession.Player1?.ConnectionId == Context.ConnectionId)
					{
						matchSession.Player1.AcceptMatch();
					}

					if (matchSession.Player2?.ConnectionId == Context.ConnectionId)
					{
						matchSession.Player2.AcceptMatch();
					}

					if (matchSession.CanStartGame())
					{
						var gameSession = matchSession.StartMatch();
						await SendGameState(ServerEventTypes.GameStartedEvent, matchSession);
					}
					else
					{
						var matchPayload = matchSession.ToPayload();
						var matchContract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.GameWaitingEvent, matchPayload);
						await Clients.Caller.SendAsync(ServerEventTypes.GameWaitingEvent, matchContract);
					}

				}
				else
				{
					await SendErrorEventAsync("MATCH_ERROR", "No match seesion was found with the given matchId.");
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("MATCH_ERROR", $"An error occurred while trying to start the game: '{e.Message}'");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <returns></returns>
		public async Task Roll(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("ROLL_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);
					matchSession.RollDices(callingPlayerId);
					await SendGameState(ServerEventTypes.GameStateEvent,matchSession);
				}
				else
				{
					await SendErrorEventAsync("ROLL_ERROR", "No match seesion was found with the given matchId.");
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("ROLL_ERROR", $"An error occurred while trying to roll the dices: '{e.Message}'");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public async Task Move(string matchId, int from, int to)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("MOVE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.");
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);
					matchSession.MoveCheckers(callingPlayerId, from, to);
					await SendGameState(ServerEventTypes.GameStateEvent, matchSession);
				}
				else
				{
					await SendErrorEventAsync("MOVE_ERROR", "No match seesion was found with the given matchId.");
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("MOVE_ERROR", $"An error occurred while trying to move checkers: '{e.Message}'");
			}
		}

		#endregion Commands

		#region Private Members

		private async Task SendGameState(string serverEventName, IMatchSessionModel matchSession)
		{
			var gameSessionPlayer1 = matchSession.GetGameState(matchSession.Player1.Id);
			var player1Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer1);
			var gameSessionPlayer2 = matchSession.GetGameState(matchSession.Player2.Id);
			var player2Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer2);
			await Clients.Client(matchSession.Player1.ConnectionId).SendAsync(serverEventName, player1Contract);
			await Clients.Client(matchSession.Player2.ConnectionId).SendAsync(serverEventName, player2Contract);
		}
		private async Task SendErrorEventAsync(string errorCode, string message, string connectionId = null)
		{
			var payload = new EventErrorPayload(errorCode, message);
			var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);
			if (!string.IsNullOrEmpty(connectionId))
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
			var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);
			await Clients.Group(groupName).SendAsync(ServerEventTypes.MatchLobbyWaitingEvent, contract);
		}

		private Guid GetCallingPlayerId(IMatchSessionModel matchSession)
		{
			if (matchSession.Player1.ConnectionId == Context.ConnectionId)
			{
				return matchSession.Player1.Id;
			}
			if (matchSession.Player2.ConnectionId == Context.ConnectionId)
			{
				return matchSession.Player2.Id;
			}
			throw new InvalidOperationException("The calling player is not part of the match session.");
		}

		#endregion Private Members
	}
}
