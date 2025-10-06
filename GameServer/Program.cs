using CommonLibrary.Extensions;
using CommonLibrary.Services;
using GameServer;
using GameServer.BackgroundServices;
using GameServer.GrpcServices;
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

builder.Services.AddAuthorization();

// CORS 정책 추가
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:29090")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("grpc-status", "grpc-message", "grpc-encoding", "grpc-accept-encoding");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowBlazorClient");
app.UseGrpcWeb();
app.MapGrpcService<GreeterGrpcService>()
    .EnableGrpcWeb()
    .RequireAuthorization();
app.MapGrpcService<RoomMatchGrpcService>()
    .EnableGrpcWeb()
    .RequireAuthorization();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.UseWebSockets();
app.Map("/ws", async (httpContext) =>
{
    var webSocketClient = new WebSocketClient(httpContext);
    await webSocketClient.Run();
});

app.UseAuthorization();

app.Run();
