using StackExchange.Redis;

namespace CommonLibrary.Services
{
    public class RoomMatchService(IConnectionMultiplexer cm, RoomMatchSettings settings)
    {
        private readonly IDatabase redis = cm.GetDatabase();
        private readonly RoomMatchSettings settings = settings;

        public async Task Enqueue(string uid, string region, int capacity, int mmr)
        {
            if (settings.Regions.Contains(region) is false)
            {
                throw new ArgumentException("지원하지 않는 region입니다.");
            }
            if (settings.Capacities.Contains(capacity) is false)
            {
                throw new ArgumentException("지원하지 않는 capacity입니다.");
            }
            (var isMatching, _) = await IsMatching(uid);
            if (isMatching)
            {
                throw new InvalidOperationException("이미 매칭 대기열에 등록된 상태입니다.");
            }
            var roomId = await GetRoomId(uid);
            if (!string.IsNullOrEmpty(roomId))
            {
                throw new InvalidOperationException("이미 매칭이 완료된 상태입니다.");
            }
            var tran = redis.CreateTransaction();
            _ = tran.SortedSetAddAsync(RoomMatchKeys.Queue(region, capacity), uid, mmr);
            _ = tran.HashSetAsync(RoomMatchKeys.UserMeta(uid),
            [
                new(RoomUserMetaHashKey.Region, region),
                new(RoomUserMetaHashKey.Capacity, capacity),
                new(RoomUserMetaHashKey.MMR, mmr),
                new(RoomUserMetaHashKey.EnqueuedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            ]);
            await tran.ExecuteAsync();
        }

        public async Task<(bool isMatching, long enqueuedAt)> IsMatching(string uid)
        {
            bool isMatching = false;
            foreach (var region in settings.Regions)
            {
                foreach (var capacity in settings.Capacities)
                {
                    var queueKey = RoomMatchKeys.Queue(region, capacity);
                    var score = await redis.SortedSetScoreAsync(queueKey, uid);
                    if (score.HasValue)
                    {
                        isMatching = true;
                        break;
                    }
                }
                if (isMatching)
                {
                    break;
                }
            }
            var enqueuedAt = 0L;
            if (isMatching)
            {
                enqueuedAt = (long)await redis.HashGetAsync(RoomMatchKeys.UserMeta(uid), RoomUserMetaHashKey.EnqueuedAt);
            }
            return (isMatching, enqueuedAt);
        }

        public async Task<string?> GetRoomId(string uid)
        {
            var roomId = await redis.StringGetAsync(RoomMatchKeys.Room(uid));
            return roomId;
        }
    }
}
