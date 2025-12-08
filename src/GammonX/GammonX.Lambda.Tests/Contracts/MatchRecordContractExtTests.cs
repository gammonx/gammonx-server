using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using GammonX.Models.History;

using Moq;

using Xunit;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Lambda.Tests.Contracts
{
    public class MatchRecordContractExtTests
    {
        private class FakeMatchHistory : IParsedMatchHistory
        {
            public HistoryFormat Format { get; set; } = HistoryFormat.MAT;
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = "test";
            public Guid Player1Id { get; set; } = Guid.NewGuid();
            public Guid Player2Id { get; set; } = Guid.NewGuid();
            public int Length { get; set; }
            public List<IParsedGameHistory> Games { get; set; } = new();

            public DateTime StartedAt { get; set; }
            public DateTime EndedAt { get; set; }

            public int PointCount(Guid _) => 12;
            public double AvgDoubleDiceCount(Guid _) => 3.4;
            public TimeSpan AvgDuration() => TimeSpan.FromSeconds(50);
            public int AvgTurnCount(Guid _) => 17;
            public double AvgDoubleOfferCount(Guid _) => 1.5;
        }

        [Fact]
        public void ToMatchMapsFieldsCorrectly()
        {
            // setting fixed ids for stable PK/SK tests
            var matchId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var playerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var contract = new MatchRecordContract
            {
                Id = matchId,
                PlayerId = playerId,
                Result = MatchResult.Won,
                Variant = MatchVariant.Backgammon,
                Type = MatchType.SevenPointGame,
                Modus = MatchModus.Ranked,
            };

            var history = new FakeMatchHistory
            {
                Length = 5,
                StartedAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                EndedAt = new DateTime(2025, 1, 1, 10, 30, 0, DateTimeKind.Utc),
            };

            // mapping to match item
            var item = contract.ToMatch(history);

            // verifying simple field copies
            Assert.Equal(matchId, item.Id);
            Assert.Equal(playerId, item.PlayerId);
            Assert.Equal(contract.Modus, item.Modus);
            Assert.Equal(contract.Variant, item.Variant);
            Assert.Equal(contract.Type, item.Type);
            Assert.Equal(contract.Result, item.Result);

            // verifying aggregated values
            Assert.Equal(12, item.Points);
            Assert.Equal(3.4, item.AvgDoubleDices);
            Assert.Equal(TimeSpan.FromSeconds(50), item.AvgDuration);
            Assert.Equal(17, item.AvgTurns);
            Assert.Equal(1.5, item.AvgDoubles);

            // verifying match duration
            Assert.Equal(TimeSpan.FromMinutes(30), item.Duration);
        }

        [Fact]
        public void ToMatchCreatesCorrectPrimaryKey()
        {
            var matchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var contract = new MatchRecordContract { Id = matchId };
            var history = new FakeMatchHistory();

            var item = contract.ToMatch(history);

            // pk must follow factory format (MATCH#{id})
            var expected = $"MATCH#{matchId}";
            Assert.Equal(expected, item.PK);
        }

        [Fact]
        public void ToMatchCreatesCorrectSortKeyForWonMatch()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                Result = MatchResult.Won
            };

            var item = contract.ToMatch(new FakeMatchHistory());

            // sk must reflect the won/lost logic (DETAILS#WON)
            Assert.Equal("DETAILS#WON", item.SK);
        }

        [Fact]
        public void ToMatchCreatesCorrectSortKeyForUnfinishedMatch()
        {
            var matchId = Guid.NewGuid();
            var playerId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

            var contract = new MatchRecordContract
            {
                Id = matchId,
                PlayerId = playerId,
                Result = MatchResult.Unknown
            };

            var item = contract.ToMatch(new FakeMatchHistory());

            // unfinished matches append NOTFINISHED#{playerId}
            Assert.Equal($"DETAILS#NOTFINISHED#{playerId}", item.SK);
        }

        [Fact]
        public void ToMatchCreatesCorrectGsiPrimaryKey()
        {
            var playerId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var contract = new MatchRecordContract { PlayerId = playerId };
            var item = contract.ToMatch(new FakeMatchHistory());

            // gsi pk must be PLAYER#{playerId}
            Assert.Equal($"PLAYER#{playerId}", item.GSI1PK);
        }

        [Fact]
        public void ToMatchCreatesCorrectGsiSortKey()
        {
            var contract = new MatchRecordContract
            {
                PlayerId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                Variant = MatchVariant.Backgammon,
                Type = MatchType.SevenPointGame,
                Modus = MatchModus.Ranked,
                Result = MatchResult.Lost
            };

            var item = contract.ToMatch(new FakeMatchHistory());

            // verifying GSI1SK (MATCH#{variant}#{type}#{modus}#LOST)
            // concrete format: "{variant}#{type}#{modus}#{WON|LOST|NOTFINISHED}"
            Assert.Equal("MATCH#Backgammon#SevenPointGame#Ranked#LOST", item.GSI1SK);
        }

        [Fact]
        public void ToMatchCorrectlyCalculatesAvgPipeCounts()
        {
            // contract-pipe-based methods must be forward-tested
            var contract = new MatchRecordContract
            {
                Games = new[]
                {
                new GameRecordContract { PipesLeft = 0, Result = GameResult.Single },
                new GameRecordContract { PipesLeft = 3, Result = GameResult.LostSingle },
                new GameRecordContract { PipesLeft = 7, Result = GameResult.LostSingle }
            }
            };

            var history = new FakeMatchHistory();

            var item = contract.ToMatch(history);

            // expected: only lost games => (3 + 7) / 2 = 5
            Assert.Equal(5, item.AvgPipesLeft);
        }

        [Fact]
        public void ToMatchFieldsMapProperly()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                Modus = MatchModus.Ranked,
                Result = MatchResult.Won,
                Variant = MatchVariant.Backgammon,
                Type = MatchType.SevenPointGame
            };

            var history = new Mock<IParsedMatchHistory>();
            var now = DateTime.UtcNow;

            // relevant: match length and dates
            history.Setup(h => h.Length).Returns(3);
            history.Setup(h => h.StartedAt).Returns(now.AddMinutes(-10));
            history.Setup(h => h.EndedAt).Returns(now);

            // relevant: point and statistic values
            history.Setup(h => h.PointCount(contract.PlayerId)).Returns(5);
            history.Setup(h => h.AvgDoubleDiceCount(contract.PlayerId)).Returns(2.5);
            history.Setup(h => h.AvgDuration()).Returns(TimeSpan.FromSeconds(20));
            history.Setup(h => h.AvgTurnCount(contract.PlayerId)).Returns(12);
            history.Setup(h => h.AvgDoubleOfferCount(contract.PlayerId)).Returns(1.2);

            // relevant: contract based values
            contract.Games = Array.Empty<GameRecordContract>();

            // relevant: gammon aggregations
            contract.MatchHistory = "";

            var result = contract.ToMatch(history.Object);

            Assert.Equal(contract.Id, result.Id);
            Assert.Equal(contract.PlayerId, result.PlayerId);
            Assert.Equal(contract.Modus, result.Modus);
            Assert.Equal(contract.Result, result.Result);
            Assert.Equal(contract.Variant, result.Variant);
            Assert.Equal(contract.Type, result.Type);
            Assert.Equal(5, result.Points);
            Assert.Equal(TimeSpan.FromMinutes(10), result.Duration);
            Assert.Equal(2.5, result.AvgDoubleDices);
            Assert.Equal(TimeSpan.FromSeconds(20), result.AvgDuration);
            Assert.Equal(12, result.AvgTurns);
            Assert.Equal(1.2, result.AvgDoubles);
        }

        [Fact]
        public void ToMatchHandlesEmptyGamesAndStatistics()
        {
            var contract = new MatchRecordContract { Id = Guid.NewGuid(), PlayerId = Guid.NewGuid() };

            var history = new Mock<IParsedMatchHistory>();
            var now = DateTime.UtcNow;

            // empty game list implies zeros
            history.Setup(h => h.Length).Returns(0);
            history.Setup(h => h.StartedAt).Returns(now);
            history.Setup(h => h.EndedAt).Returns(now);

            history.Setup(h => h.PointCount(It.IsAny<Guid>())).Returns(0);
            history.Setup(h => h.AvgDoubleDiceCount(It.IsAny<Guid>())).Returns(0);
            history.Setup(h => h.AvgDuration()).Returns(TimeSpan.Zero);
            history.Setup(h => h.AvgTurnCount(It.IsAny<Guid>())).Returns(0);
            history.Setup(h => h.AvgDoubleOfferCount(It.IsAny<Guid>())).Returns(0);

            var result = contract.ToMatch(history.Object);

            Assert.Equal(0, result.Points);
            Assert.Equal(0, result.AvgTurns);
            Assert.Equal(TimeSpan.Zero, result.AvgDuration);
            Assert.Equal(0, result.AvgDoubleDices);
            Assert.Equal(0, result.AvgDoubles);
            Assert.Equal(0, result.Length);
        }

        [Fact]
        public void ToMatchNegativeStatisticValues()
        {
            var contract = new MatchRecordContract { Id = Guid.NewGuid(), PlayerId = Guid.NewGuid() };

            var history = new Mock<IParsedMatchHistory>();
            var now = DateTime.UtcNow;

            // negative durations: should pass through (consumer responsibility)
            history.Setup(h => h.StartedAt).Returns(now);
            history.Setup(h => h.EndedAt).Returns(now.AddMinutes(-5));

            // unusual negative statistics
            history.Setup(h => h.Length).Returns(-99);
            history.Setup(h => h.PointCount(contract.PlayerId)).Returns(-5);
            history.Setup(h => h.AvgDoubleDiceCount(contract.PlayerId)).Returns(-2.2);
            history.Setup(h => h.AvgDuration()).Returns(TimeSpan.FromSeconds(-10));
            history.Setup(h => h.AvgTurnCount(contract.PlayerId)).Returns(-3);
            history.Setup(h => h.AvgDoubleOfferCount(contract.PlayerId)).Returns(-1);

            var result = contract.ToMatch(history.Object);

            Assert.Equal(-99, result.Length);
            Assert.Equal(-5, result.Points);
            Assert.Equal(-2.2, result.AvgDoubleDices);
            Assert.Equal(TimeSpan.FromSeconds(-10), result.AvgDuration);
            Assert.Equal(-3, result.AvgTurns);
            Assert.Equal(-1, result.AvgDoubles);
            Assert.Equal(TimeSpan.FromMinutes(-5), result.Duration);
        }

        [Fact]
        public void ToMatchExtremeLongDurations()
        {
            var contract = new MatchRecordContract { Id = Guid.NewGuid(), PlayerId = Guid.NewGuid() };
            var history = new Mock<IParsedMatchHistory>();

            // extreme long duration
            var start = DateTime.UtcNow.AddYears(-10);
            var end = DateTime.UtcNow;

            history.Setup(h => h.StartedAt).Returns(start);
            history.Setup(h => h.EndedAt).Returns(end);

            history.Setup(h => h.Length).Returns(1);
            history.Setup(h => h.PointCount(It.IsAny<Guid>())).Returns(1);
            history.Setup(h => h.AvgDoubleDiceCount(It.IsAny<Guid>())).Returns(0);
            history.Setup(h => h.AvgDuration()).Returns(TimeSpan.Zero);
            history.Setup(h => h.AvgTurnCount(It.IsAny<Guid>())).Returns(1);
            history.Setup(h => h.AvgDoubleOfferCount(It.IsAny<Guid>())).Returns(0);

            var result = contract.ToMatch(history.Object);

            Assert.Equal(end - start, result.Duration);
        }

        [Fact]
        public void ToMatchEdgeEnumValues()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),

                // important: force all enums to Unknown to ensure they map correctly
                Modus = MatchModus.Unknown,
                Result = MatchResult.Unknown,
                Variant = MatchVariant.Unknown,
                Type = MatchType.Unknown
            };

            var history = new Mock<IParsedMatchHistory>();
            var now = DateTime.UtcNow;

            history.Setup(h => h.StartedAt).Returns(now);
            history.Setup(h => h.EndedAt).Returns(now);
            history.Setup(h => h.Length).Returns(0);

            var result = contract.ToMatch(history.Object);

            Assert.Equal(MatchModus.Unknown, result.Modus);
            Assert.Equal(MatchResult.Unknown, result.Result);
            Assert.Equal(MatchVariant.Unknown, result.Variant);
            Assert.Equal(MatchType.Unknown, result.Type);
        }

        [Fact]
        public void ToMatchHistoryMapsBasicFieldsCorrectly()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                MatchHistory = "some match history data",
                Format = HistoryFormat.MAT
            };

            // convert contract to match history item
            var result = contract.ToMatchHistory();

            Assert.Equal(contract.Id, result.MatchId);
            Assert.Equal("some match history data", result.Data);
            Assert.Equal(HistoryFormat.MAT, result.Format);
        }

        [Fact]
        public void ToMatchHistoryHandlesEmptyHistoryString()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                MatchHistory = string.Empty,
                Format = HistoryFormat.Unknown
            };

            var result = contract.ToMatchHistory();

            Assert.Equal(contract.Id, result.MatchId);
            Assert.Equal(string.Empty, result.Data);
            Assert.Equal(HistoryFormat.Unknown, result.Format);
        }

        [Fact]
        public void ToMatchHistoryHandlesExtremeGuidValues()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.Empty,
                MatchHistory = "extreme guid test",
                Format = HistoryFormat.MAT
            };

            var result = contract.ToMatchHistory();

            Assert.Equal(Guid.Empty, result.MatchId);
            Assert.Equal("extreme guid test", result.Data);
            Assert.Equal(HistoryFormat.MAT, result.Format);
        }

        [Fact]
        public void ToMatchHistoryHandlesLongHistoryData()
        {
            // create very long string to test edge case
            var longData = new string('x', 100_000);

            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                MatchHistory = longData,
                Format = HistoryFormat.MAT
            };

            var result = contract.ToMatchHistory();

            Assert.Equal(longData.Length, result.Data.Length);
            Assert.Equal(longData, result.Data);
        }

        [Fact]
        public void ToMatchHistoryConstructsPKAndSKProperly()
        {
            var contract = new MatchRecordContract
            {
                Id = Guid.NewGuid(),
                MatchHistory = "test history",
                Format = HistoryFormat.MAT
            };

            var result = contract.ToMatchHistory();

            // pk should contain match id string representation
            Assert.Contains(contract.Id.ToString(), result.PK);

            // sk should match factory sk format (non empty)
            Assert.False(string.IsNullOrWhiteSpace(result.SK));
        }
    }
}
