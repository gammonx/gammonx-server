using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

public partial class PerformanceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task MeasureIdenticalInputs(int boardNumber)
    {
        var json = boardNumber switch
        {
            1 => MockBoards.PerformanceBoard1,
            2 => MockBoards.PerformanceBoard2,
            3 => MockBoards.PerformanceBoard3,
            _ => throw new ArgumentOutOfRangeException(nameof(boardNumber))
        };
        var contract = JsonConvert.DeserializeObject<BoardModelContract>(json);
        Assert.NotNull(contract);
        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard(contract);
        int[] rolls = boardNumber == 2 ? [3, 2] : [2, 2, 2, 2];
        var moves = boardService.GetUniqueLegalMoveSequences(board, false, rolls);
        using var neural = Assert.IsType<BatchedNeuralEvalService>(BatchedNeuralEvalService.Load(
            GameModus.Backgammon,
            Path.Combine("Data", "NeuralNets", "Backgammon", "training_net.dat"), CPU));
        await neural.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var observer = new DuplicateInputObserver(neural);
            var service = FeatureEvalServiceFactory.Create(GameModus.Backgammon, observer);
            output.WriteLine("DUPLICATES columns: board,root,logical_rows,unique_rows,within_batch_duplicates,cross_batch_duplicates,groups,groups_with_new_rows,unique_payload_bytes,cross_roll_duplicates");
            for (var index = 0; index < moves.Length; index++)
            {
                observer.Reset(ExpectedReplyGroups(boardService, board, moves[index]));
                var result = await service.EvalMoveSequenceAsync(
                    contract, false, moves[index], BotLevel.Expert, EvalWeights.DefaultContactWeights);
                Assert.True(double.IsFinite(result.Score));
                Assert.Equal(observer.Rows, observer.Unique + observer.WithinBatchDuplicates + observer.CrossBatchDuplicates);
                Assert.Equal(0, observer.RemainingGroups);
                output.WriteLine(FormattableString.Invariant(
                    $"DUPLICATES,Board{boardNumber},{index + 1},{observer.Rows},{observer.Unique},{observer.WithinBatchDuplicates},{observer.CrossBatchDuplicates},{observer.Groups},{observer.GroupsWithNewRows},{observer.PayloadBytes},{observer.CrossRollDuplicates}"));
            }
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
        }
    }

    private static IEnumerable<(int Roll, int Rows)> ExpectedReplyGroups(
        IBoardService service, IBoardModel initial, MoveSequenceModel move)
    {
        var root = initial.DeepClone();
        foreach (var step in move.Moves)
            service.MoveCheckerTo(root, step.From, step.To, false);
        for (var die1 = 1; die1 <= 6; die1++)
        {
            for (var die2 = die1; die2 <= 6; die2++)
            {
                int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];
                var count = Math.Max(1, service.GetUniqueLegalMoveSequences(root, true, rolls).Length);
                for (var start = 0; start < count; start += 32)
                    yield return (die1 * 10 + die2, Math.Min(32, count - start));
            }
        }
    }

    private sealed class DuplicateInputObserver(INeuralEvalService inner) : INeuralEvalService
    {
        private readonly Dictionary<float[], int> _seen = new(NeuralInputComparer.Instance);
        private Queue<(int Roll, int Rows)> _groups = new();
        public int Rows { get; private set; }
        public int Unique => _seen.Count;
        public int WithinBatchDuplicates { get; private set; }
        public int CrossBatchDuplicates { get; private set; }
        public int CrossRollDuplicates { get; private set; }
        public int Groups { get; private set; }
        public int GroupsWithNewRows { get; private set; }
        public long PayloadBytes { get; private set; }
        public int MaxBatchSize => inner.MaxBatchSize;
        public int RemainingGroups => _groups.Count;

        public void Reset(IEnumerable<(int Roll, int Rows)> groups)
        {
            _seen.Clear();
            _groups = new Queue<(int Roll, int Rows)>(groups);
            Rows = WithinBatchDuplicates = CrossBatchDuplicates = Groups = GroupsWithNewRows = 0;
            CrossRollDuplicates = 0;
            PayloadBytes = 0;
        }

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
            inner.ExtractFeatures(model, board, isWhite);

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
            inner.PredictAsync(model, board, isWhite);

        public Task<float[][]> PredictBatchAsync(float[][] features)
        {
            var group = _groups.Dequeue();
            Assert.Equal(group.Rows, features.Length);
            var batch = new HashSet<float[]>(NeuralInputComparer.Instance);
            var misses = 0;
            Groups++;
            foreach (var row in features)
            {
                Rows++;
                if (!batch.Add(row))
                {
                    WithinBatchDuplicates++;
                    if (_seen[row] != group.Roll)
                        CrossRollDuplicates++;
                    continue;
                }
                if (!_seen.TryGetValue(row, out var firstRoll))
                {
                    _seen.Add(row, group.Roll);
                    misses++;
                    PayloadBytes += row.Length * sizeof(float);
                }
                else
                {
                    CrossBatchDuplicates++;
                    if (firstRoll != group.Roll)
                        CrossRollDuplicates++;
                }
            }
            if (misses > 0)
                GroupsWithNewRows++;
            return inner.PredictBatchAsync(features);
        }
    }
}
