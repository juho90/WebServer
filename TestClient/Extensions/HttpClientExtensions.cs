using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TestClient.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetToAsync<T>(this HttpClient httpClient, string requestUri, CancellationToken ct)
        {
            using var response = await httpClient.GetAsync(requestUri, ct);
            response.EnsureSuccessStatusCode();
            ct.ThrowIfCancellationRequested();
            var result = await response.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new InvalidOperationException("Response content is null");
            return result;
        }

        public static async Task<T> PostToAsync<T>(this HttpClient httpClient, string requestUri, object? contentObj, CancellationToken ct)
        {
            StringContent? content = null;
            if (contentObj != null)
            {
                var contentStr = JsonSerializer.Serialize(contentObj);
                content = new StringContent(contentStr, Encoding.UTF8, "application/json");
            }
            using var response = await httpClient.PostAsync(requestUri, content, ct);
            response.EnsureSuccessStatusCode();
            ct.ThrowIfCancellationRequested();
            var result = await response.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new InvalidOperationException("Response content is null");
            return result;
        }
    }
}
