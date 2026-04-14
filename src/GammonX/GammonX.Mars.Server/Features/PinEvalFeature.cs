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
            IBoardModel playersBoard = isWhite ? board.InvertBoard() : board;

            if (playersBoard is not IPinModel pinModel)
                return new PinEvalResult(0, 0, 0, 0);

            var pinnedOppCount = 0;
            var pinnedPlayerCount = 0;

            for (var i = 0; i < pinModel.PinnedFields.Length; i++)
            {
                // PinnedFields[i] < 0 = white (opponent) checker pinned by black (player)
                // PinnedFields[i] > 0 = black (player) checker pinned by white (opponent)
                if (pinModel.PinnedFields[i] < 0)
                    pinnedOppCount += Math.Abs(pinModel.PinnedFields[i]);
                else if (pinModel.PinnedFields[i] > 0)
                    pinnedPlayerCount += pinModel.PinnedFields[i];
            }

            var oppMotherIdx = playersBoard.StartRangeWhite.Start.Value;
            var playerMotherIdx = playersBoard.StartRangeBlack.Start.Value;

            var oppMotherPinned = pinModel.PinnedFields[oppMotherIdx] < 0 ? 1 : 0;
            var playerMotherPinned = pinModel.PinnedFields[playerMotherIdx] > 0 ? 1 : 0;

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