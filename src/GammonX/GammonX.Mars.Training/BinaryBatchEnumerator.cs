using Microsoft.Win32.SafeHandles;

using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;

using static TorchSharp.torch;

namespace GammonX.Mars.Training;

/// <summary>
/// Streams batches of (features, labels) tensors from a CSV file on disk,
/// using a pre-built row-offset index to seek directly to rows in any order.
/// This avoids loading the entire dataset into RAM.
/// </summary>
public sealed class BinaryBatchEnumerator : IEnumerable<(Tensor features, Tensor labels)>
{
    private readonly string _binaryPath;
    private readonly int _batchSize;
    private readonly int _labelCount;
    private readonly int _featureCount;
    private readonly int[] _rowOrder;
    private readonly Device _device;
    private readonly int[]? _labelPermutation;
    private readonly long _rowStrideBytes;

    private readonly int _producerCount;
    private readonly int _queueCapacity;

    public BinaryBatchEnumerator(
        string binaryPath,
        int batchSize,
        int labelCount,
        int featureCount,
        int[] rowOrder,
        Device device,
        int[]? labelPermutation = null,
        int producerCount = 1,
        int queueCapacity = 2)
    {
        _binaryPath = binaryPath;
        _batchSize = batchSize;
        _labelCount = labelCount;
        _featureCount = featureCount;
        _rowOrder = rowOrder;
        _device = device;
        _labelPermutation = labelPermutation;
        _rowStrideBytes = checked(((long)_featureCount + _labelCount) * sizeof(float));
        _producerCount = producerCount;
        _queueCapacity = queueCapacity;
    }

    /// <summary>
    /// Scans a CSV file to determine the number of features and rows, and converts it to a binary format for efficient access.
    /// </summary>
    /// <param name="sourceCsvPath">The path to the source CSV file.</param>
    /// <param name="binaryPath">The path to the output binary file.</param>
    /// <param name="labelCount">The number of label columns in the CSV file.</param>
    /// <returns>A tuple containing the number of feature columns, the number of rows, and the header string.</returns>
    public static (int featureCount, int rowCount, string header) ScanCsvAndConvertToBinary(string sourceCsvPath, string binaryPath, int labelCount)
    {
        var count = 0;
        var sum = 0.0;
        var min = float.MaxValue;
        var max = float.MinValue;
        var near05Count = 0;
        var name = Path.GetFileNameWithoutExtension(sourceCsvPath);

        using var stream = new FileStream(sourceCsvPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        using var reader = new StreamReader(stream);
        // We omit the header from the binary file
        var header = reader.ReadLine();
        if (string.IsNullOrEmpty(header))
            throw new InvalidDataException($"CSV file '{sourceCsvPath}' has no header row.");

        if (labelCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(labelCount), labelCount, "The label count must be greater than zero.");

        var expectedColumnCount = header.Split(',').Length;
        if (expectedColumnCount < labelCount)
        {
            throw new InvalidDataException($"CSV header in '{sourceCsvPath}' has {expectedColumnCount} columns, but {labelCount} label columns were requested.");
        }

        var featureCount = expectedColumnCount - labelCount;
        using var binaryWriter = new BinaryWriter(File.Create(binaryPath));
        var rowCount = 0;
        var physicalLineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            physicalLineNumber++;
            if (!string.IsNullOrEmpty(line))
            {
                var tokens = line.Split(',');
                if (tokens.Length != expectedColumnCount)
                {
                    throw new InvalidDataException($"CSV row {physicalLineNumber} in '{sourceCsvPath}' has {tokens.Length} columns; expected {expectedColumnCount}.");
                }

                var columnIndex = 0;
                foreach (var token in tokens)
                {
                    var value = float.Parse(token, CultureInfo.InvariantCulture);
                    binaryWriter.Write(value);
                    // We only read the first label (pWin) for statistics, as the other labels are derived from it
                    if (columnIndex == featureCount)
                    {
                        // We track some statistics about the labels
                        sum += value;
                        if (value < min)
                            min = value;
                        if (value > max)
                            max = value;
                        if (Math.Abs(value - 0.5f) < 0.05f)
                            near05Count++;
                        count++;
                    }
                    columnIndex++;
                }
                rowCount++;
            }
        }

        var mean = sum / count;
        var near05 = near05Count / (float)count;
        Console.WriteLine($"[{name}] n={count}  mean={mean:F4}  min={min:F4}  max={max:F4}  near-0.5={near05:P1}");

        return new ValueTuple<int, int, string>(featureCount, rowCount, header);
    }

    /// <summary>
    /// Builds an index of byte offsets for each row in the CSV file, allowing for random access to rows without loading the entire file into memory.
    /// </summary>
    /// <param name="path">The path to the CSV file.</param>
    /// <param name="labelCount">The number of label columns in the CSV file.</param>
    /// <returns>A tuple containing the array of byte offsets, the total number of rows, the number of feature columns, and the header string.</returns>
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

