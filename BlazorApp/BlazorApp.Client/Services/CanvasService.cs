using Microsoft.JSInterop;

namespace BlazorApp.Client.Services
{
    public class CanvasService(IJSRuntime jSRuntime)
    {
        private readonly IJSRuntime jSRuntime = jSRuntime;

        public async Task InitializeAsync(string canvasId)
        {
            await jSRuntime.InvokeVoidAsync("canvas.initialize", canvasId);
        }

        public async Task ClearAsync()
        {
            await jSRuntime.InvokeVoidAsync("canvas.clear");
        }

        public async Task DrawCircleAsync(float x, float y, float radius, string color)
        {
            await jSRuntime.InvokeVoidAsync("canvas.drawCircle", x, y, radius, color);
        }
    }
}
