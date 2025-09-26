using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using WebServer.Services;

namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/match")]
    [Authorize]
    public class MatchController(IConnectionMultiplexer cm, RoomMatchConfig config) : ControllerBase
    {
        private readonly IDatabase redis = cm.GetDatabase();
        private readonly RoomMatchConfig config = config;

        [HttpPost("enter")]
        public async Task<IActionResult> Enter([FromQuery] string region, [FromQuery] int capacity, [FromQuery] int mmr)
        {
            if (config.Regions.Contains(region) is false)
            {
                return BadRequest(new
                {
                    error = "지원하지 않는 region입니다."
                });
            }
            if (config.Capacities.Contains(capacity) is false)
            {
                return BadRequest(new
                {
                    error = "지원하지 않는 capacity입니다."
                });
            }
            var uid = User.Identity?.Name!;
            var ticket = await redis.StringGetAsync(RoomMatchKeys.Ticket(uid));
            if (ticket.HasValue)
            {
                return Ok(new
                {
                    roomId = ticket
                });
            }
            var tran = redis.CreateTransaction();
            _ = tran.SortedSetAddAsync(RoomMatchKeys.Queue(region, capacity), uid, mmr);
            _ = tran.HashSetAsync(RoomMatchKeys.UserMeta(uid),
            [
                new("mmr", mmr),
                new("region", region),
                new("capacity", capacity),
                new("enqueuedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            ]);
            var ok = await tran.ExecuteAsync();
            return Ok(new
            {
                enqueued = ok
            });
        }

        [HttpGet("ticket")]
        public async Task<IActionResult> Ticket()
        {
            var uid = User.Identity?.Name!;
            var ticket = await redis.StringGetAsync(RoomMatchKeys.Ticket(uid));
            return Ok(new
            {
                roomId = (string?)ticket
            });
        }
    }
}
