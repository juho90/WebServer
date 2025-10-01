using CommonLibrary.Services;
using Flatbuffers;
using GameServer.Services;
using System.Net.WebSockets;

namespace GameServer
{
    public class WebSocketClient(HttpContext httpContext)
    {
        private readonly HttpContext httpContext = httpContext;
        private readonly JwtValidator jwtValidator = httpContext.RequestServices.GetRequiredService<JwtValidator>();
        private readonly RoomMatchService roomMatchService = httpContext.RequestServices.GetRequiredService<RoomMatchService>();
        private readonly ILogger<WebSocketClient> logger = httpContext.RequestServices.GetRequiredService<ILogger<WebSocketClient>>();
        private readonly CancellationTokenSource cts = new();

        public async Task Run()
        {
            using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
            string? uid = null;
            try
            {
                var buffer = new byte[1024];
                do
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.CloseStatus.HasValue)
                    {
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cts.Token);
                        break;
                    }
                    var flatbufferId = FlatBufferUtil.GetFlatbufferId(buffer);
                    if (string.IsNullOrEmpty(uid) && flatbufferId != FlatBufferId.Authentication)
                    {
                        throw new Exception("인증되지 않은 상태에서 다른 메시지를 보낼 수 없습니다.");
                    }
                    switch (flatbufferId)
                    {
                        case FlatBufferId.Authentication:
                            uid = OnAuthentication(webSocket, buffer);
                            break;

                        case FlatBufferId.EchoMessage:
                            await OnEchoMessage(webSocket, buffer);
                            break;

                        case FlatBufferId.RoomEnter:
                            await OnRoomEnter(webSocket, uid!, buffer);
                            break;

                        default:
                            break;
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
            finally
            {
                await OnClose(webSocket, uid);
            }
        }

        private string OnAuthentication(WebSocket webSocket, byte[] buffer)
        {
            var authentication = FlatBufferUtil.DeserializeAuthentication(buffer);
            var principal = jwtValidator.Validate(authentication.AccessToken);
            var uid = principal.Identity?.Name!;
            WebSocketBroadcastManager.Add(uid, webSocket);
            return uid;
        }

        private async Task OnEchoMessage(WebSocket webSocket, byte[] buffer)
        {
            var echoMessage = FlatBufferUtil.DeserializeEchoMessage(buffer);
            var echoMessageBuffer = FlatBufferUtil.SerializeEchoMessage(echoMessage.Message);
            await webSocket.SendAsync(new ArraySegment<byte>(echoMessageBuffer), WebSocketMessageType.Binary, true, cts.Token);
        }

        private async Task OnRoomEnter(WebSocket webSocket, string uid, byte[] buffer)
        {
            var roomEnter = FlatBufferUtil.DeserializeRoomEnter(buffer);
            var roomId = await roomMatchService.GetRoomId(uid);
            if (roomId != roomEnter.RoomId)
            {
                throw new Exception("잘못된 방 입장 시도입니다.");
            }
            WebSocketBroadcastManager.AddGroup(roomEnter.RoomId, webSocket);
        }

        private async Task OnClose(WebSocket webSocket, string? uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return;
            }
            WebSocketBroadcastManager.Remove(uid);
            var roomId = await roomMatchService.GetRoomId(uid);
            if (string.IsNullOrEmpty(roomId))
            {
                return;
            }
            WebSocketBroadcastManager.AddGroup(roomId, webSocket);
        }
    }
}
