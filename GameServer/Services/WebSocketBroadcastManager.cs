using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Services
{
    public static class WebSocketBroadcastManager
    {
        private static readonly ConcurrentDictionary<string, WebSocket> webSocketPersonal = new();
        private static readonly ConcurrentDictionary<string, List<WebSocket>> webSocketGroups = new();

        public static void Add(string uid, WebSocket socket)
        {
            webSocketPersonal[uid] = socket;
        }

        public static void Remove(string uid)
        {
            webSocketPersonal.TryRemove(uid, out _);
        }

        public static void Broadcast(string uid, byte[] data)
        {
            if (webSocketPersonal.TryGetValue(uid, out var webSocket) is false)
            {
                return;
            }
            webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public static List<WebSocket> GetGroup(string groupId)
        {
            return webSocketGroups.TryGetValue(groupId, out var webSocketGroup)
                ? webSocketGroup
                : throw new Exception($"Group not found: {groupId}");
        }

        public static void AddGroup(string groupId, WebSocket webSocket)
        {
            var webSocketGroup = webSocketGroups.GetOrAdd(groupId, _ => []);
            lock (webSocketGroup)
            {
                webSocketGroup.Add(webSocket);
            }
        }

        public static void RemoveGroup(string groupId, WebSocket webSocket)
        {
            if (webSocketGroups.TryGetValue(groupId, out var webSocketGroup) is false)
            {
                return;
            }
            lock (webSocketGroup)
            {
                webSocketGroup.Remove(webSocket);
            }
        }

        public static void BroadcastGroup(string roomId, byte[] data)
        {
            if (webSocketGroups.TryGetValue(roomId, out var webSocketGroup) is false)
            {
                return;
            }
            BroadcastGroup(webSocketGroup, data);
        }

        public static void BroadcastGroup(List<WebSocket> webSocketGroup, byte[] data)
        {
            lock (webSocketGroup)
            {
                List<WebSocket> closedWebSockets = [];
                foreach (var webSocket in webSocketGroup)
                {
                    try
                    {
                        if (webSocket.State is WebSocketState.Open)
                        {
                            webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
                        }
                        else
                        {
                            closedWebSockets.Add(webSocket);
                        }
                    }
                    catch
                    {
                        closedWebSockets.Add(webSocket);
                    }
                }
                foreach (var s in closedWebSockets)
                {
                    webSocketGroup.Remove(s);
                }
            }
        }
    }
}