using System.Net.Http.Headers;
using TestClient.Extensions;

namespace TestClient.Commands.Handlers
{
    public class EnqueueHandle
    {
        public record AuthDto(string? AccessToken);
        public record MatchingDto(bool? Enqueued);
        public record MatchingStateDto(bool? IsMatching);
        public record RoomIdDto(string? RoomId);

        public static async Task Handler(CommandInput input)
        {
            if (!input.ArgDict.TryGetValue("count", out var countObj))
            {
                return;
            }
            if (countObj is not int count)
            {
                return;
            }
            if (!input.ArgDict.TryGetValue("each", out var eachObj))
            {
                return;
            }
            if (eachObj is not int each)
            {
                return;
            }
            var tasks = new List<Task>();
            for (var batch = 0; batch < count; batch++)
            {
                var task = Enqueue(input.URL, batch * each, each, input.CancellationToken);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task Enqueue(string url, int offset, int count, CancellationToken ct)
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(url) };
            for (var index = 0; index < count; index++)
            {
                var uid = $"testuser{offset + index}";
                var loginPayload = new { UID = uid };
                var authDto = await httpClient.PostToAsync<AuthDto>("api/auth/test-login", loginPayload, ct);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authDto?.AccessToken);
                var roomIdDto = await httpClient.GetToAsync<RoomIdDto>("api/match/room-id", ct);
                if (!string.IsNullOrEmpty(roomIdDto?.RoomId))
                {
                    Console.WriteLine($"[{uid}] room-id => {roomIdDto?.RoomId}");
                    continue;
                }
                var matchingStateDto = await httpClient.GetToAsync<MatchingStateDto>("api/match/matching-status", ct);
                if (matchingStateDto?.IsMatching == true)
                {
                    Console.WriteLine($"[{uid}] matching => {matchingStateDto?.IsMatching}");
                    continue;
                }
                var region = "kr";
                var capacity = 4;
                var mmr = 300;
                var matchingUri = $"api/match/matching?region={region}&capacity={capacity}&mmr={mmr}";
                var enterDto = await httpClient.PostToAsync<MatchingDto>(matchingUri, null, ct);
                Console.WriteLine($"[{uid}] try => {enterDto?.Enqueued}");
                await Task.Delay(50, ct);
            }
        }
    }
}
