using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Extensions
{
    public static class MatchSessionExtensions
    {
        public static MatchRecordContract ToRecord(this IMatchSessionModel match, Guid playerId)
        {
            var gameRecords = new List<GameRecordContract>();
            var gameSessions = match.GetGameSessions().ToList();
            foreach (var gameSession in gameSessions)
            {
                if (gameSession != null)
                {
                    var gameRound = gameSessions.IndexOf(gameSession) + 1;
                    var gameRecord = ToRecord(match, gameRound, playerId);
                    gameRecords.Add(gameRecord);
                }
            }

            var matchHistory = match.GetHistory();
            var matchHistoryStr = matchHistory.ToString();
            var result = GetResult(match, playerId);

            return new MatchRecordContract()
            {
                Id = match.Id,
                PlayerId = playerId,
                Modus = match.Modus,
                Variant = match.Variant,
                Type = match.Type,
                Games = gameRecords,
                MatchHistory = matchHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT,
                Result = result
            };
        }

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

        private static MatchResult GetResult(IMatchSessionModel match, Guid playerId)
        {
            if (match.Player1.Id.Equals(playerId))
            {
                return match.Player1.Points > match.Player2.Points ? MatchResult.Won : MatchResult.Lost;
            }
            else if (match.Player2.Id.Equals(playerId))
            {
                return match.Player2.Points > match.Player1.Points ? MatchResult.Won : MatchResult.Lost;
            }
            throw new InvalidOperationException("Player is not part of this match session.");
        }

        private static bool IsWhite(IMatchSessionModel match, Guid playerId)
        {
            // player 1 plays always with white checkers
            if (match.Player1.Id.Equals(playerId))
            {
                return true;
            }
            // player 2 plays always with black checkers
            else if (match.Player2.Id.Equals(playerId))
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
