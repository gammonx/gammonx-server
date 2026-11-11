using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public sealed class EvalScoreCalculatorTests
{
    public static IEnumerable<object[]> Modes() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Fevga, GameModus.Plakoto }
            .Select(mode => new object[] { mode });

    public static IEnumerable<object[]> ContactPaths() =>
        Modes().SelectMany(row => new[] { false, true }.Select(cheap => new object[] { row[0], cheap }));

    public static IEnumerable<object[]> MeasurementDirections()
    {
        // Player/opponent property pairs and their tendencies from the feature contract.
        (string Player, string Opponent, int PlayerDirection, bool Cheap)[] pairs =
        [
            ("HitProbability1", "HitOpponentProbability1", -1, false),
            ("HitProbability2", "HitOpponentProbability2", -1, false),
            ("BlotInStartRangeCount", "BlotInStartRangeCountOpp", -1, true),
            ("AnchorCount", "AnchorCountOpp", 1, true),
            ("PipToBearOff", "PipToBearOffOpp", -1, true),
            ("NumChFrontLastPin", "NumChFrontLastPinOpp", 1, false),
            ("EscapeProbability1", "EscapeProbability1Opp", 1, false),
            ("EscapeProbability2", "EscapeProbability2Opp", 1, false),
            ("PinCountPlayer", "PinCountOpp", -1, true),
            ("PlayerMotherPinned", "OppMotherPinned", -1, false),
            ("MotherDistancePlayer", "MotherDistanceOpp", -1, false),
            ("MaxPrimeLengthPlayer", "MaxPrimeLengthOpp", 1, true),
            ("HomebarCountPlayer", "HomebarCountOpp", -1, true),
            ("PrimeProbabilityPlayer", "PrimeProbabilityOpp", 1, false),
            ("AnchorCountInFrontPlayer", "AnchorCountInFrontOpp", 1, true),
            ("AverageStackHeightPlayer", "AverageStackHeightOpp", -1, true),
            ("AverageDistanceToBearOffPlayer", "AverageDistanceToBearOffOpp", -1, true),
            ("AverageGapSizePlayer", "AverageGapSizeOpp", -1, true),
            ("CheckersInPrimeZonePlayer", "CheckersInPrimeZoneOpp", 1, true)
        ];
        foreach (var mode in Modes().Select(row => (GameModus)row[0]))
        foreach (var pair in pairs)
        {
            yield return [mode, pair.Player, pair.PlayerDirection, pair.Cheap];
            yield return [mode, pair.Opponent, -pair.PlayerDirection, pair.Cheap];
        }
    }

    [Theory]
    [MemberData(nameof(MeasurementDirections))]
    public void EachWeightUsesItsMatchingMeasurementAndStrategicDirection(GameModus mode, string name, int direction, bool cheap)
    {
        object boxed = new EvalResultModel();
        typeof(EvalResultModel).GetProperty(name)!.SetValue(boxed, 1d);
        var one = name switch
        {
            "HitProbability2" => "HitProbability1",
            "HitOpponentProbability2" => "HitOpponentProbability1",
            "EscapeProbability2" => "EscapeProbability1",
            "EscapeProbability2Opp" => "EscapeProbability1Opp",
            _ => null
        };
        if (one != null) typeof(EvalResultModel).GetProperty(one)!.SetValue(boxed, 1d);
        var raw = (EvalResultModel)boxed;
        var weights = new ContactWeightModel();
        typeof(ContactWeightModel).GetProperty(name + "Weight")!.SetValue(weights, 0.4);

        var fullScore = EvalScoreCalculator.CalculateScore(raw, weights, mode);
        var cheapScore = EvalScoreCalculator.CalculateCheapScore(raw, weights, new RaceWeightModel(), mode).Item2;
        Assert.True(direction * fullScore > 0, $"{mode}: {name} has the wrong weight or direction.");
        Assert.Equal(cheap ? fullScore : 0, cheapScore, 12);
        Assert.InRange(Math.Abs(fullScore), double.Epsilon, 0.4);
    }

    [Fact]
    public void DirectionCasesCoverEveryContactWeight()
    {
        var covered = MeasurementDirections().Select(row => (string)row[1])
            .Concat(["BlotCount", "BlotCountOpp", "PipDifference"])
            .Distinct().Select(name => name + "Weight").Order();
        Assert.Equal(typeof(ContactWeightModel).GetProperties().Select(property => property.Name).Order(), covered);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void CheapRaceIgnoresContactFeaturesAndKeepsOwnOpponentProgressDirections(GameModus mode)
    {
        var contact = new ContactWeightModel { BlotCountWeight = 0.7, BlotCountOppWeight = 0.2 };
        var race = new RaceWeightModel { PipToBearOffWeight = 0.3, PipToBearOffOppWeight = 0.6 };
        var homePips = mode == GameModus.Plakoto ? 270 : 285;
        var baseline = EvalScoreCalculator.CalculateCheapScore(new EvalResultModel
        {
            Race = true, BlotCount = 15, BlotCountOpp = 9
        }, contact, race, mode).Item2;
        var own = EvalScoreCalculator.CalculateCheapScore(new EvalResultModel
        {
            Race = true, PipToBearOff = homePips
        }, contact, race, mode).Item2;
        var opponent = EvalScoreCalculator.CalculateCheapScore(new EvalResultModel
        {
            Race = true, PipToBearOffOpp = homePips
        }, contact, race, mode).Item2;
        Assert.Equal(0, baseline);
        Assert.Equal(-0.3, own, 12);
        Assert.Equal(0.6, opponent, 12);
    }

    [Theory]
    [MemberData(nameof(ContactPaths))]
    public void SingletonCountsFavorBoardControlInFevgaAndPenalizeExposureElsewhere(GameModus mode, bool cheap)
    {
        var weights = new ContactWeightModel { BlotCountWeight = 0.7, BlotCountOppWeight = 0.2 };
        var direction = mode == GameModus.Fevga ? 1 : -1;
        double Score(EvalResultModel raw) => cheap
            ? EvalScoreCalculator.CalculateCheapScore(raw, weights, new RaceWeightModel(), mode).Item2
            : EvalScoreCalculator.CalculateScore(raw, weights, mode);

        var neutral = Score(new EvalResultModel());
        var own = Score(new EvalResultModel { BlotCount = 3 });
        var opponent = Score(new EvalResultModel { BlotCountOpp = 6 });
        Assert.Equal(0, neutral);
        Assert.Equal(direction * 0.7 * 3 / 15, own, 12);
        Assert.Equal(-direction * 0.2 * 6 / 15, opponent, 12);
        Assert.True(direction * own > neutral);
        Assert.True(direction * opponent < neutral);
    }

    [Theory]
    [MemberData(nameof(ContactPaths))]
    public void PipContributionIsSignedAndUsesTheConfiguredMagnitude(GameModus mode, bool cheap)
    {
        var weights = new ContactWeightModel { PipDifferenceWeight = 0.7 };
        var raceWeights = new RaceWeightModel { PipDifferenceWeight = 0.3 };
        var maxPips = mode == GameModus.Plakoto ? 360 : 375;
        foreach (var pips in new[] { -maxPips, -120, 0, 120, maxPips })
        {
            var raw = new EvalResultModel { PipDifference = pips };
            var full = EvalScoreCalculator.CalculateScore(raw, weights, mode);
            var contact = EvalScoreCalculator.CalculateCheapScore(raw, weights, raceWeights, mode);
            var race = EvalScoreCalculator.CalculateCheapScore(new EvalResultModel { Race = true, PipDifference = pips }, weights, raceWeights, mode);
            Assert.Equal((double)pips / maxPips * 0.7, cheap ? contact.Item2 : full, 12);
            Assert.Equal((double)pips / maxPips * 0.3, race.Item2, 12);
            Assert.Equal(0.5 + (double)pips / (2 * maxPips), contact.Item1.PipDifference, 12);
            Assert.Equal(contact.Item1.PipDifference, race.Item1.PipDifference);
        }
    }

    [Fact]
    public void CheapScoreUsesMatchingAverageDistanceWeights()
    {
        var weights = new ContactWeightModel
        {
            AverageDistanceToBearOffPlayerWeight = 0.7,
            AverageDistanceToBearOffOppWeight = 0.2,
        };
        var eval = new EvalResultModel
        {
            AverageDistanceToBearOffPlayer = 12.5,
            AverageDistanceToBearOffOpp = 25,
        };

        var (_, score) = EvalScoreCalculator.CalculateCheapScore(
            eval,
            weights,
            new RaceWeightModel(),
            GameModus.Backgammon);

        Assert.Equal(-0.15, score, precision: 10);
    }
}
