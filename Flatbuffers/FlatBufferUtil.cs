using Auth;
using Echo;
using Google.FlatBuffers;
using Room;

namespace Flatbuffers
{
    public enum FlatBufferId : byte
    {
        Authentication = 0,
        EchoMessage = 1,
        RoomEnter = 2,
    }

    public static class FlatBufferUtil
    {
        public static FlatBufferId GetFlatbufferId(byte[] bytes)
        {
            return (FlatBufferId)bytes[0];
        }

        public static byte[] SerializeAuthentication(string accessToken)
        {
            var builder = new FlatBufferBuilder(1024);
            var accessTokenOffset = builder.CreateString(accessToken);
            Authentication.StartAuthentication(builder);
            Authentication.AddAccessToken(builder, accessTokenOffset);
            var inputOffset = Authentication.EndAuthentication(builder);
            builder.Finish(inputOffset.Value);
            var flatBufferBytes = builder.SizedByteArray();
            var result = new byte[1 + flatBufferBytes.Length];
            result[0] = (byte)FlatBufferId.Authentication;
            Buffer.BlockCopy(flatBufferBytes, 0, result, 1, flatBufferBytes.Length);
            return result;
        }

        public static Authentication DeserializeAuthentication(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes, 1);
            return Authentication.GetRootAsAuthentication(buffer);
        }

        public static byte[] SerializeEchoMessage(string message)
        {
            var builder = new FlatBufferBuilder(1024);
            var messageOffset = builder.CreateString(message);
            EchoMessage.StartEchoMessage(builder);
            EchoMessage.AddMessage(builder, messageOffset);
            var inputOffset = EchoMessage.EndEchoMessage(builder);
            builder.Finish(inputOffset.Value);
            var flatBufferBytes = builder.SizedByteArray();
            var result = new byte[1 + flatBufferBytes.Length];
            result[0] = (byte)FlatBufferId.EchoMessage;
            Buffer.BlockCopy(flatBufferBytes, 0, result, 1, flatBufferBytes.Length);
            return result;
        }

        public static EchoMessage DeserializeEchoMessage(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes, 1);
            return EchoMessage.GetRootAsEchoMessage(buffer);
        }

        public static byte[] SerializeRoomEnter(string roomId)
        {
            var builder = new FlatBufferBuilder(1024);
            var roomIdOffset = builder.CreateString(roomId);
            RoomEnter.StartRoomEnter(builder);
            RoomEnter.AddRoomId(builder, roomIdOffset);
            var inputOffset = RoomEnter.EndRoomEnter(builder);
            builder.Finish(inputOffset.Value);
            var flatBufferBytes = builder.SizedByteArray();
            var result = new byte[1 + flatBufferBytes.Length];
            result[0] = (byte)FlatBufferId.RoomEnter;
            Buffer.BlockCopy(flatBufferBytes, 0, result, 1, flatBufferBytes.Length);
            return result;
        }

        public static RoomEnter DeserializeRoomEnter(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes, 1);
            return RoomEnter.GetRootAsRoomEnter(buffer);
        }
    }
}
