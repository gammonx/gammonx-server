using GammonX.Mars.NN.Services;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tournament
{
    public sealed record TournamentEntry(
        string? Path,
        BotLevel Level,
        bool? IsWhite,
        INeuralEvalService? Service
    );

    public sealed record TournamentGameResult(
        GameResult WhiteResult,
        int TurnCount,
        bool Discarded
    );

    public readonly record struct BotPlayerAssignment(
        Guid ModelPlayerId,
        Guid WildBgPlayerId
    );

    public sealed record TournamentResult(
        string ModelALabel,
        string ModelBLabel,
        int TotalGames,
        int ModelAWins,
        int ModelBWins,
        int Draws,
        int Discarded,
        double AvgTurns,
        long ModelATotalPoints,
        long ModelASquaredPoints)
    {
        public int ModelAGammonWins { get; init; }

        public int ModelABackgammonWins { get; init; }

        public int ModelBGammonWins { get; init; }

        public int ModelBBackgammonWins { get; init; }

        public int CompletedGames => ModelAWins + ModelBWins + Draws;
    }

    internal readonly record struct TournamentEquityStatistics(
        double? PointsPerGame,
        double? LowerBound,
        double? UpperBound
    );
}
