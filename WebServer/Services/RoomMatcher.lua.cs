namespace WebServer.Services
{
    public static class RoomMatcherLua
    {
        public const string MatchGroup = @"
local queue = KEYS[1]
local roomKey = KEYS[2]
local evKey = KEYS[3]

local roomId = ARGV[1]
local now = tonumber(ARGV[2])
local delta = tonumber(ARGV[3])
local ttl = tonumber(ARGV[4])
local count = tonumber(ARGV[5])

local uids = {}
local mmrs = {}
local minmmr = 1000000000
local maxmmr = -1000000000

local idx = 6
for i=1,count do
    local uid = ARGV[idx]
    idx = idx + 1
    local mmr = tonumber(ARGV[idx])
    idx = idx + 1
    uids[i] = uid
    mmrs[i] = mmr
    if mmr < minmmr
        then minmmr = mmr
    end
    if mmr > maxmmr
        then maxmmr = mmr
    end
end

if (maxmmr - minmmr) > delta then
    return {0, 'mmr_range_too_wide'}
end

-- 이미 티켓 있으면 실패
for i=1,count do
    local tkey = 'match:t:' .. uids[i]
    if redis.call('EXISTS', tkey) == 1 then
        return {0, 'ticket_exists', uids[i]}
    end
end

-- 큐에서 모두 제거 (존재하지 않으면 실패)
for i=1,count do
    local rem = redis.call('ZREM', queue, uids[i])
    if rem ~= 1 then
        return {0, 'not_in_queue', uids[i]}
    end
end

-- 티켓 발급 및 룸 멤버 등록
for i=1,count do
    local tkey = 'match:t:' .. uids[i]
    redis.call('SET', tkey, roomId, 'PX', ttl)
end
redis.call('SADD', roomKey, unpack(uids))
redis.call('PEXPIRE', roomKey, ttl)

-- 이벤트 기록
local fields = {
    'type',
    'matchedN',
    'roomId',
    roomId,
    'ts',
    now,
    'count',
    count
}
for i=1,count do
    table.insert(fields, 'member.'..i);
    table.insert(fields, uids[i]);
end
redis.call('XADD', evKey, '*', unpack(fields))

return {1, roomId}
";
    }
}
