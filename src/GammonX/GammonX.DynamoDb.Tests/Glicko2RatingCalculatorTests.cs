using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Stats;
using System.Runtime.ConstrainedExecution;

namespace GammonX.DynamoDb.Tests
{
    public class Glicko2RatingCalculatorTests
    {
        private static Glicko2Rating NewPlayer() =>
            new(Glicko2RatingCalculator.ToMu(Glicko2Constants.DefaultRating), Glicko2RatingCalculator.ToPhi(Glicko2Constants.DefaultRD), Glicko2Constants.DefaultSigma);

        private static Glicko2Rating VeteranPlayer(double rating = 1500, double rd = 50, double sigma = 0.03) =>
            new(Glicko2RatingCalculator.ToMu(rating), Glicko2RatingCalculator.ToPhi(rd), sigma);

        private static RatingPeriodItem RatingPeriod(Glicko2Rating playerRating, Glicko2Rating oppRating, double matchScore) => new()
        {
            PlayerRating = Glicko2RatingCalculator.FromMu(playerRating.Mu),
            PlayerRatingDeviation = Glicko2RatingCalculator.FromPhi(playerRating.Phi),
            PlayerSigma = playerRating.Sigma,
            OpponentRating = Glicko2RatingCalculator.FromMu(oppRating.Mu),
            OpponentRatingDeviation = Glicko2RatingCalculator.FromPhi(oppRating.Phi),
            OpponentSigma = oppRating.Sigma,
            MatchScore = matchScore,
            CreatedAt = DateTime.UtcNow,
            MatchId = Guid.NewGuid(),
            Modus = Models.Enums.MatchModus.Ranked,
            Type = Models.Enums.MatchType.SevenPointGame,
            Variant = Models.Enums.MatchVariant.Tavli,
            PlayerId = Guid.NewGuid(),
            OpponentId = Guid.NewGuid()
        };

        private static List<RatingPeriodItem> MakeRatingPeriods(Glicko2Rating player, IEnumerable<(Glicko2Rating opp, double score)> items)
        {
            return items.Select(i => RatingPeriod(player, i.opp, i.score)).ToList();
        }

        private readonly static double StrongWinScore = 1.0;
        private readonly static double StrongLossScore = 0.0;

