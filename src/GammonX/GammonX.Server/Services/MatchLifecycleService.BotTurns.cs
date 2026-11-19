using GammonX.Diagnostics.Server;

using GammonX.Engine.Models;

using GammonX.Server.Bot;

using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Serilog;

using MatchSessionLease = GammonX.Server.Services.MatchSessionRepository.MatchSessionLease;

namespace GammonX.Server.Services
{
    internal sealed partial class MatchLifecycleService
    {
        internal async Task PerformOfferDoubleAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            IDoubleCubeMatchSession doubleCubeSession,
            Guid offeringPlayerId,
            CancellationToken cancellationToken = default)
        {
            // active players changes here
            doubleCubeSession.OfferDouble(offeringPlayerId);
            CancelTimersFor(matchSession.Id, offeringPlayerId, CancellationTokenCategory.Turn);

            matchLock.Commit();
            var offeredPlayerId = matchLock.GetOtherPlayerId(offeringPlayerId);

            if (matchLock.IsBotPlayer(offeredPlayerId))
            {
                await PerformBotTurnAsync(matchLock, matchSession, offeredPlayerId, cancellationToken);
            }
            else
            {
                if (matchLock.IsBotPlayer(offeringPlayerId))
                {
                    StartTurnTimer(matchSession, offeredPlayerId);
                }

                // the player who is offering the double is waiting for a response
                var callingPlayerGameSession = matchSession.GetGameState(offeringPlayerId);
                var callingPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.DoubleOfferedEvent, callingPlayerGameSession);
                var callingConnectionId = matchLock.GetPlayerConnectionId(offeringPlayerId);
                await SendToClientAsync(callingConnectionId, ServerEventTypes.DoubleOfferedEvent, callingPlayerContract, cancellationToken);

                // the player who got the double offered has to accept or decline it
                var otherPlayerGameSession = matchSession.GetGameState(offeredPlayerId);
                var otherPlayerContract = new EventResponseContract<EventGameStatePayload>(ServerEventTypes.DoubleOfferedEvent, otherPlayerGameSession);
                var otherPlayerConnectionId = matchLock.GetPlayerConnectionId(offeredPlayerId);
                await SendToClientAsync(otherPlayerConnectionId, ServerEventTypes.DoubleOfferedEvent, otherPlayerContract, cancellationToken);
            }
        }

        internal async Task PerformAcceptDoubleAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            IDoubleCubeMatchSession doubleCubeSession,
            Guid offeredPlayerId,
            CancellationToken cancellationToken = default)
        {
            doubleCubeSession.AcceptDouble(offeredPlayerId);
            CancelTimersFor(matchSession.Id, offeredPlayerId, CancellationTokenCategory.Turn);

            var offeringPlayerId = matchLock.GetOtherPlayerId(offeredPlayerId);
            matchLock.Commit();

            if (matchLock.IsBotPlayer(offeringPlayerId))
            {
                // the bot offered the double and the player player accepted it
                // bot executes the next turn
                await PerformBotTurnAsync(matchLock, matchSession, offeringPlayerId, cancellationToken);
            }
            else
            {
                // doubles can only be offered/accepted/declined at the start of a turn and before the dices got rolled
                // Therefore, the next command must be the roll command
                if (matchLock.IsBotPlayer(offeredPlayerId))
                {
                    StartTurnTimer(matchSession, offeringPlayerId);
                }

                await SendGameState(ServerEventTypes.DoubleAcceptedEvent, matchSession, cancellationToken);
            }
        }

        internal async Task PerformDeclineDoubleAsync(MatchSessionLease matchLock, IMatchSessionModel matchSession, IDoubleCubeMatchSession doubleCubeSession, Guid offeredPlayerId)
        {
            // declining a double offer automatically loses the current game round
            doubleCubeSession.DeclineDouble(offeredPlayerId);
            CancelTimersFor(matchSession.Id, offeredPlayerId, CancellationTokenCategory.Turn);

            matchLock.Commit();
            // check if this was the last game round of the match
            if (matchSession.IsMatchOver())
            {
                // match ended bot does not have another turn
                await SendMatchState(matchLock, ServerEventTypes.MatchEndedEvent, matchSession);
            }
            else
            {
                // game ended, a potential bot turn is handled in the start game command
                await SendMatchState(matchLock, ServerEventTypes.GameEndedEvent, matchSession);
            }
        }

        internal async Task PerformRollAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid callingPlayerId,
            CancellationToken cancellationToken = default)
        {
            matchSession.RollDices(callingPlayerId);
            matchLock.Commit();
            await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
        }

        internal async Task PerformMoveAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid callingPlayerId,
            int from,
            int to,
            CancellationToken cancellationToken = default)
        {
            var gameOver = matchSession.MoveCheckers(callingPlayerId, from, to);
            matchLock.Commit();

            try
            {
                await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
            }
            finally
            {
                if (gameOver)
                {
                    // The result was already committed. Attempt publication even if the board send fails.
                    var resultEvent = matchSession.IsMatchOver() ? ServerEventTypes.MatchEndedEvent : ServerEventTypes.GameEndedEvent;
                    await SendMatchState(matchLock, resultEvent, matchSession);
                }
            }
        }

        internal async Task PerformBotTurnAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            Guid botPlayerId,
            CancellationToken disconnected,
            Func<Task>? notifyStart = null)
        {
            var game = matchSession.GetGameSession(matchSession.GameRound);
            if (game == null)
            {
                throw new InvalidOperationException("A bot turn requires an active game.");
            }

            var turn = new BotTurnIdentity(matchSession.Id, botPlayerId, game.Id, game.TurnNumber);
            CancelTimersFor(turn.MatchId, turn.BotId, CancellationTokenCategory.BotRecovery);

            using var operation = CancellationTokenSource.CreateLinkedTokenSource(disconnected, _botRecovery.Stopping);

            try
            {
                operation.Token.ThrowIfCancellationRequested();

                if (notifyStart != null)
                {
                    await notifyStart();
                }

                await RunBotTurnAsync(matchLock, matchSession, turn, operation.Token);
            }
            catch (Exception) when (operation.IsCancellationRequested)
            {
                // A reconnect can resume from the last committed action.
            }
            catch (Exception ex)
            {
                if (GetActiveBotGame(_matchRepository.Get(turn.MatchId), turn) != null)
                {
                    // we schedule a bot retry once, if this fails to we close the match as inconclusive (no rating change)
                    _botRecovery.Schedule(turn, disconnected, RecoverBotTurnAsync);
                }

                Log.Error(ex, "Bot turn failed for match {MatchId}, game {GameId}, turn {TurnNumber}.", turn.MatchId, turn.GameId, turn.TurnNumber);
                throw new BotServiceException(WellKnownBotServices.Mars, "An error occurred while executing the bot turn", ex);
            }
        }

        private async Task RunBotTurnAsync(
            MatchSessionLease matchLock,
            IMatchSessionModel matchSession,
            BotTurnIdentity turn,
            CancellationToken cancellationToken,
            bool fallbackEval = false)
        {
            var game = RequireActiveBotGame(matchSession, turn, cancellationToken);

            if (matchSession is IDoubleCubeMatchSession cubeSession)
            {
                if (cubeSession.IsDoubleOfferPending)
                {
                    var shouldTake = fallbackEval || await EvaluateBotCubeAsync(matchSession, turn.BotId, true, cancellationToken);
                    RequireActiveBotGame(matchSession, turn, cancellationToken);

                    if (shouldTake)
                    {
                        await PerformAcceptDoubleAsync(matchLock, matchSession, cubeSession, turn.BotId, cancellationToken);
                    }
                    else
                    {
                        await PerformDeclineDoubleAsync(matchLock, matchSession, cubeSession, turn.BotId);
                    }

                    return;
                }

                if (!fallbackEval && cubeSession.CanOfferDouble(turn.BotId))
                {
                    var shouldOffer = await EvaluateBotCubeAsync(matchSession, turn.BotId, false, cancellationToken);
                    RequireActiveBotGame(matchSession, turn, cancellationToken);

                    if (shouldOffer)
                    {
                        await PerformOfferDoubleAsync(matchLock, matchSession, cubeSession, turn.BotId, cancellationToken);
                        return;
                    }
                }
            }

            if (game.Phase == GamePhase.WaitingForRoll)
            {
                await PerformRollAsync(matchLock, matchSession, turn.BotId, cancellationToken);
                game = RequireActiveBotGame(matchSession, turn, cancellationToken);
            }

            var diceRemaining = game.DiceRolls.Count(roll => !roll.Used);
            IReadOnlyList<MoveModel>? moveSeq = null;
            var nextMove = 0;

            while (diceRemaining > 0 && game.CanMove(matchLock.IsWhite(turn.BotId)))
            {
                RequireActiveBotGame(matchSession, turn, cancellationToken);

                if (!fallbackEval && (moveSeq == null || nextMove >= moveSeq.Count))
                {
                    try
                    {
                        moveSeq = (await GetBotService().GetNextMovesAsync(matchSession, turn.BotId, cancellationToken))?.Moves;
                    }
                    catch (Exception ex)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Log.Warning(ex, "Bot move evaluation failed for match {MatchId}.", turn.MatchId);
                        moveSeq = null;
                    }

                    nextMove = 0;
                    game = RequireActiveBotGame(matchSession, turn, cancellationToken);
                }

                var move = moveSeq != null && nextMove < moveSeq.Count ? moveSeq[nextMove++] : null;
                if (move == null)
                {
                    if (!fallbackEval)
                    {
                        ServerMetrics.BotFallbacks.Add(1, new KeyValuePair<string, object?>("operation", "move"));
                        fallbackEval = true;
                    }

                    // the bot service failed to deliver a move, we just fall back on the first one
                    moveSeq = game.GetLegalMoveSequences(matchLock.IsWhite(turn.BotId)).First().Moves;
                    nextMove = 1;
                    move = moveSeq[0];
                }

                RequireActiveBotGame(matchSession, turn, cancellationToken);

                await PerformMoveAsync(matchLock, matchSession, turn.BotId, move.From, move.To, cancellationToken);

                if (game.Phase == GamePhase.GameOver)
                {
                    return;
                }

                game = RequireActiveBotGame(matchSession, turn, cancellationToken);

                var unused = game.DiceRolls.Count(roll => !roll.Used);
                if (unused >= diceRemaining)
                {
                    throw new InvalidOperationException("A committed bot move must spend at least one die.");
                }

                diceRemaining = unused;
            }

            RequireActiveBotGame(matchSession, turn, cancellationToken);

            if (!matchSession.CanEndTurn(turn.BotId))
            {
                throw new InvalidOperationException("The bot could not finish its turn within the remaining dice.");
            }

            matchSession.EndTurn(turn.BotId);
            matchLock.Commit();

            StartTurnTimer(matchSession, matchLock.GetOtherPlayerId(turn.BotId));
            await SendGameState(ServerEventTypes.GameStateEvent, matchSession, cancellationToken);
        }

        private async Task<bool> EvaluateBotCubeAsync(
            IMatchSessionModel match,
            Guid botId,
            bool take,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var bot = GetBotService();

                return take
                    ? await bot.ShouldTakeDouble(match, botId, cancellationToken)
                    : await bot.ShouldOfferDouble(match, botId, cancellationToken);
            }
            catch (Exception ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Log.Warning(ex, "Bot cube evaluation failed for match {MatchId}.", match.Id);

                ServerMetrics.BotFallbacks.Add(1, new KeyValuePair<string, object?>("operation", take ? "cube.take" : "cube.offer"));

                return take;
            }
        }

        private static IGameSessionModel? GetActiveBotGame(IMatchSessionModel? match, BotTurnIdentity turn)
        {
            var game = match?.GetGameSession(match.GameRound);
            var sameMatch = match?.Id == turn.MatchId;
            var sameGame = game?.Id == turn.GameId;
            var sameTurn = game?.TurnNumber == turn.TurnNumber;
            var isBotTurn = game?.ActivePlayer == turn.BotId;
            var isGameOver = game?.Phase != GamePhase.GameOver;
            return sameMatch && sameGame && sameTurn && isBotTurn && isGameOver ? game : null;
        }

        private static IGameSessionModel RequireActiveBotGame(IMatchSessionModel match, BotTurnIdentity turn, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var game = GetActiveBotGame(match, turn);
            if (game == null)
            {
                throw new InvalidOperationException("The bot no longer owns this game turn.");
            }

            return game;
        }

        private async Task RecoverBotTurnAsync(BotTurnRecoveryService.Deadline deadline, CancellationToken cancellationToken)
        {
            var turn = deadline.Turn;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Generation.Token);
            await using var matchLock = await _matchRepository.AcquireStateWithCancellationAsync(turn.MatchId, wait.Token);

            cancellationToken.ThrowIfCancellationRequested();

            var match = matchLock.Get();
            if (match == null || GetActiveBotGame(match, turn) == null ||
                IsDisconnectPending(turn.MatchId, matchLock.GetOtherPlayerId(turn.BotId)) ||
                !_cancellationTokenService.TryCancel(deadline.Key, deadline.Generation))
            {
                return;
            }

            ServerMetrics.BotRecoveries.Add(1);

            try
            {
                await RunBotTurnAsync(matchLock, match, turn, cancellationToken, fallbackEval: true);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Disconnect/reconnect or host shutdown now owns the committed state.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Automatic bot turn recovery failed for match {MatchId}, game {GameId}, turn {TurnNumber}.", turn.MatchId, turn.GameId, turn.TurnNumber);

                var committed = _matchRepository.Get(turn.MatchId);
                if (committed == null || GetActiveBotGame(committed, turn) == null)
                {
                    return;
                }

                var groupName = ConstructGroupName(turn.MatchId);

                try
                {
                    await SendErrorEventToGroupAsync("BOT_RECOVERY_FAILED", "The match was closed because the bot turn could not be completed.", groupName, cancellationToken: cancellationToken);
                }
                finally
                {
                    await CleanUpMatchAsync(matchLock, committed, groupName, cancellationToken);
                }
            }
        }

        private IBotService GetBotService() => botService ?? throw new InvalidOperationException("Mars bot service is unavailable.");
    }
}
