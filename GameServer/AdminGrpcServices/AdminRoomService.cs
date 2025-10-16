using CommonLibrary.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyProtos;
using StackExchange.Redis;

namespace GameServer.AdminGrpcServices
{
    [Authorize(Roles = "Admin")]
    public class AdminRoomService(IConnectionMultiplexer cm) : AdminRoom.AdminRoomBase
    {
        private readonly IDatabase redis = cm.GetDatabase();

        public override async Task<MatchingQueueCountReply> MatchingQueueCount(MatchingQueueCountRequest request, ServerCallContext context)
        {
            var queueKey = AdminRoomInfoKeys.MatchingQueueCount(request.Region, request.Capacity);
            var count = (int)await redis.SortedSetLengthAsync(queueKey);
            return new MatchingQueueCountReply
            {
                Count = count
            };
        }

        public override async Task<MatchingQueueReply> MatchingQueue(MatchingQueueRequest request, ServerCallContext context)
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

        public override async Task<MatchingUserCountReply> MatchingUserCount(MatchingUserCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingUserPattern());
            return await Task.FromResult(new MatchingUserCountReply
            {
                Count = userKeys.Count()
            });
        }

        public override async Task<MatchingUsersReply> MatchingUsers(MatchingUsersRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingUserPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            var reply = new MatchingUsersReply
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

        public override async Task<MatchingRoomCountReply> MatchingRoomCount(MatchingRoomCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingRoomPattern());
            return await Task.FromResult(new MatchingRoomCountReply
            {
                Count = roomKeys.Count()
            });
        }

        public override async Task<MatchingRoomsReply> MatchingRooms(MatchingRoomsRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var userKeys = server.Keys(pattern: AdminRoomInfoKeys.MatchingRoomPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            var reply = new MatchingRoomsReply
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

        public override async Task<RoomCountReply> RoomCount(RoomCountRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.RoomPattern());
            return await Task.FromResult(new RoomCountReply
            {
                Count = roomKeys.Count()
            });
        }

        public override async Task<RoomsReply> Rooms(RoomsRequest request, ServerCallContext context)
        {
            var endpoint = redis.Multiplexer.GetEndPoints()[0];
            var server = redis.Multiplexer.GetServer(endpoint);
            var roomInfoReply = new RoomsReply();
            var roomKeys = server.Keys(pattern: AdminRoomInfoKeys.RoomPattern())
                .Skip(request.Offset)
                .Take(request.Count)
                .ToArray();
            foreach (var roomKey in roomKeys)
            {
                var json = await redis.StringGetAsync(roomKey);
                var roomInfo = MyProtos.Room.Parser.ParseJson(json);
                var roomMembers = await redis.SetMembersAsync(AdminRoomInfoKeys.RoomMembers(roomInfo.RoomId));
                foreach (var roomMember in roomMembers)
                {
                    roomInfo.Uids.Add(roomMember);
                }
                roomInfoReply.Rooms.Add(roomInfo);
            }
            return roomInfoReply;
        }
    }
}
