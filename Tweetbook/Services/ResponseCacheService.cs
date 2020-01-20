using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Tweetbook.Services
{
    public class ResponseCacheService : IResponseCacheService
    {
        private readonly IDistributedCache _distributedCache;

        public ResponseCacheService(IDistributedCache distributedCache) {
            _distributedCache = distributedCache;
        }

        public async Task cacheResponseAsync(string cacheKey, object response, TimeSpan timeToLive) {
            if (response == null) {
                return;
            }

            var serializeResponse = JsonConvert.SerializeObject(response);
            await _distributedCache.SetStringAsync(cacheKey, serializeResponse, new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = timeToLive
            });
        }

        public async Task<string> GetCacheResponseAsync(string cacheKey) {
            var cacheResponse = await _distributedCache.GetStringAsync(cacheKey);
            return string.IsNullOrEmpty(cacheResponse)
                ? null
                : cacheResponse;
        }
    }
}