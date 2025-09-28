using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace BlazorApp.Client.Services
{
    public class AccessTokenHandler(IJSRuntime jsRuntime) : DelegatingHandler
    {
        private readonly IJSRuntime jsRuntime = jsRuntime;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
