using CommonLibrary.Extensions;
using CommonLibrary.Services;
using GameServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddSingleton<JwtValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.UseWebSockets();
app.Map("/ws", async context =>
{
    var token = context.Request.Headers.SecWebSocketProtocol.FirstOrDefault();
    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("JWT 토큰이 필요합니다.");
        return;
    }
    var jwtValidator = context.RequestServices.GetRequiredService<JwtValidator>();
    try
    {
        var principal = jwtValidator.Validate(token);
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
    catch (Exception)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("JWT 토큰이 유효하지 않습니다.");
    }
});

app.Run();
