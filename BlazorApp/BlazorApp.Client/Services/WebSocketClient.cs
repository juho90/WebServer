using Microsoft.JSInterop;

namespace BlazorApp.Client.Services
{
    public class WebSocketClient : IAsyncDisposable
    {
        private readonly IJSRuntime jsRuntime;
        private readonly DotNetObjectReference<WebSocketClient>? thisRef;
        private TaskCompletionSource? connectTcs = null;

        public event Action<byte[]>? OnReceive;

        public WebSocketClient(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            thisRef = DotNetObjectReference.Create(this);
        }

        public async Task ConnectAsync(string url)
        {
            connectTcs = new TaskCompletionSource();
            await jsRuntime.InvokeVoidAsync("webSocket.connect", url, thisRef);
            await connectTcs.Task;
        }

        public async Task SendAsync(byte[] data)
        {
            await jsRuntime.InvokeVoidAsync("webSocket.send", data);
        }

        public async Task CloseAsync()
        {
            await jsRuntime.InvokeVoidAsync("webSocket.close");
        }

        public async ValueTask DisposeAsync()
        {
            thisRef?.Dispose();
            await CloseAsync();
            GC.SuppressFinalize(this);
        }

        [JSInvokable]
        public void Open()
        {
            connectTcs?.TrySetResult();
        }

        [JSInvokable]
        public void ReceiveMessage(byte[] data)
        {
            OnReceive?.Invoke(data);
        }
    }
}
