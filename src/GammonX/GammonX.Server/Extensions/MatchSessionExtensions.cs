using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Extensions
{
    public static class MatchSessionExtensions
    {
        public static GameRecordContract ToRecord(this IMatchSessionModel match, int gameRound, Guid playerId)
        {
            var game = match.GetGameSession(gameRound);

            if (game == null)
            {
                throw new InvalidOperationException($"no game session found for round '{gameRound}'");
            }

            int? doublingCubeValue = null;
            if (match is IDoubleCubeMatchSession cubeMatchSession)
            {
                doublingCubeValue = cubeMatchSession.GetDoublingCubeValue();
            }

            var isWhite = IsWhite(match, playerId);
            var pipCount = GetPipeCount(game, isWhite);
            var gameResult = game.Result.GetResult(playerId);
            var gameHistory = game.GetHistory(match.Player1.Id, match.Player2.Id);
            var gameHistoryStr = gameHistory.ToString();

            return new GameRecordContract()
            {
                MatchId = match.Id,
                Id = game.Id,
                DoublingCubeValue = doublingCubeValue,
                PlayerId = playerId,
                PipesLeft = pipCount,
                Result = gameResult,
                GameHistory = gameHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT
            };
        }

        private static bool IsWhite(IMatchSessionModel model, Guid playerId)
        {
            // player 1 plays always with white checkers
            if (model.Player1.Id.Equals(playerId))
            {
                return true;
            }
            // player 2 plays always with black checkers
            else if (model.Player2.Id.Equals(playerId))
            {
                return false;
            }
            throw new InvalidOperationException("Player is not part of this match session.");
        }

        private static int GetPipeCount(IGameSessionModel model, bool isWhite)
        {
            if (isWhite)
            {
                return model.BoardModel.PipCountWhite;
            }
            return model.BoardModel.PipCountBlack;
        }
    }
}
