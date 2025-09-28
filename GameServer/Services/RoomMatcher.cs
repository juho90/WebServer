using CommonLibrary.Services;
using StackExchange.Redis;

namespace GameServer.Services
{
    public class RoomMatcher(IConnectionMultiplexer mux, RoomMatchSettings settings)
    {
        private readonly IDatabase redis = mux.GetDatabase();
        private readonly RoomMatchSettings settings = settings;

        public async Task<Guid?> TryMatchAsync(string region, int capacity, CancellationToken ct = default)
        {
            var matchQueue = RoomMatchKeys.Queue(region, capacity);
            var queueEntries = await redis.SortedSetRangeByRankWithScoresAsync(matchQueue, 0, settings.UserPool - 1, Order.Ascending);
            if (queueEntries.Length < capacity)
            {
                return null;
            }
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var start = 0; start + capacity - 1 < queueEntries.Length; start++)
            {
                ct.ThrowIfCancellationRequested();
                var window = queueEntries.Skip(start).Take(capacity).ToArray();
                var uids = new RedisValue[capacity];
                var mmrs = new int[capacity];
                var minWait = long.MaxValue;
                for (var index = 0; index < capacity; index++)
                {
                    uids[index] = window[index].Element!;
                    mmrs[index] = (int)window[index].Score;
                    var uid = (string)uids[index]!;
                    var userMata = RoomMatchKeys.UserMeta(uid);
                    var w = await redis.HashGetAsync(userMata, "enqueuedAt");
                    if (w.HasValue)
                    {
                        var wait = now - (long)w;
                        if (wait < minWait)
                        {
                            minWait = wait;
                        }
                    }
                    else
                    {
                        minWait = Math.Min(minWait, 0);
                    }
                }
                var delta = settings.BaseMMR + ((int)(minWait / 1000 / 5) * settings.MMRPer5Sec);
                var roomId = Guid.NewGuid();
                var keys = new RedisKey[]
                {
                    matchQueue,
                    RoomMatchKeys.RoomMembers(roomId),
                    RoomMatchKeys.Events
                };
                var argv = new List<RedisValue>
                {
                    roomId.ToString(),
                    now,
                    delta,
                    settings.TicketTtlMs,
                    capacity
                };
                for (var index = 0; index < capacity; index++)
                {
                    argv.Add(uids[index]);
                    argv.Add(mmrs[index]);
                }
                var res = await redis.ScriptEvaluateAsync(RoomMatcherLua.MatchGroup, keys, [.. argv]);
                var ok = (int)res[0] == 1;
                if (ok)
                {
                    return roomId;
                }
            }
            return null;
        }
    }
}
