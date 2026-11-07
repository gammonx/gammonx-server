namespace GammonX.Mars.Training.Tournament
{
    internal sealed class TournamentScore
    {
        public int ModelAWins { get; private set; }

        public int ModelBWins { get; private set; }

        public int Draws { get; private set; }

        public int Discarded { get; private set; }

        public long TotalTurns { get; private set; }

        public long ModelATotalPoints { get; private set; }

        public long ModelASquaredPoints { get; private set; }

        public int CompletedGames => ModelAWins + ModelBWins + Draws;

        public void RecordResult(TournamentGameResult result, bool modelAIsWhite)
        {
            if (result.Discarded)
            {
                RecordDiscardedGame(result.TurnCount);
                return;
            }

            var points = TournamentRunner.GetModelAPoints(result.WhiteResult, modelAIsWhite);
            TotalTurns += result.TurnCount;
            ModelATotalPoints += points;
            ModelASquaredPoints += points * points;

            if (points > 0)
            {
                ModelAWins++;
            }
            else if (points < 0)
            {
                ModelBWins++;
            }
            else
            {
                Draws++;
            }
        }

        public void RecordDiscardedGame(int turnCount = 0)
        {
            Discarded++;
            TotalTurns += turnCount;
        }
    }
}