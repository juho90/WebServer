using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using MyProtos;
using System.Net;
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
                var task = Enqueue(input.HttpURI
                    , input.GrpcURI
                    , batch * each
                    , each
                    , input.CancellationToken);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task Enqueue(string httpUri, string grpcUri, int offset, int count, CancellationToken ct)
        {
            // var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var httpClient = new HttpClient { BaseAddress = new Uri(httpUri) };
            var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());
            var grpcChannel = GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                HttpVersion = HttpVersion.Version11,
                HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact,
                // LoggerFactory = loggerFactory
            });
            var grpcClient = new RoomMatcher.RoomMatcherClient(grpcChannel);
            for (var index = 0; index < count; index++)
            {
                var uid = $"testuser{offset + index}";
                var loginPayload = new { UID = uid };
                var authDto = await httpClient.PostToAsync<AuthDto>("api/auth/test-login", loginPayload, ct);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authDto?.AccessToken);
                var grpcHeaders = new Metadata
                {
                    { "Authorization", $"Bearer {authDto?.AccessToken}" }
                };
                var roomIdReply = await grpcClient.RoomIdAsync(new RoomIdRequest { }, grpcHeaders, cancellationToken: ct);
                if (!string.IsNullOrEmpty(roomIdReply?.RoomId))
                {
                    Console.WriteLine($"[{uid}] room-id => {roomIdReply?.RoomId}");
                    continue;
                }
                var matchingStateReply = await grpcClient.MatchingStatusAsync(new MatchingStatusRequest { }, grpcHeaders, cancellationToken: ct);
                if (matchingStateReply?.IsMatched is true)
                {
                    Console.WriteLine($"[{uid}] matching => {matchingStateReply?.IsMatched}");
                    continue;
                }
                var enterDto = await grpcClient.MatchingAsync(new MatchingRequest
                {
                    Region = "kr",
                    Capacity = 4,
                    Mmr = 300
                }, grpcHeaders, cancellationToken: ct);

                Console.WriteLine($"[{uid}] try => {enterDto?.Enqueued}");

                await Task.Delay(50, ct);
            }
        }
    }
}
