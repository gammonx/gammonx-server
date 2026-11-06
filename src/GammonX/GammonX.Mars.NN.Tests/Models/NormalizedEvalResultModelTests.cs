using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Models;

public sealed class NormalizedEvalResultModelTests
{
    [Fact]
    public void DomainCasesCoverEveryNumericProperty()
    {
        var rawNames = typeof(EvalResultModel).GetProperties().Where(p => p.PropertyType != typeof(bool)).Select(p => p.Name).Order();
        var normalizedNames = typeof(NormalizedEvalResultModel).GetProperties().Where(p => p.PropertyType == typeof(double)).Select(p => p.Name).Order();
        
        Assert.Equal(rawNames, Domains(GameModus.Backgammon).Keys.Order());
        Assert.Equal(rawNames, normalizedNames);
    }

    [Theory]
    [MemberData(nameof(FeatureDomains))]
    public void BoundsAndInteriorValuesNormalizeWithoutChangingDirection(GameModus mode, string name, double min, double max, bool integer)
    {
        var middle = integer ? Math.Floor((min + max) / 2) : max / 2;
        double previous = -1;
        
        foreach (var value in new[] { min, middle, max })
        {
            var model = Normalize(Raw(name, value), mode);
            AssertAllNormalized(model);
            var actual = (double)typeof(NormalizedEvalResultModel).GetProperty(name)!.GetValue(model)!;
            var expected = name switch
            {
                "PipDifference" => 0.5 + value / (2 * max),
                "TurnNumber" => value / (value + 100d),
                _ => value / max
            };
            
            Assert.Equal(expected, actual, 12);
            Assert.True(actual >= previous);
            previous = actual;
        }
    }

    [Theory]
    [MemberData(nameof(FeatureDomains))]
    public void RejectsInvalidValuesInsteadOfClamping(GameModus mode, string name, double min, double max, bool integer)
    {
        var invalid = name == "TurnNumber"
            ? new[] { -1d }
            : new[] { min - 1, max + 1, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
        
        foreach (var value in invalid)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Normalize(Raw(name, value), mode));
            Assert.Equal(name, exception.ParamName);
        }
        
        if (integer && name != "TurnNumber")
            Assert.Throws<ArgumentOutOfRangeException>(() => Normalize(Raw(name, 0.5), mode));
        
