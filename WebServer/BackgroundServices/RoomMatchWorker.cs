using WebServer.Services;

namespace WebServer.BackgroundServices
{
    public class RoomMatchWorker(RoomMatcher matcher, RoomMatchConfig config, ILogger<RoomMatchWorker> log) : BackgroundService
    {
        private readonly RoomMatcher matcher = matcher;
        private readonly RoomMatchConfig config = config;
        private readonly ILogger<RoomMatchWorker> log = log;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            log.LogInformation("Lua MatchWorker started");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    foreach (var region in config.Regions)
                    {
                        foreach (var capacity in config.Capacities)
                        {
                            var room = await matcher.TryMatchAsync(region, capacity, ct);
                            if (room.HasValue)
                            {
                                log.LogInformation("matched {cap}p in {region} => {room}", capacity, region, room);
                            }
                        }
                    }
                    await Task.Delay(config.LoopIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "loop error");
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}
