namespace BlazorApp.Client.Extensions
{
    public static class TimeExtensions
    {
        public static int ToElapsedSeconds(this long currentAt)
        {
            var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (int)((milliseconds - currentAt) / 1000);
        }

        public static string FormatElapsed(this int seconds)
        {
            int min = seconds / 60;
            int sec = seconds % 60;
            return $"{min:D2}분 {sec:D2}초";
        }
    }
}
