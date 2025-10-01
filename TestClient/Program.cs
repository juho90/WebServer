using Flatbuffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TestClient;
using TestClient.Commands;

Console.WriteLine("Hello, World!");

var apiUrl = "http://localhost:29090";
try
{
    using var cts = new CancellationTokenSource();

    var uid = "testuser";

    using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
    Console.WriteLine($"API = {httpClient.BaseAddress}");

    var maxWaitSeconds = 30;
    var waitIntervalMs = 1000;
    var serverReady = false;
    for (var index = 0; index < maxWaitSeconds; index++)
    {
        try
        {
            var response = await httpClient.GetAsync($"health");
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

    var loginPayload = new { UID = uid };
    var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

    Console.WriteLine("JWT 토큰 발급 요청...");
    var resLogin = await httpClient.PostAsync($"api/auth/test-login", content);
    resLogin.EnsureSuccessStatusCode();
    var resStr = await resLogin.Content.ReadAsStringAsync();
    var resJson = JsonDocument.Parse(resStr).RootElement;
    var token = resJson.GetProperty("accessToken").GetString();

    Console.WriteLine($"발급받은 토큰: {token}");

    var wsUrl = "ws://localhost:25050/ws";

    var socketUri = new Uri(wsUrl);
    Console.WriteLine($"WS = {socketUri.AbsoluteUri}");

    using var wsClient = new ClientWebSocket();

    Console.WriteLine("WebSocket 연결 시도...");
    await wsClient.ConnectAsync(socketUri, cts.Token);
    Console.WriteLine("연결 성공!");

    var authBuffer = FlatBufferUtil.SerializeAuthentication(token!);
    await wsClient.SendAsync(new ArraySegment<byte>(authBuffer), WebSocketMessageType.Binary, true, cts.Token);
    Console.WriteLine("인증 메시지 전송");

    var sendBuffer = FlatBufferUtil.SerializeEchoMessage("Hello WebSocket!");
    await wsClient.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, true, cts.Token);
    Console.WriteLine("메시지 전송: Hello WebSocket!");

    var receiveBuffer = new byte[1024];
    var result = await wsClient.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
    var echoMessage = FlatBufferUtil.DeserializeEchoMessage(receiveBuffer);
    Console.WriteLine($"서버 응답: {echoMessage.Message}");

    await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "테스트 종료", cts.Token);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    Console.WriteLine("연결 종료");
}

Console.WriteLine("명령을 입력하세요");
var registry = new CommandRegistry();
CommandRegister.Register(registry);
using var cancellationTokenSource = new CancellationTokenSource();
while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }
    var commandArgs = CommandArgParser.Split(line.Trim());
    var commandName = commandArgs[0].ToLowerInvariant();
    if (commandName is "quit" or "exit")
    {
        cancellationTokenSource.Cancel();
        break;
    }
    if (!registry.TryGet(commandName, out var command))
    {
        Console.WriteLine("알 수 없는 명령입니다. 'help'를 입력하세요.");
        continue;
    }
    var commandArgOnly = commandArgs.Skip(1).ToArray();
    var validate = command.ValidateAndBind(commandArgOnly);
    if (!validate.Ok)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"인수 오류: {validate.Error}\n사용법: {command.Usage}");
        Console.ResetColor();
        continue;
    }
    try
    {
        await command.Handler(new CommandInput(registry
            , validate.ArgDict!
            , apiUrl
            , cancellationTokenSource.Token));
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"실행 오류: {ex.Message}");
        Console.ResetColor();
    }
}