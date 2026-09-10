namespace GammonX.Mars.Training;

public static class WellKnownCsvHeaders
{
    public const string TrainingDataHeaderLegacy = "pWin";

    public const string TrainingDataHeader = "pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss";

    public const string ExplorationSidecarHeader = "gameId,modus,turnIndex,earlyPhase,againstBot,candidateCount,bestScore,secondBestScore,scoreGap,choice,selectedRank,selectedScore";

    public const string SelectiveTwoPlySearchSidecarHeader =
            "gameId,modus,turnIndex,againstBot,candidateCount,evaluatedCandidateCount,selectiveCandidateLimit,onePlyBestScore,onePlySecondBestScore,onePlyScoreGap,reason,twoPlyBestScore,twoPlySecondBestScore,twoPlyScoreGap,bestMoveChanged,twoPlyBestOnePlyRank";

    public const string TrajectorySidecarHeader = "gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss";

    public const string GamesSidecarHeader = "gameId,totalTurns,whiteWon,winnerResult,loserResult";
}