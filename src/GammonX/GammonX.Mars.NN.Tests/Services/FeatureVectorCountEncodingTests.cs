using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public class FeatureVectorCountEncodingTests
{
    public static IEnumerable<object[]> Variants() =>
        new[] { GameModus.Backgammon, GameModus.Portes, GameModus.Tavla, GameModus.Fevga, GameModus.Plakoto }
            .SelectMany(modus => new[] { true, false }.Select(isWhite => new object[] { modus, isWhite }));

    [Theory]
    [MemberData(nameof(Variants))]
    public void EncodesEveryStackSizeForBothPlayers(GameModus modus, bool isWhite)
    {
        var board = BoardServiceFactory.Create(modus).CreateBoard();
        Array.Clear(board.Fields);
        var extractor = FeatureVectorExtractorFactory.Create(modus);
        var model = new NormalizedEvalResultModel();
        var buffer = new float[extractor.FeatureCount];
        const int point = 7;
        // Fevga changes perspective by rotating twelve points; the other games mirror the board.
        var boardPoint = isWhite ? point : modus == GameModus.Fevga ? (point + 12) % 24 : 23 - point;
        var offset = (modus == GameModus.Plakoto ? 26 : 24) + point * 8;
        float[] tails = [0, 0, 0, 0, 1f / 12, 1f / 6, 1f / 4, 1f / 3, 5f / 12,
            1f / 2, 7f / 12, 2f / 3, 3f / 4, 5f / 6, 11f / 12, 1];

        for (var count = -15; count <= 15; count++)
        {
            board.Fields[boardPoint] = isWhite ? count : -count;
            var magnitude = Math.Abs(count);
            float[] stack = [magnitude >= 1 ? 1 : 0, magnitude >= 2 ? 1 : 0,
                magnitude >= 3 ? 1 : 0, tails[magnitude]];
            var expected = new float[8];
            stack.CopyTo(expected, count >= 0 ? 0 : 4);

            // Reuse the buffer across decreasing, empty and increasing stacks.
            extractor.Extract(model, board, isWhite, buffer);
            var allocated = extractor.Extract(model, board, isWhite);

            Assert.Equal(expected, buffer.AsSpan(offset, 8).ToArray());
            Assert.Equal(allocated, buffer);
            Assert.All(buffer, value => Assert.InRange(value, 0f, 1f));
            Assert.Equal(isWhite ? count : -count, board.Fields[boardPoint]);
        }
    }
}
