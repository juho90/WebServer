using CommonLibrary.Services;
using GameServer.Services;

namespace WebServer.BackgroundServices
{
    public class RoomMatchWorker(RoomMatcher matcher, RoomMatchSettings settings, ILogger<RoomMatchWorker> log) : BackgroundService
    {
        private readonly RoomMatcher matcher = matcher;
        private readonly RoomMatchSettings settings = settings;
        private readonly ILogger<RoomMatchWorker> log = log;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            log.LogInformation("Lua MatchWorker started");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    foreach (var region in settings.Regions)
                    {
                        foreach (var capacity in settings.Capacities)
                        {
                            var room = await matcher.TryMatchAsync(region, capacity, ct);
                            if (room.HasValue)
                            {
                                log.LogInformation("matched {capacity}p in {region} => {room}", capacity, region, room);
                            }
                        }
                    }
                    await Task.Delay(settings.LoopIntervalMs, ct);
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
