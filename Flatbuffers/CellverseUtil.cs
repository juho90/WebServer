using Cellverse;
using Google.FlatBuffers;

namespace Flatbuffers
{
    public enum CellverseId : byte
    {
        CellSpawn = 100,
    }

    public static class CellverseUtil
    {
        public static CellverseId GetCellverseId(byte[] bytes)
        {
            return (CellverseId)bytes[0];
        }

        public static byte[] SerializeCellSpawn(int id, float x, float y, int size)
        {
            var builder = new FlatBufferBuilder(1024);
            CellSpawn.StartCellSpawn(builder);
            CellSpawn.AddId(builder, id);
            CellSpawn.AddX(builder, x);
            CellSpawn.AddY(builder, y);
            CellSpawn.AddSize(builder, size);
            var inputOffset = CellSpawn.EndCellSpawn(builder);
            builder.Finish(inputOffset.Value);
            var flatBufferBytes = builder.SizedByteArray();
            var result = new byte[1 + flatBufferBytes.Length];
            result[0] = (byte)CellverseId.CellSpawn;
            Buffer.BlockCopy(flatBufferBytes, 0, result, 1, flatBufferBytes.Length);
            return result;
        }

        public static CellSpawn DeserializeCellSpawn(byte[] bytes)
        {
            var buffer = new ByteBuffer(bytes, 1);
            return CellSpawn.GetRootAsCellSpawn(buffer);
        }
    }
}
