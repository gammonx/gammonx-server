using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Calculates the number of checkers of the player that are in front of the last pinned opponent checker which resides
    /// in the players home board. This is an important feature for the plakoto variant, as the player can only bear off checkers
    /// if all of his checkers are in his home board and there is no pinned opponent checker in front of them. 
    /// The more checkers the player has in front of the last pinned opponent checker, the better his chances to bear off checkers 
    /// and win the game with a gammon (2pts).
    /// </summary>
    public sealed class NumbersOfCheckersInFronOfLastPinFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            IBoardModel playersBoard = GetBoard(board, isWhite);
            IPinModel pinModel = (IPinModel)playersBoard;
            // scan black's home range ascending (0>5): count black checkers until the first
            // pinned white is found — that is the last pin black encounters on its path to bear-off
            var number = 0;
            var hasPinnedWhite = false;
            for (int i = playersBoard.HomeRangeBlack.End.Value; i <= playersBoard.HomeRangeBlack.Start.Value; i++)
            {
                if (pinModel.PinnedFields[i] < 0)
                {
                    hasPinnedWhite = true;
                    break;
                }
                if (playersBoard.Fields[i] > 0)
                {
                    number += playersBoard.Fields[i];
                }
            }
            return hasPinnedWhite ? number : 0;
        }

        private static IBoardModel GetBoard(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                return board.InvertBoard();
            }

            return board;
        }
    }
}
