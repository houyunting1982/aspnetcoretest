using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tweetbook.Cache;
using Tweetbook.Services;

namespace Tweetbook.Installers
{
    public class CacheInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration) {
            var redisCatchSettings = new RedisCacheSettings();
            configuration.GetSection(nameof(RedisCacheSettings)).Bind(redisCatchSettings);
            services.AddSingleton(redisCatchSettings);
            if (!redisCatchSettings.Enabled) {
                return;
            }

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisCatchSettings.ConnectionString));
            services.AddStackExchangeRedisCache(options => {
                options.Configuration = redisCatchSettings.ConnectionString;
            });
            services.AddSingleton<IResponseCacheService, ResponseCacheService>();
        }
    }
}