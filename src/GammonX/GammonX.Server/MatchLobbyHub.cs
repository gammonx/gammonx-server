using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Analysis;
using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;

using Serilog;

namespace GammonX.Server
{
	/// <summary>
	/// SignalR hub for managing match lobbies and handling the game flow.
	/// </summary>
	internal class MatchLobbyHub : Hub
	{
		private readonly IMatchmakingService _matchmakingService;
		private readonly MatchSessionRepository _repository;
		private readonly IDiceService _diceService;
		private readonly IBotService _botService;
		private readonly IMatchAnalysisQueue _analysisQueue;

		public MatchLobbyHub(
			IMatchmakingService matchmakingService,
			MatchSessionRepository repository,
			IDiceServiceFactory diceServiceFactory,
			IBotService botService,
			IMatchAnalysisQueue analysisQueue)
		{
			_matchmakingService = matchmakingService;
			_repository = repository;
			_diceService = diceServiceFactory.Create();
			_botService = botService;
			_analysisQueue = analysisQueue;
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
		[HubMethodName(ServerCommands.JoinMatchCommand)]
		public async Task JoinMatchAsync(string matchId, string playerId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);

				if (!Guid.TryParse(playerId, out var playerIdGuid))
					await SendErrorEventAsync("LOBBY_ERROR", $"The given playerId '{playerIdGuid}'  is not a valid GUID.", Context.ConnectionId);

				if (_matchmakingService.TryFindMatchLobby(matchGuid, out var matchLobby) && matchLobby != null)
				{
					if (matchLobby.Status != QueueEntryStatus.OpponentFound)
					{
						throw new InvalidOperationException($"The match with given id '{matchId}' cannot be started. No opponent found yet.");
					}

					var matchSession = _repository.GetOrCreate(matchLobby.MatchId, matchLobby.QueueKey);
					var groupName = matchLobby.GroupName;
					if (matchLobby.Player1.PlayerId == playerIdGuid)
					{
						matchLobby.Player1.SetConnectionId(Context.ConnectionId);
						ArgumentNullException.ThrowIfNull(matchLobby.Player1.ConnectionId, nameof(matchLobby.Player1.ConnectionId));
						matchSession.JoinSession(matchLobby.Player1);
						await Groups.AddToGroupAsync(matchLobby.Player1.ConnectionId, groupName);

						if (matchLobby.QueueKey.MatchModus == WellKnownMatchModus.Bot)
						{
							// in a bot game, the player 2 is always the "bot"
							var botPlayer = new LobbyEntry(Guid.NewGuid());
							botPlayer.SetConnectionId(Guid.Empty.ToString());
							matchSession.JoinSession(botPlayer);
						}
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
						await SendToGroup(groupName, ServerEventTypes.MatchLobbyFoundEvent, matchLobbyContract);
						await SendMatchState(ServerEventTypes.MatchStartedEvent, matchSession, ServerCommands.StartGameCommand);
					}
					else
					{
						var matchLobbyPayload = new EventMatchLobbyPayload(matchLobby.MatchId, matchLobby.Player1.PlayerId, null);
						var matchLobbyContract = new EventResponseContract<EventMatchLobbyPayload>(ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyPayload);
						await SendToGroup(groupName, ServerEventTypes.MatchLobbyWaitingEvent, matchLobbyContract);
					}
				}
				else
				{
					await SendErrorEventAsync("LOBBY_ERROR", "No match lobby was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("LOBBY_ERROR", $"An error occurred while trying to join the match lobby: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// Starts the first game round of the match with the given <paramref name="matchId"/>.
		/// First player to start the game receives the <see cref="ServerEventTypes.GameWaitingEvent"/> with the match state.
		/// The game phase advances to <see cref="GamePhase.WaitingForRoll"/>.
		/// If both players accepted the match, the <see cref="ServerEventTypes.GameStartedEvent"/> is sent to both players
		/// containing the initial game state and the first player to roll his dices.
		/// </summary>
		/// <param name="matchId">Match containing the game to start.</param>
		/// <returns>A task to be awaited.</returns>
		[HubMethodName(ServerCommands.StartGameCommand)]
		public async Task StartGameAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("MATCH_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					if (matchSession.Player1?.ConnectionId == Context.ConnectionId)
					{
						matchSession.Player1.AcceptNextGame();

						if (matchSession.Modus == WellKnownMatchModus.Bot)
						{
							// in a bot game, the player 2 is always the "bot"
							matchSession.Player2.AcceptNextGame();
						}
					}

					if (matchSession.Player2?.ConnectionId == Context.ConnectionId)
					{
						matchSession.Player2.AcceptNextGame();
					}

					if (matchSession.CanStartNextGame())
					{
						var startingPlayerId = GetStartingPlayerId(matchSession);
						var gameSession = matchSession.StartNextGame(startingPlayerId);
						if (IsBotTurn(matchSession, startingPlayerId))
						{
							await PerfromBotTurnAsync(matchSession, startingPlayerId);
						}
						else
						{
							await SendGameState(ServerEventTypes.GameStartedEvent, matchSession, ServerCommands.RollCommand);
						}
						// we clean up the match lobby as soon as the game has started
						_matchmakingService.TryRemoveMatchLobby(matchSession.Id);
					}
					else
					{
						var matchPayload = matchSession.ToPayload();
						var matchContract = new EventResponseContract<EventMatchStatePayload>(ServerEventTypes.GameWaitingEvent, matchPayload);
						await SendToCaller(ServerEventTypes.GameWaitingEvent, matchContract);
					}
				}
				else
				{
					await SendErrorEventAsync("MATCH_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("MATCH_ERROR", $"An error occurred while trying to start the game: '{e.Message}'", Context.ConnectionId, e);
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
		[HubMethodName(ServerCommands.RollCommand)]
		public async Task RollAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("ROLL_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);
					await PerfromRollAsync(matchSession, callingPlayerId);
				}
				else
				{
					await SendErrorEventAsync("ROLL_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("ROLL_ERROR", $"An error occurred while trying to roll the dices: '{e.Message}'", Context.ConnectionId, e);
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
		[HubMethodName(ServerCommands.MoveCommand)]
		public async Task MoveAsync(string matchId, int from, int to)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("MOVE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}
				// TODO: move from bar was not possible but others can be moved
				// TODO: if no move is possible END_TURN AllowedCommand is not returned
				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					// calling and active player must be the same
					var callingPlayerId = GetCallingPlayerId(matchSession);
					var isWhite = IsWhite(matchSession, callingPlayerId);
					if (!isWhite)
					{
						// we need to invert the from/to move back for black checkers
						// because the client always plays and acts with white checkers
						var invertedMove = InvertFromToMove(matchSession, from, to);
						from = invertedMove.Item1;
						to = invertedMove.Item2;

					}
					await PerformMoveAsync(matchSession, callingPlayerId, from, to);
				}
				else
				{
					await SendErrorEventAsync("MOVE_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("MOVE_ERROR", $"An error occurred while trying to move checkers: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// The active player undoes, if possible, his last move in his move stack.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A task to be awaited.</returns>
		[HubMethodName(ServerCommands.UndoMoveCommand)]
		public async Task UndoMoveAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("UNDO_MOVE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);

					if (!matchSession.CanUndoLastMove(callingPlayerId))
					{
						await SendErrorEventAsync("UNDO_MOVE_ERROR", "You cannot undo a move at this moment.", Context.ConnectionId);
						return;
					}

					matchSession.UndoLastMove(callingPlayerId);

					if (matchSession.CanUndoLastMove(callingPlayerId))
					{
						// has still moves to undo
						await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand, ServerCommands.UndoMoveCommand);
					}
					else
					{
						// no moves to undo left
						await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand);
					}
				}
				else
				{
					await SendErrorEventAsync("UNDO_MOVE_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("UNDO_MOVE_ERROR", $"An error occurred while trying to undo the last move: '{e.Message}'", Context.ConnectionId, e);
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
		[HubMethodName(ServerCommands.EndTurnCommand)]
		public async Task EndTurnAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("END_TURN_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);

					if (!matchSession.CanEndTurn(callingPlayerId))
					{
						await SendErrorEventAsync("END_TURN_ERROR", "You cannot end your turn at this moment.", Context.ConnectionId);
						return;
					}

					matchSession.EndTurn(callingPlayerId);

					var otherPlayerId = GetOtherPlayerId(matchSession);
					if (IsBotTurn(matchSession, otherPlayerId))
					{
						await PerfromBotTurnAsync(matchSession, otherPlayerId);
					}
					else
					{
						await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.RollCommand);
					}
				}
				else
				{
					await SendErrorEventAsync("END_TURN_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("ROLL_ERROR", $"An error occurred while trying to end the turn: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// The calling player resigns the match with given <paramref name="matchId"/>.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>Task to be awaited.</returns>
		[HubMethodName(ServerCommands.ResignMatchCommand)]
		public async Task ResignMatchAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("RESIGN_MATCH_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					// calling and active player must be the same
					var callingPlayerId = GetCallingPlayerId(matchSession);
					matchSession.ResignMatch(callingPlayerId);
					await SendMatchState(ServerEventTypes.MatchEndedEvent, matchSession);
				}
				else
				{
					await SendErrorEventAsync("RESIGN_MATCH_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("RESIGN_MATCH_ERROR", $"An error occurred while resigning the game: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// The calling player resigns the active game round of the match with given <paramref name="matchId"/>.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>Task to be awaited.</returns>
		[HubMethodName(ServerCommands.ResignGameCommand)]
		public async Task ResignGameAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("RESIGN_GAME_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					// calling and active player must be the same
					var callingPlayerId = GetCallingPlayerId(matchSession);
					matchSession.ResignGame(callingPlayerId);
					// check if this was the last game round of the match
					if (matchSession.IsMatchOver())
					{
						await SendMatchState(ServerEventTypes.MatchEndedEvent, matchSession);
					}
					else
					{
						await SendMatchState(ServerEventTypes.GameEndedEvent, matchSession, ServerCommands.StartGameCommand);
					}
				}
				else
				{
					await SendErrorEventAsync("RESIGN_GAME_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("RESIGN_GAME_ERROR", $"An error occurred while resigning the game: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		#region Doubling Cube Commands

		/// <summary>
		/// The active doubling cube owner offers a double to his opponent who can accept or decline it.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>Task to be awaited.</returns>
		[HubMethodName(ServerCommands.OfferDoubleCommands)]
		public async Task OfferDoubleAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var otherPlayerId = GetOtherPlayerId(matchSession);
					if (IsBotTurn(matchSession, otherPlayerId))
					{
						var shouldAccept = await PerformShouldBotAcceptsDoubleAsync(matchSession, otherPlayerId);
						if (shouldAccept)
						{
							await PerformAcceptDoubleAsync(matchSession, otherPlayerId);
						}
						else
						{
							await PerformDeclineDoubleAsync(matchSession, otherPlayerId);
						}
					}
					else
					{
						var callingPlayerId = GetCallingPlayerId(matchSession);
						await PerformOfferDoubleAsync(matchSession, callingPlayerId);
					}
				}
				else
				{
					await SendErrorEventAsync("OFFER_DOUBLE_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"An error occurred while a double was offered: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// The player who got the offer for a doubling cube increase accepted it.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A task to be awaited.</returns>
		[HubMethodName(ServerCommands.AcceptDoubleCommands)]
		public async Task AcceptDoubleAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);
					await PerformAcceptDoubleAsync(matchSession, callingPlayerId);
				}
				else
				{
					await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"An error occurred while accepting a double offer: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		/// <summary>
		/// The player who got the offer for a doubling cube declines it and loses the active game round.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A task to be awaited.</returns>
		[HubMethodName(ServerCommands.DeclineDoubleCommands)]
		public async Task DeclineDoubleAsync(string matchId)
		{
			try
			{
				if (!Guid.TryParse(matchId, out var matchGuid))
				{
					await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"The given matchId '{matchId}' is not a valid GUID.", Context.ConnectionId);
				}

				var matchSession = _repository.Get(matchGuid);
				if (matchSession != null)
				{
					var callingPlayerId = GetCallingPlayerId(matchSession);
					await PerformDeclineDoubleAsync(matchSession, callingPlayerId);
				}
				else
				{
					await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", "No match seesion was found with the given matchId.", Context.ConnectionId);
				}
			}
			catch (Exception e)
			{
				await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"An error occurred while declining a double offer: '{e.Message}'", Context.ConnectionId, e);
			}
		}

		private async Task PerformOfferDoubleAsync(IMatchSessionModel matchSession, Guid offeringPlayerId)
		{
			// TODO: double can only be offered before roll
			// TODO: server side validation for allowed commands
			if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
			{
				doubleCubeSession.OfferDouble(offeringPlayerId);
				var otherPlayerId = GetOtherPlayerId(matchSession);
				// the player who is offering the double is waiting for a response
				var callingPlayerGameSession = matchSession.GetGameState(offeringPlayerId);
				var callingPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.GameWaitingEvent, callingPlayerGameSession);
				var callingConnectionId = GetPlayerConnectionId(matchSession, offeringPlayerId);
				await SendToClient(callingConnectionId, ServerEventTypes.GameWaitingEvent, callingPlayerContract);
				// the player who got the double offered has to accept or decline it
				var otherPlayerGameSession = matchSession.GetGameState(otherPlayerId, [ServerCommands.AcceptDoubleCommands, ServerCommands.DeclineDoubleCommands]);
				var otherPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.DoubleOffered, otherPlayerGameSession);
				var otherPlayerConnectionId = GetPlayerConnectionId(matchSession, otherPlayerId);
				await SendToClient(otherPlayerConnectionId, ServerEventTypes.DoubleOffered, otherPlayerContract);
			}
			else
			{
				await SendErrorEventAsync("OFFER_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
			}
		}

		private async Task PerformAcceptDoubleAsync(IMatchSessionModel matchSession, Guid offeredPlayerId)
		{
			if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
			{
				doubleCubeSession.AcceptDouble(offeredPlayerId);

				var offeringPlayerId = GetOtherPlayerId(matchSession);
				if (IsBotTurn(matchSession, offeringPlayerId))
				{
					// the bot offered the double and the human player accepted it
					// bot executes the next turn
					await PerfromBotTurnAsync(matchSession, offeringPlayerId);
				}
				else
				{
					// doubles can only be offered/accepted/declined at the start of a turn and before the dices got rolled
					// Therefore, the next command must be the roll command
					await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.RollCommand);
				}
			}
			else
			{
				await SendErrorEventAsync("ACCEPT_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
			}
		}

		private async Task PerformDeclineDoubleAsync(IMatchSessionModel matchSession, Guid offeredPlayerId)
		{
			if (matchSession is IDoubleCubeMatchSession doubleCubeSession)
			{
				// declining a double offer automatically loses the current game round
				doubleCubeSession.DeclineDouble(offeredPlayerId);
				// check if this was the last game round of the match
				if (matchSession.IsMatchOver())
				{
					// match ended bot does not have another turn
					await SendMatchState(ServerEventTypes.MatchEndedEvent, matchSession);
				}
				else
				{
					// game ended, a potential bot turn is handled in the start game command
					await SendMatchState(ServerEventTypes.GameEndedEvent, matchSession, ServerCommands.StartGameCommand);
				}
			}
			else
			{
				await SendErrorEventAsync("DECLINE_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
			}
		}

		private async Task<bool> PerformShouldBotAcceptsDoubleAsync(IMatchSessionModel matchSession, Guid callingPlayerId)
		{
			if (matchSession is IDoubleCubeMatchSession cubeSession && cubeSession.CanOfferDouble(callingPlayerId))
			{
				var shouldAccept = await _botService.ShouldAcceptDouble(matchSession, callingPlayerId);
				return shouldAccept;
			}
			else
			{
				await SendErrorEventAsync("BOT_ACCEPT_DOUBLE_ERROR", $"The match with matchId '{matchSession.Id}' does not support doubling cubes.", Context.ConnectionId);
			}

			return false;
		}

		#endregion Doubling Cube Commands

		private async Task PerfromRollAsync(IMatchSessionModel matchSession, Guid callingPlayerId)
		{
			matchSession.RollDices(callingPlayerId);
			await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand);
		}

		private async Task<bool> PerformMoveAsync(IMatchSessionModel matchSession, Guid callingPlayerId, int from, int to)
		{
			var gameOver = matchSession.MoveCheckers(callingPlayerId, from, to);
			// check if this move won the game for the active player
			if (gameOver)
			{
				// check if this was the last move to win the last game and the match
				if (matchSession.IsMatchOver())
				{
					await SendMatchState(ServerEventTypes.MatchEndedEvent, matchSession);
					return false;
				}
				else
				{
					await SendMatchState(ServerEventTypes.GameEndedEvent, matchSession, ServerCommands.StartGameCommand);
					return false;
				}
			}
			// check if the turn was finished
			else if (matchSession.CanEndTurn(callingPlayerId))
			{
				// calling player finished his turn, other player can now roll
				await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.EndTurnCommand, ServerCommands.UndoMoveCommand);
				return false;
			}
			else
			{
				// calling player can still move
				await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.MoveCommand, ServerCommands.UndoMoveCommand);
				return true;
			}
		}

		private async Task PerfromBotTurnAsync(IMatchSessionModel matchSession, Guid botPlayerId)
		{
			try
			{
				if (matchSession is IDoubleCubeMatchSession cubeSession && cubeSession.CanOfferDouble(botPlayerId))
				{
					var shouldOffer = await _botService.ShouldOfferDouble(matchSession, botPlayerId);
					if (shouldOffer)
					{
						await PerformOfferDoubleAsync(matchSession, botPlayerId);
						// the bot turn ends here, waiting for the player to accept or decline the double
						return;
					}
				}

				await PerfromRollAsync(matchSession, botPlayerId);

				bool canMove = false;
				do
				{
					var nextMoves = await _botService.GetNextMovesAsync(matchSession, botPlayerId);

					if (nextMoves == null)
					{
						throw new InvalidOperationException("An error occurred while evaluating the next move for the bot");
					}

					foreach (var nextMove in nextMoves.Moves)
					{
						canMove = await PerformMoveAsync(matchSession, botPlayerId, nextMove.From, nextMove.To);
					}
				}
				while (canMove);

				// if we cannot end the current turn, the match/game is probably over
				if (matchSession.CanEndTurn(botPlayerId))
				{
					matchSession.EndTurn(botPlayerId);
					await SendGameState(ServerEventTypes.GameStateEvent, matchSession, ServerCommands.RollCommand);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error(ex, "Code {errorCode} :: Message {errorMessage}", "BOT_EXECUTE_TURN_ERROR", "An Error occurred while executing the bot turn");
				throw;
			}
		}

		#endregion Commands

		#region Private Members

		private Guid GetStartingPlayerId(IMatchSessionModel matchSession)
		{
			var activeSession = matchSession.GetGameSession(matchSession.GameRound);
			// if null, the active game session is the first
			if (activeSession == null)
			{
				// we decide which player starts with a single dice roll
				int player1Roll;
				int player2Roll;
				do
				{
					player1Roll = _diceService.Roll(1, 6)[0];
					player2Roll = _diceService.Roll(1, 6)[0];
				}
				while (player1Roll == player2Roll);

				if (player1Roll > player2Roll)
				{
					return matchSession.Player1.Id;
				}
				else
				{
					return matchSession.Player2.Id;
				}
			}
			// if not null, the first game session was already played
			else if (activeSession.Phase == GamePhase.GameOver)
			{
				var contract = activeSession.ToContract(matchSession.GameRound);
				return contract.Winner ?? matchSession.Player1.Id;
			}
			else
			{
				throw new InvalidOperationException("An error occurred while determining the last game round winner");
			}
		}

		private async Task SendGameState(string serverEventName, IMatchSessionModel matchSession, params string[] allowedCommands)
		{
			var gameSessionPlayer1 = matchSession.GetGameState(matchSession.Player1.Id, allowedCommands);
			var player1Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer1);
			var gameSessionPlayer2 = matchSession.GetGameState(matchSession.Player2.Id, allowedCommands);
			var player2Contract = new EventResponseContract<EventGameStatePayload>(serverEventName, gameSessionPlayer2);
			await SendToClient(matchSession.Player1.ConnectionId, serverEventName, player1Contract);
			if (matchSession.Modus != WellKnownMatchModus.Bot)
			{
				await SendToClient(matchSession.Player2.ConnectionId, serverEventName, player2Contract);
			}
		}

		private async Task SendMatchState(string serverEventName, IMatchSessionModel matchSession, params string[] allowedCommands)
		{
			// TODO :: match ended sync :: stats for end screen
			// - game rounds points
			// - rating change (ranked) > calculate elo
			// > send ServerEventTypes.MatchStatsEvent
			// > new payload extension for ranked/paid subscriptions?

			var matchStatePayload = matchSession.ToPayload(allowedCommands);
			var matchStateContract = new EventResponseContract<EventMatchStatePayload>(serverEventName, matchStatePayload);
			await SendToClient(matchSession.Player1.ConnectionId, serverEventName, matchStateContract);
			if (matchSession.Modus != WellKnownMatchModus.Bot)
			{
				await SendToClient(matchSession.Player2.ConnectionId, serverEventName, matchStateContract);
			}

			if (serverEventName.Equals(ServerEventTypes.MatchEndedEvent))
			{
				// TODO :: match ended async :: to database
				var analysisJob = new MatchAnalysisJob(matchSession.Id);
				await _analysisQueue.EnqueueAsync(analysisJob);

				// clients can now safely disconnect from socket
				var emptyResponse = new EventResponseContract<EmptyEventPayload>(ServerEventTypes.ForceDisconnect, new EmptyEventPayload());
				await SendToGroup(matchStatePayload.GroupName, ServerEventTypes.ForceDisconnect, emptyResponse);
				await Groups.RemoveFromGroupAsync(matchSession.Player1.ConnectionId, matchStatePayload.GroupName);
				await Groups.RemoveFromGroupAsync(matchSession.Player2.ConnectionId, matchStatePayload.GroupName);
			}
		}

		private async Task SendErrorEventAsync(string errorCode, string message, string? connectionId = null, Exception? ex = null)
		{
			var payload = new EventErrorPayload(errorCode, message);
			var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);
			if (!string.IsNullOrEmpty(connectionId))
			{
				await SendToClient(connectionId, ServerEventTypes.ErrorEvent, contract);
			}
			else
			{
				await SendToCaller(ServerEventTypes.ErrorEvent, contract);
			}

			Log.Logger.Error(ex, "ConnectionId {connId} :: Code {errorCode} :: Message {errorMessage}", connectionId, errorCode, message);
		}

		private async Task SendErrorEventToGroupAsync(string errorCode, string message, string groupName, Exception? ex = null)
		{
			var payload = new EventErrorPayload(errorCode, message);
			var contract = new EventResponseContract<EventErrorPayload>(ServerEventTypes.ErrorEvent, payload);
			await SendToGroup(groupName, ServerEventTypes.ErrorEvent, contract);
			Log.Logger.Error(ex, "Code {errorCode} :: Message {errorMessage}", errorCode, message);
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

		private Guid GetOtherPlayerId(IMatchSessionModel matchSession)
		{
			if (matchSession.Player1.ConnectionId == Context.ConnectionId)
			{
				return matchSession.Player2.Id;
			}
			if (matchSession.Player2.ConnectionId == Context.ConnectionId)
			{
				return matchSession.Player1.Id;
			}
			throw new InvalidOperationException("The calling player is not part of the match session.");
		}

		private static bool IsWhite(IMatchSessionModel matchSession, Guid callingPlayerId)
		{
			// player 1 plays always with white checkers
			if (matchSession.Player1.Id.Equals(callingPlayerId))
			{
				return true;
			}
			// player 2 plays always with black checkers
			else if (matchSession.Player2.Id.Equals(callingPlayerId))
			{
				return false;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}

		private static (int, int) InvertFromToMove(IMatchSessionModel matchSession, int from, int to)
		{
			var gameModus = matchSession.GetGameModus();
			return BoardBroker.InvertFromToMove(gameModus, from, to);
		}

		private static bool IsBotTurn(IMatchSessionModel matchSession, Guid playerId)
		{
			if (matchSession.Modus == WellKnownMatchModus.Bot)
			{
				if (matchSession.Player1.Id == playerId)
				{
					return false;
				}
				else if (matchSession.Player2.Id == playerId && matchSession.Player2.IsBot)
				{
					return true;
				}
			}
			return false;
		}

		private static string GetPlayerConnectionId(IMatchSessionModel matchSession, Guid playerId)
		{
			if (matchSession.Player1.Id.Equals(playerId))
			{
				return matchSession.Player1.ConnectionId;
			}
			else if (matchSession.Player2.Id.Equals(playerId))
			{
				return matchSession.Player2.ConnectionId;
			}
			throw new InvalidOperationException("The calling player is not part of the match session.");
		}

		private async Task SendToGroup<T>(string groupName, string serverEventName, EventResponseContract<T> response) where T : EventPayloadBase
		{
			await Clients.Group(groupName).SendAsync(serverEventName, response);
		}

		private async Task SendToCaller<T>(string serverEventName, EventResponseContract<T> response) where T : EventPayloadBase
		{
			await Clients.Caller.SendAsync(serverEventName, response);
		}

		private async Task SendToClient<T>(string connectionId, string serverEventName, EventResponseContract<T> response) where T : EventPayloadBase
		{
			await Clients.Client(connectionId).SendAsync(serverEventName, response);
		}

		#endregion Private Members
	}
}
