using CommonLibrary.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyProtos;

namespace GameServer.GrpcServices
{
    [Authorize]
    public class RoomMatchGrpcService(RoomMatchService roomMatchService) : RoomMatcher.RoomMatcherBase
    {
        private readonly RoomMatchService roomMatchService = roomMatchService;

        public override async Task<MatchingReply> Matching(MatchingRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var uid = httpContext?.User?.Identity?.Name;
            await roomMatchService.Enqueue(uid!, request.Region, request.Capacity, request.Mmr);
            return new MatchingReply
            {
                Enqueued = true
            };
        }

        public override async Task<MatchingStatusReply> MatchingStatus(MatchingStatusRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var uid = httpContext?.User?.Identity?.Name;
            (var isMatched, var enqueuedAt) = await roomMatchService.IsMatching(uid!);
            return new MatchingStatusReply
            {
                IsMatched = isMatched,
                EnqueuedAt = enqueuedAt
            };
        }

        public override async Task<RoomIdReply> RoomId(RoomIdRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var uid = httpContext?.User?.Identity?.Name;
            var roomId = await roomMatchService.GetRoomId(uid!);
            return new RoomIdReply
            {
                RoomId = roomId ?? string.Empty
            };
        }
    }
}
