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
    GameOutcomeOutputMode OutputMode,
    NetArchitecture Architecture)
{
    public const int CurrentFormatVersion = 2;
    public const string MetadataSuffix = ".meta.json";

    /// <summary>
    /// Gets the model filename, including its extension.
    /// </summary>
    public string? Name { get; init; }

    public NetTrainingMetadata? Training { get; init; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetPath(string modelPath) => modelPath + MetadataSuffix;

    public static NetModelMetadata ReadOrLegacy(string modelPath, GameModus modus)
    {
        var metadataPath = GetPath(modelPath);
        using var stream = File.Exists(metadataPath) ? File.OpenRead(metadataPath) : null;
        return ReadOrLegacy(stream, modus, metadataPath) with { Name = Path.GetFileName(modelPath) };
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

    internal void Write(string modelPath)
    {
        File.WriteAllText(GetPath(modelPath), JsonSerializer.Serialize(this, JsonOptions));
    }

    private static NetModelMetadata Read(Stream stream, GameModus modus, string sourceName)
    {
        var metadata = JsonSerializer.Deserialize<NetModelMetadata>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Model metadata '{sourceName}' is empty.");

        if (metadata.FormatVersion is not (1 or CurrentFormatVersion))
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' has unsupported format version {metadata.FormatVersion}; expected {CurrentFormatVersion}.");
        }

        // Single-head models previously ignored architecture B and always constructed architecture A.
        if (metadata.FormatVersion == 1 && metadata.Modus is GameModus.Plakoto or GameModus.Fevga && metadata.Architecture == NetArchitecture.B)
        {
            metadata = metadata with { Architecture = NetArchitecture.A };
        }

        metadata.Validate(modus, sourceName);
        return metadata;
    }

    internal void Validate(GameModus modus, string sourceName)
    {
        if (!Enum.IsDefined(Modus))
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' contains unknown game mode value {(int)Modus}.");
        }

        if (Modus != modus)
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' targets {Modus}, but {modus} was requested.");
        }

        if (!Enum.IsDefined(OutputMode))
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' contains unknown output mode value {(int)OutputMode}.");
        }

        if (!Enum.IsDefined(Architecture))
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' contains unknown architecture value {(int)Architecture}.");
        }

        if (modus is GameModus.Plakoto or GameModus.Fevga && OutputMode != GameOutcomeOutputMode.LegacyIndependentSigmoid)
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' requests five-head output semantics for the single-head {modus} model.");
        }

        if (modus is GameModus.Plakoto or GameModus.Fevga && Architecture != NetArchitecture.A)
        {
            throw new InvalidDataException($"Model metadata '{sourceName}' requests an unsupported architecture for the single-head {modus} model.");
        }
    }

    private static NetModelMetadata LegacyFallback(GameModus modus) => new(0, modus, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A);
}
