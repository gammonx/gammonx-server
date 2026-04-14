using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// A race means both sides have fully passed each other.
    /// the lowest-indexed white checker sits at a higher index than the highest-indexed black checker. 
    /// </summary>
    public sealed class RaceFeature : IFeature<bool>
    {
        // <inheritdoc />
        public bool Eval(IBoardModel board, bool isWhite)
        {
            // positive values = black checkers (move 23>0)
            // negative values = white checkers (move 0>23)
            var maxBlackIndex = -1;
            var minWhiteIndex = board.Fields.Length;

            for (int i = 0; i < board.Fields.Length; i++)
            {
                if (board.Fields[i] > 0)
                {
                    maxBlackIndex = i;
                }
                else if (board.Fields[i] < 0)
                {
                    minWhiteIndex = Math.Min(minWhiteIndex, i);
                }
            }

            // in Plakoto, a pinned checker is hidden from Fields[]: the pinner's checker occupies
            // the field while the pinned checker is recorded only in PinnedFields[]. A pinned
            // checker is still on the board and must be included in the contact check — omitting
            // it causes a false race when e.g. a white checker is pinned deep in black's home.
            if (board is IPinModel pinModel)
            {
                for (int i = 0; i < pinModel.PinnedFields.Length; i++)
                {
                    if (pinModel.PinnedFields[i] < 0) // white checker pinned by black at i
                        minWhiteIndex = Math.Min(minWhiteIndex, i);
                    else if (pinModel.PinnedFields[i] > 0) // black checker pinned by white at i
                        maxBlackIndex = Math.Max(maxBlackIndex, i);
                }
            }

            // race: all black checkers are at lower indices than all white checkers
            // meaning both sides have passed each other with no contact remaining
            return minWhiteIndex >= maxBlackIndex;
        }
    }
}
