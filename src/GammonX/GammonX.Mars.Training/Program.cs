using GammonX.Mars.NN;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Nets;

using GammonX.Mars.Training;
using GammonX.Mars.Training.Data;
using GammonX.Mars.Training.Generator;
using GammonX.Mars.Training.Sidecars;
using GammonX.Mars.Training.Train;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

using System.Diagnostics;

using static TorchSharp.torch;

Console.WriteLine("===========================================");
Console.WriteLine("  GammonX Mars � Training Console");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine("  1  Generate Training Data");
Console.WriteLine("  2  Train Mode");
Console.WriteLine("  3  Merge and Shuffle Mode");
Console.WriteLine("  4  Noise floor Mode");
Console.WriteLine("  5  Tournament Mode");
Console.WriteLine("  6  Tournament Mode against wildbg");
Console.WriteLine("  7  Rebuild TD targets");
Console.WriteLine("  8  Select random replay games");
Console.WriteLine("  9  Analyze score-gap diagnostics");
Console.WriteLine(" 10  Audit trajectory output constraints");
Console.WriteLine(" 11  Analyze selective 2-ply diagnostics");
Console.WriteLine();
Console.Write("Select mode: ");

var modeInput = Console.ReadLine()?.Trim();
if (modeInput == "1")
{
    RunGenerateTrainingDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}
else if (modeInput == "2")
{
    RunTrainModel();
}
else if (modeInput == "3")
{
    RunShuffleCsv();
}
else if (modeInput == "4")
{
    RunNoiseDiagnostic();
}
else if (modeInput == "5")
{
    RunTournamentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}
else if (modeInput == "6")
{
    RunBotServiceTournamentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}
else if (modeInput == "7")
{
    RunRebuildTdTargets();
}
else if (modeInput == "8")
{
    RunSelectRandomGames();
}
else if (modeInput == "9")
{
    RunAnalyzeExploration();
}
else if (modeInput == "10")
{
    RunAnalyzeConstraints();
}
else if (modeInput == "11")
{
    RunAnalyzeSelectiveTwoPlySearch();
}
else
{
    Console.WriteLine("Invalid selection. Exiting.");
}

Console.ReadLine();

#region Train Model

static void RunTrainModel()
{
    Console.WriteLine();

    // backgammon, tavla and portes share the same neural net and feature tensors
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    var trainingCsvPath = PromptString("Training CSV path", "training_data.csv");
    var outputModelPath = PromptString("Output model path", "training_net.dat");
    var inputModelPath = PromptString("Input model path. Leave blank for new model.", "");
    // we assume that a batch size of 4096 takes up 10MB of GPU RAM
    // batch size influences the amount of optimizer updates per epoch, so a smaller batch size will result in more updates and potentially better convergence
    var batchSize = PromptInt("Batch size", 16384);
    var producerCount = PromptInt("Producer threads", Environment.ProcessorCount * 2);
    var queueCapacity = PromptInt("Queue capacity", Environment.ProcessorCount * 4);

    NetArchitecture netArchitecture;
    GameOutcomeOutputMode outputMode;
    if (string.IsNullOrEmpty(inputModelPath))
    {
        netArchitecture = PromptEnum("Net architecture", [NetArchitecture.A, NetArchitecture.B], NetArchitecture.A);
        outputMode = PromptEnum("Output mode", [GameOutcomeOutputMode.MonotonicCumulative, GameOutcomeOutputMode.LegacyIndependentSigmoid],
            GameOutcomeOutputMode.MonotonicCumulative);
    }
    else
    {
        var metadata = NetModelMetadata.ReadOrLegacy(inputModelPath, modus);
        netArchitecture = metadata.Architecture;
        outputMode = metadata.OutputMode;
    }

    NetTrainer.Train(
        modus,
        trainCsvPath: trainingCsvPath,
        valCsvPath: Path.ChangeExtension(trainingCsvPath, ".val.csv"),
        inputModelPath: inputModelPath,
        outputModelPath: outputModelPath,
        batchSize: batchSize,
        producerCount: producerCount,
        queueCapacity: queueCapacity,
        outputMode: outputMode,
        architecture: netArchitecture);
}

#endregion Train Model

#region Noise Floor Diagnostic

static void RunNoiseDiagnostic()
{
    // we check based on a random label distribution if the given feature set have any predictive power
    // if the noise diagnostic loss gap compared to a real run is greater than 0.3 then the features have predictive capacity.
    // if it is less than that, then the feature sets hold no or to little positional information.
    // backgammon, tavla and portes share the same neural net and feature tensors
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    var trainingCsvPath = PromptString("Training CSV path", "training_data.csv");
    var outputModelPath = PromptString("Output model path", "noise_diagnostic.dat");
    var inputModelPath = PromptString("Input model path. Leave blank for new model.", "");
    var netArchitecture = PromptEnum("Net architecture", [NetArchitecture.A, NetArchitecture.B], NetArchitecture.A);

    NetTrainer.Train(
        modus,
        trainCsvPath: trainingCsvPath,
        valCsvPath: Path.ChangeExtension(trainingCsvPath, ".val.csv"),
        outputModelPath: outputModelPath,
        inputModelPath: inputModelPath,
        shuffleLabels: true,
        architecture: netArchitecture);
}

#endregion Noise Floor Diagnostic

#region Tournament

static async Task RunTournamentAsync()
{
    Console.WriteLine();

    // backgammon, tavla and portes share the same neural net and feature tensors
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    var modelAPath = PromptString("Model A path (model to evaluate)", "model_a.dat");
    var modelABotLevel = PromptEnum("Model A bot level", [BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.TwoPly], BotLevel.Hard);
    var modelBPath = PromptString("Model B path (model to play against)", "model_b.dat");
    var modelBBotLevel = PromptEnum("Model B bot level", [BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.TwoPly], BotLevel.Hard);
    var totalGames = PromptInt("Total games", 1000);
    var evalBatchSize = PromptInt("Eval Batch Size", 64);
    var processCount = PromptInt("Process count", Environment.ProcessorCount);

    if (!File.Exists(modelAPath))
    {
        Console.WriteLine($"Model A not found: {modelAPath}");
        return;
    }

    if (!File.Exists(modelBPath))
    {
        Console.WriteLine($"Model B not found: {modelBPath}");
        return;
    }

    var entryA = new TournamentEntry(modelAPath, modelABotLevel, null, null);
    TournamentEntry? entryB = File.Exists(modelBPath) ? new TournamentEntry(modelBPath, modelBBotLevel, null, null) : null;

    var contactWeights = EvalWeights.GetContactWeights(modus);

    var result = await TournamentRunner.RunAsync(modus, entryA, entryB, totalGames, contactWeights, evalBatchSize, processCount);

    TournamentRunner.PrintReport(result);
}

