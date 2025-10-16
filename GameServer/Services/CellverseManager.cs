using System.Collections.Concurrent;

namespace GameServer.Services
{
    public static class CellverseManager
    {
        private static readonly ConcurrentDictionary<string, CellverseService> cellverses = new();

        public static CellverseService GetOrAdd(string roomId)
        {
            return cellverses.GetOrAdd(roomId, _ => new CellverseService(roomId));
        }

        public static void Remove(string roomId)
        {
            cellverses.TryRemove(roomId, out _);
        }
    }
}
