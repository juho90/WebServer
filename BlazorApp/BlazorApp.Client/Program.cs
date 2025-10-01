using BlazorApp.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<WebSocketClient>()
    .AddScoped<AccessTokenHandler>()
    .AddScoped<FlatbufferClient>();

builder.Services.AddHttpClient("ApiClient", client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<AccessTokenHandler>();

await builder.Build().RunAsync();
