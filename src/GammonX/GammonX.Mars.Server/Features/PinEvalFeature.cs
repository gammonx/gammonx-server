using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Evaluates the current pin state of the board for both player and opponent.
    /// Reads <see cref="IPinModel.PinnedFields"/> directly — O(24), no probability computation needed.
    /// </summary>
    public sealed class PinEvalFeature : IFeature<PinEvalResult>
    {
        // <inheritdoc />
        public PinEvalResult Eval(IBoardModel board, bool isWhite)
        {
            if (board is not IPinModel pinModel)
                return new PinEvalResult(0, 0, 0, 0);

            var pinnedOppCount = 0;
            var pinnedPlayerCount = 0;

            for (var i = 0; i < pinModel.PinnedFields.Length; i++)
            {
                var val = pinModel.PinnedFields[i];
                if (val == 0) continue;

                if (isWhite)
                {
                    // PinnedFields[i] > 0 = black (opponent) checker pinned by white (player)
                    // PinnedFields[i] < 0 = white (player) checker pinned by black (opponent)
                    if (val > 0) pinnedOppCount += val;
                    else pinnedPlayerCount += Math.Abs(val);
                }
                else
                {
                    // PinnedFields[i] < 0 = white (opponent) checker pinned by black (player)
                    // PinnedFields[i] > 0 = black (player) checker pinned by white (opponent)
                    if (val < 0) pinnedOppCount += Math.Abs(val);
                    else pinnedPlayerCount += val;
                }
            }

            int oppMotherIdx;
            int playerMotherIdx;
            if (isWhite)
            {
                oppMotherIdx = board.StartRangeBlack.Start.Value;
                playerMotherIdx = board.StartRangeWhite.Start.Value;
            }
            else
            {
                oppMotherIdx = board.StartRangeWhite.Start.Value;
                playerMotherIdx = board.StartRangeBlack.Start.Value;
            }

            var oppMotherPinned = isWhite
                ? (pinModel.PinnedFields[oppMotherIdx] > 0 ? 1 : 0)
                : (pinModel.PinnedFields[oppMotherIdx] < 0 ? 1 : 0);

            var playerMotherPinned = isWhite
                ? (pinModel.PinnedFields[playerMotherIdx] < 0 ? 1 : 0)
                : (pinModel.PinnedFields[playerMotherIdx] > 0 ? 1 : 0);

            return new PinEvalResult(pinnedOppCount, pinnedPlayerCount, oppMotherPinned, playerMotherPinned);
        }
    }

    public readonly record struct PinEvalResult(
        int PinnedOppCount,
        int PinnedPlayerCount,
        int OppMotherPinned,
        int PlayerMotherPinned
    );
}