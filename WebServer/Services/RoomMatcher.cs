using StackExchange.Redis;

namespace WebServer.Services
{
    public class RoomMatcher(IConnectionMultiplexer mux, RoomMatchConfig config)
    {
        private readonly IDatabase redis = mux.GetDatabase();
        private readonly RoomMatchConfig config = config;

        public async Task<Guid?> TryMatchAsync(string region, int capacity, CancellationToken ct = default)
        {
            var matchQueue = RoomMatchKeys.Queue(region, capacity);
            var queueEntries = await redis.SortedSetRangeByRankWithScoresAsync(matchQueue, 0, config.UserPool - 1, Order.Ascending);
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
                    var uid = Guid.Parse((string)uids[index]!);
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
                var delta = config.BaseMMR + ((int)(minWait / 1000 / 5) * config.MMRPer5Sec);
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
                    config.TicketTtlMs,
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

    public class RoomMatchConfig
    {
        public string[] Regions { get; set; } = ["kr"];
        public int[] Capacities { get; set; } = [4];
        public int UserPool { get; set; } = 256;
        public int TicketTtlMs { get; set; } = 180_000;
        public int BaseMMR { get; set; } = 300;
        public int MMRPer5Sec { get; set; } = 50;
        public int LoopIntervalMs { get; set; } = 300;
    }

    public class RoomMatchKeys
    {
        public static string Queue(string region, int cap)
        {
            return $"match:q:{region}:{cap}";
        }

        public static string UserMeta(Guid uid)
        {
            return $"match:u:{uid}";
        }

        public static string Ticket(Guid uid)
        {
            return $"match:t:{uid}";
        }

        public static string RoomMembers(Guid room)
        {
            return $"room:{room}:members";
        }

        public const string Events = "match:events";
    }
}