        if (name.StartsWith("AverageStackHeight") || name.StartsWith("AverageDistanceToBearOff"))
            Assert.Throws<ArgumentOutOfRangeException>(() => Normalize(Raw(name, 0.5), mode));
    }

    [Theory]
    [InlineData("HitProbability1", "HitProbability2")]
    [InlineData("HitOpponentProbability1", "HitOpponentProbability2")]
    [InlineData("EscapeProbability1", "EscapeProbability2")]
    [InlineData("EscapeProbability1Opp", "EscapeProbability2Opp")]
    public void TwoCheckerProbabilityCannotExceedOneCheckerProbability(string one, string two)
    {
        object raw = new EvalResultModel();
        
        typeof(EvalResultModel).GetProperty(one)!.SetValue(raw, 0.25);
        typeof(EvalResultModel).GetProperty(two)!.SetValue(raw, 0.5);
        
        Assert.Throws<ArgumentException>(() => Normalize((EvalResultModel)raw, GameModus.Plakoto));
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void EqualPipsAreNeutralAndSwappingPlayersComplementsTheDifference(GameModus mode)
    {
        Assert.Equal(0.5, Normalize(new EvalResultModel(), mode).PipDifference);
        
        var ahead = Normalize(new EvalResultModel { PipDifference = 120 }, mode).PipDifference;
        var behind = Normalize(new EvalResultModel { PipDifference = -120 }, mode).PipDifference;
        
        Assert.True(ahead > 0.5);
        Assert.True(behind < 0.5);
        Assert.Equal(1, ahead + behind, 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(250)]
    [InlineData(int.MaxValue)]
    public void LongGamesUseBoundedTurnCompression(int turn)
    {
        var model = Normalize(new EvalResultModel { TurnNumber = turn }, GameModus.Backgammon);
        
        Assert.Equal(turn / (turn + 100d), model.TurnNumber, 12);
        
        AssertAllNormalized(model);
    }

    [Theory]
    [InlineData(GameModus.Unknown)]
    [InlineData((GameModus)1234)]
    public void RejectsUnsupportedModes(GameModus mode) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Normalize(new EvalResultModel(), mode));

    [Theory]
    [MemberData(nameof(Modes))]
    public void StartingPositionsAndVectorsAreNormalizedForBothColors(GameModus mode)
    {
        var board = BoardServiceFactory.Create(mode).CreateBoard();
        foreach (var white in new[] { true, false })
        {
            var model = Evaluate(board, white, 250);
            AssertAllNormalized(model);
            
            Assert.Equal(0.5, model.PipDifference);
            var extractor = FeatureVectorExtractorFactory.Create(mode);
            var allocated = extractor.Extract(model, board, white);
            var buffer = new float[extractor.FeatureCount];
            extractor.Extract(model, board, white, buffer);
            
            Assert.Equal(allocated, buffer);
            Assert.All(buffer, value => Assert.True(float.IsFinite(value) && value >= 0 && value <= 1));
            
            if (mode == GameModus.Fevga)
            {
                Assert.Equal(1d / 15, model.BlotCount);
                Assert.Equal(1d / 15, model.BlotCountOpp);
            }

            if (mode == GameModus.Plakoto)
            {
                Assert.Equal(1, model.PipToBearOff);
            }
        }
    }

    [Fact]
    public void LargePrimesPinsAndPipDifferencesStayInRangeThroughEvaluation()
    {
        var standard = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();
        
        Array.Clear(standard.Fields);
        for (var i = 0; i < 7; i++) standard.Fields[i] = -2;
        standard.Fields[7] = -1; standard.Fields[23] = 15;
        var prime = Evaluate(standard);
        
        Assert.Equal(1, prime.MaxPrimeLengthPlayer);
        AssertAllNormalized(prime);

        Array.Clear(standard.Fields);
        standard.Fields[0] = -15; standard.Fields[1] = 15;
        
        Assert.Equal(0.06, Evaluate(standard).PipDifference, 12);
        Assert.Equal(0.94, Evaluate(standard, false).PipDifference, 12);

        var pinsBoard = BoardServiceFactory.Create(GameModus.Plakoto).CreateBoard();
        
        Array.Clear(pinsBoard.Fields);
        var pins = (IPinModel)pinsBoard;
        for (var i = 6; i < 14; i++) { pinsBoard.Fields[i] = -1; pins.PinnedFields[i] = 1; }
        pinsBoard.Fields[18] = -7; pinsBoard.Fields[23] = 7;
        var pinModel = Evaluate(pinsBoard);
        
        Assert.Equal(0.6, pinModel.AnchorCount);
        AssertAllNormalized(pinModel);
    }

    public static IEnumerable<object[]> Modes() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga }
            .Select(mode => new object[] { mode });

    private static readonly string[] ProbabilityNames =
    [
        "HitProbability1", "HitProbability2", "HitOpponentProbability1", "HitOpponentProbability2",
        "EscapeProbability1", "EscapeProbability2", "EscapeProbability1Opp", "EscapeProbability2Opp",
        "PrimeProbabilityPlayer", "PrimeProbabilityOpp"
    ];

    private static Dictionary<string, (double Min, double Max, bool Integer)> Domains(GameModus mode)
    {
        var distance = mode == GameModus.Plakoto ? 24 : 25;
        var madePoints = mode is GameModus.Plakoto or GameModus.Fevga ? 15 : 7;
        var domains = new Dictionary<string, (double, double, bool)>();
        void Add(double maximum, bool integral, params string[] names)
        {
            foreach (var name in names)
                domains.Add(name, (0, maximum, integral));
        }
        Add(1, false, ProbabilityNames);
        Add(15, true, "BlotCount", "BlotCountOpp", "NumChFrontLastPin", "NumChFrontLastPinOpp",
            "PinCountPlayer", "PinCountOpp", "HomebarCountPlayer", "HomebarCountOpp",
            "CheckersInPrimeZonePlayer", "CheckersInPrimeZoneOpp");
        Add(6, true, "BlotInStartRangeCount", "BlotInStartRangeCountOpp");
        Add(madePoints, true, "AnchorCount", "AnchorCountOpp", "MaxPrimeLengthPlayer", "MaxPrimeLengthOpp",
            "AnchorCountInFrontPlayer", "AnchorCountInFrontOpp");
        Add(15 * (distance - 6), true, "PipToBearOff", "PipToBearOffOpp");
        domains.Add("PipDifference", (-15 * distance, 15 * distance, true));
        Add(1, true, "OppMotherPinned", "PlayerMotherPinned");
        Add(18, true, "MotherDistancePlayer", "MotherDistanceOpp");
        Add(int.MaxValue, true, "TurnNumber");
        Add(15, false, "AverageStackHeightPlayer", "AverageStackHeightOpp");
        Add(distance, false, "AverageDistanceToBearOffPlayer", "AverageDistanceToBearOffOpp");
        Add(22, false, "AverageGapSizePlayer", "AverageGapSizeOpp");
        return domains;
    }

    public static IEnumerable<object[]> FeatureDomains() => Modes().SelectMany(row =>
    {
        var mode = (GameModus)row[0];
        return Domains(mode).Select(pair => new object[] { mode, pair.Key, pair.Value.Min, pair.Value.Max, pair.Value.Integer });
    });

    private static EvalResultModel Raw(string name, double value)
    {
        object raw = new EvalResultModel();
        // Supply a valid upper probability when independently testing a two-checker probability.
        var one = name switch
        {
            "HitProbability2" => "HitProbability1",
            "HitOpponentProbability2" => "HitOpponentProbability1",
            "EscapeProbability2" => "EscapeProbability1",
            "EscapeProbability2Opp" => "EscapeProbability1Opp",
            _ => null
        };
        if (one != null) typeof(EvalResultModel).GetProperty(one)!.SetValue(raw, 1d);
        var property = typeof(EvalResultModel).GetProperty(name)!;
        property.SetValue(raw, property.PropertyType == typeof(int) ? (object)checked((int)value) : value);
        return (EvalResultModel)raw;
    }

    private static NormalizedEvalResultModel Normalize(EvalResultModel raw, GameModus mode) => NormalizedEvalResultModel.From(raw, mode);

    private static NormalizedEvalResultModel Evaluate(IBoardModel board, bool white = true, int turn = 0) =>
        FeatureEvalServiceFactory.Create(board.Modus, null!).EvalPositionForTraining(new FeatureEvalOptions
        {
            BoardContract = board.ToContract(false), IsWhite = white, TurnNumber = turn
        });

    private static void AssertAllNormalized(NormalizedEvalResultModel model)
    {
        foreach (var property in typeof(NormalizedEvalResultModel).GetProperties().Where(p => p.PropertyType == typeof(double)))
        {
            var value = (double)property.GetValue(model)!;
            Assert.True(double.IsFinite(value) && value >= 0 && value <= 1, $"{property.Name} = {value}");
        }
    }
}
