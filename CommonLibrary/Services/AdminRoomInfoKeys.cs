namespace CommonLibrary.Services
{
    public static class AdminRoomInfoKeys
    {
        public static string MatchingQueueCount(string region, int capacity)
        {
            return $"match:q:{region}:{capacity}";
        }

        public static string MatchingUserPattern()
        {
            return $"match:u:*";
        }

        public static string MatchingRoomPattern()
        {
            return $"match:r:*";
        }

        public static string RoomPattern()
        {
            return $"room:i:*";
        }

        public static string RoomMembers(string roomId)
        {
            return $"room:m:{roomId}";
        }
    }
}
