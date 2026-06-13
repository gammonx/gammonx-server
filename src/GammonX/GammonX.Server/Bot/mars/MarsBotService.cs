using GammonX.Engine.Extensions;
using GammonX.Engine.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Services;

namespace GammonX.Server.Bot
{
    // <inheritdoc />
    public class MarsBotService : IBotService
    {
        private readonly HttpClient _httpClient;

        public MarsBotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // <inheritdoc />
        public async Task<MoveSequenceModel> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
        {
            try
            {
                var gameSession = matchSession.GetGameSession(matchSession.GameRound);
                if (gameSession == null)
                    throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

                var modus = gameSession.Modus;

                // bot plays with black checkers
                var isWhite = IsWhite(matchSession, playerId);
                var boardContract = gameSession.BoardModel.ToContract(false);
                var rolls = gameSession.DiceRolls.Select(dr => dr.Roll).ToArray();

                EvalMoveRequestContract parameters = new EvalMoveRequestContract
                {
                    Modus = modus,
                    Board = boardContract,
                    Rolls = rolls,
                    IsWhite = isWhite,
                    BotLevel = matchSession.BotLevel,
                };

                var client = new MarsClient(_httpClient);
                try
                {
                    var result = await client.GetMoveEvalAsync(parameters);
                    var moveSeq = result.Payload.MoveSequence;
                    return moveSeq;
                }
                catch (Exception)
                {
                    // debugging purposes only
                    throw;
                }
            }
            catch (Exception)
            {
                // debugging purposes only
                throw;
            }
        }

        // <inheritdoc />
        public async Task<bool> ShouldTakeDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            try
            {
                var gameSession = matchSession.GetGameSession(matchSession.GameRound);
                if (gameSession == null)
                    throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

                var modus = gameSession.Modus;

                // bot plays with black checkers
                var isWhite = IsWhite(matchSession, playerId);
                var boardContract = gameSession.BoardModel.ToContract(false);

                var matchLength = matchSession.Type.GetMaxPoints();
                EvalCubeRequestContract parameters = new EvalCubeRequestContract
                {
                    Board = boardContract,
                    IsWhite = isWhite,
                    Modus = modus,
                    MatchLength = matchLength,
                    PointsAwayPlayer = matchSession.PointsAway(playerId),
                    PointsAwayOpp = matchSession.PointsAway(GetOtherPlayerId(matchSession, playerId)),
                    BotLevel = matchSession.BotLevel,
                };

                var client = new MarsClient(_httpClient);
                try
                {
                    var result = await client.GetCubeEvalAsync(parameters);
                    var cubeAction = result.Payload;
                    return cubeAction.ShouldTake == CubeAction.Take;
                }
                catch (Exception)
                {
                    // debugging purposes only
                    throw;
                }
            }
            catch (Exception)
            {
                // debugging purposes only
                throw;
            }
        }

        // <inheritdoc />
        public async Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            try
            {
                var gameSession = matchSession.GetGameSession(matchSession.GameRound);
                if (gameSession == null)
                    throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

                var modus = gameSession.Modus;

                // bot plays with black checkers
                var isWhite = IsWhite(matchSession, playerId);
                var boardContract = gameSession.BoardModel.ToContract(false);

                var matchLength = matchSession.Type.GetMaxPoints();
                EvalCubeRequestContract parameters = new EvalCubeRequestContract
                {
                    Board = boardContract,
                    IsWhite = isWhite,
                    Modus = modus,
                    MatchLength = matchLength,
                    PointsAwayPlayer = matchSession.PointsAway(playerId),
                    PointsAwayOpp = matchSession.PointsAway(GetOtherPlayerId(matchSession, playerId)),
                    BotLevel = matchSession.BotLevel,
                };

                var client = new MarsClient(_httpClient);
                try
                {
                    var result = await client.GetCubeEvalAsync(parameters);
                    var cubeAction = result.Payload;
                    return cubeAction.ShouldOffer == CubeAction.Double;
                }
                catch (Exception)
                {
                    // debugging purposes only
                    throw;
                }
            }
            catch (Exception)
            {
                // debugging purposes only
                throw;
            }
        }

        private static bool IsWhite(IMatchSessionModel matchSession, Guid playerId)
        {
            if (matchSession.Player1.Id.Equals(playerId))
            {
                return true;
            }
            else if (matchSession.Player2.Id.Equals(playerId))
            {
                return false;
            }
            throw new InvalidOperationException("Player is not part of this match session.");
        }

        private static Guid GetOtherPlayerId(IMatchSessionModel matchSession, Guid callingPlayerId)
        {
            if (matchSession.Player1.Id.Equals(callingPlayerId))
            {
                return matchSession.Player2.Id;
            }
            if (matchSession.Player2.Id.Equals(callingPlayerId))
            {
                return matchSession.Player1.Id;
            }
            throw new InvalidOperationException("The calling player is not part of the match session.");
        }
    }
}
