using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
    /// <summary>
	/// Single player connection session repository that manages all client player connections within the match hub.
	/// </summary>
    public sealed class PlayerConnectionRepository
    {
        private readonly ConcurrentDictionary<Guid, Lazy<PlayerConnection>> _connections = new();

        public PlayerConnection Create(Guid playerId)
        {
            var lazy = new Lazy<PlayerConnection>(() => new PlayerConnection(playerId));

            if (_connections.TryAdd(playerId, lazy))
            {
                return lazy.Value;
            }
            throw new InvalidOperationException("player connection already exists");

        }

        public PlayerConnection? Get(Guid playerId)
        {
            if (_connections.TryGetValue(playerId, out var lazy))
            {
                return lazy.Value;
            }
            else
            {
                return null;
            }
        }

        public PlayerConnection GetOrCreate(Guid playerId)
        {
            var lazy = _connections.GetOrAdd(
                playerId,
                _ => new Lazy<PlayerConnection>(
                    () => new PlayerConnection(playerId),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }

        public void Remove(Guid playerId)
        {
            if (!_connections.TryRemove(playerId, out var _))
            {
                throw new KeyNotFoundException($"No player connection found for playerId: '{playerId}'");
            }
        }
    }
}
