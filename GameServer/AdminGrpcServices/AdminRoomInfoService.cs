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
            var reply = new MatchingQueueReply
            {
                Region = request.Region,
                Capacity = request.Capacity
            };
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

        public override async Task<MatchingUserCountReply> GetMatchingUserCount(MatchingUserCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingUserPattern());
            return await Task.FromResult(new MatchingUserCountReply
            {
                Count = userKeys.Count()
            });
        }

        public override async Task<MatchingUserReply> GetMatchingUser(MatchingUserRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingUserPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            var reply = new MatchingUserReply
            {
                Region = request.Region,
                Capacity = request.Capacity
            };
            foreach (var userKey in userKeys)
            {
                var uid = userKey.ToString().Split(':')[2];
                var enqueuedAt = (long)await redis.HashGetAsync(RoomMatchKeys.MatchingUserMeta(uid!), RoomUserMetaHashKey.EnqueuedAt);
                reply.MatchingUsers.Add(new MatchingUser
                {
                    Uid = uid,
                    EnqueuedAt = enqueuedAt
                });
            }
            return reply;
        }

        public override async Task<MatchingRoomCountReply> GetMatchingRoomCount(MatchingRoomCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingRoomPattern());
            return await Task.FromResult(new MatchingRoomCountReply
            {
                Count = roomKeys.Count()
            });
        }

        public override async Task<MatchingRoomReply> GetMatchingRoom(MatchingRoomRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingRoomPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            var reply = new MatchingRoomReply
            {
                Region = request.Region,
                Capacity = request.Capacity
            };
            foreach (var userKey in userKeys)
            {
                var uid = userKey.ToString().Split(':')[2];
                var roomId = await redis.StringGetAsync(RoomMatchKeys.MatchedRoomId(uid!));
                var enqueuedAt = (long)await redis.HashGetAsync(RoomMatchKeys.MatchingUserMeta(uid!), RoomUserMetaHashKey.EnqueuedAt);
                reply.MatchingRooms.Add(new MatchingRoom
                {
                    Uid = uid,
                    RoomId = roomId,
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
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.RoomPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            foreach (var roomKey in roomKeys)
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
