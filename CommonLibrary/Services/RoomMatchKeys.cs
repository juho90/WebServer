namespace CommonLibrary.Services
{
    public class RoomMatchKeys
    {
        public static string Queue(string region, int capacity)
        {
            return $"match:q:{region}:{capacity}";
        }

        public static string Room(string uid)
        {
            return $"match:r:{uid}";
        }

        public static string UserMeta(string uid)
        {
            return $"match:u:{uid}";
        }

        public static string RoomMembers(string roomId)
        {
            return $"room:{roomId}:members";
        }

        public const string Events = "match:events";
    }
}
