using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;

using static TorchSharp.torch;

namespace GammonX.Mars.Training;

/// <summary>
/// Streams batches of (features, labels) tensors from a CSV file on disk,
/// using a pre-built row-offset index to seek directly to rows in any order.
/// This avoids loading the entire dataset into RAM.
/// </summary>
public sealed class CsvBatchEnumerator : IEnumerable<(Tensor features, Tensor labels)>
{
    private readonly string _csvPath;
    private readonly int _batchSize;
    private readonly int _labelCount;
    private readonly int _featureCols;
    private readonly long[] _offsets;
    private readonly int[] _rowOrder;
    private readonly Device _device;
    private readonly int[]? _labelPermutation;

    public CsvBatchEnumerator(
        string csvPath,
        int batchSize,
        int labelCount,
        int featureCols,
        long[] offsets,
        int[] rowOrder,
        Device device,
        int[]? labelPermutation = null)
    {
        _csvPath = csvPath;
        _batchSize = batchSize;
        _labelCount = labelCount;
        _featureCols = featureCols;
        _offsets = offsets;
        _rowOrder = rowOrder;
        _device = device;
        _labelPermutation = labelPermutation;
    }

    /// <summary>
    /// Scans the CSV file once to build a byte-offset index for every data row.
    /// Uses manual byte-offset tracking because StreamReader buffers ahead of FileStream.Position.
    /// </summary>
    public static (long[] offsets, int totalRows, int featureCols, string header) BuildRowIndex(string path, int labelCount)
    {
        var offsets = new List<long>();
        var encoding = System.Text.Encoding.UTF8;
        var newLineBytes = DetectNewLineByteCount(path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
        using var reader = new StreamReader(stream, encoding);

        var header = reader.ReadLine()!;
        var featureCols = header.Split(',').Length - labelCount;

        var byteOffset = DetectBomLength(path) + encoding.GetByteCount(header) + newLineBytes;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                offsets.Add(byteOffset);
                byteOffset += encoding.GetByteCount(line) + newLineBytes;
            }
            else
            {
                byteOffset += newLineBytes;
            }
        }

