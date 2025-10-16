using GameServer.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyProtos;

namespace GameServer.GrpcServices
{
    [Authorize]
    public class CellverseGrpcService : MyProtos.Cellverse.CellverseBase
    {
        public override Task<CellsReply> Cells(CellsRequest request, ServerCallContext context)
        {
            var cellverseService = CellverseManager.GetOrAdd(request.RoomId);
            var reply = new CellsReply
            {
                RoomId = request.RoomId
            };
            var cells = cellverseService.GetCells();
            foreach (var cell in cells)
            {
                reply.Cells.Add(new Cell
                {
                    Id = cell.Id,
                    X = cell.X,
                    Y = cell.Y,
                    Size = cell.Size,
                });
            }
            return Task.FromResult(reply);
        }

        public override Task<EnterCellverseReply> EnterCellverse(EnterCellverseRequest request, ServerCallContext context)
        {
            var cellverseService = CellverseManager.GetOrAdd(request.RoomId);
            var httpContext = context.GetHttpContext();
            var uid = httpContext?.User?.Identity?.Name;
            var cell = cellverseService.SpawnCell(uid!);
            var data = Flatbuffers.CellverseUtil.SerializeCellSpawn(cell.Id, cell.X, cell.Y, cell.Size);
            cellverseService.Broadcast(data);
            var reply = new EnterCellverseReply { };
            return Task.FromResult(reply);
        }
    }
}
