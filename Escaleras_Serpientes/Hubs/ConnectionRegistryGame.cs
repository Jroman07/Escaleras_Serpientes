using System.Collections.Concurrent;

namespace Escaleras_Serpientes.Hubs
{
    public sealed class ConnectionRegistryGame
    {
        public sealed record ConnectionInfo(
            string ConnectionId,
            string RoomId,
            string PlayerName,
            Guid? UserId,
            DateTime ConnectedAtUtc
        );

        // connId -> info
        private readonly ConcurrentDictionary<string, ConnectionInfo> _byConn =
            new(StringComparer.Ordinal);

        // roomId -> set(connId)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _roomIndex =
            new(StringComparer.Ordinal);

        public bool Set(string connId, string roomId, string playerName, Guid? userId = null)
        {
            var info = new ConnectionInfo(connId, roomId, playerName, userId, DateTime.UtcNow);
            _byConn[connId] = info;

            var set = _roomIndex.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
            set[connId] = 0;

            return true;
        }

        public bool TryGet(string connId, out ConnectionInfo? info)
            => _byConn.TryGetValue(connId, out info);

        public bool Remove(string connId, out ConnectionInfo? removed)
        {
            if (_byConn.TryRemove(connId, out removed))
            {
                if (_roomIndex.TryGetValue(removed.RoomId, out var set))
                {
                    set.TryRemove(connId, out _);
                    if (set.IsEmpty)
                        _roomIndex.TryRemove(removed.RoomId, out _);
                }
                return true;
            }
            removed = null;
            return false;
        }

        public bool Move(string connId, string newRoomId)
        {
            if (!_byConn.TryGetValue(connId, out var info)) return false;

            // quitar de sala vieja
            if (_roomIndex.TryGetValue(info.RoomId, out var oldSet))
            {
                oldSet.TryRemove(connId, out _);
                if (oldSet.IsEmpty)
                    _roomIndex.TryRemove(info.RoomId, out _);
            }

            // agregar a sala nueva
            var newSet = _roomIndex.GetOrAdd(newRoomId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
            newSet[connId] = 0;

            // actualizar info
            var updated = info with { RoomId = newRoomId };
            _byConn[connId] = updated;

            return true;
        }

        public IReadOnlyList<string> GetPlayersInRoom(string roomId)
        {
            if (!_roomIndex.TryGetValue(roomId, out var set)) return Array.Empty<string>();
            var result = new List<string>(set.Count);
            foreach (var kv in set.Keys)
            {
                if (_byConn.TryGetValue(kv, out var info))
                    result.Add(info.PlayerName);
            }
            return result;
        }

        public IReadOnlyCollection<string> GetConnectionsInRoom(string roomId)
        {
            if (!_roomIndex.TryGetValue(roomId, out var set)) return Array.Empty<string>();
            return set.Keys.ToArray();
        }

        public bool IsInRoom(string connId, string roomId)
            => _byConn.TryGetValue(connId, out var info) && string.Equals(info.RoomId, roomId, StringComparison.Ordinal);

        public string? GetRoomOf(string connId)
            => _byConn.TryGetValue(connId, out var info) ? info.RoomId : null;
    }
}
