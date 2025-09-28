namespace BlazorApp.Services
{
    public class RoomMatchKeys
    {
        public static string Queue(string region, int capacity)
        {
            return $"match:q:{region}:{capacity}";
        }

        public static string UserMeta(string uid)
        {
            return $"match:u:{uid}";
        }

        public static string Ticket(string uid)
        {
            return $"match:t:{uid}";
        }

        public static string RoomMembers(Guid room)
        {
            return $"room:{room}:members";
        }

        public const string Events = "match:events";
    }
}
