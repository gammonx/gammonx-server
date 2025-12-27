using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Tests
{
    public class MoveSequencesTests
    {
        private static MoveModel M(int from, int to) => new(from, to);

        private static MoveSequenceModel Seq(params MoveModel[] moves)
        {
            var s = new MoveSequenceModel();
            s.Moves.AddRange(moves);
            return s;
        }

        [Fact]
        public void SingleMoveIsPreferredOverCombinedMove()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 23),     // die 5
                    M(23, -100)    // die 6
                ),
                Seq(
                    M(18, -100)   // die 11 (bear-off)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, -100), played[0]);
        }

        [Fact]
        public void CombinedMoveLength2IsResolvedCorrectly()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
            Assert.Equal(M(18, 13), played[0]);
            Assert.Equal(M(13, 8), played[1]);
        }

        [Fact]
        public void PaschPrefixOfTwoMovesIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
        }

        [Fact]
        public void PaschPrefixOfThreeMovesIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, 3, out var played);

            Assert.True(result);
            Assert.Equal(3, played.Count);
        }

        [Fact]
        public void PaschFullPrefixIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Equal(4, played.Count);
        }

        [Fact]
        public void NonContiguousChainIsRejected()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(12, 8),
                    M(8, 3)
                )
            };

            var result = sequences.TryUseMove(18, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void SuffixChainIsRejected()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3)
                )
            };

            var result = sequences.TryUseMove(13, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void MatchingLaterToIndexDoesNotOverridePrefixRule()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 13) // same TO as first move
                )
            };

            var result = sequences.TryUseMove(18, 13, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, 13), played[0]);
        }

        [Fact]
        public void BearOffAmbiguityExactMatchWins()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 23),
                    M(23, -100)
                ),
                Seq(
                    M(18, -100)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, -100), played[0]);
        }

        [Fact]
        public void CombinedMoveMustStartAtFirstMove()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                )
            };

            // illegal combined move, skipping the first move
            var result = sequences.TryUseMove(13, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void CorrectSequenceIsSelectedWhenMultipleExist()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                ),
                Seq(
                    M(18, 12),
                    M(12, 6)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
            Assert.Equal(M(18, 13), played[0]);
        }
    }
}
