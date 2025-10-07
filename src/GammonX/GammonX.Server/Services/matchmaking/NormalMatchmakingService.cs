using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Simple matchmaking service that allows players to join a queue and find matches.
	/// </summary>
	internal class NormalMatchmakingService : MatchmakingServiceBaseImpl
	{
		// <inheritdoc />
		public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			if (queueKey.MatchModus != WellKnownMatchModus.Normal)
			{
				throw new InvalidOperationException("match modus must be of type normal in order to join this queue");
			}

			if (_queue.Any(ml => ml.Value.PlayerId == playerId || ml.Value.PlayerId == playerId))
			{
				throw new InvalidOperationException("Already part of the match lobby queue");
			}

			// add player to queue
			var entry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 0);
			_queue[entry.Id] = entry;
			// add player to mode specific queue
			var modeQueue = _modeQueues.GetOrAdd(queueKey, _ => new ConcurrentQueue<Guid>());
			modeQueue.Enqueue(entry.Id);
			// return queue entry;
			return Task.FromResult(entry);
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
				// remove paired players from queue
				var remaining = new ConcurrentQueue<Guid>(snapshot.Where(id => !matched.Contains(id)));
				_modeQueues[queueKey] = remaining;
			}
			return Task.CompletedTask;
		}
	}
}
