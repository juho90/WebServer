namespace CommonLibrary.Services
{
    public static class RoomMatchKeys
    {
        public static string MatchingQueue(string region, int capacity)
        {
            return $"match:q:{region}:{capacity}";
        }

        public static string MatchingUserMeta(string uid)
        {
            return $"match:u:{uid}";
        }

        public static string MatchedRoomId(string uid)
        {
            return $"match:r:{uid}";
        }

        public static string RoomInfo(string roomId)
        {
            return $"room:i:{roomId}";
        }

        public static string RoomMembers(string roomId)
        {
            return $"room:m:{roomId}";
        }

        public const string Events = "match:events";
    }
}
