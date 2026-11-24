using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Engine.Models;

namespace GammonX.Server.Extensions
{
    /// <summary>
    /// Holds both player views of a completed match for reuse across publications.
    /// </summary>
    /// <param name="Player1">The white player's record.</param>
    /// <param name="Player2">The black player's record.</param>
    public record MatchRecordContracts(
        MatchRecordContract Player1,
        MatchRecordContract Player2
    );

    /// <summary>
    /// Holds both player views of one completed game, sharing its formatted history.
    /// </summary>
    /// <param name="Player1">The white player's record.</param>
    /// <param name="Player2">The black player's record.</param>
    public record GameRecordContracts(
        GameRecordContract Player1,
        GameRecordContract Player2
    );

    public static class MatchSessionExtensions
    {
        /// <summary>
        /// Builds both player records for a completed match, sharing their formatted history strings.
        /// Treat the returned records as read-only while publishing them to multiple queues.
        /// </summary>
        /// <param name="match">The completed match.</param>
        /// <returns>The two player views.</returns>
        public static MatchRecordContracts ToRecords(this IMatchSessionModel match)
        {
            var player1Id = match.Player1.Id;
            var player2Id = match.Player2.Id;
            var player1GameRecords = new List<GameRecordContract>();
            var player2GameRecords = new List<GameRecordContract>();
            var gameSessions = match.GetGameSessions().ToList();

            foreach (var gameSession in gameSessions)
            {
                if (gameSession != null)
                {
                    var gameRound = gameSessions.IndexOf(gameSession) + 1;
                    var gameRecords = ToRecords(match, gameRound);
                    player1GameRecords.Add(gameRecords.Player1);
                    player2GameRecords.Add(gameRecords.Player2);
                }
            }

            var matchHistory = match.GetHistory();
            var matchHistoryStr = matchHistory.ToString();

            var player1Record = new MatchRecordContract()
            {
                Id = match.Id,
                PlayerId = player1Id,
                Modus = match.Modus,
                Variant = match.Variant,
                Type = match.Type,
                BotLevel = match.BotLevel,
                Games = player1GameRecords,
                MatchHistory = matchHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT,
                Result = GetResult(match, player1Id)
            };

            var player2Record = new MatchRecordContract()
            {
                Id = match.Id,
                PlayerId = player2Id,
                Modus = match.Modus,
                Variant = match.Variant,
                Type = match.Type,
                BotLevel = match.BotLevel,
                Games = player2GameRecords,
                MatchHistory = matchHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT,
                Result = GetResult(match, player2Id)
            };

            return new MatchRecordContracts(player1Record, player2Record);
        }

        /// <summary>
        /// Builds both player records for the specified completed round.
        /// </summary>
        /// <param name="match">The owning match.</param>
        /// <param name="gameRound">The one-based completed round number.</param>
        /// <returns>The two player views, sharing a formatted game history.</returns>
        public static GameRecordContracts ToRecords(this IMatchSessionModel match, int gameRound)
        {
            var game = match.GetGameSession(gameRound);

            if (game == null)
            {
                throw new InvalidOperationException($"no game session found for round '{gameRound}'");
            }

            var player1Id = match.Player1.Id;
            var player2Id = match.Player2.Id;

            int? doublingCubeValue = null;
            if (game.BoardModel is IDoublingCubeModel cubeBoard)
            {
                doublingCubeValue = cubeBoard.DoublingCubeValue;
            }

            var gameHistory = game.GetHistory(match.Player1.Id, match.Player2.Id);
            var gameHistoryStr = gameHistory.ToString();

            var player1IsWhite = IsWhite(match, player1Id);
            var player1Record = new GameRecordContract()
            {
                MatchId = match.Id,
                Id = game.Id,
                DoublingCubeValue = doublingCubeValue,
                PlayerId = player1Id,
                PipesLeft = GetPipeCount(game, player1IsWhite),
                Result = game.Result.GetResult(player1Id),
                GameHistory = gameHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT
            };

            var player2IsWhite = IsWhite(match, player2Id);
            var player2Record = new GameRecordContract()
            {
                MatchId = match.Id,
                Id = game.Id,
                DoublingCubeValue = doublingCubeValue,
                PlayerId = player2Id,
                PipesLeft = GetPipeCount(game, player2IsWhite),
                Result = game.Result.GetResult(player2Id),
                GameHistory = gameHistoryStr ?? string.Empty,
                Format = HistoryFormat.MAT
            };

            return new GameRecordContracts(player1Record, player2Record);
        }

        private static MatchResult GetResult(IMatchSessionModel match, Guid playerId)
        {
            if (match.Player1.Id.Equals(playerId))
            {
                if (match.Player1.Points > match.Player2.Points)
                {
                    return MatchResult.Won;
                }
                else if (match.Player1.Points < match.Player2.Points)
                {
                    return MatchResult.Lost;
                }
                else
                {
                    return MatchResult.Unknown;
                }
            }
            else if (match.Player2.Id.Equals(playerId))
            {
                if (match.Player2.Points > match.Player1.Points)
                {
                    return MatchResult.Won;
                }
                else if (match.Player2.Points < match.Player1.Points)
                {
                    return MatchResult.Lost;
                }
                else
                {
                    return MatchResult.Unknown;
                }
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