static async Task RunBotServiceTournamentAsync()
{
    Console.WriteLine();

    // backgammon, tavla and portes share the same neural net and feature tensors
    var modus = PromptEnum("Game modus", [GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Backgammon);
    var modelAPath = PromptString("Model path (model to evaluate)", "model_a.dat");
    var modelABotLevel = PromptEnum("Model A bot level", [BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.TwoPly], BotLevel.Hard);
    var totalGames = PromptInt("Total games", 1000);

    if (!File.Exists(modelAPath))
    {
        Console.WriteLine($"Model A not found: {modelAPath}");
        return;
    }

    var contactWeights = EvalWeights.GetContactWeights(modus);
    var evalBatchSize = PromptInt("Eval Batch Size", 64);
    var processCount = PromptInt("Process count", Environment.ProcessorCount);

    var entryA = new TournamentEntry(modelAPath, modelABotLevel, null, null);

    var result = await TournamentRunner.RunAsync(modus, entryA, null, totalGames, contactWeights, evalBatchSize, processCount);

    TournamentRunner.PrintReport(result);
}

#endregion Tournament

#region Shuffle CSV

static void RunShuffleCsv()
{
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    // TODO: enable full GAME equity predictions for plakoto/fevga
    var labelCount = modus is GameModus.Fevga or GameModus.Plakoto ? 1 : 5;

    // we shuffle all inputs training data to create new CSV files which can be used for combined training
    Console.WriteLine();
    Console.WriteLine("Enter input CSV paths one per line. Leave blank to finish:");

    var inputPaths = new List<string>();
    while (true)
    {
        Console.Write($"  Path {inputPaths.Count + 1}: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            break;
        if (!File.Exists(input))
        {
            Console.WriteLine($"  File not found, skipping: {input}");
            continue;
        }

        inputPaths.Add(input);
    }

    if (inputPaths.Count == 0)
    {
        Console.WriteLine("No valid input files provided.");
        return;
    }

    var defaultOutput = inputPaths.Count == 1 ? inputPaths[0] : "merged.csv";
    var outputPath = PromptString("Output CSV path", defaultOutput);
    var validationSplitSeed = PromptInt("Validation split seed", GameGroupedShuffler.DefaultValidationSplitSeed);

    Console.WriteLine($"Indexing {inputPaths.Count} file(s)...");
    string? header = null;
    string? trajectoryHeader = null;
    var trajectoryPaths = inputPaths.Select(path => Path.ChangeExtension(path, ".trajectory.csv")).ToArray();

    var gameMetadataPaths = inputPaths
        .Select(GetGameMetadataPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var rowIndices = new List<(int fileIndex, long offset, int rowNumber)>();
    var rowGameIds = new List<Guid>();
    var trajectoryOffsets = new List<long[]>();
    var fileIndex = 0;

    foreach (var path in inputPaths)
    {
        var rowIndex = CsvBatchEnumerator.BuildRowIndex(path, labelCount);
        if (header == null)
        {
            header = rowIndex.header;
        }
        else if (!string.Equals(header, rowIndex.header, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Header mismatch: {Path.GetFileName(path)} does not match the first input file.");
        }

        var trajectoryIndex = CsvBatchEnumerator.BuildRowIndex(trajectoryPaths[fileIndex], labelCount: 0);
        if (trajectoryHeader == null)
            trajectoryHeader = trajectoryIndex.header;
        else if (!string.Equals(trajectoryHeader, trajectoryIndex.header, StringComparison.Ordinal))
            throw new InvalidDataException($"Trajectory header mismatch: {Path.GetFileName(trajectoryPaths[fileIndex])} does not match the first input sidecar.");

        if (trajectoryIndex.totalRows != rowIndex.totalRows)
            throw new InvalidDataException($"Trajectory row count mismatch for {Path.GetFileName(path)}.");

        trajectoryOffsets.Add(trajectoryIndex.offsets);

        var gameIds = TrajectoryCsvReader.ReadGameIds(trajectoryPaths[fileIndex]);
        if (gameIds.Count != rowIndex.totalRows)
            throw new InvalidDataException($"Trajectory game ID count mismatch for {Path.GetFileName(path)}.");

        for (var rowNumber = 0; rowNumber < rowIndex.offsets.Length; rowNumber++)
        {
            rowIndices.Add((fileIndex, rowIndex.offsets[rowNumber], rowNumber));
            rowGameIds.Add(gameIds[rowNumber]);
        }


        fileIndex++;
        Console.WriteLine($"  Indexed {rowIndex.totalRows:N0} rows from {Path.GetFileName(path)}");
    }

    if (rowIndices.Count == 0)
    {
        Console.WriteLine("No rows loaded.");
        return;
    }

    Console.WriteLine($"Total rows: {rowIndices.Count:N0}. Shuffling by game...");
    var rowOrder = Enumerable.Range(0, rowIndices.Count).ToArray();
    var split = GameGroupedShuffler.SplitByStableHash(
        rowOrder,
        rowIndex => rowGameIds[rowIndex],
        trainFraction: 0.85,
        validationSplitSeed);
    if (split.Training.Count == 0 || split.Validation.Count == 0)
    {
        Console.WriteLine("The stable validation split produced an empty partition. Use more games or a different validation split seed.");
        return;
    }

    Console.WriteLine($"Train={split.Training.Count:N0}  Validation={split.Validation.Count:N0}  Seed={validationSplitSeed}");
    Console.WriteLine("Writing CSV files...");

    var valPath = Path.ChangeExtension(outputPath, ".val.csv");

    // We keep all input file streams open for random-access reading
    var streams = new FileStream[inputPaths.Count];
    var readers = new StreamReader[inputPaths.Count];
    var trajectoryStreams = new FileStream[inputPaths.Count];
    var trajectoryReaders = new StreamReader[inputPaths.Count];

    try
    {
        for (var f = 0; f < inputPaths.Count; f++)
        {
            streams[f] = new FileStream(inputPaths[f], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
            readers[f] = new StreamReader(streams[f]);
            trajectoryStreams[f] = new FileStream(trajectoryPaths[f], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
            trajectoryReaders[f] = new StreamReader(trajectoryStreams[f]);
        }

        if (string.IsNullOrEmpty(header))
        {
            Console.WriteLine("No or empty header row was detected");
        }

        WriteShuffledCsv(outputPath, header!, split.Training, rowIndices, streams, readers);
        WriteShuffledCsv(valPath, header!, split.Validation, rowIndices, streams, readers);
        var trajectoryOutputPath = Path.ChangeExtension(outputPath, ".trajectory.csv");
        var trajectoryValPath = Path.ChangeExtension(valPath, ".trajectory.csv");
        WriteShuffledCsv(trajectoryOutputPath, trajectoryHeader!, split.Training, rowIndices, trajectoryStreams, trajectoryReaders, trajectoryOffsets);
        WriteShuffledCsv(trajectoryValPath, trajectoryHeader!, split.Validation, rowIndices, trajectoryStreams, trajectoryReaders, trajectoryOffsets);
    }
    finally
    {
        for (var f = 0; f < inputPaths.Count; f++)
        {
            readers[f].Dispose();
            streams[f].Dispose();
            trajectoryReaders[f].Dispose();
            trajectoryStreams[f].Dispose();
        }
    }

    var gamesOutputPath = Path.ChangeExtension(outputPath, ".games.csv");
    MergeGameMetadataCsv(gameMetadataPaths, gamesOutputPath);

    Console.WriteLine($"Written: {outputPath}");
    Console.WriteLine($"Written: {valPath}");
    Console.WriteLine($"Written: {Path.ChangeExtension(outputPath, ".trajectory.csv")}");
    Console.WriteLine($"Written: {Path.ChangeExtension(valPath, ".trajectory.csv")}");
    Console.WriteLine($"Written: {Path.ChangeExtension(outputPath, ".games.csv")}");
    Console.WriteLine("Complete.");
}

static void WriteShuffledCsv(
    string outputPath,
    string header,
    IReadOnlyList<int> rowOrder,
    IReadOnlyList<(int fileIndex, long offset, int rowNumber)> rowIndices,
    FileStream[] streams,
    StreamReader[] readers,
    IReadOnlyList<long[]>? alternateOffsets = null)
{
    using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8, bufferSize: 1 << 16);
    writer.WriteLine(header);

    var count = 0;
    foreach (var rowOrderIndex in rowOrder)
    {
        var rowIndex = rowIndices[rowOrderIndex];
        var stream = streams[rowIndex.fileIndex];
        var reader = readers[rowIndex.fileIndex];

        var offset = alternateOffsets is null
            ? rowIndex.offset
            : alternateOffsets[rowIndex.fileIndex][rowIndex.rowNumber];
        stream.Seek(offset, SeekOrigin.Begin);
        reader.DiscardBufferedData();
        var line = reader.ReadLine();
        if (line is not null)
        {
            writer.WriteLine(line);
            count++;
        }
    }

    Console.WriteLine($"Written {count:N0} rows to {outputPath}");
}

static void MergeGameMetadataCsv(IReadOnlyList<string> inputPaths, string outputPath)
{
    var gameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8);
    writer.WriteLine(WellKnownCsvHeaders.GamesSidecarHeader);

    foreach (var path in inputPaths)
    {
        using var reader = new StreamReader(path);
        var header = reader.ReadLine();
        if (header != WellKnownCsvHeaders.GamesSidecarHeader)
            throw new InvalidDataException($"Unexpected game metadata header in '{path}'.");

        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 5)
            {
                throw new InvalidDataException($"Malformed game metadata row {lineNumber} in '{path}'.");
            }

            if (!gameIds.Add(columns[0]))
            {
                // We just skip duplicates, since the game metadata is expected to be identical across all sidecar files.
                Console.WriteLine($"Duplicate game ID '{columns[0]}' found in row {lineNumber} of '{path}'.");
                continue;
            }

            writer.WriteLine(line);
        }
    }
}

static string GetGameMetadataPath(string csvPath)
{
    var directory = Path.GetDirectoryName(csvPath);
    var fileName = Path.GetFileNameWithoutExtension(csvPath);
    if (fileName.EndsWith(".val", StringComparison.OrdinalIgnoreCase))
        fileName = fileName[..^4];

    return Path.Combine(directory ?? string.Empty, $"{fileName}.games.csv");
}

#endregion Shuffle CSV

#region Select Random Games

static void RunSelectRandomGames()
{
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    // TODO: enable full GAME equity predictions for plakoto/fevga
    var labelCount = modus is GameModus.Fevga or GameModus.Plakoto ? 1 : 5;

    Console.WriteLine();
    Console.WriteLine("Enter input CSV paths one per line. Leave blank to finish:");

    var inputPaths = new List<string>();
    while (true)
    {
        Console.Write($"  Path {inputPaths.Count + 1}: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            break;
        if (!File.Exists(input))
        {
            Console.WriteLine($"  File not found, skipping: {input}");
            continue;
        }

        if (inputPaths.Any(path => string.Equals(path, input, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"  File already added, skipping: {input}");
            continue;
        }

        inputPaths.Add(input);
    }

    if (inputPaths.Count == 0)
    {
        Console.WriteLine("No valid input files provided.");
        return;
    }

    var gameCount = PromptInt("Number of complete games to select", 1_000);
    if (gameCount <= 0)
    {
        Console.WriteLine("The number of games must be greater than zero.");
        return;
    }

    var outputPath = PromptString("Output CSV path", "replay.csv");
    var requestedSeed = PromptOptionalInt("Random seed");
    var effectiveSeed = requestedSeed ?? Random.Shared.Next();
    var trajectoryOutputPath = Path.ChangeExtension(outputPath, ".trajectory.csv");
    var gamesOutputPath = Path.ChangeExtension(outputPath, ".games.csv");

    var trajectoryPaths = inputPaths
        .Select(path => Path.ChangeExtension(path, ".trajectory.csv"))
        .ToArray();
    var trajectorySidecarsPresent = trajectoryPaths.All(File.Exists);
    if (!trajectorySidecarsPresent)
    {
        if (trajectoryPaths.Any(File.Exists))
            throw new InvalidDataException("Either all input trajectory sidecars must exist or none may exist.");

        Console.WriteLine("Replay extraction requires a matching .trajectory.csv for every input CSV.");
        return;
    }

    var gameMetadataPaths = inputPaths
        .Select(GetGameMetadataPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (gameMetadataPaths.Any(path => !File.Exists(path)))
        throw new InvalidDataException("Replay extraction requires a matching .games.csv file for every input.");

    var outputPaths = new[] { outputPath, trajectoryOutputPath, gamesOutputPath };
    if (outputPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != outputPaths.Length)
        throw new InvalidDataException("The output CSV and its sidecar paths must be distinct.");

    var inputRelatedPaths = inputPaths
        .Concat(trajectoryPaths)
        .Concat(gameMetadataPaths)
        .Select(Path.GetFullPath)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (outputPaths.Any(path => inputRelatedPaths.Contains(Path.GetFullPath(path))))
        throw new InvalidDataException("The output CSV or one of its sidecars would overwrite an input file.");

    var gamesById = new Dictionary<Guid, GameMetadata>();
    foreach (var path in gameMetadataPaths)
    {
        foreach (var game in TrajectoryCsvReader.ReadGames(path))
        {
            if (!gamesById.TryAdd(game.GameId, game))
                throw new InvalidDataException($"Game metadata contains duplicate game ID '{game.GameId}'.");
        }
    }

    Console.WriteLine($"Indexing {inputPaths.Count} file(s)...");

    string? header = null;
    string? trajectoryHeader = null;
    var rowIndices = new List<(int fileIndex, long offset, int rowNumber)>();
    var trajectoryOffsets = new List<long[]>();
    var rowsByGame = new Dictionary<Guid, List<int>>();
    var gameSources = new Dictionary<Guid, int>();

    for (var fileIndex = 0; fileIndex < inputPaths.Count; fileIndex++)
    {
        var path = inputPaths[fileIndex];
        var rowIndex = CsvBatchEnumerator.BuildRowIndex(path, labelCount);
        if (rowIndex.totalRows == 0)
            throw new InvalidDataException($"Input CSV '{path}' contains no data rows.");

        if (header == null)
        {
            header = rowIndex.header;
        }
        else if (!string.Equals(header, rowIndex.header, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Header mismatch: {Path.GetFileName(path)} does not match the first input file.");
        }

        var trajectoryIndex = CsvBatchEnumerator.BuildRowIndex(trajectoryPaths[fileIndex], labelCount: 0);
        if (trajectoryHeader == null)
            trajectoryHeader = trajectoryIndex.header;
        else if (!string.Equals(trajectoryHeader, trajectoryIndex.header, StringComparison.Ordinal))
            throw new InvalidDataException($"Trajectory header mismatch: {Path.GetFileName(trajectoryPaths[fileIndex])} does not match the first input sidecar.");

        if (trajectoryIndex.totalRows != rowIndex.totalRows)
            throw new InvalidDataException($"Trajectory row count mismatch for {Path.GetFileName(path)}.");

        var gameIds = TrajectoryCsvReader.ReadGameIds(trajectoryPaths[fileIndex]);
        if (gameIds.Count != rowIndex.totalRows)
            throw new InvalidDataException($"Trajectory game ID count mismatch for {Path.GetFileName(path)}.");

        trajectoryOffsets.Add(trajectoryIndex.offsets);
        for (var rowNumber = 0; rowNumber < rowIndex.offsets.Length; rowNumber++)
        {
            var gameId = gameIds[rowNumber];
            if (gameSources.TryGetValue(gameId, out var sourceFileIndex) && sourceFileIndex != fileIndex)
                throw new InvalidDataException($"Game ID '{gameId}' occurs in multiple input CSV files.");

            gameSources[gameId] = fileIndex;
            var globalRowIndex = rowIndices.Count;
            rowIndices.Add((fileIndex, rowIndex.offsets[rowNumber], rowNumber));

            if (!rowsByGame.TryGetValue(gameId, out var gameRows))
            {
                gameRows = [];
                rowsByGame.Add(gameId, gameRows);
            }

            gameRows.Add(globalRowIndex);
        }

        Console.WriteLine($"  Indexed {rowIndex.totalRows:N0} rows from {Path.GetFileName(path)}");
    }

    // We validate all games and remove any that are not complete or have missing metadata
    // We only want to select fully available games for replay extraction
    var gameIdsToRemove = new List<Guid>();
    foreach (var (gameId, gameRows) in rowsByGame)
    {
        if (!gamesById.TryGetValue(gameId, out var metadata))
        {
            Console.WriteLine($"No game metadata exists for game ID '{gameId}'.");
            gameIdsToRemove.Add(gameId);
            continue;
        }

        if (metadata.TotalTurns != gameRows.Count)
        {
            Console.WriteLine($"Game metadata turn count mismatch for game ID '{gameId}': metadata={metadata.TotalTurns}, rows={gameRows.Count}.");
            gameIdsToRemove.Add(gameId);
        }
    }

    gameIdsToRemove.ForEach(gameId => rowsByGame.Remove(gameId));

    var random = new Random(effectiveSeed);
    var selectedGameIds = GameGroupedShuffler.SelectGames(rowsByGame.Keys.ToArray(), gameCount, random);
    var selectedRowOrder = selectedGameIds
        .SelectMany(gameId => rowsByGame[gameId])
        .ToArray();
    var selectedGames = selectedGameIds
        .Select(gameId => gamesById[gameId])
        .ToArray();

    Console.WriteLine($"Available complete games: {rowsByGame.Count:N0}");
    Console.WriteLine($"Selected games: {selectedGameIds.Count:N0}");
    Console.WriteLine($"Selected trajectory rows: {selectedRowOrder.Length:N0}");
    Console.WriteLine($"Random seed: {effectiveSeed}");
    Console.WriteLine("Writing replay CSV files...");

    var streams = new FileStream[inputPaths.Count];
    var readers = new StreamReader[inputPaths.Count];
    var trajectoryStreams = new FileStream[inputPaths.Count];
    var trajectoryReaders = new StreamReader[inputPaths.Count];
    try
    {
        for (var fileIndex = 0; fileIndex < inputPaths.Count; fileIndex++)
        {
            streams[fileIndex] = new FileStream(inputPaths[fileIndex], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
            readers[fileIndex] = new StreamReader(streams[fileIndex]);
            trajectoryStreams[fileIndex] = new FileStream(trajectoryPaths[fileIndex], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 16);
            trajectoryReaders[fileIndex] = new StreamReader(trajectoryStreams[fileIndex]);
        }

        WriteShuffledCsv(outputPath, header!, selectedRowOrder, rowIndices, streams, readers);
        WriteShuffledCsv(trajectoryOutputPath, trajectoryHeader!, selectedRowOrder, rowIndices, trajectoryStreams, trajectoryReaders, trajectoryOffsets);
        TrajectoryCsvWriter.WriteGames(gamesOutputPath, selectedGames);
    }
    finally
    {
        for (var fileIndex = 0; fileIndex < inputPaths.Count; fileIndex++)
        {
            readers[fileIndex].Dispose();
            streams[fileIndex].Dispose();
            trajectoryReaders[fileIndex].Dispose();
            trajectoryStreams[fileIndex].Dispose();
        }
    }

    Console.WriteLine($"Written: {outputPath}");
    Console.WriteLine($"Written: {trajectoryOutputPath}");
    Console.WriteLine($"Written: {gamesOutputPath}");
    Console.WriteLine("Complete.");
}

#endregion Select Random Games

#region Analyze Score-Gap Diagnostics

static void RunAnalyzeExploration()
{
    Console.WriteLine();
    var path = PromptString("Exploration diagnostics path", "training_data.exploration.csv");
    if (!File.Exists(path))
    {
        Console.WriteLine($"File not found: {path}");
        return;
    }

    var report = ExplorationBroker.Analyze(ExplorationCsvReader.ReadDecisions(path));
    Console.WriteLine("Overall score-gap diagnostics");
    Console.WriteLine("==============================");
    PrintExplorationSummary(report.Overall);

    foreach (var group in report.Groups
                 .OrderBy(pair => pair.Key.Modus)
                 .ThenBy(pair => pair.Key.AgainstBot)
                 .ThenBy(pair => pair.Key.EarlyPhase ? 0 : 1)
                 .ThenBy(pair => pair.Key.CandidateBucket))
    {
        var key = group.Key;
        Console.WriteLine();
        Console.WriteLine($"{key.Modus} | {(key.AgainstBot ? "wildbg" : "self-play")} | {(key.EarlyPhase ? "early" : "late")} phase");
        Console.WriteLine($"Candidates: {key.CandidateBucket}");
        Console.WriteLine(new string('-', 40));
        PrintExplorationSummary(group.Value);
    }
}

static void PrintExplorationSummary(ExplorationGapSummary summary)
{
    Console.WriteLine($"  Decisions : {summary.DecisionCount:N0}");
    Console.WriteLine($"  Gaps      : {summary.GapCount:N0}");
    Console.WriteLine($"  Missing   : {summary.NoGapCount:N0}");

    if (summary.GapCount == 0)
        return;

    Console.WriteLine();
    Console.WriteLine("  Score gaps");
    Console.WriteLine($"    Minimum : {summary.MinGap:F6}");
    Console.WriteLine($"    P10     : {summary.P10Gap:F6}");
    Console.WriteLine($"    P25     : {summary.P25Gap:F6}");
    Console.WriteLine($"    Median  : {summary.P50Gap:F6}");
    Console.WriteLine($"    P75     : {summary.P75Gap:F6}");
    Console.WriteLine($"    P90     : {summary.P90Gap:F6}");
    Console.WriteLine($"    P95     : {summary.P95Gap:F6}");
    Console.WriteLine($"    Mean    : {summary.MeanGap:F6}");
    Console.WriteLine($"    Maximum : {summary.MaxGap:F6}");
    Console.WriteLine();
    Console.WriteLine("  Choices");
    Console.WriteLine($"    Greedy  : {summary.ChoiceCounts.GetValueOrDefault(ExplorationChoice.Greedy):N0}");
    Console.WriteLine($"    Ranked  : {summary.ChoiceCounts.GetValueOrDefault(ExplorationChoice.Ranked):N0}");
    Console.WriteLine($"    All legal: {summary.ChoiceCounts.GetValueOrDefault(ExplorationChoice.AllLegal):N0}");
}

#endregion Analyze Score-Gap Diagnostics

#region Analyze Selective Two-Ply Diagnostics

static void RunAnalyzeSelectiveTwoPlySearch()
{
    Console.WriteLine();
    var path = PromptString("Selective 2-ply diagnostics path", "training_data.search.csv");
    if (!File.Exists(path))
    {
        Console.WriteLine($"File not found: {path}");
        return;
    }

    var report = SelectiveTwoPlySearchBroker.Analyze(SelectiveTwoPlySearchCsvReader.ReadDecisions(path));
    Console.WriteLine("Overall selective 2-ply diagnostics");
    Console.WriteLine("===================================");
    PrintSelectiveTwoPlySummary(report.Overall);

    foreach (var group in report.Groups.OrderBy(pair => pair.Key.Modus).ThenBy(pair => pair.Key.AgainstBot))
    {
        Console.WriteLine();
        Console.WriteLine($"{group.Key.Modus} | {(group.Key.AgainstBot ? "wildbg" : "self-play")}");
        Console.WriteLine(new string('-', 40));
        PrintSelectiveTwoPlySummary(group.Value);
    }
}

static void PrintSelectiveTwoPlySummary(SelectiveTwoPlySearchSummary summary)
{
    Console.WriteLine($"  Decisions          : {summary.DecisionCount:N0}");
    Console.WriteLine($"  Evaluated          : {summary.EvaluatedDecisionCount:N0}");
    Console.WriteLine($"  Best move changed  : {summary.BestMoveChangedCount:N0} ({summary.BestMoveChangeRate:P2})");
    Console.WriteLine($"  Avg candidates     : {summary.AverageEvaluatedCandidateCount:F2}");
    Console.WriteLine($"  Mean 1-ply gap     : {summary.MeanOnePlyScoreGap:F6}");
    Console.WriteLine($"  Mean 2-ply gap     : {summary.MeanTwoPlyScoreGap:F6}");
    Console.WriteLine("  Audits");
    Console.WriteLine($"    Decisions       : {summary.AuditDecisionCount:N0}");
    Console.WriteLine($"    Best move changed: {summary.AuditBestMoveChangedCount:N0} ({summary.AuditBestMoveChangeRate:P2})");
    Console.WriteLine($"    Ranked audits   : {summary.RankedAuditDecisionCount:N0}");
    if (summary.RankedAuditDecisionCount > 0)
    {
        Console.WriteLine($"    Winner outside K: {summary.AuditWinnerOutsideCandidateLimitCount:N0} ({summary.AuditWinnerOutsideCandidateLimitRate:P2})");
    }

    if (summary.AuditWinnerOnePlyRankCounts.Count > 0)
    {
        Console.WriteLine("    Winner 1-ply ranks");
        foreach (var rank in summary.AuditWinnerOnePlyRankCounts.OrderBy(pair => pair.Key))
        {
            Console.WriteLine($"      {rank.Key,3}: {rank.Value:N0}");
        }
    }

    Console.WriteLine("  Reasons");
    foreach (var reason in Enum.GetValues<SelectiveTwoPlyReason>())
    {
        Console.WriteLine($"    {reason,-15}: {summary.ReasonCounts.GetValueOrDefault(reason):N0}");
    }
}

#endregion Analyze Selective Two-Ply Diagnostics

#region Analyze Trajectory Constraints

static void RunAnalyzeConstraints()
{
    Console.WriteLine();
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Backgammon);
    var trajectoryPath = PromptString("Trajectory sidecar path", "training_data.trajectory.csv");
    if (!File.Exists(trajectoryPath))
    {
        Console.WriteLine($"File not found: {trajectoryPath}");
        return;
    }

    var currentModelPath = PromptString("Current model path. Leave blank to audit sidecar only", "");
    string? trainingCsvPath = null;

    if (!string.IsNullOrWhiteSpace(currentModelPath))
    {
        trainingCsvPath = PromptString("Training CSV path for current-model re-evaluation", "training_data.csv");
    }

    var batchSize = PromptInt("Audit batch size", 256);
    var device = cuda.is_available() ? CUDA : CPU;

    var report = TrajectoryConstraintAuditor.Analyze(
        modus,
        trajectoryPath,
        trainingCsvPath,
        string.IsNullOrWhiteSpace(currentModelPath) ? null : currentModelPath,
        device,
        batchSize);

    Console.WriteLine();
    Console.WriteLine("Source-model predictions");
    Console.WriteLine("------------------------");
    PrintConstraintAuditMetrics(report.SourceModel);

    Console.WriteLine();
    Console.WriteLine("Current-model predictions");
    Console.WriteLine("-------------------------");
    PrintConstraintAuditMetrics(report.CurrentModel);
}

static void PrintConstraintAuditMetrics(ConstraintMetricsResult? metrics)
{
    if (metrics is null)
    {
        Console.WriteLine("  Not applicable or not requested.");
        return;
    }

    Console.WriteLine($"  Rows             : {metrics.RowCount:N0}");
    Console.WriteLine($"  Invalid rows     : {metrics.InvalidRowCount:N0} ({metrics.InvalidRate:P2})");
    Console.WriteLine($"  Rule violations  : {metrics.RuleViolationCount:N0}");
    Console.WriteLine($"  Average severity : {metrics.AverageSeverity:F5}");
    Console.WriteLine($"  Maximum severity : {metrics.MaxSeverity:F5}");
    Console.WriteLine();
    Console.WriteLine("  Violations by rule");
    Console.WriteLine($"    Range             : {metrics.RangeViolationRows:N0}");
    Console.WriteLine($"    Win gammon        : {metrics.WinGammonHierarchyViolationRows:N0}");
    Console.WriteLine($"    Win backgammon    : {metrics.WinBackgammonHierarchyViolationRows:N0}");
    Console.WriteLine($"    Lose complement   : {metrics.LoseGammonComplementViolationRows:N0}");
    Console.WriteLine($"    Lose backgammon   : {metrics.LoseBackgammonHierarchyViolationRows:N0}");
}

#endregion Analyze Trajectory Constraints

#region Rebuild TD Targets

static void RunRebuildTdTargets()
{
    Console.WriteLine();

    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Backgammon);
    var trainingCsvPath = PromptString("Input training CSV path", "training_data.csv");
    var validationCsvPath = PromptString("Input validation CSV path", Path.ChangeExtension(trainingCsvPath, ".val.csv"));
    var trajectoryCsvPath = PromptString("Input trajectory sidecar path", Path.ChangeExtension(trainingCsvPath, ".trajectory.csv"));
    var validationTrajectoryCsvPath = PromptString("Input validation trajectory sidecar path", Path.ChangeExtension(validationCsvPath, ".trajectory.csv"));
    var gamesCsvPath = PromptString("Input games metadata path", Path.ChangeExtension(trainingCsvPath, ".games.csv"));
    var outputPath = PromptString("Output CSV path", "training_data.td.csv");
    var lambda = PromptFloat("TD-lambda", SelfPlayRecorder.DefaultLambda);
    var gamma = PromptFloat("TD-gamma", 1.0f);
    var labelCount = modus is GameModus.Fevga or GameModus.Plakoto ? 1 : 5;

    if (!File.Exists(trainingCsvPath)
        || !File.Exists(validationCsvPath)
        || !File.Exists(trajectoryCsvPath)
        || !File.Exists(validationTrajectoryCsvPath)
        || !File.Exists(gamesCsvPath))
    {
        Console.WriteLine("Training/validation CSV, trajectory sidecar, or games metadata file was not found.");
        return;
    }

    var trainingRows = TrajectoryCsvReader.ReadRows(trainingCsvPath, trajectoryCsvPath, labelCount);
    var validationRows = TrajectoryCsvReader.ReadRows(validationCsvPath, validationTrajectoryCsvPath, labelCount);

    var games = TrajectoryCsvReader.ReadGames(gamesCsvPath);
    var rebuiltRows = TrajectoryTargetBuilder.Recalculate(
        trainingRows.Concat(validationRows),
        games,
        lambda,
        gamma,
        labelCount == 1 ? 1 : 5);

    var rebuiltTrainingRows = rebuiltRows.Take(trainingRows.Count).ToList();
    var rebuiltValidationRows = rebuiltRows.Skip(trainingRows.Count).ToList();

    var featureCount = rebuiltTrainingRows.Count == 0 ? 0 : rebuiltTrainingRows[0].Position.Features.Length;

    if (featureCount == 0)
    {
        Console.WriteLine("No trajectory rows were loaded.");
        return;
    }

    var validationOutputPath = Path.ChangeExtension(outputPath, ".val.csv");
    TrainingCsvWriter.Write(outputPath, modus, rebuiltTrainingRows, featureCount);
    TrainingCsvWriter.Write(validationOutputPath, modus, rebuiltValidationRows, featureCount);

    var trajectoryTrainPath = Path.ChangeExtension(outputPath, ".trajectory.csv");
    TrajectoryCsvWriter.WritePositions(trajectoryTrainPath, rebuiltTrainingRows);

    var trajectoryValpath = Path.ChangeExtension(validationOutputPath, ".trajectory.csv");
    TrajectoryCsvWriter.WritePositions(trajectoryValpath, rebuiltValidationRows);

    var gamesPath = Path.ChangeExtension(outputPath, ".games.csv");
    TrajectoryCsvWriter.WriteGames(gamesPath, games);

    Console.WriteLine($"Rebuilt {rebuiltRows.Count:N0} labels with lambda={lambda} gamma={gamma}.");
    Console.WriteLine($"Written: {outputPath}");
    Console.WriteLine($"Written: {validationOutputPath}");
    Console.WriteLine($"Written: {trajectoryTrainPath}");
    Console.WriteLine($"Written: {trajectoryValpath}");
    Console.WriteLine($"Written: {gamesPath}");
}

#endregion Rebuild TD Targets

#region Generate Training Data

static async Task RunGenerateTrainingDataAsync()
{
    Console.WriteLine();

    // backgammon, tavla and portes share the same neural net and feature tensors
    var modus = PromptEnum("Game modus", [GameModus.Plakoto, GameModus.Fevga, GameModus.Backgammon, GameModus.Tavla, GameModus.Portes], GameModus.Plakoto);
    var totalGames = PromptInt("Total games", 1_000);
    var outputPath = PromptString("Output CSV path", "training_data.csv");
    var validationSplitSeed = PromptInt("Validation split seed", GameGroupedShuffler.DefaultValidationSplitSeed);
    var modelAPath = PromptString("Model A path. Leave blank for linear.", "");
    var modelABotLevel = PromptEnum("Model A bot level", [BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.TwoPly], BotLevel.Hard);
    var modelBPath = PromptString("Model B path. Leave blank for single-model or linear.", "");
    var modelBBotLevel = PromptEnum("Model A bot level", [BotLevel.Easy, BotLevel.Medium, BotLevel.Hard, BotLevel.TwoPly], BotLevel.Hard);
    // we expect with a lambda below < 1.0 smooth intermediate labels, not just binary 1/0.
    // train/val mean should stay below 0.53 to ensure the model does not learn asymmetric win/loss patterns
    // we also expect near-0.5 positions to increase above 0.0%
    var lambda = PromptFloat("TD-lambda", SelfPlayRecorder.DefaultLambda);
    var playAgainstBotService = PromptBool("Play against wildbg bot", false);
    var evalBatchSize = PromptInt("Eval Batch Size", 64);
    var processCount = PromptInt("Process count", Environment.ProcessorCount);
    // we configure the rank aware exploration parameters (selecting the best move exploration)
    var collectScoreGapDiagnostics = PromptBool("Write score-gap diagnostics", false);
    var scoreGapAwareExploration = PromptBool("Enable score-gap ranked exploration", false);
    var scoreGapSmallThreshold = PromptFloat("Small score-gap threshold", 0.02f);
    var scoreGapLargeThreshold = PromptFloat("Large score-gap threshold", 0.2f);
    var smallGapRankedMultiplier = PromptFloat("Small-gap ranked multiplier", 1.5f);
    var largeGapRankedMultiplier = PromptFloat("Large-gap ranked multiplier", 0.5f);
    // we configure the selective two-ply search parameters (selectively execute a two-ply search based on the 1-ply evaluation gap)
    // it is a compromise between computational cost (full 2-ply) and search accuracy (selective 2-ply)
    var selectiveTwoPlyEnabled = PromptBool("Enable selective 2-ply search", false);
    var selectiveTwoPlyGap = selectiveTwoPlyEnabled ? PromptFloat("Maximum 1-ply gap for selective search", 0.02f) : 0.02f;
    var selectiveTwoPlyCandidates = selectiveTwoPlyEnabled ? PromptInt("Maximum selective 2-ply candidates", 3) : 3;
    var selectiveTwoPlyAuditProbability = selectiveTwoPlyEnabled ? PromptFloat("Full-search audit probability", 0.01f) : 0.01f;
    var collectSelectiveTwoPlyDiagnostics = selectiveTwoPlyEnabled && PromptBool("Write selective 2-ply diagnostics", true);

    Console.WriteLine();

    var hasModelAPath = !string.IsNullOrWhiteSpace(modelAPath);
    var hasModelBPath = !string.IsNullOrWhiteSpace(modelBPath);

    if (hasModelBPath && !hasModelAPath)
    {
        Console.WriteLine("Model B requires a Model A path.");
        return;
    }

    if (hasModelAPath && !File.Exists(modelAPath))
    {
        Console.WriteLine($"Model A not found: {modelAPath}");
        return;
    }

    if (hasModelBPath && !File.Exists(modelBPath))
    {
        Console.WriteLine($"Model B not found: {modelBPath}");
        return;
    }

    if (hasModelAPath && hasModelBPath && string.Equals(Path.GetFullPath(modelAPath), Path.GetFullPath(modelBPath), StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Model A and Model B must be different files.");
        return;
    }

    if (hasModelBPath && playAgainstBotService)
    {
        Console.WriteLine("Model B cannot be used when playing against the WildBG bot service.");
        return;
    }

    if (selectiveTwoPlyEnabled && !hasModelAPath)
    {
        Console.WriteLine("Selective 2-ply search requires a Model A path.");
        return;
    }

    if (selectiveTwoPlyEnabled && modelABotLevel == BotLevel.TwoPly)
    {
        Console.WriteLine("Selective 2-ply search requires a 1-ply Model A bot level.");
        return;
    }

    var selectiveTwoPlyOptions = new SelectiveTwoPlyOptions
    {
        Enabled = selectiveTwoPlyEnabled,
        MaximumOnePlyGap = selectiveTwoPlyGap,
        MaximumCandidates = selectiveTwoPlyCandidates,
        FullSearchAuditProbability = selectiveTwoPlyAuditProbability,
        CollectDiagnostics = collectSelectiveTwoPlyDiagnostics
    };

    try
    {
        selectiveTwoPlyOptions.Validate();
    }
    catch (ArgumentOutOfRangeException exception)
    {
        Console.WriteLine($"Invalid selective 2-ply configuration: {exception.Message}");
        return;
    }

    var extractor = GetFeatureVectorExtractor(modus);
    var contactWeights = EvalWeights.GetContactWeights(modus);

    var useNeuralEval = hasModelAPath;
    BatchedNeuralEvalService? modelAService = null;
    BatchedNeuralEvalService? modelBService = null;

    var device = cuda.is_available() ? CUDA : CPU;
    Console.WriteLine($"Device: {device}");

    try
    {
        if (useNeuralEval)
        {
            modelAService = (BatchedNeuralEvalService)BatchedNeuralEvalService.Load(modus, modelAPath, device, evalBatchSize);
            modelAService.StartAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Loaded Model A: {modelAPath}");

            if (hasModelBPath)
            {
                modelBService = (BatchedNeuralEvalService)BatchedNeuralEvalService.Load(modus, modelBPath, device, evalBatchSize);
                modelBService.StartAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                Console.WriteLine($"Loaded Model B: {modelBPath}");
            }
        }
        else
        {
            Console.WriteLine("Using linear model.");
        }
    }
    catch
    {
        try
        {
            StopNeuralEvalService(modelBService);
        }
        finally
        {
            StopNeuralEvalService(modelAService);
        }

        throw;
    }

    var completed = 0;
    var discarded = 0;
    var allSamples = new List<TrainingDataRow>(capacity: totalGames * 40);
    var completedGames = new List<GameMetadata>(capacity: totalGames);
    var explorationDecisions = new List<ExplorationDecision>();
    var searchDecisions = new List<SelectiveTwoPlySearchDecision>();
    var totalTurnCount = 0L;
    var totalPredVariance = 0.0;
    var predVarianceCount = 0;
    // we collect constraint metrics for all played games
    var constraintMetrics = useNeuralEval ? new ConstraintMetricsAccumulator() : null;
    var lockObj = new object();

    Console.WriteLine($"Starting self-play: {totalGames} games, modus={modus}, lambda={lambda}");
    Console.WriteLine($"Output: {Path.GetFullPath(outputPath)}");
    Console.WriteLine();

    var stopwatch = Stopwatch.StartNew();

    var explorationOptions = new ExplorationOptions
    {
        CollectScoreGapDiagnostics = collectScoreGapDiagnostics,
        ScoreGapAwareExplorationEnabled = scoreGapAwareExploration,
        ScoreGapSmallThreshold = scoreGapSmallThreshold,
        ScoreGapLargeThreshold = scoreGapLargeThreshold,
        SmallGapRankedExplorationMultiplier = smallGapRankedMultiplier,
        LargeGapRankedExplorationMultiplier = largeGapRankedMultiplier
    };

    var entryA = new SelfPlayEntry(modelAService, modelABotLevel);
    var entryB = hasModelBPath ? new SelfPlayEntry(modelBService, modelBBotLevel) : null;
    Exception? generationException = null;

    try
    {
        try
        {
            await Parallel.ForAsync(
                0,
                totalGames,
                new ParallelOptions { MaxDegreeOfParallelism = processCount },
                async (i, _) =>
                {
                    try
                    {
                        var recorder = new SelfPlayRecorder(extractor, modelAService, lambda);
                        var runner = new SelfPlayRunner(
                            recorder,
                            modus,
                            entryA,
                            entryB,
                            explorationOptions,
                            selectiveTwoPlyOptions);

                        SelfPlayRunResult result;

                        if (playAgainstBotService)
                        {
                            var modelIsWhite = i % 2 == 0;
                            result = await runner.RunAgainstBotServiceGameAsync(modus, modelIsWhite, contactWeights);
                        }
                        else if (entryA.EvalService != null)
                        {
                            result = await runner.RunAsync(contactWeights, modelAIsWhite: i % 2 == 0);
                        }
                        else
                        {
                            result = await runner.RunAsync(contactWeights);
                        }

                        lock (lockObj)
                        {
                            totalTurnCount += result.TurnCount;

                            if (result.ConstraintMetrics != null)
                            {
                                constraintMetrics?.Merge(result.ConstraintMetrics);
                            }

                            if (result.Samples.Count == 0 || result.Trajectory == null || result.Trajectory.Positions.Count != result.Samples.Count)
                            {
                                discarded++;
                            }
                            else
                            {
                                for (var sampleIndex = 0; sampleIndex < result.Samples.Count; sampleIndex++)
                                {
                                    var position = result.Trajectory.Positions[sampleIndex];
                                    allSamples.Add(new TrainingDataRow(
                                        result.Trajectory.GameId,
                                        position,
                                        result.Samples[sampleIndex].Label));
                                }

                                completedGames.Add(result.Trajectory.Metadata);
                                completed++;

                                if (result.ExplorationDecisions != null && result.ExplorationDecisions.Count > 0)
                                {
                                    explorationDecisions.AddRange(result.ExplorationDecisions);
                                }

                                if (result.SearchDecisions != null && result.SearchDecisions.Count > 0)
                                {
                                    searchDecisions.AddRange(result.SearchDecisions);
                                }

                                if (result.PredictionVariance.HasValue)
                                {
                                    totalPredVariance += result.PredictionVariance.Value;
                                    predVarianceCount++;
                                }
                            }

                            var started = completed + discarded;
                            Console.WriteLine($"  {started,6} / {totalGames} completed={completed} discarded={discarded} samples={allSamples.Count:N0}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            discarded++;
                            Console.WriteLine($"  Game {i + 1} failed and was discarded:");
                            Console.WriteLine(ex.ToString());

                            var started = completed + discarded;
                            Console.WriteLine($"  {started,6} / {totalGames} completed={completed} discarded={discarded} samples={allSamples.Count:N0}");
                        }
                    }
                });
        }
        catch (Exception ex)
        {
            generationException = ex;
            Console.WriteLine();
            Console.WriteLine($"Self-play stopped after {completed + discarded} of {totalGames} games:");
            Console.WriteLine(ex.ToString());
        }
    }
    finally
    {
        try
        {
            StopNeuralEvalService(modelBService);
        }
        finally
        {
            StopNeuralEvalService(modelAService);
        }
    }

    var generationIncomplete = generationException != null || completed + discarded < totalGames;
    if (generationIncomplete)
    {
        Console.WriteLine();
        Console.WriteLine($"Generation incomplete: {completed + discarded} of {totalGames} games processed.");
    }

    Console.WriteLine();
    Console.WriteLine($"Done. Completed={completed}  Discarded={discarded}  Total samples={allSamples.Count:N0}");
    Console.WriteLine($"Avg turns/game : {(double)totalTurnCount / totalGames:F1}");

    if (predVarianceCount > 0)
        Console.WriteLine($"Avg pred variance: {totalPredVariance / predVarianceCount:F5}  (over {predVarianceCount} completed games)");

    if (constraintMetrics != null)
    {
        var metrics = constraintMetrics.Complete();
        if (metrics.RowCount > 0)
        {
            Console.WriteLine(
                $"Output constraints: rows={metrics.RowCount:N0} invalid={metrics.InvalidRowCount:N0} "
                + $"({metrics.InvalidRate:P2}) rules={metrics.RuleViolationCount:N0} "
                + $"avgSeverity={metrics.AverageSeverity:F5} maxSeverity={metrics.MaxSeverity:F5}");
        }
    }

    Console.WriteLine("Shuffling by game...");

    var split = GameGroupedShuffler.SplitByStableHash(
        allSamples,
        sample => sample.GameId,
        trainFraction: 0.85,
        validationSplitSeed);

    IReadOnlyList<TrainingDataRow> trainSamples;
    IReadOnlyList<TrainingDataRow> valSamples;

    if (split.Training.Count == 0 || split.Validation.Count == 0)
    {
        if (generationIncomplete)
        {
            Console.WriteLine("The stable validation split produced an empty partition. Writing all available samples to partial training output.");
            trainSamples = allSamples;
            valSamples = [];
        }
        else
        {
            Console.WriteLine("The stable validation split produced an empty partition. Use more games or a different validation split seed.");
            return;
        }
    }
    else
    {
        trainSamples = split.Training;
        valSamples = split.Validation;
    }

    var finalOutputPath = generationIncomplete ? GetPartialOutputPath(outputPath) : outputPath;
    if (generationIncomplete)
    {
        Console.WriteLine($"Writing partial output: {Path.GetFullPath(finalOutputPath)}");
    }

    Console.WriteLine($"Train={trainSamples.Count:N0}  Validation={valSamples.Count:N0}  Seed={validationSplitSeed}");
    Console.WriteLine("Writing CSV files...");

    TrainingCsvWriter.Write(finalOutputPath, modus, trainSamples, extractor.FeatureCount);
    var valPath = Path.ChangeExtension(finalOutputPath, ".val.csv");
    TrainingCsvWriter.Write(valPath, modus, valSamples, extractor.FeatureCount);

    var trajectoryPath = Path.ChangeExtension(finalOutputPath, ".trajectory.csv");
    var valTrajectoryPath = Path.ChangeExtension(valPath, ".trajectory.csv");
    var gamesPath = Path.ChangeExtension(finalOutputPath, ".games.csv");
    TrajectoryCsvWriter.WritePositions(trajectoryPath, trainSamples);
    TrajectoryCsvWriter.WritePositions(valTrajectoryPath, valSamples);
    TrajectoryCsvWriter.WriteGames(gamesPath, completedGames);

    if (collectScoreGapDiagnostics)
    {
        var explorationPath = Path.ChangeExtension(finalOutputPath, ".exploration.csv");
        ExplorationCsvWriter.WriteDecisions(explorationPath, explorationDecisions);
        Console.WriteLine($"Written: {explorationPath}");
    }


    if (collectSelectiveTwoPlyDiagnostics)
    {
        var searchPath = Path.ChangeExtension(finalOutputPath, ".search.csv");
        SelectiveTwoPlySearchCsvWriter.WriteDecisions(searchPath, searchDecisions);
        Console.WriteLine($"Written: {searchPath}");
    }

    Console.WriteLine($"Written: {finalOutputPath}");
    Console.WriteLine($"Written: {valPath}");
    Console.WriteLine($"Written: {trajectoryPath}");
    Console.WriteLine($"Written: {valTrajectoryPath}");
    Console.WriteLine($"Written: {gamesPath}");
    Console.WriteLine($@"Elapsed: {stopwatch.Elapsed:dd\:hh\:mm\:ss}");
    Console.WriteLine(generationIncomplete ? "Partial output complete." : "Complete.");
}

#endregion Generate Training Data

#region Prompt Helpers

static T PromptEnum<T>(string label, T[] options, T defaultValue)
{
    Console.WriteLine($"{label}:");
    for (var i = 0; i < options.Length; i++)
        Console.WriteLine($"  {i + 1}  {options[i]}");
    Console.Write($"Select [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    if (int.TryParse(input, out var idx) && idx >= 1 && idx <= options.Length)
        return options[idx - 1];
    return defaultValue;
}

static int PromptInt(string label, int defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return int.TryParse(input, out var v) ? v : defaultValue;
}

static float PromptFloat(string label, float defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return float.TryParse(input, out var v) ? v : defaultValue;
}

static int? PromptOptionalInt(string label)
{
    Console.Write($"{label} [random]: ");
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input))
        return null;
    if (int.TryParse(input, out var value))
        return value;

    Console.WriteLine("Invalid integer. Using a random seed.");
    return null;
}

static string PromptString(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return string.IsNullOrEmpty(input) ? defaultValue : input;
}

static bool PromptBool(string label, bool defaultValue)
{
    Console.Write($"{label} yes/no [{(defaultValue ? "YES" : "NO")}]: ");
    var input = Console.ReadLine()?.Trim().ToLower();
    if (string.IsNullOrEmpty(input))
        return defaultValue;
    if (input == "y" || input == "yes")
        return true;
    if (input == "n" || input == "no")
        return false;
    return defaultValue;
}

#endregion Prompt Helpers

#region Helpers

static void StopNeuralEvalService(BatchedNeuralEvalService? service)
{
    if (service == null)
        return;

    try
    {
        service.StopAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    finally
    {
        service.Dispose();
    }
}

static string GetPartialOutputPath(string outputPath)
{
    var directory = Path.GetDirectoryName(outputPath);
    var fileName = Path.GetFileNameWithoutExtension(outputPath);
    var extension = Path.GetExtension(outputPath);
    var partialFileName = $"{fileName}.partial{extension}";
    return string.IsNullOrEmpty(directory) ? partialFileName : Path.Combine(directory, partialFileName);
}

static IFeatureVectorExtractor GetFeatureVectorExtractor(GameModus modus)
{
    IFeatureVectorExtractor extractor = modus switch
    {
        GameModus.Plakoto => new PlakotoFeatureVectorExtractor(),
        GameModus.Fevga => new FevgaFeatureVectorExtractor(),
        GameModus.Backgammon => new DefaultFeatureVectorExtractor(),
        GameModus.Tavla => new DefaultFeatureVectorExtractor(),
        GameModus.Portes => new DefaultFeatureVectorExtractor(),
        _ => throw new NotSupportedException($"Modus {modus} has no feature extractor.")
    };
    return extractor;
}

#endregion Helpers