using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

var apiUrl = "http://localhost:28080";
var username = "testuser";

using var httpClient = new HttpClient();
var maxWaitSeconds = 30;
var waitIntervalMs = 1000;
var serverReady = false;
for (var index = 0; index < maxWaitSeconds; index++)
{
    try
    {
        var response = await httpClient.GetAsync($"{apiUrl}/health");
        if (response.IsSuccessStatusCode)
        {
            serverReady = true;
            break;
        }
    }
    catch
    {
    }
    Console.WriteLine("서버 대기 중...");
    await Task.Delay(waitIntervalMs);
}

if (!serverReady)
{
    Console.WriteLine("서버가 시작되지 않았습니다. 프로그램을 종료합니다.");
    return;
}

var loginPayload = new { Username = username };
var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

Console.WriteLine("JWT 토큰 발급 요청...");
var responseLogin = await httpClient.PostAsync($"{apiUrl}/api/auth/test-login", content);
responseLogin.EnsureSuccessStatusCode();
var responseStr = await responseLogin.Content.ReadAsStringAsync();
var responseJson = JsonDocument.Parse(responseStr).RootElement;
var token = responseJson.GetProperty("accessToken").GetString();

Console.WriteLine($"발급받은 토큰: {token}");

var socketUri = new Uri("ws://localhost:28080/ws");

using var client = new ClientWebSocket();
client.Options.SetRequestHeader("Sec-WebSocket-Protocol", token);

Console.WriteLine("WebSocket 연결 시도...");
await client.ConnectAsync(socketUri, CancellationToken.None);
Console.WriteLine("연결 성공!");

var sendBuffer = Encoding.UTF8.GetBytes("Hello, WebSocket!");
await client.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
Console.WriteLine("메시지 전송: Hello WebSocket!");

var receiveBuffer = new byte[1024];
var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
var receivedText = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
Console.WriteLine($"서버 응답: {receivedText}");

await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "테스트 종료", CancellationToken.None);
Console.WriteLine("연결 종료");

Console.WriteLine("아무 키나 누르면 프로그램이 종료됩니다.");
Console.ReadKey();
