namespace GameServer.Services
{
    public static class RoomMatcherLua
    {
        public const string MatchingScript = @"
local queue = KEYS[1]
local roomInfoKey = KEYS[2]
local roomMemberKey = KEYS[3]
local eventKey = KEYS[4]

local roomId = ARGV[1]
local now = tonumber(ARGV[2])
local ttl = tonumber(ARGV[3])
local region = ARGV[4]
local capacity = tonumber(ARGV[5])
local matchingMMR = tonumber(ARGV[6])

local uids = {}
local mmrs = {}
local minMMR = 1000000000
local maxMMR = -1000000000

local idx = 7
for i=1,capacity do
    local uid = ARGV[idx]
    idx = idx + 1
    local mmr = tonumber(ARGV[idx])
    idx = idx + 1
    uids[i] = uid
    mmrs[i] = mmr
    if mmr < minMMR
        then minMMR = mmr
    end
    if mmr > maxMMR
        then maxMMR = mmr
    end
end

if (maxMMR - minMMR) > matchingMMR then
    return {0, 'mmr_range_too_wide'}
end

-- 이미 티켓 있으면 실패
for i=1,capacity do
    local roomKey = 'match:r:' .. uids[i]
    if redis.call('EXISTS', roomKey) == 1 then
        return {0, uids[i]}
    end
end

-- 큐에서 모두 제거 (존재하지 않으면 실패)
for i=1,capacity do
    local rem = redis.call('ZREM', queue, uids[i])
    if rem ~= 1 then
        return {0, uids[i]}
    end
end

-- 티켓 발급 및 룸 멤버 등록
for i=1,capacity do
    local roomKey = 'match:r:' .. uids[i]
    redis.call('SET', roomKey, roomId, 'PX', ttl)
end
redis.call('SADD', roomMemberKey, unpack(uids))
redis.call('PEXPIRE', roomMemberKey, ttl)

-- roomInfoKey에 JSON 정보 저장
local roomInfoJson = string.format(
    '{""roomId"":""%s"",""region"":""%s"",""capacity"":%d,""mmr"":%d,""createdAt"":%d}',
    roomId, region, capacity, matchingMMR, now
)
redis.call('SET', roomInfoKey, roomInfoJson, 'PX', ttl)

-- 이벤트 기록
local fields = {
    'type',
    'matchedN',
    'roomId',
    roomId,
    'ts',
    now,
    'capacity',
    capacity
}
for i=1,capacity do
    table.insert(fields, 'member.'..i);
    table.insert(fields, uids[i]);
end
redis.call('XADD', eventKey, '*', unpack(fields))

return {1, roomId}
";
    }
}
