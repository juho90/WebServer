using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TestClient.Commands.Handlers
{
    public class EnqueueHandle
    {
        public record AuthDto(string? AccessToken);
        public record TicketDto(string? RoomId);
        public record EnterDto(bool? Enqueued);

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

        public static async Task Enqueue(string url, int offset, int count, CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(url) };
            for (var index = 0; index < count; index++)
            {
                var uid = $"testuser{offset + index}";
                var loginPayload = new { UID = uid };
                var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
                var resLogin = await httpClient.PostAsync($"api/auth/test-login", content, cancellationToken);
                resLogin.EnsureSuccessStatusCode();
                cancellationToken.ThrowIfCancellationRequested();
                var authDto = await resLogin.Content.ReadFromJsonAsync<AuthDto>(cancellationToken);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authDto?.AccessToken);
                using var resTicket = await httpClient.GetAsync("api/match/ticket", cancellationToken);
                resTicket.EnsureSuccessStatusCode();
                cancellationToken.ThrowIfCancellationRequested();
                var ticketDto = await resTicket.Content.ReadFromJsonAsync<TicketDto>(cancellationToken);
                if (!string.IsNullOrEmpty(ticketDto?.RoomId))
                {
                    Console.WriteLine($"[{uid}] ticketUri => {ticketDto?.RoomId}");
                    continue;
                }
                using var resEnqueue = await httpClient.PostAsync("api/match/enter?region=kr&capacity=4&mmr=300", content: null, cancellationToken);
                resEnqueue.EnsureSuccessStatusCode();
                cancellationToken.ThrowIfCancellationRequested();
                var enterDto = await resEnqueue.Content.ReadFromJsonAsync<EnterDto>(cancellationToken);
                Console.WriteLine($"[{uid}] enqueued => {enterDto?.Enqueued}");
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
