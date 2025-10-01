using Flatbuffers;

namespace GameServer.Services
{
    public class WebSocketBroker(RoomMatcher roomMatcher)
    {
        private readonly RoomMatcher roomMatcher = roomMatcher;

        public async void NotifyRoomCreate(string roomId)
        {
            var roomCreateBuffer = FlatBufferUtil.SerializeRoomCreate(roomId);
            var roomMembers = await roomMatcher.GetRoomMembers(roomId);
            foreach (var roomMember in roomMembers)
            {
                WebSocketBroadcastManager.BroadcastAsync(roomMember, roomCreateBuffer);
            }
        }
    }
}
