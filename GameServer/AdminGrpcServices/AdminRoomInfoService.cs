using CommonLibrary.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyProtos;
using StackExchange.Redis;

namespace GameServer.AdminGrpcServices
{
    [Authorize(Roles = "Admin")]
    public class AdminRoomInfoService(IConnectionMultiplexer cm) : AdminRoomInfo.AdminRoomInfoBase
    {
        private readonly IDatabase redis = cm.GetDatabase();

        public override async Task<MatchingQueueCountReply> GetMatchingQueueCount(MatchingQueueCountRequest request, ServerCallContext context)
        {
            var queueKey = AdminRoomInfoKeys.MatchingQueueCount(request.Region, request.Capacity);
            var count = (int)await redis.SortedSetLengthAsync(queueKey);
            return new MatchingQueueCountReply
            {
                Count = count
            };
        }

        public override async Task<MatchingQueueReply> GetMatchingQueue(MatchingQueueRequest request, ServerCallContext context)
        {
            var queueKey = AdminRoomInfoKeys.MatchingQueueCount(request.Region, request.Capacity);
            var uids = await redis.SortedSetRangeByRankAsync(queueKey, request.Offset, request.Offset + request.Count - 1, Order.Ascending);
            var reply = new MatchingQueueReply();
            foreach (var uid in uids)
            {
                var enqueuedAt = (long)await redis.HashGetAsync(RoomMatchKeys.MatchingUserMeta(uid!), RoomUserMetaHashKey.EnqueuedAt);
                reply.MatchingQueues.Add(new MatchingQueue
                {
                    Uid = uid,
                    EnqueuedAt = enqueuedAt
                });
            }
            return reply;
        }

        public override async Task<RoomInfoCountReply> GetRoomInfoCount(RoomInfoCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.RoomPattern());
            return await Task.FromResult(new RoomInfoCountReply
            {
                Count = roomKeys.Count()
            });
        }

        public override async Task<RoomInfoReply> GetRoomInfo(RoomInfoRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomInfoReply = new RoomInfoReply();
            var roomKeys = server.KeysAsync(pattern: AdminRoomInfoKeys.RoomPattern(), pageOffset: request.Offset, pageSize: request.Count);
            await foreach (var roomKey in roomKeys)
            {
                var json = await redis.StringGetAsync(roomKey);
                var roomInfo = RoomInfo.Parser.ParseJson(json);
                var roomMembers = await redis.SetMembersAsync(AdminRoomInfoKeys.RoomMembers(roomInfo.RoomId));
                foreach (var roomMember in roomMembers)
                {
                    roomInfo.Uids.Add(roomMember);
                }
                roomInfoReply.RoomInfos.Add(roomInfo);
            }
            return roomInfoReply;
        }
    }
}
