using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Simple matchmaking service that allows players to join a queue and find matches.
	/// </summary>
	public class SimpleMatchmakingService : IMatchmakingService
	{
		// simple storage for demo: maps queueId -> entry
		private readonly ConcurrentDictionary<Guid, QueueEntry> _queue = new();
		// quick lookup of waiting players per mode
		private readonly ConcurrentDictionary<QueueKey, ConcurrentQueue<Guid>> _modeQueues = new();

		private readonly ConcurrentDictionary<Guid, MatchLobby> _matchLobbies = new ConcurrentDictionary<Guid, MatchLobby>();

		// <inheritdoc />
		public bool TryFindMatchLobby(Guid matchId, out MatchLobby? matchLobby)
		{
			var result = _matchLobbies.GetValueOrDefault(matchId);
			if (result == null)
			{
				matchLobby = null;
				return false;
			}

			matchLobby = result;
			return true;
		}

		// <inheritdoc />
		public bool TryRemoveMatchLobby(Guid matchId)
		{
			if (_matchLobbies.TryRemove(matchId, out var _))
			{
				return true;
			}
			return false;
		}

		// <inheritdoc />
		public Guid JoinQueue(LobbyEntry newPlayer, QueueKey queueKey)
		{
			if (_matchLobbies.Any(ml => ml.Value.Player1.PlayerId == newPlayer.PlayerId || ml.Value.Player2?.PlayerId == newPlayer.PlayerId))
			{
				throw new InvalidOperationException("Already part of the match lobby queue");
			}

			if (_queue.Count > 0)
			{
				if (!_modeQueues.TryGetValue(queueKey, out var matchVariantQueue))
					throw new InvalidOperationException("An error occurred while creating the match lobby");

				if (matchVariantQueue.Contains(newPlayer.PlayerId))
				{
					throw new InvalidOperationException("Already part of the match lobby queue");
				}

				if (matchVariantQueue.TryDequeue(out var opponentId))
				{
					if (!_queue.TryRemove(opponentId, out var opponentQueueEntry))
						throw new InvalidOperationException("An error occurred while creating the match lobby");

					var matchLobby = GetMatchLobby(opponentQueueEntry.Player);
					matchLobby.Join(newPlayer);

					if (_matchLobbies.TryUpdate(matchLobby.MatchId, matchLobby, _matchLobbies[matchLobby.MatchId]))
					{
						return matchLobby.MatchId;
					}

					throw new InvalidOperationException("An error occurred while updating the match lobby");
				}

				throw new InvalidOperationException("An error occurred while creating the match lobby");
			}
			else
			{
				var entry = new QueueEntry(newPlayer, queueKey, DateTime.UtcNow);
				_queue[newPlayer.PlayerId] = entry;

				var mq = _modeQueues.GetOrAdd(queueKey, _ => new ConcurrentQueue<Guid>());
				mq.Enqueue(newPlayer.PlayerId);

				var matchId = Guid.NewGuid();
				var matchLobby = new MatchLobby(matchId, queueKey, newPlayer);
				if (_matchLobbies.TryAdd(matchLobby.MatchId, matchLobby))
				{
					return matchLobby.MatchId;
				}

				throw new InvalidOperationException("An error occurred while creating the match lobby");
			}
		}

		private MatchLobby GetMatchLobby(LobbyEntry opponent)
		{
			return _matchLobbies.FirstOrDefault(ml => ml.Value.Player1.PlayerId == opponent.PlayerId).Value;
		}

	}
}
