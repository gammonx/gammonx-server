using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	// <inheritdoc />
	public class SimplePlakotoBotService : IBotService
	{
		private readonly IBoardService _boardService = BoardServiceFactory.Create(GameModus.Plakoto);

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
			throw new InvalidOperationException("plakoto does not support doubling");
		}

		// <inheritdoc />
		public Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
		{
			throw new InvalidOperationException("plakoto does not support doubling");
		}

		private MoveSequenceModel FindBestMoveSequence(IBoardModel model, List<MoveSequenceModel> sequences, bool isWhite)
		{
			var bestSequence = new MoveSequenceModel();
			int bestScore = 0;

			foreach (var seq in sequences)
			{
				int score = EvaluatePlakotoMove(model, seq, isWhite);
				if (score > bestScore)
				{
					bestScore = score;
					bestSequence = seq;
				}
			}

			return bestSequence;
		}

		private int EvaluatePlakotoMove(IBoardModel model, MoveSequenceModel moveSequence, bool isWhite)
		{
			int score = 0;
			var shadowBboard = model.DeepClone();
			foreach (var move in moveSequence.Moves)
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
					// check the target field
					int target = shadowBboard.Fields[to];

					if (isWhite)
					{
						if (target == 1) // enemy single checker to pin
							score += 50;
						else if (target > 1) // blocked by enemy
							score -= 20;
						else if (target <= -2) // own chain
							score += 10;
						else if (target == -1) // own single checker
							score += 15;
						else if (target == 0) // empty field
							score -= 5;
						else
							score += 0; // should not happen
					}
					else
					{
						if (target == -1) // enemy single checker to pin
							score += 50;
						else if (target < -1) // blocked by enemy
							score -= 20;
						else if (target >= 2) // own chain
							score += 10;
						else if (target == 1) // own single checker
							score += 15;
						else if (target == 0) // empty field
							score -= 5;
						else
							score += 0; // should not happen
					}
				}

				_boardService.MoveCheckerTo(shadowBboard, from, to, isWhite);
			}
			// longer move sequences are better
			score += moveSequence.Moves.Count * 5;
			return score;
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
