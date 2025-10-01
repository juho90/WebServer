using CommonLibrary.Extensions;
using CommonLibrary.Services;
using GameServer;
using GameServer.BackgroundServices;
using GameServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.BindRoomMatchSettings(builder.Configuration);
builder.Services.AddSingleton<JwtValidator>()
    .AddHostedService<RoomMatchWorker>()
    .AddSingleton<WebSocketBroker>()
    .AddSingleton<RoomMatcher>()
    .AddScoped<RoomMatchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.UseWebSockets();
app.Map("/ws", async (httpContext) =>
{
    var webSocketClient = new WebSocketClient(httpContext);
    await webSocketClient.Run();
});

app.Run();