        return (offsets.ToArray(), offsets.Count, featureCols, header);
    }

    private const int ProducerCount = 12;

    public IEnumerator<(Tensor features, Tensor labels)> GetEnumerator()
    {
        // Bounded capacity limits memory: producers block when the queue is full
        var queue = new BlockingCollection<(Tensor features, Tensor labels)>(boundedCapacity: ProducerCount * 3);

        var totalRows = _rowOrder.Length;

        // Partition batches across producers: each gets a contiguous range of batch starts
        var allBatchStarts = new List<int>();
        for (var b = 0; b < totalRows; b += _batchSize)
        {
            allBatchStarts.Add(b);
        }

        var remaining = ProducerCount;
        var producers = new Task[ProducerCount];

        for (var p = 0; p < ProducerCount; p++)
        {
            // Slice of batch starts for this producer
            var chunkSize = (allBatchStarts.Count + ProducerCount - 1) / ProducerCount;
            var start = p * chunkSize;
            var end = Math.Min(start + chunkSize, allBatchStarts.Count);
            var myBatchStarts = allBatchStarts.GetRange(start, end - start);

            producers[p] = Task.Factory.StartNew(() =>
            {
                // Each producer owns its own buffers and file handles
                var featBuf = new float[_batchSize * _featureCols];
                var lblBuf = new float[_batchSize * _labelCount];

                using var stream = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
                using var reader = new StreamReader(stream);

                FileStream? labelStream = _labelPermutation != null
                    ? new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16)
                    : null;
                StreamReader? labelReader = labelStream != null ? new StreamReader(labelStream) : null;

                try
                {
                    foreach (var batchStart in myBatchStarts)
                    {
                        var count = Math.Min(_batchSize, totalRows - batchStart);

                        for (var i = 0; i < count; i++)
                        {
                            var rowIdx = _rowOrder[batchStart + i];

                            stream.Seek(_offsets[rowIdx], SeekOrigin.Begin);
                            reader.DiscardBufferedData();
                            var line = reader.ReadLine()!;
                            ParseFeatures(line.AsSpan(), featBuf, i, _featureCols);

                            if (_labelPermutation == null)
                            {
                                ParseLabels(line.AsSpan(), lblBuf, i, _featureCols, _labelCount);
                            }
                            else
                            {
                                var labelRowIdx = _labelPermutation[rowIdx];
                                labelStream!.Seek(_offsets[labelRowIdx], SeekOrigin.Begin);
                                labelReader!.DiscardBufferedData();
                                var labelLine = labelReader.ReadLine()!;
                                ParseLabels(labelLine.AsSpan(), lblBuf, i, _featureCols, _labelCount);
                            }
                        }

                        var features = tensor(featBuf.AsSpan(0, count * _featureCols).ToArray(), [count, _featureCols], null, _device);
                        var labels = _labelCount == 1
                                    ? tensor(lblBuf.AsSpan(0, count).ToArray(), [count], null, _device)
                                    : tensor(lblBuf.AsSpan(0, count * _labelCount).ToArray(), [count, _labelCount], null, _device);

                        queue.Add((features, labels));
                    }
                }
                finally
                {
                    labelReader?.Dispose();
                    labelStream?.Dispose();

                    // Last producer to finish marks the queue complete
                    if (Interlocked.Decrement(ref remaining) == 0)
                        queue.CompleteAdding();
                }
            }, TaskCreationOptions.LongRunning);
        }

        // Consumer: yield batches as they become available
        foreach (var batch in queue.GetConsumingEnumerable())
        {
            yield return batch;
        }

        // Propagate any producer exceptions
        Task.WaitAll(producers);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ParseFeatures(ReadOnlySpan<char> span, float[] buffer, int rowInBatch, int featureCols)
    {
        var colIndex = 0;
        while (!span.IsEmpty && colIndex < featureCols)
        {
            var commaPos = span.IndexOf(',');
            var field = commaPos >= 0 ? span[..commaPos] : span;
            buffer[rowInBatch * featureCols + colIndex] = float.Parse(field, CultureInfo.InvariantCulture);
            colIndex++;
            span = commaPos >= 0 ? span[(commaPos + 1)..] : [];
        }
    }

    private static void ParseLabels(ReadOnlySpan<char> span, float[] buffer, int rowInBatch, int featureCols, int labelCount)
    {
        // We skip feature columns
        var colIndex = 0;
        while (!span.IsEmpty && colIndex < featureCols)
        {
            var commaPos = span.IndexOf(',');
            colIndex++;
            span = commaPos >= 0 ? span[(commaPos + 1)..] : [];
        }

        // We parse label columns
        var lblIdx = 0;
        while (!span.IsEmpty && lblIdx < labelCount)
        {
            var commaPos = span.IndexOf(',');
            var field = commaPos >= 0 ? span[..commaPos] : span;
            buffer[rowInBatch * labelCount + lblIdx] = float.Parse(field, CultureInfo.InvariantCulture);
            lblIdx++;
            span = commaPos >= 0 ? span[(commaPos + 1)..] : [];
        }
    }

    private static long DetectBomLength(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> buf = stackalloc byte[4];
        var read = stream.Read(buf);
        if (read >= 3 && buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF) return 3; // UTF-8
        if (read >= 2 && buf[0] == 0xFF && buf[1] == 0xFE) return 2; // UTF-16 LE
        if (read >= 2 && buf[0] == 0xFE && buf[1] == 0xFF) return 2; // UTF-16 BE
        return 0;
    }

    private static int DetectNewLineByteCount(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        var buf = new byte[Math.Min(8192, stream.Length)];
        var read = stream.Read(buf, 0, buf.Length);

        for (var i = 0; i < read; i++)
        {
            if (buf[i] == (byte)'\n')
                return (i > 0 && buf[i - 1] == (byte)'\r') ? 2 : 1;
        }

        return 1;
    }
}
