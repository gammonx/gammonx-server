using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace GammonX.Diagnostics.Server;

/// <summary>
/// Counts bytes as JSON messages are serialized, including the SignalR envelope and separator.
/// A cached serialization can serve multiple recipients; this is not a network egress counter.
/// </summary>
internal sealed class MetricsJsonHubProtocol : IHubProtocol
{
    private readonly JsonHubProtocol _inner;

    public MetricsJsonHubProtocol(IOptions<JsonHubProtocolOptions> options)
    {
        _inner = new JsonHubProtocol(options);
    }

    public string Name => _inner.Name;

    public int Version => _inner.Version;

    public TransferFormat TransferFormat => _inner.TransferFormat;

    public bool IsVersionSupported(int version) => _inner.IsVersionSupported(version);

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
        => _inner.TryParseMessage(ref input, binder, out message);

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        var bytes = _inner.GetMessageBytes(message);
        ServerMetrics.MessageBytes.Record(bytes.Length);
        return bytes;
    }

    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    {
        if (!ServerMetrics.MessageBytes.Enabled)
        {
            _inner.WriteMessage(message, output);
            return;
        }

        var writer = new CountingWriter(output);
        _inner.WriteMessage(message, writer);
        ServerMetrics.MessageBytes.Record(writer.Bytes);
    }

    private sealed class CountingWriter(IBufferWriter<byte> output) : IBufferWriter<byte>
    {
        internal long Bytes { get; private set; }
        
        public Memory<byte> GetMemory(int sizeHint = 0) => output.GetMemory(sizeHint);
        
        public Span<byte> GetSpan(int sizeHint = 0) => output.GetSpan(sizeHint);

        public void Advance(int count)
        {
            output.Advance(count);
            Bytes += count;
        }
    }
}