    // <inheritdoc />
    public IEnumerator<(Tensor features, Tensor labels)> GetEnumerator()
    {
        // We expect a bounded capacity to limit memory: producers block when the queue is full
        var queue = new BlockingCollection<(Tensor features, Tensor labels)>(boundedCapacity: _queueCapacity);

        var totalRows = _rowOrder.Length;

        // We partition batches across producers: each gets a contiguous range of batch starts
        var allBatchStarts = new List<int>();
        for (var b = 0; b < totalRows; b += _batchSize)
        {
            allBatchStarts.Add(b);
        }

        var remaining = _producerCount;
        var producers = new Task[_producerCount];

        for (var p = 0; p < _producerCount; p++)
        {
            // We slice of batch starts for this producer
            var chunkSize = (allBatchStarts.Count + _producerCount - 1) / _producerCount;
            var start = p * chunkSize;
            var end = Math.Min(start + chunkSize, allBatchStarts.Count);
            var count = end - start;

            if (count <= 0)
            {
                // This producer has no work to do
                Interlocked.Decrement(ref remaining);
                continue;
            }

            var myBatchStarts = allBatchStarts.GetRange(start, count);

            producers[p] = Task.Factory.StartNew(() =>
            {
                // We let each producer owns its own buffers and file handles
                var featBuf = new float[_batchSize * _featureCount];
                var lblBuf = new float[_batchSize * _labelCount];

                using SafeFileHandle handle = File.OpenHandle(
                    _binaryPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                try
                {
                    foreach (var batchStart in myBatchStarts)
                    {
                        var batchLength = Math.Min(_batchSize, totalRows - batchStart);

                        var values = new float[_featureCount + _labelCount];
                        var valueBytes = MemoryMarshal.AsBytes(values.AsSpan());
                        var labelValues = _labelPermutation == null ? null : new float[_featureCount + _labelCount];
                        var labelBytes = labelValues == null ? default : MemoryMarshal.AsBytes(labelValues.AsSpan());

                        for (var i = 0; i < batchLength; i++)
                        {
                            var rowIdx = _rowOrder[batchStart + i];
                            // We must ensure that the row offset is computed as a long to avoid integer overflow for large datasets
                            var rowOffset = checked((long)rowIdx * _rowStrideBytes);
                            ReadRow(handle, valueBytes, rowOffset, rowIdx);

                            ParseFeatures(values, featBuf, i, _featureCount);

                            if (_labelPermutation == null)
                            {
                                ParseLabels(values, lblBuf, i, _featureCount, _labelCount);
                                ValidateLabels(lblBuf, i, _labelCount, rowIdx);
                            }
                            else
                            {
                                if ((uint)rowIdx >= (uint)_labelPermutation.Length)
                                    throw new InvalidDataException($"Label permutation has no entry for row {rowIdx}.");

                                var labelRowIdx = _labelPermutation[rowIdx];
                                if ((uint)labelRowIdx >= (uint)totalRows)
                                    throw new InvalidDataException($"Label permutation maps row {rowIdx} to invalid row {labelRowIdx}.");

                                // We must ensure that the row offset is computed as a long to avoid integer overflow for large datasets
                                var labelRowOffset = checked((long)labelRowIdx * _rowStrideBytes);
                                ReadRow(handle, labelBytes, labelRowOffset, labelRowIdx);
                                ParseLabels(labelValues!, lblBuf, i, _featureCount, _labelCount);
                                ValidateLabels(lblBuf, i, _labelCount, labelRowIdx);
                            }
                        }

                        var features = tensor(featBuf.AsSpan(0, batchLength * _featureCount).ToArray(), [batchLength, _featureCount], null, _device);
                        var labels = _labelCount == 1
                                    ? tensor(lblBuf.AsSpan(0, batchLength).ToArray(), [batchLength], null, _device)
                                    : tensor(lblBuf.AsSpan(0, batchLength * _labelCount).ToArray(), [batchLength, _labelCount], null, _device);

                        queue.Add((features, labels));
                    }
                }
                finally
                {
                    // We let the last producer to finish marks the queue complete
                    if (Interlocked.Decrement(ref remaining) == 0)
                        queue.CompleteAdding();
                }
            }, TaskCreationOptions.LongRunning);
        }

        // We expect the consumer to yield batches as they become available
        foreach (var batch in queue.GetConsumingEnumerable())
        {
            yield return batch;
        }

        // We propagate any producer exceptions
        Task.WaitAll(producers.Where(p => p != null!).ToArray());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ParseFeatures(ReadOnlySpan<float> span, float[] buffer, int rowInBatch, int featureCols)
    {
        for (var colIndex = 0; colIndex < featureCols; colIndex++)
        {
            buffer[rowInBatch * featureCols + colIndex] = span[colIndex];
        }
    }

    private static void ReadRow(SafeFileHandle handle, Span<byte> buffer, long rowOffset, int rowIndex)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = RandomAccess.Read(handle, buffer[totalRead..], rowOffset + totalRead);
            if (bytesRead == 0)
            {
                throw new InvalidDataException($"Binary row {rowIndex} is truncated: expected {buffer.Length} bytes, but read {totalRead}.");
            }

            totalRead += bytesRead;
        }
    }

    private static void ParseLabels(ReadOnlySpan<float> span, float[] buffer, int rowInBatch, int featureCols, int labelCount)
    {
        // We skip feature columns
        // We only parse label columns
        var labelSpan = span[featureCols..];
        for (var colIndex = 0; colIndex < labelCount; colIndex++)
        {
            buffer[rowInBatch * labelCount + colIndex] = labelSpan[colIndex];
        }
    }

    private static void ValidateLabels(float[] buffer, int rowInBatch, int labelCount, int rowIndex)
    {
        for (var labelIndex = 0; labelIndex < labelCount; labelIndex++)
        {
            var value = buffer[rowInBatch * labelCount + labelIndex];
            if (!float.IsFinite(value) || value is < 0f or > 1f)
            {
                throw new InvalidDataException($"Training label at row {rowIndex}, column {labelIndex} must be finite and in [0, 1], but was {value.ToString(CultureInfo.InvariantCulture)}.");
            }
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
