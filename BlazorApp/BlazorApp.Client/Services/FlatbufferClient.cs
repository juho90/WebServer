using Flatbuffers;
using Microsoft.JSInterop;

namespace BlazorApp.Client.Services
{
    public class FlatbufferClient : IDisposable
    {
        private readonly WebSocketClient webSocketClient;
        private readonly IJSRuntime jSRuntime;
        private readonly Queue<byte[]> messageQueue = new Queue<byte[]>();

        public FlatbufferClient(WebSocketClient webSocketClient, IJSRuntime jSRuntime)
        {
            this.webSocketClient = webSocketClient;
            this.jSRuntime = jSRuntime;
            webSocketClient.OnReceive += OnReceive;
        }

        private void OnReceive(byte[] data)
        {
            messageQueue.Enqueue(data);
        }

        public bool Receive(out byte[]? data)
        {
            if (messageQueue.Count > 0)
            {
                data = messageQueue.Dequeue();
                return true;
            }
            data = null;
            return false;
        }

        public async Task ConnectAsync(string url)
        {
            await webSocketClient.ConnectAsync(url);
        }

        public async Task SendAuthenticationAsync()
        {
            var accessToken = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Access token is null or empty.");
            }
            var authBuffer = FlatBufferUtil.SerializeAuthentication(accessToken);
            await webSocketClient.SendAsync(authBuffer);
        }

        public void Dispose()
        {
            webSocketClient.OnReceive -= OnReceive;
            GC.SuppressFinalize(this);
        }
    }
}
