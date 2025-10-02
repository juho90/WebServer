using BlazorApp.Client.Services;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyProtos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<WebSocketClient>()
    .AddScoped<FlatbufferClient>()
    .AddScoped(provider => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddScoped(sp =>
    {
        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());
        var channel = GrpcChannel.ForAddress("http://localhost:25050", new GrpcChannelOptions { HttpHandler = httpHandler });
        return new Greeter.GreeterClient(channel);
    });

await builder.Build().RunAsync();
