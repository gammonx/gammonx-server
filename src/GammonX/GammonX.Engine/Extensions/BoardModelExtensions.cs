using GammonX.Engine.Models;

using GammonX.Models;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Engine.Extensions
{
    public static class BoardModelExtensions
    {
        /// <summary>
        /// Constructs a board state contract from the given model.
        /// </summary>
        /// <param name="model">Model to construct from.</param>
        /// <param name="inverted">Boolean indicating if some of the board values has to be inverted.</param>
        /// <returns>A board state contract.</returns>
        public static BoardModelContract ToContract(this IBoardModel model, bool inverted)
        {
            var boardState = new BoardModelContract();

            if (inverted)
            {
                model = model.InvertBoard();
            }

            boardState.Fields = model.Fields;
            boardState.BearOffCountWhite = model.BearOffCountWhite;
            boardState.BearOffCountBlack = model.BearOffCountBlack;
            boardState.PipCountWhite = model.PipCountWhite;
            boardState.PipCountBlack = model.PipCountBlack;

            if (model is IPinModel pinModel)
            {
                boardState.PinnedFields = pinModel.PinnedFields;
            }
            else
            {
                boardState.PinnedFields = null;
            }

            if (model is IHomeBarModel homeBarModel)
            {
                boardState.HomeBarCountWhite = homeBarModel.HomeBarCountWhite;
                boardState.HomeBarCountBlack = homeBarModel.HomeBarCountBlack;
            }

            if (model is IDoublingCubeModel doublingCubeModel)
            {
                boardState.DoublingCubeValue = doublingCubeModel.DoublingCubeValue;
                boardState.DoublingCubeOwner = doublingCubeModel.DoublingCubeOwner;
            }

            return boardState;
        }

        public static GameResultModel ToGameResult(this IBoardModel board, Guid winnerId, bool isWhite)
        {
            if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
            {
                // if both players hit heir opponents mother checker
                // the game ends in a tie and concluded with 0 points
                return GameResultModel.Draw();
            }

            var bearOffCountPlayer = isWhite ? board.BearOffCountWhite : board.BearOffCountBlack;
            if (bearOffCountPlayer != board.WinConditionCount)
                throw new InvalidOperationException("Player 1 cannot win the game, because not all checkers are borne off.");

            // we support backgammon, gammon and doubling cubes (e.g. in game modus backgammon)
            if (board is IDoublingCubeModel cubeModel && board is IHomeBarModel homeBarModel)
            {
                var cubeValue = cubeModel.DoublingCubeValue;
                var bearOffCountOpp = isWhite ? board.BearOffCountBlack : board.BearOffCountWhite;
                var homeBarCountOpp = isWhite ? homeBarModel.HomeBarCountBlack : homeBarModel.HomeBarCountWhite;
                if (bearOffCountOpp == 0 && (homeBarCountOpp > 0 || LoserHasCheckersInWinnersHomeBoard(board, isWhite)))
                {
                    var points = 3 * cubeValue;
                    var result = new GameResultModel(winnerId, GameResult.Backgammon, GameResult.LostBackgammon, points);
                    return result;
                }
                if (bearOffCountOpp == 0)
                {
                    var points = 2 * cubeValue;
                    var result = new GameResultModel(winnerId, GameResult.Gammon, GameResult.LostGammon, points);
                    return result;
                }
                else
                {
                    var points = 1 * cubeValue;
                    var result = new GameResultModel(winnerId, GameResult.Single, GameResult.LostSingle, points);
                    return result;

                }
            }
            // support for gammon wins (e.g. tavli and tavla)
            else
            {
                var bearOffCountOpp = isWhite ? board.BearOffCountBlack : board.BearOffCountWhite;

                if (bearOffCountOpp == 0)
                {
                    return new GameResultModel(winnerId, GameResult.Gammon, GameResult.LostGammon, 2);
                }
                else
                {
                    return new GameResultModel(winnerId, GameResult.Single, GameResult.LostSingle, 1);
                }
            }
        }

        private static bool LoserHasCheckersInWinnersHomeBoard(IBoardModel model, bool isWhite)
        {
            if (isWhite)
            {
                var blackHasCheckersThere = model.Fields
                    .Take(model.HomeRangeWhite)
                    .Any(v => v > 0);
                return blackHasCheckersThere;
            }
            else
            {
                if (model.HomeRangeBlack.Start.Value > model.HomeRangeBlack.End.Value)
                {
                    var whiteHasCheckersThere = model.Fields
                        .Take(new Range(model.HomeRangeBlack.End.Value, model.HomeRangeBlack.Start.Value))
                        .Any(v => v < 0);
                    return whiteHasCheckersThere;
                }
                else
                {
                    var whiteHasCheckersThere = model.Fields
                        .Take(model.HomeRangeBlack)
                        .Any(v => v < 0);
                    return whiteHasCheckersThere;
                }
            }
        }
    }
}
