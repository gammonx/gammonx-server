using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	// <inheritdoc />
	public class SimpleFevgaBotService : IBotService
	{
		private readonly IBoardService _boardService = BoardServiceFactory.Create(GameModus.Fevga);

		// <inheritdoc />
		public Task<MoveSequenceModel> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
		{
			try
			{
				var gameSession = matchSession.GetGameSession(matchSession.GameRound);
				if (gameSession == null)
					throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

				var isWhite = IsWhite(matchSession, playerId);
				var diceRolls = gameSession.DiceRolls.GetRemainingRolls();

				var moveSequences = _boardService.GetLegalMoveSequences(gameSession.BoardModel, isWhite, diceRolls);
				var bestMoveSequence = FindBestMoveSequence(gameSession.BoardModel, moveSequences.ToList(), isWhite);
				return Task.FromResult(bestMoveSequence);
			}
			catch (Exception)
			{
				// debugging purposes
				throw;
			}
		}

		// <inheritdoc />
		public Task<bool> ShouldAcceptDouble(IMatchSessionModel matchSession, Guid playerId)
		{
			throw new InvalidOperationException("fevga does not support doubling");
		}

		// <inheritdoc />
		public Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
		{
			throw new InvalidOperationException("fevga does not support doubling");
		}

		private MoveSequenceModel FindBestMoveSequence(IBoardModel model, List<MoveSequenceModel> sequences, bool isWhite)
		{
			MoveSequenceModel bestSequence = new();
			int bestScore = int.MinValue;

			foreach (var seq in sequences)
			{
				int score = EvaluateFevgaMove(model, seq, isWhite);
				if (score > bestScore)
				{
					bestScore = score;
					bestSequence = seq;
				}
			}

			return bestSequence;
		}

		private int EvaluateFevgaMove(IBoardModel model, MoveSequenceModel sequence, bool isWhite)
		{
			int score = 0;
			var shadowBboard = model.DeepClone();
			foreach (var move in sequence.Moves)
			{
				int from = move.From;
				int to = move.To;
				// the further forward the better
				score += isWhite ? to : (23 - to);
				if (to == WellKnownBoardPositions.BearOffWhite || to == WellKnownBoardPositions.BearOffBlack)
				{
					// bearing off is very good
					score += 100;
				}
				else
				{
					int target = shadowBboard.Fields[to];
					if (isWhite)
					{
						if (target < 0) // own checkers there
						{
							score += 5;
						}
						else if (target == 0) // opponent there
						{
							score += 25;
						}
					}
					else
					{
						if (target > 0) // own checkers there
						{
							score += 5;
						}
						else if (target == 0) // opponent there
						{
							score += 25;
						}
					}
				}
				_boardService.MoveCheckerTo(shadowBboard, from, to, isWhite);
			}
			// bonus for using more moves
			score += sequence.Moves.Count * 5;
			score += CheckForPrime(shadowBboard, isWhite) * 100;
			return score;
		}

		private static int CheckForPrime(IBoardModel model, bool isWhite)
		{
			var longest = 0;
			var current = 0;
			var fields = model.Fields;

			for (int i = 0; i < fields.Length; i++)
			{
				if ((isWhite && fields[i] < 0) || (!isWhite && fields[i] > 0))
				{
					current++;
					longest = Math.Max(longest, current);
				}
				else
				{
					current = 0;
				}
			}

			// huge bonus if there's a prime of at least 6 points
			return longest >= 6 ? 1 : 0;
		}

		private static bool IsWhite(IMatchSessionModel matchSession, Guid playerId)
		{
			if (matchSession.Player1.Id.Equals(playerId))
			{
				return true;
			}
			else if (matchSession.Player2.Id.Equals(playerId))
			{
				return false;
			}
			throw new InvalidOperationException("Player is not part of this match session.");
		}
	}
}
