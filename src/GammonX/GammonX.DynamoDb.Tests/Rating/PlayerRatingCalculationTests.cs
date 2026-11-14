using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Services;
using GammonX.DynamoDb.Stats;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Rating
{
    public class PlayerRatingCalculationTests
    {
        [Fact]
        public async Task StrongerMatchPerformanceIncreasesWinnerGainAndLoserLoss()
        {
            // Arrange.
            var winner = ItemFactory.CreatePlayer();
            var loser = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            wonMatch.Gammons = 0;
            wonMatch.Backgammons = 0;
            var repo = new Mock<IDynamoDbRepository>();
            var reader = repo.As<IDynamoDbConsistentReader>();
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Array.Empty<PlayerRatingItem>());

            // Act.
            var normal = await repo.Object.CalculateMatchRatingsAsync(wonMatch, lostMatch);
            wonMatch.Gammons = 1;
            var stronger = await repo.Object.CalculateMatchRatingsAsync(wonMatch, lostMatch);

            // Assert.
            Assert.True(stronger.Winner.Period.MatchScore > normal.Winner.Period.MatchScore);
            Assert.True(stronger.Winner.Rating.Rating > normal.Winner.Rating.Rating);
            Assert.True(stronger.Loser.Rating.Rating < normal.Loser.Rating.Rating);
            Assert.Equal(1200, stronger.Winner.Period.PlayerRating);
            Assert.Equal(1200, stronger.Loser.Period.PlayerRating);
        }

        [Theory]
        [InlineData(-1d)]
        [InlineData(0d)]
        [InlineData(0.99)]
        [InlineData(1d)]
        [InlineData(7d)]
        [InlineData(36500d)]
        public async Task PairAppliesFullInactiveDaysToBothSnapshotsWithoutMutatingStoredRatings(double elapsedDays)
        {
            // Arrange.
            var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
            var winner = ItemFactory.CreatePlayer();
            var loser = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var winnerRating = PlayerRatingItemFactory.CreateInitial(winner.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            var loserRating = PlayerRatingItemFactory.CreateInitial(loser.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            winnerRating.RatingDeviation = 75;
            winnerRating.Sigma = 0.04;
            loserRating.RatingDeviation = 60;
            winnerRating.LastRatedAt = loserRating.LastRatedAt = now.AddDays(-elapsedDays);
            var repo = new Mock<IDynamoDbRepository>(MockBehavior.Strict);
            var reader = repo.As<IDynamoDbConsistentReader>();
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(winner.Id, winnerRating.SK)).ReturnsAsync([winnerRating]);
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(loser.Id, loserRating.SK)).ReturnsAsync([loserRating]);

            // Act.
            var pair = await repo.Object.CalculateMatchRatingsAsync(wonMatch, lostMatch, new FixedTimeProvider(now));

            // Assert: the closed form matches repeated Glicko-2 no-game updates, capped at the initial deviation.
            var periods = Math.Max(0, Math.Floor(elapsedDays));
            var expectedWinnerRd = Math.Min(350, Math.Sqrt(75 * 75 + periods * Math.Pow(0.04 * Glicko2Constants.Scale, 2)));
            var expectedLoserRd = Math.Min(350, Math.Sqrt(60 * 60 + periods * Math.Pow(0.06 * Glicko2Constants.Scale, 2)));
            Assert.Equal(expectedWinnerRd, pair.Winner.Period.PlayerRatingDeviation, 10);
            Assert.Equal(expectedLoserRd, pair.Loser.Period.PlayerRatingDeviation, 10);
            Assert.Equal(pair.Winner.Period.PlayerRatingDeviation, pair.Loser.Period.OpponentRatingDeviation);
            Assert.Equal(pair.Loser.Period.PlayerRatingDeviation, pair.Winner.Period.OpponentRatingDeviation);
            Assert.Equal(1.0, pair.Winner.Period.MatchScore + pair.Loser.Period.MatchScore, 12);
            Assert.Equal(now, pair.Winner.Rating.LastRatedAt);
            Assert.Equal(now, pair.Loser.Rating.LastRatedAt);
            Assert.Equal(now, pair.Winner.Period.CreatedAt);
            Assert.Equal(now, pair.Loser.Period.CreatedAt);
            Assert.Equal(75, winnerRating.RatingDeviation);
            Assert.Equal(60, loserRating.RatingDeviation);
            Assert.Equal(0, winnerRating.Revision);
            Assert.Equal(1200, winnerRating.Rating);
            Assert.Equal(now.AddDays(-elapsedDays), winnerRating.LastRatedAt);
            reader.VerifyAll();
        }

        [Fact]
        public async Task LegacyRatingStartsInactivityTrackingAndDoesNotCountTheSameGapTwice()
        {
            // Arrange.
            var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
            var winner = ItemFactory.CreatePlayer();
            var loser = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var currentWinner = PlayerRatingItemFactory.CreateInitial(winner.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            var currentLoser = PlayerRatingItemFactory.CreateInitial(loser.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            currentWinner.RatingDeviation = 75;
            var repo = new Mock<IDynamoDbRepository>();
            var reader = repo.As<IDynamoDbConsistentReader>();
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(winner.Id, currentWinner.SK)).ReturnsAsync(() => new[] { currentWinner });
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(loser.Id, currentLoser.SK)).ReturnsAsync(() => new[] { currentLoser });

            // Act: persist the first calculation, then rate another match less than one full day later.
            var first = await repo.Object.CalculateMatchRatingsAsync(wonMatch, lostMatch, new FixedTimeProvider(now));
            currentWinner = first.Winner.Rating;
            currentLoser = first.Loser.Rating;
            var second = await repo.Object.CalculateMatchRatingsAsync(wonMatch, lostMatch, new FixedTimeProvider(now.AddHours(12)));

            // Assert.
            Assert.Equal(75, first.Winner.Period.PlayerRatingDeviation);
            Assert.Equal(now, first.Winner.Rating.LastRatedAt);
            Assert.Equal(currentWinner.RatingDeviation, second.Winner.Period.PlayerRatingDeviation);
            Assert.Equal(currentLoser.RatingDeviation, second.Loser.Period.PlayerRatingDeviation);
            Assert.Equal(now.AddHours(12), second.Winner.Rating.LastRatedAt);
            Assert.Equal(2, second.Winner.Rating.Revision);
            Assert.Equal(1, currentWinner.Revision);
        }

        private sealed class FixedTimeProvider(DateTime now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(now);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FirstMatchInitializesRatingAndPreservesPreMatchInputs(bool playerWon)
        {
            // Arrange.
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var winner = playerWon ? player : opponent;
            var loser = playerWon ? opponent : player;
            var wonMatch = ItemFactory.CreateMatch(matchId, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var repository = new Mock<IDynamoDbRepository>(MockBehavior.Strict);
            var reader = repository.As<IDynamoDbConsistentReader>();
            reader.Setup(repo => repo.GetItemsConsistentlyAsync<PlayerRatingItem>(It.IsAny<Guid>(), "RATING#Backgammon#SevenPointGame"))
                .ReturnsAsync(Array.Empty<PlayerRatingItem>());

            // Act.
            var (rating, period) = await repository.Object.CalculatePlayerRatingAsync(player.Id, wonMatch, lostMatch);
            var factory = ItemFactoryCreator.Create<RatingPeriodItem>();
            var persistedPeriod = factory.CreateItem(factory.CreateItem(period));

            // Assert.
            Assert.Equal(player.Id, rating.PlayerId);
            Assert.Equal(MatchModus.Ranked, rating.Modus);
            Assert.Equal(1, rating.MatchesPlayed);
            Assert.Equal(1, rating.Revision);
            Assert.Equal(playerWon, rating.Rating > 1200);
            Assert.Equal(Math.Max(1200, rating.Rating), rating.HighestRating);
            Assert.Equal(Math.Min(1200, rating.Rating), rating.LowestRating);
            Assert.Equal(matchId, persistedPeriod.MatchId);
            Assert.Equal(player.Id, persistedPeriod.PlayerId);
            Assert.Equal(opponent.Id, persistedPeriod.OpponentId);
            Assert.Equal(1200, persistedPeriod.PlayerRating);
            Assert.Equal(1200, persistedPeriod.OpponentRating);
            Assert.Equal(350, persistedPeriod.PlayerRatingDeviation);
            Assert.Equal(350, persistedPeriod.OpponentRatingDeviation);
            Assert.Equal(0.06, persistedPeriod.PlayerSigma);
            Assert.Equal(0.06, persistedPeriod.OpponentSigma);
            Assert.InRange(persistedPeriod.MatchScore, playerWon ? 0.75 : 0.0, playerWon ? 1.0 : 0.25);
            Assert.Equal(period.CreatedAt, persistedPeriod.CreatedAt);
            Assert.Equal(period.SK, persistedPeriod.SK);
            reader.Verify(repo => repo.GetItemsConsistentlyAsync<PlayerRatingItem>(It.IsAny<Guid>(), It.IsAny<string>()), Times.Exactly(2));
            repository.Verify(repo => repo.SaveAsync(It.IsAny<PlayerRatingItem>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExistingRatingAdvancesRevisionAndPreservesHistoricalExtrema(bool playerWon)
        {
            // Arrange.
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, playerWon ? player : opponent, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, playerWon ? opponent : player, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var previous = PlayerRatingItemFactory.CreateInitial(player.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            previous.Rating = 1400;
            previous.RatingDeviation = 75;
            previous.Sigma = 0.04;
            previous.Revision = 12;
            previous.MatchesPlayed = 12;
            previous.HighestRating = 1800;
            previous.LowestRating = 1000;
            var repository = new Mock<IDynamoDbRepository>(MockBehavior.Strict);
            var reader = repository.As<IDynamoDbConsistentReader>();
            reader.Setup(repo => repo.GetItemsConsistentlyAsync<PlayerRatingItem>(player.Id, previous.SK)).ReturnsAsync([previous]);
            reader.Setup(repo => repo.GetItemsConsistentlyAsync<PlayerRatingItem>(opponent.Id, previous.SK))
                .ReturnsAsync([PlayerRatingItemFactory.CreateInitial(opponent.Id, MatchVariant.Backgammon, MatchType.SevenPointGame)]);

            // Act.
            var (rating, period) = await repository.Object.CalculatePlayerRatingAsync(player.Id, wonMatch, lostMatch);

            // Assert.
            Assert.Equal(13, rating.Revision);
            Assert.Equal(13, rating.MatchesPlayed);
            Assert.Equal(1800, rating.HighestRating);
            Assert.Equal(1000, rating.LowestRating);
            Assert.Equal(1400, period.PlayerRating);
            Assert.Equal(75, period.PlayerRatingDeviation);
            Assert.Equal(0.04, period.PlayerSigma);
            Assert.Equal(playerWon, rating.Rating > period.PlayerRating);
        }

        [Fact]
        public async Task CalculatePlayerRatingUsesOnlyCurrentMatchPeriod()
        {
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, opponent, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            
            var playerRating = PlayerRatingItemFactory.CreateInitial( player.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            var opponentRating = PlayerRatingItemFactory.CreateInitial( opponent.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            opponentRating.Rating = 1350;
            
            var ratingSk = playerRating.SK;
            var repository = new Mock<IDynamoDbRepository>();
            var consistentReader = repository.As<IDynamoDbConsistentReader>();

            consistentReader
                .Setup(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(player.Id, ratingSk))
                .ReturnsAsync([playerRating]);
            
            consistentReader
                .Setup(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(opponent.Id, ratingSk))
                .ReturnsAsync([opponentRating]);

            var (updatedRating, period) = await repository.Object.CalculatePlayerRatingAsync( player.Id, wonMatch, lostMatch);

            Assert.Equal(1, updatedRating.MatchesPlayed);
            Assert.Equal(1, updatedRating.Revision);
            Assert.True(updatedRating.Rating > 1200);
            Assert.Equal(1200, period.PlayerRating);
            Assert.Equal(1350, period.OpponentRating);
            Assert.Equal(matchId, period.MatchId);
            consistentReader.Verify(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(It.IsAny<Guid>(), ratingSk), Times.Exactly(2));
            repository.Verify(value => value.GetItemsAsync<PlayerRatingItem>(It.IsAny<Guid>(), ratingSk), Times.Never);
            repository.Verify(value => value.GetItemsAsync<RatingPeriodItem>(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
    }
}
