using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;

namespace GammonX.Server
{
	/// <summary>
	/// SignalR hub for managing match lobbies and handling the game flow.
	/// </summary>
	internal class MatchLobbyHub : Hub
	{
		private readonly SimpleMatchmakingService _service;
		private readonly MatchSessionRepository _repository;

		public MatchLobbyHub(SimpleMatchmakingService service, MatchSessionRepository repository)
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
		/// The given player with id <paramref name="playerId"/> joins the match <paramref name="matchId"/>.
		/// </summary>
		/// <remarks>
		/// First player to join the match receives the <see cref="ServerEventTypes.MatchLobbyWaitingEvent"/>.
		/// If both players joined the match, the <see cref="ServerEventTypes.MatchLobbyFoundEvent"/> is sent to both players.
		/// Afterwards, the clients also receive the <see cref="ServerEventTypes.MatchStartedEvent"/> with the initial match state.
		/// </remarks>
		/// <param name="matchId">Match to join.</param>
		/// <param name="playerId">Player who wants to join the match.</param>
		/// <returns>Task to be awaited.</returns>
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
					if (matchLobby.Player1.PlayerId == playerIdGuid)
					{
						matchLobby.Player1.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player1.ConnectionId, nameof(matchLobby.Player1.ConnectionId));
						matchSession.JoinSession(matchLobby.Player1);
						await Groups.AddToGroupAsync(matchLobby.Player1.ConnectionId, groupName);
					}

					if (matchLobby.Player2?.PlayerId == playerIdGuid)
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
						await SendMatchState(ServerEventTypes.MatchStartedEvent, matchSession, ServerCommands.StartGameCommand);
					}
					else
					{
						var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.PlayerId, null);
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
		/// Starts the first game round of the match with the given <paramref name="matchId"/>.
		/// First player to start the game receives the <see cref="ServerEventTypes.GameWaitingEvent"/> with the match state.
		/// The game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
		/// If both players accepted the match, the <see cref="ServerEventTypes.GameStartedEvent"/> is sent to both players
		/// containing the initial game state and the first player to roll his dices.
		/// </summary>
		/// <remarks>
		/// Player 1 always takes the first turn in the game.
		/// </remarks>
		/// <param name="matchId">Match containing the game to start.</param>
		/// <returns>A task to be awaited.</returns>
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
						matchSession.Player1.AcceptNextGame();
					}

					if (matchSession.Player2?.ConnectionId == Context.ConnectionId)
					{
						matchSession.Player2.AcceptNextGame();
					}

					if (matchSession.CanStartNextGame())
					{
						var gameSession = matchSession.StartNextGame();
						await SendGameState(ServerEventTypes.GameStartedEvent, matchSession, ServerCommands.RollCommand);
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
		/// The active turn player rolls the dices for the current game round. The game phase advances to <see cref="GamePhase.Rolling"/>.
		/// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 an inverted board state.
		/// </summary>
		/// <remarks>
		/// The player which is not active receives the <see cref="GamePhase.WaitingForOpponent"/> and an inverted board state.
		/// </remarks>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A task to be awaited.</returns>
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
					await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand);
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
		/// The active turn player moves his checkers from <paramref name="from"/> to <paramref name="to"/>.
		/// The game phase advances to <see cref="GamePhase.Moving"/>.
		/// If some dices are not used up, the active player can continue to move his checkers.
		/// If the given from to move uses up all rolled dices, The game phase advances to <see cref="GamePhase.WaitingForEndTurn"/>.
		/// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 an inverted board state.
		/// </summary>
		/// <remarks>
		/// An internal validation happens if the given from to move is based on the rolled dices and is actually a legal move.
		/// The player which is not active receives the <see cref="GamePhase.WaitingForOpponent"/> and an inverted board state.
		/// </remarks>
		/// <param name="matchId">Id of the match.</param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>A task to be awaited.</returns>
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
					// calling and active player must be the same
					var callingPlayerId = GetCallingPlayerId(matchSession);
					var gameOver = matchSession.MoveCheckers(callingPlayerId, from, to);
					// check if this move won the game for the active player
					if (gameOver)
					{
						// check if this was the last move to win the last game and the match
						if (matchSession.IsMatchOver())
						{
							await SendMatchState(ServerEventTypes.MatchEndedEvent, matchSession);
							// TODO :: end web socket connections for both players
						}
						else
						{
							// TODO :: maybe send game win type (norma,gammon, backgammon)
							await SendMatchState(ServerEventTypes.GameEndedEvent, matchSession, ServerCommands.StartGameCommand);
						}
					}					
					// check if the turn was finished
					else if (matchSession.CanEndTurn(callingPlayerId))
					{
						// calling player finished his turn, other player can now roll
						await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.EndTurnCommand);
					}
					else
					{
						// calling player can still move
						await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand);
					}
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

		/// <summary>
		/// The active player ends his turn. The active player is switched to the other player.
		/// The former active players game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
		/// The new active players game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
		/// Both players receive the <see cref="ServerEventTypes.GameStateEvent"/> with the game state. Player 2 an inverted board state.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A task to be awaited.</returns>
		public async Task EndTurn(string matchId)
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

					if (!matchSession.CanEndTurn(callingPlayerId))
					{
						await SendErrorEventAsync("ROLL_ERROR", "You cannot end your turn at this moment.");
						return;
					}

					matchSession.EndTurn(callingPlayerId);
					await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.RollCommand);
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

		#endregion Commands

		#region Private Members

		private async Task SendGameState(string serverEventName, IMatchSessionModel matchSession, params string[] allowedCommands)
		{
			var gameSessionPlayer1 = matchSession.GetGameState(matchSession.Player1.Id, allowedCommands);
			var player1Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer1);
			var gameSessionPlayer2 = matchSession.GetGameState(matchSession.Player2.Id, allowedCommands);
			var player2Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer2);
			await Clients.Client(matchSession.Player1.ConnectionId).SendAsync(serverEventName, player1Contract);
			await Clients.Client(matchSession.Player2.ConnectionId).SendAsync(serverEventName, player2Contract);
		}

		private async Task SendMatchState(string serverEventName, IMatchSessionModel matchSession, params string[] allowedCommands)
		{
			var matchStatePayload = matchSession.ToPayload(allowedCommands);
			var matchStateContract = new EventResponseContract<EventMatchStatePayload>(serverEventName, matchStatePayload);
			await Clients.Client(matchSession.Player1.ConnectionId).SendAsync(serverEventName, matchStateContract);
			await Clients.Client(matchSession.Player2.ConnectionId).SendAsync(serverEventName, matchStateContract);
		}

		private async Task SendErrorEventAsync(string errorCode, string message, string? connectionId = null)
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
