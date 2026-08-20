using System.Text.Json;
using System.Text.Json.Serialization;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Nets;

/// <summary>
/// Defines how a five-head game-outcome network converts its sigmoid outputs into probabilities.
/// </summary>
public enum GameOutcomeOutputMode
{
    /// <summary>
    /// Keeps the five sigmoid outputs independent for compatibility with legacy models.
    /// </summary>
    LegacyIndependentSigmoid = 0,

    /// <summary>
    /// Interprets sigmoid outputs as conditional gates so specific outcomes cannot exceed broader outcomes.
    /// </summary>
    MonotonicCumulative = 1
}

public sealed record NetModelMetadata(
    int FormatVersion,
    GameModus Modus,
    GameOutcomeOutputMode OutputMode)
{
    public const int CurrentFormatVersion = 1;
    public const string MetadataSuffix = ".meta.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetPath(string modelPath) => modelPath + MetadataSuffix;

    public static NetModelMetadata ReadOrLegacy(string modelPath, GameModus modus)
    {
        var metadataPath = GetPath(modelPath);
        if (!File.Exists(metadataPath))
        {
            return LegacyFallback(modus);
        }

        try
        {
            using var stream = File.OpenRead(metadataPath);
            return Read(stream, modus, metadataPath);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Could not parse model metadata '{metadataPath}'.", exception);
        }
    }

    public static NetModelMetadata ReadOrLegacy(Stream? stream, GameModus modus, string sourceName)
    {
        if (stream == null)
        {
            return LegacyFallback(modus);
        }

        try
        {
            return Read(stream, modus, sourceName);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Could not parse model metadata '{sourceName}'.", exception);
        }
    }

    public static void Write(string modelPath, GameModus modus, GameOutcomeOutputMode outputMode)
    {
        var metadata = new NetModelMetadata(CurrentFormatVersion, modus, outputMode);
        File.WriteAllText(GetPath(modelPath), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private static NetModelMetadata Read(Stream stream, GameModus modus, string sourceName)
    {
        var metadata = JsonSerializer.Deserialize<NetModelMetadata>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Model metadata '{sourceName}' is empty.");

        if (metadata.FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Model metadata '{sourceName}' has unsupported format version {metadata.FormatVersion}; expected {CurrentFormatVersion}.");
        }

        if (metadata.Modus != modus)
        {
            throw new InvalidDataException(
                $"Model metadata '{sourceName}' targets {metadata.Modus}, but {modus} was requested.");
        }

        if (!Enum.IsDefined(metadata.OutputMode))
        {
            throw new InvalidDataException(
                $"Model metadata '{sourceName}' contains unknown output mode value {(int)metadata.OutputMode}.");
        }

        if (modus is GameModus.Plakoto or GameModus.Fevga
            && metadata.OutputMode != GameOutcomeOutputMode.LegacyIndependentSigmoid)
        {
            throw new InvalidDataException(
                $"Model metadata '{sourceName}' requests five-head output semantics for the single-head {modus} model.");
        }

        return metadata;
    }

    private static NetModelMetadata LegacyFallback(GameModus modus)
        => new(0, modus, GameOutcomeOutputMode.LegacyIndependentSigmoid);
}
