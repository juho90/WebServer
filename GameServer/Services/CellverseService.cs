using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Services
{
    public class CellverseService(string roomId)
    {
        private readonly string roomId = roomId;
        private readonly List<WebSocket> webSocketGroup = WebSocketBroadcastManager.GetGroup(roomId);
        private readonly ConcurrentDictionary<int, Cell> cells = new();
        private readonly ConcurrentDictionary<string, int> cellByUid = new();
        private int id = 0;

        public IEnumerable<Cell> GetCells()
        {
            return cells.Values;
        }

        public bool TryGetCellByUid(string uid, out int id)
        {
            return cellByUid.TryGetValue(uid, out id);
        }

        public Cell SpawnCell(string uid)
        {
            if (cellByUid.ContainsKey(uid))
            {
                throw new Exception($"Cell already exists for user: {uid}");
            }
            var newId = Interlocked.Increment(ref id);
            cellByUid[uid] = newId;
            var rand = new Random();
            var x = rand.Next(400);
            var y = rand.Next(400);
            var cell = new Cell
            {
                Id = newId,
                X = x,
                Y = y,
            };
            return cells.TryAdd(newId, cell)
                ? cell
                : throw new Exception($"Cell already exists: {newId}");
        }

        public void Broadcast(byte[] data)
        {
            WebSocketBroadcastManager.BroadcastGroup(webSocketGroup, data);
        }

        public class Cell
        {
            public int Id { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public int Size { get; set; }
        }
    }
}
