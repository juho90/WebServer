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
        public static async void Main(HttpContext context)
        {
            var token = context.Request.Headers.SecWebSocketProtocol.FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("JWT 토큰이 필요합니다.");
                return;
            }
            var result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                var jwtValidator = context.RequestServices.GetRequiredService<JwtValidator>();
                var principal = jwtValidator.Validate(token);
                var uid = principal.Identity?.Name!;
                var buffer = new byte[1024];
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    var flatbufferId = FlatBufferUtil.GetFlatbufferId(buffer);
                    switch (flatbufferId)
                    {
                        case FlatBufferId.EchoMessage:

                            var echoMessage = FlatBufferUtil.DeserializeEchoMessage(buffer);
                            await OnEchoMessage(webSocket, echoMessage);
                            break;

                        case FlatBufferId.RoomEnter:
                            var roomEnter = FlatBufferUtil.DeserializeRoomEnter(buffer);
                            await OnRoomEnter(context, webSocket, uid, roomEnter);
                            break;

                        default:
                            break;
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (result.CloseStatus.HasValue)
                {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
        }

        public static async Task OnEchoMessage(WebSocket webSocket, EchoMessage echoMessage)
        {
            var buffer = FlatBufferUtil.SerializeEchoMessage(echoMessage.Message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
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
