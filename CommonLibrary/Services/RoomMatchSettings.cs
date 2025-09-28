namespace CommonLibrary.Services
{
    public class RoomMatchSettings
    {
        public string[] Regions { get; set; } = ["kr"];
        public int[] Capacities { get; set; } = [4];
        public int UserPool { get; set; } = 256;
        public int TicketTtlMs { get; set; } = 180_000;
        public int BaseMMR { get; set; } = 300;
        public int MMRPer5Sec { get; set; } = 50;
        public int LoopIntervalMs { get; set; } = 300;
    }
}
