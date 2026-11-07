namespace GammonX.Mars.Training.Tournament
{
    internal sealed class TournamentScore
    {
        public int ModelAWins { get; private set; }

        public int ModelBWins { get; private set; }

        public int ModelAGammonWins { get; private set; }

        public int ModelABackgammonWins { get; private set; }

        public int ModelBGammonWins { get; private set; }

        public int ModelBBackgammonWins { get; private set; }

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

            // Gammons count only two-point wins; backgammons are a separate category.
            if (points > 0)
            {
                ModelAWins++;
                if (points == 2)
                {
                    ModelAGammonWins++;
                }
                else if (points == 3)
                {
                    ModelABackgammonWins++;
                }
            }
            else if (points < 0)
            {
                ModelBWins++;
                if (points == -2)
                {
                    ModelBGammonWins++;
                }
                else if (points == -3)
                {
                    ModelBBackgammonWins++;
                }
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
