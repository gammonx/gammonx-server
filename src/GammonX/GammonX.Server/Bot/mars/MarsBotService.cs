using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Models.Contracts;

using GammonX.Server.Contracts;
using GammonX.Server.Models;

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

                var gameModus = gameSession.Modus;

                if (gameModus != GammonX.Models.Enums.GameModus.Plakoto)
                {
                    throw new InvalidOperationException("Use wildbg bot service instead");
                }

                // bot plays with black checkers
                var isWhite = IsWhite(matchSession, playerId);
                var boardContract = gameSession.BoardModel.ToContract(false);
                var rolls = gameSession.DiceRolls.Select(dr => dr.Roll).ToArray();

                EvalMoveRequestContract parameters = new EvalMoveRequestContract
                {
                    Modus = gameModus,
                    Board = boardContract,
                    Rolls = rolls,
                    IsWhite = isWhite
                };

                var client = new MarsClient(_httpClient);
                ResponseContract<MoveEvalPayload>? result = null;
                try
                {
                    result = await client.GetMoveEvalAsync(parameters);
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
        public Task<bool> ShouldAcceptDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            throw new InvalidOperationException("Mars bot does not support accepting doubles.");
        }

        // <inheritdoc />
        public Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            throw new InvalidOperationException("Mars bot does not support accepting doubles.");
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
    }
}