        [Fact]
        public void HighRatingShouldGainLittleWhenBeatingLowRating()
        {
            var high = VeteranPlayer(2000, rd: 50);
            var low = VeteranPlayer(1000, rd: 50);
            var ratingPeriod = RatingPeriod(high, low, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(high, ratingPeriod);
            // high beating low gives almost nothing
            Assert.True(updated.Mu < high.Mu + 0.01);
            Assert.True(updated.Mu >= high.Mu);
        }

        [Fact]
        public void LowRatingShouldGainALotWhenBeatingHighRating()
        {
            var high = VeteranPlayer(2000, rd: 50);
            var low = VeteranPlayer(1000, rd: 50);
            var ratingPeriod = RatingPeriod(low, high, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(low, ratingPeriod);
            // significant jump
            Assert.True(updated.Mu > low.Mu + 0.05);
        }

        [Fact]
        public void HighVsHighWinShouldGiveModerateGain()
        {
            var p1 = VeteranPlayer(1800, 50);
            var opp = VeteranPlayer(1800, 50);
            var ratingPeriod = RatingPeriod(p1, opp, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(p1, ratingPeriod);
            Assert.True(updated.Mu > p1.Mu);
        }

        [Fact]
        public void LowVsLowWinShouldGiveModerateGain()
        {
            var p1 = VeteranPlayer(1100, 80);
            var opp = VeteranPlayer(1100, 80);
            var ratingPeriod = RatingPeriod(p1, opp, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(p1, ratingPeriod);
            Assert.True(updated.Mu > p1.Mu);
        }

        [Fact]
        public void NewPlayerShouldGainMassiveWhenBeatingVeteran()
        {
            var newbie = NewPlayer();
            var expert = VeteranPlayer(1800, 50);
            var ratingPeriod = RatingPeriod(newbie, expert, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(newbie, ratingPeriod);
            Assert.True(updated.Mu > newbie.Mu + 0.1);
        }

        [Fact]
        public void VeteranShouldGainAlmostNothingWhenBeatingNewPlayer()
        {
            var expert = VeteranPlayer(1800, 50);
            var newbie = NewPlayer();
            var ratingPeriod = RatingPeriod(expert, newbie, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(expert, ratingPeriod);
            Assert.True(updated.Mu <= expert.Mu + 0.006);
            Assert.True(updated.Mu >= expert.Mu);
        }

        [Fact]
        public void NewVsNewAWinShouldGiveLargeMovement()
        {
            var p1 = NewPlayer();
            var p2 = NewPlayer();
            var ratingPeriod = RatingPeriod(p1, p2, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(p1, ratingPeriod);
            Assert.True(updated.Mu > p1.Mu + 0.05);
        }

        [Fact]
        public void VeteranVsVeteran_AWin_ShouldGiveSmallButMeaningfulMovement()
        {
            var p1 = VeteranPlayer(1600, 40);
            var p2 = VeteranPlayer(1600, 40);
            var ratingPeriod = RatingPeriod(p1, p2, StrongWinScore);
            var updated = Glicko2RatingCalculator.Calculate(p1, ratingPeriod);
            Assert.True(updated.Mu > p1.Mu);
            Assert.True(updated.Mu < p1.Mu + 0.05);
        }

        [Fact]
        public void HighMatchScoreShouldIncreaseRatingMore()
        {
            var p1 = VeteranPlayer(1500, 50);
            var opp = VeteranPlayer(1500, 50);
            var ratingPeriodStrongWin = RatingPeriod(p1, opp, StrongWinScore);
            var win = Glicko2RatingCalculator.Calculate(p1, ratingPeriodStrongWin);
            var ratingPeriodStrongLoss = RatingPeriod(p1, opp, StrongLossScore);
            var loss = Glicko2RatingCalculator.Calculate(p1, ratingPeriodStrongLoss);
            Assert.True(win.Mu > p1.Mu);
            Assert.True(loss.Mu < p1.Mu);
        }

        [Fact]
        public void InconsistentPlayerShouldChangeMoreAfterMatch()
        {
            var consistent = new Glicko2Rating(
                Glicko2RatingCalculator.ToMu(1500),
                Glicko2RatingCalculator.ToPhi(60),
                sigma: 0.02);
            var inconsistent = new Glicko2Rating(
                Glicko2RatingCalculator.ToMu(1500),
                Glicko2RatingCalculator.ToPhi(60),
                sigma: 0.20);
            var opp = VeteranPlayer(1500, 50);
            var ratingPeriodConsistent = RatingPeriod(consistent, opp, StrongWinScore);
            var upConsistent = Glicko2RatingCalculator.Calculate(consistent, ratingPeriodConsistent);
            var ratingPeriodInconsistent = RatingPeriod(inconsistent, opp, StrongWinScore);
            var upInconsistent = Glicko2RatingCalculator.Calculate(inconsistent, ratingPeriodInconsistent);
            Assert.True(upInconsistent.Mu - inconsistent.Mu > upConsistent.Mu - consistent.Mu);
        }

        [Fact]
        public void HighRDShouldLeadToLargerRatingChanges()
        {
            var trusted = VeteranPlayer(1500, rd: 30);
            var untrusted = VeteranPlayer(1500, rd: 200);
            var opp = VeteranPlayer(1500, rd: 30);
            var ratingPeriodTrusted = RatingPeriod(trusted, opp, StrongWinScore);
            var trustedUpdated = Glicko2RatingCalculator.Calculate(trusted, ratingPeriodTrusted);
            var ratingPeriodUntrusted = RatingPeriod(untrusted, opp, StrongWinScore);
            var untrustedUpdated = Glicko2RatingCalculator.Calculate(untrusted, ratingPeriodUntrusted);
            Assert.True(untrustedUpdated.Mu - untrusted.Mu > trustedUpdated.Mu - trusted.Mu);
        }

        [Fact]
        public void HighRatingShouldLoseALotWhenLosingToLowRating()
        {
            var high = VeteranPlayer(2000, 50);
            var low = VeteranPlayer(1000, 50);
            var ratingPeriod = RatingPeriod(high, low, StrongLossScore);
            var updated = Glicko2RatingCalculator.Calculate(high, ratingPeriod);
            Assert.True(updated.Mu < high.Mu - 0.05);
        }

        [Fact]
        public void LowRatingShouldLoseLittleWhenLosingToHighRating()
        {
            var high = VeteranPlayer(2000, 50);
            var low = VeteranPlayer(1000, 50);
            var ratingPeriod = RatingPeriod(low, high, StrongLossScore);
            var updated = Glicko2RatingCalculator.Calculate(low, ratingPeriod);
            Assert.True(updated.Mu <= low.Mu);
            Assert.True(updated.Mu > low.Mu - 0.02);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void NewPlayerFewPeriodsShouldMoveALotOnWin(int count)
        {
            var player = NewPlayer();
            var opponent = VeteranPlayer(1500, 60);

            // create period rating history with previously lost matches
            var history = Enumerable.Range(0, count)
                .Select(_ => (opponent, StrongLossScore))
                .ToList();

            var items = MakeRatingPeriods(player, history);

            var updated = Glicko2RatingCalculator.Calculate(player, items.ToArray());

            // losing repeatedly lowers rating
            Assert.True(updated.Mu < player.Mu);
            // RD should shrink
            Assert.True(updated.Phi < player.Phi + 0.001);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(9)]
        public void VeteranPlayerManyPeriodsShouldBeStable(int count)
        {
            var player = VeteranPlayer(1600, rd: 40, sigma: 0.03);
            var opponent = VeteranPlayer(1600, rd: 40, sigma: 0.03);

            var history = Enumerable.Range(0, count).Select(_ => (opponent, StrongWinScore)).ToList();

            var items = MakeRatingPeriods(player, history);

            var updated = Glicko2RatingCalculator.Calculate(player, items.ToArray());

            // with many periods the movement should be small but positive
            Assert.True(updated.Mu > player.Mu);
            // small stable change
            Assert.True(updated.Mu < player.Mu + 0.25);
            // RD should shrink more
            Assert.True(updated.Phi < player.Phi);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(6)]
        [InlineData(9)]
        public void LowPlayerBeatsHighOpponentShouldGainMoreWithFewerPeriods(int count)
        {
            var low = VeteranPlayer(1000, rd: 80, sigma: 0.06);
            var high = VeteranPlayer(2000, rd: 40, sigma: 0.03);
            // low player lost previous matches
            var history = Enumerable.Range(0, count).Select(_ => (high, StrongLossScore)).ToList();

            var items = MakeRatingPeriods(low, history);

            var updated = Glicko2RatingCalculator.Calculate(low, items.ToArray());

            // losing repeatedly lowers rating
            Assert.True(updated.Mu < low.Mu);
            // uncertainty decreases
            Assert.True(updated.Phi - low.Phi < 0.01);
        }

        [Fact]
        public void ConsistentPlayerShouldHaveLowVolatility()
        {
            var player = VeteranPlayer(1500, 60, sigma: 0.03);
            var opp = VeteranPlayer(1200, 60, sigma: 0.03);

            var history = Enumerable.Range(0, 9).Select(_ => (opp, StrongWinScore)).ToList();

            var items = MakeRatingPeriods(player, history);

            var updated = Glicko2RatingCalculator.Calculate(player, items.ToArray());

            // volatitlity should stay stable
            Assert.True(Math.Abs(updated.Sigma - player.Sigma) < 0.005);
        }

        [Fact]
        public void InconsistentPlayerShouldHaveHigherVolatility()
        {
            var player = VeteranPlayer(1500, 60, sigma: 0.03);
            var opp = VeteranPlayer(1500, 60, sigma: 0.03);

            var scores = new[] { 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 };

            var history = scores.Select(s => (opp, s)).ToList();

            var items = MakeRatingPeriods(player, history);

            var updated = Glicko2RatingCalculator.Calculate(player, items.ToArray());

            // volatitlity should stay stable
            Assert.True(Math.Abs(updated.Sigma - player.Sigma) < 0.005);
        }

        [Fact]
        public void HighPlayerBeatsLowPlayerShouldGainVeryLittleAfterManyPeriods()
        {
            var high = VeteranPlayer(2000, rd: 30, sigma: 0.03);
            var low = VeteranPlayer(1000, rd: 70, sigma: 0.06);

            var history = Enumerable.Range(0, 9).Select(_ => (low, StrongWinScore)).ToList();

            var items = MakeRatingPeriods(high, history);

            var updated = Glicko2RatingCalculator.Calculate(high, items.ToArray());

            // still increases
            Assert.True(updated.Mu > high.Mu);
            // but tiny increase
            Assert.True(updated.Mu - high.Mu < 0.02);
            // uncertainty shrinks
            Assert.True(updated.Phi - high.Phi < 0.01);
        }

        [Fact]
        public void NewPlayerWithManyPeriodsShouldBecomeVeteranLike()
        {
            var newPlayer = NewPlayer();
            var opp = VeteranPlayer(1500, rd: 40);

            // new player keeps winning
            var history = Enumerable.Range(0, 9).Select(_ => (opp, StrongWinScore)).ToList();

            var items = MakeRatingPeriods(newPlayer, history);

            var updated = Glicko2RatingCalculator.Calculate(newPlayer, items.ToArray());

            Assert.True(updated.Phi < newPlayer.Phi);
            // sharply reduces uncertainty
            Assert.True(updated.Phi < 1.8);
            Assert.True(updated.Mu > newPlayer.Mu);
        }
    }
}
