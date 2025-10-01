using Auth;
using CommonLibrary.Services;
using Echo;
using Flatbuffers;
using GameServer.Services;
using Room;
using System.Net.WebSockets;

namespace GameServer
{
    public static class WebSocketProgram
    {
        public static async Task Main(HttpContext context)
        {
            var cts = new CancellationTokenSource();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                string? uid = null;
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
                            var authentication = FlatBufferUtil.DeserializeAuthentication(buffer);
                            uid = OnAuthentication(context, authentication);
                            break;

                        case FlatBufferId.EchoMessage:
                            var echoMessage = FlatBufferUtil.DeserializeEchoMessage(buffer);
                            await OnEchoMessage(webSocket, echoMessage, cts.Token);
                            break;

                        case FlatBufferId.RoomEnter:
                            var roomEnter = FlatBufferUtil.DeserializeRoomEnter(buffer);
                            await OnRoomEnter(context, webSocket, uid!, roomEnter);
                            break;

                        default:
                            break;
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static string OnAuthentication(HttpContext context, Authentication authentication)
        {
            var jwtValidator = context.RequestServices.GetRequiredService<JwtValidator>();
            var principal = jwtValidator.Validate(authentication.AccessToken);
            return principal.Identity?.Name!;
        }

        public static async Task OnEchoMessage(WebSocket webSocket, EchoMessage echoMessage, CancellationToken ct)
        {
            var buffer = FlatBufferUtil.SerializeEchoMessage(echoMessage.Message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, ct);
        }

        public static async Task OnRoomEnter(HttpContext context, WebSocket webSocket, string uid, RoomEnter roomEnter)
        {
            var roomMatchService = context.RequestServices.GetRequiredService<RoomMatchService>();
            var roomId = await roomMatchService.GetRoomId(uid);
            if (roomId != roomEnter.RoomId)
            {
                throw new Exception("잘못된 방 입장 시도입니다.");
            }
            WebSocketBroadcastManager.AddSocket(roomEnter.RoomId, webSocket);
        }
    }
}
