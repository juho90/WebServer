using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BlazorApp.Client.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task AuthorizeAsync(this HttpClient httpClient, IJSRuntime jsRuntime)
        {
            var token = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("accessToken is empty");
            }
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

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
