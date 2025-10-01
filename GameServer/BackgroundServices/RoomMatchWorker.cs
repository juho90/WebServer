using CommonLibrary.Services;
using GameServer.Services;

namespace GameServer.BackgroundServices
{
    public class RoomMatchWorker(WebSocketBroker webSocketBroker, RoomMatcher matcher, RoomMatchSettings settings, ILogger<RoomMatchWorker> log) : BackgroundService
    {
        private readonly WebSocketBroker WebSocketBroker = webSocketBroker;
        private readonly RoomMatcher roomMatcher = matcher;
        private readonly RoomMatchSettings roomMatchSettings = settings;
        private readonly ILogger<RoomMatchWorker> logger = log;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            logger.LogInformation("Lua MatchWorker started");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    foreach (var region in roomMatchSettings.Regions)
                    {
                        foreach (var capacity in roomMatchSettings.Capacities)
                        {
                            var roomId = await roomMatcher.TryRoomMatch(region, capacity, ct);
                            if (string.IsNullOrEmpty(roomId))
                            {
                                continue;
                            }
                            logger.LogInformation("matched {capacity}p in {region} => {room}", capacity, region, roomId);
                            WebSocketBroker.NotifyRoomCreate(roomId);
                        }
                    }
                    await Task.Delay(roomMatchSettings.LoopIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "loop error");
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}
