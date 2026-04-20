using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Calculates the length of the longest prime of the given player
    /// </summary>
    public class MaxPrimeLengthFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                var whiteAnchors = board.Fields.Index().Where(i => i.Item <= -board.BlockAmount);
                if (board is IPinModel pinModel)
                {
                    var potWhiteAnchors = board.Fields.Index()
                        .Where(i => i.Item == -(board.BlockAmount - 1) && pinModel.PinnedFields[i.Index] == board.BlockAmount - 1);
                    whiteAnchors = whiteAnchors.Concat(potWhiteAnchors);
                }
                return LongestConsecutiveRun(whiteAnchors.Select(i => i.Index));
            }
            else
            {
                var blackAnchors = board.Fields.Index().Where(i => i.Item >= board.BlockAmount);
                if (board is IPinModel pinModel)
                {
                    var potBlackAnchors = board.Fields.Index()
                        .Where(i => i.Item == board.BlockAmount - 1 && pinModel.PinnedFields[i.Index] == -(board.BlockAmount - 1));
                    blackAnchors = blackAnchors.Concat(potBlackAnchors);
                }
                return LongestConsecutiveRun(blackAnchors.Select(i => i.Index));
            }
        }

        private static int LongestConsecutiveRun(IEnumerable<int> indices)
        {
            var sorted = indices.Order().ToArray();
            if (sorted.Length == 0)
                return 0;

            int longest = 1;
            int current = 1;
            for (int i = 1; i < sorted.Length; i++)
            {
                if (sorted[i] == sorted[i - 1] + 1)
                {
                    current++;
                    if (current > longest)
                        longest = current;
                }
                else if (sorted[i] != sorted[i - 1])
                {
                    current = 1;
                }
            }
            return longest;
        }
    }
}
