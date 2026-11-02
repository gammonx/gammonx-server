using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Performance;

public class NeuralInputComparerTests
{
    [Fact]
    public void EqualityUsesEveryBitIncludingSignedZeroAndNanPayload()
    {
        var comparer = NeuralInputComparer.Instance;
        var minusZero = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
        var firstNan = BitConverter.Int32BitsToSingle(0x7FC00001);
        var secondNan = BitConverter.Int32BitsToSingle(0x7FC00002);
        Assert.False(comparer.Equals([0f], [minusZero]));
        Assert.False(comparer.Equals([firstNan], [secondNan]));
        Assert.True(comparer.Equals([firstNan, minusZero], [firstNan, minusZero]));
        Assert.False(comparer.Equals([1], [1, 0]));
        Assert.False(comparer.Equals(null, [1]));
        Assert.Equal(comparer.GetHashCode([1, 2, 3]), comparer.GetHashCode([1, 2, 3]));
    }

    [Fact]
    public void HashCollisionsDoNotMergeDifferentInputs()
    {
        var inputs = new HashSet<float[]>(new CollidingComparer());
        Assert.True(inputs.Add([1, 3]));
        Assert.True(inputs.Add([1, 4]));
        Assert.False(inputs.Add([1, 3]));
        Assert.Equal(2, inputs.Count);
    }

    [Fact]
    public void HistoryAndPerspectiveRemainPartOfTheInputIdentity()
    {
        var service = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = service.CreateBoard();
        var extractor = new DefaultFeatureVectorExtractor();
        var first = extractor.Extract(new NormalizedEvalResultModel(), board, true);
        service.AddRollEventToHistory(board, true, [2, 3]);
        var withHistory = extractor.Extract(new NormalizedEvalResultModel(), board, true);
        Assert.False(NeuralInputComparer.Instance.Equals(first, withHistory));
        board.Fields[0] = -1;
        var white = extractor.Extract(new NormalizedEvalResultModel(), board, true);
        var black = extractor.Extract(new NormalizedEvalResultModel(), board, false);
        Assert.False(NeuralInputComparer.Instance.Equals(white, black));
    }

    private sealed class CollidingComparer : IEqualityComparer<float[]>
    {
        public bool Equals(float[]? left, float[]? right) => NeuralInputComparer.Instance.Equals(left, right);
        public int GetHashCode(float[] values) => 0;
    }
}
