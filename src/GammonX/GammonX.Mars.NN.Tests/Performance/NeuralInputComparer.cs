using System.Runtime.InteropServices;

namespace GammonX.Mars.NN.Tests.Performance;

internal sealed class NeuralInputComparer : IEqualityComparer<float[]>
{
    public static NeuralInputComparer Instance { get; } = new();

    public bool Equals(float[]? left, float[]? right) =>
        ReferenceEquals(left, right) ||
        (left is not null && right is not null &&
         MemoryMarshal.AsBytes(left.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(right.AsSpan())));

    public int GetHashCode(float[] values)
    {
        var hash = new HashCode();
        hash.Add(values.Length);
        hash.AddBytes(MemoryMarshal.AsBytes(values.AsSpan()));
        return hash.ToHashCode();
    }
}
