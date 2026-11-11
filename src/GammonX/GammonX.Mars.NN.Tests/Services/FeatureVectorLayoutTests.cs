using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public sealed class FeatureVectorLayoutTests
{
    // These lists specify the public vector contract independently of the extractors.
    
    private static readonly string[] DefaultScalars =
    [
        "MaxPrimeLengthPlayer", "MaxPrimeLengthOpp", "HomebarCountPlayer", "HomebarCountOpp",
        "BlotCount", "BlotCountOpp", "AnchorCountInFrontPlayer", "AnchorCountInFrontOpp",
        "AverageStackHeightPlayer", "AverageStackHeightOpp", "AverageDistanceToBearOffPlayer", "AverageDistanceToBearOffOpp",
        "AverageGapSizePlayer", "AverageGapSizeOpp", "CheckersInPrimeZonePlayer", "CheckersInPrimeZoneOpp",
        "PipToBearOff", "PipToBearOffOpp", "PipDifference"
    ];

    private static readonly string[] PlakotoScalars =
    [
        "PinCountOpp", "PinCountPlayer", "OppMotherPinned", "PlayerMotherPinned",
        "MotherDistancePlayer", "MotherDistanceOpp", "NumChFrontLastPin", "NumChFrontLastPinOpp",
        "BlotCount", "BlotCountOpp", "BlotInStartRangeCount", "BlotInStartRangeCountOpp",
        "AnchorCount", "AnchorCountOpp", "AverageStackHeightPlayer", "AverageStackHeightOpp",
        "AverageDistanceToBearOffPlayer", "AverageDistanceToBearOffOpp", "PipDifference", "PipToBearOff", "PipToBearOffOpp"
    ];

    public static IEnumerable<object[]> Players() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Fevga, GameModus.Plakoto }
            .SelectMany(mode => new[] { true, false }.Select(white => new object[] { mode, white }));

    [Theory]
    [MemberData(nameof(Players))]
    public void ScalarChannelsKeepTheirExactIndices(GameModus mode, bool white)
    {
        var board = BoardServiceFactory.Create(mode).CreateBoard();
        board.BearOffChecker(true, 3);
        board.BearOffChecker(false, 5);
        
        var names = mode == GameModus.Plakoto ? PlakotoScalars : DefaultScalars;
        
        foreach (var race in new[] { false, true })
        {
            object boxed = new NormalizedEvalResultModel { Race = race };
            var properties = typeof(NormalizedEvalResultModel).GetProperties().Where(p => p.PropertyType == typeof(double)).ToArray();
            
            for (var i = 0; i < properties.Length; i++)
            {
                properties[i].SetValue(boxed, (i + 1) / 64d);
            }
            
            var model = (NormalizedEvalResultModel)boxed;
            float Value(string name) => (float)(double)typeof(NormalizedEvalResultModel).GetProperty(name)!.GetValue(model)!;
            float[] expected = [.. names.Select(Value), race ? 1f : 0f, 0f,
                (white ? 3f : 5f) / 15, (white ? 5f : 3f) / 15, Value("TurnNumber")];

            var (allocated, buffered) = ExtractBoth(model, board, white);
            
            Assert.Equal(expected, allocated.Take(expected.Length));
            Assert.Equal(expected, buffered.Take(expected.Length));
        }
    }

    [Theory]
    [MemberData(nameof(Players))]
    public void EveryPhysicalPointKeepsItsCanonicalIndexAndOpponentFirstChannels(GameModus mode, bool white)
    {
        var board = BoardServiceFactory.Create(mode).CreateBoard();
        
        Array.Clear(board.Fields);
        var rawOffset = mode == GameModus.Plakoto ? 26 : 24;

        for (var physical = 0; physical < 24; physical++)        
        foreach (var own in new[] { false, true })
        {
            // The engine stores white checkers as negative. Canonical own checkers stay negative.
            board.Fields[physical] = own == white ? -4 : 4;
            var canonical = white ? physical : mode == GameModus.Fevga ? (physical + 12) % 24 : 23 - physical;
            var channel = rawOffset + canonical * 8 + (own ? 4 : 0);
            var expected = new float[mode == GameModus.Plakoto ? 266 : 216];
            expected[channel] = expected[channel + 1] = expected[channel + 2] = 1;
            expected[channel + 3] = 1f / 12;

            var (allocated, buffered) = ExtractBoth(new NormalizedEvalResultModel(), board, white);
            
            Assert.Equal(expected, allocated);
            Assert.Equal(expected, buffered);
            Assert.Equal(own == white ? -4 : 4, board.Fields[physical]);
            
            board.Fields[physical] = 0;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlakotoPinsKeepOpponentThenPlayerOwnershipAtEveryPoint(bool white)
    {
        var board = BoardServiceFactory.Create(GameModus.Plakoto).CreateBoard();
        var pins = (IPinModel)board;
        Array.Clear(board.Fields);
        Array.Clear(pins.PinnedFields);
        
        for (var physical = 0; physical < 24; physical++)
        foreach (var ownPin in new[] { false, true })
        {
            var pin = ownPin == white ? -1 : 1;
            pins.PinnedFields[physical] = pin;
            board.Fields[physical] = -pin; // A legal opposing pinner.
            var canonical = white ? physical : 23 - physical;
            var expected = new float[266];
            expected[26 + canonical * 8 + (ownPin ? 0 : 4)] = 1;
            expected[218 + canonical * 2 + (ownPin ? 1 : 0)] = 1;

            var (allocated, buffered) = ExtractBoth(new NormalizedEvalResultModel(), board, white);
            
            Assert.Equal(expected, allocated);
            Assert.Equal(expected, buffered);
            Assert.Equal(pin, pins.PinnedFields[physical]);
            Assert.Equal(-pin, board.Fields[physical]);
            
            pins.PinnedFields[physical] = board.Fields[physical] = 0;
        }
    }

    private static (float[] Allocated, float[] Buffered) ExtractBoth(NormalizedEvalResultModel model, IBoardModel board, bool white)
    {
        var extractor = FeatureVectorExtractorFactory.Create(board.Modus);
        
        Assert.Equal(board.Modus == GameModus.Plakoto ? 266 : 216, extractor.FeatureCount);
        var buffer = Enumerable.Repeat(float.NaN, extractor.FeatureCount + 2).ToArray();
        buffer[0] = -99;
        buffer[^1] = 99;
        extractor.Extract(model, board, white, buffer.AsSpan(1, extractor.FeatureCount));
        
        Assert.Equal(-99, buffer[0]);
        Assert.Equal(99, buffer[^1]);
        var written = buffer[1..^1];
        
        Assert.All(written, value => Assert.True(float.IsFinite(value) && value >= 0 && value <= 1));
        return (extractor.Extract(model, board, white), written);
    }
}
