using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal class RankedMatchmakingService : MatchmakingServiceBaseImpl
	{
		private readonly IServiceScopeFactory _scopeFactory;

		public RankedMatchmakingService(IServiceScopeFactory scopeFactory)
		{
			_scopeFactory = scopeFactory;
		}

		// <inheritdoc />
		public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			if (queueKey.MatchModus != WellKnownMatchModus.Ranked)
			{
				throw new InvalidOperationException("Match modus must be of type normal in order to join this queue");
			}

			if (_queue.Any(ml => ml.Value.PlayerId == playerId || ml.Value.PlayerId == playerId))
			{
				throw new InvalidOperationException("Already part of the match lobby queue");
			}

			// TODO: get rating from AWS lambda function
			var relevantRating = 1200;
			var newEntry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, relevantRating);
			var queue = _modeQueues.GetOrAdd(queueKey, _ => new ConcurrentQueue<Guid>());
			_queue[newEntry.Id] = newEntry;
			queue.Enqueue(newEntry.Id);
			return Task.FromResult(newEntry);
		}

		// <inheritdoc />
		public override Task MatchQueuedPlayersAsync()
		{
			foreach (var (queueKey, queue) in _modeQueues)
			{
				if (queue.Count < 2)
					continue;

				var snapshot = queue.ToArray();
				var matched = new HashSet<Guid>();

				for (int entry1Index = 0; entry1Index < snapshot.Length; entry1Index++)
				{
					if (!_queue.TryGetValue(snapshot[entry1Index], out var entryA) || matched.Contains(entryA.PlayerId))
						continue;

					for (int entry2Index = entry1Index + 1; entry2Index < snapshot.Length; entry2Index++)
					{
						if (!_queue.TryGetValue(snapshot[entry2Index], out var entryB) || matched.Contains(entryB.PlayerId))
							continue;

						// try to find a fair match within a ok time frame
						var diff = Math.Abs(entryA.CurrentRating - entryB.CurrentRating);
						var allowed = Math.Min(GetAllowedRatingDifference(entryA), GetAllowedRatingDifference(entryB));
						if (diff <= allowed)
						{
							// match found
							matched.Add(entryA.Id);
							matched.Add(entryB.Id);
							// remove players from queue
							_queue.TryRemove(entryA.PlayerId, out _);
							_queue.TryRemove(entryB.PlayerId, out _);
							// create new lobby
							var lobbyEntryA = new LobbyEntry(entryA.PlayerId);
							var lobbyEntryB = new LobbyEntry(entryB.PlayerId);
							var lobby = new MatchLobby(Guid.NewGuid(), queueKey, lobbyEntryA);
							lobby.Join(lobbyEntryB);
							_matchLobbies[entryA] = lobby;
							_matchLobbies[entryB] = lobby;
						}
					}
				}
				// remove paired players from queue
				var remaining = new ConcurrentQueue<Guid>(snapshot.Where(id => !matched.Contains(id)));
				_modeQueues[queueKey] = remaining;
			}
			return Task.CompletedTask;
		}

		private static double GetAllowedRatingDifference(QueueEntry entry)
		{
			var seconds = (DateTime.UtcNow - entry.EnqueuedAt).TotalSeconds;

			if (seconds < 10) return 50;
			if (seconds < 30) return 100;
			if (seconds < 60) return 200;
			return 400;
		}
	}
}
