using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Simple matchmaking service that allows players to join a queue and find matches.
	/// </summary>
	public class SimpleMatchmakingService
	{
		// TODO :: encapsulate this into an interface

		// simple storage for demo: maps queueId -> entry
		private readonly ConcurrentDictionary<Guid, QueueEntry> _queue = new();
		// quick lookup of waiting players per mode
		private readonly ConcurrentDictionary<WellKnownMatchVariant, ConcurrentQueue<Guid>> _modeQueues = new();

		private readonly ConcurrentDictionary<Guid, MatchLobby> _matchLobbies = new ConcurrentDictionary<Guid, MatchLobby>();

		/// <summary>
		/// Tries to find a match lobby by its ID. Returns null if not found.
		/// </summary>
		/// <param name="matchId">Match id to search for.</param>
		/// <param name="matchLobby">Result.</param>
		/// <returns>Boolean indicating if a match lobby was found.</returns>
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

		/// <summary>
		/// Join the given <paramref name="newPlayer"/> to the matchmaking queue.
		/// </summary>
		/// <param name="newPlayer">Player to join the queue.</param>
		/// <returns>A guid of the created match lobby.</returns>
		public Guid JoinQueue(LobbyEntry newPlayer, WellKnownMatchVariant matchVariant)
		{
			if (_queue.Count > 0)
			{
				if (!_modeQueues.TryGetValue(matchVariant, out var matchVariantQueue))
					throw new InvalidOperationException("An error occurred while creating the match lobby");

				// TODO :: multiple join requests from same client
				if (matchVariantQueue.TryDequeue(out var opponentId))
				{
					if (opponentId == newPlayer.PlayerId)
					{
						matchVariantQueue.Enqueue(opponentId);
						throw new InvalidOperationException("Already part of the match lobby queue");
					}

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
				var entry = new QueueEntry(newPlayer, matchVariant, DateTime.UtcNow);
				_queue[newPlayer.PlayerId] = entry;

				var mq = _modeQueues.GetOrAdd(matchVariant, _ => new ConcurrentQueue<Guid>());
				mq.Enqueue(newPlayer.PlayerId);

				var matchId = Guid.NewGuid();
				var matchLobby = new MatchLobby(matchId, matchVariant, newPlayer);
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
