using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Services
{
    public class WebSocketBroadcastManager
    {
        private static readonly ConcurrentDictionary<string, List<WebSocket>> webSocketGroups = new();

        public static void AddSocket(string groupId, WebSocket socket)
        {
            var socketGroup = webSocketGroups.GetOrAdd(groupId, _ => []);
            lock (socketGroup)
            {
                socketGroup.Add(socket);
            }
        }

        public static void RemoveSocket(string groupId, WebSocket socket)
        {
            if (webSocketGroups.TryGetValue(groupId, out var sockets) is false)
            {
                return;
            }
            lock (sockets)
            {
                sockets.Remove(socket);
            }
        }

        public static void BroadcastAsync(string roomId, byte[] message, WebSocketMessageType type = WebSocketMessageType.Text)
        {
            if (webSocketGroups.TryGetValue(roomId, out var sockets) is false)
            {
                return;
            }
            lock (sockets)
            {
                List<WebSocket> closedSockets = [];
                foreach (var socket in sockets)
                {
                    if (socket.State is WebSocketState.Open)
                    {
                        socket.SendAsync(new ArraySegment<byte>(message), type, true, CancellationToken.None);
                    }
                    else
                    {
                        closedSockets.Add(socket);
                    }
                }
                foreach (var s in closedSockets)
                {
                    sockets.Remove(s);
                }
            }
        }
    }
}
