using CommonLibrary.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLibrary.Extensions
{
    public static class RoomMatchExtensions
    {
        public static IServiceCollection BindRoomMatchSettings(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSingleton(sp =>
            {
                var settings = new RoomMatchSettings();
                configuration.GetSection("RoomMatch").Bind(settings);
                return settings;
            });
        }
    }
}
